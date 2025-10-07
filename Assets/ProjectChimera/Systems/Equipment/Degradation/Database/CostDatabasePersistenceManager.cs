using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;
using Newtonsoft.Json;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// REFACTORED: Cost Database Persistence Manager - Focused database file I/O and persistence
    /// Single Responsibility: Managing database file operations, auto-save, and data serialization
    /// Extracted from CostDatabaseManager for better SRP compliance
    /// </summary>
    public class CostDatabasePersistenceManager
    {
        private readonly bool _enableLogging;
        private readonly bool _persistData;
        private readonly string _databaseFileName;
        private readonly float _autoSaveInterval;

        // File paths
        private string _databaseFilePath;

        // Persistence state
        private float _lastAutoSave;
        private bool _isDatabaseDirty = false;

        // Persistence statistics
        private DatabasePersistenceStatistics _persistenceStats = new DatabasePersistenceStatistics();

        // Events
        public event System.Action<string> OnDatabaseSaved;
        public event System.Action<string> OnDatabaseLoaded;
        public event System.Action<string> OnPersistenceError;

        public CostDatabasePersistenceManager(bool enableLogging = false, bool persistData = true,
                                            string databaseFileName = "cost_database.json", float autoSaveInterval = 300f)
        {
            _enableLogging = enableLogging;
            _persistData = persistData;
            _databaseFileName = databaseFileName;
            _autoSaveInterval = autoSaveInterval;

            InitializeFilePaths();
        }

        // Properties
        public DatabasePersistenceStatistics Statistics => _persistenceStats;
        public bool IsPersistenceEnabled => _persistData;
        public bool IsDatabaseDirty => _isDatabaseDirty;
        public string DatabaseFilePath => _databaseFilePath;

        #region Initialization

        /// <summary>
        /// Initialize file paths and directories
        /// </summary>
        private void InitializeFilePaths()
        {
            _databaseFilePath = Path.Combine(Application.persistentDataPath, "Database", _databaseFileName);

            try
            {
                var databaseDir = Path.GetDirectoryName(_databaseFilePath);
                if (!Directory.Exists(databaseDir))
                    Directory.CreateDirectory(databaseDir);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database persistence path: {_databaseFilePath}", null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to initialize database directories: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Initialize persistence manager
        /// </summary>
        public void Initialize()
        {
            _lastAutoSave = Time.time;

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_PERSIST", "Database persistence manager initialized", null);
        }

        #endregion

        #region Save Operations

        /// <summary>
        /// Save database to file
        /// </summary>
        public bool SaveDatabaseToFile(Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                                     Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                                     List<CostDataPoint> historicalData)
        {
            if (!_persistData)
                return false;

            try
            {
                var startTime = DateTime.Now;

                var databaseData = new CostDatabaseData
                {
                    CostDatabase = costDatabase,
                    EquipmentProfiles = equipmentProfiles,
                    HistoricalData = historicalData,
                    SavedAt = DateTime.Now,
                    Version = "1.0"
                };

                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var jsonContent = JsonConvert.SerializeObject(databaseData, jsonSettings);

                // Write to temporary file first, then move to final location for atomic operation
                var tempFilePath = _databaseFilePath + ".tmp";
                File.WriteAllText(tempFilePath, jsonContent);

                if (File.Exists(_databaseFilePath))
                    File.Delete(_databaseFilePath);

                File.Move(tempFilePath, _databaseFilePath);

                var saveTime = (DateTime.Now - startTime).TotalMilliseconds;
                _persistenceStats.TotalSaves++;
                _persistenceStats.TotalSaveTime += saveTime;
                _persistenceStats.LastSaveTime = DateTime.Now;

                _isDatabaseDirty = false;
                _lastAutoSave = Time.time;

                OnDatabaseSaved?.Invoke(_databaseFilePath);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database saved to {_databaseFilePath} ({saveTime:F1}ms)", null);

                return true;
            }
            catch (Exception ex)
            {
                _persistenceStats.SaveErrors++;
                var errorMessage = $"Failed to save database: {ex.Message}";

                OnPersistenceError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", errorMessage, null);

                return false;
            }
        }

        /// <summary>
        /// Auto-save if interval elapsed and database is dirty
        /// </summary>
        public bool AutoSave(Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                           Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                           List<CostDataPoint> historicalData)
        {
            if (!_persistData || !_isDatabaseDirty)
                return true;

            if (Time.time - _lastAutoSave >= _autoSaveInterval)
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", "Performing auto-save", null);

                return SaveDatabaseToFile(costDatabase, equipmentProfiles, historicalData);
            }

            return true;
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Load database from file
        /// </summary>
        public CostDatabaseData LoadDatabaseFromFile()
        {
            if (!_persistData || !File.Exists(_databaseFilePath))
                return new CostDatabaseData();

            try
            {
                var startTime = DateTime.Now;

                var jsonContent = File.ReadAllText(_databaseFilePath);

                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var databaseData = JsonConvert.DeserializeObject<CostDatabaseData>(jsonContent, jsonSettings);

                var loadTime = (DateTime.Now - startTime).TotalMilliseconds;
                _persistenceStats.TotalLoads++;
                _persistenceStats.TotalLoadTime += loadTime;
                _persistenceStats.LastLoadTime = DateTime.Now;

                OnDatabaseLoaded?.Invoke(_databaseFilePath);

                if (_enableLogging)
                {
                    var entriesCount = databaseData.CostDatabase?.Count ?? 0;
                    var profilesCount = databaseData.EquipmentProfiles?.Count ?? 0;
                    var historyCount = databaseData.HistoricalData?.Count ?? 0;

                    ChimeraLogger.LogInfo("DB_PERSIST",
                        $"Database loaded from {_databaseFilePath} ({loadTime:F1}ms): {entriesCount} entries, {profilesCount} profiles, {historyCount} history points", null);
                }

                return databaseData ?? new CostDatabaseData();
            }
            catch (Exception ex)
            {
                _persistenceStats.LoadErrors++;
                var errorMessage = $"Failed to load database: {ex.Message}";

                OnPersistenceError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", errorMessage, null);

                return new CostDatabaseData();
            }
        }

        #endregion

        #region Backup Operations

        /// <summary>
        /// Create a backup of the current database
        /// </summary>
        public bool CreateBackup()
        {
            if (!_persistData || !File.Exists(_databaseFilePath))
                return false;

            try
            {
                var backupDir = Path.Combine(Path.GetDirectoryName(_databaseFilePath), "Backups");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var backupFileName = $"cost_database_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupFilePath = Path.Combine(backupDir, backupFileName);

                File.Copy(_databaseFilePath, backupFilePath, true);

                _persistenceStats.BackupsCreated++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database backup created: {backupFileName}", null);

                return true;
            }
            catch (Exception ex)
            {
                _persistenceStats.BackupErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to create database backup: {ex.Message}", null);

                return false;
            }
        }

        /// <summary>
        /// Restore from a specific backup
        /// </summary>
        public CostDatabaseData RestoreFromBackup(string backupFileName)
        {
            try
            {
                var backupDir = Path.Combine(Path.GetDirectoryName(_databaseFilePath), "Backups");
                var backupFilePath = Path.Combine(backupDir, backupFileName);

                if (!File.Exists(backupFilePath))
                {
                    var errorMessage = $"Backup file not found: {backupFileName}";
                    OnPersistenceError?.Invoke(errorMessage);

                    if (_enableLogging)
                        ChimeraLogger.LogWarning("DB_PERSIST", errorMessage, null);

                    return new CostDatabaseData();
                }

                var jsonContent = File.ReadAllText(backupFilePath);

                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var restoredData = JsonConvert.DeserializeObject<CostDatabaseData>(jsonContent, jsonSettings);

                _persistenceStats.BackupsRestored++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database restored from backup: {backupFileName}", null);

                return restoredData ?? new CostDatabaseData();
            }
            catch (Exception ex)
            {
                _persistenceStats.RestoreErrors++;
                var errorMessage = $"Failed to restore from backup {backupFileName}: {ex.Message}";

                OnPersistenceError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", errorMessage, null);

                return new CostDatabaseData();
            }
        }

        /// <summary>
        /// Get available backup files
        /// </summary>
        public List<DatabaseBackupInfo> GetAvailableBackups()
        {
            var backups = new List<DatabaseBackupInfo>();

            try
            {
                var backupDir = Path.Combine(Path.GetDirectoryName(_databaseFilePath), "Backups");
                if (!Directory.Exists(backupDir))
                    return backups;

                var backupFiles = Directory.GetFiles(backupDir, "cost_database_backup_*.json");

                foreach (var backupFile in backupFiles)
                {
                    var fileInfo = new FileInfo(backupFile);
                    var backupInfo = new DatabaseBackupInfo
                    {
                        FileName = Path.GetFileName(backupFile),
                        FilePath = backupFile,
                        CreatedAt = fileInfo.CreationTime,
                        FileSize = fileInfo.Length,
                        IsAccessible = fileInfo.Exists
                    };

                    backups.Add(backupInfo);
                }

                // Sort by creation time (newest first)
                backups.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to get available backups: {ex.Message}", null);
            }

            return backups;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Mark database as dirty (needs saving)
        /// </summary>
        public void MarkDirty()
        {
            _isDatabaseDirty = true;
        }

        /// <summary>
        /// Clear dirty flag
        /// </summary>
        public void ClearDirty()
        {
            _isDatabaseDirty = false;
        }

        /// <summary>
        /// Update persistence manager (call from Update loop)
        /// </summary>
        public void UpdatePersistence(Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                                    Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                                    List<CostDataPoint> historicalData)
        {
            if (_persistData && _isDatabaseDirty)
            {
                AutoSave(costDatabase, equipmentProfiles, historicalData);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if database file exists
        /// </summary>
        public bool DatabaseFileExists()
        {
            return File.Exists(_databaseFilePath);
        }

        /// <summary>
        /// Get database file size
        /// </summary>
        public long GetDatabaseFileSize()
        {
            try
            {
                if (File.Exists(_databaseFilePath))
                {
                    var fileInfo = new FileInfo(_databaseFilePath);
                    return fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to get database file size: {ex.Message}", null);
            }

            return 0;
        }

        /// <summary>
        /// Get database file last modified time
        /// </summary>
        public DateTime GetDatabaseFileLastModified()
        {
            try
            {
                if (File.Exists(_databaseFilePath))
                {
                    return File.GetLastWriteTime(_databaseFilePath);
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to get database file modification time: {ex.Message}", null);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Validate database file integrity
        /// </summary>
        public DatabaseValidationResult ValidateDatabaseFile()
        {
            var result = new DatabaseValidationResult
            {
                IsValid = false,
                ValidationTime = DateTime.Now
            };

            try
            {
                if (!File.Exists(_databaseFilePath))
                {
                    result.ErrorMessage = "Database file does not exist";
                    return result;
                }

                var jsonContent = File.ReadAllText(_databaseFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    result.ErrorMessage = "Database file is empty";
                    return result;
                }

                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var databaseData = JsonConvert.DeserializeObject<CostDatabaseData>(jsonContent, jsonSettings);

                if (databaseData == null)
                {
                    result.ErrorMessage = "Failed to deserialize database data";
                    return result;
                }

                // Basic validation
                result.EntriesCount = databaseData.CostDatabase?.Count ?? 0;
                result.ProfilesCount = databaseData.EquipmentProfiles?.Count ?? 0;
                result.HistoryCount = databaseData.HistoricalData?.Count ?? 0;
                result.FileSize = GetDatabaseFileSize();
                result.IsValid = true;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database file validation passed: {result.EntriesCount} entries, {result.ProfilesCount} profiles, {result.HistoryCount} history points", null);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _persistenceStats.ValidationErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Database file validation failed: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Reset persistence statistics
        /// </summary>
        public void ResetStatistics()
        {
            _persistenceStats = new DatabasePersistenceStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_PERSIST", "Database persistence statistics reset", null);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Database persistence statistics
    /// </summary>
    [System.Serializable]
    }
