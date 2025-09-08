using System.Threading.Tasks;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Core save operations interface
    /// </summary>
    public interface ISaveCore
    {
        bool IsInitialized { get; }
        StorageMetrics Metrics { get; }

        void Initialize(string saveDirectory, string backupDirectory, string archiveDirectory = null);
        void Shutdown();

        Task<StorageResult> WriteFileAsync(string fileName, byte[] data, bool createBackup = true);
        Task<StorageResult> DeleteFileAsync(string fileName, bool deleteBackups = true);

        Task<string> BeginTransactionAsync();
        Task<StorageResult> BeginTransactionAsync(string slotName);
        Task<StorageResult> CommitTransactionAsync(string transactionId);
        Task<StorageResult> RollbackTransactionAsync(string transactionId);
        
        string GetSaveFilePath(string slotName);
        string GetTempFilePath(string slotName);
        string GetBackupFilePath(string slotName, string backupSuffix = null);
        
        Task<bool> HasSufficientDiskSpaceAsync(long requiredBytes);
        Task QueueOperation(FileOperation operation);
    }
    
}
