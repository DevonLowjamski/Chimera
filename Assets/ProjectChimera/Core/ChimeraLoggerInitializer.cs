using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Initializes ChimeraLogger settings at application startup
    /// Ensures logger is properly configured before any other systems start
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Execute very early
    public class ChimeraLoggerInitializer : MonoBehaviour
    {
        [Header("Logger Configuration")]
        [SerializeField] private bool _configureOnAwake = true;
        [SerializeField] private bool _showConfigurationOnStart = true;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _enableWarnings = true;
        [SerializeField] private bool _enableErrors = true;

        private void Awake()
        {
            if (_configureOnAwake)
            {
                ConfigureLogger();
            }
        }

        private void Start()
        {
            if (_showConfigurationOnStart)
            {
                ChimeraLogger.Log("ChimeraLoggerInitializer", "Logger configuration:");
                ChimeraLogger.Log("ChimeraLoggerInitializer", GetConfiguration());
            }
            
            TestLogLevels();
        }

        private void ConfigureLogger()
        {
            // Configure the simple ChimeraLogger with boolean flags
            ChimeraLogger.DebugEnabled = _enableDebugLogs;
            ChimeraLogger.WarningsEnabled = _enableWarnings;
            ChimeraLogger.ErrorsEnabled = _enableErrors;

            ChimeraLogger.Log("ChimeraLoggerInitializer", "ChimeraLogger initialized and configured");
        }

        private void TestLogLevels()
        {
            ChimeraLogger.Log("Verbose logging test - controlled by DebugEnabled flag");
            ChimeraLogger.Log("Info logging test - controlled by DebugEnabled flag");
            ChimeraLogger.LogWarning("Warning logging test - controlled by WarningsEnabled flag");
            ChimeraLogger.LogError("Error logging test - controlled by ErrorsEnabled flag");
        }

        [ContextMenu("Reconfigure Logger")]
        public void ReconfigureLogger()
        {
            ConfigureLogger();
            ChimeraLogger.Log("ChimeraLoggerInitializer", "Logger reconfigured at runtime");
        }

        [ContextMenu("Show Logger Configuration")]
        public void ShowConfiguration()
        {
            ChimeraLogger.Log(GetConfiguration());
        }

        private string GetConfiguration()
        {
            return $"ChimeraLogger Configuration:\n" +
                   $"  Debug Enabled: {ChimeraLogger.DebugEnabled}\n" +
                   $"  Warnings Enabled: {ChimeraLogger.WarningsEnabled}\n" +
                   $"  Errors Enabled: {ChimeraLogger.ErrorsEnabled}";
        }

        /// <summary>
        /// Initialize the logger programmatically with custom settings
        /// </summary>
        /// <param name="enableDebug">Enable debug logging</param>
        /// <param name="enableWarnings">Enable warning logging</param>  
        /// <param name="enableErrors">Enable error logging</param>
        public static void InitializeLogger(bool enableDebug = true, bool enableWarnings = true, bool enableErrors = true)
        {
            ChimeraLogger.DebugEnabled = enableDebug;
            ChimeraLogger.WarningsEnabled = enableWarnings;
            ChimeraLogger.ErrorsEnabled = enableErrors;
            
            ChimeraLogger.Log("ChimeraLoggerInitializer", "Logger initialized programmatically");
        }

        /// <summary>
        /// Configure logger for Unity editor mode
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ConfigureForEditor()
        {
            #if UNITY_EDITOR
            ChimeraLogger.DebugEnabled = true;
            ChimeraLogger.WarningsEnabled = true;
            ChimeraLogger.ErrorsEnabled = true;
            ChimeraLogger.Log("ChimeraLogger configured in editor mode");
            ChimeraLogger.Log("Logger Configuration: Debug=true, Warnings=true, Errors=true");
            #endif
        }

        /// <summary>
        /// Example component showing different logging categories and levels
        /// </summary>
        [ContextMenu("Run Logger Examples")]
        public void RunLoggerExamples()
        {
            ChimeraLogger.Log("This is basic debug logging");
            ChimeraLogger.LogWarning("This is warning logging");
            ChimeraLogger.LogError("This is error logging");

            // Category-based logging
            ChimeraLogger.Log("ExampleSystem", "System initialized successfully");
            ChimeraLogger.Log("Performance", "Operation completed successfully");
            ChimeraLogger.LogWarning("Validation", "Minor validation warning");
            ChimeraLogger.LogError("Critical", "Critical system error");
        }
    }

    /// <summary>
    /// Example MonoBehaviour showing ChimeraLogger usage patterns
    /// </summary>
    public class ChimeraLoggerExample : MonoBehaviour
    {
        private void Start()
        {
            ChimeraLogger.Log("ChimeraLoggerExample", "Example component started");
        }

        [ContextMenu("Test Logger")]
        public void TestLogger()
        {
            ChimeraLogger.Log("Test message from example");
            ChimeraLogger.LogWarning("Test warning from example");
            ChimeraLogger.LogError("Test error from example");
        }
    }
}