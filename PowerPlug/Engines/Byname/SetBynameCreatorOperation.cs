using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;

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
            new BynameRemover(AliasCmdlet, ProfileInfo).Remove();
            ProfileInfo.WriteLine(PsCommandAsString);
        }
    }
}
