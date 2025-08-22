using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.UI.Components
{
    /// <summary>
    /// Enhanced KPI card component for displaying key performance indicators
    /// Features sparklines, trend indicators, status colors, and interactive elements
    /// </summary>
    public class KPICard : VisualElement
    {
        [System.Serializable]
        public class KPIData
        {
            public string Title;
            public float CurrentValue;
            public float PreviousValue;
            public List<float> HistoryValues = new List<float>();
            public string Unit = "";
            public KPIStatus Status = KPIStatus.Normal;
            public Color CardColor = Color.white;
            public bool ShowSparkline = true;
            public bool ShowTrend = true;
        }

        public enum KPIStatus
        {
            Excellent,
            Good,
            Normal,
            Warning,
            Critical
        }

        public enum KPISize
        {
            Small,   // 150x100
            Medium,  // 200x120
            Large    // 250x150
        }

        private KPIData _data;
        private KPISize _size;
        
        // UI Elements
        private Label _titleLabel;
        private Label _valueLabel;
        private Label _unitLabel;
        private Label _trendLabel;
        private VisualElement _statusIndicator;
        private SparkLineWidget _sparkline;
        private VisualElement _cardContainer;
        private VisualElement _header;
        private VisualElement _content;
        private VisualElement _footer;

        // Settings
        private bool _isInteractive = true;
        private bool _showStatusIndicator = true;
        private int _maxHistoryPoints = 20;

        public KPIData Data => _data;
        public bool IsInteractive { get => _isInteractive; set => _isInteractive = value; }

        public KPICard(string title = "", KPISize size = KPISize.Medium)
        {
            _data = new KPIData { Title = title };
            _size = size;
            
            InitializeCard();
            SetupLayout();
            ApplySize();
            UpdateDisplay();
        }

        #region Initialization

        private void InitializeCard()
        {
            this.AddToClassList("kpi-card");
            this.AddToClassList($"kpi-card-{_size.ToString().ToLower()}");
            
            // Enable interactions
            if (_isInteractive)
            {
                this.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
                this.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
                this.RegisterCallback<ClickEvent>(OnCardClick);
            }
        }

        private void SetupLayout()
        {
            // Main card container
            _cardContainer = new VisualElement();
            _cardContainer.AddToClassList("kpi-card-container");
            this.Add(_cardContainer);

            // Header section
            _header = new VisualElement();
            _header.AddToClassList("kpi-card-header");
            _cardContainer.Add(_header);

            // Title
            _titleLabel = new Label(_data.Title);
            _titleLabel.AddToClassList("kpi-card-title");
            _header.Add(_titleLabel);

            // Status indicator
            if (_showStatusIndicator)
            {
                _statusIndicator = new VisualElement();
                _statusIndicator.AddToClassList("kpi-status-indicator");
                _header.Add(_statusIndicator);
            }

            // Content section
            _content = new VisualElement();
            _content.AddToClassList("kpi-card-content");
            _cardContainer.Add(_content);

            // Value container
            var valueContainer = new VisualElement();
            valueContainer.AddToClassList("kpi-value-container");
            _content.Add(valueContainer);

            _valueLabel = new Label("--");
            _valueLabel.AddToClassList("kpi-card-value");
            valueContainer.Add(_valueLabel);

            _unitLabel = new Label("");
            _unitLabel.AddToClassList("kpi-card-unit");
            valueContainer.Add(_unitLabel);

            // Footer section
            _footer = new VisualElement();
            _footer.AddToClassList("kpi-card-footer");
            _cardContainer.Add(_footer);

            // Trend indicator
            if (_data.ShowTrend)
            {
                _trendLabel = new Label("--");
                _trendLabel.AddToClassList("kpi-card-trend");
                _footer.Add(_trendLabel);
            }

            // Sparkline
            if (_data.ShowSparkline)
            {
                _sparkline = new SparkLineWidget();
                _sparkline.AddToClassList("kpi-card-sparkline");
                _footer.Add(_sparkline);
            }
        }

        private void ApplySize()
        {
            var dimensions = GetDimensions(_size);
            this.style.width = dimensions.x;
            this.style.height = dimensions.y;

            if (_sparkline != null)
            {
                var sparklineSize = GetSparklineSize(_size);
                _sparkline.SetSize(sparklineSize);
            }
        }

        private Vector2 GetDimensions(KPISize size)
        {
            return size switch
            {
                KPISize.Small => new Vector2(150, 100),
                KPISize.Medium => new Vector2(200, 120),
                KPISize.Large => new Vector2(250, 150),
                _ => new Vector2(200, 120)
            };
        }

        private Vector2 GetSparklineSize(KPISize size)
        {
            return size switch
            {
                KPISize.Small => new Vector2(80, 20),
                KPISize.Medium => new Vector2(100, 25),
                KPISize.Large => new Vector2(120, 30),
                _ => new Vector2(100, 25)
            };
        }

        #endregion

        #region Data Updates

        public void UpdateData(KPIData newData)
        {
            _data = newData;
            UpdateDisplay();
        }

        public void UpdateValue(float value, string unit = null)
        {
            _data.PreviousValue = _data.CurrentValue;
            _data.CurrentValue = value;
            
            if (unit != null)
                _data.Unit = unit;

            // Add to history
            _data.HistoryValues.Add(value);
            if (_data.HistoryValues.Count > _maxHistoryPoints)
                _data.HistoryValues.RemoveAt(0);

            UpdateDisplay();
        }

        public void SetMetricData(string metricKey, float value, List<MetricDataPoint> history = null)
        {
            _data.Title = FormatMetricTitle(metricKey);
            _data.Unit = GetMetricUnit(metricKey);
            _data.CardColor = GetMetricColor(metricKey);
            
            UpdateValue(value);
            
            if (history != null && history.Count > 0)
            {
                _data.HistoryValues = history.Select(h => h.Value).TakeLast(_maxHistoryPoints).ToList();
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            // Update title
            if (_titleLabel != null)
                _titleLabel.text = _data.Title;

            // Update value
            if (_valueLabel != null)
                _valueLabel.text = FormatValue(_data.CurrentValue);

            // Update unit
            if (_unitLabel != null)
                _unitLabel.text = _data.Unit;

            // Update trend
            if (_trendLabel != null && _data.ShowTrend)
                UpdateTrendDisplay();

            // Update sparkline
            if (_sparkline != null && _data.ShowSparkline && _data.HistoryValues.Count > 1)
                _sparkline.SetData(_data.HistoryValues, _data.CardColor);

            // Update status indicator
            if (_statusIndicator != null)
                UpdateStatusIndicator();

            // Update card colors
            UpdateCardColors();
        }

        private void UpdateTrendDisplay()
        {
            if (_data.CurrentValue == _data.PreviousValue)
            {
                _trendLabel.text = "→ 0.0%";
                _trendLabel.RemoveFromClassList("trend-up");
                _trendLabel.RemoveFromClassList("trend-down");
                _trendLabel.AddToClassList("trend-neutral");
                return;
            }

            float change = _data.CurrentValue - _data.PreviousValue;
            float changePercent = _data.PreviousValue != 0 ? (change / _data.PreviousValue) * 100 : 0;

            string arrow = change > 0 ? "↑" : "↓";
            _trendLabel.text = $"{arrow} {Mathf.Abs(changePercent):F1}%";

            _trendLabel.RemoveFromClassList("trend-up");
            _trendLabel.RemoveFromClassList("trend-down");
            _trendLabel.RemoveFromClassList("trend-neutral");

            if (change > 0)
                _trendLabel.AddToClassList("trend-up");
            else if (change < 0)
                _trendLabel.AddToClassList("trend-down");
            else
                _trendLabel.AddToClassList("trend-neutral");
        }

        private void UpdateStatusIndicator()
        {
            if (_statusIndicator == null) return;

            _statusIndicator.RemoveFromClassList("status-excellent");
            _statusIndicator.RemoveFromClassList("status-good");
            _statusIndicator.RemoveFromClassList("status-normal");
            _statusIndicator.RemoveFromClassList("status-warning");
            _statusIndicator.RemoveFromClassList("status-critical");

            _statusIndicator.AddToClassList($"status-{_data.Status.ToString().ToLower()}");
        }

        private void UpdateCardColors()
        {
            // Apply subtle color accent based on card color through border properties
            var borderColor = new Color(_data.CardColor.r, _data.CardColor.g, _data.CardColor.b, 0.3f);
            this.style.borderTopColor = borderColor;
            this.style.borderBottomColor = borderColor;
            this.style.borderLeftColor = borderColor;
            this.style.borderRightColor = borderColor;
        }

        #endregion

        #region Event Handlers

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            if (!_isInteractive) return;
            
            this.AddToClassList("kpi-card-hover");
            
            // Use CSS transform for scaling via USS classes instead of direct style manipulation
            // The scaling effect is handled by the kpi-card-hover CSS class
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (!_isInteractive) return;
            
            this.RemoveFromClassList("kpi-card-hover");
        }

        private void OnCardClick(ClickEvent evt)
        {
            if (!_isInteractive) return;
            
            // Trigger click animation
            this.AddToClassList("kpi-card-clicked");
            
            // Remove after animation
            this.schedule.Execute(() => {
                this.RemoveFromClassList("kpi-card-clicked");
            }).StartingIn(150);

            // Notify listeners
            CardClicked?.Invoke(_data);
        }

        #endregion

        #region Utility Methods

        private string FormatValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "--";

            // Format based on magnitude
            if (Mathf.Abs(value) >= 1000000)
                return $"{value / 1000000:F1}M";
            else if (Mathf.Abs(value) >= 1000)
                return $"{value / 1000:F1}K";
            else if (Mathf.Abs(value) >= 100)
                return $"{value:F0}";
            else if (Mathf.Abs(value) >= 10)
                return $"{value:F1}";
            else
                return $"{value:F2}";
        }

        private string FormatMetricTitle(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => "Yield/Hour",
                "ActivePlants" => "Active Plants",
                "CashBalance" => "Cash Balance",
                "NetCashFlow" => "Net Cash Flow",
                "EnergyUsage" => "Energy Usage",
                "EnergyEfficiency" => "Energy Efficiency",
                "PlantHealth" => "Plant Health",
                "FacilityUtilization" => "Facility Use",
                "TotalRevenue" => "Total Revenue",
                "TotalExpenses" => "Total Expenses",
                _ => metricKey
            };
        }

        private string GetMetricUnit(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => "g/hr",
                "CashBalance" => "$",
                "NetCashFlow" => "$",
                "TotalRevenue" => "$",
                "TotalExpenses" => "$",
                "EnergyUsage" => "kWh",
                "EnergyEfficiency" => "g/kWh",
                "PlantHealth" => "%",
                "FacilityUtilization" => "%",
                _ => ""
            };
        }

        private Color GetMetricColor(string metricKey)
        {
            return metricKey switch
            {
                "YieldPerHour" => Color.green,
                "ActivePlants" => Color.blue,
                "CashBalance" => Color.cyan,
                "NetCashFlow" => Color.cyan,
                "EnergyUsage" => Color.yellow,
                "EnergyEfficiency" => new Color(0.8f, 1f, 0.2f),
                "PlantHealth" => new Color(0.2f, 1f, 0.2f),
                "FacilityUtilization" => Color.magenta,
                "TotalRevenue" => Color.green,
                "TotalExpenses" => Color.red,
                _ => Color.white
            };
        }

        #endregion

        #region Public Events

        public System.Action<KPIData> CardClicked;

        #endregion

        #region Public API

        public void SetSize(KPISize size)
        {
            _size = size;
            this.RemoveFromClassList($"kpi-card-{_size.ToString().ToLower()}");
            this.AddToClassList($"kpi-card-{size.ToString().ToLower()}");
            ApplySize();
        }

        public void SetStatus(KPIStatus status)
        {
            _data.Status = status;
            UpdateStatusIndicator();
        }

        public void SetInteractive(bool interactive)
        {
            _isInteractive = interactive;
            if (interactive)
            {
                this.AddToClassList("kpi-card-interactive");
            }
            else
            {
                this.RemoveFromClassList("kpi-card-interactive");
                this.RemoveFromClassList("kpi-card-hover");
            }
        }

        public void Flash(Color flashColor, float duration = 0.5f)
        {
            var originalColor = this.style.backgroundColor;
            this.style.backgroundColor = flashColor;
            
            this.schedule.Execute(() => {
                this.style.backgroundColor = originalColor;
            }).StartingIn((long)(duration * 1000));
        }

        public void SetAnimationsEnabled(bool enabled)
        {
            // Enable or disable animations for this KPI card
            if (enabled)
            {
                this.AddToClassList("kpi-card-animations-enabled");
                this.RemoveFromClassList("kpi-card-animations-disabled");
            }
            else
            {
                this.AddToClassList("kpi-card-animations-disabled");
                this.RemoveFromClassList("kpi-card-animations-enabled");
            }
        }

        #endregion
    }

    #region KPI Card Factory

    public static class KPICardFactory
    {
        public static KPICard CreateStandardCard(string metricKey, KPICard.KPISize size = KPICard.KPISize.Medium)
        {
            var card = new KPICard("", size);
            card.SetMetricData(metricKey, 0f);
            return card;
        }

        public static KPICard CreateCustomCard(string title, float value, string unit, Color color, KPICard.KPISize size = KPICard.KPISize.Medium)
        {
            var card = new KPICard(title, size);
            var data = new KPICard.KPIData
            {
                Title = title,
                CurrentValue = value,
                Unit = unit,
                CardColor = color
            };
            card.UpdateData(data);
            return card;
        }

        public static VisualElement CreateKPIGrid(List<KPICard> cards, int columns = 4)
        {
            var grid = new VisualElement();
            grid.AddToClassList("kpi-grid");
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;

            foreach (var card in cards)
            {
                grid.Add(card);
            }

            return grid;
        }
    }

    #endregion
}