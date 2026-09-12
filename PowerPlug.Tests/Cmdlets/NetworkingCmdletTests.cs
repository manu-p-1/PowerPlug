using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using PowerPlug.Cmdlets.Networking;
using PowerPlug.Internal;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

[Collection(NetworkTestGroup.Name)]
public class TestPortCmdletTests
{
    [Fact]
    public void OpenPortReportsLatency()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<TestPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = [listener.Port];
        h.Run();

        var result = h.Only<PortTestResult>();
        Assert.True(result.Open);
        Assert.NotNull(result.LatencyMs);
        Assert.Null(result.Error);
        Assert.Equal(listener.Port, result.Port);
    }

    [Fact]
    public void ClosedPortReportsError()
    {
        var h = new CmdletHarness<TestPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = [LoopbackListener.FreePort()];
        h.Cmdlet.TimeoutMs = 1000;
        h.Run();

        var result = h.Only<PortTestResult>();
        Assert.False(result.Open);
        Assert.Null(result.LatencyMs);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public void MultiplePortsProduceMultipleResults()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<TestPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = [listener.Port, LoopbackListener.FreePort()];
        h.Run();

        var results = h.OutputOf<PortTestResult>();
        Assert.Equal(2, results.Count);
        Assert.True(results[0].Open);
        Assert.False(results[1].Open);
    }

    [Fact]
    public void TimeoutIsReportedAsErrorNotException()
    {
        // 192.0.2.0/24 is reserved for documentation and never routed, so the connect attempt just times out.
        var h = new CmdletHarness<TestPortCmdlet>();
        h.Cmdlet.HostName = "192.0.2.1";
        h.Cmdlet.Port = [80];
        h.Cmdlet.TimeoutMs = 300;
        h.Run();

        var result = h.Only<PortTestResult>();
        Assert.False(result.Open);
        Assert.Null(result.LatencyMs);
        Assert.Contains("timed out", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}

[Collection(NetworkTestGroup.Name)]
public class WaitPortCmdletTests
{
    [Fact]
    public void ReturnsImmediatelyWhenPortIsOpen()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<WaitPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = listener.Port;
        h.Cmdlet.TimeoutSeconds = 5;
        h.Run();

        var result = h.Only<PortWaitResult>();
        Assert.True(result.Open);
        Assert.Equal(1, result.Attempts);
        Assert.Empty(h.Warnings);
    }

    [Fact]
    public void GivesUpAfterTimeoutAndWarns()
    {
        var h = new CmdletHarness<WaitPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = LoopbackListener.FreePort();
        h.Cmdlet.TimeoutSeconds = 1;
        h.Cmdlet.IntervalMs = 100;
        h.Run();

        var result = h.Only<PortWaitResult>();
        Assert.False(result.Open);
        Assert.True(result.Attempts >= 2, $"Expected several attempts in a second, got {result.Attempts}");
        Assert.True(result.ElapsedMs >= 500, $"Gave up too early: {result.ElapsedMs} ms");
        Assert.Single(h.Warnings);
        Assert.Contains(h.Progress, p => p.RecordType == System.Management.Automation.ProgressRecordType.Completed);
    }

    [Fact]
    public void QuietReturnsBoolean()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<WaitPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = listener.Port;
        h.Cmdlet.Quiet = true;
        h.Run();
        Assert.True(h.Only<bool>());
    }

    [Fact]
    public async Task DetectsPortOpeningLater()
    {
        var port = LoopbackListener.FreePort();
        var h = new CmdletHarness<WaitPortCmdlet>();
        h.Cmdlet.HostName = "127.0.0.1";
        h.Cmdlet.Port = port;
        h.Cmdlet.TimeoutSeconds = 10;
        h.Cmdlet.IntervalMs = 100;

        var listener = new TcpListener(IPAddress.Loopback, port);
        var opener = Task.Run(async () =>
        {
            await Task.Delay(400, TestContext.Current.CancellationToken);
            listener.Start();
        }, TestContext.Current.CancellationToken);

        try
        {
            h.Run();
            await opener;
            var result = h.Only<PortWaitResult>();
            Assert.True(result.Open);
            Assert.True(result.Attempts >= 2, "The first attempt should have failed because the listener was not up yet");
        }
        finally
        {
            listener.Stop();
        }
    }
}

/// <summary>
/// A loopback HTTP server with a handful of fixed routes for Test-Url. Shared per class so it starts once.
/// </summary>
public sealed class LoopbackHttpServer : IDisposable
{
    private readonly HttpListener _server = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serve;

