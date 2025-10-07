using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// BASIC: Simple trait expression engine for Project Chimera's genetics system.
    /// Focuses on essential trait calculations without complex GxE interactions and compute shaders.
    /// </summary>
    public class TraitExpressionEngine : MonoBehaviour, ITraitExpressionEngine
    {
        [Header("Basic Expression Settings")]
        [SerializeField] private bool _enableBasicExpression = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _baseThcMultiplier = 1.0f;
        [SerializeField] private float _baseYieldMultiplier = 1.0f;

        // Basic trait tracking
        private readonly Dictionary<string, TraitResult> _expressionCache = new Dictionary<string, TraitResult>();
        private bool _isInitialized = false;
        private TraitExpressionConfig _config;

        // ITraitExpressionEngine Properties
        public bool IsInitialized => _isInitialized;
        public bool UseComputeShader { get; set; } = false;

        /// <summary>
        /// Events for trait expression
        /// </summary>
        public event System.Action<string, TraitResult> OnTraitEvaluated;

        /// <summary>
        /// Initialize basic trait expression engine
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Evaluate basic traits for a strain
        /// </summary>
        public TraitResult EvaluateTraits(string strainId, float baseThc, float baseYield, float environmentFactor = 1.0f)
        {
            if (!_enableBasicExpression || !_isInitialized) return new TraitResult();

            // Check cache first
            string cacheKey = $"{strainId}_{baseThc:F2}_{baseYield:F2}_{environmentFactor:F2}";
            if (_expressionCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // Simple trait calculation
            float finalThc = baseThc * _baseThcMultiplier * environmentFactor;
            float finalYield = baseYield * _baseYieldMultiplier * environmentFactor;

            // Add some genetic variation (±10%)
            float thcVariation = Random.Range(0.9f, 1.1f);
            float yieldVariation = Random.Range(0.9f, 1.1f);

            finalThc *= thcVariation;
            finalYield *= yieldVariation;

            var result = new TraitResult
            {
                StrainId = strainId,
                FinalThc = Mathf.Clamp(finalThc, 0f, 35f), // Cap at 35%
                FinalYield = Mathf.Max(finalYield, 0f),
                EnvironmentFactor = environmentFactor,
                CalculatedTime = System.DateTime.Now
            };

            // Cache result
            _expressionCache[cacheKey] = result;
            OnTraitEvaluated?.Invoke(strainId, result);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }

            return result;
        }

        /// <summary>
        /// Evaluate traits for a genetic data object
        /// </summary>
        public TraitResult EvaluateTraits(GeneticData geneticData, float environmentFactor = 1.0f)
        {
            if (geneticData == null) return new TraitResult();

            return EvaluateTraits(geneticData.StrainId, geneticData.ThcContent, geneticData.YieldPotential, environmentFactor);
        }

        /// <summary>
        /// Get cached trait result
        /// </summary>
        public TraitResult GetCachedResult(string cacheKey)
        {
            return _expressionCache.TryGetValue(cacheKey, out var result) ? result : null;
        }

        /// <summary>
        /// Clear expression cache
        /// </summary>
        public void ClearCache()
        {
            _expressionCache.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                CachedResults = _expressionCache.Count,
                IsCacheEnabled = true,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Set expression enabled state
        /// </summary>
        public void SetExpressionEnabled(bool enabled)
        {
            _enableBasicExpression = enabled;

            if (!enabled)
            {
                ClearCache();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Update multipliers
        /// </summary>
        public void UpdateMultipliers(float thcMultiplier, float yieldMultiplier)
        {
            _baseThcMultiplier = Mathf.Max(0.1f, thcMultiplier);
            _baseYieldMultiplier = Mathf.Max(0.1f, yieldMultiplier);

            // Clear cache when multipliers change
            ClearCache();

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        #region ITraitExpressionEngine Implementation

        /// <summary>
        /// Initialize with configuration (ITraitExpressionEngine interface)
        /// </summary>
        public void Initialize(TraitExpressionConfig config)
        {
            _config = config;
            Initialize(); // Call existing initialization
        }

        /// <summary>
        /// Evaluate trait expression based on genotype and environment
        /// </summary>
        public TraitExpressionResult Evaluate(PlantGenotype genotype, EnvironmentSnapshot environment)
        {
            if (genotype == null) return new TraitExpressionResult();

            // Convert to simplified format for basic evaluation
            var geneticData = new GeneticData
            {
                StrainId = genotype.GenotypeID,
                ThcContent = 15f, // Default - would extract from genotype in full implementation
                YieldPotential = 500f // Default - would extract from genotype in full implementation
            };

            var basicResult = EvaluateTraits(geneticData, environment.StressLevel);

            var result = new TraitExpressionResult
            {
                GenotypeID = genotype.GenotypeID,
                EnvironmentalStress = environment.StressLevel,
                CalculationTime = System.DateTime.Now
            };

            result.SetTraitValue(TraitType.THCContent, basicResult.FinalThc);
            result.SetTraitValue(TraitType.Yield, basicResult.FinalYield / 100f); // Normalize to 0-1 range

            return result;
        }

        /// <summary>
        /// Evaluate trait expression using ScriptableObject genotype data
        /// </summary>
        public TraitExpressionResult Evaluate(GenotypeDataSO genotypeData, EnvironmentSnapshot environment)
        {
            if (genotypeData == null) return new TraitExpressionResult();

            // Create PlantGenotype from ScriptableObject data
            var genotype = new PlantGenotype
            {
                GenotypeID = genotypeData.GenotypeID,
                StrainName = "Unknown Strain",
                Genotype = new Dictionary<string, object>()
            };

            return Evaluate(genotype, environment);
        }

        /// <summary>
        /// Evaluate a specific trait
        /// </summary>
        public float EvaluateSpecificTrait(PlantGenotype genotype, EnvironmentSnapshot environment, TraitType trait)
        {
            var result = Evaluate(genotype, environment);
            return result.GetTraitValue(trait);
        }

        /// <summary>
        /// Get response curve (simplified implementation)
        /// </summary>
        public EnvironmentalResponseCurve GetResponseCurve(TraitType trait, EnvironmentalFactorType environmentalFactor)
        {
            // Return default response curve
            return new EnvironmentalResponseCurve
            {
                Trait = trait,
                EnvironmentalFactor = (EnvironmentalFactor)environmentalFactor,
                ResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                EffectStrength = 1f
            };
        }

        /// <summary>
        /// Calculate environmental stress modifier
        /// </summary>
        public float CalculateEnvironmentalStress(EnvironmentSnapshot environment, OptimalEnvironmentalRanges optimalRanges)
        {
            float stressModifier = 1.0f;

            // Simple stress calculation based on temperature
            if (environment.Temperature < optimalRanges.TemperatureRange.x ||
                environment.Temperature > optimalRanges.TemperatureRange.y)
            {
                stressModifier *= 0.8f;
            }

            // Simple stress calculation based on humidity
            if (environment.Humidity < optimalRanges.HumidityRange.x ||
                environment.Humidity > optimalRanges.HumidityRange.y)
            {
                stressModifier *= 0.9f;
            }

            return Mathf.Clamp01(stressModifier);
        }

        /// <summary>
        /// Apply GxE profile modifications
        /// </summary>
        public float ApplyGxEModification(float baseExpression, EnvironmentSnapshot environment, GxE_ProfileSO gxeProfile)
        {
            if (gxeProfile == null) return baseExpression;

            // Simple environmental modifier based on stress level
            float environmentModifier = 1.0f - (environment.StressLevel * 0.3f);
            return baseExpression * environmentModifier;
        }

        #endregion
    }

    /// <summary>
    /// Basic trait result
    /// </summary>
    [System.Serializable]
    public class TraitResult
    {
        public string StrainId;
        public float FinalThc;
        public float FinalYield;
        public float EnvironmentFactor;
        public System.DateTime CalculatedTime;
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    [System.Serializable]
    public struct CacheStats
    {
        public int CachedResults;
        public bool IsCacheEnabled;
        public bool IsInitialized;
    }
}
