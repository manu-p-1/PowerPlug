using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Cmdlets.Util;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Compares a file's user specified hash with another signature</para>
    /// <para type="description">This function will compare a user defined hash of a file, such as an executable with the known signature of the file. 
    /// This is especially useful since hashed values are long. The current supported hashes are SHA256, SHA512, SHA384, MD5.
    /// </para>
    /// <example>
    /// <para>A sample Compare-Hash command</para>
    /// <code>Compare-Hash -Hash SHA256 -Path .\audacity-win-2.4.2.exe -Signature 1f20cd153b2c322bf1ff9941e4e5204098abdc7da37250ce3fb38612b3e927bc</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "Hash")]
    [Alias("csh")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class CompareHashCmdlet : PowerPlugCmdletBase
    {
        private const string Sha256Option = "SHA256";
        private const string Sha512Option = "SHA512";
        private const string Sha384Option = "SHA384";
        private const string Md5Option = "MD5";

        /// <summary>
        /// <para type="description">The hashing algorithm (SHA256, SHA512, SHA384, MD5) to use when comparing the signature</para>
        /// </summary>
        [Alias("HashType")]
        [Parameter(Position = 0, Mandatory = true,
            HelpMessage = "Choose from: [SHA256, SHA512, SHA384, MD5] corresponding to the signature")]
        [ValidateSet(Sha256Option, Sha512Option, Sha384Option, Md5Option)]
        public string Hash { get; set; } = Sha256Option;

        /// <summary>
        /// <para type="description">The path to the file</para>
        /// </summary>
        [Alias("FilePath")]
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The known signature of the file to compare against</para>
        /// </summary>
        [Alias("KnownHash")]
        [Parameter(Position = 2, Mandatory = true, HelpMessage = "The signature to compare the hashed file against")]
        [ValidateNotNullOrEmpty]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var resolvedPath = CmdletUtilities.ResolvePath(Path, this);

            if (!File.Exists(resolvedPath))
            {
                WriteError(new ErrorRecord(
                    new FileNotFoundException($"File not found: {resolvedPath}"),
                    "FileNotFound",
                    ErrorCategory.ObjectNotFound,
                    resolvedPath));
                return;
            }

            string computedHash;
            try
            {
                computedHash = ComputeFileHash(resolvedPath, Hash);
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, resolvedPath));
                return;
            }
            catch (IOException ex)
            {
                WriteError(new ErrorRecord(ex, "IOError", ErrorCategory.ReadError, resolvedPath));
                return;
            }

            var normalizedSignature = Signature.Trim().Replace("-", "", StringComparison.Ordinal);
            var match = string.Equals(computedHash, normalizedSignature, StringComparison.OrdinalIgnoreCase);

            var pso = new PSObject();
            pso.Members.Add(new PSNoteProperty("Algorithm", Hash));
            pso.Members.Add(new PSNoteProperty("FilePath", resolvedPath));
            pso.Members.Add(new PSNoteProperty("ComputedHash", computedHash));
            pso.Members.Add(new PSNoteProperty("Match", match));
            pso.Members.Add(new PSNoteProperty("Result", match ? "SIGNATURE MATCH SUCCESS" : "SIGNATURE MATCH FAILURE"));
            WriteObject(pso);
        }

        private static string ComputeFileHash(string filePath, string algorithm)
        {
            using var stream = File.OpenRead(filePath);
            using var hashAlgorithm = algorithm switch
            {
                Sha256Option => (HashAlgorithm)SHA256.Create(),
                Sha512Option => SHA512.Create(),
                Sha384Option => SHA384.Create(),
#pragma warning disable CA5351 // MD5 is intentionally offered for legacy hash verification
                Md5Option => MD5.Create(),
#pragma warning restore CA5351
                _ => throw new ArgumentException($"Unsupported hash algorithm: {algorithm}", nameof(algorithm))
            };

            var hashBytes = hashAlgorithm.ComputeHash(stream);
            return Convert.ToHexString(hashBytes);
        }
    }
}
