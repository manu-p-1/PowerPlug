using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Lists the largest files under a directory.</para>
/// <para type="description">Walks the tree and returns the biggest files first. Symbolic links are not followed and
/// unreadable folders are skipped. The output has a FullName property, so it binds to Move-Trash, Compare-Hash and
/// Remove-Item on the pipeline.</para>
/// <example>
/// <para>Ten largest files under the current directory</para>
/// <code>Get-LargestFile -Recurse</code>
/// </example>
/// <example>
/// <para>Anything over 100 MB in Downloads</para>
/// <code>Get-LargestFile ~/Downloads -Recurse -MinimumSize 100MB -Top 50</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "LargestFile")]
[Alias("bigfiles")]
[OutputType(typeof(FileSizeInfo))]
public sealed class GetLargestFileCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Directories to scan. Defaults to the current directory.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    public string[] Path { get; set; } = ["."];

    /// <summary>
    /// <para type="description">How many files to return. Defaults to 10.</para>
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateRange(1, 100_000)]
    public int Top { get; set; } = 10;

    /// <summary>
    /// <para type="description">Include sub directories.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Recurse { get; set; }

    /// <summary>
    /// <para type="description">Ignore files smaller than this many bytes.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, long.MaxValue)]
    public long MinimumSize { get; set; }

    private readonly List<string> _roots = [];

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            foreach (var resolved in ResolvePaths(path))
            {
                if (Directory.Exists(resolved))
                {
                    _roots.Add(resolved);
                }
                else
                {
                    WriteError(new DirectoryNotFoundException($"Directory not found: {resolved}"),
                        "DirectoryNotFound", ErrorCategory.ObjectNotFound, resolved);
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void EndProcessing()
    {
        // Keep only the current top N while walking so large trees do not need every FileInfo in memory.
        var top = new PriorityQueue<FileInfo, long>();
        var walker = new DirectoryWalker();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in _roots.Distinct(StringComparer.Ordinal))
        {
            foreach (var file in walker.EnumerateFiles(root, Recurse, p => WriteVerbose($"Skipped unreadable folder: {p}")))
            {
                if (file.LinkTarget is not null || file.Length < MinimumSize || !seen.Add(file.FullName))
                {
                    continue;
                }

                top.Enqueue(file, file.Length);
                if (top.Count > Top)
                {
                    top.Dequeue();
                }
            }
        }

        var results = new List<FileInfo>(top.Count);
        while (top.Count > 0)
        {
            results.Add(top.Dequeue());
        }

        for (var i = results.Count - 1; i >= 0; i--)
        {
            var file = results[i];
            WriteObject(new FileSizeInfo
            {
                FullName = file.FullName,
                Name = file.Name,
                SizeBytes = file.Length,
                Size = ByteSize.Format(file.Length),
                LastWriteTime = file.LastWriteTime,
            });
        }
    }
}
