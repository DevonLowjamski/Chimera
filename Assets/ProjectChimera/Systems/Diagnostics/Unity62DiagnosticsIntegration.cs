using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// SIMPLE: Basic diagnostics integration aligned with Project Chimera's simple simulation vision.
    /// Focuses on essential error reporting and basic logging without complex analytics.
    /// </summary>
    public class Unity62DiagnosticsIntegration : MonoBehaviour
    {
        [Header("Basic Diagnostics Settings")]
        [SerializeField] private bool _enableBasicLogging = true;
        [SerializeField] private bool _enableErrorReporting = true;
        [SerializeField] private bool _enablePerformanceLogging = false;

        // Basic diagnostic tracking
        private readonly List<string> _loggedMessages = new List<string>();
        private readonly List<string> _errorMessages = new List<string>();
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic diagnostics
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableBasicLogging)
            {
                ChimeraLogger.Log("DIAGNOSTICS", "Unity62DiagnosticsIntegration initialized", this);
            }
        }

        /// <summary>
        /// Log a basic message
        /// </summary>
        public void LogMessage(string message, LogLevel level = LogLevel.Info)
        {
            if (!_enableBasicLogging) return;

            var formattedMessage = $"[{level.ToString().ToUpper()}] {message}";
            ChimeraLogger.Log("DIAGNOSTICS", formattedMessage, this);

            _loggedMessages.Add(formattedMessage);

            // Keep only recent messages
            if (_loggedMessages.Count > 100)
            {
                _loggedMessages.RemoveAt(0);
            }
        }

        /// <summary>
        /// Report an error
        /// </summary>
        public void ReportError(string errorMessage, string context = "")
        {
            if (!_enableErrorReporting) return;

            var fullErrorMessage = string.IsNullOrEmpty(context) ?
                $"Error: {errorMessage}" :
                $"Error in {context}: {errorMessage}";

            ChimeraLogger.LogError("DIAGNOSTICS", fullErrorMessage, this);
            _errorMessages.Add(fullErrorMessage);

            // Keep only recent errors
            if (_errorMessages.Count > 50)
            {
                _errorMessages.RemoveAt(0);
            }
        }

        /// <summary>
        /// Log performance metric
        /// </summary>
        public void LogPerformanceMetric(string metricName, float value)
        {
            if (!_enablePerformanceLogging) return;

            var message = $"Performance: {metricName} = {value:F2}";
            LogMessage(message, LogLevel.Info);
        }

        /// <summary>
        /// Get recent logged messages
        /// </summary>
        public List<string> GetRecentMessages(int count = 10)
        {
            var messages = new List<string>(_loggedMessages);
            if (messages.Count > count)
            {
                messages = messages.GetRange(messages.Count - count, count);
            }
            return messages;
        }

        /// <summary>
        /// Get recent error messages
        /// </summary>
        public List<string> GetRecentErrors(int count = 10)
        {
            var errors = new List<string>(_errorMessages);
            if (errors.Count > count)
            {
                errors = errors.GetRange(errors.Count - count, count);
            }
            return errors;
        }

        /// <summary>
        /// Clear all logged messages
        /// </summary>
        public void ClearLogs()
        {
            _loggedMessages.Clear();
            _errorMessages.Clear();

            if (_enableBasicLogging)
            {
                ChimeraLogger.Log("DIAGNOSTICS", "Diagnostics logs cleared", this);
            }
        }

        /// <summary>
        /// Get diagnostics summary
        /// </summary>
        public DiagnosticsSummary GetDiagnosticsSummary()
        {
            return new DiagnosticsSummary
            {
                TotalMessages = _loggedMessages.Count,
                TotalErrors = _errorMessages.Count,
                IsInitialized = _isInitialized,
                LoggingEnabled = _enableBasicLogging,
                ErrorReportingEnabled = _enableErrorReporting,
                PerformanceLoggingEnabled = _enablePerformanceLogging
            };
        }

        /// <summary>
        /// Set logging enabled/disabled
        /// </summary>
        public void SetLoggingEnabled(bool enabled)
        {
            _enableBasicLogging = enabled;

            if (_enableBasicLogging)
            {
                ChimeraLogger.Log("DIAGNOSTICS", "Diagnostics logging enabled", this);
            }
        }

        /// <summary>
        /// Set error reporting enabled/disabled
        /// </summary>
        public void SetErrorReportingEnabled(bool enabled)
        {
            _enableErrorReporting = enabled;

            if (_enableBasicLogging)
            {
                ChimeraLogger.Log("DIAGNOSTICS", "Error reporting setting updated", this);
            }
        }
    }

    /// <summary>
    /// Diagnostics summary
    /// </summary>
    [System.Serializable]
    public class DiagnosticsSummary
    {
        public int TotalMessages;
        public int TotalErrors;
        public bool IsInitialized;
        public bool LoggingEnabled;
        public bool ErrorReportingEnabled;
        public bool PerformanceLoggingEnabled;
    }
}
