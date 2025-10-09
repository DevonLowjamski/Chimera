using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// REFACTORED: Cost Database Persistence Manager Coordinator
    /// Single Responsibility: Coordinate database persistence through helper classes
    /// Reduced from 550 lines using composition with DatabaseSerializer and DatabaseFileIO
    /// </summary>
    public class CostDatabasePersistenceManager
    {
        private readonly bool _enableLogging;
        private readonly bool _persistData;
        private readonly float _autoSaveInterval;

        // Helper components (Composition pattern for SRP)
        private DatabaseSerializer _serializer;
        private DatabaseFileIO _fileIO;

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
            _autoSaveInterval = autoSaveInterval;

            // Initialize helper components
            string databaseFilePath = Path.Combine(Application.persistentDataPath, "Database", databaseFileName);
            _serializer = new DatabaseSerializer(enableLogging);
            _fileIO = new DatabaseFileIO(databaseFilePath, enableLogging);
        }

        // Properties
        public DatabasePersistenceStatistics Statistics => _persistenceStats;
        public bool IsPersistenceEnabled => _persistData;
        public bool IsDatabaseDirty => _isDatabaseDirty;
        public string DatabaseFilePath => _fileIO?.GetFileSize().ToString() ?? "0";

        public void Initialize()
        {
            _lastAutoSave = Time.time;

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_PERSIST", "Database persistence manager initialized", null);
        }

        public bool SaveDatabaseToFile(Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                                     Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                                     List<CostDataPoint> historicalData)
        {
            if (!_persistData) return false;

            try
            {
                var startTime = Time.realtimeSinceStartup;

                // Serialize database
                string json = _serializer.SerializeDatabase(costDatabase, equipmentProfiles, historicalData);
                if (string.IsNullOrEmpty(json))
                {
                    OnPersistenceError?.Invoke("Failed to serialize database");
                    return false;
                }

                // Write to file
                bool success = _fileIO.WriteToFile(json);
                if (success)
                {
                    _isDatabaseDirty = false;
                    _persistenceStats.LastSaveTime = DateTime.Now;
                    _persistenceStats.SaveOperations++;

                    var saveTime = Time.realtimeSinceStartup - startTime;
                    _persistenceStats.TotalSaveTime += saveTime;
                    // AverageSaveTime is computed automatically

                    OnDatabaseSaved?.Invoke(_fileIO.GetFileSize().ToString());

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("DB_PERSIST", $"Database saved successfully (took {saveTime:F3}s)", null);
                }
                else
                {
                    _persistenceStats.SaveErrors++;
                    OnPersistenceError?.Invoke("Failed to write database file");
                }

                return success;
            }
            catch (Exception ex)
            {
                _persistenceStats.SaveErrors++;
                OnPersistenceError?.Invoke($"Save error: {ex.Message}");

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to save database: {ex.Message}", null);

                return false;
            }
        }

        public (Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                List<CostDataPoint> historicalData) LoadDatabaseFromFile()
        {
            if (!_persistData || !_fileIO.FileExists())
            {
                return (
                    new Dictionary<MalfunctionType, CostDatabaseEntry>(),
                    new Dictionary<EquipmentType, EquipmentCostProfile>(),
                    new List<CostDataPoint>()
                );
            }

            try
            {
                var startTime = Time.realtimeSinceStartup;

                // Read from file
                string json = _fileIO.ReadFromFile();
                if (string.IsNullOrEmpty(json))
                {
                    OnPersistenceError?.Invoke("Failed to read database file");
                    return (
                        new Dictionary<MalfunctionType, CostDatabaseEntry>(),
                        new Dictionary<EquipmentType, EquipmentCostProfile>(),
                        new List<CostDataPoint>()
                    );
                }

                // Deserialize database
                var data = _serializer.DeserializeDatabase(json);

                _persistenceStats.LastLoadTime = DateTime.Now;
                _persistenceStats.LoadOperations++;

                var loadTime = Time.realtimeSinceStartup - startTime;
                _persistenceStats.TotalLoadTime += loadTime;
                // AverageLoadTime is computed automatically

                OnDatabaseLoaded?.Invoke(_fileIO.GetFileSize().ToString());

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", $"Database loaded successfully (took {loadTime:F3}s)", null);

                return data;
            }
            catch (Exception ex)
            {
                _persistenceStats.LoadErrors++;
                OnPersistenceError?.Invoke($"Load error: {ex.Message}");

                if (_enableLogging)
                    ChimeraLogger.LogError("DB_PERSIST", $"Failed to load database: {ex.Message}", null);

                return (
                    new Dictionary<MalfunctionType, CostDatabaseEntry>(),
                    new Dictionary<EquipmentType, EquipmentCostProfile>(),
                    new List<CostDataPoint>()
                );
            }
        }

        public void UpdateAutoSave(Dictionary<MalfunctionType, CostDatabaseEntry> costDatabase,
                                  Dictionary<EquipmentType, EquipmentCostProfile> equipmentProfiles,
                                  List<CostDataPoint> historicalData)
        {
            if (!_persistData || !_isDatabaseDirty) return;

            float currentTime = Time.time;
            if (currentTime - _lastAutoSave >= _autoSaveInterval)
            {
                SaveDatabaseToFile(costDatabase, equipmentProfiles, historicalData);
                _lastAutoSave = currentTime;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_PERSIST", "Auto-save triggered", null);
            }
        }

        public void MarkDirty()
        {
            _isDatabaseDirty = true;
        }

        public void ResetStatistics()
        {
            _persistenceStats = new DatabasePersistenceStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("DB_PERSIST", "Persistence statistics reset", null);
        }

        public bool DeleteDatabase()
        {
            return _fileIO.DeleteFile();
        }

        public long GetDatabaseFileSize()
        {
            return _fileIO.GetFileSize();
        }
    }
}

