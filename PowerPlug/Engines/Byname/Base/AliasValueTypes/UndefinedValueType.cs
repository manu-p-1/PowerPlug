using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    internal sealed record UndefinedValueType : CommandAliasValueBaseType
    {
        public UndefinedValueType(WritableByname cmdlet) : base(cmdlet) { }
    }
}
