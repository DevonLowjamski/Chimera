using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// UI and system feedback effects library
    /// </summary>
    [CreateAssetMenu(fileName = "UI System Effects Library", menuName = "Project Chimera/Effects/UI System Effects")]
    public class UISystemEffectsSO : ScriptableObject
    {
        [Header("UI Feedback Effects")]
        [SerializeField] private List<EffectPrefabEntry> _uiFeedbackEffects = new List<EffectPrefabEntry>();
        
        [Header("System Notification Effects")]
        [SerializeField] private List<EffectPrefabEntry> _notificationEffects = new List<EffectPrefabEntry>();
        
        [Header("Achievement Effects")]
        [SerializeField] private List<EffectPrefabEntry> _achievementEffects = new List<EffectPrefabEntry>();
        
        [Header("Status Effects")]
        [SerializeField] private List<EffectPrefabEntry> _statusEffects = new List<EffectPrefabEntry>();
        
        public List<EffectPrefabEntry> UIFeedbackEffects => _uiFeedbackEffects;
        public List<EffectPrefabEntry> NotificationEffects => _notificationEffects;
        public List<EffectPrefabEntry> AchievementEffects => _achievementEffects;
        public List<EffectPrefabEntry> StatusEffects => _statusEffects;
        
        public void InitializeDefaults()
        {
            CreateUIFeedbackEffects();
            CreateNotificationEffects();
            CreateAchievementEffects();
            CreateStatusEffects();
        }
        
        private void CreateUIFeedbackEffects()
        {
            // Button Click Sparkle
            _uiFeedbackEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "button_click_sparkle",
                PrefabName = "Button Click Sparkle",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.UI,
                Duration = 0.5f,
                IntensityRange = new Vector2(0.8f, 1.2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 15,
                    EmissionRate = 30f,
                    ParticleLifetime = 0.8f,
                    StartColor = Color.white,
                    EndColor = new Color(0.8f, 0.8f, 1f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.zero,
                    Shape = ParticleSystemShapeType.Circle
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.3f,
                    Pitch = 1.2f,
                    Is3D = false,
                    MinDistance = 1f,
                    MaxDistance = 5f
                }
            });

            // Hover Glow Effect
            _uiFeedbackEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "hover_glow",
                PrefabName = "Hover Glow",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.UI,
                Duration = -1f, // Persistent while hovering
                IntensityRange = new Vector2(0.3f, 0.8f),
                RequiredComponents = new List<string> { "Light" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    Color = new Color(0.3f, 0.8f, 1f),
                    Intensity = 0.5f,
                    Range = 2f,
                    FlickerSpeed = 0.1f,
                    EnableFlicker = true
                }
            });

            // Selection Ring
            _uiFeedbackEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "selection_ring",
                PrefabName = "Selection Ring",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.UI,
                Duration = -1f, // Persistent while selected
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 20,
                    EmissionRate = 8f,
                    ParticleLifetime = 2f,
                    StartColor = new Color(1f, 0.8f, 0f, 0.8f),
                    EndColor = new Color(1f, 1f, 0f, 0f),
                    StartSize = 0.05f,
                    EndSize = 0.1f,
                    VelocityOverLifetime = Vector3.zero,
                    Shape = ParticleSystemShapeType.Circle
                }
            });
        }
        
        private void CreateNotificationEffects()
        {
            // System Alert Flash
            _notificationEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "system_alert_flash",
                PrefabName = "System Alert Flash",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.System,
                Duration = 1f,
                IntensityRange = new Vector2(1f, 2f),
                RequiredComponents = new List<string> { "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    Color = Color.red,
                    Intensity = 3f,
                    Range = 5f,
                    FlickerSpeed = 5f,
                    EnableFlicker = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.5f,
                    Pitch = 1.5f,
                    Is3D = false,
                    MinDistance = 2f,
                    MaxDistance = 10f
                }
            });

            // Information Ping
            _notificationEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "information_ping",
                PrefabName = "Information Ping",
                Prefab = null,
                EffectType = EffectType.Composite,
                EffectCategory = EffectCategory.System,
                Duration = 2f,
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "ParticleSystem", "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 10,
                    EmissionRate = 5f,
                    ParticleLifetime = 1.5f,
                    StartColor = Color.blue,
                    EndColor = new Color(0.5f, 0.8f, 1f, 0f),
                    StartSize = 0.1f,
                    EndSize = 0.3f,
                    VelocityOverLifetime = Vector3.up * 0.5f,
                    Shape = ParticleSystemShapeType.Circle
                },
                LightProperties = new LightEffectProperties
                {
                    Color = Color.blue,
                    Intensity = 1f,
                    Range = 3f,
                    FlickerSpeed = 2f,
                    EnableFlicker = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 1.3f,
                    Is3D = false,
                    MinDistance = 1f,
                    MaxDistance = 8f
                }
            });
        }
        
        private void CreateAchievementEffects()
        {
            // Achievement Fireworks
            _achievementEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "achievement_fireworks",
                PrefabName = "Achievement Fireworks",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Achievement,
                Duration = 3f,
                IntensityRange = new Vector2(1f, 2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.High,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 200,
                    EmissionRate = 100f,
                    ParticleLifetime = 2f,
                    StartColor = Color.yellow,
                    EndColor = Color.red,
                    StartSize = 0.1f,
                    EndSize = 0.05f,
                    VelocityOverLifetime = Vector3.up * 5f,
                    Shape = ParticleSystemShapeType.Cone
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.7f,
                    Pitch = 1f,
                    Is3D = false,
                    MinDistance = 5f,
                    MaxDistance = 25f
                }
            });

            // Goal Completion Burst
            _achievementEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "goal_completion_burst",
                PrefabName = "Goal Completion Burst",
                Prefab = null,
                EffectType = EffectType.Composite,
                EffectCategory = EffectCategory.Achievement,
                Duration = 2f,
                IntensityRange = new Vector2(1.5f, 2.5f),
                RequiredComponents = new List<string> { "ParticleSystem", "Light", "AudioSource" },
                PerformanceCost = PerformanceCost.Medium,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 80,
                    EmissionRate = 50f,
                    ParticleLifetime = 1.5f,
                    StartColor = Color.gold,
                    EndColor = Color.white,
                    StartSize = 0.15f,
                    EndSize = 0.3f,
                    VelocityOverLifetime = Vector3.up * 3f,
                    Shape = ParticleSystemShapeType.Sphere
                },
                LightProperties = new LightEffectProperties
                {
                    Color = Color.yellow,
                    Intensity = 4f,
                    Range = 8f,
                    FlickerSpeed = 1f,
                    EnableFlicker = true
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.6f,
                    Pitch = 1.1f,
                    Is3D = false,
                    MinDistance = 3f,
                    MaxDistance = 15f
                }
            });
        }
        
        private void CreateStatusEffects()
        {
            // Low Resource Warning
            _statusEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "low_resource_warning",
                PrefabName = "Low Resource Warning",
                Prefab = null,
                EffectType = EffectType.Light,
                EffectCategory = EffectCategory.Status,
                Duration = -1f, // Persistent while condition is true
                IntensityRange = new Vector2(0.5f, 1f),
                RequiredComponents = new List<string> { "Light" },
                PerformanceCost = PerformanceCost.Low,
                LightProperties = new LightEffectProperties
                {
                    Color = Color.red,
                    Intensity = 1f,
                    Range = 2f,
                    FlickerSpeed = 1f,
                    EnableFlicker = true
                }
            });

            // Process Complete Indicator
            _statusEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "process_complete",
                PrefabName = "Process Complete",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Status,
                Duration = 1.5f,
                IntensityRange = new Vector2(0.8f, 1.2f),
                RequiredComponents = new List<string> { "ParticleSystem", "AudioSource" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 30,
                    EmissionRate = 20f,
                    ParticleLifetime = 1f,
                    StartColor = Color.green,
                    EndColor = Color.white,
                    StartSize = 0.08f,
                    EndSize = 0.15f,
                    VelocityOverLifetime = Vector3.up * 1.5f,
                    Shape = ParticleSystemShapeType.Circle
                },
                AudioProperties = new AudioEffectProperties
                {
                    AudioClip = null,
                    Volume = 0.4f,
                    Pitch = 1.4f,
                    Is3D = true,
                    MinDistance = 2f,
                    MaxDistance = 8f
                }
            });

            // Progress Indicator
            _statusEffects.Add(new EffectPrefabEntry
            {
                PrefabId = "progress_indicator",
                PrefabName = "Progress Indicator",
                Prefab = null,
                EffectType = EffectType.Particle,
                EffectCategory = EffectCategory.Status,
                Duration = -1f, // Persistent during progress
                IntensityRange = new Vector2(0.3f, 0.7f),
                RequiredComponents = new List<string> { "ParticleSystem" },
                PerformanceCost = PerformanceCost.Low,
                ParticleProperties = new ParticleEffectProperties
                {
                    MaxParticles = 12,
                    EmissionRate = 4f,
                    ParticleLifetime = 3f,
                    StartColor = Color.cyan,
                    EndColor = new Color(0.5f, 1f, 1f, 0f),
                    StartSize = 0.03f,
                    EndSize = 0.06f,
                    VelocityOverLifetime = Vector3.up * 0.8f,
                    Shape = ParticleSystemShapeType.Circle
                }
            });
        }
        
        public EffectPrefabEntry GetEffectById(string effectId)
        {
            return _uiFeedbackEffects.Concat(_notificationEffects)
                                    .Concat(_achievementEffects)
                                    .Concat(_statusEffects)
                                    .FirstOrDefault(e => e.PrefabId == effectId);
        }
        
        public List<EffectPrefabEntry> GetEffectsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "ui" or "feedback" => _uiFeedbackEffects,
                "notification" or "system" => _notificationEffects,
                "achievement" => _achievementEffects,
                "status" => _statusEffects,
                _ => new List<EffectPrefabEntry>()
            };
        }
    }
}