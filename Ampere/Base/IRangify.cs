using System;

namespace Ampere.Base
{
    /// <summary>The range class represents a range of values of a generic type.
    /// The Range class which was modified from user @drharris on StackOverflow.
    /// By convention, both sides of the range should be inclusive values.
    /// </summary>
    /// <typeparam name="T">The generic parameter</typeparam>
    public interface IRangify<T> where T : IComparable<T>
    {
        /// <summary>
        /// The minimum value of this range
        /// </summary>
        public T Minimum { get; }

        /// <summary>
        /// The maximum value of this range
        /// </summary>
        public T Maximum { get; }

        /// <summary>
        /// Determines if the range is valid.
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public bool IsValid();

        /// <summary>
        /// Determines if the provided value is inside the range.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value);

        /// <summary>
        /// Determines if this Range is inside the bounds of another range.
        /// </summary>
        /// <param name="range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(IRangify<T> range);
    }
}
