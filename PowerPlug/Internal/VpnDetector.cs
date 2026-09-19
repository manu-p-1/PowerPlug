using System.Net.NetworkInformation;

namespace PowerPlug.Internal;

/// <summary>
/// Heuristically flags network interfaces that look like VPN tunnels. .NET has no API that reports this directly, so
/// this checks the interface type and common naming conventions used by tunnel drivers and VPN clients.
/// </summary>
internal static class VpnDetector
{
    private static readonly string[] NameNeedles =
    [
        "tun", "tap", "utun", "ppp", "wg", "wireguard", "vpn", "tailscale", "zerotier", "nordlynx", "wintun",
        "openvpn", "cisco anyconnect", "globalprotect", "fortinet", "pulse secure", "ipsec",
    ];

    /// <summary>
    /// True when the interface's type or name/description suggests it carries VPN traffic.
    /// </summary>
    public static bool IsLikelyVpn(NetworkInterface nic)
    {
        if (nic.NetworkInterfaceType is NetworkInterfaceType.Ppp or NetworkInterfaceType.Tunnel)
        {
            return true;
        }

        return NameNeedles.Any(needle =>
            nic.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
            nic.Description.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
