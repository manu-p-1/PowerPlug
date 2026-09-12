using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Reports the total size of a directory tree.</para>
/// <para type="description">Adds up every file under a directory, like du -sh. Symbolic links are not followed and
/// folders that cannot be read are counted as skipped rather than aborting. Use -Children to get a row per child
/// directory instead.</para>
/// <example>
/// <para>Size of the current directory</para>
/// <code>Get-DirectorySize</code>
/// </example>
/// <example>
/// <para>Largest folders under the home directory</para>
/// <code>Get-DirectorySize ~ -Children | Sort-Object SizeBytes -Descending | Select-Object -First 10</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "DirectorySize")]
[Alias("dirsize")]
[OutputType(typeof(DirectorySizeInfo))]
public sealed class GetDirectorySizeCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Directories to measure. Defaults to the current directory. Wildcards are allowed.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    public string[] Path { get; set; } = ["."];

    /// <summary>
    /// <para type="description">Report each direct child directory separately instead of the total.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Children { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            foreach (var directory in ResolveDirectories(path))
            {
                if (Children)
                {
                    foreach (var child in SafeChildren(directory))
                    {
                        WriteObject(Measure(child));
                    }
                }
                else
                {
                    WriteObject(Measure(directory));
                }
            }
        }
    }

    private IEnumerable<string> ResolveDirectories(string path)
    {
        foreach (var candidate in ResolvePaths(path))
        {
            if (Directory.Exists(candidate))
            {
                yield return candidate;
            }
            else
            {
                WriteError(new DirectoryNotFoundException($"Directory not found: {candidate}"),
                    "DirectoryNotFound", ErrorCategory.ObjectNotFound, candidate);
            }
        }
    }

    private IEnumerable<string> SafeChildren(string directory)
    {
        try
        {
            return Directory.GetDirectories(directory).OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, directory);
            return [];
        }
    }

    private DirectorySizeInfo Measure(string directory)
    {
        var walker = new DirectoryWalker();
        foreach (var _ in walker.EnumerateFiles(directory, onSkipped: p => WriteVerbose($"Skipped unreadable folder: {p}")))
        {
            // Enumeration does the counting.
        }

        return new DirectorySizeInfo
        {
            Path = directory,
            SizeBytes = walker.TotalBytes,
            Size = ByteSize.Format(walker.TotalBytes),
            FileCount = walker.FileCount,
            DirectoryCount = walker.DirectoryCount,
            SkippedCount = walker.SkippedCount,
        };
    }
}
