using System;

namespace PowerPlug.PowerPlugUtilities.Attributes
{
    /// <summary>
    /// The BetaAttribute attribute represents any cmdlets which are functional, but may result in unintended behavior due
    /// to its "beta" state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class BetaAttribute : Attribute
    {
        /// <summary>
        /// The message attributed to this BetaCmdletAttribute, if any.
        /// </summary>
        public string Msg { get; } = string.Empty;

        /// <summary>
        /// The default warning message for this Attribute.
        /// </summary>
        internal const string WarningMessage = "This is a Beta PowerPlug Cmdlet - it may cause unexpected behavior";

        /// <summary>
        /// Creates a new BetaAttribute with no message. 
        /// </summary>
        internal BetaAttribute() { }

        /// <summary>
        /// Creates a new BetaAttribute with the specified message.
        /// </summary>
        /// <param name="msg">A message specifying or representing the state of the cmdlet</param>
        internal BetaAttribute(string msg)
        {
            this.Msg = msg;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Msg);
            Console.ResetColor();
        }
    }
}
