using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Base implementation for analytics providers
    /// Provides common functionality and helper methods for metrics collection
    /// </summary>
    public abstract class AnalyticsProviderBase : MonoBehaviour, IAnalyticsProvider
    {
        protected readonly Dictionary<string, MetricDefinition> _metricDefinitions = new Dictionary<string, MetricDefinition>();
        protected readonly Dictionary<string, Func<float>> _metricCalculators = new Dictionary<string, Func<float>>();
        
        protected bool _enableDebugLogging = false;
        protected string _providerName;

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_providerName))
                _providerName = GetType().Name;
                
            RegisterMetrics();
        }

        #region IAnalyticsProvider Implementation

        public virtual IEnumerable<string> GetAvailableMetrics()
        {
            return _metricDefinitions.Keys;
        }

        public virtual float GetMetricValue(string metricName)
        {
            if (!_metricCalculators.ContainsKey(metricName))
            {
                if (_enableDebugLogging)
                    ChimeraLogger.LogWarning($"[{_providerName}] Metric '{metricName}' not found");
                return 0f;
            }

            try
            {
                return _metricCalculators[metricName].Invoke();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[{_providerName}] Error calculating metric '{metricName}': {ex.Message}");
                return 0f;
            }
        }

        public virtual bool SupportsMetric(string metricName)
        {
            return _metricDefinitions.ContainsKey(metricName);
        }

        public virtual string GetMetricDisplayName(string metricName)
        {
            return _metricDefinitions.TryGetValue(metricName, out var definition) 
                ? definition.DisplayName 
                : metricName;
        }

        public virtual string GetMetricUnit(string metricName)
        {
            return _metricDefinitions.TryGetValue(metricName, out var definition) 
                ? definition.Unit 
                : "";
        }

        #endregion

        #region Metric Registration

        /// <summary>
        /// Override this method to register all metrics this provider supports
        /// </summary>
        protected abstract void RegisterMetrics();

        /// <summary>
        /// Register a metric with its calculation function
        /// </summary>
        protected void RegisterMetric(string metricName, string displayName, string unit, Func<float> calculator)
        {
            _metricDefinitions[metricName] = new MetricDefinition
            {
                Name = metricName,
                DisplayName = displayName,
                Unit = unit
            };
            
            _metricCalculators[metricName] = calculator;

            if (_enableDebugLogging)
                ChimeraLogger.Log($"[{_providerName}] Registered metric: {metricName} ({displayName})");
        }

        /// <summary>
        /// Register a simple metric that returns a constant value
        /// </summary>
        protected void RegisterConstantMetric(string metricName, string displayName, string unit, float value)
        {
            RegisterMetric(metricName, displayName, unit, () => value);
        }

        /// <summary>
        /// Register a metric that gets value from a property or field
        /// </summary>
        protected void RegisterPropertyMetric<T>(string metricName, string displayName, string unit, Func<T> propertyGetter)
            where T : struct
        {
            RegisterMetric(metricName, displayName, unit, () => Convert.ToSingle(propertyGetter()));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate percentage safely (avoiding division by zero)
        /// </summary>
        protected float CalculatePercentage(float numerator, float denominator)
        {
            return denominator > 0 ? (numerator / denominator) * 100f : 0f;
        }

        /// <summary>
        /// Calculate rate safely (per hour)
        /// </summary>
        protected float CalculateHourlyRate(float total, float timeInHours)
        {
            return timeInHours > 0 ? total / timeInHours : 0f;
        }

        /// <summary>
        /// Get current time in hours since startup
        /// </summary>
        protected float GetRuntimeHours()
        {
            return Time.time / 3600f;
        }

        /// <summary>
        /// Clamp value to reasonable range for UI display
        /// </summary>
        protected float ClampForDisplay(float value, float min = 0f, float max = float.MaxValue)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Round value for cleaner display
        /// </summary>
        protected float RoundForDisplay(float value, int decimals = 2)
        {
            return (float)Math.Round(value, decimals);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate all metric calculators are working
        /// </summary>
        public virtual bool ValidateMetrics()
        {
            var validationResults = new List<bool>();
            
            foreach (var metric in _metricCalculators)
            {
                try
                {
                    var value = metric.Value.Invoke();
                    var isValid = !float.IsNaN(value) && !float.IsInfinity(value);
                    validationResults.Add(isValid);
                    
                    if (!isValid && _enableDebugLogging)
                        ChimeraLogger.LogWarning($"[{_providerName}] Metric '{metric.Key}' returned invalid value: {value}");
                }
                catch (Exception ex)
                {
                    validationResults.Add(false);
                    ChimeraLogger.LogError($"[{_providerName}] Metric '{metric.Key}' validation failed: {ex.Message}");
                }
            }
            
            var allValid = validationResults.All(r => r);
            if (_enableDebugLogging)
                ChimeraLogger.Log($"[{_providerName}] Metric validation: {validationResults.Count(r => r)}/{validationResults.Count} passed");
            
            return allValid;
        }

        #endregion

        /// <summary>
        /// Enable or disable debug logging for this provider
        /// </summary>
        public void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
        }

        /// <summary>
        /// Get summary of all metrics for debugging
        /// </summary>
        public virtual string GetMetricsSummary()
        {
            var summary = $"[{_providerName}] Metrics Summary:\n";
            foreach (var metric in _metricDefinitions)
            {
                try
                {
                    var value = GetMetricValue(metric.Key);
                    summary += $"  {metric.Value.DisplayName}: {value:F2} {metric.Value.Unit}\n";
                }
                catch (Exception ex)
                {
                    summary += $"  {metric.Value.DisplayName}: ERROR - {ex.Message}\n";
                }
            }
            return summary;
        }
    }

    /// <summary>
    /// Metric definition structure
    /// </summary>
    public class MetricDefinition
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public MetricCategory Category { get; set; } = MetricCategory.General;
    }

    /// <summary>
    /// Categories for organizing metrics
    /// </summary>
    public enum MetricCategory
    {
        General,
        Cultivation,
        Economic,
        Environmental,
        Facility,
        Operational,
        Performance
    }
}