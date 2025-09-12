using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Audio
{
    /// <summary>
    /// SIMPLE: Basic audio manager aligned with Project Chimera's audio system vision.
    /// Focuses on essential audio playback for cultivation activities.
    /// </summary>
    public class AdvancedAudioManager : MonoBehaviour
    {
        [Header("Basic Audio Settings")]
        [SerializeField] private bool _enableAudio = true;
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private bool _enableLogging = true;

        // Basic audio sources
        private AudioSource _effectSource;
        private AudioSource _ambientSource;
        private AudioSource _musicSource;
        private bool _isInitialized = false;

        // Basic audio state
        private bool _isMuted = false;
        private AudioState _currentState = AudioState.Facility;

        // Events
        public System.Action<AudioClip> OnAudioPlayed;
        public System.Action<float> OnVolumeChanged;
        public System.Action<AudioState> OnStateChanged;

        /// <summary>
        /// Initialize the audio manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            SetupAudioSources();

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log("[AdvancedAudioManager] Initialized successfully");
            }
        }

        /// <summary>
        /// Play an audio clip
        /// </summary>
        public void PlayAudio(AudioClip clip, AudioCategory category = AudioCategory.Effect, float volume = 1f)
        {
            if (!_enableAudio || !_isInitialized || clip == null || _isMuted) return;

            AudioSource source = GetAudioSourceForCategory(category);
            if (source != null)
            {
                source.clip = clip;
                source.volume = volume * _masterVolume;
                source.Play();

                OnAudioPlayed?.Invoke(clip);

                if (_enableLogging)
                {
                    Debug.Log($"[AdvancedAudioManager] Played {category}: {clip.name}");
                }
            }
        }

        /// <summary>
        /// Play ambient audio
        /// </summary>
        public void PlayAmbient(AudioClip clip, bool loop = true)
        {
            if (!_enableAudio || !_isInitialized || clip == null) return;

            if (_ambientSource != null)
            {
                _ambientSource.clip = clip;
                _ambientSource.loop = loop;
                _ambientSource.volume = _masterVolume;
                _ambientSource.Play();

                if (_enableLogging)
                {
                    Debug.Log($"[AdvancedAudioManager] Started ambient: {clip.name}");
                }
            }
        }

        /// <summary>
        /// Play music
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!_enableAudio || !_isInitialized || clip == null) return;

            if (_musicSource != null)
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.volume = _masterVolume * 0.7f; // Slightly lower music volume
                _musicSource.Play();

                if (_enableLogging)
                {
                    Debug.Log($"[AdvancedAudioManager] Started music: {clip.name}");
                }
            }
        }

        /// <summary>
        /// Stop ambient audio
        /// </summary>
        public void StopAmbient()
        {
            if (_ambientSource != null && _ambientSource.isPlaying)
            {
                _ambientSource.Stop();

                if (_enableLogging)
                {
                    Debug.Log("[AdvancedAudioManager] Stopped ambient audio");
                }
            }
        }

        /// <summary>
        /// Stop music
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();

                if (_enableLogging)
                {
                    Debug.Log("[AdvancedAudioManager] Stopped music");
                }
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);

            if (_effectSource != null)
                _effectSource.volume = _masterVolume;
            if (_ambientSource != null)
                _ambientSource.volume = _masterVolume;
            if (_musicSource != null)
                _musicSource.volume = _masterVolume * 0.7f;

            OnVolumeChanged?.Invoke(_masterVolume);

            if (_enableLogging)
            {
                Debug.Log($"[AdvancedAudioManager] Volume set to: {_masterVolume:F2}");
            }
        }

        /// <summary>
        /// Mute/unmute audio
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;

            if (_effectSource != null)
                _effectSource.mute = muted;
            if (_ambientSource != null)
                _ambientSource.mute = muted;
            if (_musicSource != null)
                _musicSource.mute = muted;

            if (_enableLogging)
            {
                Debug.Log($"[AdvancedAudioManager] Muted: {muted}");
            }
        }

        /// <summary>
        /// Set audio state
        /// </summary>
        public void SetAudioState(AudioState state)
        {
            if (_currentState == state) return;

            _currentState = state;
            OnStateChanged?.Invoke(state);

            if (_enableLogging)
            {
                Debug.Log($"[AdvancedAudioManager] State changed to: {state}");
            }
        }

        /// <summary>
        /// Get current volume
        /// </summary>
        public float GetMasterVolume()
        {
            return _masterVolume;
        }

        /// <summary>
        /// Get current mute state
        /// </summary>
        public bool IsMuted()
        {
            return _isMuted;
        }

        /// <summary>
        /// Get current audio state
        /// </summary>
        public AudioState GetCurrentState()
        {
            return _currentState;
        }

        #region Private Methods

        private void SetupAudioSources()
        {
            // Create effect audio source
            if (_effectSource == null)
            {
                _effectSource = gameObject.AddComponent<AudioSource>();
                _effectSource.playOnAwake = false;
                _effectSource.spatialBlend = 0f; // 2D audio
            }

            // Create ambient audio source
            if (_ambientSource == null)
            {
                _ambientSource = gameObject.AddComponent<AudioSource>();
                _ambientSource.playOnAwake = false;
                _ambientSource.spatialBlend = 0f; // 2D audio
            }

            // Create music audio source
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f; // 2D audio
            }

            SetMasterVolume(_masterVolume);
        }

        private AudioSource GetAudioSourceForCategory(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ambient:
                    return _ambientSource;
                case AudioCategory.UI:
                case AudioCategory.Voice:
                    return _effectSource; // Use effect source for UI and voice
                default:
                    return _effectSource;
            }
        }

        #endregion
    }
}
