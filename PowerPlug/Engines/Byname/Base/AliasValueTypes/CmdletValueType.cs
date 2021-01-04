using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    internal sealed record CmdletValueType : CommandAliasValueBaseType
    {
        public CmdletValueType(WritableByname cmdlet) : base(cmdlet) { }
    }
}
