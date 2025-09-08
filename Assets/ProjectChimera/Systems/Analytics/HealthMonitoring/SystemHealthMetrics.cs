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
    /// Calculates and reports health metrics and analytics
    /// Focused component extracted from SystemHealthMonitoring
    /// </summary>
    public class SystemHealthMetrics : MonoBehaviour
    {
        private SystemHealthDataStorage _dataStorage;

        private void Start()
        {
            _dataStorage = GetComponent<SystemHealthDataStorage>();
        }

        public OverallHealthMetrics CalculateOverallHealth()
        {
            var allHealth = _dataStorage?.GetAllSystemHealth() ?? new Dictionary<string, SystemHealthStatus>();

            if (allHealth.Count == 0)
                return new OverallHealthMetrics { OverallScore = 1.0f, Status = "No Data" };

            var totalScore = allHealth.Values.Sum(h => h.HealthScore);
            var averageScore = totalScore / allHealth.Count;

            var criticalCount = allHealth.Values.Count(h => h.Status == HealthStatus.Critical);
            var warningCount = allHealth.Values.Count(h => h.Status == HealthStatus.Warning);
            var healthyCount = allHealth.Values.Count(h => h.Status == HealthStatus.Healthy);

            return new OverallHealthMetrics
            {
                OverallScore = averageScore,
                TotalSystems = allHealth.Count,
                HealthySystems = healthyCount,
                WarningSystems = warningCount,
                CriticalSystems = criticalCount,
                Status = DetermineOverallStatus(criticalCount, warningCount, healthyCount)
            };
        }

        private string DetermineOverallStatus(int critical, int warning, int healthy)
        {
            if (critical > 0)
                return "Critical Issues Detected";
            else if (warning > 0)
                return "Some Systems Need Attention";
            else
                return "All Systems Healthy";
        }

        public SystemTrendAnalysis AnalyzeSystemTrend(string systemId, TimeSpan period)
        {
            var history = _dataStorage?.GetSystemHistory(systemId) ?? new List<HealthCheckResult>();
            var cutoffTime = DateTime.UtcNow.Subtract(period);
            var recentHistory = history.Where(h => h.Timestamp >= cutoffTime).ToList();

            if (recentHistory.Count < 2)
                return new SystemTrendAnalysis { SystemId = systemId, Trend = "Insufficient Data" };

            var earliestScore = recentHistory.First().HealthScore;
            var latestScore = recentHistory.Last().HealthScore;
            var scoreDelta = latestScore - earliestScore;

            var trend = scoreDelta > 0.1f ? "Improving" :
                       scoreDelta < -0.1f ? "Declining" : "Stable";

            return new SystemTrendAnalysis
            {
                SystemId = systemId,
                Trend = trend,
                ScoreDelta = scoreDelta,
                DataPoints = recentHistory.Count
            };
        }
    }
}
