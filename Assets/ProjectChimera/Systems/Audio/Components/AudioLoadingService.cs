using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Audio.Components
{
    /// <summary>
    /// BASIC: Simple audio loading for Project Chimera.
    /// Focuses on essential audio functionality without complex loading systems.
    /// </summary>
    public class AudioLoadingService : MonoBehaviour
    {
        [Header("Basic Audio Settings")]
        [SerializeField] private bool _enableBasicAudio = true;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic audio cache
        private readonly Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for audio operations
        /// </summary>
        public event System.Action<string> OnAudioLoaded;
        public event System.Action<string, string> OnAudioLoadError;

        /// <summary>
        /// Initialize basic audio service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log("[AudioLoadingService] Initialized successfully");
            }
        }

        /// <summary>
        /// Load audio clip by name
        /// </summary>
        public AudioClip LoadAudioClip(string clipName)
        {
            if (!_enableBasicAudio || !_isInitialized) return null;

            // Check cache first
            if (_enableCaching && _audioCache.ContainsKey(clipName))
            {
                return _audioCache[clipName];
            }

            // Load from Resources
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            if (clip != null)
            {
                // Cache the clip
                if (_enableCaching)
                {
                    _audioCache[clipName] = clip;
                }

                OnAudioLoaded?.Invoke(clipName);

                if (_enableLogging)
                {
                    Debug.Log($"[AudioLoadingService] Loaded audio: {clipName}");
                }

                return clip;
            }
            else
            {
                OnAudioLoadError?.Invoke(clipName, "Audio clip not found in Resources");

                if (_enableLogging)
                {
                    Debug.LogWarning($"[AudioLoadingService] Failed to load audio: {clipName}");
                }

                return null;
            }
        }

        /// <summary>
        /// Load UI sound
        /// </summary>
        public AudioClip LoadUISound(string soundName)
        {
            return LoadAudioClip($"UI/{soundName}");
        }

        /// <summary>
        /// Load ambient sound
        /// </summary>
        public AudioClip LoadAmbientSound(string soundName)
        {
            return LoadAudioClip($"Ambient/{soundName}");
        }

        /// <summary>
        /// Load music track
        /// </summary>
        public AudioClip LoadMusic(string musicName)
        {
            return LoadAudioClip($"Music/{musicName}");
        }

        /// <summary>
        /// Unload audio clip from cache
        /// </summary>
        public void UnloadAudioClip(string clipName)
        {
            if (_audioCache.Remove(clipName))
            {
                if (_enableLogging)
                {
                    Debug.Log($"[AudioLoadingService] Unloaded audio: {clipName}");
                }
            }
        }

        /// <summary>
        /// Clear all cached audio
        /// </summary>
        public void ClearCache()
        {
            _audioCache.Clear();

            if (_enableLogging)
            {
                Debug.Log("[AudioLoadingService] Cleared audio cache");
            }
        }

        /// <summary>
        /// Check if audio exists
        /// </summary>
        public bool AudioExists(string clipName)
        {
            if (_enableCaching && _audioCache.ContainsKey(clipName))
            {
                return true;
            }

            // Check Resources
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            if (clip != null)
            {
                Resources.UnloadAsset(clip);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get cached audio count
        /// </summary>
        public int GetCachedAudioCount()
        {
            return _audioCache.Count;
        }

        /// <summary>
        /// Get cached audio names
        /// </summary>
        public List<string> GetCachedAudioNames()
        {
            return new List<string>(_audioCache.Keys);
        }

        /// <summary>
        /// Get audio loading statistics
        /// </summary>
        public AudioStats GetAudioStats()
        {
            return new AudioStats
            {
                CachedAudioCount = _audioCache.Count,
                IsAudioEnabled = _enableBasicAudio,
                IsCachingEnabled = _enableCaching,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Preload common audio
        /// </summary>
        public void PreloadCommonAudio()
        {
            if (!_enableBasicAudio || !_isInitialized) return;

            // Preload some common audio that might be used frequently
            string[] commonAudio = {
                "UI/Click",
                "UI/Hover",
                "Ambient/GrowRoom",
                "Music/Background"
            };

            foreach (string audioPath in commonAudio)
            {
                LoadAudioClip(audioPath);
            }

            if (_enableLogging)
            {
                Debug.Log($"[AudioLoadingService] Preloaded {commonAudio.Length} common audio files");
            }
        }
    }

    /// <summary>
    /// Audio loading statistics
    /// </summary>
    [System.Serializable]
    public struct AudioStats
    {
        public int CachedAudioCount;
        public bool IsAudioEnabled;
        public bool IsCachingEnabled;
        public bool IsInitialized;
    }
}
