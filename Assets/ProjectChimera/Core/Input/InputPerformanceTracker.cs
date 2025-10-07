using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Input Performance Tracker
    /// Single Responsibility: Tracking performance metrics and statistics for input systems
    /// Extracted from OptimizedInputManager for better separation of concerns
    /// MIGRATED: Uses ITickable for centralized update management
    /// </summary>
    public class InputPerformanceTracker : MonoBehaviour, ITickable
    {
        [Header("Performance Tracking Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedTracking = true;
        [SerializeField] private float _performanceReportInterval = 10f; // seconds
        [SerializeField] private int _maxPerformanceHistory = 100;

        // Performance tracking
        private InputPerformanceStats _currentStats = new InputPerformanceStats();
        private InputPerformanceStats _lastReportedStats = new InputPerformanceStats();
        private float _lastReportTime;

        // Detailed performance tracking
        private float _sessionStartTime;
        private int _totalUpdateCycles;
        private float _totalUpdateTime;

        // State tracking
        private bool _isInitialized = false;
        private bool _trackingEnabled = true;

        // Events
        public event System.Action<InputPerformanceStats> OnPerformanceStatsUpdated;
        public event System.Action<InputPerformanceReport> OnPerformanceReportGenerated;

        public bool IsInitialized => _isInitialized;
        public InputPerformanceStats CurrentStats => _currentStats;
        public bool IsTrackingEnabled => _trackingEnabled;

        public void Initialize()
        {
            if (_isInitialized) return;

            ResetStats();
            _sessionStartTime = Time.realtimeSinceStartup;
            _lastReportTime = Time.time;
            _totalUpdateCycles = 0;
            _totalUpdateTime = 0f;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Input Performance Tracker initialized", this);
            }
        }

        /// <summary>
        /// Record input poll operation
        /// </summary>
        public void RecordInputPoll(float pollTime)
        {
            if (!_isInitialized || !_trackingEnabled) return;

            _currentStats.InputPolls++;
            _currentStats.TotalPollTime += pollTime;
            _currentStats.AveragePollTime = _currentStats.TotalPollTime / _currentStats.InputPolls;

            if (pollTime > _currentStats.MaxPollTime)
                _currentStats.MaxPollTime = pollTime;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// Record event processing operation
        /// </summary>
        public void RecordEventProcessing(int eventsProcessed, float processingTime)
        {
            if (!_isInitialized || !_trackingEnabled) return;

            _currentStats.EventsProcessed += eventsProcessed;
            _currentStats.TotalProcessingTime += processingTime;
            _currentStats.AverageProcessingTime = _currentStats.EventsProcessed > 0
                ? _currentStats.TotalProcessingTime / _currentStats.EventsProcessed
                : 0f;

            if (processingTime > _currentStats.MaxProcessingTime)
                _currentStats.MaxProcessingTime = processingTime;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// Record handler notification operation
        /// </summary>
        public void RecordHandlerNotification(int handlersNotified, float notificationTime)
        {
            if (!_isInitialized || !_trackingEnabled) return;

            _currentStats.HandlerNotifications += handlersNotified;
            _currentStats.TotalNotificationTime += notificationTime;
            _currentStats.AverageNotificationTime = _currentStats.HandlerNotifications > 0
                ? _currentStats.TotalNotificationTime / _currentStats.HandlerNotifications
                : 0f;

            if (notificationTime > _currentStats.MaxNotificationTime)
                _currentStats.MaxNotificationTime = notificationTime;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// Record update cycle
        /// </summary>
        public void RecordUpdateCycle(float updateTime)
        {
            if (!_isInitialized || !_trackingEnabled) return;

            _currentStats.UpdateCycles++;
            _totalUpdateCycles++;
            _totalUpdateTime += updateTime;

            _currentStats.TotalUpdateTime += updateTime;
            _currentStats.AverageUpdateTime = _currentStats.UpdateCycles > 0
                ? _currentStats.TotalUpdateTime / _currentStats.UpdateCycles
                : 0f;

            if (updateTime > _currentStats.MaxUpdateTime)
                _currentStats.MaxUpdateTime = updateTime;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// Record registered handlers count
        /// </summary>
        public void RecordHandlerRegistration(int totalHandlers, bool registered)
        {
            if (!_isInitialized || !_trackingEnabled) return;

            _currentStats.RegisteredHandlers = totalHandlers;

            if (registered)
                _currentStats.HandlerRegistrations++;
            else
                _currentStats.HandlerUnregistrations++;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public InputPerformanceMetrics GetCurrentMetrics()
        {
            if (!_isInitialized) return new InputPerformanceMetrics();

            float sessionTime = Time.realtimeSinceStartup - _sessionStartTime;

            return new InputPerformanceMetrics
            {
                SessionDuration = sessionTime,
                TotalEvents = _currentStats.EventsProcessed,
                EventsPerSecond = sessionTime > 0f ? _currentStats.EventsProcessed / sessionTime : 0f,
                AverageFrameTime = _currentStats.AverageUpdateTime,
                PerformanceScore = CalculatePerformanceScore(),
                EfficiencyRating = CalculateEfficiencyRating()
            };
        }

        /// <summary>
        /// Generate performance report
        /// </summary>
        public InputPerformanceReport GeneratePerformanceReport()
        {
            if (!_isInitialized) return new InputPerformanceReport();

            var report = new InputPerformanceReport
            {
                ReportTime = Time.time,
                SessionDuration = Time.realtimeSinceStartup - _sessionStartTime,
                CurrentStats = _currentStats,
                Metrics = GetCurrentMetrics(),
                PerformanceTrend = CalculatePerformanceTrend()
            };

            OnPerformanceReportGenerated?.Invoke(report);

            if (_enableLogging)
            {
                LogPerformanceReport(report);
            }

            _lastReportedStats = _currentStats;
            return report;
        }

        /// <summary>
        /// Reset performance statistics
        /// </summary>
        public void ResetPerformanceStats()
        {
            ResetStats();
            _sessionStartTime = Time.realtimeSinceStartup;
            _totalUpdateCycles = 0;
            _totalUpdateTime = 0f;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Performance statistics reset", this);
            }
        }

        /// <summary>
        /// Enable or disable performance tracking
        /// </summary>
        public void SetTrackingEnabled(bool enabled)
        {
            _trackingEnabled = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Performance tracking {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Set performance report interval
        /// </summary>
        public void SetReportInterval(float intervalSeconds)
        {
            _performanceReportInterval = Mathf.Max(1f, intervalSeconds);

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Performance report interval set to {_performanceReportInterval}s", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Update performance statistics
        /// </summary>
        private void UpdatePerformanceStats()
        {
            OnPerformanceStatsUpdated?.Invoke(_currentStats);

            // Generate periodic reports
            if (Time.time - _lastReportTime >= _performanceReportInterval)
            {
                GeneratePerformanceReport();
                _lastReportTime = Time.time;
            }
        }

        /// <summary>
        /// Calculate overall performance score (0-1 range)
        /// </summary>
        private float CalculatePerformanceScore()
        {
            float score = 1f;

            // Penalize high processing times
            if (_currentStats.AverageUpdateTime > 0.016f) // 60 FPS target
            {
                score -= (_currentStats.AverageUpdateTime - 0.016f) * 10f;
            }

            // Penalize high poll times
            if (_currentStats.AveragePollTime > 0.001f)
            {
                score -= (_currentStats.AveragePollTime - 0.001f) * 100f;
            }

            // Boost score for high event throughput
            var sessionTime = Time.realtimeSinceStartup - _sessionStartTime;
            if (sessionTime > 0f)
            {
                float eventsPerSecond = _currentStats.EventsProcessed / sessionTime;
                if (eventsPerSecond > 60f) // Good event processing rate
                {
                    score += 0.1f;
                }
            }

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Calculate efficiency rating
        /// </summary>
        private InputEfficiencyRating CalculateEfficiencyRating()
        {
            float score = CalculatePerformanceScore();

            if (score >= 0.9f) return InputEfficiencyRating.Excellent;
            if (score >= 0.8f) return InputEfficiencyRating.Good;
            if (score >= 0.6f) return InputEfficiencyRating.Fair;
            if (score >= 0.4f) return InputEfficiencyRating.Poor;
            return InputEfficiencyRating.Critical;
        }

        /// <summary>
        /// Calculate performance trend
        /// </summary>
        private InputPerformanceTrend CalculatePerformanceTrend()
        {
            // Compare current stats with last reported stats
            if (_lastReportedStats.UpdateCycles == 0)
                return InputPerformanceTrend.Stable;

            float currentAvgTime = _currentStats.AverageUpdateTime;
            float lastAvgTime = _lastReportedStats.AverageUpdateTime;

            if (currentAvgTime < lastAvgTime * 0.9f)
                return InputPerformanceTrend.Improving;
            else if (currentAvgTime > lastAvgTime * 1.1f)
                return InputPerformanceTrend.Declining;
            else
                return InputPerformanceTrend.Stable;
        }

        /// <summary>
        /// Log performance report
        /// </summary>
        private void LogPerformanceReport(InputPerformanceReport report)
        {
            var message = $"INPUT PERFORMANCE REPORT:\n" +
                         $"Session Duration: {report.SessionDuration:F1}s\n" +
                         $"Events Processed: {report.CurrentStats.EventsProcessed}\n" +
                         $"Average Update Time: {report.CurrentStats.AverageUpdateTime * 1000f:F2}ms\n" +
                         $"Performance Score: {report.Metrics.PerformanceScore:F2}\n" +
                         $"Efficiency Rating: {report.Metrics.EfficiencyRating}\n" +
                         $"Performance Trend: {report.PerformanceTrend}";

            ChimeraLogger.Log("INPUT", message, this);
        }

        /// <summary>
        /// Reset statistics to default values
        /// </summary>
        private void ResetStats()
        {
            _currentStats = new InputPerformanceStats
            {
                UpdateCycles = 0,
                InputPolls = 0,
                EventsProcessed = 0,
                RegisteredHandlers = 0,
                HandlerNotifications = 0,
                HandlerRegistrations = 0,
                HandlerUnregistrations = 0,
                TotalUpdateTime = 0f,
                TotalPollTime = 0f,
                TotalProcessingTime = 0f,
                TotalNotificationTime = 0f,
                AverageUpdateTime = 0f,
                AveragePollTime = 0f,
                AverageProcessingTime = 0f,
                AverageNotificationTime = 0f,
                MaxUpdateTime = 0f,
                MaxPollTime = 0f,
                MaxProcessingTime = 0f,
                MaxNotificationTime = 0f
            };
        }

        #endregion

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.InputSystem;
        public bool IsTickable => _isInitialized && _trackingEnabled && isActiveAndEnabled;

        public void Tick(float deltaTime)
        {
            // Automatic performance reporting
            if (Time.time - _lastReportTime >= _performanceReportInterval)
            {
                GeneratePerformanceReport();
                _lastReportTime = Time.time;
            }
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "InputPerformanceTracker registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "InputPerformanceTracker unregistered from UpdateOrchestrator", this);
        }

        // Register/unregister with UpdateOrchestrator
        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    /// <summary>
    /// Input performance metrics
    /// </summary>
    [System.Serializable]
    public struct InputPerformanceMetrics
    {
        public float SessionDuration;
        public int TotalEvents;
        public float EventsPerSecond;
        public float AverageFrameTime;
        public float PerformanceScore;
        public InputEfficiencyRating EfficiencyRating;
    }

    /// <summary>
    /// Input performance report
    /// </summary>
    [System.Serializable]
    public struct InputPerformanceReport
    {
        public float ReportTime;
        public float SessionDuration;
        public InputPerformanceStats CurrentStats;
        public InputPerformanceMetrics Metrics;
        public InputPerformanceTrend PerformanceTrend;
    }

    /// <summary>
    /// Input efficiency rating
    /// </summary>
    public enum InputEfficiencyRating
    {
        Critical = 0,
        Poor = 1,
        Fair = 2,
        Good = 3,
        Excellent = 4
    }

    /// <summary>
    /// Input performance trend
    /// </summary>
    public enum InputPerformanceTrend
    {
        Declining = -1,
        Stable = 0,
        Improving = 1
    }

    /// <summary>
    /// Input performance statistics (original from OptimizedInputManager)
    /// </summary>
    [System.Serializable]
    public struct InputPerformanceStats
    {
        public int UpdateCycles;
        public int InputPolls;
        public int EventsProcessed;
        public int RegisteredHandlers;
        public int HandlerNotifications;
        public int HandlerRegistrations;
        public int HandlerUnregistrations;

        public float TotalUpdateTime;
        public float TotalPollTime;
        public float TotalProcessingTime;
        public float TotalNotificationTime;

        public float AverageUpdateTime;
        public float AveragePollTime;
        public float AverageProcessingTime;
        public float AverageNotificationTime;

        public float MaxUpdateTime;
        public float MaxPollTime;
        public float MaxProcessingTime;
        public float MaxNotificationTime;
    }
}
