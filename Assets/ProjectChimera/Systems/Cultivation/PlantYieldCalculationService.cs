using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant yield calculation service aligned with Project Chimera's cultivation vision.
    /// Focuses on essential harvest yield calculations for basic plant care mechanics.
    /// </summary>
    public class PlantYieldCalculationService : MonoBehaviour
    {
        [Header("Basic Yield Settings")]
        [SerializeField] private bool _enableYieldCalculation = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _baseYieldGrams = 50f;
        [SerializeField] private float _yieldVariability = 0.2f; // ±20%

        // Basic yield tracking
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize the basic yield calculation service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Calculate yield for a plant
        /// </summary>
        public YieldResult CalculateYield(PlantInstance plant)
        {
            if (!_enableYieldCalculation || plant == null) return null;

            var result = new YieldResult
            {
                PlantID = plant.PlantID,
                BaseYield = _baseYieldGrams,
                QualityMultiplier = 1f
            };

            // Basic quality calculation based on health
            result.QualityMultiplier = Mathf.Lerp(0.5f, 1.5f, plant.Health);

            // Apply growth stage modifier
            float stageModifier = GetStageYieldModifier(plant.CurrentGrowthStage);
            result.StageModifier = stageModifier;

            // Calculate final yield with variability
            float baseModifiedYield = _baseYieldGrams * result.QualityMultiplier * stageModifier;
            float variability = Random.Range(-_yieldVariability, _yieldVariability);
            result.FinalYield = Mathf.Max(0f, baseModifiedYield * (1f + variability));

            // Determine quality rating
            result.QualityRating = GetQualityRating(result.QualityMultiplier);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }

            return result;
        }

        /// <summary>
        /// Calculate total yield for multiple plants
        /// </summary>
        public float CalculateTotalYield(List<PlantInstance> plants)
        {
            if (plants == null || plants.Count == 0) return 0f;

            float totalYield = 0f;
            foreach (var plant in plants)
            {
                var yieldResult = CalculateYield(plant);
                if (yieldResult != null)
                {
                    totalYield += yieldResult.FinalYield;
                }
            }

            return totalYield;
        }

        /// <summary>
        /// Get yield modifier for growth stage
        /// </summary>
        private float GetStageYieldModifier(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Flowering:
                    return 1.0f; // Full yield in flowering
                case PlantGrowthStage.Vegetative:
                    return 0.3f; // Reduced yield if harvested early
                case PlantGrowthStage.Seedling:
                    return 0.1f; // Very low yield if harvested as seedling
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Get quality rating based on multiplier
        /// </summary>
        private string GetQualityRating(float qualityMultiplier)
        {
            if (qualityMultiplier >= 1.3f) return "Premium";
            if (qualityMultiplier >= 1.1f) return "High";
            if (qualityMultiplier >= 0.9f) return "Standard";
            if (qualityMultiplier >= 0.7f) return "Low";
            return "Poor";
        }

        /// <summary>
        /// Estimate yield for a plant without calculating
        /// </summary>
        public float EstimateYield(PlantInstance plant)
        {
            if (plant == null) return 0f;

            float qualityMultiplier = Mathf.Lerp(0.5f, 1.5f, plant.Health);
            float stageModifier = GetStageYieldModifier(plant.CurrentGrowthStage);
            return _baseYieldGrams * qualityMultiplier * stageModifier;
        }

        /// <summary>
        /// Check if plant is ready for harvest
        /// </summary>
        public bool IsReadyForHarvest(PlantInstance plant)
        {
            if (plant == null) return false;

            // Basic readiness check - flowering stage and adequate health
            return plant.CurrentGrowthStage == PlantGrowthStage.Flowering && plant.Health >= 0.6f;
        }

        /// <summary>
        /// Get yield statistics
        /// </summary>
        public YieldStatistics GetYieldStatistics()
        {
            return new YieldStatistics
            {
                BaseYield = _baseYieldGrams,
                YieldVariability = _yieldVariability,
                IsInitialized = _isInitialized,
                EnableYieldCalculation = _enableYieldCalculation
            };
        }

        /// <summary>
        /// Set base yield
        /// </summary>
        public void SetBaseYield(float baseYield)
        {
            _baseYieldGrams = Mathf.Max(0f, baseYield);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Set yield variability
        /// </summary>
        public void SetYieldVariability(float variability)
        {
            _yieldVariability = Mathf.Clamp01(variability);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }
    }

    /// <summary>
    /// Basic yield result
    /// </summary>
    [System.Serializable]
    public class YieldResult
    {
        public string PlantID;
        public float BaseYield;
        public float QualityMultiplier;
        public float StageModifier;
        public float FinalYield;
        public string QualityRating;
    }

    /// <summary>
    /// Yield statistics
    /// </summary>
    [System.Serializable]
    public class YieldStatistics
    {
        public float BaseYield;
        public float YieldVariability;
        public bool IsInitialized;
        public bool EnableYieldCalculation;
    }
}
