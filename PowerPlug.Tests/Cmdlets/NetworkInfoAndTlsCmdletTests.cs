using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PowerPlug.Cmdlets.Networking;
using PowerPlug.Internal;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class GetNetworkInfoCmdletTests
{
    [Fact]
    public void AllIncludesLoopbackAndEveryInterface()
    {
        var h = new CmdletHarness<GetNetworkInfoCmdlet>();
        h.Cmdlet.All = true;
        h.Run();

        var rows = h.OutputOf<NetworkInterfaceInfo>();
        Assert.Equal(NetworkInterface.GetAllNetworkInterfaces().Length, rows.Count + h.Errors.Count);
        Assert.Contains(rows, r => r.Type == "Loopback");
    }

    [Fact]
    public void DefaultReturnsOnlyUpNonLoopbackInterfaces()
    {
        var all = new CmdletHarness<GetNetworkInfoCmdlet>();
        all.Cmdlet.All = true;
        all.Run();

        var h = new CmdletHarness<GetNetworkInfoCmdlet>();
        h.Run();
        var rows = h.OutputOf<NetworkInterfaceInfo>();

        Assert.All(rows, r =>
        {
            Assert.Equal("Up", r.Status);
            Assert.NotEqual("Loopback", r.Type);
        });
        Assert.True(rows.Count < all.OutputOf<NetworkInterfaceInfo>().Count);
    }

    [Fact]
    public void NameFilterMatchesExactlyOneInterface()
    {
        var all = new CmdletHarness<GetNetworkInfoCmdlet>();
        all.Cmdlet.All = true;
        all.Run();
        var target = all.OutputOf<NetworkInterfaceInfo>().First(r => r.Type == "Loopback");

        var h = new CmdletHarness<GetNetworkInfoCmdlet>();
        h.Cmdlet.All = true;
        h.Cmdlet.Name = target.Name;
        h.Run();

        var row = h.Only<NetworkInterfaceInfo>();
        Assert.Equal(target.Name, row.Name);
    }

    [Fact]
    public void NameFilterWithNoMatchReturnsNothing()
    {
        var h = new CmdletHarness<GetNetworkInfoCmdlet>();
        h.Cmdlet.All = true;
        h.Cmdlet.Name = "no-such-interface-*";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Empty(h.Errors);
    }

    [Fact]
    public void Describe_LoopbackHasExpectedShape()
    {
        var loopback = NetworkInterface.GetAllNetworkInterfaces().First(n => n.NetworkInterfaceType == NetworkInterfaceType.Loopback);
        var info = GetNetworkInfoCmdlet.Describe(loopback);

        Assert.Equal(loopback.Name, info.Name);
        Assert.Equal("Loopback", info.Type);
        Assert.Contains("127.0.0.1", new[] { info.IPv4Address ?? string.Empty });
        Assert.NotNull(info.IPv6Addresses);
        Assert.NotNull(info.Gateways);
        Assert.NotNull(info.DnsServers);
        if (info.MacAddress is not null)
        {
            Assert.Matches("^([0-9A-F]{2}:)*[0-9A-F]{2}$", info.MacAddress);
        }
    }
}

[Collection(NetworkTestGroup.Name)]
public class GetListeningPortCmdletTests
{
    [Fact]
    public void FindsOurOwnListenerWithoutProcessLookup()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<GetListeningPortCmdlet>();
        h.Cmdlet.Port = [listener.Port];
        h.Cmdlet.NoProcess = true;
        h.Cmdlet.Protocol = "TCP";
        h.Run();

        var row = Assert.Single(h.OutputOf<ListeningPort>(), r => r.Port == listener.Port);
        Assert.Equal("TCP", row.Protocol);
        Assert.Null(row.ProcessId);
        Assert.Empty(h.Warnings);
    }

    [Fact]
    public void ProcessLookupFindsCurrentProcess()
    {
        Assert.SkipWhen(ListeningPortOwners.Discover().Count == 0, "No netstat/lsof/ss available or no permission to read socket owners.");

        using var listener = new LoopbackListener();
        var h = new CmdletHarness<GetListeningPortCmdlet>();
        h.Cmdlet.Port = [listener.Port];
        h.Cmdlet.Protocol = "TCP";
        h.Run();

        var row = Assert.Single(h.OutputOf<ListeningPort>(), r => r.Port == listener.Port);
        Assert.Equal(Environment.ProcessId, row.ProcessId);
        Assert.False(string.IsNullOrEmpty(row.ProcessName));
    }

    [Fact]
    public void UdpOnlyExcludesTcpListener()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<GetListeningPortCmdlet>();
        h.Cmdlet.Protocol = "UDP";
        h.Cmdlet.NoProcess = true;
        h.Run();

        var rows = h.OutputOf<ListeningPort>();
        Assert.All(rows, r => Assert.Equal("UDP", r.Protocol));
        Assert.DoesNotContain(rows, r => r.Port == listener.Port);
    }

    [Fact]
    public void PortFilterWithNoMatchReturnsNothing()
    {
        var h = new CmdletHarness<GetListeningPortCmdlet>();
        h.Cmdlet.Port = [LoopbackListener.FreePort()];
        h.Cmdlet.NoProcess = true;
        h.Run();
        Assert.Empty(h.Output);
    }

    [Fact]
    public void OutputIsSortedByProtocolThenPort()
    {
        using var a = new LoopbackListener();
        using var b = new LoopbackListener();
        var h = new CmdletHarness<GetListeningPortCmdlet>();
        h.Cmdlet.NoProcess = true;
        h.Run();

        var rows = h.OutputOf<ListeningPort>();
        var sorted = rows.OrderBy(r => r.Protocol, StringComparer.Ordinal).ThenBy(r => r.Port).ThenBy(r => r.LocalAddress, StringComparer.Ordinal).ToList();
        Assert.Equal(sorted.Select(r => (r.Protocol, r.Port, r.LocalAddress)), rows.Select(r => (r.Protocol, r.Port, r.LocalAddress)));
    }
}

