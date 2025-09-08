using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Environmental effects library for weather, atmosphere, and ambient effects
    /// </summary>
    [CreateAssetMenu(fileName = "Environmental Effects Library", menuName = "Project Chimera/Effects/Environmental Effects")]
    public class EnvironmentalEffectsSO : ScriptableObject
    {
        [Header("Weather Effects")]
        [SerializeField] private List<EffectPrefabEntry> _weatherEffects = new List<EffectPrefabEntry>();
        
        [Header("Atmospheric Effects")]
        [SerializeField] private List<EffectPrefabEntry> _atmosphericEffects = new List<EffectPrefabEntry>();
        
        [Header("Lighting Effects")]
        [SerializeField] private List<EffectPrefabEntry> _lightingEffects = new List<EffectPrefabEntry>();
        
        [Header("Ambient Effects")]
        [SerializeField] private List<EffectPrefabEntry> _ambientEffects = new List<EffectPrefabEntry>();
        
        public List<EffectPrefabEntry> WeatherEffects => _weatherEffects;
        public List<EffectPrefabEntry> AtmosphericEffects => _atmosphericEffects;
        public List<EffectPrefabEntry> LightingEffects => _lightingEffects;
        public List<EffectPrefabEntry> AmbientEffects => _ambientEffects;
        
        public void InitializeDefaults()
        {
            CreateWeatherEffects();
            CreateAtmosphericEffects();
            CreateLightingEffects();
            CreateAmbientEffects();
        }
        
        private void CreateWeatherEffects()
        {
            // Rain Effect
            _weatherEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "rain_effect",
                PrefabName = "Rain Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.3f, 1.5f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 1000,
                    EmissionRate = 200f,
                    ParticleLifetime = 2f,
                    StartColor = Color.white,
                    EndColor = new Color(0.8f, 0.8f, 1f, 0.3f),
                    StartSize = 0.02f,
                    EndSize = 0.02f,
                    VelocityOverLifetime = new Vector3(0, -15f, 0),
                    Shape = ParticleSystemShapeType.Box
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 1f,
                    Is3D = false,
                    MinDistance = 5f,
                    MaxDistance = 50f
                }
            });

            // Wind Gust Particles
            _weatherEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "wind_gust",
                PrefabName = "Wind Gust",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = 3f,
                IntensityRange = new Vector2(0.5f, 1.2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 50,
                    EmissionRate = 15f,
                    ParticleLifetime = 4f,
                    StartColor = new Color(0.9f, 0.9f, 0.9f, 0.3f),
                    EndColor = new Color(0.8f, 0.8f, 0.8f, 0f),
                    StartSize = 0.1f,
                    EndSize = 0.2f,
                    VelocityOverLifetime = new Vector3(5f, 0.5f, 0),
                    Shape = ParticleSystemShapeType.Cone
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.3f,
                    Pitch = 0.8f,
                    Is3D = true,
                    MinDistance = 3f,
                    MaxDistance = 20f
                }
            });

            // Snow Effect
            _weatherEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "snow_effect",
                PrefabName = "Snow Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.2f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 500,
                    EmissionRate = 80f,
                    ParticleLifetime = 6f,
                    StartColor = Color.white,
                    EndColor = Color.white,
                    StartSize = 0.05f,
                    EndSize = 0.05f,
                    VelocityOverLifetime = new Vector3(0.5f, -2f, 0.3f),
                    Shape = ParticleSystemShapeType.Box
                }
            });
        }
        
        private void CreateAtmosphericEffects()
        {
            // Fog Effect
            _atmosphericEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "fog_effect",
                PrefabName = "Fog Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.1f, 0.8f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.High,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 200,
                    EmissionRate = 20f,
                    ParticleLifetime = 15f,
                    StartColor = new Color(0.9f, 0.9f, 0.95f, 0.4f),
                    EndColor = new Color(0.8f, 0.8f, 0.85f, 0.1f),
                    StartSize = 2f,
                    EndSize = 4f,
                    VelocityOverLifetime = new Vector3(0.2f, 0.1f, 0.1f),
                    Shape = ParticleSystemShapeType.Box
                }
            });

            // Dust Motes
            _atmosphericEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "dust_motes",
                PrefabName = "Dust Motes",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.3f, 0.7f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 100,
                    EmissionRate = 5f,
                    ParticleLifetime = 20f,
                    StartColor = new Color(1f, 0.9f, 0.7f, 0.6f),
                    EndColor = new Color(0.8f, 0.7f, 0.5f, 0.2f),
                    StartSize = 0.01f,
                    EndSize = 0.02f,
                    VelocityOverLifetime = new Vector3(0.1f, 0.05f, 0.1f),
                    Shape = ParticleSystemShapeType.Box
                }
            });
        }
        
        private void CreateLightingEffects()
        {
            // Sunbeam Effect
            _lightingEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "sunbeam_effect",
                PrefabName = "Sunbeam Effect",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.3f, 1f),
                RequiredComponents = new List<string> { "Light", "ParticleSystem" },
                PerformanceCost = PerformanceCost.Medium,
                LightProperties = new LightEffectProperties
                {
                    Color = new Color(1f, 0.9f, 0.6f),
                    Intensity = 1.5f,
                    Range = 10f,
                    FlickerSpeed = 0.05f,
                    EnableFlicker = true
                },
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 30,
                    EmissionRate = 2f,
                    ParticleLifetime = 8f,
                    StartColor = new Color(1f, 0.9f, 0.6f, 0.3f),
                    EndColor = new Color(1f, 0.8f, 0.4f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.up * 0.2f,
                    Shape = ParticleSystemShapeType.Cone
                }
            });

            // Lightning Flash
            _lightingEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "lightning_flash",
                PrefabName = "Lightning Flash",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.Environment,
                Duration = 0.5f,
                IntensityRange = new Vector2(2f, 5f),
                RequiredComponents = new List<string> { "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    Color = new Color(0.9f, 0.9f, 1f),
                    Intensity = 8f,
                    Range = 50f,
                    FlickerSpeed = 10f,
                    EnableFlicker = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.7f,
                    Pitch = 1f,
                    Is3D = false,
                    MinDistance = 20f,
                    MaxDistance = 100f
                }
            });
        }
        
        private void CreateAmbientEffects()
        {
            // Firefly Effect
            _ambientEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "fireflies",
                PrefabName = "Fireflies",
                Prefab = null,
                EffectType = EffectType.Composite,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.2f, 0.6f),
                RequiredComponents = new List<string> { "ParticleSystem", "Light" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 20,
                    EmissionRate = 1f,
                    ParticleLifetime = 10f,
                    StartColor = new Color(1f, 1f, 0.3f, 0.8f),
                    EndColor = new Color(0.8f, 1f, 0.3f, 0.3f),
                    StartSize = 0.02f,
                    EndSize = 0.03f,
                    VelocityOverLifetime = new Vector3(0.5f, 0.2f, 0.3f),
                    Shape = ParticleSystemShapeType.Sphere
                },
                LightProperties = new LightEffectProperties
                {
                    Color = new Color(1f, 1f, 0.3f),
                    Intensity = 0.5f,
                    Range = 1.5f,
                    FlickerSpeed = 0.3f,
                    EnableFlicker = true
                }
            });

            // Ambient Sparkles
            _ambientEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "ambient_sparkles",
                PrefabName = "Ambient Sparkles",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environment,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.1f, 0.5f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 15,
                    EmissionRate = 2f,
                    ParticleLifetime = 5f,
                    StartColor = Color.white,
                    EndColor = new Color(0.8f, 0.8f, 1f, 0f),
                    StartSize = 0.01f,
                    EndSize = 0.02f,
                    VelocityOverLifetime = Vector3.up * 0.1f,
                    Shape = ParticleSystemShapeType.Sphere
                }
            });
        }
        
        public EffectPrefabEntry GetEffectById(string effectId)
        {
            return _weatherEffects.Concat(_atmosphericEffects)
                                 .Concat(_lightingEffects)
                                 .Concat(_ambientEffects)
                                 .FirstOrDefault(e => e.PrefabId == effectId);
        }
        
        public List<EffectPrefabEntry> GetEffectsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "weather" => _weatherEffects,
                "atmospheric" => _atmosphericEffects,
                "lighting" => _lightingEffects,
                "ambient" => _ambientEffects,
                _ => new List<EffectPrefabEntry>()
            };
        }
    }
}