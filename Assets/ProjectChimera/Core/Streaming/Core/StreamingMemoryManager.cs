using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Streaming Memory Manager - Focused memory management and cleanup
    /// Handles memory pressure monitoring, asset lifetime tracking, and automatic cleanup
    /// Single Responsibility: Memory management and optimization
    /// </summary>
    public class StreamingMemoryManager : MonoBehaviour
    {
        [Header("Memory Management Settings")]
        [SerializeField] private bool _enableMemoryManagement = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _memoryCheckInterval = 2f;

        [Header("Memory Thresholds")]
        [SerializeField] private long _memoryThreshold = 512 * 1024 * 1024; // 512MB
        [SerializeField] private long _criticalMemoryThreshold = 768 * 1024 * 1024; // 768MB
        [SerializeField] private float _unusedAssetLifetime = 30f;
        [SerializeField] private int _maxCachedAssets = 100;

        [Header("Cleanup Settings")]
        [SerializeField] private bool _enableAutomaticCleanup = true;
        [SerializeField] private float _cleanupInterval = 10f;
        [SerializeField] private int _assetsToCleanupPerFrame = 5;

        // Memory tracking
        private readonly Dictionary<string, MemoryAssetData> _memoryTrackedAssets = new Dictionary<string, MemoryAssetData>();
        private readonly List<string> _assetsToCleanup = new List<string>();

        // Timing
        private float _lastMemoryCheck;
        private float _lastCleanupCheck;

        // Statistics
        private MemoryManagerStats _stats = new MemoryManagerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public MemoryManagerStats GetStats() => _stats;

        // Events
        public System.Action<long, long> OnMemoryPressureChanged;
        public System.Action<string> OnAssetCleanedUp;
        public System.Action OnCriticalMemoryReached;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new MemoryManagerStats();
            _lastMemoryCheck = Time.time;
            _lastCleanupCheck = Time.time;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🧠 StreamingMemoryManager initialized", this);
        }

        /// <summary>
        /// Update memory management - main coordination method
        /// </summary>
        public void UpdateMemoryManagement()
        {
            if (!IsEnabled || !_enableMemoryManagement) return;

            float currentTime = Time.time;

            // Check memory pressure
            if (currentTime - _lastMemoryCheck >= _memoryCheckInterval)
            {
                CheckMemoryPressure();
                _lastMemoryCheck = currentTime;
            }

            // Perform cleanup
            if (_enableAutomaticCleanup && currentTime - _lastCleanupCheck >= _cleanupInterval)
            {
                PerformMemoryCleanup();
                _lastCleanupCheck = currentTime;
            }

            // Update asset lifetimes
            UpdateAssetLifetimes();
        }

        /// <summary>
        /// Track asset for memory management
        /// </summary>
        public void TrackAsset(string assetKey, long estimatedMemorySize = 0)
        {
            if (!_enableMemoryManagement || string.IsNullOrEmpty(assetKey))
                return;

            var memoryData = new MemoryAssetData
            {
                AssetKey = assetKey,
                EstimatedMemorySize = estimatedMemorySize,
                LastAccessTime = Time.time,
                LoadTime = Time.time,
                AccessCount = 1
            };

            _memoryTrackedAssets[assetKey] = memoryData;
            _stats.TrackedAssets++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Started tracking asset for memory: {assetKey}", this);
        }

        /// <summary>
        /// Update asset access time
        /// </summary>
        public void UpdateAssetAccess(string assetKey)
        {
            if (_memoryTrackedAssets.TryGetValue(assetKey, out var data))
            {
                data.LastAccessTime = Time.time;
                data.AccessCount++;
                _memoryTrackedAssets[assetKey] = data;
            }
        }

        /// <summary>
        /// Untrack asset from memory management
        /// </summary>
        public void UntrackAsset(string assetKey)
        {
            if (_memoryTrackedAssets.Remove(assetKey))
            {
                _stats.TrackedAssets--;
                _stats.UnloadedAssets++;

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Stopped tracking asset: {assetKey}", this);
            }
        }

        /// <summary>
        /// Check current memory pressure
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            long currentMemory = GC.GetTotalMemory(false);

            if (currentMemory >= _criticalMemoryThreshold)
                return MemoryPressureLevel.Critical;
            else if (currentMemory >= _memoryThreshold)
                return MemoryPressureLevel.High;
            else if (currentMemory >= _memoryThreshold * 0.7f)
                return MemoryPressureLevel.Medium;
            else
                return MemoryPressureLevel.Low;
        }

        /// <summary>
        /// Force memory cleanup
        /// </summary>
        public int ForceMemoryCleanup(int maxAssetsToCleanup = -1)
        {
            if (maxAssetsToCleanup == -1)
                maxAssetsToCleanup = _assetsToCleanupPerFrame * 3;

            return PerformMemoryCleanup(maxAssetsToCleanup);
        }

        /// <summary>
        /// Get assets recommended for cleanup
        /// </summary>
        public List<string> GetAssetsRecommendedForCleanup()
        {
            var recommendedAssets = new List<string>();
            float currentTime = Time.time;

            foreach (var kvp in _memoryTrackedAssets)
            {
                var data = kvp.Value;

                // Check various cleanup criteria
                if (ShouldAssetBeCleanedUp(data, currentTime))
                {
                    recommendedAssets.Add(kvp.Key);
                }
            }

            // Sort by cleanup priority (least recently used first)
            recommendedAssets.Sort((a, b) =>
            {
                var dataA = _memoryTrackedAssets[a];
                var dataB = _memoryTrackedAssets[b];
                return dataA.LastAccessTime.CompareTo(dataB.LastAccessTime);
            });

            return recommendedAssets;
        }

        /// <summary>
        /// Clear memory cache
        /// </summary>
        public void ClearMemoryCache()
        {
            var assetKeys = new List<string>(_memoryTrackedAssets.Keys);
            foreach (var assetKey in assetKeys)
            {
                UntrackAsset(assetKey);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Memory cache cleared and GC forced", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearMemoryCache();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingMemoryManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check memory pressure and respond accordingly
        /// </summary>
        private void CheckMemoryPressure()
        {
            long currentMemory = GC.GetTotalMemory(false);
            var pressureLevel = GetMemoryPressureLevel();

            // Update statistics
            _stats.CurrentMemoryUsage = currentMemory;
            _stats.MemoryPressureLevel = pressureLevel;

            // Fire memory pressure event
            OnMemoryPressureChanged?.Invoke(currentMemory, _memoryThreshold);

            // Handle critical memory situations
            if (pressureLevel == MemoryPressureLevel.Critical)
            {
                OnCriticalMemoryReached?.Invoke();

                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", $"Critical memory pressure detected: {currentMemory / (1024 * 1024)}MB", this);

                // Force aggressive cleanup
                ForceMemoryCleanup(_assetsToCleanupPerFrame * 2);
            }
            else if (pressureLevel == MemoryPressureLevel.High)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", $"High memory pressure detected: {currentMemory / (1024 * 1024)}MB", this);
            }
        }

        /// <summary>
        /// Perform memory cleanup
        /// </summary>
        private int PerformMemoryCleanup(int maxAssets = -1)
        {
            if (maxAssets == -1)
                maxAssets = _assetsToCleanupPerFrame;

            var assetsToCleanup = GetAssetsRecommendedForCleanup();
            int cleanedUp = 0;

            for (int i = 0; i < Mathf.Min(assetsToCleanup.Count, maxAssets); i++)
            {
                var assetKey = assetsToCleanup[i];
                OnAssetCleanedUp?.Invoke(assetKey);
                UntrackAsset(assetKey);
                cleanedUp++;
            }

            if (cleanedUp > 0)
            {
                _stats.CleanupOperations++;
                _stats.AssetsCleanedUp += cleanedUp;

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Cleaned up {cleanedUp} assets from memory", this);
            }

            return cleanedUp;
        }

        /// <summary>
        /// Update asset lifetimes and mark for cleanup
        /// </summary>
        private void UpdateAssetLifetimes()
        {
            float currentTime = Time.time;
            _assetsToCleanup.Clear();

            foreach (var kvp in _memoryTrackedAssets)
            {
                var data = kvp.Value;

                if (ShouldAssetBeCleanedUp(data, currentTime))
                {
                    _assetsToCleanup.Add(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Check if asset should be cleaned up
        /// </summary>
        private bool ShouldAssetBeCleanedUp(MemoryAssetData data, float currentTime)
        {
            // Check unused lifetime
            if (currentTime - data.LastAccessTime > _unusedAssetLifetime)
                return true;

            // Check if we're over the cache limit
            if (_memoryTrackedAssets.Count > _maxCachedAssets)
            {
                // Clean up assets with low access count first
                if (data.AccessCount <= 1 && currentTime - data.LoadTime > 60f) // 1 minute
                    return true;
            }

            // Memory pressure-based cleanup
            var pressureLevel = GetMemoryPressureLevel();
            if (pressureLevel == MemoryPressureLevel.Critical)
            {
                // More aggressive cleanup during critical memory pressure
                if (currentTime - data.LastAccessTime > _unusedAssetLifetime * 0.5f)
                    return true;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Memory pressure levels
    /// </summary>
    public enum MemoryPressureLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Memory asset tracking data
    /// </summary>
    [System.Serializable]
    public struct MemoryAssetData
    {
        public string AssetKey;
        public long EstimatedMemorySize;
        public float LastAccessTime;
        public float LoadTime;
        public int AccessCount;
    }

    /// <summary>
    /// Memory manager statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryManagerStats
    {
        public int TrackedAssets;
        public int UnloadedAssets;
        public int CleanupOperations;
        public int AssetsCleanedUp;
        public long CurrentMemoryUsage;
        public MemoryPressureLevel MemoryPressureLevel;
    }
}