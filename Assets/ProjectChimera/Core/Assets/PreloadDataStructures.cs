using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// PHASE 0 REFACTORED: Preload Data Structures
    /// Single Responsibility: Define all asset preload data types
    /// Extracted from AddressableAssetPreloader (691 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Preload priority levels
    /// </summary>
    public enum PreloadPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Preload asset information
    /// </summary>
    [Serializable]
    public struct PreloadAssetInfo
    {
        public string Address;
        public PreloadPriority Priority;
        public DateTime AddedTime;

        /// <summary>
        /// Create preload asset info
        /// </summary>
        public static PreloadAssetInfo Create(string address, PreloadPriority priority)
        {
            return new PreloadAssetInfo
            {
                Address = address,
                Priority = priority,
                AddedTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Preload configuration
    /// </summary>
    [Serializable]
    public struct PreloadConfiguration
    {
        public List<string> CriticalAssets;
        public bool PreloadInParallel;
        public int MaxConcurrentPreloads;
        public float TimeoutSeconds;

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static PreloadConfiguration CreateDefault()
        {
            return new PreloadConfiguration
            {
                CriticalAssets = new List<string>(),
                PreloadInParallel = true,
                MaxConcurrentPreloads = 5,
                TimeoutSeconds = 60f
            };
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public readonly bool IsValid()
        {
            return CriticalAssets != null &&
                   MaxConcurrentPreloads > 0 &&
                   TimeoutSeconds > 0f;
        }
    }

    /// <summary>
    /// Preload progress tracking
    /// </summary>
    [Serializable]
    public struct PreloadProgress
    {
        public int TotalAssets;
        public int CompletedAssets;
        public string CurrentAsset;
        public float OverallProgress;

        /// <summary>
        /// Calculate completion percentage
        /// </summary>
        public readonly float CompletionPercentage => TotalAssets > 0 ? (float)CompletedAssets / TotalAssets * 100f : 0f;

        /// <summary>
        /// Check if complete
        /// </summary>
        public readonly bool IsComplete => CompletedAssets >= TotalAssets;

        /// <summary>
        /// Create progress
        /// </summary>
        public static PreloadProgress Create(int totalAssets)
        {
            return new PreloadProgress
            {
                TotalAssets = totalAssets,
                CompletedAssets = 0,
                CurrentAsset = string.Empty,
                OverallProgress = 0f
            };
        }
    }

    /// <summary>
    /// Preload failure information
    /// </summary>
    [Serializable]
    public struct PreloadFailure
    {
        public string Address;
        public string ErrorMessage;

        /// <summary>
        /// Create failure record
        /// </summary>
        public static PreloadFailure Create(string address, string errorMessage)
        {
            return new PreloadFailure
            {
                Address = address,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Preload operation result
    /// </summary>
    [Serializable]
    public struct PreloadResult
    {
        public bool Success;
        public string ErrorMessage;
        public int TotalAssets;
        public int SuccessfulAssets;
        public int FailedAssets;
        public List<PreloadFailure> Failures;
        public float PreloadTime;

        /// <summary>
        /// Calculate success rate
        /// </summary>
        public readonly float SuccessRate => TotalAssets > 0 ? (float)SuccessfulAssets / TotalAssets : 0f;

        /// <summary>
        /// Check if all assets loaded
        /// </summary>
        public readonly bool IsFullSuccess => Success && FailedAssets == 0;

        /// <summary>
        /// Create successful result
        /// </summary>
        public static PreloadResult CreateSuccess(int totalAssets, int successfulAssets, float preloadTime)
        {
            return new PreloadResult
            {
                Success = true,
                TotalAssets = totalAssets,
                SuccessfulAssets = successfulAssets,
                FailedAssets = totalAssets - successfulAssets,
                Failures = new List<PreloadFailure>(),
                PreloadTime = preloadTime
            };
        }

        /// <summary>
        /// Create failure result
        /// </summary>
        public static PreloadResult CreateFailure(string errorMessage)
        {
            return new PreloadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                TotalAssets = 0,
                SuccessfulAssets = 0,
                FailedAssets = 0,
                Failures = new List<PreloadFailure>(),
                PreloadTime = 0f
            };
        }
    }

    /// <summary>
    /// Preload statistics
    /// </summary>
    [Serializable]
    public struct PreloadStats
    {
        public int PreloadAttempts;
        public int SuccessfulPreloads;
        public int FailedPreloads;
        public int RetryAttempts;
        public float TotalPreloadTime;
        public int TotalAssetsPreloaded;

        public float AveragePreloadTime => SuccessfulPreloads > 0 ? TotalPreloadTime / SuccessfulPreloads : 0f;
        public float SuccessRate => PreloadAttempts > 0 ? (float)SuccessfulPreloads / PreloadAttempts : 0f;

        /// <summary>
        /// Create empty stats
        /// </summary>
        public static PreloadStats CreateEmpty()
        {
            return new PreloadStats
            {
                PreloadAttempts = 0,
                SuccessfulPreloads = 0,
                FailedPreloads = 0,
                RetryAttempts = 0,
                TotalPreloadTime = 0f,
                TotalAssetsPreloaded = 0
            };
        }
    }

    /// <summary>
    /// Comprehensive preload summary
    /// </summary>
    [Serializable]
    public struct PreloadSummary
    {
        public bool IsEnabled;
        public bool IsPreloading;
        public int TotalPreloadAssets;
        public int PreloadedAssets;
        public int FailedAssets;
        public PreloadProgress CurrentProgress;
        public PreloadStats Stats;
        public Dictionary<PreloadPriority, int> AssetsByPriority;
        public PreloadConfiguration Configuration;

        /// <summary>
        /// Get overall completion percentage
        /// </summary>
        public readonly float CompletionPercentage => TotalPreloadAssets > 0
            ? (float)PreloadedAssets / TotalPreloadAssets * 100f
            : 0f;
    }
#endif
}

