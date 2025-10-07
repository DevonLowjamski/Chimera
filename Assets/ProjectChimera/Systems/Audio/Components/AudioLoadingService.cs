using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Audio.Components
{
    /// <summary>
    /// REAL ADDRESSABLES: Audio loading service for Project Chimera using genuine Addressables
    /// Provides async audio loading with proper memory management and caching
    /// </summary>
    public class AudioLoadingService : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private bool _enableAudio = true;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxCacheSize = 50;

        // Audio cache with proper management
        private readonly Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> _preloadedAudio = new HashSet<string>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for audio operations
        /// </summary>
        public event System.Action<string> OnAudioLoaded;
        public event System.Action<string, string> OnAudioLoadError;

        /// <summary>
        /// Critical audio clips to preload
        /// </summary>
        private readonly string[] CRITICAL_AUDIO = {
            "UIClick",
            "UIError",
            "UISuccess",
            "BackgroundAmbient"
        };

        #region Initialization

        private void Start()
        {
            InitializeAsync();
        }

        /// <summary>
        /// Initialize audio service with Addressables
        /// </summary>
        public async void InitializeAsync()
        {
            if (_isInitialized) return;

            // Wait for IAssetManager to be ready via ServiceContainer
            var serviceContainer = ServiceContainerFactory.Instance;
            var assetManager = serviceContainer.TryResolve<IAssetManager>();

            if (assetManager == null)
            {
                ChimeraLogger.LogError("AUDIO", "IAssetManager not available - audio service cannot initialize", this);
                return;
            }

            _isInitialized = true;

            // Preload critical audio clips
            await PreloadCriticalAudio();

            if (_enableLogging)
            {
                ChimeraLogger.Log("AUDIO", "AudioLoadingService initialized with Addressables", this);
            }
        }

        /// <summary>
        /// Preload critical audio clips for immediate availability
        /// </summary>
        private async Task PreloadCriticalAudio()
        {
            if (!_enableAudio) return;

            ChimeraLogger.Log("AUDIO", "Preloading critical audio clips...", this);
            int successCount = 0;

            foreach (var audioKey in CRITICAL_AUDIO)
            {
                try
                {
                    var clip = await LoadAudioClipAsync(audioKey);
                    if (clip != null)
                    {
                        _preloadedAudio.Add(audioKey);
                        successCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogWarning("AUDIO", $"Failed to preload critical audio '{audioKey}': {ex.Message}", this);
                }
            }

            ChimeraLogger.Log("AUDIO", $"Preloaded {successCount}/{CRITICAL_AUDIO.Length} critical audio clips", this);
        }

        #endregion

        #region Audio Loading (Async)

        /// <summary>
        /// Load audio clip asynchronously using Addressables
        /// </summary>
        public async Task<AudioClip> LoadAudioClipAsync(string clipName)
        {
            if (!_enableAudio || !_isInitialized || string.IsNullOrEmpty(clipName))
                return null;

            // Check cache first
            if (_enableCaching && _audioCache.TryGetValue(clipName, out var cachedClip))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("AUDIO", $"Audio cache hit: {clipName}", this);
                return cachedClip;
            }

            try
            {
                // Load via Addressables
                var serviceContainer = ServiceContainerFactory.Instance;
                var assetManager = serviceContainer.TryResolve<IAssetManager>();
                if (assetManager == null)
                {
                    ChimeraLogger.LogError("AUDIO", "IAssetManager not available", this);
                    return null;
                }

                var clip = await assetManager.LoadAssetAsync<AudioClip>(clipName);

                if (clip != null)
                {
                    // Cache the clip
                    if (_enableCaching)
                    {
                        CacheAudioClip(clipName, clip);
                    }

                    OnAudioLoaded?.Invoke(clipName);

                    if (_enableLogging)
                        ChimeraLogger.Log("AUDIO", $"Loaded audio clip: {clipName}", this);

                    return clip;
                }
                else
                {
                    var errorMsg = $"Failed to load audio clip: {clipName}";
                    ChimeraLogger.LogWarning("AUDIO", errorMsg, this);
                    OnAudioLoadError?.Invoke(clipName, errorMsg);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"Exception loading audio clip '{clipName}': {ex.Message}";
                ChimeraLogger.LogError("AUDIO", errorMsg, this);
                OnAudioLoadError?.Invoke(clipName, errorMsg);
                return null;
            }
        }

        /// <summary>
        /// Load multiple audio clips in batch
        /// </summary>
        public async Task<Dictionary<string, AudioClip>> LoadAudioClipBatchAsync(IList<string> clipNames)
        {
            var results = new Dictionary<string, AudioClip>();

            if (!_enableAudio || !_isInitialized)
                return results;

            var tasks = new List<Task<AudioClip>>();
            var names = new List<string>();

            foreach (var clipName in clipNames)
            {
                if (!string.IsNullOrEmpty(clipName))
                {
                    names.Add(clipName);
                    tasks.Add(LoadAudioClipAsync(clipName));
                }
            }

            var clips = await Task.WhenAll(tasks);

            for (int i = 0; i < names.Count; i++)
            {
                if (clips[i] != null)
                {
                    results[names[i]] = clips[i];
                }
            }

            ChimeraLogger.Log("AUDIO", $"Batch loaded {results.Count}/{clipNames.Count} audio clips", this);
            return results;
        }

        #endregion

        #region Audio Loading (Synchronous - Legacy)

        /// <summary>
        /// Load audio clip synchronously (legacy compatibility - not recommended)
        /// </summary>
        [System.Obsolete("Use LoadAudioClipAsync instead for better performance")]
        public AudioClip LoadAudioClip(string clipName)
        {
            if (!_enableAudio || !_isInitialized) return null;

            ChimeraLogger.LogWarning("AUDIO", $"Synchronous audio loading used for '{clipName}' - consider using async version", this);

            // Check cache first
            if (_enableCaching && _audioCache.TryGetValue(clipName, out var cachedClip))
            {
                return cachedClip;
            }

            // Use async version and wait (not ideal but provides compatibility)
            var task = LoadAudioClipAsync(clipName);
            task.Wait();
            return task.Result;
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Cache audio clip with size management
        /// </summary>
        private void CacheAudioClip(string clipName, AudioClip clip)
        {
            if (_audioCache.Count >= _maxCacheSize)
            {
                // Remove oldest non-preloaded clip
                string oldestKey = null;
                foreach (var key in _audioCache.Keys)
                {
                    if (!_preloadedAudio.Contains(key))
                    {
                        oldestKey = key;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(oldestKey))
                {
                    _audioCache.Remove(oldestKey);

                    // Release the asset from Addressables
                    var serviceContainer = ServiceContainerFactory.Instance;
                    var assetManager = serviceContainer.TryResolve<IAssetManager>();
                    assetManager?.UnloadAsset(oldestKey);
                }
            }

            _audioCache[clipName] = clip;
        }

        /// <summary>
        /// Check if audio clip exists in cache
        /// </summary>
        public bool HasAudioClip(string clipName)
        {
            return _audioCache.ContainsKey(clipName);
        }

        /// <summary>
        /// Clear audio cache (except preloaded clips)
        /// </summary>
        public void ClearCache()
        {
            var clipsToRemove = new List<string>();

            foreach (var clipName in _audioCache.Keys)
            {
                if (!_preloadedAudio.Contains(clipName))
                {
                    clipsToRemove.Add(clipName);
                }
            }

            var serviceContainer = ServiceContainerFactory.Instance;
            var assetManager = serviceContainer.TryResolve<IAssetManager>();

            foreach (var clipName in clipsToRemove)
            {
                _audioCache.Remove(clipName);
                assetManager?.UnloadAsset(clipName);
            }

            if (_enableLogging)
                ChimeraLogger.Log("AUDIO", $"Cleared {clipsToRemove.Count} audio clips from cache", this);
        }

        /// <summary>
        /// Release specific audio clip
        /// </summary>
        public void ReleaseAudioClip(string clipName)
        {
            if (_audioCache.ContainsKey(clipName) && !_preloadedAudio.Contains(clipName))
            {
                _audioCache.Remove(clipName);
                var serviceContainer = ServiceContainerFactory.Instance;
                var assetManager = serviceContainer.TryResolve<IAssetManager>();
                assetManager?.UnloadAsset(clipName);

                if (_enableLogging)
                    ChimeraLogger.Log("AUDIO", $"Released audio clip: {clipName}", this);
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get audio service statistics
        /// </summary>
        public AudioServiceStats GetStats()
        {
            return new AudioServiceStats
            {
                IsInitialized = _isInitialized,
                CachedClips = _audioCache.Count,
                MaxCacheSize = _maxCacheSize,
                PreloadedClips = _preloadedAudio.Count,
                CachingEnabled = _enableCaching
            };
        }

        /// <summary>
        /// Check if audio service is ready
        /// </summary>
        public bool IsReady()
        {
            var serviceContainer = ServiceContainerFactory.Instance;
            var assetManager = serviceContainer.TryResolve<IAssetManager>();
            return _isInitialized && _enableAudio && assetManager != null;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // Release all non-preloaded audio clips
            ClearCache();

            if (_enableLogging)
                ChimeraLogger.Log("AUDIO", "AudioLoadingService cleanup completed", this);
        }

        #endregion
    }

    /// <summary>
    /// Audio service statistics
    /// </summary>
    [System.Serializable]
    public struct AudioServiceStats
    {
        public bool IsInitialized;
        public int CachedClips;
        public int MaxCacheSize;
        public int PreloadedClips;
        public bool CachingEnabled;
    }
}