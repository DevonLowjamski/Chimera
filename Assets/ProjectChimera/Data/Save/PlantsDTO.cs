using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// SIMPLIFIED: Basic plant data coordinator aligned with Project Chimera's direct player control vision.
    /// Focuses on essential plant data while maintaining simple, focused mechanics.
    ///
    /// Coordinator Structure:
    /// - PlantInstanceDTO.cs: Basic plant instance data
    /// - PlantStrainDTO.cs: Genetic strain information
    /// - CultivationZoneDTO.cs: Zone and environmental data
    /// - PlantsDTO.cs: Coordinates the plant data system
    /// </summary>

    /// <summary>
    /// Simplified cultivation state DTO for essential plant management
    /// </summary>
    [System.Serializable]
    public class CultivationStateDTO
    {
        [Header("Active Plants")]
        public List<PlantInstanceDTO> ActivePlants = new List<PlantInstanceDTO>();
        public Dictionary<string, Vector3> PlantPositions = new Dictionary<string, Vector3>();
        public Dictionary<string, string> PlantZoneAssignments = new Dictionary<string, string>();

        [Header("Genetic Library")]
        public List<PlantStrainDTO> AvailableStrains = new List<PlantStrainDTO>();

        [Header("Cultivation Zones")]
        public List<CultivationZoneDTO> CultivationZones = new List<CultivationZoneDTO>();

        [Header("Basic Configuration")]
        public bool EnableCultivationSystem = true;
        public int MaxPlantsPerGrow = 50;
        public float GrowthUpdateInterval = 1.0f;

        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }

    // PlantInstanceDTO is defined in PlantInstanceDTO.cs to avoid duplication
    // PlantStrainDTO is defined in PlantStrainDTO.cs to avoid duplication


    // CultivationZoneDTO is defined in CultivationZoneDTO.cs to avoid duplication

    /// <summary>
    /// Simple cultivation statistics
    /// </summary>
    [System.Serializable]
    public class CultivationMetricsDTO
    {
        public int TotalPlantsGrown;
        public int TotalPlantsHarvested;
        public float TotalYieldHarvested; // grams
        public float AverageYieldPerPlant; // grams
        public DateTime LastHarvestDate;
    }
}
