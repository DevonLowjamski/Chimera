using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Malfunction Repair Processor Coordinator
    /// Single Responsibility: Coordinate malfunction repairs through helper classes
    /// Reduced from 554 lines using composition with RepairQualityCalculator and RepairHistoryTracker
    /// </summary>
    public class MalfunctionRepairProcessor : MonoBehaviour
    {
        [Header("Repair Processing Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableAdvancedRepairLogic = true;
        [SerializeField] private float _repairQualityVariance = 0.1f;
        [SerializeField] private bool _trackRepairHistory = true;

        [Header("Repair Success Rates")]
        [SerializeField] private float _baseRepairSuccessRate = 0.85f;
        [SerializeField] private float _specialistBonus = 0.15f;
        [SerializeField] private float _qualityImpactFactor = 0.2f;

        // Helper components (Composition pattern for SRP)
        private RepairQualityCalculator _qualityCalculator;
        private RepairHistoryTracker _historyTracker;

        // Repair tracking
        private readonly Dictionary<string, RepairOperation> _activeRepairs = new Dictionary<string, RepairOperation>();

        // Statistics
        private MalfunctionRepairProcessorStats _stats = new MalfunctionRepairProcessorStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<RepairOperation> OnRepairStarted;
        public event System.Action<RepairResult> OnRepairCompleted;
        public event System.Action<string, float> OnRepairProgress;

        public bool IsInitialized => _isInitialized;
        public MalfunctionRepairProcessorStats Stats => _stats;
        public int ActiveRepairCount => _activeRepairs.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize helper components
            _qualityCalculator = new RepairQualityCalculator(
                _baseRepairSuccessRate,
                _specialistBonus,
                _qualityImpactFactor,
                _repairQualityVariance);

            _historyTracker = new RepairHistoryTracker();

            _activeRepairs.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("EQUIPMENT", "Malfunction Repair Processor initialized", this);
        }

        public RepairOperation StartRepair(EquipmentMalfunction malfunction, float repairQuality, bool useSpecialist = false)
        {
            if (!_isInitialized || malfunction == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot start repair - invalid state", this);
                return null;
            }

            if (_activeRepairs.ContainsKey(malfunction.MalfunctionId))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("EQUIPMENT", $"Repair already in progress for malfunction {malfunction.MalfunctionId}", this);
                return _activeRepairs[malfunction.MalfunctionId];
            }

            var repairStartTime = Time.realtimeSinceStartup;

            try
            {
                float effectiveQuality = _qualityCalculator.CalculateEffectiveRepairQuality(repairQuality);
                float estimatedCost = _qualityCalculator.CalculateRepairCost(
                    malfunction.EstimatedCost, 
                    malfunction.Severity, 
                    useSpecialist);
                float estimatedTime = _qualityCalculator.CalculateRepairTime(
                    1800f, // 30 minutes base time
                    malfunction.Type,
                    malfunction.Severity,
                    useSpecialist);

                var repairOperation = new RepairOperation
                {
                    RepairId = GenerateRepairId(),
                    MalfunctionId = malfunction.MalfunctionId,
                    EquipmentId = malfunction.EquipmentId,
                    Type = DetermineRepairType(malfunction),
                    Status = RepairStatus.Pending,
                    EstimatedCost = estimatedCost,
                    ActualCost = estimatedCost,
                    EstimatedTime = TimeSpan.FromSeconds(estimatedTime),
                    StartTime = DateTime.Now,
                    Progress = 0f,
                    RepairQuality = effectiveQuality,
                    UseSpecialist = useSpecialist
                };

                _activeRepairs[malfunction.MalfunctionId] = repairOperation;
                _stats.RepairsStarted++;

                OnRepairStarted?.Invoke(repairOperation);

                if (_enableLogging)
                    ChimeraLogger.Log("EQUIPMENT", $"Started repair {repairOperation.RepairId} for malfunction {malfunction.MalfunctionId}", this);

                return repairOperation;
            }
            catch (System.Exception ex)
            {
                _stats.RepairErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("EQUIPMENT", $"Error starting repair: {ex.Message}", this);
                return null;
            }
        }

        public RepairResult CompleteRepair(string malfunctionId, bool forceSuccess = false)
        {
            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
            {
                return CreateFailedRepairResult(malfunctionId, "Repair operation not found");
            }

            try
            {
                float successRate = _qualityCalculator.CalculateRepairSuccessRate(
                    repairOperation.RepairQuality,
                    repairOperation.UseSpecialist);

                bool repairSuccess = forceSuccess || UnityEngine.Random.value <= successRate;

                var repairResult = new RepairResult
                {
                    Success = repairSuccess,
                    MalfunctionId = malfunctionId,
                    RepairId = repairOperation.RepairId,
                    RepairCost = repairOperation.ActualCost,
                    RepairTime = TimeSpan.FromTicks(DateTime.Now.Ticks - repairOperation.StartTime.Ticks),
                    WearReduction = repairSuccess ? UnityEngine.Random.Range(0.15f, 0.35f) : 0f,
                    EquipmentRestored = repairSuccess,
                    RepairQuality = repairOperation.RepairQuality,
                    Reason = repairSuccess ? "Repair completed successfully" : "Repair failed",
                    CompletionTime = DateTime.Now
                };

                _activeRepairs.Remove(malfunctionId);

                if (repairSuccess)
                    _stats.RepairsCompleted++;
                else
                    _stats.RepairsFailed++;

                UpdateStats(repairResult);

                if (_trackRepairHistory && _historyTracker != null)
                    _historyTracker.AddRepair(repairResult, repairOperation.EquipmentId);

                OnRepairCompleted?.Invoke(repairResult);

                if (_enableLogging)
                {
                    var result = repairSuccess ? "succeeded" : "failed";
                    ChimeraLogger.Log("EQUIPMENT", $"Repair {repairOperation.RepairId} {result} for malfunction {malfunctionId}", this);
                }

                return repairResult;
            }
            catch (System.Exception ex)
            {
                _stats.RepairErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("EQUIPMENT", $"Error completing repair for malfunction {malfunctionId}: {ex.Message}", this);
                return CreateFailedRepairResult(malfunctionId, $"Repair processing error: {ex.Message}");
            }
        }

        public bool UpdateRepairProgress(string malfunctionId, float progress)
        {
            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
                return false;

            repairOperation.Progress = Mathf.Clamp01(progress);
            OnRepairProgress?.Invoke(malfunctionId, repairOperation.Progress);

            return true;
        }

        public RepairResult CancelRepair(string malfunctionId, string reason = "Repair cancelled")
        {
            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
                return CreateFailedRepairResult(malfunctionId, "Repair operation not found");

            _activeRepairs.Remove(malfunctionId);
            _stats.RepairsCancelled++;

            var cancelResult = new RepairResult
            {
                Success = false,
                MalfunctionId = malfunctionId,
                RepairId = repairOperation.RepairId,
                RepairCost = repairOperation.ActualCost * 0.5f,
                RepairTime = TimeSpan.FromTicks(DateTime.Now.Ticks - repairOperation.StartTime.Ticks),
                WearReduction = 0f,
                EquipmentRestored = false,
                RepairQuality = 0f,
                Reason = reason,
                CompletionTime = DateTime.Now
            };

            OnRepairCompleted?.Invoke(cancelResult);

            if (_enableLogging)
                ChimeraLogger.Log("EQUIPMENT", $"Cancelled repair {repairOperation.RepairId} for malfunction {malfunctionId}: {reason}", this);

            return cancelResult;
        }

        public RepairOperation GetActiveRepair(string malfunctionId) =>
            _activeRepairs.TryGetValue(malfunctionId, out var repair) ? repair : null;

        public List<RepairOperation> GetActiveRepairs() =>
            _activeRepairs.Values.ToList();

        public List<RepairResult> GetEquipmentRepairHistory(string equipmentId) =>
            _historyTracker?.GetEquipmentRepairHistory(equipmentId) ?? new List<RepairResult>();

        public RepairStatistics GetRepairStatistics()
        {
            var totalRepairs = _stats.RepairsCompleted + _stats.RepairsFailed;

            return new RepairStatistics
            {
                TotalRepairs = totalRepairs,
                SuccessfulRepairs = _stats.RepairsCompleted,
                FailedRepairs = _stats.RepairsFailed,
                SuccessRate = totalRepairs > 0 ? (float)_stats.RepairsCompleted / totalRepairs : 0f,
                AverageRepairTime = _stats.AverageRepairTime,
                AverageRepairCost = _stats.AverageRepairCost,
                TotalRepairCost = _stats.TotalRepairCost
            };
        }

        public void SetRepairParameters(float baseSuccessRate, float specialistBonus, float qualityImpact)
        {
            _baseRepairSuccessRate = Mathf.Clamp01(baseSuccessRate);
            _specialistBonus = Mathf.Clamp01(specialistBonus);
            _qualityImpactFactor = Mathf.Clamp01(qualityImpact);

            // Reinitialize quality calculator with new parameters
            _qualityCalculator = new RepairQualityCalculator(
                _baseRepairSuccessRate,
                _specialistBonus,
                _qualityImpactFactor,
                _repairQualityVariance);

            if (_enableLogging)
                ChimeraLogger.Log("EQUIPMENT", $"Repair parameters updated", this);
        }

        #region Private Methods

        private string GenerateRepairId() =>
            $"REP_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";

        private RepairType DetermineRepairType(EquipmentMalfunction malfunction)
        {
            return malfunction.Type switch
            {
                MalfunctionType.MechanicalFailure => RepairType.Mechanical,
                MalfunctionType.ElectricalFailure => RepairType.Electrical,
                MalfunctionType.SensorDrift => RepairType.Calibration,
                MalfunctionType.OverheatingProblem => RepairType.Thermal,
                MalfunctionType.SoftwareError => RepairType.Software,
                MalfunctionType.WearAndTear => RepairType.Maintenance,
                _ => RepairType.General
            };
        }

        private RepairResult CreateFailedRepairResult(string malfunctionId, string reason)
        {
            return new RepairResult
            {
                Success = false,
                MalfunctionId = malfunctionId,
                RepairId = "FAILED",
                RepairCost = 0f,
                RepairTime = TimeSpan.Zero,
                WearReduction = 0f,
                EquipmentRestored = false,
                RepairQuality = 0f,
                Reason = reason,
                CompletionTime = DateTime.Now
            };
        }

        private void UpdateStats(RepairResult result)
        {
            _stats.TotalRepairCost += result.RepairCost;
            _stats.TotalRepairTime += (float)result.RepairTime.TotalSeconds;

            var totalRepairs = _stats.RepairsCompleted + _stats.RepairsFailed;
            if (totalRepairs > 0)
            {
                _stats.AverageRepairCost = _stats.TotalRepairCost / totalRepairs;
                _stats.AverageRepairTime = _stats.TotalRepairTime / totalRepairs;
            }
        }

        private void ResetStats()
        {
            _stats = new MalfunctionRepairProcessorStats();
        }

        #endregion
    }
}
