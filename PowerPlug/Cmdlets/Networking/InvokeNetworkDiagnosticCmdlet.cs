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
/// <para type="synopsis">Runs a full connection health check: gateway, DNS, path and Wi-Fi, in one shot.</para>
/// <para type="description">Answers "why did my connection just drop" in a single command. It pings the default
/// gateway to separate a local Wi-Fi/cable problem from an upstream one, times a DNS resolution, traces the path to
/// a target host hop by hop, checks whether a VPN tunnel is active, and reads the Wi-Fi signal-to-noise ratio when
/// the active interface is wireless (a weak SNR causes exactly the "full bars but nothing works" symptom). Plain
/// language issues are called out; everything measured is also on the object for scripting.</para>
/// <example>
/// <para>Full check against the default target</para>
/// <code>Invoke-NetworkDiagnostic</code>
/// </example>
/// <example>
/// <para>Diagnose while reaching a specific service, skipping the public IP lookup</para>
/// <code>Invoke-NetworkDiagnostic -Target api.example.com -SkipPublicIP</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Invoke, "NetworkDiagnostic")]
[Alias("diag", "netcheck")]
[OutputType(typeof(NetworkDiagnosticResult))]
[ExperimentalCmdlet("It pings, resolves DNS and traces routes; results depend on ICMP being allowed by your network and the target.")]
public sealed class InvokeNetworkDiagnosticCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The host used for the DNS timing check and path trace. Defaults to example.com.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Target { get; set; } = "example.com";

    /// <summary>
    /// <para type="description">Number of gateway pings used to compute latency, jitter and loss. Defaults to 8.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(2, 100)]
    public int PingCount { get; set; } = 8;

    /// <summary>
    /// <para type="description">Maximum number of hops to trace towards -Target. Defaults to 20.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 64)]
    public int MaxHops { get; set; } = 20;

    /// <summary>
    /// <para type="description">Skip the public IP address lookup, which requires internet access to a third party service.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SkipPublicIP { get; set; }

    /// <summary>
    /// <para type="description">Skip the hop by hop path trace.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SkipTraceroute { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var issues = new List<string>();

        var nic = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork));

        if (nic is null)
        {
            WriteObject(new NetworkDiagnosticResult
            {
                Timestamp = DateTimeOffset.Now,
                IsVpnActive = false,
                DnsServers = [],
                Path = [],
                Issues = ["No active network interface with an IPv4 address was found."],
                Status = "Unreachable",
            });
            return;
        }

        WriteProgress(new ProgressRecord(1, "Network diagnostic", "Pinging gateway") { PercentComplete = 10 });
        var info = GetNetworkInfoCmdlet.Describe(nic);
        var isVpn = VpnDetector.IsLikelyVpn(nic);
        var gateway = info.Gateways.Count > 0 ? info.Gateways[0] : null;

        var gatewayLatencies = new List<double>();
        bool? gatewayReachable = null;
        if (gateway is not null)
        {
            gatewayLatencies = PingMany(gateway, PingCount);
            gatewayReachable = gatewayLatencies.Count > 0;
            if (!gatewayReachable.Value)
            {
                issues.Add($"The default gateway ({gateway}) did not answer any of {PingCount} pings. Check the physical connection or Wi-Fi association first.");
            }
        }

        var gatewayLossPercent = gateway is null ? (double?)null : Math.Round(100.0 * (PingCount - gatewayLatencies.Count) / PingCount, 1);
        if (gatewayLossPercent is > 5)
        {
            issues.Add($"{gatewayLossPercent}% packet loss to the gateway. Intermittent loss this close to you usually means a physical/Wi-Fi problem, not an ISP one.");
        }

        var gatewayJitter = gatewayLatencies.Count > 1 ? Statistics.Jitter(gatewayLatencies) : (double?)null;
        if (gatewayJitter is > 30)
        {
            issues.Add($"Gateway jitter is {Math.Round(gatewayJitter.Value, 1)} ms. High jitter causes choppy calls and stalled streams even when average latency looks fine.");
        }

        WriteProgress(new ProgressRecord(1, "Network diagnostic", "Resolving DNS") { PercentComplete = 30 });
        var (dnsOk, dnsMs) = TimeDnsResolution(Target);
        if (dnsOk == false)
        {
            issues.Add($"DNS resolution of '{Target}' failed. Try Get-DnsRecord {Target} -Server 1.1.1.1 to see whether a specific resolver is at fault.");
        }
        else if (dnsMs is > 300)
        {
            issues.Add($"DNS resolution took {Math.Round(dnsMs.Value, 0)} ms, which is slow enough to be noticeable. Consider a faster resolver (1.1.1.1 or 8.8.8.8).");
        }

        int? wifiSignal = null;
        int? wifiSnr = null;
        if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
        {
            WriteProgress(new ProgressRecord(1, "Network diagnostic", "Reading Wi-Fi signal") { PercentComplete = 45 });
            var wifi = WifiInfo.Read(nic.Name);
            if (wifi is { RssiDbm: not null, NoiseDbm: not null } w)
            {
                wifiSignal = w.SignalPercent;
                wifiSnr = w.RssiDbm - w.NoiseDbm;
                if (wifiSnr < 15)
                {
                    issues.Add($"Wi-Fi signal-to-noise ratio is only {wifiSnr} dB. That is consistent with 'full bars but nothing works': something nearby is likely raising the noise floor (a jammer, microwave, or an overlapping neighbor network on the same channel).");
                }
            }
            else if (wifi is { SignalPercent: { } percent })
            {
                wifiSignal = percent;
            }
        }

        string? publicIp = null;
        if (!SkipPublicIP)
        {
            WriteProgress(new ProgressRecord(1, "Network diagnostic", "Checking public IP") { PercentComplete = 60 });
            publicIp = TryGetPublicIp();
        }

        var path = new List<TracerouteHop>();
        if (!SkipTraceroute && gatewayReachable != false)
        {
            WriteProgress(new ProgressRecord(1, "Network diagnostic", "Tracing route") { PercentComplete = 75 });
            path = TraceRoute(Target, MaxHops);
            var lastLatency = 0.0;
            foreach (var hop in path.Where(h => h.LatencyMs is not null))
            {
                if (hop.LatencyMs!.Value - lastLatency > 150 && lastLatency > 0)
                {
                    issues.Add($"Latency jumped by {Math.Round(hop.LatencyMs.Value - lastLatency, 0)} ms at hop {hop.Hop} ({hop.Address}). That hop, or the network just beyond it, is likely where congestion is happening.");
                }

                lastLatency = hop.LatencyMs.Value;
            }

            if (path.Count > 0 && !path[^1].IsDestination)
            {
                issues.Add($"The trace to '{Target}' did not reach the destination within {MaxHops} hops.");
            }
        }

        WriteProgress(new ProgressRecord(1, "Network diagnostic", "Done") { RecordType = ProgressRecordType.Completed });

        var status = gatewayReachable == false
            ? "Unreachable"
            : issues.Count > 0
                ? "Degraded"
                : "Healthy";

        WriteObject(new NetworkDiagnosticResult
        {
            Timestamp = DateTimeOffset.Now,
            Interface = nic.Name,
            LocalIPAddress = info.IPv4Address,
            IsVpnActive = isVpn,
            GatewayAddress = gateway,
            GatewayReachable = gatewayReachable,
            GatewayLatencyMs = gatewayLatencies.Count > 0 ? Math.Round(gatewayLatencies.Average(), 2) : null,
            GatewayJitterMs = gatewayJitter is not null ? Math.Round(gatewayJitter.Value, 2) : null,
            GatewayPacketLossPercent = gatewayLossPercent,
            DnsServers = info.DnsServers,
            DnsResolutionSucceeded = dnsOk,
            DnsResolutionMs = dnsMs is not null ? Math.Round(dnsMs.Value, 2) : null,
            PublicIPAddress = publicIp,
            WifiSignalPercent = wifiSignal,
            WifiSignalToNoiseDb = wifiSnr,
            Path = path,
            Issues = issues,
            Status = status,
        });
    }

    private static List<double> PingMany(string host, int count)
    {
        var latencies = new List<double>(count);
        using var ping = new Ping();
        for (var i = 0; i < count; i++)
        {
            try
            {
                var reply = ping.Send(host, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);
                }
            }
            catch (PingException)
            {
                // Counted as a lost packet.
            }
        }

        return latencies;
    }

    private static (bool? Succeeded, double? Ms) TimeDnsResolution(string target)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Dns.GetHostAddresses(target);
            stopwatch.Stop();
            return (true, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (SocketException)
        {
            stopwatch.Stop();
            return (false, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static string? TryGetPublicIp()
    {
        try
        {
            using var response = SharedHttp.Client.GetAsync("https://1.1.1.1/cdn-cgi/trace", CancellationToken.None).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return GetPublicIPAddressCmdlet.ExtractAddress(body)?.ToString();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return null;
        }
    }

    private static List<TracerouteHop> TraceRoute(string target, int maxHops)
    {
        var hops = new List<TracerouteHop>();
        IPAddress destination;
        try
        {
            destination = Dns.GetHostAddresses(target).First(a => a.AddressFamily == AddressFamily.InterNetwork);
        }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException)
        {
            return hops;
        }

        using var ping = new Ping();
        var buffer = new byte[32];
        for (var ttl = 1; ttl <= maxHops; ttl++)
        {
            var options = new PingOptions(ttl, true);
            PingReply reply;
            try
            {
                reply = ping.Send(destination, 2000, buffer, options);
            }
            catch (PingException)
            {
                hops.Add(new TracerouteHop { Hop = ttl, Address = null, Hostname = null, LatencyMs = null, IsDestination = false });
                continue;
            }

            var isDestination = reply.Status == IPStatus.Success;
            if (reply.Status is IPStatus.Success or IPStatus.TtlExpired)
            {
                var address = reply.Address?.ToString();
                hops.Add(new TracerouteHop
                {
                    Hop = ttl,
                    Address = address,
                    Hostname = address is null ? null : TryReverseDns(reply.Address!),
                    LatencyMs = reply.RoundtripTime,
                    IsDestination = isDestination,
                });
            }
            else
            {
                hops.Add(new TracerouteHop { Hop = ttl, Address = null, Hostname = null, LatencyMs = null, IsDestination = false });
            }

            if (isDestination)
            {
                break;
            }
        }

        return hops;
    }

    private static string? TryReverseDns(IPAddress address)
    {
        try
        {
            var task = Dns.GetHostEntryAsync(address);
            return task.Wait(TimeSpan.FromMilliseconds(300)) ? task.Result.HostName : null;
        }
        catch (Exception ex) when (ex is SocketException or AggregateException)
        {
            return null;
        }
    }
}
