using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Lists all entries in the system PATH environment variable</para>
    /// <para type="description">Parses the PATH environment variable and outputs each directory entry as a structured object
    /// with the path, index, and whether the directory actually exists on disk. Useful for diagnosing PATH issues.</para>
    /// <example>
    /// <para>List all PATH entries</para>
    /// <code>Get-EnvironmentPath</code>
    /// </example>
    /// <example>
    /// <para>Find invalid PATH entries</para>
    /// <code>Get-EnvironmentPath | Where-Object { -not $_.Exists }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EnvironmentPath")]
    [Alias("gpath")]
    [OutputType(typeof(PSObject))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class GetEnvironmentPathCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The target of the environment variable: Process, User, or Machine</para>
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateSet("Process", "User", "Machine")]
        public string Target { get; set; } = "Process";

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var target = Target switch
            {
                "User" => EnvironmentVariableTarget.User,
                "Machine" => EnvironmentVariableTarget.Machine,
                _ => EnvironmentVariableTarget.Process
            };

            // User/Machine scope is only supported on Windows
            if (target != EnvironmentVariableTarget.Process && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WriteWarning($"'{Target}' scope environment variables are only supported on Windows. Falling back to 'Process' scope.");
                target = EnvironmentVariableTarget.Process;
            }

            var pathVar = Environment.GetEnvironmentVariable("PATH", target);
            if (string.IsNullOrEmpty(pathVar))
            {
                WriteWarning("PATH environment variable is empty or not set.");
                return;
            }

            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            var paths = pathVar.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < paths.Length; i++)
            {
                var entry = paths[i].Trim();
                var pso = new PSObject();
                pso.Members.Add(new PSNoteProperty("Index", i));
                pso.Members.Add(new PSNoteProperty("Path", entry));
                pso.Members.Add(new PSNoteProperty("Exists", Directory.Exists(entry)));
                pso.Members.Add(new PSNoteProperty("Target", Target));
                WriteObject(pso);
            }
        }
    }
}
