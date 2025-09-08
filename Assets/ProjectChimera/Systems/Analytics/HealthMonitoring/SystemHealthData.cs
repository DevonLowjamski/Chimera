using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
// using ProjectChimera.Systems.Services.Core; // Removed - namespace doesn't exist
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Analytics.HealthMonitoring
{
    /// <summary>
    /// Data structures and enums for health monitoring system
    /// Extracted from SystemHealthMonitoring for better organization
    /// </summary>

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    public enum AlertLevel
    {
        None,
        Info,
        Warning,
        Critical
    }

    public enum RecoveryActionType
    {
        Restart,
        Reset,
        Reinitialize,
        Custom
    }

    [System.Serializable]
    public class SystemHealthStatus
    {
        public string SystemId;
        public HealthStatus Status;
        public float HealthScore;
        public DateTime LastUpdated;
        public string Message;
    }

    [System.Serializable]
    public class HealthCheckResult
    {
        public string SystemId;
        public HealthStatus Status;
        public float HealthScore;
        public string Message;
        public DateTime Timestamp;
        public Dictionary<string, object> MetricData;
    }

    [System.Serializable]
    public class HealthAlert
    {
        public string SystemId;
        public AlertLevel Level;
        public string Message;
        public DateTime Timestamp;
        public HealthStatus Status;

        public bool ShouldExpire(float deltaTime)
        {
            return (DateTime.UtcNow - Timestamp).TotalMinutes > 60; // Expire after 1 hour
        }
    }

    [System.Serializable]
    public class RecoveryAction
    {
        public string SystemId;
        public RecoveryActionType ActionType;
        public System.Action Execute;
        public DateTime ScheduledTime;

        public bool IsReadyToExecute(float deltaTime)
        {
            return DateTime.UtcNow >= ScheduledTime;
        }
    }

    [System.Serializable]
    public class OverallHealthMetrics
    {
        public float OverallScore;
        public int TotalSystems;
        public int HealthySystems;
        public int WarningSystems;
        public int CriticalSystems;
        public string Status;
    }

    [System.Serializable]
    public class SystemTrendAnalysis
    {
        public string SystemId;
        public string Trend;
        public float ScoreDelta;
        public int DataPoints;
    }

    public interface IHealthCheckProvider
    {
        HealthCheckResult CheckHealth();
    }
}
