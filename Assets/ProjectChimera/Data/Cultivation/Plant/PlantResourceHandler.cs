using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Resource Handler
    /// Single Responsibility: Plant resource management including water, nutrients, and energy
    /// Extracted from PlantInstanceSO for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantResourceHandler
    {
        [Header("Resource Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableAutoDecay = true;
        [SerializeField] private bool _validateResourceLimits = true;
        [SerializeField] private float _resourceDecayRate = 0.02f; // Per day

        // Resource levels
        [SerializeField, Range(0f, 1f)] private float _waterLevel = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _nutrientLevel = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _energyReserves = 0.6f;

        // Resource thresholds
        [SerializeField, Range(0f, 1f)] private float _criticalWaterThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _criticalNutrientThreshold = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _criticalEnergyThreshold = 0.15f;

        // Resource history tracking
        [SerializeField] private DateTime _lastWatering = DateTime.Now;
        [SerializeField] private DateTime _lastFeeding = DateTime.Now;
        [SerializeField] private DateTime _lastTraining = DateTime.MinValue;

        // Resource consumption rates
        [SerializeField] private float _waterConsumptionRate = 0.05f; // Per day
        [SerializeField] private float _nutrientConsumptionRate = 0.03f; // Per day
        [SerializeField] private float _energyConsumptionRate = 0.04f; // Per day

        // Nutrient breakdown
        private Dictionary<string, float> _nutrients = new Dictionary<string, float>
        {
            { "Nitrogen", 0.7f },
            { "Phosphorus", 0.7f },
            { "Potassium", 0.7f },
            { "Calcium", 0.6f },
            { "Magnesium", 0.6f },
            { "Iron", 0.5f }
        };

        // Resource schedules
        private WateringSchedule _optimalWateringSchedule = new WateringSchedule();
        private FeedingSchedule _optimalFeedingSchedule = new FeedingSchedule();

        // Statistics
        private PlantResourceStats _stats = new PlantResourceStats();

        // State tracking
        private bool _isInitialized = false;
        private DateTime _lastResourceUpdate = DateTime.Now;

        // Events
        public event System.Action<float, float> OnWaterLevelChanged; // old level, new level
        public event System.Action<float, float> OnNutrientLevelChanged; // old level, new level
        public event System.Action<float, float> OnEnergyLevelChanged; // old level, new level
        public event System.Action<string> OnCriticalResourceLevel; // resource type
        public event System.Action<WateringResult> OnWateringCompleted;
        public event System.Action<FeedingResult> OnFeedingCompleted;
        public event System.Action OnTrainingApplied;

        public bool IsInitialized => _isInitialized;
        public PlantResourceStats Stats => _stats;
        public float WaterLevel => _waterLevel;
        public float NutrientLevel => _nutrientLevel;
        public float EnergyReserves => _energyReserves;
        public DateTime LastWatering => _lastWatering;
        public DateTime LastFeeding => _lastFeeding;
        public DateTime LastTraining => _lastTraining;
        public bool HasCriticalWaterLevel => _waterLevel <= _criticalWaterThreshold;
        public bool HasCriticalNutrientLevel => _nutrientLevel <= _criticalNutrientThreshold;
        public bool HasCriticalEnergyLevel => _energyReserves <= _criticalEnergyThreshold;

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializeNutrients();
            InitializeSchedules();
            ResetStats();
            _lastResourceUpdate = DateTime.Now;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant Resource Handler initialized - Water: {_waterLevel:F2}, Nutrients: {_nutrientLevel:F2}, Energy: {_energyReserves:F2}");
            }
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public WateringResult Water(float waterAmount)
        {
            if (!_isInitialized) Initialize();

            var oldLevel = _waterLevel;
            var effectiveAmount = Mathf.Max(0f, waterAmount);

            if (_validateResourceLimits)
            {
                _waterLevel = Mathf.Clamp01(_waterLevel + effectiveAmount);
            }
            else
            {
                _waterLevel += effectiveAmount;
            }

            var result = new WateringResult
            {
                WaterAmount = effectiveAmount,
                PreviousLevel = oldLevel,
                NewLevel = _waterLevel,
                Success = effectiveAmount > 0f,
                Timestamp = DateTime.Now,
                Message = effectiveAmount > 0f ? "Watering successful" : "Invalid water amount"
            };

            if (result.Success)
            {
                _lastWatering = DateTime.Now;
                _stats.WateringEvents++;
                _stats.TotalWaterApplied += effectiveAmount;

                OnWaterLevelChanged?.Invoke(oldLevel, _waterLevel);
                OnWateringCompleted?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Plant watered: +{effectiveAmount:F3} -> {_waterLevel:F2}");
                }
            }
            else if (_enableLogging)
            {
                ChimeraLogger.LogWarning("PLANT", $"Watering failed: Invalid amount {waterAmount}");
            }

            return result;
        }

        /// <summary>
        /// Feed the plant with nutrients
        /// </summary>
        public FeedingResult Feed(Dictionary<string, float> nutrients)
        {
            if (!_isInitialized) Initialize();

            var oldLevel = _nutrientLevel;
            var totalNutrientValue = 0f;
            var appliedNutrients = new Dictionary<string, float>();

            foreach (var nutrient in nutrients)
            {
                if (nutrient.Value > 0f && _nutrients.ContainsKey(nutrient.Key))
                {
                    var oldNutrientLevel = _nutrients[nutrient.Key];

                    if (_validateResourceLimits)
                    {
                        _nutrients[nutrient.Key] = Mathf.Clamp01(oldNutrientLevel + nutrient.Value);
                    }
                    else
                    {
                        _nutrients[nutrient.Key] += nutrient.Value;
                    }

                    appliedNutrients[nutrient.Key] = _nutrients[nutrient.Key] - oldNutrientLevel;
                    totalNutrientValue += appliedNutrients[nutrient.Key];
                }
            }

            // Update overall nutrient level (average of all nutrients)
            _nutrientLevel = _nutrients.Values.Sum() / _nutrients.Count;

            var result = new FeedingResult
            {
                AppliedNutrients = appliedNutrients,
                TotalNutrientValue = totalNutrientValue,
                PreviousLevel = oldLevel,
                NewLevel = _nutrientLevel,
                Success = totalNutrientValue > 0f,
                Timestamp = DateTime.Now,
                Message = totalNutrientValue > 0f ? "Feeding successful" : "No valid nutrients provided"
            };

            if (result.Success)
            {
                _lastFeeding = DateTime.Now;
                _stats.FeedingEvents++;
                _stats.TotalNutrientsApplied += totalNutrientValue;

                OnNutrientLevelChanged?.Invoke(oldLevel, _nutrientLevel);
                OnFeedingCompleted?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Plant fed: +{totalNutrientValue:F3} -> {_nutrientLevel:F2}");
                }
            }
            else if (_enableLogging)
            {
                ChimeraLogger.LogWarning("PLANT", "Feeding failed: No valid nutrients provided");
            }

            return result;
        }

        /// <summary>
        /// Apply training to the plant
        /// </summary>
        public TrainingResult ApplyTraining()
        {
            if (!_isInitialized) Initialize();

            // Training typically requires energy
            var energyCost = 0.05f;
            var oldEnergy = _energyReserves;

            if (_energyReserves >= energyCost)
            {
                _energyReserves -= energyCost;
                _lastTraining = DateTime.Now;
                _stats.TrainingEvents++;

                var result = new TrainingResult
                {
                    TrainingType = "General Training",
                    Success = true,
                    StressIncrease = 0.02f, // Small stress increase from training
                    TrainingDate = DateTime.Now,
                    EnergyCost = energyCost
                };

                OnEnergyLevelChanged?.Invoke(oldEnergy, _energyReserves);
                OnTrainingApplied?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Training applied: Energy cost {energyCost:F3}, remaining {_energyReserves:F2}");
                }

                return result;
            }
            else
            {
                var result = new TrainingResult
                {
                    TrainingType = "General Training",
                    Success = false,
                    StressIncrease = 0f,
                    TrainingDate = DateTime.Now,
                    EnergyCost = 0f
                };

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", $"Training failed: Insufficient energy ({_energyReserves:F2} < {energyCost:F2})");
                }

                return result;
            }
        }

        /// <summary>
        /// Update resources (decay over time)
        /// </summary>
        public void UpdateResources(float deltaTimeDays = 1f)
        {
            if (!_isInitialized) Initialize();

            if (!_enableAutoDecay) return;

            var oldWater = _waterLevel;
            var oldNutrients = _nutrientLevel;
            var oldEnergy = _energyReserves;

            // Apply consumption and decay
            _waterLevel = Mathf.Max(0f, _waterLevel - (_waterConsumptionRate * deltaTimeDays));
            _energyReserves = Mathf.Max(0f, _energyReserves - (_energyConsumptionRate * deltaTimeDays));

            // Decay individual nutrients
            var nutrientKeys = new List<string>(_nutrients.Keys);
            foreach (var key in nutrientKeys)
            {
                _nutrients[key] = Mathf.Max(0f, _nutrients[key] - (_nutrientConsumptionRate * deltaTimeDays));
            }

            // Recalculate overall nutrient level
            _nutrientLevel = _nutrients.Values.Sum() / _nutrients.Count;

            _stats.ResourceUpdates++;
            _lastResourceUpdate = DateTime.Now;

            // Check for critical levels
            CheckCriticalLevels();

            // Fire events for significant changes
            if (Mathf.Abs(_waterLevel - oldWater) > 0.01f)
            {
                OnWaterLevelChanged?.Invoke(oldWater, _waterLevel);
            }

            if (Mathf.Abs(_nutrientLevel - oldNutrients) > 0.01f)
            {
                OnNutrientLevelChanged?.Invoke(oldNutrients, _nutrientLevel);
            }

            if (Mathf.Abs(_energyReserves - oldEnergy) > 0.01f)
            {
                OnEnergyLevelChanged?.Invoke(oldEnergy, _energyReserves);
            }

            if (_enableLogging && _stats.ResourceUpdates % 10 == 0) // Log every 10 updates
            {
                ChimeraLogger.Log("PLANT", $"Resources updated: W:{_waterLevel:F2}, N:{_nutrientLevel:F2}, E:{_energyReserves:F2}");
            }
        }

        /// <summary>
        /// Get resource status summary
        /// </summary>
        public float GetResourceStatus()
        {
            if (!_isInitialized) Initialize();

            // Weighted average of all resources
            var weightedAverage = (_waterLevel * 0.4f) + (_nutrientLevel * 0.4f) + (_energyReserves * 0.2f);
            return weightedAverage;
        }

        /// <summary>
        /// Get optimal watering schedule
        /// </summary>
        public float GetOptimalWateringSchedule()
        {
            if (!_isInitialized) Initialize();

            // Calculate days since last watering
            var daysSinceWatering = (float)(DateTime.Now - _lastWatering).TotalDays;

            // Base schedule frequency (every 2-3 days)
            var baseFrequency = 2.5f;

            // Adjust based on current water level
            var waterLevelFactor = 1f - _waterLevel;

            // Calculate optimal next watering time
            var optimalInterval = baseFrequency * (1f - (waterLevelFactor * 0.5f));

            return Mathf.Max(0f, optimalInterval - daysSinceWatering);
        }

        /// <summary>
        /// Get optimal feeding schedule
        /// </summary>
        public float GetOptimalFeedingSchedule()
        {
            if (!_isInitialized) Initialize();

            // Calculate days since last feeding
            var daysSinceFeeding = (float)(DateTime.Now - _lastFeeding).TotalDays;

            // Base schedule frequency (every 7-10 days)
            var baseFrequency = 8f;

            // Adjust based on current nutrient level
            var nutrientLevelFactor = 1f - _nutrientLevel;

            // Calculate optimal next feeding time
            var optimalInterval = baseFrequency * (1f - (nutrientLevelFactor * 0.3f));

            return Mathf.Max(0f, optimalInterval - daysSinceFeeding);
        }

        /// <summary>
        /// Get detailed nutrient breakdown
        /// </summary>
        public Dictionary<string, float> GetNutrientBreakdown()
        {
            return new Dictionary<string, float>(_nutrients);
        }

        /// <summary>
        /// Set energy level directly (for external systems)
        /// </summary>
        public void SetEnergyLevel(float newLevel)
        {
            if (!_isInitialized) Initialize();

            var oldLevel = _energyReserves;
            _energyReserves = _validateResourceLimits ? Mathf.Clamp01(newLevel) : newLevel;

            OnEnergyLevelChanged?.Invoke(oldLevel, _energyReserves);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Energy level set: {oldLevel:F2} -> {_energyReserves:F2}");
            }
        }

        /// <summary>
        /// Get resource summary
        /// </summary>
        public PlantResourceSummary GetResourceSummary()
        {
            return new PlantResourceSummary
            {
                WaterLevel = _waterLevel,
                NutrientLevel = _nutrientLevel,
                EnergyReserves = _energyReserves,
                OverallResourceStatus = GetResourceStatus(),
                HasCriticalWater = HasCriticalWaterLevel,
                HasCriticalNutrients = HasCriticalNutrientLevel,
                HasCriticalEnergy = HasCriticalEnergyLevel,
                LastWatering = _lastWatering,
                LastFeeding = _lastFeeding,
                LastTraining = _lastTraining,
                NutrientBreakdown = GetNutrientBreakdown(),
                NextWateringRecommendation = GetOptimalWateringSchedule(),
                NextFeedingRecommendation = GetOptimalFeedingSchedule()
            };
        }

        /// <summary>
        /// Check for critical resource levels
        /// </summary>
        private void CheckCriticalLevels()
        {
            if (HasCriticalWaterLevel)
            {
                OnCriticalResourceLevel?.Invoke("Water");
            }

            if (HasCriticalNutrientLevel)
            {
                OnCriticalResourceLevel?.Invoke("Nutrients");
            }

            if (HasCriticalEnergyLevel)
            {
                OnCriticalResourceLevel?.Invoke("Energy");
            }
        }

        /// <summary>
        /// Initialize nutrients with default values
        /// </summary>
        private void InitializeNutrients()
        {
            if (_nutrients == null || _nutrients.Count == 0)
            {
                _nutrients = new Dictionary<string, float>
                {
                    { "Nitrogen", 0.7f },
                    { "Phosphorus", 0.7f },
                    { "Potassium", 0.7f },
                    { "Calcium", 0.6f },
                    { "Magnesium", 0.6f },
                    { "Iron", 0.5f }
                };
            }
        }

        /// <summary>
        /// Initialize optimal schedules
        /// </summary>
        private void InitializeSchedules()
        {
            _optimalWateringSchedule = new WateringSchedule
            {
                FrequencyInDays = 2.5f,
                AmountPerWatering = 0.3f,
                TimeOfDay = "Morning"
            };

            _optimalFeedingSchedule = new FeedingSchedule
            {
                FrequencyInDays = 8f,
                NutrientMix = new Dictionary<string, float>
                {
                    { "Nitrogen", 0.2f },
                    { "Phosphorus", 0.15f },
                    { "Potassium", 0.2f }
                }
            };
        }

        /// <summary>
        /// Reset resource statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new PlantResourceStats();
        }

        /// <summary>
        /// Set resource parameters
        /// </summary>
        public void SetResourceParameters(bool enableAutoDecay, bool validateLimits, float decayRate)
        {
            _enableAutoDecay = enableAutoDecay;
            _validateResourceLimits = validateLimits;
            _resourceDecayRate = Mathf.Max(0f, decayRate);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Resource parameters updated: Decay={enableAutoDecay}, Validate={validateLimits}, Rate={decayRate:F3}");
            }
        }

        /// <summary>
        /// Force resource refresh and validation
        /// </summary>
        [ContextMenu("Force Resource Refresh")]
        public void ForceResourceRefresh()
        {
            if (_isInitialized)
            {
                CheckCriticalLevels();
                _lastResourceUpdate = DateTime.Now;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant resources manually refreshed");
                }
            }
        }
    }

    /// <summary>
    /// Plant resource statistics
    /// </summary>
    [System.Serializable]
    public struct PlantResourceStats
    {
        public int WateringEvents;
        public int FeedingEvents;
        public int TrainingEvents;
        public int ResourceUpdates;
        public float TotalWaterApplied;
        public float TotalNutrientsApplied;
    }

    /// <summary>
    /// Plant resource summary
    /// </summary>
    [System.Serializable]
    public struct PlantResourceSummary
    {
        public float WaterLevel;
        public float NutrientLevel;
        public float EnergyReserves;
        public float OverallResourceStatus;
        public bool HasCriticalWater;
        public bool HasCriticalNutrients;
        public bool HasCriticalEnergy;
        public DateTime LastWatering;
        public DateTime LastFeeding;
        public DateTime LastTraining;
        public Dictionary<string, float> NutrientBreakdown;
        public float NextWateringRecommendation;
        public float NextFeedingRecommendation;
    }

    /// <summary>
    /// Watering schedule
    /// </summary>
    [System.Serializable]
    public struct WateringSchedule
    {
        public float FrequencyInDays;
        public float AmountPerWatering;
        public string TimeOfDay;
    }

    /// <summary>
    /// Feeding schedule
    /// </summary>
    [System.Serializable]
    public struct FeedingSchedule
    {
        public float FrequencyInDays;
        public Dictionary<string, float> NutrientMix;
    }

    /// <summary>
    /// Enhanced watering result
    /// </summary>
    [System.Serializable]
    public struct WateringResult
    {
        public float WaterAmount;
        public float PreviousLevel;
        public float NewLevel;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Enhanced feeding result
    /// </summary>
    [System.Serializable]
    public struct FeedingResult
    {
        public Dictionary<string, float> AppliedNutrients;
        public float TotalNutrientValue;
        public float PreviousLevel;
        public float NewLevel;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Enhanced training result
    /// </summary>
    [System.Serializable]
    public struct TrainingOutcome
    {
        public string TrainingType;
        public bool Success;
        public float StressIncrease;
        public DateTime TrainingDate;
        public float EnergyCost;
    }
}

// Extension methods for easier calculation
public static class DictionaryExtensions
{
    public static float Sum(this IEnumerable<float> values)
    {
        float sum = 0f;
        foreach (var value in values)
        {
            sum += value;
        }
        return sum;
    }
}
