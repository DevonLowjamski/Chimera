using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// SIMPLE: Basic logging infrastructure aligned with Project Chimera's logging vision.
    /// Focuses on essential logging functionality without complex buffering or destinations.
    /// </summary>
    public class LoggingInfrastructure : MonoBehaviour
    {
        [Header("Basic Logging Settings")]
        [SerializeField] private LogLevel _minimumLogLevel = LogLevel.Info;
        [SerializeField] private bool _enableConsoleLogging = true;
        [SerializeField] private bool _enableFileLogging = false;
        [SerializeField] private bool _enableTimestamps = true;

        // Basic logging state
        private string _logFilePath;
        private bool _isInitialized = false;

        // Static instance
        private static LoggingInfrastructure _instance;
        public static LoggingInfrastructure Instance => _instance;

        private void Awake()
        {
            // Ensure singleton
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogging();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the logging system
        /// </summary>
        private void InitializeLogging()
        {
            if (_isInitialized) return;

            if (_enableFileLogging)
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                _logFilePath = Path.Combine(logDirectory, $"ProjectChimera_{DateTime.Now:yyyyMMdd}.log");
            }

            _isInitialized = true;

            LogInfo("LoggingInfrastructure", "Logging system initialized successfully");
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public void LogInfo(string category, string message)
        {
            Log(LogLevel.Info, category, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void LogWarning(string category, string message)
        {
            Log(LogLevel.Warning, category, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void LogError(string category, string message)
        {
            Log(LogLevel.Error, category, message);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public void LogDebug(string category, string message)
        {
            Log(LogLevel.Debug, category, message);
        }

        /// <summary>
        /// Log a verbose message
        /// </summary>
        public void LogVerbose(string category, string message)
        {
            Log(LogLevel.Verbose, category, message);
        }

        /// <summary>
        /// Generic log method
        /// </summary>
        private void Log(LogLevel level, string category, string message)
        {
            if (level < _minimumLogLevel) return;

            string timestamp = _enableTimestamps ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " : "";
            string logMessage = $"{timestamp}[{level}] [{category}] {message}";

            // Console logging
            if (_enableConsoleLogging)
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Logger.LogError("OTHER", logMessage, this);
                        break;
                    case LogLevel.Warning:
                        Logger.LogWarning("OTHER", logMessage, this);
                        break;
                    default:
                        Logger.Log("OTHER", logMessage, this);
                        break;
                }
            }

            // File logging
            if (_enableFileLogging && !string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Logger.LogError("OTHER", $"File logging failed: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Get current log file path
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Clear log file
        /// </summary>
        public void ClearLogFile()
        {
            if (_enableFileLogging && !string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
            {
                try
                {
                    File.WriteAllText(_logFilePath, string.Empty);
                    LogInfo("LoggingInfrastructure", "Log file cleared");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("OTHER", "Failed to clear log file", this);
                }
            }
        }

        /// <summary>
        /// Get logging statistics
        /// </summary>
        public LoggingStatistics GetStatistics()
        {
            return new LoggingStatistics
            {
                ConsoleLoggingEnabled = _enableConsoleLogging,
                FileLoggingEnabled = _enableFileLogging,
                MinimumLogLevel = _minimumLogLevel,
                TimestampsEnabled = _enableTimestamps,
                LogFilePath = _logFilePath
            };
        }
    }

    /// <summary>
    /// Log level enum
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Basic logging statistics
    /// </summary>
    public class LoggingStatistics
    {
        public bool ConsoleLoggingEnabled;
        public bool FileLoggingEnabled;
        public LogLevel MinimumLogLevel;
        public bool TimestampsEnabled;
        public string LogFilePath;
    }
}
