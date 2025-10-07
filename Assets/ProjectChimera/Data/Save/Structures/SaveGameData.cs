using UnityEngine;
using System.Collections.Generic;


namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// BASIC: Simple save game data structure for Project Chimera.
    /// Focuses on essential game state without complex DTOs and metadata.
    /// </summary>
    [System.Serializable]
    public class SaveGameData
    {
        [Header("Basic Save Info")]
        public string SaveName = "Game Save";
        public string SlotName = "Slot 1";
        public string PlayerName = "Player";
        public string Description = "";
        public System.DateTime SaveTime;
        public System.DateTime SaveTimestamp;
        public string GameVersion = "1.0";
        public int SaveSystemVersion = 1;
        public float PlayTimeHours = 0f;

        [Header("Game State")]
        public PlayerSaveState PlayerState;
        public CultivationSaveState CultivationState;
        public ConstructionSaveState ConstructionState;
        public EconomySaveState EconomyState;
        public ProgressionSaveState ProgressionState;
        public FacilityStateDTO FacilityData;
        public CultivationStateDTO PlantsData;
        public EconomyStateDTO EconomyStateData;
        public ProgressionStateDTO ProgressionStateData;
        public UIStateDTO UIData;

        /// <summary>
        /// Create new save data
        /// </summary>
        public static SaveGameData CreateNew(string saveName, string playerName)
        {
            return new SaveGameData
            {
                SaveName = saveName,
                PlayerName = playerName,
                SaveTime = System.DateTime.Now,
                PlayerState = new PlayerSaveState(),
                CultivationState = new CultivationSaveState(),
                ConstructionState = new ConstructionSaveState(),
                EconomyState = new EconomySaveState()
            };
        }

        /// <summary>
        /// Validate save data integrity
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SaveName) &&
                   !string.IsNullOrEmpty(PlayerName) &&
                   PlayerState != null &&
                   CultivationState != null &&
                   ConstructionState != null &&
                   EconomyState != null;
        }

        /// <summary>
        /// Get save data summary
        /// </summary>
        public SaveSummary GetSummary()
        {
            return new SaveSummary
            {
                SaveName = SaveName,
                PlayerName = PlayerName,
                SaveTime = SaveTime,
                PlayTimeHours = PlayTimeHours,
                PlayerLevel = PlayerState?.Level ?? 1,
                Currency = EconomyState?.Currency ?? 0,
                PlantCount = CultivationState?.Plants?.Count ?? 0,
                BuildingCount = ConstructionState?.Buildings?.Count ?? 0
            };
        }

        /// <summary>
        /// Clone save data
        /// </summary>
        public SaveGameData Clone()
        {
            return new SaveGameData
            {
                SaveName = this.SaveName,
                SlotName = this.SlotName,
                PlayerName = this.PlayerName,
                SaveTime = this.SaveTime,
                SaveTimestamp = this.SaveTimestamp,
                GameVersion = this.GameVersion,
                SaveSystemVersion = this.SaveSystemVersion,
                PlayTimeHours = this.PlayTimeHours,
                PlayerState = this.PlayerState,
                CultivationState = this.CultivationState,
                ConstructionState = this.ConstructionState,
                EconomyState = this.EconomyState,
                ProgressionState = this.ProgressionState,
                FacilityData = this.FacilityData,
                PlantsData = this.PlantsData,
                EconomyStateData = this.EconomyStateData,
                ProgressionStateData = this.ProgressionStateData,
                UIData = this.UIData
            };
        }

        /// <summary>
        /// Validate save data
        /// </summary>
        public bool ValidateData()
        {
            return !string.IsNullOrEmpty(SaveName) &&
                   !string.IsNullOrEmpty(PlayerName) &&
                   SaveSystemVersion > 0;
        }

        /// <summary>
        /// Equipment state DTO for offline progression calculations
        /// </summary>
        [System.Serializable]
        public class EquipmentStateDTO
        {
            public string EquipmentId;
            public string EquipmentType;
            public Vector3 Position;
            public bool IsOperational = true;
            public float Efficiency = 1f;
            public float PowerConsumption = 100f;
            public System.DateTime LastMaintenance;
            public float WearLevel = 0f;
        }
    }

    /// <summary>
    /// Basic player save state
    /// </summary>
    [System.Serializable]
    public class PlayerSaveState
    {
        public int Level = 1;
        public float Experience = 0f;
        public int SkillPoints = 0;
        public List<string> CompletedAchievements = new List<string>();
        public List<string> UnlockedFeatures = new List<string>();
    }

    /// <summary>
    /// Basic cultivation save state
    /// </summary>
    [System.Serializable]
    public class CultivationSaveState
    {
        public List<PlantSaveState> Plants = new List<PlantSaveState>();
        public float Temperature = 25f;
        public float Humidity = 60f;
        public float LightLevel = 500f;
        public System.DateTime LastWatering;
        public System.DateTime LastNutrients;
    }

    /// <summary>
    /// Basic construction save state
    /// </summary>
    [System.Serializable]
    public class ConstructionSaveState
    {
        public List<BuildingSaveState> Buildings = new List<BuildingSaveState>();
        public Vector3 FacilitySize = new Vector3(20, 10, 15);
        public int ConstructionProgress = 0;
    }

    /// <summary>
    /// Basic economy save state
    /// </summary>
    [System.Serializable]
    public class EconomySaveState
    {
        public float Currency = 1000f;
        public List<ItemSaveState> Inventory = new List<ItemSaveState>();
        public List<TransactionSaveState> TransactionHistory = new List<TransactionSaveState>();
    }

    /// <summary>
    /// Basic plant save state
    /// </summary>
    [System.Serializable]
    public class PlantSaveState
    {
        public string PlantId;
        public string StrainName;
        public Vector3 Position;
        public float Age;
        public float Health;
        public float GrowthStage;
        public System.DateTime PlantDate;
    }

    /// <summary>
    /// Basic building save state
    /// </summary>
    [System.Serializable]
    public class BuildingSaveState
    {
        public string BuildingId;
        public string BuildingType;
        public Vector3 Position;
        public Vector3 Size;
        public bool IsConstructed;
        public int ConstructionProgress;
    }

    /// <summary>
    /// Basic item save state
    /// </summary>
    [System.Serializable]
    public class ItemSaveState
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public float Value;
    }

    /// <summary>
    /// Basic transaction save state
    /// </summary>
    [System.Serializable]
    public class TransactionSaveState
    {
        public string TransactionId;
        public string Description;
        public float Amount;
        public System.DateTime TransactionTime;
        public string TransactionType;
    }

        /// <summary>
        /// Save data summary
        /// </summary>
        [System.Serializable]
        public struct SaveSummary
        {
            public string SaveName;
            public string PlayerName;
            public System.DateTime SaveTime;
            public float PlayTimeHours;
            public int PlayerLevel;
            public float Currency;
            public int PlantCount;
            public int BuildingCount;
        }

        /// <summary>
        /// Progression state DTO
        /// </summary>
        [System.Serializable]
        public class ProgressionStateDTO
        {
            public System.DateTime SaveTimestamp;
            public string SaveVersion;
            public bool EnableProgressionTracking;
            public PlayerProgressDTO PlayerProgress;
            public UnlockSystemDTO UnlockSystem;
            public SkillSystemDTO SkillSystem;
            public AchievementSystemDTO AchievementSystem;
            public int PlayerLevel;
            public float Experience;
            public int SkillPoints;
            public int AchievementCount;
            public System.DateTime LastUpdate;
        }

        /// <summary>
        /// Player progress data
        /// </summary>
        [System.Serializable]
        public class PlayerProgressDTO
        {
            public int PlayerLevel;
            public float TotalExperience;
            public float ExperienceToNextLevel;
        }

        /// <summary>
        /// Unlock system data
        /// </summary>
        [System.Serializable]
        public class UnlockSystemDTO
        {
            public List<string> UnlockedFeatures = new List<string>();
            public List<string> UnlockedSchematics = new List<string>();
        }

        /// <summary>
        /// Progression save state
        /// </summary>
        [System.Serializable]
        public class ProgressionSaveState
        {
            public int CurrentLevel = 1;
            public float CurrentXP = 0f;
            public List<string> UnlockedSkills = new List<string>();
            public List<string> CompletedQuests = new List<string>();
            public System.DateTime LastProgressionUpdate;
        }

        /// <summary>
        /// Skill system data
        /// </summary>
        [System.Serializable]
        public class SkillSystemDTO
        {
            public List<string> UnlockedSkills = new List<string>();
            public int SkillPoints;
        }

        /// <summary>
        /// Achievement system data
        /// </summary>
        [System.Serializable]
        public class AchievementSystemDTO
        {
            public List<string> UnlockedAchievements = new List<string>();
            public int CompletedCount;
        }


}
