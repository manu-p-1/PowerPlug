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
    /// <summary>
    /// <para type="synopsis">Set's an alias within the profile instead of the session</para>
    /// <para type="description">Set's an alias using Set-Byname, but writes to the $PROFILE instead of the session
    /// </para>
    /// <para type="aliases">sbn</para>
    /// <example>
    /// <para>A sample Set-Alias command</para>
    /// <code>Set-Byname -Name list -Value Get-ChildItem</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Byname", HelpUri = "https://docs.microsoft.com/en-us/powershell/module/Microsoft.PowerShell.Utility/Set-Alias?view=powershell-7")]
    [Alias("sbn")]
    public class SetBynameProfileAlias : WritableBynameBase
    {
        ///<inheritdoc cref="PowerPlug.BaseCmdlets.BynameBase"/>
        protected override void ProcessRecord() => new SetBynameCreator(this).Execute();
    }
}
