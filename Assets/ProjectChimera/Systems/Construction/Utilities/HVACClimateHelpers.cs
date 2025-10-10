using System;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Simulation.HVAC;

namespace ProjectChimera.Systems.Construction.Utilities
{
    /// <summary>
    /// Helper utilities for HVAC climate calculations and simulations.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class HVACClimateHelpers
    {
        /// <summary>
        /// Updates zone climate based on equipment and targets.
        /// Simplified PID-style control for Phase 1.
        /// </summary>
        public static void UpdateZoneClimate(HVACZone zone, float temperatureChangeRate,
            float humidityChangeRate, float updateIntervalSeconds,
            Action<string, string> onClimateAlert)
        {
            var current = zone.CurrentConditions;
            var target = zone.TargetConditions;

            // Temperature control
            float tempDelta = target.Temperature - current.Temperature;
            if (Mathf.Abs(tempDelta) > zone.ControlParameters.TemperatureTolerance)
            {
                // Check if we have appropriate equipment
                bool hasCooling = zone.ZoneEquipment.Any(e => e.EquipmentType == "AirConditioner" && e.IsActive);
                bool hasHeating = zone.ZoneEquipment.Any(e => e.EquipmentType == "Heater" && e.IsActive);

                if (tempDelta < 0 && hasCooling)
                {
                    // Too hot, cool down
                    current.Temperature -= temperatureChangeRate * (updateIntervalSeconds / 60f);
                    zone.ZoneStatus = HVACZoneStatus.Cooling;
                }
                else if (tempDelta > 0 && hasHeating)
                {
                    // Too cold, heat up
                    current.Temperature += temperatureChangeRate * (updateIntervalSeconds / 60f);
                    zone.ZoneStatus = HVACZoneStatus.Heating;
                }
                else
                {
                    // Missing equipment!
                    string alert = tempDelta < 0
                        ? $"⚠️ {zone.ZoneName} needs cooling equipment (AC)"
                        : $"⚠️ {zone.ZoneName} needs heating equipment";
                    onClimateAlert?.Invoke(zone.ZoneId, alert);
                }
            }
            else
            {
                zone.ZoneStatus = HVACZoneStatus.Active;
            }

            // Humidity control
            float humidityDelta = target.Humidity - current.Humidity;
            if (Mathf.Abs(humidityDelta) > zone.ControlParameters.HumidityTolerance)
            {
                bool hasHumidifier = zone.ZoneEquipment.Any(e => e.EquipmentType == "Humidifier" && e.IsActive);
                bool hasDehumidifier = zone.ZoneEquipment.Any(e => e.EquipmentType == "Dehumidifier" && e.IsActive);

                if (humidityDelta > 0 && hasHumidifier)
                {
                    // Too dry, add humidity
                    current.Humidity += humidityChangeRate * (updateIntervalSeconds / 60f);
                }
                else if (humidityDelta < 0 && hasDehumidifier)
                {
                    // Too humid, remove humidity
                    current.Humidity -= humidityChangeRate * (updateIntervalSeconds / 60f);
                }
            }

            // Clamp values to realistic ranges
            current.Temperature = Mathf.Clamp(current.Temperature, 50f, 95f);
            current.Humidity = Mathf.Clamp(current.Humidity, 20f, 90f);

            zone.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Calculates VPD (Vapor Pressure Deficit) for given conditions.
        /// GAMEPLAY: Advanced players optimize VPD for maximum plant health.
        /// </summary>
        public static float CalculateVPD(float temperatureFahrenheit, float humidityPercent)
        {
            // Convert Fahrenheit to Celsius for VPD calculation
            float tempC = (temperatureFahrenheit - 32f) * 5f / 9f;

            // Simplified VPD calculation
            // VPD (kPa) = SVP * (1 - RH/100)
            // SVP = Saturated Vapor Pressure (exponential function of temp)
            float svp = 0.6108f * Mathf.Exp((17.27f * tempC) / (tempC + 237.3f));
            float vpd = svp * (1f - humidityPercent / 100f);

            return vpd;
        }

        /// <summary>
        /// Checks if zone is within optimal ranges.
        /// </summary>
        public static bool IsZoneOptimal(HVACZone zone)
        {
            float tempDiff = Mathf.Abs(zone.CurrentConditions.Temperature - zone.TargetConditions.Temperature);
            float humidityDiff = Mathf.Abs(zone.CurrentConditions.Humidity - zone.TargetConditions.Humidity);

            return tempDiff <= zone.ControlParameters.TemperatureTolerance &&
                   humidityDiff <= zone.ControlParameters.HumidityTolerance;
        }

        /// <summary>
        /// Generates zone climate summary for UI display.
        /// </summary>
        public static ZoneClimateSummary GenerateZoneSummary(HVACZone zone)
        {
            float vpd = CalculateVPD(zone.CurrentConditions.Temperature, zone.CurrentConditions.Humidity);

            return new ZoneClimateSummary
            {
                ZoneId = zone.ZoneId,
                ZoneName = zone.ZoneName,
                CurrentTemp = zone.CurrentConditions.Temperature,
                TargetTemp = zone.TargetConditions.Temperature,
                CurrentHumidity = zone.CurrentConditions.Humidity,
                TargetHumidity = zone.TargetConditions.Humidity,
                CurrentCO2 = zone.CurrentConditions.CO2Level,
                TargetCO2 = zone.TargetConditions.CO2Level,
                VPD = vpd,
                Status = zone.ZoneStatus,
                EquipmentCount = zone.ZoneEquipment.Count,
                IsOptimal = IsZoneOptimal(zone)
            };
        }
    }
}
