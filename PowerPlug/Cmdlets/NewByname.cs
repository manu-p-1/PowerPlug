using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname;

namespace PowerPlug.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/new-alias?view=powershell-7")]
    [Alias("nbn")]
    public class NewByname : WritableBynameBase
    {
        ///<inheritdoc cref="BynameBase"/>
        protected override void ProcessRecord() => new NewBynameCreator(this).Execute();
    }
}