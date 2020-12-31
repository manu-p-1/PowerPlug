using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname;

namespace PowerPlug.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/remove-alias?view=powershell-7")]
    [Alias("rbn")]
    public class RemoveBynameProfileAlias : BynameBase
    {
        ///<inheritdoc cref="PowerPlug.BaseCmdlets.BynameBase"/>
        protected override void ProcessRecord() => new RemoveBynameCreator(this).Execute();
    }
}
