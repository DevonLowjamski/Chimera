using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.UI;

namespace ProjectChimera.UI.Components
{
    /// <summary>
    /// Reusable progress bar component for Project Chimera UI
    /// </summary>
    public class UIProgressBar : VisualElement
    {
        private VisualElement _background;
        private VisualElement _fill;
        private Label _label;
        private float _value;
        private float _maxValue;
        private string _format = "{0:F1}%";
        
        public float Value 
        { 
            get => _value; 
            set => SetValue(value);
        }
        
        public float MaxValue 
        { 
            get => _maxValue; 
            set => SetMaxValue(value);
        }
        
        public string Format 
        { 
            get => _format; 
            set => SetFormat(value);
        }
        
        public UIProgressBar()
        {
            SetupElement();
        }
        
        public UIProgressBar(float maxValue) : this()
        {
            _maxValue = maxValue;
        }
        
        private void SetupElement()
        {
            name = "progress-bar";
            
            // Container styles
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.height = 20;
            style.minWidth = 100;
            
            // Background
            _background = new VisualElement();
            _background.name = "progress-background";
            _background.style.flexGrow = 1;
            _background.style.height = 8;
            _background.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _background.style.borderTopLeftRadius = 4;
            _background.style.borderTopRightRadius = 4;
            _background.style.borderBottomLeftRadius = 4;
            _background.style.borderBottomRightRadius = 4;
            
            // Fill
            _fill = new VisualElement();
            _fill.name = "progress-fill";
            _fill.style.height = 8;
            _fill.style.backgroundColor = new Color(0.2f, 0.8f, 0.3f, 1f);
            _fill.style.borderTopLeftRadius = 4;
            _fill.style.borderTopRightRadius = 4;
            _fill.style.borderBottomLeftRadius = 4;
            _fill.style.borderBottomRightRadius = 4;
            _fill.style.width = 0;
            
            // Label
            _label = new Label();
            _label.name = "progress-label";
            _label.style.marginLeft = 8;
            _label.style.fontSize = 12;
            _label.style.color = Color.white;
            
            _background.Add(_fill);
            Add(_background);
            Add(_label);
            
            _maxValue = 100f;
            UpdateDisplay();
        }
        
        private void SetValue(float value)
        {
            _value = Mathf.Clamp(value, 0f, _maxValue);
            UpdateDisplay();
        }
        
        private void SetMaxValue(float maxValue)
        {
            _maxValue = Mathf.Max(0.001f, maxValue);
            _value = Mathf.Clamp(_value, 0f, _maxValue);
            UpdateDisplay();
        }
        
        private void SetFormat(string format)
        {
            _format = format ?? "{0:F1}%";
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            float percentage = (_maxValue > 0) ? (_value / _maxValue) * 100f : 0f;
            float fillPercentage = (_maxValue > 0) ? (_value / _maxValue) : 0f;
            
            _fill.style.width = Length.Percent(fillPercentage * 100f);
            _label.text = string.Format(_format, percentage);
        }
        
        public void SetColor(Color color)
        {
            _fill.style.backgroundColor = color;
        }
        
        public void SetBackgroundColor(Color color)
        {
            _background.style.backgroundColor = color;
        }
    }
    
    /// <summary>
    /// Reusable data card component for displaying key-value information
    /// </summary>
    public class UIDataCard : VisualElement
    {
        private Label _titleLabel;
        private Label _valueLabel;
        private Label _unitLabel;
        private VisualElement _iconContainer;
        
        public string Title 
        { 
            get => _titleLabel.text; 
            set => _titleLabel.text = value;
        }
        
        public string Value 
        { 
            get => _valueLabel.text; 
            set => _valueLabel.text = value;
        }
        
        public string Unit 
        { 
            get => _unitLabel.text; 
            set => _unitLabel.text = value;
        }
        
        public UIDataCard()
        {
            SetupElement();
        }
        
        public UIDataCard(string title, string value, string unit = "") : this()
        {
            Title = title;
            Value = value;
            Unit = unit;
        }
        
