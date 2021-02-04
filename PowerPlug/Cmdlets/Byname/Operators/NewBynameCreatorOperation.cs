using System.Collections.Generic;
using System.Management.Automation;
using Ampere.FileUtils;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Cmdlets.Byname.Operators
{
    /// <summary>
    /// The NewBynameCreatorOperation is responsible for writing the actual contents of the Byname cmdlet into the
    /// $PROFILE.
    /// </summary>
    internal class NewBynameCreatorOperation : WritableBynameCreatorBaseOperation
    {
        /// <inheritdoc cref="WritableBynameCreatorBaseOperation"/>
        internal NewBynameCreatorOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(cmdlet, commandResults) { }

        /// <summary>
        /// Writes all of the information from the invoked command to the PowerShell console. The information is then
        /// written to the PowerShell $PROFILE.
        /// </summary>
        internal override void ExecuteCommand()
        {
            foreach (var p in PsCommandResults)
            {
                AliasCmdlet.WriteObject(p);
            }
            FileUtils.WriteLine(ProfileInfo.FileInfo, PsCommandAsString);
        }
    }
}
