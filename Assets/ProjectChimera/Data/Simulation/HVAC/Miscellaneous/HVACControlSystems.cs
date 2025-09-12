using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// Core HVAC control systems and parameters
    /// Handles PID controllers, VPD optimization, and control loops
    /// </summary>

    [System.Serializable]
    public class PIDControllerSettings
    {
        [Header("Temperature Control")]
        [Range(0.1f, 2f)] public float TemperatureKp = 0.8f;
        [Range(0.01f, 0.5f)] public float TemperatureKi = 0.1f;
        [Range(0.01f, 0.2f)] public float TemperatureKd = 0.05f;

        [Header("Humidity Control")]
        [Range(0.1f, 2f)] public float HumidityKp = 0.6f;
        [Range(0.01f, 0.5f)] public float HumidityKi = 0.08f;
        [Range(0.01f, 0.2f)] public float HumidityKd = 0.03f;

        [Header("Airflow Control")]
        [Range(0.1f, 2f)] public float AirflowKp = 0.9f;
        [Range(0.01f, 0.5f)] public float AirflowKi = 0.12f;
        [Range(0.01f, 0.2f)] public float AirflowKd = 0.04f;

        /// <summary>
        /// Validate PID parameters
        /// </summary>
        public bool ValidateParameters()
        {
            return TemperatureKp >= 0.1f && TemperatureKp <= 2f &&
                   HumidityKp >= 0.1f && HumidityKp <= 2f &&
                   AirflowKp >= 0.1f && AirflowKp <= 2f &&
                   TemperatureKi >= 0.01f && TemperatureKi <= 0.5f &&
                   HumidityKi >= 0.01f && HumidityKi <= 0.5f &&
                   AirflowKi >= 0.01f && AirflowKi <= 0.5f;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void ResetToDefaults()
        {
            TemperatureKp = 0.8f; TemperatureKi = 0.1f; TemperatureKd = 0.05f;
            HumidityKp = 0.6f; HumidityKi = 0.08f; HumidityKd = 0.03f;
            AirflowKp = 0.9f; AirflowKi = 0.12f; AirflowKd = 0.04f;
        }
    }

    [System.Serializable]
    public class VPDOptimizationSettings
    {
        [Header("VPD Control")]
        [Range(0.4f, 1.6f)] public float TargetVPD = 1.0f;
        [Range(0.1f, 0.5f)] public float VPDTolerance = 0.2f;

        [Header("Dynamic Features")]
        public bool EnableDynamicVPD = true;
        public bool EnableGrowthStageAdjustment = true;

        [Header("Schedule")]
        public List<VPDSchedulePoint> VPDSchedule = new List<VPDSchedulePoint>();

        /// <summary>
        /// Get VPD target for specific hour
        /// </summary>
        public float GetVPDTargetForHour(int hour)
        {
            var point = VPDSchedule.Find(p => p.Hour == hour);
            return point != null ? point.TargetVPD : TargetVPD;
        }

        /// <summary>
        /// Get VPD target for growth stage
        /// </summary>
        public float GetVPDTargetForStage(PlantGrowthStage stage)
        {
            var points = VPDSchedule.FindAll(p => p.GrowthStage == stage);
            if (points.Count == 0) return TargetVPD;

            // Return average of all points for this stage
            float total = 0f;
            foreach (var point in points) total += point.TargetVPD;
            return total / points.Count;
        }
    }

    [System.Serializable]
    public class VPDSchedulePoint
    {
        [Range(0, 24)] public int Hour;
        [Range(0.4f, 1.6f)] public float TargetVPD;
        public PlantGrowthStage GrowthStage;

        /// <summary>
        /// Validate schedule point
        /// </summary>
        public bool IsValid()
        {
            return Hour >= 0 && Hour <= 24 &&
                   TargetVPD >= 0.4f && TargetVPD <= 1.6f;
        }
    }

    [System.Serializable]
    public class HVACControlParameters
    {
        [Header("Temperature Limits")]
        [Range(15f, 35f)] public float MinTemperature = 18f;
        [Range(20f, 40f)] public float MaxTemperature = 32f;
        [Range(20f, 30f)] public float TargetTemperature = 25f;

        [Header("Humidity Limits")]
        [Range(30f, 70f)] public float MinHumidity = 40f;
        [Range(50f, 90f)] public float MaxHumidity = 80f;
        [Range(40f, 80f)] public float TargetHumidity = 60f;

        [Header("Airflow Settings")]
        [Range(0f, 1f)] public float MinAirflowRate = 0.1f;
        [Range(0.5f, 2f)] public float MaxAirflowRate = 1.5f;
        [Range(0.1f, 1f)] public float TargetAirflowRate = 0.8f;

        [Header("Control Settings")]
        public bool EnableAutomaticControl = true;
        public float ControlUpdateInterval = 5f; // seconds
        public float DeadbandTolerance = 0.5f;

        /// <summary>
        /// Check if environmental values are within acceptable ranges
        /// </summary>
        public bool IsWithinLimits(float temperature, float humidity, float airflow)
        {
            return temperature >= MinTemperature && temperature <= MaxTemperature &&
                   humidity >= MinHumidity && humidity <= MaxHumidity &&
                   airflow >= MinAirflowRate && airflow <= MaxAirflowRate;
        }

        /// <summary>
        /// Calculate control error for temperature
        /// </summary>
        public float GetTemperatureError(float currentTemp)
        {
            return TargetTemperature - currentTemp;
        }

        /// <summary>
        /// Calculate control error for humidity
        /// </summary>
        public float GetHumidityError(float currentHumidity)
        {
            return TargetHumidity - currentHumidity;
        }
    }

    [System.Serializable]
    public class NightModeSettings
    {
        [Header("Night Mode Control")]
        public bool EnableNightMode = true;
        [Range(22, 6)] public int NightStartHour = 22; // 10 PM
        [Range(6, 10)] public int NightEndHour = 6;    // 6 AM

        [Header("Night Mode Targets")]
        [Range(15f, 25f)] public float NightTemperatureTarget = 20f;
        [Range(50f, 70f)] public float NightHumidityTarget = 55f;
        [Range(0.1f, 0.5f)] public float NightAirflowTarget = 0.3f;

        [Header("Energy Saving")]
        public bool EnableEnergySaving = true;
        [Range(0.1f, 0.8f)] public float EnergySavingMultiplier = 0.6f;

        /// <summary>
        /// Check if current time is within night mode hours
        /// </summary>
        public bool IsNightTime(DateTime currentTime)
        {
            if (!EnableNightMode) return false;

            int currentHour = currentTime.Hour;
            if (NightStartHour > NightEndHour)
            {
                // Night spans midnight (e.g., 22:00 to 06:00)
                return currentHour >= NightStartHour || currentHour <= NightEndHour;
            }
            else
            {
                // Night within same day (e.g., 01:00 to 05:00)
                return currentHour >= NightStartHour && currentHour <= NightEndHour;
            }
        }

        /// <summary>
        /// Get adjusted target based on night mode
        /// </summary>
        public float GetNightAdjustedTarget(float dayTarget, float nightTarget, DateTime currentTime)
        {
            return IsNightTime(currentTime) ? nightTarget : dayTarget;
        }
    }

    [System.Serializable]
    public class HVACControlLoop
    {
        public string ControlLoopId;
        public string ZoneId;
        public HVACControlType ControlType;
        public ControlStrategy Strategy;

        [Header("Current State")]
        public float CurrentValue;
        public float TargetValue;
        public float Error;
        public float Integral;
        public float Derivative;

        [Header("PID Gains")]
        public float Kp;
        public float Ki;
        public float Kd;

        [Header("Control Output")]
        public float Output;
        public float MinOutput = 0f;
        public float MaxOutput = 100f;

        [Header("Performance")]
        public float LastUpdateTime;
        public bool IsActive = true;
        public bool IsInDeadband = false;

        /// <summary>
        /// Update PID control loop
        /// </summary>
        public float UpdateControl(float currentValue, float targetValue, float deltaTime)
        {
            if (!IsActive) return Output;

            // Calculate error
            Error = targetValue - currentValue;

            // Check if in deadband
            IsInDeadband = Mathf.Abs(Error) < 0.1f;

            // Update integral (prevent windup)
            Integral += Error * deltaTime;
            Integral = Mathf.Clamp(Integral, -10f, 10f);

            // Calculate derivative
            float previousError = Error - Derivative * deltaTime;
            Derivative = (Error - previousError) / deltaTime;

            // Calculate PID output
            float pidOutput = Kp * Error + Ki * Integral + Kd * Derivative;

            // Apply output limits
            Output = Mathf.Clamp(pidOutput, MinOutput, MaxOutput);

            CurrentValue = currentValue;
            TargetValue = targetValue;
            LastUpdateTime = Time.time;

            return Output;
        }

        /// <summary>
        /// Reset control loop
        /// </summary>
        public void Reset()
        {
            Error = 0f;
            Integral = 0f;
            Derivative = 0f;
            Output = 0f;
            IsInDeadband = false;
        }

        /// <summary>
        /// Get control loop status
        /// </summary>
        public string GetStatus()
        {
            return $"Loop {ControlLoopId}: Error={Error:F2}, Output={Output:F1}%, Active={IsActive}";
        }
    }
}
