using System;
using System.Management.Automation;
using System.Reflection;
using PowerPlug.Attributes;

namespace PowerPlug.Base
{
    /// <summary>
    /// Base class for all PowerPlug cmdlets. Provides common functionality including
    /// beta warning emission for cmdlets decorated with <see cref="BetaCmdlet"/>.
    /// </summary>
    public abstract class PowerPlugCmdletBase : PSCmdlet
    {
        /// <summary>
        /// Emits a warning if the cmdlet is decorated with <see cref="BetaCmdlet"/>.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            var betaAttr = GetType().GetCustomAttribute<BetaCmdlet>();
            if (betaAttr != null)
            {
                var msg = string.IsNullOrEmpty(betaAttr.Msg)
                    ? BetaCmdlet.WarningMessage
                    : betaAttr.Msg;
                WriteWarning(msg);
            }
        }
    }
}
