using System.Management.Automation;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname;
using PowerPlug.Engines.Byname.Base;
using PowerPlug.PowerPlugUtilities.Attributes;
using PowerPlug.PowerPlugUtilities.Cmdlets;

namespace PowerPlug.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/remove-alias?view=powershell-7")]
    [Alias("rbn")]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public class RemoveBynameCmdlet : PSCmdlet, IByname
    {
        /// <summary>
        /// The Name parameter for the command.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

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
