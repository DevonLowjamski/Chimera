using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Preload Asset Manager
    /// Single Responsibility: Manage preload asset lists, tracking, and configuration
    /// Extracted from AddressableAssetPreloader (691 lines → 4 files <500 lines each)
    /// </summary>
    public class PreloadAssetManager
    {
        private readonly Dictionary<string, PreloadAssetInfo> _preloadAssets = new Dictionary<string, PreloadAssetInfo>();
        private readonly HashSet<string> _preloadedAssets = new HashSet<string>();
        private readonly HashSet<string> _failedPreloads = new HashSet<string>();
        private PreloadConfiguration _config;
        private readonly bool _enableLogging;

        public int TotalPreloadAssets => _preloadAssets.Count;
        public int PreloadedAssetCount => _preloadedAssets.Count;
        public int FailedPreloadCount => _failedPreloads.Count;

        public PreloadAssetManager(bool enableLogging = false)
        {
            _enableLogging = enableLogging;
            _config = PreloadConfiguration.CreateDefault();
        }

        #region Configuration

        /// <summary>
        /// Load default preload configuration
        /// </summary>
        public void LoadDefaultConfiguration()
        {
            _config = PreloadConfiguration.CreateDefault();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Loaded default preload configuration");
            }
        }

        /// <summary>
        /// Get current configuration
        /// </summary>
        public PreloadConfiguration GetConfiguration()
        {
            return _config;
        }

        /// <summary>
        /// Set preload configuration
        /// </summary>
        public void SetConfiguration(PreloadConfiguration config)
        {
            if (!config.IsValid())
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("ASSETS", "Invalid preload configuration provided");
                }
                return;
            }

            _config = config;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Preload configuration updated: {config.CriticalAssets.Count} critical assets");
            }
        }

        /// <summary>
        /// Get critical assets list
        /// </summary>
        public string[] GetCriticalAssets()
        {
            return _config.CriticalAssets.ToArray();
        }

        #endregion

        #region Asset Management

        /// <summary>
        /// Add asset to preload list
        /// </summary>
        public bool AddPreloadAsset(string address, PreloadPriority priority = PreloadPriority.Normal)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            var assetInfo = PreloadAssetInfo.Create(address, priority);
            _preloadAssets[address] = assetInfo;

            if (!_config.CriticalAssets.Contains(address))
            {
                _config.CriticalAssets.Add(address);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Added '{address}' to preload list with {priority} priority");
            }

            return true;
        }

        /// <summary>
        /// Remove asset from preload list
        /// </summary>
        public bool RemovePreloadAsset(string address)
        {
            if (_preloadAssets.Remove(address))
            {
                _config.CriticalAssets.Remove(address);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", $"Removed '{address}' from preload list");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all preload assets
        /// </summary>
        public void ClearPreloadAssets()
        {
            _preloadAssets.Clear();
            _config.CriticalAssets.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Cleared all preload assets");
            }
        }

        /// <summary>
        /// Get preload asset info
        /// </summary>
        public PreloadAssetInfo? GetAssetInfo(string address)
        {
            return _preloadAssets.TryGetValue(address, out var info) ? info : (PreloadAssetInfo?)null;
        }

        /// <summary>
        /// Get all preload assets by priority
        /// </summary>
        public Dictionary<PreloadPriority, List<string>> GetAssetsByPriority()
        {
            var assetsByPriority = new Dictionary<PreloadPriority, List<string>>();

            foreach (var priority in Enum.GetValues(typeof(PreloadPriority)).Cast<PreloadPriority>())
            {
                assetsByPriority[priority] = new List<string>();
            }

            foreach (var assetInfo in _preloadAssets.Values)
            {
                assetsByPriority[assetInfo.Priority].Add(assetInfo.Address);
            }

            return assetsByPriority;
        }

        #endregion

        #region Tracking

        /// <summary>
        /// Mark asset as preloaded
        /// </summary>
        public void MarkAssetPreloaded(string address)
        {
            _preloadedAssets.Add(address);
            _failedPreloads.Remove(address); // Remove from failed if was previously failed
        }

        /// <summary>
        /// Mark asset preload as failed
        /// </summary>
        public void MarkAssetFailed(string address)
        {
            _failedPreloads.Add(address);
        }

        /// <summary>
        /// Check if asset was preloaded
        /// </summary>
        public bool IsAssetPreloaded(string address)
        {
            return _preloadedAssets.Contains(address);
        }

        /// <summary>
        /// Check if asset preload failed
        /// </summary>
        public bool DidPreloadFail(string address)
        {
            return _failedPreloads.Contains(address);
        }

        /// <summary>
        /// Get all preloaded assets
        /// </summary>
        public HashSet<string> GetPreloadedAssets()
        {
            return new HashSet<string>(_preloadedAssets);
        }

        /// <summary>
        /// Get all failed preload assets
        /// </summary>
        public HashSet<string> GetFailedPreloads()
        {
            return new HashSet<string>(_failedPreloads);
        }

        /// <summary>
        /// Clear failed preloads list
        /// </summary>
        public void ClearFailedPreloads()
        {
            _failedPreloads.Clear();
        }

        /// <summary>
        /// Reset all tracking
        /// </summary>
        public void ResetTracking()
        {
            _preloadedAssets.Clear();
            _failedPreloads.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Reset preload tracking");
            }
        }

        #endregion

        #region Summary Generation

        /// <summary>
        /// Generate comprehensive preload summary
        /// </summary>
        public PreloadSummary GenerateSummary(bool isEnabled, bool isPreloading, PreloadProgress currentProgress, PreloadStats stats)
        {
            return new PreloadSummary
            {
                IsEnabled = isEnabled,
                IsPreloading = isPreloading,
                TotalPreloadAssets = _preloadAssets.Count,
                PreloadedAssets = _preloadedAssets.Count,
                FailedAssets = _failedPreloads.Count,
                CurrentProgress = currentProgress,
                Stats = stats,
                AssetsByPriority = GetAssetsByPriorityCount(),
                Configuration = _config
            };
        }

        /// <summary>
        /// Get asset count by priority
        /// </summary>
        private Dictionary<PreloadPriority, int> GetAssetsByPriorityCount()
        {
            var counts = new Dictionary<PreloadPriority, int>();

            foreach (var priority in Enum.GetValues(typeof(PreloadPriority)).Cast<PreloadPriority>())
            {
                counts[priority] = 0;
            }

            foreach (var assetInfo in _preloadAssets.Values)
            {
                counts[assetInfo.Priority]++;
            }

            return counts;
        }

        #endregion
    }
#endif
}

