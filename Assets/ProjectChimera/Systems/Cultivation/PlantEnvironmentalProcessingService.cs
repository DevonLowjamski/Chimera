using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Environment;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Genetics;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
using ProjectChimera.Systems.Environment;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using EnvironmentalStressSO = ProjectChimera.Data.Simulation.EnvironmentalStressSO;
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
    public class PlantEnvironmentalProcessingService : IPlantEnvironmentalProcessingService
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
        private EnvironmentManager _environmentalManager;
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
        
        public PlantEnvironmentalProcessingService(EnvironmentManager environmentalManager = null)
        {
            _environmentalManager = environmentalManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[PlantEnvironmentalProcessingService] Already initialized");
                return;
            }
            
            // Initialize trait expression engine for genetic-environmental interactions
            if (_enableGxEInteractions)
            {
                _traitExpressionEngine = new object();
            }
            
            // Get environmental manager if not provided
            if (_environmentalManager == null)
            {
                _environmentalManager = GameManager.Instance?.GetManager<EnvironmentManager>();
            }
            
            IsInitialized = true;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantEnvironmentalProcessingService] Initialized successfully");
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
                Debug.Log("[PlantEnvironmentalProcessingService] Shutdown completed");
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
                Debug.Log($"[PlantEnvironmentalProcessingService] Updated environmental processing for plant {plant.PlantID}: Fitness={environmentalFitness:F3}");
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
                Debug.Log($"[PlantEnvironmentalProcessingService] Batch processed {plantsToProcess} plants in {processingTime:F4}s");
            }
        }
        
        /// <summary>
        /// Calculates environmental fitness for a plant based on current conditions.
        /// </summary>
        public float CalculateEnvironmentalFitness(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (plant?.GeneticProfile?.BaseSpecies == null) return 1f;
            
            var species = plant.GeneticProfile.BaseSpecies;
            // Compute simple midpoints instead of requiring species.GetOptimalEnvironment()
            float optimalTemp = (species.TemperatureRange.x + species.TemperatureRange.y) / 2f;
            float optimalHumidity = (species.HumidityRange.x + species.HumidityRange.y) / 2f;
            float optimalLight = (species.LightIntensityRange.x + species.LightIntensityRange.y) / 2f;
            float optimalCO2 = (species.Co2Range.x + species.Co2Range.y) / 2f;
            
            // Calculate fitness for each environmental factor
            float temperatureFitness = CalculateTemperatureFitness(conditions.Temperature, species, optimalTemp);
            float humidityFitness = CalculateHumidityFitness(conditions.Humidity, species, optimalHumidity);
            float lightFitness = CalculateLightFitness(conditions.LightIntensity, species, optimalLight);
            float co2Fitness = CalculateCO2Fitness(conditions.CO2Level, species, optimalCO2);
            
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
                    Debug.Log($"[PlantEnvironmentalProcessingService] Applied stress '{stressSource.StressName}' to plant {plant.PlantID} with intensity {intensity:F2}");
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
                Debug.Log($"[PlantEnvironmentalProcessingService] Removed stress '{stressSource.StressName}' from plant {plant.PlantID}");
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
            if (_environmentalManager != null)
            {
                var cultivationConditions = _environmentalManager.GetCultivationConditions(plant.transform.position);
                return cultivationConditions;
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
                var tempStressSource = CreateEnvironmentalStressSource(conditions, plant.GeneticProfile?.BaseSpecies);
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
        
        private float CalculateTemperatureFitness(float temperature, PlantSpeciesSO species, float optimal)
        {
            var temperatureRange = species.TemperatureRange;
            
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
        
        private float CalculateHumidityFitness(float humidity, PlantSpeciesSO species, float optimal)
        {
            var humidityRange = species.HumidityRange;
            
            if (humidity >= humidityRange.x && humidity <= humidityRange.y)
            {
                float distance = Mathf.Abs(humidity - optimal);
                float maxDistance = Mathf.Max(optimal - humidityRange.x, humidityRange.y - optimal);
                return 1f - (distance / maxDistance) * 0.2f;
            }
            
            float outsideDistance = Mathf.Min(Mathf.Abs(humidity - humidityRange.x), Mathf.Abs(humidity - humidityRange.y));
            return Mathf.Max(0.2f, 0.8f - outsideDistance * 0.05f);
        }
        
        private float CalculateLightFitness(float lightIntensity, PlantSpeciesSO species, float optimal)
        {
            var lightRange = species.LightIntensityRange;
            
            if (lightIntensity >= lightRange.x && lightIntensity <= lightRange.y)
            {
                float distance = Mathf.Abs(lightIntensity - optimal);
                float maxDistance = Mathf.Max(optimal - lightRange.x, lightRange.y - optimal);
                return 1f - (distance / maxDistance) * 0.2f;
            }
            
            float outsideDistance = Mathf.Min(Mathf.Abs(lightIntensity - lightRange.x), Mathf.Abs(lightIntensity - lightRange.y));
            return Mathf.Max(0.2f, 0.8f - outsideDistance * 0.001f);
        }
        
        private float CalculateCO2Fitness(float co2Level, PlantSpeciesSO species, float optimal)
        {
            var co2Range = species.Co2Range;
            
            if (co2Level >= co2Range.x && co2Level <= co2Range.y)
            {
                float distance = Mathf.Abs(co2Level - optimal);
                float maxDistance = Mathf.Max(optimal - co2Range.x, co2Range.y - optimal);
                return 1f - (distance / maxDistance) * 0.15f;
            }
            
            float outsideDistance = Mathf.Min(Mathf.Abs(co2Level - co2Range.x), Mathf.Abs(co2Level - co2Range.y));
            return Mathf.Max(0.3f, 0.9f - outsideDistance * 0.001f);
        }
        
        private EnvironmentalStressSO CreateEnvironmentalStressSource(EnvironmentalConditions conditions, PlantSpeciesSO species)
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
                TemperatureFitness = CalculateTemperatureFitness(conditions.Temperature, plant.GeneticProfile?.BaseSpecies, (plant.GeneticProfile?.BaseSpecies?.TemperatureRange.x + plant.GeneticProfile?.BaseSpecies?.TemperatureRange.y) / 2f ?? 22f),
                HumidityFitness = CalculateHumidityFitness(conditions.Humidity, plant.GeneticProfile?.BaseSpecies, (plant.GeneticProfile?.BaseSpecies?.HumidityRange.x + plant.GeneticProfile?.BaseSpecies?.HumidityRange.y) / 2f ?? 60f),
                LightFitness = CalculateLightFitness(conditions.LightIntensity, plant.GeneticProfile?.BaseSpecies, (plant.GeneticProfile?.BaseSpecies?.LightIntensityRange.x + plant.GeneticProfile?.BaseSpecies?.LightIntensityRange.y) / 2f ?? 400f),
                CO2Fitness = CalculateCO2Fitness(conditions.CO2Level, plant.GeneticProfile?.BaseSpecies, (plant.GeneticProfile?.BaseSpecies?.Co2Range.x + plant.GeneticProfile?.BaseSpecies?.Co2Range.y) / 2f ?? 1000f)
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
            
            var species = plant.GeneticProfile?.BaseSpecies;
            if (species == null)
            {
                return new PlantStressFactors { OverallStressLevel = 0f };
            }
            
            // Calculate individual stress factors
            float tempStress = CalculateTemperatureStress(conditions.Temperature, species);
            float humidityStress = CalculateHumidityStress(conditions.Humidity, species);
            float lightStress = CalculateLightStress(conditions.LightIntensity, species);
            float co2Stress = CalculateCO2Stress(conditions.CO2Level, species);
            
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
        
        private float CalculateTemperatureStress(float temperature, PlantSpeciesSO species)
        {
            float optimal = (species.TemperatureRange.x + species.TemperatureRange.y) / 2f;
            var temperatureFitness = CalculateTemperatureFitness(temperature, species, optimal);
            return 1f - temperatureFitness; // Convert fitness to stress
        }
        
        private float CalculateHumidityStress(float humidity, PlantSpeciesSO species)
        {
            float optimal = (species.HumidityRange.x + species.HumidityRange.y) / 2f;
            var humidityFitness = CalculateHumidityFitness(humidity, species, optimal);
            return 1f - humidityFitness;
        }
        
        private float CalculateLightStress(float lightIntensity, PlantSpeciesSO species)
        {
            float optimal = (species.LightIntensityRange.x + species.LightIntensityRange.y) / 2f;
            var lightFitness = CalculateLightFitness(lightIntensity, species, optimal);
            return 1f - lightFitness;
        }
        
        private float CalculateCO2Stress(float co2Level, PlantSpeciesSO species)
        {
            float optimal = (species.Co2Range.x + species.Co2Range.y) / 2f;
            var co2Fitness = CalculateCO2Fitness(co2Level, species, optimal);
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