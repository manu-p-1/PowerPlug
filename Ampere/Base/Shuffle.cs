using System;
using System.Diagnostics.Contracts;

namespace Ampere.Base
{

    /*
     * -----    IMPLEMENTATION NOTE     -----
     * -----       Manu Puduvalli       -----
     * -----    LAST REVISED: 12/1/19   -----
     * 
     * The methodology of the shuffle is simple to understand while being
     * less biased than System.Random. For each character that exists in
     * the char array, a swap occurs two times. Four characters are randomly
     * chosen from the array and their indices are stored. The index values
     * occur from the range of [0, length). In addition, two randomly chosen
     * numbers are created in order to decide which two indices are swapped
     * first.

     * The following is a visual example of the shuffling algorithm described
     * in the summary:
     * -- Note: In the loop below, the values in this example are chosen
     * arbitrarily and will not necessarily be the same values on each
     * iteration. The example uses an array of characters
     *   
     * -- Assume the char array 
     *      -> "[T,h,i,s,c,a,n,b,e,a,r,e,a,l,l,y,l,o,n,g,s,t,r,i,n,g]"
     *   
     * -- loop begins:
     *   
     * -- Four indices (w, x, y, z) are chosen at random: [3, 10, 7, 20]
     *   
     * -- Two more random values (a, b) are chosen in the range of [1,4]
     *   where each value represents one of the four index values.
     * 
     * -- Given every possible combination of [1,4] with combination size 's'
     * where 's' equals 2, the random values 'a' and 'b' match a possible
     * combination and perform two swaps.
     *   
     *      -- If value 'a' is 2 and value 'b' is 4 (or vice versa), then
     *      indices 'x' and 'z' are swapped first, subsequently followed
     *      by the swap of indices 'w' and 'y'.
     *   
     *      -- In the event that value 'a' and 'b' are the same value,
     *      the same random values used for generating 'a' and b'
     *      are reused in order to generate 2 values (d, e) in the
     *      range, [1, 2]. The value 'd' decides whether 'a' or 'b'
     *      will be changed value. For example, if 'd' evaluates
     *      to 1, then value 'a' is guaranteed to change. If 'd'
     *      evaluates to 2, however, then value 'b' is guaranteed change.
     *      Value 'e' decides whether to increment or decrement the number
     *      in order to break the the tie between values 'a' and 'b'.
     *      If 'e' is 1, either 'a' or 'b' is decremented. If 'e' is 2,
     *      either 'a' or 'b is incremented. Special consideration is taken
     *      for the edge cases (if 'a' or 'b' holds the value 1 or 4).
     *   
     * -- The current state of the char array is 
     *      -> "[T,h,i,b,c,a,n,s,e,a,s,e,a,l,l,y,l,o,n,g,r,t,r,i,n,g]
     *   
     * -- Loop 'n' times where 'n' is the size of the array
     *   
     * -- For each letter in the array, a maximum of four character swaps may occur.
     * If swapping values are the same, then a swap does not occur.
     */

    /// <summary>
    /// A utility to shuffle an enumerable
    /// </summary>
    /// <typeparam name="T">The element type of the array</typeparam>
    internal sealed class Shuffle<T>
    {
       /*
        * Holds the array
        */
        private readonly T[] _data;

        /// <summary>
        /// The constructor for the shuffle utility.
        /// </summary>
        /// <param name="data">The generic array to be shuffled</param>
        public Shuffle(T[] data)
        {
            this._data = data;
        }

