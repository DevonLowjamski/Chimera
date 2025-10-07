using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Resource Handler - Coordinator
    /// Single Responsibility: Coordinates plant resource management
    /// Uses: PlantResourceDataStructures.cs for data types
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

        // Statistics
        private PlantResourceStats _stats = new PlantResourceStats();

        // State tracking
        private bool _isInitialized = false;
        private DateTime _lastResourceUpdate = DateTime.Now;

        // Events
        public event System.Action<float, float> OnWaterLevelChanged;
        public event System.Action<float, float> OnNutrientLevelChanged;
        public event System.Action<float, float> OnEnergyLevelChanged;
        public event System.Action<string> OnCriticalResourceLevel;
        public event System.Action<WateringResult> OnWateringCompleted;
        public event System.Action<FeedingResult> OnFeedingCompleted;
        public event System.Action OnTrainingApplied;

        // Properties
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
            _stats = new PlantResourceStats();
            _lastResourceUpdate = DateTime.Now;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Resource Handler initialized - W:{_waterLevel:F2}, N:{_nutrientLevel:F2}, E:{_energyReserves:F2}");
            }
        }

        public WateringResult Water(float waterAmount)
        {
            if (!_isInitialized) Initialize();

            var oldLevel = _waterLevel;
            var effectiveAmount = Mathf.Max(0f, waterAmount);

            _waterLevel = _validateResourceLimits ? 
                Mathf.Clamp01(_waterLevel + effectiveAmount) : 
                _waterLevel + effectiveAmount;

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
                    ChimeraLogger.Log("PLANT", $"Watered: +{effectiveAmount:F3} → {_waterLevel:F2}");
                }
            }

            return result;
        }

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
                    _nutrients[nutrient.Key] = _validateResourceLimits ? 
                        Mathf.Clamp01(oldNutrientLevel + nutrient.Value) : 
                        oldNutrientLevel + nutrient.Value;

                    appliedNutrients[nutrient.Key] = _nutrients[nutrient.Key] - oldNutrientLevel;
                    totalNutrientValue += appliedNutrients[nutrient.Key];
                }
            }

            _nutrientLevel = _nutrients.Values.Sum() / _nutrients.Count;

            var result = new FeedingResult
            {
                AppliedNutrients = appliedNutrients,
                TotalNutrientValue = totalNutrientValue,
                PreviousLevel = oldLevel,
                NewLevel = _nutrientLevel,
                Success = totalNutrientValue > 0f,
                Timestamp = DateTime.Now,
                Message = totalNutrientValue > 0f ? "Feeding successful" : "No valid nutrients"
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
                    ChimeraLogger.Log("PLANT", $"Fed: +{totalNutrientValue:F3} → {_nutrientLevel:F2}");
                }
            }

            return result;
        }

        public TrainingResult ApplyTraining()
        {
            if (!_isInitialized) Initialize();

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
                    StressIncrease = 0.02f,
                    TrainingDate = DateTime.Now,
                    EnergyCost = energyCost
                };

                OnEnergyLevelChanged?.Invoke(oldEnergy, _energyReserves);
                OnTrainingApplied?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Training applied: -{energyCost:F3} → {_energyReserves:F2}");
                }

                return result;
            }

            return new TrainingResult { Success = false, TrainingDate = DateTime.Now };
        }

        public void UpdateResources(float deltaTimeDays = 1f)
        {
            if (!_isInitialized || !_enableAutoDecay) return;

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

            _nutrientLevel = _nutrients.Values.Sum() / _nutrients.Count;
            _stats.ResourceUpdates++;
            _lastResourceUpdate = DateTime.Now;

            CheckCriticalLevels();

            // Fire events for significant changes
            if (Mathf.Abs(_waterLevel - oldWater) > 0.01f)
                OnWaterLevelChanged?.Invoke(oldWater, _waterLevel);
            if (Mathf.Abs(_nutrientLevel - oldNutrients) > 0.01f)
                OnNutrientLevelChanged?.Invoke(oldNutrients, _nutrientLevel);
            if (Mathf.Abs(_energyReserves - oldEnergy) > 0.01f)
                OnEnergyLevelChanged?.Invoke(oldEnergy, _energyReserves);

            if (_enableLogging && _stats.ResourceUpdates % 10 == 0)
            {
                ChimeraLogger.Log("PLANT", $"Resources: W:{_waterLevel:F2}, N:{_nutrientLevel:F2}, E:{_energyReserves:F2}");
            }
        }

        public float GetResourceStatus()
        {
            if (!_isInitialized) Initialize();
            return (_waterLevel * 0.4f) + (_nutrientLevel * 0.4f) + (_energyReserves * 0.2f);
        }

        public float GetOptimalWateringSchedule()
        {
            if (!_isInitialized) Initialize();
            var daysSinceWatering = (float)(DateTime.Now - _lastWatering).TotalDays;
            var baseFrequency = 2.5f;
            var waterLevelFactor = 1f - _waterLevel;
            var optimalInterval = baseFrequency * (1f - (waterLevelFactor * 0.5f));
            return Mathf.Max(0f, optimalInterval - daysSinceWatering);
        }

        public float GetOptimalFeedingSchedule()
        {
            if (!_isInitialized) Initialize();
            var daysSinceFeeding = (float)(DateTime.Now - _lastFeeding).TotalDays;
            var baseFrequency = 8f;
            var nutrientLevelFactor = 1f - _nutrientLevel;
            var optimalInterval = baseFrequency * (1f - (nutrientLevelFactor * 0.3f));
            return Mathf.Max(0f, optimalInterval - daysSinceFeeding);
        }

        public Dictionary<string, float> GetNutrientBreakdown()
        {
            return new Dictionary<string, float>(_nutrients);
        }

        public void SetEnergyLevel(float newLevel)
        {
            if (!_isInitialized) Initialize();
            var oldLevel = _energyReserves;
            _energyReserves = _validateResourceLimits ? Mathf.Clamp01(newLevel) : newLevel;
            OnEnergyLevelChanged?.Invoke(oldLevel, _energyReserves);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Energy set: {oldLevel:F2} → {_energyReserves:F2}");
            }
        }

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

        public void SetResourceParameters(bool enableAutoDecay, bool validateLimits, float decayRate)
        {
            _enableAutoDecay = enableAutoDecay;
            _validateResourceLimits = validateLimits;
            _resourceDecayRate = Mathf.Max(0f, decayRate);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Parameters: Decay={enableAutoDecay}, Validate={validateLimits}, Rate={decayRate:F3}");
            }
        }

        [ContextMenu("Force Resource Refresh")]
        public void ForceResourceRefresh()
        {
            if (_isInitialized)
            {
                CheckCriticalLevels();
                _lastResourceUpdate = DateTime.Now;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Resources manually refreshed");
                }
            }
        }

        private void CheckCriticalLevels()
        {
            if (HasCriticalWaterLevel) OnCriticalResourceLevel?.Invoke("Water");
            if (HasCriticalNutrientLevel) OnCriticalResourceLevel?.Invoke("Nutrients");
            if (HasCriticalEnergyLevel) OnCriticalResourceLevel?.Invoke("Energy");
        }

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
    }
}