    public string BaseUrl { get; }
    public string? LastUserAgent { get; private set; }
    public string? LastMethod { get; private set; }

    public LoopbackHttpServer()
    {
        var port = LoopbackListener.FreePort();
        BaseUrl = $"http://127.0.0.1:{port}/";
        _server.Prefixes.Add(BaseUrl);
        _server.Start();
        _serve = Task.Run(ServeAsync);
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

            LastUserAgent = context.Request.UserAgent;
            LastMethod = context.Request.HttpMethod;
            var response = context.Response;
            try
            {
                switch (context.Request.Url?.AbsolutePath)
                {
                    case "/ok":
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        response.Headers["Server"] = "PowerPlugTest";
                        var body = "hello"u8.ToArray();
                        response.ContentLength64 = body.Length;
                        if (context.Request.HttpMethod != "HEAD")
                        {
                            await response.OutputStream.WriteAsync(body, _cts.Token);
                        }

                        break;
                    case "/redirect":
                        response.StatusCode = 302;
                        response.RedirectLocation = BaseUrl + "ok";
                        break;
                    case "/slow":
                        try
                        {
                            await Task.Delay(3000, _cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }

                        response.StatusCode = 200;
                        break;
                    case "/echo-auth":
                        response.StatusCode = context.Request.Headers["Authorization"] == "Bearer token" ? 204 : 401;
                        break;
                    default:
                        response.StatusCode = 404;
                        break;
                }
            }
            catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException)
            {
                // Client went away.
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _server.Stop();
        _server.Close();
        _serve.Wait(TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}

[Collection(NetworkTestGroup.Name)]
public class TestUrlCmdletTests : IClassFixture<LoopbackHttpServer>
{
    private readonly LoopbackHttpServer _server;

    public TestUrlCmdletTests(LoopbackHttpServer server)
    {
        _server = server;
    }

    private CmdletHarness<TestUrlCmdlet> Run(string path, Action<TestUrlCmdlet>? configure = null)
    {
        var h = new CmdletHarness<TestUrlCmdlet>();
        h.Cmdlet.Url = [_server.BaseUrl + path];
        configure?.Invoke(h.Cmdlet);
        h.Run();
        return h;
    }

    [Fact]
    public void SuccessfulHeadRequestReportsHeaders()
    {
        var result = Run("ok").Only<UrlTestResult>();

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("OK", result.Status);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(5, result.ContentLength);
        Assert.Equal("PowerPlugTest", result.Server);
        Assert.NotNull(result.LatencyMs);
        Assert.Null(result.Error);
        Assert.Equal("HEAD", _server.LastMethod);
        Assert.Equal(SharedHttp.UserAgent, _server.LastUserAgent);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("OPTIONS")]
    public void MethodIsHonoured(string method)
    {
        Run("ok", c => c.Method = method);
        Assert.Equal(method, _server.LastMethod);
    }

    [Theory]
    [InlineData("redirect", 302, true)]
    [InlineData("missing", 404, false)]
    public void StatusCodeDeterminesSuccess(string path, int status, bool success)
    {
        var result = Run(path).Only<UrlTestResult>();
        Assert.Equal(status, result.StatusCode);
        Assert.Equal(success, result.Success);
    }

    [Fact]
    public void RedirectTargetIsReportedNotFollowed()
    {
        var result = Run("redirect").Only<UrlTestResult>();
        Assert.Equal(_server.BaseUrl + "ok", result.RedirectTo);
    }

    [Fact]
    public void MultipleUrlsProduceOneResultEach()
    {
        var h = new CmdletHarness<TestUrlCmdlet>();
        h.Cmdlet.Url = [_server.BaseUrl + "ok", _server.BaseUrl + "missing"];
        h.Run();

        var results = h.OutputOf<UrlTestResult>();
        Assert.Equal([200, 404], results.Select(r => r.StatusCode));
    }

    [Fact]
    public void TimeoutIsReportedAsError()
    {
        var result = Run("slow", c => c.TimeoutSeconds = 1).Only<UrlTestResult>();
        Assert.False(result.Success);
        Assert.Null(result.StatusCode);
        Assert.Contains("Timed out", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomHeadersAreSent()
    {
        var result = Run("echo-auth", c =>
        {
            c.Method = "GET";
            c.Headers = new System.Collections.Hashtable { ["Authorization"] = "Bearer token" };
        }).Only<UrlTestResult>();
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    public void ContentHeadersAreRejectedWithAWarning()
    {
        var h = Run("ok", c => c.Headers = new System.Collections.Hashtable { ["Content-Type"] = "application/json" });
        Assert.Contains(h.Warnings, w => w.Contains("Content-Type", StringComparison.Ordinal));
        Assert.True(h.Only<UrlTestResult>().Success);
    }

    [Fact]
    public void ConnectionRefusedIsReportedAsError()
    {
        var h = new CmdletHarness<TestUrlCmdlet>();
        h.Cmdlet.Url = [$"http://127.0.0.1:{LoopbackListener.FreePort()}/"];
        h.Run();
        var result = h.Only<UrlTestResult>();
        Assert.False(result.Success);
        Assert.Null(result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public void InvalidSchemeIsReportedAsError()
    {
        var h = new CmdletHarness<TestUrlCmdlet>();
        h.Cmdlet.Url = ["ftp://example.com"];
        h.Run();
        var result = h.Only<UrlTestResult>();
        Assert.False(result.Success);
        Assert.Contains("http", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemeDefaultsToHttps()
    {
        var h = new CmdletHarness<TestUrlCmdlet>();
        h.Cmdlet.Url = ["127.0.0.1:1"];
        h.Cmdlet.TimeoutSeconds = 1;
        h.Run();
        Assert.StartsWith("https://", h.Only<UrlTestResult>().Url, StringComparison.Ordinal);
    }
}

[Collection(NetworkTestGroup.Name)]
public class GetPublicIPAddressCmdletTests
{
    [Theory]
    [InlineData("203.0.113.5\n", "203.0.113.5")]
    [InlineData("  2001:db8::1  ", "2001:db8::1")]
    [InlineData("fl=1f2\nh=1.1.1.1\nip=203.0.113.9\nts=1\n", "203.0.113.9")]
    public void ExtractAddress_HandlesPlainAndTraceFormats(string body, string expected)
    {
        Assert.Equal(IPAddress.Parse(expected), GetPublicIPAddressCmdlet.ExtractAddress(body));
    }

    [Fact]
    public void ExtractAddress_ReturnsNullForGarbage()
    {
        Assert.Null(GetPublicIPAddressCmdlet.ExtractAddress("<html>nope</html>"));
    }

    [Fact]
    public void UnknownProviderWritesError()
    {
        var h = new CmdletHarness<GetPublicIPAddressCmdlet>();
        h.Cmdlet.Provider = "not a provider";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidArgument, h.OnlyError("InvalidProvider").CategoryInfo.Category);
    }

    [Fact]
    public void RefusedConnectionWritesLookupFailed()
    {
        var h = new CmdletHarness<GetPublicIPAddressCmdlet>();
        h.Cmdlet.Provider = $"http://127.0.0.1:{LoopbackListener.FreePort()}/ip";
        h.Cmdlet.TimeoutSeconds = 2;
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.ConnectionError, h.OnlyError("LookupFailed").CategoryInfo.Category);
    }

    private static async Task<CmdletHarness<GetPublicIPAddressCmdlet>> RunAgainst(string body, Action<GetPublicIPAddressCmdlet>? configure = null)
    {
        var port = LoopbackListener.FreePort();
        using var server = new HttpListener();
        server.Prefixes.Add($"http://127.0.0.1:{port}/");
        server.Start();
        var serve = Task.Run(async () =>
        {
            var ctx = await server.GetContextAsync();
            var bytes = System.Text.Encoding.ASCII.GetBytes(body);
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
            ctx.Response.Close();
        }, TestContext.Current.CancellationToken);

        var h = new CmdletHarness<GetPublicIPAddressCmdlet>();
        h.Cmdlet.Provider = $"http://127.0.0.1:{port}/ip";
        configure?.Invoke(h.Cmdlet);
        h.Run();
        await serve;
        return h;
    }

    [Fact]
    public async Task CustomUrlProviderIsUsed()
    {
        var h = await RunAgainst("198.51.100.7");

        var result = h.Only<PublicIPResult>();
        Assert.Equal("198.51.100.7", result.IPAddress);
        Assert.Equal("IPv4", result.AddressFamily);
        Assert.Equal("127.0.0.1", result.Provider);
        Assert.True(result.LatencyMs >= 0);
    }

    [Fact]
    public async Task IPv6AnswerIsLabelled()
    {
        var h = await RunAgainst("2001:db8::42\n");
        Assert.Equal("IPv6", h.Only<PublicIPResult>().AddressFamily);
    }

    [Fact]
    public async Task GarbageBodyWritesUnexpectedResponse()
    {
        var h = await RunAgainst("<html>not an ip</html>");
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.InvalidData, h.OnlyError("UnexpectedResponse").CategoryInfo.Category);
    }
}

