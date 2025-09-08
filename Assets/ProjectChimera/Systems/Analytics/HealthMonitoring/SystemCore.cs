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
    /// Core health monitoring orchestrator - coordinates all health monitoring activities
    /// Refactored from SystemHealthMonitoring for Single Responsibility Principle
    /// </summary>
    public class SystemCore : MonoBehaviour, ITickable
    {
        [Header("Core Configuration")]
        [SerializeField] private bool _enableHealthChecks = true;
        [SerializeField] private float _healthCheckInterval = 5f;

        // Dependencies
        private SystemHealthChecker _healthChecker;
        private SystemAlertManager _alertManager;
        private SystemRecoveryManager _recoveryManager;
        private SystemHealthDataStorage _dataStorage;

        public int Priority => 300; // SystemCore priority
        public bool Enabled => enabled && _enableHealthChecks;

        private void Start()
        {
            InitializeDependencies();
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        public void Tick(float deltaTime)
        {
            // Orchestrate health monitoring activities
            if (_healthChecker != null)
                _healthChecker.ProcessHealthChecks(deltaTime);

            if (_alertManager != null)
                _alertManager.ProcessAlerts(deltaTime);

            if (_recoveryManager != null)
                _recoveryManager.ProcessRecoveryActions(deltaTime);
        }

        private void InitializeDependencies()
        {
            _healthChecker = GetComponent<SystemHealthChecker>() ?? gameObject.AddComponent<SystemHealthChecker>();
            _alertManager = GetComponent<SystemAlertManager>() ?? gameObject.AddComponent<SystemAlertManager>();
            _recoveryManager = GetComponent<SystemRecoveryManager>() ?? gameObject.AddComponent<SystemRecoveryManager>();
            _dataStorage = GetComponent<SystemHealthDataStorage>() ?? gameObject.AddComponent<SystemHealthDataStorage>();
        }
    }
}
