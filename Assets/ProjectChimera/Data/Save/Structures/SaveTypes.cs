using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Save.Structures
{
    /// <summary>
    /// Core save system types and enumerations
    /// </summary>
    public static class SaveTypes
    {
        /// <summary>
        /// Save operation result enumeration
        /// </summary>
        public enum SaveResult
        {
            Success,
            Failure,
            Partial,
            Cancelled,
            NotFound,
            PermissionDenied,
            DiskFull,
            Corrupted,
            VersionMismatch,
            NetworkError
        }

        /// <summary>
        /// Save operation type enumeration
        /// </summary>
        public enum SaveOperation
        {
            QuickSave,
            ManualSave,
            AutoSave,
            Backup,
            Export,
            Import,
            Migrate
        }

        /// <summary>
        /// Save data compression method
        /// </summary>
        public enum CompressionMethod
        {
            None,
            GZip,
            LZ4,
            Brotli
        }

        /// <summary>
        /// Save data validation level
        /// </summary>
        public enum ValidationLevel
        {
            None,
            Basic,
            Full,
            Strict
        }

        /// <summary>
        /// Save slot status
        /// </summary>
        public enum SaveSlotStatus
        {
            Empty,
            Valid,
            Corrupted,
            Incompatible,
            Locked,
            Backup
        }
    }

    /// <summary>
    /// Save operation result structure
    /// </summary>
    [Serializable]
    public struct SaveOperationResult
    {
        public SaveTypes.SaveResult Result;
        public string Message;
        public string ErrorCode;
        public DateTime Timestamp;
        public TimeSpan Duration;
        public long DataSize;
        public Dictionary<string, object> Metadata;

        public bool IsSuccess => Result == SaveTypes.SaveResult.Success;
        public bool IsFailure => Result != SaveTypes.SaveResult.Success;

        public static SaveOperationResult Success(string message = "Operation completed successfully")
        {
            return new SaveOperationResult
            {
                Result = SaveTypes.SaveResult.Success,
                Message = message,
                Timestamp = DateTime.Now,
                Metadata = new Dictionary<string, object>()
            };
        }

        public static SaveOperationResult Failure(string message, string errorCode = null)
        {
            return new SaveOperationResult
            {
                Result = SaveTypes.SaveResult.Failure,
                Message = message,
                ErrorCode = errorCode,
                Timestamp = DateTime.Now,
                Metadata = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Save slot information
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public string SlotName;
        public string DisplayName;
        public SaveTypes.SaveSlotStatus Status;
        public DateTime LastModified;
        public string GameVersion;
        public TimeSpan PlayTime;
        public string ThumbnailPath;
        public long FileSize;
        public bool IsAutoSave;
        public bool IsBackup;
        public Dictionary<string, object> Metadata;

        public SaveSlotInfo()
        {
            Metadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Save system configuration
    /// </summary>
    [Serializable]
    public class SaveSystemConfig
    {
        public int MaxSaveSlots = 10;
        public int MaxAutoSaves = 5;
        public int MaxBackups = 3;
        public SaveTypes.CompressionMethod Compression = SaveTypes.CompressionMethod.GZip;
        public SaveTypes.ValidationLevel Validation = SaveTypes.ValidationLevel.Full;
        public bool EnableAutoSave = true;
        public float AutoSaveInterval = 300f; // 5 minutes
        public bool EnableCompression = true;
        public bool EnableEncryption = false;
        public string SaveDirectory = "SaveGames";
        public string BackupDirectory = "Backups";
        public bool EnableCloudSync = false;
        public Dictionary<string, object> CustomSettings;

        public SaveSystemConfig()
        {
            CustomSettings = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Save file metadata
    /// </summary>
    [Serializable]
    public class SaveFileMetadata
    {
        public string FileName;
        public string FullPath;
        public DateTime CreatedDate;
        public DateTime LastModified;
        public long FileSize;
        public string Checksum;
        public SaveTypes.SaveSlotStatus Status;
        public Dictionary<string, object> Properties;

        public SaveFileMetadata()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}
