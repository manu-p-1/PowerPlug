using System;
using System.Collections;
using System.Collections.Generic;

namespace Ampere.Base
{
    /// <summary>
    /// The range class represents a range of int values. Unlike other <see cref="IRangify{T}"/> implementing classes,
    /// IntRange contains an <see cref="IEnumerator{T}"/> of type int to enumerate through all of the values between
    /// the minimum and maximum ranges. By convention, both sides of the range should be inclusive values.
    /// </summary>
    public class IntRange : Range<int>, IEnumerable<int>
    {
        /// <summary>
        /// Creates a new instance of IntRange, specifying the minimum and maximum values.
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        public IntRange(int minimum, int maximum) : base(minimum, maximum){ }

        /// <summary>
        /// Returns an instance of the IntRangeEnumerator that's used to enumerate through the range
        /// values of this instance.
        /// </summary>
        /// <returns>An instance of the IntRangeEnumerator class</returns>
        public IEnumerator<int> GetEnumerator() => new IntRangeEnumerator(Minimum, Maximum);

        /// <summary>
        /// Returns an instance of the IntRangeEnumerator that's used to enumerate through the range
        /// values of this instance.
        /// </summary>
        /// <returns>An instance of the IntRangeEnumerator class</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private sealed class IntRangeEnumerator : IEnumerator<int>
        {
            private readonly int _minimum;
            private readonly int _maximum;
            private int _position;

            public IntRangeEnumerator(int minimum, int maximum)
            {
                this._minimum = minimum;
                this._maximum = maximum;
                this._position = minimum;
            }

            public bool MoveNext()
            {
                _position++;
                return (_position < _maximum);
            }

            public void Reset()
            {
                _position = _minimum;
            }

            public object Current
            {
                get
                {
                    if (_position > this._maximum)
                    {
                        throw new InvalidOperationException("Range maximum exceeded");
                    }
                    return _position;
                }
            }

            // ReSharper disable once PossibleNullReferenceException
            int IEnumerator<int>.Current => (int)Current;

            public void Dispose() { } // According to Microsoft, leave empty if nothing to Dispose
        }
    }
}