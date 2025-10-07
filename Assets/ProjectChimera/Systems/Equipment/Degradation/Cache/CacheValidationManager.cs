using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    /// <summary>
    /// REFACTORED: Cache Validation Manager - Focused cache accuracy validation and quality assurance
    /// Single Responsibility: Managing cache validation, accuracy tracking, and quality metrics
    /// Extracted from EstimateCacheManager for better SRP compliance
    /// </summary>
    public class CacheValidationManager : ITickable
    {
        private readonly bool _enableLogging;
        private readonly bool _enableValidation;
        private readonly float _validationInterval;
        private readonly float _accuracyThreshold;
        private readonly int _validationSampleSize;

        // Validation tracking
        private readonly Dictionary<string, ValidationRecord> _validationHistory = new Dictionary<string, ValidationRecord>();
        private readonly Queue<ValidationSample> _recentValidations = new Queue<ValidationSample>();
        private ValidationStatistics _validationStats = new ValidationStatistics();

        // Timing
        private float _lastValidationTime;

        // Dependencies
        private CacheStorageManager _storageManager;

        // Events
        public event System.Action<ValidationResult> OnValidationCompleted;
        public event System.Action<string, float> OnAccuracyBelowThreshold;
        public event System.Action<ValidationStatistics> OnStatisticsUpdated;

        public CacheValidationManager(bool enableLogging = false, bool enableValidation = true,
                                    float validationInterval = 1800f, float accuracyThreshold = 0.85f,
                                    int validationSampleSize = 20)
        {
            _enableLogging = enableLogging;
            _enableValidation = enableValidation;
            _validationInterval = validationInterval;
            _accuracyThreshold = accuracyThreshold;
            _validationSampleSize = validationSampleSize;
        }

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheValidation;
        public bool IsTickable => _enableValidation && _storageManager != null;

        #region Initialization

        /// <summary>
        /// Initialize with storage manager dependency
        /// </summary>
        public void Initialize(CacheStorageManager storageManager)
        {
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _lastValidationTime = Time.time;

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_VAL", "Cache validation manager initialized", null);
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastValidationTime >= _validationInterval)
            {
                PerformValidation();
                _lastValidationTime = Time.time;
            }
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_VAL", "Cache validation manager registered with UpdateOrchestrator", null);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_VAL", "Cache validation manager unregistered from UpdateOrchestrator", null);
        }

        #endregion

        #region Validation Operations

        /// <summary>
        /// Perform comprehensive cache validation
        /// </summary>
        public ValidationResult PerformValidation()
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new ValidationResult { StartTime = DateTime.Now };

            try
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("CACHE_VAL", "Starting cache validation", null);

                // Get sample of cached estimates for validation
                var sampleKeys = SelectValidationSample();
                result.SampleSize = sampleKeys.Count;

                if (sampleKeys.Count == 0)
                {
                    result.Success = true;
                    result.Message = "No items available for validation";
                    return result;
                }

                // Validate each sample
                var validationResults = new List<SampleValidationResult>();
                foreach (var key in sampleKeys)
                {
                    var sampleResult = ValidateSample(key);
                    if (sampleResult.HasValue)
                    {
                        validationResults.Add(sampleResult.Value);
                    }
                }

                // Calculate overall results
                result.ValidatedSamples = validationResults.Count;
                result.AccurateEstimates = validationResults.Count(r => r.IsAccurate);
                result.OverallAccuracy = validationResults.Count > 0
                    ? (float)result.AccurateEstimates / validationResults.Count
                    : 0f;

                result.AverageErrorMargin = validationResults.Count > 0
                    ? validationResults.Average(r => r.ErrorMargin)
                    : 0f;

                result.Success = true;
                result.ExecutionTime = Time.realtimeSinceStartup - startTime;

                // Update statistics
                UpdateValidationStatistics(result);

                // Check accuracy threshold
                if (result.OverallAccuracy < _accuracyThreshold)
                {
                    OnAccuracyBelowThreshold?.Invoke("Overall cache accuracy", result.OverallAccuracy);
                }

                OnValidationCompleted?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CACHE_VAL",
                        $"Validation completed: {result.OverallAccuracy:F2} accuracy, {result.ValidatedSamples} samples in {result.ExecutionTime:F2}s", null);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                ChimeraLogger.LogError("CACHE_VAL", $"Validation failed: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Validate a specific cached estimate
        /// </summary>
        public SampleValidationResult ValidateEstimate(string key)
        {
            return ValidateSample(key) ?? new SampleValidationResult
            {
                Key = key,
                IsValid = false,
                ValidationMessage = "Sample validation failed",
                IsAccurate = false,
                ValidationTime = DateTime.Now
            };
        }

        /// <summary>
        /// Validate a sample estimate
        /// </summary>
        private SampleValidationResult? ValidateSample(string key)
        {
            try
            {
                var cachedEstimate = _storageManager.GetEstimate(key);
                if (cachedEstimate == null)
                    return null;

                // Recalculate estimate for comparison
                var recalculatedEstimate = RecalculateEstimate(cachedEstimate);
                if (recalculatedEstimate == null)
                    return null;

                // Compare estimates
                var errorMargin = Math.Abs(cachedEstimate.Cost - recalculatedEstimate.Cost) / Math.Max(cachedEstimate.Cost, 0.01f);
                var isAccurate = errorMargin <= (1f - _accuracyThreshold);

                var result = new SampleValidationResult
                {
                    Key = key,
                    CachedCost = cachedEstimate.Cost,
                    RecalculatedCost = recalculatedEstimate.Cost,
                    ErrorMargin = errorMargin,
                    IsAccurate = isAccurate,
                    ValidationTime = DateTime.Now,
                    IsValid = isAccurate,
                    ValidationMessage = isAccurate ? "Validation passed" : $"Validation failed - Error margin: {errorMargin:F2}"
                };

                // Record validation
                RecordValidation(key, result);

                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE_VAL", $"Error validating sample {key}: {ex.Message}", null);
                return null;
            }
        }

        /// <summary>
        /// Recalculate an estimate for validation comparison
        /// </summary>
        private CachedEstimate RecalculateEstimate(CachedEstimate originalEstimate)
        {
            // This would interface with the actual cost calculation engine
            // For now, we'll simulate recalculation with some variation
            var variationFactor = UnityEngine.Random.Range(0.95f, 1.05f); // 5% variation simulation

            return new CachedEstimate
            {
                EstimateKey = originalEstimate.EstimateKey,
                Cost = originalEstimate.Cost * variationFactor,
                Confidence = originalEstimate.Confidence,
                Timestamp = Time.time,
                Parameters = originalEstimate.Parameters,
                EquipmentType = originalEstimate.EquipmentType,
                MalfunctionType = originalEstimate.MalfunctionType
            };
        }

        #endregion

        #region Sample Selection

        /// <summary>
        /// Select a representative sample for validation
        /// </summary>
        private List<string> SelectValidationSample()
        {
            var allKeys = _storageManager.GetAllKeys().ToList();

            if (allKeys.Count <= _validationSampleSize)
                return allKeys;

            // Use stratified sampling to get diverse samples
            return PerformStratifiedSampling(allKeys);
        }

        /// <summary>
        /// Perform stratified sampling for better validation coverage
        /// </summary>
        private List<string> PerformStratifiedSampling(List<string> allKeys)
        {
            var sample = new List<string>();

            // Group by equipment type and malfunction type
            var groupedKeys = GroupKeysByAttributes(allKeys);

            var samplesPerGroup = Math.Max(1, _validationSampleSize / groupedKeys.Count);
            var extraSamples = _validationSampleSize % groupedKeys.Count;

            foreach (var group in groupedKeys)
            {
                var groupSampleSize = samplesPerGroup + (extraSamples > 0 ? 1 : 0);
                if (extraSamples > 0) extraSamples--;

                var groupSample = group.Value.OrderBy(x => Guid.NewGuid()).Take(groupSampleSize);
                sample.AddRange(groupSample);
            }

            return sample.Take(_validationSampleSize).ToList();
        }

        /// <summary>
        /// Group keys by equipment and malfunction attributes
        /// </summary>
        private Dictionary<string, List<string>> GroupKeysByAttributes(List<string> keys)
        {
            var groups = new Dictionary<string, List<string>>();

            foreach (var key in keys)
            {
                var estimate = _storageManager.GetEstimate(key);
                if (estimate != null)
                {
                    var groupKey = $"{estimate.EquipmentType}_{estimate.MalfunctionType}";
                    if (!groups.ContainsKey(groupKey))
                    {
                        groups[groupKey] = new List<string>();
                    }
                    groups[groupKey].Add(key);
                }
            }

            return groups;
        }

        #endregion

        #region Validation Recording

        /// <summary>
        /// Record validation result for tracking
        /// </summary>
        private void RecordValidation(string key, SampleValidationResult result)
        {
            // Update validation history for this key
            if (!_validationHistory.ContainsKey(key))
            {
                _validationHistory[key] = new ValidationRecord();
            }

            var record = _validationHistory[key];
            record.ValidationCount++;
            record.LastValidationTime = result.ValidationTime;
            record.TotalErrorMargin += result.ErrorMargin;
            record.AverageErrorMargin = record.TotalErrorMargin / record.ValidationCount;

            if (result.IsAccurate)
                record.AccurateValidations++;

            record.AccuracyRate = (float)record.AccurateValidations / record.ValidationCount;

            // Add to recent validations queue
            var validationSample = new ValidationSample
            {
                Key = key,
                ErrorMargin = result.ErrorMargin,
                IsAccurate = result.IsAccurate,
                Timestamp = result.ValidationTime
            };

            _recentValidations.Enqueue(validationSample);

            // Maintain queue size
            while (_recentValidations.Count > _validationSampleSize * 10) // Keep 10x sample size
            {
                _recentValidations.Dequeue();
            }
        }

        #endregion

        #region Statistics and Analysis

        /// <summary>
        /// Update validation statistics
        /// </summary>
        private void UpdateValidationStatistics(ValidationResult result)
        {
            _validationStats.TotalValidations++;
            _validationStats.TotalSamplesValidated += result.ValidatedSamples;
            _validationStats.TotalAccurateSamples += result.AccurateEstimates;
            _validationStats.LastValidationTime = result.StartTime;

            // Calculate running averages
            _validationStats.AverageAccuracy = _validationStats.TotalSamplesValidated > 0
                ? (float)_validationStats.TotalAccurateSamples / _validationStats.TotalSamplesValidated
                : 0f;

            _validationStats.RecentAccuracy = CalculateRecentAccuracy();

            OnStatisticsUpdated?.Invoke(_validationStats);
        }

        /// <summary>
        /// Calculate accuracy from recent validations
        /// </summary>
        private float CalculateRecentAccuracy()
        {
            var recentSamples = _recentValidations.ToList();
            if (recentSamples.Count == 0)
                return 0f;

            var accurateCount = recentSamples.Count(s => s.IsAccurate);
            return (float)accurateCount / recentSamples.Count;
        }

        /// <summary>
        /// Get validation statistics
        /// </summary>
        public ValidationStatistics GetValidationStatistics()
        {
            return _validationStats;
        }

        /// <summary>
        /// Get validation history for a specific key
        /// </summary>
        public ValidationRecord GetValidationHistory(string key)
        {
            return _validationHistory.TryGetValue(key, out var record) ? record : null;
        }

        /// <summary>
        /// Get all validation records
        /// </summary>
        public Dictionary<string, ValidationRecord> GetAllValidationRecords()
        {
            return new Dictionary<string, ValidationRecord>(_validationHistory);
        }

        /// <summary>
        /// Get accuracy trend over time
        /// </summary>
        public AccuracyTrend GetAccuracyTrend()
        {
            var recentSamples = _recentValidations.ToList();
            if (recentSamples.Count < 10)
            {
                return new AccuracyTrend { Trend = TrendDirection.Stable, Confidence = 0f };
            }

            // Split into two halves for trend analysis
            var firstHalf = recentSamples.Take(recentSamples.Count / 2);
            var secondHalf = recentSamples.Skip(recentSamples.Count / 2);

            var firstHalfAccuracy = firstHalf.Count(s => s.IsAccurate) / (float)firstHalf.Count();
            var secondHalfAccuracy = secondHalf.Count(s => s.IsAccurate) / (float)secondHalf.Count();

            var difference = secondHalfAccuracy - firstHalfAccuracy;
            var threshold = 0.05f; // 5% threshold for trend detection

            TrendDirection trend;
            if (difference > threshold)
                trend = TrendDirection.Improving;
            else if (difference < -threshold)
                trend = TrendDirection.Declining;
            else
                trend = TrendDirection.Stable;

            return new AccuracyTrend
            {
                Trend = trend,
                Confidence = Math.Abs(difference),
                FirstPeriodAccuracy = firstHalfAccuracy,
                SecondPeriodAccuracy = secondHalfAccuracy
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force immediate validation
        /// </summary>
        public ValidationResult ForceValidation()
        {
            return PerformValidation();
        }

        /// <summary>
        /// Clear validation history
        /// </summary>
        public void ClearValidationHistory()
        {
            _validationHistory.Clear();
            _recentValidations.Clear();
            _validationStats = new ValidationStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_VAL", "Validation history cleared", null);
        }

        /// <summary>
        /// Get items with poor accuracy
        /// </summary>
        public IEnumerable<string> GetPoorAccuracyItems(float threshold = 0.7f)
        {
            return _validationHistory
                .Where(kvp => kvp.Value.AccuracyRate < threshold)
                .Select(kvp => kvp.Key);
        }

        #endregion
    }

    /// <summary>
    /// Validation result information
    /// </summary>
    [System.Serializable]
    }
