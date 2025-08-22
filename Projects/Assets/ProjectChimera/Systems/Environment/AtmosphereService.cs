using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC-013-3a: Atmosphere Service - Extracted core atmospheric simulation algorithms
    /// from AtmosphericPhysicsSimulator.cs for Single Responsibility Principle
    /// Focuses solely on atmospheric physics calculations and environmental modeling
    /// </summary>
    public class AtmosphereService : IAtmosphereService
    {
        [Header("Atmosphere Service Configuration")]
        [SerializeField] private bool _enableAdvancedPhysics = true;
        [SerializeField] private bool _enableCFDSimulation = false; // Disabled by default for performance
        [SerializeField] private float _simulationAccuracy = 1.0f;
        [SerializeField] private int _maxSimulationIterations = 100; // Reduced for real-time performance
        [SerializeField] private float _convergenceTolerance = 0.01f; // Relaxed for performance
        
        [Header("Performance Optimization")]
        [SerializeField] private int _maxConcurrentSimulations = 2; // Reduced for performance
        [SerializeField] private float _simulationTimeStep = 0.1f; // Increased for performance
        [SerializeField] private bool _useGPUAcceleration = false; // Disabled by default
        [SerializeField] private bool _enableLODOptimization = true;
        
        [Header("Physics Properties")]
        [SerializeField] private float _airDensity = 1.225f; // kg/m³ at sea level
        [SerializeField] private float _dynamicViscosity = 1.81e-5f; // kg/(m·s) at 15°C
        [SerializeField] private float _thermalConductivity = 0.0262f; // W/(m·K)
        [SerializeField] private float _specificHeatCapacity = 1005f; // J/(kg·K)
        
        // Core calculation state
        private Dictionary<string, AtmosphericState> _atmosphericStates = new Dictionary<string, AtmosphericState>();
        private Dictionary<string, float> _environmentalFitness = new Dictionary<string, float>();
        private AtmosphericPerformanceMetrics _performanceMetrics = new AtmosphericPerformanceMetrics();
        
        // Performance tracking
        private int _calculationsPerformed = 0;
        private float _totalProcessingTime = 0f;
        
        public bool IsInitialized { get; private set; }
        
        public bool EnableAdvancedPhysics
        {
            get => _enableAdvancedPhysics;
            set => _enableAdvancedPhysics = value;
        }
        
        public float SimulationAccuracy
        {
            get => _simulationAccuracy;
            set => _simulationAccuracy = Mathf.Clamp(value, 0.1f, 2.0f);
        }
        
        public AtmosphereService()
        {
        }
        
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[AtmosphereService] Already initialized");
                return;
            }
            
            InitializeAtmosphericCalculations();
            IsInitialized = true;
            
            Debug.Log("[AtmosphereService] Atmospheric simulation service initialized successfully");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            _atmosphericStates.Clear();
            _environmentalFitness.Clear();
            _performanceMetrics = new AtmosphericPerformanceMetrics();
            
            IsInitialized = false;
            Debug.Log("[AtmosphereService] Atmospheric simulation service shutdown completed");
        }
        
        /// <summary>
        /// Calculate atmospheric conditions for a given zone
        /// </summary>
        public EnvironmentalConditions CalculateAtmosphericConditions(
            Vector3 position, 
            EnvironmentalConditions baseConditions,
            object equipmentState = null)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[AtmosphereService] Service not initialized");
                return baseConditions;
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Apply atmospheric physics calculations
            var result = new EnvironmentalConditions
            {
                Temperature = CalculateTemperatureDistribution(position, baseConditions, equipmentState),
                Humidity = CalculateHumidityDistribution(position, baseConditions, equipmentState),
                CO2Level = CalculateCO2Distribution(position, baseConditions, equipmentState),
                LightIntensity = CalculateLightDistribution(position, baseConditions, equipmentState),
                AirVelocity = CalculateAirflowVelocity(position, baseConditions, equipmentState)
            };
            
            // Apply atmospheric mixing effects
            ApplyAtmosphericMixing(ref result, position, baseConditions);
            
            // Apply convection and diffusion effects
            ApplyConvectionDiffusion(ref result, position, equipmentState);
            
            // Update performance tracking
            _calculationsPerformed++;
            _totalProcessingTime += Time.realtimeSinceStartup - startTime;
            
            return result;
        }
        
        /// <summary>
        /// Calculate environmental fitness for atmospheric conditions
        /// </summary>
        public float CalculateEnvironmentalFitness(
            EnvironmentalConditions conditions,
            EnvironmentalConditions optimalConditions)
        {
            if (!IsInitialized) return 1f;
            
            // Temperature fitness
            float tempFitness = CalculateTemperatureFitness(conditions.Temperature, optimalConditions.Temperature);
            
            // Humidity fitness
            float humidityFitness = CalculateHumidityFitness(conditions.Humidity, optimalConditions.Humidity);
            
            // CO2 fitness
            float co2Fitness = CalculateCO2Fitness(conditions.CO2Level, optimalConditions.CO2Level);
            
            // Light fitness
            float lightFitness = CalculateLightFitness(conditions.LightIntensity, optimalConditions.LightIntensity);
            
            // Air circulation fitness
            float airflowFitness = CalculateAirflowFitness(conditions.AirVelocity, optimalConditions.AirVelocity);
            
            // Weighted average of all fitness factors
            float overallFitness = (tempFitness * 0.25f + 
                                  humidityFitness * 0.25f + 
                                  co2Fitness * 0.2f + 
                                  lightFitness * 0.2f + 
                                  airflowFitness * 0.1f);
            
            return Mathf.Clamp01(overallFitness);
        }
        
        /// <summary>
        /// Simulate atmospheric response to equipment changes
        /// </summary>
        public AtmosphericResponse SimulateEquipmentResponse(
            Vector3 position,
            EnvironmentalConditions currentConditions,
            object equipmentChange,
            float deltaTime)
        {
            if (!IsInitialized)
            {
                return new AtmosphericResponse
                {
                    NewConditions = currentConditions,
                    ResponseTime = 0f,
                    StabilityFactor = 1f
                };
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Calculate response to equipment changes
            var newConditions = CalculateAtmosphericConditions(position, currentConditions, equipmentChange);
            
            // Calculate response time based on atmospheric properties
            float responseTime = CalculateAtmosphericResponseTime(currentConditions, newConditions, equipmentChange);
            
            // Calculate stability factor
            float stabilityFactor = CalculateAtmosphericStability(currentConditions, newConditions);
            
            // Apply time-based transition
            var transitionedConditions = ApplyTimeBasedTransition(
                currentConditions, 
                newConditions, 
                deltaTime, 
                responseTime);
            
            var processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            return new AtmosphericResponse
            {
                NewConditions = transitionedConditions,
                ResponseTime = responseTime,
                StabilityFactor = stabilityFactor,
                ProcessingTimeMs = processingTime
            };
        }
        
        /// <summary>
        /// Calculate atmospheric turbulence and mixing effects
        /// </summary>
        public AtmosphericTurbulenceData CalculateAtmosphericTurbulence(
            Vector3 position,
            EnvironmentalConditions conditions,
            object equipmentState = null)
        {
            if (!IsInitialized)
            {
                return new AtmosphericTurbulenceData
                {
                    TurbulenceIntensity = 0.1f,
                    MixingEfficiency = 0.8f,
                    FlowPattern = AtmosphericFlowPattern.Laminar
                };
            }
            
            // Calculate turbulence intensity based on temperature gradients
            float temperatureGradient = CalculateTemperatureGradient(position, conditions);
            float turbulenceIntensity = Mathf.Clamp01(temperatureGradient * 0.1f + 0.05f);
            
            // Calculate mixing efficiency based on air velocity and turbulence
            float mixingEfficiency = CalculateMixingEfficiency(conditions.AirVelocity, turbulenceIntensity);
            
            // Determine flow pattern
            AtmosphericFlowPattern flowPattern = DetermineFlowPattern(conditions.AirVelocity, turbulenceIntensity);
            
            return new AtmosphericTurbulenceData
            {
                TurbulenceIntensity = turbulenceIntensity,
                MixingEfficiency = mixingEfficiency,
                FlowPattern = flowPattern,
                TemperatureGradient = temperatureGradient
            };
        }
        
        /// <summary>
        /// Get atmospheric simulation performance metrics
        /// </summary>
        public AtmosphericPerformanceMetrics GetPerformanceMetrics()
        {
            return new AtmosphericPerformanceMetrics
            {
                TotalCalculations = _calculationsPerformed,
                AverageProcessingTime = _calculationsPerformed > 0 ? (_totalProcessingTime / _calculationsPerformed) * 1000f : 0f,
                TrackedZones = _atmosphericStates.Count,
                AdvancedPhysicsEnabled = _enableAdvancedPhysics,
                CFDSimulationEnabled = _enableCFDSimulation,
                SimulationAccuracy = _simulationAccuracy
            };
        }
        
        /// <summary>
        /// Update atmospheric state for a zone
        /// </summary>
        public void UpdateAtmosphericState(string zoneId, EnvironmentalConditions conditions)
        {
            if (!IsInitialized) return;
            
            var atmosphericState = new AtmosphericState
            {
                ZoneId = zoneId,
                Conditions = conditions,
                LastUpdate = DateTime.Now,
                Turbulence = CalculateAtmosphericTurbulence(Vector3.zero, conditions),
                Fitness = CalculateEnvironmentalFitness(conditions, EnvironmentalConditions.CreateIndoorDefault())
            };
            
            _atmosphericStates[zoneId] = atmosphericState;
            _environmentalFitness[zoneId] = atmosphericState.Fitness;
        }
        
        /// <summary>
        /// Get atmospheric state for a zone
        /// </summary>
        public AtmosphericState GetAtmosphericState(string zoneId)
        {
            return _atmosphericStates.GetValueOrDefault(zoneId, new AtmosphericState { ZoneId = zoneId });
        }
        
        #region Private Calculation Methods
        
        private void InitializeAtmosphericCalculations()
        {
            _atmosphericStates.Clear();
            _environmentalFitness.Clear();
            _performanceMetrics = new AtmosphericPerformanceMetrics();
            _calculationsPerformed = 0;
            _totalProcessingTime = 0f;
        }
        
        private float CalculateTemperatureDistribution(Vector3 position, EnvironmentalConditions baseConditions, object equipmentState)
        {
            float baseTemperature = baseConditions.Temperature;
            
            // Apply equipment effects (simplified model)
            float equipmentEffect = CalculateEquipmentTemperatureEffect(equipmentState);
            
            // Apply atmospheric mixing
            float mixingEffect = CalculateTemperatureMixing(position, baseTemperature);
            
            // Apply convection effects
            float convectionEffect = CalculateTemperatureConvection(position, baseTemperature);
            
            return baseTemperature + equipmentEffect + mixingEffect + convectionEffect;
        }
        
        private float CalculateHumidityDistribution(Vector3 position, EnvironmentalConditions baseConditions, object equipmentState)
        {
            float baseHumidity = baseConditions.Humidity;
            
            // Apply equipment effects
            float equipmentEffect = CalculateEquipmentHumidityEffect(equipmentState);
            
            // Apply atmospheric mixing
            float mixingEffect = CalculateHumidityMixing(position, baseHumidity);
            
            // Apply evaporation/condensation effects
            float phaseChangeEffect = CalculateHumidityPhaseChange(baseConditions.Temperature, baseHumidity);
            
            return Mathf.Clamp(baseHumidity + equipmentEffect + mixingEffect + phaseChangeEffect, 0f, 100f);
        }
        
        private float CalculateCO2Distribution(Vector3 position, EnvironmentalConditions baseConditions, object equipmentState)
        {
            float baseCO2 = baseConditions.CO2Level;
            
            // Apply equipment effects (ventilation, etc.)
            float equipmentEffect = CalculateEquipmentCO2Effect(equipmentState);
            
            // Apply atmospheric mixing
            float mixingEffect = CalculateCO2Mixing(position, baseCO2);
            
            // Apply plant consumption effects (if in cultivation zone)
            float plantEffect = CalculatePlantCO2Consumption(position);
            
            return Mathf.Max(300f, baseCO2 + equipmentEffect + mixingEffect - plantEffect);
        }
        
        private float CalculateLightDistribution(Vector3 position, EnvironmentalConditions baseConditions, object equipmentState)
        {
            float baseLightIntensity = baseConditions.LightIntensity;
            
            // Apply equipment effects (lighting systems)
            float equipmentEffect = CalculateEquipmentLightEffect(equipmentState);
            
            // Apply atmospheric scattering and absorption
            float atmosphericEffect = CalculateAtmosphericLightEffect(baseConditions.Humidity, position);
            
            return Mathf.Max(0f, baseLightIntensity + equipmentEffect + atmosphericEffect);
        }
        
        private float CalculateAirflowVelocity(Vector3 position, EnvironmentalConditions baseConditions, object equipmentState)
        {
            float baseVelocity = baseConditions.AirVelocity;
            
            // Apply equipment effects (fans, HVAC)
            float equipmentEffect = CalculateEquipmentAirflowEffect(equipmentState);
            
            // Apply thermal convection
            float convectionEffect = CalculateConvectionAirflow(baseConditions.Temperature, position);
            
            return Mathf.Max(0f, baseVelocity + equipmentEffect + convectionEffect);
        }
        
        private void ApplyAtmosphericMixing(ref EnvironmentalConditions conditions, Vector3 position, EnvironmentalConditions baseConditions)
        {
            float mixingFactor = CalculateAtmosphericMixingFactor(conditions.AirVelocity, position);
            
            // Apply mixing to all atmospheric properties
            conditions.Temperature = Mathf.Lerp(conditions.Temperature, baseConditions.Temperature, mixingFactor * 0.1f);
            conditions.Humidity = Mathf.Lerp(conditions.Humidity, baseConditions.Humidity, mixingFactor * 0.1f);
            conditions.CO2Level = Mathf.Lerp(conditions.CO2Level, baseConditions.CO2Level, mixingFactor * 0.15f);
        }
        
        private void ApplyConvectionDiffusion(ref EnvironmentalConditions conditions, Vector3 position, object equipmentState)
        {
            // Apply convection effects based on temperature differences
            float convectionStrength = CalculateConvectionStrength(conditions.Temperature, position);
            
            // Apply diffusion effects for atmospheric properties
            float diffusionRate = CalculateDiffusionRate(conditions.AirVelocity, convectionStrength);
            
            // Modify conditions based on convection and diffusion
            conditions.Temperature += CalculateConvectionTemperatureChange(convectionStrength) * Time.deltaTime;
            conditions.Humidity += CalculateDiffusionHumidityChange(diffusionRate) * Time.deltaTime;
        }
        
        private float CalculateTemperatureFitness(float temperature, float optimalTemperature)
        {
            float tolerance = 2f; // 2°C tolerance
            float distance = Mathf.Abs(temperature - optimalTemperature);
            
            if (distance <= tolerance)
            {
                return 1f - (distance / tolerance) * 0.2f; // Max 20% fitness reduction within tolerance
            }
            
            // Outside tolerance - severe fitness penalty
            return Mathf.Max(0.1f, 0.8f - (distance - tolerance) * 0.1f);
        }
        
        private float CalculateHumidityFitness(float humidity, float optimalHumidity)
        {
            float tolerance = 10f; // 10% tolerance
            float distance = Mathf.Abs(humidity - optimalHumidity);
            
            if (distance <= tolerance)
            {
                return 1f - (distance / tolerance) * 0.15f; // Max 15% fitness reduction within tolerance
            }
            
            return Mathf.Max(0.2f, 0.85f - (distance - tolerance) * 0.05f);
        }
        
        private float CalculateCO2Fitness(float co2Level, float optimalCO2)
        {
            float tolerance = 200f; // 200 ppm tolerance
            float distance = Mathf.Abs(co2Level - optimalCO2);
            
            if (distance <= tolerance)
            {
                return 1f - (distance / tolerance) * 0.1f; // Max 10% fitness reduction within tolerance
            }
            
            return Mathf.Max(0.3f, 0.9f - (distance - tolerance) * 0.001f);
        }
        
        private float CalculateLightFitness(float lightIntensity, float optimalLight)
        {
            float tolerance = 100f; // 100 PPFD tolerance
            float distance = Mathf.Abs(lightIntensity - optimalLight);
            
            if (distance <= tolerance)
            {
                return 1f - (distance / tolerance) * 0.1f;
            }
            
            return Mathf.Max(0.2f, 0.9f - (distance - tolerance) * 0.002f);
        }
        
        private float CalculateAirflowFitness(float airVelocity, float optimalVelocity)
        {
            float tolerance = 0.2f; // 0.2 m/s tolerance
            float distance = Mathf.Abs(airVelocity - optimalVelocity);
            
            if (distance <= tolerance)
            {
                return 1f - (distance / tolerance) * 0.1f;
            }
            
            return Mathf.Max(0.5f, 0.9f - (distance - tolerance) * 0.5f);
        }
        
        private float CalculateAtmosphericResponseTime(EnvironmentalConditions current, EnvironmentalConditions target, object equipmentChange)
        {
            // Base response time (simplified model)
            float baseResponseTime = 120f; // 2 minutes
            
            // Modify based on magnitude of change
            float tempChange = Mathf.Abs(target.Temperature - current.Temperature);
            float humidityChange = Mathf.Abs(target.Humidity - current.Humidity);
            
            float changeMagnitude = (tempChange / 10f) + (humidityChange / 20f);
            
            return baseResponseTime * (1f + changeMagnitude * 0.5f);
        }
        
        private float CalculateAtmosphericStability(EnvironmentalConditions current, EnvironmentalConditions target)
        {
            // Calculate stability based on change rates
            float tempStability = 1f - Mathf.Clamp01(Mathf.Abs(target.Temperature - current.Temperature) / 20f);
            float humidityStability = 1f - Mathf.Clamp01(Mathf.Abs(target.Humidity - current.Humidity) / 40f);
            
            return (tempStability + humidityStability) * 0.5f;
        }
        
        private EnvironmentalConditions ApplyTimeBasedTransition(
            EnvironmentalConditions current, 
            EnvironmentalConditions target, 
            float deltaTime, 
            float responseTime)
        {
            float transitionRate = deltaTime / responseTime;
            
            return new EnvironmentalConditions
            {
                Temperature = Mathf.Lerp(current.Temperature, target.Temperature, transitionRate),
                Humidity = Mathf.Lerp(current.Humidity, target.Humidity, transitionRate),
                CO2Level = Mathf.Lerp(current.CO2Level, target.CO2Level, transitionRate * 0.5f), // CO2 changes slower
                LightIntensity = Mathf.Lerp(current.LightIntensity, target.LightIntensity, transitionRate * 2f), // Light changes faster
                AirVelocity = Mathf.Lerp(current.AirVelocity, target.AirVelocity, transitionRate * 1.5f)
            };
        }
        
        // Simplified helper methods for atmospheric calculations
        private float CalculateEquipmentTemperatureEffect(object equipmentState) => 0f; // Placeholder
        private float CalculateEquipmentHumidityEffect(object equipmentState) => 0f; // Placeholder
        private float CalculateEquipmentCO2Effect(object equipmentState) => 0f; // Placeholder
        private float CalculateEquipmentLightEffect(object equipmentState) => 0f; // Placeholder
        private float CalculateEquipmentAirflowEffect(object equipmentState) => 0f; // Placeholder
        private float CalculateTemperatureMixing(Vector3 position, float baseTemperature) => 0f; // Placeholder
        private float CalculateHumidityMixing(Vector3 position, float baseHumidity) => 0f; // Placeholder
        private float CalculateCO2Mixing(Vector3 position, float baseCO2) => 0f; // Placeholder
        private float CalculateTemperatureConvection(Vector3 position, float baseTemperature) => 0f; // Placeholder
        private float CalculateHumidityPhaseChange(float temperature, float humidity) => 0f; // Placeholder
        private float CalculatePlantCO2Consumption(Vector3 position) => 0f; // Placeholder
        private float CalculateAtmosphericLightEffect(float humidity, Vector3 position) => 0f; // Placeholder
        private float CalculateConvectionAirflow(float temperature, Vector3 position) => 0f; // Placeholder
        private float CalculateAtmosphericMixingFactor(float airVelocity, Vector3 position) => 0.1f; // Placeholder
        private float CalculateConvectionStrength(float temperature, Vector3 position) => 0.1f; // Placeholder
        private float CalculateDiffusionRate(float airVelocity, float convectionStrength) => 0.1f; // Placeholder
        private float CalculateConvectionTemperatureChange(float convectionStrength) => 0f; // Placeholder
        private float CalculateDiffusionHumidityChange(float diffusionRate) => 0f; // Placeholder
        private float CalculateTemperatureGradient(Vector3 position, EnvironmentalConditions conditions) => 0.1f; // Placeholder
        private float CalculateMixingEfficiency(float airVelocity, float turbulenceIntensity) => Mathf.Clamp01(airVelocity * 0.5f + turbulenceIntensity * 0.3f + 0.2f);
        private AtmosphericFlowPattern DetermineFlowPattern(float airVelocity, float turbulenceIntensity)
        {
            if (turbulenceIntensity > 0.3f) return AtmosphericFlowPattern.Turbulent;
            if (airVelocity > 1f) return AtmosphericFlowPattern.Transitional;
            return AtmosphericFlowPattern.Laminar;
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    /// <summary>
    /// Atmospheric simulation response data
    /// </summary>
    [System.Serializable]
    public class AtmosphericResponse
    {
        public EnvironmentalConditions NewConditions;
        public float ResponseTime;
        public float StabilityFactor;
        public float ProcessingTimeMs;
    }
    
    /// <summary>
    /// Atmospheric state data for a zone
    /// </summary>
    [System.Serializable]
    public class AtmosphericState
    {
        public string ZoneId;
        public EnvironmentalConditions Conditions;
        public AtmosphericTurbulenceData Turbulence;
        public float Fitness;
        public DateTime LastUpdate;
    }
    
    /// <summary>
    /// Atmospheric turbulence and flow pattern data
    /// </summary>
    [System.Serializable]
    public class AtmosphericTurbulenceData
    {
        public float TurbulenceIntensity;
        public float MixingEfficiency;
        public AtmosphericFlowPattern FlowPattern;
        public float TemperatureGradient;
    }
    
    /// <summary>
    /// Atmospheric simulation performance metrics
    /// </summary>
    [System.Serializable]
    public class AtmosphericPerformanceMetrics
    {
        public int TotalCalculations;
        public float AverageProcessingTime; // milliseconds
        public int TrackedZones;
        public bool AdvancedPhysicsEnabled;
        public bool CFDSimulationEnabled;
        public float SimulationAccuracy;
        
        public override string ToString()
        {
            return $"Atmospheric Performance: {TotalCalculations} calcs, {AverageProcessingTime:F2}ms avg, {TrackedZones} zones";
        }
    }
    
    /// <summary>
    /// Atmospheric flow pattern types for simulation
    /// </summary>
    public enum AtmosphericFlowPattern
    {
        Laminar,
        Transitional,
        Turbulent
    }
    
    #endregion
}