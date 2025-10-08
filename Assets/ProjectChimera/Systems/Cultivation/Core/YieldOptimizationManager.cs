using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Systems.Cultivation.Advanced;
using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Yield Optimization Manager - Focused yield calculation and optimization
    /// Handles yield predictions, quality assessment, and harvest optimization
    /// Single Responsibility: Yield optimization and quality management
    /// </summary>
    public class YieldOptimizationManager : MonoBehaviour
    {
        [Header("Yield Optimization Settings")]
        [SerializeField] private bool _enableYieldOptimization = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _optimizationUpdateInterval = 5f;

        [Header("Quality Factors")]
        [SerializeField] private float _healthQualityWeight = 0.3f;
        [SerializeField] private float _growthQualityWeight = 0.25f;
        [SerializeField] private float _environmentalQualityWeight = 0.2f;
        [SerializeField] private float _careQualityWeight = 0.15f;
        [SerializeField] private float _geneticsQualityWeight = 0.1f;

        [Header("Yield Prediction")]
        [SerializeField] private float _baseYieldPerPlant = 100f;
        [SerializeField] private float _qualityYieldMultiplier = 2f;
        [SerializeField] private float _maturityYieldBonus = 1.5f;
        [SerializeField] private float _earlyHarvestPenalty = 0.6f;

        // Optimization management
        private readonly Dictionary<string, YieldOptimizationData> _plantYieldData = new Dictionary<string, YieldOptimizationData>();
        private readonly List<string> _plantsToOptimize = new List<string>();
        private YieldOptimizationStats _stats = new YieldOptimizationStats();

        // Timing
        private float _lastOptimizationUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public YieldOptimizationStats GetStats() => _stats;

        // Events
        public System.Action<string, YieldPrediction> OnYieldPredictionUpdated;
        public System.Action<string, float> OnQualityScoreChanged;
        public System.Action<string> OnOptimalHarvestTimeReached;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastOptimizationUpdate = Time.time;
            _stats = new YieldOptimizationStats();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "📊 YieldOptimizationManager initialized", this);
        }

        /// <summary>
        /// Optimize yields for all registered plants
        /// </summary>
        public void OptimizeYields()
        {
            if (!IsEnabled || !_enableYieldOptimization) return;

            float currentTime = Time.time;
            if (currentTime - _lastOptimizationUpdate < _optimizationUpdateInterval) return;

            _lastOptimizationUpdate = currentTime;
            var startTime = Time.realtimeSinceStartup;

            _plantsToOptimize.Clear();
            foreach (var kvp in _plantYieldData)
            {
                if (kvp.Value.IsActive)
                {
                    _plantsToOptimize.Add(kvp.Key);
                }
            }

            foreach (var plantId in _plantsToOptimize)
            {
                try
                {
                    OptimizePlantYield(plantId);
                    _stats.OptimizationsProcessed++;
                }
                catch (System.Exception ex)
                {
                    _stats.OptimizationErrors++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("CULTIVATION", $"Yield optimization error for plant {plantId}: {ex.Message}", this);
                }
            }

            // Update statistics
            var endTime = Time.realtimeSinceStartup;
            var optimizationTime = endTime - startTime;
            _stats.LastOptimizationTime = optimizationTime;
            _stats.AverageOptimizationTime = (_stats.AverageOptimizationTime + optimizationTime) / 2f;
            if (optimizationTime > _stats.MaxOptimizationTime) _stats.MaxOptimizationTime = optimizationTime;
        }

        /// <summary>
        /// Register plant for yield optimization
        /// </summary>
        public bool RegisterPlantForOptimization(AdvancedPlantInstance plant)
        {
            if (plant == null || string.IsNullOrEmpty(plant.PlantId))
                return false;

            if (_plantYieldData.ContainsKey(plant.PlantId))
                return false;

            var yieldData = new YieldOptimizationData
            {
                PlantId = plant.PlantId,
                IsActive = true,
                CurrentQualityScore = CalculateInitialQualityScore(plant),
                PredictedYield = _baseYieldPerPlant,
                OptimalHarvestTime = CalculateOptimalHarvestTime(plant),
                LastOptimizationUpdate = Time.time
            };

            _plantYieldData[plant.PlantId] = yieldData;
            _stats.ActiveOptimizations = _plantYieldData.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Registered plant for yield optimization: {plant.PlantId}", this);

            return true;
        }

        /// <summary>
        /// Unregister plant from yield optimization
        /// </summary>
        public bool UnregisterPlantFromOptimization(string plantId)
        {
            if (!_plantYieldData.ContainsKey(plantId))
                return false;

            _plantYieldData.Remove(plantId);
            _stats.ActiveOptimizations = _plantYieldData.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Unregistered plant from yield optimization: {plantId}", this);

            return true;
        }

        /// <summary>
        /// Calculate yield prediction for plant
        /// </summary>
        public YieldPrediction CalculateYieldPrediction(AdvancedPlantInstance plant)
        {
            if (plant == null) return new YieldPrediction();

            var prediction = new YieldPrediction
            {
                PlantId = plant.PlantId,
                BaseYield = _baseYieldPerPlant
            };

            // Calculate quality factors
            var qualityFactors = CalculateQualityFactors(plant);
            prediction.QualityScore = qualityFactors.OverallScore;

            // Calculate yield modifiers
            prediction.HealthModifier = CalculateHealthModifier(plant.HealthPercentage);
            prediction.GrowthModifier = CalculateGrowthModifier(plant.GrowthStage, plant.GrowthProgress);
            prediction.EnvironmentalModifier = CalculateEnvironmentalModifier(plant);
            prediction.MaturityModifier = CalculateMaturityModifier(plant);

            // Calculate final predicted yield
            prediction.PredictedYield = prediction.BaseYield *
                                      prediction.HealthModifier *
                                      prediction.GrowthModifier *
                                      prediction.EnvironmentalModifier *
                                      prediction.MaturityModifier *
                                      (1f + prediction.QualityScore * _qualityYieldMultiplier);

            // Calculate quality grade
            prediction.QualityGrade = DetermineQualityGrade(prediction.QualityScore);

            return prediction;
        }

        /// <summary>
        /// Get optimal harvest time for plant
        /// </summary>
        public float GetOptimalHarvestTime(string plantId)
        {
            if (_plantYieldData.TryGetValue(plantId, out var yieldData))
            {
                return yieldData.OptimalHarvestTime;
            }
            return 0f;
        }

        /// <summary>
        /// Get current quality score for plant
        /// </summary>
        public float GetPlantQualityScore(string plantId)
        {
            if (_plantYieldData.TryGetValue(plantId, out var yieldData))
            {
                return yieldData.CurrentQualityScore;
            }
            return 0f;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"YieldOptimizationManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Optimize yield for individual plant
        /// </summary>
        private void OptimizePlantYield(string plantId)
        {
            if (!_plantYieldData.TryGetValue(plantId, out var yieldData))
                return;

            // Get plant instance
            var plant = GetPlantInstance(plantId);
            if (plant == null) return;

            // Update yield prediction
            var previousQualityScore = yieldData.CurrentQualityScore;
            var prediction = CalculateYieldPrediction(plant);

            yieldData.CurrentQualityScore = prediction.QualityScore;
            yieldData.PredictedYield = prediction.PredictedYield;
            yieldData.LastOptimizationUpdate = Time.time;

            // Check for optimal harvest time
            CheckOptimalHarvestTime(plant, yieldData);

            // Update yield data
            _plantYieldData[plantId] = yieldData;

            // Fire events
            OnYieldPredictionUpdated?.Invoke(plantId, prediction);

            if (Mathf.Abs(previousQualityScore - yieldData.CurrentQualityScore) > 0.05f)
            {
                OnQualityScoreChanged?.Invoke(plantId, yieldData.CurrentQualityScore);
            }
        }

        /// <summary>
        /// Calculate quality factors for plant
        /// </summary>
        private QualityFactors CalculateQualityFactors(AdvancedPlantInstance plant)
        {
            var factors = new QualityFactors();

            // Health factor
            factors.HealthFactor = plant.HealthPercentage / 100f;

            // Growth factor
            factors.GrowthFactor = CalculateGrowthQualityFactor(plant);

            // Environmental factor
            factors.EnvironmentalFactor = CalculateEnvironmentalQualityFactor(plant);

            // Care factor
            factors.CareFactor = CalculateCareQualityFactor(plant);

            // Genetics factor
            factors.GeneticsFactor = CalculateGeneticsQualityFactor(plant);

            // Calculate overall score
            factors.OverallScore = (factors.HealthFactor * _healthQualityWeight +
                                  factors.GrowthFactor * _growthQualityWeight +
                                  factors.EnvironmentalFactor * _environmentalQualityWeight +
                                  factors.CareFactor * _careQualityWeight +
                                  factors.GeneticsFactor * _geneticsQualityWeight);

            return factors;
        }

        /// <summary>
        /// Calculate initial quality score for plant
        /// </summary>
        private float CalculateInitialQualityScore(AdvancedPlantInstance plant)
        {
            return CalculateQualityFactors(plant).OverallScore;
        }

        /// <summary>
        /// Calculate optimal harvest time
        /// </summary>
        private float CalculateOptimalHarvestTime(AdvancedPlantInstance plant)
        {
            // Estimate based on growth stage and progress
            float estimatedTime = 0f;

            switch (plant.GrowthStage)
            {
                case GrowthStage.Seedling:
                    estimatedTime = 60f; // 60 days
                    break;
                case GrowthStage.Vegetative:
                    estimatedTime = 30f; // 30 days
                    break;
                case GrowthStage.Flowering:
                    estimatedTime = 14f; // 14 days
                    break;
                case GrowthStage.Harvest:
                    estimatedTime = 7f * (1f - plant.GrowthProgress); // Up to 7 days
                    break;
            }

            return Time.time + estimatedTime;
        }

        /// <summary>
        /// Check if optimal harvest time is reached
        /// </summary>
        private void CheckOptimalHarvestTime(AdvancedPlantInstance plant, YieldOptimizationData yieldData)
        {
            if (plant.GrowthStage == GrowthStage.Harvest &&
                plant.GrowthProgress >= 0.9f &&
                Time.time >= yieldData.OptimalHarvestTime)
            {
                OnOptimalHarvestTimeReached?.Invoke(plant.PlantId);
            }
        }

        /// <summary>
        /// Get plant instance
        /// </summary>
        private AdvancedPlantInstance GetPlantInstance(string plantId)
        {
            // This would come from the plant lifecycle manager
            return null; // Placeholder
        }

        /// <summary>
        /// Calculate health modifier
        /// </summary>
        private float CalculateHealthModifier(float healthPercentage)
        {
            return Mathf.Clamp01(healthPercentage / 100f);
        }

        /// <summary>
        /// Calculate growth modifier
        /// </summary>
        private float CalculateGrowthModifier(GrowthStage stage, float progress)
        {
            switch (stage)
            {
                case GrowthStage.Seedling: return 0.3f + (progress * 0.2f);
                case GrowthStage.Vegetative: return 0.5f + (progress * 0.3f);
                case GrowthStage.Flowering: return 0.8f + (progress * 0.2f);
                case GrowthStage.Harvest: return progress < 0.3f ? _earlyHarvestPenalty :
                                                 progress > 0.9f ? _maturityYieldBonus : 1f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Calculate environmental modifier
        /// </summary>
        private float CalculateEnvironmentalModifier(AdvancedPlantInstance plant)
        {
            // This would interface with environmental response manager
            return 1f; // Placeholder
        }

        /// <summary>
        /// Calculate maturity modifier
        /// </summary>
        private float CalculateMaturityModifier(AdvancedPlantInstance plant)
        {
            if (plant.GrowthStage == GrowthStage.Harvest && plant.GrowthProgress >= 0.9f)
            {
                return _maturityYieldBonus;
            }
            return 1f;
        }

        /// <summary>
        /// Calculate growth quality factor
        /// </summary>
        private float CalculateGrowthQualityFactor(AdvancedPlantInstance plant)
        {
            return Mathf.Clamp01(plant.GrowthProgress);
        }

        /// <summary>
        /// Calculate environmental quality factor
        /// </summary>
        private float CalculateEnvironmentalQualityFactor(AdvancedPlantInstance plant)
        {
            // This would interface with environmental systems
            return 0.8f; // Placeholder
        }

        /// <summary>
        /// Calculate care quality factor
        /// </summary>
        private float CalculateCareQualityFactor(AdvancedPlantInstance plant)
        {
            // This would track care activities
            return 0.9f; // Placeholder
        }

        /// <summary>
        /// Calculate genetics quality factor
        /// </summary>
        private float CalculateGeneticsQualityFactor(AdvancedPlantInstance plant)
        {
            // This would interface with genetics systems
            return 0.85f; // Placeholder
        }

        /// <summary>
        /// Determine quality grade based on score
        /// </summary>
        private QualityGrade DetermineQualityGrade(float qualityScore)
        {
            if (qualityScore >= 0.9f) return QualityGrade.Premium;
            if (qualityScore >= 0.75f) return QualityGrade.High;
            if (qualityScore >= 0.6f) return QualityGrade.Medium;
            if (qualityScore >= 0.4f) return QualityGrade.Low;
            return QualityGrade.Poor;
        }

        #endregion
    }
}
