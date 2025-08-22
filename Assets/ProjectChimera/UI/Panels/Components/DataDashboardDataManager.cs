using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Systems.Analytics;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles data fetching, caching, and refresh operations for the analytics dashboard in Project Chimera's game.
    /// Manages analytics service integration and provides cannabis cultivation metrics for the dashboard.
    /// </summary>
    public class DataDashboardDataManager : MonoBehaviour
    {
        [Header("Data Management Configuration")]
        [SerializeField] private bool _enableAutoRefresh = true;
        [SerializeField] private float _refreshInterval = 2.0f;
        [SerializeField] private bool _enableDataCaching = true;
        [SerializeField] private float _cacheExpirationTime = 30f;
        [SerializeField] private bool _enableDebugLogging = false;

        // Service dependencies
        public IAnalyticsService _analyticsService; // Made public for dashboard access
        private bool _isServiceAvailable;

        // Data caching
        private Dictionary<string, float> _cachedMetrics;
        private Dictionary<string, List<MetricDataPoint>> _cachedHistoryData;
        private Dictionary<string, float> _cacheTimestamps;
        private float _lastRefreshTime;

        // Current state
        private TimeRange _currentTimeRange;
        private string _currentFacility;
        private bool _isRefreshing;

        // Events
        public System.Action<Dictionary<string, float>> OnMetricsUpdated;
        public System.Action<string, List<MetricDataPoint>> OnHistoryDataUpdated;
        public System.Action<bool> OnServiceAvailabilityChanged;
        public System.Action<string> OnDataError;

        // Properties
        public bool IsRefreshing => _isRefreshing;
        public bool IsServiceAvailable => _isServiceAvailable;
        public float LastRefreshTime => _lastRefreshTime;
        public Dictionary<string, float> CurrentMetrics => new Dictionary<string, float>(_cachedMetrics);
        public TimeRange CurrentTimeRange => _currentTimeRange;
        public string CurrentFacility => _currentFacility;

        private void Awake()
        {
            InitializeDataManager();
        }

        private void Start()
        {
            InitializeAnalyticsService();
        }

        private void Update()
        {
            if (_enableAutoRefresh && Time.time >= _lastRefreshTime + _refreshInterval)
            {
                RefreshData();
            }
        }

        #region Initialization

        /// <summary>
        /// Public method to initialize the data manager
        /// </summary>
        public void Initialize()
        {
            InitializeDataManager();
            InitializeAnalyticsService();
        }

        private void InitializeDataManager()
        {
            _cachedMetrics = new Dictionary<string, float>();
            _cachedHistoryData = new Dictionary<string, List<MetricDataPoint>>();
            _cacheTimestamps = new Dictionary<string, float>();
            
            _currentTimeRange = TimeRange.Last24Hours;
            _currentFacility = "All Facilities";
            _isRefreshing = false;
            _isServiceAvailable = false;

            LogInfo("Data manager initialized for cannabis cultivation dashboard");
        }

        private void InitializeAnalyticsService()
        {
            try
            {
                _analyticsService = GetAnalyticsServiceFromDI();
                _isServiceAvailable = _analyticsService != null;
                
                OnServiceAvailabilityChanged?.Invoke(_isServiceAvailable);

                if (_isServiceAvailable)
                {
                    LogInfo("Analytics service connected successfully");
                    RefreshData();
                }
                else
                {
                    LogWarning("Analytics service not available - using placeholder data");
                    LoadPlaceholderData();
                }
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to initialize analytics service: {ex.Message}");
                OnDataError?.Invoke($"Service initialization failed: {ex.Message}");
            }
        }

        private IAnalyticsService GetAnalyticsServiceFromDI()
        {
            try
            {
                var analyticsService = AnalyticsManager.GetService();
                if (analyticsService != null)
                {
                    LogInfo("AnalyticsService obtained from DI system");
                    return analyticsService;
                }
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to get AnalyticsService from DI: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Data Refresh Operations

        public void RefreshData()
        {
            if (_isRefreshing) return;

            try
            {
                _isRefreshing = true;
                _lastRefreshTime = Time.time;

                RefreshCurrentMetrics();
                RefreshHistoryData();

                LogInfo($"Data refresh completed for {_currentFacility} ({_currentTimeRange})");
            }
            catch (System.Exception ex)
            {
                LogWarning($"Error during data refresh: {ex.Message}");
                OnDataError?.Invoke($"Data refresh failed: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void RefreshCurrentMetrics()
        {
            var metrics = GetCurrentMetricsForFacility(_currentFacility);
            
            if (metrics != null && metrics.Count > 0)
            {
                UpdateCachedMetrics(metrics);
                OnMetricsUpdated?.Invoke(metrics);
            }
            else
            {
                LogWarning("No metrics data received from analytics service");
            }
        }

        private void RefreshHistoryData()
        {
            var metricKeys = GetKnownMetricKeys();
            
            foreach (var metricKey in metricKeys)
            {
                var historyData = GetMetricHistoryForFacility(metricKey, _currentTimeRange, _currentFacility);
                
                if (historyData != null && historyData.Count > 0)
                {
                    UpdateCachedHistoryData(metricKey, historyData);
                    OnHistoryDataUpdated?.Invoke(metricKey, historyData);
                }
            }
        }

        #endregion

        #region Data Fetching

        private Dictionary<string, float> GetCurrentMetricsForFacility(string facilityName)
        {
            if (!_isServiceAvailable) 
            {
                return GetPlaceholderMetrics();
            }

            try
            {
                if (_analyticsService is AnalyticsManager analyticsManager)
                {
                    return analyticsManager.GetCurrentMetrics(facilityName);
                }
                
                return _analyticsService.GetCurrentMetrics();
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to get current metrics: {ex.Message}");
                return GetPlaceholderMetrics();
            }
        }

        private List<MetricDataPoint> GetMetricHistoryForFacility(string metricKey, TimeRange timeRange, string facilityName)
        {
            if (!_isServiceAvailable)
            {
                return GetPlaceholderHistoryData(metricKey);
            }

            try
            {
                if (_analyticsService is AnalyticsManager analyticsManager)
                {
                    return analyticsManager.GetMetricHistory(metricKey, timeRange, facilityName);
                }
                
                return _analyticsService.GetMetricHistory(metricKey, timeRange);
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to get metric history for {metricKey}: {ex.Message}");
                return GetPlaceholderHistoryData(metricKey);
            }
        }

        public List<string> GetAvailableFacilities()
        {
            if (!_isServiceAvailable)
            {
                return GetPlaceholderFacilities();
            }

            try
            {
                if (_analyticsService is AnalyticsManager analyticsManager)
                {
                    return analyticsManager.GetAvailableFacilities();
                }
                
                return GetPlaceholderFacilities();
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to get available facilities: {ex.Message}");
                return GetPlaceholderFacilities();
            }
        }

        #endregion

        #region Data Caching

        private void UpdateCachedMetrics(Dictionary<string, float> newMetrics)
        {
            if (!_enableDataCaching) return;

            foreach (var kvp in newMetrics)
            {
                _cachedMetrics[kvp.Key] = kvp.Value;
                _cacheTimestamps[kvp.Key] = Time.time;
            }
        }

        private void UpdateCachedHistoryData(string metricKey, List<MetricDataPoint> historyData)
        {
            if (!_enableDataCaching) return;

            _cachedHistoryData[metricKey] = new List<MetricDataPoint>(historyData);
            _cacheTimestamps[$"{metricKey}_history"] = Time.time;
        }

        private bool IsCacheValid(string cacheKey)
        {
            if (!_enableDataCaching) return false;
            
            return _cacheTimestamps.ContainsKey(cacheKey) && 
                   (Time.time - _cacheTimestamps[cacheKey]) < _cacheExpirationTime;
        }

        public void ClearCache()
        {
            _cachedMetrics.Clear();
            _cachedHistoryData.Clear();
            _cacheTimestamps.Clear();
            
            LogInfo("Data cache cleared");
        }

        #endregion

        #region Placeholder Data

        private void LoadPlaceholderData()
        {
            var placeholderMetrics = GetPlaceholderMetrics();
            UpdateCachedMetrics(placeholderMetrics);
            OnMetricsUpdated?.Invoke(placeholderMetrics);
            
            LogInfo("Loaded placeholder data for cannabis cultivation metrics");
        }

        private Dictionary<string, float> GetPlaceholderMetrics()
        {
            // Generate realistic placeholder data for cannabis cultivation game
            var random = new System.Random(System.DateTime.Now.Millisecond);
            
            return new Dictionary<string, float>
            {
                ["YieldPerHour"] = 2.5f + (float)(random.NextDouble() * 1.5f), // 2.5-4.0 g/hr
                ["ActivePlants"] = 25f + (float)(random.NextDouble() * 20f), // 25-45 plants
                ["CashBalance"] = 15000f + (float)(random.NextDouble() * 10000f), // $15k-25k
                ["EnergyUsage"] = 150f + (float)(random.NextDouble() * 50f), // 150-200 kWh
                ["PlantHealth"] = 85f + (float)(random.NextDouble() * 10f), // 85-95%
                ["FacilityUtilization"] = 70f + (float)(random.NextDouble() * 20f), // 70-90%
                ["NetCashFlow"] = 500f + (float)(random.NextDouble() * 1000f), // $500-1500
                ["EnergyEfficiency"] = 12f + (float)(random.NextDouble() * 6f) // 12-18 g/kWh
            };
        }

        private List<MetricDataPoint> GetPlaceholderHistoryData(string metricKey)
        {
            var dataPoints = new List<MetricDataPoint>();
            var random = new System.Random(metricKey.GetHashCode());
            
            // Generate 24 hours of data points for cannabis cultivation
            for (int i = 24; i >= 0; i--)
            {
                var timestamp = System.DateTime.Now.AddHours(-i);
                float value = GeneratePlaceholderValue(metricKey, random);
                
                dataPoints.Add(new MetricDataPoint
                {
                    Timestamp = timestamp,
                    Value = value,
                    MetricName = metricKey
                });
            }
            
            return dataPoints;
        }

        private float GeneratePlaceholderValue(string metricKey, System.Random random)
        {
            return metricKey switch
            {
                "YieldPerHour" => 2.0f + (float)(random.NextDouble() * 2.5f),
                "ActivePlants" => 20f + (float)(random.NextDouble() * 30f),
                "CashBalance" => 10000f + (float)(random.NextDouble() * 20000f),
                "EnergyUsage" => 100f + (float)(random.NextDouble() * 100f),
                "PlantHealth" => 80f + (float)(random.NextDouble() * 15f),
                "FacilityUtilization" => 60f + (float)(random.NextDouble() * 30f),
                "NetCashFlow" => (float)(random.NextDouble() * 2000f - 500f), // Can be negative
                "EnergyEfficiency" => 10f + (float)(random.NextDouble() * 8f),
                _ => (float)(random.NextDouble() * 100f)
            };
        }

        private List<string> GetPlaceholderFacilities()
        {
            return new List<string>
            {
                "All Facilities",
                "Main Cannabis Facility",
                "Secondary Growing Facility",
                "Indoor Hydroponic Lab",
                "Outdoor Growing Plots",
                "Research & Development Lab"
            };
        }

        #endregion

        #region Configuration and State Management

        public void SetTimeRange(TimeRange timeRange)
        {
            if (_currentTimeRange != timeRange)
            {
                _currentTimeRange = timeRange;
                LogInfo($"Time range changed to: {timeRange}");
                
                // Refresh history data for new time range
                RefreshHistoryData();
            }
        }

        public void SetFacility(string facilityName)
        {
            if (_currentFacility != facilityName)
            {
                _currentFacility = facilityName;
                LogInfo($"Facility changed to: {facilityName}");
                
                // Refresh all data for new facility
                RefreshData();
            }
        }

        public void SetRefreshInterval(float intervalSeconds)
        {
            _refreshInterval = Mathf.Clamp(intervalSeconds, 0.5f, 300f); // 0.5s to 5 minutes
            LogInfo($"Refresh interval set to {_refreshInterval} seconds");
        }

        public void SetAutoRefresh(bool enabled)
        {
            _enableAutoRefresh = enabled;
            LogInfo($"Auto refresh {(enabled ? "enabled" : "disabled")}");
        }

        #endregion

        #region Public API

        public void UpdateAnalyticsService(IAnalyticsService newService)
        {
            _analyticsService = newService;
            _isServiceAvailable = newService != null;
            
            OnServiceAvailabilityChanged?.Invoke(_isServiceAvailable);
            
            if (_isServiceAvailable)
            {
                RefreshData();
                LogInfo("Analytics service updated successfully");
            }
        }

        public float GetMetricValue(string metricKey)
        {
            return _cachedMetrics.TryGetValue(metricKey, out var value) ? value : 0f;
        }

        public List<MetricDataPoint> GetMetricHistory(string metricKey)
        {
            return _cachedHistoryData.TryGetValue(metricKey, out var history) 
                ? new List<MetricDataPoint>(history) 
                : new List<MetricDataPoint>();
        }

        public bool HasMetricData(string metricKey)
        {
            return _cachedMetrics.ContainsKey(metricKey);
        }

        public bool HasHistoryData(string metricKey)
        {
            return _cachedHistoryData.ContainsKey(metricKey) && 
                   _cachedHistoryData[metricKey].Count > 0;
        }

        public Dictionary<string, object> GetDataStatus()
        {
            return new Dictionary<string, object>
            {
                ["IsServiceAvailable"] = _isServiceAvailable,
                ["IsRefreshing"] = _isRefreshing,
                ["LastRefreshTime"] = _lastRefreshTime,
                ["CachedMetricsCount"] = _cachedMetrics.Count,
                ["CachedHistoryCount"] = _cachedHistoryData.Count,
                ["CurrentTimeRange"] = _currentTimeRange,
                ["CurrentFacility"] = _currentFacility,
                ["RefreshInterval"] = _refreshInterval,
                ["AutoRefreshEnabled"] = _enableAutoRefresh
            };
        }

        #endregion

        #region Helper Methods

        private List<string> GetKnownMetricKeys()
        {
            return new List<string>
            {
                "YieldPerHour",
                "ActivePlants",
                "CashBalance",
                "EnergyUsage",
                "PlantHealth",
                "FacilityUtilization",
                "NetCashFlow",
                "EnergyEfficiency"
            };
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[DataDashboardData] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[DataDashboardData] {message}");
        }
    }
}