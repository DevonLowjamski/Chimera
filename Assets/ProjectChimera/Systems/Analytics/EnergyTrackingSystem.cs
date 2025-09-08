using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// using ProjectChimera.Systems.Cultivation; // Temporarily removed - namespace reorganization
// using ProjectChimera.Systems.Environment; // Temporarily removed - namespace reorganization

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Stub implementation for energy usage tracking
    /// Provides realistic energy consumption estimates based on facility operations
    /// Will be enhanced when environmental systems are fully integrated
    /// </summary>
    public class EnergyTrackingSystem : MonoBehaviour, ITickable
    {
        [Header("Energy Configuration")]
        [SerializeField] private float _baseEnergyConsumption = 50f; // kWh base facility consumption
        [SerializeField] private bool _enableDetailedTracking = true;
        [SerializeField] private bool _enableEnergyLogging = false;

        [Header("Equipment Energy Rates (kWh per unit)")]
        [SerializeField] private float _growLightConsumption = 0.6f; // Per plant per hour
        [SerializeField] private float _hvacConsumption = 0.4f; // Per plant per hour
        [SerializeField] private float _ventilationConsumption = 0.1f; // Per plant per hour
        [SerializeField] private float _irrigationConsumption = 0.05f; // Per plant per hour
        [SerializeField] private float _monitoringConsumption = 0.02f; // Per plant per hour

        [Header("Dynamic Consumption Modifiers")]
        [SerializeField] private AnimationCurve _plantGrowthEnergyMultiplier = AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f);
        [SerializeField] private AnimationCurve _environmentalEfficiencyMultiplier = AnimationCurve.Linear(0f, 1.2f, 1f, 0.8f);

        // Energy tracking data
        private Dictionary<string, float> _equipmentEnergyUsage;
        private Dictionary<string, float> _systemEnergyUsage;
        private float _totalEnergyConsumed;
        private float _lastUpdateTime;
        private float _currentHourlyUsage;

        // System references
        private ICultivationManager _cultivationManager;
        private IEnvironmentalManager _environmentManager;

        public float TotalEnergyConsumed => _totalEnergyConsumed;
        public float CurrentHourlyUsage => _currentHourlyUsage;
        public Dictionary<string, float> EquipmentBreakdown => new Dictionary<string, float>(_equipmentEnergyUsage);
        public Dictionary<string, float> SystemBreakdown => new Dictionary<string, float>(_systemEnergyUsage);

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeEnergyTracking();
        }

        private void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
            FindSystemReferences();
        }

        public void Tick(float deltaTime)
        {
            if (_enableDetailedTracking)
            {
                UpdateEnergyConsumption();
            }
        }

        #endregion

        #region Initialization

        private void InitializeEnergyTracking()
        {
            _equipmentEnergyUsage = new Dictionary<string, float>
            {
                ["GrowLights"] = 0f,
                ["HVAC"] = 0f,
                ["Ventilation"] = 0f,
                ["Irrigation"] = 0f,
                ["Monitoring"] = 0f,
                ["Other"] = 0f
            };

            _systemEnergyUsage = new Dictionary<string, float>
            {
                ["Cultivation"] = 0f,
                ["Climate Control"] = 0f,
                ["Security"] = 0f,
                ["Base Facility"] = 0f
            };

            _totalEnergyConsumed = 0f;
            _lastUpdateTime = Time.time;
            _currentHourlyUsage = 0f;

            if (_enableEnergyLogging)
                ChimeraLogger.Log("[EnergyTrackingSystem] Energy tracking initialized");
        }

        private void FindSystemReferences()
        {
            _cultivationManager = ServiceContainerFactory.Instance?.TryResolve<ICultivationManager>();
            _environmentManager = ServiceContainerFactory.Instance?.TryResolve<IEnvironmentalManager>();

            if (_enableEnergyLogging)
            {
                ChimeraLogger.Log($"[EnergyTrackingSystem] Found references - Cultivation: {_cultivationManager != null}, Environment: {_environmentManager != null}");
            }
        }

        #endregion

        #region Energy Calculation

        private void UpdateEnergyConsumption()
        {
            var currentTime = Time.time;
            var deltaTime = (currentTime - _lastUpdateTime) / 3600f; // Convert to hours

            if (deltaTime <= 0f) return;

            var hourlyConsumption = CalculateHourlyEnergyConsumption();
            var energyConsumed = hourlyConsumption * deltaTime;

            _totalEnergyConsumed += energyConsumed;
            _currentHourlyUsage = hourlyConsumption;
            _lastUpdateTime = currentTime;

            // Update equipment and system breakdowns
            UpdateEnergyBreakdowns(deltaTime);

            if (_enableEnergyLogging && deltaTime > 0.1f) // Log every ~6 minutes
            {
                ChimeraLogger.Log($"[EnergyTrackingSystem] Hourly usage: {hourlyConsumption:F2} kWh, Total consumed: {_totalEnergyConsumed:F2} kWh");
            }
        }

        private float CalculateHourlyEnergyConsumption()
        {
            var totalConsumption = _baseEnergyConsumption;

            // Get plant count and health for dynamic calculations
            var activePlants = _cultivationManager?.GetActivePlantCount() ?? 0;
            var avgPlantHealth = 1f; // Placeholder - interface doesn't have AveragePlantHealth yet

            if (activePlants > 0)
            {
                // Calculate equipment-based consumption
                var growLightPower = CalculateGrowLightConsumption(activePlants, avgPlantHealth);
                var hvacPower = CalculateHVACConsumption(activePlants, avgPlantHealth);
                var ventilationPower = CalculateVentilationConsumption(activePlants);
                var irrigationPower = CalculateIrrigationConsumption(activePlants);
                var monitoringPower = CalculateMonitoringConsumption(activePlants);

                totalConsumption += growLightPower + hvacPower + ventilationPower + irrigationPower + monitoringPower;

                // Apply efficiency modifiers
                var efficiencyMultiplier = _environmentalEfficiencyMultiplier.Evaluate(avgPlantHealth);
                totalConsumption *= efficiencyMultiplier;
            }

            return totalConsumption;
        }

        private float CalculateGrowLightConsumption(int plantCount, float avgHealth)
        {
            // More plants and higher growth stages require more light
            var baseConsumption = plantCount * _growLightConsumption;
            var growthMultiplier = _plantGrowthEnergyMultiplier.Evaluate(avgHealth);
            return baseConsumption * growthMultiplier;
        }

        private float CalculateHVACConsumption(int plantCount, float avgHealth)
        {
            // HVAC consumption scales with plant count and environmental demands
            var baseConsumption = plantCount * _hvacConsumption;

            // Simulate seasonal variations (stub)
            var seasonalMultiplier = 1f + Mathf.Sin(Time.time * 0.1f) * 0.3f; // ±30% seasonal variation

            return baseConsumption * seasonalMultiplier;
        }

        private float CalculateVentilationConsumption(int plantCount)
        {
            // Ventilation scales linearly with plant count
            return plantCount * _ventilationConsumption;
        }

        private float CalculateIrrigationConsumption(int plantCount)
        {
            // Irrigation includes pumps, valves, and control systems
            return plantCount * _irrigationConsumption;
        }

        private float CalculateMonitoringConsumption(int plantCount)
        {
            // Sensors, cameras, and monitoring equipment
            return plantCount * _monitoringConsumption;
        }

        #endregion

        #region Energy Breakdown Tracking

        private void UpdateEnergyBreakdowns(float deltaTime)
        {
            var activePlants = _cultivationManager?.GetActivePlantCount() ?? 0;
            var avgPlantHealth = 1f; // Placeholder - interface doesn't have AveragePlantHealth yet

            if (activePlants > 0)
            {
                // Update equipment breakdown
                _equipmentEnergyUsage["GrowLights"] += CalculateGrowLightConsumption(activePlants, avgPlantHealth) * deltaTime;
                _equipmentEnergyUsage["HVAC"] += CalculateHVACConsumption(activePlants, avgPlantHealth) * deltaTime;
                _equipmentEnergyUsage["Ventilation"] += CalculateVentilationConsumption(activePlants) * deltaTime;
                _equipmentEnergyUsage["Irrigation"] += CalculateIrrigationConsumption(activePlants) * deltaTime;
                _equipmentEnergyUsage["Monitoring"] += CalculateMonitoringConsumption(activePlants) * deltaTime;
            }

            _equipmentEnergyUsage["Other"] += _baseEnergyConsumption * deltaTime;

            // Update system breakdown
            _systemEnergyUsage["Cultivation"] += (CalculateGrowLightConsumption(activePlants, avgPlantHealth) +
                                                   CalculateIrrigationConsumption(activePlants)) * deltaTime;
            _systemEnergyUsage["Climate Control"] += (CalculateHVACConsumption(activePlants, avgPlantHealth) +
                                                      CalculateVentilationConsumption(activePlants)) * deltaTime;
            _systemEnergyUsage["Security"] += CalculateMonitoringConsumption(activePlants) * deltaTime;
            _systemEnergyUsage["Base Facility"] += _baseEnergyConsumption * deltaTime;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get energy efficiency ratio (yield per kWh)
        /// </summary>
        public float GetEnergyEfficiency()
        {
            if (_totalEnergyConsumed <= 0f) return 0f;

            var totalYield = _cultivationManager?.GetTotalYieldHarvested() ?? 0f;
            return totalYield / _totalEnergyConsumed;
        }

        /// <summary>
        /// Get projected daily energy cost
        /// </summary>
        public float GetProjectedDailyCost(float electricityRate = 0.12f)
        {
            return _currentHourlyUsage * 24f * electricityRate;
        }

        /// <summary>
        /// Get energy usage for specific equipment type
        /// </summary>
        public float GetEquipmentEnergyUsage(string equipmentType)
        {
            return _equipmentEnergyUsage.ContainsKey(equipmentType) ? _equipmentEnergyUsage[equipmentType] : 0f;
        }

        /// <summary>
        /// Reset energy tracking (for testing or new facility)
        /// </summary>
        public void ResetEnergyTracking()
        {
            _totalEnergyConsumed = 0f;
            _currentHourlyUsage = 0f;

            foreach (var key in _equipmentEnergyUsage.Keys.ToArray())
            {
                _equipmentEnergyUsage[key] = 0f;
            }

            foreach (var key in _systemEnergyUsage.Keys.ToArray())
            {
                _systemEnergyUsage[key] = 0f;
            }

            if (_enableEnergyLogging)
                ChimeraLogger.Log("[EnergyTrackingSystem] Energy tracking reset");
        }

        #endregion

        #region ITickable Implementation

        // ITickable implementation
        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }
}
