using System;
using System.Collections.Generic;
#if UNITY_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Release Data Structures
    /// Single Responsibility: Define all data types related to asset release management
    /// Extracted from AddressableAssetReleaseManager for better SRP compliance
    /// </summary>

    /// <summary>
    /// Asset handle tracking information
    /// </summary>
    [Serializable]
    public struct AssetHandle
    {
        public string Address;
        public AsyncOperationHandle Handle;
        public Type AssetType;
        public DateTime RegistrationTime;
        public int ReferenceCount;
    }

    /// <summary>
    /// Release policy configuration
    /// </summary>
    [Serializable]
    public struct ReleasePolicy
    {
        public TimeSpan MaxAge;
        public bool ProtectedByDefault;
        public int MaxReferences;
    }

    /// <summary>
    /// Release operation types
    /// </summary>
    public enum ReleaseOperationType
    {
        Manual = 0,
        AgedAssets = 1,
        LeastRecentlyUsed = 2,
        ByType = 3,
        All = 4,
        MemoryPressure = 5
    }

    /// <summary>
    /// Release operation tracking
    /// </summary>
    [Serializable]
    public struct ReleaseOperation
    {
        public ReleaseOperationType OperationType;
        public DateTime StartTime;
        public DateTime CompletionTime;
        public float Duration;
        public int ReleasedAssets;
        public int FailedReleases;
        public bool Success;
        public string ErrorMessage;
    }

    /// <summary>
    /// Release statistics
    /// </summary>
    [Serializable]
    public struct ReleaseStats
    {
        public int HandlesRegistered;
        public int HandlesReleased;
        public int ReleaseErrors;
        public int AgedReleaseOperations;
        public int LRUReleaseOperations;
        public int FullReleaseOperations;
        public int AutoReleaseOperations;
        public float TotalReleaseTime;

        public readonly float ReleaseSuccessRate => HandlesRegistered > 0 ? (float)(HandlesRegistered - ReleaseErrors) / HandlesRegistered : 0f;
        public readonly float AverageReleaseTime => HandlesReleased > 0 ? TotalReleaseTime / HandlesReleased : 0f;
    }

    /// <summary>
    /// Release summary information
    /// </summary>
    [Serializable]
    public struct ReleaseSummary
    {
        public int TotalHandles;
        public int ProtectedAssets;
        public Dictionary<Type, int> HandlesByType;
        public ReleaseStats Stats;
        public bool AutoReleaseEnabled;
        public DateTime LastAutoRelease;
        public string OldestAsset;
        public string NewestAsset;
        public float AverageHandleAge;
    }
#endif
}

