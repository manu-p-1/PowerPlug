using System.Text;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname
{
    public class SetBynameCreator : CreatableBynameCreatorBase
    {
        public SetBynameCreator(WritableBynameBase cmdlet) : base(cmdlet) { }

        public sealed override void Execute()
        {
            var sb = base.RunCommand(SetAliasCommand);
            RemoveBynameCreator.RemoveBynameFromFile(this.AliasCmdlet, ProfileInfo);
            ProfileInfo.WriteLine(sb.ToString());
        }
    }
}
