using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Systems.Analytics;

namespace ProjectChimera.UI.Components
{
    /// <summary>
    /// Simple chart widgets for data visualization in analytics dashboard
    /// Lightweight implementation for Phase 9 using UI Toolkit drawing
    /// </summary>

    #region Base Chart Widget

    public abstract class ChartWidget : VisualElement
    {
        [System.Serializable]
        public class ChartData
        {
            public List<float> Values = new List<float>();
            public List<string> Labels = new List<string>();
            public string Title = "";
            public Color ChartColor = Color.cyan;
        }

        protected ChartData _chartData;
        protected Rect _chartRect;
        protected Vector2 _chartSize = new Vector2(300, 200);
        protected float _marginTop = 30f;
        protected float _marginBottom = 30f;
        protected float _marginLeft = 40f;
        protected float _marginRight = 20f;

        // Style properties
        protected Color _backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        protected Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        protected Color _textColor = Color.white;
        protected int _fontSize = 12;

        public ChartWidget()
        {
            _chartData = new ChartData();
            this.AddToClassList("chart-widget");
            this.generateVisualContent += OnGenerateVisualContent;
            
            // Set fixed size
            this.style.width = _chartSize.x;
            this.style.height = _chartSize.y;
            this.style.backgroundColor = _backgroundColor;
            this.style.borderTopLeftRadius = 6;
            this.style.borderTopRightRadius = 6;
            this.style.borderBottomLeftRadius = 6;
            this.style.borderBottomRightRadius = 6;
        }

        public virtual void SetData(ChartData data)
        {
            _chartData = data;
            MarkDirtyRepaint();
        }

        public virtual void SetData(List<MetricDataPoint> dataPoints, string title = "", Color? color = null)
        {
            _chartData.Values = dataPoints.Select(dp => dp.Value).ToList();
            _chartData.Labels = dataPoints.Select(dp => dp.Timestamp.ToString("HH:mm")).ToList();
            _chartData.Title = title;
            _chartData.ChartColor = color ?? Color.cyan;
            MarkDirtyRepaint();
        }

        protected abstract void OnGenerateVisualContent(MeshGenerationContext context);

        protected void DrawBackground(MeshGenerationContext context)
        {
            var rect = contentRect;
            var painter = context.painter2D;
            
            painter.fillColor = _backgroundColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(0, rect.height));
            painter.ClosePath();
            painter.Fill();
        }

        protected void DrawTitle(MeshGenerationContext context)
        {
            if (string.IsNullOrEmpty(_chartData.Title)) return;

            var rect = contentRect;
            // Note: Text rendering in generateVisualContent is limited
            // For full text support, would need TextElement approach
        }

        protected void DrawGrid(MeshGenerationContext context, int horizontalLines = 4, int verticalLines = 4)
        {
            var rect = GetChartArea();
            var painter = context.painter2D;
            
            painter.strokeColor = _gridColor;
            painter.lineWidth = 1f;

            // Horizontal grid lines
            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = rect.y + (rect.height / horizontalLines) * i;
                painter.BeginPath();
                painter.MoveTo(new Vector2(rect.x, y));
                painter.LineTo(new Vector2(rect.x + rect.width, y));
                painter.Stroke();
            }

