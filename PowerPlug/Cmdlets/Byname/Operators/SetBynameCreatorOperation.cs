using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Cmdlets.Byname.Operators
{
    /// <summary>
    /// The SetBynameCreatorOperation is responsible for writing the actual contents of the Byname cmdlet into the
    /// $PROFILE. This class will be responsible replace the contents of an existing cmdlet.
    /// </summary>
    internal class SetBynameCreatorOperation : WritableBynameCreatorBaseOperation
    {
        /// <inheritdoc cref="WritableBynameCreatorBaseOperation"/>
        internal SetBynameCreatorOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(cmdlet, commandResults) { }

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
            new BynameRemover(AliasCmdlet, ProfileInfo).Remove();
            FileUtilities.WriteLine(ProfileInfo.FileInfo, PsCommandAsString);
        }
    }
}
