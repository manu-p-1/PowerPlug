using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Engines.Byname
{
    /// <summary>
    /// The NewBynameCreatorOperation is responsible for writing the actual contents of the Byname cmdlet into the
    /// $PROFILE.
    /// </summary>
    public class NewBynameCreatorOperation : WritableBynameCreatorBaseOperation
    {
        /// <inheritdoc cref="WritableBynameCreatorBaseOperation"/>
        public NewBynameCreatorOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(cmdlet, commandResults) { }

        /// <summary>
        /// Writes all of the information from the invoked command to the PowerShell console. The information is then
        /// written to the PowerShell $PROFILE.
        /// </summary>
        public override void ExecuteCommand()
        {
            foreach (var p in PsCommandResults)
            {
                AliasCmdlet.WriteObject(p);
            }
            FileUtilities.WriteLine(ProfileInfo.FileInfo, PsCommandAsString);
        }
    }
}
