using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Data.Facilities;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Basic plant instance data structure
    /// Represents a single plant in the cultivation system
    /// </summary>
    [System.Serializable]
    public class PlantInstance
    {
        [Header("Basic Information")]
        public string PlantId;
        public string StrainName;
        public string Species;
        public ProjectChimera.Data.Shared.PlantGrowthStage GrowthStage;

        [Header("Location")]
        public Vector3 Position;
        public string ZoneId;
        public int GridX;
        public int GridY;

        [Header("Growth Data")]
        public float Age; // in days
        public float Height; // in cm
        public float Biomass; // in grams
        public float Health; // 0-1 scale
        public float Stress; // 0-1 scale

        [Header("Resource Data")]
        public float WaterLevel; // 0-1 scale
        public float NutrientLevel; // 0-1 scale
        public float EnergyLevel; // 0-1 scale

        [Header("Environmental")]
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float CO2Level;

        [Header("Maintenance")]
        public DateTime LastWatering;
        public DateTime LastFeeding;
        public DateTime LastPruning;
        public DateTime PlantedDate;
        public DateTime HarvestDate;

        [Header("Status")]
        public bool IsAlive;
        public bool IsFlowering;
        public bool HasPests;
        public bool HasDisease;

        /// <summary>
        /// Get plant age in days
        /// </summary>
        public float GetAgeInDays()
        {
            return (float)(DateTime.Now - PlantedDate).TotalDays;
        }

        /// <summary>
        /// Check if plant needs watering
        /// </summary>
        public bool NeedsWatering(float threshold = 0.3f)
        {
            return WaterLevel < threshold;
        }

        /// <summary>
        /// Check if plant needs nutrients
        /// </summary>
        public bool NeedsNutrients(float threshold = 0.3f)
        {
            return NutrientLevel < threshold;
        }

        /// <summary>
        /// Get overall plant health score
        /// </summary>
        public float GetOverallHealth()
        {
            return (Health + (1f - Stress) + WaterLevel + NutrientLevel) / 4f;
        }

        /// <summary>
        /// Check if plant is ready for harvest
        /// </summary>
        public bool IsReadyForHarvest(float maturityThreshold = 0.8f)
        {
            return Health >= maturityThreshold &&
                   Stress < 0.2f &&
                   GrowthStage == PlantGrowthStage.Flowering;
        }
    }

    /// <summary>
    /// Plant growth data for calculations
    /// </summary>
    [System.Serializable]
    public class PlantGrowthData
    {
        public float GrowthRate;
        public float WaterConsumptionRate;
        public float NutrientConsumptionRate;
        public float EnergyConsumptionRate;
        public float OptimalTemperature;
        public float OptimalHumidity;
        public float OptimalLightIntensity;
        public float OptimalCO2Level;
        public Dictionary<string, float> GeneticModifiers;
    }

    /// <summary>
    /// Plant resource data
    /// </summary>
    [System.Serializable]
    public class PlantResourceData
    {
        public float CurrentWaterLevel;
        public float CurrentNutrientLevel;
        public float CurrentEnergyLevel;
        public float MaxWaterCapacity;
        public float MaxNutrientCapacity;
        public float MaxEnergyCapacity;
        public Dictionary<string, float> NutrientComposition;
    }

    /// <summary>
    /// Plant harvest data
    /// </summary>
    [System.Serializable]
    public class PlantHarvestData
    {
        public DateTime HarvestDate;
        public float TotalYield; // in grams
        public float THCContent; // percentage
        public float CBDContent; // percentage
        public float QualityScore; // 0-1 scale
        public string HarvestNotes;
        public Dictionary<string, float> TerpeneProfile;
    }

    /// <summary>
    /// Cultural practice for plant care
    /// </summary>
    [System.Serializable]
    public class CulturalPractice
    {
        public string PracticeId;
        public string Name;
        public string Description;
        public PracticeType Type;
        public float FrequencyDays;
        public float DurationHours;
        public float LaborRequired;
        public Dictionary<string, float> Effects;

        public enum PracticeType
        {
            Watering,
            Fertilizing,
            Pruning,
            Training,
            PestControl,
            Harvesting
        }
    }
}
