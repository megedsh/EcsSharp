using System;
using System.Collections.Generic;

namespace EcsSharp.Helpers
{
    internal static class DictionaryExtensions
    {
        internal static TVal ComputeIfAbsent<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, TKey key, Func<TKey, TVal> factory)
        {
            if (!dictionary.TryGetValue(key, out TVal val))
            {
                dictionary[key] = factory.Invoke(key);
                return dictionary[key];
            }

            return val;
        }
        internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
            where TValue : class
        {
            if (source.TryGetValue(key, out TValue val))
            {
                return val;
            }

            return default(TValue);
        }

        internal static TValue? GetValueOrNullable<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
            where TValue : struct
        {
            if (source.TryGetValue(key, out TValue val))
            {
                return val;
            }

            return new TValue?();
        }
    }
}