using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Data.Environment;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC-013-3b: HVAC Service - Extracted core HVAC control and environmental conditioning algorithms
    /// from HVACController.cs for Single Responsibility Principle
    /// Focuses solely on HVAC calculations, control logic, and environmental conditioning
    /// </summary>
    public class HVAC_Service : IHVAC_Service
    {
        [Header("HVAC Service Configuration")]
        [SerializeField] private bool _enableAdvancedControl = true;
        [SerializeField] private bool _enablePredictiveControl = false;
        [SerializeField] private float _controlAccuracy = 1.0f;
        [SerializeField] private float _responseTimeMultiplier = 1.0f;
        
        [Header("Default HVAC Specifications")]
        [SerializeField] private float _defaultMaxHeatingCapacity = 10f; // kW
        [SerializeField] private float _defaultMaxCoolingCapacity = 12f; // kW
        [SerializeField] private float _defaultMaxHumidificationRate = 5f; // L/hr
        [SerializeField] private float _defaultMaxDehumidificationRate = 8f; // L/hr
        [SerializeField] private float _defaultEnergyEfficiency = 0.85f; // 0-1
        
        [Header("Control Parameters")]
        [SerializeField] private float _defaultTemperatureTolerance = 1f; // °C
        [SerializeField] private float _defaultHumidityTolerance = 5f; // %
        [SerializeField] private float _defaultResponseTime = 2f; // minutes
        [SerializeField] private float _controlUpdateInterval = 30f; // seconds
        
        // HVAC system tracking
        private Dictionary<string, HVACSystemState> _hvacSystems = new Dictionary<string, HVACSystemState>();
        private Dictionary<string, HVACControlData> _controlData = new Dictionary<string, HVACControlData>();
        private HVACPerformanceMetrics _performanceMetrics = new HVACPerformanceMetrics();
        
        // Performance tracking
        private int _controlCalculationsPerformed = 0;
        private float _totalControlProcessingTime = 0f;
        private float _totalEnergyConsumed = 0f;
        
        public bool IsInitialized { get; private set; }
        
        public bool EnableAdvancedControl
        {
            get => _enableAdvancedControl;
            set => _enableAdvancedControl = value;
        }
        
        public float ControlAccuracy
        {
            get => _controlAccuracy;
            set => _controlAccuracy = Mathf.Clamp(value, 0.1f, 2.0f);
        }
        
        public HVAC_Service()
        {
        }
        
        public void Initialize()
        {
            if (IsInitialized)
            {
                ChimeraLogger.LogWarning("[HVAC_Service] Already initialized");
                return;
            }
            
            InitializeHVACCalculations();
            IsInitialized = true;
            
            ChimeraLogger.Log("[HVAC_Service] HVAC control service initialized successfully");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            _hvacSystems.Clear();
            _controlData.Clear();
            _performanceMetrics = new HVACPerformanceMetrics();
            
            IsInitialized = false;
            ChimeraLogger.Log("[HVAC_Service] HVAC control service shutdown completed");
        }
        
        /// <summary>
        /// Calculate HVAC control response for target environmental conditions
        /// </summary>
        public HVACControlResponse CalculateHVACResponse(
            string systemId,
            EnvironmentalConditions currentConditions,
            EnvironmentalConditions targetConditions,
            HVACSystemSpecifications specs = null)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogWarning("[HVAC_Service] Service not initialized");
                return CreateDefaultResponse();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Use default specs if none provided
            if (specs == null)
            {
                specs = CreateDefaultHVACSpecifications();
            }
            
            // Calculate required control actions
            var temperatureControl = CalculateTemperatureControl(currentConditions.Temperature, targetConditions.Temperature, specs);
            var humidityControl = CalculateHumidityControl(currentConditions.Humidity, targetConditions.Humidity, specs);
            var airflowControl = CalculateAirflowControl(currentConditions.AirVelocity, targetConditions.AirVelocity, specs);
            
            // Calculate power consumption
            float powerConsumption = CalculatePowerConsumption(temperatureControl, humidityControl, airflowControl, specs);
            
            // Calculate response time
            float responseTime = CalculateResponseTime(currentConditions, targetConditions, specs);
            
            // Calculate efficiency
            float efficiency = CalculateSystemEfficiency(temperatureControl, humidityControl, specs);
            
            // Update system state
            UpdateHVACSystemState(systemId, currentConditions, targetConditions, specs);
            
            // Update performance tracking
            _controlCalculationsPerformed++;
            _totalControlProcessingTime += Time.realtimeSinceStartup - startTime;
            _totalEnergyConsumed += powerConsumption;
            
            return new HVACControlResponse
            {
                SystemId = systemId,
                TemperatureControl = temperatureControl,
                HumidityControl = humidityControl,
                AirflowControl = airflowControl,
                PowerConsumption = powerConsumption,
                ResponseTime = responseTime,
                Efficiency = efficiency,
                ProcessingTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f
            };
        }
        
        /// <summary>
        /// Simulate HVAC environmental impact over time
        /// </summary>
        public EnvironmentalConditions SimulateHVACImpact(
            string systemId,
            EnvironmentalConditions currentConditions,
            HVACControlResponse controlResponse,
            float deltaTime)
        {
            if (!IsInitialized) return currentConditions;
            
            var newConditions = new EnvironmentalConditions
            {
                Temperature = currentConditions.Temperature,
                Humidity = currentConditions.Humidity,
                CO2Level = currentConditions.CO2Level,
                LightIntensity = currentConditions.LightIntensity,
                AirVelocity = currentConditions.AirVelocity
            };
            
            // Apply temperature changes
            ApplyTemperatureControl(ref newConditions, controlResponse.TemperatureControl, deltaTime);
            
            // Apply humidity changes
            ApplyHumidityControl(ref newConditions, controlResponse.HumidityControl, deltaTime);
            
            // Apply airflow changes
            ApplyAirflowControl(ref newConditions, controlResponse.AirflowControl, deltaTime);
            
            // Apply HVAC side effects (CO2 changes from ventilation, etc.)
            ApplyHVACSideEffects(ref newConditions, controlResponse, deltaTime);
            
            // Update control data
            UpdateHVACControlData(systemId, currentConditions, newConditions, controlResponse);
            
            return newConditions;
        }
        
        /// <summary>
        /// Calculate optimal HVAC settings for target conditions
        /// </summary>
        public HVACOptimalSettings CalculateOptimalSettings(
            EnvironmentalConditions currentConditions,
            EnvironmentalConditions targetConditions,
            HVACSystemSpecifications specs,
            OptimizationCriteria criteria = OptimizationCriteria.Efficiency)
        {
            if (!IsInitialized)
            {
                return new HVACOptimalSettings
                {
                    HeatingPower = 0f,
                    CoolingPower = 0f,
                    HumidificationPower = 0f,
                    DehumidificationPower = 0f,
                    FanSpeed = 0.5f,
                    EstimatedEfficiency = 0.8f
                };
            }
            
            var settings = new HVACOptimalSettings();
            
            // Calculate optimal power levels based on criteria
            switch (criteria)
            {
                case OptimizationCriteria.Efficiency:
                    settings = CalculateEfficiencyOptimizedSettings(currentConditions, targetConditions, specs);
                    break;
                case OptimizationCriteria.Speed:
                    settings = CalculateSpeedOptimizedSettings(currentConditions, targetConditions, specs);
                    break;
                case OptimizationCriteria.EnergyConservation:
                    settings = CalculateEnergyOptimizedSettings(currentConditions, targetConditions, specs);
                    break;
                case OptimizationCriteria.Balance:
                    settings = CalculateBalancedSettings(currentConditions, targetConditions, specs);
                    break;
            }
            
            // Validate settings against system capabilities
            ValidateHVACSettings(ref settings, specs);
            
            return settings;
        }
        
        /// <summary>
        /// Get HVAC system performance analysis
        /// </summary>
        public HVACPerformanceAnalysis GetPerformanceAnalysis(string systemId)
        {
            if (!IsInitialized || !_hvacSystems.ContainsKey(systemId))
            {
                return new HVACPerformanceAnalysis
                {
                    SystemId = systemId,
                    OverallEfficiency = 0.8f,
                    EnergyConsumption = 0f,
                    ResponseAccuracy = 0.9f,
                    OperatingHours = 0f
                };
            }
            
            var systemState = _hvacSystems[systemId];
            var controlData = _controlData.GetValueOrDefault(systemId, new HVACControlData());
            
            return new HVACPerformanceAnalysis
            {
                SystemId = systemId,
                OverallEfficiency = CalculateOverallEfficiency(systemState, controlData),
                EnergyConsumption = systemState.TotalEnergyConsumed,
                ResponseAccuracy = CalculateResponseAccuracy(controlData),
                OperatingHours = systemState.OperatingHours,
                CycleCount = systemState.CycleCount,
                AverageResponseTime = controlData.AverageResponseTime,
                MaintenanceScore = CalculateMaintenanceScore(systemState)
            };
        }
        
        /// <summary>
        /// Get HVAC service performance metrics
        /// </summary>
        public HVACPerformanceMetrics GetPerformanceMetrics()
        {
            return new HVACPerformanceMetrics
            {
                TotalControlCalculations = _controlCalculationsPerformed,
                AverageProcessingTime = _controlCalculationsPerformed > 0 ? (_totalControlProcessingTime / _controlCalculationsPerformed) * 1000f : 0f,
                TrackedSystems = _hvacSystems.Count,
                TotalEnergyConsumed = _totalEnergyConsumed,
                AdvancedControlEnabled = _enableAdvancedControl,
                PredictiveControlEnabled = _enablePredictiveControl,
                ControlAccuracy = _controlAccuracy
            };
        }
        
        /// <summary>
        /// Register a new HVAC system for tracking
        /// </summary>
        public void RegisterHVACSystem(string systemId, HVACSystemSpecifications specs)
        {
            if (!IsInitialized) return;
            
            var systemState = new HVACSystemState
            {
                SystemId = systemId,
                Specifications = specs,
                IsOperational = true,
                LastUpdate = DateTime.Now,
                TotalEnergyConsumed = 0f,
                OperatingHours = 0f,
                CycleCount = 0
            };
            
            _hvacSystems[systemId] = systemState;
            _controlData[systemId] = new HVACControlData { SystemId = systemId };
            
            ChimeraLogger.Log($"[HVAC_Service] Registered HVAC system: {systemId}");
        }
        
        /// <summary>
        /// Unregister an HVAC system
        /// </summary>
        public void UnregisterHVACSystem(string systemId)
        {
            if (!IsInitialized) return;
            
            _hvacSystems.Remove(systemId);
            _controlData.Remove(systemId);
            
            ChimeraLogger.Log($"[HVAC_Service] Unregistered HVAC system: {systemId}");
        }
        
        #region Private Calculation Methods
        
        private void InitializeHVACCalculations()
        {
            _hvacSystems.Clear();
            _controlData.Clear();
            _performanceMetrics = new HVACPerformanceMetrics();
            _controlCalculationsPerformed = 0;
            _totalControlProcessingTime = 0f;
            _totalEnergyConsumed = 0f;
        }
        
        private HVACSystemSpecifications CreateDefaultHVACSpecifications()
        {
            return new HVACSystemSpecifications
            {
                MaxHeatingCapacity = _defaultMaxHeatingCapacity,
                MaxCoolingCapacity = _defaultMaxCoolingCapacity,
                MaxHumidificationRate = _defaultMaxHumidificationRate,
                MaxDehumidificationRate = _defaultMaxDehumidificationRate,
                EnergyEfficiency = _defaultEnergyEfficiency,
                TemperatureTolerance = _defaultTemperatureTolerance,
                HumidityTolerance = _defaultHumidityTolerance,
                ResponseTime = _defaultResponseTime
            };
        }
        
        private TemperatureControlData CalculateTemperatureControl(float currentTemp, float targetTemp, HVACSystemSpecifications specs)
        {
            float tempDifference = targetTemp - currentTemp;
            bool needsHeating = tempDifference > specs.TemperatureTolerance;
            bool needsCooling = tempDifference < -specs.TemperatureTolerance;
            
            var control = new TemperatureControlData
            {
                TargetTemperature = targetTemp,
                CurrentTemperature = currentTemp,
                NeedsHeating = needsHeating,
                NeedsCooling = needsCooling,
                HeatingPower = 0f,
                CoolingPower = 0f
            };
            
            if (needsHeating)
            {
                // Calculate heating power based on temperature difference
                float normalizedDifference = Mathf.Clamp01(tempDifference / 10f); // Normalize to 10°C max difference
                control.HeatingPower = normalizedDifference * specs.EnergyEfficiency;
            }
            else if (needsCooling)
            {
                // Calculate cooling power based on temperature difference
                float normalizedDifference = Mathf.Clamp01(-tempDifference / 10f); // Normalize to 10°C max difference
                control.CoolingPower = normalizedDifference * specs.EnergyEfficiency;
            }
            
            return control;
        }
        
        private HumidityControlData CalculateHumidityControl(float currentHumidity, float targetHumidity, HVACSystemSpecifications specs)
        {
            float humidityDifference = targetHumidity - currentHumidity;
            bool needsHumidification = humidityDifference > specs.HumidityTolerance;
            bool needsDehumidification = humidityDifference < -specs.HumidityTolerance;
            
            var control = new HumidityControlData
            {
                TargetHumidity = targetHumidity,
                CurrentHumidity = currentHumidity,
                NeedsHumidification = needsHumidification,
                NeedsDehumidification = needsDehumidification,
                HumidificationPower = 0f,
                DehumidificationPower = 0f
            };
            
            if (needsHumidification)
            {
                // Calculate humidification power based on humidity difference
                float normalizedDifference = Mathf.Clamp01(humidityDifference / 30f); // Normalize to 30% max difference
                control.HumidificationPower = normalizedDifference * specs.EnergyEfficiency;
            }
            else if (needsDehumidification)
            {
                // Calculate dehumidification power based on humidity difference
                float normalizedDifference = Mathf.Clamp01(-humidityDifference / 30f); // Normalize to 30% max difference
                control.DehumidificationPower = normalizedDifference * specs.EnergyEfficiency;
            }
            
            return control;
        }
        
        private AirflowControlData CalculateAirflowControl(float currentAirflow, float targetAirflow, HVACSystemSpecifications specs)
        {
            float airflowDifference = targetAirflow - currentAirflow;
            
            return new AirflowControlData
            {
                TargetAirflow = targetAirflow,
                CurrentAirflow = currentAirflow,
                FanSpeed = Mathf.Clamp01((targetAirflow + 0.5f) / 2f), // Convert airflow to fan speed
                NeedsAdjustment = Mathf.Abs(airflowDifference) > 0.1f
            };
        }
        
        private float CalculatePowerConsumption(
            TemperatureControlData tempControl, 
            HumidityControlData humidityControl, 
            AirflowControlData airflowControl,
            HVACSystemSpecifications specs)
        {
            float power = 0f;
            
            // Temperature control power
            if (tempControl.NeedsHeating)
                power += specs.MaxHeatingCapacity * tempControl.HeatingPower * 0.8f; // Heating efficiency factor
            if (tempControl.NeedsCooling)
                power += specs.MaxCoolingCapacity * tempControl.CoolingPower;
            
            // Humidity control power
            if (humidityControl.NeedsHumidification)
                power += 0.5f * humidityControl.HumidificationPower; // Humidifier power
            if (humidityControl.NeedsDehumidification)
                power += 1.2f * humidityControl.DehumidificationPower; // Dehumidifier power
            
            // Fan power
            power += 0.2f * airflowControl.FanSpeed; // Base fan power
            
            // Base system power (controls, sensors, etc.)
            power += 0.1f;
            
            return power;
        }
        
        private float CalculateResponseTime(EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            float tempChange = Mathf.Abs(target.Temperature - current.Temperature);
            float humidityChange = Mathf.Abs(target.Humidity - current.Humidity);
            
            // Base response time modified by change magnitude
            float changeFactor = (tempChange / 10f) + (humidityChange / 30f);
            
            return specs.ResponseTime * (1f + changeFactor) * _responseTimeMultiplier;
        }
        
        private float CalculateSystemEfficiency(TemperatureControlData tempControl, HumidityControlData humidityControl, HVACSystemSpecifications specs)
        {
            float baseEfficiency = specs.EnergyEfficiency;
            
            // Reduce efficiency when multiple systems are running simultaneously
            float systemLoad = 0f;
            if (tempControl.NeedsHeating || tempControl.NeedsCooling) systemLoad += 0.5f;
            if (humidityControl.NeedsHumidification || humidityControl.NeedsDehumidification) systemLoad += 0.3f;
            
            // Efficiency penalty for high system load
            float efficiencyPenalty = systemLoad > 0.7f ? (systemLoad - 0.7f) * 0.2f : 0f;
            
            return Mathf.Max(0.5f, baseEfficiency - efficiencyPenalty);
        }
        
        private void ApplyTemperatureControl(ref EnvironmentalConditions conditions, TemperatureControlData control, float deltaTime)
        {
            float temperatureChange = 0f;
            
            if (control.NeedsHeating)
            {
                // Heating rate calculation
                float heatingRate = control.HeatingPower * _defaultMaxHeatingCapacity * deltaTime / 60f; // Convert to minutes
                temperatureChange = heatingRate * 0.5f; // Simplified heating model
            }
            else if (control.NeedsCooling)
            {
                // Cooling rate calculation
                float coolingRate = control.CoolingPower * _defaultMaxCoolingCapacity * deltaTime / 60f; // Convert to minutes
                temperatureChange = -coolingRate * 0.4f; // Simplified cooling model
            }
            
            conditions.Temperature = Mathf.Clamp(conditions.Temperature + temperatureChange, 10f, 40f);
        }
        
        private void ApplyHumidityControl(ref EnvironmentalConditions conditions, HumidityControlData control, float deltaTime)
        {
            float humidityChange = 0f;
            
            if (control.NeedsHumidification)
            {
                // Humidification rate calculation
                float humidificationRate = control.HumidificationPower * _defaultMaxHumidificationRate * deltaTime / 60f;
                humidityChange = humidificationRate * 2f; // Simplified humidification model
            }
            else if (control.NeedsDehumidification)
            {
                // Dehumidification rate calculation
                float dehumidificationRate = control.DehumidificationPower * _defaultMaxDehumidificationRate * deltaTime / 60f;
                humidityChange = -dehumidificationRate * 1.5f; // Simplified dehumidification model
            }
            
            conditions.Humidity = Mathf.Clamp(conditions.Humidity + humidityChange, 10f, 90f);
        }
        
        private void ApplyAirflowControl(ref EnvironmentalConditions conditions, AirflowControlData control, float deltaTime)
        {
            // Simple airflow adjustment based on fan speed
            float targetAirflow = control.FanSpeed * 2f; // Max 2 m/s airflow
            float airflowChange = (targetAirflow - conditions.AirVelocity) * deltaTime * 2f; // 2-second response time
            
            conditions.AirVelocity = Mathf.Max(0f, conditions.AirVelocity + airflowChange);
        }
        
        private void ApplyHVACSideEffects(ref EnvironmentalConditions conditions, HVACControlResponse response, float deltaTime)
        {
            // Ventilation affects CO2 levels
            if (response.AirflowControl.FanSpeed > 0.5f)
            {
                float ventilationEffect = response.AirflowControl.FanSpeed * deltaTime * 10f; // CO2 reduction from ventilation
                conditions.CO2Level = Mathf.Max(400f, conditions.CO2Level - ventilationEffect);
            }
        }
        
        private void UpdateHVACSystemState(string systemId, EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            if (!_hvacSystems.ContainsKey(systemId))
            {
                RegisterHVACSystem(systemId, specs);
            }
            
            var systemState = _hvacSystems[systemId];
            systemState.LastUpdate = DateTime.Now;
            systemState.OperatingHours += Time.deltaTime / 3600f; // Convert to hours
            
            // Update cycle count if switching between heating/cooling
            if ((current.Temperature < target.Temperature - specs.TemperatureTolerance) || 
                (current.Temperature > target.Temperature + specs.TemperatureTolerance))
            {
                systemState.CycleCount++;
            }
        }
        
        private void UpdateHVACControlData(string systemId, EnvironmentalConditions before, EnvironmentalConditions after, HVACControlResponse response)
        {
            if (!_controlData.ContainsKey(systemId))
            {
                _controlData[systemId] = new HVACControlData { SystemId = systemId };
            }
            
            var controlData = _controlData[systemId];
            controlData.LastControlResponse = response;
            controlData.TotalResponses++;
            
            // Calculate response accuracy
            float tempAccuracy = 1f - Mathf.Abs(after.Temperature - response.TemperatureControl.TargetTemperature) / 10f;
            float humidityAccuracy = 1f - Mathf.Abs(after.Humidity - response.HumidityControl.TargetHumidity) / 30f;
            float currentAccuracy = (tempAccuracy + humidityAccuracy) * 0.5f;
            
            // Update rolling average
            controlData.AverageResponseAccuracy = (controlData.AverageResponseAccuracy * (controlData.TotalResponses - 1) + currentAccuracy) / controlData.TotalResponses;
            controlData.AverageResponseTime = (controlData.AverageResponseTime * (controlData.TotalResponses - 1) + response.ResponseTime) / controlData.TotalResponses;
        }
        
        // Optimization method implementations (simplified)
        private HVACOptimalSettings CalculateEfficiencyOptimizedSettings(EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            // Focus on energy efficiency over speed
            return CalculateBalancedSettings(current, target, specs);
        }
        
        private HVACOptimalSettings CalculateSpeedOptimizedSettings(EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            // Focus on reaching target quickly
            var settings = CalculateBalancedSettings(current, target, specs);
            settings.HeatingPower *= 1.2f;
            settings.CoolingPower *= 1.2f;
            settings.HumidificationPower *= 1.1f;
            settings.DehumidificationPower *= 1.1f;
            return settings;
        }
        
        private HVACOptimalSettings CalculateEnergyOptimizedSettings(EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            // Focus on minimizing energy consumption
            var settings = CalculateBalancedSettings(current, target, specs);
            settings.HeatingPower *= 0.8f;
            settings.CoolingPower *= 0.8f;
            settings.HumidificationPower *= 0.9f;
            settings.DehumidificationPower *= 0.9f;
            return settings;
        }
        
        private HVACOptimalSettings CalculateBalancedSettings(EnvironmentalConditions current, EnvironmentalConditions target, HVACSystemSpecifications specs)
        {
            var tempControl = CalculateTemperatureControl(current.Temperature, target.Temperature, specs);
            var humidityControl = CalculateHumidityControl(current.Humidity, target.Humidity, specs);
            var airflowControl = CalculateAirflowControl(current.AirVelocity, target.AirVelocity, specs);
            
            return new HVACOptimalSettings
            {
                HeatingPower = tempControl.HeatingPower,
                CoolingPower = tempControl.CoolingPower,
                HumidificationPower = humidityControl.HumidificationPower,
                DehumidificationPower = humidityControl.DehumidificationPower,
                FanSpeed = airflowControl.FanSpeed,
                EstimatedEfficiency = CalculateSystemEfficiency(tempControl, humidityControl, specs)
            };
        }
        
        private void ValidateHVACSettings(ref HVACOptimalSettings settings, HVACSystemSpecifications specs)
        {
            // Ensure settings don't exceed system capabilities
            settings.HeatingPower = Mathf.Clamp01(settings.HeatingPower);
            settings.CoolingPower = Mathf.Clamp01(settings.CoolingPower);
            settings.HumidificationPower = Mathf.Clamp01(settings.HumidificationPower);
            settings.DehumidificationPower = Mathf.Clamp01(settings.DehumidificationPower);
            settings.FanSpeed = Mathf.Clamp01(settings.FanSpeed);
            
            // Ensure heating and cooling are not both active
            if (settings.HeatingPower > 0f && settings.CoolingPower > 0f)
            {
                if (settings.HeatingPower > settings.CoolingPower)
                    settings.CoolingPower = 0f;
                else
                    settings.HeatingPower = 0f;
            }
            
            // Ensure humidification and dehumidification are not both active
            if (settings.HumidificationPower > 0f && settings.DehumidificationPower > 0f)
            {
                if (settings.HumidificationPower > settings.DehumidificationPower)
                    settings.DehumidificationPower = 0f;
                else
                    settings.HumidificationPower = 0f;
            }
        }
        
        private HVACControlResponse CreateDefaultResponse()
        {
            return new HVACControlResponse
            {
                SystemId = "default",
                TemperatureControl = new TemperatureControlData(),
                HumidityControl = new HumidityControlData(),
                AirflowControl = new AirflowControlData(),
                PowerConsumption = 0f,
                ResponseTime = _defaultResponseTime,
                Efficiency = _defaultEnergyEfficiency,
                ProcessingTimeMs = 0f
            };
        }
        
        // Performance analysis helper methods
        private float CalculateOverallEfficiency(HVACSystemState systemState, HVACControlData controlData)
        {
            return controlData.AverageResponseAccuracy * systemState.Specifications.EnergyEfficiency;
        }
        
        private float CalculateResponseAccuracy(HVACControlData controlData)
        {
            return controlData.AverageResponseAccuracy;
        }
        
        private float CalculateMaintenanceScore(HVACSystemState systemState)
        {
            // Simple maintenance score based on operating hours and cycles
            float hoursScore = Mathf.Max(0f, 1f - systemState.OperatingHours / 8760f); // 1 year = 8760 hours
            float cyclesScore = Mathf.Max(0f, 1f - systemState.CycleCount / 10000f); // 10000 cycles
            
            return (hoursScore + cyclesScore) * 0.5f;
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    /// <summary>
    /// HVAC system specifications and capabilities
    /// </summary>
    [System.Serializable]
    public class HVACSystemSpecifications
    {
        public float MaxHeatingCapacity; // kW
        public float MaxCoolingCapacity; // kW
        public float MaxHumidificationRate; // L/hr
        public float MaxDehumidificationRate; // L/hr
        public float EnergyEfficiency; // 0-1
        public float TemperatureTolerance; // °C
        public float HumidityTolerance; // %
        public float ResponseTime; // minutes
    }
    
    /// <summary>
    /// HVAC control response data
    /// </summary>
    [System.Serializable]
    public class HVACControlResponse
    {
        public string SystemId;
        public TemperatureControlData TemperatureControl;
        public HumidityControlData HumidityControl;
        public AirflowControlData AirflowControl;
        public float PowerConsumption;
        public float ResponseTime;
        public float Efficiency;
        public float ProcessingTimeMs;
        
        // Additional properties for direct environmental adjustments
        public float TemperatureAdjustment;
        public float HumidityAdjustment;
        public float AirflowAdjustment;
    }
    
    /// <summary>
    /// Temperature control data
    /// </summary>
    [System.Serializable]
    public class TemperatureControlData
    {
        public float TargetTemperature;
        public float CurrentTemperature;
        public bool NeedsHeating;
        public bool NeedsCooling;
        public float HeatingPower; // 0-1
        public float CoolingPower; // 0-1
    }
    
    /// <summary>
    /// Humidity control data
    /// </summary>
    [System.Serializable]
    public class HumidityControlData
    {
        public float TargetHumidity;
        public float CurrentHumidity;
        public bool NeedsHumidification;
        public bool NeedsDehumidification;
        public float HumidificationPower; // 0-1
        public float DehumidificationPower; // 0-1
    }
    
    /// <summary>
    /// Airflow control data
    /// </summary>
    [System.Serializable]
    public class AirflowControlData
    {
        public float TargetAirflow;
        public float CurrentAirflow;
        public float FanSpeed; // 0-1
        public bool NeedsAdjustment;
    }
    
    /// <summary>
    /// HVAC optimal settings
    /// </summary>
    [System.Serializable]
    public class HVACOptimalSettings
    {
        public float HeatingPower; // 0-1
        public float CoolingPower; // 0-1
        public float HumidificationPower; // 0-1
        public float DehumidificationPower; // 0-1
        public float FanSpeed; // 0-1
        public float EstimatedEfficiency; // 0-1
    }
    
    /// <summary>
    /// HVAC system state tracking
    /// </summary>
    [System.Serializable]
    public class HVACSystemState
    {
        public string SystemId;
        public HVACSystemSpecifications Specifications;
        public bool IsOperational;
        public DateTime LastUpdate;
        public float TotalEnergyConsumed;
        public float OperatingHours;
        public int CycleCount;
    }
    
    /// <summary>
    /// HVAC control data tracking
    /// </summary>
    [System.Serializable]
    public class HVACControlData
    {
        public string SystemId;
        public HVACControlResponse LastControlResponse;
        public int TotalResponses;
        public float AverageResponseAccuracy;
        public float AverageResponseTime;
    }
    
    /// <summary>
    /// HVAC performance analysis
    /// </summary>
    [System.Serializable]
    public class HVACPerformanceAnalysis
    {
        public string SystemId;
        public float OverallEfficiency;
        public float EnergyConsumption;
        public float ResponseAccuracy;
        public float OperatingHours;
        public int CycleCount;
        public float AverageResponseTime;
        public float MaintenanceScore;
    }
    
    /// <summary>
    /// HVAC service performance metrics
    /// </summary>
    [System.Serializable]
    public class HVACPerformanceMetrics
    {
        public int TotalControlCalculations;
        public float AverageProcessingTime; // milliseconds
        public int TrackedSystems;
        public float TotalEnergyConsumed;
        public bool AdvancedControlEnabled;
        public bool PredictiveControlEnabled;
        public float ControlAccuracy;
        
        public override string ToString()
        {
            return $"HVAC Performance: {TotalControlCalculations} calculations, {AverageProcessingTime:F2}ms avg, {TrackedSystems} systems";
        }
    }
    
    /// <summary>
    /// HVAC optimization criteria
    /// </summary>
    public enum OptimizationCriteria
    {
        Efficiency,
        Speed,
        EnergyConservation,
        Balance
    }
    
    #endregion
}