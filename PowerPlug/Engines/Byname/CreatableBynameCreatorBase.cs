using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname
{
    public abstract class CreatableBynameCreatorBase : BynameCreatorBase
    {
        protected const string NewAliasCommand = "New-Alias";
        protected const string SetAliasCommand = "Set-Alias";
        protected WritableBynameBase AliasCmdlet { get; }

        protected CreatableBynameCreatorBase(WritableBynameBase cmdlet) : base(cmdlet)
        {
            AliasCmdlet = cmdlet;
        }

        protected static CommandAliasValueType GetAliasValueType(WritableBynameBase aliasCmdlet)
        {

            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
            var gc = ps
                .AddScript($"Get-Command {aliasCmdlet.Name} | select *")
                .Invoke();

            var cmdType = gc.ElementAt(0).Properties.FirstOrDefault(e => e.Name == "CommandType");

            if (cmdType == null)
            {
                Exception e = new ArgumentException($"Alias name {nameof(aliasCmdlet)} was invalid");
                aliasCmdlet.ThrowTerminatingError(new ErrorRecord(e, "InvalidAliasNameArgument", ErrorCategory.InvalidArgument, aliasCmdlet));
            }

            switch (cmdType.Value.ToString())
            {
                case "Alias":
                    return new AliasValueType(aliasCmdlet);
                case "Function":
                {
                    var elem0 = gc.ElementAt(0).Properties.FirstOrDefault(e => e.Name == "ScriptBlock");
                    if (elem0 == null)
                    {
                        throw new Exception("ScripBlock Not Found");
                    }
                    return new FunctionValueType(aliasCmdlet, elem0.Value.ToString().Trim());
                }
                default:
                    return new CmdletValueType(aliasCmdlet);
            }
        }

        public override Collection<PSObject> RunCommand(string realCommand)
        {
            using var psShell = PowerShell.Create(RunspaceMode.CurrentRunspace);
            
            var ps = psShell
                .AddCommand(realCommand)
                .AddParameter("Name", AliasCmdlet.Name)
                .AddParameter("Value", AliasCmdlet.Value)
                .AddParameter("Description", AliasCmdlet.Description)
                .AddParameter("Option", AliasCmdlet.Option)
                .AddParameter("PassThru", AliasCmdlet.PassThru)
                .AddParameter("Scope", AliasCmdlet.Scope)
                .AddParameter("Force", AliasCmdlet.Force)
                .AddParameter("WhatIf", AliasCmdlet.WhatIf)
                .AddParameter("Confirm", AliasCmdlet.Confirm)
                .Invoke();

            var errorRec = psShell.Streams.Error;
            if(errorRec.Count >= 1)
            {
                AliasCmdlet.ThrowTerminatingError(errorRec[0]);
            }
            return ps;
        }
    }

    public class CommandAliasValueType
    {
        protected WritableBynameBase AliasCmdlet { get; }

        public CommandAliasValueType(WritableBynameBase cmdlet)
        {
            AliasCmdlet = cmdlet;
        }
    }

    public class FunctionValueType : CommandAliasValueType
    {
        public string ScriptBlock { get; }

        public FunctionValueType(WritableBynameBase cmdlet, string scriptBlock) : base(cmdlet)
        {
            ScriptBlock = scriptBlock;
        }
    }

    public class CmdletValueType : CommandAliasValueType
    {
        public CmdletValueType(WritableBynameBase cmdlet) : base(cmdlet) { }
    }

    public class AliasValueType : CommandAliasValueType
    {
        public AliasValueType(WritableBynameBase cmdlet) : base(cmdlet) { }
    }
}
