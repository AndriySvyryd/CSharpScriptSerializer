using System;
using System.Collections.Generic;

namespace CSharpScriptSerialization
{
    internal static class Extensions
    {
        internal static IEnumerable<T> GetFlags<T>(this T flags)
        {
            var values = new List<T>();
            var defaultValue = Enum.ToObject(typeof(T), value: 0);
            foreach (Enum currValue in Enum.GetValues(typeof(T)))
            {
                if (currValue.Equals(defaultValue))
                {
                    continue;
                }

                if (((Enum)(object)flags).HasFlag(currValue))
                {
                    values.Add((T)(object)currValue);
                }
            }

            return values;
        }

        internal static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary.GetValueOrDefault(key, default(TValue));

        internal static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key, TValue fallBack)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return fallBack;
        }
    }
}