using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Compares a file's user specified hash with another signature</para>
    /// <para type="description">This function will compare a user defined hash of a file, such as an executable with the known signature of the file. 
    /// This is especially useful since hashed values are long. The current supported hashes are SHA256, SHA512, MD5.
    /// </para>
    /// <para type="aliases">trash</para>
    /// <example>
    /// <para>A sample Compare-Sha256 command</para>
    /// <code>Compare-Hash .\audacity-win-2.4.2.exe 1f20cd153b2c322bf1ff9941e4e5204098abdc7da37250ce3fb38612b3e927bc</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "Hash")]
    [Alias("csh")]
    public class CompareHash : PSCmdlet
    {

        private const string Sha256Option = "SHA256";
        private const string Sha512Option = "SHA512";
        private const string Md5Option = "MD5";

        private string _signature;

        /// <summary>
        /// <para type="description">The hashing algorithm (SHA256, SHA512, MD5) to use when comparing the signature</para>
        /// </summary>
        [Alias("HashType")]
        [Parameter(Position = 0, Mandatory = true,
            HelpMessage = "Choose from: [SHA256, SHA512, MD5] corresponding to the signature")]
        [ValidateSet(Sha256Option, Sha512Option, Md5Option)]
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
        public string Signature
        {
            get => _signature;
            set => _signature = value.ToLower();
        }

        /// <summary>
        /// <para type="description">Processes the PSCmdlet</para>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        protected override void ProcessRecord()
        {
            var current = SessionState.Path.CurrentFileSystemLocation.ToString();
            var fullPath = System.IO.Path.Combine(current, Path);

            var hash = Hash switch
            {
                Sha256Option => ConvertHashAlgorithmToX2FormattedString(SHA256.Create(), fullPath),
                Sha512Option => ConvertHashAlgorithmToX2FormattedString(SHA512.Create(), fullPath),
                Md5Option => ConvertHashAlgorithmToX2FormattedString(MD5.Create(), fullPath),
                _ => throw new NotImplementedException()
            };
            var truth = (hash.ToLower() == Signature);

            if (truth)
            {
                Host.UI.WriteLine(ConsoleColor.Green, Host.UI.RawUI.BackgroundColor,
                    "\n\n~~~~~~~~~~~~~~~~~~~~~~~~~ SIGNATURE MATCH ~~~~~~~~~~~~~~~~~~~~~~~~~\n\n");
            }
            else
            {
                Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor,
                    "\n\n~~~~~~~~~~~~~~~~ WARNING: SIGNATURE FAILED TO MATCH ~~~~~~~~~~~~~~~~\n\n");
            }

        }

        /// <summary>
        /// Converts a <see cref="HashAlgorithm"/> to the Path specified by the PSCmdlet
        /// </summary>
        /// <param name="ha">The HashAlgorithm instance</param>
        /// <param name="filePath">The Path property</param>
        /// <returns>a Base64 encoded string</returns>
        private static string ConvertHashAlgorithmToX2FormattedString(HashAlgorithm ha, string filePath)
        {
            using (ha)
            {
                using var fileStream = File.OpenRead(filePath);
                var computed = ha.ComputeHash(fileStream);

                var sb = new StringBuilder();
                foreach (var c in computed)
                    sb.Append(c.ToString("X2"));

                return sb.ToString().ToLower();
            }
        }
    }
}
