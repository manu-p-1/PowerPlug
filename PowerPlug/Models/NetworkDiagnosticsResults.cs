namespace PowerPlug.Models;

/// <summary>
/// A TCP or UDP connection (any state), and the process that owns it, where that can be determined.
/// </summary>
public sealed class NetworkConnectionInfo
{
    /// <summary>TCP or UDP.</summary>
    public required string Protocol { get; init; }

    /// <summary>The local address the socket is bound to.</summary>
    public required string LocalAddress { get; init; }

    /// <summary>The local port.</summary>
    public required int LocalPort { get; init; }

    /// <summary>The remote address, or null for sockets without a peer.</summary>
    public string? RemoteAddress { get; init; }

    /// <summary>The remote port, or null for sockets without a peer.</summary>
    public int? RemotePort { get; init; }

    /// <summary>Connection state such as ESTABLISHED, LISTEN or TIME_WAIT. Null for UDP on platforms that do not report one.</summary>
    public string? State { get; init; }

    /// <summary>The owning process id, or null when unavailable.</summary>
    public int? ProcessId { get; init; }

    /// <summary>The owning process name, or null when unavailable.</summary>
    public string? ProcessName { get; init; }
}

/// <summary>
/// A single DNS resource record returned by a query.
/// </summary>
public sealed class DnsRecordResult
{
    /// <summary>The name that was queried.</summary>
    public required string Name { get; init; }

    /// <summary>The record type, such as A, AAAA, CNAME, MX, TXT, NS, SOA or PTR.</summary>
    public required string Type { get; init; }

    /// <summary>The record value, formatted for the record type.</summary>
    public required string Value { get; init; }

    /// <summary>Time to live, in seconds, as reported by the server.</summary>
    public required uint TimeToLiveSeconds { get; init; }

    /// <summary>The DNS server that answered the query.</summary>
    public required string Server { get; init; }

    /// <summary>Round trip time of the query in milliseconds.</summary>
    public required double QueryTimeMs { get; init; }
}

/// <summary>
/// Wireless adapter status: signal strength, channel and security.
/// </summary>
public sealed class WifiInfoResult
{
    /// <summary>The network interface name.</summary>
    public required string Interface { get; init; }

    /// <summary>The network name (SSID).</summary>
    public string? Ssid { get; init; }

    /// <summary>The access point's hardware address (BSSID).</summary>
    public string? Bssid { get; init; }

    /// <summary>Signal quality as a percentage, where the platform reports one.</summary>
    public int? SignalPercent { get; init; }

    /// <summary>Received signal strength in dBm, where the platform reports one.</summary>
    public int? RssiDbm { get; init; }

    /// <summary>Noise floor in dBm, where the platform reports one. A weaker (higher) value close to the signal indicates interference.</summary>
    public int? NoiseDbm { get; init; }

    /// <summary>Signal to noise ratio in dB, derived when both signal and noise are known. Below ~15 dB usually means an unreliable link.</summary>
    public int? SignalToNoiseDb { get; init; }

    /// <summary>The Wi-Fi channel currently in use.</summary>
    public int? Channel { get; init; }

    /// <summary>The frequency band, such as 2.4GHz, 5GHz or 6GHz.</summary>
    public string? Band { get; init; }

    /// <summary>The negotiated PHY mode, such as 802.11ac or 802.11ax.</summary>
    public string? PhyMode { get; init; }

    /// <summary>The security protocol in use, such as WPA2-Personal.</summary>
    public string? Security { get; init; }

    /// <summary>Current transmit rate in megabits per second.</summary>
    public double? TransmitRateMbps { get; init; }
}

/// <summary>
/// One entry from the ARP / neighbor discovery cache: an IP address this machine has recently resolved to a MAC address.
/// </summary>
public sealed class NetworkNeighborInfo
{
    /// <summary>The neighbor's IP address.</summary>
    public required string IPAddress { get; init; }

    /// <summary>The neighbor's MAC address, when known.</summary>
    public string? MacAddress { get; init; }

