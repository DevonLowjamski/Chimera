using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Configuration Persistence Manager (Coordinator)
    /// Single Responsibility: Orchestrate configuration persistence using dedicated components
    /// Original file (686 lines) refactored into 4 files (<500 lines each)
    /// </summary>
    public class ConfigurationPersistenceManager
    {
        private readonly bool _enableLogging;
        private readonly bool _persistConfiguration;
        private readonly float _autoSaveInterval;
        private readonly float _backupInterval;

        // Dependencies
        private ConfigurationSerializer _serializer;
        private ConfigurationBackupManager _backupManager;

        // Persistence state
        private float _lastAutoSave;
        private float _lastBackup;
        private bool _isDirty = false;

        // File paths
        private string _configFilePath;
        private string _backupDirectory;

        // Persistence statistics
        private PersistenceStatistics _persistenceStats = new PersistenceStatistics();

        // Events
        public event System.Action<string> OnConfigurationSaved;
        public event System.Action<string> OnConfigurationLoaded;
        public event System.Action<string> OnBackupCreated;
        public event System.Action<string> OnConfigurationError;

        public ConfigurationPersistenceManager(
            bool enableLogging = false,
            bool persistConfiguration = true,
            string configFileName = "cost_estimation_config.json",
            float autoSaveInterval = 600f,
            bool enableBackups = true,
            int maxBackups = 5,
            float backupInterval = 3600f,
            bool compressBackups = true)
        {
            _enableLogging = enableLogging;
            _persistConfiguration = persistConfiguration;
            _autoSaveInterval = autoSaveInterval;
            _backupInterval = backupInterval;

            InitializeFilePaths(configFileName);
            InitializeComponents(enableBackups, maxBackups, compressBackups);
        }

        // Properties
        public PersistenceStatistics Statistics => _persistenceStats;
        public bool IsDirty => _isDirty;
        public bool IsPersistenceEnabled => _persistConfiguration;
        public string ConfigurationFilePath => _configFilePath;
        public string BackupDirectory => _backupDirectory;

        #region Initialization

        /// <summary>
        /// Initialize file paths and directories
        /// </summary>
        private void InitializeFilePaths(string configFileName)
        {
            _configFilePath = Path.Combine(Application.persistentDataPath, "Config", configFileName);
            _backupDirectory = Path.Combine(Application.persistentDataPath, "Config", "Backups");

            // Ensure directories exist
            try
            {
                var configDir = Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                if (!Directory.Exists(_backupDirectory))
                    Directory.CreateDirectory(_backupDirectory);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Configuration path: {_configFilePath}", null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to initialize directories: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Initialize component dependencies
        /// </summary>
        private void InitializeComponents(bool enableBackups, int maxBackups, bool compressBackups)
        {
            // Create serializer
            _serializer = new ConfigurationSerializer(
                _enableLogging,
                _configFilePath,
                _persistenceStats,
                OnConfigurationSaved,
                OnConfigurationLoaded,
                OnConfigurationError
            );

            // Create backup manager
            _backupManager = new ConfigurationBackupManager(
                _enableLogging,
                enableBackups,
                maxBackups,
                compressBackups,
                _configFilePath,
                _backupDirectory,
                _persistenceStats,
                _serializer,
                OnBackupCreated,
                OnConfigurationError
            );
        }

        /// <summary>
        /// Initialize persistence manager
        /// </summary>
        public void Initialize()
        {
            _lastAutoSave = Time.time;
            _lastBackup = Time.time;

            _backupManager.Initialize();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PERSIST", "Configuration persistence manager initialized (Coordinator)", null);
        }

        #endregion

        #region Save Operations

        /// <summary>
        /// Save configuration profiles to disk
        /// </summary>
        public bool SaveConfiguration(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (!_persistConfiguration || profiles == null)
                return false;

            // Create backup before saving if enabled
            _backupManager.CreateBackupInternal();

            // Delegate to serializer
            var success = _serializer.SaveConfiguration(profiles);

            if (success)
            {
                _isDirty = false;
                _lastAutoSave = Time.time;
            }

            return success;
        }

        /// <summary>
        /// Auto-save if interval elapsed and configuration is dirty
        /// </summary>
        public bool AutoSave(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (!_persistConfiguration || !_isDirty)
                return true;

            if (Time.time - _lastAutoSave >= _autoSaveInterval)
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", "Performing auto-save", null);

                return SaveConfiguration(profiles);
            }

            return true;
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Load configuration profiles from disk
        /// </summary>
        public Dictionary<string, CostConfigurationProfile> LoadConfiguration()
        {
            if (!_persistConfiguration)
                return new Dictionary<string, CostConfigurationProfile>();

            // Delegate to serializer
            return _serializer.LoadConfiguration();
        }

        #endregion

        #region Backup Management

        /// <summary>
        /// Create a configuration backup
        /// </summary>
        public bool CreateBackup(Dictionary<string, CostConfigurationProfile> profiles)
        {
            return _backupManager.CreateBackup(profiles);
        }

        /// <summary>
        /// Auto-backup if interval elapsed
        /// </summary>
        public bool AutoBackup(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (Time.time - _lastBackup >= _backupInterval)
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", "Performing auto-backup", null);

                var success = _backupManager.CreateBackup(profiles);
                if (success)
                    _lastBackup = Time.time;

                return success;
            }

            return true;
        }

        /// <summary>
        /// Restore from a specific backup
        /// </summary>
        public Dictionary<string, CostConfigurationProfile> RestoreFromBackup(string backupId)
        {
            return _backupManager.RestoreFromBackup(backupId);
        }

        /// <summary>
        /// Get available backups information
        /// </summary>
        public IEnumerable<BackupInfo> GetAvailableBackups()
        {
            return _backupManager.GetAvailableBackups();
        }

        /// <summary>
        /// Clean up old and invalid backups
        /// </summary>
        public int CleanupBackups()
        {
            return _backupManager.CleanupBackups();
        }

        #endregion

        #region Profile Import/Export

        /// <summary>
        /// Export a single profile to a file
        /// </summary>
        public bool ExportProfile(CostConfigurationProfile profile, string filePath)
        {
            if (profile == null || string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = _serializer.SerializeProfile(profile);
                File.WriteAllText(filePath, json);

                _persistenceStats.TotalSaves++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Profile '{profile.Name}' exported to {filePath}", null);

                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to export profile: {ex.Message}", null);

                OnConfigurationError?.Invoke($"Export failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import a profile from a file
        /// </summary>
        public CostConfigurationProfile ImportProfile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CONFIG_PERSIST", $"Import file not found: {filePath}", null);
                return null;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var profile = _serializer.DeserializeProfile(json);

                _persistenceStats.TotalLoads++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Profile '{profile?.Name}' imported from {filePath}", null);

                return profile;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to import profile: {ex.Message}", null);

                OnConfigurationError?.Invoke($"Import failed: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Mark configuration as dirty (needs saving)
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Clear dirty flag
        /// </summary>
        public void ClearDirty()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Update persistence manager (call from Update loop)
        /// </summary>
        public void UpdatePersistence(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (_persistConfiguration && _isDirty)
            {
                AutoSave(profiles);
            }

            AutoBackup(profiles);
        }

        /// <summary>
        /// Reset persistence statistics
        /// </summary>
        public void ResetStatistics()
        {
            _persistenceStats = new PersistenceStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_PERSIST", "Persistence statistics reset", null);
        }

        #endregion
    }
}

