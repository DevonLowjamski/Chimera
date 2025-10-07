using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;


namespace ProjectChimera.Data.Visuals
{
    /// <summary>
    /// Defines growth stage transitions and visual configurations for SpeedTree seasonal parameter control.
    /// Maps cannabis lifecycle stages to SpeedTree's 0-1 seasonal system with smooth transition curves.
    /// </summary>
    [CreateAssetMenu(fileName = "New Growth Stage Configuration", menuName = "Project Chimera/Visuals/Growth Stage Configuration", order = 3)]
    public class GrowthStageConfigurationSO : ChimeraDataSO
    {
        [Header("Growth Stage Definitions")]
        [SerializeField] private List<GrowthStageConfig> _growthStageConfigs = new List<GrowthStageConfig>();

        [Header("Transition Settings")]
        [SerializeField] private AnimationCurve _defaultTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Range(0.1f, 10f)] private float _defaultTransitionDuration = 2f;
        [SerializeField] private bool _enableSmoothTransitions = true;

        [Header("Environmental Influence")]
        [SerializeField] private EnvironmentalStageInfluence _environmentalInfluence;

        [Header("Strain-Specific Modifiers")]
        [SerializeField] private bool _enableStrainModifiers = true;
        [SerializeField] private List<StrainStageModifier> _strainModifiers = new List<StrainStageModifier>();

        // Public Properties
        public List<GrowthStageConfig> GrowthStageConfigs => _growthStageConfigs;
        public AnimationCurve DefaultTransitionCurve => _defaultTransitionCurve;
        public float DefaultTransitionDuration => _defaultTransitionDuration;
        public bool EnableSmoothTransitions => _enableSmoothTransitions;
        public EnvironmentalStageInfluence EnvironmentalInfluence => _environmentalInfluence;

        /// <summary>
        /// Gets the SpeedTree seasonal parameter for a specific growth stage.
        /// </summary>
        public float GetSeasonalParameterForStage(PlantGrowthStage stage)
        {
            var config = GetGrowthStageConfig(stage);
            return config?.SeasonalParameter ?? 0f;
        }

        /// <summary>
        /// Gets the growth stage configuration for a specific stage.
        /// </summary>
        public GrowthStageConfig GetGrowthStageConfig(PlantGrowthStage stage)
        {
            return _growthStageConfigs.Find(config => config.Stage == stage);
        }

        /// <summary>
        /// Calculates the modified seasonal parameter based on strain and environmental factors.
        /// </summary>
        public float CalculateModifiedSeasonalParameter(PlantGrowthStage stage, GeneticPlantStrainSO strain, EnvironmentalConditions environment)
        {
            float baseParameter = GetSeasonalParameterForStage(stage);

            // Apply strain modifiers
            if (_enableStrainModifiers && strain != null)
            {
                baseParameter = ApplyStrainModifier(baseParameter, stage, strain);
            }

            // Apply environmental influence
            if (_environmentalInfluence.EnableEnvironmentalInfluence)
            {
                baseParameter = ApplyEnvironmentalInfluence(baseParameter, stage, environment);
            }

            return Mathf.Clamp01(baseParameter);
        }

        /// <summary>
        /// Gets the transition configuration between two growth stages.
        /// </summary>
        public StageTransition GetTransitionConfig(PlantGrowthStage fromStage, PlantGrowthStage toStage)
        {
            var fromConfig = GetGrowthStageConfig(fromStage);
            var toConfig = GetGrowthStageConfig(toStage);

            if (fromConfig == null || toConfig == null)
            {
                return CreateDefaultTransition(fromStage, toStage);
            }

            // Check for custom transition
            var customTransition = fromConfig.CustomTransitions.Find(t => t.ToStage == toStage);
            if (customTransition != null)
            {
                return customTransition;
            }

            // Use default transition
            return CreateDefaultTransition(fromStage, toStage);
        }

        /// <summary>
        /// Gets all available growth stages in chronological order.
        /// </summary>
        public List<PlantGrowthStage> GetOrderedGrowthStages()
        {
            var orderedStages = new List<PlantGrowthStage>();

            foreach (PlantGrowthStage stage in System.Enum.GetValues(typeof(PlantGrowthStage)))
            {
                if (GetGrowthStageConfig(stage) != null)
                {
                    orderedStages.Add(stage);
                }
            }

            return orderedStages;
        }

        /// <summary>
        /// Validates that all essential growth stages are configured.
        /// </summary>
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            // Check for essential stages
            PlantGrowthStage[] essentialStages = {
                PlantGrowthStage.Seedling,
                PlantGrowthStage.Vegetative,
                PlantGrowthStage.Flowering,
                PlantGrowthStage.Harvest
            };

