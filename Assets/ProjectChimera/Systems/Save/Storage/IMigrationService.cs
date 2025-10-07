using ProjectChimera.Data.Save;
using ProjectChimera.Systems.Save.Components;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Interface for backup management, cloud sync, and storage optimization
    /// </summary>
    public interface IMigrationService
    {
        // Backup Management
        Task<StorageResult> CreateBackupAsync(string slotName, bool incremental = false);
        Task<StorageResult> RestoreFromBackupAsync(string slotName, string backupFileName = null);
        Task CleanupOldBackupsAsync(string slotName);
        Task DeleteBackupsAsync(string slotName);
        Task MoveBackupsAsync(string fromSlot, string toSlot);

        // File Migration
        Task<StorageResult> MoveFileAsync(string fromSlotName, string toSlotName);
        Task<StorageResult> ArchiveFileAsync(string slotName);
        Task<StorageResult> RestoreArchivedFileAsync(string slotName);

        // Cloud Sync
        Task<ProjectChimera.Data.Save.CloudSyncResult> SyncToCloudAsync(string slotName);
        Task<ProjectChimera.Data.Save.CloudSyncResult> SyncFromCloudAsync(string slotName);
        ProjectChimera.Data.Save.CloudSyncStatus GetCloudSyncStatus(string slotName);
        void SetCloudProvider(ProjectChimera.Systems.Save.Storage.ICloudStorageProvider provider);

        // Storage Optimization
        Task<StorageResult> OptimizeStorageAsync();
        Task<StorageResult> ForceCleanupAsync();
        Task<StorageResult> DefragmentStorageAsync();
        Task<long> CalculateStorageUsageAsync();
        Task<ProjectChimera.Data.Save.StorageOptimizationReport> AnalyzeStorageAsync();

        // Monitoring
        bool IsMonitoringEnabled { get; set; }
        void StartStorageMonitoring();
        void StopStorageMonitoring();

        void Initialize(bool enableAutoBackup, int maxBackupsPerSlot, int maxTotalBackups,
                       TimeSpan backupRetentionPeriod, bool enableCloudSync, bool enableStorageOptimization,
                       long minFreeSpaceBytes, bool enableAutoCleanup, float cleanupThresholdPercent);
        void Shutdown();
    }
}
