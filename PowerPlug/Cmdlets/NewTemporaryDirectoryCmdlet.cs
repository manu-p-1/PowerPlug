using System;
using System.IO;
using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Creates a new uniquely named temporary directory</para>
    /// <para type="description">Creates a new temporary directory with a unique name under the system's temp path
    /// and returns the DirectoryInfo object. Optionally accepts a prefix for the directory name.</para>
    /// <example>
    /// <para>Create a temporary directory</para>
    /// <code>$tmpDir = New-TemporaryDirectory</code>
    /// </example>
    /// <example>
    /// <para>Create a temporary directory with a prefix</para>
    /// <code>$tmpDir = New-TemporaryDirectory -Prefix "build_"</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory")]
    [Alias("ntd")]
    [OutputType(typeof(DirectoryInfo))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class NewTemporaryDirectoryCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">An optional prefix for the temporary directory name</para>
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateLength(0, 50)]
        public string Prefix { get; set; } = string.Empty;

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                var tempBase = Path.GetTempPath();
                var dirName = string.IsNullOrEmpty(Prefix)
                    ? Path.GetRandomFileName()
                    : $"{Prefix}{Path.GetRandomFileName()}";

                var fullPath = Path.Combine(tempBase, dirName);
                var dirInfo = Directory.CreateDirectory(fullPath);

                WriteVerbose($"Created temporary directory: {dirInfo.FullName}");
                WriteObject(dirInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteError(new ErrorRecord(ex, "AccessDenied", ErrorCategory.PermissionDenied, null));
            }
            catch (IOException ex)
            {
                WriteError(new ErrorRecord(ex, "IOError", ErrorCategory.WriteError, null));
            }
        }
    }
}
