using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Removes empty directories from a tree.</para>
/// <para type="description">Walks the tree bottom up and deletes every directory that contains nothing, including
/// directories that only become empty once their empty children are removed. The root itself is kept unless
/// -IncludeRoot is specified. Supports -WhatIf.</para>
/// <example>
/// <para>Preview the clean up</para>
/// <code>Remove-EmptyDirectory ./build -WhatIf</code>
/// </example>
/// <example>
/// <para>Clean up and list what was removed</para>
/// <code>Remove-EmptyDirectory ./build -PassThru</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "EmptyDirectory", SupportsShouldProcess = true)]
[Alias("rmempty")]
[OutputType(typeof(string))]
public sealed class RemoveEmptyDirectoryCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Root directories to clean. Defaults to the current directory.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    public string[] Path { get; set; } = ["."];

    /// <summary>
    /// <para type="description">Also remove the root directory when it ends up empty.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeRoot { get; set; }

    /// <summary>
    /// <para type="description">Return the path of each removed directory.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            var root = ResolvePath(path);
            if (!Directory.Exists(root))
            {
                WriteError(new DirectoryNotFoundException($"Directory not found: {root}"),
                    "DirectoryNotFound", ErrorCategory.ObjectNotFound, root);
                continue;
            }

            var removed = 0;
            foreach (var directory in DirectoryWalker.DirectoriesDeepestFirst(root))
            {
                if (directory == root && !IncludeRoot)
                {
                    continue;
                }

                if (!IsEmpty(directory))
                {
                    continue;
                }

                if (!ShouldProcess(directory, "Remove empty directory"))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(directory);
                    removed++;
                    if (PassThru)
                    {
                        WriteObject(directory);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    WriteFileError(ex, directory, writing: true);
                }
            }

            WriteVerbose($"Removed {removed} empty director{(removed == 1 ? "y" : "ies")} under {root}");
        }
    }

    private static bool IsEmpty(string directory)
    {
        try
        {
            return !Directory.EnumerateFileSystemEntries(directory).Any();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
