namespace PowerPlug.Models;

/// <summary>
/// Result of comparing a file hash against a known signature.
/// </summary>
public sealed class HashComparisonResult
{
    /// <summary>The hash algorithm used.</summary>
    public required string Algorithm { get; init; }

    /// <summary>The file that was hashed.</summary>
    public required string Path { get; init; }

    /// <summary>The hex digest computed from the file.</summary>
    public required string ComputedHash { get; init; }

    /// <summary>The expected digest after normalisation.</summary>
    public required string ExpectedHash { get; init; }

    /// <summary>True when both digests match, ignoring case.</summary>
    public required bool Match { get; init; }
}

/// <summary>
/// A hash computed from a string.
/// </summary>
public sealed class StringHashResult
{
    /// <summary>The hash algorithm used.</summary>
    public required string Algorithm { get; init; }

    /// <summary>The input that was hashed.</summary>
    public required string Input { get; init; }

    /// <summary>The text encoding used to turn the input into bytes.</summary>
    public required string Encoding { get; init; }

    /// <summary>The hex digest.</summary>
    public required string Hash { get; init; }
}

/// <summary>
/// One entry from the PATH environment variable.
/// </summary>
public sealed class PathEntry
{
    /// <summary>Zero based position within PATH.</summary>
    public required int Index { get; init; }

    /// <summary>The directory as it appears in PATH.</summary>
    public required string Path { get; init; }

    /// <summary>True when the directory exists on disk.</summary>
    public required bool Exists { get; init; }

    /// <summary>True when the same directory appears earlier in PATH.</summary>
    public required bool Duplicate { get; init; }

    /// <summary>The environment target the entry was read from.</summary>
    public required string Target { get; init; }
}

/// <summary>
/// Timing statistics gathered by Measure-ScriptBlock.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>Number of timed iterations.</summary>
    public required int Iterations { get; init; }

    /// <summary>Iterations that threw an exception.</summary>
    public required int Failures { get; init; }

    /// <summary>Sum of all iteration times in milliseconds.</summary>
    public required double TotalMs { get; init; }

    /// <summary>Mean iteration time in milliseconds.</summary>
    public required double AverageMs { get; init; }

    /// <summary>Median iteration time in milliseconds.</summary>
    public required double MedianMs { get; init; }

    /// <summary>Fastest iteration in milliseconds.</summary>
    public required double MinMs { get; init; }

    /// <summary>Slowest iteration in milliseconds.</summary>
    public required double MaxMs { get; init; }

    /// <summary>Population standard deviation in milliseconds.</summary>
    public required double StdDevMs { get; init; }
}

/// <summary>
/// Result of a TCP connectivity check.
/// </summary>
public sealed class PortTestResult
{
    /// <summary>The host that was tested.</summary>
    public required string Host { get; init; }

    /// <summary>The TCP port that was tested.</summary>
    public required int Port { get; init; }

    /// <summary>True when the connection was established.</summary>
    public required bool Open { get; init; }

    /// <summary>Time to connect in milliseconds, or null when the port was closed.</summary>
    public double? LatencyMs { get; init; }

    /// <summary>The failure reason when the port was not open.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of waiting for a TCP port.
/// </summary>
public sealed class PortWaitResult
{
    /// <summary>The host that was polled.</summary>
    public required string Host { get; init; }

    /// <summary>The TCP port that was polled.</summary>
    public required int Port { get; init; }

    /// <summary>True when the port opened within the timeout.</summary>
    public required bool Open { get; init; }

    /// <summary>Number of connection attempts made.</summary>
    public required int Attempts { get; init; }

    /// <summary>Total time spent waiting in milliseconds.</summary>
    public required double ElapsedMs { get; init; }
}

/// <summary>
/// Result of an HTTP endpoint check.
/// </summary>
public sealed class UrlTestResult
{
    /// <summary>The URL that was requested.</summary>
    public required string Url { get; init; }

    /// <summary>True when the response status was a 2xx or 3xx code.</summary>
    public required bool Success { get; init; }

    /// <summary>The numeric HTTP status code, or null when no response was received.</summary>
    public int? StatusCode { get; init; }

