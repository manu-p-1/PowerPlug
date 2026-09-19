using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Lists every TCP/UDP socket (not just listeners) and, where possible, the process that owns it. This shells out to
/// the same platform tools as <see cref="ListeningPortOwners"/> (netstat, lsof, ss) but keeps connected and closing
/// sockets instead of filtering down to listeners only.
/// </summary>
internal static class NetworkConnections
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private static readonly Regex SsUsers = new(@"\(\(""(?<name>[^""]+)"",pid=(?<pid>\d+)", RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>A single connection row.</summary>
    public readonly record struct Connection(
        string Protocol,
        string LocalAddress,
        int LocalPort,
        string? RemoteAddress,
        int? RemotePort,
        string? State,
        int? ProcessId,
        string? ProcessName);

    /// <summary>
    /// Returns every socket currently known to the platform tool. Returns an empty list when the tool is unavailable.
    /// </summary>
    public static List<Connection> Discover()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ParseNetstat(ProcessRunner.TryRun("netstat", "-ano", ToolTimeout));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ParseLsof(ProcessRunner.TryRun("lsof", "-nP -iTCP -iUDP", ToolTimeout));
        }

        return ParseSs(ProcessRunner.TryRun("ss", "-Hntup", ToolTimeout));
    }

    /// <summary>
    /// Parses netstat -ano output, keeping every state. Exposed for testing.
    /// </summary>
    internal static List<Connection> ParseNetstat(string? output)
    {
        var result = new List<Connection>();
        foreach (var line in Lines(output))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts[0] is not ("TCP" or "UDP"))
            {
                continue;
            }

            var isTcp = parts[0] == "TCP";
            if (isTcp && parts.Length < 5)
            {
                continue;
            }

            if (!int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            {
                continue;
            }

            var (localAddress, localPort) = SplitEndpoint(parts[1]);
            if (localPort is null)
            {
                continue;
            }

            var (remoteAddress, remotePort) = SplitEndpoint(parts[2]);
            var state = isTcp ? parts[3] : null;

            result.Add(new Connection(parts[0], localAddress, localPort.Value, remoteAddress, remotePort, state, pid == 0 ? null : pid, ProcessNameOf(pid)));
        }

        return result;
    }

    /// <summary>
    /// Parses plain (non -F) lsof -i output. Command names may be truncated by lsof's column width; the process id
    /// is used to look up the full name instead. Exposed for testing.
    /// </summary>
    internal static List<Connection> ParseLsof(string? output)
    {
        var result = new List<Connection>();
        var lines = Lines(output);
        if (lines.Length == 0)
        {
            return result;
        }

        // Header: COMMAND PID USER FD TYPE DEVICE SIZE/OFF NODE NAME [STATE]
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
            {
                continue;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            {
                continue;
            }

            // The connection state, when present, is a separate trailing token like "(ESTABLISHED)".
            var last = parts[^1];
            string? state = null;
            var nameIndex = parts.Length - 1;
            var protocolIndex = parts.Length - 2;
            if (last.StartsWith('(') && last.EndsWith(')'))
            {
                state = last[1..^1];
                nameIndex = parts.Length - 2;
                protocolIndex = parts.Length - 3;
            }

            if (protocolIndex < 0)
            {
                continue;
            }

            var protocol = parts[protocolIndex];
            if (protocol is not ("TCP" or "UDP"))
            {
                continue;
            }

            var name = parts[nameIndex];
            var arrow = name.IndexOf("->", StringComparison.Ordinal);
            var localPart = arrow >= 0 ? name[..arrow] : name;
            var remotePart = arrow >= 0 ? name[(arrow + 2)..] : null;

            var (localAddress, localPort) = SplitEndpoint(localPart);
            if (localPort is null)
            {
                continue;
            }

            var (remoteAddress, remotePort) = remotePart is null ? (null, (int?)null) : SplitEndpoint(remotePart);

            result.Add(new Connection(protocol, localAddress, localPort.Value, remoteAddress, remotePort, state, pid, ProcessNameOf(pid)));
        }

        return result;
    }

    /// <summary>
    /// Parses ss -Hntup output, keeping every state. Exposed for testing.
    /// </summary>
    internal static List<Connection> ParseSs(string? output)
    {
        var result = new List<Connection>();
        foreach (var line in Lines(output))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            var protocol = parts[0].ToUpperInvariant();
            if (protocol is not ("TCP" or "UDP"))
            {
                continue;
            }

            // Columns: Netid State Recv-Q Send-Q Local-Address:Port Peer-Address:Port [Process]
            var (localAddress, localPort) = SplitEndpoint(parts[4]);
            if (localPort is null)
            {
                continue;
            }

            var (remoteAddress, remotePort) = SplitEndpoint(parts[5]);
            var users = SsUsers.Match(line);
            int? pid = null;
            string? processName = null;
            if (users.Success)
            {
                pid = int.Parse(users.Groups["pid"].Value, CultureInfo.InvariantCulture);
                processName = users.Groups["name"].Value;
            }

            result.Add(new Connection(protocol, localAddress, localPort.Value, remoteAddress, remotePort, parts[1], pid, processName));
        }

        return result;
    }

    private static string[] Lines(string? output) =>
        string.IsNullOrEmpty(output)
            ? []
            : output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Splits "address:port" into its parts, tolerating IPv6 addresses (with or without brackets) and the "*"/"::"
    /// wildcards used by netstat, lsof and ss. Returns a null port when the endpoint is not recognisable.
    /// </summary>
    private static (string Address, int? Port) SplitEndpoint(string endpoint)
    {
        if (endpoint is "*:*" or "*")
        {
            return ("*", null);
        }

        if (endpoint.StartsWith('[') && endpoint.Contains(']', StringComparison.Ordinal))
        {
            var close = endpoint.IndexOf(']', StringComparison.Ordinal);
            var address = endpoint[1..close];
            var rest = endpoint[(close + 1)..];
            var colon = rest.LastIndexOf(':');
            return colon >= 0 && int.TryParse(rest.AsSpan(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bracketedPort)
                ? (address, bracketedPort)
                : (address, null);
        }

        var lastColon = endpoint.LastIndexOf(':');
        if (lastColon < 0)
        {
            return (endpoint, null);
        }

        var addr = endpoint[..lastColon];
        var portText = endpoint[(lastColon + 1)..];
        if (portText == "*")
        {
            return (addr, null);
        }

        return int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) ? (addr, port) : (endpoint, null);
    }

    private static string? ProcessNameOf(int pid)
    {
        if (pid <= 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }
}
