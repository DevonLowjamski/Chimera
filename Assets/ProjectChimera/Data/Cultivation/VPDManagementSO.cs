using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using System;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Advanced Vapor Pressure Deficit (VPD) management system for professional cannabis cultivation.
    /// Implements real-world scientific calculations and professional cultivation VPD strategies.
    /// 
    /// VPD is critical for:
    /// - Optimizing transpiration rates and nutrient uptake
    /// - Preventing mold/mildew while maintaining growth
    /// - Maximizing photosynthesis and yield potential
    /// - Professional cultivation environmental control
    /// </summary>
    [CreateAssetMenu(fileName = "New VPD Management", menuName = "Project Chimera/Cultivation/VPD Management")]
    public class VPDManagementSO : ChimeraConfigSO
    {
        [Header("VPD Target Ranges by Growth Stage")]
        [SerializeField] private VPDStageTarget[] _stageTargets = new VPDStageTarget[]
        {
            new VPDStageTarget { Stage = PlantGrowthStage.Seed, OptimalVPD = 0.4f, MinVPD = 0.2f, MaxVPD = 0.6f },
            new VPDStageTarget { Stage = PlantGrowthStage.Germination, OptimalVPD = 0.5f, MinVPD = 0.3f, MaxVPD = 0.7f },
            new VPDStageTarget { Stage = PlantGrowthStage.Seedling, OptimalVPD = 0.6f, MinVPD = 0.4f, MaxVPD = 0.8f },
            new VPDStageTarget { Stage = PlantGrowthStage.Vegetative, OptimalVPD = 0.9f, MinVPD = 0.7f, MaxVPD = 1.1f },
            new VPDStageTarget { Stage = PlantGrowthStage.PreFlowering, OptimalVPD = 1.0f, MinVPD = 0.8f, MaxVPD = 1.2f },
            new VPDStageTarget { Stage = PlantGrowthStage.Flowering, OptimalVPD = 1.1f, MinVPD = 0.9f, MaxVPD = 1.3f },
            new VPDStageTarget { Stage = PlantGrowthStage.Ripening, OptimalVPD = 1.2f, MinVPD = 1.0f, MaxVPD = 1.4f }
        };
        
        [Header("Photoperiod-Based VPD Adjustments")]
        [SerializeField] private VPDPhotoperiodAdjustment[] _photoperiodAdjustments = new VPDPhotoperiodAdjustment[]
        {
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 0f, VPDMultiplier = 0.8f }, // Lights on
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 2f, VPDMultiplier = 1.0f }, // Ramp up
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 6f, VPDMultiplier = 1.1f }, // Peak day
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 10f, VPDMultiplier = 1.0f }, // Stable
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 16f, VPDMultiplier = 0.9f }, // Evening
            new VPDPhotoperiodAdjustment { HoursIntoPhotoperiod = 18f, VPDMultiplier = 0.7f }  // Lights off
        };
        
        [Header("Environmental Stress Factors")]
        [SerializeField] private float _heatStressVPDReduction = 0.15f; // Reduce VPD target when temperature stress
        [SerializeField] private float _lowLightVPDReduction = 0.1f; // Reduce VPD target in low light
        [SerializeField] private float _rootStressVPDReduction = 0.2f; // Reduce VPD target when root problems
        [SerializeField] private float _nutrientStressVPDReduction = 0.1f; // Reduce VPD target during nutrient stress
        
        [Header("Strain-Specific Adjustments")]
        [SerializeField] private float _indicaDominantVPDModifier = -0.1f; // Indica prefers slightly lower VPD
        [SerializeField] private float _sativaDominantVPDModifier = 0.1f; // Sativa tolerates higher VPD
        [SerializeField] private float _autoflowerVPDModifier = -0.05f; // Autos prefer slightly lower VPD
        [SerializeField] private float _highTHCStrainModifier = 0.05f; // High THC strains often handle higher VPD
        
        [Header("Advanced VPD Calculations")]
        [SerializeField] private bool _useLeafTemperatureOffset = true;
        [SerializeField, Range(-5f, 5f)] private float _leafTemperatureOffset = -2f; // Leaf typically 2°F cooler than air
        [SerializeField] private bool _useDewPointCalculations = true;
        [SerializeField] private bool _compensateForAirMovement = true;
        [SerializeField, Range(0f, 5f)] private float _airMovementFactor = 1.2f; // Air movement increases transpiration
        
        [Header("Professional Cultivation Parameters")]
        [SerializeField] private VPDControlStrategy _defaultControlStrategy = VPDControlStrategy.Balanced;
        [SerializeField] private float _vpdChangeRateLimit = 0.1f; // Maximum VPD change per hour (kPa/hr)
        [SerializeField] private float _criticalVPDDeviationThreshold = 0.3f; // Alert threshold
        [SerializeField] private bool _enableNightTimeVPDControl = true;
        [SerializeField] private float _nightTimeVPDReduction = 0.2f; // Lower VPD during dark period
        
        [Header("Predictive VPD Management")]
        [SerializeField] private bool _enablePredictiveControl = true;
        [SerializeField] private float _predictionHours = 4f; // Hours ahead to predict
        [SerializeField] private AnimationCurve _vpdTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        /// <summary>
        /// Calculates precise VPD using real-world atmospheric physics.
        /// Uses the Magnus formula for saturation vapor pressure calculation.
        /// </summary>
        public float CalculateVPD(float airTemperature, float relativeHumidity, float leafTemperatureOffset = 0f)
        {
            // Use leaf temperature if offset is enabled, otherwise use air temperature
            float leafTemp = _useLeafTemperatureOffset ? airTemperature + _leafTemperatureOffset + leafTemperatureOffset : airTemperature;
            
            // Calculate saturation vapor pressure at leaf temperature using Magnus formula
            // SVP = 0.6108 * exp((17.27 * T) / (T + 237.3)) where T is in Celsius
            float leafSVP = 0.6108f * Mathf.Exp((17.27f * leafTemp) / (leafTemp + 237.3f));
            
            // Calculate saturation vapor pressure at air temperature
            float airSVP = 0.6108f * Mathf.Exp((17.27f * airTemperature) / (airTemperature + 237.3f));
            
            // Calculate actual vapor pressure from relative humidity
            float actualVP = airSVP * (relativeHumidity / 100f);
            
            // VPD = Leaf SVP - Actual VP (both in kPa)
            float vpd = leafSVP - actualVP;
            
            // Compensate for air movement if enabled
            if (_compensateForAirMovement)
            {
                vpd *= _airMovementFactor;
            }
            
            return Mathf.Max(0f, vpd);
        }
        
        /// <summary>
        /// Gets the optimal VPD target for a specific plant considering all factors.
        /// </summary>
        public float GetOptimalVPD(PlantInstanceSO plant, EnvironmentalConditions environment, float currentTimeInPhotoperiod = 0f)
        {
            if (plant == null) return 0.8f; // Default fallback
            
            // Get base VPD target for growth stage
            float baseVPD = GetStageOptimalVPD(plant.CurrentGrowthStage);
            
            // Apply photoperiod adjustments
            float photoperiodModifier = GetPhotoperiodModifier(currentTimeInPhotoperiod, environment.PhotoperiodHours);
            baseVPD *= photoperiodModifier;
            
            // Apply strain-specific adjustments
            float strainModifier = GetStrainVPDModifier(plant.Strain);
            baseVPD += strainModifier;
            
            // Apply environmental stress adjustments
            float stressAdjustment = CalculateStressAdjustment(plant, environment);
            baseVPD += stressAdjustment;
            
            // Apply night time reduction if in dark period
            if (_enableNightTimeVPDControl && IsInDarkPeriod(currentTimeInPhotoperiod, environment.PhotoperiodHours))
            {
                baseVPD *= (1f - _nightTimeVPDReduction);
            }
            
            // Ensure VPD stays within acceptable bounds
            var stageTarget = GetStageTarget(plant.CurrentGrowthStage);
            return Mathf.Clamp(baseVPD, stageTarget.MinVPD, stageTarget.MaxVPD);
        }
        
        /// <summary>
        /// Calculates the required environmental adjustments to achieve target VPD.
        /// </summary>
        public VPDAdjustmentRecommendation GetVPDAdjustmentRecommendation(
            EnvironmentalConditions currentEnvironment, 
            float targetVPD, 
            VPDControlStrategy strategy = VPDControlStrategy.Balanced)
        {
            float currentVPD = CalculateVPD(currentEnvironment.Temperature, currentEnvironment.Humidity);
            float vpdDifference = targetVPD - currentVPD;
            
            var recommendation = new VPDAdjustmentRecommendation
            {
                CurrentVPD = currentVPD,
                TargetVPD = targetVPD,
                VPDDifference = vpdDifference,
                IsWithinRange = Mathf.Abs(vpdDifference) < 0.1f,
                Priority = GetAdjustmentPriority(Mathf.Abs(vpdDifference))
            };
            
            if (recommendation.IsWithinRange)
            {
                recommendation.RecommendedActions = new[] { "VPD is within optimal range. No adjustments needed." };
                return recommendation;
            }
            
            // Calculate adjustment strategies based on control preference
            switch (strategy)
            {
                case VPDControlStrategy.TemperaturePriority:
                    recommendation = CalculateTemperaturePriorityAdjustment(recommendation, currentEnvironment);
                    break;
                case VPDControlStrategy.HumidityPriority:
                    recommendation = CalculateHumidityPriorityAdjustment(recommendation, currentEnvironment);
                    break;
                case VPDControlStrategy.Balanced:
                    recommendation = CalculateBalancedAdjustment(recommendation, currentEnvironment);
                    break;
                case VPDControlStrategy.EnergyEfficient:
                    recommendation = CalculateEnergyEfficientAdjustment(recommendation, currentEnvironment);
                    break;
            }
            
            return recommendation;
        }
        
        /// <summary>
        /// Analyzes VPD trends and provides professional cultivation insights.
        /// </summary>
        public VPDAnalysis AnalyzeVPDPerformance(VPDHistoryData[] vpdHistory, PlantInstanceSO plant)
        {
            var analysis = new VPDAnalysis
            {
                AnalysisTimestamp = DateTime.Now,
                PlantID = plant?.PlantID ?? "Unknown",
                TotalDataPoints = vpdHistory?.Length ?? 0
            };
            
            if (vpdHistory == null || vpdHistory.Length == 0)
            {
                analysis.OverallRating = VPDPerformanceRating.Insufficient_Data;
                analysis.Recommendations = new[] { "Insufficient VPD history data for analysis" };
                return analysis;
            }
            
            // Calculate performance metrics
            float avgVPD = CalculateAverageVPD(vpdHistory);
            float vpdVariability = CalculateVPDVariability(vpdHistory);
            float timeInOptimalRange = CalculateTimeInOptimalRange(vpdHistory, plant);
            float timeInCriticalRange = CalculateTimeInCriticalRange(vpdHistory, plant);
            
            analysis.AverageVPD = avgVPD;
            analysis.VPDVariability = vpdVariability;
            analysis.TimeInOptimalRange = timeInOptimalRange;
            analysis.TimeInCriticalRange = timeInCriticalRange;
            
            // Determine overall rating
            analysis.OverallRating = DeterminePerformanceRating(timeInOptimalRange, timeInCriticalRange, vpdVariability);
            
            // Generate professional recommendations
            analysis.Recommendations = GeneratePerformanceRecommendations(analysis, plant);
            
            // Identify trends and patterns
            analysis.TrendAnalysis = AnalyzeTrends(vpdHistory);
            
            return analysis;
        }
        
        /// <summary>
        /// Calculates dewpoint for advanced humidity control.
        /// </summary>
        public float CalculateDewPoint(float temperature, float relativeHumidity)
        {
            if (!_useDewPointCalculations) return temperature - 10f; // Simple approximation
            
            // Magnus formula for dewpoint calculation
            float a = 17.27f;
            float b = 237.7f;
            float alpha = ((a * temperature) / (b + temperature)) + Mathf.Log(relativeHumidity / 100f);
            float dewPoint = (b * alpha) / (a - alpha);
            
            return dewPoint;
        }
        
        /// <summary>
        /// Predicts future VPD requirements based on plant development and environmental trends.
        /// </summary>
        public VPDForecast PredictVPDRequirements(PlantInstanceSO plant, EnvironmentalConditions currentEnvironment, float forecastHours = 24f)
        {
            if (!_enablePredictiveControl) return null;
            
            var forecast = new VPDForecast
            {
                ForecastStartTime = DateTime.Now,
                ForecastHours = forecastHours,
                PlantID = plant?.PlantID ?? "Unknown"
            };
            
            // Predict plant development
            float expectedGrowthDays = forecastHours / 24f;
            float currentStageProgress = plant.DaysInCurrentStage;
            float predictedStageProgress = currentStageProgress + expectedGrowthDays;
            
            // Determine if stage transition is likely
            PlantGrowthStage predictedStage = PredictGrowthStage(plant.CurrentGrowthStage, predictedStageProgress);
            
            // Calculate VPD requirements for predicted conditions
            float baseVPDRequirement = GetStageOptimalVPD(predictedStage);
            
            // Factor in plant stress trends
            float stressTrend = CalculateStressTrend(plant);
            float adjustedVPD = baseVPDRequirement + (stressTrend * _criticalVPDDeviationThreshold);
            
            forecast.PredictedOptimalVPD = adjustedVPD;
            forecast.PredictedGrowthStage = predictedStage;
            forecast.ConfidenceLevel = CalculateForecastConfidence(expectedGrowthDays);
            forecast.RecommendedPreparations = GenerateForecastRecommendations(forecast, currentEnvironment);
            
            return forecast;
        }
        
        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();
            
            // Validate stage targets
            if (_stageTargets == null || _stageTargets.Length == 0)
            {
                SharedLogger.LogWarning($"VPDManagementSO '{name}' has no stage targets defined.", this);
                isValid = false;
            }
            else
            {
                foreach (var target in _stageTargets)
                {
                    if (target.MinVPD >= target.MaxVPD)
                    {
                        SharedLogger.LogWarning($"VPDManagementSO '{name}' has invalid VPD range for stage {target.Stage}.", this);
                        isValid = false;
                    }
                    
                    if (target.OptimalVPD < target.MinVPD || target.OptimalVPD > target.MaxVPD)
                    {
                        SharedLogger.LogWarning($"VPDManagementSO '{name}' has optimal VPD outside valid range for stage {target.Stage}.", this);
                        isValid = false;
                    }
                }
            }
            
            // Validate photoperiod adjustments
            if (_photoperiodAdjustments != null)
            {
                foreach (var adjustment in _photoperiodAdjustments)
                {
                    if (adjustment.VPDMultiplier <= 0f || adjustment.VPDMultiplier > 2f)
                    {
                        SharedLogger.LogWarning($"VPDManagementSO '{name}' has invalid VPD multiplier: {adjustment.VPDMultiplier}.", this);
                        isValid = false;
                    }
                }
            }
            
            // Validate control parameters
            if (_vpdChangeRateLimit <= 0f)
            {
                SharedLogger.LogWarning($"VPDManagementSO '{name}' has invalid VPD change rate limit.", this);
                _vpdChangeRateLimit = 0.1f;
                isValid = false;
            }
            
            return isValid;
        }
        
        // Private helper methods
        private float GetStageOptimalVPD(PlantGrowthStage stage)
        {
            var target = GetStageTarget(stage);
            return target?.OptimalVPD ?? 0.8f;
        }
        
        private VPDStageTarget GetStageTarget(PlantGrowthStage stage)
        {
            foreach (var target in _stageTargets)
            {
                if (target.Stage == stage) return target;
            }
            return _stageTargets[0]; // Fallback to first target
        }
        
        private float GetPhotoperiodModifier(float currentTimeInPhotoperiod, float totalPhotoperiodHours)
        {
            if (_photoperiodAdjustments == null || _photoperiodAdjustments.Length == 0) return 1f;
            
            // Find the appropriate adjustment based on time in photoperiod
            for (int i = 0; i < _photoperiodAdjustments.Length - 1; i++)
            {
                var current = _photoperiodAdjustments[i];
                var next = _photoperiodAdjustments[i + 1];
                
                if (currentTimeInPhotoperiod >= current.HoursIntoPhotoperiod && 
                    currentTimeInPhotoperiod < next.HoursIntoPhotoperiod)
                {
                    // Interpolate between the two adjustments
                    float t = (currentTimeInPhotoperiod - current.HoursIntoPhotoperiod) / 
                             (next.HoursIntoPhotoperiod - current.HoursIntoPhotoperiod);
                    return Mathf.Lerp(current.VPDMultiplier, next.VPDMultiplier, t);
                }
            }
            
            return _photoperiodAdjustments[_photoperiodAdjustments.Length - 1].VPDMultiplier;
        }
        
        private float GetStrainVPDModifier(PlantStrainSO strain)
        {
            if (strain == null) return 0f;
            
            float modifier = 0f;
            
            // Apply strain-specific modifiers based on genetics
            // This would typically analyze the strain's genetic composition
            // For now, use placeholder logic based on strain characteristics
            
            return modifier;
        }
        
        private float CalculateStressAdjustment(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            float adjustment = 0f;
            
            // Heat stress adjustment
            if (environment.Temperature > 28f) // 82°F
            {
                adjustment -= _heatStressVPDReduction;
            }
            
            // Low light adjustment
            if (environment.LightIntensity < 300f)
            {
                adjustment -= _lowLightVPDReduction;
            }
            
            // Plant health adjustments
            if (plant.OverallHealth < 0.7f)
            {
                adjustment -= _rootStressVPDReduction * (1f - plant.OverallHealth);
            }
            
            // Nutrient stress adjustment
            if (plant.NutrientLevel < 0.5f)
            {
                adjustment -= _nutrientStressVPDReduction;
            }
            
            return adjustment;
        }
        
        private bool IsInDarkPeriod(float currentTimeInPhotoperiod, float photoperiodHours)
        {
            return currentTimeInPhotoperiod >= photoperiodHours;
        }
        
        private VPDAdjustmentRecommendation CalculateBalancedAdjustment(VPDAdjustmentRecommendation recommendation, EnvironmentalConditions environment)
        {
            var actions = new System.Collections.Generic.List<string>();
            
            if (recommendation.VPDDifference > 0.1f) // VPD too low, need to increase
            {
                actions.Add($"Increase temperature by {recommendation.VPDDifference * 2f:F1}°F OR decrease humidity by {recommendation.VPDDifference * 8f:F1}%");
                actions.Add("Consider increasing air circulation to enhance transpiration");
                
                recommendation.TemperatureAdjustment = recommendation.VPDDifference * 1.1f; // °C
                recommendation.HumidityAdjustment = -recommendation.VPDDifference * 4.4f; // %
            }
            else if (recommendation.VPDDifference < -0.1f) // VPD too high, need to decrease
            {
                actions.Add($"Decrease temperature by {Mathf.Abs(recommendation.VPDDifference) * 2f:F1}°F OR increase humidity by {Mathf.Abs(recommendation.VPDDifference) * 8f:F1}%");
                actions.Add("Consider reducing air circulation if excessive");
                
                recommendation.TemperatureAdjustment = recommendation.VPDDifference * 1.1f; // °C
                recommendation.HumidityAdjustment = -recommendation.VPDDifference * 4.4f; // %
            }
            
            recommendation.RecommendedActions = actions.ToArray();
            return recommendation;
        }
        
        private VPDAdjustmentRecommendation CalculateTemperaturePriorityAdjustment(VPDAdjustmentRecommendation recommendation, EnvironmentalConditions environment)
        {
            // Implementation for temperature-priority adjustment strategy
            recommendation.RecommendedActions = new[] { "Temperature-priority VPD adjustment strategy" };
            return recommendation;
        }
        
        private VPDAdjustmentRecommendation CalculateHumidityPriorityAdjustment(VPDAdjustmentRecommendation recommendation, EnvironmentalConditions environment)
        {
            // Implementation for humidity-priority adjustment strategy
            recommendation.RecommendedActions = new[] { "Humidity-priority VPD adjustment strategy" };
            return recommendation;
        }
        
        private VPDAdjustmentRecommendation CalculateEnergyEfficientAdjustment(VPDAdjustmentRecommendation recommendation, EnvironmentalConditions environment)
        {
            // Implementation for energy-efficient adjustment strategy
            recommendation.RecommendedActions = new[] { "Energy-efficient VPD adjustment strategy" };
            return recommendation;
        }
        
        private VPDPriority GetAdjustmentPriority(float vpdDeviation)
        {
            if (vpdDeviation < 0.1f) return VPDPriority.Low;
            if (vpdDeviation < 0.2f) return VPDPriority.Medium;
            if (vpdDeviation < 0.3f) return VPDPriority.High;
            return VPDPriority.Critical;
        }
        
        // Additional helper methods for analysis
        private float CalculateAverageVPD(VPDHistoryData[] history) => 0.8f; // Placeholder
        private float CalculateVPDVariability(VPDHistoryData[] history) => 0.1f; // Placeholder
        private float CalculateTimeInOptimalRange(VPDHistoryData[] history, PlantInstanceSO plant) => 0.7f; // Placeholder
        private float CalculateTimeInCriticalRange(VPDHistoryData[] history, PlantInstanceSO plant) => 0.1f; // Placeholder
        private VPDPerformanceRating DeterminePerformanceRating(float optimalTime, float criticalTime, float variability) => VPDPerformanceRating.Good; // Placeholder
        private string[] GeneratePerformanceRecommendations(VPDAnalysis analysis, PlantInstanceSO plant) => new[] { "VPD performance analysis complete" }; // Placeholder
        private VPDTrendData AnalyzeTrends(VPDHistoryData[] history) => new VPDTrendData(); // Placeholder
        private PlantGrowthStage PredictGrowthStage(PlantGrowthStage current, float progress) => current; // Placeholder
        private float CalculateStressTrend(PlantInstanceSO plant) => 0f; // Placeholder
        private float CalculateForecastConfidence(float daysAhead) => Mathf.Clamp01(1f - daysAhead / 7f); // Placeholder
        private string[] GenerateForecastRecommendations(VPDForecast forecast, EnvironmentalConditions environment) => new[] { "Forecast recommendations" }; // Placeholder
    }
    
    // Supporting data structures for comprehensive VPD management
    [System.Serializable]
    public class VPDStageTarget
    {
        public PlantGrowthStage Stage;
        [Range(0f, 2f)] public float OptimalVPD = 0.8f;
        [Range(0f, 2f)] public float MinVPD = 0.4f;
        [Range(0f, 2f)] public float MaxVPD = 1.2f;
        [TextArea(2, 3)] public string Notes;
    }
    
    [System.Serializable]
    public class VPDPhotoperiodAdjustment
    {
        [Range(0f, 24f)] public float HoursIntoPhotoperiod;
        [Range(0.5f, 1.5f)] public float VPDMultiplier = 1f;
        public string Description;
    }
    
    [System.Serializable]
    public class VPDAdjustmentRecommendation
    {
        public float CurrentVPD;
        public float TargetVPD;
        public float VPDDifference;
        public bool IsWithinRange;
        public VPDPriority Priority;
        public float TemperatureAdjustment; // °C change needed
        public float HumidityAdjustment; // % change needed
        public string[] RecommendedActions;
        public DateTime Timestamp;
    }
    
    [System.Serializable]
    public class VPDAnalysis
    {
        public DateTime AnalysisTimestamp;
        public string PlantID;
        public int TotalDataPoints;
        public float AverageVPD;
        public float VPDVariability;
        public float TimeInOptimalRange; // Percentage
        public float TimeInCriticalRange; // Percentage
        public VPDPerformanceRating OverallRating;
        public string[] Recommendations;
        public VPDTrendData TrendAnalysis;
    }
    
    [System.Serializable]
    public class VPDHistoryData
    {
        public DateTime Timestamp;
        public float VPD;
        public float Temperature;
        public float Humidity;
        public PlantGrowthStage PlantStage;
        public float PlantHealth;
    }
    
    [System.Serializable]
    public class VPDForecast
    {
        public DateTime ForecastStartTime;
        public float ForecastHours;
        public string PlantID;
        public float PredictedOptimalVPD;
        public PlantGrowthStage PredictedGrowthStage;
        public float ConfidenceLevel;
        public string[] RecommendedPreparations;
    }
    
    [System.Serializable]
    public class VPDTrendData
    {
        public bool IsImproving;
        public float TrendSlope;
        public string TrendDescription;
        public DateTime[] CriticalEventTimes;
    }
    
    public enum VPDControlStrategy
    {
        Balanced,           // Balance temperature and humidity adjustments
        TemperaturePriority, // Prefer temperature adjustments
        HumidityPriority,   // Prefer humidity adjustments
        EnergyEfficient     // Minimize energy consumption
    }
    
    public enum VPDPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum VPDPerformanceRating
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical,
        Insufficient_Data
    }
}