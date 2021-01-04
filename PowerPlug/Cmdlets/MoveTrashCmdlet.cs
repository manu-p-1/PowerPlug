using System.IO;
using System.Management.Automation;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Moves a file to the Recycle Bin</para>
    /// <para type="description">This function will move a file, whether directory or file, to the system Recycle Bin.
    /// <para type="aliases">trash</para>
    /// If the param list is true, it will print the contents of the current directory after recycling the file. 
    /// Only error dialogs are printed and no confirmation message is shown.
    /// </para>
    /// <example>
    /// <para>A sample Move-Trash command</para>
    /// <code>Move-Trash -Path Documents\file.txt -List</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "Trash")]
    [Alias("trash")]
    public class MoveTrashCmdlet : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The path to the file</para>
        /// </summary>
        [Alias("FilePath")]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Path { get; set; }

        /// <summary>
        /// <para type="description">A flag indicating whether to print the contents of the current directory</para>
        /// </summary>
        [Alias("ListDirectory")]
        [Parameter(Position = 1, Mandatory = false)]
        public SwitchParameter List { get; set; }

        /// <summary>
        /// Processes the PSCmdlet
        /// </summary>
        protected override void ProcessRecord()
        {
            var current = SessionState.Path.CurrentFileSystemLocation.ToString();
            var parent = System.IO.Path.Combine(Path, "..");
            var fullPath = System.IO.Path.Combine(current, Path);

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

            if (List)
            {
                foreach (var ps in InvokeProvider.ChildItem.Get(parent, false))
                {
                    WriteObject(ps);
                }
            }
        }
    }
}
