using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Finds which process owns each listening socket. .NET can list listeners but not their owners, so this shells out
/// to the platform tool: netstat on Windows, lsof on macOS, ss on Linux.
/// </summary>
internal static class ListeningPortOwners
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    // ss: users:(("sshd",pid=1234,fd=3))
    private static readonly Regex SsUsers = new(@"\(\(""(?<name>[^""]+)"",pid=(?<pid>\d+)", RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>
    /// Owner of a socket.
    /// </summary>
    public readonly record struct Owner(int ProcessId, string? ProcessName);

    /// <summary>
    /// Returns a map from (protocol, port) to owner. The map is empty when the platform tool is unavailable.
    /// </summary>
    public static Dictionary<(string Protocol, int Port), Owner> Discover()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ParseNetstat(ProcessRunner.TryRun("netstat", "-ano", ToolTimeout));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // -F emits one field per line (p=pid, c=command, n=name) so command names with spaces are safe.
            var tcp = ProcessRunner.TryRun("lsof", "-nP -F pcn -iTCP -sTCP:LISTEN", ToolTimeout);
            var udp = ProcessRunner.TryRun("lsof", "-nP -F pcn -iUDP", ToolTimeout);
            var owners = ParseLsof(tcp, "TCP");
            foreach (var (key, owner) in ParseLsof(udp, "UDP"))
            {
                owners.TryAdd(key, owner);
            }

            return owners;
        }

        return ParseSs(ProcessRunner.TryRun("ss", "-Hlntup", ToolTimeout));
    }

    /// <summary>
    /// Parses netstat -ano output. Exposed for testing.
    /// </summary>
    internal static Dictionary<(string, int), Owner> ParseNetstat(string? output)
    {
        var result = new Dictionary<(string, int), Owner>();
        foreach (var line in Lines(output))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts[0] is not ("TCP" or "UDP"))
            {
                continue;
            }

            var isTcp = parts[0] == "TCP";
            if (isTcp && (parts.Length < 5 || parts[3] != "LISTENING"))
            {
                continue;
            }

            if (!int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            {
                continue;
            }

            var port = PortOf(parts[1]);
            if (port is not null)
            {
                result.TryAdd((parts[0], port.Value), new Owner(pid, ProcessNameOf(pid)));
            }
        }

        return result;
    }

    /// <summary>
    /// Parses lsof -F pcn output. Each process starts with a "p&lt;pid&gt;" line, followed by "c&lt;command&gt;" and
    /// one "n&lt;name&gt;" line per socket. Exposed for testing.
    /// </summary>
    internal static Dictionary<(string, int), Owner> ParseLsof(string? output, string protocol)
    {
        var result = new Dictionary<(string, int), Owner>();
        var pid = 0;
        string? command = null;

        foreach (var line in Lines(output))
        {
            switch (line[0])
            {
                case 'p':
                    pid = int.TryParse(line.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
                    command = null;
                    break;
                case 'c':
                    command = line[1..];
                    break;
                case 'n' when pid > 0:
                    var name = line[1..];
                    if (name.Contains("->", StringComparison.Ordinal))
                    {
                        break;
                    }

                    var port = PortOf(name);
                    if (port is not null)
                    {
                        result.TryAdd((protocol, port.Value), new Owner(pid, command));
                    }

                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses ss -Hlntup output. Exposed for testing.
    /// </summary>
    internal static Dictionary<(string, int), Owner> ParseSs(string? output)
    {
        var result = new Dictionary<(string, int), Owner>();
        foreach (var line in Lines(output))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                continue;
            }

            var protocol = parts[0].ToUpperInvariant();
            if (protocol is not ("TCP" or "UDP"))
            {
                continue;
            }

            var port = PortOf(parts[4]);
            var users = SsUsers.Match(line);
            if (port is null || !users.Success)
            {
                continue;
            }

            var pid = int.Parse(users.Groups["pid"].Value, CultureInfo.InvariantCulture);
            result.TryAdd((protocol, port.Value), new Owner(pid, users.Groups["name"].Value));
        }

        return result;
    }

    private static string[] Lines(string? output) =>
        string.IsNullOrEmpty(output)
            ? []
            : output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int? PortOf(string endpoint)
    {
        var colon = endpoint.LastIndexOf(':');
        return colon >= 0 && int.TryParse(endpoint.AsSpan(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            ? port
            : null;
    }

    private static string? ProcessNameOf(int pid)
    {
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
