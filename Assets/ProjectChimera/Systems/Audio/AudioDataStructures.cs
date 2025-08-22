using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ProjectChimera.Systems.Audio
{
    /// <summary>
    /// Comprehensive data structures for the advanced audio system.
    /// Includes soundscapes, audio libraries, performance tracking, and music management.
    /// </summary>
    
    // Core Audio Enums
    public enum AudioState
    {
        Menu,
        Facility,
        Construction,
        Genetics,
        Research,
        Cinematic
    }
    
    public enum SoundscapeLayer
    {
        Ambient,
        HVAC,
        Electrical,
        Ventilation,
        Growth,
        Water,
        Mechanical
    }
    
    public enum AudioAlertType
    {
        Environmental,
        System,
        Plant,
        Construction,
        Economic,
        Research
    }
    
    // Soundscape System
    [System.Serializable]
    public class DynamicSoundscape
    {
        public string SoundscapeId;
        public string SoundscapeName;
        public AudioState TargetState;
        public List<SoundscapeLayerData> Layers = new List<SoundscapeLayerData>();
        public float TransitionDuration = 2f;
        public bool IsLooping = true;
        public EnvironmentalTriggers Triggers;
    }
    
    [System.Serializable]
    public class SoundscapeLayerData
    {
        public SoundscapeLayer LayerType;
        public AudioClip AudioClip;
        public float Volume = 1f;
        public float Pitch = 1f;
        public bool IsRandomized = false;
        public Vector2 VolumeRange = new Vector2(0.8f, 1.2f);
        public Vector2 PitchRange = new Vector2(0.9f, 1.1f);
        public float RandomizationInterval = 30f;
    }
    
    [System.Serializable]
    public class EnvironmentalTriggers
    {
        public bool ReactToTemperature = true;
        public bool ReactToHumidity = true;
        public bool ReactToLighting = true;
        public bool ReactToAirflow = true;
        public bool ReactToPlantCount = true;
        public float TriggerSensitivity = 1f;
    }
    
    // Audio Library System
    [System.Serializable]
    public class AudioClipEntry
    {
        public string ClipId;
        public string ClipName;
        public AudioClip AudioClip;
        public AudioCategory Category;
        public float DefaultVolume = 1f;
        public float DefaultPitch = 1f;
        public bool Is3D = false;
        public float MaxDistance = 20f;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;
        public List<string> Tags = new List<string>();
    }
    
    public enum AudioCategory
    {
        UI,
        Environment,
        Plant,
        Equipment,
        Construction,
        Alert,
        Music,
        Ambient,
        Voice,
        Effect
    }
    
    // Environmental Audio
    [System.Serializable]
    public class EnvironmentalAudioSource
    {
        public string SourceId;
        public AudioSource AudioSource;
        public Vector3 Position;
        public float MaxDistance;
        public float BaseVolume;
        public EnvironmentalAudioType AudioType;
        public bool IsWeatherDependent = false;
        public bool IsTemperatureDependent = false;
    }
    
    public enum EnvironmentalAudioType
    {
        HVAC,
        Lighting,
        Ventilation,
        Pump,
        Fan,
        Motor,
        Electronic,
        Mechanical,
        Water,
        Air
    }
    
    // Music System
    [System.Serializable]
    public class MusicTrack
    {
        public string TrackId;
        public string TrackName;
        public AudioClip AudioClip;
        public MusicGenre Genre;
        public MusicMood Mood;
        public float Duration;
        public bool CanLoop = true;
        public float FadeInTime = 2f;
        public float FadeOutTime = 2f;
        public List<string> Tags = new List<string>();
    }
    
    public enum MusicGenre
    {
        Ambient,
        Electronic,
        Cinematic,
        Industrial,
        Nature,
        Minimal,
        Experimental
    }
    
    public enum MusicMood
    {
        Calm,
        Focused,
        Energetic,
        Mysterious,
        Tense,
        Peaceful,
        Productive,
        Scientific
    }
    
    [System.Serializable]
    public class MusicPlaylist
    {
        public string PlaylistId;
        public string PlaylistName;
        public List<string> TrackIds = new List<string>();
        public PlaylistMode PlayMode = PlaylistMode.Sequential;
        public bool Shuffle = false;
        public float CrossfadeTime = 3f;
    }
    
    public enum PlaylistMode
    {
        Sequential,
        Random,
        Weighted,
        MoodBased,
        Adaptive
    }
    
    // Audio Processing
    [System.Serializable]
    public class AudioEffect
    {
        public string EffectId;
        public AudioEffectType EffectType;
        public bool IsEnabled = true;
        public float Intensity = 1f;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
    }
    
    public enum AudioEffectType
    {
        Reverb,
        Echo,
        LowPass,
        HighPass,
        Distortion,
        Chorus,
        Flanger,
        Compressor,
        Limiter,
        EQ
    }
    
    // Procedural Audio
    [System.Serializable]
    public class ProceduralAudioGenerator
    {
        public string GeneratorId;
        public ProceduralAudioType AudioType;
        public bool IsActive = true;
        public float GenerationFrequency = 1f;
        public AudioParameterRange VolumeRange;
        public AudioParameterRange PitchRange;
        public AudioParameterRange DurationRange;
        public List<AudioClip> SourceClips = new List<AudioClip>();
        
        /// <summary>
        /// Cleanup method for the procedural audio generator
        /// </summary>
        public void Cleanup()
        {
            IsActive = false;
            SourceClips?.Clear();
        }
    }
    
    public enum ProceduralAudioType
    {
        PlantGrowth,
        WaterDrops,
        ElectricalHum,
        AirBubbles,
        MachineryNoise,
        RandomAmbient
    }
    
    [System.Serializable]
    public class AudioParameterRange
    {
        public float MinValue;
        public float MaxValue;
        public AnimationCurve DistributionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        public float GetRandomValue()
        {
            float t = UnityEngine.Random.value;
            float curveValue = DistributionCurve.Evaluate(t);
            return Mathf.Lerp(MinValue, MaxValue, curveValue);
        }
    }
    
    // Performance Monitoring
    [System.Serializable]
    public class AudioPerformanceMetrics
    {
        public int MaxConcurrentSources;
        public int ActiveSources;
        public int AvailableSources;
        public int EnvironmentalSources;
        public int PersistentSources;
        public int PoolSize;
        public float CPUUsage;
        public float MemoryUsage;
        public bool SpatialAudioEnabled;
        public DateTime LastUpdate;
    }
    
    [System.Serializable]
    public class AudioLoadData
    {
        public DateTime Timestamp;
        public int ActiveSources;
        public float CPULoad;
        public float MemoryLoad;
        public int DroppedSamples;
    }
    
    [System.Serializable]
    public class AudioPerformanceReport
    {
        public AudioPerformanceMetrics Metrics;
        public List<AudioLoadData> PerformanceHistory;
        public DynamicSoundscape SoundscapeInfo;
        public int ActiveSourceCount;
        public List<string> ActiveAudioClips;
        public float AverageLatency;
    }
    
    // Audio Events and Alerts
    [System.Serializable]
    public class AudioAlert
    {
        public AudioAlertType AlertType;
        public string Message;
        public DateTime Timestamp;
        public float Priority = 1f;
        public AudioClip AlertSound;
        public bool RequiresAcknowledgment = false;
    }
    
    [System.Serializable]
    public class AudioEvent
    {
        public string EventId;
        public AudioEventType EventType;
        public Vector3 Position;
        public float Volume = 1f;
        public float Pitch = 1f;
        public bool Is3D = false;
        public AudioClip AudioClip;
        public DateTime Timestamp;
        public object EventData;
    }
    
    public enum AudioEventType
    {
        PlantGrowth,
        EnvironmentalChange,
        EquipmentActivation,
        SystemAlert,
        UserInteraction,
        ConstructionActivity,
        Research,
        Economic
    }
    
    // Audio Mixer Configuration
    [System.Serializable]
    public class AudioMixerConfiguration
    {
        public AudioMixer MasterMixer;
        public Dictionary<string, AudioMixerGroup> MixerGroups;
        public Dictionary<string, float> DefaultVolumes;
        public List<AudioEffect> GlobalEffects;
        public bool EnableDynamicRange = true;
        public float CompressionThreshold = -12f;
        public float CompressionRatio = 4f;
    }
    
    // Music Controller
    public class MusicController
    {
        private MusicPlaylistSO _currentPlaylist;
        private int _currentTrackIndex = 0;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private float _currentVolume = 1f;
        
        public MusicController(MusicPlaylistSO playlist)
        {
            _currentPlaylist = playlist;
        }
        
        public void PlayTrack(MusicTrack track, AudioSource musicSource, bool loop = true, float fadeInTime = 2f)
        {
            if (musicSource != null && track?.AudioClip != null)
            {
                musicSource.clip = track.AudioClip;
                musicSource.loop = loop;
                musicSource.volume = 0f;
                musicSource.Play();
                
                // Fade in
                FadeToVolume(musicSource, _currentVolume, fadeInTime);
                _isPlaying = true;
            }
        }
        
        public void StopCurrentTrack(AudioSource musicSource, float fadeOutTime = 2f)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                FadeToVolume(musicSource, 0f, fadeOutTime, () =>
                {
                    musicSource.Stop();
                    _isPlaying = false;
                });
            }
        }
        
        public void SetVolume(float volume)
        {
            _currentVolume = Mathf.Clamp01(volume);
        }
        
        /// <summary>
        /// Cleanup method for the music controller
        /// </summary>
        public void Cleanup()
        {
            _isPlaying = false;
            _isPaused = false;
            _currentTrackIndex = 0;
            _currentPlaylist = null;
        }
        
        private void FadeToVolume(AudioSource source, float targetVolume, float duration, System.Action onComplete = null)
        {
            // Implementation would use coroutine or DOTween for smooth volume transitions
            source.volume = targetVolume;
            onComplete?.Invoke();
        }
    }
    
    // Audio Library ScriptableObjects
    [CreateAssetMenu(fileName = "New Audio Library", menuName = "Project Chimera/Audio/Audio Library")]
    public class AudioLibrarySO : ScriptableObject
    {
        [SerializeField] private List<AudioClipEntry> _audioClips = new List<AudioClipEntry>();
        [SerializeField] private List<AudioClipEntry> _uiSounds = new List<AudioClipEntry>();
        [SerializeField] private List<AudioClipEntry> _environmentalSounds = new List<AudioClipEntry>();
        
        private Dictionary<string, AudioClipEntry> _clipLookup;
        private Dictionary<AudioCategory, List<AudioClipEntry>> _categoryLookup;
        
        public void InitializeDefaults()
        {
            if (_audioClips.Count == 0)
            {
                CreateDefaultAudioClips();
            }
            
            BuildLookupTables();
        }
        
        private void CreateDefaultAudioClips()
        {
            // UI Sounds
            _uiSounds.AddRange(new[]
            {
                CreateAudioClipEntry("ui_click", "UI Click", AudioCategory.UI),
                CreateAudioClipEntry("ui_hover", "UI Hover", AudioCategory.UI),
                CreateAudioClipEntry("ui_error", "UI Error", AudioCategory.UI),
                CreateAudioClipEntry("ui_success", "UI Success", AudioCategory.UI),
                CreateAudioClipEntry("ui_notification", "Notification", AudioCategory.UI)
            });
            
            // Environmental Sounds
            _environmentalSounds.AddRange(new[]
            {
                CreateAudioClipEntry("hvac_running", "HVAC Running", AudioCategory.Environment, true, 15f),
                CreateAudioClipEntry("light_ballast_hum", "Light Ballast Hum", AudioCategory.Equipment, true, 10f),
                CreateAudioClipEntry("fan_running", "Fan Running", AudioCategory.Equipment, true, 12f),
                CreateAudioClipEntry("water_pump", "Water Pump", AudioCategory.Equipment, true, 8f),
                CreateAudioClipEntry("electrical_hum", "Electrical Hum", AudioCategory.Environment, true, 20f)
            });
            
            // Plant/Growth Sounds
            _audioClips.AddRange(new[]
            {
                CreateAudioClipEntry("plant_growth_subtle", "Plant Growth", AudioCategory.Plant),
                CreateAudioClipEntry("plant_watering", "Plant Watering", AudioCategory.Plant, true, 5f),
                CreateAudioClipEntry("plant_added", "Plant Added", AudioCategory.Plant),
                CreateAudioClipEntry("plant_flowering", "Plant Flowering", AudioCategory.Plant),
                CreateAudioClipEntry("plant_harvest_ready", "Harvest Ready", AudioCategory.Plant)
            });
            
            // Construction Sounds
            _audioClips.AddRange(new[]
            {
                CreateAudioClipEntry("construction_ambient", "Construction Ambient", AudioCategory.Construction, true, 25f),
                CreateAudioClipEntry("construction_started", "Construction Started", AudioCategory.Construction),
                CreateAudioClipEntry("construction_warning", "Construction Warning", AudioCategory.Alert)
            });
            
            // Alert Sounds
            _audioClips.AddRange(new[]
            {
                CreateAudioClipEntry("environmental_alert", "Environmental Alert", AudioCategory.Alert),
                CreateAudioClipEntry("temperature_warning", "Temperature Warning", AudioCategory.Alert),
                CreateAudioClipEntry("humidity_warning", "Humidity Warning", AudioCategory.Alert),
                CreateAudioClipEntry("system_error", "System Error", AudioCategory.Alert)
            });
        }
        
        private AudioClipEntry CreateAudioClipEntry(string id, string name, AudioCategory category, 
                                                   bool is3D = false, float maxDistance = 20f)
        {
            return new AudioClipEntry
            {
                ClipId = id,
                ClipName = name,
                Category = category,
                Is3D = is3D,
                MaxDistance = maxDistance,
                DefaultVolume = 1f,
                DefaultPitch = 1f,
                RolloffMode = AudioRolloffMode.Logarithmic
            };
        }
        
        private void BuildLookupTables()
        {
            _clipLookup = new Dictionary<string, AudioClipEntry>();
            _categoryLookup = new Dictionary<AudioCategory, List<AudioClipEntry>>();
            
            var allClips = new List<AudioClipEntry>();
            allClips.AddRange(_audioClips);
            allClips.AddRange(_uiSounds);
            allClips.AddRange(_environmentalSounds);
            
            foreach (var clip in allClips)
            {
                _clipLookup[clip.ClipId] = clip;
                
                if (!_categoryLookup.ContainsKey(clip.Category))
                {
                    _categoryLookup[clip.Category] = new List<AudioClipEntry>();
                }
                _categoryLookup[clip.Category].Add(clip);
            }
        }
        
        public AudioClip GetAudioClip(string clipId)
        {
            return _clipLookup.TryGetValue(clipId, out var entry) ? entry.AudioClip : null;
        }
        
        public AudioClip GetUISound(string soundId)
        {
            var uiSound = _uiSounds.FirstOrDefault(s => s.ClipId == soundId);
            return uiSound?.AudioClip;
        }
        
        public List<AudioClipEntry> GetClipsByCategory(AudioCategory category)
        {
            return _categoryLookup.TryGetValue(category, out var clips) ? clips : new List<AudioClipEntry>();
        }
        
        public AudioClipEntry GetClipEntry(string clipId)
        {
            return _clipLookup.TryGetValue(clipId, out var entry) ? entry : null;
        }
    }
    
    [CreateAssetMenu(fileName = "New Soundscape Library", menuName = "Project Chimera/Audio/Soundscape Library")]
    public class SoundscapeLibrarySO : ScriptableObject
    {
        [SerializeField] private List<DynamicSoundscape> _soundscapes = new List<DynamicSoundscape>();
        
        private Dictionary<string, DynamicSoundscape> _soundscapeLookup;
        
        public void InitializeDefaults()
        {
            if (_soundscapes.Count == 0)
            {
                CreateDefaultSoundscapes();
            }
            
            BuildLookupTable();
        }
        
        private void CreateDefaultSoundscapes()
        {
            // Facility Ambient Soundscape
            _soundscapes.Add(new DynamicSoundscape
            {
                SoundscapeId = "facility_ambient",
                SoundscapeName = "Facility Ambient",
                TargetState = AudioState.Facility,
                Layers = new List<SoundscapeLayerData>
                {
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.Ambient, Volume = 0.3f },
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.Electrical, Volume = 0.2f },
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.HVAC, Volume = 0.4f }
                }
            });
            
            // Construction Soundscape
            _soundscapes.Add(new DynamicSoundscape
            {
                SoundscapeId = "construction_ambient",
                SoundscapeName = "Construction Ambient",
                TargetState = AudioState.Construction,
                Layers = new List<SoundscapeLayerData>
                {
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.Ambient, Volume = 0.2f },
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.Mechanical, Volume = 0.6f }
                }
            });
            
            // Menu Soundscape
            _soundscapes.Add(new DynamicSoundscape
            {
                SoundscapeId = "menu_ambient",
                SoundscapeName = "Menu Ambient",
                TargetState = AudioState.Menu,
                Layers = new List<SoundscapeLayerData>
                {
                    new SoundscapeLayerData { LayerType = SoundscapeLayer.Ambient, Volume = 0.4f }
                }
            });
        }
        
        private void BuildLookupTable()
        {
            _soundscapeLookup = _soundscapes.ToDictionary(s => s.SoundscapeId, s => s);
        }
        
        public DynamicSoundscape GetSoundscape(string soundscapeId)
        {
            return _soundscapeLookup.TryGetValue(soundscapeId, out var soundscape) ? soundscape : null;
        }
        
        public List<DynamicSoundscape> GetSoundscapesByState(AudioState state)
        {
            return _soundscapes.Where(s => s.TargetState == state).ToList();
        }
    }
    
    [CreateAssetMenu(fileName = "New Music Playlist", menuName = "Project Chimera/Audio/Music Playlist")]
    public class MusicPlaylistSO : ScriptableObject
    {
        [SerializeField] private List<MusicTrack> _musicTracks = new List<MusicTrack>();
        [SerializeField] private List<MusicPlaylist> _playlists = new List<MusicPlaylist>();
        
        private Dictionary<string, MusicTrack> _trackLookup;
        private Dictionary<string, MusicPlaylist> _playlistLookup;
        
        public void InitializeDefaults()
        {
            if (_musicTracks.Count == 0)
            {
                CreateDefaultMusicTracks();
            }
            
            if (_playlists.Count == 0)
            {
                CreateDefaultPlaylists();
            }
            
            BuildLookupTables();
        }
        
        private void CreateDefaultMusicTracks()
        {
            _musicTracks.AddRange(new[]
            {
                new MusicTrack { TrackId = "facility_ambient_1", TrackName = "Facility Ambient 1", Genre = MusicGenre.Ambient, Mood = MusicMood.Calm },
                new MusicTrack { TrackId = "research_focus", TrackName = "Research Focus", Genre = MusicGenre.Electronic, Mood = MusicMood.Focused },
                new MusicTrack { TrackId = "construction_energy", TrackName = "Construction Energy", Genre = MusicGenre.Industrial, Mood = MusicMood.Energetic },
                new MusicTrack { TrackId = "menu_peaceful", TrackName = "Menu Peaceful", Genre = MusicGenre.Ambient, Mood = MusicMood.Peaceful }
            });
        }
        
        private void CreateDefaultPlaylists()
        {
            _playlists.Add(new MusicPlaylist
            {
                PlaylistId = "facility_playlist",
                PlaylistName = "Facility Background",
                TrackIds = new List<string> { "facility_ambient_1", "research_focus" },
                PlayMode = PlaylistMode.Sequential
            });
        }
        
        private void BuildLookupTables()
        {
            _trackLookup = _musicTracks.ToDictionary(t => t.TrackId, t => t);
            _playlistLookup = _playlists.ToDictionary(p => p.PlaylistId, p => p);
        }
        
        public MusicTrack GetTrack(string trackId)
        {
            return _trackLookup.TryGetValue(trackId, out var track) ? track : null;
        }
        
        public MusicPlaylist GetPlaylist(string playlistId)
        {
            return _playlistLookup.TryGetValue(playlistId, out var playlist) ? playlist : null;
        }
        
        public List<MusicTrack> GetTracksByMood(MusicMood mood)
        {
            return _musicTracks.Where(t => t.Mood == mood).ToList();
        }
    }
}