using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Networking
{
    /// <summary>
    /// <para type="synopsis">Performs a network speed test with download, upload, and latency measurements</para>
    /// <para type="description">Get-Speed tests your network connectivity by measuring download speed, upload speed,
    /// and latency. It also reports basic network interface information. By default it uses Cloudflare's speed test
    /// endpoints, but custom URLs can be specified. Results are reported in Mbps.</para>
    /// <example>
    /// <para>Run a basic speed test</para>
    /// <code>Get-Speed</code>
    /// </example>
    /// <example>
    /// <para>Run a speed test with a larger download payload</para>
    /// <code>Get-Speed -DownloadSize 50MB</code>
    /// </example>
    /// <example>
    /// <para>Run a speed test with custom endpoints</para>
    /// <code>Get-Speed -DownloadUrl "https://myserver.com/testfile" -UploadUrl "https://myserver.com/upload"</code>
    /// </example>
    /// <example>
    /// <para>Measure only latency</para>
    /// <code>Get-Speed -LatencyOnly</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Speed")]
    [Alias("speedtest", "gspd")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class GetSpeedCmdlet : PowerPlugCmdletBase
    {
        private const string DefaultUploadUrl = "https://speed.cloudflare.com/__up";
        private const string DefaultLatencyHost = "1.1.1.1";
        private const int DefaultPingCount = 5;

        private static readonly HttpClient SpeedTestClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PowerPlug-SpeedTest/0.9.0");
            return client;
        }

        /// <summary>
        /// <para type="description">URL to download from for the speed test. Defaults to Cloudflare speed test.</para>
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">URL to upload to for the speed test. Defaults to Cloudflare speed test.</para>
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string UploadUrl { get; set; } = DefaultUploadUrl;

        /// <summary>
        /// <para type="description">Host to ping for latency measurement (default: 1.1.1.1)</para>
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string LatencyHost { get; set; } = DefaultLatencyHost;

        /// <summary>
        /// <para type="description">Size in bytes for the download test (default: 10MB, range: 100KB-100MB)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(100_000, 100_000_000)]
        public int DownloadSize { get; set; } = 10_000_000;

        /// <summary>
        /// <para type="description">Size in bytes for the upload test (default: 5MB, range: 100KB-50MB)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(100_000, 50_000_000)]
        public int UploadSize { get; set; } = 5_000_000;

        /// <summary>
        /// <para type="description">Number of ping probes for latency measurement (default: 5, range: 1-20)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(1, 20)]
        public int PingCount { get; set; } = DefaultPingCount;

        /// <summary>
        /// <para type="description">Only measure latency, skip download and upload tests</para>
        /// </summary>
        [Parameter]
        public SwitchParameter LatencyOnly { get; set; }

        /// <summary>
        /// <para type="description">Skip the upload test</para>
        /// </summary>
        [Parameter]
        public SwitchParameter SkipUpload { get; set; }

        /// <summary>
        /// Processes the Get-Speed PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var result = new PSObject();

            // --- Network interface info ---
            WriteProgress(new ProgressRecord(1, "Speed Test", "Gathering network information...") { PercentComplete = 5 });
            AddNetworkInfo(result);

            // --- Latency ---
            WriteProgress(new ProgressRecord(1, "Speed Test", "Measuring latency...") { PercentComplete = 15 });
            MeasureLatency(result);

            if (!LatencyOnly)
            {
                // --- Download ---
                WriteProgress(new ProgressRecord(1, "Speed Test", "Measuring download speed...") { PercentComplete = 30 });
                MeasureDownload(result);

                // --- Upload ---
                if (!SkipUpload)
                {
                    WriteProgress(new ProgressRecord(1, "Speed Test", "Measuring upload speed...") { PercentComplete = 70 });
                    MeasureUpload(result);
                }
            }

            WriteProgress(new ProgressRecord(1, "Speed Test", "Complete") { PercentComplete = 100, RecordType = ProgressRecordType.Completed });
            WriteObject(result);
        }

        private void AddNetworkInfo(PSObject result)
        {
            try
            {
                var activeInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .OrderByDescending(n => n.GetIPv4Statistics().BytesReceived)
                    .FirstOrDefault();

                if (activeInterface != null)
                {
                    var ipProps = activeInterface.GetIPProperties();
                    var ipv4Addr = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                    result.Properties.Add(new PSNoteProperty("Interface", activeInterface.Name));
                    result.Properties.Add(new PSNoteProperty("InterfaceType", activeInterface.NetworkInterfaceType.ToString()));
                    result.Properties.Add(new PSNoteProperty("LinkSpeedMbps", activeInterface.Speed / 1_000_000));
                    result.Properties.Add(new PSNoteProperty("LocalIP", ipv4Addr?.Address.ToString() ?? "N/A"));

                    var gateway = ipProps.GatewayAddresses
                        .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
                    result.Properties.Add(new PSNoteProperty("Gateway", gateway?.Address.ToString() ?? "N/A"));

                    var dnsServers = string.Join(", ", ipProps.DnsAddresses
                        .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                        .Select(d => d.ToString()));
                    result.Properties.Add(new PSNoteProperty("DnsServers", string.IsNullOrEmpty(dnsServers) ? "N/A" : dnsServers));
                }
                else
                {
                    result.Properties.Add(new PSNoteProperty("Interface", "None detected"));
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Could not gather network info: {ex.Message}");
                result.Properties.Add(new PSNoteProperty("Interface", "Error"));
            }
        }

        private void MeasureLatency(PSObject result)
        {
            try
            {
                using var pinger = new Ping();
                var latencies = new double[PingCount];
                var successCount = 0;

                for (var i = 0; i < PingCount; i++)
                {
                    try
                    {
                        var reply = pinger.Send(LatencyHost, 5000);
                        if (reply.Status == IPStatus.Success)
                        {
                            latencies[successCount] = reply.RoundtripTime;
                            successCount++;
                        }

                        WriteProgress(new ProgressRecord(1, "Speed Test",
                            $"Measuring latency... ping {i + 1}/{PingCount}")
                        { PercentComplete = 15 + (15 * i / PingCount) });
                    }
                    catch (PingException)
                    {
                        // Individual ping failure
                    }
                }

                if (successCount > 0)
                {
                    var validLatencies = latencies.Take(successCount).OrderBy(l => l).ToArray();
                    result.Properties.Add(new PSNoteProperty("LatencyMinMs", Math.Round(validLatencies[0], 2)));
                    result.Properties.Add(new PSNoteProperty("LatencyMaxMs", Math.Round(validLatencies[^1], 2)));
                    result.Properties.Add(new PSNoteProperty("LatencyAvgMs", Math.Round(validLatencies.Average(), 2)));

                    // Jitter = average of absolute differences between consecutive pings
                    if (successCount > 1)
                    {
                        var jitter = 0.0;
                        for (var i = 1; i < successCount; i++)
                        {
                            jitter += Math.Abs(validLatencies[i] - validLatencies[i - 1]);
                        }
                        result.Properties.Add(new PSNoteProperty("JitterMs", Math.Round(jitter / (successCount - 1), 2)));
                    }
                    else
                    {
                        result.Properties.Add(new PSNoteProperty("JitterMs", 0.0));
                    }

                    result.Properties.Add(new PSNoteProperty("PacketLoss",
                        $"{Math.Round((1.0 - (double)successCount / PingCount) * 100, 1)}%"));
                }
                else
                {
                    result.Properties.Add(new PSNoteProperty("LatencyMinMs", (object)null!));
                    result.Properties.Add(new PSNoteProperty("LatencyMaxMs", (object)null!));
                    result.Properties.Add(new PSNoteProperty("LatencyAvgMs", (object)null!));
                    result.Properties.Add(new PSNoteProperty("JitterMs", (object)null!));
                    result.Properties.Add(new PSNoteProperty("PacketLoss", "100%"));
                    WriteWarning("All ping probes failed. Host may be unreachable or ICMP may be blocked.");
                }
            }
            catch (PlatformNotSupportedException)
            {
                WriteWarning("ICMP ping is not supported on this platform. Falling back to TCP latency.");
                MeasureTcpLatency(result);
            }
        }

        private void MeasureTcpLatency(PSObject result)
        {
            var latencies = new double[PingCount];
            var successCount = 0;

            for (var i = 0; i < PingCount; i++)
            {
                try
                {
                    using var client = new TcpClient();
                    var sw = Stopwatch.StartNew();
                    var task = client.ConnectAsync(LatencyHost, 443);
                    if (task.Wait(5000))
                    {
                        sw.Stop();
                        latencies[successCount] = sw.Elapsed.TotalMilliseconds;
                        successCount++;
                    }
                }
                catch (Exception)
                {
                    // TCP probe failed
                }
            }

            if (successCount > 0)
            {
                var valid = latencies.Take(successCount).OrderBy(l => l).ToArray();
                result.Properties.Add(new PSNoteProperty("LatencyMinMs", Math.Round(valid[0], 2)));
                result.Properties.Add(new PSNoteProperty("LatencyMaxMs", Math.Round(valid[^1], 2)));
                result.Properties.Add(new PSNoteProperty("LatencyAvgMs", Math.Round(valid.Average(), 2)));
                result.Properties.Add(new PSNoteProperty("JitterMs", (object)null!));
                result.Properties.Add(new PSNoteProperty("PacketLoss", "N/A (TCP fallback)"));
            }
            else
            {
                result.Properties.Add(new PSNoteProperty("LatencyMinMs", (object)null!));
                result.Properties.Add(new PSNoteProperty("LatencyMaxMs", (object)null!));
                result.Properties.Add(new PSNoteProperty("LatencyAvgMs", (object)null!));
                result.Properties.Add(new PSNoteProperty("JitterMs", (object)null!));
                result.Properties.Add(new PSNoteProperty("PacketLoss", "100%"));
            }
        }

        private void MeasureDownload(PSObject result)
        {
            try
            {
                var url = string.IsNullOrEmpty(DownloadUrl)
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "https://speed.cloudflare.com/__down?bytes={0}", DownloadSize)
                    : DownloadUrl;

                var sw = Stopwatch.StartNew();
                using var response = SpeedTestClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                var buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytes += bytesRead;

                    if (totalBytes % (DownloadSize / 10) < buffer.Length)
                    {
                        var pct = (int)(30 + 40.0 * totalBytes / DownloadSize);
                        WriteProgress(new ProgressRecord(1, "Speed Test",
                            $"Downloading... {totalBytes / 1_000_000.0:F1} MB")
                        { PercentComplete = Math.Min(pct, 69) });
                    }
                }

                sw.Stop();

                var durationSec = sw.Elapsed.TotalSeconds;
                var bitsPerSecond = totalBytes * 8.0 / durationSec;
                var mbps = bitsPerSecond / 1_000_000.0;

                result.Properties.Add(new PSNoteProperty("DownloadMbps", Math.Round(mbps, 2)));
                result.Properties.Add(new PSNoteProperty("DownloadBytes", totalBytes));
                result.Properties.Add(new PSNoteProperty("DownloadDurationSec", Math.Round(durationSec, 2)));
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                WriteWarning($"Download test failed: {msg}");
                result.Properties.Add(new PSNoteProperty("DownloadMbps", (object)null!));
                result.Properties.Add(new PSNoteProperty("DownloadBytes", 0L));
                result.Properties.Add(new PSNoteProperty("DownloadDurationSec", (object)null!));
            }
        }

        private void MeasureUpload(PSObject result)
        {
            try
            {
                // Generate random upload payload
                var payload = new byte[UploadSize];
                System.Security.Cryptography.RandomNumberGenerator.Fill(payload);

                var sw = Stopwatch.StartNew();
                using var content = new ByteArrayContent(payload);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                using var response = SpeedTestClient.PostAsync(UploadUrl, content)
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                sw.Stop();

                var durationSec = sw.Elapsed.TotalSeconds;
                var bitsPerSecond = UploadSize * 8.0 / durationSec;
                var mbps = bitsPerSecond / 1_000_000.0;

                result.Properties.Add(new PSNoteProperty("UploadMbps", Math.Round(mbps, 2)));
                result.Properties.Add(new PSNoteProperty("UploadBytes", (long)UploadSize));
                result.Properties.Add(new PSNoteProperty("UploadDurationSec", Math.Round(durationSec, 2)));
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                WriteWarning($"Upload test failed: {msg}");
                result.Properties.Add(new PSNoteProperty("UploadMbps", (object)null!));
                result.Properties.Add(new PSNoteProperty("UploadBytes", 0L));
                result.Properties.Add(new PSNoteProperty("UploadDurationSec", (object)null!));
            }
        }
    }
}
