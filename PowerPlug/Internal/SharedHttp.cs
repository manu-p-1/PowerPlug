using System.Net.Http.Headers;

namespace PowerPlug.Internal;

/// <summary>
/// A single shared <see cref="HttpClient"/> for the networking cmdlets. Cmdlets are short lived, so
/// creating a client per invocation would exhaust sockets on repeated use.
/// </summary>
internal static class SharedHttp
{
    /// <summary>
    /// The user agent PowerPlug identifies itself with, including the module version.
    /// </summary>
    public static readonly string UserAgent = $"PowerPlug/{ModuleInfo.Version}";

    /// <summary>
    /// The shared client. Callers should pass their own <see cref="CancellationToken"/> for timeouts
    /// rather than relying on the client wide timeout.
    /// </summary>
    public static HttpClient Client { get; } = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        return client;
    }
}
