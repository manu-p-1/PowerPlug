using System.IO;
using System.Management.Automation;

namespace PowerPlug.PowerPlugFile
{

    /// <summary>
    /// An abstracted representation of the location of a user's PowerShell $PROFILE path
    /// </summary>
    public class Profile : PowerPlugFileBase
    {
        /// <inheritdoc cref="PowerPlugFileBase"/>
        public Profile(string path) : base(path) { }

        ///<inheritdoc cref="PowerPlugFileBase"/>
        public Profile(FileInfo fileInfo) : base(fileInfo) { }

        /// <summary>
        /// Runs a PowerShell script to check if the user's $PROFILE path exists. The command run internally
        /// is <code>Test-Path $PROFILE</code>
        /// </summary>
        /// <returns></returns>
        public static bool ProfileExists()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            return bool.Parse(ps.AddScript("Test-Path $PROFILE").Invoke()[0].ToString());
        }

        /// <summary>
        /// Return's a new <see cref="Profile"/> object containing information about the user's $PROFILE path
        /// </summary>
        /// <exception cref="SessionStateException">A SessionStateException is thrown if the user's $PROFILE cannot be found</exception>
        /// <returns></returns>
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
