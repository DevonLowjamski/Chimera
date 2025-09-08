using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Manages thermal monitoring, cooling systems, and temperature control.
    /// Extracted from AdvancedGrowLightSystem for modular architecture.
    /// Handles heat generation, cooling, and thermal safety systems.
    /// </summary>
    public class GrowLightThermalManager : MonoBehaviour, ITickable
    {
        [Header("Thermal Management Configuration")]
        [SerializeField] private bool _enableThermalLogging = true;
        [SerializeField] private float _thermalUpdateInterval = 5f; // Update every 5 seconds
        [SerializeField] private float _maxSafeTemperature = 80f; // Celsius
        [SerializeField] private float _criticalTemperature = 90f; // Emergency shutdown temperature
        [SerializeField] private float _targetTemperature = 65f; // Optimal operating temperature

        // Dependencies
        private GrowLightController _lightController;

        // Thermal state
        private float _currentTemperature = 25f; // Room temperature start
        private float _heatGeneration = 0f;
        private float _coolingCapacity = 100f; // Watts of cooling
        private bool _thermalThrottlingActive = false;
        private bool _emergencyShutdownTriggered = false;
        private float _lastThermalUpdate = 0f;

        // Cooling systems
        private List<CoolingSystem> _coolingSystems = new List<CoolingSystem>();
        private Dictionary<CoolingSystemType, bool> _coolingSystemStates = new Dictionary<CoolingSystemType, bool>();

        // Temperature history for trending
        private Queue<TemperatureReading> _temperatureHistory = new Queue<TemperatureReading>();
        private const int MaxHistoryPoints = 100;

        // Events
        public System.Action<float> OnTemperatureChanged;
        public System.Action<bool> OnThermalThrottlingChanged;
        public System.Action<CoolingSystem> OnCoolingSystemStateChanged;
        public System.Action OnEmergencyShutdown;
        public System.Action<ThermalAlert> OnThermalAlert;

        // Properties
        public float CurrentTemperature => _currentTemperature;
        public float HeatGeneration => _heatGeneration;
        public float CoolingCapacity => _coolingCapacity;
        public bool ThermalThrottlingActive => _thermalThrottlingActive;
        public bool EmergencyShutdownTriggered => _emergencyShutdownTriggered;
        public float TargetTemperature => _targetTemperature;
        public int ActiveCoolingSystems => _coolingSystems.FindAll(c => c.IsActive).Count;

        /// <summary>
        /// Initialize thermal manager with dependencies
        /// </summary>
        public void Initialize(GrowLightController lightController)
        {
            _lightController = lightController;

            InitializeCoolingSystems();
            InitializeTemperatureHistory();

            // Subscribe to light controller events
            if (_lightController != null)
            {
                _lightController.OnIntensityChanged += OnLightIntensityChanged;
                _lightController.OnPowerConsumptionChanged += OnPowerConsumptionChanged;
            }

            LogDebug("Grow light thermal manager initialized");
        }

            public void Tick(float deltaTime)
    {
            _lastThermalUpdate += deltaTime;

            if (_lastThermalUpdate >= _thermalUpdateInterval)
            {
                UpdateThermalState();
                _lastThermalUpdate = 0f;

    }
        }

        #region Cooling Systems

        /// <summary>
        /// Initialize cooling systems
        /// </summary>
        private void InitializeCoolingSystems()
        {
            // Active air cooling (fans)
            _coolingSystems.Add(new CoolingSystem
            {
                SystemId = "fan_primary",
                SystemType = CoolingSystemType.Fan,
                CoolingPowerWatts = 50f,
                PowerConsumption = 15f,
                ActivationTemperature = 60f,
                DeactivationTemperature = 55f,
                IsActive = false,
                IsAvailable = true
            });

            // Liquid cooling system
            _coolingSystems.Add(new CoolingSystem
            {
                SystemId = "liquid_cooling",
                SystemType = CoolingSystemType.Liquid,
                CoolingPowerWatts = 150f,
                PowerConsumption = 40f,
                ActivationTemperature = 70f,
                DeactivationTemperature = 65f,
                IsActive = false,
                IsAvailable = false // Needs to be enabled
            });

            // Emergency cooling (heat sink)
            _coolingSystems.Add(new CoolingSystem
            {
                SystemId = "heat_sink",
                SystemType = CoolingSystemType.HeatSink,
                CoolingPowerWatts = 80f,
                PowerConsumption = 0f, // Passive cooling
                ActivationTemperature = 75f,
                DeactivationTemperature = 70f,
                IsActive = true, // Always active (passive)
                IsAvailable = true
            });

            // Initialize cooling system states
            foreach (CoolingSystemType type in System.Enum.GetValues(typeof(CoolingSystemType)))
            {
                _coolingSystemStates[type] = false;
            }
        }

        /// <summary>
        /// Update cooling system states based on temperature
        /// </summary>
        private void UpdateCoolingSystems()
        {
            foreach (var coolingSystem in _coolingSystems)
            {
                if (!coolingSystem.IsAvailable) continue;

                bool shouldActivate = _currentTemperature >= coolingSystem.ActivationTemperature && !coolingSystem.IsActive;
                bool shouldDeactivate = _currentTemperature <= coolingSystem.DeactivationTemperature && coolingSystem.IsActive;

                if (shouldActivate)
                {
                    ActivateCoolingSystem(coolingSystem);
                }
                else if (shouldDeactivate && coolingSystem.SystemType != CoolingSystemType.HeatSink)
                {
                    DeactivateCoolingSystem(coolingSystem);
                }
            }
        }

        /// <summary>
        /// Activate a cooling system
        /// </summary>
        private void ActivateCoolingSystem(CoolingSystem system)
        {
            system.IsActive = true;
            system.LastActivated = System.DateTime.Now;
            _coolingSystemStates[system.SystemType] = true;

            OnCoolingSystemStateChanged?.Invoke(system);
            LogDebug($"Activated cooling system: {system.SystemType} ({system.CoolingPowerWatts}W cooling power)");
        }

        /// <summary>
        /// Deactivate a cooling system
        /// </summary>
        private void DeactivateCoolingSystem(CoolingSystem system)
        {
            system.IsActive = false;
            _coolingSystemStates[system.SystemType] = false;

            OnCoolingSystemStateChanged?.Invoke(system);
            LogDebug($"Deactivated cooling system: {system.SystemType}");
        }

        /// <summary>
        /// Calculate total active cooling capacity
        /// </summary>
        private float CalculateTotalCoolingCapacity()
        {
            float totalCooling = 0f;

            foreach (var system in _coolingSystems)
            {
                if (system.IsActive)
                {
                    totalCooling += system.CoolingPowerWatts;
                }
            }

            return totalCooling;
        }

        #endregion

        #region Thermal State Management

        /// <summary>
        /// Update overall thermal state
        /// </summary>
        private void UpdateThermalState()
        {
            UpdateHeatGeneration();
            UpdateCoolingSystems();
            UpdateTemperature();
            CheckThermalThresholds();
            RecordTemperatureReading();
        }

        /// <summary>
        /// Update heat generation based on light intensity
        /// </summary>
        private void UpdateHeatGeneration()
        {
            if (_lightController == null) return;

            // Heat generation roughly correlates with power consumption
            float powerConsumption = _lightController.PowerConsumption;
            float efficiency = _lightController.CalculateCurrentEfficiency();

            // Inefficient lights generate more heat
            float heatFactor = Mathf.Clamp(1f / Mathf.Max(efficiency, 0.5f), 0.5f, 2f);
            _heatGeneration = powerConsumption * heatFactor * 0.7f; // 70% of power becomes heat
        }

        /// <summary>
        /// Update temperature based on heat generation and cooling
        /// </summary>
        private void UpdateTemperature()
        {
            float ambientTemperature = 25f; // Room temperature
            float thermalMass = 500f; // Thermal mass of the light fixture

            // Calculate net heat (generation - cooling)
            float totalCooling = CalculateTotalCoolingCapacity();
            float netHeat = _heatGeneration - totalCooling;

            // Temperature change rate based on thermal mass
            float temperatureChangeRate = netHeat / thermalMass * Time.deltaTime * _thermalUpdateInterval;

            // Apply temperature change with ambient cooling
            float ambientCoolingRate = (_currentTemperature - ambientTemperature) * 0.01f * Time.deltaTime * _thermalUpdateInterval;
            _currentTemperature += temperatureChangeRate - ambientCoolingRate;

            // Clamp to reasonable limits
            _currentTemperature = Mathf.Clamp(_currentTemperature, ambientTemperature, _criticalTemperature + 10f);

            OnTemperatureChanged?.Invoke(_currentTemperature);
        }

        /// <summary>
        /// Check thermal thresholds and trigger alerts
        /// </summary>
        private void CheckThermalThresholds()
        {
            // Emergency shutdown check
            if (_currentTemperature >= _criticalTemperature && !_emergencyShutdownTriggered)
            {
                TriggerEmergencyShutdown();
                return;
            }

            // Thermal throttling check
            bool shouldThrottle = _currentTemperature > _maxSafeTemperature;
            if (shouldThrottle != _thermalThrottlingActive)
            {
                _thermalThrottlingActive = shouldThrottle;
                _lightController?.SetThermalThrottling(_thermalThrottlingActive);
                OnThermalThrottlingChanged?.Invoke(_thermalThrottlingActive);

                var alert = new ThermalAlert
                {
                    AlertType = _thermalThrottlingActive ? ThermalAlertType.ThrottlingActivated : ThermalAlertType.ThrottlingDeactivated,
                    Temperature = _currentTemperature,
                    Timestamp = System.DateTime.Now,
                    Message = _thermalThrottlingActive ? "Thermal throttling activated" : "Thermal throttling deactivated"
                };

                OnThermalAlert?.Invoke(alert);
                LogDebug(alert.Message);
            }

            // Generate temperature warnings
            if (_currentTemperature > _maxSafeTemperature * 0.9f && _currentTemperature < _maxSafeTemperature)
            {
                // Temperature approaching warning threshold
                var alert = new ThermalAlert
                {
                    AlertType = ThermalAlertType.TemperatureWarning,
                    Temperature = _currentTemperature,
                    Timestamp = System.DateTime.Now,
                    Message = $"Temperature approaching safe limit: {_currentTemperature:F1}°C"
                };

                OnThermalAlert?.Invoke(alert);
            }
        }

        /// <summary>
        /// Trigger emergency shutdown
        /// </summary>
        private void TriggerEmergencyShutdown()
        {
            _emergencyShutdownTriggered = true;

            // Immediately shut down lights
            _lightController?.TurnOff();
            _lightController?.SetIntensityImmediate(0f);

            // Activate all available cooling systems
            foreach (var system in _coolingSystems)
            {
                if (system.IsAvailable && !system.IsActive && system.SystemType != CoolingSystemType.HeatSink)
                {
                    ActivateCoolingSystem(system);
                }
            }

            var alert = new ThermalAlert
            {
                AlertType = ThermalAlertType.EmergencyShutdown,
                Temperature = _currentTemperature,
                Timestamp = System.DateTime.Now,
                Message = $"EMERGENCY SHUTDOWN: Critical temperature reached ({_currentTemperature:F1}°C)"
            };

            OnThermalAlert?.Invoke(alert);
            OnEmergencyShutdown?.Invoke();

            LogError($"Emergency thermal shutdown triggered at {_currentTemperature:F1}°C");
        }

        /// <summary>
        /// Reset emergency shutdown state
        /// </summary>
        public void ResetEmergencyShutdown()
        {
            if (_currentTemperature < _maxSafeTemperature)
            {
                _emergencyShutdownTriggered = false;
                LogDebug("Emergency shutdown state reset");
            }
            else
            {
                LogError("Cannot reset emergency shutdown - temperature still too high");
            }
        }

        #endregion

        #region Temperature History

        /// <summary>
        /// Initialize temperature history tracking
        /// </summary>
        private void InitializeTemperatureHistory()
        {
            _temperatureHistory.Clear();

            // Add initial reading
            RecordTemperatureReading();
        }

        /// <summary>
        /// Record a temperature reading
        /// </summary>
        private void RecordTemperatureReading()
        {
            var reading = new TemperatureReading
            {
                Temperature = _currentTemperature,
                Timestamp = System.DateTime.Now,
                HeatGeneration = _heatGeneration,
                CoolingCapacity = CalculateTotalCoolingCapacity()
            };

            _temperatureHistory.Enqueue(reading);

            // Keep history manageable
            while (_temperatureHistory.Count > MaxHistoryPoints)
            {
                _temperatureHistory.Dequeue();
            }
        }

        /// <summary>
        /// Get temperature history
        /// </summary>
        public List<TemperatureReading> GetTemperatureHistory(int maxPoints = 50)
        {
            var history = _temperatureHistory.ToArray();
            var count = Mathf.Min(maxPoints, history.Length);
            var result = new List<TemperatureReading>();

            for (int i = history.Length - count; i < history.Length; i++)
            {
                result.Add(history[i]);
            }

            return result;
        }

        /// <summary>
        /// Get temperature trend over time
        /// </summary>
        public float GetTemperatureTrend(int minutesBack = 10)
        {
            var cutoffTime = System.DateTime.Now.AddMinutes(-minutesBack);
            var recentReadings = _temperatureHistory
                .Where(r => r.Timestamp >= cutoffTime)
                .OrderBy(r => r.Timestamp)
                .ToList();

            if (recentReadings.Count < 2) return 0f;

            // Simple linear trend calculation
            float firstTemp = recentReadings.First().Temperature;
            float lastTemp = recentReadings.Last().Temperature;

            return lastTemp - firstTemp;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle light intensity changes
        /// </summary>
        private void OnLightIntensityChanged(float intensity)
        {
            // Heat generation will be updated on next thermal update
            LogDebug($"Light intensity changed: {intensity} PPFD");
        }

        /// <summary>
        /// Handle power consumption changes
        /// </summary>
        private void OnPowerConsumptionChanged(float powerConsumption)
        {
            // Heat generation will be updated on next thermal update
            LogDebug($"Power consumption changed: {powerConsumption}W");
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Enable or disable a cooling system
        /// </summary>
        public void SetCoolingSystemAvailable(CoolingSystemType systemType, bool available)
        {
            var system = _coolingSystems.Find(s => s.SystemType == systemType);
            if (system != null)
            {
                system.IsAvailable = available;

                if (!available && system.IsActive)
                {
                    DeactivateCoolingSystem(system);
                }

                LogDebug($"Cooling system {systemType} {(available ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Set target temperature
        /// </summary>
        public void SetTargetTemperature(float targetTemperature)
        {
            _targetTemperature = Mathf.Clamp(targetTemperature, 20f, _maxSafeTemperature - 5f);
            LogDebug($"Target temperature set to: {_targetTemperature:F1}°C");
        }

        /// <summary>
        /// Get thermal status summary
        /// </summary>
        public ThermalStatus GetThermalStatus()
        {
            return new ThermalStatus
            {
                CurrentTemperature = _currentTemperature,
                TargetTemperature = _targetTemperature,
                HeatGeneration = _heatGeneration,
                CoolingCapacity = CalculateTotalCoolingCapacity(),
                ThermalThrottlingActive = _thermalThrottlingActive,
                EmergencyShutdownTriggered = _emergencyShutdownTriggered,
                ActiveCoolingSystems = ActiveCoolingSystems,
                TemperatureTrend = GetTemperatureTrend()
            };
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            // Unsubscribe from events
            if (_lightController != null)
            {
                _lightController.OnIntensityChanged -= OnLightIntensityChanged;
                _lightController.OnPowerConsumptionChanged -= OnPowerConsumptionChanged;
            }
        }

        private void LogDebug(string message)
        {
            if (_enableThermalLogging)
                ChimeraLogger.Log($"[GrowLightThermalManager] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[GrowLightThermalManager] {message}");
        }

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

    protected virtual void Start()
    {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

}

    /// <summary>
    /// Cooling system configuration
    /// </summary>
    [System.Serializable]
    public class CoolingSystem
    {
        public string SystemId;
        public CoolingSystemType SystemType;
        public float CoolingPowerWatts;
        public float PowerConsumption;
        public float ActivationTemperature;
        public float DeactivationTemperature;
        public bool IsActive = false;
        public bool IsAvailable = true;
        public System.DateTime LastActivated;
    }

    /// <summary>
    /// Temperature reading data point
    /// </summary>
    [System.Serializable]
    public class TemperatureReading
    {
        public float Temperature;
        public System.DateTime Timestamp;
        public float HeatGeneration;
        public float CoolingCapacity;
    }

    /// <summary>
    /// Thermal alert information
    /// </summary>
    [System.Serializable]
    public class ThermalAlert
    {
        public ThermalAlertType AlertType;
        public float Temperature;
        public System.DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Thermal status summary
    /// </summary>
    [System.Serializable]
    public class ThermalStatus
    {
        public float CurrentTemperature;
        public float TargetTemperature;
        public float HeatGeneration;
        public float CoolingCapacity;
        public bool ThermalThrottlingActive;
        public bool EmergencyShutdownTriggered;
        public int ActiveCoolingSystems;
        public float TemperatureTrend;
    }

    public enum CoolingSystemType
    {
        Fan,
        Liquid,
        HeatSink,
        Thermoelectric
    }

    public enum ThermalAlertType
    {
        TemperatureWarning,
        ThrottlingActivated,
        ThrottlingDeactivated,
        EmergencyShutdown,
        CoolingSystemFailure
    }
}
