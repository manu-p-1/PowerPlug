using System;

namespace Ampere.Statistics
{

    /// <summary>
    /// An exception that is thrown when the data set is not large enough to compute a statistic.
    /// </summary>
    [Serializable]
    public class InsufficientDataSetException : Exception
    {
        /// <summary>
        /// Creates a new InsufficientDataSetException.
        /// </summary>
        public InsufficientDataSetException()
        { }

        /// <summary>
        /// Creates a new overloaded InsufficientDataSetException containing a message.
        /// </summary>
        /// <param name="message"></param>
        public InsufficientDataSetException(string message) : base(message) { }

        /// <summary>
        /// Creates a new overloaded InsufficientDataSetException containing a message and an inner Exception.
        /// </summary>
        /// <param name="message">The message of this exception type</param>
        /// <param name="inner">The inner Exception</param>
        public InsufficientDataSetException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Creates a new overloaded InsufficientDataSetException containing a <see cref="System.Runtime.Serialization.SerializationInfo"/> instance
        /// and a <see cref="System.Runtime.Serialization.StreamingContext"/> instance.
        /// </summary>
        /// <param name="info">The SerializationInfo instance</param>
        /// <param name="context">The StreamingContext instance</param>
        protected InsufficientDataSetException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable()]
    public class NoModeException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public NoModeException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public NoModeException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public NoModeException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NoModeException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

