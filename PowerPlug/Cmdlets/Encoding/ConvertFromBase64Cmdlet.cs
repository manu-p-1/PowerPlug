using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using PowerPlug.Attributes;
using PowerPlug.Base;
using PowerPlug.Cmdlets.Util;

namespace PowerPlug.Cmdlets.Encoding
{
    /// <summary>
    /// <para type="synopsis">Converts a Base64 encoded string back to plaintext or file</para>
    /// <para type="description">Decodes a Base64 encoded string back to its original plaintext or writes the decoded bytes to a file.
    /// Supports pipeline input and multiple encoding formats.</para>
    /// <example>
    /// <para>Decode a Base64 string</para>
    /// <code>"SGVsbG8sIFdvcmxkIQ==" | ConvertFrom-Base64</code>
    /// </example>
    /// <example>
    /// <para>Decode Base64 to a file</para>
    /// <code>ConvertFrom-Base64 -InputString $base64Data -OutputPath ./decoded.bin</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Base64", DefaultParameterSetName = "String")]
    [Alias("frombase64")]
    [OutputType(typeof(string))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class ConvertFromBase64Cmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The Base64 encoded string to decode</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string InputString { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">Optional output file path to write decoded bytes to</para>
        /// </summary>
        [Parameter(ParameterSetName = "File")]
        [ValidateNotNullOrEmpty]
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The character encoding to use for string output (default: UTF8)</para>
        /// </summary>
        [Parameter(ParameterSetName = "String")]
        [ValidateSet("UTF8", "ASCII", "Unicode", "UTF32")]
        public string Encoding { get; set; } = "UTF8";

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(InputString);
            }
            catch (FormatException ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidBase64", ErrorCategory.InvalidData, InputString));
                return;
            }

            if (!string.IsNullOrEmpty(OutputPath))
            {
                try
                {
                    var resolvedPath = CmdletUtilities.ResolvePath(OutputPath, this);
                    File.WriteAllBytes(resolvedPath, decoded);
                    WriteVerbose($"Decoded content written to: {resolvedPath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    WriteError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, OutputPath));
                }
                catch (IOException ex)
                {
                    WriteError(new ErrorRecord(ex, "IOError", ErrorCategory.WriteError, OutputPath));
                }
            }
            else
            {
                var encoding = GetEncoding();
                WriteObject(encoding.GetString(decoded));
            }
        }

        private System.Text.Encoding GetEncoding() => Encoding switch
        {
            "ASCII" => System.Text.Encoding.ASCII,
            "Unicode" => System.Text.Encoding.Unicode,
            "UTF32" => System.Text.Encoding.UTF32,
            _ => System.Text.Encoding.UTF8,
        };
    }
}
