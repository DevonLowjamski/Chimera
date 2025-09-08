using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Control data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>



    [System.Serializable]
    public class TemperatureControlSettings
    {
        [Range(15f, 35f)] public float MinTemperature = 18f;
        [Range(15f, 35f)] public float MaxTemperature = 30f;
        [Range(0.1f, 5f)] public float TemperatureRampRate = 1f; // Degrees per hour
        public bool EnableNightTimeSetback = true;
        [Range(0f, 10f)] public float NightTimeSetback = 3f;
        public bool EnableSeasonalAdjustment = false;
    }



    [System.Serializable]
    public class HumidityControlSettings
    {
        [Range(20f, 80f)] public float MinHumidity = 40f;
        [Range(20f, 80f)] public float MaxHumidity = 70f;
        [Range(1f, 20f)] public float HumidityRampRate = 5f; // Percent per hour
        public bool EnableDeadband = true;
        [Range(1f, 10f)] public float DeadbandWidth = 5f;
        public bool EnableCondensationPrevention = true;
    }



    [System.Serializable]
    public class AirflowControlSettings
    {
        [Range(0.1f, 2f)] public float MinAirVelocity = 0.1f;
        [Range(0.1f, 2f)] public float MaxAirVelocity = 1.5f;
        [Range(0.5f, 10f)] public float AirChangesPerHour = 4f;
        public bool EnableVariableAirflow = true;
        public bool EnableCO2BasedControl = true;
        [Range(300f, 1500f)] public float CO2Setpoint = 800f;
    }



    public enum ControlMode
    {
        Automatic,
        Manual,
        Override,
        Emergency
    }

    /// <summary>
    /// PID Controller implementation for HVAC control systems
    /// </summary>
    [System.Serializable]
    public class PIDController
    {
        public float Kp { get; set; }
        public float Ki { get; set; }
        public float Kd { get; set; }
        
        private float _integral;
        private float _previousError;
        private bool _firstRun = true;
        
        public PIDController(float kp, float ki, float kd)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Reset();
        }
        
        public PIDController() : this(1.0f, 0.1f, 0.01f) { }
        
        public void Reset()
        {
            _integral = 0f;
            _previousError = 0f;
            _firstRun = true;
        }
        
        public float Calculate(float setpoint, float processVariable, float deltaTime)
        {
            float error = setpoint - processVariable;
            
            if (_firstRun)
            {
                _previousError = error;
                _firstRun = false;
            }
            
            // Proportional term
            float proportional = Kp * error;
            
            // Integral term
            _integral += error * deltaTime;
            float integral = Ki * _integral;
            
            // Derivative term
            float derivative = Kd * (error - _previousError) / deltaTime;
            
            _previousError = error;
            
            return proportional + integral + derivative;
        }
    }
}
