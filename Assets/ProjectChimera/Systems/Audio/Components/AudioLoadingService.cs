using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Audio.Components
{
    /// <summary>
    /// Handles audio clip loading, caching, and resource management for Project Chimera's cannabis cultivation game.
    /// Manages audio libraries, soundscape resources, and music playlist loading for optimal performance.
    /// </summary>
    public class AudioLoadingService : MonoBehaviour
    {
        [Header("Loading Configuration")]
        [SerializeField] private bool _enablePreloading = true;
        [SerializeField] private bool _enableAsyncLoading = true;
        [SerializeField] private int _maxConcurrentLoads = 3;
        [SerializeField] private bool _enableDebugLogging = false;

        // Audio Libraries
        private AudioLibrarySO _audioLibrary;
        private SoundscapeLibrarySO _soundscapeLibrary;
        private MusicPlaylistSO _musicPlaylist;

        // Loading state
        private Dictionary<string, AudioClip> _loadedClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, DynamicSoundscape> _loadedSoundscapes = new Dictionary<string, DynamicSoundscape>();
        private HashSet<string> _loadingClips = new HashSet<string>();
        private Queue<AudioLoadRequest> _loadQueue = new Queue<AudioLoadRequest>();
        private int _activeLoadOperations = 0;

        // Events
        public System.Action<string, AudioClip> OnAudioClipLoaded;
        public System.Action<string, DynamicSoundscape> OnSoundscapeLoaded;
        public System.Action<string> OnAudioLoadFailed;
        public System.Action<float> OnLoadingProgress;

        // Properties
        public int LoadedClipsCount => _loadedClips.Count;
        public int LoadedSoundscapesCount => _loadedSoundscapes.Count;
        public bool IsLoading => _activeLoadOperations > 0 || _loadQueue.Count > 0;
        public float LoadingProgress { get; private set; }

        public void Initialize(AudioLibrarySO audioLibrary, SoundscapeLibrarySO soundscapeLibrary, MusicPlaylistSO musicPlaylist)
        {
            _audioLibrary = audioLibrary;
            _soundscapeLibrary = soundscapeLibrary;
            _musicPlaylist = musicPlaylist;

            if (_enablePreloading)
            {
                StartCoroutine(PreloadEssentialAudio());
            }

            LogInfo("Audio loading service initialized for cannabis cultivation game");
        }

        #region Audio Clip Loading

        public AudioClip GetAudioClip(string clipId)
        {
            // Return cached clip if available
            if (_loadedClips.TryGetValue(clipId, out var cachedClip))
            {
                return cachedClip;
            }

            // Try to get from library directly
            var clip = _audioLibrary?.GetAudioClip(clipId);
            if (clip != null)
            {
                _loadedClips[clipId] = clip;
                return clip;
            }

            // Queue for loading if not already queued
            if (!_loadingClips.Contains(clipId))
            {
                QueueAudioLoad(clipId, AudioLoadType.Clip);
            }

            return null;
        }

        public AudioClip GetUISound(string soundId)
        {
            // Check cache first
            string uiKey = $"ui_{soundId}";
            if (_loadedClips.TryGetValue(uiKey, out var cachedClip))
            {
                return cachedClip;
            }

            // Try to get from library
            var clip = _audioLibrary?.GetUISound(soundId);
            if (clip != null)
            {
                _loadedClips[uiKey] = clip;
                return clip;
            }

            // Queue for loading
            if (!_loadingClips.Contains(uiKey))
            {
                QueueAudioLoad(soundId, AudioLoadType.UISound);
            }

            return null;
        }

        public void PreloadAudioClip(string clipId)
        {
            if (!_loadedClips.ContainsKey(clipId) && !_loadingClips.Contains(clipId))
            {
                QueueAudioLoad(clipId, AudioLoadType.Clip);
            }
        }

        public void UnloadAudioClip(string clipId)
        {
            if (_loadedClips.TryGetValue(clipId, out var clip))
            {
                if (clip != null)
                {
                    Resources.UnloadAsset(clip);
                }
                _loadedClips.Remove(clipId);
                LogInfo($"Unloaded audio clip: {clipId}");
            }
        }

        #endregion

        #region Soundscape Loading

        public DynamicSoundscape GetSoundscape(string soundscapeId)
        {
            // Return cached soundscape if available
            if (_loadedSoundscapes.TryGetValue(soundscapeId, out var cachedSoundscape))
            {
                return cachedSoundscape;
            }

            // Try to get from library directly
            var soundscape = _soundscapeLibrary?.GetSoundscape(soundscapeId);
            if (soundscape != null)
            {
                _loadedSoundscapes[soundscapeId] = soundscape;
                PreloadSoundscapeLayers(soundscape);
                return soundscape;
            }

            // Queue for loading
            if (!_loadingClips.Contains(soundscapeId))
            {
                QueueAudioLoad(soundscapeId, AudioLoadType.Soundscape);
            }

            return null;
        }

        private void PreloadSoundscapeLayers(DynamicSoundscape soundscape)
        {
            if (soundscape?.Layers == null) return;

            foreach (var layer in soundscape.Layers)
            {
                if (layer.AudioClip != null)
                {
                    string layerKey = $"soundscape_{layer.LayerType}_{soundscape.SoundscapeName}";
                    if (!_loadedClips.ContainsKey(layerKey))
                    {
                        _loadedClips[layerKey] = layer.AudioClip;
                    }
                }
            }
        }

        public void PreloadSoundscape(string soundscapeId)
        {
            if (!_loadedSoundscapes.ContainsKey(soundscapeId) && !_loadingClips.Contains(soundscapeId))
            {
                QueueAudioLoad(soundscapeId, AudioLoadType.Soundscape);
            }
        }

        #endregion

        #region Music Loading

        public AudioClip GetMusicClip(string trackId)
        {
            // Check cache first
            string musicKey = $"music_{trackId}";
            if (_loadedClips.TryGetValue(musicKey, out var cachedTrack))
            {
                return cachedTrack;
            }

            // Try to get from playlist
            var track = _musicPlaylist?.GetTrack(trackId);
            if (track != null)
            {
                _loadedClips[musicKey] = track.AudioClip;
                return track.AudioClip;
            }

            // Queue for loading
            if (!_loadingClips.Contains(musicKey))
            {
                QueueAudioLoad(trackId, AudioLoadType.Music);
            }

            return null;
        }

        public MusicTrack GetMusicTrack(string trackId)
        {
            // Try to get from playlist
            var track = _musicPlaylist?.GetTrack(trackId);
            if (track != null)
            {
                // Ensure the clip is cached
                string musicKey = $"music_{trackId}";
                if (!_loadedClips.ContainsKey(musicKey))
                {
                    _loadedClips[musicKey] = track.AudioClip;
                }
                return track;
            }

            // Queue for loading if not found
            string cacheKey = $"music_{trackId}";
            if (!_loadingClips.Contains(cacheKey))
            {
                QueueAudioLoad(trackId, AudioLoadType.Music);
            }

            return null;
        }

        public void PreloadMusicTrack(string trackId)
        {
            string musicKey = $"music_{trackId}";
            if (!_loadedClips.ContainsKey(musicKey) && !_loadingClips.Contains(musicKey))
            {
                QueueAudioLoad(trackId, AudioLoadType.Music);
            }
        }

        #endregion

        #region Loading Queue Management

        private void QueueAudioLoad(string resourceId, AudioLoadType loadType)
        {
            var loadRequest = new AudioLoadRequest
            {
                ResourceId = resourceId,
                LoadType = loadType,
                Priority = GetLoadPriority(loadType),
                QueueTime = Time.time
            };

            _loadQueue.Enqueue(loadRequest);
            _loadingClips.Add(GetCacheKey(resourceId, loadType));

            // Start processing if not already running
            if (_enableAsyncLoading && _activeLoadOperations < _maxConcurrentLoads)
            {
                StartCoroutine(ProcessLoadQueue());
            }
        }

        private IEnumerator ProcessLoadQueue()
        {
            while (_loadQueue.Count > 0 && _activeLoadOperations < _maxConcurrentLoads)
            {
                var request = _loadQueue.Dequeue();
                _activeLoadOperations++;

                yield return StartCoroutine(LoadAudioResource(request));

                _activeLoadOperations--;
                
                // Update loading progress
                UpdateLoadingProgress();
            }
        }

        private IEnumerator LoadAudioResource(AudioLoadRequest request)
        {
            string cacheKey = GetCacheKey(request.ResourceId, request.LoadType);
            
            yield return new WaitForEndOfFrame(); // Simulate async loading delay

            IEnumerator loadOperation = null;
            switch (request.LoadType)
            {
                case AudioLoadType.Clip:
                    loadOperation = LoadAudioClipAsync(request.ResourceId);
                    break;
                case AudioLoadType.UISound:
                    loadOperation = LoadUISoundAsync(request.ResourceId);
                    break;
                case AudioLoadType.Soundscape:
                    loadOperation = LoadSoundscapeAsync(request.ResourceId);
                    break;
                case AudioLoadType.Music:
                    loadOperation = LoadMusicTrackAsync(request.ResourceId);
                    break;
            }

            if (loadOperation != null)
            {
                yield return loadOperation;
            }

            try
            {
                _loadingClips.Remove(cacheKey);
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to load audio resource {request.ResourceId}: {ex.Message}");
                OnAudioLoadFailed?.Invoke(request.ResourceId);
            }
        }

        private IEnumerator LoadAudioClipAsync(string clipId)
        {
            var clip = _audioLibrary?.GetAudioClip(clipId);
            
            yield return null; // Simulate loading time
            
            if (clip != null)
            {
                _loadedClips[clipId] = clip;
                OnAudioClipLoaded?.Invoke(clipId, clip);
                LogInfo($"Loaded audio clip: {clipId}");
            }
        }

        private IEnumerator LoadUISoundAsync(string soundId)
        {
            var clip = _audioLibrary?.GetUISound(soundId);
            
            yield return null;
            
            if (clip != null)
            {
                string uiKey = $"ui_{soundId}";
                _loadedClips[uiKey] = clip;
                OnAudioClipLoaded?.Invoke(uiKey, clip);
                LogInfo($"Loaded UI sound: {soundId}");
            }
        }

        private IEnumerator LoadSoundscapeAsync(string soundscapeId)
        {
            var soundscape = _soundscapeLibrary?.GetSoundscape(soundscapeId);
            
            yield return null;
            
            if (soundscape != null)
            {
                _loadedSoundscapes[soundscapeId] = soundscape;
                PreloadSoundscapeLayers(soundscape);
                OnSoundscapeLoaded?.Invoke(soundscapeId, soundscape);
                LogInfo($"Loaded soundscape: {soundscapeId}");
            }
        }

        private IEnumerator LoadMusicTrackAsync(string trackId)
        {
            var track = _musicPlaylist?.GetTrack(trackId);
            
            yield return null;
            
            if (track != null)
            {
                string musicKey = $"music_{trackId}";
                _loadedClips[musicKey] = track.AudioClip;
                OnAudioClipLoaded?.Invoke(musicKey, track.AudioClip);
                LogInfo($"Loaded music track: {trackId}");
            }
        }

        #endregion

        #region Preloading

        private IEnumerator PreloadEssentialAudio()
        {
            LogInfo("Starting essential audio preloading for cannabis cultivation");

            // Preload common game sounds
            var essentialClips = new[]
            {
                "plant_added", "plant_growth_stage", "construction_started", 
                "temperature_warning", "humidity_warning", "hvac_running",
                "light_ballast_hum", "construction_ambient", "plant_growth_subtle"
            };

            foreach (var clipId in essentialClips)
            {
                PreloadAudioClip(clipId);
                yield return new WaitForEndOfFrame();
            }

            // Preload essential UI sounds
            var essentialUISounds = new[]
            {
                "alert_medium", "button_click", "menu_select", "notification"
            };

            foreach (var soundId in essentialUISounds)
            {
                GetUISound(soundId);
                yield return new WaitForEndOfFrame();
            }

            // Preload essential soundscapes
            var essentialSoundscapes = new[]
            {
                "facility_ambient", "construction_ambient", "menu_ambient"
            };

            foreach (var soundscapeId in essentialSoundscapes)
            {
                PreloadSoundscape(soundscapeId);
                yield return new WaitForSeconds(0.1f);
            }

            LogInfo("Essential audio preloading completed");
        }

        #endregion

        #region Cache Management

        public void ClearCache()
        {
            // Clear audio clips
            foreach (var clip in _loadedClips.Values)
            {
                if (clip != null)
                {
                    Resources.UnloadAsset(clip);
                }
            }
            _loadedClips.Clear();

            // Clear soundscapes
            _loadedSoundscapes.Clear();

            // Clear loading state
            _loadingClips.Clear();
            _loadQueue.Clear();

            LogInfo("Audio cache cleared");
        }

        public void OptimizeCache()
        {
            // Remove unused clips based on last access time
            var unusedClips = new List<string>();
            
            foreach (var kvp in _loadedClips)
            {
                // Simple optimization: remove clips that haven't been accessed recently
                // In a real implementation, you'd track last access times
                if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance to be considered unused
                {
                    unusedClips.Add(kvp.Key);
                }
            }

            foreach (var clipId in unusedClips)
            {
                UnloadAudioClip(clipId);
            }

            if (unusedClips.Count > 0)
            {
                LogInfo($"Optimized cache: removed {unusedClips.Count} unused clips");
            }
        }

        public AudioLoadingStats GetLoadingStats()
        {
            return new AudioLoadingStats
            {
                LoadedClips = _loadedClips.Count,
                LoadedSoundscapes = _loadedSoundscapes.Count,
                QueuedLoads = _loadQueue.Count,
                ActiveLoads = _activeLoadOperations,
                LoadingProgress = LoadingProgress
            };
        }

        #endregion

        #region Utility Methods

        private string GetCacheKey(string resourceId, AudioLoadType loadType)
        {
            return loadType switch
            {
                AudioLoadType.UISound => $"ui_{resourceId}",
                AudioLoadType.Music => $"music_{resourceId}",
                AudioLoadType.Soundscape => resourceId,
                _ => resourceId
            };
        }

        private int GetLoadPriority(AudioLoadType loadType)
        {
            return loadType switch
            {
                AudioLoadType.UISound => 10,    // Highest priority
                AudioLoadType.Clip => 5,        // Medium priority
                AudioLoadType.Soundscape => 3,  // Lower priority
                AudioLoadType.Music => 1,       // Lowest priority
                _ => 1
            };
        }

        private void UpdateLoadingProgress()
        {
            if (_loadQueue.Count == 0 && _activeLoadOperations == 0)
            {
                LoadingProgress = 1f;
            }
            else
            {
                int totalOperations = _loadQueue.Count + _activeLoadOperations;
                LoadingProgress = 1f - (totalOperations / (float)(_loadedClips.Count + totalOperations));
            }

            OnLoadingProgress?.Invoke(LoadingProgress);
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.Log($"[AudioLoadingService] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.LogWarning($"[AudioLoadingService] {message}");
        }

        private enum AudioLoadType
        {
            Clip,
            UISound,
            Soundscape,
            Music
        }

        private struct AudioLoadRequest
        {
            public string ResourceId;
            public AudioLoadType LoadType;
            public int Priority;
            public float QueueTime;
        }

        public struct AudioLoadingStats
        {
            public int LoadedClips;
            public int LoadedSoundscapes;
            public int QueuedLoads;
            public int ActiveLoads;
            public float LoadingProgress;
        }
    }
}