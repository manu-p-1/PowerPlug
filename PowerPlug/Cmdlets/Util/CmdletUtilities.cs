using System.Collections.Generic;
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

            if (ps.HadErrors)
            {
                psCmdlet.ThrowTerminatingError(ps.Streams.Error[0]);
            }

            return res;
        }
    }
}
