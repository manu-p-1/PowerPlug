using System.Globalization;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PowerPlug.Base;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Lists network interfaces with their addresses, gateways and DNS servers.</para>
/// <para type="description">A cross platform view of the machine's network interfaces. Only interfaces that are up
/// (excluding loopback) are shown by default; -All includes the rest. Names accept wildcards.</para>
/// <example>
/// <para>Active interfaces</para>
/// <code>Get-NetworkInfo</code>
/// </example>
/// <example>
/// <para>Wi-Fi interface on macOS</para>
/// <code>Get-NetworkInfo -Name en0</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "NetworkInfo")]
[Alias("gni", "netinfo")]
[OutputType(typeof(NetworkInterfaceInfo))]
public sealed class GetNetworkInfoCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Interface name or description. Wildcards are allowed.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? Name { get; set; }

    /// <summary>
    /// <para type="description">Include interfaces that are down and the loopback interface.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter All { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        IEnumerable<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces();

        if (!All)
        {
            interfaces = interfaces.Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        }

        if (!string.IsNullOrEmpty(Name))
        {
            var pattern = new WildcardPattern(Name, WildcardOptions.IgnoreCase);
            interfaces = interfaces.Where(n => pattern.IsMatch(n.Name) || pattern.IsMatch(n.Description));
        }

        foreach (var nic in interfaces)
        {
            try
            {
                WriteObject(Describe(nic));
            }
            catch (NetworkInformationException ex)
            {
                // Some virtual adapters refuse to report properties. Skip them rather than abort the listing.
                WriteError(ex, "InterfaceUnavailable", ErrorCategory.ResourceUnavailable, nic.Name);
            }
        }
    }

    /// <summary>
    /// Builds the summary for one interface. Exposed for testing.
    /// </summary>
    internal static NetworkInterfaceInfo Describe(NetworkInterface nic)
    {
        var ip = nic.GetIPProperties();
        var ipv4 = ip.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
        var mac = nic.GetPhysicalAddress().GetAddressBytes();

        long? bytesSent = null;
        long? bytesReceived = null;
        try
        {
            var stats = nic.GetIPStatistics();
            bytesSent = stats.BytesSent;
            bytesReceived = stats.BytesReceived;
        }
        catch (Exception ex) when (ex is NetworkInformationException or PlatformNotSupportedException)
        {
            // Some virtual adapters do not expose counters.
        }

        return new NetworkInterfaceInfo
        {
            Name = nic.Name,
            Description = nic.Description,
            Type = nic.NetworkInterfaceType.ToString(),
            Status = nic.OperationalStatus.ToString(),
            MacAddress = mac.Length == 0 ? null : string.Join(":", mac.Select(b => b.ToString("X2", CultureInfo.InvariantCulture))),
            LinkSpeedMbps = nic.Speed > 0 ? nic.Speed / 1_000_000 : null,
            IPv4Address = ipv4?.Address.ToString(),
            SubnetMask = ipv4?.IPv4Mask?.ToString(),
            IPv6Addresses = ip.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6).Select(a => a.Address.ToString()).ToArray(),
            Gateways = ip.GatewayAddresses.Select(g => g.Address.ToString()).ToArray(),
            DnsServers = ip.DnsAddresses.Select(d => d.ToString()).ToArray(),
            DhcpServers = SafeDhcp(ip),
            BytesSent = bytesSent,
            BytesReceived = bytesReceived,
        };
    }

    private static string[] SafeDhcp(IPInterfaceProperties ip)
    {
        if (OperatingSystem.IsMacOS())
        {
            return [];
        }

        try
        {
            return ip.DhcpServerAddresses.Select(d => d.ToString()).ToArray();
        }
        catch (PlatformNotSupportedException)
        {
            return [];
        }
    }
}