    /// <summary>The HTTP reason phrase.</summary>
    public string? Status { get; init; }

    /// <summary>Time until response headers arrived, in milliseconds.</summary>
    public double? LatencyMs { get; init; }

    /// <summary>The Location header for redirect responses.</summary>
    public string? RedirectTo { get; init; }

    /// <summary>Content-Type of the response.</summary>
    public string? ContentType { get; init; }

    /// <summary>Content-Length of the response when the server sent one.</summary>
    public long? ContentLength { get; init; }

    /// <summary>The Server header when present.</summary>
    public string? Server { get; init; }

    /// <summary>The failure reason when the request did not complete.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Summary of a remote TLS certificate.
/// </summary>
public sealed class TlsCertificateInfo
{
    /// <summary>The host that was connected to.</summary>
    public required string Host { get; init; }

    /// <summary>The TCP port that was connected to.</summary>
    public required int Port { get; init; }

    /// <summary>Certificate subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Certificate issuer.</summary>
    public required string Issuer { get; init; }

    /// <summary>Start of the validity period.</summary>
    public required DateTime NotBefore { get; init; }

    /// <summary>End of the validity period.</summary>
    public required DateTime NotAfter { get; init; }

    /// <summary>Whole days until the certificate expires. Negative when already expired.</summary>
    public required int DaysRemaining { get; init; }

    /// <summary>True when the certificate is currently within its validity window.</summary>
    public required bool IsValid { get; init; }

    /// <summary>True when the certificate chain validated against the machine trust store.</summary>
    public required bool ChainTrusted { get; init; }

    /// <summary>Subject alternative names on the certificate.</summary>
    public required IReadOnlyList<string> SubjectAlternativeNames { get; init; }

    /// <summary>The negotiated TLS protocol.</summary>
    public required string Protocol { get; init; }

    /// <summary>SHA256 fingerprint of the certificate.</summary>
    public required string Thumbprint { get; init; }

    /// <summary>Signature algorithm used by the certificate.</summary>
    public required string SignatureAlgorithm { get; init; }

    /// <summary>Certificate serial number.</summary>
    public required string SerialNumber { get; init; }
}

/// <summary>
/// The public IP address as seen by an external service.
/// </summary>
public sealed class PublicIPResult
{
    /// <summary>The public IP address.</summary>
    public required string IPAddress { get; init; }

    /// <summary>IPv4 or IPv6.</summary>
    public required string AddressFamily { get; init; }

    /// <summary>The service that reported the address.</summary>
    public required string Provider { get; init; }

    /// <summary>Time the lookup took in milliseconds.</summary>
    public required double LatencyMs { get; init; }
}

/// <summary>
/// A listening socket and the process that owns it, where that can be determined.
/// </summary>
public sealed class ListeningPort
{
    /// <summary>TCP or UDP.</summary>
    public required string Protocol { get; init; }

    /// <summary>The address the socket is bound to.</summary>
    public required string LocalAddress { get; init; }

    /// <summary>The local port.</summary>
    public required int Port { get; init; }

    /// <summary>The owning process id, or null when unavailable.</summary>
    public int? ProcessId { get; init; }

    /// <summary>The owning process name, or null when unavailable.</summary>
    public string? ProcessName { get; init; }
}

/// <summary>
/// Details of a network interface.
/// </summary>
public sealed class NetworkInterfaceInfo
{
    /// <summary>Interface name.</summary>
    public required string Name { get; init; }

    /// <summary>Interface description as reported by the OS.</summary>
    public required string Description { get; init; }

    /// <summary>Interface type such as Ethernet or Wireless80211.</summary>
    public required string Type { get; init; }

    /// <summary>Operational status.</summary>
    public required string Status { get; init; }

    /// <summary>True when the interface's type or name suggests it is a VPN tunnel (WireGuard, OpenVPN, Tailscale, PPP, and similar).</summary>
    public required bool IsVpn { get; init; }

    /// <summary>MAC address, or null for interfaces without one.</summary>
    public string? MacAddress { get; init; }

    /// <summary>Link speed in megabits per second, or null when unknown.</summary>
    public long? LinkSpeedMbps { get; init; }

