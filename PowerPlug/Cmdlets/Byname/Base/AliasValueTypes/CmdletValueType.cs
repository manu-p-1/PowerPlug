using PowerPlug.BaseCmdlets;

namespace PowerPlug.Cmdlets.Byname.Base.AliasValueTypes
{
    /// <summary>
    /// Represents the type of the value of a command string which is a PowerShell cmdlet.
    /// </summary>
    internal sealed record CmdletValueType : CommandAliasValueBaseType
    {
        ///<inheritdoc cref="CommandAliasValueBaseType"/>
        public CmdletValueType(WritableByname cmdlet) : base(cmdlet) { }
    }
}
