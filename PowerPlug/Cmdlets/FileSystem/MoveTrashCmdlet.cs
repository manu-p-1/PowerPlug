using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.FileSystem;

/// <summary>
/// <para type="synopsis">Moves files or folders to the Recycle Bin or Trash instead of deleting them.</para>
/// <para type="description">On Windows items go to the Recycle Bin, on macOS to ~/.Trash, and on Linux to the
/// FreeDesktop trash (~/.local/share/Trash) with a .trashinfo record so desktop environments can restore them.
/// Wildcards are supported and items can be piped in from Get-ChildItem.</para>
/// <example>
/// <para>Trash a file</para>
/// <code>Move-Trash ./old-notes.txt</code>
/// </example>
/// <example>
/// <para>Trash every log file and show what happened</para>
/// <code>Get-ChildItem *.log | Move-Trash -PassThru</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Move, "Trash", SupportsShouldProcess = true)]
[Alias("trash")]
[OutputType(typeof(string))]
[ExperimentalCmdlet("It relies on platform specific trash behaviour. On Linux it follows the FreeDesktop spec, which some environments may not honor.")]
public sealed class MoveTrashCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">One or more paths. Wildcards are allowed.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName", "FilePath")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [];

    /// <summary>
    /// <para type="description">Return the original path of each item that was trashed.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            foreach (var resolved in ResolvePaths(path))
            {
                TrashItem(resolved);
            }
        }
    }

    private void TrashItem(string fullPath)
    {
        if (!System.IO.Path.Exists(fullPath))
        {
            WriteFileError(new FileNotFoundException($"Path not found: {fullPath}"), fullPath);
            return;
        }

        if (!ShouldProcess(fullPath, "Move to Trash"))
        {
            return;
        }

        try
        {
            var destination = Trash.MoveToTrash(fullPath);
            WriteVerbose($"Moved '{fullPath}' to {destination}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            WriteFileError(ex, fullPath, writing: true);
            return;
        }

        if (PassThru)
        {
            WriteObject(fullPath);
        }
    }
}
