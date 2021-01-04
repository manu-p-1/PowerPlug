using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname.Base;

namespace PowerPlug.Engines.Byname
{
    public class SetBynameCreatorOperation : WritableBynameCreatorBaseOperation
    {
        public SetBynameCreatorOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(cmdlet, commandResults) { }

        public override void ExecuteCommand()
        {
            foreach (var p in PsCommandResults)
            {
                AliasCmdlet.WriteObject(p);
            }
            RemoveBynameCreatorOperation.RemoveBynameFromFile(AliasCmdlet, ProfileInfo);
            ProfileInfo.WriteLine(PsCommandAsString);
        }
    }
}
