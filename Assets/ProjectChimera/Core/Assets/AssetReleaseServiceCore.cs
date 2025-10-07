using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// REFACTORED: Asset Release Service - Core Operations (POCO - Unity-independent)
    /// Part 1 of 2: Tracking, protection, and basic release operations
    /// Kept under 500 lines per clean architecture guidelines
    /// </summary>
    public partial class AssetReleaseService
    {
        // Configuration
        private readonly bool _enableLogging;
        private readonly bool _enableAutomaticCleanup;
        private readonly float _cleanupInterval;
        private readonly float _unusedAssetTimeout;
        private readonly bool _enableMemoryPressureCleanup;
        private readonly long _memoryPressureThreshold;
        private readonly bool _forceGCAfterCleanup;
        private readonly ReleaseStrategy _releaseStrategy;
        private readonly int _maxReleasesPerFrame;

        // Asset tracking for cleanup
        private readonly Dictionary<string, AssetReleaseInfo> _trackedAssets = new Dictionary<string, AssetReleaseInfo>();
        private readonly Queue<string> _releaseQueue = new Queue<string>();
        private readonly HashSet<string> _protectedAssets = new HashSet<string>();

        // External references
        private IAssetManager _assetManager;
        private AssetCacheManager _cacheManager;

        // Cleanup timing (injected via time provider)
        private float _lastCleanupTime = 0f;
        private float _lastMemoryCheck = 0f;

        // Statistics
        private AssetReleaseManagerStats _stats = new AssetReleaseManagerStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<string> OnAssetReleased;
        public event System.Action<CleanupResult> OnCleanupCompleted;
        public event System.Action<MemoryPressureEvent> OnMemoryPressureDetected;

        public bool IsInitialized => _isInitialized;
        public AssetReleaseManagerStats Stats => _stats;
        public int TrackedAssetCount => _trackedAssets.Count;
        public int QueuedReleaseCount => _releaseQueue.Count;

        public AssetReleaseService(
            bool enableLogging = false,
            bool enableAutomaticCleanup = true,
            float cleanupInterval = 60f,
            float unusedAssetTimeout = 300f,
            bool enableMemoryPressureCleanup = true,
            long memoryPressureThreshold = 536870912,
            bool forceGCAfterCleanup = true,
            ReleaseStrategy releaseStrategy = ReleaseStrategy.TimeoutBased,
            int maxReleasesPerFrame = 10)
        {
            _enableLogging = enableLogging;
            _enableAutomaticCleanup = enableAutomaticCleanup;
            _cleanupInterval = cleanupInterval;
            _unusedAssetTimeout = unusedAssetTimeout;
            _enableMemoryPressureCleanup = enableMemoryPressureCleanup;
            _memoryPressureThreshold = memoryPressureThreshold;
            _forceGCAfterCleanup = forceGCAfterCleanup;
            _releaseStrategy = releaseStrategy;
            _maxReleasesPerFrame = maxReleasesPerFrame;
        }

        public void Initialize(IAssetManager assetManager = null, AssetCacheManager cacheManager = null)
        {
            if (_isInitialized) return;

            _assetManager = assetManager;
            _cacheManager = cacheManager;

            if (_cacheManager == null && _enableLogging)
            {
                ChimeraLogger.LogWarning("ASSETS", "AssetCacheManager not provided - caching features disabled");
            }

            _trackedAssets.Clear();
            _releaseQueue.Clear();
            _protectedAssets.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Asset Release Service initialized");
            }
        }

        /// <summary>
        /// Track asset for potential cleanup
        /// </summary>
        public void TrackAsset(string address, object asset, float currentTime)
        {
            if (!_isInitialized || string.IsNullOrEmpty(address) || asset == null)
                return;

            var releaseInfo = new AssetReleaseInfo
            {
                Address = address,
                Asset = asset,
                TrackingStartTime = currentTime,
                LastAccessTime = currentTime,
                AccessCount = 1,
                ReferenceCount = 1,
                IsProtected = false
            };

            _trackedAssets[address] = releaseInfo;
            _stats.AssetsTracked++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Tracking asset for cleanup: {address}");
            }
        }

        /// <summary>
        /// Update asset access information
        /// </summary>
        public void UpdateAssetAccess(string address, float currentTime)
        {
            if (_trackedAssets.TryGetValue(address, out var releaseInfo))
            {
                releaseInfo.LastAccessTime = currentTime;
                releaseInfo.AccessCount++;
                _trackedAssets[address] = releaseInfo;
            }
        }

        /// <summary>
        /// Increment asset reference count
        /// </summary>
        public void IncrementReferenceCount(string address)
        {
            if (_trackedAssets.TryGetValue(address, out var releaseInfo))
            {
                releaseInfo.ReferenceCount++;
                _trackedAssets[address] = releaseInfo;
            }
        }

        /// <summary>
        /// Decrement asset reference count
        /// </summary>
        public void DecrementReferenceCount(string address)
        {
            if (_trackedAssets.TryGetValue(address, out var releaseInfo))
            {
                releaseInfo.ReferenceCount = System.Math.Max(0, releaseInfo.ReferenceCount - 1);
                _trackedAssets[address] = releaseInfo;

                // Queue for release if no more references
                if (releaseInfo.ReferenceCount == 0 && !releaseInfo.IsProtected)
                {
                    QueueAssetForRelease(address);
                }
            }
        }

        /// <summary>
        /// Protect asset from automatic cleanup
        /// </summary>
        public void ProtectAsset(string address)
        {
            _protectedAssets.Add(address);

            if (_trackedAssets.TryGetValue(address, out var releaseInfo))
            {
                releaseInfo.IsProtected = true;
                _trackedAssets[address] = releaseInfo;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Protected asset from cleanup: {address}");
            }
        }

        /// <summary>
        /// Unprotect asset from automatic cleanup
        /// </summary>
        public void UnprotectAsset(string address)
        {
            _protectedAssets.Remove(address);

            if (_trackedAssets.TryGetValue(address, out var releaseInfo))
            {
                releaseInfo.IsProtected = false;
                _trackedAssets[address] = releaseInfo;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Unprotected asset from cleanup: {address}");
            }
        }

        /// <summary>
        /// Manually release specific asset
        /// </summary>
        public bool ReleaseAsset(string address)
        {
            if (!_isInitialized || string.IsNullOrEmpty(address))
                return false;

            try
            {
                // Remove from tracking
                _trackedAssets.Remove(address);
                _protectedAssets.Remove(address);

                // Remove from queues
                var queueItems = _releaseQueue.ToArray();
                _releaseQueue.Clear();
                foreach (var item in queueItems)
                {
                    if (item != address)
                        _releaseQueue.Enqueue(item);
                }

                // Release via asset manager
                bool released = false;
                if (_assetManager != null)
                {
                    try
                    {
                        _assetManager.UnloadAsset(address);
                        released = true;
                    }
                    catch (System.Exception ex)
                    {
                        if (_enableLogging)
                        {
                            ChimeraLogger.LogWarning("ASSETS", $"Asset release warning for {address}: {ex.Message}");
                        }
                        released = false;
                    }
                }

                // Remove from cache
                if (_cacheManager != null)
                {
                    _cacheManager.RemoveFromCache(address);
                }

                if (released)
                {
                    _stats.AssetsReleased++;
                    OnAssetReleased?.Invoke(address);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("ASSETS", $"Released asset: {address}");
                    }
                }

                return released;
            }
            catch (System.Exception ex)
            {
                _stats.ReleaseErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Error releasing asset {address}: {ex.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// Check if automatic cleanup should run
        /// </summary>
        public bool ShouldRunAutomaticCleanup(float currentTime)
        {
            return _enableAutomaticCleanup && currentTime - _lastCleanupTime >= _cleanupInterval;
        }

        /// <summary>
        /// Check if memory pressure check should run
        /// </summary>
        public bool ShouldCheckMemoryPressure(float currentTime)
        {
            return _enableMemoryPressureCleanup && currentTime - _lastMemoryCheck >= 10f;
        }

        /// <summary>
        /// Update memory check time
        /// </summary>
        public void UpdateMemoryCheckTime(float currentTime)
        {
            _lastMemoryCheck = currentTime;
        }

        #region Private Methods

        /// <summary>
        /// Queue asset for release
        /// </summary>
        private void QueueAssetForRelease(string address)
        {
            if (!_releaseQueue.Contains(address))
            {
                _releaseQueue.Enqueue(address);
            }
        }

        /// <summary>
        /// Process queued asset releases
        /// </summary>
        private void ProcessReleaseQueue()
        {
            int processed = 0;
            while (_releaseQueue.Count > 0 && processed < _maxReleasesPerFrame)
            {
                var address = _releaseQueue.Dequeue();
                ReleaseAsset(address);
                processed++;
            }
        }

        /// <summary>
        /// Format bytes for display
        /// </summary>
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F1} GB";
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new AssetReleaseManagerStats
            {
                AssetsTracked = 0,
                AssetsReleased = 0,
                ReleaseErrors = 0,
                AutomaticCleanups = 0,
                MemoryPressureEvents = 0
            };
        }

        #endregion
    }

    /// <summary>
    /// Asset release strategy
    /// </summary>
    public enum ReleaseStrategy
    {
        TimeoutBased,
        LeastRecentlyUsed,
        LeastFrequentlyUsed,
        ReferenceCountBased
    }

    /// <summary>
    /// Asset release information
    /// </summary>
    [System.Serializable]
    public struct AssetReleaseInfo
    {
        public string Address;
        public object Asset;
        public float TrackingStartTime;
        public float LastAccessTime;
        public int AccessCount;
        public int ReferenceCount;
        public bool IsProtected;
    }

    /// <summary>
    /// Cleanup operation result
    /// </summary>
    [System.Serializable]
    public struct CleanupResult
    {
        public int InitialAssetCount;
        public int FinalAssetCount;
        public int AssetsReleased;
        public float CleanupTime;
        public List<string> ReleasedAssets;
    }

    /// <summary>
    /// Memory pressure event data
    /// </summary>
    [System.Serializable]
    public struct MemoryPressureEvent
    {
        public long CurrentMemoryUsage;
        public long MemoryThreshold;
        public long ExcessMemory;
        public float DetectionTime;
    }

    /// <summary>
    /// Asset release manager statistics
    /// </summary>
    [System.Serializable]
    public struct AssetReleaseManagerStats
    {
        public int AssetsTracked;
        public int AssetsReleased;
        public int ReleaseErrors;
        public int AutomaticCleanups;
        public int MemoryPressureEvents;
    }
}
