using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Release Executor
    /// Single Responsibility: Execute asset release operations (single, by type, aged, LRU, all)
    /// Extracted from AddressableAssetReleaseManager for better SRP compliance
    /// </summary>
    public class AssetReleaseExecutor
    {
        private readonly bool _enableLogging;
        private readonly Dictionary<string, AssetHandle> _managedHandles;
        private readonly Dictionary<string, DateTime> _lastAccessTimes;
        private readonly HashSet<string> _protectedAssets;
        private readonly Func<Type, ReleasePolicy> _getPolicyForType;
        private ReleaseStats _stats;

        private readonly Action<string, Type> _onAssetReleased;
        private readonly Action<ReleaseOperation> _onReleaseOperationComplete;

        public AssetReleaseExecutor(
            bool enableLogging,
            Dictionary<string, AssetHandle> managedHandles,
            Dictionary<string, DateTime> lastAccessTimes,
            HashSet<string> protectedAssets,
            Func<Type, ReleasePolicy> getPolicyForType,
            ReleaseStats stats,
            Action<string, Type> onAssetReleased,
            Action<ReleaseOperation> onReleaseOperationComplete)
        {
            _enableLogging = enableLogging;
            _managedHandles = managedHandles;
            _lastAccessTimes = lastAccessTimes;
            _protectedAssets = protectedAssets;
            _getPolicyForType = getPolicyForType;
            _stats = stats;
            _onAssetReleased = onAssetReleased;
            _onReleaseOperationComplete = onReleaseOperationComplete;
        }

        /// <summary>
        /// Release specific asset
        /// </summary>
        public bool ReleaseAsset(string address, bool force = false)
        {
            if (!_managedHandles.TryGetValue(address, out var assetHandle))
            {
                return false;
            }

            // Check if asset is protected
            if (!force && _protectedAssets.Contains(address))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("ASSETS", $"Cannot release protected asset '{address}'");
                }
                return false;
            }

            try
            {
                // Release the Addressable handle
                if (assetHandle.Handle.IsValid())
                {
                    Addressables.Release(assetHandle.Handle);
                }

                // Remove from tracking
                _managedHandles.Remove(address);
                _lastAccessTimes.Remove(address);
                _protectedAssets.Remove(address);

                _stats.HandlesReleased++;
                _onAssetReleased?.Invoke(address, assetHandle.AssetType);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", $"Released asset '{address}' ({assetHandle.AssetType.Name})");
                }

                return true;
            }
            catch (Exception ex)
            {
                _stats.ReleaseErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Error releasing asset '{address}': {ex.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// Release assets by type
        /// </summary>
        public int ReleaseAssetsByType(Type assetType)
        {
            var assetsToRelease = _managedHandles.Values
                .Where(h => h.AssetType == assetType || assetType.IsAssignableFrom(h.AssetType))
                .Select(h => h.Address)
                .ToList();

            var releasedCount = 0;

            foreach (var address in assetsToRelease)
            {
                if (ReleaseAsset(address))
                {
                    releasedCount++;
                }
            }

            if (_enableLogging && releasedCount > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Released {releasedCount} assets of type {assetType.Name}");
            }

            return releasedCount;
        }

        /// <summary>
        /// Release aged assets based on policies
        /// </summary>
        public ReleaseOperation ReleaseAgedAssets()
        {
            var operation = new ReleaseOperation
            {
                OperationType = ReleaseOperationType.AgedAssets,
                StartTime = DateTime.Now
            };

            var currentTime = DateTime.Now;
            var assetsToRelease = new List<string>();

            // Identify aged assets
            foreach (var kvp in _managedHandles)
            {
                var address = kvp.Key;
                var handle = kvp.Value;

                if (_protectedAssets.Contains(address))
                {
                    continue;
                }

                var policy = _getPolicyForType(handle.AssetType);
                var lastAccess = _lastAccessTimes.GetValueOrDefault(address, handle.RegistrationTime);
                var age = currentTime - lastAccess;

                if (age > policy.MaxAge)
                {
                    assetsToRelease.Add(address);
                }
            }

            // Release identified assets
            foreach (var address in assetsToRelease)
            {
                if (ReleaseAsset(address))
                {
                    operation.ReleasedAssets++;
                }
                else
                {
                    operation.FailedReleases++;
                }
            }

            operation.Success = operation.FailedReleases == 0;
            operation.CompletionTime = DateTime.Now;
            operation.Duration = (float)(operation.CompletionTime - operation.StartTime).TotalMilliseconds;

            _stats.AgedReleaseOperations++;
            _onReleaseOperationComplete?.Invoke(operation);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Aged asset release: {operation.ReleasedAssets} released, {operation.FailedReleases} failed");
            }

            return operation;
        }

        /// <summary>
        /// Release least recently used assets
        /// </summary>
        public ReleaseOperation ReleaseLeastRecentlyUsed(int maxToRelease)
        {
            var operation = new ReleaseOperation
            {
                OperationType = ReleaseOperationType.LeastRecentlyUsed,
                StartTime = DateTime.Now
            };

            // Sort by last access time (excluding protected assets)
            var candidatesForRelease = _lastAccessTimes
                .Where(kvp => !_protectedAssets.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .Take(maxToRelease)
                .Select(kvp => kvp.Key)
                .ToList();

            // Release assets
            foreach (var address in candidatesForRelease)
            {
                if (ReleaseAsset(address))
                {
                    operation.ReleasedAssets++;
                }
                else
                {
                    operation.FailedReleases++;
                }
            }

            operation.Success = operation.FailedReleases == 0;
            operation.CompletionTime = DateTime.Now;
            operation.Duration = (float)(operation.CompletionTime - operation.StartTime).TotalMilliseconds;

            _stats.LRUReleaseOperations++;
            _onReleaseOperationComplete?.Invoke(operation);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"LRU release: {operation.ReleasedAssets} released, {operation.FailedReleases} failed");
            }

            return operation;
        }

        /// <summary>
        /// Release all non-protected assets
        /// </summary>
        public ReleaseOperation ReleaseAll(bool includeProtected = false)
        {
            var operation = new ReleaseOperation
            {
                OperationType = ReleaseOperationType.All,
                StartTime = DateTime.Now
            };

            var assetsToRelease = _managedHandles.Keys.ToList();

            if (!includeProtected)
            {
                assetsToRelease = assetsToRelease.Where(a => !_protectedAssets.Contains(a)).ToList();
            }

            foreach (var address in assetsToRelease)
            {
                if (ReleaseAsset(address, includeProtected))
                {
                    operation.ReleasedAssets++;
                }
                else
                {
                    operation.FailedReleases++;
                }
            }

            operation.Success = operation.FailedReleases == 0;
            operation.CompletionTime = DateTime.Now;
            operation.Duration = (float)(operation.CompletionTime - operation.StartTime).TotalMilliseconds;

            _stats.FullReleaseOperations++;
            _onReleaseOperationComplete?.Invoke(operation);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Release all: {operation.ReleasedAssets} released, {operation.FailedReleases} failed");
            }

            return operation;
        }

        /// <summary>
        /// Update statistics reference
        /// </summary>
        public void UpdateStats(ReleaseStats stats)
        {
            _stats = stats;
        }
    }
#endif
}

