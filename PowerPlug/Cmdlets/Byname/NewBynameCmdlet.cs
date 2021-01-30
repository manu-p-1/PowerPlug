using System.Management.Automation;
using System.Text;
using PowerPlug.BaseCmdlets;
using PowerPlug.Cmdlets.Byname.Base;
using PowerPlug.Cmdlets.Byname.Operators;
using PowerPlug.Cmdlets.Util;
using PowerPlug.Common.Attributes;
using PowerPlug.Common.Extensions;

namespace PowerPlug.Cmdlets.Byname
{
    /// <summary>
    /// <para type="synopsis">Creates a new Byname</para>
    /// <para type="description">New-Byname is a wrapper cmdlet for the New-Alias cmdlet, however, the fully qualified
    /// command name is written to the user's $PROFILE. An error is thrown if no $PROFILE exists. This cmdlet is to be used for trivial
    /// purposes to quickly persist an alias across sessions. It should not be used outside of the PowerShell Console in order to
    /// prevent unintended behavior.
    /// </para>
    /// <para type="aliases">nbn</para>
    /// <example>
    /// <para>A sample New-Byname command</para>
    /// <code>New-Byname -Name list -Value Get-ChildItem</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/new-alias?view=powershell-7")]
    [Alias("nbn")]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public class NewBynameCmdlet : WritableByname
    {
        /// <summary>
        /// Processes the New-Byname PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand(WritableBynameCreatorBaseOperation.NewAliasCommand)
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
                new NewBynameCreatorOperation(
                    this,
                    CmdletUtilities.InvokePowershellCommandOrThrowIfUnsuccessful(ps, this)
                )
            ).ExecuteStrategy();
        }

        ///<inheritdoc cref="WritableByname.ToString"/>
        public override string ToString() =>
            new StringBuilder()
                .Append("New-Alias")
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