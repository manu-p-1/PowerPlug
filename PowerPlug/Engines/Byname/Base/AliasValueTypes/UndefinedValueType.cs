using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    /// <summary>
    /// Represents the type of the value of a command string which is not <em>immediately</em> recognized by PowerShell.
    /// </summary>
    internal sealed record UndefinedValueType : CommandAliasValueBaseType
    {
        ///<inheritdoc cref="CommandAliasValueBaseType"/>
        public UndefinedValueType(WritableByname cmdlet) : base(cmdlet) { }
    }
}
