﻿using System.Management.Automation;
using System.IO;
using System.Security.Cryptography;
using System;
using System.Text;

namespace PowerShellCmdletLibrary
{
    /// <summary>
    /// <para type="synopsis">Moves a file to the Recycle Bin</para>
    /// <para type="description">This function will move a file, whether directory or file, to the system Recycle Bin.
    /// <para type="aliases">trash</para>
    /// If the param list is true, it will print the contents of the current directory after recycling the file. 
    /// Only error dialogsare printed and no confirmation message is shown.
    /// </para>
    /// <example>
    /// <para>A sample Move-Trash command</para>
    /// <code>Move-Trash -Path Documents\file.txt -List</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "Trash")]
    [Alias("trash")]
    public class MoveTrash : PSCmdlet
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
            var curr = this.SessionState.Path.CurrentFileSystemLocation.ToString();
            var parent = System.IO.Path.Combine(Path, "..");
            var fullPath = System.IO.Path.Combine(curr, Path);

            FileAttributes attr = File.GetAttributes(fullPath);
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

    /// <summary>
    /// <para type="synopsis">Compares a file's user specified hash with another signature</para>
    /// <para type="description">This function will compare a user defined hash of a file, such as an executible with the known signature of the file. 
    /// This is especially useful since hashed values are long. The current supported hashes are SHA256, SHA512, MD5.
    /// </para>
    /// <para type="aliases">trash</para>
    /// <example>
    /// <para>A sample Compare-Sha256 command</para>
    /// <code>Compare-Hash .\audacity-win-2.4.2.exe 1f20cd153b2c322bf1ff9941e4e5204098abdc7da37250ce3fb38612b3e927bc</code>
    /// </example>
    /// </summary>
    /// 
    [Cmdlet(VerbsData.Compare, "Hash")]
    [Alias("csh")]
    public class CompareHash : PSCmdlet
    {

        private const string T_SHA256 = "SHA256";
        private const string T_SHA512 = "SHA512";
        private const string T_MD5 = "MD5";

        private string _signature;

        /// <summary>
        /// <para type="description">The hashing algorithm (SHA256, SHA512, MD5) to use when comparing the signature</para>
        /// </summary>
        [Alias("HashType")]
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Choose from: [SHA256, SHA512, MD5] corresponding to the signature")]
        [ValidateSet(T_SHA256, T_SHA512, T_MD5)]
        public string Hash { get; set; }

        /// <summary>
        /// <para type="description">The path to the file</para>
        /// </summary>
        [Alias("FilePath")]
        [Parameter(Position = 1, Mandatory = true)]
        public string Path { get; set; }

        /// <summary>
        /// <para type="description">The the known SHA256 signature of the file</para>
        /// </summary>
        [Alias("KnownHash")]
        [Parameter(Position = 2, Mandatory = true, HelpMessage = "The signature to compare the hashed file against")]
        public string Signature {
            get => _signature;
            set => _signature = value.ToLower();
        }

        /// <summary>
        /// <para type="description">Processes the PSCmdlet</para>
        /// </summary>
        protected override void ProcessRecord()
        {
            var curr = this.SessionState.Path.CurrentFileSystemLocation.ToString();
            var fullPath = System.IO.Path.Combine(curr, Path);

            var hash = Hash switch
            {
                T_SHA256 => ConvertHashAlgorithmToBase64String(SHA256.Create(), fullPath),
                T_SHA512 => ConvertHashAlgorithmToBase64String(SHA512.Create(), fullPath),
                T_MD5 => ConvertHashAlgorithmToBase64String(MD5.Create(), fullPath),
                _ => throw new NotImplementedException()
            };
            var truth = (hash.ToLower() == Signature);

            if (truth)
            {
                Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor, "\n\n~~~~~~~~~~~~~~~~~~~~~~~~~ SIGNATURE MATCH ~~~~~~~~~~~~~~~~~~~~~~~~~\n\n");
            }
            else
            {
                Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, "\n\n~~~~~~~~~~~~~~~~ WARNING: SIGNATURE FAILED TO MATCH ~~~~~~~~~~~~~~~~\n\n");
            }

        }

        /// <summary>
        /// Converts a <see cref="HashAlgorithm"/> to the Path specified by the PSCmdlet
        /// </summary>
        /// <param name="ha">The HashAlgorithm instance</param>
        /// <param name="filePath">The Path property</param>
        /// <returns>a Base64 encoded string</returns>
        private string ConvertHashAlgorithmToBase64String(HashAlgorithm ha, string filePath)
        {
            using (ha)
            {
                using var fileStream = File.OpenRead(filePath);
                var computed = ha.ComputeHash(fileStream);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in computed)
                    sb.Append(b.ToString("X2"));

                return sb.ToString().ToLower();
            }
        }
    }
}
