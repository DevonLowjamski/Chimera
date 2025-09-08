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
    /// Manages health alerts and notifications
    /// Focused component extracted from SystemHealthMonitoring
    /// </summary>
    public class SystemAlertManager : MonoBehaviour
    {
        [Header("Alert Configuration")]
        [SerializeField] private float _criticalHealthThreshold = 0.3f;
        [SerializeField] private float _warningHealthThreshold = 0.7f;
        [SerializeField] private int _maxConsecutiveFailures = 5;

        private readonly List<HealthAlert> _activeHealthAlerts = new List<HealthAlert>();
        private readonly Dictionary<string, int> _consecutiveFailures = new Dictionary<string, int>();

        public void ProcessAlerts(float deltaTime)
        {
            // Process and manage active alerts
            for (int i = _activeHealthAlerts.Count - 1; i >= 0; i--)
            {
                var alert = _activeHealthAlerts[i];
                if (alert.ShouldExpire(deltaTime))
                {
                    _activeHealthAlerts.RemoveAt(i);
                }
            }
        }

        public void TriggerHealthAlert(HealthCheckResult result)
        {
            var alertLevel = DetermineAlertLevel(result);
            if (alertLevel == AlertLevel.None)
                return;

            var alert = new HealthAlert
            {
                SystemId = result.SystemId,
                Level = alertLevel,
                Message = result.Message,
                Timestamp = DateTime.UtcNow,
                Status = result.Status
            };

            _activeHealthAlerts.Add(alert);
            NotifyAlert(alert);
        }

        private AlertLevel DetermineAlertLevel(HealthCheckResult result)
        {
            if (result.HealthScore < _criticalHealthThreshold)
                return AlertLevel.Critical;
            else if (result.HealthScore < _warningHealthThreshold)
                return AlertLevel.Warning;
            else
                return AlertLevel.None;
        }

        private void NotifyAlert(HealthAlert alert)
        {
            ChimeraLogger.LogWarning($"Health Alert [{alert.Level}]: {alert.Message}");
            // Additional notification logic here
        }

        public List<HealthAlert> GetActiveAlerts() => new List<HealthAlert>(_activeHealthAlerts);
    }
}
