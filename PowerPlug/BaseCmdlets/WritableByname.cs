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
        public override string Name { get; set; } = string.Empty;

        /// <summary>
        /// The scope parameter for the command determines which scope the alias is set in.
        /// </summary>
        [Parameter]
        [ValidateSet("Global", "Local", "Private", "Script")]
        public string Scope { get; set; } = "Local";

        /// <summary>
        /// If set to true and an existing alias of the same name exists
        /// and is ReadOnly, the alias will be overwritten.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// The Value parameter for the command.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Value { get; set; } = string.Empty;

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
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Every WritableByname must have ToString overriden. This is because a Byname is simply a wrapper for
        /// the "New-Alias" or "Set-Alias" command. Therefore, the ToString method represents the either of the
        /// previously mentioned alias commands as a string in it's fully qualified form.
        /// </summary>
        /// <returns>A string representing the entire command with all options included in the string</returns>
        public abstract override string ToString();
    }
}
