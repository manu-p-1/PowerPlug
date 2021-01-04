using System.Collections.Generic;
using System.Management.Automation;

namespace PowerPlug.PowerPlugUtilities.Cmdlets
{
    internal class CmdletUtilities
    {
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
