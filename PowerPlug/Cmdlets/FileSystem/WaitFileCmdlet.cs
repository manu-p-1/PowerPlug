using System.Diagnostics;
using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Waits for a file to appear, change or disappear.</para>
/// <para type="description">Polls the path until the condition holds or the timeout passes. Returns the FileInfo
/// when the file exists at the end, a boolean otherwise, or always a boolean with -Quiet.</para>
/// <example>
/// <para>Wait for a build artefact</para>
/// <code>Wait-File ./out/app.zip -TimeoutSeconds 300</code>
/// </example>
/// <example>
/// <para>Wait for a log file to be updated</para>
/// <code>Wait-File ./app.log -Changed -TimeoutSeconds 60 -Quiet</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Wait, "File", DefaultParameterSetName = ExistsSet)]
[Alias("waitfile")]
[OutputType(typeof(FileInfo))]
[OutputType(typeof(bool))]
public sealed class WaitFileCmdlet : PowerPlugCmdlet, IDisposable
{
    private const string ExistsSet = "Exists";
    private const string ChangedSet = "Changed";
    private const string DeletedSet = "Deleted";

    /// <summary>
    /// <para type="description">The file to watch.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Wait until the file's size or last write time changes from what it is now.</para>
    /// </summary>
    [Parameter(ParameterSetName = ChangedSet)]
    public SwitchParameter Changed { get; set; }

    /// <summary>
    /// <para type="description">Wait until the file no longer exists.</para>
    /// </summary>
    [Parameter(ParameterSetName = DeletedSet)]
    public SwitchParameter Deleted { get; set; }

    /// <summary>
    /// <para type="description">Give up after this many seconds. Defaults to 30.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 86_400)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// <para type="description">Delay between checks in milliseconds. Defaults to 250.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(10, 60_000)]
    public int IntervalMs { get; set; } = 250;

    /// <summary>
    /// <para type="description">Return only true or false.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Quiet { get; set; }

    private readonly CancellationTokenSource _cancellation = new();

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var file = ResolvePath(Path);
        var deadline = TimeSpan.FromSeconds(TimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();
        var baseline = Snapshot(file);

        if (ParameterSetName == ChangedSet && baseline is null)
        {
            WriteWarning($"{file} does not exist yet; waiting for it to be created counts as a change.");
        }

        var satisfied = false;
        while (!_cancellation.IsCancellationRequested)
        {
            satisfied = Check(file, baseline);
            if (satisfied || stopwatch.Elapsed >= deadline)
            {
                break;
            }

            WriteProgress(new ProgressRecord(1, $"Waiting for {file}", ParameterSetName)
            {
                PercentComplete = (int)Math.Min(99, 100 * stopwatch.Elapsed.TotalMilliseconds / deadline.TotalMilliseconds),
                SecondsRemaining = (int)(deadline - stopwatch.Elapsed).TotalSeconds,
            });

            var remaining = deadline - stopwatch.Elapsed;
            var wait = remaining < TimeSpan.FromMilliseconds(IntervalMs) ? remaining : TimeSpan.FromMilliseconds(IntervalMs);
            if (_cancellation.Token.WaitHandle.WaitOne(wait))
            {
                break;
            }
        }

        WriteProgress(new ProgressRecord(1, $"Waiting for {file}", "Done") { RecordType = ProgressRecordType.Completed });

        if (_cancellation.IsCancellationRequested)
        {
            return;
        }

        if (!satisfied)
        {
            WriteWarning($"Timed out after {TimeoutSeconds} seconds waiting for {file} ({ParameterSetName}).");
        }

        if (!Quiet && satisfied && File.Exists(file))
        {
            WriteObject(new FileInfo(file));
        }
        else
        {
            WriteObject(satisfied);
        }
    }

    private bool Check(string file, (long, DateTime)? baseline)
    {
        var current = Snapshot(file);
        return ParameterSetName switch
        {
            ChangedSet => current != baseline,
            DeletedSet => current is null,
            _ => current is not null,
        };
    }

    private static (long Length, DateTime Written)? Snapshot(string file)
    {
        try
        {
            var info = new FileInfo(file);
            return info.Exists ? (info.Length, info.LastWriteTimeUtc) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override void StopProcessing() => _cancellation.Cancel();

    /// <inheritdoc />
    public void Dispose() => _cancellation.Dispose();
}
