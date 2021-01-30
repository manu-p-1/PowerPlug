using System;

namespace PowerPlug.MathUtils
{
    /// <summary>
    /// An exception that occurs if a Matrix property is violated
    /// when examining certain properties at runtime.
    /// </summary>
    [Serializable]
    public class MatrixPropertyException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public MatrixPropertyException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public MatrixPropertyException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public MatrixPropertyException(string message, System.Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MatrixPropertyException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