        private void SetupElement()
        {
            name = "data-card";
            
            // Card styles
            style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 16;
            style.paddingRight = 16;
            style.minWidth = 120;
            style.flexDirection = FlexDirection.Column;
            
            // Icon container
            _iconContainer = new VisualElement();
            _iconContainer.name = "icon-container";
            _iconContainer.style.height = 24;
            _iconContainer.style.marginBottom = 8;
            _iconContainer.style.display = DisplayStyle.None;
            
            // Title
            _titleLabel = new Label();
            _titleLabel.name = "title";
            _titleLabel.style.fontSize = 12;
            _titleLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            _titleLabel.style.marginBottom = 4;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            
            // Value container
            var valueContainer = new VisualElement();
            valueContainer.name = "value-container";
            valueContainer.style.flexDirection = FlexDirection.Row;
            valueContainer.style.alignItems = Align.FlexEnd;
            
            // Value
            _valueLabel = new Label();
            _valueLabel.name = "value";
            _valueLabel.style.fontSize = 18;
            _valueLabel.style.color = Color.white;
            _valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Unit
            _unitLabel = new Label();
            _unitLabel.name = "unit";
            _unitLabel.style.fontSize = 12;
            _unitLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            _unitLabel.style.marginLeft = 4;
            _unitLabel.style.marginBottom = 2;
            
            valueContainer.Add(_valueLabel);
            valueContainer.Add(_unitLabel);
            
            Add(_iconContainer);
            Add(_titleLabel);
            Add(valueContainer);
        }
        
        public void SetIcon(Background icon)
        {
            _iconContainer.style.backgroundImage = icon;
            _iconContainer.style.display = DisplayStyle.Flex;
        }
        
        public void SetValueColor(Color color)
        {
            _valueLabel.style.color = color;
        }
        
        public void SetCardColor(Color backgroundColor)
        {
            style.backgroundColor = backgroundColor;
        }
    }
    
    /// <summary>
    /// Reusable chart component for displaying data visualizations
    /// </summary>
    public class UISimpleChart : VisualElement
    {
        private VisualElement _chartArea;
        private Label _titleLabel;
        private List<float> _dataPoints;
        private Color _lineColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        private Color _fillColor = new Color(0.2f, 0.8f, 0.3f, 0.3f);
        private float _minValue = 0f;
        private float _maxValue = 100f;
        
        public string Title 
        { 
            get => _titleLabel.text; 
            set => _titleLabel.text = value;
        }
        
        public Color LineColor 
        { 
            get => _lineColor; 
            set => SetLineColor(value);
        }
        
        public Color FillColor 
        { 
            get => _fillColor; 
            set => SetFillColor(value);
        }
        
        public UISimpleChart()
        {
            SetupElement();
            _dataPoints = new List<float>();
        }
        
        public UISimpleChart(string title) : this()
        {
            Title = title;
        }
        
        private void SetupElement()
        {
            name = "simple-chart";
            
            // Container styles
            style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 16;
            style.paddingRight = 16;
            style.minHeight = 200;
            style.flexDirection = FlexDirection.Column;
            
            // Title
            _titleLabel = new Label();
            _titleLabel.name = "chart-title";
            _titleLabel.style.fontSize = 14;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.marginBottom = 12;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Chart area
            _chartArea = new VisualElement();
            _chartArea.name = "chart-area";
            _chartArea.style.flexGrow = 1;
            _chartArea.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            _chartArea.style.borderTopLeftRadius = 4;
            _chartArea.style.borderTopRightRadius = 4;
            _chartArea.style.borderBottomLeftRadius = 4;
            _chartArea.style.borderBottomRightRadius = 4;
            
            Add(_titleLabel);
            Add(_chartArea);
        }
        
        public void SetData(List<float> dataPoints)
        {
            _dataPoints = dataPoints ?? new List<float>();
            UpdateChart();
        }
        
        public void AddDataPoint(float value)
        {
            _dataPoints.Add(value);
            UpdateChart();
        }
        
