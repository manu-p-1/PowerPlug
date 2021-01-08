using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    /// <summary>
    /// Represents the type of the value of a command string which is a PowerShell function.
    /// </summary>
    internal sealed record FunctionValueType : CommandAliasValueBaseType
    {
        public string ScriptBlock { get; }

        /// <summary>
        /// Sets the variables for the WritableByname instance and the string of values inside the function script block.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="scriptBlock"></param>
        public FunctionValueType(WritableByname cmdlet, string scriptBlock) : base(cmdlet)
        {
            ScriptBlock = scriptBlock;
        }
    }
}
