using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
#if UNITY_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Release Policy Manager
    /// Single Responsibility: Manage release policies, handle tracking, and protection status
    /// Extracted from AddressableAssetReleaseManager for better SRP compliance
    /// </summary>
    public class ReleasePolicyManager
    {
        private readonly bool _enableLogging;
        private readonly Dictionary<string, AssetHandle> _managedHandles;
        private readonly Dictionary<string, DateTime> _lastAccessTimes;
        private readonly HashSet<string> _protectedAssets;
        private readonly Dictionary<Type, ReleasePolicy> _typePolicies;
        private ReleasePolicy _defaultPolicy;
        private ReleaseStats _stats;

        private readonly Action<string> _onReleaseProtectionSet;

        public int ManagedHandleCount => _managedHandles.Count;
        public int ProtectedAssetCount => _protectedAssets.Count;

        public ReleasePolicyManager(
            bool enableLogging,
            Dictionary<string, AssetHandle> managedHandles,
            Dictionary<string, DateTime> lastAccessTimes,
            HashSet<string> protectedAssets,
            Dictionary<Type, ReleasePolicy> typePolicies,
            Action<string> onReleaseProtectionSet,
            ReleaseStats stats)
        {
            _enableLogging = enableLogging;
            _managedHandles = managedHandles;
            _lastAccessTimes = lastAccessTimes;
            _protectedAssets = protectedAssets;
            _typePolicies = typePolicies;
            _onReleaseProtectionSet = onReleaseProtectionSet;
            _stats = stats;
            _defaultPolicy = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// Initialize default release policies
        /// </summary>
        public void InitializeDefaultPolicies()
        {
            _typePolicies[typeof(Texture2D)] = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(15) };
            _typePolicies[typeof(AudioClip)] = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(20) };
            _typePolicies[typeof(GameObject)] = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(5) };
            _typePolicies[typeof(Material)] = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(10) };
            _typePolicies[typeof(ScriptableObject)] = new ReleasePolicy { MaxAge = TimeSpan.FromMinutes(30) };
        }

        /// <summary>
        /// Register asset handle for management
        /// </summary>
        public void RegisterHandle(string address, AsyncOperationHandle handle, Type assetType)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            var assetHandle = new AssetHandle
            {
                Address = address,
                Handle = handle,
                AssetType = assetType,
                RegistrationTime = DateTime.Now,
                ReferenceCount = 1
            };

            _managedHandles[address] = assetHandle;
            _lastAccessTimes[address] = DateTime.Now;
            _stats.HandlesRegistered++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Registered handle for '{address}' ({assetType.Name})");
            }
        }

        /// <summary>
        /// Update access time for asset
        /// </summary>
        public void UpdateAccessTime(string address)
        {
            if (_managedHandles.ContainsKey(address))
            {
                _lastAccessTimes[address] = DateTime.Now;

                var handle = _managedHandles[address];
                handle.ReferenceCount++;
                _managedHandles[address] = handle;
            }
        }

        /// <summary>
        /// Set asset protection status
        /// </summary>
        public bool SetAssetProtection(string address, bool protect)
        {
            if (!_managedHandles.ContainsKey(address))
            {
                return false;
            }

            if (protect)
            {
                _protectedAssets.Add(address);
            }
            else
            {
                _protectedAssets.Remove(address);
            }

            _onReleaseProtectionSet?.Invoke(address);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Asset '{address}' protection {(protect ? "enabled" : "disabled")}");
            }

            return true;
        }

        /// <summary>
        /// Set release policy for asset type
        /// </summary>
        public void SetReleasePolicy(Type assetType, ReleasePolicy policy)
        {
            _typePolicies[assetType] = policy;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Set release policy for {assetType.Name}: MaxAge={policy.MaxAge.TotalMinutes:F1}min");
            }
        }

        /// <summary>
        /// Get release policy for asset type
        /// </summary>
        public ReleasePolicy GetPolicyForType(Type assetType)
        {
            if (_typePolicies.TryGetValue(assetType, out var policy))
            {
                return policy;
            }

            // Check for inherited policies
            foreach (var kvp in _typePolicies)
            {
                if (kvp.Key.IsAssignableFrom(assetType))
                {
                    return kvp.Value;
                }
            }

            return _defaultPolicy;
        }

        /// <summary>
        /// Get release summary
        /// </summary>
        public ReleaseSummary GetReleaseSummary(DateTime lastAutoRelease, bool autoReleaseEnabled)
        {
            var handlesByType = _managedHandles.Values
                .GroupBy(h => h.AssetType)
                .ToDictionary(g => g.Key, g => g.Count());

            var oldestAsset = "";
            var newestAsset = "";
            var oldestTime = DateTime.MaxValue;
            var newestTime = DateTime.MinValue;

            foreach (var kvp in _lastAccessTimes)
            {
                if (kvp.Value < oldestTime)
                {
                    oldestTime = kvp.Value;
                    oldestAsset = kvp.Key;
                }

                if (kvp.Value > newestTime)
                {
                    newestTime = kvp.Value;
                    newestAsset = kvp.Key;
                }
            }

            return new ReleaseSummary
            {
                TotalHandles = _managedHandles.Count,
                ProtectedAssets = _protectedAssets.Count,
                HandlesByType = handlesByType,
                Stats = _stats,
                AutoReleaseEnabled = autoReleaseEnabled,
                LastAutoRelease = lastAutoRelease,
                OldestAsset = oldestAsset,
                NewestAsset = newestAsset,
                AverageHandleAge = CalculateAverageHandleAge()
            };
        }

        /// <summary>
        /// Calculate average handle age
        /// </summary>
        private float CalculateAverageHandleAge()
        {
            if (_lastAccessTimes.Count == 0)
            {
                return 0f;
            }

            var currentTime = DateTime.Now;
            var totalAge = _lastAccessTimes.Values.Sum(time => (currentTime - time).TotalSeconds);
            return (float)(totalAge / _lastAccessTimes.Count);
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

