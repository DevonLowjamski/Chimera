// REFACTORED: Database File I/O
// Extracted from CostDatabasePersistenceManager for better separation of concerns

using System;
using System.IO;
using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// Handles file I/O operations for database persistence
    /// </summary>
    public class DatabaseFileIO
    {
        private readonly bool _enableLogging;
        private readonly string _filePath;

        public DatabaseFileIO(string filePath, bool enableLogging = false)
        {
            _filePath = filePath;
            _enableLogging = enableLogging;

            EnsureDirectoryExists();
        }

        public bool WriteToFile(string content)
        {
            try
            {
                File.WriteAllText(_filePath, content);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_IO", $"Database saved to {_filePath}", null);

                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_IO", $"Failed to write database: {ex.Message}", null);
                return false;
            }
        }

        public string ReadFromFile()
        {
            if (!File.Exists(_filePath))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("DB_IO", $"Database file not found at {_filePath}", null);
                return null;
            }

            try
            {
                string content = File.ReadAllText(_filePath);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_IO", $"Database loaded from {_filePath}", null);

                return content;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_IO", $"Failed to read database: {ex.Message}", null);
                return null;
            }
        }

        public bool FileExists() => File.Exists(_filePath);

        public bool DeleteFile()
        {
            if (!File.Exists(_filePath))
                return true;

            try
            {
                File.Delete(_filePath);
                if (_enableLogging)
                    ChimeraLogger.LogInfo("DB_IO", $"Database file deleted: {_filePath}", null);
                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_IO", $"Failed to delete database file: {ex.Message}", null);
                return false;
            }
        }

        public long GetFileSize()
        {
            if (!File.Exists(_filePath))
                return 0;

            try
            {
                return new FileInfo(_filePath).Length;
            }
            catch
            {
                return 0;
            }
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("DB_IO", $"Failed to create database directory: {ex.Message}", null);
            }
        }
    }
}

