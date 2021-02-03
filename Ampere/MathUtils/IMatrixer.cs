using System.Collections.Generic;

namespace Ampere.MathUtils
{
    /// <summary>
    /// Represents the minimum requirements to create a Matrix.
    /// </summary>
    /// <typeparam name="T">The element type of this Matrix</typeparam>
    public interface IMatrixer<out T> : IEnumerable<T>
    {
        /// <summary>
        /// Property representing the values of the IMatrixer as a generic 2D array.
        /// </summary>
        public T[,] Values { get; }

        /// <summary>
        /// Property for the number of Rows in and IMatrixer.
        /// </summary>
        public int Rows { get; }

        /// <summary>
        /// Property for the number of columns in an IMatrixer.
        /// </summary>
        public int Cols { get; }

        /// <summary>
        /// Transposes the contents of the Matrix and returns a new Matrix.
        /// </summary>
        /// <returns>A new Matrix containing the transposed version of the original</returns>
        public Matrix Transpose();

        /// <summary>
        /// Returns a string representation of an IMatrixer.
        /// </summary>
        /// <returns></returns>
        public string ToString();
    }
}
