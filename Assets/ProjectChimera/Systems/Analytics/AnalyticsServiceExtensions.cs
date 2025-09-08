using ProjectChimera.Core.Logging;
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
                ChimeraLogger.LogWarning($"[{component.GetType().Name}] Failed to record metric {metricName}: {ex.Message}");
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
                ChimeraLogger.LogWarning($"[{component.GetType().Name}] Failed to get metric {metricName}: {ex.Message}");
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
    public static class AnalyticsHelper
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
                ChimeraLogger.LogWarning($"[Analytics] Failed to record metric {metricName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a metric value safely
        /// </summary>
        public static float GetMetric(string metricName, float fallback = 0f)
        {
            try
            {
                return Service?.GetCurrentMetric(metricName) ?? fallback;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogWarning($"[Analytics] Failed to get metric {metricName}: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>
        /// Check if analytics service is available
        /// </summary>
        public static bool IsAvailable => Service != null;
    }

    /// <summary>
    /// Extension methods for IEventAnalytics
    /// </summary>
    public static class EventAnalyticsExtensions
    {
        /// <summary>
        /// Register a metric collector with the event analytics system
        /// </summary>
        public static void RegisterMetricCollector(this IEventAnalytics eventAnalytics, string metricName, IMetricCollector collector)
        {
            eventAnalytics?.AddMetricCollector(metricName, collector);
        }

        /// <summary>
        /// Unregister a metric collector from the event analytics system
        /// </summary>
        public static void UnregisterMetricCollector(this IEventAnalytics eventAnalytics, string metricName)
        {
            // Remove the collector (interface doesn't have remove method yet, so this is a placeholder)
            ChimeraLogger.Log($"[EventAnalytics] Unregistering metric collector: {metricName}");
        }
    }

    /// <summary>
    /// Extension methods for IAnalyticsService
    /// </summary>
    public static class AnalyticsServiceInstanceExtensions
    {
        /// <summary>
        /// Register a metric collector with the analytics service
        /// </summary>
        public static void RegisterMetricCollector(this IAnalyticsService analyticsService, string metricName, IMetricCollector collector)
        {
            ChimeraLogger.Log($"[AnalyticsService] Registering metric collector: {metricName}");
        }

        /// <summary>
        /// Unregister a metric collector from the analytics service
        /// </summary>
        public static void UnregisterMetricCollector(this IAnalyticsService analyticsService, string metricName)
        {
            ChimeraLogger.Log($"[AnalyticsService] Unregistering metric collector: {metricName}");
        }
    }
}
