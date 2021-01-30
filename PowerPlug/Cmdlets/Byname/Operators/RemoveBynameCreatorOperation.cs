using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using PowerPlug.BaseCmdlets;
using PowerPlug.Cmdlets.Byname.Base;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Cmdlets.Byname.Operators
{
    /// <summary>
    /// The RemoveBynameCreatorOperation is responsible for removing an existing Byname cmdlet string from the user's $PROFILE.
    /// </summary>
    internal class RemoveBynameCreatorOperation : BynameCreatorStrategy
    {
        /// <summary>
        /// The RemoveBynameCmdlet instance
        /// </summary>
        protected RemoveBynameCmdlet AliasCmdlet { get; }

        /// <summary>
        /// The Remove-Alias command as a string constant.
        /// </summary>
        protected const string RemoveAliasCommand = "Remove-Alias";

        /// <summary>
        /// Sets the variables for this cmdlet.
        /// </summary>
        /// <param name="cmdlet">The WritableByname cmdlet</param>
        /// <param name="commandResults">The results of invoking the PowerShell command for the RemoveBynameCmdlet cmdlet</param>>
        internal RemoveBynameCreatorOperation(RemoveBynameCmdlet cmdlet, IEnumerable<PSObject> commandResults) : base(commandResults)
        {
            AliasCmdlet = cmdlet;
        }
    
        /// <summary>
        /// Removes all of the command string information from the user's $PROFILE.
        /// </summary>
        internal override void ExecuteCommand()
        {
            foreach (var r in PsCommandResults)
            {
                AliasCmdlet.WriteObject(r);
            }
            new BynameRemover(this.AliasCmdlet, ProfileInfo).Remove();
        }
    }

    /// <summary>
    /// A BynameRemover class which is responsible for finding pattern matches within the $PROFILE and executing
    /// a removal of those matches. The BynameRemover uses a <see cref="BynameBase"/> to store as a Byname because
    /// even writable and non-writable cmdlets may use this to remove or clean anything from the $PROFILE before
    /// writing or removing from it. This is an internal class in the event of any expansion on the removal process
    /// needs to be added.
    /// </summary>
    internal class BynameRemover
    {
        /// <summary>
        /// The BynameBase instance.
        /// </summary>
        internal BynameBase Byname { get; }

        /// <summary>
        /// A Profile instance containing information about the location of the PowerShell $PROFILE.
        /// </summary>
        internal Profile Profile { get; }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="bb">The BynameBase instance</param>
        /// <param name="profileInfo">The Profile instance</param>
        internal BynameRemover(BynameBase bb, Profile profileInfo)
        {
            Byname = bb;
            Profile = profileInfo;
        }

        /// <summary>
        /// Removes the Byname from the $PROFILE. From an implementation standpoint, this is done by finding a regex match for the command string
        /// in the user $PROFILE. All matches are removed, including any function references which are attached to the command value. Finally,
        /// a new line is appended to the $PROFILE.
        /// </summary>
        /// <returns>A bool representing whether the removal was successful</returns>
        internal bool Remove()
        {
            var text = File.ReadAllText(Profile.FileInfo.FullName);
            var aliasPattern =
                $"(\\s*)((New|Set)-Alias)(\\s)(-Name)(\\s)({Regex.Escape(Byname.Name)})(\\s)(-Value)(\\s)([a-zA-z0-9].*)(\\s)(-Option)(\\s)([a-zA-z].*)(-Scope)(\\s)([a-zA-Z].*)";

            var aliasRegex = new Regex(aliasPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

            try
            {
                foreach (Match match in aliasRegex.Matches(text))
                {
                    var value = match.Value.Split(" ")[4];
                    var funcPattern = $"(\\s*)(function)(\\s*)({Regex.Escape(str: value)})(\\s*)(\\{{)(?s).*(\\}})";
                    var funcRegex = new Regex(funcPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
                    text = funcRegex.Replace(text, string.Empty);
                }
            }
            catch (InvalidOperationException ioe)
            {
                throw new InvalidOperationException("Byname could not be removed due to a formatting issue in the $PROFILE", ioe);
            }

            text = aliasRegex.Replace(text, string.Empty);
            text += Environment.NewLine;

            File.WriteAllText(Profile.FileInfo.FullName, text);
            return true;
        }
    }
}