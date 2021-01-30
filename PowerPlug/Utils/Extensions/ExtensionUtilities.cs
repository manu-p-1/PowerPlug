using System;
using System.Text;

namespace PowerPlug.Utils.Extensions
{
    /// <summary>
    /// An internal class representing StringBuilder extension methods.
    /// </summary>
    internal static class StringBuilderExtensions
    {
        /// <summary>
        /// A StringBuilder extension to append to the StringBuilder if and only if a condition is met.
        /// </summary>
        /// <param name="this">The StringBuilder extension</param>
        /// <param name="str">The string to append</param>
        /// <param name="condition">The condition to meet in order for the append to occur</param>
        /// <returns></returns>
        public static StringBuilder AppendIf(this StringBuilder @this, string str, bool condition)
        {
            if (@this is null)
            {
                throw new ArgumentNullException(nameof(@this));
            }

            if (condition)
            {
                @this.Append(str);
            }

            return @this;
        }
    }
}