using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Time Estimation Tracker
    /// Single Responsibility: Track statistics and accuracy of time estimates
    /// Extracted from TimeEstimationEngine (866 lines → 4 files <500 lines each)
    /// </summary>
    public class TimeEstimationTracker
    {
        private TimeEstimationStats _stats = new TimeEstimationStats();
        private readonly List<TimeEstimateAccuracy> _accuracyHistory = new List<TimeEstimateAccuracy>();
        private readonly int _maxHistorySize;
        private readonly float _accuracyTolerancePercent;

        public TimeEstimationStats Stats => _stats;
        public float CurrentAccuracyRate => CalculateCurrentAccuracyRate();

        public event Action<float> OnEstimateAccuracyUpdated;

        public TimeEstimationTracker(int maxHistorySize = 100, float accuracyTolerancePercent = 0.15f)
        {
            _maxHistorySize = maxHistorySize;
            _accuracyTolerancePercent = accuracyTolerancePercent;
            ResetStats();
        }

        /// <summary>
        /// Track accuracy of a time estimate
        /// </summary>
        public void TrackEstimateAccuracy(
            MalfunctionType type,
            MalfunctionSeverity severity,
            EquipmentType equipmentType,
            TimeSpan estimatedTime,
            TimeSpan actualTime)
        {
            var accuracy = CalculateAccuracy(estimatedTime, actualTime);

            var accuracyEntry = new TimeEstimateAccuracy
            {
                MalfunctionType = type,
                Severity = severity,
                EquipmentType = equipmentType,
                EstimatedTime = estimatedTime,
                ActualTime = actualTime,
                AccuracyPercent = accuracy,
                Timestamp = DateTime.Now
            };

            _accuracyHistory.Add(accuracyEntry);

            // Maintain history size
            if (_accuracyHistory.Count > _maxHistorySize)
            {
                _accuracyHistory.RemoveAt(0);
            }

            // Update accuracy stats
            UpdateAccuracyStats(accuracy);

            OnEstimateAccuracyUpdated?.Invoke(accuracy);
        }

        /// <summary>
        /// Update time estimation statistics
        /// </summary>
        public void UpdateTimeEstimationStats(float estimationTime, float estimatedMinutes)
        {
            _stats.TotalEstimationTime += estimationTime;
            _stats.AverageEstimationTime = _stats.TimeEstimatesGenerated > 0
                ? _stats.TotalEstimationTime / _stats.TimeEstimatesGenerated
                : 0f;

            if (estimationTime > _stats.MaxEstimationTime)
                _stats.MaxEstimationTime = estimationTime;

            _stats.TotalEstimatedTime += estimatedMinutes;
            _stats.AverageEstimatedTime = _stats.TimeEstimatesGenerated > 0
                ? _stats.TotalEstimatedTime / _stats.TimeEstimatesGenerated
                : 0f;
        }

        /// <summary>
        /// Increment time estimate counter
        /// </summary>
        public void IncrementTimeEstimatesGenerated()
        {
            _stats.TimeEstimatesGenerated++;
        }

        /// <summary>
        /// Increment diagnostic estimate counter
        /// </summary>
        public void IncrementDiagnosticEstimatesGenerated()
        {
            _stats.DiagnosticEstimatesGenerated++;
        }

        /// <summary>
        /// Increment time breakdown counter
        /// </summary>
        public void IncrementTimeBreakdownsGenerated()
        {
            _stats.TimeBreakdownsGenerated++;
        }

        /// <summary>
        /// Increment estimates without base data counter
        /// </summary>
        public void IncrementEstimatesWithoutBaseData()
        {
            _stats.EstimatesWithoutBaseData++;
        }

        /// <summary>
        /// Increment estimation errors counter
        /// </summary>
        public void IncrementEstimationErrors()
        {
            _stats.EstimationErrors++;
        }

        /// <summary>
        /// Get recent accuracy history
        /// </summary>
        public IEnumerable<TimeEstimateAccuracy> GetRecentAccuracyHistory(int count)
        {
            return _accuracyHistory.TakeLast(count);
        }

        /// <summary>
        /// Get accuracy history for specific malfunction type
        /// </summary>
        public IEnumerable<TimeEstimateAccuracy> GetAccuracyHistoryForType(MalfunctionType type)
        {
            return _accuracyHistory.Where(a => a.MalfunctionType == type);
        }

        /// <summary>
        /// Get average accuracy for malfunction type
        /// </summary>
        public float GetAverageAccuracyForType(MalfunctionType type)
        {
            var typeHistory = GetAccuracyHistoryForType(type).ToList();
            if (typeHistory.Count == 0) return 0f;

            float sum = 0f;
            foreach (var entry in typeHistory)
            {
                sum += entry.AccuracyPercent;
            }
            return sum / typeHistory.Count;
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStats()
        {
            _stats = new TimeEstimationStats
            {
                TimeEstimatesGenerated = 0,
                DiagnosticEstimatesGenerated = 0,
                TimeBreakdownsGenerated = 0,
                EstimatesWithoutBaseData = 0,
                EstimationErrors = 0,
                TotalEstimationTime = 0f,
                AverageEstimationTime = 0f,
                MaxEstimationTime = 0f,
                TotalEstimatedTime = 0f,
                AverageEstimatedTime = 0f,
                AccuracyTrackingCount = 0,
                TotalAccuracyScore = 0f,
                AverageAccuracy = 0f,
                BestAccuracy = 0f,
                WorstAccuracy = 0f
            };
        }

        /// <summary>
        /// Clear accuracy history
        /// </summary>
        public void ClearAccuracyHistory()
        {
            _accuracyHistory.Clear();
        }

        #region Private Methods

        /// <summary>
        /// Calculate accuracy percentage
        /// </summary>
        private float CalculateAccuracy(TimeSpan estimated, TimeSpan actual)
        {
            if (actual.TotalMinutes == 0) return 0f;

            var difference = Math.Abs(estimated.TotalMinutes - actual.TotalMinutes);
            var percentageError = difference / actual.TotalMinutes;

            return Mathf.Max(0f, 1f - (float)percentageError);
        }

        /// <summary>
        /// Calculate current accuracy rate from recent history
        /// </summary>
        private float CalculateCurrentAccuracyRate()
        {
            if (_accuracyHistory.Count == 0) return 0f;

            var recentEntries = _accuracyHistory.TakeLast(20).ToList();
            if (recentEntries.Count == 0) return 0f;

            float sum = 0f;
            foreach (var entry in recentEntries)
            {
                sum += entry.AccuracyPercent;
            }
            return sum / recentEntries.Count;
        }

        /// <summary>
        /// Update accuracy statistics
        /// </summary>
        private void UpdateAccuracyStats(float accuracy)
        {
            _stats.TotalAccuracyScore += accuracy;
            _stats.AccuracyTrackingCount++;
            _stats.AverageAccuracy = _stats.TotalAccuracyScore / _stats.AccuracyTrackingCount;

            if (accuracy > _stats.BestAccuracy)
                _stats.BestAccuracy = accuracy;

            if (accuracy < _stats.WorstAccuracy || _stats.WorstAccuracy == 0f)
                _stats.WorstAccuracy = accuracy;
        }

        #endregion

        #region Analysis Methods

        /// <summary>
        /// Get accuracy trend (positive = improving, negative = declining)
        /// </summary>
        public float GetAccuracyTrend()
        {
            if (_accuracyHistory.Count < 10) return 0f;

            var recent = _accuracyHistory.TakeLast(10).ToList();
            var older = _accuracyHistory.TakeLast(20).Take(10).ToList();

            if (older.Count == 0) return 0f;

            float recentSum = 0f;
            foreach (var entry in recent)
            {
                recentSum += entry.AccuracyPercent;
            }
            float recentAvg = recentSum / recent.Count;

            float olderSum = 0f;
            foreach (var entry in older)
            {
                olderSum += entry.AccuracyPercent;
            }
            float olderAvg = olderSum / older.Count;

            return recentAvg - olderAvg;
        }

        /// <summary>
        /// Check if estimates are within tolerance
        /// </summary>
        public bool AreEstimatesAccurate()
        {
            if (_accuracyHistory.Count < 5) return true; // Not enough data

            var recentAccuracy = CalculateCurrentAccuracyRate();
            return recentAccuracy >= (1f - _accuracyTolerancePercent);
        }

        /// <summary>
        /// Get performance summary
        /// </summary>
        public string GetPerformanceSummary()
        {
            var summary = $"Time Estimation Performance:\n";
            summary += $"Total Estimates: {_stats.TimeEstimatesGenerated}\n";
            summary += $"Average Accuracy: {_stats.AverageAccuracy:P1}\n";
            summary += $"Current Accuracy: {CurrentAccuracyRate:P1}\n";
            summary += $"Trend: {GetAccuracyTrend():+0.00;-0.00;0.00}\n";
            summary += $"Errors: {_stats.EstimationErrors}";
            return summary;
        }

        #endregion
    }
}

