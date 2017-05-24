using System.Collections.Generic;

namespace EFCore.FluentApiToAnnotation.Extensions
{
    public static class DictionaryExtensions
    {
        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.ContainsKey(key))
            {
                dict.Remove(key);
                return true;
            }
            return false;
        }
    }
}
