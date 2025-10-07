using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// BASIC: Simple equipment types for Project Chimera's equipment system.
    /// Focuses on essential equipment characteristics without complex reliability profiles.
    /// </summary>
    public static class EquipmentTypes
    {
        /// <summary>
        /// Basic equipment type definitions
        /// </summary>
        public static readonly Dictionary<Data.Equipment.EquipmentType, EquipmentProfile> EquipmentProfiles = new Dictionary<Data.Equipment.EquipmentType, EquipmentProfile>
        {
            // Lighting Equipment
            [Data.Equipment.EquipmentType.LED_Light] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.LED_Light,
                BaseLifespan = 5f, // years
                BaseEfficiency = 0.92f,
                BaseCost = 250f,
                MaintenanceInterval = 12f, // months
                PowerConsumption = 120f, // watts
                Description = "High-efficiency LED grow light"
            },

            [Data.Equipment.EquipmentType.HPS_Light] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.HPS_Light,
                BaseLifespan = 2f,
                BaseEfficiency = 0.75f,
                BaseCost = 180f,
                MaintenanceInterval = 6f,
                PowerConsumption = 400f,
                Description = "High Pressure Sodium grow light"
            },

            [Data.Equipment.EquipmentType.GrowLight] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.GrowLight,
                BaseLifespan = 3f, // years
                BaseEfficiency = 0.9f,
                BaseCost = 200f,
                MaintenanceInterval = 6f, // months
                PowerConsumption = 150f, // watts
                Description = "Generic grow light for plant cultivation"
            },

            // Ventilation Equipment
            [Data.Equipment.EquipmentType.Exhaust_Fan] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Exhaust_Fan,
                BaseLifespan = 5f,
                BaseEfficiency = 0.85f,
                BaseCost = 300f,
                MaintenanceInterval = 12f,
                PowerConsumption = 100f,
                Description = "Air circulation and ventilation system"
            },

            [Data.Equipment.EquipmentType.Intake_Fan] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Intake_Fan,
                BaseLifespan = 5f,
                BaseEfficiency = 0.85f,
                BaseCost = 280f,
                MaintenanceInterval = 12f,
                PowerConsumption = 90f,
                Description = "Air intake fan for ventilation system"
            },

            [Data.Equipment.EquipmentType.Air_Circulator] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Air_Circulator,
                BaseLifespan = 4f,
                BaseEfficiency = 0.82f,
                BaseCost = 150f,
                MaintenanceInterval = 6f,
                PowerConsumption = 60f,
                Description = "Air circulation fan for improved airflow"
            },

            // Irrigation Equipment
            [Data.Equipment.EquipmentType.Watering_System] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Watering_System,
                BaseLifespan = 2.5f,
                BaseEfficiency = 0.8f,
                BaseCost = 150f,
                MaintenanceInterval = 3f,
                PowerConsumption = 50f,
                Description = "Water delivery and irrigation system"
            },

            [Data.Equipment.EquipmentType.Drip_System] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Drip_System,
                BaseLifespan = 3f,
                BaseEfficiency = 0.88f,
                BaseCost = 120f,
                MaintenanceInterval = 2f,
                PowerConsumption = 25f,
                Description = "Precision drip irrigation system"
            },

            // Climate Control Equipment
            [Data.Equipment.EquipmentType.Climate_Controller] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Climate_Controller,
                BaseLifespan = 4f,
                BaseEfficiency = 0.75f,
                BaseCost = 500f,
                MaintenanceInterval = 6f,
                PowerConsumption = 200f,
                Description = "Temperature and humidity control system"
            },

            [Data.Equipment.EquipmentType.Environmental_Controller] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Environmental_Controller,
                BaseLifespan = 4f,
                BaseEfficiency = 0.78f,
                BaseCost = 550f,
                MaintenanceInterval = 6f,
                PowerConsumption = 180f,
                Description = "Advanced environmental control system"
            },

            // Storage Equipment
            [Data.Equipment.EquipmentType.Reservoir] = new EquipmentProfile
            {
                Type = Data.Equipment.EquipmentType.Reservoir,
                BaseLifespan = 10f,
                BaseEfficiency = 0.95f,
                BaseCost = 100f,
                MaintenanceInterval = 24f,
                PowerConsumption = 10f,
                Description = "Water storage reservoir"
            }
        };

        /// <summary>
        /// Get equipment profile
        /// </summary>
        public static EquipmentProfile GetProfile(Data.Equipment.EquipmentType type)
        {
            return EquipmentProfiles.TryGetValue(type, out var profile) ? profile : null;
        }

        /// <summary>
        /// Provide a synthesized reliability profile for an equipment type
        /// to support malfunction analysis APIs.
        /// </summary>
        public static EquipmentReliabilityProfile GetReliabilityProfile(Data.Equipment.EquipmentType type)
        {
            var baseProfile = GetProfile(type);
            var reliability = new EquipmentReliabilityProfile
            {
                Type = type,
                MeanTimeBetweenFailures = baseProfile != null ? baseProfile.BaseLifespan * 365f * 24f : 4380f,
                AverageLifespan = baseProfile != null ? baseProfile.BaseLifespan : 5f,
                FailureRate = 0.002f,
                WearProgressionRate = 0.02f,
                CriticalWearThreshold = 0.8f,
                CommonFailureModes = new Dictionary<MalfunctionType, float>
                {
                    { MalfunctionType.WearAndTear, 1f },
                    { MalfunctionType.MechanicalFailure, 0.5f },
                    { MalfunctionType.ElectricalFailure, 0.5f },
                    { MalfunctionType.SensorDrift, 0.25f }
                },
                EnvironmentalSensitivity = new Dictionary<string, float>
                {
                    { "Temperature", 1f },
                    { "Humidity", 1f }
                }
            };

            return reliability;
        }

        /// <summary>
        /// Get all equipment types
        /// </summary>
        public static List<Data.Equipment.EquipmentType> GetAllTypes()
        {
            return new List<Data.Equipment.EquipmentType>(EquipmentProfiles.Keys);
        }

        /// <summary>
        /// Get equipment by cost range
        /// </summary>
        public static List<Data.Equipment.EquipmentType> GetByCostRange(float minCost, float maxCost)
        {
            var result = new List<Data.Equipment.EquipmentType>();
            foreach (var kvp in EquipmentProfiles)
            {
                if (kvp.Value.BaseCost >= minCost && kvp.Value.BaseCost <= maxCost)
                {
                    result.Add(kvp.Key);
                }
            }
            return result;
        }

        /// <summary>
        /// Get equipment by power consumption
        /// </summary>
        public static List<Data.Equipment.EquipmentType> GetByPowerConsumption(float maxPower)
        {
            var result = new List<Data.Equipment.EquipmentType>();
            foreach (var kvp in EquipmentProfiles)
            {
                if (kvp.Value.PowerConsumption <= maxPower)
                {
                    result.Add(kvp.Key);
                }
            }
            return result;
        }

        /// <summary>
        /// Calculate degradation rate for equipment
        /// </summary>
        public static float CalculateDegradationRate(Data.Equipment.EquipmentType type, float age)
        {
            var profile = GetProfile(type);
            if (profile == null) return 0.01f; // Default degradation

            // Simple degradation based on age and lifespan
            float ageRatio = age / profile.BaseLifespan;
            return Mathf.Lerp(0.005f, 0.05f, ageRatio); // 0.5% to 5% degradation per year
        }

        /// <summary>
        /// Calculate maintenance cost
        /// </summary>
        public static float CalculateMaintenanceCost(Data.Equipment.EquipmentType type)
        {
            var profile = GetProfile(type);
            if (profile == null) return 10f;

            return profile.BaseCost * 0.1f; // 10% of base cost
        }

        /// <summary>
        /// Get equipment statistics
        /// </summary>
        public static EquipmentStatistics GetStatistics()
        {
            float totalCost = 0f;
            float totalPower = 0f;
            float avgLifespan = 0f;

            foreach (var profile in EquipmentProfiles.Values)
            {
                totalCost += profile.BaseCost;
                totalPower += profile.PowerConsumption;
                avgLifespan += profile.BaseLifespan;
            }

            avgLifespan /= EquipmentProfiles.Count;

            return new EquipmentStatistics
            {
                TotalEquipmentTypes = EquipmentProfiles.Count,
                AverageCost = totalCost / EquipmentProfiles.Count,
                TotalPowerConsumption = totalPower,
                AverageLifespan = avgLifespan
            };
        }
    }

    /// <summary>
    /// Equipment type mapping to Data layer enum
    /// </summary>
    public static class EquipmentTypeMapper
    {
        public static readonly Dictionary<Data.Equipment.EquipmentType, string> TypeCategories = new Dictionary<Data.Equipment.EquipmentType, string>
        {
            [Data.Equipment.EquipmentType.LED_Light] = "Light",
            [Data.Equipment.EquipmentType.HPS_Light] = "Light",
            [Data.Equipment.EquipmentType.GrowLight] = "Light",
            [Data.Equipment.EquipmentType.Exhaust_Fan] = "Ventilation",
            [Data.Equipment.EquipmentType.Intake_Fan] = "Ventilation",
            [Data.Equipment.EquipmentType.Air_Circulator] = "Ventilation",
            [Data.Equipment.EquipmentType.Watering_System] = "Irrigation",
            [Data.Equipment.EquipmentType.Drip_System] = "Irrigation",
            [Data.Equipment.EquipmentType.Reservoir] = "Storage",
            [Data.Equipment.EquipmentType.Climate_Controller] = "ClimateControl",
            [Data.Equipment.EquipmentType.Environmental_Controller] = "ClimateControl"
        };
    }

    /// <summary>
    /// Basic equipment profile
    /// </summary>
    [System.Serializable]
    public class EquipmentProfile
    {
        public Data.Equipment.EquipmentType Type;
        public float BaseLifespan; // years
        public float BaseEfficiency; // 0-1
        public float BaseCost;
        public float MaintenanceInterval; // months
        public float PowerConsumption; // watts
        public string Description;
    }

    /// <summary>
    /// Equipment statistics
    /// </summary>
    [System.Serializable]
    public struct EquipmentStatistics
    {
        public int TotalEquipmentTypes;
        public float AverageCost;
        public float TotalPowerConsumption;
        public float AverageLifespan;
    }
}
