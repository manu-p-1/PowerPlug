using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Networking
{
    /// <summary>
    /// <para type="synopsis">Displays detailed information about network interfaces</para>
    /// <para type="description">Get-NetworkInfo enumerates network interfaces and reports detailed information
    /// including IP addresses, subnet masks, gateways, DNS servers, MAC addresses, link speed, and status.
    /// By default only active interfaces are shown.</para>
    /// <example>
    /// <para>List all active network interfaces</para>
    /// <code>Get-NetworkInfo</code>
    /// </example>
    /// <example>
    /// <para>Show all interfaces including inactive ones</para>
    /// <code>Get-NetworkInfo -All</code>
    /// </example>
    /// <example>
    /// <para>Get info for a specific interface by name</para>
    /// <code>Get-NetworkInfo -Name "en0"</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "NetworkInfo")]
    [Alias("gni", "netinfo")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class GetNetworkInfoCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">Show all interfaces, including inactive ones</para>
        /// </summary>
        [Parameter]
        public SwitchParameter All { get; set; }

        /// <summary>
        /// <para type="description">Filter by interface name (supports wildcards)</para>
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string? Name { get; set; }

        /// <summary>
        /// Processes the Get-NetworkInfo PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .AsEnumerable();

            if (!All)
            {
                interfaces = interfaces.Where(n =>
                    n.OperationalStatus == OperationalStatus.Up
                    && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            }

            if (!string.IsNullOrEmpty(Name))
            {
                var pattern = new WildcardPattern(Name, WildcardOptions.IgnoreCase);
                interfaces = interfaces.Where(n => pattern.IsMatch(n.Name) || pattern.IsMatch(n.Description));
            }

            foreach (var nic in interfaces)
            {
                WriteObject(BuildInterfaceInfo(nic));
            }
        }

        private static PSObject BuildInterfaceInfo(NetworkInterface nic)
        {
            var result = new PSObject();
            var ipProps = nic.GetIPProperties();

            result.Properties.Add(new PSNoteProperty("Name", nic.Name));
            result.Properties.Add(new PSNoteProperty("Description", nic.Description));
            result.Properties.Add(new PSNoteProperty("Type", nic.NetworkInterfaceType.ToString()));
            result.Properties.Add(new PSNoteProperty("Status", nic.OperationalStatus.ToString()));
            result.Properties.Add(new PSNoteProperty("MACAddress", FormatMac(nic.GetPhysicalAddress())));
            result.Properties.Add(new PSNoteProperty("LinkSpeedMbps", nic.Speed / 1_000_000));

            // IPv4
            var ipv4 = ipProps.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
            result.Properties.Add(new PSNoteProperty("IPv4Address", ipv4?.Address.ToString() ?? "N/A"));
            result.Properties.Add(new PSNoteProperty("SubnetMask", ipv4?.IPv4Mask?.ToString() ?? "N/A"));

            // IPv6
            var ipv6Addrs = ipProps.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(a => a.Address.ToString())
                .ToArray();
            result.Properties.Add(new PSNoteProperty("IPv6Addresses",
                ipv6Addrs.Length > 0 ? string.Join(", ", ipv6Addrs) : "N/A"));

            // Gateway
            var gateways = ipProps.GatewayAddresses
                .Select(g => g.Address.ToString())
                .ToArray();
            result.Properties.Add(new PSNoteProperty("Gateway",
                gateways.Length > 0 ? string.Join(", ", gateways) : "N/A"));

            // DNS
            var dnsServers = ipProps.DnsAddresses
                .Select(d => d.ToString())
                .ToArray();
            result.Properties.Add(new PSNoteProperty("DnsServers",
                dnsServers.Length > 0 ? string.Join(", ", dnsServers) : "N/A"));

            // DHCP
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    result.Properties.Add(new PSNoteProperty("DhcpServer", "N/A"));
                }
                else
                {
                    var dhcpServers = ipProps.DhcpServerAddresses
                        .Select(d => d.ToString())
                        .ToArray();
                    result.Properties.Add(new PSNoteProperty("DhcpServer",
                        dhcpServers.Length > 0 ? string.Join(", ", dhcpServers) : "N/A"));
                }
            }
            catch (PlatformNotSupportedException)
            {
                result.Properties.Add(new PSNoteProperty("DhcpServer", "N/A"));
            }

            // Traffic stats
            try
            {
                var stats = nic.GetIPv4Statistics();
                result.Properties.Add(new PSNoteProperty("BytesSent", stats.BytesSent));
                result.Properties.Add(new PSNoteProperty("BytesReceived", stats.BytesReceived));
            }
            catch (NetworkInformationException)
            {
                result.Properties.Add(new PSNoteProperty("BytesSent", 0L));
                result.Properties.Add(new PSNoteProperty("BytesReceived", 0L));
            }

            return result;
        }

        private static string FormatMac(PhysicalAddress mac)
        {
            var bytes = mac.GetAddressBytes();
            return bytes.Length == 0
                ? "N/A"
                : string.Join(":", bytes.Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
        }
    }
}