        /// <summary>
        /// Conducts the shuffle given this array. For more information on how the shuffle is done,
        /// view the implementation note for this class. 
        /// </summary>
        /// <returns>The shuffled array</returns>
        public T[] ShuffleThis()
        {
            using var rngcsp = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var len = _data.Length;

            for (var i = 0; i < len; i++)
            {
                var _1 = new byte[8];
                var _2 = new byte[8];
                var _3 = new byte[8];
                var _4 = new byte[8];

                rngcsp.GetBytes(_1);
                rngcsp.GetBytes(_2);
                rngcsp.GetBytes(_3);
                rngcsp.GetBytes(_4);

                var one = (int)(Math.Abs(BitConverter.ToInt64(_1, 0)) % (len - 1) + 1);
                var two = (int)(Math.Abs(BitConverter.ToInt64(_2, 0)) % (len - 1) + 1);
                var three = (int)(Math.Abs(BitConverter.ToInt64(_3, 0)) % (len - 1) + 1);
                var four = (int)(Math.Abs(BitConverter.ToInt64(_4, 0)) % (len - 1) + 1);

                var indexOne = new byte[8];
                rngcsp.GetBytes(data: indexOne);
                long longIndexOne = Math.Abs(BitConverter.ToInt64(indexOne, startIndex: 0));

                var indexTwo = new byte[8];
                rngcsp.GetBytes(data: indexTwo);
                long longIndexTwo = Math.Abs(BitConverter.ToInt64(indexTwo, startIndex: 0));

                var randOne = (int)(longIndexOne % 4 + 1);
                var randTwo = (int)(longIndexTwo % 4 + 1);

                if (randOne == randTwo)
                {
                    var chooser = (int)(longIndexOne % 2 + 1);
                    var changer = (int)(longIndexTwo % 2 + 1);

                    if (chooser == 1)
                    {
                        if (randOne == 1) randOne++;
                        else if (randOne == 4) randOne--;
                        else randOne = changer == 1 ? randOne - 1 : randOne + 1;
                    }
                    else // 2
                    {
                        if (randTwo == 1) randTwo++;
                        else if (randTwo == 4) randTwo--;
                        else randTwo = changer == 1 ? randTwo - 1 : randTwo + 1;
                    }
                }

                //All combinations of [1-4]
                if ((randOne == 1 && randTwo == 2) || (randOne == 2 && randTwo == 1))
                {
                    ShuffleSwapper(one, two, three, four);
                }
                else if ((randOne == 1 && randTwo == 3) || (randOne == 3 && randTwo == 1))
                {
                    ShuffleSwapper(one, three, three, four);
                }
                else if ((randOne == 1 && randTwo == 4) || (randOne == 4 && randTwo == 1))
                {
                    ShuffleSwapper(one, four, two, three);
                }
                else if ((randOne == 2 && randTwo == 3) || (randOne == 3 && randTwo == 2))
                {
                    ShuffleSwapper(two, three, one, four);
                }
                else if ((randOne == 2 && randTwo == 4) || (randOne == 4 && randTwo == 2))
                {
                    ShuffleSwapper(two, four, one, three);
                }
                else
                {
                    ShuffleSwapper(three, four, one, two);
                }
            }

            return _data;
        }

        /// <summary>
        /// Performs a dual swap on four indices.
        /// </summary>
        /// <param name="indexOne">The first index to be swapped</param>
        /// <param name="indexTwo">The second index to be swapped</param>
        /// <param name="indexThree">The third index to be swapped</param>
        /// <param name="indexFour">The fourth index to be swapped</param>
        private void ShuffleSwapper(int indexOne, int indexTwo, int indexThree, int indexFour)
        {
            //Contract.Requires<ArgumentNullException>(sb != null);
            //Contract.Requires((indexOne >= 0) && (indexTwo >= 0) && (indexThree >= 0) && (indexFour >= 0));
            Contract.EndContractBlock();

            if (indexOne != indexTwo)
            {
                var tmp = _data[indexOne];
                _data[indexOne] = _data[indexTwo];
                _data[indexTwo] = tmp;
            }
            if (indexThree != indexFour)
            {
                var tmp2 = _data[indexThree];
                _data[indexThree] = _data[indexFour];
                _data[indexFour] = tmp2;
            }
        } 
    } //ShuffleUtil
} //Namespace
