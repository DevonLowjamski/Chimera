using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Data.Visuals
{
    /// <summary>
    /// Maps cultivation parameters (nutrients, environment, genetics) to SpeedTree procedural controls.
    /// Enables real-time visual response to growing conditions in the simulation.
    /// </summary>
    [CreateAssetMenu(fileName = "New SpeedTree Parameter Mapper", menuName = "Project Chimera/Visuals/SpeedTree Parameter Mapper", order = 2)]
    public class SpeedTreeParameterMapperSO : ChimeraDataSO
    {
        [Header("Environmental Response Curves")]
        [SerializeField] private AnimationCurve _temperatureResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _humidityResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _lightResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _co2ResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Nutrient Response Curves")]
        [SerializeField] private AnimationCurve _nitrogenResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _phosphorusResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _potassiumResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("SpeedTree Parameter Mapping")]
        [SerializeField] private List<ParameterMapping> _parameterMappings = new List<ParameterMapping>();

        [Header("Stress Response Configuration")]
        [SerializeField] private StressResponseConfig _stressResponse;

        [Header("Genetic Trait Influence")]
        [SerializeField] private GeneticTraitInfluence _geneticInfluence;

        // Public Properties
        public List<ParameterMapping> ParameterMappings => _parameterMappings;
        public StressResponseConfig StressResponse => _stressResponse;
        public GeneticTraitInfluence GeneticInfluence => _geneticInfluence;

        /// <summary>
        /// Calculates SpeedTree parameters based on current cultivation conditions and plant genetics.
        /// </summary>
        public SpeedTreeParameters CalculateSpeedTreeParameters(CultivationConditions conditions, PlantStrainSO strain)
        {
            var parameters = new SpeedTreeParameters();

            // Calculate environmental stress
            float environmentalStress = CalculateEnvironmentalStress(conditions, strain);

            // Calculate nutrient effects
            float nutrientHealth = CalculateNutrientHealth(conditions);

            // Apply genetic modifiers
            float geneticScaleFactor = CalculateGeneticScaleFactor(strain);

            // Map to SpeedTree parameters
            parameters.OverallHealth = (1f - environmentalStress) * nutrientHealth;
            parameters.GrowthRate = parameters.OverallHealth * geneticScaleFactor;
            parameters.SeasonalParameter = CalculateSeasonalParameter(conditions, strain);
            parameters.FoliageDensity = CalculateFoliageDensity(conditions, strain);
            parameters.BranchDensity = CalculateBranchDensity(conditions, strain);
            parameters.LeafSize = CalculateLeafSize(conditions, strain);
            parameters.TrunkThickness = CalculateTrunkThickness(conditions, strain);
            parameters.ColorVariation = CalculateColorVariation(conditions, strain);

            // Apply stress responses
            ApplyStressResponses(ref parameters, environmentalStress, conditions);

            return parameters;
        }

        private float CalculateEnvironmentalStress(CultivationConditions conditions, PlantStrainSO strain)
        {
            if (strain?.BaseSpecies == null) return 0f;

            float tempStress = CalculateParameterStress(conditions.Temperature, new Vector2(18f, 28f), _temperatureResponseCurve);
            float humidityStress = CalculateParameterStress(conditions.Humidity, new Vector2(40f, 70f), _humidityResponseCurve);
            float lightStress = CalculateParameterStress(conditions.LightIntensity, new Vector2(200f, 800f), _lightResponseCurve);
            float co2Stress = CalculateParameterStress(conditions.CO2Level, new Vector2(300f, 1500f), _co2ResponseCurve);

            return (tempStress + humidityStress + lightStress + co2Stress) * 0.25f;
        }

        private float CalculateParameterStress(float value, Vector2 optimalRange, AnimationCurve responseCurve)
        {
            // Normalize value to 0-1 based on optimal range
            float normalizedValue = Mathf.InverseLerp(optimalRange.x, optimalRange.y, value);

            // Apply response curve
            float response = responseCurve.Evaluate(normalizedValue);

            // Convert to stress (inverse of response)
            return 1f - response;
        }

        private float CalculateNutrientHealth(CultivationConditions conditions)
        {
            float nResponse = _nitrogenResponseCurve.Evaluate(conditions.NitrogenLevel);
            float pResponse = _phosphorusResponseCurve.Evaluate(conditions.PhosphorusLevel);
            float kResponse = _potassiumResponseCurve.Evaluate(conditions.PotassiumLevel);

            return (nResponse + pResponse + kResponse) / 3f;
        }

        private float CalculateGeneticScaleFactor(PlantStrainSO strain)
        {
            if (strain == null) return 1f;

            float baseScale = strain.HeightModifier * strain.GrowthRateModifier;
            float geneticVariation = _geneticInfluence.BaseGeneticVariation;

            return baseScale * (1f + geneticVariation);
        }

        private float CalculateSeasonalParameter(CultivationConditions conditions, PlantStrainSO strain)
        {
            // This would typically be driven by growth stage, but can be influenced by environment
            float baseParameter = conditions.GrowthStageProgress;

            // Apply environmental influence
            float environmentalInfluence = CalculateEnvironmentalInfluence(conditions, strain);

            return Mathf.Clamp01(baseParameter + environmentalInfluence * 0.1f);
        }

        private float CalculateFoliageDensity(CultivationConditions conditions, PlantStrainSO strain)
        {
            float baseDensity = 1f;

            if (strain != null)
            {
                baseDensity *= strain.WidthModifier;
            }

            // Light affects foliage density
            float lightEffect = _lightResponseCurve.Evaluate(conditions.LightIntensity / 800f); // Normalize to typical max PPFD

            return baseDensity * lightEffect;
        }

        private float CalculateBranchDensity(CultivationConditions conditions, PlantStrainSO strain)
        {
            float baseDensity = 1f;

            if (strain?.BaseSpecies != null)
            {
                baseDensity = 4f / 5f; // Default MaxBranchingLevels = 4, normalized to typical max levels
            }

            // Nutrients affect branching
            float nutrientEffect = CalculateNutrientHealth(conditions);

            return baseDensity * nutrientEffect;
        }

        private float CalculateLeafSize(CultivationConditions conditions, PlantStrainSO strain)
        {
            float baseSize = 1f;

            if (strain?.BaseSpecies != null)
            {
                Vector2 leafRange = new Vector2(0.5f, 1.5f); // Default leaf size range
                baseSize = (leafRange.x + leafRange.y) * 0.5f / 0.2f; // Normalize to typical leaf size
            }

            // Environmental conditions affect leaf size
            float environmentalEffect = CalculateEnvironmentalInfluence(conditions, strain);

            return baseSize * environmentalEffect;
        }

        private float CalculateTrunkThickness(CultivationConditions conditions, PlantStrainSO strain)
        {
            float baseThickness = 1f;

            if (strain != null)
            {
                baseThickness *= strain.HeightModifier * strain.WidthModifier;
            }

            // Nutrient availability affects trunk development
            float nutrientEffect = CalculateNutrientHealth(conditions);

            return baseThickness * nutrientEffect;
        }

        private float CalculateColorVariation(CultivationConditions conditions, PlantStrainSO strain)
        {
            // Environmental stress can cause color changes
            float stress = CalculateEnvironmentalStress(conditions, strain);

            // More stress = more color variation (yellowing, purpling, etc.)
            return stress * _stressResponse.MaxColorVariation;
        }

        private float CalculateEnvironmentalInfluence(CultivationConditions conditions, PlantStrainSO strain)
        {
            if (strain?.BaseSpecies == null) return 1f;

            return EvaluateEnvironmentalSuitability(new EnvironmentalConditions
            {
                Temperature = conditions.Temperature,
                Humidity = conditions.Humidity,
                CO2Level = conditions.CO2Level,
                LightIntensity = conditions.LightIntensity
            }, strain);
        }

        private float EvaluateEnvironmentalSuitability(EnvironmentalConditions conditions, PlantStrainSO strain)
        {
            // Simple environmental suitability calculation based on strain type
            float suitability = 1.0f;

            if (strain.StrainType == ProjectChimera.Data.Genetics.StrainType.Indica)
            {
                suitability *= (conditions.Temperature >= 20f && conditions.Temperature <= 26f) ? 1.0f : 0.8f;
            }
            else if (strain.StrainType == ProjectChimera.Data.Genetics.StrainType.Sativa)
            {
                suitability *= (conditions.Temperature >= 24f && conditions.Temperature <= 30f) ? 1.0f : 0.8f;
            }

            return suitability;
        }

        private void ApplyStressResponses(ref SpeedTreeParameters parameters, float stress, CultivationConditions conditions)
        {
            if (stress > _stressResponse.StressThreshold)
            {
                float stressMultiplier = 1f - (stress * _stressResponse.StressImpactMultiplier);

                parameters.FoliageDensity *= stressMultiplier;
                parameters.LeafSize *= stressMultiplier;
                parameters.GrowthRate *= stressMultiplier;

                // Increase color variation under stress
                parameters.ColorVariation = Mathf.Max(parameters.ColorVariation, stress * _stressResponse.MaxColorVariation);
            }
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_parameterMappings.Count == 0)
            {
                SharedLogger.LogWarning($"[Chimera] SpeedTreeParameterMapperSO '{DisplayName}' has no parameter mappings configured.", this);
                isValid = false;
            }

            return isValid;
        }
    }

    /// <summary>
    /// Maps a cultivation parameter to a SpeedTree control parameter.
    /// </summary>
    [System.Serializable]
    public class ParameterMapping
    {
        [SerializeField] private CultivationParameter _sourceParameter;
        [SerializeField] private SpeedTreeParameter _targetParameter;
        [SerializeField] private AnimationCurve _mappingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Range(0f, 2f)] private float _influenceMultiplier = 1f;
        [SerializeField] private bool _invertMapping = false;

        public CultivationParameter SourceParameter => _sourceParameter;
        public SpeedTreeParameter TargetParameter => _targetParameter;
        public AnimationCurve MappingCurve => _mappingCurve;
        public float InfluenceMultiplier => _influenceMultiplier;
        public bool InvertMapping => _invertMapping;

        public float MapValue(float inputValue)
        {
            float mappedValue = _mappingCurve.Evaluate(inputValue) * _influenceMultiplier;
            return _invertMapping ? 1f - mappedValue : mappedValue;
        }
    }

    /// <summary>
    /// Configuration for how plants respond to environmental stress.
    /// </summary>
    [System.Serializable]
    public class StressResponseConfig
    {
        [SerializeField, Range(0f, 1f)] private float _stressThreshold = 0.3f;
        [SerializeField, Range(0f, 2f)] private float _stressImpactMultiplier = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _maxColorVariation = 0.3f;
        [SerializeField] private bool _enableStressWilting = true;
        [SerializeField] private bool _enableStressDiscoloration = true;

        public float StressThreshold => _stressThreshold;
        public float StressImpactMultiplier => _stressImpactMultiplier;
        public float MaxColorVariation => _maxColorVariation;
        public bool EnableStressWilting => _enableStressWilting;
        public bool EnableStressDiscoloration => _enableStressDiscoloration;
    }

    /// <summary>
    /// Configuration for how genetic traits influence visual parameters.
    /// </summary>
    [System.Serializable]
    public class GeneticTraitInfluence
    {
        [SerializeField, Range(0f, 0.5f)] private float _baseGeneticVariation = 0.1f;
        [SerializeField, Range(0f, 2f)] private float _heightGeneInfluence = 1f;
        [SerializeField, Range(0f, 2f)] private float _widthGeneInfluence = 1f;
        [SerializeField, Range(0f, 1f)] private float _colorGeneInfluence = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _morphologyGeneInfluence = 0.5f;

        public float BaseGeneticVariation => _baseGeneticVariation;
        public float HeightGeneInfluence => _heightGeneInfluence;
        public float WidthGeneInfluence => _widthGeneInfluence;
        public float ColorGeneInfluence => _colorGeneInfluence;
        public float MorphologyGeneInfluence => _morphologyGeneInfluence;
    }

    /// <summary>
    /// Current cultivation conditions that influence plant appearance.
    /// </summary>
    public struct CultivationConditions
    {
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float CO2Level;
        public float NitrogenLevel;
        public float PhosphorusLevel;
        public float PotassiumLevel;
        public float GrowthStageProgress; // 0-1 through current growth stage
        public float WaterLevel;
        public float pH;
    }

    /// <summary>
    /// Calculated parameters to apply to SpeedTree assets.
    /// </summary>
    public struct SpeedTreeParameters
    {
        public float OverallHealth;
        public float GrowthRate;
        public float SeasonalParameter;
        public float FoliageDensity;
        public float BranchDensity;
        public float LeafSize;
        public float TrunkThickness;
        public float ColorVariation;
        public Vector3 ScaleModifier;
        public Color TintColor;
    }

    public enum CultivationParameter
    {
        Temperature,
        Humidity,
        LightIntensity,
        CO2Level,
        NitrogenLevel,
        PhosphorusLevel,
        PotassiumLevel,
        WaterLevel,
        pH,
        GrowthStage,
        PlantAge,
        EnvironmentalStress
    }

    public enum SpeedTreeParameter
    {
        SeasonalValue,
        FoliageDensity,
        BranchDensity,
        LeafSize,
        TrunkThickness,
        ColorVariation,
        OverallScale,
        WindResponse,
        LODQuality
    }
}
