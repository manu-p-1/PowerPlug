using System.Collections.Generic;
using System.Management.Automation;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Cmdlets.Byname.Base
{
    /// <summary>
    /// A strategy class to invoke a BynameCreator of a specific type. This follows the strategy design pattern.
    /// </summary>
    internal abstract class BynameCreatorStrategy
    {
        /// <summary>
        /// A Profile instance containing information about the location of the PowerShell $PROFILE
        /// </summary>
        internal Profile ProfileInfo { get; }

        /// <summary>
        /// The results of the PowerShell command as an <see cref="IEnumerable{T}"/>
        /// </summary>
        protected IEnumerable<PSObject> PsCommandResults { get; }

        /// <summary>
        /// Sets the variables for the BynameCreatorStrategy.
        /// </summary>
        /// <param name="commandResults">The results of invoking the PowerShell command</param>
        protected BynameCreatorStrategy(IEnumerable<PSObject> commandResults)
        {
            ProfileInfo = Profile.GetProfile(); //Get the profile and throw an exception if not exists.
            PsCommandResults = commandResults;
        }

        /// <summary>
        /// Executes a BynameCreator command. This could be any operation the command introduces in order to
        /// create a Byname.
        /// </summary>
        internal abstract void ExecuteCommand();
    }
}