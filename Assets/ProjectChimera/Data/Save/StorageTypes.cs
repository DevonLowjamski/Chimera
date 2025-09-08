using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Hash algorithm types for data validation
    /// </summary>
    public enum HashAlgorithmType
    {
        MD5,
        SHA1,
        SHA256,
        SHA512
    }

    /// <summary>
    /// Storage operation result
    /// </summary>
    public class StorageResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public long BytesProcessed { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static StorageResult CreateSuccess(string message = "Operation completed successfully", long bytesProcessed = 0)
        {
            return new StorageResult
            {
                Success = true,
                Message = message,
                BytesProcessed = bytesProcessed
            };
        }

        public static StorageResult CreateFailure(string message = "Operation failed")
        {
            return new StorageResult
            {
                Success = false,
                Message = message,
                ErrorMessage = message,
                BytesProcessed = 0
            };
        }
    }

    /// <summary>
    /// Storage data result with byte data and additional info
    /// </summary>
    public class StorageDataResult : StorageResult
    {
        public byte[] Data { get; set; }
        public StorageInfo Info { get; set; }

        public new static StorageDataResult CreateSuccess(byte[] data, string message = "Operation completed successfully", long bytesProcessed = 0)
        {
            return new StorageDataResult
            {
                Success = true,
                Message = message,
                BytesProcessed = bytesProcessed,
                Data = data
            };
        }

        public new static StorageDataResult CreateFailure(string message = "Operation failed")
        {
            return new StorageDataResult
            {
                Success = false,
                Message = message,
                ErrorMessage = message,
                BytesProcessed = 0,
                Data = null
            };
        }
    }

    /// <summary>
    /// Storage info
    /// </summary>
    public class StorageInfo
    {
        public int TotalSaveFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public long AvailableSpaceBytes { get; set; }
        public long TotalSpaceBytes { get; set; }
        public string SaveDirectory { get; set; }
        public DateTime LastChecked { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string Hash { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsValid { get; set; }
        public bool Exists { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalReads { get; set; }
        public long TotalBytesRead { get; set; }
    }

    /// <summary>
    /// Storage metrics
    /// </summary>
    public class StorageMetrics
    {
        public long TotalBytesWritten { get; set; }
        public long TotalBytesRead { get; set; }
        public long TotalBytesStored { get; set; }
        public long TotalBytesCompressed { get; set; }
        public int OperationsPerformed { get; set; }
        public int FailedOperations { get; set; }
        public int FileCount { get; set; }
        public int TotalReads { get; set; }
        public int TotalWrites { get; set; }
        public int TotalDeletes { get; set; }
        public TimeSpan AverageOperationTime { get; set; }
        public float CompressionRatio => TotalBytesStored > 0 ? (float)TotalBytesCompressed / TotalBytesStored : 1f;
    }

    /// <summary>
    /// Corruption scan result
    /// </summary>
    public class CorruptionScanResult
    {
        public string ErrorMessage { get; set; }
        public long FileSize { get; set; }
        public System.DateTime ScanDate { get; set; }
        public string Message { get; set; }
        public bool IsValid { get; set; }
        public bool IsCorrupted { get; set; }
        public List<string> CorruptedSections { get; set; } = new List<string>();
        public List<string> RecoverableSections { get; set; } = new List<string>();
        public double CorruptionPercentage { get; set; }
        public bool CanRecover { get; set; }
        public string ScanMethod { get; set; }
    }

    /// <summary>
    /// File operation types enum
    /// </summary>
    public enum FileOperationType
    {
        Read,
        Write,
        Delete,
        Create,
        Validate,
        Operational
    }

    /// <summary>
    /// File operation for queuing
    /// </summary>
    public class FileOperation
    {
        public string OperationType { get; set; }
        public string OperationId { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
        public string Directory { get; set; }
        public DateTime QueueTime { get; set; } = DateTime.UtcNow;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TaskCompletionSource<StorageResult> CompletionSource { get; set; }
        public TaskCompletionSource<StorageResult> Result { get; set; }
        public FileOperationType Type { get; set; }
        public string SlotName { get; set; }
        public bool CreateBackup { get; set; }
        public bool DeleteBackups { get; set; }
    }

    /// <summary>
    /// Save transaction for atomic operations
    /// </summary>
    public class SaveTransaction
    {
        public string TransactionId { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<FileOperation> Operations { get; set; } = new List<FileOperation>();
    }

    /// <summary>
    /// Transaction status enum
    /// </summary>
    public enum TransactionStatus
    {
        Pending,
        Active,
        Committing,
        Committed,
        Failed,
        RolledBack
    }

    /// <summary>
    /// Cloud sync result
    /// </summary>
    public class CloudSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] Data { get; set; }
        public DateTime SyncTime { get; set; }
        public CloudSyncDirection Direction { get; set; }
        public long BytesTransferred { get; set; }
        public string RemoteHash { get; set; }
    }

    /// <summary>
    /// Cloud sync status enum
    /// </summary>
    public enum CloudSyncStatusType
    {
        NotConnected,
        Connected,
        Error,
        Syncing,
        Synced
    }

    /// <summary>
    /// Cloud sync status information
    /// </summary>
    public class CloudSyncStatus
    {
        public string SlotName { get; set; }
        public DateTime LastSyncTime { get; set; }
        public CloudSyncDirection LastSyncDirection { get; set; }
        public CloudSyncStatusType Status { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsConnected { get; set; }
    }

    /// <summary>
    /// Cloud sync direction enum
    /// </summary>
    public enum CloudSyncDirection
    {
        Upload,
        Download,
        Sync
    }

}