/// <summary>
/// A loopback TLS server with a self-signed certificate so the certificate cmdlet has something real to talk to.
/// </summary>
public sealed class LoopbackTlsServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();

    /// <summary>The server certificate, or null when the platform could not create one (see <see cref="Unavailable"/>).</summary>
    public X509Certificate2? Certificate { get; }

    /// <summary>Why the server could not start, when it could not.</summary>
    public string? Unavailable { get; }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public LoopbackTlsServer()
    {
        try
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=powerplug.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("powerplug.test");
            san.AddDnsName("alt.powerplug.test");
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
            using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

            // Export and re-import so the private key is usable by SslStream on every platform.
#pragma warning disable SYSLIB0057 // The loader API is net9+, and the test project also targets net8.
            Certificate = new X509Certificate2(created.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
        }
        catch (CryptographicException ex)
        {
            // .NET 8 on macOS needs an unlocked keychain to attach a private key; skip rather than fail.
            Unavailable = $"Could not create a self-signed certificate on this platform: {ex.Message}";
            return;
        }

        _listener.Start();
        _ = Task.Run(ServeAsync);
    }

    private async Task ServeAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                using var c = client;
                using var ssl = new SslStream(c.GetStream(), false);
                try
                {
                    await ssl.AuthenticateAsServerAsync(Certificate!, false, false);
                    await Task.Delay(200, _cts.Token);
                }
                catch (Exception ex) when (ex is IOException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
                {
                    // Client closed early or the test is shutting down.
                }
            });
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        Certificate?.Dispose();
        _cts.Dispose();
    }
}

[Collection(NetworkTestGroup.Name)]
public class GetTlsCertificateCmdletTests : IClassFixture<LoopbackTlsServer>
{
    private readonly LoopbackTlsServer _server;

    public GetTlsCertificateCmdletTests(LoopbackTlsServer server)
    {
        _server = server;
    }

    private X509Certificate2 ServerCertificate()
    {
        Assert.SkipWhen(_server.Certificate is null, _server.Unavailable ?? "TLS server unavailable");
        return _server.Certificate!;
    }

    [Fact]
    public void SelfSignedCertificateIsReportedAsUntrustedButReturned()
    {
        var certificate = ServerCertificate();
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = ["127.0.0.1"];
        h.Cmdlet.Port = _server.Port;
        h.Cmdlet.TimeoutSeconds = 5;
        h.Run();

        Assert.Empty(h.Errors);
        var info = h.Only<TlsCertificateInfo>();
        Assert.Equal("CN=powerplug.test", info.Subject);
        Assert.Equal(info.Subject, info.Issuer);
        Assert.False(info.ChainTrusted);
        Assert.True(info.IsValid);
        Assert.InRange(info.DaysRemaining, 28, 30);
        Assert.Contains("powerplug.test", info.SubjectAlternativeNames);
        Assert.Contains("alt.powerplug.test", info.SubjectAlternativeNames);
        Assert.Contains("127.0.0.1", info.SubjectAlternativeNames);
        Assert.Equal(Convert.ToHexString(certificate.GetCertHash(HashAlgorithmName.SHA256)), info.Thumbprint);
        Assert.StartsWith("Tls1", info.Protocol, StringComparison.Ordinal);
        Assert.Equal(_server.Port, info.Port);
    }

    [Theory]
    [InlineData("127.0.0.1:{0}")]
    [InlineData("https://127.0.0.1:{0}/some/path")]
    public void HostPortAndUrlFormsAreParsed(string format)
    {
        ServerCertificate();
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = [string.Format(System.Globalization.CultureInfo.InvariantCulture, format, _server.Port)];
        h.Cmdlet.TimeoutSeconds = 5;
        h.Run();

        Assert.Empty(h.Errors);
        var info = h.Only<TlsCertificateInfo>();
        Assert.Equal("127.0.0.1", info.Host);
        Assert.Equal(_server.Port, info.Port);
    }

    [Fact]
    public void PassThruReturnsTheCertificateObject()
    {
        var expected = ServerCertificate();
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = ["127.0.0.1"];
        h.Cmdlet.Port = _server.Port;
        h.Cmdlet.PassThru = true;
        h.Run();

        using var certificate = h.Only<X509Certificate2>();
        Assert.Equal(expected.Thumbprint, certificate.Thumbprint);
    }

    [Fact]
    public void OutOfRangePortInTargetWritesError()
    {
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = ["example.com:99999"];
        h.Run();
        Assert.Empty(h.Output);
        h.OnlyError("InvalidPort");
    }

    [Fact]
    public void ConnectionRefusedWritesError()
    {
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = ["127.0.0.1"];
        h.Cmdlet.Port = LoopbackListener.FreePort();
        h.Cmdlet.TimeoutSeconds = 2;
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.ConnectionError, h.OnlyError("TlsHandshakeFailed").CategoryInfo.Category);
    }

    [Fact]
    public void NonTlsListenerWritesError()
    {
        using var listener = new LoopbackListener();
        var h = new CmdletHarness<GetTlsCertificateCmdlet>();
        h.Cmdlet.HostName = [$"127.0.0.1:{listener.Port}"];
        h.Cmdlet.TimeoutSeconds = 2;
        h.Run();
        Assert.Empty(h.Output);
        h.OnlyError("TlsHandshakeFailed");
    }
}
