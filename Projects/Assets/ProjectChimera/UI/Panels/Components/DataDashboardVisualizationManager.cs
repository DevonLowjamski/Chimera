using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.UI.Components;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles all visualization components for the analytics dashboard in Project Chimera's game.
    /// Manages KPI cards, charts, and visual data representation for cannabis cultivation metrics.
    /// </summary>
    public class DataDashboardVisualizationManager : MonoBehaviour
    {
        [Header("Visualization Configuration")]
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private bool _showTrendIndicators = true;
        [SerializeField] private int _maxDataPoints = 20;
        [SerializeField] private bool _enableDebugLogging = false;

        // UI Container references
        private VisualElement _kpiCardsContainer;
        private VisualElement _chartsContainer;

        // KPI Card Management
        private Dictionary<string, KPICard> _enhancedKpiCards;
        private Dictionary<string, VisualElement> _legacyKpiCards;
        private Dictionary<string, Label> _kpiValues;
        private Dictionary<string, Label> _kpiTrends;

        // Chart Management
        private Dictionary<string, ChartWidget> _chartWidgets;
        private LineChartWidget _yieldChart;
        private BarChartWidget _energyChart;
        private LineChartWidget _cashFlowChart;

        // Data Management
        private Dictionary<string, List<float>> _trendData;
        private Dictionary<string, float> _cachedMetrics;

        // Events
        public System.Action<string> OnKPICardInteraction;
        public System.Action<string, float> OnMetricUpdated;

        // Properties
        public int ActiveKPICount => _enhancedKpiCards?.Count ?? 0;
        public int ActiveChartCount => _chartWidgets?.Count ?? 0;
        public bool AnimationsEnabled => _enableAnimations;

        public void Initialize(VisualElement kpiContainer, VisualElement chartsContainer)
        {
            _kpiCardsContainer = kpiContainer;
            _chartsContainer = chartsContainer;

            InitializeDataStructures();
            CreateKPICards();
            CreateChartWidgets();

            LogInfo("Visualization manager initialized for game dashboard");
        }

        #region Data Structure Initialization

        private void InitializeDataStructures()
        {
            _enhancedKpiCards = new Dictionary<string, KPICard>();
            _legacyKpiCards = new Dictionary<string, VisualElement>();
            _kpiValues = new Dictionary<string, Label>();
            _kpiTrends = new Dictionary<string, Label>();
            _chartWidgets = new Dictionary<string, ChartWidget>();
            _trendData = new Dictionary<string, List<float>>();
            _cachedMetrics = new Dictionary<string, float>();
        }

        #endregion

        #region KPI Card Management

        public void CreateKPICards()
        {
            // Define game-specific KPI metrics for cannabis cultivation
            var kpiMetrics = new List<string>
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

            // Create enhanced KPI cards for game metrics
            foreach (var metricKey in kpiMetrics)
            {
                CreateEnhancedKPICard(metricKey);
                CreateLegacyKPICard(metricKey);
            }

            LogInfo($"Created {kpiMetrics.Count} KPI cards for game dashboard");
        }

        private void CreateEnhancedKPICard(string metricKey)
        {
            var enhancedCard = KPICardFactory.CreateStandardCard(metricKey, KPICard.KPISize.Medium);
            enhancedCard.CardClicked += OnKPICardClicked;
            
            _enhancedKpiCards[metricKey] = enhancedCard;
            _kpiCardsContainer.Add(enhancedCard);
        }

        private void CreateLegacyKPICard(string metricKey)
        {
            // Create legacy card for backward compatibility
            var card = new VisualElement();
            card.name = $"kpi-card-{metricKey}";
            card.AddToClassList("kpi-card");
            
            // Create card header
            var header = new Label(GetDisplayName(metricKey));
            header.AddToClassList("kpi-card-title");
            card.Add(header);

            // Create value display
            var valueLabel = new Label("--");
            valueLabel.AddToClassList("kpi-card-value");
            card.Add(valueLabel);

            // Create trend indicator if enabled
            if (_showTrendIndicators)
            {
                var trendLabel = new Label("--");
                trendLabel.AddToClassList("kpi-card-trend");
                card.Add(trendLabel);
                _kpiTrends[metricKey] = trendLabel;
            }

            // Store references
            _legacyKpiCards[metricKey] = card;
            _kpiValues[metricKey] = valueLabel;
        }

        private void OnKPICardClicked(KPICard.KPIData cardData)
        {
            if (_enableDebugLogging)
                LogInfo($"KPI Card clicked: {cardData.Title}");
            
            // Flash the card to show interaction
            var card = _enhancedKpiCards.Values.FirstOrDefault(c => c.Data.Title == cardData.Title);
            card?.Flash(Color.cyan, 0.3f);

            OnKPICardInteraction?.Invoke(cardData.Title);
        }

        public void UpdateKPICard(string metricKey, float value, List<MetricDataPoint> history = null)
        {
            try
            {
                // Update enhanced KPI card
                if (_enhancedKpiCards.ContainsKey(metricKey))
                {
                    var card = _enhancedKpiCards[metricKey];
                    card.SetMetricData(metricKey, value, history);
                }

                // Update legacy KPI card
                if (_kpiValues.ContainsKey(metricKey))
                {
                    string formattedValue = FormatMetricValue(metricKey, value);
                    _kpiValues[metricKey].text = formattedValue;
                }

                // Cache value and update trend data
                _cachedMetrics[metricKey] = value;
                UpdateTrendData(metricKey, value);

                OnMetricUpdated?.Invoke(metricKey, value);
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to update KPI card {metricKey}: {ex.Message}");
            }
        }

        private void UpdateTrendData(string metricKey, float value)
        {
            if (!_trendData.ContainsKey(metricKey))
                _trendData[metricKey] = new List<float>();
                
            _trendData[metricKey].Add(value);
            
            // Limit trend data points for game performance
            if (_trendData[metricKey].Count > _maxDataPoints)
                _trendData[metricKey].RemoveAt(0);
        }

        public void UpdateTrendIndicators()
        {
            foreach (var kpi in _kpiTrends.Keys.ToArray())
            {
                if (_trendData.ContainsKey(kpi) && _trendData[kpi].Count >= 2)
                {
                    var data = _trendData[kpi];
                    var current = data.Last();
                    var previous = data[data.Count - 2];
                    var change = current - previous;
                    var changePercent = previous != 0 ? (change / previous) * 100 : 0;

                    string trendText = change > 0 ? "↑" : change < 0 ? "↓" : "→";
                    string changeText = $"{trendText} {changePercent:F1}%";
                    
                    _kpiTrends[kpi].text = changeText;
                    
                    // Apply trend styling for game UI
                    ApplyTrendStyling(_kpiTrends[kpi], change);
                }
            }
        }

        private void ApplyTrendStyling(Label trendLabel, float change)
        {
            // Remove existing trend classes
            trendLabel.RemoveFromClassList("trend-up");
            trendLabel.RemoveFromClassList("trend-down");
            trendLabel.RemoveFromClassList("trend-neutral");
            
            // Apply appropriate class based on trend direction
            if (change > 0)
                trendLabel.AddToClassList("trend-up");
            else if (change < 0)
                trendLabel.AddToClassList("trend-down");
            else
                trendLabel.AddToClassList("trend-neutral");
        }

        #endregion

        #region Chart Management

        public void CreateChartWidgets()
        {
            CreateYieldChart();
            CreateEnergyChart();
            CreateCashFlowChart();
            
            // Add placeholder if no charts available
            if (_chartsContainer.childCount == 0)
            {
                var placeholder = new Label("Charts will display once cannabis cultivation data is available");
                placeholder.AddToClassList("charts-placeholder");
                _chartsContainer.Add(placeholder);
            }

            LogInfo("Chart widgets created for game analytics");
        }

        private void CreateYieldChart()
        {
            _yieldChart = ChartFactory.CreateLineChart("Yield Trend", Color.green);
            var yieldContainer = ChartFactory.CreateChartContainer("Cannabis Yield Over Time", _yieldChart);
            _chartsContainer.Add(yieldContainer);
            _chartWidgets["YieldPerHour"] = _yieldChart;
        }

        private void CreateEnergyChart()
        {
            _energyChart = ChartFactory.CreateBarChart("Energy Usage", Color.yellow);
            var energyContainer = ChartFactory.CreateChartContainer("Facility Energy Consumption", _energyChart);
            _chartsContainer.Add(energyContainer);
            _chartWidgets["EnergyUsage"] = _energyChart;
        }

        private void CreateCashFlowChart()
        {
            _cashFlowChart = ChartFactory.CreateLineChart("Cash Flow", Color.cyan);
            var cashContainer = ChartFactory.CreateChartContainer("Cannabis Business Cash Flow", _cashFlowChart);
            _chartsContainer.Add(cashContainer);
            _chartWidgets["NetCashFlow"] = _cashFlowChart;
        }

        public void UpdateChartData(string metricKey, List<MetricDataPoint> data)
        {
            if (!_chartWidgets.ContainsKey(metricKey)) return;

            try
            {
                var chartWidget = _chartWidgets[metricKey];
                
                if (data != null && data.Count > 0)
                {
                    chartWidget.SetData(data, GetChartTitle(metricKey), GetChartColor(metricKey));
                }
            }
            catch (System.Exception ex)
            {
                LogWarning($"Failed to update chart {metricKey}: {ex.Message}");
            }
        }

        #endregion

        #region Visual Formatting

        private string GetDisplayName(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => "Yield/Hour",
                "ActivePlants" => "Active Plants",
                "CashBalance" => "Cash Balance",
                "EnergyUsage" => "Energy Usage",
                "PlantHealth" => "Avg Health",
                "FacilityUtilization" => "Facility Use",
                "NetCashFlow" => "Net Cash Flow",
                "EnergyEfficiency" => "Energy Efficiency",
                _ => metricKey
            };
        }

        private string FormatMetricValue(string metricKey, float value)
        {
            return metricKey switch
            {
                "CashBalance" => $"${value:F0}",
                "NetCashFlow" => $"${value:F0}",
                "EnergyUsage" => $"{value:F1} kWh",
                "YieldPerHour" => $"{value:F2} g/hr",
                "PlantHealth" => $"{value:F1}%",
                "FacilityUtilization" => $"{value:F1}%",
                "EnergyEfficiency" => $"{value:F2} g/kWh",
                "ActivePlants" => $"{value:F0}",
                _ => $"{value:F2}"
            };
        }

        private string GetChartTitle(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => "Cannabis Yield Trend (g/hr)",
                "EnergyUsage" => "Facility Energy Usage (kWh)",
                "NetCashFlow" => "Business Cash Flow ($)",
                _ => metricKey
            };
        }

        private Color GetChartColor(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => Color.green,
                "EnergyUsage" => Color.yellow,
                "NetCashFlow" => Color.cyan,
                "ActivePlants" => Color.blue,
                "PlantHealth" => new Color(0.8f, 1f, 0.8f),
                _ => Color.white
            };
        }

        #endregion

        #region Animation Support

        public void SetAnimationsEnabled(bool enabled)
        {
            _enableAnimations = enabled;
            
            // Apply animation settings to all cards
            foreach (var card in _enhancedKpiCards.Values)
            {
                card.SetAnimationsEnabled(enabled);
            }
        }

        public void FlashKPICard(string metricKey, Color flashColor, float duration = 0.3f)
        {
            if (_enhancedKpiCards.ContainsKey(metricKey))
            {
                _enhancedKpiCards[metricKey].Flash(flashColor, duration);
            }
        }

        #endregion

        #region Data Access

        public Dictionary<string, float> GetCurrentMetrics()
        {
            return new Dictionary<string, float>(_cachedMetrics);
        }

        public List<float> GetTrendData(string metricKey)
        {
            return _trendData.ContainsKey(metricKey) 
                ? new List<float>(_trendData[metricKey]) 
                : new List<float>();
        }

        public void ClearCachedData()
        {
            _cachedMetrics.Clear();
            _trendData.Clear();
            
            LogInfo("Cached visualization data cleared");
        }

        #endregion

        #region Public API

        public void SetTrendIndicatorsEnabled(bool enabled)
        {
            _showTrendIndicators = enabled;
            
            // Show/hide trend indicators
            foreach (var trendLabel in _kpiTrends.Values)
            {
                trendLabel.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetMaxDataPoints(int maxPoints)
        {
            _maxDataPoints = Mathf.Clamp(maxPoints, 5, 100);
            
            // Trim existing trend data if necessary
            foreach (var trendData in _trendData.Values)
            {
                while (trendData.Count > _maxDataPoints)
                {
                    trendData.RemoveAt(0);
                }
            }
        }

        public bool HasMetricData(string metricKey)
        {
            return _cachedMetrics.ContainsKey(metricKey);
        }

        public float GetLastMetricValue(string metricKey)
        {
            return _cachedMetrics.TryGetValue(metricKey, out var value) ? value : 0f;
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[DataDashboardVisualization] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[DataDashboardVisualization] {message}");
        }
    }
}