using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
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
            if (GetAliasValueType() is FunctionValueType ft)
            {
                sb.Append($"function {AliasCmdlet.Value} {{ {ft.ScriptBlock} }}{Environment.NewLine}");
            }

            PsCommandAsString = sb.Append(cmdlet).ToString();
        }

        /// <summary>
        /// Get's the type of the value of the WritableByname cmdlet. This is done through invoking a script
        /// in the default run space thread of execution. The script run is <code>Get-Command {AliasCmdlet.Name} | select*
        /// </code>.
        /// The properties of the command are read and a return type is assumed.
        /// </summary>
        /// <returns>A <see cref="CommandAliasValueBaseType"/> base type</returns>
        private CommandAliasValueBaseType GetAliasValueType()
        {
            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var gc = ps
                .AddScript($"Get-Command {AliasCmdlet.Name} | select *")
                .Invoke<PSObject>();

            var gcProp = gc[0].Properties;
            var resolvedValue = gcProp.FirstOrDefault(e => e.Name == "ResolvedCommand").Value;

            if (resolvedValue == null)
            {
                return new UndefinedValueType(AliasCmdlet);
            }

            var cmd = (resolvedValue as CommandInfo);
            return cmd.CommandType.ToString() switch
            {
                "Function" => new FunctionValueType(AliasCmdlet, cmd.Definition.Trim()),
                "Cmdlet" => new CmdletValueType(AliasCmdlet),
                _ => new UndefinedValueType(AliasCmdlet),
            };
        }
    }
}
