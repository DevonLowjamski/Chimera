using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Save;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using FileOperationType = ProjectChimera.Data.Save.FileOperationType;
using ISaveCore = ProjectChimera.Data.Save.ISaveCore;

namespace ProjectChimera.Systems.Save.Storage
{
    /// <summary>
    /// Core implementation for basic save operations and file management
    /// Updated to fix interface implementation issues
    /// </summary>
    public class SaveCore : ISaveCore
    {
        private string _baseSaveDirectory;
        private string _fullBackupDirectory;
        private string _fullTempDirectory;
        private string _fullArchiveDirectory;
        private string _saveFileExtension = ".save";
        private string _backupFileExtension = ".backup";

        // File operation tracking
        private Dictionary<string, FileOperation> _activeOperations = new Dictionary<string, FileOperation>();
        private Queue<FileOperation> _operationQueue = new Queue<FileOperation>();
        private int _currentConcurrentOperations = 0;
        private int _maxConcurrentOperations = 4;

        // Transaction support
        private Dictionary<string, SaveTransaction> _activeTransactions = new Dictionary<string, SaveTransaction>();
        private Dictionary<string, List<string>> _transactionOperations = new Dictionary<string, List<string>>();
        private int _nextTransactionId = 1;

        // Configuration
        private bool _enableAsyncOperations = true;
        private bool _enableAtomicWrites = true;
        private bool _enableTransactionSupport = true;
        private long _maxFileSize = 100 * 1024 * 1024; // 100MB
        private long _minFreeSpaceBytes = 1024 * 1024 * 1024; // 1GB

        // Storage metrics
        private StorageMetrics _metrics = new StorageMetrics();
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;
        public StorageMetrics Metrics => _metrics;

        public void Initialize(string saveDirectory, string backupDirectory, string archiveDirectory = null)
        {
            if (_isInitialized) return;

            _baseSaveDirectory = Path.Combine(Application.persistentDataPath, saveDirectory);
            _fullBackupDirectory = Path.Combine(_baseSaveDirectory, backupDirectory);
            _fullTempDirectory = Path.Combine(_baseSaveDirectory, "temp");
            _fullArchiveDirectory = string.IsNullOrEmpty(archiveDirectory) ? 
                Path.Combine(_baseSaveDirectory, "archive") : 
                Path.Combine(_baseSaveDirectory, archiveDirectory);

            CreateDirectoryStructure();
            _isInitialized = true;

            ChimeraLogger.Log("OTHER", "$1", null);
        }

        public void Shutdown()
        {
            _activeOperations.Clear();
            _operationQueue.Clear();
            _activeTransactions.Clear();
            _isInitialized = false;

            ChimeraLogger.Log("OTHER", "$1", null);
        }

        public async Task<StorageResult> WriteFileAsync(string fileName, byte[] data, bool createBackup = true)
        {
            if (!_isInitialized)
            {
                return StorageResult.CreateFailure("SaveCore not initialized");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return StorageResult.CreateFailure("Invalid file name");
            }

            if (data == null || data.Length == 0)
            {
                return StorageResult.CreateFailure("No data to write");
            }

            if (data.Length > _maxFileSize)
            {
                return StorageResult.CreateFailure($"File size exceeds maximum allowed size ({_maxFileSize} bytes)");
            }

            // Check disk space
            if (!await HasSufficientDiskSpaceAsync(data.Length))
            {
                return StorageResult.CreateFailure("Insufficient disk space");
            }

            var operation = new FileOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                Type = FileOperationType.Write,
                SlotName = fileName,
                Data = data,
                CreateBackup = true,
                Timestamp = DateTime.Now,
                FileName = fileName,
                Result = new TaskCompletionSource<StorageResult>()
            };

            if (_enableAsyncOperations && _currentConcurrentOperations < _maxConcurrentOperations)
            {
                await ExecuteOperation(operation);
            }
            else
            {
                await QueueOperation(operation);
            }

            return await operation.Result.Task;
        }

        public async Task<StorageResult> DeleteFileAsync(string fileName, bool deleteBackups = true)
        {
            if (!_isInitialized)
            {
                return StorageResult.CreateFailure("SaveCore not initialized");
            }

            var operation = new FileOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                Type = FileOperationType.Delete,
                SlotName = fileName,
                DeleteBackups = true,
                Timestamp = DateTime.Now,
                FileName = fileName,
                Result = new TaskCompletionSource<StorageResult>()
            };

            if (_enableAsyncOperations && _currentConcurrentOperations < _maxConcurrentOperations)
            {
                await ExecuteOperation(operation);
            }
            else
            {
                await QueueOperation(operation);
            }

