using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname
{
    public class NewBynameCreatorOperation : WritableBynameCreatorBaseOperation
    {
        public NewBynameCreatorOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(cmdlet, commandResults) { }

        public override void ExecuteCommand()
        {
            foreach (var p in PsCommandResults)
            {
                AliasCmdlet.WriteObject(p);
            }
            ProfileInfo.WriteLine(PsCommandAsString);
        }
    }
}
