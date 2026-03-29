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
    /// <para type="synopsis">Converts a string or file to Base64 encoding</para>
    /// <para type="description">Encodes a plaintext string or file contents to a Base64 string.
    /// Supports pipeline input and multiple encoding formats.</para>
    /// <example>
    /// <para>Encode a string to Base64</para>
    /// <code>"Hello, World!" | ConvertTo-Base64</code>
    /// </example>
    /// <example>
    /// <para>Encode a file to Base64</para>
    /// <code>ConvertTo-Base64 -Path ./myfile.txt</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Base64", DefaultParameterSetName = "String")]
    [Alias("tobase64")]
    [OutputType(typeof(string))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class ConvertToBase64Cmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The input string to encode</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "String")]
        [ValidateNotNull]
        public string InputString { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The path to a file whose contents should be encoded</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "File")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The character encoding to use (default: UTF8)</para>
        /// </summary>
        [Parameter]
        [ValidateSet("UTF8", "ASCII", "Unicode", "UTF32")]
        public string Encoding { get; set; } = "UTF8";

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                byte[] bytes;

                if (ParameterSetName == "File")
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
                    bytes = File.ReadAllBytes(resolvedPath);
                }
                else
                {
                    var encoding = GetEncoding();
                    bytes = encoding.GetBytes(InputString);
                }

                WriteObject(Convert.ToBase64String(bytes));
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, Path));
            }
            catch (IOException ex)
            {
                WriteError(new ErrorRecord(ex, "IOError", ErrorCategory.ReadError, Path));
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
