using System.Diagnostics;
using System.Management.Automation;
using System.Net.Http.Headers;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Checks an HTTP endpoint and reports status, latency and key headers.</para>
/// <para type="description">Sends a request (HEAD by default) and reports the status code, time to first byte,
/// redirect target, content type and length. Redirects are not followed. DNS failures and timeouts are returned in
/// the Error property rather than thrown.</para>
/// <example>
/// <para>Check a site</para>
/// <code>Test-Url https://example.com</code>
/// </example>
/// <example>
/// <para>Check several endpoints with a GET and a 5 second timeout</para>
/// <code>"https://a.example.com/health", "https://b.example.com/health" | Test-Url -Method GET -TimeoutSeconds 5</code>
/// </example>
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "Url")]
[Alias("turl")]
[OutputType(typeof(UrlTestResult))]
public sealed class TestUrlCmdlet : PowerPlugCmdlet, IDisposable
{
    /// <summary>
    /// <para type="description">One or more absolute URLs. A missing scheme defaults to https.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [Alias("Uri")]
    [ValidateNotNullOrEmpty]
    public string[] Url { get; set; } = [];

    /// <summary>
    /// <para type="description">HTTP method. Defaults to HEAD.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("HEAD", "GET", "OPTIONS")]
    public string Method { get; set; } = "HEAD";

    /// <summary>
    /// <para type="description">Request timeout in seconds. Defaults to 10.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 600)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// <para type="description">Extra request headers, for example @{ Authorization = "Bearer ..." }.</para>
    /// </summary>
    [Parameter]
    public System.Collections.IDictionary? Headers { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var url in Url)
        {
            WriteObject(Check(url));
        }
    }

    private UrlTestResult Check(string raw)
    {
        var text = raw.Contains("://", StringComparison.Ordinal) ? raw : "https://" + raw;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new UrlTestResult { Url = raw, Success = false, Error = "Not a valid http or https URL" };
        }

        using var request = new HttpRequestMessage(new HttpMethod(Method), uri);
        if (Headers is not null)
        {
            foreach (System.Collections.DictionaryEntry header in Headers)
            {
                var name = header.Key.ToString()!;
                if (!request.Headers.TryAddWithoutValidation(name, header.Value?.ToString()))
                {
                    WriteWarning($"Header '{name}' is a content header and was not sent; this cmdlet sends no body.");
                }
            }
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = SharedHttp.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult();
            stopwatch.Stop();

            var status = (int)response.StatusCode;
            return new UrlTestResult
            {
                Url = uri.ToString(),
                Success = status is >= 200 and < 400,
                StatusCode = status,
                Status = response.ReasonPhrase,
                LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                RedirectTo = response.Headers.Location?.ToString(),
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength,
                Server = response.Headers.Server.Count > 0 ? string.Join(" ", response.Headers.Server.Select(FormatServer)) : null,
            };
        }
        catch (OperationCanceledException) when (!_cancellation.IsCancellationRequested)
        {
            return new UrlTestResult { Url = uri.ToString(), Success = false, LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2), Error = $"Timed out after {TimeoutSeconds} seconds" };
        }
        catch (HttpRequestException ex)
        {
            return new UrlTestResult { Url = uri.ToString(), Success = false, LatencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2), Error = ex.InnerException?.Message ?? ex.Message };
        }
    }

    private static string FormatServer(ProductInfoHeaderValue value) => value.Product?.ToString() ?? value.Comment ?? string.Empty;

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
