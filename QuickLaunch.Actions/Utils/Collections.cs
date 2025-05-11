using System.Collections.Generic;

namespace QuickLaunch.Core.Utils
{
    public static class ListExtensions
    {

        public static void AddRange<T>(this IList<T> collection, IEnumerable<T> toAdd)
        {
            foreach (var item in toAdd)
            {
                collection.Add(item);
            }

        }
    }

    public static class DictionaryExtensions
    {
        public static void Update<K, V>(this Dictionary<K, V> dictionary, IEnumerable<KeyValuePair<K, V>> toUpdate)
            where K : notnull
        {
            foreach (var (key, value) in toUpdate)
            {
                dictionary.Add(key, value);
            }
        }
    }

}
