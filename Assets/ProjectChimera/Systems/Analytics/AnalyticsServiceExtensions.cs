using UnityEngine;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Extension methods for easy AnalyticsService integration throughout the codebase
    /// Provides convenient access patterns for the DI-integrated analytics system
    /// </summary>
    public static class AnalyticsServiceExtensions
    {
        /// <summary>
        /// Get analytics service for any MonoBehaviour-derived class
        /// </summary>
        public static IAnalyticsService GetAnalyticsService(this MonoBehaviour component)
        {
            return AnalyticsManager.GetService();
        }

        /// <summary>
        /// Record a metric value with automatic error handling
        /// </summary>
        public static void RecordMetricSafe(this MonoBehaviour component, string metricName, float value)
        {
            try
            {
                var analyticsService = component.GetAnalyticsService();
                analyticsService?.RecordMetric(metricName, value);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{component.GetType().Name}] Failed to record metric {metricName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current metric value with fallback
        /// </summary>
        public static float GetMetricSafe(this MonoBehaviour component, string metricName, float fallback = 0f)
        {
            try
            {
                var analyticsService = component.GetAnalyticsService();
                return analyticsService?.GetMetric(metricName) ?? fallback;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{component.GetType().Name}] Failed to get metric {metricName}: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>
        /// Check if analytics service is available
        /// </summary>
        public static bool IsAnalyticsAvailable(this MonoBehaviour component)
        {
            return component.GetAnalyticsService() != null;
        }
    }

    /// <summary>
    /// Static helper for non-MonoBehaviour classes to access analytics
    /// </summary>
    public static class Analytics
    {
        /// <summary>
        /// Get the analytics service instance
        /// </summary>
        public static IAnalyticsService Service => AnalyticsManager.GetService();

        /// <summary>
        /// Record a metric value safely
        /// </summary>
        public static void RecordMetric(string metricName, float value)
        {
            try
            {
                Service?.RecordMetric(metricName, value);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Analytics] Failed to record metric {metricName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a metric value safely
        /// </summary>
        public static float GetMetric(string metricName, float fallback = 0f)
        {
            try
            {
                return Service?.GetMetric(metricName) ?? fallback;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Analytics] Failed to get metric {metricName}: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>
        /// Check if analytics service is available
        /// </summary>
        public static bool IsAvailable => Service != null;
    }
}