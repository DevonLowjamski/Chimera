using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Genetics;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
// using ProjectChimera.Systems.Environment; // Environment assembly not available
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using EnvironmentalStressSO = ProjectChimera.Data.Simulation.EnvironmentalStressSO;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
// using BaseSpecies = ProjectChimera.Data.Genetics.BaseSpecies; // Removed due to object type changes
// Remove dependencies on Systems.Genetics to avoid missing namespace errors in Systems assembly
// using StressResponse = ProjectChimera.Systems.Genetics.StressResponse;
// using StressFactor = ProjectChimera.Systems.Genetics.StressFactor;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-4b: Plant Environmental Processing Service - Handles environmental effect calculations
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on environmental adaptation, stress application, and environmental fitness calculations
    /// </summary>
    public class PlantEnvironmentalProcessingService
    {
        [Header("Environmental Processing Configuration")]
        [SerializeField] private bool _enableStressSystem = true;
        [SerializeField] private bool _enableGxEInteractions = true;
        [SerializeField] private bool _enableEnvironmentalAdaptation = true;
        [SerializeField] private bool _enableDetailedLogging = false;

        [Header("Stress System Settings")]
        [SerializeField] private float _stressRecoveryRate = 0.1f;
        [SerializeField] private float _stressThreshold = 0.7f;
        [SerializeField] private float _adaptationRate = 0.05f;
        [SerializeField] private float _environmentalUpdateInterval = 2.0f;

        [Header("Performance Settings")]
        [SerializeField] private int _maxPlantsPerEnvironmentalUpdate = 50;
        [SerializeField] private bool _enableBatchProcessing = true;

        // Dependencies
        // private EnvironmentManager _environmentalManager; // EnvironmentManager not available
        private object _traitExpressionEngine;

        // Environmental tracking
        private float _lastEnvironmentalUpdate = 0f;
        private Dictionary<string, float> _plantEnvironmentalFitness = new Dictionary<string, float>();
        private Dictionary<string, EnvironmentalConditions> _plantLastConditions = new Dictionary<string, EnvironmentalConditions>();
        private Dictionary<string, float> _plantAdaptationProgress = new Dictionary<string, float>();
        private Dictionary<string, List<ActiveStressor>> _plantActiveStressors = new Dictionary<string, List<ActiveStressor>>();

        // Performance tracking
        private int _environmentalCalculationsPerformed = 0;
        private float _totalEnvironmentalProcessingTime = 0f;
        private int _stressApplicationsPerformed = 0;
        private int _adaptationUpdatesPerformed = 0;

        public bool IsInitialized { get; private set; }

        public bool EnableStressSystem
        {
            get => _enableStressSystem;
            set => _enableStressSystem = value;
        }

        public bool EnableGxEInteractions
        {
            get => _enableGxEInteractions;
            set => _enableGxEInteractions = value;
        }

        public float StressRecoveryRate
        {
            get => _stressRecoveryRate;
            set => _stressRecoveryRate = Mathf.Clamp(value, 0f, 1f);
        }

        public PlantEnvironmentalProcessingService(/* EnvironmentManager environmentalManager = null */ )
        {
            // _environmentalManager = environmentalManager; // EnvironmentManager not available
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                ChimeraLogger.LogWarning("[PlantEnvironmentalProcessingService] Already initialized");
                return;
            }

            // Initialize trait expression engine for genetic-environmental interactions
            if (_enableGxEInteractions)
            {
                _traitExpressionEngine = new object();
            }

            // Get environmental manager if not provided
            // if (_environmentalManager == null) // Commented out - environmental manager not yet implemented
            {
                // _environmentalManager = GameManager.Instance?.GetManager<EnvironmentManager>(); // EnvironmentManager not available
            }

            IsInitialized = true;

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log("[PlantEnvironmentalProcessingService] Initialized successfully");
            }
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            _plantEnvironmentalFitness.Clear();
            _plantLastConditions.Clear();
            _plantAdaptationProgress.Clear();
            _plantActiveStressors.Clear();
            _traitExpressionEngine = null;

            IsInitialized = false;

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log("[PlantEnvironmentalProcessingService] Shutdown completed");
            }
        }

        /// <summary>
        /// Updates environmental processing for a single plant.
        /// </summary>
        public void UpdatePlantEnvironmentalProcessing(PlantInstance plant, float deltaTime)
        {
            if (!IsInitialized || plant == null || !plant.IsActive)
                return;

            var startTime = Time.realtimeSinceStartup;

            // Get current environmental conditions
            var currentConditions = GetPlantEnvironmentalConditions(plant);

            // Calculate environmental fitness
            float environmentalFitness = CalculateEnvironmentalFitness(plant, currentConditions);

            // Update plant's environmental conditions if they've changed
            UpdatePlantEnvironmentalConditions(plant, currentConditions);

            // Process environmental adaptation
            if (_enableEnvironmentalAdaptation)
            {
                ProcessEnvironmentalAdaptation(plant, currentConditions, deltaTime);
            }

            // Apply environmental stress effects
            if (_enableStressSystem)
            {
                ProcessEnvironmentalStress(plant, currentConditions, environmentalFitness, deltaTime);
            }

            // Process GxE interactions if enabled
            if (_enableGxEInteractions && _traitExpressionEngine != null)
            {
                ProcessGxEInteractions(plant, currentConditions, deltaTime);
            }

            // Update tracking data
            UpdateEnvironmentalTracking(plant, currentConditions, environmentalFitness);

            // Performance tracking
            _environmentalCalculationsPerformed++;
            _totalEnvironmentalProcessingTime += Time.realtimeSinceStartup - startTime;

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalProcessingService] Updated environmental processing for plant {plant.PlantID}: Fitness={environmentalFitness:F3}");
            }
        }

        /// <summary>
        /// Updates environmental processing for multiple plants in a batch.
        /// </summary>
        public void UpdatePlantEnvironmentalProcessingBatch(List<PlantInstance> plants, float deltaTime)
        {
            if (!IsInitialized || plants == null || plants.Count == 0)
                return;

            var startTime = Time.realtimeSinceStartup;

            // Process plants in batches for performance optimization
            int plantsToProcess = Mathf.Min(_maxPlantsPerEnvironmentalUpdate, plants.Count);

            for (int i = 0; i < plantsToProcess; i++)
            {
                var plant = plants[i];
                if (plant != null && plant.IsActive)
                {
                    UpdatePlantEnvironmentalProcessing(plant, deltaTime);
                }
            }

            if (_enableDetailedLogging)
            {
                var processingTime = Time.realtimeSinceStartup - startTime;
                ChimeraLogger.Log($"[PlantEnvironmentalProcessingService] Batch processed {plantsToProcess} plants in {processingTime:F4}s");
            }
        }

        /// <summary>
        /// Calculates environmental fitness for a plant based on current conditions.
        /// </summary>
        public float CalculateEnvironmentalFitness(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (plant?.GeneticProfile == null) return 1f;

            // Use default optimal ranges for cannabis (simplified)
            float optimalTemp = 25f; // 25°C optimal temperature
            float optimalHumidity = 50f; // 50% optimal humidity
            float optimalLight = 800f; // 800 µmol/m²/s optimal light
            float optimalCO2 = 400f; // 400 ppm optimal CO2

            // Calculate fitness for each environmental factor using simplified formulas
            float temperatureFitness = CalculateTemperatureFitness(conditions.Temperature, optimalTemp);
            float humidityFitness = CalculateHumidityFitness(conditions.Humidity, optimalHumidity);
            float lightFitness = CalculateLightFitness(conditions.LightIntensity, optimalLight);
            float co2Fitness = CalculateCO2Fitness(conditions.CO2Level, optimalCO2);

            // Calculate overall environmental fitness (weighted average)
            float overallFitness = (temperatureFitness * 0.3f +
                                  humidityFitness * 0.25f +
                                  lightFitness * 0.25f +
                                  co2Fitness * 0.2f);

            return Mathf.Clamp01(overallFitness);
        }

        /// <summary>
        /// Applies environmental stress to a plant based on unfavorable conditions.
        /// </summary>
        public void ApplyEnvironmentalStress(PlantInstance plant, EnvironmentalStressSO stressSource, float intensity)
        {
            if (!IsInitialized || !_enableStressSystem || plant == null || stressSource == null)
                return;

            bool stressApplied = plant.ApplyStress(stressSource, intensity);

            if (stressApplied)
            {
                // Track stress application
                var plantId = plant.PlantID;
                if (!_plantActiveStressors.ContainsKey(plantId))
                {
                    _plantActiveStressors[plantId] = new List<ActiveStressor>();
                }

                // Add or update stressor
                var existingStressor = _plantActiveStressors[plantId].FirstOrDefault(s => s.StressSource == stressSource);
                if (existingStressor != null)
                {
                    existingStressor.Intensity = intensity;
                    // Duration is now computed from StartTime
                }
                else
                {
                    _plantActiveStressors[plantId].Add(new ActiveStressor
                    {
                        StressSource = stressSource,
                        Intensity = intensity,
                        StartTime = Time.time,
                        IsActive = true
                    });
                }

                _stressApplicationsPerformed++;

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantEnvironmentalProcessingService] Applied stress '{stressSource.StressName}' to plant {plant.PlantID} with intensity {intensity:F2}");
                }
            }
        }

        /// <summary>
        /// Removes environmental stress from a plant.
        /// </summary>
        public void RemoveEnvironmentalStress(PlantInstance plant, EnvironmentalStressSO stressSource)
        {
            if (!IsInitialized || plant == null || stressSource == null)
                return;

            plant.RemoveStress(stressSource);

            // Remove from tracking
            var plantId = plant.PlantID;
            if (_plantActiveStressors.ContainsKey(plantId))
            {
                _plantActiveStressors[plantId].RemoveAll(s => s.StressSource == stressSource);
            }

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalProcessingService] Removed stress '{stressSource.StressName}' from plant {plant.PlantID}");
            }
        }

        /// <summary>
        /// Gets the environmental fitness for a specific plant.
        /// </summary>
        public float GetPlantEnvironmentalFitness(string plantId)
        {
            return _plantEnvironmentalFitness.GetValueOrDefault(plantId, 1f);
        }

        /// <summary>
        /// Gets the active stressors affecting a specific plant.
        /// </summary>
        public List<ActiveStressor> GetPlantActiveStressors(string plantId)
        {
            return _plantActiveStressors.GetValueOrDefault(plantId, new List<ActiveStressor>());
        }

        /// <summary>
        /// Gets comprehensive environmental processing statistics.
        /// </summary>
        public EnvironmentalProcessingStatistics GetEnvironmentalStatistics()
        {
            return new EnvironmentalProcessingStatistics
            {
                TotalEnvironmentalCalculations = _environmentalCalculationsPerformed,
                AverageCalculationTime = _environmentalCalculationsPerformed > 0 ? (_totalEnvironmentalProcessingTime / _environmentalCalculationsPerformed) * 1000f : 0f,
                TrackedPlants = _plantEnvironmentalFitness.Count,
                StressApplications = _stressApplicationsPerformed,
                AdaptationUpdates = _adaptationUpdatesPerformed,
                StressSystemEnabled = _enableStressSystem,
                GxEInteractionsEnabled = _enableGxEInteractions,
                EnvironmentalAdaptationEnabled = _enableEnvironmentalAdaptation
            };
        }

        #region Private Helper Methods

        private EnvironmentalConditions GetPlantEnvironmentalConditions(PlantInstance plant)
        {
            // Get environmental conditions directly from the plant
            var dataConditions = plant.GetCurrentEnvironmentalConditions();

            // Validate that the conditions are initialized
            if (dataConditions.IsInitialized())
            {
                return dataConditions;
            }

            // Fallback to environmental manager if plant conditions are not available
            // Environmental manager not yet implemented - using default conditions
            if (false) // Temporarily disabled until EnvironmentManager is available
            {
                // var cultivationConditions = _environmentalManager.GetCultivationConditions(plant.transform.position);
                // return cultivationConditions;
            }

            // Final fallback to default indoor conditions
            return EnvironmentalConditions.CreateIndoorDefault();
        }

        private void UpdatePlantEnvironmentalConditions(PlantInstance plant, EnvironmentalConditions newConditions)
        {
            var plantId = plant.PlantID;
            var previousConditions = _plantLastConditions.GetValueOrDefault(plantId, default);

            // Update plant's environmental conditions
            plant.UpdateEnvironmentalConditions(newConditions);

            // Process environmental change if conditions have changed significantly
            if (HasSignificantEnvironmentalChange(previousConditions, newConditions))
            {
                ProcessEnvironmentalChange(plant, previousConditions, newConditions);
            }

            // Update tracking
            _plantLastConditions[plantId] = newConditions;
        }

        private void ProcessEnvironmentalAdaptation(PlantInstance plant, EnvironmentalConditions conditions, float deltaTime)
        {
            var plantId = plant.PlantID;
            var currentAdaptation = _plantAdaptationProgress.GetValueOrDefault(plantId, 0f);

            // Calculate environmental fitness for adaptation assessment
            float environmentalFitness = CalculateEnvironmentalFitness(plant, conditions);

            // Apply adaptation over time
            if (environmentalFitness < 0.8f)
            {
                // Poor conditions - plant tries to adapt
                currentAdaptation += _adaptationRate * deltaTime;
            }
            else
            {
                // Good conditions - maintain current adaptation
                currentAdaptation = Mathf.Max(0f, currentAdaptation - _adaptationRate * 0.5f * deltaTime);
            }

            currentAdaptation = Mathf.Clamp01(currentAdaptation);
            _plantAdaptationProgress[plantId] = currentAdaptation;

            // Apply adaptation to plant's environmental conditions
            plant.UpdateEnvironmentalAdaptation(conditions);

            _adaptationUpdatesPerformed++;
        }

        private void ProcessEnvironmentalStress(PlantInstance plant, EnvironmentalConditions conditions, float environmentalFitness, float deltaTime)
        {
            // Apply stress based on environmental fitness
            if (environmentalFitness < _stressThreshold)
            {
                float stressIntensity = (1f - environmentalFitness) * 0.5f;

                // Create temporary stress source for environmental conditions
                var tempStressSource = CreateEnvironmentalStressSource(conditions);
                if (tempStressSource != null)
                {
                    ApplyEnvironmentalStress(plant, tempStressSource, stressIntensity);
                }
            }

            // Process stress recovery for plants in good conditions
            if (environmentalFitness > 0.8f)
            {
                ProcessStressRecovery(plant, deltaTime);
            }
        }

        private void ProcessGxEInteractions(PlantInstance plant, EnvironmentalConditions conditions, float deltaTime)
        {
            // TODO: Implement GxE interactions when genetic system integration is resolved
            // This would calculate how genetic traits interact with environmental conditions
            // For now, this is a placeholder for future implementation
        }

        private void ProcessEnvironmentalChange(PlantInstance plant, EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            // Calculate stress from rapid environmental changes
            if (previous.Temperature != 0f) // Valid previous conditions
            {
                float tempChange = Mathf.Abs(current.Temperature - previous.Temperature);
                float humidityChange = Mathf.Abs(current.Humidity - previous.Humidity);

                // Apply shock stress for rapid changes
                if (tempChange > 5f || humidityChange > 20f)
                {
                    var shockStress = CreateEnvironmentalShockStress(tempChange, humidityChange);
                    if (shockStress != null)
                    {
                        ApplyEnvironmentalStress(plant, shockStress, 0.3f);
                    }
                }
            }
        }

        private void ProcessStressRecovery(PlantInstance plant, float deltaTime)
        {
            var plantId = plant.PlantID;
            if (_plantActiveStressors.ContainsKey(plantId))
            {
                var stressors = _plantActiveStressors[plantId];

                // Apply recovery to environmental stressors
                for (int i = stressors.Count - 1; i >= 0; i--)
                {
                    var stressor = stressors[i];
                    stressor.Intensity -= _stressRecoveryRate * deltaTime;

                    if (stressor.Intensity <= 0f)
                    {
                        RemoveEnvironmentalStress(plant, stressor.StressSource);
                    }
                }
            }
        }

        private void UpdateEnvironmentalTracking(PlantInstance plant, EnvironmentalConditions conditions, float environmentalFitness)
        {
            var plantId = plant.PlantID;
            _plantEnvironmentalFitness[plantId] = environmentalFitness;
        }

        private bool HasSignificantEnvironmentalChange(EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            if (previous.Temperature == 0f) return false; // No valid previous data

            float tempDiff = Mathf.Abs(current.Temperature - previous.Temperature);
            float humidityDiff = Mathf.Abs(current.Humidity - previous.Humidity);
            float lightDiff = Mathf.Abs(current.LightIntensity - previous.LightIntensity);

            return tempDiff > 2f || humidityDiff > 10f || lightDiff > 100f;
        }

        private float CalculateTemperatureFitness(float temperature, float optimal)
        {
            // Cannabis temperature range: 18-28°C
            var temperatureRange = new Vector2(18f, 28f);

            if (temperature >= temperatureRange.x && temperature <= temperatureRange.y)
            {
                // Within tolerance range - calculate fitness based on distance from optimal
                float distance = Mathf.Abs(temperature - optimal);
                float maxDistance = Mathf.Max(optimal - temperatureRange.x, temperatureRange.y - optimal);
                return 1f - (distance / maxDistance) * 0.3f; // Max 30% fitness reduction within range
            }

            // Outside tolerance range - severe fitness penalty
            float outsideDistance = Mathf.Min(Mathf.Abs(temperature - temperatureRange.x), Mathf.Abs(temperature - temperatureRange.y));
            return Mathf.Max(0.1f, 0.7f - outsideDistance * 0.1f); // Severe penalty, minimum 10% fitness
        }

        private float CalculateHumidityFitness(float humidity, float optimal)
        {
            // Cannabis humidity range: 30-70%
            var humidityRange = new Vector2(30f, 70f);

            if (humidity >= humidityRange.x && humidity <= humidityRange.y)
            {
                float distance = Mathf.Abs(humidity - optimal);
                float maxDistance = Mathf.Max(optimal - humidityRange.x, humidityRange.y - optimal);
                return 1f - (distance / maxDistance) * 0.2f;
            }

            float outsideDistance = Mathf.Min(Mathf.Abs(humidity - humidityRange.x), Mathf.Abs(humidity - humidityRange.y));
            return Mathf.Max(0.2f, 0.8f - outsideDistance * 0.05f);
        }

        private float CalculateLightFitness(float lightIntensity, float optimal)
        {
            // Cannabis light range: 200-1200 µmol/m²/s
            var lightRange = new Vector2(200f, 1200f);

            if (lightIntensity >= lightRange.x && lightIntensity <= lightRange.y)
            {
                float distance = Mathf.Abs(lightIntensity - optimal);
                float maxDistance = Mathf.Max(optimal - lightRange.x, lightRange.y - optimal);
                return 1f - (distance / maxDistance) * 0.2f;
            }

            float outsideDistance = Mathf.Min(Mathf.Abs(lightIntensity - lightRange.x), Mathf.Abs(lightIntensity - lightRange.y));
            return Mathf.Max(0.2f, 0.8f - outsideDistance * 0.001f);
        }

        private float CalculateCO2Fitness(float co2Level, float optimal)
        {
            // Cannabis CO2 range: 300-1500 ppm
            var co2Range = new Vector2(300f, 1500f);

            if (co2Level >= co2Range.x && co2Level <= co2Range.y)
            {
                float distance = Mathf.Abs(co2Level - optimal);
                float maxDistance = Mathf.Max(optimal - co2Range.x, co2Range.y - optimal);
                return 1f - (distance / maxDistance) * 0.15f;
            }

            float outsideDistance = Mathf.Min(Mathf.Abs(co2Level - co2Range.x), Mathf.Abs(co2Level - co2Range.y));
            return Mathf.Max(0.3f, 0.9f - outsideDistance * 0.001f);
        }

        private EnvironmentalStressSO CreateEnvironmentalStressSource(EnvironmentalConditions conditions)
        {
            // This would create temporary stress sources based on environmental conditions
            // For now, return null as this requires ScriptableObject creation which should be done in assets
            return null;
        }

        private EnvironmentalStressSO CreateEnvironmentalShockStress(float tempChange, float humidityChange)
        {
            // This would create temporary shock stress sources
            // For now, return null as this requires ScriptableObject creation which should be done in assets
            return null;
        }

        /// <summary>
        /// Process environmental effects and return result data
        /// </summary>
        public EnvironmentalProcessingResult ProcessEnvironmentalEffects(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (!IsInitialized || plant == null)
            {
                return new EnvironmentalProcessingResult
                {
                    EnvironmentalFitness = 1f,
                    StressLevel = 0f,
                    AdaptationProgress = 0f
                };
            }

            var startTime = Time.realtimeSinceStartup;

            // Calculate environmental fitness
            float environmentalFitness = CalculateEnvironmentalFitness(plant, conditions);

            // Calculate overall stress level
            var stressFactors = CalculateStressFactors(plant, conditions);

            // Get adaptation progress
            var plantId = plant.PlantID;
            float adaptationProgress = _plantAdaptationProgress.GetValueOrDefault(plantId, 0f);

            // Update plant environmental conditions
            UpdatePlantEnvironmentalConditions(plant, conditions);

            var processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            return new EnvironmentalProcessingResult
            {
                EnvironmentalFitness = environmentalFitness,
                StressLevel = stressFactors.OverallStressLevel,
                AdaptationProgress = adaptationProgress,
                ProcessingTimeMs = processingTime,
                TemperatureFitness = CalculateTemperatureFitness(conditions.Temperature, 22f),
                HumidityFitness = CalculateHumidityFitness(conditions.Humidity, 60f),
                LightFitness = CalculateLightFitness(conditions.LightIntensity, 400f),
                CO2Fitness = CalculateCO2Fitness(conditions.CO2Level, 1000f)
            };
        }

        /// <summary>
        /// Calculate comprehensive stress factors for a plant
        /// </summary>
        public PlantStressFactors CalculateStressFactors(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (plant == null)
            {
                return new PlantStressFactors { OverallStressLevel = 0f };
            }

            // Note: GeneticProfile is object type, using default stress calculations
            if (plant.GeneticProfile == null)
            {
                return new PlantStressFactors { OverallStressLevel = 0f };
            }

            // Calculate individual stress factors using default cannabis ranges
            float tempStress = CalculateTemperatureStress(conditions.Temperature);
            float humidityStress = CalculateHumidityStress(conditions.Humidity);
            float lightStress = CalculateLightStress(conditions.LightIntensity);
            float co2Stress = CalculateCO2Stress(conditions.CO2Level);

            // Calculate overall stress level (weighted average)
            float overallStress = (tempStress * 0.3f + humidityStress * 0.25f + lightStress * 0.25f + co2Stress * 0.2f);

            return new PlantStressFactors
            {
                OverallStressLevel = Mathf.Clamp01(overallStress),
                TemperatureStress = tempStress,
                HumidityStress = humidityStress,
                LightStress = lightStress,
                CO2Stress = co2Stress,
                HasCriticalStress = overallStress > 0.8f,
                StressFactorCount = (tempStress > 0.1f ? 1 : 0) + (humidityStress > 0.1f ? 1 : 0) + (lightStress > 0.1f ? 1 : 0) + (co2Stress > 0.1f ? 1 : 0)
            };
        }

        private float CalculateTemperatureStress(float temperature)
        {
            float optimal = 25f; // Optimal temperature for cannabis
            var temperatureFitness = CalculateTemperatureFitness(temperature, optimal);
            return 1f - temperatureFitness; // Convert fitness to stress
        }

        private float CalculateHumidityStress(float humidity)
        {
            float optimal = 50f; // Optimal humidity for cannabis
            var humidityFitness = CalculateHumidityFitness(humidity, optimal);
            return 1f - humidityFitness;
        }

        private float CalculateLightStress(float lightIntensity)
        {
            float optimal = 800f; // Optimal light intensity for cannabis
            var lightFitness = CalculateLightFitness(lightIntensity, optimal);
            return 1f - lightFitness;
        }

        private float CalculateCO2Stress(float co2Level)
        {
            float optimal = 400f; // Optimal CO2 level for cannabis
            var co2Fitness = CalculateCO2Fitness(co2Level, optimal);
            return 1f - co2Fitness;
        }

        #endregion
    }

    /// <summary>
    /// Result data structure for environmental processing
    /// </summary>
    [System.Serializable]
    public class EnvironmentalProcessingResult
    {
        public float EnvironmentalFitness;
        public float StressLevel;
        public float AdaptationProgress;
        public float ProcessingTimeMs;
        public float TemperatureFitness;
        public float HumidityFitness;
        public float LightFitness;
        public float CO2Fitness;
    }

    /// <summary>
    /// Comprehensive stress factors data structure
    /// </summary>
    [System.Serializable]
    public class PlantStressFactors
    {
        public float OverallStressLevel;
        public float TemperatureStress;
        public float HumidityStress;
        public float LightStress;
        public float CO2Stress;
        public bool HasCriticalStress;
        public int StressFactorCount;
    }

    /// <summary>
    /// Environmental processing statistics structure.
    /// </summary>
    [System.Serializable]
    public class EnvironmentalProcessingStatistics
    {
        public int TotalEnvironmentalCalculations;
        public float AverageCalculationTime; // milliseconds
        public int TrackedPlants;
        public int StressApplications;
        public int AdaptationUpdates;
        public bool StressSystemEnabled;
        public bool GxEInteractionsEnabled;
        public bool EnvironmentalAdaptationEnabled;

        public override string ToString()
        {
            return $"Environmental Stats: {TotalEnvironmentalCalculations} calcs, {AverageCalculationTime:F2}ms avg, {TrackedPlants} plants, {StressApplications} stress applications";
        }
    }
}
