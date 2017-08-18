using System;
using System.Collections.Generic;

namespace CSharpScriptSerialization
{
    internal static class Extensions
    {
        internal static IReadOnlyCollection<Enum> GetFlags(this Enum flags)
        {
            var values = new List<Enum>();
            var type = flags.GetType();
            var defaultValue = Enum.ToObject(type, value: 0);
            foreach (Enum currValue in Enum.GetValues(type))
            {
                if (currValue.Equals(defaultValue))
                {
                    continue;
                }

                if (flags.HasFlag(currValue))
                {
                    values.Add(currValue);
                }
            }

            return values;
        }

        internal static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key)
            => dictionary.GetValueOrDefault(key, default(TValue));

        internal static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue fallBack)
            => dictionary.TryGetValue(key, out var value) ? value : fallBack;
    }
}