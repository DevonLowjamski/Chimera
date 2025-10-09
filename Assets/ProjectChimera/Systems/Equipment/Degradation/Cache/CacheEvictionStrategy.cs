// REFACTORED: Cache Eviction Strategy
// Extracted from CacheOptimizationManager for better separation of concerns

using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    /// <summary>
    /// Handles cache eviction policy selection and implementation
    /// </summary>
    public class CacheEvictionStrategy
    {
        private readonly CacheEvictionPolicy _policy;
        private readonly Dictionary<string, int> _accessFrequency;
        private readonly CacheStorageManager _storageManager;

        public CacheEvictionStrategy(CacheEvictionPolicy policy, 
                                     Dictionary<string, int> accessFrequency,
                                     CacheStorageManager storageManager)
        {
            _policy = policy;
            _accessFrequency = accessFrequency ?? throw new ArgumentNullException(nameof(accessFrequency));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
        }

        /// <summary>
        /// Select items for eviction based on configured policy
        /// </summary>
        public IEnumerable<string> SelectItemsForEviction(int itemCount)
        {
            return _policy switch
            {
                CacheEvictionPolicy.LRU => SelectLRUItems(itemCount),
                CacheEvictionPolicy.LFU => SelectLFUItems(itemCount),
                CacheEvictionPolicy.FIFO => SelectFIFOItems(itemCount),
                CacheEvictionPolicy.Random => SelectRandomItems(itemCount),
                _ => SelectLRUItems(itemCount)
            };
        }

        /// <summary>
        /// Select Least Recently Used items
        /// </summary>
        private IEnumerable<string> SelectLRUItems(int itemCount)
        {
            var allMetadata = _storageManager.GetAllMetadata();
            return allMetadata
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select Least Frequently Used items
        /// </summary>
        private IEnumerable<string> SelectLFUItems(int itemCount)
        {
            return _accessFrequency
                .OrderBy(kvp => kvp.Value)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select First In First Out items
        /// </summary>
        private IEnumerable<string> SelectFIFOItems(int itemCount)
        {
            var allMetadata = _storageManager.GetAllMetadata();
            return allMetadata
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select random items for eviction
        /// </summary>
        private IEnumerable<string> SelectRandomItems(int itemCount)
        {
            var allKeys = _storageManager.GetAllKeys().ToList();
            var random = new Random();

            return allKeys
                .OrderBy(x => random.Next())
                .Take(itemCount);
        }
    }
}

