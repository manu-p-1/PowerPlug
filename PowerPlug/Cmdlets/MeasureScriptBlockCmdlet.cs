using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Benchmarks a script block over multiple iterations</para>
    /// <para type="description">Measure-ScriptBlock runs a script block multiple times and reports timing statistics
    /// including minimum, maximum, average, median, and standard deviation. Unlike Measure-Command, this cmdlet
    /// provides statistical analysis for reliable performance measurement.</para>
    /// <example>
    /// <para>Benchmark a command over 100 iterations</para>
    /// <code>Measure-ScriptBlock { Get-Process | Out-Null } -Iterations 100</code>
    /// </example>
    /// <example>
    /// <para>Quick benchmark with warmup</para>
    /// <code>msb { [math]::Sqrt(12345) } -Iterations 1000 -WarmUp 10</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Measure, "ScriptBlock")]
    [Alias("msb")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class MeasureScriptBlockCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The script block to benchmark</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary>
        /// <para type="description">Number of timed iterations to run (default: 10, range: 1-100000)</para>
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateRange(1, 100000)]
        public int Iterations { get; set; } = 10;

        /// <summary>
        /// <para type="description">Number of warmup iterations before timing begins (default: 0)</para>
        /// </summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int WarmUp { get; set; }

        /// <summary>
        /// Processes the Measure-ScriptBlock PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            for (var i = 0; i < WarmUp; i++)
            {
                try { ScriptBlock.Invoke(); }
                catch (RuntimeException) { /* warmup errors are ignored */ }
            }

            var timings = new List<double>(Iterations);
            var sw = new Stopwatch();

            for (var i = 0; i < Iterations; i++)
            {
                sw.Restart();
                try
                {
                    ScriptBlock.Invoke();
                }
                catch (RuntimeException ex)
                {
                    WriteWarning($"Iteration {i + 1} threw: {(ex.InnerException ?? ex).Message}");
                }
                sw.Stop();
                timings.Add(sw.Elapsed.TotalMilliseconds);
            }

            timings.Sort();
            var count = timings.Count;
            var sum = timings.Sum();
            var avg = sum / count;
            var median = count % 2 == 0
                ? (timings[count / 2 - 1] + timings[count / 2]) / 2.0
                : timings[count / 2];
            var variance = timings.Sum(t => (t - avg) * (t - avg)) / count;
            var stdDev = Math.Sqrt(variance);

            var result = new PSObject();
            result.Properties.Add(new PSNoteProperty("Iterations", count));
            result.Properties.Add(new PSNoteProperty("TotalMs", Math.Round(sum, 4)));
            result.Properties.Add(new PSNoteProperty("AverageMs", Math.Round(avg, 4)));
            result.Properties.Add(new PSNoteProperty("MedianMs", Math.Round(median, 4)));
            result.Properties.Add(new PSNoteProperty("MinMs", Math.Round(timings[0], 4)));
            result.Properties.Add(new PSNoteProperty("MaxMs", Math.Round(timings[count - 1], 4)));
            result.Properties.Add(new PSNoteProperty("StdDevMs", Math.Round(stdDev, 4)));

            WriteObject(result);
        }
    }
}
