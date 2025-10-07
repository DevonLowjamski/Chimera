using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// Phase 2.2.1: Trait Expression Engine Interface
    /// Evaluates Genotype × Environment (GxE) interactions for trait expression
    /// Based on research-calibrated response curves and fractal genetic variation
    /// </summary>
    public interface ITraitExpressionEngine
    {
        /// <summary>
        /// Evaluates trait expression based on genotype and environmental conditions
        /// </summary>
        /// <param name="genotype">Plant genotype data</param>
        /// <param name="environment">Current environmental snapshot</param>
        /// <returns>Calculated trait expression results</returns>
        TraitExpressionResult Evaluate(PlantGenotype genotype, EnvironmentSnapshot environment);
        
        /// <summary>
        /// Evaluates trait expression using ScriptableObject genotype data
        /// </summary>
        /// <param name="genotypeData">ScriptableObject genotype data</param>
        /// <param name="environment">Current environmental snapshot</param>
        /// <returns>Calculated trait expression results</returns>
        TraitExpressionResult Evaluate(GenotypeDataSO genotypeData, EnvironmentSnapshot environment);
        
        /// <summary>
        /// Evaluates a specific trait expression
        /// </summary>
        /// <param name="genotype">Plant genotype data</param>
        /// <param name="environment">Environmental conditions</param>
        /// <param name="trait">Specific trait to evaluate</param>
        /// <returns>Expression value for the trait</returns>
        float EvaluateSpecificTrait(PlantGenotype genotype, EnvironmentSnapshot environment, TraitType trait);
        
        /// <summary>
        /// Gets response curve for a specific trait and environmental factor
        /// </summary>
        /// <param name="trait">Target trait</param>
        /// <param name="environmentalFactor">Environmental factor type</param>
        /// <returns>Response curve configuration</returns>
        EnvironmentalResponseCurve GetResponseCurve(TraitType trait, EnvironmentalFactorType environmentalFactor);
        
        /// <summary>
        /// Calculates environmental stress modifier
        /// </summary>
        /// <param name="environment">Environmental conditions</param>
        /// <param name="optimalRanges">Optimal ranges for the genotype</param>
        /// <returns>Stress modifier (0-1, where 1 = no stress)</returns>
        float CalculateEnvironmentalStress(EnvironmentSnapshot environment, OptimalEnvironmentalRanges optimalRanges);
        
        /// <summary>
        /// Applies GxE profile modifications to trait expression
        /// </summary>
        /// <param name="baseExpression">Base genetic expression</param>
        /// <param name="environment">Environmental conditions</param>
        /// <param name="gxeProfile">GxE interaction profile</param>
        /// <returns>Modified trait expression</returns>
        float ApplyGxEModification(float baseExpression, EnvironmentSnapshot environment, GxE_ProfileSO gxeProfile);
        
        /// <summary>
        /// Initialize the expression engine with configuration
        /// </summary>
        /// <param name="config">Configuration for trait expression calculations</param>
        void Initialize(TraitExpressionConfig config);
        
        /// <summary>
        /// Whether the engine is initialized and ready
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Enable or disable GPU compute shader acceleration
        /// </summary>
        bool UseComputeShader { get; set; }
    }

    /// <summary>
    /// Environmental snapshot for trait expression calculations
    /// Simplified version of EnvironmentalConditions for performance
    /// </summary>
    [System.Serializable]
    public struct EnvironmentSnapshot
    {
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float CO2Level;
        public float NutrientAvailability;
        public float WaterAvailability;
        public float pH;
        public float VPD; // Vapor Pressure Deficit
        public float DLI; // Daily Light Integral
        public float StressLevel;
        
        public static EnvironmentSnapshot FromEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            return new EnvironmentSnapshot
            {
                Temperature = conditions.Temperature,
                Humidity = conditions.Humidity,
                LightIntensity = conditions.LightIntensity,
                CO2Level = conditions.CO2Level,
                NutrientAvailability = 1.0f, // Default - would need nutrient data
                WaterAvailability = 1.0f, // Default - would need water data
                pH = 6.5f, // Default optimal pH
                VPD = CalculateVPD(conditions.Temperature, conditions.Humidity),
                DLI = conditions.LightIntensity * 12f / 1000000f, // Simplified DLI calculation
                StressLevel = 0f // Default - calculated by expression engine
            };
        }
        
        private static float CalculateVPD(float temperature, float humidity)
        {
            // Simplified VPD calculation
            var svp = 0.6108f * Mathf.Exp(17.27f * temperature / (temperature + 237.3f));
            var avp = svp * (humidity / 100f);
            return svp - avp;
        }
    }

    /// <summary>
    /// Configuration for trait expression engine
    /// </summary>
    [System.Serializable]
    public class TraitExpressionConfig
    {
        [Header("Performance Settings")]
        public bool EnableComputeShader = true;
        public bool EnableCaching = true;
        public int CacheSize = 1000;
        
        [Header("Expression Calculation")]
        public bool UseRealWorldData = true;
        public bool EnableStressResponse = true;
        public bool EnableAdaptation = true;
        public float BaseExpressionMultiplier = 1.0f;
        
        [Header("Environmental Factors")]
        public bool EnableTemperatureEffects = true;
        public bool EnableHumidityEffects = true;
        public bool EnableLightEffects = true;
        public bool EnableNutrientEffects = true;
        public bool EnableCO2Effects = true;
        
        [Header("Genetic Factors")]
        public bool EnableHeritabilityModifiers = true;
        public bool EnableEpigeneticEffects = false;
        public bool EnableMutationEffects = true;
        public float MutationExpressionModifier = 0.05f;
        
        [Header("GxE Interaction")]
        public bool EnableGxEInteractions = true;
        public float GxEInteractionStrength = 1.0f;
        public bool EnableNonLinearInteractions = true;
        public bool EnableThresholdEffects = true;
    }

    /// <summary>
    /// Optimal environmental ranges for a genotype
    /// </summary>
    [System.Serializable]
    public struct OptimalEnvironmentalRanges
    {
        public Vector2 TemperatureRange; // Min/Max optimal temperature
        public Vector2 HumidityRange; // Min/Max optimal humidity
        public Vector2 LightIntensityRange; // Min/Max optimal light
        public Vector2 CO2Range; // Min/Max optimal CO2
        public Vector2 pHRange; // Min/Max optimal pH
        public Vector2 VPDRange; // Min/Max optimal VPD
        
        public static OptimalEnvironmentalRanges GetDefaultCannabis()
        {
            return new OptimalEnvironmentalRanges
            {
                TemperatureRange = new Vector2(20f, 28f), // 20-28°C optimal
                HumidityRange = new Vector2(40f, 60f), // 40-60% RH optimal
                LightIntensityRange = new Vector2(400f, 800f), // μmol/m²/s
                CO2Range = new Vector2(400f, 1200f), // ppm
                pHRange = new Vector2(6.0f, 7.0f), // pH
                VPDRange = new Vector2(0.8f, 1.2f) // kPa
            };
        }
    }

    /// <summary>
    /// Environmental factor types for response curves
    /// </summary>
    public enum EnvironmentalFactorType
    {
        Temperature,
        Humidity,
        LightIntensity,
        CO2Level,
        NutrientLevel,
        WaterLevel,
        pH,
        VPD,
        DLI,
        WindSpeed,
        AirFlow,
        StressLevel
    }
}