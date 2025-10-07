// REFACTORED: Data Structures
// Extracted from AddressableAssetLoadingEngine.cs for better separation of concerns

using UnityEngine;

namespace ProjectChimera.Core.Assets
{
    public struct LoadingOperation
    {
        public string Address;
        public DateTime StartTime;
        public bool IsActive;
    }

    public struct LoadRequest
    {
        public string Address;
        public Type AssetType;
        public System.Action<object> OnComplete;
        public System.Action<string> OnError;
        public DateTime RequestTime;
    }

    public struct LoadingStats
    {
        public int LoadsAttempted;
        public int LoadsSucceeded;
        public int LoadsFailed;
        public int LoadsCancelled;
        public int LoadsTimedOut;
        public int BatchLoadsCompleted;
        public int BatchLoadsFailed;
        public int AssetsReleased;
        public float TotalLoadTime;

        public readonly float SuccessRate => LoadsAttempted > 0 ? (float)LoadsSucceeded / LoadsAttempted : 0f;
        public readonly float AverageLoadTime => LoadsSucceeded > 0 ? TotalLoadTime / LoadsSucceeded : 0f;
    }

    public struct LoadingOperationInfo
    {
        public string Address;
        public DateTime StartTime;
        public float Progress;
        public float ElapsedTime;
    }

    public struct LoadingSummary
    {
        public List<LoadingOperationInfo> ActiveOperations;
        public int QueuedOperations;
        public LoadingStats Stats;
        public bool IsInitialized;
        public int MaxConcurrentLoads;
    }

}
