using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// SIMPLE: Basic save data structures aligned with Project Chimera's save system vision.
    /// Focuses on essential game state persistence without over-engineering.
    /// </summary>

    /// <summary>
    /// Main game save data container
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public string SaveVersion = "1.0";
        public DateTime SaveTime;
        public string PlayerName;
        
        // Core game state
        public PlayerData Player = new PlayerData();
        public CultivationData Cultivation = new CultivationData();
        public ConstructionData Construction = new ConstructionData();
        public GeneticsData Genetics = new GeneticsData();
    }

    /// <summary>
    /// Player-specific data
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string PlayerName;
        public decimal CashBalance = 1000m;
        public int SkillPoints = 100;
        public int ExperiencePoints = 0;
        public int PlayerLevel = 1;
        public DateTime LastPlayed;
    }

    /// <summary>
    /// Cultivation-specific data
    /// </summary>
    [System.Serializable]
    public class CultivationData
    {
        public List<PlantInstance> ActivePlants = new List<PlantInstance>();
        public int TotalPlantsGrown = 0;
        public decimal TotalYieldHarvested = 0m;
        public DateTime LastHarvestDate;
    }

    /// <summary>
    /// Construction-specific data
    /// </summary>
    [System.Serializable]
    public class ConstructionData
    {
        public List<PlacedObject> PlacedObjects = new List<PlacedObject>();
        public List<Facility> Facilities = new List<Facility>();
        public int TotalObjectsPlaced = 0;
    }

    /// <summary>
    /// Genetics-specific data
    /// </summary>
    [System.Serializable]
    public class GeneticsData
    {
        public List<CannabisGenotype> AvailableStrains = new List<CannabisGenotype>();
        public List<BreedingRecord> BreedingHistory = new List<BreedingRecord>();
        public int TotalStrainsCreated = 0;
    }

    /// <summary>
    /// Simple plant instance for saving
    /// </summary>
    [System.Serializable]
    public class PlantInstance
    {
        public string PlantId;
        public string StrainId;
        public float AgeInDays;
        public string GrowthStage;
        public float Health = 1f;
        public Vector3 Position;
    }

    /// <summary>
    /// Simple placed object for saving
    /// </summary>
    [System.Serializable]
    public class PlacedObject
    {
        public string ObjectId;
        public string ObjectType;
        public Vector3 Position;
        public Vector3 Rotation;
        public string GridCoordinate;
    }

    /// <summary>
    /// Simple facility for saving
    /// </summary>
    [System.Serializable]
    public class Facility
    {
        public string FacilityId;
        public string FacilityName;
        public string FacilityType;
        public Vector3 Position;
        public List<string> PlacedObjects = new List<string>();
    }

    /// <summary>
    /// Simple breeding record
    /// </summary>
    [System.Serializable]
    public class BreedingRecord
    {
        public string RecordId;
        public string Parent1Id;
        public string Parent2Id;
        public string OffspringId;
        public DateTime BreedingDate;
        public bool Success = true;
    }
}
