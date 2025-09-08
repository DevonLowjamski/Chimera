using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;
using EnvironmentalStressSO = ProjectChimera.Data.Simulation.EnvironmentalStressSO;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using DataEnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using EnvironmentEnvironmentalConditions = ProjectChimera.Data.Environment.EnvironmentalConditions;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Environmental Service - Handles environmental adaptation and stress management
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant-environment interactions, stress application, and environmental adaptation
    /// </summary>
    public class PlantEnvironmentalService
    {
        [Header("Environmental Configuration")]
        [SerializeField] private bool _enableEnvironmentalStress = true;
        [SerializeField] private float _stressRecoveryRate = 0.1f;
        [SerializeField] private bool _enableDetailedLogging = false;
        [SerializeField] private float _adaptationUpdateInterval = 2f; // Update every 2 seconds

        // Dependencies
        private IPlantLifecycleService _plantLifecycleService;
        private CultivationManager _cultivationManager;

        // Environmental tracking
        private float _lastAdaptationUpdate = 0f;
        private Dictionary<string, float> _plantStressLevels = new Dictionary<string, float>();
        private int _stressEventsApplied = 0;
        private float _totalStressRecovered = 0f;

        public bool IsInitialized { get; private set; }

        public bool EnableEnvironmentalStress
        {
            get => _enableEnvironmentalStress;
            set => _enableEnvironmentalStress = value;
        }

        public float StressRecoveryRate
        {
            get => _stressRecoveryRate;
            set => _stressRecoveryRate = Mathf.Clamp(value, 0f, 1f);
        }

        public PlantEnvironmentalService() : this(null, null)
        {
        }

        public PlantEnvironmentalService(IPlantLifecycleService plantLifecycleService, CultivationManager cultivationManager)
        {
            _plantLifecycleService = plantLifecycleService;
            _cultivationManager = cultivationManager;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[PlantEnvironmentalService] Initializing environmental management system...");

            // Get dependencies if not provided
            if (_cultivationManager == null)
            {
                _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            }

            if (_cultivationManager == null)
            {
                ChimeraLogger.LogError("[PlantEnvironmentalService] CultivationManager not found - environmental services will be limited");
                return;
            }

            _lastAdaptationUpdate = Time.time;

            IsInitialized = true;
            ChimeraLogger.Log($"[PlantEnvironmentalService] Environmental management initialized (Stress: {_enableEnvironmentalStress}, Recovery: {_stressRecoveryRate:F2})");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("[PlantEnvironmentalService] Shutting down environmental management system...");

            // Log final environmental statistics
            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalService] Final stats - Stress events: {_stressEventsApplied}, Stress recovered: {_totalStressRecovered:F1}");
            }

            _plantStressLevels.Clear();

            IsInitialized = false;
        }

        /// <summary>
        /// Updates environmental adaptation for all plants based on current conditions.
        /// </summary>
        public void UpdateEnvironmentalAdaptation(EnvironmentalConditions conditions)
        {
            if (!IsInitialized) return;

            // Check if update is needed
            if (Time.time - _lastAdaptationUpdate < _adaptationUpdateInterval)
                return;

            var trackedPlants = _plantLifecycleService?.GetTrackedPlants();
            if (trackedPlants == null) return;

            var startTime = System.DateTime.Now;
            int plantsUpdated = 0;

            foreach (var plant in trackedPlants)
            {
                if (plant != null && plant.IsActive)
                {
                    try
                    {
                        // Update plant's environmental adaptation
                        plant.UpdateEnvironmentalAdaptation(conditions);

                        // Process stress recovery if enabled
                        if (_enableEnvironmentalStress)
                        {
                            ProcessStressRecovery(plant);
                        }

                        plantsUpdated++;
                    }
                    catch (System.Exception ex)
                    {
                        ChimeraLogger.LogError($"[PlantEnvironmentalService] Error updating adaptation for plant {plant.PlantID}: {ex.Message}");
                    }
                }
            }

            _lastAdaptationUpdate = Time.time;

            var updateTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalService] Updated environmental adaptation for {plantsUpdated} plants (Time: {updateTime:F1}ms)");
            }
        }

        /// <summary>
        /// Updates environmental adaptation for a specific plant.
        /// </summary>
        public void UpdateEnvironmentalAdaptation(string plantID, EnvironmentalConditions conditions)
        {
            if (!IsInitialized) return;

            var plant = _plantLifecycleService?.GetTrackedPlant(plantID);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantEnvironmentalService] Plant {plantID} not found for environmental adaptation update");
                return;
            }

            try
            {
                plant.UpdateEnvironmentalAdaptation(conditions);

                // Process stress recovery if enabled
                if (_enableEnvironmentalStress)
                {
                    ProcessStressRecovery(plant);
                }

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantEnvironmentalService] Updated environmental adaptation for plant {plantID}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantEnvironmentalService] Error updating adaptation for plant {plantID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies environmental stress to all plants.
        /// </summary>
        public void ApplyEnvironmentalStress(EnvironmentalStressSO stressSource, float intensity)
        {
            if (!IsInitialized || !_enableEnvironmentalStress || stressSource == null) return;

            var trackedPlants = _plantLifecycleService?.GetTrackedPlants();
            if (trackedPlants == null) return;

            var startTime = System.DateTime.Now;
            int affectedPlants = 0;

            foreach (var plant in trackedPlants)
            {
                if (plant != null && plant.IsActive)
                {
                    try
                    {
                        if (plant.ApplyStress(stressSource, intensity))
                        {
                            affectedPlants++;

                            // Track stress level for recovery processing
                            _plantStressLevels[plant.PlantID] = plant.StressLevel;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ChimeraLogger.LogError($"[PlantEnvironmentalService] Error applying stress to plant {plant.PlantID}: {ex.Message}");
                    }
                }
            }

            _stressEventsApplied++;

            var stressTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            ChimeraLogger.Log($"[PlantEnvironmentalService] Applied stress '{stressSource.StressName}' (intensity: {intensity:F2}) to {affectedPlants} plants (Time: {stressTime:F1}ms)");
        }

        /// <summary>
        /// Updates environmental conditions for all plants - delegates to CultivationManager.
        /// </summary>
        public void UpdateEnvironmentalConditions(EnvironmentalConditions newConditions)
        {
            if (!IsInitialized) return;

            if (_cultivationManager != null)
            {
                try
                {
                    // CultivationManager expects ProjectChimera.Data.Cultivation.EnvironmentalConditions
                    _cultivationManager.SetZoneEnvironment("default", newConditions);

                    // Update adaptation for all plants
                    UpdateEnvironmentalAdaptation(newConditions);

                    ChimeraLogger.Log($"[PlantEnvironmentalService] Updated environmental conditions via CultivationManager");
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError($"[PlantEnvironmentalService] Error updating environmental conditions: {ex.Message}");
                }
            }
            else
            {
                ChimeraLogger.LogError("[PlantEnvironmentalService] Cannot update environmental conditions: CultivationManager is null");
            }
        }

        /// <summary>
        /// Applies targeted environmental stress to a specific plant.
        /// </summary>
        public void ApplyStressToPlant(string plantID, EnvironmentalStressSO stressSource, float intensity)
        {
            if (!IsInitialized || !_enableEnvironmentalStress || stressSource == null) return;

            var plant = _plantLifecycleService?.GetTrackedPlant(plantID);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantEnvironmentalService] Plant {plantID} not found for stress application");
                return;
            }

            try
            {
                if (plant.ApplyStress(stressSource, intensity))
                {
                    // Track stress level for recovery processing
                    _plantStressLevels[plant.PlantID] = plant.StressLevel;

                    ChimeraLogger.Log($"[PlantEnvironmentalService] Applied stress '{stressSource.StressName}' (intensity: {intensity:F2}) to plant {plantID}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantEnvironmentalService] Error applying stress to plant {plantID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes stress recovery for all plants.
        /// </summary>
        public void ProcessStressRecoveryForAllPlants()
        {
            if (!IsInitialized || !_enableEnvironmentalStress) return;

            var trackedPlants = _plantLifecycleService?.GetTrackedPlants();
            if (trackedPlants == null) return;

            var startTime = System.DateTime.Now;
            int plantsRecovered = 0;

            foreach (var plant in trackedPlants)
            {
                if (plant != null && plant.IsActive)
                {
                    if (ProcessStressRecovery(plant))
                    {
                        plantsRecovered++;
                    }
                }
            }

            var recoveryTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            if (_enableDetailedLogging && plantsRecovered > 0)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalService] Processed stress recovery for {plantsRecovered} plants (Time: {recoveryTime:F1}ms)");
            }
        }

        /// <summary>
        /// Gets environmental service statistics.
        /// </summary>
        public EnvironmentalServiceStats GetEnvironmentalStats()
        {
            if (!IsInitialized)
                return new EnvironmentalServiceStats();

            var trackedPlants = _plantLifecycleService?.GetTrackedPlants();
            int trackedPlantsCount = trackedPlants?.Count() ?? 0;

            float averageStressLevel = 0f;
            int highStressPlants = 0;

            if (trackedPlants != null)
            {
                var stressLevels = new List<float>();
                foreach (var plant in trackedPlants)
                {
                    if (plant != null && plant.IsActive)
                    {
                        float stress = plant.StressLevel;
                        stressLevels.Add(stress);

                        if (stress > 0.7f)
                            highStressPlants++;
                    }
                }

                averageStressLevel = stressLevels.Count > 0 ? stressLevels.Average() : 0f;
            }

            return new EnvironmentalServiceStats
            {
                TrackedPlants = trackedPlantsCount,
                AverageStressLevel = averageStressLevel,
                HighStressPlants = highStressPlants,
                StressEventsApplied = _stressEventsApplied,
                TotalStressRecovered = _totalStressRecovered,
                StressRecoveryRate = _stressRecoveryRate,
                EnvironmentalStressEnabled = _enableEnvironmentalStress,
                LastAdaptationUpdate = _lastAdaptationUpdate
            };
        }

        /// <summary>
        /// Resets environmental tracking data.
        /// </summary>
        public void ResetEnvironmentalData()
        {
            if (!IsInitialized) return;

            _plantStressLevels.Clear();
            _stressEventsApplied = 0;
            _totalStressRecovered = 0f;
            _lastAdaptationUpdate = Time.time;

            ChimeraLogger.Log("[PlantEnvironmentalService] Environmental tracking data reset");
        }

        /// <summary>
        /// Processes stress recovery for a specific plant.
        /// </summary>
        private bool ProcessStressRecovery(PlantInstance plant)
        {
            if (plant == null || !plant.IsActive) return false;

            string plantID = plant.PlantID;
            float currentStress = plant.StressLevel;

            // Check if plant has stress to recover from
            if (currentStress <= 0.1f) return false;

            // Calculate recovery amount based on recovery rate and time
            float recoveryAmount = _stressRecoveryRate * Time.deltaTime;

            // Apply recovery (reduce stress)
            float newStressLevel = Mathf.Max(0f, currentStress - recoveryAmount);

            // Update plant stress level (this would need to be implemented in PlantInstance)
            // For now, just track the recovery
            if (newStressLevel < currentStress)
            {
                _totalStressRecovered += (currentStress - newStressLevel);
                _plantStressLevels[plantID] = newStressLevel;

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantEnvironmentalService] Plant {plantID} stress recovery: {currentStress:F2} -> {newStressLevel:F2}");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Update environmental response for a plant under specific conditions
        /// </summary>
        public EnvironmentalResponseData UpdateEnvironmentalResponse(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (!IsInitialized || plant == null)
            {
                return new EnvironmentalResponseData
                {
                    PlantID = plant?.PlantID ?? "Unknown",
                    ResponseLevel = 0f,
                    AdaptationProgress = 0f,
                    StressLevel = 0f
                };
            }

            var startTime = System.DateTime.Now;

            // Calculate environmental response factors
            float temperatureResponse = CalculateTemperatureResponse(conditions.Temperature, plant);
            float humidityResponse = CalculateHumidityResponse(conditions.Humidity, plant);
            float lightResponse = CalculateLightResponse(conditions.LightIntensity, plant);
            float co2Response = CalculateCO2Response(conditions.CO2Level, plant);

            // Calculate overall environmental response
            float overallResponse = (temperatureResponse * 0.3f + humidityResponse * 0.25f +
                                   lightResponse * 0.3f + co2Response * 0.15f);

            // Calculate adaptation progress (simplified)
            float adaptationProgress = CalculateAdaptationProgress(plant, conditions);

            // Update plant's environmental conditions
            UpdatePlantConditions(plant, conditions);

            var processingTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            return new EnvironmentalResponseData
            {
                PlantID = plant.PlantID,
                ResponseLevel = Mathf.Clamp01(overallResponse),
                AdaptationProgress = adaptationProgress,
                StressLevel = plant.StressLevel,
                TemperatureResponse = temperatureResponse,
                HumidityResponse = humidityResponse,
                LightResponse = lightResponse,
                CO2Response = co2Response,
                ProcessingTimeMs = (float)processingTime
            };
        }

        private float CalculateTemperatureResponse(float temperature, PlantInstance plant)
        {
            // Optimal range for cannabis: 20-30°C
            float optimal = 25f;
            float deviation = Mathf.Abs(temperature - optimal);
            return Mathf.Clamp01(1f - (deviation / 15f));
        }

        private float CalculateHumidityResponse(float humidity, PlantInstance plant)
        {
            // Optimal range for cannabis: 40-70%
            float optimal = 55f;
            float deviation = Mathf.Abs(humidity - optimal);
            return Mathf.Clamp01(1f - (deviation / 25f));
        }

        private float CalculateLightResponse(float lightIntensity, PlantInstance plant)
        {
            // Optimal range for cannabis: 600-1000 PPFD
            float optimal = 800f;
            float deviation = Mathf.Abs(lightIntensity - optimal);
            return Mathf.Clamp01(1f - (deviation / 400f));
        }

        private float CalculateCO2Response(float co2Level, PlantInstance plant)
        {
            // Optimal range for cannabis: 800-1200 ppm
            float optimal = 1000f;
            float deviation = Mathf.Abs(co2Level - optimal);
            return Mathf.Clamp01(1f - (deviation / 400f));
        }

        private float CalculateAdaptationProgress(PlantInstance plant, EnvironmentalConditions conditions)
        {
            // Simplified adaptation calculation based on exposure time
            float daysAlive = plant.DaysSincePlanted;
            float adaptationRate = 0.1f; // 10% adaptation per day
            return Mathf.Clamp01(daysAlive * adaptationRate);
        }

        private void UpdatePlantConditions(PlantInstance plant, EnvironmentalConditions conditions)
        {
            // Update the plant's environmental conditions
            plant.UpdateEnvironmentalConditions(conditions);
        }

        /// <summary>
        /// Converts cultivation environmental conditions to environment environmental conditions.
        /// </summary>
    }

    /// <summary>
    /// Environmental response data structure
    /// </summary>
    [System.Serializable]
    public class EnvironmentalResponseData
    {
        public string PlantID;
        public float ResponseLevel;
        public float AdaptationProgress;
        public float StressLevel;
        public float TemperatureResponse;
        public float HumidityResponse;
        public float LightResponse;
        public float CO2Response;
        public float ProcessingTimeMs;
    }

    /// <summary>
    /// Environmental service statistics structure.
    /// </summary>
    [System.Serializable]
    public class EnvironmentalServiceStats
    {
        public int TrackedPlants;
        public float AverageStressLevel;
        public int HighStressPlants;
        public int StressEventsApplied;
        public float TotalStressRecovered;
        public float StressRecoveryRate;
        public bool EnvironmentalStressEnabled;
        public float LastAdaptationUpdate;

        public override string ToString()
        {
            return $"Environmental Stats - Plants: {TrackedPlants}, Avg Stress: {AverageStressLevel:F2}, " +
                   $"High Stress: {HighStressPlants}, Events: {StressEventsApplied}, " +
                   $"Recovered: {TotalStressRecovered:F1}, Rate: {StressRecoveryRate:F2}";
        }
    }
}
