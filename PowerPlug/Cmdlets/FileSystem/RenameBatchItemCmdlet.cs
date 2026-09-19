using System.Management.Automation;
using System.Text.RegularExpressions;
using PowerPlug.Base;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Renames many files and folders at once using a regular expression.</para>
/// <para type="description">Each item name is matched against -Pattern and rewritten with -Replacement, which
/// supports capture group references such as $1 or ${name}. Nothing is renamed when the name does not change or
/// when the target already exists. -WhatIf previews the result.</para>
/// <example>
/// <para>Preview replacing spaces with dashes</para>
/// <code>Rename-BatchItem *.md -Pattern " " -Replacement "-" -WhatIf</code>
/// </example>
/// <example>
/// <para>Reorder date prefixes from DD-MM-YYYY to YYYY-MM-DD</para>
/// <code>Get-ChildItem *.jpg | Rename-BatchItem -Pattern '^(\d{2})-(\d{2})-(\d{4})' -Replacement '$3-$2-$1'</code>
/// </example>
/// <example>
/// <para>Lower case every file name in a tree</para>
/// <code>Rename-BatchItem ./assets -Recurse -Pattern '.+' -Replacement '$0' -ToLower</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Rename, "BatchItem", SupportsShouldProcess = true)]
[Alias("Rename-Batch")]
[OutputType(typeof(RenameResult))]
public sealed class RenameBatchItemCmdlet : PowerPlugCmdlet
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <para type="description">Items to rename. Wildcards are allowed and items can be piped from Get-ChildItem.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <summary>
    /// <para type="description">Regular expression matched against each item name.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Replacement text. Supports $1, ${name} and $0 for the whole match.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    [AllowEmptyString]
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Treat -Pattern as literal text rather than a regular expression.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Literal { get; set; }

    /// <summary>
    /// <para type="description">Match case insensitively.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IgnoreCase { get; set; }

    /// <summary>
    /// <para type="description">Only replace the first match in each name.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter FirstOnly { get; set; }

    /// <summary>
    /// <para type="description">Leave the file extension alone and match only the base name.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ExcludeExtension { get; set; }

    /// <summary>
    /// <para type="description">Lower case the final name.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ToLower { get; set; }

    /// <summary>
    /// <para type="description">Upper case the final name.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ToUpper { get; set; }

    /// <summary>
    /// <para type="description">When a directory is given, rename its contents as well.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private Regex? _regex;

    /// <inheritdoc />
    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        try
        {
            _regex = BuildRegex();
        }
        catch (ArgumentException ex)
        {
            ThrowTerminatingError(new ErrorRecord(ex, "InvalidPattern", ErrorCategory.InvalidArgument, Pattern));
        }
    }

    /// <summary>
    /// Compiles -Pattern according to -Literal and -IgnoreCase. Exposed for testing.
    /// </summary>
    internal Regex BuildRegex()
    {
        var options = RegexOptions.CultureInvariant | (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return new Regex(Literal ? Regex.Escape(Pattern) : Pattern, options, RegexTimeout);
    }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            foreach (var item in ResolveItems(path))
            {
                RenameOne(item);
            }
        }
    }

    /// <summary>
    /// Computes the new name for an item. Exposed for testing.
    /// </summary>
    internal string ComputeNewName(string name, bool isDirectory)
    {
        _regex ??= BuildRegex();

        var stem = name;
        var extension = string.Empty;
        if (ExcludeExtension && !isDirectory)
        {
            extension = System.IO.Path.GetExtension(name);
            stem = name[..^extension.Length];
        }

        // With -Literal the replacement is plain text too, so a $ must not be read as a group reference.
        var replacement = Literal ? Replacement.Replace("$", "$$", StringComparison.Ordinal) : Replacement;
        var renamed = FirstOnly ? _regex.Replace(stem, replacement, 1) : _regex.Replace(stem, replacement);

        renamed += extension;
        if (ToLower)
        {
            renamed = renamed.ToLowerInvariant();
        }
        else if (ToUpper)
        {
            renamed = renamed.ToUpperInvariant();
        }

        return renamed;
    }

    private IEnumerable<FileSystemInfo> ResolveItems(string path)
    {
        foreach (var full in ResolvePaths(path))
        {
            if (Directory.Exists(full))
            {
                if (Recurse)
                {
                    // Deepest first so children are renamed before their parent folder moves.
                    var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.None };
                    var children = new DirectoryInfo(full).EnumerateFileSystemInfos("*", options)
                        .OrderByDescending(i => i.FullName.Count(c => c == System.IO.Path.DirectorySeparatorChar))
                        .ToList();
                    foreach (var child in children)
                    {
                        yield return child;
                    }
                }

                yield return new DirectoryInfo(full);
            }
            else if (File.Exists(full))
            {
                yield return new FileInfo(full);
            }
            else
            {
                WriteFileError(new FileNotFoundException($"Path not found: {full}"), full);
            }
        }
    }

    private void RenameOne(FileSystemInfo item)
    {
        var isDirectory = item is DirectoryInfo;
        string newName;
        try
        {
            newName = ComputeNewName(item.Name, isDirectory);
        }
        catch (RegexMatchTimeoutException ex)
        {
            WriteError(ex, "PatternTimeout", ErrorCategory.OperationTimeout, item.FullName);
            return;
        }

        if (string.Equals(newName, item.Name, StringComparison.Ordinal))
        {
            WriteVerbose($"Unchanged: {item.FullName}");
            return;
        }

        var result = new RenameResult
        {
            Path = item.FullName,
            OldName = item.Name,
            NewName = newName,
            Renamed = false,
        };

        if (string.IsNullOrWhiteSpace(newName) || newName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            WriteObject(result with { Reason = "New name is empty or contains invalid characters" });
            return;
        }

        var parent = System.IO.Path.GetDirectoryName(item.FullName) ?? string.Empty;
        var target = System.IO.Path.Combine(parent, newName);

        // Case-only renames on case-insensitive file systems look like "target exists". Allow those through.
        var caseOnlyChange = string.Equals(newName, item.Name, StringComparison.OrdinalIgnoreCase);
        if (!caseOnlyChange && System.IO.Path.Exists(target))
        {
            WriteObject(result with { Reason = "Target already exists" });
            return;
        }

        if (!ShouldProcess($"{item.FullName} -> {newName}", "Rename"))
        {
            WriteObject(result with { Reason = "Preview" });
            return;
        }

        try
        {
            if (isDirectory)
            {
                Directory.Move(item.FullName, target);
            }
            else
            {
                File.Move(item.FullName, target);
            }

            WriteObject(result with { Renamed = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, item.FullName, writing: true);
        }
    }
}