    /// <summary>Primary IPv4 address.</summary>
    public string? IPv4Address { get; init; }

    /// <summary>IPv4 subnet mask.</summary>
    public string? SubnetMask { get; init; }

    /// <summary>All IPv6 addresses.</summary>
    public required IReadOnlyList<string> IPv6Addresses { get; init; }

    /// <summary>Gateway addresses.</summary>
    public required IReadOnlyList<string> Gateways { get; init; }

    /// <summary>DNS server addresses.</summary>
    public required IReadOnlyList<string> DnsServers { get; init; }

    /// <summary>DHCP server addresses where the platform exposes them.</summary>
    public required IReadOnlyList<string> DhcpServers { get; init; }

    /// <summary>Bytes sent since the interface came up.</summary>
    public long? BytesSent { get; init; }

    /// <summary>Bytes received since the interface came up.</summary>
    public long? BytesReceived { get; init; }
}

/// <summary>
/// Result of a network speed test.
/// </summary>
public sealed class SpeedTestResult
{
    /// <summary>Name of the interface carrying most traffic.</summary>
    public string? Interface { get; init; }

    /// <summary>Local IPv4 address of that interface.</summary>
    public string? LocalIP { get; init; }

    /// <summary>Minimum latency in milliseconds.</summary>
    public double? LatencyMinMs { get; init; }

    /// <summary>Average latency in milliseconds.</summary>
    public double? LatencyAvgMs { get; init; }

    /// <summary>Maximum latency in milliseconds.</summary>
    public double? LatencyMaxMs { get; init; }

    /// <summary>Jitter in milliseconds.</summary>
    public double? JitterMs { get; init; }

    /// <summary>Packet loss percentage for the latency probes.</summary>
    public double? PacketLossPercent { get; init; }

    /// <summary>How latency was measured: ICMP or TCP.</summary>
    public required string LatencyMethod { get; init; }

    /// <summary>Download throughput in megabits per second.</summary>
    public double? DownloadMbps { get; init; }

    /// <summary>Bytes downloaded.</summary>
    public long DownloadBytes { get; init; }

    /// <summary>Upload throughput in megabits per second.</summary>
    public double? UploadMbps { get; init; }

    /// <summary>Bytes uploaded.</summary>
    public long UploadBytes { get; init; }
}

/// <summary>
/// A decoded JSON Web Token. The signature is not verified.
/// </summary>
public sealed class JwtToken
{
    /// <summary>The decoded header as an ordered hashtable.</summary>
    public required System.Collections.Specialized.OrderedDictionary Header { get; init; }

    /// <summary>The decoded payload (claims) as an ordered hashtable.</summary>
    public required System.Collections.Specialized.OrderedDictionary Payload { get; init; }

    /// <summary>The raw Base64Url signature segment.</summary>
    public required string Signature { get; init; }

    /// <summary>The signing algorithm from the header, if present.</summary>
    public string? Algorithm { get; init; }

    /// <summary>Issuer claim, if present.</summary>
    public string? Issuer { get; init; }

    /// <summary>Subject claim, if present.</summary>
    public string? Subject { get; init; }

    /// <summary>Audience claim, if present.</summary>
    public string? Audience { get; init; }

    /// <summary>Issued at time, if present.</summary>
    public DateTimeOffset? IssuedAt { get; init; }

    /// <summary>Not before time, if present.</summary>
    public DateTimeOffset? NotBefore { get; init; }

    /// <summary>Expiry time, if present.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>True when the token has an exp claim that is in the past.</summary>
    public required bool IsExpired { get; init; }
}

/// <summary>
/// A colour expressed in several notations.
/// </summary>
public sealed class ColorInfo
{
    /// <summary>Hex notation, for example #FF8800.</summary>
    public required string Hex { get; init; }

    /// <summary>Red channel 0-255.</summary>
    public required int R { get; init; }

    /// <summary>Green channel 0-255.</summary>
    public required int G { get; init; }

    /// <summary>Blue channel 0-255.</summary>
    public required int B { get; init; }

    /// <summary>Alpha channel 0-255.</summary>
    public required int A { get; init; }

