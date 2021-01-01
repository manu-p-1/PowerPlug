using System;
using System.Text;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname
{
    public class NewBynameCreator : CreatableBynameCreatorBase
    {
        public NewBynameCreator(WritableBynameBase cmdlet) : base(cmdlet) { }
        public sealed override void Execute()
        {
            var sb = base.RunCommand(NewAliasCommand);
            ProfileInfo.WriteLine(sb.ToString());
        }
    }
}
