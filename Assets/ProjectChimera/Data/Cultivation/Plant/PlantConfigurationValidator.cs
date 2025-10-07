using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Configuration Validator
    /// Single Responsibility: Validate plant sync configurations
    /// Extracted from PlantSyncConfigurationManager (736 lines → 4 files <500 lines each)
    /// </summary>
    public class PlantConfigurationValidator
    {
        private readonly bool _enableLogging;

        public event Action<ConfigurationValidationResult> OnValidationComplete;

        public PlantConfigurationValidator(bool enableLogging)
        {
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public ConfigurationValidationResult ValidateConfiguration(PlantSyncConfiguration config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate sync frequency
            if (config.SyncFrequency < 0.1f)
            {
                errors.Add($"Sync frequency too low: {config.SyncFrequency} (minimum: 0.1)");
            }
            else if (config.SyncFrequency > 300f)
            {
                warnings.Add($"Sync frequency very high: {config.SyncFrequency} seconds");
            }

            // Validate batch size
            if (config.BatchSize < 1)
            {
                errors.Add($"Invalid batch size: {config.BatchSize} (minimum: 1)");
            }
            else if (config.BatchSize > 1000)
            {
                warnings.Add($"Very large batch size: {config.BatchSize}");
            }

            // Validate timeout
            if (config.OperationTimeoutSeconds < 1f)
            {
                errors.Add($"Timeout too low: {config.OperationTimeoutSeconds} (minimum: 1)");
            }

            // Validate retry settings
            if (config.MaxRetryAttempts < 0 || config.MaxRetryAttempts > 10)
            {
                warnings.Add($"Unusual retry attempts: {config.MaxRetryAttempts}");
            }

            if (config.RetryDelayMultiplier < 1.0f)
            {
                warnings.Add($"Retry delay multiplier should be >= 1.0, got {config.RetryDelayMultiplier}");
            }

            // Validate performance alert threshold
            if (config.EnablePerformanceTracking && config.PerformanceAlertThreshold <= 0)
            {
                warnings.Add("Performance tracking enabled but alert threshold is invalid");
            }

            var result = new ConfigurationValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                ValidationTime = DateTime.Now
            };

            OnValidationComplete?.Invoke(result);

            if (_enableLogging && !result.IsValid)
            {
                ChimeraLogger.LogWarning("PLANT",
                    $"Configuration validation failed with {errors.Count} errors",
                    null);
            }

            return result;
        }
    }
}

