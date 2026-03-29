using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Cmdlets.Util;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Moves a file to the Recycle Bin or Trash</para>
    /// <para type="description">This function will move a file or directory to the system Recycle Bin (Windows)
    /// or Trash (macOS). On Linux, the file is permanently deleted with a warning. If the -List param is set,
    /// it will print the contents of the parent directory after recycling the file.
    /// </para>
    /// <para type="aliases">trash</para>
    /// <example>
    /// <para>A sample Move-Trash command</para>
    /// <code>Move-Trash -Path Documents\file.txt -List</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "Trash", SupportsShouldProcess = true)]
    [Alias("trash")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class MoveTrashCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The path to the file or directory</para>
        /// </summary>
        [Alias("FilePath")]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">A flag indicating whether to print the contents of the parent directory</para>
        /// </summary>
        [Alias("ListDirectory")]
        [Parameter(Position = 1, Mandatory = false)]
        public SwitchParameter List { get; set; }

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var fullPath = CmdletUtilities.ResolvePath(Path, this);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                WriteError(new ErrorRecord(
                    new FileNotFoundException($"Path not found: {fullPath}"),
                    "PathNotFound",
                    ErrorCategory.ObjectNotFound,
                    fullPath));
                return;
            }

            if (!ShouldProcess(fullPath, "Move to Trash"))
            {
                return;
            }

            var parentDir = System.IO.Path.GetDirectoryName(fullPath) ?? ".";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    MoveToTrashWindows(fullPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    MoveToTrashMacOS(fullPath);
                }
                else
                {
                    MoveToTrashLinux(fullPath);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, fullPath));
                return;
            }
            catch (IOException ex)
            {
                WriteError(new ErrorRecord(ex, "TrashError", ErrorCategory.WriteError, fullPath));
                return;
            }

            if (List)
            {
                foreach (var ps in InvokeProvider.ChildItem.Get(parentDir, false))
                {
                    WriteObject(ps);
                }
            }
        }

        private void MoveToTrashLinux(string fullPath)
        {
            WriteWarning("Linux Trash support is limited. Attempting permanent deletion.");

            var attr = File.GetAttributes(fullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                Directory.Delete(fullPath, recursive: true);
            }
            else
            {
                File.Delete(fullPath);
            }
        }

        private void MoveToTrashMacOS(string fullPath)
        {
            var trashDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".Trash");

            if (!Directory.Exists(trashDir))
            {
                Directory.CreateDirectory(trashDir);
            }

            var fileName = System.IO.Path.GetFileName(fullPath);
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var ext = System.IO.Path.GetExtension(fileName);
            var destPath = System.IO.Path.Combine(trashDir, fileName);

            // Handle name collisions in trash with bounded retry
            var counter = 1;
            while ((File.Exists(destPath) || Directory.Exists(destPath)) && counter <= 10000)
            {
                destPath = System.IO.Path.Combine(trashDir, $"{nameWithoutExt} ({counter}){ext}");
                counter++;
            }

            var attr = File.GetAttributes(fullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                Directory.Move(fullPath, destPath);
            }
            else
            {
                File.Move(fullPath, destPath);
            }

            WriteVerbose($"Moved to Trash: {destPath}");
        }

        private static void MoveToTrashWindows(string fullPath)
        {
            var attr = File.GetAttributes(fullPath);
            var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;

            if (isDir)
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    fullPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );
            }
            else
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    fullPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );
            }
        }
    }
}
