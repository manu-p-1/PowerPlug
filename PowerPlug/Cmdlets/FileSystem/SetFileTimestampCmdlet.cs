using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Updates file timestamps, creating the file when it does not exist. A cross platform touch.</para>
/// <para type="description">Sets the last write and last access times to now, to a given date, or to match another
/// file. Missing files are created empty unless -NoCreate is given; the parent directory must already exist.
/// Directories are accepted and have their timestamps updated. Supports -WhatIf.</para>
/// <example>
/// <para>Create or bump a marker file</para>
/// <code>Set-FileTimestamp ./.build-stamp</code>
/// </example>
/// <example>
/// <para>Backdate a file to match another</para>
/// <code>Set-FileTimestamp ./copy.txt -Reference ./original.txt</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Set, "FileTimestamp", SupportsShouldProcess = true, DefaultParameterSetName = DateSet)]
[Alias("touch")]
[OutputType(typeof(FileInfo))]
[OutputType(typeof(DirectoryInfo))]
public sealed class SetFileTimestampCmdlet : PowerPlugCmdlet
{
    private const string DateSet = "Date";
    private const string ReferenceSet = "Reference";

    /// <summary>
    /// <para type="description">Files to touch. Wildcards are allowed and items can be piped from Get-ChildItem.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ValueFromRemainingArguments = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <summary>
    /// <para type="description">The time to set. Defaults to now.</para>
    /// </summary>
    [Parameter(ParameterSetName = DateSet)]
    public DateTime? Date { get; set; }

    /// <summary>
    /// <para type="description">Copy the timestamps from this file or directory instead of using a date.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = ReferenceSet)]
    [ValidateNotNullOrEmpty]
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// <para type="description">Only update the last write time, leave last access alone.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter WriteTimeOnly { get; set; }

    /// <summary>
    /// <para type="description">Do not create files that are missing.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter NoCreate { get; set; }

    /// <summary>
    /// <para type="description">Return the FileInfo for each file touched.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    private DateTime _writeTime;
    private DateTime _accessTime;

    /// <inheritdoc />
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        if (ParameterSetName == ReferenceSet)
        {
            var reference = ResolvePath(Reference);
            if (!System.IO.Path.Exists(reference))
            {
                ThrowTerminatingError(new ErrorRecord(new FileNotFoundException($"Reference not found: {reference}"),
                    "ReferenceNotFound", ErrorCategory.ObjectNotFound, reference));
            }

            // UTC avoids the ambiguous hour when clocks go back.
            _writeTime = File.GetLastWriteTimeUtc(reference);
            _accessTime = File.GetLastAccessTimeUtc(reference);
        }
        else
        {
            _writeTime = _accessTime = (Date ?? DateTime.Now).ToUniversalTime();
        }
    }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            // touch creates the literal name when nothing matches, so only expand real wildcards that do match.
            if (WildcardPattern.ContainsWildcardCharacters(path))
            {
                foreach (var match in ResolvePaths(path))
                {
                    Touch(match);
                }
            }
            else
            {
                Touch(ResolvePath(path));
            }
        }
    }

    private void Touch(string target)
    {
        var isDirectory = Directory.Exists(target);
        var exists = isDirectory || File.Exists(target);

        if (!exists && NoCreate)
        {
            WriteVerbose($"Skipped missing file: {target}");
            return;
        }

        if (!ShouldProcess(target, exists ? "Update timestamp" : "Create empty file and set timestamp"))
        {
            return;
        }

        try
        {
            if (!exists)
            {
                using (File.Create(target))
                {
                }
            }

            if (isDirectory)
            {
                Directory.SetLastWriteTimeUtc(target, _writeTime);
                if (!WriteTimeOnly)
                {
                    Directory.SetLastAccessTimeUtc(target, _accessTime);
                }
            }
            else
            {
                File.SetLastWriteTimeUtc(target, _writeTime);
                if (!WriteTimeOnly)
                {
                    File.SetLastAccessTimeUtc(target, _accessTime);
                }
            }

            if (PassThru)
            {
                WriteObject(isDirectory ? new DirectoryInfo(target) : new FileInfo(target));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteFileError(ex, target, writing: true);
        }
    }
}