    /// <summary>CSS rgb() or rgba() notation.</summary>
    public required string Rgb { get; init; }

    /// <summary>Hue in degrees 0-360.</summary>
    public required double H { get; init; }

    /// <summary>Saturation percentage 0-100.</summary>
    public required double S { get; init; }

    /// <summary>Lightness percentage 0-100.</summary>
    public required double L { get; init; }

    /// <summary>CSS hsl() notation.</summary>
    public required string Hsl { get; init; }
}

/// <summary>
/// A variable loaded from a .env file.
/// </summary>
public sealed class DotEnvEntry
{
    /// <summary>Variable name.</summary>
    public required string Name { get; init; }

    /// <summary>Variable value after quote handling.</summary>
    public required string Value { get; init; }

    /// <summary>True when an existing variable was overwritten.</summary>
    public required bool Overwritten { get; init; }
}

/// <summary>
/// Summary of the local machine.
/// </summary>
public sealed class SystemInfo
{
    /// <summary>Machine host name.</summary>
    public required string ComputerName { get; init; }

    /// <summary>Current user name.</summary>
    public required string UserName { get; init; }

    /// <summary>Operating system description.</summary>
    public required string OS { get; init; }

    /// <summary>OS version string.</summary>
    public required string OSVersion { get; init; }

    /// <summary>Process and OS architecture.</summary>
    public required string Architecture { get; init; }

    /// <summary>Logical processor count.</summary>
    public required int ProcessorCount { get; init; }

    /// <summary>Memory available to the runtime in bytes. Physical memory unless a container or job limit applies.</summary>
    public long? TotalMemoryBytes { get; init; }

    /// <summary>Memory available to the runtime, formatted for reading.</summary>
    public string? TotalMemory { get; init; }

    /// <summary>Time since the OS started.</summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>.NET runtime the module is running on.</summary>
    public required string DotNetRuntime { get; init; }

    /// <summary>PowerShell version of the hosting session.</summary>
    public required string PowerShellVersion { get; init; }

    /// <summary>PowerPlug module version.</summary>
    public required string PowerPlugVersion { get; init; }

    /// <summary>True when the session is running elevated.</summary>
    public required bool IsElevated { get; init; }

    /// <summary>Local time zone.</summary>
    public required string TimeZone { get; init; }
}

/// <summary>
/// Size of a directory tree.
/// </summary>
public sealed class DirectorySizeInfo
{
    /// <summary>Full path of the directory.</summary>
    public required string Path { get; init; }

    /// <summary>Total size of all files in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Total size formatted for reading.</summary>
    public required string Size { get; init; }

    /// <summary>Number of files counted.</summary>
    public required int FileCount { get; init; }

    /// <summary>Number of sub directories counted.</summary>
    public required int DirectoryCount { get; init; }

    /// <summary>Number of entries that could not be read, usually due to permissions.</summary>
    public required int SkippedCount { get; init; }
}

/// <summary>
/// A group of files with identical content.
/// </summary>
public sealed class DuplicateFileGroup
{
    /// <summary>The shared content hash.</summary>
    public required string Hash { get; init; }

    /// <summary>Size of each file in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Size formatted for reading.</summary>
    public required string Size { get; init; }

    /// <summary>Number of files in the group.</summary>
    public required int Count { get; init; }

    /// <summary>Bytes that would be reclaimed by keeping one copy.</summary>
    public required long WastedBytes { get; init; }

    /// <summary>Full paths of every file in the group.</summary>
    public required IReadOnlyList<string> Files { get; init; }
}

/// <summary>
/// A single rename performed or previewed by Rename-BatchItem.
/// </summary>
public sealed record RenameResult
{
    /// <summary>Original full path.</summary>
    public required string Path { get; init; }

    /// <summary>Original file name.</summary>
    public required string OldName { get; init; }

    /// <summary>New file name.</summary>
    public required string NewName { get; init; }

    /// <summary>True when the rename was applied, false in preview mode or when skipped.</summary>
    public required bool Renamed { get; init; }

    /// <summary>Why the item was skipped, if it was.</summary>
    public string? Reason { get; init; }
}
