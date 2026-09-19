using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Sweeps the local subnet for live hosts.</para>
/// <para type="description">Pings every address in a subnet (your active interface's /24 by default, or a CIDR you
/// supply) in parallel, then reads the ARP cache and reverse DNS to attach a MAC address and hostname to whatever
/// answers. This is the "what else is on my network" view: unexpected devices, a neighbor piggybacking on your Wi-Fi,
/// or a misbehaving IoT gadget flooding the LAN all show up here.</para>
/// <example>
/// <para>Scan the current subnet</para>
/// <code>Find-NetworkDevice</code>
/// </example>
/// <example>
/// <para>Scan a specific range</para>
/// <code>Find-NetworkDevice -Cidr 10.0.0.0/24</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Find, "NetworkDevice")]
[Alias("scan", "nmap")]
[OutputType(typeof(NetworkDeviceInfo))]
[ExperimentalCmdlet("It pings every address in the subnet and may be flagged by intrusion detection on managed networks.")]
public sealed class FindNetworkDeviceCmdlet : PowerPlugCmdlet, IDisposable
{
    /// <summary>
    /// <para type="description">The subnet to scan in CIDR notation, e.g. 192.168.1.0/24. Defaults to the active interface's subnet.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? Cidr { get; set; }

    /// <summary>
    /// <para type="description">Per host ping timeout in milliseconds. Defaults to 400.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(50, 10_000)]
    public int TimeoutMs { get; set; } = 400;

    /// <summary>
    /// <para type="description">Maximum number of hosts pinged at once. Defaults to 64.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 256)]
    public int MaxConcurrency { get; set; } = 64;

    /// <summary>
    /// <para type="description">Skip reverse DNS lookups for a faster scan.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoResolveHostName { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var (network, prefixLength) = Cidr is not null ? ParseCidr(Cidr) : FindActiveSubnet();
        if (network is null)
        {
            WriteError(new InvalidOperationException("Could not determine a subnet to scan. Supply -Cidr explicitly."),
                "NoSubnet", ErrorCategory.InvalidArgument, Cidr);
            return;
        }

        if (prefixLength < 22)
        {
            WriteError(new ArgumentException($"/{prefixLength} covers too many hosts to scan safely. Use a /22 or smaller (fewer than ~1000 hosts)."),
                "SubnetTooLarge", ErrorCategory.InvalidArgument, Cidr);
            return;
        }

        var hosts = HostAddresses(network, prefixLength).ToList();
        var localAddresses = LocalAddresses();

        WriteProgress(new ProgressRecord(1, "Scanning network", $"Pinging {hosts.Count} addresses") { PercentComplete = 0 });

        var throttle = new SemaphoreSlim(MaxConcurrency);
        var tasks = hosts.Select(async host =>
        {
            await throttle.WaitAsync(_cancellation.Token).ConfigureAwait(false);
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, TimeoutMs).ConfigureAwait(false);
                return reply.Status == IPStatus.Success ? ((IPAddress Host, double Rtt)?)(host, reply.RoundtripTime) : null;
            }
            catch (PingException)
            {
                return null;
            }
            finally
            {
                throttle.Release();
            }
        }).ToArray();

        (IPAddress Host, double Rtt)?[] results;
        try
        {
            results = Task.WhenAll(tasks).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            results = [];
        }

        WriteProgress(new ProgressRecord(1, "Scanning network", "Reading neighbor cache") { PercentComplete = 90 });
        var neighbors = NetworkNeighbors.Discover().ToDictionary(n => n.IPAddress, n => n.MacAddress, StringComparer.Ordinal);

        WriteProgress(new ProgressRecord(1, "Scanning network", "Done") { RecordType = ProgressRecordType.Completed });

        foreach (var (host, rtt) in results.Where(r => r is not null).Select(r => r!.Value).OrderBy(r => IpSortKey(r.Host)))
        {
            neighbors.TryGetValue(host.ToString(), out var mac);
            WriteObject(new NetworkDeviceInfo
            {
                IPAddress = host.ToString(),
                MacAddress = mac,
                Hostname = NoResolveHostName ? null : ResolveHostname(host),
                ResponseTimeMs = Math.Round(rtt, 2),
                IsSelf = localAddresses.Contains(host.ToString()),
            });
        }
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    private static string? ResolveHostname(IPAddress address)
    {
        try
        {
            var task = Dns.GetHostEntryAsync(address);
            return task.Wait(TimeSpan.FromMilliseconds(500)) ? task.Result.HostName : null;
        }
        catch (Exception ex) when (ex is SocketException or AggregateException)
        {
            return null;
        }
    }

    private static HashSet<string> LocalAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .ToHashSet(StringComparer.Ordinal);

    private static (IPAddress? Network, int PrefixLength) FindActiveSubnet()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork));

        if (nic is null)
        {
            return (null, 0);
        }

        var unicast = nic.GetIPProperties().UnicastAddresses.First(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
        var prefixLength = unicast.PrefixLength > 0 ? unicast.PrefixLength : 24;
        var network = ApplyMask(unicast.Address, prefixLength);
        return (network, prefixLength);
    }

    /// <summary>
    /// Parses "a.b.c.d/n" into a network address and prefix length. Exposed for testing.
    /// </summary>
    internal static (IPAddress? Network, int PrefixLength) ParseCidr(string cidr)
    {
        var parts = cidr.Split('/', 2);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var address) || address.AddressFamily != AddressFamily.InterNetwork
            || !int.TryParse(parts[1], out var prefixLength) || prefixLength is < 0 or > 32)
        {
            return (null, 0);
        }

        return (ApplyMask(address, prefixLength), prefixLength);
    }

    private static IPAddress ApplyMask(IPAddress address, int prefixLength)
    {
        var bytes = address.GetAddressBytes();
        var mask = uint.MaxValue << (32 - prefixLength);
        var value = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]) & mask;
        return new IPAddress([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
    }

    /// <summary>
    /// Enumerates usable host addresses in the subnet (excluding the network and broadcast addresses).
    /// </summary>
    internal static IEnumerable<IPAddress> HostAddresses(IPAddress network, int prefixLength)
    {
        var hostBits = 32 - prefixLength;
        if (hostBits <= 1)
        {
            yield return network;
            yield break;
        }

        var bytes = network.GetAddressBytes();
        var baseValue = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        var count = (1u << hostBits) - 2; // Exclude network and broadcast.
        for (uint i = 1; i <= count; i++)
        {
            var value = baseValue + i;
            yield return new IPAddress([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
        }
    }

    private static uint IpSortKey(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
