using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using ProjectChimera.Core.Logging;
namespace ProjectChimera.Core.Memory
{
    /// <summary>
    /// MEMORY: String optimization utilities to reduce allocation overhead
    /// Provides string interning, caching, and efficient string operations
    /// Week 10: Memory & GC Optimization
    /// </summary>
    public static class StringOptimizer
    {
        // String interning cache
        private static readonly Dictionary<string, string> _internCache = new Dictionary<string, string>();
        private static readonly object _internLock = new object();
        
        // StringBuilder pool
        private static readonly Queue<StringBuilder> _stringBuilderPool = new Queue<StringBuilder>();
        private static readonly object _builderLock = new object();
        
        // Common string cache
        private static readonly Dictionary<int, string> _numberCache = new Dictionary<int, string>();
        private static readonly Dictionary<float, string> _floatCache = new Dictionary<float, string>();
        
        // String formatting cache
        private static readonly Dictionary<string, string> _formatCache = new Dictionary<string, string>();
        
        private const int MaxCacheSize = 1000;
        private const int MaxBuilderPoolSize = 10;
        private const int DefaultBuilderCapacity = 256;

        static StringOptimizer()
        {
            InitializeCommonStrings();
        }

        /// <summary>
        /// Intern a string to reduce memory usage for frequently used strings
        /// </summary>
        public static string Intern(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            lock (_internLock)
            {
                if (_internCache.TryGetValue(str, out string cached))
                {
                    return cached;
                }

                // Limit cache size to prevent unbounded growth
                if (_internCache.Count >= MaxCacheSize)
                {
                    ClearOldestEntries(_internCache, MaxCacheSize / 2);
                }

                string interned = string.Intern(str);
                _internCache[str] = interned;
                return interned;
            }
        }

        /// <summary>
        /// Get a StringBuilder from the pool
        /// </summary>
        public static StringBuilder GetStringBuilder(int capacity = DefaultBuilderCapacity)
        {
            lock (_builderLock)
            {
                if (_stringBuilderPool.Count > 0)
                {
                    var sb = _stringBuilderPool.Dequeue();
                    sb.Clear();
                    if (sb.Capacity < capacity)
                    {
                        sb.Capacity = capacity;
                    }
                    return sb;
                }
            }

            return new StringBuilder(capacity);
        }

        /// <summary>
        /// Return a StringBuilder to the pool
        /// </summary>
        public static string ReturnStringBuilder(StringBuilder sb)
        {
            if (sb == null) return string.Empty;

            string result = sb.ToString();

            lock (_builderLock)
            {
                if (_stringBuilderPool.Count < MaxBuilderPoolSize && sb.Capacity <= DefaultBuilderCapacity * 4)
                {
                    sb.Clear();
                    _stringBuilderPool.Enqueue(sb);
                }
            }

            return result;
        }

