using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Preload Executor
    /// Single Responsibility: Execute asset preloading with parallel/sequential strategies
    /// Extracted from AddressableAssetPreloader (691 lines → 4 files <500 lines each)
    /// </summary>
    public class PreloadExecutor
    {
        private readonly int _maxConcurrentPreloads;
        private readonly float _preloadTimeoutSeconds;
        private readonly bool _preloadInParallel;
        private readonly bool _enableLogging;

        public event Action<string, object> OnAssetPreloaded;
        public event Action<string, string> OnPreloadFailed;
        public event Action<PreloadProgress> OnPreloadProgress;

        public PreloadExecutor(PreloadConfiguration config, bool enableLogging = false)
        {
            _maxConcurrentPreloads = config.MaxConcurrentPreloads;
            _preloadTimeoutSeconds = config.TimeoutSeconds;
            _preloadInParallel = config.PreloadInParallel;
            _enableLogging = enableLogging;
        }

        #region Preload Execution

        /// <summary>
        /// Execute preload operation on asset list
        /// </summary>
        public async Task<PreloadResult> ExecutePreloadAsync(string[] assets)
        {
            if (assets == null || assets.Length == 0)
            {
                return PreloadResult.CreateSuccess(0, 0, 0f);
            }

            var startTime = DateTime.Now;
            var failures = new List<PreloadFailure>();
            int successfulAssets;

            // Report initial progress
            var progress = PreloadProgress.Create(assets.Length);
            OnPreloadProgress?.Invoke(progress);

            // Execute preload strategy
            if (_preloadInParallel)
            {
                successfulAssets = await PreloadAssetsParallel(assets, failures);
            }
            else
            {
                successfulAssets = await PreloadAssetsSequential(assets, failures);
            }

            var preloadTime = (float)(DateTime.Now - startTime).TotalSeconds;

            return new PreloadResult
            {
                Success = failures.Count == 0,
                TotalAssets = assets.Length,
                SuccessfulAssets = successfulAssets,
                FailedAssets = failures.Count,
                Failures = failures,
                PreloadTime = preloadTime
            };
        }

        #endregion

        #region Parallel Loading

        /// <summary>
        /// Preload assets in parallel with concurrency limit
        /// </summary>
        private async Task<int> PreloadAssetsParallel(string[] assets, List<PreloadFailure> failures)
        {
            var semaphore = new SemaphoreSlim(_maxConcurrentPreloads);
            var tasks = new List<Task<bool>>();

            foreach (var assetAddress in assets)
            {
                tasks.Add(PreloadSingleAssetAsync(assetAddress, semaphore, failures));
            }

            var results = await Task.WhenAll(tasks);
            return results.Count(r => r);
        }

        #endregion

        #region Sequential Loading

        /// <summary>
        /// Preload assets sequentially with progress tracking
        /// </summary>
        private async Task<int> PreloadAssetsSequential(string[] assets, List<PreloadFailure> failures)
        {
            var successCount = 0;

            for (int i = 0; i < assets.Length; i++)
            {
                var assetAddress = assets[i];

                // Report progress
                var progress = new PreloadProgress
                {
                    TotalAssets = assets.Length,
                    CompletedAssets = i,
                    CurrentAsset = assetAddress,
                    OverallProgress = (float)i / assets.Length
                };

                OnPreloadProgress?.Invoke(progress);

                if (await PreloadSingleAssetAsync(assetAddress, null, failures))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        #endregion

        #region Single Asset Loading

        /// <summary>
        /// Preload single asset with timeout and error handling
        /// </summary>
        private async Task<bool> PreloadSingleAssetAsync(string address, SemaphoreSlim semaphore, List<PreloadFailure> failures)
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync();
            }

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_preloadTimeoutSeconds)))
                {
                    var loadTask = LoadAssetForPreloadAsync(address);
                    var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);

                    var completedTask = await Task.WhenAny(loadTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        // Timeout occurred
                        var failure = PreloadFailure.Create(address, $"Timeout after {_preloadTimeoutSeconds}s");
                        failures.Add(failure);

                        OnPreloadFailed?.Invoke(address, "Timeout");

                        if (_enableLogging)
                        {
                            ChimeraLogger.LogWarning("ASSETS", $"Preload timeout for '{address}'");
                        }

                        return false;
                    }

                    var asset = await loadTask;
                    if (asset != null)
                    {
                        OnAssetPreloaded?.Invoke(address, asset);

                        if (_enableLogging)
                        {
                            ChimeraLogger.Log("ASSETS", $"Preloaded critical asset: {address}");
                        }

                        return true;
                    }
                    else
                    {
                        var failure = PreloadFailure.Create(address, "Asset load returned null");
                        failures.Add(failure);

                        OnPreloadFailed?.Invoke(address, "Load failed");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                var failure = PreloadFailure.Create(address, ex.Message);
                failures.Add(failure);

                OnPreloadFailed?.Invoke(address, ex.Message);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Exception preloading '{address}': {ex.Message}");
                }

                return false;
            }
            finally
            {
                semaphore?.Release();
            }
        }

        /// <summary>
        /// Load asset specifically for preloading using Addressables
        /// </summary>
        private async Task<UnityEngine.Object> LoadAssetForPreloadAsync(string address)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    var errorMessage = handle.OperationException?.Message ?? "Unknown error";
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("ASSETS", $"Failed to preload '{address}': {errorMessage}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Exception loading '{address}': {ex.Message}");
                }
                return null;
            }
        }

        #endregion
    }
#endif
}

