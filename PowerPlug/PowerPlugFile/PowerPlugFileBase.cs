using System.IO;

namespace PowerPlug.PowerPlugFile
{
    /// <summary>
    /// The base class for any PowerPlug file abstraction. Representations of PowerShell entities such as
    /// a user's profile should inherit this class for abstraction.
    /// </summary>
    public abstract class PowerPlugFileBase
    {
        /// <summary>
        /// The FileInfo of the path provided
        /// </summary>
        public FileInfo FileInfo { get; }

        /// <summary>
        /// The DirectoryInfo of the parent folder of the path provided
        /// </summary>
        public DirectoryInfo FileParentDir { get; }

        /// <summary>
        /// Sets initial variables given a pathname
        /// </summary>
        /// <param name="path">The pathname in order to create a PowerPlug file</param>
        protected PowerPlugFileBase(string path)
        {
            FileInfo = new FileInfo(path);
            FileParentDir = new DirectoryInfo(FileInfo.DirectoryName ?? path);
        }

        /// <summary>
        /// Sets initial variables given a FileInfo
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance in order to create a PowerPlug file</param>
        protected PowerPlugFileBase(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            FileParentDir = new DirectoryInfo(FileInfo.DirectoryName ?? fileInfo.FullName);
        }
    }
}
