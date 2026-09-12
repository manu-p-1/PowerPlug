using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Diagnostics;

/// <summary>
/// <para type="synopsis">Re-runs a script block on an interval and shows the output, like the Unix watch command.</para>
/// <para type="description">Clears the screen, runs the block, prints the result with a timestamp, waits, and repeats
/// until you press Ctrl+C, -Count iterations have run, or the -Until condition returns true. Output is written to the
/// host so it refreshes in place; add -PassThru to also send each run's objects down the pipeline.</para>
/// <example>
/// <para>Watch a folder fill up</para>
/// <code>Watch-Command { Get-ChildItem ./downloads | Measure-Object Length -Sum } -IntervalSeconds 5</code>
/// </example>
/// <example>
/// <para>Poll until a deployment finishes</para>
/// <code>Watch-Command { kubectl get pods } -Until { ($_ | Out-String) -notmatch 'ContainerCreating' }</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Watch, "Command")]
[Alias("watchcmd")]
[OutputType(typeof(object))]
[ExperimentalCmdlet("It writes directly to the host and clears the screen between runs. Behaviour depends on the host you are using.")]
public sealed class WatchCommandCmdlet : PowerPlugCmdlet, IDisposable
{
    /// <summary>
    /// <para type="description">The script block to run on each tick.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [ValidateNotNull]
    public ScriptBlock ScriptBlock { get; set; } = null!;

    /// <summary>
    /// <para type="description">Seconds between runs. Defaults to 2.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [Alias("Interval", "n")]
    [ValidateRange(0.1, 86_400)]
    public double IntervalSeconds { get; set; } = 2;

    /// <summary>
    /// <para type="description">Stop after this many runs. Runs forever by default.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int? Count { get; set; }

    /// <summary>
    /// <para type="description">Stop once this block returns true. The latest output is available as $_.</para>
    /// </summary>
    [Parameter]
    public ScriptBlock? Until { get; set; }

    /// <summary>
    /// <para type="description">Do not clear the screen between runs.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoClear { get; set; }

    /// <summary>
    /// <para type="description">Also emit each run's output to the pipeline.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var interval = TimeSpan.FromSeconds(IntervalSeconds);
        var command = ScriptBlock.ToString().Trim();
        var run = 0;

        while (!_cancellation.IsCancellationRequested)
        {
            run++;
            var output = RunOnce();

            if (!NoClear && HasInteractiveHost)
            {
                ClearHost();
            }

            var header = string.Create(CultureInfo.CurrentCulture, $"Every {IntervalSeconds:0.#}s: {command}    {DateTime.Now:HH:mm:ss}  (run {run})");
            WriteToHost(header + Environment.NewLine + Environment.NewLine + Format(output));

            if (PassThru)
            {
                foreach (var item in output)
                {
                    WriteObject(item);
                }
            }

            if (Count is not null && run >= Count)
            {
                break;
            }

            if (Until is not null && ConditionMet(output))
            {
                WriteVerbose("Until condition met.");
                break;
            }

            if (_cancellation.Token.WaitHandle.WaitOne(interval))
            {
                break;
            }
        }
    }

    private Collection<PSObject> RunOnce()
    {
        try
        {
            return ScriptBlock.Invoke();
        }
        catch (RuntimeException ex) when (ex is not PipelineStoppedException)
        {
            return [PSObject.AsPSObject($"ERROR: {(ex.InnerException ?? ex).Message}")];
        }
    }

    private bool ConditionMet(Collection<PSObject> output)
    {
        try
        {
            var result = Until!.InvokeWithContext(null, [new PSVariable("_", output)]);
            return result.Count > 0 && LanguagePrimitives.IsTrue(result[^1]);
        }
        catch (RuntimeException ex) when (ex is not PipelineStoppedException)
        {
            WriteWarning($"-Until threw: {(ex.InnerException ?? ex).Message}");
            return false;
        }
    }

    private string Format(Collection<PSObject> output)
    {
        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
        var width = Math.Max(40, HasInteractiveHost ? Host.UI.RawUI.BufferSize.Width : 120);
        ps.AddScript("$input | Out-String -Width $args[0]").AddArgument(width);
        var text = ps.Invoke(output);
        return text.Count > 0 ? text[0].ToString() : string.Empty;
    }

    // Hosts embedded in other processes (tests, services) have no UI. Write-Host is really the information
    // stream underneath, so that path works everywhere and still lands on screen in a console.
    private void WriteToHost(string text)
    {
        if (HasInteractiveHost)
        {
            Host.UI.Write(text);
            return;
        }

        WriteInformation(new HostInformationMessage { Message = text, NoNewLine = true }, ["PSHOST"]);
    }

    private bool HasInteractiveHost => Host?.UI?.RawUI is not null && Host.Name != "Default Host";

    private static void ClearHost()
    {
        try
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Clear-Host").Invoke();
        }
        catch (Exception ex) when (ex is RuntimeException or InvalidOperationException or IOException)
        {
            // Hosts without a console cannot clear. Keep going.
        }
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
