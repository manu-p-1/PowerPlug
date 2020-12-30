using System;
using System.IO;
using System.Text;

namespace PowerPlug
{
    internal class BaseUtilities
    {
        public static void AddText(Stream fs, string value)
        {
            var info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
    }
    
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
