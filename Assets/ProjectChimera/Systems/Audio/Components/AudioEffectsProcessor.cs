using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Audio.Components
{
    /// <summary>
    /// Handles audio effects processing, spatial audio, and adaptive mixing for Project Chimera's cannabis cultivation game.
    /// Manages 3D audio positioning, environmental audio effects, and dynamic sound processing.
    /// </summary>
    public class AudioEffectsProcessor : MonoBehaviour, ITickable
    {
        [Header("Effects Configuration")]
        [SerializeField] private bool _enableSpatialAudio = true;
        [SerializeField] private bool _enableEnvironmentalEffects = true;
        [SerializeField] private bool _enableAdaptiveMixing = true;
        [SerializeField] private bool _enableDebugLogging = false;

        [Header("3D Audio Settings")]
        [SerializeField] private float _maxAudioDistance = 50f;
        [SerializeField] private AnimationCurve _audioFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] private float _dopplerLevel = 1f;
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Mixer Configuration")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private AudioMixerGroup _ambientMixerGroup;
        [SerializeField] private AudioMixerGroup _effectsMixerGroup;
        [SerializeField] private AudioMixerGroup _environmentalMixerGroup;

        // Audio source management
        private Queue<AudioSource> _availableEffectSources = new Queue<AudioSource>();
        private List<AudioSource> _activeEffectSources = new List<AudioSource>();
        private Dictionary<string, AudioSource> _persistentEffectSources = new Dictionary<string, AudioSource>();

        // Environmental audio
        private Dictionary<string, EnvironmentalAudioSource> _environmentalSources = new Dictionary<string, EnvironmentalAudioSource>();
        private List<ProceduralAudioGenerator> _proceduralGenerators = new List<ProceduralAudioGenerator>();

        // Spatial audio
        private UnityEngine.Camera _listenerCamera;
        private Vector3 _lastListenerPosition;
        private float _listenerVelocity;

        // Audio state
        private AudioState _currentAudioState = AudioState.Facility;
        private float _masterVolume = 1f;
        private bool _isAudioMuted = false;

        // Events
        public System.Action<AudioState> OnAudioStateChanged;
        public System.Action<float> OnVolumeChanged;
        public System.Action<string> OnEffectProcessed;

        // Properties
        public bool SpatialAudioEnabled => _enableSpatialAudio;
        public int ActiveEffectSourcesCount => _activeEffectSources.Count;
        public int EnvironmentalSourcesCount => _environmentalSources.Count;
        public AudioState CurrentAudioState => _currentAudioState;

        public void Initialize(AudioMixer masterMixer, AudioMixerGroup ambientGroup, AudioMixerGroup effectsGroup, AudioMixerGroup environmentalGroup)
        {
            _masterMixer = masterMixer;
            _ambientMixerGroup = ambientGroup;
            _effectsMixerGroup = effectsGroup;
            _environmentalMixerGroup = environmentalGroup;

            SetupAudioListener();
            InitializeEffectSourcePool();
            InitializeMixerConfiguration();

            LogInfo("Audio effects processor initialized for cannabis cultivation game");
        }

            public void Tick(float deltaTime)
    {
            if (_enableSpatialAudio)
            {
                UpdateSpatialAudio();

    }

            UpdateAudioSourcePool();
            ProcessProceduralAudio();
        }

        #region Audio Source Pool Management

        private void InitializeEffectSourcePool()
        {
            // Create effect audio source pool
            int poolSize = 16;
            for (int i = 0; i < poolSize; i++)
            {
                var effectSource = CreateEffectAudioSource($"EffectSource_{i}");
                _availableEffectSources.Enqueue(effectSource);
            }

            LogInfo($"Initialized effect audio source pool with {poolSize} sources");
        }

        private AudioSource CreateEffectAudioSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform);

            var audioSource = go.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = _effectsMixerGroup;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = _enableSpatialAudio ? 1f : 0f;
            audioSource.rolloffMode = _rolloffMode;
            audioSource.maxDistance = _maxAudioDistance;
            audioSource.dopplerLevel = _dopplerLevel;

            go.SetActive(false);
            return audioSource;
        }

        public AudioSource GetAvailableEffectSource()
        {
            if (_availableEffectSources.Count > 0)
            {
                var audioSource = _availableEffectSources.Dequeue();
                audioSource.gameObject.SetActive(true);
                return audioSource;
            }

            // Try to find a finished audio source
            for (int i = _activeEffectSources.Count - 1; i >= 0; i--)
            {
                var activeSource = _activeEffectSources[i];
                if (!activeSource.isPlaying)
                {
                    ReturnEffectSourceToPool(activeSource);
                    return GetAvailableEffectSource();
                }
            }

            return null; // No sources available
        }

        public void ReturnEffectSourceToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            _activeEffectSources.Remove(audioSource);

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.gameObject.SetActive(false);

            _availableEffectSources.Enqueue(audioSource);
        }

        private void UpdateAudioSourcePool()
        {
            // Clean up finished effect sources
            for (int i = _activeEffectSources.Count - 1; i >= 0; i--)
            {
                var activeSource = _activeEffectSources[i];
                if (activeSource == null || !activeSource.isPlaying)
                {
                    if (activeSource != null)
                    {
                        ReturnEffectSourceToPool(activeSource);
                    }
                    else
                    {
                        _activeEffectSources.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region Spatial Audio Processing

        private void SetupAudioListener()
        {
            _listenerCamera = ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>() ?? UnityEngine.Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<UnityEngine.Camera>();

            if (_listenerCamera != null)
            {
                var listener = _listenerCamera.GetComponent<AudioListener>();
                if (listener == null)
                {
                    listener = _listenerCamera.gameObject.AddComponent<AudioListener>();
                }
                _lastListenerPosition = _listenerCamera.transform.position;
            }
        }

        private void UpdateSpatialAudio()
        {
            if (_listenerCamera == null) return;

            Vector3 currentPosition = _listenerCamera.transform.position;

            // Calculate listener velocity for Doppler effects
            _listenerVelocity = Vector3.Distance(currentPosition, _lastListenerPosition) / Time.deltaTime;
            _lastListenerPosition = currentPosition;

            // Update environmental audio source distances and volumes
            foreach (var envSource in _environmentalSources.Values)
            {
                UpdateEnvironmentalSourceSpatialAudio(envSource, currentPosition);
            }

            // Update persistent effect sources
            foreach (var effectSource in _persistentEffectSources.Values)
            {
                UpdateEffectSourceSpatialAudio(effectSource, currentPosition);
            }
        }

        private void UpdateEnvironmentalSourceSpatialAudio(EnvironmentalAudioSource envSource, Vector3 listenerPosition)
        {
            float distance = Vector3.Distance(listenerPosition, envSource.Position);
            float volumeMultiplier = _audioFalloffCurve.Evaluate(distance / envSource.MaxDistance);

            envSource.AudioSource.volume = envSource.BaseVolume * volumeMultiplier * _masterVolume;

            // Apply environmental effects based on distance
            ApplyDistanceEffects(envSource.AudioSource, distance, envSource.MaxDistance);
        }

        private void UpdateEffectSourceSpatialAudio(AudioSource effectSource, Vector3 listenerPosition)
        {
            if (effectSource.spatialBlend > 0f)
            {
                float distance = Vector3.Distance(listenerPosition, effectSource.transform.position);
                ApplyDistanceEffects(effectSource, distance, effectSource.maxDistance);
            }
        }

        private void ApplyDistanceEffects(AudioSource audioSource, float distance, float maxDistance)
        {
            if (!_enableEnvironmentalEffects) return;

            // Apply low-pass filter for distant sounds
            float distanceRatio = distance / maxDistance;
            if (distanceRatio > 0.5f)
            {
                // Simulate distant sound dampening
                audioSource.pitch = Mathf.Lerp(1f, 0.9f, (distanceRatio - 0.5f) * 2f);
            }
            else
            {
                audioSource.pitch = 1f;
            }
        }

        public AudioSource PlaySpatialEffect(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return null;

            var audioSource = GetAvailableEffectSource();
            if (audioSource == null) return null;

            // Configure spatial audio source
            audioSource.clip = clip;
            audioSource.volume = volume * _masterVolume;
            audioSource.pitch = pitch;
            audioSource.transform.position = position;
            audioSource.spatialBlend = _enableSpatialAudio ? 1f : 0f;

            audioSource.Play();
            _activeEffectSources.Add(audioSource);

            OnEffectProcessed?.Invoke(clip.name);

            // Auto-return to pool when finished
            StartCoroutine(ReturnToPoolWhenFinished(audioSource));

            return audioSource;
        }

        private IEnumerator ReturnToPoolWhenFinished(AudioSource audioSource)
        {
            while (audioSource != null && audioSource.isPlaying)
            {
                yield return null;
            }

            if (audioSource != null)
            {
                ReturnEffectSourceToPool(audioSource);
            }
        }

        #endregion

        #region Environmental Audio Effects

        public void RegisterEnvironmentalEffect(string effectId, Vector3 position, AudioClip clip, float maxDistance = 20f, float baseVolume = 1f)
        {
            if (_environmentalSources.ContainsKey(effectId))
            {
                UpdateEnvironmentalEffectPosition(effectId, position);
                return;
            }

            var audioSource = CreateEnvironmentalAudioSource($"Environmental_{effectId}");
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = maxDistance;
            audioSource.transform.position = position;
            audioSource.volume = baseVolume * _masterVolume;
            audioSource.Play();

            var envSource = new EnvironmentalAudioSource
            {
                SourceId = effectId,
                AudioSource = audioSource,
                Position = position,
                MaxDistance = maxDistance,
                BaseVolume = baseVolume
            };

            _environmentalSources[effectId] = envSource;
            LogInfo($"Registered environmental effect: {effectId}");
        }

        private AudioSource CreateEnvironmentalAudioSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform);

            var audioSource = go.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = _environmentalMixerGroup;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = _rolloffMode;
            audioSource.dopplerLevel = _dopplerLevel;

            return audioSource;
        }

        public void UpdateEnvironmentalEffectPosition(string effectId, Vector3 newPosition)
        {
            if (_environmentalSources.TryGetValue(effectId, out var envSource))
            {
                envSource.Position = newPosition;
                envSource.AudioSource.transform.position = newPosition;
            }
        }

        public void UnregisterEnvironmentalEffect(string effectId)
        {
            if (_environmentalSources.TryGetValue(effectId, out var envSource))
            {
                envSource.AudioSource.Stop();
                DestroyImmediate(envSource.AudioSource.gameObject);
                _environmentalSources.Remove(effectId);
                LogInfo($"Unregistered environmental effect: {effectId}");
            }
        }

        #endregion

        #region Adaptive Mixing

        private void InitializeMixerConfiguration()
        {
            if (_masterMixer == null) return;

            // Set initial mixer values
            _masterMixer.SetFloat("AmbientVolume", 0f);
            _masterMixer.SetFloat("EffectsVolume", 0f);
            _masterMixer.SetFloat("EnvironmentalVolume", 0f);
        }

        public void SetAudioState(AudioState newState)
        {
            if (_currentAudioState == newState) return;

            _currentAudioState = newState;

            if (_enableAdaptiveMixing)
            {
                ApplyStateBasedMixing(newState);
            }

            OnAudioStateChanged?.Invoke(newState);
            LogInfo($"Audio state changed to: {newState}");
        }

        private void ApplyStateBasedMixing(AudioState audioState)
        {
            if (_masterMixer == null) return;

            switch (audioState)
            {
                case AudioState.Facility:
                    ApplyFacilityMix();
                    break;
                case AudioState.Construction:
                    ApplyConstructionMix();
                    break;
                case AudioState.Menu:
                    ApplyMenuMix();
                    break;
            }
        }

        private void ApplyFacilityMix()
        {
            _masterMixer.SetFloat("AmbientVolume", -5f);  // Slightly reduced ambient
            _masterMixer.SetFloat("EffectsVolume", 0f);   // Full effects
            _masterMixer.SetFloat("EnvironmentalVolume", -3f); // Moderate environmental
        }

        private void ApplyConstructionMix()
        {
            _masterMixer.SetFloat("AmbientVolume", -10f); // Reduced ambient
            _masterMixer.SetFloat("EffectsVolume", 3f);   // Boosted effects
            _masterMixer.SetFloat("EnvironmentalVolume", 0f); // Full environmental
        }

        private void ApplyMenuMix()
        {
            _masterMixer.SetFloat("AmbientVolume", -15f); // Very reduced ambient
            _masterMixer.SetFloat("EffectsVolume", -5f);  // Reduced effects
            _masterMixer.SetFloat("EnvironmentalVolume", -10f); // Reduced environmental
        }

        public void SetMixerGroupVolume(string groupName, float volume)
        {
            if (_masterMixer != null)
            {
                float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
                _masterMixer.SetFloat($"{groupName}Volume", dbValue);
            }
        }

        #endregion

        #region Procedural Audio

        public void RegisterProceduralGenerator(ProceduralAudioGenerator generator)
        {
            if (!_proceduralGenerators.Contains(generator))
            {
                _proceduralGenerators.Add(generator);
                generator.Initialize(this);
                LogInfo($"Registered procedural audio generator: {generator.GetType().Name}");
            }
        }

        public void UnregisterProceduralGenerator(ProceduralAudioGenerator generator)
        {
            if (_proceduralGenerators.Remove(generator))
            {
                generator.Cleanup();
                LogInfo($"Unregistered procedural audio generator: {generator.GetType().Name}");
            }
        }

        private void ProcessProceduralAudio()
        {
            foreach (var generator in _proceduralGenerators)
            {
                generator.Process(Time.deltaTime);
            }
        }

        #endregion

        #region Persistent Effects

        public AudioSource CreatePersistentEffect(string effectKey, AudioClip clip, Vector3? position = null, float volume = 1f, bool loop = true)
        {
            // Stop existing persistent effect with same key
            if (_persistentEffectSources.TryGetValue(effectKey, out var existingSource))
            {
                existingSource.Stop();
                DestroyImmediate(existingSource.gameObject);
            }

            var audioSource = CreateEffectAudioSource($"Persistent_{effectKey}");
            audioSource.clip = clip;
            audioSource.volume = volume * _masterVolume;
            audioSource.loop = loop;

            if (position.HasValue && _enableSpatialAudio)
            {
                audioSource.transform.position = position.Value;
                audioSource.spatialBlend = 1f;
            }
            else
            {
                audioSource.spatialBlend = 0f;
            }

            audioSource.gameObject.SetActive(true);
            audioSource.Play();
            _persistentEffectSources[effectKey] = audioSource;

            LogInfo($"Created persistent effect: {effectKey}");
            return audioSource;
        }

        public void StopPersistentEffect(string effectKey)
        {
            if (_persistentEffectSources.TryGetValue(effectKey, out var audioSource))
            {
                audioSource.Stop();
                DestroyImmediate(audioSource.gameObject);
                _persistentEffectSources.Remove(effectKey);
                LogInfo($"Stopped persistent effect: {effectKey}");
            }
        }

        public void UpdatePersistentEffectVolume(string effectKey, float volume)
        {
            if (_persistentEffectSources.TryGetValue(effectKey, out var audioSource))
            {
                audioSource.volume = volume * _masterVolume;
            }
        }

        #endregion

        #region Volume and Configuration

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);

            // Update all active sources
            foreach (var source in _activeEffectSources)
            {
                if (source != null)
                {
                    source.volume *= _masterVolume;
                }
            }

            foreach (var source in _persistentEffectSources.Values)
            {
                if (source != null)
                {
                    source.volume *= _masterVolume;
                }
            }

            foreach (var envSource in _environmentalSources.Values)
            {
                if (envSource.AudioSource != null)
                {
                    envSource.AudioSource.volume = envSource.BaseVolume * _masterVolume;
                }
            }

            OnVolumeChanged?.Invoke(_masterVolume);
        }

        public void SetAudioMuted(bool muted)
        {
            _isAudioMuted = muted;

            if (_masterMixer != null)
            {
                _masterMixer.SetFloat("MasterVolume", muted ? -80f : Mathf.Log10(_masterVolume) * 20f);
            }
        }

        public void SetSpatialAudioEnabled(bool enabled)
        {
            _enableSpatialAudio = enabled;

            // Update all active sources
            foreach (var source in _activeEffectSources)
            {
                if (source != null)
                {
                    source.spatialBlend = enabled ? 1f : 0f;
                }
            }

            foreach (var source in _persistentEffectSources.Values)
            {
                if (source != null && source.clip != null)
                {
                    source.spatialBlend = enabled ? 1f : 0f;
                }
            }
        }

        #endregion

        #region Cleanup

        public void Cleanup()
        {
            // Stop all active effect sources
            foreach (var source in _activeEffectSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
            _activeEffectSources.Clear();

            // Stop all persistent effects
            foreach (var source in _persistentEffectSources.Values)
            {
                if (source != null)
                {
                    source.Stop();
                    DestroyImmediate(source.gameObject);
                }
            }
            _persistentEffectSources.Clear();

            // Cleanup environmental sources
            foreach (var envSource in _environmentalSources.Values)
            {
                if (envSource.AudioSource != null)
                {
                    envSource.AudioSource.Stop();
                    DestroyImmediate(envSource.AudioSource.gameObject);
                }
            }
            _environmentalSources.Clear();

            // Cleanup procedural generators
            foreach (var generator in _proceduralGenerators)
            {
                generator?.Cleanup();
            }
            _proceduralGenerators.Clear();

            // Return pooled sources
            while (_availableEffectSources.Count > 0)
            {
                var source = _availableEffectSources.Dequeue();
                if (source != null)
                {
                    DestroyImmediate(source.gameObject);
                }
            }

            LogInfo("Audio effects processor cleanup complete");
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.Log($"[AudioEffectsProcessor] {message}");
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}

    // Supporting classes for environmental audio and procedural generation
    public class EnvironmentalAudioSource
    {
        public string SourceId;
        public AudioSource AudioSource;
        public Vector3 Position;
        public float MaxDistance;
        public float BaseVolume;
    }

    public abstract class ProceduralAudioGenerator
    {
        public abstract void Initialize(AudioEffectsProcessor processor);
        public abstract void Process(float deltaTime);
        public abstract void Cleanup();
    }

    public enum AudioState
    {
        Facility,
        Construction,
        Menu
    }
}
