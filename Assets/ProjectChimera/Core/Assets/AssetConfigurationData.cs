using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Configuration Data Structures
    /// Single Responsibility: Define all configuration data types
    /// Extracted from AddressableAssetConfigurationManager (859 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Asset manager configuration
    /// </summary>
    [Serializable]
    public struct AssetManagerConfiguration
    {
        [Header("Caching")]
        public bool EnableCaching;
        public int MaxCacheSize;
        public long MaxMemoryUsage;

        [Header("Loading")]
        public bool PreloadCriticalAssets;
        public bool EnableParallelLoading;
        public int MaxConcurrentLoads;
        public float LoadTimeoutSeconds;

        [Header("Release Management")]
        public bool AutoReleaseEnabled;
        public float AutoReleaseInterval;
        public int MaxRetainedHandles;

        [Header("Performance")]
        public bool EnableDetailedStatistics;
        public float StatisticsUpdateInterval;
        public int MaxPerformanceHistoryEntries;

        [Header("Debugging")]
        public bool EnableLogging;

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static AssetManagerConfiguration CreateDefault()
        {
            return new AssetManagerConfiguration
            {
                // Caching
                EnableCaching = true,
                MaxCacheSize = 100,
                MaxMemoryUsage = 1024L * 1024L * 512L, // 512MB

                // Loading
                PreloadCriticalAssets = true,
                EnableParallelLoading = true,
                MaxConcurrentLoads = 10,
                LoadTimeoutSeconds = 30f,

                // Release
                AutoReleaseEnabled = true,
                AutoReleaseInterval = 60f,
                MaxRetainedHandles = 100,

                // Performance
                EnableDetailedStatistics = true,
                StatisticsUpdateInterval = 1f,
                MaxPerformanceHistoryEntries = 100,

                // Debugging
                EnableLogging = false
            };
        }
    }

    /// <summary>
    /// Configuration issue severity
    /// </summary>
    public enum ConfigurationIssueSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    /// <summary>
    /// Configuration validation issue
    /// </summary>
    [Serializable]
    public struct ConfigurationIssue
    {
        public string Property;
        public string Message;
        public ConfigurationIssueSeverity Severity;

        public ConfigurationIssue(string property, string message, ConfigurationIssueSeverity severity)
        {
            Property = property;
            Message = message;
            Severity = severity;
        }
    }

    /// <summary>
    /// Configuration update result
    /// </summary>
    [Serializable]
    public struct ConfigurationUpdateResult
    {
        public bool Success;
        public string ErrorMessage;
        public AssetManagerConfiguration? PreviousConfiguration;
        public List<ConfigurationIssue> ValidationIssues;
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    [Serializable]
    public struct ConfigurationValidationResult
    {
        public List<ConfigurationIssue> Issues;
        public bool HasCriticalIssues;
        public DateTime ValidationTime;

        public ConfigurationValidationResult(List<ConfigurationIssue> issues)
        {
            Issues = issues;
            HasCriticalIssues = issues.Exists(i => i.Severity == ConfigurationIssueSeverity.Critical);
            ValidationTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Configuration statistics
    /// </summary>
    [Serializable]
    public struct ConfigurationStats
    {
        public int ConfigurationUpdates;
        public int ParameterUpdates;
        public int ProfilesCreated;
        public int ProfilesSaved;
        public int ProfilesDeleted;
        public int ProfileSwitches;
        public int ValidationAttempts;
        public int ConfigurationResets;
        public int ConfigurationSaves;
        public int ConfigurationLoads;
        public int SaveFailures;
        public int LoadFailures;

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public static ConfigurationStats CreateEmpty()
        {
            return new ConfigurationStats
            {
                ConfigurationUpdates = 0,
                ParameterUpdates = 0,
                ProfilesCreated = 0,
                ProfilesSaved = 0,
                ProfilesDeleted = 0,
                ProfileSwitches = 0,
                ValidationAttempts = 0,
                ConfigurationResets = 0,
                ConfigurationSaves = 0,
                ConfigurationLoads = 0,
                SaveFailures = 0,
                LoadFailures = 0
            };
        }
    }

    /// <summary>
    /// Configuration summary
    /// </summary>
    [Serializable]
    public struct ConfigurationSummary
    {
        public AssetManagerConfiguration CurrentConfiguration;
        public string ActiveProfile;
        public List<string> AvailableProfiles;
        public bool IsDirty;
        public bool IsValid;
        public List<ConfigurationIssue> ValidationIssues;
        public DateTime LastConfigChange;
        public ConfigurationStats Stats;
    }

    /// <summary>
    /// Profile container for serialization
    /// </summary>
    [Serializable]
    public struct ProfileContainer
    {
        public Dictionary<string, AssetManagerConfiguration> Profiles;
    }
}

