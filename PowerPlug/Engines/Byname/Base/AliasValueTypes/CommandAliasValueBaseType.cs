using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    internal abstract record CommandAliasValueBaseType
    {
        protected WritableByname AliasCmdlet { get; }

        protected CommandAliasValueBaseType(WritableByname cmdlet)
        {
            AliasCmdlet = cmdlet;
        }
    }
}