using System.Management.Automation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PowerPlug.Base;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Retrieves the TLS certificate presented by a remote host.</para>
/// <para type="description">Performs a TLS handshake and returns the leaf certificate details: subject, issuer,
/// validity window, days until expiry, subject alternative names and whether the chain is trusted by this machine.
/// Untrusted and expired certificates are still returned, with ChainTrusted and IsValid set accordingly. -PassThru
/// returns the X509Certificate2 instead.</para>
/// <example>
/// <para>Check when a certificate expires</para>
/// <code>Get-TlsCertificate example.com | Select-Object Subject, NotAfter, DaysRemaining</code>
/// </example>
/// <example>
/// <para>Alert on certificates expiring within 30 days</para>
/// <code>"a.example.com", "b.example.com" | Get-TlsCertificate | Where-Object DaysRemaining -lt 30</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "TlsCertificate")]
[Alias("gtls", "Get-SslCertificate")]
[OutputType(typeof(TlsCertificateInfo))]
[OutputType(typeof(X509Certificate2))]
public sealed class GetTlsCertificateCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Host name to connect to. A URL is accepted and its host and port are used.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [Alias("ComputerName", "Host", "Url")]
    [ValidateNotNullOrEmpty]
    public string[] HostName { get; set; } = [];

    /// <summary>
    /// <para type="description">TCP port. Defaults to 443.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateRange(1, 65535)]
    public int Port { get; set; } = 443;

    /// <summary>
    /// <para type="description">Connection timeout in seconds. Defaults to 10.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 300)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// <para type="description">Return the X509Certificate2 object instead of the summary.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var target in HostName)
        {
            var (host, port) = ParseTarget(target);
            if (port is < 1 or > 65535)
            {
                WriteError(new ArgumentOutOfRangeException(nameof(Port), $"Port {port} in '{target}' is out of range."),
                    "InvalidPort", ErrorCategory.InvalidArgument, target);
                continue;
            }

            try
            {
                var (certificate, protocol, trusted) = Fetch(host, port);
                if (PassThru)
                {
                    WriteObject(certificate);
                    continue;
                }

                using (certificate)
                {
                    WriteObject(Summarize(host, port, certificate, protocol, trusted));
                }
            }
            catch (Exception ex) when (ex is SocketException or IOException or AuthenticationException or OperationCanceledException)
            {
                WriteError(ex, "TlsHandshakeFailed", ErrorCategory.ConnectionError, $"{host}:{port}");
            }
        }
    }

    private (string Host, int Port) ParseTarget(string target)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
        {
            return (uri.Host, uri.IsDefaultPort ? Port : uri.Port);
        }

        var colon = target.LastIndexOf(':');
        if (colon > 0 && int.TryParse(target.AsSpan(colon + 1), out var parsedPort) && !target.Contains("::", StringComparison.Ordinal))
        {
            return (target[..colon], parsedPort);
        }

        return (target, Port);
    }

    private (X509Certificate2 Certificate, SslProtocols Protocol, bool Trusted) Fetch(string host, int port)
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        client.ConnectAsync(host, port, cts.Token).AsTask().GetAwaiter().GetResult();

        var trusted = true;
#pragma warning disable CA5359 // The point of this cmdlet is to inspect certificates, including bad ones. No data is exchanged after the handshake.
        using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false, (_, _, _, errors) =>
        {
            trusted = errors == SslPolicyErrors.None;
            return true;
        });
#pragma warning restore CA5359

        ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, cts.Token).GetAwaiter().GetResult();

        var remote = ssl.RemoteCertificate ?? throw new AuthenticationException("The server did not present a certificate.");
        return (new X509Certificate2(remote), ssl.SslProtocol, trusted);
    }

    private static TlsCertificateInfo Summarize(string host, int port, X509Certificate2 certificate, SslProtocols protocol, bool trusted)
    {
        var now = DateTime.Now;
        return new TlsCertificateInfo
        {
            Host = host,
            Port = port,
            Subject = certificate.Subject,
            Issuer = certificate.Issuer,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            DaysRemaining = (int)Math.Floor((certificate.NotAfter - now).TotalDays),
            IsValid = now >= certificate.NotBefore && now <= certificate.NotAfter,
            ChainTrusted = trusted,
            SubjectAlternativeNames = GetSubjectAlternativeNames(certificate),
            Protocol = protocol.ToString(),
            Thumbprint = Convert.ToHexString(certificate.GetCertHash(HashAlgorithmName.SHA256)),
            SignatureAlgorithm = certificate.SignatureAlgorithm.FriendlyName ?? certificate.SignatureAlgorithm.Value ?? "Unknown",
            SerialNumber = certificate.SerialNumber,
        };
    }

    private static string[] GetSubjectAlternativeNames(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault();
        if (extension is null)
        {
            return [];
        }

        return extension.EnumerateDnsNames()
            .Concat(extension.EnumerateIPAddresses().Select(ip => ip.ToString()))
            .ToArray();
    }
}
