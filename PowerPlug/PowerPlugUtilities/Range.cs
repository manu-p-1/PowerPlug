using System;

namespace PowerPlug.PowerPlugUtilities
{
    /// <summary>The range class represents a range of values of a generic type.
    /// The Range class which was modified from user @drharris on StackOverflow.
    /// </summary>
    /// <typeparam name="T">The generic parameter</typeparam>
    public class Range<T> where T : IComparable<T>
    {
        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public T Minimum { get; set; }

        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public T Maximum { get; set; }

        /// <summary>
        /// Creates a new range instance.
        /// </summary>
        /// <param name="minimum">The minimum value of the range</param>
        /// <param name="maximum">The maximum value of the range</param>
        public Range(T minimum, T maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Presents the Range in a readable format.
        /// </summary>
        /// <returns>The string representation of the Range</returns>
        public override string ToString() => $"[{this.Minimum} - {this.Maximum}]";

        /// <summary>
        /// Determines if the range is valid.
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public bool IsValid() => this.Minimum.CompareTo(this.Maximum) <= 0;

        /// <summary>
        /// Determines if the provided value is inside the range.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value) => this.Minimum.CompareTo(value) <= 0 && value.CompareTo(this.Maximum) <= 0;

        /// <summary>
        /// Determines if this Range is inside the bounds of another range.
        /// </summary>
        /// <param name="range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(Range<T> range) => 
            this.IsValid() && range.IsValid() && range.ContainsValue(this.Minimum) && range.ContainsValue(this.Maximum);

        /// <summary>
        /// Determines if another range is inside the bounds of this range.
        /// </summary>
        /// <param name="range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public bool ContainsRange(Range<T> range) => 
            this.IsValid() && range.IsValid() && this.ContainsValue(range.Minimum) && this.ContainsValue(range.Maximum);
    }
}
