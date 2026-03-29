using System.Management.Automation;
using System.Text;
using PowerPlug.Attributes;
using PowerPlug.BaseCmdlets;
using PowerPlug.Cmdlets.Byname.Base;
using PowerPlug.Cmdlets.Byname.Operators;
using PowerPlug.Cmdlets.Util;
using Ampere.Str;

namespace PowerPlug.Cmdlets.Byname
{

    /// <summary>
    /// <para type="synopsis">Removes a Byname</para>
    /// <para type="description">Remove-Byname is a wrapper cmdlet for the Remove-Alias cmdlet. All instances of the fully qualified command name are
    /// removed from the $PROFILE. An error is thrown if no $PROFILE exists. This cmdlet is to be used for trivial purposes to quickly persist an alias
    /// across sessions. It should not be used outside of the PowerShell Console in order to prevent unintended behavior. Extra precaution should be used
    /// when using Remove-Byname as it removes all aliases with the same name from the $PROFILE.
    /// </para>
    /// <para type="aliases">rbn</para>
    /// <example>
    /// <para>A sample Remove-Byname command</para>
    /// <code>Remove-Byname -Name list</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Byname", SupportsShouldProcess = true,
        HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/remove-alias?view=powershell-7")]
    [Alias("rbn")]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class RemoveBynameCmdlet : BynameBase
    {
        /// <summary>
        /// The Name parameter for the command.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public override string Name { get; set; } = string.Empty;

        /// <summary>
        /// The scope parameter for the command determines which scope the alias is set in.
        /// </summary>
        [Parameter]
        [ValidateSet("Global", "Local", "Script")]
        public string Scope { get; set; } = "Local";

        /// <summary>
        /// If set to true and an existing alias of the same name exists
        /// and is ReadOnly, the alias will be overwritten.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Processes the Remove-Byname PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Alias '{Name}'", "Remove-Byname"))
            {
                return;
            }

            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Remove-Alias")
                .AddParameter("Name", Name)
                .AddParameter("Scope", Scope)
                .AddParameter("Force", Force);

            new BynameCreatorContext(
                new RemoveBynameCreatorOperation(
                    this,
                    CmdletUtilities.InvokePowershellCommandOrThrowIfUnsuccessful(ps, this)
                )
            ).ExecuteStrategy();
        }

        /// <summary>
        /// The fully qualified Remove-Byname command as it's executed in the command-line. Because Remove-Byname
        /// is a wrapper for Remove-Alias, the ToString version uses Remove-Alias as the cmdlet name. 
        /// </summary>
        /// <returns>A string representing the entire command with all options included in the string</returns>
        public override string ToString() =>
            new StringBuilder()
                .Append("Remove-Alias")
                .Append(" -Name ").Append(Name)
                .Append(" -Scope ").Append(Scope)
                .AppendIf(" -Force", Force)
                .ToString();
    }
}
