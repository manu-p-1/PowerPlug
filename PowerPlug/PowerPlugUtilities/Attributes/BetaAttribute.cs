using System;

namespace PowerPlug.PowerPlugUtilities.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class BetaCmdlet : Attribute
    {
        public string Msg { get; }
        internal const string WarningMessage = "This is a Beta PowerPlug Cmdlet - it may cause unexpected behavior";

        internal BetaCmdlet()
        {
        }

        internal BetaCmdlet(string msg)
        {
            this.Msg = msg;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Msg);
            Console.ResetColor();
        }
    }
}