            return await operation.Result.Task;
        }

        public async Task<string> BeginTransactionAsync()
        {
            if (!_enableTransactionSupport)
            {
                return null;
            }

            var transactionId = $"txn_{_nextTransactionId++}";
            var transaction = new SaveTransaction
            {
                TransactionId = transactionId,
                Status = TransactionStatus.Active,
                StartTime = DateTime.Now,
                Operations = new List<FileOperation>()
            };
            
            // Store empty operations list
            _transactionOperations[transactionId] = new List<string>();

            _activeTransactions[transactionId] = transaction;

            ChimeraLogger.Log("OTHER", "$1", null);
            return transactionId;
        }

        public async Task<StorageResult> BeginTransactionAsync(string slotName)
        {
            var transactionId = await BeginTransactionAsync();
            if (transactionId == null)
            {
                return StorageResult.CreateFailure("Transaction support is disabled");
            }

            // Store the slot name separately
            _transactionOperations[transactionId] = new List<string> { slotName };

            ChimeraLogger.Log("OTHER", "$1", null);
            return StorageResult.CreateSuccess($"Transaction {transactionId} started");
        }

        public async Task<StorageResult> CommitTransactionAsync(string transactionId)
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                return StorageResult.CreateFailure($"Transaction {transactionId} not found");
            }

            try
            {
                transaction.Status = TransactionStatus.Committing;

                // Apply all pending changes
                foreach (var operation in transaction.Operations)
                {
                    await ExecuteOperation(operation);
                }

                transaction.Status = TransactionStatus.Committed;
                transaction.EndTime = DateTime.Now;

                _activeTransactions.Remove(transactionId);

                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateSuccess($"Transaction {transactionId} committed");
            }
            catch (Exception ex)
            {
                transaction.Status = TransactionStatus.Failed;
                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateFailure($"Transaction commit failed: {ex.Message}");
            }
        }

        public async Task<StorageResult> RollbackTransactionAsync(string transactionId)
        {
            if (!_activeTransactions.TryGetValue(transactionId, out var transaction))
            {
                return StorageResult.CreateFailure($"Transaction {transactionId} not found");
            }

            try
            {
                transaction.Status = TransactionStatus.RollingBack;

                // Rollback all operations in reverse order
                for (int i = transaction.Operations.Count - 1; i >= 0; i--)
                {
                    await RollbackOperation(transaction.Operations[i]);
                }

                transaction.Status = TransactionStatus.RolledBack;
                transaction.EndTime = DateTime.Now;

                _activeTransactions.Remove(transactionId);

                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateSuccess($"Transaction {transactionId} rolled back");
            }
            catch (Exception ex)
            {
                transaction.Status = TransactionStatus.Failed;
                ChimeraLogger.Log("OTHER", "$1", null);
                return StorageResult.CreateFailure($"Transaction rollback failed: {ex.Message}");
            }
        }

        public string GetSaveFilePath(string slotName)
        {
            return Path.Combine(_baseSaveDirectory, slotName + _saveFileExtension);
        }

        public string GetTempFilePath(string slotName)
        {
            return Path.Combine(_fullTempDirectory, slotName + "_temp" + _saveFileExtension);
        }

        public string GetBackupFilePath(string slotName, string backupSuffix = null)
        {
            if (string.IsNullOrEmpty(backupSuffix))
            {
                backupSuffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            return Path.Combine(_fullBackupDirectory, $"{slotName}_{backupSuffix}" + _backupFileExtension);
        }

        public async Task<bool> HasSufficientDiskSpaceAsync(long requiredBytes)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(_baseSaveDirectory);
                var drive = new DriveInfo(directoryInfo.Root.FullName);
                return drive.AvailableFreeSpace > (requiredBytes + _minFreeSpaceBytes);
            }
            catch
            {
                return true; // Assume sufficient space if unable to check
            }
        }

        public Task QueueOperation(FileOperation operation)
        {
            return Task.Run(() => _operationQueue.Enqueue(operation));
        }

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

                _metrics.TotalWrites++;
                _metrics.TotalBytesWritten += operation.Data.Length;

                operation.Result.SetResult(StorageResult.CreateSuccess());

                ChimeraLogger.Log("OTHER", "$1", null);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                operation.Result.SetResult(StorageResult.CreateFailure(ex.Message));
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

                _metrics.TotalDeletes++;

                operation.Result.SetResult(StorageResult.CreateSuccess());

                ChimeraLogger.Log("OTHER", "$1", null);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                operation.Result.SetResult(StorageResult.CreateFailure(ex.Message));
            }
        }

        private async Task RollbackOperation(FileOperation operation)
        {
            // Simple rollback implementation - in a real system this would be more sophisticated
            await Task.Delay(1);
            ChimeraLogger.Log("OTHER", "$1", null);
        }

        private void CreateDirectoryStructure()
        {
            try
            {
                Directory.CreateDirectory(_baseSaveDirectory);
                Directory.CreateDirectory(_fullBackupDirectory);
                Directory.CreateDirectory(_fullTempDirectory);
                Directory.CreateDirectory(_fullArchiveDirectory);

                ChimeraLogger.Log("OTHER", "$1", null);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                throw;
            }
        }

        public void SetConfiguration(bool enableAsyncOperations, bool enableAtomicWrites, bool enableTransactionSupport,
                                   int maxConcurrentOperations, long maxFileSize, long minFreeSpaceBytes)
        {
            _enableAsyncOperations = enableAsyncOperations;
            _enableAtomicWrites = enableAtomicWrites;
            _enableTransactionSupport = enableTransactionSupport;
            _maxConcurrentOperations = maxConcurrentOperations;
            _maxFileSize = maxFileSize;
            _minFreeSpaceBytes = minFreeSpaceBytes;
        }

        // Duplicate method removed - using property implementation above

        // Remove duplicate Metrics method - it's already defined above
    }
}
