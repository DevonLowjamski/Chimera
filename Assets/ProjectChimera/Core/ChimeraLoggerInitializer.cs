using UnityEngine;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

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
                Logger.LogInfo("ChimeraLoggerInitializer", "$1");
                Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            }

            TestLogLevels();
        }

        private void ConfigureLogger()
        {
            // Configure the simple ChimeraLogger with boolean flags
            Logger.DebugEnabled = _enableDebugLogs;
            Logger.WarningsEnabled = _enableWarnings;
            Logger.ErrorsEnabled = _enableErrors;

            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        private void TestLogLevels()
        {
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        [ContextMenu("Reconfigure Logger")]
        public void ReconfigureLogger()
        {
            ConfigureLogger();
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        [ContextMenu("Show Logger Configuration")]
        public void ShowConfiguration()
        {
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        private string GetConfiguration()
        {
            return $"ChimeraLogger Configuration:\n" +
                   $"  Debug Enabled: {Logger.DebugEnabled}\n" +
                   $"  Warnings Enabled: {Logger.WarningsEnabled}\n" +
                   $"  Errors Enabled: {Logger.ErrorsEnabled}";
        }

        /// <summary>
        /// Initialize the logger programmatically with custom settings
        /// </summary>
        /// <param name="enableDebug">Enable debug logging</param>
        /// <param name="enableWarnings">Enable warning logging</param>
        /// <param name="enableErrors">Enable error logging</param>
        public static void InitializeLogger(bool enableDebug = true, bool enableWarnings = true, bool enableErrors = true)
        {
            Logger.DebugEnabled = enableDebug;
            Logger.WarningsEnabled = enableWarnings;
            Logger.ErrorsEnabled = enableErrors;

            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        /// <summary>
        /// Configure logger for Unity editor mode
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ConfigureForEditor()
        {
            #if UNITY_EDITOR
            Logger.DebugEnabled = true;
            Logger.WarningsEnabled = true;
            Logger.ErrorsEnabled = true;
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            #endif
        }

        /// <summary>
        /// Example component showing different logging categories and levels
        /// </summary>
        [ContextMenu("Run Logger Examples")]
        public void RunLoggerExamples()
        {
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");

            // Category-based logging
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }
    }

    /// <summary>
    /// Example MonoBehaviour showing ChimeraLogger usage patterns
    /// </summary>
    public class ChimeraLoggerExample : MonoBehaviour
    {
        private void Start()
        {
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }

        [ContextMenu("Test Logger")]
        public void TestLogger()
        {
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
            Logger.LogInfo("ChimeraLoggerInitializer", "$1");
        }
    }
}
