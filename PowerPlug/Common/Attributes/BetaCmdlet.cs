using System;

namespace PowerPlug.Common.Attributes
{
    /// <summary>
    /// The BetaCmdlet attribute represents any cmdlets which are functional, but may result in unintended behavior due
    /// to its "beta" state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class BetaCmdlet : Attribute
    {
        /// <summary>
        /// The message attributed to this BetaCmdlet, if any.
        /// </summary>
        public string Msg { get; } = string.Empty;

        /// <summary>
        /// The default warning message for this Attribute.
        /// </summary>
        internal const string WarningMessage = "This is a Beta PowerPlug Cmdlet - it may cause unexpected behavior";

        /// <summary>
        /// Creates a new BetaCmdlet with no message. 
        /// </summary>
        internal BetaCmdlet() { }

        /// <summary>
        /// Creates a new BetaCmdlet with the specified message.
        /// </summary>
        /// <param name="msg">A message specifying or representing the state of the cmdlet</param>
        internal BetaCmdlet(string msg)
        {
            this.Msg = msg;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine(Msg);
            Console.ResetColor();
        }
    }
}
