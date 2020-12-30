using System.IO;
using System.Management.Automation;

namespace PowerPlug.PowerPlugFile
{

    public class Profile : PowerPlugFileBase
    {
        public DirectoryInfo ProfilePathPath { get; }

        public Profile(string path) : base(path)
        {
            ProfilePathPath = new DirectoryInfo(Path.Combine(path, ".."));
        }

        public static bool ProfileExists(SessionState ss)
        {
            return bool.Parse(ss.InvokeCommand.InvokeScript("Test-Path $PROFILE")[0].ToString());
        }

        public static Profile GetProfile(SessionState ss)
        {
            if (!ProfileExists(ss))
            {
                throw new SessionStateException("Profile Not Found");
            }
            return new Profile(ss.InvokeCommand.InvokeScript("$PROFILE")[0].ToString());
        }
    }
}
