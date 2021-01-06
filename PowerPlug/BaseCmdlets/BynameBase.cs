using System.Management.Automation;

namespace PowerPlug.BaseCmdlets
{
    public abstract class BynameBase : PSCmdlet
    {
        public abstract string Name { get; set; }
    }
}