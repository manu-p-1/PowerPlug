using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace PowerPlug.Cmdlets.Util
{
    /// <summary>
    /// An internal set of cmdlet utilities to be used to facilitate cmdlet processes.
    /// </summary>
    internal static class CmdletUtilities
    {
        /// <summary>
        /// Invokes a PowerShell command given a PowerShell instance and a PSCmdlet instance. If an error is found while invoking
        /// the command, the PSCmdlet pipeline is stopped by invoking PSCmdlet.ThrowTerminatingError with the first error found.
        /// </summary>
        /// <param name="ps">The PowerShell instance</param>
        /// <param name="psCmdlet">The PSCmdlet instance</param>
        /// <returns>An Enumerable of PSObjects representing the results of invoking the PowerShell instance</returns>
        public static IEnumerable<PSObject> InvokePowershellCommandOrThrowIfUnsuccessful(PowerShell ps, PSCmdlet psCmdlet)
        {
            var res = ps.Invoke();

            if (ps.HadErrors && ps.Streams.Error.Count > 0)
            {
                psCmdlet.ThrowTerminatingError(ps.Streams.Error[0]);
            }

            return res;
        }

        /// <summary>
        /// Resolves a path relative to the current PowerShell working directory.
        /// </summary>
        /// <param name="path">The path to resolve</param>
        /// <param name="cmdlet">The PSCmdlet instance for accessing session state</param>
        /// <returns>The fully qualified path</returns>
        public static string ResolvePath(string path, PSCmdlet cmdlet)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }
            var current = cmdlet.SessionState.Path.CurrentFileSystemLocation.ToString();
            return Path.GetFullPath(Path.Combine(current, path));
        }
    }
}
