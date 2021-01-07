using System.Management.Automation;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname;
using PowerPlug.Engines.Byname.Base;
using PowerPlug.PowerPlugUtilities.Attributes;
using PowerPlug.PowerPlugUtilities.Cmdlets;

namespace PowerPlug.Cmdlets
{

    /// <summary>
    /// <para type="synopsis">Removes a new Byname</para>
    /// <para type="description">This Byname is a wrapper cmdlet for the Remove-Alias cmdlet. All instances of the The fully qualified command name are
    /// removed from the $PROFILE. An error is thrown if no $PROFILE exists. This cmdlet is to be used for trivial purposes to quickly persist an alias
    /// across sessions. It should not be used outside of the PowerShell Console in order to prevent unintended behavior. Extra precaution should be used
    /// when using Remove-Byname as it removes all aliases with the same name from the $PROFILE.
    /// </para>
    /// <para type="aliases">rbn</para>
    /// <example>
    /// <para>A sample Move-Trash command</para>
    /// <code>Remove-Byname -Name list</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/remove-alias?view=powershell-7")]
    [Alias("rbn")]
    [Beta(BetaAttribute.WarningMessage)]
    public class RemoveBynameCmdlet : BynameBase
    {
        /// <summary>
        /// The Name parameter for the command.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public override string Name { get; set; }

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
        public SwitchParameter Force
        {
            get => _force;

            set => _force = value;
        }

        private bool _force;

        ///<inheritdoc cref="BynameCreatorStrategy"/>
        protected override void ProcessRecord()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var resp = ps
                .AddCommand("Remove-Alias")
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
    }
}
