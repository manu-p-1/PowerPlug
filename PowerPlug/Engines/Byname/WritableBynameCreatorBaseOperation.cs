using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using PowerPlug.BaseCmdlets;
using PowerPlug.Engines.Byname.Base;
using PowerPlug.Engines.Byname.Base.AliasValueTypes;

namespace PowerPlug.Engines.Byname
{
    public abstract class WritableBynameCreatorBaseOperation : BynameCreatorStrategy
    {
        public const string NewAliasCommand = "New-Alias";
        public const string SetAliasCommand = "Set-Alias";
        protected string PsCommandAsString { get; }
        protected WritableByname AliasCmdlet { get; }

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
