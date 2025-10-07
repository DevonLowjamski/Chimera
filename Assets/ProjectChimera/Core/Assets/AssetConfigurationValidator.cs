using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Configuration Validator
    /// Single Responsibility: Validate configuration settings
    /// Extracted from AddressableAssetConfigurationManager (859 lines → 4 files <500 lines each)
    /// </summary>
    public class AssetConfigurationValidator
    {
        /// <summary>
        /// Validate asset manager configuration
        /// </summary>
        public ConfigurationValidationResult ValidateConfiguration(AssetManagerConfiguration config)
        {
            var issues = new List<ConfigurationIssue>();
            ValidateConfiguration(config, issues);
            return new ConfigurationValidationResult(issues);
        }

        /// <summary>
        /// Validate configuration and populate issues list
        /// </summary>
        public void ValidateConfiguration(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            ValidateCachingSettings(config, issues);
            ValidateLoadingSettings(config, issues);
            ValidateReleaseSettings(config, issues);
            ValidatePerformanceSettings(config, issues);
            CheckPerformanceWarnings(config, issues);
        }

        #region Validation Methods

        /// <summary>
        /// Validate caching settings
        /// </summary>
        private void ValidateCachingSettings(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            if (config.MaxCacheSize <= 0)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxCacheSize",
                    "Cache size must be greater than 0",
                    ConfigurationIssueSeverity.Critical
                ));
            }

            if (config.MaxMemoryUsage <= 0)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxMemoryUsage",
                    "Memory usage limit must be greater than 0",
                    ConfigurationIssueSeverity.Critical
                ));
            }

            // Minimum memory requirement
            long minMemory = 1024L * 1024L * 64L; // 64MB minimum
            if (config.MaxMemoryUsage < minMemory)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxMemoryUsage",
                    $"Memory usage below recommended minimum ({minMemory / 1024L / 1024L}MB)",
                    ConfigurationIssueSeverity.Warning
                ));
            }
        }

        /// <summary>
        /// Validate loading settings
        /// </summary>
        private void ValidateLoadingSettings(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            if (config.MaxConcurrentLoads <= 0)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxConcurrentLoads",
                    "Max concurrent loads must be greater than 0",
                    ConfigurationIssueSeverity.Critical
                ));
            }

            if (config.LoadTimeoutSeconds <= 0)
            {
                issues.Add(new ConfigurationIssue(
                    "LoadTimeoutSeconds",
                    "Load timeout must be greater than 0",
                    ConfigurationIssueSeverity.Critical
                ));
            }

            // Warning for very short timeout
            if (config.LoadTimeoutSeconds < 5f)
            {
                issues.Add(new ConfigurationIssue(
                    "LoadTimeoutSeconds",
                    "Very short timeout may cause premature load failures",
                    ConfigurationIssueSeverity.Warning
                ));
            }

            // Parallel loading validation
            if (config.EnableParallelLoading && config.MaxConcurrentLoads <= 1)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxConcurrentLoads",
                    "Parallel loading enabled but max concurrent loads is 1",
                    ConfigurationIssueSeverity.Info
                ));
            }
        }

        /// <summary>
        /// Validate release management settings
        /// </summary>
        private void ValidateReleaseSettings(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            if (config.AutoReleaseEnabled)
            {
                if (config.AutoReleaseInterval <= 0)
                {
                    issues.Add(new ConfigurationIssue(
                        "AutoReleaseInterval",
                        "Auto-release interval must be greater than 0 when auto-release is enabled",
                        ConfigurationIssueSeverity.Critical
                    ));
                }

                if (config.MaxRetainedHandles <= 0)
                {
                    issues.Add(new ConfigurationIssue(
                        "MaxRetainedHandles",
                        "Max retained handles must be greater than 0",
                        ConfigurationIssueSeverity.Critical
                    ));
                }

                // Warning for very frequent auto-release
                if (config.AutoReleaseInterval < 10f)
                {
                    issues.Add(new ConfigurationIssue(
                        "AutoReleaseInterval",
                        "Very frequent auto-release may impact performance",
                        ConfigurationIssueSeverity.Warning
                    ));
                }
            }
        }

        /// <summary>
        /// Validate performance settings
        /// </summary>
        private void ValidatePerformanceSettings(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            if (config.EnableDetailedStatistics)
            {
                if (config.StatisticsUpdateInterval <= 0)
                {
                    issues.Add(new ConfigurationIssue(
                        "StatisticsUpdateInterval",
                        "Statistics update interval must be greater than 0 when detailed statistics are enabled",
                        ConfigurationIssueSeverity.Critical
                    ));
                }

                if (config.MaxPerformanceHistoryEntries <= 0)
                {
                    issues.Add(new ConfigurationIssue(
                        "MaxPerformanceHistoryEntries",
                        "Max performance history entries must be greater than 0",
                        ConfigurationIssueSeverity.Critical
                    ));
                }

                // Warning for very frequent updates
                if (config.StatisticsUpdateInterval < 0.1f)
                {
                    issues.Add(new ConfigurationIssue(
                        "StatisticsUpdateInterval",
                        "Very frequent statistics updates may impact performance",
                        ConfigurationIssueSeverity.Warning
                    ));
                }
            }
        }

        /// <summary>
        /// Check for performance-related warnings
        /// </summary>
        private void CheckPerformanceWarnings(AssetManagerConfiguration config, List<ConfigurationIssue> issues)
        {
            // Very large cache size
            if (config.MaxCacheSize > 1000)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxCacheSize",
                    "Very large cache size may impact performance",
                    ConfigurationIssueSeverity.Warning
                ));
            }

            // High concurrent load count
            if (config.MaxConcurrentLoads > 50)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxConcurrentLoads",
                    "High concurrent load count may cause thread contention",
                    ConfigurationIssueSeverity.Warning
                ));
            }

            // Very high memory usage
            long highMemory = 1024L * 1024L * 1024L * 2L; // 2GB
            if (config.MaxMemoryUsage > highMemory)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxMemoryUsage",
                    "Very high memory limit may cause memory pressure",
                    ConfigurationIssueSeverity.Warning
                ));
            }

            // Large performance history
            if (config.MaxPerformanceHistoryEntries > 1000)
            {
                issues.Add(new ConfigurationIssue(
                    "MaxPerformanceHistoryEntries",
                    "Very large performance history may increase memory usage",
                    ConfigurationIssueSeverity.Info
                ));
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if configuration has any critical issues
        /// </summary>
        public bool HasCriticalIssues(List<ConfigurationIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.Severity == ConfigurationIssueSeverity.Critical)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get issue count by severity
        /// </summary>
        public int GetIssueCount(List<ConfigurationIssue> issues, ConfigurationIssueSeverity severity)
        {
            int count = 0;
            foreach (var issue in issues)
            {
                if (issue.Severity == severity)
                    count++;
            }
            return count;
        }

        #endregion
    }
}

