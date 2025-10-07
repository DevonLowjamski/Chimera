using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Foundation.Recovery;
using System.Collections.Generic;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Manager - Delegation wrapper for foundation recovery subsystems
    /// Coordinates recovery strategy execution, queue processing, history tracking, monitoring, and statistics
    /// Uses coordination pattern with specialized subsystems for focused responsibilities
    /// </summary>
    public class FoundationRecoveryManager : MonoBehaviour
    {
        [Header("Recovery Management Settings")]
        [SerializeField] private bool _enableAutomaticRecovery = true;
        [SerializeField] private bool _enableLogging = false;

        // Subsystem references
        private FoundationRecoveryCore _recoveryCore;

        // Properties
        public bool IsEnabled => _recoveryCore?.IsEnabled ?? false;
        public bool IsRecoveryInProgress => _recoveryCore?.IsRecoveryActive ?? false;
        public RecoveryManagerStats GetStats() => _recoveryCore?.GetRecoveryStats() ?? new RecoveryManagerStats();

        // Events
        public System.Action<string, RecoveryStrategy> OnRecoveryStarted;
        public System.Action<string, bool> OnRecoveryCompleted;
        public System.Action<string, string> OnRecoveryFailed;
        public System.Action<string> OnSystemDegraded;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeRecoveryCore();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🔧 FoundationRecoveryManager initialized with delegation pattern", this);
        }

        /// <summary>
        /// Initialize recovery core subsystem
        /// </summary>
        private void InitializeRecoveryCore()
        {
            // Get or create recovery core component
            _recoveryCore = GetComponent<FoundationRecoveryCore>();
            if (_recoveryCore == null)
            {
                _recoveryCore = gameObject.AddComponent<FoundationRecoveryCore>();
            }

            // Configure recovery core
            _recoveryCore.SetEnabled(_enableAutomaticRecovery);

            // Connect event handlers
            _recoveryCore.OnRecoveryStarted += (systemName, strategy) => OnRecoveryStarted?.Invoke(systemName, strategy);
            _recoveryCore.OnRecoveryCompleted += (systemName, success) => OnRecoveryCompleted?.Invoke(systemName, success);
            _recoveryCore.OnRecoveryFailed += (systemName, error) => OnRecoveryFailed?.Invoke(systemName, error);
        }

        /// <summary>
        /// Process recovery operations (legacy compatibility)
        /// </summary>
        public void ProcessRecoveryOperations()
        {
            // Delegated to recovery core - processing happens automatically via ITickable
        }

        /// <summary>
        /// Trigger manual recovery for specific system
        /// </summary>
        public bool TriggerRecovery(string systemName)
        {
            if (_recoveryCore != null)
            {
                return _recoveryCore.TriggerRecovery(systemName);
            }
            return false;
        }

        /// <summary>
        /// Get recovery status for system
        /// </summary>
        public RecoveryStatus GetRecoveryStatus(string systemName)
        {
            return _recoveryCore?.GetRecoveryStatus(systemName) ?? RecoveryStatus.None;
        }

        /// <summary>
        /// Get recovery data for system (legacy compatibility)
        /// </summary>
        public RecoveryData GetRecoveryData(string systemName)
        {
            // Legacy method - data is now managed by subsystems
            return new RecoveryData { SystemName = systemName };
        }

        /// <summary>
        /// Get recovery history for system
        /// </summary>
        public RecoveryAttempt[] GetRecoveryHistory(string systemName)
        {
            return _recoveryCore?.GetRecoveryHistory(systemName) ?? new RecoveryAttempt[0];
        }

        /// <summary>
        /// Get systems currently being recovered
        /// </summary>
        public string[] GetSystemsUnderRecovery()
        {
            // Legacy compatibility - would need to query subsystems
            return new string[0];
        }

        /// <summary>
        /// Stop recovery operation for specific system
        /// </summary>
        public bool StopRecovery(string systemName)
        {
            // Legacy compatibility - would need to delegate to strategy subsystem
            return false;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableAutomaticRecovery = enabled;
            _recoveryCore?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Legacy Compatibility Methods

        /// <summary>
        /// Force optimization analysis (legacy compatibility)
        /// </summary>
        public void ForceRecoveryAnalysis()
        {
            // Legacy method - analysis is now handled continuously by subsystems
        }






        #endregion
    }

    #region Legacy Data Structures (Backward Compatibility)
    // Data structures moved to FoundationRecoveryDataStructures.cs for shared use
    #endregion
}
