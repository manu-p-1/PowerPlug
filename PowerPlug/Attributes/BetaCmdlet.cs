using System;

namespace PowerPlug.Attributes
{
    /// <summary>
    /// The BetaCmdlet attribute represents any cmdlets which are functional, but may result in unintended behavior due
    /// to its "beta" state. The warning message should be emitted via <c>WriteWarning</c> in the cmdlet's
    /// <c>BeginProcessing</c> method, not in the attribute constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class BetaCmdlet : Attribute
    {
        /// <summary>
        /// The message attributed to this BetaCmdlet, if any.
        /// </summary>
        public string Msg { get; }

        /// <summary>
        /// The default warning message for this Attribute.
        /// </summary>
        internal const string WarningMessage = "This is a Beta PowerPlug Cmdlet - it may cause unexpected behavior";

        /// <summary>
        /// Creates a new BetaCmdlet with no message. 
        /// </summary>
        internal BetaCmdlet()
        {
            Msg = string.Empty;
        }

        /// <summary>
        /// Creates a new BetaCmdlet with the specified message.
        /// </summary>
        /// <param name="msg">A message specifying or representing the state of the cmdlet</param>
        internal BetaCmdlet(string msg)
        {
            Msg = msg ?? string.Empty;
        }
    }
}
