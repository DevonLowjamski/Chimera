using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Persistence Data Structures
    /// Single Responsibility: Define all data types related to configuration persistence
    /// Extracted from ConfigurationPersistenceManager for better SRP compliance
    /// </summary>

    /// <summary>
    /// Configuration data container for serialization
    /// </summary>
    [Serializable]
    public class ConfigurationData
    {
        public Dictionary<string, CostConfigurationProfile> Profiles;
        public DateTime SavedAt;
        public string Version;
    }

    /// <summary>
    /// Configuration backup information
    /// </summary>
    [Serializable]
    public class ConfigurationBackup
    {
        public Dictionary<string, CostConfigurationProfile> Profiles;
        public DateTime CreatedAt;
        public string BackupId;
        public string FilePath;
        public long FileSize;
    }

    /// <summary>
    /// Backup information for UI display
    /// </summary>
    [Serializable]
    public struct BackupInfo
    {
        public string BackupId;
        public DateTime CreatedAt;
        public string FilePath;
        public long FileSize;
        public bool IsAccessible;
    }

    /// <summary>
    /// Persistence statistics tracking
    /// </summary>
    [Serializable]
    public class PersistenceStatistics
    {
        public int TotalSaves = 0;
        public int TotalLoads = 0;
        public int TotalBackups = 0;
        public int BackupsDeleted = 0;
        public int BackupsRestored = 0;
        public int SaveErrors = 0;
        public int LoadErrors = 0;
        public int BackupErrors = 0;
        public int RestoreErrors = 0;
        public double TotalSaveTime = 0.0;
        public double TotalLoadTime = 0.0;
        public DateTime LastSaveTime = DateTime.MinValue;
        public DateTime LastLoadTime = DateTime.MinValue;

        /// <summary>
        /// Get average save time in milliseconds
        /// </summary>
        public double AverageSaveTime => TotalSaves > 0 ? TotalSaveTime / TotalSaves : 0.0;

        /// <summary>
        /// Get average load time in milliseconds
        /// </summary>
        public double AverageLoadTime => TotalLoads > 0 ? TotalLoadTime / TotalLoads : 0.0;

        /// <summary>
        /// Get save success rate
        /// </summary>
        public float SaveSuccessRate => TotalSaves > 0 ? (float)(TotalSaves - SaveErrors) / TotalSaves : 1f;

        /// <summary>
        /// Get load success rate
        /// </summary>
        public float LoadSuccessRate => TotalLoads > 0 ? (float)(TotalLoads - LoadErrors) / TotalLoads : 1f;

        /// <summary>
        /// Get backup success rate
        /// </summary>
        public float BackupSuccessRate => TotalBackups > 0 ? (float)(TotalBackups - BackupErrors) / TotalBackups : 1f;
    }
}

