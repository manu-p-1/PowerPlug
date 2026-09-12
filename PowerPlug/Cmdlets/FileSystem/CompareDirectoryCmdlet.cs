using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Compares two directory trees and reports what differs.</para>
/// <para type="description">Lists files that exist only on one side and files that exist on both but differ. By default
/// files are compared by size. -CompareBy Hash also compares content for files whose sizes match, and -CompareBy
/// Timestamp compares last write times with a two second tolerance. Symbolic links are not followed. Pass
/// -IncludeSame to also list matching files.</para>
/// <example>
/// <para>What changed between a backup and the live folder</para>
/// <code>Compare-Directory ./backup ./live</code>
/// </example>
/// <example>
/// <para>Byte for byte verification of a copy</para>
/// <code>Compare-Directory D:\photos E:\photos -CompareBy Hash | Where-Object Status -ne Same</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Compare, "Directory")]
[Alias("dirdiff")]
[OutputType(typeof(DirectoryComparison))]
public sealed class CompareDirectoryCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The directory used as the baseline.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true)]
    [Alias("Left", "Source")]
    [ValidateNotNullOrEmpty]
    public string ReferencePath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">The directory compared against the baseline.</para>
    /// </summary>
    [Parameter(Position = 1, Mandatory = true)]
    [Alias("Right", "Destination")]
    [ValidateNotNullOrEmpty]
    public string DifferencePath { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">How files present on both sides are compared: Size (default), Hash or Timestamp.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("Size", "Hash", "Timestamp")]
    public string CompareBy { get; set; } = "Size";

    /// <summary>
    /// <para type="description">Also report files that are the same on both sides.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeSame { get; set; }

    /// <summary>
    /// <para type="description">Compare sub directories as well. Off by default, matching Compare-Object.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Recurse { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var reference = ResolvePath(ReferencePath);
        var difference = ResolvePath(DifferencePath);

        foreach (var (path, name) in new[] { (reference, nameof(ReferencePath)), (difference, nameof(DifferencePath)) })
        {
            if (!Directory.Exists(path))
            {
                ThrowTerminatingError(new ErrorRecord(new DirectoryNotFoundException($"{name} not found: {path}"),
                    "DirectoryNotFound", ErrorCategory.ObjectNotFound, path));
            }
        }

        var left = Index(reference);
        var right = Index(difference);
        var all = left.Keys.Union(right.Keys, PathComparer.Default).OrderBy(k => k, PathComparer.Default);

        foreach (var relative in all)
        {
            var hasLeft = left.TryGetValue(relative, out var l);
            var hasRight = right.TryGetValue(relative, out var r);

            if (hasLeft && !hasRight)
            {
                WriteObject(Row(relative, "ReferenceOnly", l, null, null));
            }
            else if (!hasLeft && hasRight)
            {
                WriteObject(Row(relative, "DifferenceOnly", null, r, null));
            }
            else
            {
                var reason = Differs(l!, r!);
                if (reason is not null)
                {
                    WriteObject(Row(relative, "Modified", l, r, reason));
                }
                else if (IncludeSame)
                {
                    WriteObject(Row(relative, "Same", l, r, null));
                }
            }
        }
    }

    private Dictionary<string, FileInfo> Index(string root)
    {
        var result = new Dictionary<string, FileInfo>(PathComparer.Default);
        var walker = new DirectoryWalker();
        foreach (var file in walker.EnumerateFiles(root, Recurse, p => WriteVerbose($"Skipped unreadable folder: {p}")))
        {
            if (file.LinkTarget is null)
            {
                result[System.IO.Path.GetRelativePath(root, file.FullName)] = file;
            }
        }

        return result;
    }

    private string? Differs(FileInfo left, FileInfo right)
    {
        if (left.Length != right.Length)
        {
            return "Size";
        }

        switch (CompareBy)
        {
            case "Timestamp":
                // FAT and exFAT store write times to two seconds.
                var delta = (left.LastWriteTimeUtc - right.LastWriteTimeUtc).Duration();
                return delta > TimeSpan.FromSeconds(2) ? "Timestamp" : null;
            case "Hash":
                try
                {
                    return FileContent.AreEqual(left.FullName, right.FullName) ? null : "Hash";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    WriteFileError(ex, ex is FileNotFoundException fnf && fnf.FileName is not null ? fnf.FileName : left.FullName);
                    return "Unreadable";
                }
            default:
                return null;
        }
    }

    private static DirectoryComparison Row(string relative, string status, FileInfo? left, FileInfo? right, string? reason) => new()
    {
        RelativePath = relative,
        Status = status,
        ReferenceSize = left?.Length,
        DifferenceSize = right?.Length,
        ReferenceModified = left?.LastWriteTime,
        DifferenceModified = right?.LastWriteTime,
        Reason = reason,
    };
}
