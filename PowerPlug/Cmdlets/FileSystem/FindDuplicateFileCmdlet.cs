using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Finds files with identical content.</para>
/// <para type="description">Groups files by size first, then hashes only the files that share a size, so large
/// trees are scanned quickly. Each group lists every path with that content and how many bytes would be freed by
/// keeping a single copy. Empty files are ignored unless -MinimumSize is set to 0.</para>
/// <example>
/// <para>Find duplicates in the Downloads folder</para>
/// <code>Find-DuplicateFile ~/Downloads -Recurse</code>
/// </example>
/// <example>
/// <para>Show only duplicates over 10 MB</para>
/// <code>Find-DuplicateFile . -Recurse -MinimumSize 10MB | Format-List</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Find, "DuplicateFile")]
[Alias("dupes")]
[OutputType(typeof(DuplicateFileGroup))]
public sealed class FindDuplicateFileCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Directories to scan. Defaults to the current directory.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    public string[] Path { get; set; } = ["."];

    /// <summary>
    /// <para type="description">Include sub directories.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Recurse { get; set; }

    /// <summary>
    /// <para type="description">Ignore files smaller than this many bytes. Defaults to 1.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(0, long.MaxValue)]
    public long MinimumSize { get; set; } = 1;

    /// <summary>
    /// <para type="description">Hash algorithm used to confirm duplicates. Defaults to SHA256.</para>
    /// </summary>
    [Parameter]
    [ValidateSet(Hashing.Sha256, Hashing.Sha1, Hashing.Md5, IgnoreCase = true)]
    public string Algorithm { get; set; } = Hashing.Sha256;

    private readonly List<string> _roots = [];

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            var resolved = ResolvePath(path);
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

    /// <inheritdoc />
    protected override void EndProcessing()
    {
        var bySize = new Dictionary<long, List<FileInfo>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var walker = new DirectoryWalker();

        foreach (var root in _roots.Distinct(StringComparer.Ordinal))
        {
            foreach (var file in walker.EnumerateFiles(root, Recurse, p => WriteVerbose($"Skipped unreadable folder: {p}")))
            {
                // Overlapping roots ("." and "./sub" with -Recurse) would otherwise pair a file with itself.
                if (file.Length < MinimumSize || file.LinkTarget is not null || !seen.Add(file.FullName))
                {
                    continue;
                }

                if (!bySize.TryGetValue(file.Length, out var list))
                {
                    bySize[file.Length] = list = [];
                }

                list.Add(file);
            }
        }

        var candidates = bySize.Values.Where(files => files.Count > 1).ToList();
        WriteVerbose($"Scanned {walker.FileCount} files, {candidates.Sum(c => c.Count)} share a size with another file.");

        var groups = new List<DuplicateFileGroup>();
        var processed = 0;
        var total = candidates.Sum(c => c.Count);

        foreach (var files in candidates)
        {
            var byHash = new Dictionary<string, List<FileInfo>>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                processed++;
                WriteProgress(new ProgressRecord(1, "Find-DuplicateFile", $"Hashing {file.Name}")
                {
                    PercentComplete = total == 0 ? 100 : (int)(100L * processed / total),
                });

                string hash;
                try
                {
                    hash = Hashing.ComputeFileHash(file.FullName, Algorithm);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    WriteFileError(ex, file.FullName);
                    continue;
                }

                if (!byHash.TryGetValue(hash, out var list))
                {
                    byHash[hash] = list = [];
                }

                list.Add(file);
            }

            foreach (var (hash, matches) in byHash.Where(kv => kv.Value.Count > 1))
            {
                var size = matches[0].Length;
                groups.Add(new DuplicateFileGroup
                {
                    Hash = hash,
                    SizeBytes = size,
                    Size = ByteSize.Format(size),
                    Count = matches.Count,
                    WastedBytes = size * (matches.Count - 1),
                    Files = matches.Select(f => f.FullName).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray(),
                });
            }
        }

        WriteProgress(new ProgressRecord(1, "Find-DuplicateFile", "Done") { RecordType = ProgressRecordType.Completed });

        foreach (var group in groups.OrderByDescending(g => g.WastedBytes))
        {
            WriteObject(group);
        }
    }
}
