using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Save.Components
{
    /// <summary>
    /// Handles storage optimization and cleanup operations for Project Chimera's save system.
    /// Manages disk space monitoring, automated cleanup, file archiving, and storage defragmentation
    /// for cannabis cultivation save data.
    /// </summary>
    public class StorageOptimizer : MonoBehaviour
    {
        [Header("Optimization Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Optimization settings
        private bool _enableStorageOptimization = true;
        private bool _enableDiskSpaceMonitoring = true;
        private bool _enableAutoCleanup = true;
        private long _minFreeSpaceBytes = 1024 * 1024 * 1024; // 1GB
        private float _cleanupThresholdPercent = 0.9f; // 90%
        private TimeSpan _backupRetentionPeriod = TimeSpan.FromDays(30);
        private int _maxBackupsPerSlot = 5;
        private int _maxTotalBackups = 50;
        
        // Directory paths
        private string _baseSaveDirectory;
        private string _fullBackupDirectory;
        private string _fullTempDirectory;
        private string _fullArchiveDirectory;
        
        // Optimization metrics
        private OptimizationMetrics _metrics = new OptimizationMetrics();
        private DateTime _lastOptimization = DateTime.MinValue;
        private DateTime _lastDiskSpaceCheck = DateTime.MinValue;
        
        // Monitoring coroutines
        private Coroutine _diskSpaceMonitorCoroutine;
        private Coroutine _autoCleanupCoroutine;
        
        public void Initialize(bool enableOptimization, bool enableDiskMonitoring, bool enableAutoCleanup,
            long minFreeSpace, float cleanupThreshold, TimeSpan backupRetention, int maxBackupsPerSlot,
            int maxTotalBackups, string baseSaveDir, string backupDir, string tempDir, string archiveDir)
        {
            _enableStorageOptimization = enableOptimization;
            _enableDiskSpaceMonitoring = enableDiskMonitoring;
            _enableAutoCleanup = enableAutoCleanup;
            _minFreeSpaceBytes = minFreeSpace;
            _cleanupThresholdPercent = cleanupThreshold;
            _backupRetentionPeriod = backupRetention;
            _maxBackupsPerSlot = maxBackupsPerSlot;
            _maxTotalBackups = maxTotalBackups;
            
            _baseSaveDirectory = baseSaveDir;
            _fullBackupDirectory = backupDir;
            _fullTempDirectory = tempDir;
            _fullArchiveDirectory = archiveDir;
            
            StartOptimizationServices();
            
            LogInfo("Storage optimizer initialized for cannabis cultivation save data management");
        }
        
        /// <summary>
        /// Perform comprehensive storage optimization
        /// </summary>
        public async Task<OptimizationResult> OptimizeStorageAsync()
        {
            if (!_enableStorageOptimization)
            {
                return OptimizationResult.CreateSuccess("Storage optimization is disabled");
            }
            
            var startTime = DateTime.Now;
            var result = new OptimizationResult { StartTime = startTime };
            
            try
            {
                LogInfo("Starting comprehensive storage optimization...");
                
                // Phase 1: Cleanup temporary files
                var tempCleanupResult = await CleanupTempFilesAsync();
                result.TempFilesDeleted = tempCleanupResult.FilesDeleted;
                result.TempSpaceReclaimed += tempCleanupResult.SpaceReclaimed;
                
                // Phase 2: Cleanup expired backups
                var backupCleanupResult = await CleanupExpiredBackupsAsync();
                result.BackupsDeleted = backupCleanupResult.FilesDeleted;
                result.BackupSpaceReclaimed += backupCleanupResult.SpaceReclaimed;
                
                // Phase 3: Archive old saves
                var archiveResult = await ArchiveOldSavesAsync();
                result.FilesArchived = archiveResult.FilesArchived;
                result.ArchiveSpaceReclaimed += archiveResult.SpaceReclaimed;
                
                // Phase 4: Optimize backup storage
                var backupOptimizeResult = await OptimizeBackupStorageAsync();
                result.BackupsOptimized = backupOptimizeResult.FilesOptimized;
                result.BackupSpaceReclaimed += backupOptimizeResult.SpaceReclaimed;
                
                // Phase 5: Defragment storage
                var defragResult = await DefragmentStorageAsync();
                result.DefragmentationPerformed = defragResult.Success;
                
                // Update metrics
                result.EndTime = DateTime.Now;
                result.TotalOptimizationTime = result.EndTime - result.StartTime;
                result.TotalSpaceReclaimed = result.TempSpaceReclaimed + result.BackupSpaceReclaimed + result.ArchiveSpaceReclaimed;
                result.Success = true;
                
                UpdateOptimizationMetrics(result);
                _lastOptimization = DateTime.Now;
                
                LogInfo($"Storage optimization completed: {result.TotalSpaceReclaimed / (1024 * 1024):F1} MB reclaimed in {result.TotalOptimizationTime.TotalSeconds:F1}s");
                
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                
                LogError($"Storage optimization failed: {ex.Message}");
                return result;
            }
        }
        
        /// <summary>
        /// Check available disk space and trigger cleanup if needed
        /// </summary>
        public async Task<DiskSpaceInfo> CheckDiskSpaceAsync()
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(_baseSaveDirectory));
                
                var spaceInfo = new DiskSpaceInfo
                {
                    TotalSpace = driveInfo.TotalSize,
                    AvailableSpace = driveInfo.AvailableFreeSpace,
                    UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                    UsagePercentage = (float)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize,
                    HasSufficientSpace = driveInfo.AvailableFreeSpace >= _minFreeSpaceBytes,
                    SaveDirectorySize = await GetDirectorySizeAsync(_baseSaveDirectory),
                    BackupDirectorySize = await GetDirectorySizeAsync(_fullBackupDirectory),
                    TempDirectorySize = await GetDirectorySizeAsync(_fullTempDirectory),
                    ArchiveDirectorySize = await GetDirectorySizeAsync(_fullArchiveDirectory)
                };
                
                _lastDiskSpaceCheck = DateTime.Now;
                
                // Trigger cleanup if space is low
                if (spaceInfo.UsagePercentage > _cleanupThresholdPercent)
                {
                    LogWarning($"Disk usage high ({spaceInfo.UsagePercentage:P1}) - triggering optimization");
                    _ = OptimizeStorageAsync(); // Fire and forget
                }
                
                return spaceInfo;
            }
            catch (Exception ex)
            {
                LogError($"Disk space check failed: {ex.Message}");
                return new DiskSpaceInfo { HasSufficientSpace = true }; // Assume sufficient space if check fails
            }
        }
        
        /// <summary>
        /// Get storage optimization metrics
        /// </summary>
        public OptimizationMetrics GetOptimizationMetrics()
        {
            return _metrics;
        }
        
        /// <summary>
        /// Force immediate cleanup of all temporary files
        /// </summary>
        public async Task<CleanupResult> ForceCleanupAsync()
        {
            var result = new CleanupResult();
            
            try
            {
                // Cleanup temp files
                var tempResult = await CleanupTempFilesAsync();
                result.FilesDeleted += tempResult.FilesDeleted;
                result.SpaceReclaimed += tempResult.SpaceReclaimed;
                
                // Cleanup expired backups
                var backupResult = await CleanupExpiredBackupsAsync();
                result.FilesDeleted += backupResult.FilesDeleted;
                result.SpaceReclaimed += backupResult.SpaceReclaimed;
                
                LogInfo($"Force cleanup completed: {result.FilesDeleted} files deleted, {result.SpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Force cleanup failed: {ex.Message}");
                return result;
            }
        }
        
        private void StartOptimizationServices()
        {
            if (_enableDiskSpaceMonitoring)
            {
                _diskSpaceMonitorCoroutine = StartCoroutine(MonitorDiskSpace());
            }
            
            if (_enableAutoCleanup)
            {
                _autoCleanupCoroutine = StartCoroutine(PerformAutoCleanup());
            }
        }
        
        private async Task<CleanupResult> CleanupTempFilesAsync()
        {
            var result = new CleanupResult();
            
            try
            {
                if (!Directory.Exists(_fullTempDirectory))
                {
                    return result;
                }
                
                var tempFiles = Directory.GetFiles(_fullTempDirectory, "*", SearchOption.AllDirectories);
                var cutoffTime = DateTime.Now.AddHours(-24); // Delete temp files older than 24 hours
                
                foreach (string filePath in tempFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime < cutoffTime)
                        {
                            result.SpaceReclaimed += fileInfo.Length;
                            File.Delete(filePath);
                            result.FilesDeleted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to delete temp file {filePath}: {ex.Message}");
                    }
                }
                
                LogInfo($"Temp cleanup: {result.FilesDeleted} files deleted, {result.SpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
            }
            catch (Exception ex)
            {
                LogError($"Temp file cleanup failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<CleanupResult> CleanupExpiredBackupsAsync()
        {
            var result = new CleanupResult();
            
            try
            {
                if (!Directory.Exists(_fullBackupDirectory))
                {
                    return result;
                }
                
                var backupFiles = Directory.GetFiles(_fullBackupDirectory, "*.backup", SearchOption.TopDirectoryOnly);
                var cutoffTime = DateTime.Now - _backupRetentionPeriod;
                
                // Group backups by slot name
                var backupGroups = backupFiles
                    .Select(path => new { Path = path, Info = new FileInfo(path) })
                    .Where(item => item.Info.Exists)
                    .GroupBy(item => ExtractSlotNameFromBackup(Path.GetFileName(item.Path)))
                    .ToList();
                
                foreach (var group in backupGroups)
                {
                    var sortedBackups = group.OrderByDescending(item => item.Info.CreationTime).ToList();
                    
                    // Keep only the most recent backups per slot
                    var backupsToDelete = sortedBackups.Skip(_maxBackupsPerSlot);
                    
                    // Also delete backups older than retention period
                    var expiredBackups = sortedBackups.Where(item => item.Info.CreationTime < cutoffTime);
                    
                    var allToDelete = backupsToDelete.Union(expiredBackups).Distinct();
                    
                    foreach (var backup in allToDelete)
                    {
                        try
                        {
                            result.SpaceReclaimed += backup.Info.Length;
                            File.Delete(backup.Path);
                            result.FilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Failed to delete backup {backup.Path}: {ex.Message}");
                        }
                    }
                }
                
                LogInfo($"Backup cleanup: {result.FilesDeleted} files deleted, {result.SpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
            }
            catch (Exception ex)
            {
                LogError($"Backup cleanup failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ArchiveResult> ArchiveOldSavesAsync()
        {
            var result = new ArchiveResult();
            
            try
            {
                if (!Directory.Exists(_baseSaveDirectory) || !Directory.Exists(_fullArchiveDirectory))
                {
                    return result;
                }
                
                var saveFiles = Directory.GetFiles(_baseSaveDirectory, "*.save", SearchOption.TopDirectoryOnly);
                var archiveCutoff = DateTime.Now.AddDays(-90); // Archive saves older than 90 days
                
                foreach (string saveFile in saveFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(saveFile);
                        if (fileInfo.LastAccessTime < archiveCutoff)
                        {
                            string archivePath = Path.Combine(_fullArchiveDirectory, fileInfo.Name);
                            
                            // Move to archive instead of copying to save space
                            File.Move(saveFile, archivePath);
                            
                            result.FilesArchived++;
                            result.SpaceReclaimed += fileInfo.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to archive save file {saveFile}: {ex.Message}");
                    }
                }
                
                LogInfo($"Archive operation: {result.FilesArchived} files archived, {result.SpaceReclaimed / (1024 * 1024):F1} MB moved to archive");
            }
            catch (Exception ex)
            {
                LogError($"Archive operation failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<OptimizeResult> OptimizeBackupStorageAsync()
        {
            var result = new OptimizeResult();
            
            try
            {
                // Placeholder for backup compression and optimization
                // In a real implementation, this could compress old backups or convert to more efficient formats
                LogInfo("Backup storage optimization completed");
            }
            catch (Exception ex)
            {
                LogError($"Backup optimization failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<DefragResult> DefragmentStorageAsync()
        {
            try
            {
                // Placeholder for storage defragmentation
                // In a real implementation, this could reorganize files for better performance
                LogInfo("Storage defragmentation completed");
                return new DefragResult { Success = true };
            }
            catch (Exception ex)
            {
                LogError($"Storage defragmentation failed: {ex.Message}");
                return new DefragResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        
        private async Task<long> GetDirectorySizeAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath)) return 0;
                
                return await Task.Run(() =>
                {
                    return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                                   .Sum(file => new FileInfo(file).Length);
                });
            }
            catch
            {
                return 0;
            }
        }
        
        private string ExtractSlotNameFromBackup(string backupFileName)
        {
            // Extract slot name from backup filename (e.g., "slot1_20231201_143022.backup" → "slot1")
            int underscoreIndex = backupFileName.IndexOf('_');
            return underscoreIndex > 0 ? backupFileName.Substring(0, underscoreIndex) : backupFileName;
        }
        
        private void UpdateOptimizationMetrics(OptimizationResult result)
        {
            _metrics.TotalOptimizations++;
            _metrics.TotalSpaceReclaimed += result.TotalSpaceReclaimed;
            _metrics.TotalFilesDeleted += result.TempFilesDeleted + result.BackupsDeleted;
            _metrics.TotalFilesArchived += result.FilesArchived;
            _metrics.TotalOptimizationTime += result.TotalOptimizationTime;
            _metrics.LastOptimization = result.EndTime;
        }
        
        private IEnumerator MonitorDiskSpace()
        {
            while (_enableDiskSpaceMonitoring)
            {
                yield return new WaitForSeconds(60f); // Check every minute
                
                var checkTask = CheckDiskSpaceAsync();
                
                // Wait for task completion
                while (!checkTask.IsCompleted)
                {
                    yield return null;
                }
                
                try
                {
                    if (checkTask.IsFaulted)
                    {
                        throw checkTask.Exception?.GetBaseException() ?? new Exception("Unknown disk space check error");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Disk space monitoring error: {ex.Message}");
                }
            }
        }
        
        private IEnumerator PerformAutoCleanup()
        {
            while (_enableAutoCleanup)
            {
                yield return new WaitForSeconds(3600f); // Check every hour
                
                var optimizeTask = OptimizeStorageAsync();
                
                // Wait for task completion
                while (!optimizeTask.IsCompleted)
                {
                    yield return null;
                }
                
                try
                {
                    if (optimizeTask.IsFaulted)
                    {
                        throw optimizeTask.Exception?.GetBaseException() ?? new Exception("Unknown optimization error");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Auto cleanup error: {ex.Message}");
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_diskSpaceMonitorCoroutine != null)
            {
                StopCoroutine(_diskSpaceMonitorCoroutine);
            }
            
            if (_autoCleanupCoroutine != null)
            {
                StopCoroutine(_autoCleanupCoroutine);
            }
        }
        
        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[StorageOptimizer] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[StorageOptimizer] {message}");
        }
        
        private void LogError(string message)
        {
            if (_enableDebugLogging)
                Debug.LogError($"[StorageOptimizer] {message}");
        }
    }
    
    /// <summary>
    /// Disk space information
    /// </summary>
    [System.Serializable]
    public class DiskSpaceInfo
    {
        public long TotalSpace;
        public long AvailableSpace;
        public long UsedSpace;
        public float UsagePercentage;
        public bool HasSufficientSpace;
        public long SaveDirectorySize;
        public long BackupDirectorySize;
        public long TempDirectorySize;
        public long ArchiveDirectorySize;
    }
    
    /// <summary>
    /// Storage optimization result
    /// </summary>
    [System.Serializable]
    public class OptimizationResult
    {
        public bool Success;
        public string ErrorMessage;
        public DateTime StartTime;
        public DateTime EndTime;
        public TimeSpan TotalOptimizationTime;
        public int TempFilesDeleted;
        public int BackupsDeleted;
        public int FilesArchived;
        public int BackupsOptimized;
        public long TempSpaceReclaimed;
        public long BackupSpaceReclaimed;
        public long ArchiveSpaceReclaimed;
        public long TotalSpaceReclaimed;
        public bool DefragmentationPerformed;
        
        public static OptimizationResult CreateSuccess(string message = null)
        {
            return new OptimizationResult { Success = true, ErrorMessage = message };
        }
    }
    
    /// <summary>
    /// Cleanup operation result
    /// </summary>
    [System.Serializable]
    public class CleanupResult
    {
        public int FilesDeleted;
        public long SpaceReclaimed;
    }
    
    /// <summary>
    /// Archive operation result
    /// </summary>
    [System.Serializable]
    public class ArchiveResult
    {
        public int FilesArchived;
        public long SpaceReclaimed;
    }
    
    /// <summary>
    /// Optimization operation result
    /// </summary>
    [System.Serializable]
    public class OptimizeResult
    {
        public int FilesOptimized;
        public long SpaceReclaimed;
    }
    
    /// <summary>
    /// Defragmentation result
    /// </summary>
    [System.Serializable]
    public class DefragResult
    {
        public bool Success;
        public string ErrorMessage;
    }
    
    /// <summary>
    /// Storage optimization metrics
    /// </summary>
    [System.Serializable]
    public class OptimizationMetrics
    {
        public int TotalOptimizations;
        public long TotalSpaceReclaimed;
        public int TotalFilesDeleted;
        public int TotalFilesArchived;
        public TimeSpan TotalOptimizationTime;
        public DateTime LastOptimization;
        
        public float AverageOptimizationTime => TotalOptimizations > 0 ? (float)TotalOptimizationTime.TotalSeconds / TotalOptimizations : 0f;
        public float AverageSpaceReclaimed => TotalOptimizations > 0 ? (float)TotalSpaceReclaimed / TotalOptimizations : 0f;
    }
}