    /// <summary>The local interface the entry was learned on, when reported.</summary>
    public string? Interface { get; init; }

    /// <summary>The reachability state, such as REACHABLE, STALE or PERMANENT, when reported.</summary>
    public string? State { get; init; }
}

/// <summary>
/// A live host discovered by a subnet scan.
/// </summary>
public sealed class NetworkDeviceInfo
{
    /// <summary>The device's IP address.</summary>
    public required string IPAddress { get; init; }

    /// <summary>The device's MAC address, when it could be resolved from the ARP cache.</summary>
    public string? MacAddress { get; init; }

    /// <summary>Reverse DNS hostname, when one resolves.</summary>
    public string? Hostname { get; init; }

    /// <summary>ICMP or TCP round trip time in milliseconds.</summary>
    public required double ResponseTimeMs { get; init; }

    /// <summary>True when this address belongs to the local machine.</summary>
    public required bool IsSelf { get; init; }
}

/// <summary>
/// One hop reported by a traceroute style probe.
/// </summary>
public sealed class TracerouteHop
{
    /// <summary>Hop number, starting at 1.</summary>
    public required int Hop { get; init; }

    /// <summary>The address that replied, or null when the hop timed out.</summary>
    public string? Address { get; init; }

    /// <summary>Reverse DNS name for the hop, when one resolves.</summary>
    public string? Hostname { get; init; }

    /// <summary>Round trip time in milliseconds, or null when the hop timed out.</summary>
    public double? LatencyMs { get; init; }

    /// <summary>True when this hop is the final destination.</summary>
    public required bool IsDestination { get; init; }
}

/// <summary>
/// A full connection health report: gateway reachability, DNS resolution, path to a target, and posture
/// information such as VPN and Wi-Fi state. Intended to answer "why did my connection just drop?".
/// </summary>
public sealed class NetworkDiagnosticResult
{
    /// <summary>When the diagnostic ran.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>The network interface that was diagnosed.</summary>
    public string? Interface { get; init; }

    /// <summary>This machine's local IPv4 address on that interface.</summary>
    public string? LocalIPAddress { get; init; }

    /// <summary>True when the interface looks like a VPN tunnel.</summary>
    public required bool IsVpnActive { get; init; }

    /// <summary>The default gateway address that was probed.</summary>
    public string? GatewayAddress { get; init; }

    /// <summary>True when the gateway answered at least one probe.</summary>
    public bool? GatewayReachable { get; init; }

    /// <summary>Average gateway latency in milliseconds.</summary>
    public double? GatewayLatencyMs { get; init; }

    /// <summary>Gateway jitter in milliseconds.</summary>
    public double? GatewayJitterMs { get; init; }

    /// <summary>Gateway probe packet loss percentage.</summary>
    public double? GatewayPacketLossPercent { get; init; }

    /// <summary>DNS servers configured on the interface.</summary>
    public required IReadOnlyList<string> DnsServers { get; init; }

    /// <summary>True when a DNS lookup of the diagnostic target succeeded.</summary>
    public bool? DnsResolutionSucceeded { get; init; }

    /// <summary>Time the DNS lookup took, in milliseconds.</summary>
    public double? DnsResolutionMs { get; init; }

    /// <summary>The public IP address as seen by an external service, when that check was not skipped.</summary>
    public string? PublicIPAddress { get; init; }

    /// <summary>Wi-Fi signal quality percentage, when the active interface is wireless.</summary>
    public int? WifiSignalPercent { get; init; }

    /// <summary>Wi-Fi signal to noise ratio in dB, when available.</summary>
    public int? WifiSignalToNoiseDb { get; init; }

    /// <summary>Hop by hop path to the diagnostic target, when traced.</summary>
    public required IReadOnlyList<TracerouteHop> Path { get; init; }

    /// <summary>Plain language problems the diagnostic noticed, worst first.</summary>
    public required IReadOnlyList<string> Issues { get; init; }

    /// <summary>Overall verdict: Healthy, Degraded or Unreachable.</summary>
    public required string Status { get; init; }
}

