using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using StorageDataResult = ProjectChimera.Data.Save.StorageDataResult;
using StorageMetrics = ProjectChimera.Data.Save.StorageMetrics;
using StorageResult = ProjectChimera.Data.Save.StorageResult;

namespace ProjectChimera.Systems.Save.Storage
{
    /// <summary>
    /// Core implementation for load operations and data retrieval
    /// </summary>
    public class LoadCore : ProjectChimera.Data.Save.ILoadCore
    {
        private string _baseSaveDirectory;
        private string _fullBackupDirectory;
        private string _saveFileExtension = ".save";
        private string _backupFileExtension = ".backup";

        // Storage metrics for read operations
        private StorageMetrics _metrics = new StorageMetrics();
        private bool _isInitialized = false;
        private ISerializationHelpers _serializationHelpers;

        public StorageMetrics Metrics => _metrics;

        public LoadCore(ISerializationHelpers serializationHelpers = null)
        {
            _serializationHelpers = serializationHelpers;
        }

        public void Initialize(string saveDirectory, string backupDirectory, string archiveDirectory = null)
        {
            if (_isInitialized) return;

            _baseSaveDirectory = Path.Combine(Application.persistentDataPath, saveDirectory);
            _fullBackupDirectory = Path.Combine(_baseSaveDirectory, backupDirectory);

            _isInitialized = true;
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        public void Shutdown()
        {
            _isInitialized = false;
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        public async Task<StorageDataResult> ReadFileAsync(string slotName)
        {
            if (!_isInitialized)
            {
                return StorageDataResult.CreateFailure("LoadCore not initialized");
            }

            if (string.IsNullOrEmpty(slotName))
            {
                return StorageDataResult.CreateFailure("Invalid slot name");
            }

            try
            {
                string filePath = GetSaveFilePath(slotName);

                if (!File.Exists(filePath))
                {
                    ChimeraLogger.Log("OTHER", "$1", null);
                    return StorageDataResult.CreateFailure($"File not found: {slotName}");
                }

                // Check file integrity before reading
                if (_serializationHelpers != null && !await _serializationHelpers.CheckDataIntegrityAsync(filePath))
                {
                    ChimeraLogger.Log("OTHER", "$1", null);

                    // Try to restore from backup
                    var backupResult = await TryRestoreFromBackup(slotName);
                    if (!backupResult.Success)
                    {
                        return StorageDataResult.CreateFailure($"File corrupted and backup restoration failed: {slotName}");
                    }
                }

                byte[] data = await File.ReadAllBytesAsync(filePath);

                // Process data through serialization helpers if available
                if (_serializationHelpers != null)
                {
                    var processedResult = await _serializationHelpers.ProcessIncomingDataAsync(data);
                    if (!processedResult.Success)
                    {
                        return processedResult;
                    }
                    data = processedResult.Data;
                }

                _metrics.TotalReads++;
                _metrics.TotalBytesRead += data.Length;

                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageDataResult.CreateSuccess(data);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageDataResult.CreateFailure(ex.Message);
            }
        }

        public async Task<ProjectChimera.Data.Save.StorageInfo> GetStorageInfoAsync()
        {
            if (!_isInitialized)
            {
                return new ProjectChimera.Data.Save.StorageInfo { IsValid = false, ErrorMessage = "LoadCore not initialized" };
            }

            try
            {
                var saveFiles = await GetSaveSlotListAsync();
                long totalSize = 0;
                int totalFiles = saveFiles.Count;

                foreach (var slot in saveFiles)
                {
                    var size = await GetFileSizeAsync(slot);
                    totalSize += size;
                }

                var driveInfo = new DriveInfo(Path.GetPathRoot(_baseSaveDirectory));

                return new ProjectChimera.Data.Save.StorageInfo
                {
                    IsValid = true,
                    TotalSaveFiles = totalFiles,
                    TotalSizeBytes = totalSize,
                    AvailableSpaceBytes = driveInfo.AvailableFreeSpace,
                    TotalSpaceBytes = driveInfo.TotalSize,
                    SaveDirectory = _baseSaveDirectory,
                    LastChecked = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return new ProjectChimera.Data.Save.StorageInfo { IsValid = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<bool> FileExistsAsync(string slotName)
        {
            if (!_isInitialized) return false;

            await Task.Yield();
            string filePath = GetSaveFilePath(slotName);
            return File.Exists(filePath);
        }

        public async Task<long> GetFileSizeAsync(string slotName)
        {
            if (!_isInitialized) return 0;

            try
            {
                await Task.Yield();
                string filePath = GetSaveFilePath(slotName);

                if (!File.Exists(filePath))
                {
                    return 0;
                }

                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return 0;
            }
        }

        public async Task<DateTime> GetFileLastModifiedAsync(string slotName)
        {
            if (!_isInitialized) return DateTime.MinValue;

            try
            {
                await Task.Yield();
                string filePath = GetSaveFilePath(slotName);

                if (!File.Exists(filePath))
                {
                    return DateTime.MinValue;
                }

                return File.GetLastWriteTime(filePath);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return DateTime.MinValue;
            }
        }

        public async Task<List<string>> GetSaveSlotListAsync()
        {
            if (!_isInitialized) return new List<string>();

            try
            {
                await Task.Yield();

                if (!Directory.Exists(_baseSaveDirectory))
                {
                    return new List<string>();
                }

                var files = Directory.GetFiles(_baseSaveDirectory, $"*{_saveFileExtension}")
                    .Select(path => Path.GetFileNameWithoutExtension(path))
                    .OrderBy(name => name)
                    .ToList();

                ChimeraLogger.Log("OTHER", "$1", null);
                return files;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetBackupListAsync(string slotName)
        {
            if (!_isInitialized) return new List<string>();

            try
            {
                await Task.Yield();

                if (!Directory.Exists(_fullBackupDirectory))
                {
                    return new List<string>();
                }

                var pattern = $"{slotName}_*{_backupFileExtension}";
                var backupFiles = Directory.GetFiles(_fullBackupDirectory, pattern)
                    .Select(path => Path.GetFileName(path))
                    .OrderByDescending(name => name) // Most recent first
                    .ToList();

                ChimeraLogger.Log("OTHER", "$1", null);
                return backupFiles;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return new List<string>();
            }
        }

        public async Task<StorageDataResult> ReadBackupAsync(string slotName, string backupFileName = null)
        {
            if (!_isInitialized)
            {
                return new StorageDataResult { Success = false, Message = "LoadCore not initialized" };
            }

            try
            {
                string backupFilePath;

                if (string.IsNullOrEmpty(backupFileName))
                {
                    // Get the most recent backup
                    var backups = await GetBackupListAsync(slotName);
                    if (!backups.Any())
                    {
                        return new StorageDataResult { Success = false, Message = $"No backups found for slot {slotName}" };
                    }
                    backupFilePath = Path.Combine(_fullBackupDirectory, backups.First());
                }
                else
                {
                    backupFilePath = Path.Combine(_fullBackupDirectory, backupFileName);
                }

                if (!File.Exists(backupFilePath))
                {
                    return new StorageDataResult { Success = false, Message = $"Backup file not found: {backupFileName ?? "latest"}" };
                }

                byte[] data = await File.ReadAllBytesAsync(backupFilePath);

                // Process data through serialization helpers if available
                if (_serializationHelpers != null)
                {
                    var processedResult = await _serializationHelpers.ProcessIncomingDataAsync(data);
                    if (!processedResult.Success)
                    {
                        return processedResult;
                    }
                    data = processedResult.Data;
                }

                ChimeraLogger.Log("OTHER", "$1", null);
                return new StorageDataResult { Success = true, Data = data, Message = "Backup loaded successfully" };
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return new StorageDataResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<bool> ValidateFileIntegrityAsync(string slotName)
        {
            if (!_isInitialized || _serializationHelpers == null) return false;

            try
            {
                string filePath = GetSaveFilePath(slotName);

                if (!File.Exists(filePath))
                {
                    return false;
                }

                return await _serializationHelpers.CheckDataIntegrityAsync(filePath);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return false;
            }
        }

        public void SetSerializationHelpers(ISerializationHelpers serializationHelpers)
        {
            _serializationHelpers = serializationHelpers;
        }

        public StorageMetrics GetMetrics()
        {
            return _metrics;
        }

        private string GetSaveFilePath(string slotName)
        {
            return Path.Combine(_baseSaveDirectory, $"{slotName}{_saveFileExtension}");
        }

        private async Task<StorageResult> TryRestoreFromBackup(string slotName)
        {
            try
            {
                var backups = await GetBackupListAsync(slotName);
                if (!backups.Any())
                {
                    return StorageResult.CreateFailure("No backups available for restoration");
                }

                // Try to restore from the most recent backup
                var latestBackup = backups.First();
                var backupPath = Path.Combine(_fullBackupDirectory, latestBackup);
                var savePath = GetSaveFilePath(slotName);

                // Validate backup integrity first
                if (_serializationHelpers != null && !await _serializationHelpers.CheckDataIntegrityAsync(backupPath))
                {
                    ChimeraLogger.Log("OTHER", "$1", null);

                    // Try older backups
                    foreach (var backup in backups.Skip(1))
                    {
                        var backupFilePath = Path.Combine(_fullBackupDirectory, backup);
                        if (await _serializationHelpers.CheckDataIntegrityAsync(backupFilePath))
                        {
                            File.Copy(backupFilePath, savePath, true);
                            ChimeraLogger.Log("OTHER", "$1", null);
                            return StorageResult.CreateSuccess($"Restored from backup: {backup}");
                        }
                    }

                    return StorageResult.CreateFailure("All backups are corrupted");
                }

                File.Copy(backupPath, savePath, true);
                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateSuccess($"Restored from backup: {latestBackup}");
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateFailure($"Backup restoration failed: {ex.Message}");
            }
        }

                // Remove duplicate methods - they already exist above in the class

        // Duplicate method removed - using property implementation above

        // Additional interface methods required by ILoadCore
        public async Task<ProjectChimera.Data.Save.StorageInfo> GetStorageInfoAsync(string slotName)
        {
            try
            {
                var fileName = $"{slotName}{_saveFileExtension}";
                var filePath = Path.Combine(_baseSaveDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    return new ProjectChimera.Data.Save.StorageInfo
                    {
                        IsValid = false,
                        FileName = fileName,
                        FileSize = 0,
                        LastModified = DateTime.MinValue
                    };
                }

                var fileInfo = new FileInfo(filePath);
                return new ProjectChimera.Data.Save.StorageInfo
                {
                    Exists = true,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                };
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                return new ProjectChimera.Data.Save.StorageInfo { Exists = false, FileName = slotName };
            }
        }

        // Additional required interface methods
        public async Task<StorageDataResult> ReadFileAsync(string fileName, string directory = null)
        {
            // Implementation that takes both fileName and optional directory
            return await ReadFileAsync(fileName);
        }
    }
}
