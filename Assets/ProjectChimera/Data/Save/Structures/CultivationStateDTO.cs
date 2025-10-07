using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save;

namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// Data Transfer Object for cultivation system state
    /// </summary>
    [System.Serializable]
    public class CultivationStateDTO
    {
        // Save metadata
        public DateTime SaveTimestamp;
        public string SaveVersion;

        // System settings
        public bool EnableCultivationSystem;
        public int MaxPlantsPerGrow;
        public bool EnableAutoGrowth;
        public float TimeAcceleration;

        // Plant data
        public List<PlantInstanceDTO> ActivePlants;

        // Statistics
        public CultivationMetricsDTO Metrics;

        // Environmental state
        public CultivationEnvironmentalStateDTO EnvironmentalState;

        // Cultivation zones
        public List<CultivationZoneDTO> CultivationZones;

        // Legacy properties for compatibility
        public List<PlantStateDTO> Plants;
        public int TotalPlants;
        public int HealthyPlants;
        public int FloweringPlants;
        public float AverageHealth;
        public float Temperature;
        public float Humidity;
        public DateTime LastUpdate;
    }

    /// <summary>
    /// Cultivation metrics data
    /// </summary>
    [System.Serializable]
    public class CultivationMetricsDTO
    {
        public int TotalPlantsCultivated;
        public int PlantsHarvested;
        public float TotalYieldProduced;
        public int ActivePlants;
    }

    /// <summary>
    /// Environmental state data
    /// </summary>
    [System.Serializable]
    public class CultivationEnvironmentalStateDTO
    {
        public bool IsInitialized;
        public DateTime LastEnvironmentalUpdate;
        public CultivationEnvironmentalDataDTO DefaultEnvironment;
    }

    /// <summary>
    /// Environmental data values
    /// </summary>
    [System.Serializable]
    public class CultivationEnvironmentalDataDTO
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirFlow;
        public float AirVelocity;
        public float pH;
        public float PhotoperiodHours;
        public float WaterAvailability;
        public float ElectricalConductivity;
        public float DailyLightIntegral;
    }

    /// <summary>
    /// Cultivation zone data
    /// </summary>
    [System.Serializable]
    public class CultivationZoneDTO
    {
        public string ZoneId;
        public string ZoneName;
        public string ZoneType;
        public bool IsActive;
        public int MaxPlantCapacity;
        public int CurrentPlantCount;
    }

    /// <summary>
    /// Legacy plant state DTO for compatibility
    /// </summary>
    [System.Serializable]
    public class PlantStateDTO
    {
        public string PlantId;
        public string PlantName;
        public float Health;
        public float GrowthStage;
        public Vector3 Position;
    }
}
