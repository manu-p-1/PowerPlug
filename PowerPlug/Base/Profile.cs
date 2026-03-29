using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PowerPlug.Base
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
        /// <returns>True if the profile exists, false otherwise.</returns>
        public static bool ProfileExists()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var results = ps.AddScript("Test-Path $PROFILE").Invoke();
            if (results == null || results.Count == 0)
            {
                return false;
            }

            return bool.TryParse(results[0]?.ToString(), out var exists) && exists;
        }

        /// <summary>
        /// Return's a new <see cref="Profile"/> object containing information about the user's $PROFILE path
        /// </summary>
        /// <exception cref="SessionStateException">A SessionStateException is thrown if the user's $PROFILE cannot be found</exception>
        /// <returns>A Profile instance for the current user's $PROFILE.</returns>
        public static Profile GetProfile()
        {
            if (!ProfileExists())
            {
                throw new SessionStateException("Profile Not Found");
            }

            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var results = ps.AddScript("$PROFILE").Invoke();
            var profilePath = results?.FirstOrDefault()?.ToString();

            if (string.IsNullOrWhiteSpace(profilePath))
            {
                throw new SessionStateException("Profile path could not be determined");
            }

            return new Profile(profilePath);
        }
    }
}
