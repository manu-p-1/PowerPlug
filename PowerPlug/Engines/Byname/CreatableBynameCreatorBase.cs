using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname
{
    public class AliasValueType
    {
        protected WritableBynameBase AliasCmdlet { get; }

        public AliasValueType(WritableBynameBase cmdlet)
        {
            AliasCmdlet = cmdlet;
        }
    }

    public class FunctionValueType : AliasValueType
    {
        public string ScriptBlock { get; }

        public FunctionValueType(WritableBynameBase cmdlet, string scriptBlock) : base(cmdlet)
        {
            ScriptBlock = scriptBlock;
        }
    }

    public class CmdletValueType : AliasValueType
    {
        public CmdletValueType(WritableBynameBase cmdlet) : base(cmdlet) { }
    }


    public abstract class CreatableBynameCreatorBase : BynameCreatorBase
    {
        protected const string NewAliasCommand = "New-Alias";
        protected const string SetAliasCommand = "Set-Alias";
        protected WritableBynameBase AliasCmdlet { get; }
        protected AliasValueType AliasValueType { get; }

        protected CreatableBynameCreatorBase(WritableBynameBase cmdlet) : base(cmdlet)
        {
            AliasCmdlet = cmdlet;
            AliasValueType = GetAliasValueType();
        }

        private AliasValueType GetAliasValueType()
        {
            // What happens when Get-Command fails? If the Powershell.Create() failed, this shouldn't run.
            var gc = AliasCmdlet.SessionState.InvokeCommand.InvokeScript(
                $"Get-Command {AliasCmdlet.Value} | select *");

            var cmdType = gc.ElementAt(0).Properties.FirstOrDefault(e => e.Name == "CommandType");
            if (cmdType is null)
            {
                throw new Exception();
            }
            if (cmdType.Value.ToString() == "Function")
            {
                var elem0 = gc.ElementAt(0).Properties.FirstOrDefault(e => e.Name == "ScriptBlock");
                if (elem0 is null)
                {
                    throw new Exception("");
                }
                return new FunctionValueType(AliasCmdlet, elem0.Value.ToString().Trim());
            }
            return new CmdletValueType(AliasCmdlet);
        }

        public override Collection<PSObject> RunCommand(string realCommandName) =>
            PowerShell.Create(RunspaceMode.CurrentRunspace)
                .AddCommand(realCommandName)
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
    }
}
