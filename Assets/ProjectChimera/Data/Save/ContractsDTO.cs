using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// BASIC: Simple save data structures for Project Chimera's save system.
    /// Focuses on essential save data without complex contract systems and detailed DTOs.
    /// </summary>

    /// <summary>
    /// Basic game save data
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public string SaveVersion = "1.0";
        public System.DateTime SaveTime;
        public PlayerSaveData PlayerData;
        public CultivationSaveData CultivationData;
        public ConstructionSaveData ConstructionData;
        public EconomySaveData EconomyData;
        public SettingsSaveData SettingsData;
    }

    /// <summary>
    /// Basic player save data
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData
    {
        public string PlayerName = "Player";
        public int Experience = 0;
        public int Level = 1;
        public float Currency = 1000f;
        public System.DateTime LastPlayed;
        public List<string> Achievements = new List<string>();
    }

    /// <summary>
    /// Basic cultivation save data
    /// </summary>
    [System.Serializable]
    public class CultivationSaveData
    {
        public List<PlantSaveData> Plants = new List<PlantSaveData>();
        public float Temperature = 25f;
        public float Humidity = 60f;
        public float LightLevel = 500f;
        public System.DateTime LastWatering;
        public System.DateTime LastNutrients;
    }

    /// <summary>
    /// Basic construction save data
    /// </summary>
    [System.Serializable]
    public class ConstructionSaveData
    {
        public List<BuildingSaveData> Buildings = new List<BuildingSaveData>();
        public Vector3 FacilitySize = new Vector3(20, 10, 15);
        public int ConstructionProgress = 0;
    }

    /// <summary>
    /// Basic economy save data
    /// </summary>
    [System.Serializable]
    public class EconomySaveData
    {
        public float Currency = 1000f;
        public List<ItemSaveData> Inventory = new List<ItemSaveData>();
        public List<TransactionSaveData> TransactionHistory = new List<TransactionSaveData>();
    }

    /// <summary>
    /// Basic settings save data
    /// </summary>
    [System.Serializable]
    public class SettingsSaveData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 1f;
        public bool EnableTooltips = true;
        public bool EnableAutosave = true;
        public float AutosaveInterval = 300f;
    }

    /// <summary>
    /// Basic plant save data
    /// </summary>
    [System.Serializable]
    public class PlantSaveData
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
    /// Basic building save data
    /// </summary>
    [System.Serializable]
    public class BuildingSaveData
    {
        public string BuildingId;
        public string BuildingType;
        public Vector3 Position;
        public Vector3 Size;
        public bool IsConstructed;
        public int ConstructionProgress;
    }

    /// <summary>
    /// Basic item save data
    /// </summary>
    [System.Serializable]
    public class ItemSaveData
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public float Value;
    }

    /// <summary>
    /// Basic transaction save data
    /// </summary>
    [System.Serializable]
    public class TransactionSaveData
    {
        public string TransactionId;
        public string Description;
        public float Amount;
        public System.DateTime TransactionTime;
        public TransactionType Type;
    }

    /// <summary>
    /// Transaction types
    /// </summary>
    public enum TransactionType
    {
        Purchase,
        Sale,
        Reward,
        Penalty
    }

    /// <summary>
    /// Save data utilities
    /// </summary>
    public static class SaveDataUtilities
    {
        /// <summary>
        /// Create new game save data
        /// </summary>
        public static GameSaveData CreateNewGame()
        {
            return new GameSaveData
            {
                SaveTime = System.DateTime.Now,
                PlayerData = new PlayerSaveData(),
                CultivationData = new CultivationSaveData(),
                ConstructionData = new ConstructionSaveData(),
                EconomyData = new EconomySaveData(),
                SettingsData = new SettingsSaveData()
            };
        }

        /// <summary>
        /// Validate save data integrity
        /// </summary>
        public static bool ValidateSaveData(GameSaveData saveData)
        {
            if (saveData == null) return false;
            if (saveData.PlayerData == null) return false;
            if (saveData.CultivationData == null) return false;
            if (saveData.ConstructionData == null) return false;
            if (saveData.EconomyData == null) return false;
            if (saveData.SettingsData == null) return false;

            return true;
        }

        /// <summary>
        /// Get save data summary
        /// </summary>
        public static SaveDataSummary GetSummary(GameSaveData saveData)
        {
            if (saveData == null) return null;

            return new SaveDataSummary
            {
                SaveVersion = saveData.SaveVersion,
                SaveTime = saveData.SaveTime,
                PlayerName = saveData.PlayerData?.PlayerName ?? "Unknown",
                PlayerLevel = saveData.PlayerData?.Level ?? 1,
                Currency = saveData.EconomyData?.Currency ?? 0,
                PlantCount = saveData.CultivationData?.Plants?.Count ?? 0,
                BuildingCount = saveData.ConstructionData?.Buildings?.Count ?? 0,
                ItemCount = saveData.EconomyData?.Inventory?.Count ?? 0
            };
        }
    }

    /// <summary>
    /// Save data summary
    /// </summary>
    [System.Serializable]
    public class SaveDataSummary
    {
        public string SaveVersion;
        public System.DateTime SaveTime;
        public string PlayerName;
        public int PlayerLevel;
        public float Currency;
        public int PlantCount;
        public int BuildingCount;
        public int ItemCount;
    }
}
