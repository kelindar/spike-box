using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spike.Scripting.Runtime
{
    /// <summary>
    /// Represent an array object.
    /// </summary>
    public sealed partial class ArrayObject
    {
        /// <summary>
        /// Converts the array to a particular typed enumerable.
        /// </summary>
        /// <typeparam name="T">The type to convert it to.</typeparam>
        /// <returns></returns>
        public IEnumerable<T> ToArray<T>()
        {
            // Create a list that will hold the result
            var result = new List<T>();

            // Foreach element in the array
            var length = this.Length;
            for (int i = 0; i < length; ++i)
            {
                // Get the item at the index and unbox it
                var item = this.Get(i).Unbox<T>();

                // Add to the result list
                result.Add(item);
            }

            // Return the list
            return result;
        }

    }

}
