using System.Management.Automation;
using System.Text;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname;
using PowerPlug.Engines.Byname.Base;
using PowerPlug.PowerPlugUtilities.Attributes;
using PowerPlug.PowerPlugUtilities.Cmdlets;
using PowerPlug.PowerPlugUtilities.Extensions;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Sets a new Byname</para>
    /// <para type="description">This Byname is a wrapper cmdlet for the Set-Alias cmdlet, however, the fully qualified
    /// command name is written to the user's $PROFILE. An error is thrown if no $PROFILE exists. This cmdlet is to be used for trivial
    /// purposes to quickly persist an alias across sessions. It should not be used outside of the PowerShell Console in order to
    /// prevent unintended behavior.
    /// </para>
    /// <para type="aliases">sbn</para>
    /// <example>
    /// <para>A sample Set-Byname command</para>
    /// <code>Set-Byname -Name gh -Value Get-Help</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/Microsoft.PowerShell.Utility/Set-Alias?view=powershell-7")]
    [Alias("sbn")]
    [Beta(BetaAttribute.WarningMessage)]
    public class SetBynameCmdlet : WritableByname
    {
        /// <summary>
        /// Processes the Set-Byname PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand(WritableBynameCreatorBaseOperation.SetAliasCommand)
                .AddParameter("Name", Name)
                .AddParameter("Value", Value)
                .AddParameter("Description", Description)
                .AddParameter("Option", Option)
                .AddParameter("PassThru", PassThru)
                .AddParameter("Scope", Scope)
                .AddParameter("Force", Force)
                .AddParameter("WhatIf", WhatIf)
                .AddParameter("Confirm", Confirm);

            new BynameCreatorContext(
                new SetBynameCreatorOperation(
                    this,
                    CmdletUtilities.InvokePowershellCommandOrThrowIfUnsuccessful(ps, this)
                )
            ).ExecuteStrategy();
        }

        ///<inheritdoc cref="WritableByname.ToString"/>
        public override string ToString() =>
            new StringBuilder()
                .Append("Set-Alias")
                .Append($" -Name {Name}")
                .Append($" -Value {Value}")
                .Append($" -Option {Option}")
                .Append($" -Scope {Scope}")
                .AppendIf(" -PassThru", PassThru)
                .AppendIf(" -Force", Force)
                .AppendIf(" -WhatIf", WhatIf)
                .AppendIf(" -Confirm", Confirm)
                .AppendIf($" -Description {Description}", Description != string.Empty)
                .ToString();
    }
}