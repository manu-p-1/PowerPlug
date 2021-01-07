using System.Management.Automation;

namespace PowerPlug.BaseCmdlets
{
    /// <summary>
    /// Represents a Byname that can be written or modified to the user's $PROFILE.
    /// </summary>
    public abstract class WritableByname : BynameBase
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
        [ValidateSet("Global", "Local", "Private", "Numbered scopes", "Script")]

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

        /// <summary>
        /// The Value parameter for the command.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Value { get; set; }

        /// <summary>
        /// The description for the alias.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The Option parameter allows the alias to be set to
        /// ReadOnly (for existing aliases) and/or Constant (only
        /// for new aliases).
        /// </summary>
        [Parameter]
        public ScopedItemOptions Option { get; set; } = ScopedItemOptions.None;

        /// <summary>
        /// If set to true, the alias that is set is passed to the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get => _passThru;

            set => _passThru = value;
        }

        private bool _passThru;

        /// <summary>
        /// Shows what would happen if the cmdlet runs. The cmdlet is not run.
        /// </summary>
        [Parameter]
        [Alias("wi")]
        public SwitchParameter WhatIf
        {
            get => _whatIf;
            set => _whatIf = value;
        }

        private bool _whatIf;

        /// <summary>
        /// Displays a confirmation dialog to require user input to execute the command.
        /// </summary>
        [Parameter]
        public SwitchParameter Confirm
        {
            get => _confirm;
            set => _confirm = value;
        }

        private bool _confirm;

        /// <summary>
        /// Every WritableByname must have ToString overriden. This is because a Byname is simply a wrapper for
        /// the "New-Alias" or "Set-Alias" command. Therefore, the ToString method represents the either of the
        /// previously mentioned alias commands as a string in it's fully qualified form.
        /// </summary>
        /// <returns>A string representing the entire command with all options included in the string</returns>
        public abstract override string ToString();
    }
}