        public void SetRange(float minValue, float maxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            UpdateChart();
        }
        
        private void SetLineColor(Color color)
        {
            _lineColor = color;
            UpdateChart();
        }
        
        private void SetFillColor(Color color)
        {
            _fillColor = color;
            UpdateChart();
        }
        
        private void UpdateChart()
        {
            // Clear existing chart elements
            _chartArea.Clear();
            
            if (_dataPoints.Count < 2) return;
            
            // Create a simple line chart representation using visual elements
            // This is a simplified implementation - for more complex charts, consider using a dedicated charting library
            var chartLine = new VisualElement();
            chartLine.name = "chart-line";
            chartLine.style.position = Position.Absolute;
            chartLine.style.width = Length.Percent(100);
            chartLine.style.height = Length.Percent(100);
            
            // Add data point indicators
            for (int i = 0; i < _dataPoints.Count; i++)
            {
                var point = new VisualElement();
                point.name = $"data-point-{i}";
                point.style.position = Position.Absolute;
                point.style.width = 6;
                point.style.height = 6;
                point.style.backgroundColor = _lineColor;
                point.style.borderTopLeftRadius = 3;
                point.style.borderTopRightRadius = 3;
                point.style.borderBottomLeftRadius = 3;
                point.style.borderBottomRightRadius = 3;
                
                // Position based on data
                float xPercent = (float)i / (_dataPoints.Count - 1) * 100f;
                float normalizedValue = Mathf.InverseLerp(_minValue, _maxValue, _dataPoints[i]);
                float yPercent = (1f - normalizedValue) * 100f; // Invert Y for UI coordinates
                
                point.style.left = Length.Percent(xPercent);
                point.style.top = Length.Percent(yPercent);
                
                chartLine.Add(point);
            }
            
            _chartArea.Add(chartLine);
        }
    }
    
    /// <summary>
    /// Reusable status indicator component
    /// </summary>
    public class UIStatusIndicator : VisualElement
    {
        private VisualElement _indicator;
        private Label _label;
        private UIStatus _status;
        
        public UIStatus Status 
        { 
            get => _status; 
            set => SetStatus(value);
        }
        
        public string Label 
        { 
            get => _label.text; 
            set => _label.text = value;
        }
        
        public UIStatusIndicator()
        {
            SetupElement();
        }
        
        public UIStatusIndicator(UIStatus status, string label = "") : this()
        {
            SetStatus(status);
            Label = label;
        }
        
        private void SetupElement()
        {
            name = "status-indicator";
            
            // Container styles
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            
            // Indicator dot
            _indicator = new VisualElement();
            _indicator.name = "status-dot";
            _indicator.style.width = 12;
            _indicator.style.height = 12;
            _indicator.style.borderTopLeftRadius = 6;
            _indicator.style.borderTopRightRadius = 6;
            _indicator.style.borderBottomLeftRadius = 6;
            _indicator.style.borderBottomRightRadius = 6;
            _indicator.style.marginRight = 8;
            
            // Label
            _label = new Label();
            _label.name = "status-label";
            _label.style.fontSize = 12;
            _label.style.color = Color.white;
            
            Add(_indicator);
            Add(_label);
            
            SetStatus(UIStatus.Unknown);
        }
        
        private void SetStatus(UIStatus status)
        {
            _status = status;
            
            Color statusColor = status switch
            {
                UIStatus.Success => new Color(0.2f, 0.8f, 0.3f, 1f),     // Green
                UIStatus.Warning => new Color(0.9f, 0.7f, 0.2f, 1f),     // Yellow
                UIStatus.Error => new Color(0.8f, 0.2f, 0.2f, 1f),       // Red
                UIStatus.Info => new Color(0.3f, 0.6f, 0.9f, 1f),        // Blue
                UIStatus.Processing => new Color(0.6f, 0.6f, 0.6f, 1f),  // Gray
                _ => new Color(0.4f, 0.4f, 0.4f, 1f)                     // Unknown
            };
            
            _indicator.style.backgroundColor = statusColor;
            
            // Add pulsing animation for processing status
            if (status == UIStatus.Processing)
            {
                // Note: For full animation support, you might want to use DOTween or similar
                // This is a simplified representation
                _indicator.style.opacity = 0.7f;
            }
            // else
            // {
                _indicator.style.opacity = 1f;
            // }
        }
    }
    
