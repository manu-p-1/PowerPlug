using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Creates a uniquely named temporary directory.</para>
/// <para type="description">Creates a new directory with a random name under the system temp folder and returns its
/// DirectoryInfo. An optional prefix makes the folder easy to spot when cleaning up.</para>
/// <example>
/// <para>Create a scratch folder</para>
/// <code>$tmp = New-TemporaryDirectory</code>
/// </example>
/// <example>
/// <para>Create a scratch folder for a build</para>
/// <code>$tmp = New-TemporaryDirectory -Prefix "build-"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "TemporaryDirectory")]
[Alias("ntd")]
[OutputType(typeof(DirectoryInfo))]
public sealed class NewTemporaryDirectoryCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Text placed before the random part of the name.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateLength(0, 64)]
    public string? Prefix { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        try
        {
            var directory = Directory.CreateTempSubdirectory(Prefix);
            WriteVerbose($"Created {directory.FullName}");
            WriteObject(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            WriteFileError(ex, Path.GetTempPath(), writing: true);
        }
    }
}