            foreach (var stage in essentialStages)
            {
                if (GetGrowthStageConfig(stage) == null)
                {
                    SharedLogger.LogWarning($"[Chimera] GrowthStageConfigurationSO '{DisplayName}' missing essential stage: {stage}.", this);
                    isValid = false;
                }
            }

            // Validate seasonal parameter progression
            ValidateSeasonalProgression();

            return isValid;
        }

        private float ApplyStrainModifier(float baseParameter, PlantGrowthStage stage, GeneticPlantStrainSO strain)
        {
            var modifier = _strainModifiers.Find(m => m.TargetStrain == strain || m.StrainType == strain.StrainType);
            if (modifier != null)
            {
                var stageModifier = modifier.StageModifiers.Find(sm => sm.Stage == stage);
                if (stageModifier != null)
                {
                    return baseParameter + stageModifier.SeasonalParameterOffset;
                }
            }

            return baseParameter;
        }

        private float ApplyEnvironmentalInfluence(float baseParameter, PlantGrowthStage stage, EnvironmentalConditions environment)
        {
            float influence = 0f;

            // Temperature influence
            if (_environmentalInfluence.TemperatureInfluence.enabled)
            {
                float tempInfluence = _environmentalInfluence.TemperatureInfluence.influenceCurve.Evaluate(environment.Temperature / 40f); // Normalize to 0-1
                influence += tempInfluence * _environmentalInfluence.TemperatureInfluence.influenceStrength;
            }

            // Light influence
            if (_environmentalInfluence.LightInfluence.enabled)
            {
                float lightInfluence = _environmentalInfluence.LightInfluence.influenceCurve.Evaluate(environment.LightIntensity / 1000f); // Normalize to 0-1
                influence += lightInfluence * _environmentalInfluence.LightInfluence.influenceStrength;
            }

            // Stress influence
            float overallStress = CalculateEnvironmentalStress(environment);
            if (_environmentalInfluence.StressInfluence.enabled && overallStress > 0.3f)
            {
                float stressInfluence = _environmentalInfluence.StressInfluence.influenceCurve.Evaluate(overallStress);
                influence += stressInfluence * _environmentalInfluence.StressInfluence.influenceStrength;
            }

            return baseParameter + (influence * _environmentalInfluence.MaxEnvironmentalInfluence);
        }

        private float CalculateEnvironmentalStress(EnvironmentalConditions environment)
        {
            // Simplified stress calculation - would typically use strain-specific optimal ranges
            float tempStress = Mathf.Abs(environment.Temperature - 24f) / 20f; // Optimal around 24°C
            float humidityStress = Mathf.Abs(environment.Humidity - 55f) / 50f; // Optimal around 55%

            return (tempStress + humidityStress) * 0.5f;
        }

        private StageTransition CreateDefaultTransition(PlantGrowthStage fromStage, PlantGrowthStage toStage)
        {
            return new StageTransition(fromStage, toStage, _defaultTransitionCurve, _defaultTransitionDuration, false);
        }

        private void ValidateSeasonalProgression()
        {
            var configs = new List<GrowthStageConfig>(_growthStageConfigs);
            configs.Sort((a, b) => ((int)a.Stage).CompareTo((int)b.Stage));

            for (int i = 1; i < configs.Count; i++)
            {
                if (configs[i].SeasonalParameter < configs[i - 1].SeasonalParameter)
                {
                    SharedLogger.LogWarning($"[Chimera] GrowthStageConfigurationSO '{DisplayName}': Seasonal parameter regression detected between {configs[i - 1].Stage} and {configs[i].Stage}.", this);
                }
            }
        }
    }

    /// <summary>
    /// Configuration for a specific growth stage.
    /// </summary>
    [System.Serializable]
    public class GrowthStageConfig
    {
        [SerializeField] private PlantGrowthStage _stage;
        [SerializeField, Range(0f, 1f)] private float _seasonalParameter = 0f;
        [SerializeField, TextArea(2, 4)] private string _stageDescription;

        [Header("Visual Properties")]
        [SerializeField] private Color _primaryColor = Color.green;
        [SerializeField] private Color _secondaryColor = Color.green;
        [SerializeField, Range(0f, 2f)] private float _scaleMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float _densityMultiplier = 1f;

        [Header("Duration")]
        [SerializeField] private Vector2 _stageDurationDays = new Vector2(7, 14);

        [Header("Custom Transitions")]
        [SerializeField] private List<StageTransition> _customTransitions = new List<StageTransition>();

        // Public Properties
        public PlantGrowthStage Stage => _stage;
        public float SeasonalParameter => _seasonalParameter;
        public string StageDescription => _stageDescription;
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public float ScaleMultiplier => _scaleMultiplier;
        public float DensityMultiplier => _densityMultiplier;
        public Vector2 StageDurationDays => _stageDurationDays;
        public List<StageTransition> CustomTransitions => _customTransitions;
    }

    /// <summary>
    /// Defines a transition between two growth stages.
    /// </summary>
    [System.Serializable]
    public class StageTransition
    {
        [SerializeField] private PlantGrowthStage _fromStage;
        [SerializeField] private PlantGrowthStage _toStage;
        [SerializeField] private bool _useCustomCurve = false;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Range(0.1f, 20f)] private float _transitionDuration = 2f;
        [SerializeField] private bool _enableColorTransition = true;
        [SerializeField] private bool _enableScaleTransition = true;

        public StageTransition() { }

        public StageTransition(PlantGrowthStage fromStage, PlantGrowthStage toStage, AnimationCurve transitionCurve, float duration, bool useCustomCurve = false)
        {
            _fromStage = fromStage;
            _toStage = toStage;
            _transitionCurve = transitionCurve;
            _transitionDuration = duration;
            _useCustomCurve = useCustomCurve;
        }

        // Public Properties
        public PlantGrowthStage FromStage => _fromStage;
        public PlantGrowthStage ToStage => _toStage;
        public bool UseCustomCurve => _useCustomCurve;
        public AnimationCurve TransitionCurve => _transitionCurve;
        public float TransitionDuration => _transitionDuration;
        public bool EnableColorTransition => _enableColorTransition;
        public bool EnableScaleTransition => _enableScaleTransition;

        public float EvaluateTransition(float normalizedTime)
        {
            return _transitionCurve.Evaluate(normalizedTime);
        }
    }

    /// <summary>
    /// Configuration for environmental influence on growth stage visualization.
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStageInfluence
    {
        [SerializeField] private bool _enableEnvironmentalInfluence = true;
        [SerializeField, Range(0f, 0.5f)] private float _maxEnvironmentalInfluence = 0.2f;

        [SerializeField] private EnvironmentalFactor _temperatureInfluence = new EnvironmentalFactor();
        [SerializeField] private EnvironmentalFactor _lightInfluence = new EnvironmentalFactor();
        [SerializeField] private EnvironmentalFactor _stressInfluence = new EnvironmentalFactor();

        public bool EnableEnvironmentalInfluence => _enableEnvironmentalInfluence;
        public float MaxEnvironmentalInfluence => _maxEnvironmentalInfluence;
        public EnvironmentalFactor TemperatureInfluence => _temperatureInfluence;
        public EnvironmentalFactor LightInfluence => _lightInfluence;
        public EnvironmentalFactor StressInfluence => _stressInfluence;
    }

    /// <summary>
    /// Defines how a specific environmental factor influences visual parameters.
    /// </summary>
    [System.Serializable]
    public class EnvironmentalFactor
    {
        [SerializeField] public bool enabled = true;
        [SerializeField, Range(0f, 1f)] public float influenceStrength = 0.3f;
        [SerializeField] public AnimationCurve influenceCurve = AnimationCurve.Linear(0f, -0.5f, 1f, 0.5f);
    }

    /// <summary>
    /// Strain-specific modifiers for growth stage visualization.
    /// </summary>
    [System.Serializable]
    public class StrainStageModifier
    {
        [SerializeField] private GeneticPlantStrainSO _targetStrain;
        [SerializeField] private ProjectChimera.Data.Genetics.StrainType _strainType = ProjectChimera.Data.Genetics.StrainType.Hybrid;
        [SerializeField] private bool _useStrainType = false; // If true, applies to all strains of this type
        [SerializeField] private List<StageModifier> _stageModifiers = new List<StageModifier>();

        public GeneticPlantStrainSO TargetStrain => _targetStrain;
        public ProjectChimera.Data.Genetics.StrainType StrainType => _strainType;
        public bool UseStrainType => _useStrainType;
        public List<StageModifier> StageModifiers => _stageModifiers;
    }

    /// <summary>
    /// Modifier values for a specific stage.
    /// </summary>
    [System.Serializable]
    public class StageModifier
    {
        [SerializeField] private PlantGrowthStage _stage;
        [SerializeField, Range(-0.3f, 0.3f)] private float _seasonalParameterOffset = 0f;
        [SerializeField, Range(0.5f, 2f)] private float _scaleModifier = 1f;
        [SerializeField] private Color _colorTint = Color.white;

        public PlantGrowthStage Stage => _stage;
        public float SeasonalParameterOffset => _seasonalParameterOffset;
        public float ScaleModifier => _scaleModifier;
        public Color ColorTint => _colorTint;
    }
}