    /// <summary>
    /// Reusable notification toast component
    /// </summary>
    public class UINotificationToast : VisualElement
    {
        private Label _messageLabel;
        private Button _closeButton;
        private UIStatus _notificationType;
        private System.Action _onClose;
        
        public string Message 
        { 
            get => _messageLabel.text; 
            set => _messageLabel.text = value;
        }
        
        public UINotificationToast()
        {
            SetupElement();
        }
        
        public UINotificationToast(string message, UIStatus type = UIStatus.Info, System.Action onClose = null) : this()
        {
            Message = message;
            SetNotificationType(type);
            _onClose = onClose;
        }
        
        private void SetupElement()
        {
            name = "notification-toast";
            
            // Container styles
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            style.borderTopLeftRadius = 6;
            style.borderTopRightRadius = 6;
            style.borderBottomLeftRadius = 6;
            style.borderBottomRightRadius = 6;
            style.paddingTop = 12;
            style.paddingBottom = 12;
            style.paddingLeft = 16;
            style.paddingRight = 16;
            style.marginBottom = 8;
            style.minWidth = 300;
            style.maxWidth = 500;
            
            // Message
            _messageLabel = new Label();
            _messageLabel.name = "message";
            _messageLabel.style.fontSize = 14;
            _messageLabel.style.color = Color.white;
            _messageLabel.style.flexGrow = 1;
            _messageLabel.style.whiteSpace = WhiteSpace.Normal;
            
            // Close button
            _closeButton = new Button(() => Close());
            _closeButton.name = "close-button";
            _closeButton.text = "×";
            _closeButton.style.fontSize = 16;
            _closeButton.style.width = 24;
            _closeButton.style.height = 24;
            _closeButton.style.marginLeft = 12;
            _closeButton.style.backgroundColor = Color.clear;
            _closeButton.style.borderTopWidth = 0;
            _closeButton.style.borderRightWidth = 0;
            _closeButton.style.borderBottomWidth = 0;
            _closeButton.style.borderLeftWidth = 0;
            _closeButton.style.color = Color.white;
            
            Add(_messageLabel);
            Add(_closeButton);
        }
        
        private void SetNotificationType(UIStatus type)
        {
            _notificationType = type;
            
            Color backgroundColor = type switch
            {
                UIStatus.Success => new Color(0.2f, 0.6f, 0.3f, 0.95f),
                UIStatus.Warning => new Color(0.8f, 0.6f, 0.2f, 0.95f),
                UIStatus.Error => new Color(0.7f, 0.2f, 0.2f, 0.95f),
                UIStatus.Info => new Color(0.2f, 0.5f, 0.8f, 0.95f),
                _ => new Color(0.2f, 0.2f, 0.2f, 0.95f)
            };
            
            style.backgroundColor = backgroundColor;
        }
        
        public void Show(VisualElement container, float duration = 5f)
        {
            if (container != null)
            {
                container.Add(this);
                
                if (duration > 0)
                {
                    // Auto-close after duration
                    this.schedule.Execute(() => Close()).StartingIn((long)(duration * 1000));
                }
            }
        }
        
        private void Close()
        {
            _onClose?.Invoke();
            
            // Animate out (simplified - could use proper tweening)
            style.opacity = 0f;
            
            // Remove after animation
            this.schedule.Execute(() => RemoveFromHierarchy()).StartingIn(200);
        }
    }
    
    /// <summary>
    /// Status enumeration for UI components
    /// </summary>
    public enum UIStatus
    {
        Success,
        Warning,
        Error,
        Info,
        Processing,
        Unknown
    }
}