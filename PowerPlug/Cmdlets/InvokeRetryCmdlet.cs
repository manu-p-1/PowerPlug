using System;
using System.Management.Automation;
using System.Threading;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Retries a script block with configurable attempts and backoff</para>
    /// <para type="description">Invoke-Retry executes a script block and retries on failure with configurable
    /// maximum attempts, delay, and optional exponential backoff. Useful for flaky network calls, file locks,
    /// or transient infrastructure failures.</para>
    /// <example>
    /// <para>Retry a web request up to 5 times</para>
    /// <code>Invoke-Retry -ScriptBlock { Invoke-RestMethod https://api.example.com/data } -MaxAttempts 5</code>
    /// </example>
    /// <example>
    /// <para>Retry with exponential backoff starting at 500ms</para>
    /// <code>Invoke-Retry { Test-Connection server01 -Count 1 } -MaxAttempts 3 -DelayMilliseconds 500 -ExponentialBackoff</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Retry")]
    [Alias("retry")]
    [OutputType(typeof(object))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class InvokeRetryCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The script block to execute and retry on failure</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary>
        /// <para type="description">Maximum number of attempts (default: 3, range: 1-100)</para>
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateRange(1, 100)]
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// <para type="description">Delay in milliseconds between retries (default: 1000, range: 0-300000)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(0, 300000)]
        public int DelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// <para type="description">If set, doubles the delay after each failed attempt</para>
        /// </summary>
        [Parameter]
        public SwitchParameter ExponentialBackoff { get; set; }

        /// <summary>
        /// Processes the Invoke-Retry PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            Exception? lastException = null;
            var currentDelay = DelayMilliseconds;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var results = ScriptBlock.Invoke();
                    foreach (var result in results)
                    {
                        WriteObject(result);
                    }
                    return;
                }
                catch (RuntimeException ex)
                {
                    lastException = ex.InnerException ?? ex;
                    WriteWarning($"Attempt {attempt}/{MaxAttempts} failed: {lastException.Message}");

                    if (attempt < MaxAttempts)
                    {
                        WriteVerbose($"Waiting {currentDelay}ms before retry...");
                        Thread.Sleep(currentDelay);

                        if (ExponentialBackoff)
                        {
                            currentDelay = Math.Min(currentDelay * 2, 300000);
                        }
                    }
                }
            }

            ThrowTerminatingError(new ErrorRecord(
                lastException!,
                "RetryExhausted",
                ErrorCategory.OperationTimeout,
                ScriptBlock));
        }
    }
}
