using System.Collections.Generic;

namespace EcsSharp.Helpers
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            HashSet<T> result = new HashSet<T>(source);
            return result;
        }
    }
}