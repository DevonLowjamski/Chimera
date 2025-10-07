// REFACTORED: Data Structures
// Extracted from CostDatabasePersistenceManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;
using Newtonsoft.Json;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    public class DatabasePersistenceStatistics
    {
        public int TotalSaves = 0;
        public int TotalLoads = 0;
        public int SaveErrors = 0;
        public int LoadErrors = 0;
        public int BackupsCreated = 0;
        public int BackupsRestored = 0;
        public int BackupErrors = 0;
        public int RestoreErrors = 0;
        public int ValidationErrors = 0;
        public double TotalSaveTime = 0.0;
        public double TotalLoadTime = 0.0;
        public DateTime LastSaveTime = DateTime.MinValue;
        public DateTime LastLoadTime = DateTime.MinValue;
    }

    public class CostDatabaseData
    {
        public Dictionary<MalfunctionType, CostDatabaseEntry> CostDatabase = new Dictionary<MalfunctionType, CostDatabaseEntry>();
        public Dictionary<EquipmentType, EquipmentCostProfile> EquipmentProfiles = new Dictionary<EquipmentType, EquipmentCostProfile>();
        public List<CostDataPoint> HistoricalData = new List<CostDataPoint>();
        public DateTime SavedAt = DateTime.Now;
        public string Version = "1.0";
    }

    public struct DatabaseBackupInfo
    {
        public string FileName;
        public string FilePath;
        public DateTime CreatedAt;
        public long FileSize;
        public bool IsAccessible;
    }

    public struct DatabaseValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public int EntriesCount;
        public int ProfilesCount;
        public int HistoryCount;
        public long FileSize;
        public DateTime ValidationTime;
    }

}
