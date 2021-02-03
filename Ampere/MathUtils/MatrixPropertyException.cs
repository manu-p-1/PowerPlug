using System;

namespace Ampere.MathUtils
{
    /// <summary>
    /// An exception that occurs if a Matrix property is violated
    /// when examining certain properties at runtime.
    /// </summary>
    [Serializable]
    public class MatrixPropertyException : Exception
    {
        /// <summary>
        /// Creates a new MatrixPropertyException.
        /// </summary>
        public MatrixPropertyException()
        { }

        /// <summary>
        /// Creates a new overloaded MatrixPropertyException containing a message.
        /// </summary>
        /// <param name="message">The message of this exception type</param>
        public MatrixPropertyException(string message) : base(message) { }

        /// <summary>
        /// Creates a new overloaded MatrixPropertyException containing a message and an inner Exception.
        /// </summary>
        /// <param name="message">The message of this exception type</param>
        /// <param name="inner">The inner Exception</param>
        public MatrixPropertyException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Creates a new overloaded MatrixPropertyException containing a <see cref="System.Runtime.Serialization.SerializationInfo"/> instance
        /// and a <see cref="System.Runtime.Serialization.StreamingContext"/> instance.
        /// </summary>
        /// <param name="info">The SerializationInfo instance</param>
        /// <param name="context">The StreamingContext instance</param>
        protected MatrixPropertyException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