        /// <summary>
        /// Optimized integer to string conversion with caching
        /// </summary>
        public static string ToString(int value)
        {
            // Cache common values
            if (value >= -100 && value <= 1000)
            {
                if (_numberCache.TryGetValue(value, out string cached))
                {
                    return cached;
                }

                if (_numberCache.Count < MaxCacheSize)
                {
                    cached = value.ToString();
                    _numberCache[value] = cached;
                    return cached;
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Optimized float to string conversion with caching
        /// </summary>
        public static string ToString(float value, string format = "F2")
        {
            // Only cache simple values to avoid excessive memory usage
            if (Mathf.Abs(value) < 1000f && _floatCache.Count < MaxCacheSize)
            {
                if (_floatCache.TryGetValue(value, out string cached))
                {
                    return cached;
                }

                cached = value.ToString(format);
                _floatCache[value] = cached;
                return cached;
            }

            return value.ToString(format);
        }

        /// <summary>
        /// Optimized string concatenation using StringBuilder pool
        /// </summary>
        public static string Concat(params string[] strings)
        {
            if (strings == null || strings.Length == 0)
                return string.Empty;

            if (strings.Length == 1)
                return strings[0] ?? string.Empty;

            var sb = GetStringBuilder();
            foreach (var str in strings)
            {
                if (str != null)
                    sb.Append(str);
            }

            return ReturnStringBuilder(sb);
        }

        /// <summary>
        /// Optimized string joining with separator
        /// </summary>
        public static string Join(string separator, IEnumerable<string> values)
        {
            if (values == null)
                return string.Empty;

            var sb = GetStringBuilder();
            bool first = true;

            foreach (var value in values)
            {
                if (!first && separator != null)
                    sb.Append(separator);

                if (value != null)
                    sb.Append(value);

                first = false;
            }

            return ReturnStringBuilder(sb);
        }

        /// <summary>
        /// Optimized string formatting with caching for common patterns
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                return format;

            // Simple cache key for common patterns
            string cacheKey = null;
            if (args.Length <= 2 && format.Length < 50)
            {
                var sb = GetStringBuilder(64);
                sb.Append(format);
                sb.Append('|');
                foreach (var arg in args)
                {
                    sb.Append(arg?.ToString() ?? "null");
                    sb.Append('|');
                }
                cacheKey = sb.ToString();
                ReturnStringBuilder(sb);

                if (_formatCache.TryGetValue(cacheKey, out string cached))
                {
                    return cached;
                }
            }

            string result = string.Format(format, args);

            // Cache result if we have a cache key and space
            if (cacheKey != null && _formatCache.Count < MaxCacheSize)
            {
                _formatCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Create optimized string representation of Vector3
        /// </summary>
        public static string ToString(Vector3 vector, string format = "F2")
        {
            var sb = GetStringBuilder();
            sb.Append('(');
            sb.Append(ToString(vector.x, format));
            sb.Append(", ");
            sb.Append(ToString(vector.y, format));
            sb.Append(", ");
            sb.Append(ToString(vector.z, format));
            sb.Append(')');
            return ReturnStringBuilder(sb);
        }

        /// <summary>
        /// Create optimized string representation of Vector2
        /// </summary>
        public static string ToString(Vector2 vector, string format = "F2")
        {
            var sb = GetStringBuilder();
            sb.Append('(');
            sb.Append(ToString(vector.x, format));
            sb.Append(", ");
            sb.Append(ToString(vector.y, format));
            sb.Append(')');
            return ReturnStringBuilder(sb);
        }

        /// <summary>
        /// Optimized comparison that avoids allocation
        /// </summary>
        public static bool EqualsIgnoreCase(string a, string b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clear caches to free memory
        /// </summary>
        public static void ClearCaches()
        {
            lock (_internLock)
            {
                _internCache.Clear();
            }

            lock (_builderLock)
            {
                _stringBuilderPool.Clear();
            }

            _numberCache.Clear();
            _floatCache.Clear();
            _formatCache.Clear();

            ChimeraLogger.LogInfo("StringOptimizer", "$1");
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public static StringCacheStats GetStats()
        {
            return new StringCacheStats
            {
                InternCacheSize = _internCache.Count,
                StringBuilderPoolSize = _stringBuilderPool.Count,
                NumberCacheSize = _numberCache.Count,
                FloatCacheSize = _floatCache.Count,
                FormatCacheSize = _formatCache.Count,
                TotalCachedItems = _internCache.Count + _numberCache.Count + _floatCache.Count + _formatCache.Count
            };
        }

        /// <summary>
        /// Trim caches to reduce memory usage
        /// </summary>
        public static void TrimCaches(int maxSize = MaxCacheSize / 2)
        {
            lock (_internLock)
            {
                if (_internCache.Count > maxSize)
                {
                    ClearOldestEntries(_internCache, maxSize);
                }
            }

            if (_numberCache.Count > maxSize)
            {
                ClearOldestEntries(_numberCache, maxSize);
            }

            if (_floatCache.Count > maxSize)
            {
                ClearOldestEntries(_floatCache, maxSize);
            }

            if (_formatCache.Count > maxSize)
            {
                ClearOldestEntries(_formatCache, maxSize);
            }
        }

        #region Private Methods

        /// <summary>
        /// Initialize cache with common strings
        /// </summary>
        private static void InitializeCommonStrings()
        {
            // Pre-cache common numbers
            for (int i = -10; i <= 100; i++)
            {
                _numberCache[i] = i.ToString();
            }

            // Pre-cache common floats
            for (int i = -10; i <= 10; i++)
            {
                float f = i * 0.1f;
                _floatCache[f] = f.ToString("F1");
                
                f = i * 0.5f;
                _floatCache[f] = f.ToString("F1");
            }
        }

        /// <summary>
        /// Clear oldest entries from a dictionary to prevent unbounded growth
        /// </summary>
        private static void ClearOldestEntries<TKey, TValue>(Dictionary<TKey, TValue> dict, int targetSize)
        {
            if (dict.Count <= targetSize) return;

            var keys = new List<TKey>(dict.Keys);
            int toRemove = dict.Count - targetSize;
            
            for (int i = 0; i < toRemove && i < keys.Count; i++)
            {
                dict.Remove(keys[i]);
            }
        }

        #endregion
    }

    /// <summary>
    /// Disposable wrapper for StringBuilder that automatically returns to pool
    /// </summary>
    public readonly struct StringBuilderScope : IDisposable
    {
        private readonly StringBuilder _stringBuilder;

        public StringBuilder StringBuilder => _stringBuilder;

        internal StringBuilderScope(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
        }

        public static StringBuilderScope Create(int capacity = 256)
        {
            return new StringBuilderScope(StringOptimizer.GetStringBuilder(capacity));
        }

        public string ToStringAndReturn()
        {
            return StringOptimizer.ReturnStringBuilder(_stringBuilder);
        }

        public void Dispose()
        {
            StringOptimizer.ReturnStringBuilder(_stringBuilder);
        }

        public override string ToString()
        {
            return _stringBuilder?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Statistics for string caches
    /// </summary>
    [System.Serializable]
    public struct StringCacheStats
    {
        public int InternCacheSize;
        public int StringBuilderPoolSize;
        public int NumberCacheSize;
        public int FloatCacheSize;
        public int FormatCacheSize;
        public int TotalCachedItems;
    }
}