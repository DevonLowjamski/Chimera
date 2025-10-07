using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// REFACTORED: Asset Release Service - Cleanup Operations (POCO - Unity-independent)
    /// Part 2 of 2: Cleanup strategies, memory management, and batch operations
    /// Kept under 500 lines per clean architecture guidelines
    /// </summary>
    public partial class AssetReleaseService
    {
        /// <summary>
        /// Release all non-critical assets
        /// </summary>
        public CleanupResult ReleaseNonCriticalAssets(float currentTime)
        {
            var initialCount = _trackedAssets.Count;
            var releasedAssets = new List<string>();

            // Find non-critical assets to release
            var assetsToRelease = _trackedAssets
                .Where(kvp => !kvp.Value.IsProtected && kvp.Value.ReferenceCount == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            // Track cleanup start
            var cleanupStartTime = System.DateTime.Now;

            // Release assets
            foreach (var address in assetsToRelease)
            {
                if (ReleaseAsset(address))
                {
                    releasedAssets.Add(address);
                }
            }

            var cleanupTime = (float)(System.DateTime.Now - cleanupStartTime).TotalSeconds;
            var result = new CleanupResult
            {
                InitialAssetCount = initialCount,
                FinalAssetCount = _trackedAssets.Count,
                AssetsReleased = releasedAssets.Count,
                CleanupTime = cleanupTime,
                ReleasedAssets = releasedAssets
            };

            OnCleanupCompleted?.Invoke(result);

            if (_enableLogging && releasedAssets.Count > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Released {releasedAssets.Count} non-critical assets in {cleanupTime * 1000f:F1}ms");
            }

            return result;
        }

        /// <summary>
        /// Release all assets
        /// </summary>
        public CleanupResult ReleaseAllAssets()
        {
            var initialCount = _trackedAssets.Count;
            var releasedAssets = new List<string>();
            var cleanupStartTime = System.DateTime.Now;

            // Release all tracked assets
            var allAssets = _trackedAssets.Keys.ToList();
            foreach (var address in allAssets)
            {
                if (ReleaseAsset(address))
                {
                    releasedAssets.Add(address);
                }
            }

            // Clear all tracking
            _trackedAssets.Clear();
            _protectedAssets.Clear();
            _releaseQueue.Clear();

            // Force garbage collection if enabled (Unity-independent)
            if (_forceGCAfterCleanup)
            {
                System.GC.Collect();
            }

            var cleanupTime = (float)(System.DateTime.Now - cleanupStartTime).TotalSeconds;
            var result = new CleanupResult
            {
                InitialAssetCount = initialCount,
                FinalAssetCount = 0,
                AssetsReleased = releasedAssets.Count,
                CleanupTime = cleanupTime,
                ReleasedAssets = releasedAssets
            };

            OnCleanupCompleted?.Invoke(result);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Released all {releasedAssets.Count} assets in {cleanupTime * 1000f:F1}ms");
            }

            return result;
        }

        /// <summary>
        /// Perform automatic cleanup based on strategy
        /// </summary>
        public CleanupResult PerformAutomaticCleanup(float currentTime)
        {
            if (!_isInitialized || !_enableAutomaticCleanup)
                return new CleanupResult();

            var initialCount = _trackedAssets.Count;
            var releasedAssets = new List<string>();
            var cleanupStartTime = System.DateTime.Now;

            // Find assets to release based on strategy
            var assetsToRelease = GetAssetsForCleanup(currentTime);

            // Process release queue first
            ProcessReleaseQueue();

            // Release identified assets
            foreach (var address in assetsToRelease.Take(_maxReleasesPerFrame))
            {
                if (ReleaseAsset(address))
                {
                    releasedAssets.Add(address);
                }
            }

            var cleanupTime = (float)(System.DateTime.Now - cleanupStartTime).TotalSeconds;
            var result = new CleanupResult
            {
                InitialAssetCount = initialCount,
                FinalAssetCount = _trackedAssets.Count,
                AssetsReleased = releasedAssets.Count,
                CleanupTime = cleanupTime,
                ReleasedAssets = releasedAssets
            };

            _stats.AutomaticCleanups++;
            OnCleanupCompleted?.Invoke(result);

            _lastCleanupTime = currentTime;

            if (_enableLogging && releasedAssets.Count > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Automatic cleanup released {releasedAssets.Count} assets");
            }

            return result;
        }

        /// <summary>
        /// Check and handle memory pressure
        /// </summary>
        public bool CheckMemoryPressure(float currentTime)
        {
            if (!_enableMemoryPressureCleanup)
                return false;

            var currentMemory = System.GC.GetTotalMemory(false);

            if (currentMemory > _memoryPressureThreshold)
            {
                var memoryEvent = new MemoryPressureEvent
                {
                    CurrentMemoryUsage = currentMemory,
                    MemoryThreshold = _memoryPressureThreshold,
                    ExcessMemory = currentMemory - _memoryPressureThreshold,
                    DetectionTime = currentTime
                };

                OnMemoryPressureDetected?.Invoke(memoryEvent);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("ASSETS", $"Memory pressure detected: {FormatBytes(currentMemory)} / {FormatBytes(_memoryPressureThreshold)}");
                }

                // Trigger aggressive cleanup
                ReleaseNonCriticalAssets(currentTime);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get assets for cleanup based on strategy
        /// </summary>
        private List<string> GetAssetsForCleanup(float currentTime)
        {
            var assetsToRelease = new List<string>();

            switch (_releaseStrategy)
            {
                case ReleaseStrategy.TimeoutBased:
                    assetsToRelease = _trackedAssets
                        .Where(kvp => !kvp.Value.IsProtected &&
                                     kvp.Value.ReferenceCount == 0 &&
                                     currentTime - kvp.Value.LastAccessTime > _unusedAssetTimeout)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    break;

                case ReleaseStrategy.LeastRecentlyUsed:
                    assetsToRelease = _trackedAssets
                        .Where(kvp => !kvp.Value.IsProtected && kvp.Value.ReferenceCount == 0)
                        .OrderBy(kvp => kvp.Value.LastAccessTime)
                        .Take(_maxReleasesPerFrame)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    break;

                case ReleaseStrategy.LeastFrequentlyUsed:
                    assetsToRelease = _trackedAssets
                        .Where(kvp => !kvp.Value.IsProtected && kvp.Value.ReferenceCount == 0)
                        .OrderBy(kvp => kvp.Value.AccessCount)
                        .Take(_maxReleasesPerFrame)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    break;

                case ReleaseStrategy.ReferenceCountBased:
                    assetsToRelease = _trackedAssets
                        .Where(kvp => !kvp.Value.IsProtected && kvp.Value.ReferenceCount == 0)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    break;
            }

            return assetsToRelease;
        }
    }
}
