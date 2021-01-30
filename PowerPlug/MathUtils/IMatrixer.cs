using System.Collections.Generic;

namespace PowerPlug.MathUtils
{
    /// <summary>
    /// Represents the minimum requirements to create a Matrix.
    /// </summary>
    /// <typeparam name="T">The element type of this Matrix</typeparam>
    public interface IMatrixer<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Property for the number of Rows in and IMatrixer
        /// </summary>
        public int Rows { get; }

        /// <summary>
        /// Property for the number of columns in an IMatrixer
        /// </summary>
        public int Cols { get; }

        /// <summary>
        /// Returns a string representation of an IMatrixer
        /// </summary>
        /// <returns></returns>
        public string ToString();
    }
}
