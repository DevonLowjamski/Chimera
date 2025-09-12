using UnityEngine;
using System.Collections.Generic;

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
        public static readonly Dictionary<EquipmentType, EquipmentProfile> EquipmentProfiles = new Dictionary<EquipmentType, EquipmentProfile>
        {
            [EquipmentType.Light] = new EquipmentProfile
            {
                Type = EquipmentType.Light,
                BaseLifespan = 3f, // years
                BaseEfficiency = 0.9f,
                BaseCost = 200f,
                MaintenanceInterval = 6f, // months
                PowerConsumption = 150f, // watts
                Description = "LED grow light for plant cultivation"
            },

            [EquipmentType.Ventilation] = new EquipmentProfile
            {
                Type = EquipmentType.Ventilation,
                BaseLifespan = 5f,
                BaseEfficiency = 0.85f,
                BaseCost = 300f,
                MaintenanceInterval = 12f,
                PowerConsumption = 100f,
                Description = "Air circulation and ventilation system"
            },

            [EquipmentType.Irrigation] = new EquipmentProfile
            {
                Type = EquipmentType.Irrigation,
                BaseLifespan = 2.5f,
                BaseEfficiency = 0.8f,
                BaseCost = 150f,
                MaintenanceInterval = 3f,
                PowerConsumption = 50f,
                Description = "Water delivery and irrigation system"
            },

            [EquipmentType.ClimateControl] = new EquipmentProfile
            {
                Type = EquipmentType.ClimateControl,
                BaseLifespan = 4f,
                BaseEfficiency = 0.75f,
                BaseCost = 500f,
                MaintenanceInterval = 6f,
                PowerConsumption = 200f,
                Description = "Temperature and humidity control system"
            },

            [EquipmentType.Storage] = new EquipmentProfile
            {
                Type = EquipmentType.Storage,
                BaseLifespan = 10f,
                BaseEfficiency = 0.95f,
                BaseCost = 100f,
                MaintenanceInterval = 24f,
                PowerConsumption = 10f,
                Description = "Storage container for equipment and supplies"
            }
        };

        /// <summary>
        /// Get equipment profile
        /// </summary>
        public static EquipmentProfile GetProfile(EquipmentType type)
        {
            return EquipmentProfiles.TryGetValue(type, out var profile) ? profile : null;
        }

        /// <summary>
        /// Get all equipment types
        /// </summary>
        public static List<EquipmentType> GetAllTypes()
        {
            return new List<EquipmentType>(EquipmentProfiles.Keys);
        }

        /// <summary>
        /// Get equipment by cost range
        /// </summary>
        public static List<EquipmentType> GetByCostRange(float minCost, float maxCost)
        {
            var result = new List<EquipmentType>();
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
        public static List<EquipmentType> GetByPowerConsumption(float maxPower)
        {
            var result = new List<EquipmentType>();
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
        public static float CalculateDegradationRate(EquipmentType type, float age)
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
        public static float CalculateMaintenanceCost(EquipmentType type)
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
    /// Equipment types
    /// </summary>
    public enum EquipmentType
    {
        Light,
        Ventilation,
        Irrigation,
        ClimateControl,
        Storage
    }

    /// <summary>
    /// Basic equipment profile
    /// </summary>
    [System.Serializable]
    public class EquipmentProfile
    {
        public EquipmentType Type;
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
