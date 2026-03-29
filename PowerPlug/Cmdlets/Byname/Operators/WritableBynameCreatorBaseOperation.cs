using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using PowerPlug.Base;
using PowerPlug.BaseCmdlets;
using PowerPlug.Cmdlets.Byname.Base;
using PowerPlug.Cmdlets.Byname.Base.AliasValueTypes;

namespace PowerPlug.Cmdlets.Byname.Operators
{
    /// <summary>
    /// The base Operation to create a writable Byname (classes that inherit <see cref="WritableByname"/>). This class
    /// is part of a broader Byname Strategy to execute cmdlets.
    /// </summary>
    internal abstract class WritableBynameCreatorBaseOperation : BynameCreatorStrategy
    {
        /// <summary>
        /// The New-Alias command as a string constant.
        /// </summary>
        internal const string NewAliasCommand = "New-Alias";

        /// <summary>
        /// The Set-Alias command as a string constant.
        /// </summary>
        internal const string SetAliasCommand = "Set-Alias";

        /// <summary>
        /// The PowerShell command to write to the $PROFILE as a string (including any functions that need to be written).
        /// </summary>
        protected string PsCommandAsString { get; }

        /// <summary>
        /// The WritableByname instance
        /// </summary>
        protected WritableByname AliasCmdlet { get; }

        /// <summary>
        /// Sets the variables for this cmdlet. The results of the command and the command string are also set.
        /// If the -Value of the command string is a function, the function is appended to the command string.
        /// </summary>
        /// <param name="cmdlet">The WritableByname cmdlet</param>
        /// <param name="commandResults">The results of invoking the PowerShell command for the WritableByname cmdlet</param>
        protected WritableBynameCreatorBaseOperation(WritableByname cmdlet, IEnumerable<PSObject> commandResults) : base(commandResults)
        {
            AliasCmdlet = cmdlet;

            var sb = new StringBuilder();
            var valueType = GetAliasValueType();
            if (valueType is FunctionValueType ft && !FunctionExistsInProfile(AliasCmdlet.Value))
            {
                var funcName = AliasCmdlet.Value;
                if (funcName.Contains(' ', StringComparison.Ordinal) || funcName.Contains('"', StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Function name '{funcName}' contains spaces or quotes and cannot be written to $PROFILE. Use a simple name without spaces.");
                }

                sb.Append("function ").Append(funcName)
                  .Append(" { ").Append(ft.ScriptBlock).Append(" }")
                  .Append(Environment.NewLine);
            }

            PsCommandAsString = sb.Append(cmdlet).ToString();
        }

        /// <summary>
        /// Checks whether a function definition for the given name already exists in the user's $PROFILE.
        /// </summary>
        private static bool FunctionExistsInProfile(string functionName)
        {
            if (!Profile.ProfileExists())
            {
                return false;
            }

            var profile = Profile.GetProfile();
            if (!profile.FileInfo.Exists)
            {
                return false;
            }

            var content = File.ReadAllText(profile.FileInfo.FullName);
            var pattern = $@"\bfunction\s+{Regex.Escape(functionName)}\b";
            return Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Get's the type of the value of the WritableByname cmdlet. This is done through invoking a script
        /// in the default run space thread of execution.
        /// </summary>
        /// <returns>A <see cref="CommandAliasValueBaseType"/> base type</returns>
        private CommandAliasValueBaseType GetAliasValueType()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var gc = ps
                .AddCommand("Get-Command")
                .AddParameter("Name", AliasCmdlet.Name)
                .AddCommand("Select-Object")
                .AddParameter("Property", "*")
                .Invoke<PSObject>();

            if (gc == null || gc.Count == 0)
            {
                return new UndefinedValueType(AliasCmdlet);
            }

            var gcProp = gc[0].Properties;
            var resolvedCommandProp = gcProp.FirstOrDefault(e => e.Name == "ResolvedCommand");

            if (resolvedCommandProp?.Value is not CommandInfo cmd)
            {
                return new UndefinedValueType(AliasCmdlet);
            }

            return cmd.CommandType switch
            {
                CommandTypes.Function => new FunctionValueType(AliasCmdlet, cmd.Definition.Trim()),
                CommandTypes.Cmdlet => new CmdletValueType(AliasCmdlet),
                _ => new UndefinedValueType(AliasCmdlet),
            };
        }
    }
}
