using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
#if UNITY_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Addressable Asset Release Manager (Coordinator)
    /// Single Responsibility: Orchestrate asset release management using dedicated components
    /// Original file (686 lines) refactored into 4 files (<500 lines each)
    /// </summary>
    public class AddressableAssetReleaseManager
    {
        [Header("Release Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _autoReleaseEnabled = true;
        [SerializeField] private float _autoReleaseInterval = 60f; // seconds
        [SerializeField] private int _maxRetainedHandles = 50;

        // Handle tracking (shared across components)
        private Dictionary<string, AssetHandle> _managedHandles = new Dictionary<string, AssetHandle>();
        private Dictionary<string, DateTime> _lastAccessTimes = new Dictionary<string, DateTime>();
        private HashSet<string> _protectedAssets = new HashSet<string>();
        private Dictionary<Type, ReleasePolicy> _typePolicies = new Dictionary<Type, ReleasePolicy>();

        // Dependencies
        private ReleasePolicyManager _policyManager;
        private AssetReleaseExecutor _releaseExecutor;

        // State tracking
        private DateTime _lastAutoRelease = DateTime.Now;
        private bool _isInitialized = false;
        private bool _isReleasing = false;

        // Statistics
        private ReleaseStats _stats = new ReleaseStats();

        // Events
        public event System.Action<string, Type> OnAssetReleased;
        public event System.Action<ReleaseOperation> OnReleaseOperationComplete;
        public event System.Action<string> OnReleaseProtectionSet;
        public event System.Action<ReleaseStats> OnStatsUpdated;

        public bool IsInitialized => _isInitialized;
        public bool AutoReleaseEnabled => _autoReleaseEnabled;
        public bool IsReleasing => _isReleasing;
        public ReleaseStats Stats => _stats;
        public int ManagedHandleCount => _policyManager?.ManagedHandleCount ?? 0;
        public int ProtectedAssetCount => _policyManager?.ProtectedAssetCount ?? 0;

        public void Initialize()
        {
            if (_isInitialized) return;

            _managedHandles.Clear();
            _lastAccessTimes.Clear();
            _protectedAssets.Clear();
            _typePolicies.Clear();

            // Initialize components
            _policyManager = new ReleasePolicyManager(
                _enableLogging,
                _managedHandles,
                _lastAccessTimes,
                _protectedAssets,
                _typePolicies,
                OnReleaseProtectionSet,
                _stats
            );

            _releaseExecutor = new AssetReleaseExecutor(
                _enableLogging,
                _managedHandles,
                _lastAccessTimes,
                _protectedAssets,
                _policyManager.GetPolicyForType,
                _stats,
                OnAssetReleased,
                OnReleaseOperationComplete
            );

            _policyManager.InitializeDefaultPolicies();
            ResetStats();

            _isInitialized = true;
            _lastAutoRelease = DateTime.Now;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Release Manager initialized (Coordinator)");
            }
        }

        /// <summary>
        /// Register asset handle for management
        /// </summary>
        public void RegisterHandle(string address, AsyncOperationHandle handle, Type assetType)
        {
            if (!_isInitialized) return;

            _policyManager.RegisterHandle(address, handle, assetType);
            _policyManager.UpdateStats(_stats);
        }

        /// <summary>
        /// Update access time for asset
        /// </summary>
        public void UpdateAccessTime(string address)
        {
            if (!_isInitialized) return;

            _policyManager.UpdateAccessTime(address);
        }

        /// <summary>
        /// Release specific asset
        /// </summary>
        public bool ReleaseAsset(string address, bool force = false)
        {
            if (!_isInitialized) return false;

            _releaseExecutor.UpdateStats(_stats);
            var result = _releaseExecutor.ReleaseAsset(address, force);
            _policyManager.UpdateStats(_stats);
            return result;
        }

        /// <summary>
        /// Release assets by type
        /// </summary>
        public int ReleaseAssetsByType<T>()
        {
            return ReleaseAssetsByType(typeof(T));
        }

        /// <summary>
        /// Release assets by type
        /// </summary>
        public int ReleaseAssetsByType(Type assetType)
        {
            if (!_isInitialized) return 0;

            _releaseExecutor.UpdateStats(_stats);
            var result = _releaseExecutor.ReleaseAssetsByType(assetType);
            _policyManager.UpdateStats(_stats);
            return result;
        }

        /// <summary>
        /// Release aged assets based on policies
        /// </summary>
        public ReleaseOperation ReleaseAgedAssets()
        {
            if (!_isInitialized)
            {
                return new ReleaseOperation { Success = false, ErrorMessage = "Not initialized" };
            }

            _releaseExecutor.UpdateStats(_stats);
            var result = _releaseExecutor.ReleaseAgedAssets();
            _policyManager.UpdateStats(_stats);
            return result;
        }

        /// <summary>
        /// Release least recently used assets
        /// </summary>
        public ReleaseOperation ReleaseLeastRecentlyUsed(int maxToRelease)
        {
            if (!_isInitialized)
            {
                return new ReleaseOperation { Success = false, ErrorMessage = "Not initialized" };
            }

            _releaseExecutor.UpdateStats(_stats);
            var result = _releaseExecutor.ReleaseLeastRecentlyUsed(maxToRelease);
            _policyManager.UpdateStats(_stats);
            return result;
        }

        /// <summary>
        /// Release all non-protected assets
        /// </summary>
        public ReleaseOperation ReleaseAll(bool includeProtected = false)
        {
            if (!_isInitialized)
            {
                return new ReleaseOperation { Success = false, ErrorMessage = "Not initialized" };
            }

            _releaseExecutor.UpdateStats(_stats);
            var result = _releaseExecutor.ReleaseAll(includeProtected);
            _policyManager.UpdateStats(_stats);
            return result;
        }

        /// <summary>
        /// Set asset protection status
        /// </summary>
        public bool SetAssetProtection(string address, bool protect)
        {
            if (!_isInitialized) return false;

            return _policyManager.SetAssetProtection(address, protect);
        }

        /// <summary>
        /// Process automatic releases
        /// </summary>
        public void ProcessAutoRelease()
        {
            if (!_isInitialized || !_autoReleaseEnabled)
            {
                return;
            }

            var timeSinceLastRelease = (DateTime.Now - _lastAutoRelease).TotalSeconds;

            if (timeSinceLastRelease >= _autoReleaseInterval)
            {
                _lastAutoRelease = DateTime.Now;

                // Check if we need to release assets due to handle count
                if (_managedHandles.Count > _maxRetainedHandles)
                {
                    var excessCount = _managedHandles.Count - _maxRetainedHandles;
                    ReleaseLeastRecentlyUsed(excessCount);
                }

                // Release aged assets
                ReleaseAgedAssets();

                OnStatsUpdated?.Invoke(_stats);
            }
        }

        /// <summary>
        /// Set release policy for asset type
        /// </summary>
        public void SetReleasePolicy<T>(ReleasePolicy policy)
        {
            SetReleasePolicy(typeof(T), policy);
        }

        /// <summary>
        /// Set release policy for asset type
        /// </summary>
        public void SetReleasePolicy(Type assetType, ReleasePolicy policy)
        {
            if (!_isInitialized) return;

            _policyManager.SetReleasePolicy(assetType, policy);
        }

        /// <summary>
        /// Get release summary
        /// </summary>
        public ReleaseSummary GetReleaseSummary()
        {
            if (!_isInitialized || _policyManager == null)
            {
                return new ReleaseSummary();
            }

            return _policyManager.GetReleaseSummary(_lastAutoRelease, _autoReleaseEnabled);
        }

        /// <summary>
        /// Set auto-release parameters
        /// </summary>
        public void SetAutoReleaseConfig(bool enabled, float intervalSeconds, int maxHandles)
        {
            _autoReleaseEnabled = enabled;
            _autoReleaseInterval = Mathf.Max(10f, intervalSeconds);
            _maxRetainedHandles = Mathf.Max(1, maxHandles);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Auto-release config: Enabled={enabled}, Interval={intervalSeconds}s, MaxHandles={maxHandles}");
            }
        }

        /// <summary>
        /// Reset release statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new ReleaseStats();
        }

        /// <summary>
        /// Clean up all resources
        /// </summary>
        public void Dispose()
        {
            ReleaseAll(true);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Release Manager disposed");
            }
        }
    }
#endif
}

