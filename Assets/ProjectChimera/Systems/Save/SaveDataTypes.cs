using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using ProjectChimera.Systems.Save.Components;

namespace ProjectChimera.Systems.Save
{
    #region Enumerations

    /// <summary>
    /// Supported serialization formats
    /// </summary>
    public enum SerializationFormat
    {
        Binary,
        Json,
        MessagePack,
        ProtoBuf
    }

    /// <summary>
    /// File operation types
    /// </summary>
    public enum FileOperationType
    {
        Read,
        Write,
        Delete,
        Move,
        Copy,
        Backup
    }

    /// <summary>
    /// Cloud storage providers
    /// </summary>
    public enum CloudStorageProvider
    {
        None,
        SteamCloud,
        GoogleDrive,
        Dropbox,
        AmazonS3,
        OneDrive
    }

    /// <summary>
    /// Encryption algorithms
    /// </summary>
    public enum EncryptionAlgorithm
    {
        None,
        AES256,
        ChaCha20,
        Blowfish
    }

    /// <summary>
    /// Hash algorithm types
    /// </summary>
    public enum HashAlgorithmType
    {
        MD5,
        SHA1,
        SHA256,
        SHA512
    }

    /// <summary>
    /// Cloud sync status
    /// </summary>
    public enum SyncStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    #endregion

    #region Data Transfer Objects

    /// <summary>
    /// Serialization performance metrics
    /// </summary>
    public class SerializationMetrics
    {
        public int TotalSerializations { get; set; }
        public int TotalDeserializations { get; set; }
        public int TotalErrors { get; set; }
        public long TotalSerializedBytes { get; set; }
        public long TotalDeserializedBytes { get; set; }
        public TimeSpan TotalSerializationTime { get; set; }
        public TimeSpan TotalDeserializationTime { get; set; }
        public DateTime LastOperationTime { get; set; }

        public float AverageSerializationSpeed => TotalSerializationTime.TotalSeconds > 0 
            ? (float)(TotalSerializedBytes / TotalSerializationTime.TotalSeconds) 
            : 0f;

        public float AverageDeserializationSpeed => TotalDeserializationTime.TotalSeconds > 0 
            ? (float)(TotalDeserializedBytes / TotalDeserializationTime.TotalSeconds) 
            : 0f;
    }

    /// <summary>
    /// Storage system metrics
    /// </summary>
    [System.Serializable]
    public class StorageMetrics
    {
        public int TotalWrites { get; set; }
        public int TotalReads { get; set; }
        public int TotalDeletes { get; set; }
        public int TotalMoveOperations { get; set; }
        public int TotalBackups { get; set; }
        public int TotalRestores { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalRollbacks { get; set; }
        public int TotalOptimizations { get; set; }
        public int TotalCloudUploads { get; set; }
        public int TotalCloudDownloads { get; set; }
        public long TotalBytesWritten { get; set; }
        public long TotalBytesRead { get; set; }
        public DateTime LastOperationTime { get; set; }
    }

    /// <summary>
    /// File operation tracking
    /// </summary>
    public class FileOperation
    {
        public string OperationId { get; set; }
        public FileOperationType Type { get; set; }
        public string SlotName { get; set; }
        public byte[] Data { get; set; }
        public bool CreateBackup { get; set; }
        public bool DeleteBackups { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public StorageResult Result { get; set; }
        public StorageDataResult DataResult { get; set; }
        
        // Compression and validation support
        public CompressionResult CompressionInfo { get; set; }
        public string ValidationHash { get; set; }
        public bool WasCompressed => CompressionInfo?.Success == true && CompressionInfo.Algorithm != CompressionAlgorithm.None;
    }

    /// <summary>
    /// Storage transaction
    /// </summary>
    public class SaveTransaction
    {
        public string TransactionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<FileOperation> Operations { get; set; } = new List<FileOperation>();
        public List<string> TempFiles { get; set; } = new List<string>();
        public bool IsCommitted { get; set; }
        public bool IsRolledBack { get; set; }
    }

