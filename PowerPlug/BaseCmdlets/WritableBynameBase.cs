using System.Management.Automation;

namespace PowerPlug.BaseCmdlets
{
    /// <summary>
    /// Fill out
    /// </summary>
    public abstract class WritableBynameBase : BynameBase
    {
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
    }
}
