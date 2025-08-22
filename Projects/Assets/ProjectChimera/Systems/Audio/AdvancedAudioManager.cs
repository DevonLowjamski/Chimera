using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Systems.Audio.Components;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using DataPlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

// Use weak-typed aliases to decouple from optional systems
using EnvironmentManager = System.Object;
using EnvironmentalAlert = System.Object;

namespace ProjectChimera.Systems.Audio
{
    /// <summary>
    /// Optimized audio orchestrator for Project Chimera cannabis cultivation game.
    /// Coordinates specialized audio components for loading, effects processing, soundscape management,
    /// and environmental audio integration while maintaining 60 FPS performance.
    /// </summary>
    public class AdvancedAudioManager : DIChimeraManager
    {
        [Header("Audio Configuration")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private AudioMixerGroup _ambientMixerGroup;
        [SerializeField] private AudioMixerGroup _effectsMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _environmentalMixerGroup;
        
        [Header("Audio Libraries")]
        [SerializeField] private AudioLibrarySO _audioLibrary;
        [SerializeField] private SoundscapeLibrarySO _soundscapeLibrary;
        [SerializeField] private MusicPlaylistSO _musicPlaylist;
        
        [Header("Dynamic Audio Settings")]
        [SerializeField] private bool _enableDynamicSoundscapes = true;
        [SerializeField] private bool _enableSpatialAudio = true;
        [SerializeField] private bool _enableAdaptiveMixing = true;
        [SerializeField] private bool _enableEnvironmentalAudio = true;
        [SerializeField] private float _audioUpdateInterval = 0.5f;
        
        // Specialized component managers
        private AudioLoadingService _loadingService;
        private AudioEffectsProcessor _effectsProcessor;
        
        // Soundscape Management
        private DynamicSoundscape _currentSoundscape;
        private Dictionary<SoundscapeLayer, AudioSource> _soundscapeLayers = new Dictionary<SoundscapeLayer, AudioSource>();
        private float _lastSoundscapeUpdate = 0f;
        
        // Music System
        private MusicController _musicController;
        private AudioSource _musicSource;
        private bool _isMusicEnabled = true;
        private float _musicVolume = 0.7f;
        
        // Performance Monitoring
        private AudioPerformanceMetrics _performanceMetrics;
        private Queue<AudioLoadData> _performanceHistory = new Queue<AudioLoadData>();
        
        // System References
        private object _environmentManager;
        private ChimeraManager _plantManager;
        private ChimeraManager _facilityConstructor;
        private Camera _listenerCamera;
        
        // Audio State
        private AudioState _currentAudioState = AudioState.Facility;
        private float _masterVolume = 1f;
        private bool _isAudioMuted = false;
        private float _lastAudioUpdate = 0f;
        
        // Events
        public System.Action<AudioClip> OnAudioClipPlayed;
        public System.Action<DynamicSoundscape> OnSoundscapeChanged;
        public System.Action<float> OnVolumeChanged;
        public System.Action<AudioAlert> OnAudioAlert;
        
        // Properties
        public float MasterVolume => _masterVolume;
        public bool IsAudioMuted => _isAudioMuted;
        public AudioState CurrentAudioState => _currentAudioState;
        public AudioPerformanceMetrics PerformanceMetrics => _performanceMetrics;
        public DynamicSoundscape CurrentSoundscape => _currentSoundscape;
        
        protected override void OnManagerInitialize()
        {
            InitializeComponents();
            InitializeAudioSystem();
            SetupAudioMixers();
            SetupSoundscapes();
            ConnectToGameSystems();
            StartAudioUpdateLoop();
        }
        
        private void Update()
        {
            float currentTime = Time.time;
            
            if (currentTime - _lastAudioUpdate >= _audioUpdateInterval)
            {
                UpdateDynamicAudio();
                UpdatePerformanceMetrics();
                _lastAudioUpdate = currentTime;
            }
        }
        
        #region Initialization

        private void InitializeComponents()
        {
            // Initialize audio loading service
            _loadingService = GetOrAddComponent<AudioLoadingService>();
            _loadingService.Initialize(_audioLibrary, _soundscapeLibrary, _musicPlaylist);
            
            // Initialize audio effects processor
            _effectsProcessor = GetOrAddComponent<AudioEffectsProcessor>();
            _effectsProcessor.Initialize(_masterMixer, _ambientMixerGroup, _effectsMixerGroup, _environmentalMixerGroup);
            
            LogInfo("Audio component managers initialized for cannabis cultivation game");
        }
        
        private void InitializeAudioSystem()
        {
            if (_audioLibrary != null) _audioLibrary.InitializeDefaults();
            if (_soundscapeLibrary != null) _soundscapeLibrary.InitializeDefaults();
            
            SetupAudioListener();
            _musicController = new MusicController(_musicPlaylist);
            
            _performanceMetrics = new AudioPerformanceMetrics
            {
                MaxConcurrentSources = 64,
                PoolSize = 32,
                SpatialAudioEnabled = _enableSpatialAudio
            };
            
            LogInfo("Advanced Audio Manager initialized");
        }
        
        private void SetupAudioMixers()
        {
            if (_masterMixer == null)
            {
                LogWarning("Master AudioMixer not assigned");
                return;
            }
            
            _masterMixer.SetFloat("MasterVolume", Mathf.Log10(_masterVolume) * 20f);
            _masterMixer.SetFloat("AmbientVolume", 0f);
            _masterMixer.SetFloat("EffectsVolume", 0f);
            _masterMixer.SetFloat("UIVolume", 0f);
            _masterMixer.SetFloat("MusicVolume", Mathf.Log10(_musicVolume) * 20f);
            
            _musicSource = CreateAudioSource("MusicSource", _musicMixerGroup);
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }
        
        private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup mixerGroup)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform);
            
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = mixerGroup;
            audioSource.playOnAwake = false;
            
