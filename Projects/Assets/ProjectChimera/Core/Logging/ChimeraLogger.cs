using UnityEngine;

namespace ProjectChimera.Core.Logging
{
    /// <summary>
    /// Centralized logging system for Project Chimera
    /// Provides consistent logging format and filtering capabilities
    /// </summary>
    public static class ChimeraLogger
    {
        private static bool _debugEnabled = true;
        private static bool _warningsEnabled = true;
        private static bool _errorsEnabled = true;
        
        /// <summary>
        /// Enable or disable debug logging
        /// </summary>
        public static bool DebugEnabled
        {
            get => _debugEnabled;
            set => _debugEnabled = value;
        }
        
        /// <summary>
        /// Enable or disable warning logging
        /// </summary>
        public static bool WarningsEnabled
        {
            get => _warningsEnabled;
            set => _warningsEnabled = value;
        }
        
        /// <summary>
        /// Enable or disable error logging
        /// </summary>
        public static bool ErrorsEnabled
        {
            get => _errorsEnabled;
            set => _errorsEnabled = value;
        }
        
        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Log(string message, Object context = null)
        {
            if (!_debugEnabled) return;
            
            var formattedMessage = FormatMessage("DEBUG", message);
            Debug.Log(formattedMessage, context);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message, Object context = null)
        {
            if (!_warningsEnabled) return;
            
            var formattedMessage = FormatMessage("WARNING", message);
            Debug.LogWarning(formattedMessage, context);
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string message, Object context = null)
        {
            if (!_errorsEnabled) return;
            
            var formattedMessage = FormatMessage("ERROR", message);
            Debug.LogError(formattedMessage, context);
        }
        
        /// <summary>
        /// Log a debug message with category
        /// </summary>
        public static void Log(string category, string message, Object context = null)
        {
            if (!_debugEnabled) return;
            
            var formattedMessage = FormatMessage($"DEBUG/{category}", message);
            Debug.Log(formattedMessage, context);
        }
        
        /// <summary>
        /// Log a warning message with category
        /// </summary>
        public static void LogWarning(string category, string message, Object context = null)
        {
            if (!_warningsEnabled) return;
            
            var formattedMessage = FormatMessage($"WARNING/{category}", message);
            Debug.LogWarning(formattedMessage, context);
        }
        
        /// <summary>
        /// Log an error message with category
        /// </summary>
        public static void LogError(string category, string message, Object context = null)
        {
            if (!_errorsEnabled) return;
            
            var formattedMessage = FormatMessage($"ERROR/{category}", message);
            Debug.LogError(formattedMessage, context);
        }
        
        /// <summary>
        /// Format log message with timestamp and category
        /// </summary>
        private static string FormatMessage(string level, string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            return $"[{timestamp}][Chimera][{level}] {message}";
        }
        
        /// <summary>
        /// Log system performance metrics
        /// </summary>
        public static void LogPerformance(string systemName, float deltaTime, Object context = null)
        {
            if (!_debugEnabled) return;
            
            var message = $"{systemName} update time: {deltaTime * 1000:F2}ms";
            Log("PERFORMANCE", message, context);
        }
        
        /// <summary>
        /// Log system initialization
        /// </summary>
        public static void LogInitialization(string systemName, bool success, Object context = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message = $"{systemName} initialization {status}";
            
            if (success)
                Log("INIT", message, context);
            else
                LogError("INIT", message, context);
        }
        
        /// <summary>
        /// Log system shutdown
        /// </summary>
        public static void LogShutdown(string systemName, Object context = null)
        {
            Log("SHUTDOWN", $"{systemName} shutdown completed", context);
        }
        
        /// <summary>
        /// Log gameplay events
        /// </summary>
        public static void LogGameplay(string eventType, string description, Object context = null)
        {
            Log($"GAMEPLAY/{eventType}", description, context);
        }
        
        /// <summary>
        /// Log data operations
        /// </summary>
        public static void LogData(string operation, string details, Object context = null)
        {
            Log($"DATA/{operation}", details, context);
        }
        
        /// <summary>
        /// Log network operations
        /// </summary>
        public static void LogNetwork(string operation, string details, Object context = null)
        {
            Log($"NETWORK/{operation}", details, context);
        }
        
        /// <summary>
        /// Log UI interactions
        /// </summary>
        public static void LogUI(string interaction, string details, Object context = null)
        {
            Log($"UI/{interaction}", details, context);
        }
        
        /// <summary>
        /// Enable all logging categories
        /// </summary>
        public static void EnableAllLogging()
        {
            _debugEnabled = true;
            _warningsEnabled = true;
            _errorsEnabled = true;
        }
        
        /// <summary>
        /// Disable all logging categories
        /// </summary>
        public static void DisableAllLogging()
        {
            _debugEnabled = false;
            _warningsEnabled = false;
            _errorsEnabled = false;
        }
        
        /// <summary>
        /// Set logging levels based on build configuration
        /// </summary>
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void ConfigureForDevelopment()
        {
            _debugEnabled = true;
            _warningsEnabled = true;
            _errorsEnabled = true;
        }
        
        /// <summary>
        /// Set logging levels for production builds
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ConfigureForProduction()
        {
            _debugEnabled = false;
            _warningsEnabled = true;
            _errorsEnabled = true;
        }
    }
} 