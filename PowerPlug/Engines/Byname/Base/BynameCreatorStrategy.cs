using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Engines.Byname.Base
{
    public abstract class BynameCreatorStrategy
    {
        public Profile ProfileInfo { get; }
        protected IEnumerable<PSObject> PsCommandResults { get; }

        protected BynameCreatorStrategy(IEnumerable<PSObject> commandResults)
        {
            ProfileInfo = Profile.GetProfile();
            PsCommandResults = commandResults;
        }

        public abstract void ExecuteCommand();
    }
}