using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Save Data Structures - Modular Orchestrator
    /// Coordinates modular save system components for Project Chimera's cultivation simulation.
    /// Manages: Core Data, Version Management, Offline Progression, and System Types.
    /// </summary>
    public static class SaveDataStructures
    {
        #region Core Components

        private static SaveSystemConfig _config;
        private static bool _isInitialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the modular save system
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // Initialize configuration
            _config = new SaveSystemConfig();

            _isInitialized = true;
            Debug.Log("[SaveDataStructures] Modular save system initialized");
        }

        /// <summary>
        /// Shuts down the modular save system
        /// </summary>
        public static void Shutdown()
        {
            _config = null;
            _isInitialized = false;
            Debug.Log("[SaveDataStructures] Modular save system shutdown");
        }

        #endregion

        #region Save Game Data Operations

        /// <summary>
        /// Creates a new save game data instance
        /// </summary>
        public static Structures.SaveGameData CreateNewSave(string slotName, string playerName = null)
        {
            var saveData = new Structures.SaveGameData(slotName)
            {
                PlayerName = playerName ?? "Cultivator",
                GameVersion = Application.version,
                SaveSystemVersion = VersionManagement.CurrentVersion,
                SaveTimestamp = DateTime.Now
            };

            // Initialize with default cultivation-focused data
            InitializeCultivationDefaults(saveData);

            return saveData;
        }

        /// <summary>
        /// Validates save game data integrity
        /// </summary>
        public static ValidationResult ValidateSaveData(Structures.SaveGameData saveData)
        {
            if (!_isInitialized) Initialize();

            var result = new ValidationResult();

            try
            {
                if (saveData == null)
                    throw new ArgumentNullException(nameof(saveData));

                // Core validation
                if (string.IsNullOrEmpty(saveData.SlotName))
                    throw new InvalidOperationException("Save slot name is required");

                if (saveData.FacilityData == null)
                    throw new InvalidOperationException("Facility data is required for cultivation simulation");

                // Version compatibility
                if (!VersionManagement.IsVersionCompatible(saveData.SaveSystemVersion))
                    throw new InvalidOperationException($"Incompatible save version: {saveData.SaveSystemVersion}");

                // Cultivation-specific validation
                ValidateCultivationData(saveData);

                result.IsValid = true;
                result.Message = "Save data validation successful";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Validation failed: {ex.Message}";
                result.ErrorCode = ex.GetType().Name;
            }

            return result;
        }

        /// <summary>
        /// Migrates save data to current version
        /// </summary>
        public static Structures.VersionManagement.MigrationResult MigrateSaveData(Structures.SaveGameData saveData)
        {
            if (!_isInitialized) Initialize();

            return VersionManagement.MigrateData(saveData);
        }

        /// <summary>
        /// Calculates offline progression
        /// </summary>
        public static Structures.OfflineProgressionResult CalculateOfflineProgression(Structures.SaveGameData saveData)
        {
            if (!_isInitialized) Initialize();

            return OfflineProgression.CalculateProgression(saveData, DateTime.Now);
        }

        /// <summary>
        /// Gets offline progression summary
        /// </summary>
        public static Structures.OfflineProgressionSummary GetOfflineProgressionSummary(Structures.SaveGameData saveData, TimeSpan offlineDuration)
        {
            if (!_isInitialized) Initialize();

            return OfflineProgression.GetProgressionSummary(saveData, offlineDuration);
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Gets the current save system configuration
        /// </summary>
        public static SaveSystemConfig GetConfiguration()
        {
            if (!_isInitialized) Initialize();
            return _config;
        }

        /// <summary>
        /// Updates save system configuration
        /// </summary>
        public static void UpdateConfiguration(SaveSystemConfig newConfig)
        {
            if (newConfig == null) return;
            _config = newConfig;
            Debug.Log("[SaveDataStructures] Configuration updated");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets save slot information
        /// </summary>
        public static Structures.SaveSlotInfo GetSaveSlotInfo(string slotName)
        {
            // Implementation would scan save directory for slot information
            return new Structures.SaveSlotInfo
            {
                SlotName = slotName,
                DisplayName = slotName,
                Status = Structures.SaveTypes.SaveSlotStatus.Empty
            };
        }

        /// <summary>
        /// Gets all available save slots
        /// </summary>
        public static List<Structures.SaveSlotInfo> GetAvailableSaveSlots()
        {
            // Implementation would scan save directory
            return new List<Structures.SaveSlotInfo>();
        }

        /// <summary>
        /// Creates a backup of save data
        /// </summary>
        public static Structures.SaveGameData CreateBackup(Structures.SaveGameData saveData)
        {
            return VersionManagement.CreateBackup(saveData);
        }

        #endregion

        #region Private Helper Methods

        private static void InitializeCultivationDefaults(Structures.SaveGameData saveData)
        {
            // Initialize with cultivation-focused defaults
            saveData.FacilityData = new Structures.SaveGameData.FacilityStateDTO
            {
                FacilityName = "Starter Grow Room",
                FacilityType = "Cultivation",
                FacilityLevel = 1
            };

            saveData.PlantsData = new Structures.SaveGameData.CultivationStateDTO();
            saveData.EconomyStateData = new Structures.SaveGameData.EconomyStateDTO();
            saveData.ProgressionStateData = new Structures.SaveGameData.ProgressionStateDTO
            {
                PlayerLevel = 1,
                ExperiencePoints = 0f
            };

            saveData.UIData = new Structures.SaveGameData.UIStateDTO();
        }

        private static void ValidateCultivationData(Structures.SaveGameData saveData)
        {
            // Cultivation-specific validation
            if (saveData.PlantsData == null)
                throw new InvalidOperationException("Cultivation data is required for cannabis simulation");

            if (saveData.FacilityData == null)
                throw new InvalidOperationException("Facility data is required for cultivation environment");

            // Validate plant data integrity
            if (saveData.PlantsData.Plants != null)
            {
                foreach (var plant in saveData.PlantsData.Plants)
                {
                    if (string.IsNullOrEmpty(plant.PlantId))
                        throw new InvalidOperationException("Plant ID cannot be empty");

                    if (plant.Health < 0f || plant.Health > 1f)
                        throw new InvalidOperationException($"Invalid plant health: {plant.Health}");
                }
            }
        }

        #endregion
    }

    #region Legacy Compatibility (Deprecated)

    /// <summary>
    /// Legacy save game data container - DEPRECATED
    /// Use ProjectChimera.Data.Save.Structures.SaveGameData instead
    /// </summary>
    [System.Serializable]
    [Obsolete("Use ProjectChimera.Data.Save.Structures.SaveGameData instead")]
    public class LegacySaveGameData
    {
        [Header("Legacy Save Meta Information")]
        public string SlotName;
        public string Description;
        public DateTime SaveTimestamp;
        public string GameVersion;
        public string SaveSystemVersion;
        public TimeSpan PlayTime;

        [Header("Legacy Game Data - Deprecated")]
        public PlayerSaveData PlayerData;
        public CultivationSaveData CultivationData;
        public EconomySaveData EconomyData;
        public EnvironmentSaveData EnvironmentData;
        public ProgressionSaveData ProgressionData;
        public ObjectiveSaveData ObjectiveData;
        public EventSaveData EventData;
        public GameSettingsSaveData SettingsData;
    }

    #endregion

    #region Legacy Data Structures (Kept for Migration)
    // Note: Legacy save data classes are defined in ContractsDTO.cs
    // PlayerSaveData, CultivationSaveData, PlantSaveData, EconomySaveData, and TransactionSaveData
    // are available from the main namespace and should be used instead of duplicating here

    [System.Serializable]
    public class EnvironmentSaveData
    {
        public Dictionary<string, object> EnvironmentalData;
    }

    [System.Serializable]
    public class ProgressionSaveData
    {
        public int PlayerLevel;
        public Dictionary<string, SkillSaveData> Skills;
    }

    [System.Serializable]
    public class SkillSaveData
    {
        public string SkillId;
        public int Level;
        public float Experience;
    }

    [System.Serializable]
    public class ObjectiveSaveData
    {
        public List<QuestSaveData> ActiveQuests;
        public List<AchievementSaveData> Achievements;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public string QuestId;
        public string Status;
        public float Progress;
    }

    [System.Serializable]
    public class AchievementSaveData
    {
        public string AchievementId;
        public bool Unlocked;
        public DateTime UnlockDate;
    }

    [System.Serializable]
    public class EventSaveData
    {
        public List<GameEventSaveData> Events;
    }

    [System.Serializable]
    public class GameEventSaveData
    {
        public string EventId;
        public string EventType;
        public DateTime Timestamp;
        public Dictionary<string, object> EventData;
    }

    [System.Serializable]
    public class GameSettingsSaveData
    {
        public Dictionary<string, object> Settings;
    }

    #endregion
}
