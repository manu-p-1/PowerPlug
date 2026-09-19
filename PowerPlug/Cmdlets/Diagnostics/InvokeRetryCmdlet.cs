using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Diagnostics;

/// <summary>
/// <para type="synopsis">Runs a script block again when it fails.</para>
/// <para type="description">Executes the script block and retries on a terminating error, waiting between attempts.
/// Delay can grow exponentially and be capped; -Jitter adds a random spread so many clients do not retry in lock step.
/// The output of the first successful attempt is returned. When every attempt fails the last error is rethrown.
/// Non-terminating errors inside the block do not trigger a retry; use -ErrorAction Stop inside the block if they should.</para>
/// <example>
/// <para>Retry a flaky call five times</para>
/// <code>Invoke-Retry { Invoke-RestMethod https://api.example.com/data } -MaxAttempts 5</code>
/// </example>
/// <example>
/// <para>Exponential backoff starting at 500 ms, capped at 10 s</para>
/// <code>Invoke-Retry { Test-Connection db01 -Count 1 -ErrorAction Stop } -DelayMilliseconds 500 -ExponentialBackoff -MaxDelayMilliseconds 10000</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Invoke, "Retry")]
[Alias("retry")]
[OutputType(typeof(object))]
public sealed class InvokeRetryCmdlet : PowerPlugCmdlet, IDisposable
{
    /// <summary>
    /// <para type="description">The script block to run.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [ValidateNotNull]
    public ScriptBlock ScriptBlock { get; set; } = null!;

    /// <summary>
    /// <para type="description">Maximum attempts including the first. Defaults to 3.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateRange(1, 1000)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// <para type="description">Delay before the second attempt in milliseconds. Defaults to 1000.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 3_600_000)]
    public int DelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// <para type="description">Double the delay after each failure.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ExponentialBackoff { get; set; }

    /// <summary>
    /// <para type="description">Upper bound for the delay when backing off. Defaults to 5 minutes.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 3_600_000)]
    public int MaxDelayMilliseconds { get; set; } = 300_000;

    /// <summary>
    /// <para type="description">Randomise each delay by up to 25 percent.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Jitter { get; set; }

    /// <summary>
    /// <para type="description">Arguments passed to the script block as $args.</para>
    /// </summary>
    [Parameter]
    public object[]? ArgumentList { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        Exception? last = null;
        var delay = DelayMilliseconds;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var output = ScriptBlock.Invoke(ArgumentList ?? []);
                foreach (var item in output)
                {
                    WriteObject(item);
                }

                if (attempt > 1)
                {
                    WriteVerbose($"Succeeded on attempt {attempt}.");
                }

                return;
            }
            catch (RuntimeException ex) when (ex is not PipelineStoppedException)
            {
                last = ex.InnerException ?? ex;
                WriteWarning($"Attempt {attempt} of {MaxAttempts} failed: {last.Message}");
            }

            if (attempt == MaxAttempts)
            {
                break;
            }

            var wait = NextDelay(delay);
            WriteVerbose($"Waiting {wait} ms before attempt {attempt + 1}.");
            if (_cancellation.Token.WaitHandle.WaitOne(wait))
            {
                return;
            }

            if (ExponentialBackoff)
            {
                delay = (int)Math.Min((long)delay * 2, MaxDelayMilliseconds);
            }
        }

        ThrowTerminatingError(new ErrorRecord(last!, "RetryExhausted", ErrorCategory.OperationTimeout, ScriptBlock));
    }

    /// <summary>
    /// Applies jitter to a delay. Exposed for testing.
    /// </summary>
    internal int NextDelay(int delay)
    {
        if (!Jitter || delay == 0)
        {
            return Math.Min(delay, MaxDelayMilliseconds);
        }

        var spread = delay / 4;
        var jittered = delay + Random.Shared.Next(-spread, spread + 1);
        return Math.Clamp(jittered, 0, MaxDelayMilliseconds);
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
