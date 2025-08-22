using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Cultivation;
using ProjectChimera.Systems.Economy;
using ProjectChimera.Systems.Environment;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Lightweight analytics system for tracking and aggregating basic KPIs
    /// Phase 9 implementation focusing on yield/time, cash flow, and energy metrics
    /// </summary>
    public class AnalyticsManager : DIChimeraManager, IAnalyticsService
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

        // Core metrics storage
        private Dictionary<string, List<MetricDataPoint>> _metricHistory;
        private Dictionary<string, float> _currentMetrics;
        private Dictionary<string, IMetricCollector> _metricCollectors;

        // Facility-specific metrics storage
        private Dictionary<string, Dictionary<string, List<MetricDataPoint>>> _facilityMetricHistory;
        private Dictionary<string, Dictionary<string, float>> _facilityCurrentMetrics;
        private List<string> _availableFacilities;
        private string _currentFacilityFilter = "All Facilities";

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
                var serviceLocator = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance;
                return serviceLocator?.GetService<IAnalyticsService>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AnalyticsManager] Failed to get service from DI container: {ex.Message}");
                return UnityEngine.Object.FindObjectOfType<AnalyticsManager>();
            }
        }

        #region Service Interface Implementation

        public float GetMetric(string metricName)
        {
            if (!IsEnabled || !_currentMetrics.ContainsKey(metricName))
                return 0f;

            return _currentMetrics[metricName];
        }

        public List<MetricDataPoint> GetMetricHistory(string metricName, TimeRange timeRange)
        {
            if (!IsEnabled || !_metricHistory.ContainsKey(metricName))
                return new List<MetricDataPoint>();

            var cutoffTime = DateTime.Now - GetTimeSpanFromRange(timeRange);
            var filteredData = _metricHistory[metricName]
                .Where(point => point.Timestamp >= cutoffTime)
                .OrderBy(point => point.Timestamp)
                .ToList();

            // Apply data aggregation for large time ranges to improve performance
            return AggregateDataPoints(filteredData, timeRange);
        }

        public Dictionary<string, float> GetCurrentMetrics()
        {
            return IsEnabled ? new Dictionary<string, float>(_currentMetrics) : new Dictionary<string, float>();
        }

        public void RegisterMetricCollector(string metricName, IMetricCollector collector)
        {
            if (!IsEnabled) return;

            _metricCollectors[metricName] = collector;
            
            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsManager] Registered collector for metric: {metricName}");
        }

        public void UnregisterMetricCollector(string metricName)
        {
            if (!IsEnabled) return;

            _metricCollectors.Remove(metricName);
            
            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsManager] Unregistered collector for metric: {metricName}");
        }

        public void RecordMetric(string metricName, float value)
        {
            if (!IsEnabled) return;

            _currentMetrics[metricName] = value;
            
            if (!_metricHistory.ContainsKey(metricName))
                _metricHistory[metricName] = new List<MetricDataPoint>();

            var dataPoint = new MetricDataPoint
            {
                Value = value,
                Timestamp = DateTime.Now,
                MetricName = metricName
            };

            _metricHistory[metricName].Add(dataPoint);

            // Limit history size
            if (_metricHistory[metricName].Count > _maxDataPoints)
            {
                _metricHistory[metricName].RemoveAt(0);
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
        }

        private void Update()
        {
            if (!IsEnabled) return;

            if (Time.time >= _lastUpdateTime + _updateInterval)
            {
                UpdateMetrics();
                _lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region DIChimeraManager Implementation

        protected override void RegisterSelfWithServiceLocator()
        {
            base.RegisterSelfWithServiceLocator();
            
            // Ensure IAnalyticsService interface is explicitly registered
            try
            {
                ServiceLocator.RegisterSingleton<IAnalyticsService>(this);
                
                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsManager] Successfully registered IAnalyticsService interface with DI container");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Failed to register IAnalyticsService interface: {ex.Message}");
            }
        }

        #endregion

        #region ChimeraManager Implementation

        protected override void OnManagerInitialize()
        {
            InitializeAnalytics();
        }

        protected override void OnManagerShutdown()
        {
            if (_metricHistory != null)
            {
                _metricHistory.Clear();
            }
            
            if (_currentMetrics != null)
            {
                _currentMetrics.Clear();
            }
            
            if (_metricCollectors != null)
            {
                _metricCollectors.Clear();
            }

            _isInitialized = false;

            if (_enableDebugLogging)
                Debug.Log("[AnalyticsManager] Analytics system shutdown complete");
        }

        #endregion

        #region Initialization

        private void InitializeAnalytics()
        {
            try
            {
                _metricHistory = new Dictionary<string, List<MetricDataPoint>>();
                _currentMetrics = new Dictionary<string, float>();
                _metricCollectors = new Dictionary<string, IMetricCollector>();

                // Initialize facility-specific storage
                _facilityMetricHistory = new Dictionary<string, Dictionary<string, List<MetricDataPoint>>>();
                _facilityCurrentMetrics = new Dictionary<string, Dictionary<string, float>>();
                _availableFacilities = new List<string>();

                InitializeFacilities();
                InitializeDefaultMetrics();
                InitializeMetricCollectors();
                
                _isInitialized = true;
                _lastUpdateTime = Time.time;

                if (_enableDebugLogging)
                    Debug.Log("[AnalyticsManager] Analytics system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Failed to initialize analytics: {ex.Message}");
                _enableAnalytics = false;
            }
        }

        private void InitializeMetricCollectors()
        {
            try
            {
                // Find required managers
                var cultivationManager = UnityEngine.Object.FindObjectOfType<CultivationManager>();
                var currencyManager = UnityEngine.Object.FindObjectOfType<CurrencyManager>();
                var environmentManager = UnityEngine.Object.FindObjectOfType<EnvironmentManager>();

                // Initialize yield/time collectors if cultivation system is available
                if (_trackYieldMetrics && cultivationManager != null)
                {
                    RegisterMetricCollector("YieldPerHour", new YieldPerHourCollector(cultivationManager));
                    RegisterMetricCollector("ActivePlants", new ActivePlantsCollector(cultivationManager));
                    RegisterMetricCollector("PlantHealth", new PlantHealthCollector(cultivationManager));
                    RegisterMetricCollector("TotalHarvested", new TotalHarvestedCollector(cultivationManager));
                    RegisterMetricCollector("FacilityUtilization", new FacilityUtilizationCollector(cultivationManager));
                    RegisterMetricCollector("OperationalEfficiency", new OperationalEfficiencyCollector(cultivationManager, currencyManager));
                    
                    if (_enableDebugLogging)
                        Debug.Log("[AnalyticsManager] Yield/time metric collectors initialized");
                }

                // Initialize cash flow collectors if currency system is available
                if (_trackCashFlow && currencyManager != null)
                {
                    RegisterMetricCollector("CashBalance", new CashBalanceCollector(currencyManager));
                    RegisterMetricCollector("TotalRevenue", new TotalRevenueCollector(currencyManager));
                    RegisterMetricCollector("TotalExpenses", new TotalExpensesCollector(currencyManager));
                    RegisterMetricCollector("NetCashFlow", new NetCashFlowCollector(currencyManager));
                    
                    if (_enableDebugLogging)
                        Debug.Log("[AnalyticsManager] Cash flow metric collectors initialized");
                }

                // Initialize energy collectors (enhanced implementation)
                if (_trackEnergyUsage)
                {
                    var energyCollector = new EnergyUsageCollector(environmentManager);
                    RegisterMetricCollector("EnergyUsage", energyCollector);
                    RegisterMetricCollector("TotalEnergyConsumed", new TotalEnergyConsumedCollector());
                    RegisterMetricCollector("EnergyDailyCost", new EnergyDailyCostCollector(0.12f)); // $0.12/kWh default rate
                    
                    if (cultivationManager != null)
                    {
                        RegisterMetricCollector("EnergyEfficiency", new EnergyEfficiencyCollector(cultivationManager, energyCollector));
                    }
                    
                    if (_enableDebugLogging)
                        Debug.Log("[AnalyticsManager] Enhanced energy metric collectors initialized");
                }

                if (_enableDebugLogging)
                    Debug.Log($"[AnalyticsManager] Initialized {_metricCollectors.Count} metric collectors");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Failed to initialize metric collectors: {ex.Message}");
            }
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

        #endregion

        #region Metric Collection

        private void UpdateMetrics()
        {
            try
            {
                foreach (var collector in _metricCollectors)
                {
                    var value = collector.Value.CollectMetric();
                    RecordMetric(collector.Key, value);
                }

                // Update derived metrics
                UpdateDerivedMetrics();
                
                // Update facility-specific metrics
                if (_trackFacilityMetrics)
                {
                    UpdateFacilityMetrics();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Error updating metrics: {ex.Message}");
            }
        }

        private void UpdateDerivedMetrics()
        {
            // Calculate net cash flow
            var revenue = GetMetric("TotalRevenue");
            var expenses = GetMetric("TotalExpenses");
            RecordMetric("NetCashFlow", revenue - expenses);

            // Calculate facility efficiency
            var activePlants = GetMetric("ActivePlants");
            var maxPlants = GetMaxPlantsCapacity();
            if (maxPlants > 0)
            {
                RecordMetric("FacilityUtilization", (activePlants / maxPlants) * 100f);
            }
        }

        private float GetMaxPlantsCapacity()
        {
            // Stub implementation - would integrate with facility manager
            return 100f; // Default max capacity
        }

        #endregion

        #region Utility Methods

        private TimeSpan GetTimeSpanFromRange(TimeRange timeRange)
        {
            return timeRange switch
            {
                TimeRange.LastHour => TimeSpan.FromHours(1),
                TimeRange.Last6Hours => TimeSpan.FromHours(6),
                TimeRange.Last12Hours => TimeSpan.FromHours(12),
                TimeRange.Last24Hours => TimeSpan.FromDays(1),
                TimeRange.Last3Days => TimeSpan.FromDays(3),
                TimeRange.LastWeek => TimeSpan.FromDays(7),
                TimeRange.Last2Weeks => TimeSpan.FromDays(14),
                TimeRange.LastMonth => TimeSpan.FromDays(30),
                TimeRange.Last3Months => TimeSpan.FromDays(90),
                TimeRange.Last6Months => TimeSpan.FromDays(180),
                TimeRange.LastYear => TimeSpan.FromDays(365),
                TimeRange.AllTime => TimeSpan.FromDays(3650), // 10 years as "all time"
                _ => TimeSpan.FromDays(1)
            };
        }

        private List<MetricDataPoint> AggregateDataPoints(List<MetricDataPoint> dataPoints, TimeRange timeRange)
        {
            if (dataPoints.Count == 0) return dataPoints;

            // Determine aggregation interval based on time range
            var aggregationInterval = GetAggregationInterval(timeRange);
            
            if (aggregationInterval == TimeSpan.Zero || dataPoints.Count <= 100)
            {
                // No aggregation needed for short ranges or small datasets
                return dataPoints;
            }

            // Group data points by aggregation interval and average values
            var aggregatedData = new List<MetricDataPoint>();
            var currentBucketStart = dataPoints.First().Timestamp;
            var currentBucketEnd = currentBucketStart.Add(aggregationInterval);
            var currentBucketValues = new List<float>();
            
            foreach (var point in dataPoints)
            {
                if (point.Timestamp >= currentBucketEnd)
                {
                    // Finalize current bucket
                    if (currentBucketValues.Count > 0)
                    {
                        aggregatedData.Add(new MetricDataPoint
                        {
                            MetricName = point.MetricName,
                            Value = currentBucketValues.Average(),
                            Timestamp = currentBucketStart.AddMilliseconds(aggregationInterval.TotalMilliseconds / 2) // Midpoint
                        });
                    }
                    
                    // Start new bucket
                    currentBucketStart = currentBucketEnd;
                    currentBucketEnd = currentBucketStart.Add(aggregationInterval);
                    currentBucketValues.Clear();
                }
                
                currentBucketValues.Add(point.Value);
            }
            
            // Finalize last bucket
            if (currentBucketValues.Count > 0)
            {
                aggregatedData.Add(new MetricDataPoint
                {
                    MetricName = dataPoints.First().MetricName,
                    Value = currentBucketValues.Average(),
                    Timestamp = currentBucketStart.AddMilliseconds(aggregationInterval.TotalMilliseconds / 2)
                });
            }

            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsManager] Aggregated {dataPoints.Count} points to {aggregatedData.Count} for range {timeRange}");
            
            return aggregatedData;
        }

        private TimeSpan GetAggregationInterval(TimeRange timeRange)
        {
            return timeRange switch
            {
                // Short ranges - no aggregation
                TimeRange.LastHour => TimeSpan.Zero,
                TimeRange.Last6Hours => TimeSpan.Zero,
                TimeRange.Last12Hours => TimeSpan.FromMinutes(5),
                
                // Medium ranges - aggregate by minutes/hours
                TimeRange.Last24Hours => TimeSpan.FromMinutes(15),
                TimeRange.Last3Days => TimeSpan.FromHours(1),
                TimeRange.LastWeek => TimeSpan.FromHours(2),
                TimeRange.Last2Weeks => TimeSpan.FromHours(4),
                
                // Long ranges - aggregate by hours/days
                TimeRange.LastMonth => TimeSpan.FromHours(8),
                TimeRange.Last3Months => TimeSpan.FromDays(1),
                TimeRange.Last6Months => TimeSpan.FromDays(2),
                TimeRange.LastYear => TimeSpan.FromDays(7),
                TimeRange.AllTime => TimeSpan.FromDays(30),
                
                _ => TimeSpan.Zero
            };
        }

        #endregion

        #region Facility Filtering

        private void InitializeFacilities()
        {
            // Initialize with default facilities - in a real implementation, 
            // this would discover facilities from a FacilityManager or similar system
            _availableFacilities.Clear();
            _availableFacilities.Add("All Facilities");
            _availableFacilities.Add("Main Facility");
            _availableFacilities.Add("Greenhouse A");
            _availableFacilities.Add("Greenhouse B");
            _availableFacilities.Add("Processing Wing");
            
            // Initialize storage for each facility
            foreach (var facility in _availableFacilities)
            {
                if (facility != "All Facilities")
                {
                    _facilityMetricHistory[facility] = new Dictionary<string, List<MetricDataPoint>>();
                    _facilityCurrentMetrics[facility] = new Dictionary<string, float>();
                }
            }

            if (_enableDebugLogging)
                Debug.Log($"[AnalyticsManager] Initialized {_availableFacilities.Count} facilities for filtering");
        }

        public List<string> GetAvailableFacilities()
        {
            return new List<string>(_availableFacilities);
        }

        public void SetFacilityFilter(string facilityName)
        {
            if (_availableFacilities.Contains(facilityName))
            {
                _currentFacilityFilter = facilityName;
                
                if (_enableDebugLogging)
                    Debug.Log($"[AnalyticsManager] Facility filter set to: {facilityName}");
            }
            else
            {
                Debug.LogWarning($"[AnalyticsManager] Unknown facility: {facilityName}");
            }
        }

        public string GetCurrentFacilityFilter()
        {
            return _currentFacilityFilter;
        }

        public Dictionary<string, float> GetCurrentMetrics(string facilityName)
        {
            if (facilityName == "All Facilities" || string.IsNullOrEmpty(facilityName))
            {
                return GetCurrentMetrics(); // Return aggregated metrics
            }

            if (_facilityCurrentMetrics.ContainsKey(facilityName))
            {
                return new Dictionary<string, float>(_facilityCurrentMetrics[facilityName]);
            }

            return new Dictionary<string, float>();
        }

        public List<MetricDataPoint> GetMetricHistory(string metricName, TimeRange timeRange, string facilityName)
        {
            if (facilityName == "All Facilities" || string.IsNullOrEmpty(facilityName))
            {
                return GetMetricHistory(metricName, timeRange); // Return aggregated history
            }

            if (!_facilityMetricHistory.ContainsKey(facilityName) || 
                !_facilityMetricHistory[facilityName].ContainsKey(metricName))
            {
                return new List<MetricDataPoint>();
            }

            var cutoffTime = DateTime.Now - GetTimeSpanFromRange(timeRange);
            var filteredData = _facilityMetricHistory[facilityName][metricName]
                .Where(point => point.Timestamp >= cutoffTime)
                .OrderBy(point => point.Timestamp)
                .ToList();

            // Apply data aggregation for large time ranges
            return AggregateDataPoints(filteredData, timeRange);
        }

        public void RecordMetricForFacility(string facilityName, string metricName, float value)
        {
            if (!IsEnabled || facilityName == "All Facilities") return;

            // Ensure facility exists
            if (!_facilityCurrentMetrics.ContainsKey(facilityName))
            {
                _facilityCurrentMetrics[facilityName] = new Dictionary<string, float>();
                _facilityMetricHistory[facilityName] = new Dictionary<string, List<MetricDataPoint>>();
            }

            // Record for specific facility
            _facilityCurrentMetrics[facilityName][metricName] = value;
            
            if (!_facilityMetricHistory[facilityName].ContainsKey(metricName))
                _facilityMetricHistory[facilityName][metricName] = new List<MetricDataPoint>();

            var dataPoint = new MetricDataPoint
            {
                MetricName = metricName,
                Value = value,
                Timestamp = DateTime.Now
            };

            _facilityMetricHistory[facilityName][metricName].Add(dataPoint);

            // Limit data points per facility
            if (_facilityMetricHistory[facilityName][metricName].Count > _maxDataPoints)
            {
                _facilityMetricHistory[facilityName][metricName].RemoveAt(0);
            }

            // Also update aggregate metrics
            RecordMetric(metricName, value);
        }

        private void UpdateFacilityMetrics()
        {
            // In a real implementation, this would collect metrics from each facility
            // For now, generate some sample facility-specific data
            
            foreach (var facility in _availableFacilities)
            {
                if (facility == "All Facilities") continue;

                // Generate facility-specific variations of metrics
                float facilityMultiplier = GetFacilityMultiplier(facility);
                
                foreach (var collector in _metricCollectors)
                {
                    try
                    {
                        float baseValue = collector.Value.CollectMetric();
                        float facilityValue = baseValue * facilityMultiplier;
                        
                        RecordMetricForFacility(facility, collector.Key, facilityValue);
                    }
                    catch (Exception ex)
                    {
                        if (_enableDebugLogging)
                            Debug.LogWarning($"[AnalyticsManager] Failed to collect {collector.Key} for {facility}: {ex.Message}");
                    }
                }
            }
        }

        private float GetFacilityMultiplier(string facilityName)
        {
            // Simple facility-specific multipliers for demo purposes
            return facilityName switch
            {
                "Main Facility" => 1.2f,
                "Greenhouse A" => 0.9f,
                "Greenhouse B" => 1.1f,
                "Processing Wing" => 0.7f,
                _ => 1.0f
            };
        }

        #endregion

        #region Data Aggregation and Calculation Logic

        /// <summary>
        /// Aggregates metric data over a specified time range using different aggregation methods
        /// </summary>
        public AggregatedMetricData GetAggregatedMetric(string metricName, TimeRange timeRange, AggregationType aggregationType, string facilityName = null)
        {
            try
            {
                var dataPoints = GetFilteredDataPoints(metricName, timeRange, facilityName);
                if (!dataPoints.Any())
                {
                    return new AggregatedMetricData
                    {
                        MetricName = metricName,
                        TimeRange = timeRange,
                        AggregationType = aggregationType,
                        Value = 0f,
                        DataPointCount = 0,
                        IsValid = false
                    };
                }

                var aggregatedValue = CalculateAggregation(dataPoints, aggregationType);
                var trend = CalculateTrend(dataPoints);
                var statistics = CalculateStatistics(dataPoints);

                return new AggregatedMetricData
                {
                    MetricName = metricName,
                    TimeRange = timeRange,
                    AggregationType = aggregationType,
                    Value = aggregatedValue,
                    DataPointCount = dataPoints.Count,
                    IsValid = true,
                    Trend = trend,
                    Statistics = statistics,
                    FacilityName = facilityName
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Error aggregating metric '{metricName}': {ex.Message}");
                return new AggregatedMetricData { MetricName = metricName, IsValid = false };
            }
        }

        /// <summary>
        /// Gets multiple aggregated metrics in a single call for dashboard efficiency
        /// </summary>
        public Dictionary<string, AggregatedMetricData> GetAggregatedMetrics(List<string> metricNames, TimeRange timeRange, AggregationType aggregationType, string facilityName = null)
        {
            var results = new Dictionary<string, AggregatedMetricData>();
            
            foreach (var metricName in metricNames)
            {
                results[metricName] = GetAggregatedMetric(metricName, timeRange, aggregationType, facilityName);
            }

            return results;
        }

        /// <summary>
        /// Calculates KPI summary data for dashboard cards
        /// </summary>
        public KPISummary GetKPISummary(string metricName, TimeRange timeRange, string facilityName = null)
        {
            try
            {
                var currentValue = GetAggregatedMetric(metricName, TimeRange.LastHour, AggregationType.Average, facilityName);
                var previousValue = GetAggregatedMetric(metricName, GetPreviousTimeRange(timeRange), AggregationType.Average, facilityName);
                var trend = CalculateKPITrend(currentValue.Value, previousValue.Value);
                var sparklineData = GetSparklineData(metricName, timeRange, facilityName);

                return new KPISummary
                {
                    MetricName = metricName,
                    CurrentValue = currentValue.Value,
                    PreviousValue = previousValue.Value,
                    PercentageChange = trend.PercentageChange,
                    TrendDirection = trend.Direction,
                    TrendIndicator = trend.Indicator,
                    SparklineData = sparklineData,
                    Unit = GetMetricUnit(metricName),
                    DisplayName = GetMetricDisplayName(metricName),
                    IsValid = currentValue.IsValid
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsManager] Error calculating KPI summary for '{metricName}': {ex.Message}");
                return new KPISummary { MetricName = metricName, IsValid = false };
            }
        }

        /// <summary>
        /// Gets sparkline data for KPI cards - simplified trending visualization
        /// </summary>
        public List<float> GetSparklineData(string metricName, TimeRange timeRange, string facilityName = null, int maxPoints = 20)
        {
            var dataPoints = GetFilteredDataPoints(metricName, timeRange, facilityName);
            if (!dataPoints.Any()) return new List<float>();

            // Downsample data to maxPoints for sparkline visualization
            var sampledData = SampleDataPoints(dataPoints, maxPoints);
            return sampledData.Select(dp => dp.Value).ToList();
        }

        #region Private Aggregation Helpers

        private List<MetricDataPoint> GetFilteredDataPoints(string metricName, TimeRange timeRange, string facilityName)
        {
            var allDataPoints = new List<MetricDataPoint>();
            var cutoffTime = GetTimeRangeCutoff(timeRange);

            if (string.IsNullOrEmpty(facilityName) || facilityName == "All Facilities")
            {
                // Global metrics
                if (_metricHistory.ContainsKey(metricName))
                {
                    allDataPoints.AddRange(_metricHistory[metricName].Where(dp => dp.Timestamp >= cutoffTime));
                }
            }
            else
            {
                // Facility-specific metrics
                if (_facilityMetricHistory.ContainsKey(facilityName) && 
                    _facilityMetricHistory[facilityName].ContainsKey(metricName))
                {
                    allDataPoints.AddRange(_facilityMetricHistory[facilityName][metricName].Where(dp => dp.Timestamp >= cutoffTime));
                }
            }

            return allDataPoints.OrderBy(dp => dp.Timestamp).ToList();
        }

        private float CalculateAggregation(List<MetricDataPoint> dataPoints, AggregationType aggregationType)
        {
            if (!dataPoints.Any()) return 0f;

            return aggregationType switch
            {
                AggregationType.Sum => dataPoints.Sum(dp => dp.Value),
                AggregationType.Average => dataPoints.Average(dp => dp.Value),
                AggregationType.Minimum => dataPoints.Min(dp => dp.Value),
                AggregationType.Maximum => dataPoints.Max(dp => dp.Value),
                AggregationType.Latest => dataPoints.Last().Value,
                AggregationType.First => dataPoints.First().Value,
                AggregationType.Count => dataPoints.Count,
                AggregationType.Median => CalculateMedian(dataPoints.Select(dp => dp.Value).ToList()),
                AggregationType.StandardDeviation => CalculateStandardDeviation(dataPoints.Select(dp => dp.Value).ToList()),
                AggregationType.Range => dataPoints.Max(dp => dp.Value) - dataPoints.Min(dp => dp.Value),
                _ => dataPoints.Average(dp => dp.Value)
            };
        }

        private TrendData CalculateTrend(List<MetricDataPoint> dataPoints)
        {
            if (dataPoints.Count < 2)
            {
                return new TrendData { Direction = TrendDirection.Stable, PercentageChange = 0f };
            }

            var firstValue = dataPoints.First().Value;
            var lastValue = dataPoints.Last().Value;
            var percentageChange = firstValue != 0 ? ((lastValue - firstValue) / firstValue) * 100f : 0f;

            return new TrendData
            {
                Direction = percentageChange > 5f ? TrendDirection.Increasing :
                           percentageChange < -5f ? TrendDirection.Decreasing : TrendDirection.Stable,
                PercentageChange = percentageChange,
                Slope = CalculateLinearTrendSlope(dataPoints)
            };
        }

        private MetricStatistics CalculateStatistics(List<MetricDataPoint> dataPoints)
        {
            if (!dataPoints.Any())
            {
                return new MetricStatistics();
            }

            var values = dataPoints.Select(dp => dp.Value).ToList();
            var mean = values.Average();

            return new MetricStatistics
            {
                Mean = mean,
                Median = CalculateMedian(values),
                StandardDeviation = CalculateStandardDeviation(values),
                Minimum = values.Min(),
                Maximum = values.Max(),
                Range = values.Max() - values.Min(),
                Count = values.Count,
                Sum = values.Sum()
            };
        }

        private float CalculateMedian(List<float> values)
        {
            if (!values.Any()) return 0f;
            
            var sorted = values.OrderBy(x => x).ToList();
            var count = sorted.Count;
            
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2f;
            }
            else
            {
                return sorted[count / 2];
            }
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            if (values.Count < 2) return 0f;
            
            var mean = values.Average();
            var sumOfSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));
            return (float)Math.Sqrt(sumOfSquaredDifferences / (values.Count - 1));
        }

        private float CalculateLinearTrendSlope(List<MetricDataPoint> dataPoints)
        {
            if (dataPoints.Count < 2) return 0f;

            var n = dataPoints.Count;
            var sumX = 0f;
            var sumY = 0f;
            var sumXY = 0f;
            var sumX2 = 0f;

            for (int i = 0; i < n; i++)
            {
                var x = i; // Time index
                var y = dataPoints[i].Value;
                
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }

        private KPITrendData CalculateKPITrend(float currentValue, float previousValue)
        {
            if (previousValue == 0f)
            {
                return new KPITrendData
                {
                    Direction = TrendDirection.Stable,
                    PercentageChange = 0f,
                    Indicator = "stable"
                };
            }

            var percentageChange = ((currentValue - previousValue) / previousValue) * 100f;
            var direction = Math.Abs(percentageChange) < 0.1f ? TrendDirection.Stable :
                           percentageChange > 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;

            return new KPITrendData
            {
                Direction = direction,
                PercentageChange = percentageChange,
                Indicator = direction == TrendDirection.Increasing ? "up" :
                           direction == TrendDirection.Decreasing ? "down" : "stable"
            };
        }

        private List<MetricDataPoint> SampleDataPoints(List<MetricDataPoint> dataPoints, int targetCount)
        {
            if (dataPoints.Count <= targetCount) return dataPoints;

            var sampledPoints = new List<MetricDataPoint>();
            var step = (float)dataPoints.Count / targetCount;

            for (int i = 0; i < targetCount; i++)
            {
                var index = Mathf.RoundToInt(i * step);
                if (index < dataPoints.Count)
                {
                    sampledPoints.Add(dataPoints[index]);
                }
            }

            return sampledPoints;
        }

        private DateTime GetTimeRangeCutoff(TimeRange timeRange)
        {
            var now = DateTime.Now;
            return timeRange switch
            {
                TimeRange.LastHour => now.AddHours(-1),
                TimeRange.Last24Hours => now.AddDays(-1),
                TimeRange.LastWeek => now.AddDays(-7),
                TimeRange.LastMonth => now.AddMonths(-1),
                TimeRange.Last3Months => now.AddMonths(-3),
                TimeRange.LastYear => now.AddYears(-1),
                _ => now.AddDays(-1)
            };
        }

        private TimeRange GetPreviousTimeRange(TimeRange timeRange)
        {
            return timeRange switch
            {
                TimeRange.LastHour => TimeRange.LastHour, // Previous hour
                TimeRange.Last24Hours => TimeRange.Last24Hours, // Previous day
                TimeRange.LastWeek => TimeRange.LastWeek, // Previous week
                TimeRange.LastMonth => TimeRange.LastMonth, // Previous month
                _ => TimeRange.Last24Hours
            };
        }

        private string GetMetricUnit(string metricName)
        {
            return metricName switch
            {
                "TotalRevenue" or "TotalExpenses" or "DailyRevenue" or "CashBalance" => "$",
                "ActivePlants" or "SeedlingCount" or "VegetativeCount" => "plants",
                "AveragePlantHealth" or "HealthyPlantsRatio" or "FacilityUtilization" => "%",
                "TotalYieldHarvested" or "YieldPerHour" => "g",
                "EnergyConsumption" or "PowerUsage" => "kWh",
                _ => ""
            };
        }

        private string GetMetricDisplayName(string metricName)
        {
            return metricName switch
            {
                "TotalRevenue" => "Total Revenue",
                "TotalExpenses" => "Total Expenses", 
                "DailyRevenue" => "Daily Revenue",
                "CashBalance" => "Cash Balance",
                "ActivePlants" => "Active Plants",
                "AveragePlantHealth" => "Plant Health",
                "TotalYieldHarvested" => "Total Yield",
                "FacilityUtilization" => "Facility Usage",
                "EnergyConsumption" => "Energy Used",
                _ => metricName
            };
        }

        #endregion

        #endregion

    }

    #region Supporting Types

    /// <summary>
    /// Interface for analytics service functionality
    /// </summary>
    public interface IAnalyticsService
    {
        float GetMetric(string metricName);
        List<MetricDataPoint> GetMetricHistory(string metricName, TimeRange timeRange);
        Dictionary<string, float> GetCurrentMetrics();
        void RegisterMetricCollector(string metricName, IMetricCollector collector);
        void UnregisterMetricCollector(string metricName);
        void RecordMetric(string metricName, float value);
        
        // New aggregation methods
        AggregatedMetricData GetAggregatedMetric(string metricName, TimeRange timeRange, AggregationType aggregationType, string facilityName = null);
        Dictionary<string, AggregatedMetricData> GetAggregatedMetrics(List<string> metricNames, TimeRange timeRange, AggregationType aggregationType, string facilityName = null);
        KPISummary GetKPISummary(string metricName, TimeRange timeRange, string facilityName = null);
        List<float> GetSparklineData(string metricName, TimeRange timeRange, string facilityName = null, int maxPoints = 20);
    }

    /// <summary>
    /// Interface for metric collection from other systems
    /// </summary>
    public interface IMetricCollector
    {
        float CollectMetric();
    }

    /// <summary>
    /// Data point for metric storage
    /// </summary>
    [System.Serializable]
    public class MetricDataPoint
    {
        public string MetricName;
        public float Value;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Time range enumeration for filtering metrics
    /// </summary>
    public enum TimeRange
    {
        LastHour,
        Last6Hours,
        Last12Hours,
        Last24Hours,
        Last3Days,
        LastWeek,
        Last2Weeks,
        LastMonth,
        Last3Months,
        Last6Months,
        LastYear,
        AllTime
    }

    /// <summary>
    /// Enumeration of available aggregation types for metric calculations
    /// </summary>
    public enum AggregationType
    {
        Sum,
        Average,
        Minimum,
        Maximum,
        Latest,
        First,
        Count,
        Median,
        StandardDeviation,
        Range
    }

    /// <summary>
    /// Enumeration for trend direction indicators
    /// </summary>
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable
    }

    /// <summary>
    /// Data structure for aggregated metric results
    /// </summary>
    [System.Serializable]
    public class AggregatedMetricData
    {
        public string MetricName;
        public TimeRange TimeRange;
        public AggregationType AggregationType;
        public float Value;
        public int DataPointCount;
        public bool IsValid;
        public TrendData Trend;
        public MetricStatistics Statistics;
        public string FacilityName;
    }

    /// <summary>
    /// Data structure for trend analysis results
    /// </summary>
    [System.Serializable]
    public class TrendData
    {
        public TrendDirection Direction;
        public float PercentageChange;
        public float Slope;
    }

    /// <summary>
    /// Data structure for statistical analysis of metrics
    /// </summary>
    [System.Serializable]
    public class MetricStatistics
    {
        public float Mean;
        public float Median;
        public float StandardDeviation;
        public float Minimum;
        public float Maximum;
        public float Range;
        public int Count;
        public float Sum;
    }

    /// <summary>
    /// Data structure for KPI summary cards
    /// </summary>
    [System.Serializable]
    public class KPISummary
    {
        public string MetricName;
        public string DisplayName;
        public float CurrentValue;
        public float PreviousValue;
        public float PercentageChange;
        public TrendDirection TrendDirection;
        public string TrendIndicator;
        public List<float> SparklineData;
        public string Unit;
        public bool IsValid;
    }

    /// <summary>
    /// Data structure for KPI trend calculations
    /// </summary>
    [System.Serializable]
    public class KPITrendData
    {
        public TrendDirection Direction;
        public float PercentageChange;
        public string Indicator;
    }

    #endregion
}