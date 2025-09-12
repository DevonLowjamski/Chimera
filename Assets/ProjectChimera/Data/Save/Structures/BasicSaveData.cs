using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// Basic save data structure for Project Chimera
    /// Contains essential game state information for persistence
    /// </summary>
    [System.Serializable]
    public class BasicSaveData
    {
        [Header("Save Metadata")]
        public string SaveId;
        public string SaveName;
        public DateTime CreatedDate;
        public DateTime LastModifiedDate;
        public string GameVersion;
        public int SaveVersion;

        [Header("Player Information")]
        public string PlayerName;
        public long PlayerExperience;
        public int PlayerLevel;
        public long CurrencyAmount;
        public long Currency;
        public float PlayTimeHours;
        public DateTime SaveTime;

        [Header("Game State")]
        public DateTime GameTime;
        public float TimeScale;
        public bool IsPaused;
        public GameDifficulty Difficulty;

        [Header("Facility Data")]
        public List<FacilitySaveData> Facilities = new List<FacilitySaveData>();
        public string ActiveFacilityId;

        [Header("Progression")]
        public PlayerProgressionData Progression = new PlayerProgressionData();

        [Header("Statistics")]
        public GameStatistics Statistics = new GameStatistics();

        /// <summary>
        /// Validate save data integrity
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SaveId) &&
                   !string.IsNullOrEmpty(SaveName) &&
                   Facilities != null &&
                   Progression != null &&
                   Statistics != null;
        }

        /// <summary>
        /// Get total facility count
        /// </summary>
        public int GetTotalFacilityCount()
        {
            return Facilities?.Count ?? 0;
        }

        /// <summary>
        /// Get operational facility count
        /// </summary>
        public int GetOperationalFacilityCount()
        {
            return Facilities?.FindAll(f => f.IsOperational)?.Count ?? 0;
        }

        /// <summary>
        /// Calculate total value of all facilities
        /// </summary>
        public long GetTotalFacilityValue()
        {
            long totalValue = 0;
            if (Facilities != null)
            {
                foreach (var facility in Facilities)
                {
                    totalValue += facility.FacilityValue;
                }
            }
            return totalValue;
        }
    }

    /// <summary>
    /// Individual facility save data
    /// </summary>
    [System.Serializable]
    public class FacilitySaveData
    {
        public string FacilityId;
        public string FacilityName;
        public Vector3 Position;
        public Vector3 Size;
        public bool IsOperational;
        public long FacilityValue;
        public DateTime ConstructionDate;
        public ProjectChimera.Data.Facilities.FacilityType Type;
        public List<RoomSaveData> Rooms = new List<RoomSaveData>();
        public List<EquipmentSaveData> Equipment = new List<EquipmentSaveData>();
    }

    /// <summary>
    /// Room save data
    /// </summary>
    [System.Serializable]
    public class RoomSaveData
    {
        public string RoomId;
        public string RoomName;
        public Vector3 Position;
        public Vector3 Size;
        public bool IsConstructed;
        public ProjectChimera.Data.Facilities.RoomType Type;
        public List<PlantSaveData> Plants = new List<PlantSaveData>();
    }

    /// <summary>
    /// Equipment save data
    /// </summary>
    [System.Serializable]
    public class EquipmentSaveData
    {
        public string EquipmentId;
        public string EquipmentName;
        public Vector3 Position;
        public bool IsOperational;
        public float PowerConsumption;
        public EquipmentType Type;
    }

    /// <summary>
    /// Plant save data
    /// </summary>
    [System.Serializable]
    public class PlantSaveData
    {
        public string PlantId;
        public string StrainName;
        public Vector3 Position;
        public float Health;
        public float Age;
        public PlantGrowthStage GrowthStage;
    }

    /// <summary>
    /// Player progression data
    /// </summary>
    [System.Serializable]
    public class PlayerProgressionData
    {
        public int CurrentLevel;
        public long ExperiencePoints;
        public List<string> UnlockedFeatures = new List<string>();
        public Dictionary<string, int> SkillLevels = new Dictionary<string, int>();
        public List<AchievementData> Achievements = new List<AchievementData>();
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [System.Serializable]
    public class AchievementData
    {
        public string AchievementId;
        public string Name;
        public bool IsUnlocked;
        public DateTime UnlockDate;
        public int Progress;
        public int MaxProgress;
    }

    /// <summary>
    /// Game statistics
    /// </summary>
    [System.Serializable]
    public class GameStatistics
    {
        public long TotalPlayTime; // in seconds
        public int TotalPlantsGrown;
        public int TotalHarvests;
        public long TotalYield; // in grams
        public long TotalRevenue;
        public int FacilitiesBuilt;
        public int EquipmentPurchased;
        public Dictionary<string, long> StrainStatistics = new Dictionary<string, long>();
    }

    /// <summary>
    /// Game difficulty levels
    /// </summary>
    public enum GameDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

}
