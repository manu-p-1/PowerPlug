using System.Diagnostics;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Returns the public IP address of this machine.</para>
/// <para type="description">Asks an external service which address your traffic appears to come from. The default
/// provider is Cloudflare (https://1.1.1.1/cdn-cgi/trace); ipify and icanhazip are also built in, or supply any URL
/// that returns the address as plain text. Use -IPv6 to prefer an IPv6 answer where the provider supports it.</para>
/// <example>
/// <para>Show the public IP</para>
/// <code>Get-PublicIPAddress</code>
/// </example>
/// <example>
/// <para>Just the address, for scripts</para>
/// <code>(Get-PublicIPAddress -Provider ipify).IPAddress</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "PublicIPAddress")]
[Alias("pubip", "Get-PublicIP")]
[OutputType(typeof(PublicIPResult))]
public sealed class GetPublicIPAddressCmdlet : PowerPlugCmdlet
{
    private static readonly IReadOnlyDictionary<string, (string V4, string V6)> Providers =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Cloudflare"] = ("https://1.1.1.1/cdn-cgi/trace", "https://[2606:4700:4700::1111]/cdn-cgi/trace"),
            ["ipify"] = ("https://api.ipify.org", "https://api6.ipify.org"),
            ["icanhazip"] = ("https://ipv4.icanhazip.com", "https://ipv6.icanhazip.com"),
        };

    /// <summary>
    /// <para type="description">Built in provider name or a custom URL returning the address as text. Defaults to Cloudflare.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Provider { get; set; } = "Cloudflare";

    /// <summary>
    /// <para type="description">Prefer the provider's IPv6 endpoint.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IPv6 { get; set; }

    /// <summary>
    /// <para type="description">Request timeout in seconds. Defaults to 10.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        string url;
        string providerName;
        if (Providers.TryGetValue(Provider, out var endpoints))
        {
            url = IPv6 ? endpoints.V6 : endpoints.V4;
            providerName = Provider;
        }
        else if (Uri.TryCreate(Provider, UriKind.Absolute, out var custom) && (custom.Scheme == Uri.UriSchemeHttps || custom.Scheme == Uri.UriSchemeHttp))
        {
            url = custom.ToString();
            providerName = custom.Host;
        }
        else
        {
            WriteError(new ArgumentException($"'{Provider}' is not a known provider or a valid URL. Known providers: {string.Join(", ", Providers.Keys)}."),
                "InvalidProvider", ErrorCategory.InvalidArgument, Provider);
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        var stopwatch = Stopwatch.StartNew();
        string body;
        try
        {
            using var response = SharedHttp.Client.GetAsync(url, cts.Token).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            body = response.Content.ReadAsStringAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            WriteError(ex, "LookupFailed", ErrorCategory.ConnectionError, url);
            return;
        }

        stopwatch.Stop();

        var address = ExtractAddress(body);
        if (address is null)
        {
            WriteError(new FormatException($"The response from {url} did not contain an IP address."),
                "UnexpectedResponse", ErrorCategory.InvalidData, body);
            return;
        }

        WriteObject(new PublicIPResult
        {
            IPAddress = address.ToString(),
            AddressFamily = address.AddressFamily == AddressFamily.InterNetworkV6 ? "IPv6" : "IPv4",
            Provider = providerName,
            LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
        });
    }

    /// <summary>
    /// Pulls an IP address out of a plain text body or a Cloudflare style "key=value" trace. Exposed for testing.
    /// </summary>
    internal static IPAddress? ExtractAddress(string body)
    {
        foreach (var rawLine in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine;
            var equals = line.IndexOf('=', StringComparison.Ordinal);
            if (equals >= 0)
            {
                if (!line.StartsWith("ip=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                line = line[(equals + 1)..];
            }

            if (IPAddress.TryParse(line, out var address))
            {
                return address;
            }
        }

        return null;
    }
}
