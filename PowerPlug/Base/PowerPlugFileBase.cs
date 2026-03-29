using System;
using System.IO;

namespace PowerPlug.Base
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
            var directory = FileInfo.DirectoryName ?? Path.GetDirectoryName(Path.GetFullPath(path));
            FileParentDir = new DirectoryInfo(directory ?? throw new ArgumentException("Unable to determine parent directory", nameof(path)));
        }

        /// <summary>
        /// Sets initial variables given a FileInfo
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance in order to create a PowerPlug file</param>
        protected PowerPlugFileBase(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            var directory = FileInfo.DirectoryName ?? Path.GetDirectoryName(fileInfo.FullName);
            FileParentDir = new DirectoryInfo(directory ?? throw new ArgumentException("Unable to determine parent directory", nameof(fileInfo)));
        }
    }
}
