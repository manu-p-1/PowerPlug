using System;

namespace PowerPlug.Statistics
{

    /// <summary>
    /// 
    /// </summary>
    [Serializable()]
    public class InsufficientDataSetException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public InsufficientDataSetException()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public InsufficientDataSetException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public InsufficientDataSetException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
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