            return audioSource;
        }
        
        private void SetupAudioListener()
        {
            _listenerCamera = FindObjectOfType<Camera>();
            
            if (_listenerCamera != null)
            {
                var listener = _listenerCamera.GetComponent<AudioListener>();
                if (listener == null)
                    listener = _listenerCamera.gameObject.AddComponent<AudioListener>();
            }
        }
        
        private void SetupSoundscapes()
        {
            if (_soundscapeLibrary == null) return;
            
            foreach (SoundscapeLayer layer in Enum.GetValues(typeof(SoundscapeLayer)))
            {
                var layerSource = CreateAudioSource($"Soundscape_{layer}", _ambientMixerGroup);
                layerSource.loop = true;
                layerSource.spatialBlend = 0f;
                _soundscapeLayers[layer] = layerSource;
            }
            
            SetSoundscape("facility_ambient");
        }
        
        private void ConnectToGameSystems()
        {
            ConnectSystemEvents();
            LogInfo("Connected to game systems");
        }
        
        private void ConnectSystemEvents()
        {
            // Event subscriptions handled through GameManager event system
        }
        
        private void StartAudioUpdateLoop()
        {
            if (_enableDynamicSoundscapes)
                InvokeRepeating(nameof(UpdateSoundscapeLogic), 1f, 2f);
        }
        
        #endregion
        
        #region Audio Playback
        
        public AudioSource PlayAudioClip(string clipId, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            var audioClip = _loadingService?.GetAudioClip(clipId);
            if (audioClip == null)
            {
                LogWarning($"Audio clip not found: {clipId}");
                return null;
            }
            
            return _effectsProcessor?.PlaySpatialEffect(audioClip, position ?? Vector3.zero, volume, pitch);
        }
        
        public AudioSource PlayAudioClip(AudioClip clip, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return null;
            return _effectsProcessor?.PlaySpatialEffect(clip, position ?? Vector3.zero, volume, pitch);
        }
        
        public AudioSource PlayLoopingAudio(string clipId, string sourceKey, Vector3? position = null, float volume = 1f)
        {
            var audioClip = _loadingService?.GetAudioClip(clipId);
            if (audioClip == null)
            {
                LogWarning($"Audio clip not found: {clipId}");
                return null;
            }
            
            return _effectsProcessor?.CreatePersistentEffect(sourceKey, audioClip, position, volume, true);
        }
        
        public void StopLoopingAudio(string sourceKey)
        {
            _effectsProcessor?.StopPersistentEffect(sourceKey);
        }
        
        public void PlayUISound(string soundId, float volume = 1f)
        {
            var audioClip = _loadingService?.GetUISound(soundId);
            if (audioClip == null) return;
            
            var audioSource = _effectsProcessor?.GetAvailableEffectSource();
            if (audioSource == null) return;
            
            audioSource.outputAudioMixerGroup = _uiMixerGroup;
            audioSource.clip = audioClip;
            audioSource.volume = volume * _masterVolume;
            audioSource.spatialBlend = 0f;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();
        }
        
        #endregion
        
        #region Soundscape Management
        
        public void SetSoundscape(string soundscapeId)
        {
            var soundscape = _loadingService?.GetSoundscape(soundscapeId);
            if (soundscape == null)
            {
                LogWarning($"Soundscape not found: {soundscapeId}");
                return;
            }
            
            _currentSoundscape = soundscape;
            ApplySoundscape(soundscape);
            OnSoundscapeChanged?.Invoke(soundscape);
        }
        
        private void ApplySoundscape(DynamicSoundscape soundscape)
        {
            foreach (var layerSource in _soundscapeLayers.Values)
                layerSource.Stop();
            
            foreach (var layer in soundscape.Layers)
            {
                if (_soundscapeLayers.TryGetValue(layer.LayerType, out var layerSource))
                {
                    layerSource.clip = layer.AudioClip;
                    layerSource.volume = layer.Volume * _masterVolume;
                    layerSource.pitch = layer.Pitch;
                    
                    if (layer.AudioClip != null)
                        layerSource.Play();
                }
            }
        }
        
        private void UpdateSoundscapeLogic()
        {
            if (!_enableDynamicSoundscapes || _currentSoundscape == null) return;
            
            UpdateEnvironmentalSoundscape();
            UpdateFacilitySoundscape();
            UpdatePlantSoundscape();
        }
        
        private void UpdateEnvironmentalSoundscape()
        {
            if (_environmentManager == null) return;
            
            var conditions = EnvironmentalConditions.CreateIndoorDefault();
            float hvacIntensity = CalculateHVACIntensity(conditions);
            
            if (_soundscapeLayers.TryGetValue(SoundscapeLayer.HVAC, out var hvacSource))
                hvacSource.volume = hvacIntensity * _masterVolume;
            
            if (_soundscapeLayers.TryGetValue(SoundscapeLayer.Ventilation, out var ventSource))
                ventSource.volume = Mathf.Clamp01(conditions.AirVelocity / 2f) * _masterVolume;
        }
        
        private void UpdateFacilitySoundscape()
        {
            if (_soundscapeLayers.TryGetValue(SoundscapeLayer.Electrical, out var electricalSource))
                electricalSource.volume = CalculateElectricalActivity() * _masterVolume;
        }
        
        private void UpdatePlantSoundscape()
        {
            if (_plantManager == null) return;
            
            if (_soundscapeLayers.TryGetValue(SoundscapeLayer.Growth, out var growthSource))
                growthSource.volume = UnityEngine.Random.Range(0.1f, 0.5f) * 0.3f * _masterVolume;
        }
        
        #endregion
        
        #region Environmental Audio
        
        public void RegisterEnvironmentalAudioSource(string sourceId, Vector3 position, AudioClip clip, float maxDistance = 20f)
        {
            _effectsProcessor?.RegisterEnvironmentalEffect(sourceId, position, clip, maxDistance);
        }
        
        public void UpdateEnvironmentalAudioSource(string sourceId, Vector3 newPosition)
        {
            _effectsProcessor?.UpdateEnvironmentalEffectPosition(sourceId, newPosition);
        }
        
        public void UnregisterEnvironmentalAudioSource(string sourceId)
        {
            _effectsProcessor?.UnregisterEnvironmentalEffect(sourceId);
        }
        
        #endregion
        
        #region Music System
        
        public void PlayMusic(string trackId, bool loop = true, float fadeInTime = 2f)
        {
            var musicClip = _loadingService?.GetMusicTrack(trackId);
            if (musicClip == null)
            {
                LogWarning($"Music track not found: {trackId}");
                return;
            }
            
            _musicController.PlayTrack(musicClip, _musicSource, loop, fadeInTime);
        }
        
        public void StopMusic(float fadeOutTime = 2f)
        {
            _musicController.StopCurrentTrack(_musicSource, fadeOutTime);
        }
        
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _masterMixer.SetFloat("MusicVolume", Mathf.Log10(_musicVolume) * 20f);
        }
        
        public void SetMusicEnabled(bool enabled)
        {
            _isMusicEnabled = enabled;
            if (!enabled && _musicSource.isPlaying)
                StopMusic();
        }
        
        #endregion
        
        #region Dynamic Audio Updates
        
        private void UpdateDynamicAudio()
        {
            UpdateEnvironmentalAudio();
            UpdateConstructionAudio();
            UpdatePlantAudio();
            UpdateAdaptiveMixing();
        }
        
        private void UpdateEnvironmentalAudio()
        {
            if (!_enableEnvironmentalAudio || _environmentManager == null) return;
            
            var conditions = EnvironmentalConditions.CreateIndoorDefault();
            UpdateHVACAudio(conditions);
            UpdateLightingAudio(conditions);
        }
        
        private void UpdateHVACAudio(EnvironmentalConditions conditions)
        {
            float tempDiff = Mathf.Abs(conditions.Temperature - 24f);
            float hvacActivity = Mathf.Clamp01(tempDiff / 5f);
            
            if (hvacActivity > 0.1f)
                PlayLoopingAudio("hvac_running", "hvac_system", null, hvacActivity * 0.5f);
            else
                StopLoopingAudio("hvac_system");
        }

        private void UpdateLightingAudio(EnvironmentalConditions conditions)
        {
            bool hasActiveLights = UnityEngine.Random.Range(0f, 1f) > 0.3f;
            
            if (hasActiveLights)
            {
                float ballastVolume = UnityEngine.Random.Range(0.1f, 0.3f);
                PlayLoopingAudio("light_ballast_hum", "general_lighting", null, ballastVolume);
            }
            else
                StopLoopingAudio("general_lighting");
        }
        
        private void UpdateConstructionAudio()
        {
            if (_facilityConstructor == null) return;
            
            bool hasActiveConstruction = UnityEngine.Random.Range(0f, 1f) > 0.7f;
            
            if (hasActiveConstruction)
                PlayLoopingAudio("construction_ambient", "construction_work", null, 0.4f);
            else
                StopLoopingAudio("construction_work");
        }
        
        private void UpdatePlantAudio()
        {
            if (_plantManager == null) return;
            
            int simulatedHealthyPlants = UnityEngine.Random.Range(5, 15);
            
            if (simulatedHealthyPlants > 0)
            {
                float growthIntensity = simulatedHealthyPlants / 20f;
                PlayLoopingAudio("plant_growth_subtle", "plant_growth", null, growthIntensity * 0.2f);
            }
            else
                StopLoopingAudio("plant_growth");
        }
        
        #endregion
        
        #region Adaptive Mixing
        
        private void UpdateAdaptiveMixing()
        {
            if (!_enableAdaptiveMixing) return;
            
            // Note: Commenting out until AudioState compatibility is resolved
            // _effectsProcessor?.SetAudioState(_currentAudioState);
        }
        
        #endregion
        
        #region Event Handlers

        private void HandleEnvironmentalChange(EnvironmentalConditions conditions)
        {
            if (conditions.Temperature > 30f)
                PlayAudioClip("temperature_warning", null, 0.5f);
            if (conditions.Humidity > 80f)
                PlayAudioClip("humidity_warning", null, 0.5f);
        }
        
        private void HandleEnvironmentalAlert(object alert)
        {
            if (alert != null)
                PlayUISound("alert_medium", 0.8f);
        }
        
        private void HandlePlantAdded(object plant)
        {
            PlayAudioClip("plant_added", null, 0.4f);
        }
        
        private void HandlePlantStageChanged(object plant)
        {
            PlayAudioClip("plant_growth_stage", null, 0.3f);
        }
        
        private void HandleConstructionStarted(object project)
        {
            PlayAudioClip("construction_started", null, 0.6f);
            SetAudioState(AudioState.Construction);
        }
        
        private void HandleConstructionIssue(string projectId, object issue)
        {
            PlayAudioClip("construction_issue_generic", null, 0.8f);
        }
        
        #endregion
        
        #region Performance Monitoring
        
        private void UpdatePerformanceMetrics()
        {
            _performanceMetrics.ActiveSources = _effectsProcessor?.ActiveEffectSourcesCount ?? 0;
            _performanceMetrics.AvailableSources = 32 - (_effectsProcessor?.ActiveEffectSourcesCount ?? 0);
            _performanceMetrics.EnvironmentalSources = _effectsProcessor?.EnvironmentalSourcesCount ?? 0;
            _performanceMetrics.PersistentSources = 0;
            _performanceMetrics.LastUpdate = DateTime.Now;
            
            var loadData = new AudioLoadData
            {
                Timestamp = DateTime.Now,
                ActiveSources = _effectsProcessor?.ActiveEffectSourcesCount ?? 0,
                CPULoad = 0f
            };
            
            _performanceHistory.Enqueue(loadData);
            
            while (_performanceHistory.Count > 300)
                _performanceHistory.Dequeue();
        }
        
        #endregion
        
        #region Utility Methods

        private float CalculateHVACIntensity(EnvironmentalConditions conditions)
        {
            float tempDiff = Mathf.Abs(conditions.Temperature - 24f) / 10f;
            float humidityDiff = Mathf.Abs(conditions.Humidity - 60f) / 40f;
            return Mathf.Clamp01(Mathf.Max(tempDiff, humidityDiff));
        }
        
        private float CalculateElectricalActivity()
        {
            return UnityEngine.Random.Range(0.3f, 0.8f);
        }
        
        #endregion
        
        #region Public Interface
        
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            _masterMixer.SetFloat("MasterVolume", Mathf.Log10(_masterVolume) * 20f);
            _effectsProcessor?.SetMasterVolume(_masterVolume);
            
            OnVolumeChanged?.Invoke(_masterVolume);
        }
        
        public void SetAudioMuted(bool muted)
        {
            _isAudioMuted = muted;
            _masterMixer.SetFloat("MasterVolume", muted ? -80f : Mathf.Log10(_masterVolume) * 20f);
            _effectsProcessor?.SetAudioMuted(muted);
        }
        
        public void SetAudioState(AudioState newState)
        {
            if (_currentAudioState == newState) return;
            
            _currentAudioState = newState;
            
            UpdateAdaptiveMixing();
            
            string newSoundscape = newState switch
            {
                AudioState.Facility => "facility_ambient",
                AudioState.Construction => "construction_ambient",
                AudioState.Menu => "menu_ambient",
                _ => "facility_ambient"
            };
            
            SetSoundscape(newSoundscape);
        }
        
        public void SetMixerGroupVolume(string groupName, float volume)
        {
            if (_masterMixer != null)
            {
                _masterMixer.SetFloat($"{groupName}Volume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
            }
        }
        
        public AudioPerformanceReport GetPerformanceReport()
        {
            return new AudioPerformanceReport
            {
                Metrics = _performanceMetrics,
                PerformanceHistory = _performanceHistory.ToList(),
                SoundscapeInfo = _currentSoundscape,
                ActiveSourceCount = _effectsProcessor?.ActiveEffectSourcesCount ?? 0
            };
        }
        
        /// <summary>
        /// Get or add component to this GameObject
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        #endregion
        
        protected override void OnManagerShutdown()
        {
            StopAllCoroutines();
            CancelInvoke();
            
            // Cleanup components
            _effectsProcessor?.Cleanup();
            _loadingService?.ClearCache();
            
            // Stop music
            if (_musicSource != null)
                _musicSource.Stop();
            
            // Stop soundscape layers
            foreach (var layerSource in _soundscapeLayers.Values)
            {
                if (layerSource != null)
                    layerSource.Stop();
            }
            _soundscapeLayers.Clear();
            
            // Cleanup music controller
            _musicController?.Cleanup();
            
            // Disconnect system events
            DisconnectSystemEvents();
            
            LogInfo("Advanced Audio Manager shutdown complete");
        }
        
        private void DisconnectSystemEvents()
        {
            // Event unsubscription handled through component cleanup
        }
    }
}