using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Handles environmental response calculations and GxE (Genotype × Environment) interactions.
    /// Manages environmental adaptation, fitness calculations, and stress responses to environmental changes.
    /// </summary>
    public class EnvironmentalResponseSystem
    {
        private object _strain;
        private object _gxeProfile; // Use object to avoid type mismatch between different GxE profile types
        private float _environmentalFitness = 1f;
        private EnvironmentalConditions _currentConditions;
        private EnvironmentalConditions _previousConditions;

        // Adaptation tracking
        private Dictionary<string, float> _adaptationLevels = new Dictionary<string, float>();
        private float _overallAdaptation = 1f;
        private List<EnvironmentalChangeEvent> _changeHistory = new List<EnvironmentalChangeEvent>();

        // Configuration and thresholds
        private PlantUpdateConfiguration _configuration;
        private readonly Dictionary<string, Vector2> _optimalRanges = new Dictionary<string, Vector2>();

        // Performance tracking
        private float _lastUpdateTime;
        private const int MAX_CHANGE_HISTORY = 50;

        /// <summary>
        /// Initialize the environmental response system with strain data
        /// </summary>
        public void Initialize(object strain, PlantUpdateConfiguration configuration = null)
        {
            _strain = strain;
            _configuration = configuration ?? PlantUpdateConfiguration.CreateDefault();

            // Initialize adaptation levels
            InitializeAdaptationParameters();

            // Extract environmental preferences from strain
            ExtractStrainEnvironmentalPreferences(strain);

            // Setup optimal ranges
            SetupOptimalRanges();

            _lastUpdateTime = Time.time;

            ChimeraLogger.Log($"[EnvironmentalResponseSystem] Initialized for strain: {strain?.GetType().Name}");
        }

        /// <summary>
        /// Updates environmental responses and calculates fitness
        /// </summary>
        public void UpdateEnvironmentalResponse(EnvironmentalConditions conditions, float deltaTime)
        {
            _previousConditions = _currentConditions;
            _currentConditions = conditions;

            // Calculate environmental fitness
            _environmentalFitness = CalculateEnvironmentalFitness(conditions);

            // Process adaptation if conditions have changed
            if (HasSignificantChange(_previousConditions, conditions))
            {
                ProcessEnvironmentalChange(_previousConditions, conditions);
                ProcessAdaptationResponse(conditions, deltaTime);
            }

            // Update adaptation levels over time
            UpdateAdaptationLevels(conditions, deltaTime);

            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Processes changes in environmental conditions and triggers stress responses
        /// </summary>
        public void ProcessEnvironmentalChange(EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            if (previous.Temperature == 0f) // Invalid previous conditions
                return;

            var changeEvent = new EnvironmentalChangeEvent
            {
                Timestamp = Time.time,
                PreviousConditions = previous,
                CurrentConditions = current
            };

            // Calculate changes in each parameter
            float tempChange = Mathf.Abs(current.Temperature - previous.Temperature);
            float humidityChange = Mathf.Abs(current.Humidity - previous.Humidity);
            float lightChange = Mathf.Abs(current.LightIntensity - previous.LightIntensity);
            float co2Change = Mathf.Abs(current.CO2Level - previous.CO2Level);

            // Determine change severity
            changeEvent.ChangeSeverity = CalculateChangeSeverity(tempChange, humidityChange, lightChange, co2Change);

            // Record change event
            _changeHistory.Add(changeEvent);

            // Limit history size
            if (_changeHistory.Count > MAX_CHANGE_HISTORY)
            {
                _changeHistory.RemoveAt(0);
            }

            // Log significant changes
            if (changeEvent.ChangeSeverity > 0.3f)
            {
                ChimeraLogger.Log($"[EnvironmentalResponseSystem] Significant environmental change detected - " +
                                 $"Temp: {tempChange:F1}°C, Humidity: {humidityChange:F1}%, " +
                                 $"Light: {lightChange:F0} PPFD, CO2: {co2Change:F0} ppm");
            }
        }

        /// <summary>
        /// Processes environmental adaptation for the plant
        /// </summary>
        public void ProcessAdaptation(EnvironmentalConditions conditions, float adaptationRate)
        {
            ProcessAdaptationResponse(conditions, adaptationRate);
        }

        /// <summary>
        /// Get environmental stress factors based on current conditions
        /// </summary>
        public List<StressFactor> GetEnvironmentalStressFactors()
        {
            var stressFactors = new List<StressFactor>();

            if (_currentConditions.Temperature == 0f) return stressFactors; // No valid conditions

            // Temperature stress
            float tempStress = CalculateParameterStress("Temperature", _currentConditions.Temperature);
            if (tempStress > 0.1f)
            {
                stressFactors.Add(new StressFactor
                {
                    StressType = "Temperature",
                    Severity = tempStress,
                    Duration = GetStressDuration("Temperature"),
                    IsAcute = tempStress > 0.7f
                });
            }

            // Humidity stress
            float humidityStress = CalculateParameterStress("Humidity", _currentConditions.Humidity);
            if (humidityStress > 0.1f)
            {
                stressFactors.Add(new StressFactor
                {
                    StressType = "Humidity",
                    Severity = humidityStress,
                    Duration = GetStressDuration("Humidity"),
                    IsAcute = humidityStress > 0.7f
                });
            }

            // Light stress
            float lightStress = CalculateParameterStress("Light", _currentConditions.LightIntensity);
            if (lightStress > 0.1f)
            {
                stressFactors.Add(new StressFactor
                {
                    StressType = "Light",
                    Severity = lightStress,
                    Duration = GetStressDuration("Light"),
                    IsAcute = lightStress > 0.7f
                });
            }

            // CO2 stress
            float co2Stress = CalculateParameterStress("CO2", _currentConditions.CO2Level);
            if (co2Stress > 0.1f)
            {
                stressFactors.Add(new StressFactor
                {
                    StressType = "CO2",
                    Severity = co2Stress,
                    Duration = GetStressDuration("CO2"),
                    IsAcute = co2Stress > 0.7f
                });
            }

            return stressFactors;
        }

        #region Public Properties and Getters

        public float GetEnvironmentalFitness() => _environmentalFitness;
        public float GetOverallAdaptation() => _overallAdaptation;
        public EnvironmentalConditions GetCurrentConditions() => _currentConditions;

        /// <summary>
        /// Get adaptation level for specific environmental parameter
        /// </summary>
        public float GetAdaptationLevel(string parameter)
        {
            return _adaptationLevels.TryGetValue(parameter, out float level) ? level : 1f;
        }

        /// <summary>
        /// Get environmental stress summary
        /// </summary>
        public EnvironmentalStressSummary GetStressSummary()
        {
            var stressFactors = GetEnvironmentalStressFactors();

            return new EnvironmentalStressSummary
            {
                TotalStressFactors = stressFactors.Count,
                OverallStressLevel = stressFactors.Count > 0 ? stressFactors.Average(s => s.Severity) : 0f,
                MostStressfulFactor = stressFactors.OrderByDescending(s => s.Severity).FirstOrDefault()?.GetStressTypeName() ?? "None",
                AdaptationLevel = _overallAdaptation,
                EnvironmentalFitness = _environmentalFitness
            };
        }

        /// <summary>
        /// Get recent environmental changes
        /// </summary>
        public List<EnvironmentalChangeEvent> GetRecentChanges(int count = 10)
        {
            int startIndex = Mathf.Max(0, _changeHistory.Count - count);
            return _changeHistory.GetRange(startIndex, _changeHistory.Count - startIndex);
        }

        #endregion

        #region Private Calculation Methods

        private float CalculateEnvironmentalFitness(EnvironmentalConditions conditions)
        {
            float tempFitness = CalculateTemperatureFitness(conditions.Temperature);
            float humidityFitness = CalculateHumidityFitness(conditions.Humidity);
            float lightFitness = CalculateLightFitness(conditions.LightIntensity);
            float co2Fitness = CalculateCO2Fitness(conditions.CO2Level);

            // Apply adaptation bonuses
            tempFitness *= GetAdaptationLevel("Temperature");
            humidityFitness *= GetAdaptationLevel("Humidity");
            lightFitness *= GetAdaptationLevel("Light");
            co2Fitness *= GetAdaptationLevel("CO2");

            // Weighted average of all environmental factors
            float fitness = (tempFitness * 0.3f) + (humidityFitness * 0.25f) +
                           (lightFitness * 0.3f) + (co2Fitness * 0.15f);

            return Mathf.Clamp01(fitness);
        }

        private float CalculateTemperatureFitness(float temperature)
        {
            var temperatureRange = GetOptimalRange("Temperature");
            return CalculateFitnessFromRange(temperature, temperatureRange);
        }

        private float CalculateHumidityFitness(float humidity)
        {
            var humidityRange = GetOptimalRange("Humidity");
            return CalculateFitnessFromRange(humidity, humidityRange);
        }

        private float CalculateLightFitness(float lightIntensity)
        {
            var lightRange = GetOptimalRange("Light");
            return CalculateFitnessFromRange(lightIntensity, lightRange);
        }

        private float CalculateCO2Fitness(float co2Level)
        {
            var co2Range = GetOptimalRange("CO2");
            return CalculateFitnessFromRange(co2Level, co2Range);
        }

        private float CalculateFitnessFromRange(float value, Vector2 optimalRange)
        {
            // Check if value is within optimal range
            if (value >= optimalRange.x && value <= optimalRange.y)
                return 1f;

            // Calculate distance from nearest edge of optimal range
            float distance = Mathf.Min(Mathf.Abs(value - optimalRange.x),
                                     Mathf.Abs(value - optimalRange.y));

            // Linear falloff beyond optimal range
            float rangeSize = optimalRange.y - optimalRange.x;
            float falloffRange = rangeSize * 0.5f;
            float fitness = 1f - (distance / falloffRange);

            return Mathf.Clamp01(fitness);
        }

        private float CalculateParameterStress(string parameter, float value)
        {
            var optimalRange = GetOptimalRange(parameter);

            if (value >= optimalRange.x && value <= optimalRange.y)
                return 0f; // No stress within optimal range

            // Calculate distance from optimal range
            float distance = value < optimalRange.x ?
                optimalRange.x - value :
                value - optimalRange.y;

            // Normalize stress based on parameter type
            float maxDistance = parameter switch
            {
                "Temperature" => 15f, // 15°C from optimal
                "Humidity" => 40f,    // 40% from optimal
                "Light" => 500f,      // 500 PPFD from optimal
                "CO2" => 800f,        // 800 ppm from optimal
                _ => 10f
            };

            return Mathf.Clamp01(distance / maxDistance);
        }

        private float CalculateChangeSeverity(float tempChange, float humidityChange, float lightChange, float co2Change)
        {
            // Normalize changes to 0-1 scale
            float normalizedTempChange = Mathf.Clamp01(tempChange / 10f); // 10°C = severe
            float normalizedHumidityChange = Mathf.Clamp01(humidityChange / 30f); // 30% = severe
            float normalizedLightChange = Mathf.Clamp01(lightChange / 300f); // 300 PPFD = severe
            float normalizedCO2Change = Mathf.Clamp01(co2Change / 500f); // 500 ppm = severe

            // Weighted average of all changes
            return (normalizedTempChange * 0.4f) + (normalizedHumidityChange * 0.3f) +
                   (normalizedLightChange * 0.2f) + (normalizedCO2Change * 0.1f);
        }

        private void ProcessAdaptationResponse(EnvironmentalConditions conditions, float deltaTime)
        {
            float adaptationRate = _configuration.AdaptationRate * deltaTime;

            // Update adaptation for each parameter
            UpdateParameterAdaptation("Temperature", conditions.Temperature, adaptationRate);
            UpdateParameterAdaptation("Humidity", conditions.Humidity, adaptationRate);
            UpdateParameterAdaptation("Light", conditions.LightIntensity, adaptationRate);
            UpdateParameterAdaptation("CO2", conditions.CO2Level, adaptationRate);

            // Calculate overall adaptation
            _overallAdaptation = _adaptationLevels.Values.Average();
        }

        private void UpdateParameterAdaptation(string parameter, float currentValue, float adaptationRate)
        {
            var optimalRange = GetOptimalRange(parameter);
            float currentFitness = CalculateFitnessFromRange(currentValue, optimalRange);
            float currentAdaptation = GetAdaptationLevel(parameter);

            // Adapt towards current conditions
            if (currentFitness < currentAdaptation)
            {
                // Adapting to worse conditions - slower process
                _adaptationLevels[parameter] = Mathf.Lerp(currentAdaptation, currentFitness, adaptationRate * 0.3f);
            }
            else
            {
                // Adapting to better conditions - faster process
                _adaptationLevels[parameter] = Mathf.Lerp(currentAdaptation, currentFitness, adaptationRate);
            }

            // Clamp adaptation level
            _adaptationLevels[parameter] = Mathf.Clamp(_adaptationLevels[parameter], 0.3f, 1.2f);
        }

        private float GetStressDuration(string parameter)
        {
            // This would track how long each stress factor has been active
            // For now, return a placeholder value
            return Time.time - _lastUpdateTime;
        }

        #endregion

        #region Private Helper Methods

        private void InitializeAdaptationParameters()
        {
            _adaptationLevels["Temperature"] = 1f;
            _adaptationLevels["Humidity"] = 1f;
            _adaptationLevels["Light"] = 1f;
            _adaptationLevels["CO2"] = 1f;
        }

        private void ExtractStrainEnvironmentalPreferences(object strain)
        {
            try
            {
                if (strain is ProjectChimera.Data.Genetics.PlantGeneticsData geneticsStrain)
                {
                    // Try to get GxE profile from genetics strain
                    _gxeProfile = geneticsStrain.GxEProfile;
                }
                else if (strain is ProjectChimera.Data.Cultivation.PlantStrainSO cultivationStrain)
                {
                    // Try to get GxE profile from cultivation strain
                    _gxeProfile = cultivationStrain.GxEProfile;
                }
                else
                {
                    ChimeraLogger.LogWarning("[EnvironmentalResponseSystem] Strain type not recognized, using defaults");
                }
            }
            catch
            {
                _gxeProfile = null;
                ChimeraLogger.LogWarning("[EnvironmentalResponseSystem] Could not extract strain environmental preferences");
            }
        }

        private void SetupOptimalRanges()
        {
            // Default optimal ranges - would be extracted from strain data in full implementation
            _optimalRanges["Temperature"] = new Vector2(20f, 26f);  // 20-26°C
            _optimalRanges["Humidity"] = new Vector2(45f, 65f);     // 45-65% RH
            _optimalRanges["Light"] = new Vector2(300f, 700f);      // 300-700 PPFD
            _optimalRanges["CO2"] = new Vector2(800f, 1200f);       // 800-1200 ppm
        }

        private Vector2 GetOptimalRange(string parameter)
        {
            return _optimalRanges.TryGetValue(parameter, out Vector2 range) ? range : Vector2.zero;
        }

        private bool HasSignificantChange(EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            if (previous.Temperature == 0f) return false; // No valid previous data

            float tempChange = Mathf.Abs(current.Temperature - previous.Temperature);
            float humidityChange = Mathf.Abs(current.Humidity - previous.Humidity);
            float lightChange = Mathf.Abs(current.LightIntensity - previous.LightIntensity);
            float co2Change = Mathf.Abs(current.CO2Level - previous.CO2Level);

            // Consider changes significant if any parameter changes beyond thresholds
            return tempChange > 2f || humidityChange > 10f || lightChange > 100f || co2Change > 200f;
        }

        private void UpdateAdaptationLevels(EnvironmentalConditions conditions, float deltaTime)
        {
            // Gradual adaptation over time
            ProcessAdaptationResponse(conditions, deltaTime);
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Get recommended environmental adjustments
        /// </summary>
        public List<EnvironmentalRecommendation> GetEnvironmentalRecommendations()
        {
            var recommendations = new List<EnvironmentalRecommendation>();
            var stressFactors = GetEnvironmentalStressFactors();

            foreach (var stressFactor in stressFactors.Where(s => s.Severity > 0.3f))
            {
                string parameter = stressFactor.GetStressTypeName();
                var optimalRange = GetOptimalRange(parameter);
                float currentValue = GetCurrentParameterValue(parameter);

                var recommendation = new EnvironmentalRecommendation
                {
                    Parameter = parameter,
                    CurrentValue = currentValue,
                    OptimalRange = optimalRange,
                    Priority = stressFactor.Severity > 0.7f ? "High" : "Medium",
                    Suggestion = GenerateAdjustmentSuggestion(parameter, currentValue, optimalRange)
                };

                recommendations.Add(recommendation);
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Predict fitness under different environmental conditions
        /// </summary>
        public float PredictFitnessForConditions(EnvironmentalConditions predictedConditions)
        {
            return CalculateEnvironmentalFitness(predictedConditions);
        }

        /// <summary>
        /// Reset adaptation levels (for testing or strain changes)
        /// </summary>
        public void ResetAdaptation()
        {
            InitializeAdaptationParameters();
            _overallAdaptation = 1f;
            _changeHistory.Clear();

            ChimeraLogger.Log("[EnvironmentalResponseSystem] Adaptation levels reset");
        }

        #endregion

        #region Private Helper Methods (continued)

        private float GetCurrentParameterValue(string parameter)
        {
            return parameter switch
            {
                "Temperature" => _currentConditions.Temperature,
                "Humidity" => _currentConditions.Humidity,
                "Light" => _currentConditions.LightIntensity,
                "CO2" => _currentConditions.CO2Level,
                _ => 0f
            };
        }

        private string GenerateAdjustmentSuggestion(string parameter, float currentValue, Vector2 optimalRange)
        {
            if (currentValue < optimalRange.x)
                return $"Increase {parameter} to at least {optimalRange.x:F1}";
            else if (currentValue > optimalRange.y)
                return $"Decrease {parameter} to no more than {optimalRange.y:F1}";
            else
                return $"Maintain {parameter} within optimal range";
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Environmental change event for tracking history
    /// </summary>
    [System.Serializable]
    public class EnvironmentalChangeEvent
    {
        public float Timestamp { get; set; }
        public EnvironmentalConditions PreviousConditions { get; set; }
        public EnvironmentalConditions CurrentConditions { get; set; }
        public float ChangeSeverity { get; set; }
    }

    /// <summary>
    /// Summary of environmental stress factors
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStressSummary
    {
        public int TotalStressFactors { get; set; }
        public float OverallStressLevel { get; set; }
        public string MostStressfulFactor { get; set; }
        public float AdaptationLevel { get; set; }
        public float EnvironmentalFitness { get; set; }
    }

    /// <summary>
    /// Environmental adjustment recommendation
    /// </summary>
    [System.Serializable]
    public class EnvironmentalRecommendation
    {
        public string Parameter { get; set; }
        public float CurrentValue { get; set; }
        public Vector2 OptimalRange { get; set; }
        public string Priority { get; set; }
        public string Suggestion { get; set; }
    }

    #endregion
}
