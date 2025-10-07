using UnityEngine;
using System;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// Recovery strategy enumeration
    /// </summary>
    public enum RecoveryStrategy
    {
        Reinitialize,
        Restart,
        DependencyRecovery,
        GracefulDegradation
    }

    /// <summary>
    /// Recovery status enumeration
    /// </summary>
    public enum RecoveryStatus
    {
        None,
        InProgress,
        Successful,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Recovery trigger enumeration
    /// </summary>
    public enum RecoveryTrigger
    {
        Manual,
        HealthAlert,
        CriticalFailure,
        HealthCheck,
        Queued
    }

    /// <summary>
    /// Recovery data structure
    /// </summary>
    [Serializable]
    public struct RecoveryData
    {
        public string SystemName;
        public RecoveryStatus Status;
        public RecoveryStrategy CurrentStrategy;
        public RecoveryTrigger Trigger;
        public int AttemptCount;
        public float FirstAttemptTime;
        public float LastAttemptTime;
        public float LastCompletionTime;
    }

    /// <summary>
    /// Recovery attempt record
    /// </summary>
    [Serializable]
    public struct RecoveryAttempt
    {
        public string SystemName;
        public RecoveryStrategy Strategy;
        public float AttemptTime;
        public bool Success;
        public float Duration;
        public string ErrorMessage;
    }

    /// <summary>
    /// Recovery manager statistics
    /// </summary>
    [Serializable]
    public struct RecoveryManagerStats
    {
        public int RecoveryAttempts;
        public int SuccessfulRecoveries;
        public int FailedRecoveries;
        public int ActiveRecoveries;
        public float RecoverySuccessRate;
        public int SystemsDegraded;
        public float LastUpdateTime;
    }
}
