using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Core data structures, enums, and base classes for the effects system
    /// </summary>

    // Core effect data structure
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

    // Effect property classes
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
        public Color Color = Color.white;
        public float Intensity = 1f;
        public float Range = 10f;
        public float FlickerSpeed = 0f;
        public bool EnableFlicker = false;

        // Legacy properties for backward compatibility
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

    // Core enums
    public enum EffectType
    {
        Particle,
        Audio,
        Light,
        PostProcess,
        Animation,
        Shader,
        Composite
    }

    public enum EffectCategory
    {
        Plant,
        Environmental,
        System,
        UI,
        Interaction,
        Atmosphere,
        Feedback,
        Achievement,
        Status,
        Environment
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

    // Effect recommendation and validation classes
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
}
