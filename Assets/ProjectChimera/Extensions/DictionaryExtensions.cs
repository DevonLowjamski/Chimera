using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Extensions
{
    /// <summary>
    /// Extension methods for Dictionary to support facility statistics operations
    /// </summary>
    public static class DictionaryExtensions
    {
        public static string CurrentTier(this Dictionary<string, object> dict)
        {
            return dict.ContainsKey("currentTier") ? dict["currentTier"]?.ToString() ?? "None" : "None";
        }

        public static int OwnedFacilities(this Dictionary<string, object> dict)
        {
            return dict.ContainsKey("ownedFacilities") && dict["ownedFacilities"] is int value ? value : 0;
        }

        public static bool CanUpgrade(this Dictionary<string, object> dict)
        {
            return dict.ContainsKey("canUpgrade") && dict["canUpgrade"] is bool value ? value : false;
        }

        /// <summary>
        /// Generic extension method for getting dictionary values with default fallback
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
