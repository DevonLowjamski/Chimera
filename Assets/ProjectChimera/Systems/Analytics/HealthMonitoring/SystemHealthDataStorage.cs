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
    /// Handles storage and retrieval of health monitoring data
    /// Focused component extracted from SystemHealthMonitoring
    /// </summary>
    public class SystemHealthDataStorage : MonoBehaviour
    {
        [Header("Storage Configuration")]
        [SerializeField] private int _maxHistoryEntries = 1000;
        [SerializeField] private float _dataRetentionDays = 7f;

        private readonly Dictionary<string, SystemHealthStatus> _systemHealth = new Dictionary<string, SystemHealthStatus>();
        private readonly Dictionary<string, List<HealthCheckResult>> _healthHistory = new Dictionary<string, List<HealthCheckResult>>();

        public void StoreHealthCheckResult(HealthCheckResult result)
        {
            // Update current health status
            _systemHealth[result.SystemId] = new SystemHealthStatus
            {
                SystemId = result.SystemId,
                Status = result.Status,
                HealthScore = result.HealthScore,
                LastUpdated = DateTime.UtcNow,
                Message = result.Message
            };

            // Store in history
            if (!_healthHistory.ContainsKey(result.SystemId))
                _healthHistory[result.SystemId] = new List<HealthCheckResult>();

            _healthHistory[result.SystemId].Add(result);

            // Cleanup old entries
            CleanupOldEntries(result.SystemId);
        }

        private void CleanupOldEntries(string systemId)
        {
            var history = _healthHistory[systemId];
            var cutoffTime = DateTime.UtcNow.AddDays(-_dataRetentionDays);

            history.RemoveAll(entry => entry.Timestamp < cutoffTime);

            // Also limit by max entries
            while (history.Count > _maxHistoryEntries)
            {
                history.RemoveAt(0);
            }
        }

        public SystemHealthStatus GetSystemHealth(string systemId)
        {
            return _systemHealth.GetValueOrDefault(systemId);
        }

        public List<HealthCheckResult> GetSystemHistory(string systemId)
        {
            return _healthHistory.GetValueOrDefault(systemId, new List<HealthCheckResult>());
        }

        public Dictionary<string, SystemHealthStatus> GetAllSystemHealth()
        {
            return new Dictionary<string, SystemHealthStatus>(_systemHealth);
        }
    }
}