    /// <summary>
    /// Backup information
    /// </summary>
    [System.Serializable]
    public class BackupInfo
    {
        public string SlotName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedTime { get; set; }
        public long Size { get; set; }
        public bool IsIncremental { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Cloud sync status tracking
    /// </summary>
    public class CloudSyncStatus
    {
        public string SlotName { get; set; }
        public SyncStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ErrorMessage { get; set; }
        public long BytesTransferred { get; set; }
    }

    /// <summary>
    /// Storage system information
    /// </summary>
    [System.Serializable]
    public class StorageInfo
    {
        public string SaveDirectory { get; set; }
        public long TotalDiskSpace { get; set; }
        public long AvailableDiskSpace { get; set; }
        public long UsedDiskSpace { get; set; }
        public long SaveDirectorySize { get; set; }
        public long BackupDirectorySize { get; set; }
        public int TotalSaveFiles { get; set; }
        public int TotalBackupFiles { get; set; }
        public StorageMetrics Metrics { get; set; }
        
        // Component-specific metrics
        public CompressionMetrics CompressionMetrics { get; set; }
        public ValidationMetrics ValidationMetrics { get; set; }
        public OptimizationMetrics OptimizationMetrics { get; set; }

        public float DiskUsagePercent => TotalDiskSpace > 0 ? (float)UsedDiskSpace / TotalDiskSpace : 0f;
        public float SaveDirectoryPercent => TotalDiskSpace > 0 ? (float)SaveDirectorySize / TotalDiskSpace : 0f;
    }

    /// <summary>
    /// Serialization test result
    /// </summary>
    public class SerializationTestResult
    {
        public string TestedType { get; set; }
        public bool Success { get; set; }
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan SerializationTime { get; set; }
        public TimeSpan DeserializationTime { get; set; }
        public long SerializedSize { get; set; }
        public string ErrorMessage { get; set; }

        public TimeSpan TotalTestTime => TestEndTime - TestStartTime;
    }

    #endregion

    #region Result Types

    /// <summary>
    /// Generic storage operation result
    /// </summary>
    public class StorageResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public DateTime OperationTime { get; set; } = DateTime.Now;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static StorageResult CreateSuccess(Dictionary<string, object> metadata = null)
        {
            return new StorageResult
            {
                Success = true,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        public static StorageResult CreateSuccess(string message)
        {
            return new StorageResult
            {
                Success = true,
                Metadata = new Dictionary<string, object> { { "message", message } }
            };
        }

        public static StorageResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new StorageResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Storage operation result with data
    /// </summary>
    public class StorageDataResult : StorageResult
    {
        public byte[] Data { get; set; }

        public static StorageDataResult CreateSuccess(byte[] data, Dictionary<string, object> metadata = null)
        {
            return new StorageDataResult
            {
                Success = true,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        public new static StorageDataResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new StorageDataResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Cloud sync operation result
    /// </summary>
    public class CloudSyncResult : StorageDataResult
    {
        public string CloudPath { get; set; }
        public long BytesTransferred { get; set; }
        public TimeSpan SyncDuration { get; set; }

        public new static CloudSyncResult CreateSuccess(byte[] data = null, Dictionary<string, object> metadata = null)
        {
            return new CloudSyncResult
            {
                Success = true,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        public new static CloudSyncResult CreateFailure(string errorMessage, Exception exception = null)
        {
            return new CloudSyncResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Base interface for data serializers
    /// </summary>
    public interface IDataSerializer
    {
        byte[] Serialize<T>(T data) where T : class;
        T Deserialize<T>(byte[] data) where T : class;
        bool SupportsType(Type type);
        string FormatName { get; }
    }

    /// <summary>
    /// Binary serialization interface
    /// </summary>
    public interface IBinarySerializer : IDataSerializer
    {
        bool UseCompression { get; set; }
        bool ValidateChecksums { get; set; }
    }

    /// <summary>
    /// JSON serialization interface
    /// </summary>
    public interface IJsonSerializer : IDataSerializer
    {
        bool PrettyPrint { get; set; }
        bool IgnoreNullValues { get; set; }
    }

    /// <summary>
    /// MessagePack serialization interface
    /// </summary>
    public interface IMessagePackSerializer : IDataSerializer
    {
        bool UseCompression { get; set; }
        bool UseLZ4Compression { get; set; }
    }

    /// <summary>
    /// Compression provider interface
    /// </summary>
    public interface ICompressionProvider
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] compressedData);
        CompressionLevel CompressionLevel { get; set; }
        float GetCompressionRatio(byte[] originalData, byte[] compressedData);
    }

    /// <summary>
    /// Encryption provider interface
    /// </summary>
    public interface IEncryptionProvider
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] encryptedData);
        void GenerateKey();
        void SetKey(byte[] key);
        bool ValidateKey(byte[] key);
    }

    /// <summary>
    /// Hash provider interface
    /// </summary>
    public interface IHashProvider
    {
        byte[] ComputeHash(byte[] data);
        bool VerifyHash(byte[] data, byte[] hash);
        string HashAlgorithmName { get; }
    }

    /// <summary>
    /// Memory pool interface
    /// </summary>
    public interface IMemoryPool
    {
        byte[] RentBuffer(int minimumSize);
        void ReturnBuffer(byte[] buffer);
        void ClearPool();
        int PoolSize { get; }
        int AvailableBuffers { get; }
    }

    /// <summary>
    /// Cloud storage provider interface
    /// </summary>
    public interface ICloudStorageProvider
    {
        Task<bool> Initialize();
        Task<CloudSyncResult> UploadFileAsync(string fileName, byte[] data);
        Task<CloudSyncResult> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<List<string>> ListFilesAsync();
        bool IsConnected { get; }
        string ProviderName { get; }
    }

    #endregion
}