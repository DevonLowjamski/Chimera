using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Specialized prefab library for visual and audio effects including particle systems,
    /// post-processing effects, audio feedback, and environmental atmosphere.
    /// </summary>
    [CreateAssetMenu(fileName = "New Effects Prefab Library", menuName = "Project Chimera/Prefabs/Effects Library")]
    public class EffectsPrefabLibrarySO : ScriptableObject
    {
        [Header("Effect Categories")]
        [SerializeField] private List<EffectPrefabEntry> _effectPrefabs = new List<EffectPrefabEntry>();
        [SerializeField] private List<EffectSequence> _effectSequences = new List<EffectSequence>();
        
        [Header("Particle Systems")]
        [SerializeField] private List<ParticleEffectSet> _particleEffects = new List<ParticleEffectSet>();
        [SerializeField] private bool _enableParticlePooling = true;
        [SerializeField] private int _maxParticleInstances = 200;
        
        [Header("Audio Effects")]
        [SerializeField] private List<AudioEffectSet> _audioEffects = new List<AudioEffectSet>();
        [SerializeField] private bool _enable3DAudio = true;
        [SerializeField] private bool _enableDynamicMixing = true;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableLODEffects = true;
        [SerializeField] private float _cullingDistance = 50f;
        [SerializeField] private int _maxConcurrentEffects = 50;
        
        // Cached lookup tables
        private Dictionary<string, EffectPrefabEntry> _prefabLookup;
        private Dictionary<EffectType, List<EffectPrefabEntry>> _typeLookup;
        private Dictionary<string, EffectSequence> _sequenceLookup;
        
        public List<EffectPrefabEntry> EffectPrefabs => _effectPrefabs;
        
        public void InitializeDefaults()
        {
            if (_effectPrefabs.Count == 0)
            {
                CreateDefaultEffectPrefabs();
            }
            
            if (_effectSequences.Count == 0)
            {
                CreateDefaultEffectSequences();
            }
            
            BuildLookupTables();
        }
        
        private void CreateDefaultEffectPrefabs()
        {
            CreatePlantEffects();
            CreateEnvironmentalEffects();
            CreateSystemEffects();
            CreateInteractionEffects();
            CreateAtmosphericEffects();
        }
        
        private void CreatePlantEffects()
        {
            // Plant Growth Sparkles
            _effectPrefabs.Add(new EffectPrefabEntry
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
            
            // Watering Effect
            _effectPrefabs.Add(new EffectPrefabEntry
            {
                PrefabId = "plant_watering_effect",
                PrefabName = "Plant Watering Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 5f,
                IntensityRange = new Vector2(0.8f, 1.2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 100,
                    EmissionRate = 30f,
                    ParticleLifetime = 1.5f,
                    StartColor = new Color(0.3f, 0.6f, 1f, 0.8f),
                    EndColor = new Color(0.3f, 0.6f, 1f, 0f),
                    StartSize = 0.02f,
                    EndSize = 0.01f,
                    VelocityOverLifetime = Vector3.down * 5f,
                    Shape = ParticleSystemShapeType.Cone,
                    UseGravity = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 1f,
                    Is3D = true,
                    MinDistance = 1f,
                    MaxDistance = 8f,
                    Loop = true
                }
            });
            
            // Harvest Celebration
            _effectPrefabs.Add(new EffectPrefabEntry
            {
                PrefabId = "plant_harvest_celebration",
                PrefabName = "Harvest Celebration Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Plant,
                Duration = 2f,
                IntensityRange = new Vector2(1f, 2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource", "Light" },
                PerformanceCost = PerformanceCost.High,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 200,
                    EmissionRate = 100f,
                    ParticleLifetime = 3f,
                    StartColor = Color.yellow,
                    EndColor = new Color(1f, 0.8f, 0f, 0f),
                    StartSize = 0.2f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = new Vector3(0f, 3f, 0f),
                    Shape = ParticleSystemShapeType.Sphere,
                    UseColorOverLifetime = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.6f,
                    Pitch = 1.3f,
                    Is3D = true,
                    MinDistance = 3f,
                    MaxDistance = 15f
                }
            });
        }
        
        private void CreateEnvironmentalEffects()
        {
            // Air Circulation Particles
            _effectPrefabs.Add(new EffectPrefabEntry
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
            
            // Heat Shimmer Effect
            _effectPrefabs.Add(new EffectPrefabEntry
            {
                PrefabId = "heat_shimmer_effect",
                PrefabName = "Heat Shimmer Effect",
                Prefab = null,
                EffectType = EffectType.PostProcess,
                EffectCategory = EffectCategory.Environmental,
                Duration = 0f, // Continuous
                IntensityRange = new Vector2(0.1f, 0.5f),
                RequiredComponents = new List<string> { "PostProcessVolume", "HeatDistortion" },
                PerformanceCost = PerformanceCost.Medium,
                PostProcessProperties = new PostProcessEffectProperties
                {
                    DistortionStrength = 0.1f,
                    NoiseScale = 10f,
                    AnimationSpeed = 2f,
                    BlendMode = EffectBlendMode.Additive
                }
            });
            
            // Humidity Mist
            _effectPrefabs.Add(new EffectPrefabEntry
            {
                PrefabId = "humidity_mist_effect",
                PrefabName = "Humidity Mist Effect",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Environmental,
                Duration = 0f, // Continuous
                IntensityRange = new Vector2(0.2f, 0.8f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 150,
                    EmissionRate = 25f,
                    ParticleLifetime = 8f,
                    StartColor = new Color(1f, 1f, 1f, 0.2f),
                    EndColor = new Color(1f, 1f, 1f, 0f),
                    StartSize = 0.5f,
                    EndSize = 2f,
                    VelocityOverLifetime = Vector3.up * 0.5f,
                    Shape = ParticleSystemShapeType.Box,
                    UseSizeOverLifetime = true
                }
            });
        }
        
        private void CreateSystemEffects()
        {
            // Equipment Power On
            _effectPrefabs.Add(new EffectPrefabEntry
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
            
            // System Alert
            _effectPrefabs.Add(new EffectPrefabEntry
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
            
            // Data Processing
            _effectPrefabs.Add(new EffectPrefabEntry
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
        }
        
        private void CreateInteractionEffects()
        {
            // Click Feedback
            _effectPrefabs.Add(new EffectPrefabEntry
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
            _effectPrefabs.Add(new EffectPrefabEntry
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
            _effectPrefabs.Add(new EffectPrefabEntry
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
        
        private void CreateAtmosphericEffects()
        {
            // Ambient Atmosphere
            _effectPrefabs.Add(new EffectPrefabEntry
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
            
            // Facility Hum
            _effectPrefabs.Add(new EffectPrefabEntry
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
        
        private void CreateDefaultEffectSequences()
        {
            // Plant Growth Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "plant_growth_sequence",
                SequenceName = "Plant Growth Sequence",
                TotalDuration = 10f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_growth_sparkles", 
                        StartTime = 0f, 
                        Duration = 3f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_watering_effect", 
                        StartTime = 2f, 
                        Duration = 5f,
                        Intensity = 0.8f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_growth_sparkles", 
                        StartTime = 6f, 
                        Duration = 4f,
                        Intensity = 1.2f
                    }
                }
            });
            
            // System Startup Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "system_startup_sequence",
                SequenceName = "System Startup Sequence",
                TotalDuration = 5f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "equipment_power_on", 
                        StartTime = 0f, 
                        Duration = 1.5f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "data_processing_effect", 
                        StartTime = 1f, 
                        Duration = 2f,
                        Intensity = 0.8f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "air_circulation_particles", 
                        StartTime = 2f, 
                        Duration = 0f, // Continuous
                        Intensity = 0.6f
                    }
                }
            });
        }
        
        private void BuildLookupTables()
        {
            _prefabLookup = _effectPrefabs.ToDictionary(e => e.PrefabId, e => e);
            
            _typeLookup = _effectPrefabs.GroupBy(e => e.EffectType)
                                      .ToDictionary(g => g.Key, g => g.ToList());
            
            _sequenceLookup = _effectSequences.ToDictionary(s => s.SequenceId, s => s);
        }
        
        public EffectPrefabEntry GetEffectPrefab(EffectType effectType, string prefabId = null)
        {
            if (!string.IsNullOrEmpty(prefabId) && _prefabLookup.TryGetValue(prefabId, out var specificPrefab))
            {
                return specificPrefab;
            }
            
            if (_typeLookup.TryGetValue(effectType, out var typePrefabs) && typePrefabs.Count > 0)
            {
                return typePrefabs[0];
            }
            
            return null;
        }
        
        public List<EffectPrefabEntry> GetEffectsByType(EffectType effectType)
        {
            return _typeLookup.TryGetValue(effectType, out var effects) ? effects : new List<EffectPrefabEntry>();
        }
        
        public List<EffectPrefabEntry> GetEffectsByCategory(EffectCategory category)
        {
            return _effectPrefabs.Where(e => e.EffectCategory == category).ToList();
        }
        
        public List<EffectPrefabEntry> GetEffectsByPerformanceCost(PerformanceCost maxCost)
        {
            return _effectPrefabs.Where(e => e.PerformanceCost <= maxCost).ToList();
        }
        
        public EffectSequence GetEffectSequence(string sequenceId)
        {
            return _sequenceLookup.TryGetValue(sequenceId, out var sequence) ? sequence : null;
        }
        
        public EffectRecommendation GetEffectRecommendation(string context, EffectCategory category, PerformanceCost maxCost)
        {
            var availableEffects = GetEffectsByCategory(category)
                .Where(e => e.PerformanceCost <= maxCost)
                .ToList();
            
            var recommendation = new EffectRecommendation
            {
                Context = context,
                RecommendedEffects = new List<string>(),
                EstimatedPerformanceImpact = 0f,
                QualityScore = 0f
            };
            
            // Simple recommendation logic based on context
            switch (context.ToLower())
            {
                case "plant_interaction":
                    recommendation.RecommendedEffects.AddRange(
                        availableEffects.Where(e => e.PrefabId.Contains("plant")).Select(e => e.PrefabId)
                    );
                    break;
                    
                case "system_feedback":
                    recommendation.RecommendedEffects.AddRange(
                        availableEffects.Where(e => e.PrefabId.Contains("system") || e.PrefabId.Contains("equipment")).Select(e => e.PrefabId)
                    );
                    break;
                    
                case "environmental":
                    recommendation.RecommendedEffects.AddRange(
                        availableEffects.Where(e => e.EffectCategory == EffectCategory.Environmental).Select(e => e.PrefabId)
                    );
                    break;
            }
            
            // Calculate performance impact
            foreach (var effectId in recommendation.RecommendedEffects)
            {
                var effect = _prefabLookup.TryGetValue(effectId, out var e) ? e : null;
                if (effect != null)
                {
                    recommendation.EstimatedPerformanceImpact += (float)effect.PerformanceCost;
                }
            }
            
            recommendation.QualityScore = Mathf.Clamp01(1f - recommendation.EstimatedPerformanceImpact / 20f);
            
            return recommendation;
        }
        
        public EffectValidationResult ValidateEffectConfiguration(List<string> effectIds)
        {
            var result = new EffectValidationResult
            {
                IsValid = true,
                ValidationIssues = new List<string>(),
                PerformanceWarnings = new List<string>(),
                TotalPerformanceCost = 0f
            };
            
            // Check total performance cost
            foreach (var effectId in effectIds)
            {
                var effect = _prefabLookup.TryGetValue(effectId, out var e) ? e : null;
                if (effect != null)
                {
                    result.TotalPerformanceCost += (float)effect.PerformanceCost;
                }
                else
                {
                    result.ValidationIssues.Add($"Effect {effectId} not found");
                    result.IsValid = false;
                }
            }
            
            // Performance validation
            if (result.TotalPerformanceCost > 15f)
            {
                result.PerformanceWarnings.Add("High total performance cost may impact frame rate");
            }
            
            if (effectIds.Count > _maxConcurrentEffects)
            {
                result.ValidationIssues.Add($"Too many concurrent effects ({effectIds.Count} > {_maxConcurrentEffects})");
                result.IsValid = false;
            }
            
            // Check for conflicting effects
            ValidateEffectConflicts(effectIds, result);
            
            return result;
        }
        
        private void ValidateEffectConflicts(List<string> effectIds, EffectValidationResult result)
        {
            var particleEffects = effectIds.Where(id => 
                _prefabLookup.TryGetValue(id, out var effect) && effect.EffectType == EffectType.Particle
            ).ToList();
            
            if (particleEffects.Count > 10)
            {
                result.PerformanceWarnings.Add("Large number of particle effects may cause performance issues");
            }
            
            var audioEffects = effectIds.Where(id => 
                _prefabLookup.TryGetValue(id, out var effect) && effect.EffectType == EffectType.Audio
            ).ToList();
            
            if (audioEffects.Count > 20)
            {
                result.PerformanceWarnings.Add("Large number of audio effects may cause audio mixing issues");
            }
        }
        
        public EffectOptimizationSuggestion OptimizeEffectConfiguration(List<string> effectIds, PerformanceCost targetCost)
        {
            var suggestion = new EffectOptimizationSuggestion
            {
                OriginalEffects = effectIds,
                OptimizedEffects = new List<string>(),
                RemovableEffects = new List<string>(),
                AlternativeEffects = new Dictionary<string, string>(),
                PerformanceGain = 0f
            };
            
            float currentCost = 0f;
            float targetCostValue = (float)targetCost;
            
            // Calculate current cost and identify optimization opportunities
            foreach (var effectId in effectIds)
            {
                var effect = _prefabLookup.TryGetValue(effectId, out var e) ? e : null;
                if (effect != null)
                {
                    float effectCost = (float)effect.PerformanceCost;
                    currentCost += effectCost;
                    
                    if (effectCost <= targetCostValue)
                    {
                        suggestion.OptimizedEffects.Add(effectId);
                    }
                    else
                    {
                        // Find alternative with lower cost
                        var alternative = FindAlternativeEffect(effect, targetCost);
                        if (alternative != null)
                        {
                            suggestion.AlternativeEffects[effectId] = alternative.PrefabId;
                            suggestion.OptimizedEffects.Add(alternative.PrefabId);
                        }
                        else
                        {
                            suggestion.RemovableEffects.Add(effectId);
                        }
                    }
                }
            }
            
            // Calculate performance gain
            float optimizedCost = 0f;
            foreach (var effectId in suggestion.OptimizedEffects)
            {
                var effect = _prefabLookup.TryGetValue(effectId, out var e) ? e : null;
                if (effect != null)
                {
                    optimizedCost += (float)effect.PerformanceCost;
                }
            }
            
            suggestion.PerformanceGain = currentCost - optimizedCost;
            
            return suggestion;
        }
        
        private EffectPrefabEntry FindAlternativeEffect(EffectPrefabEntry originalEffect, PerformanceCost targetCost)
        {
            return _effectPrefabs
                .Where(e => e.EffectType == originalEffect.EffectType && 
                           e.EffectCategory == originalEffect.EffectCategory &&
                           e.PerformanceCost <= targetCost &&
                           e.PrefabId != originalEffect.PrefabId)
                .OrderByDescending(e => e.PerformanceCost) // Get the highest quality within budget
                .FirstOrDefault();
        }
        
        public EffectsLibraryStats GetLibraryStats()
        {
            return new EffectsLibraryStats
            {
                TotalEffects = _effectPrefabs.Count,
                TotalSequences = _effectSequences.Count,
                TypeDistribution = _typeLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
                CategoryDistribution = _effectPrefabs.GroupBy(e => e.EffectCategory)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AveragePerformanceCost = _effectPrefabs.Average(e => (float)e.PerformanceCost),
                ParticlePoolingEnabled = _enableParticlePooling,
                Audio3DEnabled = _enable3DAudio
            };
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                BuildLookupTables();
            }
        }
    }
    
    // Supporting data structures
    [System.Serializable]
    public class EffectPrefabEntry
    {
        public string PrefabId;
        public string PrefabName;
        public GameObject Prefab;
        public EffectType EffectType;
        public EffectCategory EffectCategory;
        public float Duration;
        public Vector2 IntensityRange = Vector2.one;
        public List<string> RequiredComponents = new List<string>();
        public PerformanceCost PerformanceCost;
        public ParticleEffectProperties ParticleProperties;
        public AudioEffectProperties AudioProperties;
        public LightEffectProperties LightProperties;
        public PostProcessEffectProperties PostProcessProperties;
    }
    
    [System.Serializable]
    public class ParticleEffectProperties
    {
        public int MaxParticles = 100;
        public float EmissionRate = 10f;
        public float ParticleLifetime = 1f;
        public Color StartColor = Color.white;
        public Color EndColor = Color.clear;
        public float StartSize = 1f;
        public float EndSize = 1f;
        public Vector3 VelocityOverLifetime = Vector3.zero;
        public ParticleSystemShapeType Shape = ParticleSystemShapeType.Circle;
        public bool UseGravity = false;
        public bool UseColorOverLifetime = false;
        public bool UseSizeOverLifetime = false;
        public bool UseVelocityInheritance = false;
        public bool UseTrails = false;
        public bool UseRadialVelocity = false;
    }
    
    [System.Serializable]
    public class AudioEffectProperties
    {
        public AudioClip AudioClip;
        public float Volume = 1f;
        public float Pitch = 1f;
        public bool Is3D = false;
        public float MinDistance = 1f;
        public float MaxDistance = 500f;
        public bool Loop = false;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;
    }
    
    [System.Serializable]
    public class LightEffectProperties
    {
        public float StartIntensity = 0f;
        public float EndIntensity = 1f;
        public Color LightColor = Color.white;
        public float FlickerFrequency = 0f;
        public float FlickerDuration = 0f;
        public bool PulsePattern = false;
        public AnimationCurve IntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
    
    [System.Serializable]
    public class PostProcessEffectProperties
    {
        public float DistortionStrength = 0.1f;
        public float NoiseScale = 1f;
        public float AnimationSpeed = 1f;
        public EffectBlendMode BlendMode = EffectBlendMode.Alpha;
        public bool UseDepthTexture = false;
        public bool UseNormalTexture = false;
    }
    
    [System.Serializable]
    public class EffectSequence
    {
        public string SequenceId;
        public string SequenceName;
        public float TotalDuration;
        public List<EffectSequenceStep> SequenceSteps = new List<EffectSequenceStep>();
        public bool Loop = false;
        public float LoopDelay = 0f;
    }
    
    [System.Serializable]
    public class EffectSequenceStep
    {
        public string EffectId;
        public float StartTime;
        public float Duration;
        public float Intensity = 1f;
        public Vector3 Position = Vector3.zero;
        public bool UseRelativePosition = true;
    }
    
    [System.Serializable]
    public class ParticleEffectSet
    {
        public string SetId;
        public string SetName;
        public List<string> ParticleEffectIds = new List<string>();
        public bool EnableBatching = true;
        public int MaxInstancesPerSet = 10;
    }
    
    [System.Serializable]
    public class AudioEffectSet
    {
        public string SetId;
        public string SetName;
        public List<string> AudioEffectIds = new List<string>();
        public float MasterVolume = 1f;
        public bool EnableRandomization = false;
    }
    
    [System.Serializable]
    public class EffectRecommendation
    {
        public string Context;
        public List<string> RecommendedEffects = new List<string>();
        public float EstimatedPerformanceImpact;
        public float QualityScore;
        public string ReasoningExplanation;
    }
    
    [System.Serializable]
    public class EffectValidationResult
    {
        public bool IsValid;
        public List<string> ValidationIssues = new List<string>();
        public List<string> PerformanceWarnings = new List<string>();
        public float TotalPerformanceCost;
    }
    
    [System.Serializable]
    public class EffectOptimizationSuggestion
    {
        public List<string> OriginalEffects = new List<string>();
        public List<string> OptimizedEffects = new List<string>();
        public List<string> RemovableEffects = new List<string>();
        public Dictionary<string, string> AlternativeEffects = new Dictionary<string, string>();
        public float PerformanceGain;
    }
    
    [System.Serializable]
    public class EffectsLibraryStats
    {
        public int TotalEffects;
        public int TotalSequences;
        public Dictionary<EffectType, int> TypeDistribution;
        public Dictionary<EffectCategory, int> CategoryDistribution;
        public float AveragePerformanceCost;
        public bool ParticlePoolingEnabled;
        public bool Audio3DEnabled;
    }
    
    // Enums
    public enum EffectType
    {
        Particle,
        Audio,
        Light,
        PostProcess,
        Animation,
        Shader
    }
    
    public enum EffectCategory
    {
        Plant,
        Environmental,
        System,
        UI,
        Interaction,
        Atmosphere,
        Feedback
    }
    
    public enum PerformanceCost
    {
        VeryLow = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        VeryHigh = 5
    }
    
    public enum EffectBlendMode
    {
        Alpha,
        Additive,
        Multiply,
        Screen,
        Overlay
    }
}