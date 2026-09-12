using System.Diagnostics;
using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Networking;

/// <summary>
/// <para type="synopsis">Waits until a TCP port starts accepting connections.</para>
/// <para type="description">Polls a host and port until a connection succeeds or the timeout expires. Returns a
/// result object, or a plain boolean with -Quiet.</para>
/// <example>
/// <para>Wait up to a minute for a local database</para>
/// <code>Wait-Port localhost 5432 -TimeoutSeconds 60</code>
/// </example>
/// <example>
/// <para>Fail the script if the API never comes up</para>
/// <code>if (-not (Wait-Port api.internal 8080 -Quiet)) { throw "API did not start" }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Wait, "Port")]
[Alias("waitport")]
[OutputType(typeof(PortWaitResult))]
[OutputType(typeof(bool))]
public sealed class WaitPortCmdlet : PowerPlugCmdlet, IDisposable
{
    /// <summary>
    /// <para type="description">Host name or IP address.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [Alias("ComputerName", "Server", "Host")]
    [ValidateNotNullOrEmpty]
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">TCP port, 1-65535.</para>
    /// </summary>
    [Parameter(Position = 1, Mandatory = true)]
    [ValidateRange(1, 65535)]
    public int Port { get; set; }

    /// <summary>
    /// <para type="description">Give up after this many seconds. Defaults to 30.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 86_400)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// <para type="description">Delay between attempts in milliseconds. Defaults to 500.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(50, 60_000)]
    public int IntervalMs { get; set; } = 500;

    /// <summary>
    /// <para type="description">Return only true or false.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Quiet { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var deadline = TimeSpan.FromSeconds(TimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();
        var attempts = 0;
        var open = false;

        while (stopwatch.Elapsed < deadline && !_cancellation.IsCancellationRequested)
        {
            attempts++;
            var remaining = deadline - stopwatch.Elapsed;
            var probeTimeout = remaining < TimeSpan.FromSeconds(2) ? remaining : TimeSpan.FromSeconds(2);

            WriteProgress(new ProgressRecord(1, $"Waiting for {HostName}:{Port}", $"Attempt {attempts}")
            {
                PercentComplete = (int)Math.Min(99, 100 * stopwatch.Elapsed.TotalMilliseconds / deadline.TotalMilliseconds),
                SecondsRemaining = (int)remaining.TotalSeconds,
            });

            if (TcpProbe.Connect(HostName, Port, probeTimeout, _cancellation.Token).Open)
            {
                open = true;
                break;
            }

            if (stopwatch.Elapsed + TimeSpan.FromMilliseconds(IntervalMs) >= deadline)
            {
                break;
            }

            if (_cancellation.Token.WaitHandle.WaitOne(IntervalMs))
            {
                break;
            }
        }

        stopwatch.Stop();
        WriteProgress(new ProgressRecord(1, $"Waiting for {HostName}:{Port}", "Done") { RecordType = ProgressRecordType.Completed });

        if (!open)
        {
            WriteWarning($"{HostName}:{Port} did not open within {TimeoutSeconds} seconds ({attempts} attempts).");
        }

        if (Quiet)
        {
            WriteObject(open);
            return;
        }

        WriteObject(new PortWaitResult
        {
            Host = HostName,
            Port = Port,
            Open = open,
            Attempts = attempts,
            ElapsedMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
        });
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
