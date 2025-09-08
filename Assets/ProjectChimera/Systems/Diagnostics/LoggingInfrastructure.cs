using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// Logging infrastructure for Project Chimera
    /// Replaces Debug.Log statements with proper structured logging
    /// Integrates with Unity 6.2 diagnostics while providing development-specific logging
    /// </summary>
    public class LoggingInfrastructure : MonoBehaviour, ITickable
    {
        [Header("Logging Configuration")]
        [SerializeField] private LogLevel _minimumLogLevel = LogLevel.Info;
        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private bool _enableConsoleLogging = true;
        [SerializeField] private bool _enableUnityDiagnosticsLogging = true;
        [SerializeField] private int _maxLogFiles = 10;
        [SerializeField] private long _maxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB
        
        [Header("Development Logging")]
        [SerializeField] private bool _enableVerboseLogging = false;
        [SerializeField] private bool _enableStackTraces = true;
        [SerializeField] private bool _enableTimestamps = true;
        [SerializeField] private bool _enableThreadInfo = false;
        
        [Header("Performance Settings")]
        [SerializeField] private int _logBufferSize = 1000;
        [SerializeField] private float _flushInterval = 5f;
        [SerializeField] private bool _enableAsyncLogging = true;
        
        // Core Systems
        // Diagnostics integration temporarily disabled
        // private Unity62DiagnosticsIntegration _unity62Diagnostics;
        
        // Logging State
        private readonly Queue<LogEntry> _logBuffer = new Queue<LogEntry>();
        private readonly Dictionary<string, ILogDestination> _logDestinations = new Dictionary<string, ILogDestination>();
        private string _logDirectory;
        private FileLogDestination _fileLogger;
        private float _lastFlushTime;
        
        // Static instance for global access
        private static LoggingInfrastructure _instance;
        public static LoggingInfrastructure Instance => _instance;
        
        // Events
        public event Action<LogEntry> OnLogEntryCreated;
        public event Action<LogLevel, string> OnCriticalError;
        
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
                return;
            }
        }
        
        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            FindSystemReferences();
            SetupLogDestinations();
            
            // Log initialization
            LogInfo("LoggingInfrastructure", "Logging system initialized successfully");
        }
        
            public void Tick(float deltaTime)
    {
            if (Time.time - _lastFlushTime >= _flushInterval)
            {
                FlushLogBuffer();
                _lastFlushTime = Time.time;
            
    }
        }
        
        private void InitializeLogging()
        {
            // Set up log directory
            _logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            // Clean up old log files
            CleanupOldLogFiles();
            
            // Redirect Unity log handler
            Application.logMessageReceived += HandleUnityLogMessage;
            
            ChimeraLogger.Log("[LoggingInfrastructure] Logging infrastructure initialized");
        }
        
        private void FindSystemReferences()
        {
            // Diagnostics integration temporarily disabled
            // _unity62Diagnostics = ServiceContainerFactory.Instance?.TryResolve<Unity62DiagnosticsIntegration>();
        }
        
        private void SetupLogDestinations()
        {
            // Console destination
            if (_enableConsoleLogging)
            {
                var consoleDestination = new ConsoleLogDestination();
                _logDestinations["console"] = consoleDestination;
            }
            
            // File destination
            if (_enableFileLogging)
            {
                var fileName = $"ProjectChimera_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var filePath = Path.Combine(_logDirectory, fileName);
                _fileLogger = new FileLogDestination(filePath, _maxLogFileSizeBytes);
                _logDestinations["file"] = _fileLogger;
            }
            
            // Unity Diagnostics destination
            // Diagnostics integration temporarily disabled
            // if (_enableUnityDiagnosticsLogging && _unity62Diagnostics != null)
            // {
            //     var unityDestination = new UnityDiagnosticsLogDestination(_unity62Diagnostics);
            //     _logDestinations["unity_diagnostics"] = unityDestination;
            // }
        }
        
        private void HandleUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            // Convert Unity LogType to our LogLevel
            var logLevel = ConvertUnityLogType(type);
            
            // Create log entry
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = logLevel,
                Category = "Unity",
                Message = logString,
                StackTrace = _enableStackTraces ? stackTrace : null,
                ThreadId = _enableThreadInfo ? System.Threading.Thread.CurrentThread.ManagedThreadId : 0
            };
            
            // Process the log entry
            ProcessLogEntry(entry);
        }
        
        private LogLevel ConvertUnityLogType(LogType unityLogType)
        {
            return unityLogType switch
            {
                LogType.Error => LogLevel.Error,
                LogType.Assert => LogLevel.Error,
                LogType.Warning => LogLevel.Warning,
                LogType.Log => LogLevel.Info,
                LogType.Exception => LogLevel.Error,
                _ => LogLevel.Info
            };
        }
        
        public static void LogTrace(string category, string message, object context = null)
        {
            Instance?.Log(LogLevel.Trace, category, message, context);
        }
        
        public static void LogDebug(string category, string message, object context = null)
        {
            Instance?.Log(LogLevel.Debug, category, message, context);
        }
        
        public static void LogInfo(string category, string message, object context = null)
        {
            Instance?.Log(LogLevel.Info, category, message, context);
        }
        
        public static void LogWarning(string category, string message, object context = null)
        {
            Instance?.Log(LogLevel.Warning, category, message, context);
        }
        
        public static void LogError(string category, string message, Exception exception = null, object context = null)
        {
            Instance?.Log(LogLevel.Error, category, message, context, exception);
        }
        
        public static void LogCritical(string category, string message, Exception exception = null, object context = null)
        {
            Instance?.Log(LogLevel.Critical, category, message, context, exception);
        }
        
        public void Log(LogLevel level, string category, string message, object context = null, Exception exception = null)
        {
            // Check minimum log level
            if (level < _minimumLogLevel)
                return;
            
            // Create log entry
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Context = context,
                Exception = exception,
                StackTrace = _enableStackTraces && (level >= LogLevel.Warning || _enableVerboseLogging) 
                    ? Environment.StackTrace : null,
                ThreadId = _enableThreadInfo ? System.Threading.Thread.CurrentThread.ManagedThreadId : 0
            };
            
            // Process the log entry
            ProcessLogEntry(entry);
        }
        
        private void ProcessLogEntry(LogEntry entry)
        {
            // Add to buffer
            _logBuffer.Enqueue(entry);
            
            // Trim buffer if too large
            while (_logBuffer.Count > _logBufferSize)
            {
                _logBuffer.Dequeue();
            }
            
            // Trigger event
            OnLogEntryCreated?.Invoke(entry);
            
            // Handle critical errors immediately
            if (entry.Level >= LogLevel.Critical)
            {
                OnCriticalError?.Invoke(entry.Level, entry.Message);
                FlushLogBuffer(); // Immediate flush for critical errors
            }
            
            // Process asynchronously or synchronously
            if (_enableAsyncLogging)
            {
                _ = ProcessLogEntryAsync(entry);
            }
            else
            {
                ProcessLogEntrySync(entry);
            }
        }
        
        private async Task ProcessLogEntryAsync(LogEntry entry)
        {
            await Task.Run(() => ProcessLogEntrySync(entry));
        }
        
        private void ProcessLogEntrySync(LogEntry entry)
        {
            // Send to all configured destinations
            foreach (var destination in _logDestinations.Values)
            {
                try
                {
                    destination.WriteLog(entry);
                }
                catch (Exception ex)
                {
                    // Fallback to Unity Debug.Log to avoid infinite recursion
                    ChimeraLogger.LogError($"[LoggingInfrastructure] Failed to write to log destination: {ex.Message}");
                }
            }
        }
        
        private void FlushLogBuffer()
        {
            // Flush all destinations
            foreach (var destination in _logDestinations.Values)
            {
                try
                {
                    destination.Flush();
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[LoggingInfrastructure] Failed to flush log destination: {ex.Message}");
                }
            }
        }
        
        private void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                Array.Sort(logFiles, (x, y) => File.GetCreationTime(y).CompareTo(File.GetCreationTime(x)));
                
                // Delete excess files
                for (int i = _maxLogFiles; i < logFiles.Length; i++)
                {
                    File.Delete(logFiles[i]);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[LoggingInfrastructure] Failed to cleanup old log files: {ex.Message}");
            }
        }
        
        public void SetLogLevel(LogLevel minimumLevel)
        {
            _minimumLogLevel = minimumLevel;
            LogInfo("LoggingInfrastructure", $"Log level set to {minimumLevel}");
        }
        
        public void EnableVerboseLogging(bool enable)
        {
            _enableVerboseLogging = enable;
            LogInfo("LoggingInfrastructure", $"Verbose logging {(enable ? "enabled" : "disabled")}");
        }
        
        public List<LogEntry> GetRecentLogEntries(int maxEntries = 100)
        {
            var entries = new List<LogEntry>();
            var bufferArray = _logBuffer.ToArray();
            
            int startIndex = Math.Max(0, bufferArray.Length - maxEntries);
            for (int i = startIndex; i < bufferArray.Length; i++)
            {
                entries.Add(bufferArray[i]);
            }
            
            return entries;
        }
        
        public void AddLogDestination(string name, ILogDestination destination)
        {
            _logDestinations[name] = destination;
            LogInfo("LoggingInfrastructure", $"Added log destination: {name}");
        }
        
        public void RemoveLogDestination(string name)
        {
            if (_logDestinations.ContainsKey(name))
            {
                _logDestinations[name].Dispose();
                _logDestinations.Remove(name);
                LogInfo("LoggingInfrastructure", $"Removed log destination: {name}");
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushLogBuffer();
                LogInfo("LoggingInfrastructure", "Application paused - logs flushed");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushLogBuffer();
            }
        }
        
        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            if (_instance == this)
            {
                Application.logMessageReceived -= HandleUnityLogMessage;
                FlushLogBuffer();
                
                // Dispose all destinations
                foreach (var destination in _logDestinations.Values)
                {
                    destination.Dispose();
                }
                
                _instance = null;
            }
        }
    
    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;
    
    public virtual void OnRegistered() 
    { 
        // Override in derived classes if needed
    }
    
    public virtual void OnUnregistered() 
    { 
        // Override in derived classes if needed
    }

}
    
    // Log Entry Structure
    [System.Serializable]
    public class LogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public string Category;
        public string Message;
        public object Context;
        public Exception Exception;
        public string StackTrace;
        public int ThreadId;
        
        public override string ToString()
        {
            var timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = Level.ToString().ToUpper().PadRight(8);
            var category = Category.PadRight(20);
            
            var result = $"[{timestamp}] [{level}] [{category}] {Message}";
            
            if (Exception != null)
            {
                result += $"\nException: {Exception}";
            }
            
            if (!string.IsNullOrEmpty(StackTrace))
            {
                result += $"\nStack Trace:\n{StackTrace}";
            }
            
            return result;
        }
    }
    
    // Log Levels
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }
    
    // Log Destination Interface
    public interface ILogDestination : IDisposable
    {
        void WriteLog(LogEntry entry);
        void Flush();
    }
    
    // Console Log Destination
    public class ConsoleLogDestination : ILogDestination
    {
        public void WriteLog(LogEntry entry)
        {
            var message = FormatLogEntry(entry);
            
            switch (entry.Level)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    ChimeraLogger.LogError(message);
                    break;
                case LogLevel.Warning:
                    ChimeraLogger.LogWarning(message);
                    break;
                default:
                    ChimeraLogger.Log(message);
                    break;
            }
        }
        
        private string FormatLogEntry(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
            return $"[{timestamp}] [{entry.Category}] {entry.Message}";
        }
        
        public void Flush()
        {
            // Console doesn't need explicit flushing
        }
        
        public void Dispose()
        {
            // Nothing to dispose for console
        }
    }
    
    // File Log Destination
    public class FileLogDestination : ILogDestination
    {
        private readonly string _filePath;
        private readonly long _maxFileSize;
        private StreamWriter _writer;
        private readonly object _lock = new object();
        
        public FileLogDestination(string filePath, long maxFileSize)
        {
            _filePath = filePath;
            _maxFileSize = maxFileSize;
            InitializeWriter();
        }
        
        private void InitializeWriter()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = new StreamWriter(_filePath, append: true);
                _writer.AutoFlush = false;
            }
        }
        
        public void WriteLog(LogEntry entry)
        {
            lock (_lock)
            {
                if (_writer == null) return;
                
                try
                {
                    _writer.WriteLine(entry.ToString());
                    
                    // Check file size and rotate if necessary
                    if (_writer.BaseStream.Length > _maxFileSize)
                    {
                        RotateLogFile();
                    }
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[FileLogDestination] Failed to write log: {ex.Message}");
                }
            }
        }
        
        private void RotateLogFile()
        {
            try
            {
                _writer.Flush();
                _writer.Close();
                
                // Create new file with timestamp
                var directory = Path.GetDirectoryName(_filePath);
                var baseFileName = Path.GetFileNameWithoutExtension(_filePath);
                var extension = Path.GetExtension(_filePath);
                var newFileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var newFilePath = Path.Combine(directory, newFileName);
                
                File.Move(_filePath, newFilePath);
                InitializeWriter();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[FileLogDestination] Failed to rotate log file: {ex.Message}");
            }
        }
        
        public void Flush()
        {
            lock (_lock)
            {
                _writer?.Flush();
            }
        }
        
        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
    
    // Unity Diagnostics Log Destination
    public class UnityDiagnosticsLogDestination : ILogDestination
    {
        private readonly Unity62DiagnosticsIntegration _diagnostics;
        
        public UnityDiagnosticsLogDestination(Unity62DiagnosticsIntegration diagnostics)
        {
            _diagnostics = diagnostics;
        }
        
        public void WriteLog(LogEntry entry)
        {
            if (_diagnostics == null) return;
            
            // Only send significant log entries to Unity Diagnostics
            if (entry.Level < LogLevel.Warning) return;
            
            var logData = new Dictionary<string, object>
            {
                ["log_level"] = entry.Level.ToString(),
                ["log_category"] = entry.Category,
                ["log_message"] = entry.Message,
                ["timestamp"] = entry.Timestamp.ToString("O")
            };
            
            if (entry.Exception != null)
            {
                logData["exception_type"] = entry.Exception.GetType().Name;
                logData["exception_message"] = entry.Exception.Message;
            }
            
            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                logData["has_stack_trace"] = true;
            }
            
            _ = _diagnostics.SendCustomEventAsync("application_log", logData);
        }
        
        public void Flush()
        {
            // Unity Diagnostics handles its own flushing
        }
        
        public void Dispose()
        {
            // Nothing to dispose for Unity Diagnostics
        }
    }
    
    // Utility Extensions for Common Logging Patterns
    public static class LoggingExtensions
    {
        public static void LogMethodEntry(this MonoBehaviour component, string methodName, params object[] parameters)
        {
            LoggingInfrastructure.LogTrace(component.GetType().Name, 
                $"Entering {methodName}" + (parameters.Length > 0 ? $" with parameters: {string.Join(", ", parameters)}" : ""));
        }
        
        public static void LogMethodExit(this MonoBehaviour component, string methodName, object result = null)
        {
            LoggingInfrastructure.LogTrace(component.GetType().Name, 
                $"Exiting {methodName}" + (result != null ? $" with result: {result}" : ""));
        }
        
        public static void LogPerformance(this MonoBehaviour component, string operation, TimeSpan duration)
        {
            LoggingInfrastructure.LogDebug(component.GetType().Name, 
                $"Performance: {operation} took {duration.TotalMilliseconds:F2}ms");
        }
    }
}