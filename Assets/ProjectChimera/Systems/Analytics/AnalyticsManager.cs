using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Analytics Manager - orchestrates all analytics components
    /// Maintains the original IAnalyticsService interface while using modular components
    /// Refactored from monolithic 1,175-line class into focused components
    /// </summary>
    public class AnalyticsManager : DIChimeraManager, IAnalyticsService, ITickable
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool _enableAnalytics = true;
        [SerializeField] private float _updateInterval = 5.0f;
        [SerializeField] private int _maxDataPoints = 1000;
        [SerializeField] private bool _enableDebugLogging = false;

        [Header("Data Collection Settings")]
        [SerializeField] private bool _trackYieldMetrics = true;
        [SerializeField] private bool _trackCashFlow = true;
        [SerializeField] private bool _trackEnergyUsage = true;
        [SerializeField] private bool _trackFacilityMetrics = true;

        // Analytics components
        private IAnalyticsCore _coreAnalytics;
        private IEventAnalytics _eventAnalytics;
        private IPerformanceAnalytics _performanceAnalytics;
        private IReportingAnalytics _reportingAnalytics;

        // Update timing
        private float _lastUpdateTime;
        private bool _isInitialized = false;

        public string SystemName => "Analytics Manager";
        public bool IsEnabled => _enableAnalytics && _isInitialized;

        /// <summary>
        /// Static helper to get the analytics service from the DI container
        /// </summary>
        public static IAnalyticsService GetService()
        {
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                return serviceContainer?.TryResolve<IAnalyticsService>();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogWarning($"[AnalyticsManager] Failed to get service from DI container: {ex.Message}");
                return null;
            }
        }

        #region Service Interface Implementation

        public float GetMetric(string metricName)
        {
            return GetCurrentMetric(metricName);
        }

        public List<MetricDataPoint> GetMetricHistory(string metricName, TimeRange timeRange)
        {
            return _reportingAnalytics?.GetMetricHistory(metricName, timeRange) ?? new List<MetricDataPoint>();
        }

        public Dictionary<string, float> GetCurrentMetrics()
        {
            return _coreAnalytics?.GetCurrentMetrics() ?? new Dictionary<string, float>();
        }

        Dictionary<string, object> IAnalyticsService.GetCurrentMetrics()
        {
            var floatMetrics = GetCurrentMetrics();
            var result = new Dictionary<string, object>();
            foreach (var kvp in floatMetrics)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        Dictionary<string, object> IAnalyticsService.GetCurrentMetrics(string category)
        {
            var floatMetrics = GetCurrentMetrics();
            var result = new Dictionary<string, object>();
            foreach (var kvp in floatMetrics)
            {
                if (string.IsNullOrEmpty(category) || kvp.Key.StartsWith(category + "."))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        List<Dictionary<string, object>> IAnalyticsService.GetMetricHistory(string metricKey, int count, string category)
        {
            // Convert MetricDataPoint history to Dictionary format
            var history = GetMetricHistory(metricKey, ProjectChimera.Systems.Analytics.TimeRange.LastHour);
            return history.Take(count).Select(m => new Dictionary<string, object>
            {
                ["timestamp"] = m.Timestamp,
                ["value"] = m.Value,
                ["metric"] = m.MetricName
            }).ToList();
        }

        public void RegisterMetricCollector(string metricName, IMetricCollector collector)
        {
            _eventAnalytics?.RegisterMetricCollector(metricName, collector);
        }

        public void UnregisterMetricCollector(string metricName)
        {
            _eventAnalytics?.UnregisterMetricCollector(metricName);
        }

        public void RecordMetric(string metricName, float value)
        {
            _coreAnalytics?.RecordMetric(metricName, value);
        }

        public float GetCurrentMetric(string metricName)
        {
            return _coreAnalytics?.GetMetric(metricName) ?? 0f;
        }

        public Dictionary<string, float> GetAllMetrics()
        {
            return _coreAnalytics?.GetCurrentMetrics() ?? new Dictionary<string, float>();
        }

        public void ClearMetrics()
        {
            _coreAnalytics?.ClearMetrics();
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        public int Priority => TickPriority.AnalyticsManager;
        public bool Enabled => IsEnabled;

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || Time.time - _lastUpdateTime < _updateInterval) return;

            UpdateMetrics();
            _lastUpdateTime = Time.time;
        }

        public void OnRegistered()
        {
            // Called when registered with UpdateOrchestrator
        }

        public void OnUnregistered()
        {
            // Called when unregistered from UpdateOrchestrator
        }

        #endregion

        #region Manager Lifecycle

        protected override void OnManagerInitialize()
        {
            try
            {
                InitializeAllComponents();
                _isInitialized = true;
                _lastUpdateTime = Time.time;

                if (_enableDebugLogging)
                    ChimeraLogger.Log("[AnalyticsManager] Analytics system initialized successfully");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsManager] Failed to initialize analytics: {ex.Message}");
                _enableAnalytics = false;
            }
        }

        protected override void OnManagerShutdown()
        {
            try
            {
                _coreAnalytics?.Shutdown();
                _eventAnalytics?.Shutdown();
                _reportingAnalytics?.Shutdown();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsManager] Error during shutdown: {ex.Message}");
            }

            _isInitialized = false;

            if (_enableDebugLogging)
                ChimeraLogger.Log("[AnalyticsManager] Analytics system shutdown complete");
        }

        #endregion

        #region Component Management

        private void InitializeComponents()
        {
            // Create analytics components
            _coreAnalytics = new CoreAnalytics();
            _reportingAnalytics = new ReportingAnalytics();
            _eventAnalytics = new EventAnalytics(_coreAnalytics);
            _performanceAnalytics = new PerformanceAnalytics(_coreAnalytics, _reportingAnalytics);
        }

        private void InitializeAllComponents()
        {
            // Configure components
            (_coreAnalytics as CoreAnalytics)?.SetDebugLogging(_enableDebugLogging);
            (_eventAnalytics as EventAnalytics)?.SetDebugLogging(_enableDebugLogging);
            (_eventAnalytics as EventAnalytics)?.SetTrackingOptions(_trackYieldMetrics, _trackCashFlow, _trackEnergyUsage);
            (_performanceAnalytics as PerformanceAnalytics)?.SetDebugLogging(_enableDebugLogging);
            (_reportingAnalytics as ReportingAnalytics)?.SetConfiguration(_maxDataPoints, _enableDebugLogging);

            // Initialize all components
            _coreAnalytics.Initialize();
            _reportingAnalytics.Initialize();
            (_eventAnalytics as EventAnalytics)?.Initialize();

            if (_enableDebugLogging)
                ChimeraLogger.Log("[AnalyticsManager] All components initialized");
        }

        private void UpdateMetrics()
        {
            try
            {
                // Collect all metrics via event analytics
                _eventAnalytics?.ProcessMetrical();

                // Update derived metrics via performance analytics
                _performanceAnalytics?.UpdateDerivedMetrics();

                if (_enableDebugLogging)
                    ChimeraLogger.Log("[AnalyticsManager] Metrics update completed");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnalyticsManager] Error updating metrics: {ex.Message}");
            }
        }

        #endregion

        #region Extended Interface for Facility Support

        public void SetFacilityFilter(string facilityName)
        {
            _reportingAnalytics?.SetFacilityFilter(facilityName);
        }

        public string GetCurrentFacilityFilter()
        {
            return _reportingAnalytics?.GetCurrentFacilityFilter() ?? "All Facilities";
        }

        public List<string> GetAvailableFacilities()
        {
            return _reportingAnalytics?.GetAvailableFacilities() ?? new List<string>();
        }

        public void RecordMetricForFacility(string facilityName, string metricName, float value)
        {
            _reportingAnalytics?.RecordMetricForFacility(facilityName, metricName, value);
        }

        public Dictionary<string, float> GetFacilityMetrics(string facilityName)
        {
            return _reportingAnalytics?.GetFacilityMetrics(facilityName) ?? new Dictionary<string, float>();
        }

        public Dictionary<string, float> GetPerformanceSummary()
        {
            return _performanceAnalytics?.GetPerformanceSummary() ?? new Dictionary<string, float>();
        }

        public float GetAverageMetric(string metricName, TimeRange timeRange)
        {
            return _performanceAnalytics?.GetAverageMetric(metricName, timeRange) ?? 0f;
        }

        #endregion

        #region Configuration

        public void SetAnalyticsEnabled(bool enabled)
        {
            _enableAnalytics = enabled;
        }

        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Mathf.Max(0.1f, interval);
        }

        public void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;

            // Update all components
            (_coreAnalytics as CoreAnalytics)?.SetDebugLogging(enabled);
            (_eventAnalytics as EventAnalytics)?.SetDebugLogging(enabled);
            (_performanceAnalytics as PerformanceAnalytics)?.SetDebugLogging(enabled);
            (_reportingAnalytics as ReportingAnalytics)?.SetConfiguration(_maxDataPoints, enabled);
        }

        #endregion
    }
}
