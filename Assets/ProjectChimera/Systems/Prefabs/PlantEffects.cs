using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Plant-related visual and audio effects library
    /// </summary>
    [CreateAssetMenu(fileName = "Plant Effects Library", menuName = "Project Chimera/Effects/Plant Effects")]
    public class PlantEffectsSO : ScriptableObject
    {
        [Header("Plant Growth Effects")]
        [SerializeField] private List<EffectPrefabEntry> _growthEffects = new List<EffectPrefabEntry>();

        [Header("Plant Health Effects")]
        [SerializeField] private List<EffectPrefabEntry> _healthEffects = new List<EffectPrefabEntry>();

        [Header("Harvest Effects")]
        [SerializeField] private List<EffectPrefabEntry> _harvestEffects = new List<EffectPrefabEntry>();

        [Header("Maintenance Effects")]
        [SerializeField] private List<EffectPrefabEntry> _maintenanceEffects = new List<EffectPrefabEntry>();

        public List<EffectPrefabEntry> GrowthEffects => _growthEffects;
        public List<EffectPrefabEntry> HealthEffects => _healthEffects;
        public List<EffectPrefabEntry> HarvestEffects => _harvestEffects;
        public List<EffectPrefabEntry> MaintenanceEffects => _maintenanceEffects;

        public void InitializeDefaults()
        {
            CreatePlantGrowthEffects();
            CreatePlantHealthEffects();
            CreateHarvestEffects();
            CreateMaintenanceEffects();
        }

        private void CreatePlantGrowthEffects()
        {
            // Plant Growth Sparkles
            _growthEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "plant_growth_sparkles",
                PrefabName = "Plant Growth Sparkles",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 3f,
                IntensityRange = new Vector2(0.5f, 1.5f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 50,
                    EmissionRate = 15f,
                    ParticleLifetime = 2f,
                    StartColor = Color.green,
                    EndColor = new Color(0.5f, 1f, 0.5f, 0f),
                    StartSize = 0.1f,
                    EndSize = 0.05f,
                    VelocityOverLifetime = Vector3.up * 2f,
                    Shape = ParticleSystemShapeType.Circle
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.3f,
                    Pitch = 1.2f,
                    Is3D = true,
                    MinDistance = 2f,
                    MaxDistance = 10f
                }
            });

            // Growth Stage Transition
            _growthEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "growth_stage_transition",
                PrefabName = "Growth Stage Transition",
                Prefab = null,
                EffectType = EffectType.Composite,
                EffectCategory = EffectCategory.Plant,
                Duration = 2f,
                IntensityRange = new Vector2(0.8f, 1.2f),
                RequiredComponents = new List<string> { "ParticleSystem", "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 75,
                    EmissionRate = 25f,
                    ParticleLifetime = 1.5f,
                    StartColor = Color.yellow,
                    EndColor = Color.green,
                    StartSize = 0.2f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.up * 1.5f,
                    Shape = ParticleSystemShapeType.Sphere
                },
                LightProperties = new LightEffectProperties
                {
                    Color = Color.green,
                    Intensity = 2f,
                    Range = 3f,
                    FlickerSpeed = 0.5f,
                    EnableFlicker = true
                }
            });
        }

        private void CreatePlantHealthEffects()
        {
            // Healthy Plant Glow
            _healthEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "healthy_plant_glow",
                PrefabName = "Healthy Plant Glow",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.Plant,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.3f, 0.8f),
                RequiredComponents = new List<string> { "Light" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    Color = new Color(0.5f, 1f, 0.5f),
                    Intensity = 0.5f,
                    Range = 1.5f,
                    FlickerSpeed = 0.1f,
                    EnableFlicker = true
                }
            });

            // Disease Warning Particles
            _healthEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "disease_warning",
                PrefabName = "Disease Warning",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 5f,
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 30,
                    EmissionRate = 8f,
                    ParticleLifetime = 3f,
                    StartColor = Color.red,
                    EndColor = new Color(1f, 0.5f, 0f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.up * 0.5f,
                    Shape = ParticleSystemShapeType.Circle
                }
            });
        }

        private void CreateHarvestEffects()
        {
            // Harvest Ready Shimmer
            _harvestEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "harvest_ready_shimmer",
                PrefabName = "Harvest Ready Shimmer",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = -1f, // Persistent
                IntensityRange = new Vector2(0.3f, 0.7f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 20,
                    EmissionRate = 5f,
                    ParticleLifetime = 4f,
                    StartColor = Color.white,
                    EndColor = new Color(1f, 1f, 0.5f, 0f),
                    StartSize = 0.03f,
                    EndSize = 0.08f,
                    VelocityOverLifetime = Vector3.up * 0.3f,
                    Shape = ParticleSystemShapeType.Circle
                }
            });

            // Harvest Completion Burst
            _harvestEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "harvest_completion_burst",
                PrefabName = "Harvest Completion Burst",
                Prefab = null,
                EffectType = EffectType.Composite,
                EffectCategory = EffectCategory.Plant,
                Duration = 1.5f,
                IntensityRange = new Vector2(1f, 1.5f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 100,
                    EmissionRate = 60f,
                    ParticleLifetime = 1f,
                    StartColor = Color.yellow,
                    EndColor = Color.white,
                    StartSize = 0.1f,
                    EndSize = 0.2f,
                    VelocityOverLifetime = Vector3.up * 3f,
                    Shape = ParticleSystemShapeType.Sphere
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.6f,
                    Pitch = 1f,
                    Is3D = true,
                    MinDistance = 3f,
                    MaxDistance = 15f
                }
            });
        }

        private void CreateMaintenanceEffects()
        {
            // Watering Effect
            _maintenanceEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "watering_effect",
                PrefabName = "Watering Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 2f,
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 40,
                    EmissionRate = 20f,
                    ParticleLifetime = 1.5f,
                    StartColor = Color.blue,
                    EndColor = new Color(0.5f, 0.8f, 1f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.down * 2f,
                    Shape = ParticleSystemShapeType.Cone
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 0.9f,
                    Is3D = true,
                    MinDistance = 1f,
                    MaxDistance = 8f
                }
            });

            // Fertilizer Application
            _maintenanceEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "fertilizer_application",
                PrefabName = "Fertilizer Application",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 3f,
                IntensityRange = new Vector2(0.4f, 0.8f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 25,
                    EmissionRate = 10f,
                    ParticleLifetime = 2f,
                    StartColor = new Color(0.8f, 0.6f, 0.2f),
                    EndColor = new Color(0.6f, 0.4f, 0.1f, 0f),
                    StartSize = 0.02f,
                    EndSize = 0.05f,
                    VelocityOverLifetime = Vector3.down * 0.5f,
                    Shape = ParticleSystemShapeType.Circle
                }
            });
        }

        public EffectPrefabEntry GetEffectById(string effectId)
        {
            return _growthEffects.Concat(_healthEffects)
                                .Concat(_harvestEffects)
                                .Concat(_maintenanceEffects)
                                .FirstOrDefault(e => e.PrefabId == effectId);
        }

        public List<EffectPrefabEntry> GetEffectsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "growth" => _growthEffects,
                "health" => _healthEffects,
                "harvest" => _harvestEffects,
                "maintenance" => _maintenanceEffects,
                _ => new List<EffectPrefabEntry>()
            };
        }
    }
}
