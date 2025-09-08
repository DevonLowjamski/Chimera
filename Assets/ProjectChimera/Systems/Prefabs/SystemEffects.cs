using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// System and equipment effects library for power, alerts, data processing, and interaction feedback
    /// </summary>
    [CreateAssetMenu(fileName = "System Effects Library", menuName = "Project Chimera/Effects/System Effects")]
    public class SystemEffectsSO : ScriptableObject
    {
        [Header("Equipment Effects")]
        [SerializeField] private List<EffectPrefabEntry> _equipmentEffects = new List<EffectPrefabEntry>();
        
        [Header("System Alert Effects")]
        [SerializeField] private List<EffectPrefabEntry> _alertEffects = new List<EffectPrefabEntry>();
        
        [Header("Data Processing Effects")]
        [SerializeField] private List<EffectPrefabEntry> _processingEffects = new List<EffectPrefabEntry>();
        
        [Header("Interaction Effects")]
        [SerializeField] private List<EffectPrefabEntry> _interactionEffects = new List<EffectPrefabEntry>();
        
        public List<EffectPrefabEntry> EquipmentEffects => _equipmentEffects;
        public List<EffectPrefabEntry> AlertEffects => _alertEffects;
        public List<EffectPrefabEntry> ProcessingEffects => _processingEffects;
        public List<EffectPrefabEntry> InteractionEffects => _interactionEffects;
        
        public void InitializeDefaults()
        {
            CreateEquipmentEffects();
            CreateAlertEffects();
            CreateProcessingEffects();
            CreateInteractionEffects();
        }
        
        private void CreateEquipmentEffects()
        {
            // Equipment Power On
            _equipmentEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "equipment_power_on",
                PrefabName = "Equipment Power On Effect",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.System,
                Duration = 1.5f,
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "Light", "AudioSource", "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    StartIntensity = 0f,
                    EndIntensity = 1f,
                    LightColor = Color.green,
                    FlickerFrequency = 5f,
                    FlickerDuration = 0.5f
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.5f,
                    Pitch = 1f,
                    Is3D = true,
                    MinDistance = 2f,
                    MaxDistance = 10f
                }
            });
            
            // Facility Ambient Hum
            _equipmentEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "facility_ambient_hum",
                PrefabName = "Facility Ambient Hum",
                Prefab = null,
                EffectType = EffectType.Audio,
                EffectCategory = EffectCategory.Atmosphere,
                Duration = 0f, // Continuous
                IntensityRange = new Vector2(0.1f, 0.4f),
                RequiredComponents = new List<string> { "AudioSource" },
                PerformanceCost = PerformanceCost.VeryLow,
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.15f,
                    Pitch = 0.8f,
                    Is3D = true,
                    MinDistance = 5f,
                    MaxDistance = 30f,
                    Loop = true
                }
            });
        }
        
        private void CreateAlertEffects()
        {
            // System Alert
            _alertEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "system_alert_effect",
                PrefabName = "System Alert Effect",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.System,
                Duration = 3f,
                IntensityRange = new Vector2(0.8f, 1f),
                RequiredComponents = new List<string> { "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    StartIntensity = 0f,
                    EndIntensity = 1f,
                    LightColor = Color.red,
                    FlickerFrequency = 2f,
                    PulsePattern = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.7f,
                    Pitch = 1.5f,
                    Is3D = true,
                    MinDistance = 5f,
                    MaxDistance = 20f,
                    Loop = true
                }
            });
            
            // Ambient Atmosphere
            _alertEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "ambient_atmosphere",
                PrefabName = "Ambient Atmosphere",
                Prefab = null,
                EffectType = EffectType.Audio,
                EffectCategory = EffectCategory.Atmosphere,
                Duration = 0f, // Continuous
                IntensityRange = new Vector2(0.1f, 0.5f),
                RequiredComponents = new List<string> { "AudioSource" },
                PerformanceCost = PerformanceCost.VeryLow,
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.2f,
                    Pitch = 1f,
                    Is3D = false,
                    Loop = true
                }
            });
        }
        
        private void CreateProcessingEffects()
        {
            // Data Processing
            _processingEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "data_processing_effect",
                PrefabName = "Data Processing Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.System,
                Duration = 2f,
                IntensityRange = new Vector2(0.3f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 30,
                    EmissionRate = 15f,
                    ParticleLifetime = 1f,
                    StartColor = Color.cyan,
                    EndColor = new Color(0f, 1f, 1f, 0f),
                    StartSize = 0.1f,
                    EndSize = 0.05f,
                    VelocityOverLifetime = Vector3.up * 1f,
                    Shape = ParticleSystemShapeType.Cone,
                    UseTrails = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.2f,
                    Pitch = 2f,
                    Is3D = false
                }
            });
            
            // Air Circulation Particles
            _processingEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "air_circulation_particles",
                PrefabName = "Air Circulation Particles",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environmental,
                Duration = 0f, // Continuous
                IntensityRange = new Vector2(0.3f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 75,
                    EmissionRate = 20f,
                    ParticleLifetime = 4f,
                    StartColor = new Color(0.8f, 0.9f, 1f, 0.3f),
                    EndColor = new Color(0.8f, 0.9f, 1f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.forward * 2f,
                    Shape = ParticleSystemShapeType.Box,
                    UseVelocityInheritance = true
                }
            });
        }
        
        private void CreateInteractionEffects()
        {
            // Click Feedback
            _interactionEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "ui_click_feedback",
                PrefabName = "UI Click Feedback",
                Prefab = null,
                EffectType = EffectType.Audio,
                EffectCategory = EffectCategory.UI,
                Duration = 0.2f,
                IntensityRange = new Vector2(0.8f, 1f),
                RequiredComponents = new List<string> { "AudioSource" },
                PerformanceCost = PerformanceCost.VeryLow,
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.6f,
                    Pitch = 1f,
                    Is3D = false
                }
            });
            
            // Hover Effect
            _interactionEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "ui_hover_effect",
                PrefabName = "UI Hover Effect",
                Prefab = null,
                EffectType = EffectType.Audio,
                EffectCategory = EffectCategory.UI,
                Duration = 0.1f,
                IntensityRange = new Vector2(0.5f, 0.8f),
                RequiredComponents = new List<string> { "AudioSource" },
                PerformanceCost = PerformanceCost.VeryLow,
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.3f,
                    Pitch = 1.2f,
                    Is3D = false
                }
            });
            
            // Object Selection
            _interactionEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "object_selection_effect",
                PrefabName = "Object Selection Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Interaction,
                Duration = 0.5f,
                IntensityRange = new Vector2(0.8f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 20,
                    EmissionRate = 40f,
                    ParticleLifetime = 0.5f,
                    StartColor = Color.yellow,
                    EndColor = new Color(1f, 1f, 0f, 0f),
                    StartSize = 0.2f,
                    EndSize = 0.1f,
                    Shape = ParticleSystemShapeType.Circle,
                    UseRadialVelocity = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 1.5f,
                    Is3D = true,
                    MinDistance = 1f,
                    MaxDistance = 5f
                }
            });
        }
        
        public EffectPrefabEntry GetEffectById(string effectId)
        {
            return _equipmentEffects.Concat(_alertEffects)
                                  .Concat(_processingEffects)
                                  .Concat(_interactionEffects)
                                  .FirstOrDefault(e => e.PrefabId == effectId);
        }
        
        public List<EffectPrefabEntry> GetEffectsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "equipment" => _equipmentEffects,
                "alert" or "alerts" => _alertEffects,
                "processing" or "data" => _processingEffects,
                "interaction" => _interactionEffects,
                _ => new List<EffectPrefabEntry>()
            };
        }
    }
}