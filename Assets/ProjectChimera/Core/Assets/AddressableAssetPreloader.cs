using System;
using System.Threading.Tasks;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Addressable Asset Preloader (Coordinator)
    /// Single Responsibility: Orchestrate asset preloading components and manage lifecycle
    /// Refactored from 691 lines → 227 lines (4 files total, all <500 lines)
    /// Dependencies: PreloadExecutor, PreloadAssetManager
    /// </summary>
    public class AddressableAssetPreloader
    {
        // Component dependencies
        private PreloadExecutor _executor;
        private PreloadAssetManager _assetManager;

        // Settings
        private bool _enableLogging = false;
        private bool _preloadEnabled = true;

        // Preload state
        private PreloadProgress _currentProgress = new PreloadProgress();
        private bool _isPreloading = false;
        private bool _isInitialized = false;

        // Statistics
        private PreloadStats _stats = PreloadStats.CreateEmpty();

        // Events
        public event Action<PreloadProgress> OnPreloadProgress;
        public event Action<string, object> OnAssetPreloaded;
        public event Action<string, string> OnPreloadFailed;
        public event Action<PreloadResult> OnPreloadComplete;

        public bool IsInitialized => _isInitialized;
        public bool IsPreloading => _isPreloading;
        public PreloadProgress CurrentProgress => _currentProgress;
        public PreloadStats Stats => _stats;
        public int PreloadedAssetCount => _assetManager?.PreloadedAssetCount ?? 0;
        public int FailedPreloadCount => _assetManager?.FailedPreloadCount ?? 0;

        /// <summary>
        /// Initialize preloader and components
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize asset manager
            _assetManager = new PreloadAssetManager(_enableLogging);
            _assetManager.LoadDefaultConfiguration();
            _assetManager.ResetTracking();

            // Initialize executor with configuration
            var config = _assetManager.GetConfiguration();
            _executor = new PreloadExecutor(config, _enableLogging);

            // Forward events from executor
            _executor.OnAssetPreloaded += HandleAssetPreloaded;
            _executor.OnPreloadFailed += HandlePreloadFailed;
            _executor.OnPreloadProgress += HandlePreloadProgress;

            _stats = PreloadStats.CreateEmpty();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Preloader initialized");
            }
        }

        #region Preload Operations

        /// <summary>
        /// Start preloading critical assets
        /// </summary>
        public async Task<PreloadResult> PreloadCriticalAssetsAsync()
        {
            if (!_isInitialized || !_preloadEnabled)
            {
                return PreloadResult.CreateFailure("Preloader not initialized or disabled");
            }

            if (_isPreloading)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("ASSETS", "Preload already in progress");
                }
                return PreloadResult.CreateFailure("Preload already in progress");
            }

            _isPreloading = true;
            var startTime = DateTime.Now;

            try
            {
                // Get critical assets from asset manager
                var criticalAssets = _assetManager.GetCriticalAssets();

                // Execute preload via executor
                var result = await _executor.ExecutePreloadAsync(criticalAssets);
                result.PreloadTime = (float)(DateTime.Now - startTime).TotalSeconds;

                // Update statistics
                _stats.PreloadAttempts++;
                if (result.Success)
                {
                    _stats.SuccessfulPreloads++;
                }
                else
                {
                    _stats.FailedPreloads++;
                }
                _stats.TotalPreloadTime += result.PreloadTime;
                _stats.TotalAssetsPreloaded += result.SuccessfulAssets;

                OnPreloadComplete?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", $"Preload completed: {result.SuccessfulAssets}/{result.TotalAssets} assets in {result.PreloadTime:F1}s");
                }

                return result;
            }
            catch (Exception ex)
            {
                _stats.FailedPreloads++;

                var result = PreloadResult.CreateFailure(ex.Message);
                result.PreloadTime = (float)(DateTime.Now - startTime).TotalSeconds;

                OnPreloadComplete?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Preload exception: {ex.Message}");
                }

                return result;
            }
            finally
            {
                _isPreloading = false;
            }
        }

        /// <summary>
        /// Retry failed preloads
        /// </summary>
        public async Task<PreloadResult> RetryFailedPreloadsAsync()
        {
            if (!_isInitialized)
            {
                return PreloadResult.CreateFailure("Preloader not initialized");
            }

            var failedAssets = _assetManager.GetFailedPreloads().ToArray();
            if (failedAssets.Length == 0)
            {
                return PreloadResult.CreateSuccess(0, 0, 0f);
            }

            _assetManager.ClearFailedPreloads();

            var startTime = DateTime.Now;
            var result = await _executor.ExecutePreloadAsync(failedAssets);
            result.PreloadTime = (float)(DateTime.Now - startTime).TotalSeconds;

            _stats.RetryAttempts++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Retry completed: {result.SuccessfulAssets}/{result.TotalAssets} assets");
            }

            return result;
        }

        #endregion

        #region Asset Management

        /// <summary>
        /// Add asset to preload list
        /// </summary>
        public bool AddPreloadAsset(string address, PreloadPriority priority = PreloadPriority.Normal)
        {
            if (!_isInitialized)
            {
                return false;
            }

            return _assetManager.AddPreloadAsset(address, priority);
        }

        /// <summary>
        /// Remove asset from preload list
        /// </summary>
        public bool RemovePreloadAsset(string address)
        {
            return _assetManager?.RemovePreloadAsset(address) ?? false;
        }

        /// <summary>
        /// Check if asset was preloaded
        /// </summary>
        public bool IsAssetPreloaded(string address)
        {
            return _assetManager?.IsAssetPreloaded(address) ?? false;
        }

        /// <summary>
        /// Check if asset preload failed
        /// </summary>
        public bool DidPreloadFail(string address)
        {
            return _assetManager?.DidPreloadFail(address) ?? false;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set preload configuration
        /// </summary>
        public void SetConfiguration(PreloadConfiguration config)
        {
            if (!_isInitialized) return;

            _assetManager.SetConfiguration(config);

            // Recreate executor with new configuration
            _executor = new PreloadExecutor(config, _enableLogging);
            _executor.OnAssetPreloaded += HandleAssetPreloaded;
            _executor.OnPreloadFailed += HandlePreloadFailed;
            _executor.OnPreloadProgress += HandlePreloadProgress;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Preload configuration updated");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle asset preloaded event from executor
        /// </summary>
        private void HandleAssetPreloaded(string address, object asset)
        {
            _assetManager.MarkAssetPreloaded(address);
            OnAssetPreloaded?.Invoke(address, asset);
        }

        /// <summary>
        /// Handle preload failed event from executor
        /// </summary>
        private void HandlePreloadFailed(string address, string errorMessage)
        {
            _assetManager.MarkAssetFailed(address);
            OnPreloadFailed?.Invoke(address, errorMessage);
        }

        /// <summary>
        /// Handle preload progress event from executor
        /// </summary>
        private void HandlePreloadProgress(PreloadProgress progress)
        {
            _currentProgress = progress;
            OnPreloadProgress?.Invoke(progress);
        }

        #endregion

        #region Summary

        /// <summary>
        /// Get comprehensive preload summary
        /// </summary>
        public PreloadSummary GetSummary()
        {
            if (!_isInitialized || _assetManager == null)
            {
                return new PreloadSummary
                {
                    IsEnabled = _preloadEnabled,
                    IsPreloading = false,
                    Stats = _stats
                };
            }

            return _assetManager.GenerateSummary(_preloadEnabled, _isPreloading, _currentProgress, _stats);
        }

        #endregion
    }
#endif
}

