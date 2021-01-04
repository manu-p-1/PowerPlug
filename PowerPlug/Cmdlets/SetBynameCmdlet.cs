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
    /// <para type="synopsis">Set's an alias within the profile instead of the session</para>
    /// <para type="description">Set's an alias using Set-Byname, but writes to the $PROFILE instead of the session
    /// </para>
    /// <para type="aliases">sbn</para>
    /// <example>
    /// <para>A sample Set-Alias command</para>
    /// <code>Set-Byname -Name list -Value Get-ChildItem</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/Microsoft.PowerShell.Utility/Set-Alias?view=powershell-7")]
    [Alias("sbn")]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public class SetBynameCmdlet : WritableByname
    {
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