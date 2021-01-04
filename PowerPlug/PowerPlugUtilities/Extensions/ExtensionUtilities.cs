using System;
using System.Text;

namespace PowerPlug.PowerPlugUtilities.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    internal static class StringBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="this"></param>
        /// <param name="str"></param>
        /// <param name="condition"></param>
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