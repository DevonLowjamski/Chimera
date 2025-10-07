using UnityEngine;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Time Estimation Engine Coordinator
    /// Single Responsibility: Orchestrate time estimation components
    /// BEFORE: 866 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (TimeEstimationData, RepairTimeCalculator, TimeEstimationTracker, this coordinator)
    /// </summary>
    public class TimeEstimationEngine : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private RepairTimeParameters _parameters = new RepairTimeParameters();

        [Header("Tracking Settings")]
        [SerializeField] private bool _trackEstimateAccuracy = true;
        [SerializeField] private int _maxHistorySize = 100;
        [SerializeField] private float _accuracyTolerancePercent = 0.15f;

        // PHASE 0: Component-based architecture (SRP)
        private TimeEstimationData _data;
        private RepairTimeCalculator _calculator;
        private TimeEstimationTracker _tracker;

        private bool _isInitialized = false;

        // Events
        public event Action<TimeEstimate> OnTimeEstimateGenerated;
        public event Action<MalfunctionType, float> OnBaseTimeUpdated;
        public event Action<float> OnEstimateAccuracyUpdated;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public TimeEstimationStats Stats => _tracker?.Stats ?? new TimeEstimationStats();
        public float CurrentAccuracyRate => _tracker?.CurrentAccuracyRate ?? 0f;

        /// <summary>
        /// Initialize time estimation engine
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _data = new TimeEstimationData();
            _data.Initialize();

            _calculator = new RepairTimeCalculator(_data, _parameters);

            _tracker = new TimeEstimationTracker(_maxHistorySize, _accuracyTolerancePercent);
            _tracker.OnEstimateAccuracyUpdated += HandleAccuracyUpdated;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Time Estimation Engine initialized", this);
            }
        }

        /// <summary>
        /// Estimate repair time for malfunction
        /// </summary>
        public TimeSpan EstimateRepairTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType, bool requiresSpecialist = false)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot estimate repair time - engine not initialized", this);
                }
                return TimeSpan.Zero;
            }

            var estimationStartTime = Time.realtimeSinceStartup;

            try
            {
                // Calculate repair time
                var timeSpan = _calculator.CalculateRepairTime(type, severity, equipmentType, requiresSpecialist);

                // Update statistics
                var estimationTime = Time.realtimeSinceStartup - estimationStartTime;
                _tracker.UpdateTimeEstimationStats(estimationTime, (float)timeSpan.TotalMinutes);
                _tracker.IncrementTimeEstimatesGenerated();

                // Get confidence
                var baseTimeData = _data.GetBaseTimeData(type);
                var confidence = _calculator.CalculateTimeEstimateConfidence(type, equipmentType, baseTimeData);

                // Fire event
                OnTimeEstimateGenerated?.Invoke(new TimeEstimate
                {
                    MalfunctionType = type,
                    Severity = severity,
                    EquipmentType = equipmentType,
                    RequiresSpecialist = requiresSpecialist,
                    EstimatedTime = timeSpan,
                    EstimationTimestamp = DateTime.Now,
                    Confidence = confidence
                });

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Estimated repair time for {type} ({severity}): {timeSpan.TotalHours:F1}h", this);
                }

                return timeSpan;
            }
            catch (Exception ex)
            {
                _tracker.IncrementEstimationErrors();

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error estimating repair time: {ex.Message}", this);
                }

                return TimeSpan.FromMinutes(60); // Default 1 hour fallback
            }
        }

        /// <summary>
        /// Estimate diagnostic time for malfunction
        /// </summary>
        public TimeSpan EstimateDiagnosticTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType = EquipmentType.Generic)
        {
            if (!_isInitialized)
                return TimeSpan.FromMinutes(_parameters.BaseDiagnosticTimeMinutes);

            try
            {
                var timeSpan = _calculator.CalculateDiagnosticTime(type, severity, equipmentType);
                _tracker.IncrementDiagnosticEstimatesGenerated();
                return timeSpan;
            }
            catch (Exception ex)
            {
                _tracker.IncrementEstimationErrors();

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error estimating diagnostic time: {ex.Message}", this);
                }

                return TimeSpan.FromMinutes(_parameters.BaseDiagnosticTimeMinutes);
            }
        }

        /// <summary>
        /// Generate comprehensive time breakdown
        /// </summary>
        public TimeBreakdown GenerateTimeBreakdown(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType, bool requiresSpecialist = false)
        {
            if (!_isInitialized)
                return null;

            try
            {
                var breakdown = _calculator.GenerateTimeBreakdown(type, severity, equipmentType, requiresSpecialist);
                _tracker.IncrementTimeBreakdownsGenerated();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Generated time breakdown {breakdown.BreakdownId}: Total {breakdown.TotalTime.TotalHours:F1}h", this);
                }

                return breakdown;
            }
            catch (Exception ex)
            {
                _tracker.IncrementEstimationErrors();

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error generating time breakdown: {ex.Message}", this);
                }

                return null;
            }
        }

        /// <summary>
        /// Update time database with actual repair data
        /// </summary>
        public void UpdateTimeDatabase(MalfunctionType type, TimeSpan actualTime, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            if (!_isInitialized) return;

            var actualMinutes = (float)actualTime.TotalMinutes;

            // Track accuracy if enabled
            if (_trackEstimateAccuracy)
            {
                var estimatedTime = EstimateRepairTime(type, severity, equipmentType);
                _tracker.TrackEstimateAccuracy(type, severity, equipmentType, estimatedTime, actualTime);
            }

            // Update historical data
            _data.UpdateBaseTimeData(type, actualMinutes, _maxHistorySize);

            // Check if base time needs updating
            if (_data.ShouldUpdateBaseTime(type, out int newBaseMinutes))
            {
                OnBaseTimeUpdated?.Invoke(type, newBaseMinutes);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Updated base time for {type}: {newBaseMinutes}min", this);
                }
            }
        }

        /// <summary>
        /// Set time estimation parameters
        /// </summary>
        public void SetTimeParameters(bool useRealistic, float varianceFactor, float complexityMultiplier, float specialistBonus)
        {
            _parameters.UpdateParameters(useRealistic, varianceFactor, complexityMultiplier, specialistBonus);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Time parameters updated: Realistic={_parameters.UseRealisticTimeEstimates}, Variance={_parameters.TimeVarianceFactor:F2}", this);
            }
        }

        /// <summary>
        /// Handle accuracy updates from tracker
        /// </summary>
        private void HandleAccuracyUpdated(float accuracy)
        {
            OnEstimateAccuracyUpdated?.Invoke(accuracy);
        }

        private void OnDestroy()
        {
            if (_tracker != null)
            {
                _tracker.OnEstimateAccuracyUpdated -= HandleAccuracyUpdated;
            }
        }
    }
}

