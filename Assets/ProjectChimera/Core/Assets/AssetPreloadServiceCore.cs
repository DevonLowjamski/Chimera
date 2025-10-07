using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using ProjectChimera.Core.Logging;
using System;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// REFACTORED: Asset Preload Service - Core Operations (POCO - Unity-independent)
    /// Part 1 of 2: Initialization, queue management, and basic preloading
    /// Kept under 500 lines per clean architecture guidelines
    /// </summary>
    public partial class AssetPreloadService
    {
        // Configuration
        private readonly bool _enableLogging;
        private readonly bool _enablePreloading;
        private readonly bool _preloadOnStartup;
        private readonly float _preloadTimeoutSeconds;
        private readonly PreloadStrategy _preloadStrategy;
        private readonly int _maxConcurrentPreloads;
        private readonly bool _continueOnFailure;
        private readonly string[] _criticalAssets;
        private readonly string[] _commonAssets;

        // Preload tracking
        private readonly HashSet<string> _preloadedAssets = new HashSet<string>();
        private readonly Dictionary<string, object> _preloadedAssetCache = new Dictionary<string, object>();
        private readonly Dictionary<string, AssetPreloadResult> _preloadResults = new Dictionary<string, AssetPreloadResult>();

        // Progress tracking
        private readonly Queue<PreloadRequest> _preloadQueue = new Queue<PreloadRequest>();
        private readonly HashSet<string> _currentlyPreloading = new HashSet<string>();
        private int _activePreloadTasks = 0;

        // Statistics
        private AssetPreloaderStats _stats = new AssetPreloaderStats();

        // State tracking
        private bool _isInitialized = false;
        private bool _preloadingInProgress = false;

        // Asset loading reference
        private IAssetManager _assetManager;

        // Events
        public event Action<string> OnAssetPreloaded;
        public event Action<string, string> OnAssetPreloadFailed;
        public event Action<PreloadProgressStatus> OnPreloadProgressChanged;
        public event Action<PreloadSession> OnPreloadSessionCompleted;

        public bool IsInitialized => _isInitialized;
        public bool IsPreloadingEnabled => _enablePreloading;
        public bool IsPreloadingInProgress => _preloadingInProgress;
        public AssetPreloaderStats Stats => _stats;
        public int PreloadedAssetCount => _preloadedAssets.Count;

        public AssetPreloadService(
            bool enableLogging = false,
            bool enablePreloading = true,
            bool preloadOnStartup = true,
            float preloadTimeoutSeconds = 30f,
            PreloadStrategy preloadStrategy = PreloadStrategy.Critical,
            int maxConcurrentPreloads = 5,
            bool continueOnFailure = true,
            string[] criticalAssets = null,
            string[] commonAssets = null)
        {
            _enableLogging = enableLogging;
            _enablePreloading = enablePreloading;
            _preloadOnStartup = preloadOnStartup;
            _preloadTimeoutSeconds = preloadTimeoutSeconds;
            _preloadStrategy = preloadStrategy;
            _maxConcurrentPreloads = maxConcurrentPreloads;
            _continueOnFailure = continueOnFailure;
            _criticalAssets = criticalAssets ?? new[] { "CoreUI", "DefaultPlantStrain", "BasicConstructionPrefab", "ErrorAudio" };
            _commonAssets = commonAssets ?? new[] { "LoadingSpinner", "DefaultMaterial", "NotificationSound" };
        }

        public void Initialize(IAssetManager assetManager)
        {
            if (_isInitialized) return;

            _assetManager = assetManager;

            _preloadedAssets.Clear();
            _preloadedAssetCache.Clear();
            _preloadResults.Clear();
            _preloadQueue.Clear();
            _currentlyPreloading.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Asset Preload Service initialized");
            }
        }

        /// <summary>
        /// Start preloading assets based on strategy
        /// </summary>
        public async Task<PreloadSession> StartPreloadingAsync(float currentTime, float realtimeSinceStartup)
        {
            if (!_isInitialized || !_enablePreloading || _preloadingInProgress)
            {
                return new PreloadSession { Success = false, Message = "Preloading not available" };
            }

            _preloadingInProgress = true;
            var sessionStartTime = realtimeSinceStartup;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Starting asset preloading with {_preloadStrategy} strategy");
            }

            try
            {
                var assetsToPreload = GetAssetsToPreload();
                var session = new PreloadSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    Strategy = _preloadStrategy,
                    TotalAssets = assetsToPreload.Count,
                    StartTime = currentTime
                };

                // Queue all preload requests
                foreach (var asset in assetsToPreload)
                {
                    QueuePreloadRequest(asset);
                }

                // Process preload queue
                var results = await ProcessPreloadQueue(session);

                // Complete session
                var sessionTime = realtimeSinceStartup - sessionStartTime;
                session.CompletionTime = currentTime;
                session.Duration = sessionTime;
                session.Success = results.SuccessCount > 0;
                session.SuccessfulAssets = results.SuccessCount;
                session.FailedAssets = results.FailureCount;
                session.Message = $"Preloaded {results.SuccessCount}/{assetsToPreload.Count} assets in {sessionTime:F2}s";

                UpdatePreloadStats(session);
                OnPreloadSessionCompleted?.Invoke(session);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", session.Message);
                }

                return session;
            }
            catch (Exception ex)
            {
                var errorSession = new PreloadSession
                {
                    Success = false,
                    Message = $"Preload error: {ex.Message}",
                    Duration = realtimeSinceStartup - sessionStartTime
                };

                _stats.PreloadErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Preloading failed: {ex.Message}");
                }

                return errorSession;
            }
            finally
            {
                _preloadingInProgress = false;
            }
        }

        public async Task<AssetPreloadResult[]> PreloadAssetsAsync(string[] assetAddresses)
        {
            if (!_isInitialized || !_enablePreloading)
                return new AssetPreloadResult[0];
            var results = new List<AssetPreloadResult>();
            foreach (var address in assetAddresses)
            {
                var result = await PreloadSingleAssetAsync(address);
                results.Add(result);
            }
            return results.ToArray();
        }

        public bool IsAssetPreloaded(string address) => _preloadedAssets.Contains(address);

        public T GetPreloadedAsset<T>(string address) where T : class
        {
            if (_preloadedAssetCache.TryGetValue(address, out var asset))
                return asset as T;
            return null;
        }

        public void ClearPreloadedAssets()
        {
            _preloadedAssets.Clear();
            _preloadedAssetCache.Clear();
            _preloadResults.Clear();
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", "Cleared all preloaded assets");
        }

        #region Private Methods

        private List<string> GetAssetsToPreload()
        {
            var assetsToPreload = new List<string>();

            switch (_preloadStrategy)
            {
                case PreloadStrategy.Critical:
                    assetsToPreload.AddRange(_criticalAssets);
                    break;

                case PreloadStrategy.CriticalAndCommon:
                    assetsToPreload.AddRange(_criticalAssets);
                    assetsToPreload.AddRange(_commonAssets);
                    break;

                case PreloadStrategy.All:
                    assetsToPreload.AddRange(_criticalAssets);
                    assetsToPreload.AddRange(_commonAssets);
                    break;

                case PreloadStrategy.None:
                default:
                    break;
            }

            return assetsToPreload.Distinct().ToList();
        }

        private void QueuePreloadRequest(string address)
        {
            if (!_currentlyPreloading.Contains(address) && !_preloadedAssets.Contains(address))
            {
                _preloadQueue.Enqueue(new PreloadRequest
                {
                    Address = address,
                    Priority = _criticalAssets.Contains(address) ? 1 : 0
                });
            }
        }

        private async Task<PreloadQueueResult> ProcessPreloadQueue(PreloadSession session)
        {
            int successCount = 0;
            int failureCount = 0;
            int processedCount = 0;

            var tasks = new List<Task<AssetPreloadResult>>();

            while (_preloadQueue.Count > 0 || _activePreloadTasks > 0)
            {
                // Start new preload tasks up to concurrency limit
                while (_preloadQueue.Count > 0 && _activePreloadTasks < _maxConcurrentPreloads)
                {
                    var request = _preloadQueue.Dequeue();
                    _activePreloadTasks++;
                    tasks.Add(PreloadSingleAssetAsync(request.Address));
                }

                // Wait for any task to complete
                if (tasks.Count > 0)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    var result = await completedTask;
                    tasks.Remove(completedTask);
                    _activePreloadTasks--;

                    processedCount++;

                    if (result.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        if (!_continueOnFailure)
                        {
                            break;
                        }
                    }

                    // Report progress
                    var progress = new PreloadProgressStatus
                    {
                        SessionId = session.SessionId,
                        ProcessedAssets = processedCount,
                        TotalAssets = session.TotalAssets,
                        CurrentAsset = result.Address,
                        PercentComplete = (float)processedCount / session.TotalAssets * 100f
                    };

                    OnPreloadProgressChanged?.Invoke(progress);
                }
            }

            // Wait for remaining tasks
            if (tasks.Count > 0)
            {
                var remainingResults = await Task.WhenAll(tasks);
                foreach (var result in remainingResults)
                {
                    _activePreloadTasks--;
                    if (result.Success) successCount++;
                    else failureCount++;
                }
            }

            return new PreloadQueueResult
            {
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }

        private async Task<AssetPreloadResult> PreloadSingleAssetAsync(string address)
        {
            if (_preloadedAssets.Contains(address))
            {
                return new AssetPreloadResult
                {
                    Address = address,
                    Success = true,
                    Message = "Already preloaded"
                };
            }

            _currentlyPreloading.Add(address);
            var startTime = DateTime.Now;

            try
            {
                if (_assetManager == null)
                {
                    throw new Exception("Asset manager not initialized");
                }

                // Load asset with timeout
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_preloadTimeoutSeconds)))
                {
                    var asset = await _assetManager.LoadAssetAsync<UnityEngine.Object>(address, cts.Token);

                    if (asset != null)
                    {
                        _preloadedAssets.Add(address);
                        _preloadedAssetCache[address] = asset;
                        _stats.AssetsPreloaded++;

                        var result = new AssetPreloadResult
                        {
                            Address = address,
                            Success = true,
                            LoadTime = (float)(DateTime.Now - startTime).TotalSeconds,
                            Message = "Preloaded successfully"
                        };

                        _preloadResults[address] = result;
                        OnAssetPreloaded?.Invoke(address);

                        if (_enableLogging)
                        {
                            ChimeraLogger.Log("ASSETS", $"Preloaded: {address} ({result.LoadTime:F2}s)");
                        }

                        return result;
                    }
                    else
                    {
                        throw new Exception("Asset load returned null");
                    }
                }
            }
            catch (Exception ex)
            {
                _stats.PreloadErrors++;

                var result = new AssetPreloadResult
                {
                    Address = address,
                    Success = false,
                    LoadTime = (float)(DateTime.Now - startTime).TotalSeconds,
                    Message = ex.Message
                };

                _preloadResults[address] = result;
                OnAssetPreloadFailed?.Invoke(address, ex.Message);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("ASSETS", $"Failed to preload {address}: {ex.Message}");
                }

                return result;
            }
            finally
            {
                _currentlyPreloading.Remove(address);
            }
        }

        private void UpdatePreloadStats(PreloadSession session)
        {
            _stats.PreloadSessions++;
            if (session.Success) _stats.SuccessfulSessions++;
            else _stats.FailedSessions++;
            _stats.TotalPreloadTime += session.Duration;
            _stats.AveragePreloadTime = _stats.TotalPreloadTime / _stats.PreloadSessions;
            if (session.Duration < _stats.MinPreloadTime || _stats.MinPreloadTime == 0)
                _stats.MinPreloadTime = session.Duration;
            if (session.Duration > _stats.MaxPreloadTime)
                _stats.MaxPreloadTime = session.Duration;
        }

        private void ResetStats()
        {
            _stats = new AssetPreloaderStats();
        }

        #endregion
    }

    public enum PreloadStrategy { None, Critical, CriticalAndCommon, All }

    [Serializable]
    public struct PreloadRequest
    {
        public string Address;
        public int Priority;
    }

    [Serializable]
    public struct AssetPreloadResult
    {
        public string Address;
        public bool Success;
        public float LoadTime;
        public string Message;
    }

    [Serializable]
    public struct PreloadSession
    {
        public string SessionId;
        public PreloadStrategy Strategy;
        public int TotalAssets;
        public int SuccessfulAssets;
        public int FailedAssets;
        public float StartTime;
        public float CompletionTime;
        public float Duration;
        public bool Success;
        public string Message;
    }

    [Serializable]
    public struct PreloadProgressStatus
    {
        public string SessionId;
        public int ProcessedAssets;
        public int TotalAssets;
        public string CurrentAsset;
        public float PercentComplete;
    }

    [Serializable]
    public struct PreloadQueueResult
    {
        public int SuccessCount;
        public int FailureCount;
    }

    [Serializable]
    public struct AssetPreloaderStats
    {
        public int PreloadSessions;
        public int SuccessfulSessions;
        public int FailedSessions;
        public int AssetsPreloaded;
        public int PreloadErrors;
        public float TotalPreloadTime;
        public float AveragePreloadTime;
        public float MinPreloadTime;
        public float MaxPreloadTime;
    }
}
