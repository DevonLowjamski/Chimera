using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Core analytics implementation handling basic metric storage and operations
    /// </summary>
    public class CoreAnalytics : IAnalyticsCore
    {
        private Dictionary<string, float> _currentMetrics;
        private bool _isInitialized = false;
        private bool _enableDebugLogging = false;

        public bool IsEnabled => _isInitialized;

        public void Initialize()
        {
            try
            {
                _currentMetrics = new Dictionary<string, float>();
                InitializeDefaultMetrics();
                _isInitialized = true;

                if (_enableDebugLogging)
                    ChimeraLogger.Log("[CoreAnalytics] Core analytics initialized successfully");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[CoreAnalytics] Failed to initialize: {ex.Message}");
                _isInitialized = false;
            }
        }

        public void Shutdown()
        {
            if (_currentMetrics != null)
            {
                _currentMetrics.Clear();
                _currentMetrics = null;
            }

            _isInitialized = false;

            if (_enableDebugLogging)
                ChimeraLogger.Log("[CoreAnalytics] Core analytics shutdown complete");
        }

        public float GetMetric(string metricName)
        {
            if (!IsEnabled || !_currentMetrics.ContainsKey(metricName))
                return 0f;

            return _currentMetrics[metricName];
        }

        public Dictionary<string, float> GetCurrentMetrics()
        {
            return IsEnabled ? new Dictionary<string, float>(_currentMetrics) : new Dictionary<string, float>();
        }

        public void RecordMetric(string metricName, float value, string facilityName = null)
        {
            if (!IsEnabled) return;

            _currentMetrics[metricName] = value;

            if (_enableDebugLogging)
                ChimeraLogger.Log($"[CoreAnalytics] Recorded metric {metricName}: {value}");
        }

        public void ClearMetrics()
        {
            if (!IsEnabled) return;

            _currentMetrics.Clear();
            InitializeDefaultMetrics();

            if (_enableDebugLogging)
                ChimeraLogger.Log("[CoreAnalytics] All metrics cleared");
        }

        public void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
        }

        private void InitializeDefaultMetrics()
        {
            // Initialize core metrics with default values
            _currentMetrics["YieldPerHour"] = 0f;
            _currentMetrics["CashFlow"] = 0f;
            _currentMetrics["EnergyUsage"] = 0f;
            _currentMetrics["TotalEnergyConsumed"] = 0f;
            _currentMetrics["EnergyDailyCost"] = 0f;
            _currentMetrics["EnergyEfficiency"] = 0f;
            _currentMetrics["ActivePlants"] = 0f;
            _currentMetrics["TotalRevenue"] = 0f;
            _currentMetrics["TotalExpenses"] = 0f;
            _currentMetrics["NetCashFlow"] = 0f;
            _currentMetrics["FacilityUtilization"] = 0f;
            _currentMetrics["OperationalEfficiency"] = 0f;
        }

        public string GetCurrentMetricString()
        {
            return string.Join(", ", _currentMetrics.Select(kv => $"{kv.Key}: {kv.Value:F2}"));
        }

        public void RecordMetric(string metricName, float value)
        {
            RecordMetric(metricName, value, null);
        }

        public float GetCurrentMetric(string metricName)
        {
            return GetMetric(metricName);
        }

        // Extension method functionality moved to utility class
        public static float SelectValue(System.Collections.Generic.Dictionary<string, float> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : 0f;
        }
    }
}
