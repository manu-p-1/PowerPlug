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

        public static bool ProfileExists()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            return bool.Parse(ps.AddScript("Test-Path $PROFILE").Invoke()[0].ToString());
        }

        public static Profile GetProfile()
        {
            if (!ProfileExists())
            {
                throw new SessionStateException("Profile Not Found");
            }
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            return new Profile(ps.AddScript("$PROFILE").Invoke()[0].ToString());
        }
    }
}
