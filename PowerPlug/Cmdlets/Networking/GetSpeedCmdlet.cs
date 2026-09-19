using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Measures download speed, upload speed and latency.</para>
/// <para type="description">Runs a quick speed test against Cloudflare's public speed test endpoints by default and
/// reports throughput in Mbps along with ICMP latency, jitter and packet loss. Custom download and upload URLs can
/// be supplied to test against your own servers. Data is sent to the chosen endpoints during the test.</para>
/// <example>
/// <para>Run a speed test</para>
/// <code>Get-Speed</code>
/// </example>
/// <example>
/// <para>Latency only</para>
/// <code>Get-Speed -LatencyOnly</code>
/// </example>
/// <example>
/// <para>Bigger download sample against a custom server</para>
/// <code>Get-Speed -DownloadUrl https://files.example.com/100mb.bin -SkipUpload</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "Speed")]
[Alias("speedtest", "gspd")]
[OutputType(typeof(SpeedTestResult))]
[ExperimentalCmdlet("It sends and receives test data over the internet. Results vary with the remote endpoint and network load.")]
public sealed class GetSpeedCmdlet : PowerPlugCmdlet, IDisposable
{
    private const string CloudflareDownload = "https://speed.cloudflare.com/__down?bytes=";
    private const string CloudflareUpload = "https://speed.cloudflare.com/__up";

    /// <summary>
    /// <para type="description">URL to download from. Defaults to Cloudflare.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// <para type="description">URL to upload to. Defaults to Cloudflare.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string UploadUrl { get; set; } = CloudflareUpload;

    /// <summary>
    /// <para type="description">Host to ping for latency. Defaults to 1.1.1.1.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string LatencyHost { get; set; } = "1.1.1.1";

    /// <summary>
    /// <para type="description">Download sample size in bytes. Defaults to 10 MB.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(100_000, 500_000_000)]
    public int DownloadSize { get; set; } = 10_000_000;

    /// <summary>
    /// <para type="description">Upload sample size in bytes. Defaults to 5 MB.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(100_000, 100_000_000)]
    public int UploadSize { get; set; } = 5_000_000;

    /// <summary>
    /// <para type="description">Number of latency probes. Defaults to 5.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 50)]
    public int PingCount { get; set; } = 5;

    /// <summary>
    /// <para type="description">Only measure latency.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter LatencyOnly { get; set; }

    /// <summary>
    /// <para type="description">Skip the upload test.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter SkipUpload { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var (nicName, localIp) = FindActiveInterface();
        Progress("Measuring latency", 10);
        var (latencies, method) = MeasureLatency();

        double? downloadMbps = null;
        long downloadBytes = 0;
        double? uploadMbps = null;
        long uploadBytes = 0;

        if (!LatencyOnly && !_cancellation.IsCancellationRequested)
        {
            Progress("Measuring download", 30);
            (downloadMbps, downloadBytes) = MeasureDownload();

            if (!SkipUpload && !_cancellation.IsCancellationRequested)
            {
                Progress("Measuring upload", 70);
                (uploadMbps, uploadBytes) = MeasureUpload();
            }
        }

        WriteProgress(new ProgressRecord(1, "Speed test", "Done") { RecordType = ProgressRecordType.Completed });

        var sorted = latencies.OrderBy(l => l).ToArray();
        WriteObject(new SpeedTestResult
        {
            Interface = nicName,
            LocalIP = localIp,
            LatencyMinMs = sorted.Length > 0 ? Math.Round(sorted[0], 2) : null,
            LatencyAvgMs = sorted.Length > 0 ? Math.Round(sorted.Average(), 2) : null,
            LatencyMaxMs = sorted.Length > 0 ? Math.Round(sorted[^1], 2) : null,
            JitterMs = sorted.Length > 1 ? Math.Round(Statistics.Jitter(latencies), 2) : null,
            PacketLossPercent = Math.Round(100.0 * (PingCount - latencies.Count) / PingCount, 1),
            LatencyMethod = method,
            DownloadMbps = downloadMbps,
            DownloadBytes = downloadBytes,
            UploadMbps = uploadMbps,
            UploadBytes = uploadBytes,
        });
    }

    private void Progress(string activity, int percent) =>
        WriteProgress(new ProgressRecord(1, "Speed test", activity) { PercentComplete = percent });

    private (string? Name, string? Ip) FindActiveInterface()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => (Nic: n, Received: SafeBytesReceived(n)))
                .OrderByDescending(x => x.Received)
                .Select(x => x.Nic)
                .FirstOrDefault();

            if (nic is null)
            {
                return (null, null);
            }

            var ipv4 = nic.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
            return (nic.Name, ipv4?.Address.ToString());
        }
        catch (NetworkInformationException ex)
        {
            WriteWarning($"Could not read network interfaces: {ex.Message}");
            return (null, null);
        }
    }

    private static long SafeBytesReceived(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPStatistics().BytesReceived;
        }
        catch (Exception ex) when (ex is NetworkInformationException or PlatformNotSupportedException)
        {
            return 0;
        }
    }

    private (List<double> Latencies, string Method) MeasureLatency()
    {
        var latencies = new List<double>(PingCount);
        try
        {
            using var ping = new Ping();
            for (var i = 0; i < PingCount && !_cancellation.IsCancellationRequested; i++)
            {
                try
                {
                    var reply = ping.Send(LatencyHost, 3000);
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

            return (latencies, "ICMP");
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException or InvalidOperationException)
        {
            WriteVerbose($"ICMP unavailable ({ex.Message}); using TCP connect latency to port 443.");
        }

        for (var i = 0; i < PingCount && !_cancellation.IsCancellationRequested; i++)
        {
            var probe = TcpProbe.Connect(LatencyHost, 443, TimeSpan.FromSeconds(3), _cancellation.Token);
            if (probe.Open)
            {
                latencies.Add(probe.LatencyMs);
            }
        }

        return (latencies, "TCP");
    }

    private (double? Mbps, long Bytes) MeasureDownload()
    {
        var url = DownloadUrl ?? CloudflareDownload + DownloadSize.ToString(CultureInfo.InvariantCulture);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            var stopwatch = Stopwatch.StartNew();
            using var response = SharedHttp.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult();
            var buffer = new byte[1 << 16];
            long total = 0;
            var expected = response.Content.Headers.ContentLength ?? DownloadSize;
            var nextReport = expected / 10;
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total >= nextReport)
                {
                    Progress($"Downloading {ByteSize.Format(total)}", (int)Math.Min(69, 30 + 40.0 * total / expected));
                    nextReport += expected / 10;
                }
            }

            stopwatch.Stop();
            return (Math.Round(total * 8.0 / stopwatch.Elapsed.TotalSeconds / 1_000_000, 2), total);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            WriteWarning($"Download test failed: {ex.InnerException?.Message ?? ex.Message}");
            return (null, 0);
        }
    }

    private (double? Mbps, long Bytes) MeasureUpload()
    {
        try
        {
            var payload = new byte[UploadSize];
            Random.Shared.NextBytes(payload);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            using var content = new ByteArrayContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var stopwatch = Stopwatch.StartNew();
            using var response = SharedHttp.Client.PostAsync(UploadUrl, content, cts.Token).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            stopwatch.Stop();

            return (Math.Round(UploadSize * 8.0 / stopwatch.Elapsed.TotalSeconds / 1_000_000, 2), UploadSize);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            WriteWarning($"Upload test failed: {ex.InnerException?.Message ?? ex.Message}");
            return (null, 0);
        }
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
