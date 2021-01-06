using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using PowerPlug.BaseCmdlets;
using PowerPlug.Cmdlets;
using PowerPlug.Engines.Byname.Base;
using PowerPlug.PowerPlugFile;

namespace PowerPlug.Engines.Byname
{
    public class RemoveBynameCreatorOperation : BynameCreatorStrategy
    {
        protected RemoveBynameCmdlet AliasCmdlet { get; }

        protected const string RemoveAliasCommand = "Remove-Alias";
        public RemoveBynameCreatorOperation(RemoveBynameCmdlet cmdlet, IEnumerable<PSObject> commandResults) : base(commandResults)
        {
            AliasCmdlet = cmdlet;
        }
    
        public override void ExecuteCommand()
        {
            foreach (var r in PsCommandResults)
            {
                AliasCmdlet.WriteObject(r);
            }
            new BynameRemover(this.AliasCmdlet, ProfileInfo).Remove();
        }
    }

    internal class BynameRemover
    {
        public BynameBase Byname { get; }

        public PowerPlugFileBase PowerPlugFile { get; }

        public BynameRemover(BynameBase bb, PowerPlugFileBase powerPlugFile)
        {
            Byname = bb;
            PowerPlugFile = powerPlugFile;
            
        }

        public bool Remove()
        {
            var text = File.ReadAllText(PowerPlugFile.FileInfo.FullName);
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

            File.WriteAllText(PowerPlugFile.FileInfo.FullName, text);
            return true;
        }
    }
}