using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ProjectChimera.UI.Core;
using ProjectChimera.UI.Panels.Components;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Lightweight orchestrator for the analytics dashboard in Project Chimera's cannabis cultivation game.
    /// Coordinates specialized components for visualization, configuration, performance, and data management.
    /// </summary>
    public class DataDashboardPanel : UIPanel
    {
        [Header("Dashboard Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Specialized component managers
        private DataDashboardVisualizationManager _visualizationManager;
        private DataDashboardConfigurationManager _configurationManager;
        private DataDashboardPerformanceManager _performanceManager;
        private DataDashboardDataManager _dataManager;

        // UI Container references
        private VisualElement _dashboardContainer;
        private VisualElement _kpiCardsContainer;
        private VisualElement _chartsContainer;
        private VisualElement _filtersContainer;
        private Label _titleLabel;

        // Panel state
        private bool _isPanelActive;

        // Properties for external access
        public bool IsRefreshing => _dataManager?.IsRefreshing ?? false;
        public float AverageUpdateTime => _performanceManager?.AverageUpdateTime ?? 0f;
        public bool IsServiceAvailable => _dataManager?.IsServiceAvailable ?? false;
        public int QueuedUpdatesCount => _performanceManager?.QueuedUpdatesCount ?? 0;

        #region UIPanel Lifecycle

        protected override void OnPanelInitialized()
        {
            base.OnPanelInitialized();
            InitializeDashboard();
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            _isPanelActive = true;
            RequestDataRefresh();
        }

        protected override void OnBeforeHide()
        {
            base.OnBeforeHide();
            _isPanelActive = false;
        }

        #endregion

        #region Component Initialization

        private void InitializeDashboard()
        {
            try
            {
                CreateDashboardLayout();
                InitializeComponents();
                SetupEventHandlers();
                
                LogInfo("Cannabis cultivation dashboard initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize dashboard: {ex.Message}");
            }
        }

        private void CreateDashboardLayout()
        {
            // Create main dashboard container
            _dashboardContainer = new VisualElement();
            _dashboardContainer.name = "dashboard-container";
            _dashboardContainer.AddToClassList("dashboard-main");
            _rootElement.Add(_dashboardContainer);

            // Create title section
            var titleSection = new VisualElement();
            titleSection.name = "title-section";
            titleSection.AddToClassList("dashboard-title-section");
            
            _titleLabel = new Label("Cannabis Analytics Dashboard");
            _titleLabel.AddToClassList("dashboard-title");
            _titleLabel.name = "dashboard-title-label";
            titleSection.Add(_titleLabel);
            
            _dashboardContainer.Add(titleSection);

            // Create filters container
            _filtersContainer = new VisualElement();
            _filtersContainer.name = "filters-container";
            _filtersContainer.AddToClassList("dashboard-filters");
            _dashboardContainer.Add(_filtersContainer);

            // Create KPI cards container
            _kpiCardsContainer = new VisualElement();
            _kpiCardsContainer.name = "kpi-cards-container";
            _kpiCardsContainer.AddToClassList("kpi-cards-grid");
            _dashboardContainer.Add(_kpiCardsContainer);

            // Create charts container
            _chartsContainer = new VisualElement();
            _chartsContainer.name = "charts-container";
            _chartsContainer.AddToClassList("charts-grid");
            _dashboardContainer.Add(_chartsContainer);
        }

        private void InitializeComponents()
        {
            // Initialize data manager first
            _dataManager = GetOrAddComponent<DataDashboardDataManager>();
            _dataManager.Initialize();

            // Initialize performance manager
            _performanceManager = GetOrAddComponent<DataDashboardPerformanceManager>();

            // Initialize visualization manager
            _visualizationManager = GetOrAddComponent<DataDashboardVisualizationManager>();
            _visualizationManager.Initialize(_kpiCardsContainer, _chartsContainer);

            // Initialize configuration manager
            _configurationManager = GetOrAddComponent<DataDashboardConfigurationManager>();
            _configurationManager.Initialize(_filtersContainer, _dataManager._analyticsService);
        }

        private void SetupEventHandlers()
        {
            // Data manager events
            if (_dataManager != null)
            {
                _dataManager.OnMetricsUpdated += HandleMetricsUpdated;
                _dataManager.OnHistoryDataUpdated += HandleHistoryDataUpdated;
                _dataManager.OnServiceAvailabilityChanged += HandleServiceAvailabilityChanged;
                _dataManager.OnDataError += HandleDataError;
            }

            // Configuration manager events
            if (_configurationManager != null)
            {
                _configurationManager.OnTimeRangeChanged += HandleTimeRangeChanged;
                _configurationManager.OnFacilityChanged += HandleFacilityChanged;
                _configurationManager.OnRefreshRequested += HandleRefreshRequested;
            }

            // Visualization manager events
            if (_visualizationManager != null)
            {
                _visualizationManager.OnKPICardInteraction += HandleKPICardInteraction;
                _visualizationManager.OnMetricUpdated += HandleMetricUpdated;
            }

            // Performance manager events
            if (_performanceManager != null)
            {
                _performanceManager.OnPerformanceMetricsUpdated += HandlePerformanceUpdated;
                _performanceManager.OnPerformanceWarning += HandlePerformanceWarning;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleMetricsUpdated(Dictionary<string, float> metrics)
        {
            // Queue visualization updates through performance manager
            foreach (var metric in metrics)
            {
                _performanceManager?.QueueUpdate(() => 
                {
                    _visualizationManager?.UpdateKPICard(metric.Key, metric.Value);
                });
            }

            UpdateDashboardTitle();
        }

        private void HandleHistoryDataUpdated(string metricKey, List<MetricDataPoint> historyData)
        {
            _performanceManager?.QueueUpdate(() => 
            {
                _visualizationManager?.UpdateChartData(metricKey, historyData);
            });
        }

        private void HandleServiceAvailabilityChanged(bool isAvailable)
        {
            LogInfo($"Analytics service availability changed: {isAvailable}");
        }

        private void HandleDataError(string errorMessage)
        {
            LogError($"Data error: {errorMessage}");
        }

        private void HandleTimeRangeChanged(TimeRange newTimeRange)
        {
            _dataManager?.SetTimeRange(newTimeRange);
            UpdateDashboardTitle();
        }

        private void HandleFacilityChanged(string facilityName)
        {
            _dataManager?.SetFacility(facilityName);
            UpdateDashboardTitle();
        }

        private void HandleRefreshRequested()
        {
            RequestDataRefresh();
        }

        private void HandleKPICardInteraction(string metricKey)
        {
            LogInfo($"KPI card interaction: {metricKey}");
        }

        private void HandleMetricUpdated(string metricKey, float value)
        {
            // Queue trend indicator update
            _performanceManager?.QueueUpdate(() => 
            {
                _visualizationManager?.UpdateTrendIndicators();
            });
        }

        private void HandlePerformanceUpdated(float averageUpdateTime)
        {
            // Optional: Could update performance indicators in UI
        }

        private void HandlePerformanceWarning(string warning)
        {
            LogWarning($"Performance warning: {warning}");
        }

        #endregion

        #region Dashboard Operations

        private void RequestDataRefresh()
        {
            _dataManager?.RefreshData();
        }

        private void UpdateDashboardTitle()
        {
            if (_titleLabel != null && _configurationManager != null)
            {
                _titleLabel.text = _configurationManager.GetDashboardTitle();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually set the analytics service for the dashboard
        /// </summary>
        public void SetAnalyticsService(IAnalyticsService analyticsService)
        {
            _dataManager?.UpdateAnalyticsService(analyticsService);
            _configurationManager?.UpdateAnalyticsService(analyticsService);
            
            LogInfo("Analytics service manually set for cannabis cultivation dashboard");
        }

        /// <summary>
        /// Set the refresh interval for automatic data updates
        /// </summary>
        public void SetRefreshInterval(float intervalSeconds)
        {
            _dataManager?.SetRefreshInterval(intervalSeconds);
        }

        /// <summary>
        /// Enable or disable automatic data refresh
        /// </summary>
        public void SetAutoRefresh(bool enabled)
        {
            _dataManager?.SetAutoRefresh(enabled);
        }

        /// <summary>
        /// Get current cached metrics from the dashboard
        /// </summary>
        public Dictionary<string, float> GetCurrentMetrics()
        {
            return _dataManager?.CurrentMetrics ?? new Dictionary<string, float>();
        }

        /// <summary>
        /// Get performance metrics for the dashboard update system
        /// </summary>
        public void GetPerformanceMetrics(out float averageUpdateTime, out int queuedUpdates, out int maxUpdatesPerFrame)
        {
            if (_performanceManager != null)
            {
                var metrics = _performanceManager.GetDetailedMetrics();
                averageUpdateTime = metrics.AverageUpdateTimeMs;
                queuedUpdates = metrics.QueuedUpdates;
                maxUpdatesPerFrame = metrics.MaxUpdatesPerFrame;
            }
            else
            {
                averageUpdateTime = 0f;
                queuedUpdates = 0;
                maxUpdatesPerFrame = 0;
            }
        }

        /// <summary>
        /// Set the facility filter for cannabis cultivation analytics
        /// </summary>
        public void SetFacilityFilter(string facilityName)
        {
            _configurationManager?.SetFacilityFilter(facilityName);
        }

        /// <summary>
        /// Set the time range filter for analytics data
        /// </summary>
        public void SetTimeRangeFilter(TimeRange timeRange)
        {
            _configurationManager?.SetTimeRangeFilter(timeRange);
        }

        /// <summary>
        /// Force immediate processing of all queued updates
        /// </summary>
        public void FlushUpdateQueue()
        {
            _performanceManager?.FlushUpdateQueue();
        }

        /// <summary>
        /// Apply a performance preset to optimize for different hardware
        /// </summary>
        public void ApplyPerformancePreset(DataDashboardPerformanceManager.PerformancePreset preset)
        {
            _performanceManager?.ApplyPerformancePreset(preset);
        }

        /// <summary>
        /// Refresh the list of available cannabis cultivation facilities
        /// </summary>
        public void RefreshFacilityList()
        {
            _configurationManager?.RefreshFacilityList();
        }

        /// <summary>
        /// Get comprehensive dashboard status information
        /// </summary>
        public Dictionary<string, object> GetDashboardStatus()
        {
            var status = new Dictionary<string, object>
            {
                ["IsPanelActive"] = _isPanelActive,
                ["ComponentsInitialized"] = _dataManager != null && _configurationManager != null && 
                                           _visualizationManager != null && _performanceManager != null
            };

            if (_dataManager != null)
            {
                var dataStatus = _dataManager.GetDataStatus();
                foreach (var kvp in dataStatus)
                {
                    status[$"Data_{kvp.Key}"] = kvp.Value;
                }
            }

            if (_performanceManager != null)
            {
                var perfMetrics = _performanceManager.GetDetailedMetrics();
                status["Performance_Score"] = perfMetrics.PerformanceScore;
                status["Performance_QueuedUpdates"] = perfMetrics.QueuedUpdates;
            }

            if (_configurationManager != null)
            {
                var config = _configurationManager.GetCurrentConfiguration();
                foreach (var kvp in config)
                {
                    status[$"Config_{kvp.Key}"] = kvp.Value;
                }
            }

            return status;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get or add component to this GameObject
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[DataDashboardPanel] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[DataDashboardPanel] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogging)
                Debug.LogError($"[DataDashboardPanel] {message}");
        }

        #endregion
    }
}