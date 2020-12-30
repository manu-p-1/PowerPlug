using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Engines.Byname
{
    public abstract class BynameCreatorBase
    {
        protected Profile ProfileInfo { get; }

        protected BynameCreatorBase(PSCmdlet cmdlet)
        {
            if (!Profile.ProfileExists(cmdlet.SessionState))
            {
                throw new Exception("A $PROFILE could not be found");
            }
            ProfileInfo = Profile.GetProfile(cmdlet.SessionState);
        }

        public abstract Collection<PSObject> RunCommand(string realCommandName);
        public abstract void Execute();
    }
}
