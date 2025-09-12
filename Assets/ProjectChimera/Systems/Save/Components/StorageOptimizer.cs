using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Save.Components
{
    /// <summary>
    /// SIMPLE: Basic storage management aligned with Project Chimera's save system vision.
    /// Focuses on essential file operations without complex optimization algorithms.
    /// </summary>
    public class StorageOptimizer : MonoBehaviour
    {
        [Header("Basic Storage Settings")]
        [SerializeField] private bool _enableBasicOptimization = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxSaveFiles = 10;

        // Basic file tracking
        private readonly List<string> _saveFiles = new List<string>();
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic storage management
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[StorageOptimizer] Initialized successfully");
            }
        }

        /// <summary>
        /// Clean up old save files
        /// </summary>
        public void CleanUpOldSaves()
        {
            if (!_enableBasicOptimization) return;

            // Simple cleanup - remove excess files
            while (_saveFiles.Count > _maxSaveFiles)
            {
                string oldestFile = _saveFiles[0];
                _saveFiles.RemoveAt(0);

                // In a real implementation, you would delete the actual file
                // For now, just log it
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[StorageOptimizer] Would clean up old save: {oldestFile}");
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[StorageOptimizer] Cleaned up old saves. Current files: {_saveFiles.Count}");
            }
        }

        /// <summary>
        /// Add a save file to tracking
        /// </summary>
        public void AddSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            if (!_saveFiles.Contains(fileName))
            {
                _saveFiles.Add(fileName);

                // Auto-cleanup if we exceed the limit
                if (_saveFiles.Count > _maxSaveFiles)
                {
                    CleanUpOldSaves();
                }
            }
        }

        /// <summary>
        /// Remove a save file from tracking
        /// </summary>
        public void RemoveSaveFile(string fileName)
        {
            if (_saveFiles.Remove(fileName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[StorageOptimizer] Removed save file: {fileName}");
                }
            }
        }

        /// <summary>
        /// Get list of save files
        /// </summary>
        public List<string> GetSaveFiles()
        {
            return new List<string>(_saveFiles);
        }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public bool SaveFileExists(string fileName)
        {
            return _saveFiles.Contains(fileName);
        }

        /// <summary>
        /// Get save file count
        /// </summary>
        public int GetSaveFileCount()
        {
            return _saveFiles.Count;
        }

        /// <summary>
        /// Clear all save files
        /// </summary>
        public void ClearAllSaveFiles()
        {
            _saveFiles.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[StorageOptimizer] Cleared all save files");
            }
        }

        /// <summary>
        /// Check if storage optimization is needed
        /// </summary>
        public bool IsOptimizationNeeded()
        {
            return _saveFiles.Count > _maxSaveFiles;
        }

        /// <summary>
        /// Perform basic optimization
        /// </summary>
        public void PerformBasicOptimization()
        {
            if (!_enableBasicOptimization) return;

            CleanUpOldSaves();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[StorageOptimizer] Basic optimization completed");
            }
        }

        /// <summary>
        /// Set maximum save files
        /// </summary>
        public void SetMaxSaveFiles(int maxFiles)
        {
            _maxSaveFiles = Mathf.Max(1, maxFiles);

            // Auto-cleanup if current count exceeds new limit
            if (_saveFiles.Count > _maxSaveFiles)
            {
                CleanUpOldSaves();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[StorageOptimizer] Max save files set to {_maxSaveFiles}");
            }
        }

        /// <summary>
        /// Get storage statistics
        /// </summary>
        public StorageStatistics GetStorageStatistics()
        {
            return new StorageStatistics
            {
                TotalSaveFiles = _saveFiles.Count,
                MaxSaveFiles = _maxSaveFiles,
                IsOptimizationNeeded = IsOptimizationNeeded(),
                IsInitialized = _isInitialized,
                EnableOptimization = _enableBasicOptimization
            };
        }
    }

    /// <summary>
    /// Basic storage statistics
    /// </summary>
    [System.Serializable]
    public class StorageStatistics
    {
        public int TotalSaveFiles;
        public int MaxSaveFiles;
        public bool IsOptimizationNeeded;
        public bool IsInitialized;
        public bool EnableOptimization;
    }
}
