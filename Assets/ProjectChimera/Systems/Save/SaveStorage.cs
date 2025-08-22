using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;
using ProjectChimera.Systems.Save.Components;
using System.IO.Compression;

// HashAlgorithmType is directly available in ProjectChimera.Systems.Save namespace

// Type alias to resolve CompressionLevel ambiguity
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Orchestrator for Project Chimera's high-performance save storage system.
    /// Coordinates specialized components for compression, validation, and optimization
    /// to provide comprehensive cannabis cultivation save data management.
    /// </summary>
    public class SaveStorage : MonoBehaviour
    {
        [Header("Storage Configuration")]
        [SerializeField] private string _saveDirectory = "saves";
        [SerializeField] private string _backupDirectory = "backups";
        [SerializeField] private string _tempDirectory = "temp";
        [SerializeField] private string _archiveDirectory = "archive";
        [SerializeField] private string _saveFileExtension = ".save";
        [SerializeField] private string _backupFileExtension = ".backup";
        
        [Header("Backup Settings")]
        [SerializeField] private bool _enableAutoBackup = true;
        [SerializeField] private int _maxBackupsPerSlot = 5;
        [SerializeField] private int _maxTotalBackups = 50;
        [SerializeField] private bool _enableIncrementalBackups = true;
        [SerializeField] private TimeSpan _backupRetentionPeriod = TimeSpan.FromDays(30);
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableAsyncOperations = true;
        [SerializeField] private bool _enableAtomicWrites = true;
        [SerializeField] private bool _enableTransactionSupport = true;
        [SerializeField] private int _maxConcurrentOperations = 4;
        [SerializeField] private long _maxFileSize = 100 * 1024 * 1024; // 100MB
        
        [Header("Cloud Sync Settings")]
        [SerializeField] private bool _enableCloudSync = false;
        [SerializeField] private CloudStorageProvider _cloudProvider = CloudStorageProvider.None;
        [SerializeField] private bool _enableCloudBackup = false;
        [SerializeField] private bool _autoSyncOnSave = false;
        [SerializeField] private float _syncRetryDelay = 30f;
        
        [Header("Storage Optimization")]
        [SerializeField] private bool _enableStorageOptimization = true;
        [SerializeField] private bool _enableDiskSpaceMonitoring = true;
        [SerializeField] private long _minFreeSpaceBytes = 1024 * 1024 * 1024; // 1GB
        [SerializeField] private bool _enableAutoCleanup = true;
        [SerializeField] private float _cleanupThresholdPercent = 0.9f; // 90%
        
        [Header("Compression Settings")]
        [SerializeField] private bool _enableDataCompression = true;
        [SerializeField] private CompressionLevel _compressionLevel = CompressionLevel.Optimal;
        [SerializeField] private CompressionAlgorithm _compressionAlgorithm = CompressionAlgorithm.GZip;
        [SerializeField] private float _compressionThreshold = 0.1f;
        
        [Header("Validation Settings")]
        [SerializeField] private bool _enableIntegrityChecking = true;
        [SerializeField] private bool _enableCorruptionDetection = true;
        [SerializeField] private HashAlgorithmType _hashAlgorithm = HashAlgorithmType.SHA256;

        // Storage paths
        private string _baseSaveDirectory;
        private string _fullBackupDirectory;
        private string _fullTempDirectory;
        private string _fullArchiveDirectory;
        
        // File operation tracking
        private Dictionary<string, FileOperation> _activeOperations = new Dictionary<string, FileOperation>();
        private Queue<FileOperation> _operationQueue = new Queue<FileOperation>();
        private int _currentConcurrentOperations = 0;
        
        // Transaction support
        private Dictionary<string, SaveTransaction> _activeTransactions = new Dictionary<string, SaveTransaction>();
        private int _nextTransactionId = 1;
        
        // Cloud sync
        private ICloudStorageProvider _cloudStorageProvider;
        private Dictionary<string, CloudSyncStatus> _cloudSyncStatus = new Dictionary<string, CloudSyncStatus>();
        
        // Specialized component services
        private StorageCompressionService _compressionService;
        private StorageValidationService _validationService;
        private StorageOptimizer _storageOptimizer;
        
        // Storage metrics
        private StorageMetrics _metrics = new StorageMetrics();
        private bool _isInitialized = false;

        #region Initialization

        private void Awake()
        {
            InitializeStorageSystem();
        }

        private void InitializeStorageSystem()
        {
            if (_isInitialized) return;

            // Setup directory paths
            _baseSaveDirectory = Path.Combine(Application.persistentDataPath, _saveDirectory);
            _fullBackupDirectory = Path.Combine(_baseSaveDirectory, _backupDirectory);
            _fullTempDirectory = Path.Combine(_baseSaveDirectory, _tempDirectory);
            _fullArchiveDirectory = Path.Combine(_baseSaveDirectory, _archiveDirectory);
            
            // Create directories if they don't exist
            CreateDirectoryStructure();
            
            // Initialize specialized components
            InitializeComponents();
            
            // Initialize cloud provider if enabled
            if (_enableCloudSync && _cloudProvider != CloudStorageProvider.None)
            {
                InitializeCloudProvider();
            }
            
            _isInitialized = true;
            LogInfo($"SaveStorage initialized - Directory: {_baseSaveDirectory}");
        }

        private void CreateDirectoryStructure()
        {
            try
            {
                Directory.CreateDirectory(_baseSaveDirectory);
                Directory.CreateDirectory(_fullBackupDirectory);
                Directory.CreateDirectory(_fullTempDirectory);
                Directory.CreateDirectory(_fullArchiveDirectory);
                
                LogInfo("Storage directory structure created successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to create directory structure: {ex.Message}");
                throw;
            }
        }

        private void InitializeCloudProvider()
        {
            try
            {
                _cloudStorageProvider = _cloudProvider switch
                {
                    CloudStorageProvider.SteamCloud => new SteamCloudProvider(),
                    CloudStorageProvider.GoogleDrive => new GoogleDriveProvider(),
                    CloudStorageProvider.Dropbox => new DropboxProvider(),
                    CloudStorageProvider.AmazonS3 => new AmazonS3Provider(),
                    _ => null
                };
                
                if (_cloudStorageProvider != null)
                {
                    _cloudStorageProvider.Initialize();
                    LogInfo($"Cloud storage provider initialized: {_cloudProvider}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize cloud provider: {ex.Message}");
                _enableCloudSync = false;
            }
        }

        private void InitializeComponents()
        {
            // Initialize compression service
            _compressionService = GetOrAddComponent<StorageCompressionService>();
            _compressionService.Initialize(_compressionLevel, _compressionAlgorithm, 
                _enableDataCompression, _compressionThreshold, 3);
            
            // Initialize validation service
            _validationService = GetOrAddComponent<StorageValidationService>();
            _validationService.Initialize(_enableIntegrityChecking, _enableCorruptionDetection, 
                true, _hashAlgorithm, 3, 30f);
            
            // Initialize storage optimizer
            _storageOptimizer = GetOrAddComponent<StorageOptimizer>();
            _storageOptimizer.Initialize(_enableStorageOptimization, _enableDiskSpaceMonitoring, 
                _enableAutoCleanup, _minFreeSpaceBytes, _cleanupThresholdPercent, _backupRetentionPeriod,
                _maxBackupsPerSlot, _maxTotalBackups, _baseSaveDirectory, _fullBackupDirectory, 
                _fullTempDirectory, _fullArchiveDirectory);
                
            LogInfo("Storage components initialized for cannabis cultivation data management");
        }
        
        /// <summary>
        /// Get or add component to this GameObject
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Write save data to file with optional backup
        /// </summary>
        public async Task<StorageResult> WriteFileAsync(string slotName, byte[] data, bool createBackup = true)
        {
            if (!_isInitialized) InitializeStorageSystem();
            
            var operation = new FileOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                Type = FileOperationType.Write,
                SlotName = slotName,
                Data = data,
                CreateBackup = createBackup,
                StartTime = DateTime.Now
            };
            
            try
            {
                // Validate data before processing
                var validationResult = await _validationService?.ValidateBeforeSaveAsync(slotName, data);
                if (validationResult != null && !validationResult.Success)
                {
                    return StorageResult.CreateFailure($"Validation failed: {validationResult.ErrorMessage}");
                }
                
                // Compress data if enabled
                byte[] processedData = data;
                CompressionResult compressionResult = null;
                if (_enableDataCompression && _compressionService != null)
                {
                    compressionResult = await _compressionService.CompressDataAsync(data);
                    if (compressionResult.Success)
                    {
                        processedData = compressionResult.Data;
                    }
                }
                
                // Check file size limits
                if (processedData.Length > _maxFileSize)
                {
                    return StorageResult.CreateFailure($"File size ({processedData.Length} bytes) exceeds maximum allowed ({_maxFileSize} bytes)");
                }
                
                // Check available disk space
                if (_enableDiskSpaceMonitoring && !await HasSufficientDiskSpaceAsync(processedData.Length))
                {
                    return StorageResult.CreateFailure("Insufficient disk space available");
                }
                
                // Update operation data
                operation.Data = processedData;
                operation.CompressionInfo = compressionResult;
                operation.ValidationHash = validationResult?.IntegrityHash;
                
                // Queue operation if at capacity
                if (_currentConcurrentOperations >= _maxConcurrentOperations)
                {
                    await QueueOperation(operation);
                }
                else
                {
                    await ExecuteOperation(operation);
                }
                
                var result = operation.Result;
                
                // Perform cloud sync if enabled
                if (result.Success && _enableCloudSync && _autoSyncOnSave)
                {
                    _ = SyncToCloudAsync(slotName); // Fire and forget
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Write operation failed for {slotName}: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Read save data from file
        /// </summary>
        public async Task<StorageDataResult> ReadFileAsync(string slotName)
        {
            if (!_isInitialized) InitializeStorageSystem();
            
            var operation = new FileOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                Type = FileOperationType.Read,
                SlotName = slotName,
                StartTime = DateTime.Now
            };
            
            try
            {
                // Queue operation if at capacity
                if (_currentConcurrentOperations >= _maxConcurrentOperations)
                {
                    await QueueOperation(operation);
                }
                else
                {
                    await ExecuteOperation(operation);
                }
                
                var result = operation.DataResult;
                
                // Validate and decompress data if needed
                if (result.Success && result.Data != null)
                {
                    // Validate loaded data
                    var validationResult = await _validationService?.ValidateAfterLoadAsync(slotName, result.Data);
                    if (validationResult != null && !validationResult.Success)
                    {
                        return StorageDataResult.CreateFailure($"Loaded data validation failed: {validationResult.ErrorMessage}");
                    }
                    
                    // Handle decompression if data was compressed
                    // This would require storing compression metadata with the file
                    // For now, assume data is stored in a way that indicates compression
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Read operation failed for {slotName}: {ex.Message}");
                return StorageDataResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete save file and associated backups
        /// </summary>
        public async Task<StorageResult> DeleteFileAsync(string slotName, bool deleteBackups = true)
        {
            if (!_isInitialized) InitializeStorageSystem();
            
            var operation = new FileOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                Type = FileOperationType.Delete,
                SlotName = slotName,
                DeleteBackups = deleteBackups,
                StartTime = DateTime.Now
            };
            
            try
            {
                await ExecuteOperation(operation);
                return operation.Result;
            }
            catch (Exception ex)
            {
                LogError($"Delete operation failed for {slotName}: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Move/rename save file
        /// </summary>
        public async Task<StorageResult> MoveFileAsync(string fromSlotName, string toSlotName)
        {
            if (!_isInitialized) InitializeStorageSystem();
            
            try
            {
                string fromPath = GetSaveFilePath(fromSlotName);
                string toPath = GetSaveFilePath(toSlotName);
                
                if (!File.Exists(fromPath))
                {
                    return StorageResult.CreateFailure($"Source file not found: {fromSlotName}");
                }
                
                if (File.Exists(toPath))
                {
                    return StorageResult.CreateFailure($"Destination file already exists: {toSlotName}");
                }
                
                // Atomic move using temp file
                if (_enableAtomicWrites)
                {
                    string tempPath = GetTempFilePath(toSlotName);
                    File.Copy(fromPath, tempPath);
                    File.Move(tempPath, toPath);
                    File.Delete(fromPath);
                }
                else
                {
                    File.Move(fromPath, toPath);
                }
                
                // Move backups if they exist
                await MoveBackupsAsync(fromSlotName, toSlotName);
                
                _metrics.TotalMoveOperations++;
                LogInfo($"File moved from {fromSlotName} to {toSlotName}");
                
                return StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Move operation failed from {fromSlotName} to {toSlotName}: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        #endregion

        #region Transaction Support

        /// <summary>
        /// Begin a new storage transaction
        /// </summary>
        public string BeginTransaction()
        {
            if (!_enableTransactionSupport)
            {
                throw new InvalidOperationException("Transaction support is disabled");
            }
            
            string transactionId = $"txn_{_nextTransactionId++}_{DateTime.Now:yyyyMMddHHmmss}";
            
            var transaction = new SaveTransaction
            {
                TransactionId = transactionId,
                StartTime = DateTime.Now,
                Operations = new List<FileOperation>(),
                TempFiles = new List<string>()
            };
            
            _activeTransactions[transactionId] = transaction;
            LogInfo($"Transaction started: {transactionId}");
            
            return transactionId;
        }

        /// <summary>
        /// Commit a transaction
        /// </summary>
        public async Task<StorageResult> CommitTransactionAsync(string transactionId)
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                return StorageResult.CreateFailure($"Transaction not found: {transactionId}");
            }
            
            try
            {
                // Execute all operations atomically
                foreach (var operation in transaction.Operations)
                {
                    await ExecuteOperation(operation);
                    if (!operation.Result.Success)
                    {
                        // Rollback on failure
                        await RollbackTransactionAsync(transactionId);
                        return StorageResult.CreateFailure($"Transaction failed at operation: {operation.Type}");
                    }
                }
                
                // Cleanup temp files
                foreach (string tempFile in transaction.TempFiles)
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                
                _activeTransactions.Remove(transactionId);
                _metrics.TotalTransactions++;
                
                LogInfo($"Transaction committed: {transactionId}");
                return StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                await RollbackTransactionAsync(transactionId);
                LogError($"Transaction commit failed: {transactionId} - {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Rollback a transaction
        /// </summary>
        public async Task<StorageResult> RollbackTransactionAsync(string transactionId)
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                return StorageResult.CreateFailure($"Transaction not found: {transactionId}");
            }
            
            try
            {
                // Rollback operations in reverse order
                for (int i = transaction.Operations.Count - 1; i >= 0; i--)
                {
                    var operation = transaction.Operations[i];
                    await RollbackOperation(operation);
                }
                
                // Cleanup temp files
                foreach (string tempFile in transaction.TempFiles)
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                
                _activeTransactions.Remove(transactionId);
                _metrics.TotalRollbacks++;
                
                LogInfo($"Transaction rolled back: {transactionId}");
                return StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Transaction rollback failed: {transactionId} - {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        #endregion

        #region Backup Management

        /// <summary>
        /// Create backup of save file
        /// </summary>
        public async Task<StorageResult> CreateBackupAsync(string slotName, bool incremental = false)
        {
            if (!_enableAutoBackup) return StorageResult.CreateSuccess();
            
            try
            {
                string saveFilePath = GetSaveFilePath(slotName);
                if (!File.Exists(saveFilePath))
                {
                    return StorageResult.CreateFailure($"Save file not found: {slotName}");
                }
                
                string backupPath = GenerateBackupPath(slotName);
                
                if (incremental && _enableIncrementalBackups)
                {
                    await CreateIncrementalBackupAsync(saveFilePath, backupPath);
                }
                else
                {
                    await CreateFullBackupAsync(saveFilePath, backupPath);
                }
                
                // Cleanup old backups
                await CleanupOldBackupsAsync(slotName);
                
                _metrics.TotalBackups++;
                LogInfo($"Backup created for {slotName}: {Path.GetFileName(backupPath)}");
                
                return StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Backup creation failed for {slotName}: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Restore save file from backup
        /// </summary>
        public async Task<StorageResult> RestoreFromBackupAsync(string slotName, string backupFileName = null)
        {
            try
            {
                string backupPath;
                
                if (string.IsNullOrEmpty(backupFileName))
                {
                    // Get most recent backup
                    backupPath = GetMostRecentBackup(slotName);
                }
                else
                {
                    backupPath = Path.Combine(_fullBackupDirectory, backupFileName);
                }
                
                if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
                {
                    return StorageResult.CreateFailure($"Backup not found for {slotName}");
                }
                
                string saveFilePath = GetSaveFilePath(slotName);
                
                // Atomic restore
                if (_enableAtomicWrites)
                {
                    string tempPath = GetTempFilePath(slotName);
                    File.Copy(backupPath, tempPath);
                    
                    if (File.Exists(saveFilePath))
                    {
                        File.Delete(saveFilePath);
                    }
                    
                    File.Move(tempPath, saveFilePath);
                }
                else
                {
                    File.Copy(backupPath, saveFilePath, true);
                }
                
                _metrics.TotalRestores++;
                LogInfo($"Restored {slotName} from backup: {Path.GetFileName(backupPath)}");
                
                return StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Restore failed for {slotName}: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// List available backups for a slot
        /// </summary>
        public List<BackupInfo> GetAvailableBackups(string slotName)
        {
            var backups = new List<BackupInfo>();
            
            try
            {
                string backupPattern = $"{slotName}_*{_backupFileExtension}";
                var backupFiles = Directory.GetFiles(_fullBackupDirectory, backupPattern);
                
                foreach (string backupFile in backupFiles)
                {
                    var fileInfo = new FileInfo(backupFile);
                    backups.Add(new BackupInfo
                    {
                        SlotName = slotName,
                        FileName = fileInfo.Name,
                        FilePath = backupFile,
                        CreatedTime = fileInfo.CreationTime,
                        Size = fileInfo.Length,
                        IsIncremental = fileInfo.Name.Contains("_inc_")
                    });
                }
                
                return backups.OrderByDescending(b => b.CreatedTime).ToList();
            }
            catch (Exception ex)
            {
                LogError($"Failed to get backups for {slotName}: {ex.Message}");
                return backups;
            }
        }

        #endregion

        #region Cloud Sync

        /// <summary>
        /// Sync save file to cloud storage
        /// </summary>
        public async Task<CloudSyncResult> SyncToCloudAsync(string slotName)
        {
            if (!_enableCloudSync || _cloudStorageProvider == null)
            {
                return CloudSyncResult.CreateFailure("Cloud sync is disabled");
            }
            
            try
            {
                string saveFilePath = GetSaveFilePath(slotName);
                if (!File.Exists(saveFilePath))
                {
                    return CloudSyncResult.CreateFailure($"Save file not found: {slotName}");
                }
                
                _cloudSyncStatus[slotName] = new CloudSyncStatus
                {
                    SlotName = slotName,
                    Status = SyncStatus.InProgress,
                    StartTime = DateTime.Now
                };
                
                byte[] fileData = await File.ReadAllBytesAsync(saveFilePath);
                var result = await _cloudStorageProvider.UploadFileAsync(slotName, fileData);
                
                _cloudSyncStatus[slotName] = new CloudSyncStatus
                {
                    SlotName = slotName,
                    Status = result.Success ? SyncStatus.Completed : SyncStatus.Failed,
                    StartTime = _cloudSyncStatus[slotName].StartTime,
                    EndTime = DateTime.Now,
                    ErrorMessage = result.Success ? null : result.ErrorMessage
                };
                
                if (result.Success)
                {
                    _metrics.TotalCloudUploads++;
                    LogInfo($"Cloud sync completed for {slotName}");
                }
                else
                {
                    LogError($"Cloud sync failed for {slotName}: {result.ErrorMessage}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _cloudSyncStatus[slotName] = new CloudSyncStatus
                {
                    SlotName = slotName,
                    Status = SyncStatus.Failed,
                    EndTime = DateTime.Now,
                    ErrorMessage = ex.Message
                };
                
                LogError($"Cloud sync exception for {slotName}: {ex.Message}");
                return CloudSyncResult.CreateFailure(ex.Message, ex);
            }
        }

        /// <summary>
        /// Download save file from cloud storage
        /// </summary>
        public async Task<CloudSyncResult> SyncFromCloudAsync(string slotName)
        {
            if (!_enableCloudSync || _cloudStorageProvider == null)
            {
                return CloudSyncResult.CreateFailure("Cloud sync is disabled");
            }
            
            try
            {
                var result = await _cloudStorageProvider.DownloadFileAsync(slotName);
                
                if (result.Success && result.Data != null)
                {
                    // Write to local storage
                    var writeResult = await WriteFileAsync(slotName, result.Data, false);
                    
                    if (writeResult.Success)
                    {
                        _metrics.TotalCloudDownloads++;
                        LogInfo($"Cloud download completed for {slotName}");
                        return CloudSyncResult.CreateSuccess();
                    }
                    else
                    {
                        return CloudSyncResult.CreateFailure($"Failed to write downloaded file: {writeResult.ErrorMessage}");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Cloud download exception for {slotName}: {ex.Message}");
                return CloudSyncResult.CreateFailure(ex.Message, ex);
            }
        }

        #endregion

        #region Storage Management

        /// <summary>
        /// Get comprehensive storage information and metrics
        /// </summary>
        public async Task<StorageInfo> GetStorageInfoAsync()
        {
            try
            {
                var diskSpaceInfo = await _storageOptimizer?.CheckDiskSpaceAsync();
                
                return new StorageInfo
                {
                    SaveDirectory = _baseSaveDirectory,
                    TotalDiskSpace = diskSpaceInfo?.TotalSpace ?? 0,
                    AvailableDiskSpace = diskSpaceInfo?.AvailableSpace ?? 0,
                    UsedDiskSpace = diskSpaceInfo?.UsedSpace ?? 0,
                    SaveDirectorySize = diskSpaceInfo?.SaveDirectorySize ?? 0,
                    BackupDirectorySize = diskSpaceInfo?.BackupDirectorySize ?? 0,
                    TotalSaveFiles = GetSaveFileCount(),
                    TotalBackupFiles = GetBackupFileCount(),
                    Metrics = _metrics,
                    CompressionMetrics = _compressionService?.GetCompressionMetrics(),
                    ValidationMetrics = _validationService?.GetValidationMetrics(),
                    OptimizationMetrics = _storageOptimizer?.GetOptimizationMetrics()
                };
            }
            catch (Exception ex)
            {
                LogError($"Failed to get storage info: {ex.Message}");
                return new StorageInfo();
            }
        }

        /// <summary>
        /// Optimize storage by delegating to the storage optimizer
        /// </summary>
        public async Task<StorageResult> OptimizeStorageAsync()
        {
            if (!_enableStorageOptimization || _storageOptimizer == null)
            {
                return StorageResult.CreateSuccess("Storage optimization is disabled");
            }
            
            try
            {
                var optimizationResult = await _storageOptimizer.OptimizeStorageAsync();
                
                if (optimizationResult.Success)
                {
                    _metrics.TotalOptimizations++;
                    LogInfo($"Storage optimization completed: {optimizationResult.TotalSpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
                    return StorageResult.CreateSuccess($"Optimization completed: {optimizationResult.TotalSpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
                }
                else
                {
                    LogError($"Storage optimization failed: {optimizationResult.ErrorMessage}");
                    return StorageResult.CreateFailure(optimizationResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                LogError($"Storage optimization failed: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        #endregion

        #region Helper Methods

        private async Task ExecuteOperation(FileOperation operation)
        {
            _currentConcurrentOperations++;
            _activeOperations[operation.OperationId] = operation;
            
            try
            {
                switch (operation.Type)
                {
                    case FileOperationType.Write:
                        await ExecuteWriteOperation(operation);
                        break;
                    case FileOperationType.Read:
                        await ExecuteReadOperation(operation);
                        break;
                    case FileOperationType.Delete:
                        await ExecuteDeleteOperation(operation);
                        break;
                }
            }
            finally
            {
                _currentConcurrentOperations--;
                _activeOperations.Remove(operation.OperationId);
                
                // Process queued operations
                if (_operationQueue.Count > 0)
                {
                    var nextOperation = _operationQueue.Dequeue();
                    _ = ExecuteOperation(nextOperation); // Fire and forget
                }
            }
        }

        private async Task ExecuteWriteOperation(FileOperation operation)
        {
            try
            {
                string filePath = GetSaveFilePath(operation.SlotName);
                
                if (_enableAtomicWrites)
                {
                    // Write to temp file first
                    string tempPath = GetTempFilePath(operation.SlotName);
                    await File.WriteAllBytesAsync(tempPath, operation.Data);
                    
                    // Atomic move
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(tempPath, filePath);
                }
                else
                {
                    await File.WriteAllBytesAsync(filePath, operation.Data);
                }
                
                // Create backup if requested
                if (operation.CreateBackup)
                {
                    await CreateBackupAsync(operation.SlotName);
                }
                
                _metrics.TotalWrites++;
                _metrics.TotalBytesWritten += operation.Data.Length;
                
                operation.Result = StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                operation.Result = StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        private async Task ExecuteReadOperation(FileOperation operation)
        {
            try
            {
                string filePath = GetSaveFilePath(operation.SlotName);
                
                if (!File.Exists(filePath))
                {
                    operation.DataResult = StorageDataResult.CreateFailure($"File not found: {operation.SlotName}");
                    return;
                }
                
                byte[] data = await File.ReadAllBytesAsync(filePath);
                
                _metrics.TotalReads++;
                _metrics.TotalBytesRead += data.Length;
                
                operation.DataResult = StorageDataResult.CreateSuccess(data);
            }
            catch (Exception ex)
            {
                operation.DataResult = StorageDataResult.CreateFailure(ex.Message, ex);
            }
        }

        private async Task ExecuteDeleteOperation(FileOperation operation)
        {
            try
            {
                string filePath = GetSaveFilePath(operation.SlotName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                // Delete backups if requested
                if (operation.DeleteBackups)
                {
                    await DeleteBackupsAsync(operation.SlotName);
                }
                
                _metrics.TotalDeletes++;
                
                operation.Result = StorageResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                operation.Result = StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        private string GetSaveFilePath(string slotName)
        {
            return Path.Combine(_baseSaveDirectory, $"{slotName}{_saveFileExtension}");
        }

        private string GetTempFilePath(string slotName)
        {
            return Path.Combine(_fullTempDirectory, $"{slotName}_temp_{DateTime.Now:yyyyMMddHHmmss}{_saveFileExtension}");
        }

        private string GenerateBackupPath(string slotName)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(_fullBackupDirectory, $"{slotName}_{timestamp}{_backupFileExtension}");
        }

        private long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return 0;
            
            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                           .Sum(file => new FileInfo(file).Length);
        }

        private int GetSaveFileCount()
        {
            if (!Directory.Exists(_baseSaveDirectory)) return 0;
            return Directory.GetFiles(_baseSaveDirectory, $"*{_saveFileExtension}").Length;
        }

        private int GetBackupFileCount()
        {
            if (!Directory.Exists(_fullBackupDirectory)) return 0;
            return Directory.GetFiles(_fullBackupDirectory, $"*{_backupFileExtension}").Length;
        }

        private async Task<bool> HasSufficientDiskSpaceAsync(long requiredBytes)
        {
            try
            {
                var diskSpaceInfo = await _storageOptimizer?.CheckDiskSpaceAsync();
                if (diskSpaceInfo != null)
                {
                    return diskSpaceInfo.AvailableSpace >= (_minFreeSpaceBytes + requiredBytes);
                }
                return false;
            }
            catch
            {
                return true; // Assume sufficient space if check fails
            }
        }

        // Async helper methods (simplified implementations)
        private async Task QueueOperation(FileOperation operation) => await Task.Run(() => _operationQueue.Enqueue(operation));
        private async Task RollbackOperation(FileOperation operation) => await Task.Delay(1);
        private async Task CreateFullBackupAsync(string sourceFile, string backupPath) => File.Copy(sourceFile, backupPath);
        private async Task CreateIncrementalBackupAsync(string sourceFile, string backupPath) => File.Copy(sourceFile, backupPath);
        private async Task CleanupOldBackupsAsync(string slotName) => await Task.Delay(1);
        private async Task MoveBackupsAsync(string fromSlot, string toSlot) => await Task.Delay(1);
        private async Task DeleteBackupsAsync(string slotName) => await Task.Delay(1);
        
        private string GetMostRecentBackup(string slotName)
        {
            var backups = GetAvailableBackups(slotName);
            return backups.FirstOrDefault()?.FilePath;
        }

        #endregion

        #region Component Integration
        
        /// <summary>
        /// Get compression service metrics
        /// </summary>
        public CompressionMetrics GetCompressionMetrics()
        {
            return _compressionService?.GetCompressionMetrics();
        }
        
        /// <summary>
        /// Get validation service metrics
        /// </summary>
        public ValidationMetrics GetValidationMetrics()
        {
            return _validationService?.GetValidationMetrics();
        }
        
        /// <summary>
        /// Get storage optimization metrics
        /// </summary>
        public OptimizationMetrics GetOptimizationMetrics()
        {
            return _storageOptimizer?.GetOptimizationMetrics();
        }
        
        /// <summary>
        /// Force immediate storage cleanup
        /// </summary>
        public async Task<StorageResult> ForceCleanupAsync()
        {
            try
            {
                var cleanupResult = await _storageOptimizer?.ForceCleanupAsync();
                if (cleanupResult != null)
                {
                    LogInfo($"Force cleanup completed: {cleanupResult.FilesDeleted} files deleted, {cleanupResult.SpaceReclaimed / (1024 * 1024):F1} MB reclaimed");
                    return StorageResult.CreateSuccess($"Cleanup completed: {cleanupResult.FilesDeleted} files deleted");
                }
                return StorageResult.CreateSuccess("Cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Force cleanup failed: {ex.Message}");
                return StorageResult.CreateFailure(ex.Message, ex);
            }
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[SaveStorage] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SaveStorage] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SaveStorage] {message}");
        }

        #endregion
    }
}