            // Vertical grid lines
            for (int i = 0; i <= verticalLines; i++)
            {
                float x = rect.x + (rect.width / verticalLines) * i;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, rect.y));
                painter.LineTo(new Vector2(x, rect.y + rect.height));
                painter.Stroke();
            }
        }

        protected Rect GetChartArea()
        {
            var rect = contentRect;
            return new Rect(
                _marginLeft,
                _marginTop,
                rect.width - _marginLeft - _marginRight,
                rect.height - _marginTop - _marginBottom
            );
        }

        protected Vector2 DataToChartPosition(int index, float value)
        {
            var chartArea = GetChartArea();
            
            if (_chartData.Values.Count == 0) return Vector2.zero;

            float minValue = _chartData.Values.Min();
            float maxValue = _chartData.Values.Max();
            float valueRange = maxValue - minValue;
            
            if (valueRange == 0) valueRange = 1; // Prevent division by zero

            float x = chartArea.x + (chartArea.width / (_chartData.Values.Count - 1)) * index;
            float y = chartArea.y + chartArea.height - ((value - minValue) / valueRange) * chartArea.height;

            return new Vector2(x, y);
        }
    }

    #endregion

    #region Line Chart Widget

    public class LineChartWidget : ChartWidget
    {
        private float _lineWidth = 2f;
        private bool _showDataPoints = true;
        private float _dataPointRadius = 3f;

        public LineChartWidget() : base()
        {
            this.AddToClassList("line-chart");
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_chartData?.Values == null || _chartData.Values.Count < 2)
                return;

            DrawBackground(context);
            DrawGrid(context);
            DrawLine(context);
            
            if (_showDataPoints)
                DrawDataPoints(context);
        }

        private void DrawLine(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.strokeColor = _chartData.ChartColor;
            painter.lineWidth = _lineWidth;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;

            painter.BeginPath();
            
            for (int i = 0; i < _chartData.Values.Count; i++)
            {
                var point = DataToChartPosition(i, _chartData.Values[i]);
                
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }
            
            painter.Stroke();
        }

        private void DrawDataPoints(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.fillColor = _chartData.ChartColor;

            for (int i = 0; i < _chartData.Values.Count; i++)
            {
                var point = DataToChartPosition(i, _chartData.Values[i]);
                
                painter.BeginPath();
                painter.Arc(point, _dataPointRadius, 0, 360);
                painter.Fill();
            }
        }

        public void SetLineStyle(float lineWidth, bool showDataPoints = true, float dataPointRadius = 3f)
        {
            _lineWidth = lineWidth;
            _showDataPoints = showDataPoints;
            _dataPointRadius = dataPointRadius;
            MarkDirtyRepaint();
        }
    }

    #endregion

    #region Bar Chart Widget

    public class BarChartWidget : ChartWidget
    {
        private float _barWidthRatio = 0.8f; // Percentage of available space
        private Color _barOutlineColor = new Color(1f, 1f, 1f, 0.5f);

        public BarChartWidget() : base()
        {
            this.AddToClassList("bar-chart");
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_chartData?.Values == null || _chartData.Values.Count == 0)
                return;

            DrawBackground(context);
            DrawGrid(context);
            DrawBars(context);
        }

        private void DrawBars(MeshGenerationContext context)
        {
            var chartArea = GetChartArea();
            var painter = context.painter2D;
            
            if (_chartData.Values.Count == 0) return;

            float minValue = _chartData.Values.Min();
            float maxValue = _chartData.Values.Max();
            float valueRange = maxValue - minValue;
            
            if (valueRange == 0) valueRange = 1;

            float barWidth = (chartArea.width / _chartData.Values.Count) * _barWidthRatio;
            float barSpacing = chartArea.width / _chartData.Values.Count;

            for (int i = 0; i < _chartData.Values.Count; i++)
            {
                float value = _chartData.Values[i];
                float normalizedValue = (value - minValue) / valueRange;
                
                float barHeight = normalizedValue * chartArea.height;
                float x = chartArea.x + (barSpacing * i) + (barSpacing - barWidth) * 0.5f;
                float y = chartArea.y + chartArea.height - barHeight;

                // Draw bar fill
                painter.fillColor = _chartData.ChartColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, y));
                painter.LineTo(new Vector2(x + barWidth, y));
                painter.LineTo(new Vector2(x + barWidth, y + barHeight));
                painter.LineTo(new Vector2(x, y + barHeight));
                painter.ClosePath();
                painter.Fill();

                // Draw bar outline
                painter.strokeColor = _barOutlineColor;
                painter.lineWidth = 1f;
                painter.Stroke();
            }
        }

        public void SetBarStyle(float barWidthRatio = 0.8f, Color? outlineColor = null)
        {
            _barWidthRatio = Mathf.Clamp01(barWidthRatio);
            _barOutlineColor = outlineColor ?? new Color(1f, 1f, 1f, 0.5f);
            MarkDirtyRepaint();
        }
    }

    #endregion

    #region Mini Spark Line Chart

    public class SparkLineWidget : VisualElement
    {
        private List<float> _values = new List<float>();
        private Color _lineColor = Color.green;
        private float _lineWidth = 1.5f;
        private Vector2 _size = new Vector2(100, 30);

        public SparkLineWidget()
        {
            this.AddToClassList("sparkline-widget");
            this.generateVisualContent += OnGenerateVisualContent;
            
            this.style.width = _size.x;
            this.style.height = _size.y;
            this.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            this.style.borderTopLeftRadius = 3;
            this.style.borderTopRightRadius = 3;
            this.style.borderBottomLeftRadius = 3;
            this.style.borderBottomRightRadius = 3;
        }

        public void SetData(List<float> values, Color? color = null)
        {
            _values = new List<float>(values);
            _lineColor = color ?? Color.green;
            MarkDirtyRepaint();
        }

        public void SetSize(Vector2 size)
        {
            _size = size;
            this.style.width = size.x;
            this.style.height = size.y;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_values == null || _values.Count < 2)
                return;

            var rect = contentRect;
            var painter = context.painter2D;
            
            painter.strokeColor = _lineColor;
            painter.lineWidth = _lineWidth;
            painter.lineJoin = LineJoin.Round;

            float minValue = _values.Min();
            float maxValue = _values.Max();
            float valueRange = maxValue - minValue;
            
            if (valueRange == 0) valueRange = 1;

            painter.BeginPath();

            for (int i = 0; i < _values.Count; i++)
            {
                float x = (rect.width / (_values.Count - 1)) * i;
                float normalizedValue = (_values[i] - minValue) / valueRange;
                float y = rect.height - (normalizedValue * rect.height);

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }

            painter.Stroke();
        }
    }

    #endregion

    #region Chart Factory

    public static class ChartFactory
    {
        public static LineChartWidget CreateLineChart(string title, Color color)
        {
            var chart = new LineChartWidget();
            var data = new ChartWidget.ChartData
            {
                Title = title,
                ChartColor = color
            };
            chart.SetData(data);
            return chart;
        }

        public static BarChartWidget CreateBarChart(string title, Color color)
        {
            var chart = new BarChartWidget();
            var data = new ChartWidget.ChartData
            {
                Title = title,
                ChartColor = color
            };
            chart.SetData(data);
            return chart;
        }

        public static SparkLineWidget CreateSparkLine(Color color, Vector2 size)
        {
            var sparkline = new SparkLineWidget();
            sparkline.SetSize(size);
            sparkline.SetData(new List<float>(), color);
            return sparkline;
        }

        public static VisualElement CreateChartContainer(string title, ChartWidget chart)
        {
            var container = new VisualElement();
            container.AddToClassList("chart-container");

            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label(title);
                titleLabel.AddToClassList("chart-title");
                container.Add(titleLabel);
            }

            container.Add(chart);
            return container;
        }
    }

    #endregion
}