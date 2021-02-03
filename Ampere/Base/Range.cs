namespace Ampere.Base
{
    ///<inheritdoc cref="IRangify{T}"/>
    public class Range<T> : IRangify<T> where T : System.IComparable<T>
    {
        ///<inheritdoc cref="IRangify{T}"/>
        public T Minimum { get; set; }

        ///<inheritdoc cref="IRangify{T}"/>
        public T Maximum { get; set; }

        /// <summary>
        /// Creates a new instance of the Range class. This class is mutable - for the immutable
        /// version, see <see cref="ImmutableRange{T}"/>
        /// </summary>
        /// <param name="minimum">The minimum value</param>
        /// <param name="maximum">The maximum value</param>
        protected Range(T minimum, T maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        ///<inheritdoc cref="IRangify{T}"/>
        public override string ToString() => $"[{this.Minimum} - {this.Maximum}]";

        ///<inheritdoc cref="IRangify{T}"/>
        public bool IsValid() => this.Minimum.CompareTo(this.Maximum) <= 0;

        ///<inheritdoc cref="IRangify{T}"/>
        public bool ContainsValue(T value) => this.Minimum.CompareTo(value) <= 0 && value.CompareTo(this.Maximum) <= 0;

        ///<inheritdoc cref="IRangify{T}"/>
        public bool IsInsideRange(IRangify<T> range) =>
            this.IsValid() && range.IsValid() && range.ContainsValue(this.Minimum) && range.ContainsValue(this.Maximum);

        ///<inheritdoc cref="IRangify{T}"/>
        public bool ContainsRange(IRangify<T> range) =>
            this.IsValid() && range.IsValid() && this.ContainsValue(range.Minimum) && this.ContainsValue(range.Maximum);
    }
}