using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Harvest Execution Handler
    /// Single Responsibility: Handle harvest execution and post-harvest processing
    /// Extracted from PlantHarvestOperator (785 lines → 4 files <500 lines each)
    /// </summary>
    public class HarvestExecutionHandler
    {
        private readonly HarvestReadinessCalculator _calculator;
        private readonly bool _enableLogging;
        private readonly bool _enableQualityAssessment;
        private readonly bool _enablePostHarvestTracking;

        // Post-harvest parameters
        private PostHarvestMethod _preferredMethod = PostHarvestMethod.StandardDrying;
        private float _optimalDryingTemperature = 20f;
        private float _optimalDryingHumidity = 55f;
        private float _curingDuration = 14f; // Days

        // Harvest history
        private readonly List<HarvestAttempt> _harvestHistory = new List<HarvestAttempt>();
        private readonly int _maxHarvestHistoryEntries = 10;

        private bool _isHarvested = false;

        public bool IsHarvested => _isHarvested;
        public List<HarvestAttempt> HarvestHistory => new List<HarvestAttempt>(_harvestHistory);

        public HarvestExecutionHandler(
            HarvestReadinessCalculator calculator,
            bool enableLogging,
            bool enableQuality,
            bool enableTracking)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _enableLogging = enableLogging;
            _enableQualityAssessment = enableQuality;
            _enablePostHarvestTracking = enableTracking;
        }

        /// <summary>
        /// Set post-harvest parameters
        /// </summary>
        public void SetPostHarvestParameters(
            PostHarvestMethod method,
            float dryingTemp,
            float dryingHumidity,
            float curingDuration)
        {
            _preferredMethod = method;
            _optimalDryingTemperature = Mathf.Clamp(dryingTemp, 15f, 25f);
            _optimalDryingHumidity = Mathf.Clamp(dryingHumidity, 45f, 65f);
            _curingDuration = Mathf.Max(7f, curingDuration);
        }

        /// <summary>
        /// Execute harvest operation
        /// </summary>
        public HarvestExecutionResult ExecuteHarvest(
            string harvestMethod,
            float readinessScore,
            float estimatedYield,
            float estimatedPotency,
            bool isInOptimalWindow)
        {
            if (_isHarvested)
            {
                return HarvestExecutionResult.CreateFailure("Plant has already been harvested");
            }

            var startTime = DateTime.Now;

            // Calculate actual yield and potency
            var actualYield = _calculator.CalculateActualHarvestYield(estimatedYield, readinessScore);
            var actualPotency = _calculator.CalculateActualHarvestPotency(estimatedPotency, readinessScore);

            // Determine harvest quality
            var quality = _enableQualityAssessment
                ? _calculator.DetermineHarvestQuality(readinessScore, actualYield, actualPotency)
                : HarvestQualityGrade.Good;

            var result = HarvestExecutionResult.CreateSuccess(
                actualYield,
                actualPotency,
                quality,
                _preferredMethod
            );

            result.ProcessingTime = (float)(DateTime.Now - startTime).TotalMilliseconds;

            // Record harvest attempt
            if (_enablePostHarvestTracking)
            {
                var attempt = new HarvestAttempt
                {
                    Timestamp = DateTime.Now,
                    Method = harvestMethod,
                    ReadinessScore = readinessScore,
                    Yield = actualYield,
                    Potency = actualPotency,
                    Quality = quality,
                    Success = true
                };

                RecordHarvestAttempt(attempt);
            }

            _isHarvested = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT",
                    $"Harvest completed: {actualYield:F2}g yield, {actualPotency:F1}% potency, {quality} quality",
                    null);
            }

            return result;
        }

        /// <summary>
        /// Get post-harvest processing recommendations
        /// </summary>
        public PostHarvestProcessDetails GetPostHarvestProcess(HarvestExecutionResult harvestResult)
        {
            var processType = DetermineOptimalProcess(harvestResult);
            var duration = CalculateProcessingDuration(harvestResult);
            var recommendations = GenerateProcessingRecommendations(harvestResult);

            var process = new PostHarvestProcessDetails
            {
                ProcessType = processType,
                Duration = duration,
                Temperature = _optimalDryingTemperature,
                Humidity = _optimalDryingHumidity,
                Recommendations = recommendations
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT",
                    $"Post-harvest process recommended: {processType}, {duration:F1} days",
                    null);
            }

            return process;
        }

        /// <summary>
        /// Generate harvest recommendation reason
        /// </summary>
        public string GetRecommendationReason(float readinessScore, float daysUntilOptimal, bool isInWindow)
        {
            if (readinessScore >= 0.95f && isInWindow)
            {
                return "Perfect harvest timing - peak quality expected";
            }
            else if (readinessScore >= 0.85f && isInWindow)
            {
                return "Excellent harvest timing - high quality expected";
            }
            else if (isInWindow)
            {
                return "Within optimal window - good quality expected";
            }
            else if (daysUntilOptimal > 0)
            {
                return $"Wait {daysUntilOptimal:F0} more days for optimal results";
            }
            else if (daysUntilOptimal < -3)
            {
                return "Harvest overdue - quality may be degrading";
            }
            else
            {
                return "Harvest soon - approaching optimal window";
            }
        }

        /// <summary>
        /// Reset harvest state (for testing or replanting)
        /// </summary>
        public void ResetHarvestState()
        {
            _isHarvested = false;
            _harvestHistory.Clear();
        }

        #region Private Methods

        /// <summary>
        /// Record harvest attempt
        /// </summary>
        private void RecordHarvestAttempt(HarvestAttempt attempt)
        {
            _harvestHistory.Add(attempt);

            // Maintain history size limit
            if (_harvestHistory.Count > _maxHarvestHistoryEntries)
            {
                _harvestHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Determine optimal post-harvest processing method
        /// </summary>
        private string DetermineOptimalProcess(HarvestExecutionResult harvestResult)
        {
            // Base on quality and preferred method
            switch (_preferredMethod)
            {
                case PostHarvestMethod.QuickDry:
                    return harvestResult.Quality >= HarvestQualityGrade.Excellent
                        ? "QuickDry-Premium"
                        : "QuickDry-Standard";

                case PostHarvestMethod.SlowDry:
                    return harvestResult.Quality >= HarvestQualityGrade.Premium
                        ? "SlowDry-MaxQuality"
                        : "SlowDry-Standard";

                case PostHarvestMethod.FreezeProcessing:
                    return "FreezeProcessing-ImmediateExtraction";

                default:
                    return "StandardDrying";
            }
        }

        /// <summary>
        /// Calculate processing duration
        /// </summary>
        private float CalculateProcessingDuration(HarvestExecutionResult harvestResult)
        {
            var baseDuration = _curingDuration;

            // Adjust based on method
            switch (_preferredMethod)
            {
                case PostHarvestMethod.QuickDry:
                    baseDuration *= 0.5f;
                    break;
                case PostHarvestMethod.SlowDry:
                    baseDuration *= 1.5f;
                    break;
                case PostHarvestMethod.FreezeProcessing:
                    baseDuration = 0f; // Immediate processing
                    break;
            }

            // Adjust based on quality (higher quality = longer cure for best results)
            if (harvestResult.Quality >= HarvestQualityGrade.Premium)
            {
                baseDuration *= 1.2f;
            }

            return baseDuration;
        }

        /// <summary>
        /// Generate processing recommendations
        /// </summary>
        private List<string> GenerateProcessingRecommendations(HarvestExecutionResult harvestResult)
        {
            var recommendations = new List<string>
            {
                "Maintain consistent temperature and humidity",
                "Avoid direct light exposure during processing"
            };

            // Quality-specific recommendations
            if (harvestResult.Quality >= HarvestQualityGrade.Premium)
            {
                recommendations.Add("Use extended curing for premium quality");
                recommendations.Add("Monitor trichome integrity during processing");
            }
            else if (harvestResult.Quality <= HarvestQualityGrade.Fair)
            {
                recommendations.Add("Quick processing to minimize further degradation");
            }

            // Method-specific recommendations
            switch (_preferredMethod)
            {
                case PostHarvestMethod.SlowDry:
                    recommendations.Add("Low and slow: 15-18°C, 55-60% RH for best results");
                    break;
                case PostHarvestMethod.QuickDry:
                    recommendations.Add("Ensure adequate airflow to prevent mold");
                    break;
                case PostHarvestMethod.FreezeProcessing:
                    recommendations.Add("Process immediately after harvest for best extraction");
                    break;
            }

            // Temperature/humidity specific
            if (_optimalDryingTemperature > 22f)
            {
                recommendations.Add("Warning: High temperature may degrade terpenes");
            }
            if (_optimalDryingHumidity < 50f)
            {
                recommendations.Add("Warning: Low humidity may cause over-drying");
            }

            return recommendations;
        }

        #endregion
    }
}

