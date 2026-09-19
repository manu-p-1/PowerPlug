using System.Net;
using PowerPlug.Cmdlets.Networking;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

/// <summary>
/// A loopback HTTP server that serves a fixed number of bytes and accepts uploads, so Get-Speed never leaves the
/// machine in tests.
/// </summary>
public sealed class LoopbackSpeedServer : IDisposable
{
    private readonly HttpListener _server = new();
    private readonly CancellationTokenSource _cts = new();

    public string BaseUrl { get; }
    public long LastUploadBytes { get; private set; }

    public LoopbackSpeedServer()
    {
        var port = LoopbackListener.FreePort();
        BaseUrl = $"http://127.0.0.1:{port}/";
        _server.Prefixes.Add(BaseUrl);
        _server.Start();
        _ = Task.Run(ServeAsync);
    }

    private async Task ServeAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _server.GetContextAsync();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                return;
            }

            try
            {
                if (context.Request.HttpMethod == "POST")
                {
                    using var sink = new MemoryStream();
                    await context.Request.InputStream.CopyToAsync(sink, _cts.Token);
                    LastUploadBytes = sink.Length;
                    context.Response.StatusCode = 200;
                }
                else
                {
                    var bytes = long.Parse(context.Request.QueryString["bytes"] ?? "100000", System.Globalization.CultureInfo.InvariantCulture);
                    context.Response.ContentLength64 = bytes;
                    var chunk = new byte[64 * 1024];
                    var remaining = bytes;
                    while (remaining > 0)
                    {
                        var take = (int)Math.Min(chunk.Length, remaining);
                        await context.Response.OutputStream.WriteAsync(chunk.AsMemory(0, take), _cts.Token);
                        remaining -= take;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpListenerException or IOException or OperationCanceledException)
            {
                // Client went away.
            }
            finally
            {
                context.Response.Close();
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _server.Stop();
        _server.Close();
        _cts.Dispose();
    }
}

[Collection(NetworkTestGroup.Name)]
public class GetSpeedCmdletTests : IClassFixture<LoopbackSpeedServer>
{
    private readonly LoopbackSpeedServer _server;

    public GetSpeedCmdletTests(LoopbackSpeedServer server)
    {
        _server = server;
    }

    private CmdletHarness<GetSpeedCmdlet> Harness()
    {
        var h = new CmdletHarness<GetSpeedCmdlet>();
        h.Cmdlet.LatencyHost = "127.0.0.1";
        h.Cmdlet.PingCount = 2;
        h.Cmdlet.DownloadUrl = _server.BaseUrl + "down?bytes=200000";
        h.Cmdlet.UploadUrl = _server.BaseUrl + "up";
        h.Cmdlet.DownloadSize = 200_000;
        h.Cmdlet.UploadSize = 150_000;
        return h;
    }

    [Fact]
    public void FullRunReportsDownloadAndUpload()
    {
        var h = Harness();
        h.Run();

        Assert.Empty(h.Errors);
        var result = h.Only<SpeedTestResult>();
        Assert.Equal(200_000, result.DownloadBytes);
        Assert.NotNull(result.DownloadMbps);
        Assert.True(result.DownloadMbps > 0);
        Assert.Equal(150_000, result.UploadBytes);
        Assert.NotNull(result.UploadMbps);
        Assert.Equal(150_000, _server.LastUploadBytes);
        Assert.Contains(result.LatencyMethod, new[] { "ICMP", "TCP" });
        Assert.Contains(h.Progress, p => p.RecordType == System.Management.Automation.ProgressRecordType.Completed);
    }

    [Fact]
    public void LatencyOnlySkipsTransfers()
    {
        var h = Harness();
        h.Cmdlet.LatencyOnly = true;
        h.Run();

        var result = h.Only<SpeedTestResult>();
        Assert.Null(result.DownloadMbps);
        Assert.Equal(0, result.DownloadBytes);
        Assert.Null(result.UploadMbps);
        Assert.Equal(0, result.UploadBytes);
        Assert.NotNull(result.PacketLossPercent);
    }

    [Fact]
    public void SkipUploadLeavesUploadEmpty()
    {
        var h = Harness();
        h.Cmdlet.SkipUpload = true;
        h.Run();

        var result = h.Only<SpeedTestResult>();
        Assert.True(result.DownloadBytes > 0);
        Assert.Null(result.UploadMbps);
        Assert.Equal(0, result.UploadBytes);
    }

    [Fact]
    public void UnreachableDownloadUrlWarnsInsteadOfThrowing()
    {
        var h = Harness();
        h.Cmdlet.DownloadUrl = $"http://127.0.0.1:{LoopbackListener.FreePort()}/nothing";
        h.Cmdlet.SkipUpload = true;
        h.Run();

        var result = h.Only<SpeedTestResult>();
        Assert.Null(result.DownloadMbps);
        Assert.Equal(0, result.DownloadBytes);
        Assert.Contains(h.Warnings, w => w.StartsWith("Download test failed", StringComparison.Ordinal));
    }

    [Fact]
    public void LatencyFieldsAreConsistent()
    {
        var h = Harness();
        h.Cmdlet.LatencyOnly = true;
        h.Cmdlet.PingCount = 3;
        h.Run();

        var result = h.Only<SpeedTestResult>();
        if (result.LatencyMinMs is not null)
        {
            Assert.True(result.LatencyMinMs <= result.LatencyAvgMs);
            Assert.True(result.LatencyAvgMs <= result.LatencyMaxMs);
            Assert.NotNull(result.JitterMs);
            Assert.Equal(0, result.PacketLossPercent);
        }
        else
        {
            Assert.Equal(100, result.PacketLossPercent);
        }
    }
}
