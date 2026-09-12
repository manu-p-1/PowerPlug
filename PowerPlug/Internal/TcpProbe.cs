using System.Diagnostics;
using System.Net.Sockets;

namespace PowerPlug.Internal;

/// <summary>
/// A single TCP connect attempt with a timeout.
/// </summary>
internal static class TcpProbe
{
    /// <summary>
    /// Result of a probe.
    /// </summary>
    public readonly record struct Result(bool Open, double LatencyMs, string? Error);

    /// <summary>
    /// Attempts to connect and reports how long it took. Never throws; failures come back in <see cref="Result.Error"/>.
    /// </summary>
    public static Result Connect(string host, int port, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            client.ConnectAsync(host, port, cts.Token).AsTask().GetAwaiter().GetResult();
            stopwatch.Stop();
            return new Result(true, stopwatch.Elapsed.TotalMilliseconds, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new Result(false, stopwatch.Elapsed.TotalMilliseconds, $"Connection timed out after {timeout.TotalMilliseconds:0} ms");
        }
        catch (OperationCanceledException)
        {
            return new Result(false, stopwatch.Elapsed.TotalMilliseconds, "Cancelled");
        }
        catch (SocketException ex)
        {
            return new Result(false, stopwatch.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
