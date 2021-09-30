using System;
using System.Linq;
using System.Collections.Generic;

namespace Damselfly.Core.Utils
{
    public static class EnumerableExtensions
    {
        public static bool ArePermutations( this IEnumerable<string> collection1, IEnumerable<string> collection2 )
        {
            var set1 = new HashSet<string>(collection1, StringComparer.OrdinalIgnoreCase);
            return set1.SetEquals(collection2);
        }

        public static List<T> GetEnumList<T>()
        {
            var enumArray = (T[])Enum.GetValues(typeof(T));

            return enumArray.ToList();
        }
    }
}
