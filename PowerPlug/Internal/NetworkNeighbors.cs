using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PowerPlug.Internal;

/// <summary>
/// Reads the ARP / neighbor discovery cache: the IP-to-MAC mappings this machine has learned from other devices on
/// the same link. Useful for spotting unexpected hosts on the local network.
/// </summary>
internal static partial class NetworkNeighbors
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(10);

    /// <summary>A single neighbor cache entry.</summary>
    public readonly record struct Neighbor(string IPAddress, string? MacAddress, string? Interface, string? State);

    /// <summary>
    /// Returns the current neighbor cache. Returns an empty list when the platform tool is unavailable.
    /// </summary>
    public static List<Neighbor> Discover()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ParseWindowsArp(ProcessRunner.TryRun("arp", "-a", ToolTimeout));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ParseMacArp(ProcessRunner.TryRun("arp", "-a -n", ToolTimeout));
        }

        var neighbors = ProcessRunner.TryRun("ip", "neigh", ToolTimeout);
        return neighbors is not null ? ParseIpNeigh(neighbors) : ParseMacArp(ProcessRunner.TryRun("arp", "-a -n", ToolTimeout));
    }

    // Interface: 192.168.1.1 --- 0x2
    //   Internet Address      Physical Address      Type
    //   192.168.1.1           aa-bb-cc-dd-ee-ff      dynamic
    [GeneratedRegex(@"^(?<ip>\d{1,3}(?:\.\d{1,3}){3})\s+(?<mac>[0-9a-fA-F]{2}(?:-[0-9a-fA-F]{2}){5})\s+(?<type>\w+)", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsArpRow();

    [GeneratedRegex(@"^Interface:\s*(?<ip>\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsArpInterface();

    /// <summary>Parses Windows `arp -a` output. Exposed for testing.</summary>
    internal static List<Neighbor> ParseWindowsArp(string? output)
    {
        var result = new List<Neighbor>();
        string? currentInterface = null;
        foreach (var line in Lines(output))
        {
            var interfaceMatch = WindowsArpInterface().Match(line);
            if (interfaceMatch.Success)
            {
                currentInterface = interfaceMatch.Groups["ip"].Value;
                continue;
            }

            var row = WindowsArpRow().Match(line);
            if (!row.Success)
            {
                continue;
            }

            result.Add(new Neighbor(row.Groups["ip"].Value, row.Groups["mac"].Value.Replace('-', ':'), currentInterface, row.Groups["type"].Value));
        }

        return result;
    }

    // ? (192.168.1.1) at aa:bb:cc:dd:ee:ff on en0 ifscope [ethernet]
    // ? (192.168.1.55) at (incomplete) on en0 ifscope [ethernet]
    [GeneratedRegex(@"\((?<ip>[\d.]+)\)\s+at\s+\(?(?<mac>[0-9a-fA-F:]{11,17}|incomplete)\)?(?:\s+on\s+(?<iface>\S+))?", RegexOptions.CultureInvariant)]
    private static partial Regex MacArpRow();

    /// <summary>Parses macOS/BSD `arp -a -n` output. Exposed for testing.</summary>
    internal static List<Neighbor> ParseMacArp(string? output)
    {
        var result = new List<Neighbor>();
        foreach (var line in Lines(output))
        {
            var match = MacArpRow().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var mac = match.Groups["mac"].Value;
            result.Add(new Neighbor(match.Groups["ip"].Value, mac == "incomplete" ? null : mac, match.Groups["iface"] is { Success: true } iface ? iface.Value : null, null));
        }

        return result;
    }

    // 192.168.1.1 dev en0 lladdr aa:bb:cc:dd:ee:ff REACHABLE
    [GeneratedRegex(@"^(?<ip>[\da-fA-F:.]+)\s+dev\s+(?<iface>\S+)(?:\s+lladdr\s+(?<mac>[0-9a-fA-F:]{11,17}))?\s+(?<state>\w+)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex IpNeighRow();

    /// <summary>Parses Linux `ip neigh` output. Exposed for testing.</summary>
    internal static List<Neighbor> ParseIpNeigh(string? output)
    {
        var result = new List<Neighbor>();
        foreach (var line in Lines(output))
        {
            var match = IpNeighRow().Match(line);
            if (!match.Success)
            {
                continue;
            }

            result.Add(new Neighbor(
                match.Groups["ip"].Value,
                match.Groups["mac"] is { Success: true } mac ? mac.Value : null,
                match.Groups["iface"].Value,
                match.Groups["state"].Value));
        }

        return result;
    }

    private static string[] Lines(string? output) =>
        string.IsNullOrEmpty(output)
            ? []
            : output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
