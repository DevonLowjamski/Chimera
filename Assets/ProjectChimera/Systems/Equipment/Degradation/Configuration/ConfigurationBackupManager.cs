using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectChimera.Core.Logging;
using Newtonsoft.Json;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Configuration Backup Manager
    /// Single Responsibility: Manage configuration backups (create, restore, cleanup)
    /// Extracted from ConfigurationPersistenceManager for better SRP compliance
    /// </summary>
    public class ConfigurationBackupManager
    {
        private readonly bool _enableLogging;
        private readonly bool _enableBackups;
        private readonly int _maxBackups;
        private readonly bool _compressBackups;
        private readonly string _configFilePath;
        private readonly string _backupDirectory;
        private PersistenceStatistics _stats;

        private readonly Queue<ConfigurationBackup> _backups = new Queue<ConfigurationBackup>();
        private readonly ConfigurationSerializer _serializer;

        private readonly Action<string> _onBackupCreated;
        private readonly Action<string> _onConfigurationError;

        public ConfigurationBackupManager(
            bool enableLogging,
            bool enableBackups,
            int maxBackups,
            bool compressBackups,
            string configFilePath,
            string backupDirectory,
            PersistenceStatistics stats,
            ConfigurationSerializer serializer,
            Action<string> onBackupCreated,
            Action<string> onConfigurationError)
        {
            _enableLogging = enableLogging;
            _enableBackups = enableBackups;
            _maxBackups = maxBackups;
            _compressBackups = compressBackups;
            _configFilePath = configFilePath;
            _backupDirectory = backupDirectory;
            _stats = stats;
            _serializer = serializer;
            _onBackupCreated = onBackupCreated;
            _onConfigurationError = onConfigurationError;
        }

        /// <summary>
        /// Initialize backup manager
        /// </summary>
        public void Initialize()
        {
            if (_enableBackups)
                LoadExistingBackups();
        }

        /// <summary>
        /// Create a configuration backup
        /// </summary>
        public bool CreateBackup(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (!_enableBackups || profiles == null)
                return false;

            try
            {
                var backup = new ConfigurationBackup
                {
                    Profiles = new Dictionary<string, CostConfigurationProfile>(profiles),
                    CreatedAt = DateTime.Now,
                    BackupId = Guid.NewGuid().ToString()
                };

                var backupFileName = $"config_backup_{DateTime.Now:yyyyMMdd_HHmmss}_{backup.BackupId.Substring(0, 8)}.json";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                var jsonContent = JsonConvert.SerializeObject(backup, jsonSettings);

                if (_compressBackups)
                {
                    jsonContent = _serializer.CompressString(jsonContent);
                    backupFilePath += ".compressed";
                }

                File.WriteAllText(backupFilePath, jsonContent);

                backup.FilePath = backupFilePath;
                backup.FileSize = new FileInfo(backupFilePath).Length;

                _backups.Enqueue(backup);
                _stats.TotalBackups++;

                // Maintain backup limit
                while (_backups.Count > _maxBackups)
                {
                    var oldBackup = _backups.Dequeue();
                    if (File.Exists(oldBackup.FilePath))
                    {
                        File.Delete(oldBackup.FilePath);
                        _stats.BackupsDeleted++;
                    }
                }

                _onBackupCreated?.Invoke(backupFilePath);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Configuration backup created: {backupFileName}", null);

                return true;
            }
            catch (Exception ex)
            {
                _stats.BackupErrors++;
                var errorMessage = $"Failed to create backup: {ex.Message}";

                _onConfigurationError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", errorMessage, null);

                return false;
            }
        }

        /// <summary>
        /// Internal backup creation (simple file copy)
        /// </summary>
        public bool CreateBackupInternal()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return false;

                var backupFileName = $"config_backup_pre_save_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                File.Copy(_configFilePath, backupFilePath, true);

                var backup = new ConfigurationBackup
                {
                    CreatedAt = DateTime.Now,
                    BackupId = Guid.NewGuid().ToString(),
                    FilePath = backupFilePath,
                    FileSize = new FileInfo(backupFilePath).Length
                };

                _backups.Enqueue(backup);

                // Maintain backup limit
                while (_backups.Count > _maxBackups)
                {
                    var oldBackup = _backups.Dequeue();
                    if (File.Exists(oldBackup.FilePath))
                        File.Delete(oldBackup.FilePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to create internal backup: {ex.Message}", null);
                return false;
            }
        }

        /// <summary>
        /// Load existing backups from directory
        /// </summary>
        private void LoadExistingBackups()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                    return;

                var backupFiles = Directory.GetFiles(_backupDirectory, "config_backup_*.json*")
                    .OrderBy(f => File.GetCreationTime(f))
                    .ToList();

                foreach (var backupFile in backupFiles.Take(_maxBackups))
                {
                    var backup = new ConfigurationBackup
                    {
                        CreatedAt = File.GetCreationTime(backupFile),
                        BackupId = Path.GetFileNameWithoutExtension(backupFile),
                        FilePath = backupFile,
                        FileSize = new FileInfo(backupFile).Length
                    };

                    _backups.Enqueue(backup);
                }

                if (_enableLogging && _backups.Count > 0)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Loaded {_backups.Count} existing backups", null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to load existing backups: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Restore from a specific backup
        /// </summary>
        public Dictionary<string, CostConfigurationProfile> RestoreFromBackup(string backupId)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.BackupId == backupId);
                if (backup == null || !File.Exists(backup.FilePath))
                {
                    if (_enableLogging)
                        ChimeraLogger.LogWarning("CONFIG_PERSIST", $"Backup {backupId} not found", null);
                    return null;
                }

                var jsonContent = File.ReadAllText(backup.FilePath);

                if (_compressBackups && backup.FilePath.EndsWith(".compressed"))
                {
                    jsonContent = _serializer.DecompressString(jsonContent);
                }

                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                var restoredBackup = JsonConvert.DeserializeObject<ConfigurationBackup>(jsonContent, jsonSettings);

                _stats.BackupsRestored++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Restored configuration from backup {backupId}", null);

                return restoredBackup.Profiles;
            }
            catch (Exception ex)
            {
                _stats.RestoreErrors++;
                var errorMessage = $"Failed to restore from backup {backupId}: {ex.Message}";

                _onConfigurationError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", errorMessage, null);

                return null;
            }
        }

        /// <summary>
        /// Get available backups information
        /// </summary>
        public IEnumerable<BackupInfo> GetAvailableBackups()
        {
            return _backups.Select(b => new BackupInfo
            {
                BackupId = b.BackupId,
                CreatedAt = b.CreatedAt,
                FilePath = b.FilePath,
                FileSize = b.FileSize,
                IsAccessible = File.Exists(b.FilePath)
            }).ToList();
        }

        /// <summary>
        /// Clean up old and invalid backups
        /// </summary>
        public int CleanupBackups()
        {
            var cleaned = 0;

            try
            {
                var backupsToRemove = new List<ConfigurationBackup>();

                foreach (var backup in _backups)
                {
                    if (!File.Exists(backup.FilePath))
                    {
                        backupsToRemove.Add(backup);
                        cleaned++;
                    }
                }

                foreach (var backup in backupsToRemove)
                {
                    var tempBackups = _backups.ToList();
                    tempBackups.Remove(backup);
                    _backups.Clear();
                    foreach (var b in tempBackups)
                        _backups.Enqueue(b);
                }

                if (_enableLogging && cleaned > 0)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Cleaned up {cleaned} invalid backup references", null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", $"Failed to cleanup backups: {ex.Message}", null);
            }

            return cleaned;
        }
    }
}

