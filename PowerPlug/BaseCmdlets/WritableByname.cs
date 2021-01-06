using System.Management.Automation;

namespace PowerPlug.BaseCmdlets
{
    /// <summary>
    /// Fill out
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
        /// 
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
        /// 
        /// </summary>
        [Parameter]
        public SwitchParameter Confirm
        {
            get => _confirm;
            set => _confirm = value;
        }

        private bool _confirm;

        public abstract override string ToString();
    }
}
