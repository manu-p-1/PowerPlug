using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    internal sealed record FunctionValueType : CommandAliasValueBaseType
    {
        public string ScriptBlock { get; }

        public FunctionValueType(WritableByname cmdlet, string scriptBlock) : base(cmdlet)
        {
            ScriptBlock = scriptBlock;
        }
    }
}
