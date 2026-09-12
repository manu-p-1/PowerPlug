using System.Diagnostics;
using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Diagnostics;

/// <summary>
/// <para type="synopsis">Benchmarks a script block over many iterations.</para>
/// <para type="description">Runs the block repeatedly, optionally after a warm up, and reports minimum, maximum,
/// mean, median and standard deviation. Measure-Command times a single run and gives no spread. Output from the
/// block is discarded.</para>
/// <example>
/// <para>Compare two ways of building a string</para>
/// <code>Measure-ScriptBlock { -join (1..1000) } -Iterations 200
/// Measure-ScriptBlock { [string]::Join('', 1..1000) } -Iterations 200</code>
/// </example>
/// <example>
/// <para>Warm up first</para>
/// <code>msb { Get-Process | Out-Null } -Iterations 20 -WarmUp 3</code>
/// </example>
/// </summary>
[Cmdlet(VerbsDiagnostic.Measure, "ScriptBlock")]
[Alias("msb")]
[OutputType(typeof(BenchmarkResult))]
public sealed class MeasureScriptBlockCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The script block to time.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [ValidateNotNull]
    public ScriptBlock ScriptBlock { get; set; } = null!;

    /// <summary>
    /// <para type="description">Timed iterations. Defaults to 10.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateRange(1, 1_000_000)]
    public int Iterations { get; set; } = 10;

    /// <summary>
    /// <para type="description">Untimed iterations run first. Defaults to 0.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, 10_000)]
    public int WarmUp { get; set; }

    /// <summary>
    /// <para type="description">Arguments passed to the script block as $args.</para>
    /// </summary>
    [Parameter]
    public object[]? ArgumentList { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var args = ArgumentList ?? [];

        for (var i = 0; i < WarmUp; i++)
        {
            try
            {
                ScriptBlock.Invoke(args);
            }
            catch (RuntimeException ex) when (ex is not PipelineStoppedException)
            {
                // Warm up failures are not interesting.
            }
        }

        var timings = new List<double>(Iterations);
        var failures = 0;
        var reportEvery = Math.Max(1, Iterations / 100);

        for (var i = 0; i < Iterations; i++)
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                ScriptBlock.Invoke(args);
            }
            catch (RuntimeException ex) when (ex is not PipelineStoppedException)
            {
                failures++;
                if (failures <= 3)
                {
                    WriteWarning($"Iteration {i + 1} threw: {(ex.InnerException ?? ex).Message}");
                }
            }

            timings.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            if (Iterations >= 50 && i % reportEvery == 0)
            {
                WriteProgress(new ProgressRecord(1, "Measure-ScriptBlock", $"Iteration {i + 1} of {Iterations}")
                {
                    PercentComplete = (int)(100L * (i + 1) / Iterations),
                });
            }
        }

        if (Iterations >= 50)
        {
            WriteProgress(new ProgressRecord(1, "Measure-ScriptBlock", "Done") { RecordType = ProgressRecordType.Completed });
        }

        if (failures > 3)
        {
            WriteWarning($"{failures} of {Iterations} iterations threw. Timings include the failed runs.");
        }

        WriteObject(Summarize(timings, failures));
    }

    /// <summary>
    /// Builds the result from raw timings. Exposed for testing.
    /// </summary>
    internal static BenchmarkResult Summarize(List<double> timings, int failures)
    {
        var sorted = timings.OrderBy(t => t).ToArray();
        var total = sorted.Sum();
        var mean = total / sorted.Length;

        return new BenchmarkResult
        {
            Iterations = sorted.Length,
            Failures = failures,
            TotalMs = Math.Round(total, 4),
            AverageMs = Math.Round(mean, 4),
            MedianMs = Math.Round(Statistics.MedianOfSorted(sorted), 4),
            MinMs = Math.Round(sorted[0], 4),
            MaxMs = Math.Round(sorted[^1], 4),
            StdDevMs = Math.Round(Statistics.StandardDeviation(sorted, mean), 4),
        };
    }
}
