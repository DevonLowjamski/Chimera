using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Malfunction Repair Processor
    /// Single Responsibility: Handling malfunction repairs and repair results
    /// Extracted from MalfunctionSystem for better separation of concerns
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

        // Repair tracking
        private readonly Dictionary<string, RepairOperation> _activeRepairs = new Dictionary<string, RepairOperation>();
        private readonly List<RepairResult> _repairHistory = new List<RepairResult>();
        private readonly Dictionary<string, List<RepairResult>> _equipmentRepairHistory = new Dictionary<string, List<RepairResult>>();

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

            _activeRepairs.Clear();
            _repairHistory.Clear();
            _equipmentRepairHistory.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Malfunction Repair Processor initialized", this);
            }
        }

        /// <summary>
        /// Start a repair operation for a malfunction
        /// </summary>
        public RepairOperation StartRepair(EquipmentMalfunction malfunction, float repairQuality, bool useSpecialist = false)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot start repair - processor not initialized", this);
                }
                return null;
            }

            if (malfunction == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot start repair - malfunction is null", this);
                }
                return null;
            }

            if (_activeRepairs.ContainsKey(malfunction.MalfunctionId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", $"Repair already in progress for malfunction {malfunction.MalfunctionId}", this);
                }
                return _activeRepairs[malfunction.MalfunctionId];
            }

            var repairStartTime = Time.realtimeSinceStartup;

            try
            {
                var repairOperation = new RepairOperation
                {
                    RepairId = GenerateRepairId(),
                    MalfunctionId = malfunction.MalfunctionId,
                    EquipmentId = malfunction.EquipmentId,
                    RepairType = DetermineRepairType(malfunction),
                    StartTime = DateTime.Now,
                    EstimatedDuration = malfunction.EstimatedRepairTime,
                    RepairQuality = Mathf.Clamp01(repairQuality),
                    UseSpecialist = useSpecialist || malfunction.RequiresSpecialist,
                    RequiredParts = new List<string>(malfunction.RequiredParts),
                    EstimatedCost = malfunction.RepairCost,
                    Status = RepairStatus.InProgress,
                    Progress = 0f
                };

                // Calculate actual repair parameters
                CalculateActualRepairParameters(repairOperation, malfunction);

                _activeRepairs[malfunction.MalfunctionId] = repairOperation;
                _stats.RepairsStarted++;

                OnRepairStarted?.Invoke(repairOperation);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT", $"Started repair {repairOperation.RepairId} for malfunction {malfunction.MalfunctionId} ({malfunction.Type})", this);
                }

                return repairOperation;
            }
            catch (System.Exception ex)
            {
                _stats.RepairErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error starting repair for malfunction {malfunction.MalfunctionId}: {ex.Message}", this);
                }

                return null;
            }
        }

        /// <summary>
        /// Process repair completion
        /// </summary>
        public RepairResult CompleteRepair(string malfunctionId, float actualRepairQuality = -1f)
        {
            if (!_isInitialized)
            {
                return CreateFailedRepairResult(malfunctionId, "Processor not initialized");
            }

            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
            {
                return CreateFailedRepairResult(malfunctionId, "Repair operation not found");
            }

            var repairStartTime = Time.realtimeSinceStartup;

            try
            {
                // Use provided quality or fall back to operation quality
                float finalQuality = actualRepairQuality >= 0f ? actualRepairQuality : repairOperation.RepairQuality;

                // Calculate repair success
                bool repairSuccess = CalculateRepairSuccess(repairOperation, finalQuality);

                // Create repair result
                var repairResult = new RepairResult
                {
                    Success = repairSuccess,
                    MalfunctionId = malfunctionId,
                    RepairId = repairOperation.RepairId,
                    RepairCost = repairOperation.ActualCost,
                    RepairTime = repairOperation.ActualDuration,
                    WearReduction = CalculateWearReduction(finalQuality, repairSuccess),
                    EquipmentRestored = repairSuccess,
                    RepairQuality = finalQuality,
                    Reason = repairSuccess ? "Repair completed successfully" : "Repair failed",
                    CompletionTime = DateTime.Now
                };

                // Remove from active repairs
                _activeRepairs.Remove(malfunctionId);

                // Update statistics
                var processingTime = Time.realtimeSinceStartup - repairStartTime;
                UpdateRepairStats(processingTime, repairSuccess, repairOperation.RepairType);

                // Track repair history
                if (_trackRepairHistory)
                {
                    RecordRepairHistory(repairResult, repairOperation);
                }

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
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error completing repair for malfunction {malfunctionId}: {ex.Message}", this);
                }

                return CreateFailedRepairResult(malfunctionId, $"Repair processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update repair progress
        /// </summary>
        public bool UpdateRepairProgress(string malfunctionId, float progress)
        {
            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
                return false;

            repairOperation.Progress = Mathf.Clamp01(progress);
            OnRepairProgress?.Invoke(malfunctionId, repairOperation.Progress);

            return true;
        }

        /// <summary>
        /// Cancel active repair
        /// </summary>
        public RepairResult CancelRepair(string malfunctionId, string reason = "Repair cancelled")
        {
            if (!_activeRepairs.TryGetValue(malfunctionId, out var repairOperation))
            {
                return CreateFailedRepairResult(malfunctionId, "Repair operation not found");
            }

            _activeRepairs.Remove(malfunctionId);
            _stats.RepairsCancelled++;

            var cancelResult = new RepairResult
            {
                Success = false,
                MalfunctionId = malfunctionId,
                RepairId = repairOperation.RepairId,
                RepairCost = repairOperation.ActualCost * 0.5f, // Partial cost for cancelled repair
                RepairTime = TimeSpan.FromTicks(DateTime.Now.Ticks - repairOperation.StartTime.Ticks),
                WearReduction = 0f,
                EquipmentRestored = false,
                RepairQuality = 0f,
                Reason = reason,
                CompletionTime = DateTime.Now
            };

            OnRepairCompleted?.Invoke(cancelResult);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Cancelled repair {repairOperation.RepairId} for malfunction {malfunctionId}: {reason}", this);
            }

            return cancelResult;
        }

        /// <summary>
        /// Get active repair operation
        /// </summary>
        public RepairOperation GetActiveRepair(string malfunctionId)
        {
            return _activeRepairs.TryGetValue(malfunctionId, out var repair) ? repair : null;
        }

        /// <summary>
        /// Get all active repairs
        /// </summary>
        public List<RepairOperation> GetActiveRepairs()
        {
            return _activeRepairs.Values.ToList();
        }

        /// <summary>
        /// Get repair history for equipment
        /// </summary>
        public List<RepairResult> GetEquipmentRepairHistory(string equipmentId)
        {
            return _equipmentRepairHistory.TryGetValue(equipmentId, out var history) ? new List<RepairResult>(history) : new List<RepairResult>();
        }

        /// <summary>
        /// Get overall repair statistics
        /// </summary>
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

        /// <summary>
        /// Set repair processing parameters
        /// </summary>
        public void SetRepairParameters(float baseSuccessRate, float specialistBonus, float qualityImpact)
        {
            _baseRepairSuccessRate = Mathf.Clamp01(baseSuccessRate);
            _specialistBonus = Mathf.Clamp01(specialistBonus);
            _qualityImpactFactor = Mathf.Clamp01(qualityImpact);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", $"Repair parameters updated: BaseSuccess={_baseRepairSuccessRate:F2}, SpecialistBonus={_specialistBonus:F2}, QualityImpact={_qualityImpactFactor:F2}", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Generate unique repair ID
        /// </summary>
        private string GenerateRepairId()
        {
            return $"REP_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// Determine repair type based on malfunction
        /// </summary>
        private RepairType DetermineRepairType(EquipmentMalfunction malfunction)
        {
            return malfunction.Type switch
            {
                MalfunctionType.MechanicalFailure => RepairType.Mechanical,
                MalfunctionType.ElectricalFailure => RepairType.Electrical,
                MalfunctionType.SensorDrift => RepairType.Calibration,
                MalfunctionType.OverheatingProblem => RepairType.Thermal,
                MalfunctionType.SoftwareError => RepairType.Software,
                _ => RepairType.General
            };
        }

        /// <summary>
        /// Calculate actual repair parameters with variance
        /// </summary>
        private void CalculateActualRepairParameters(RepairOperation repairOperation, EquipmentMalfunction malfunction)
        {
            // Add variance to cost and duration
            float costVariance = UnityEngine.Random.Range(-_repairQualityVariance, _repairQualityVariance);
            float durationVariance = UnityEngine.Random.Range(-_repairQualityVariance, _repairQualityVariance);

            repairOperation.ActualCost = malfunction.RepairCost * (1f + costVariance);
            repairOperation.ActualDuration = TimeSpan.FromTicks((long)(malfunction.EstimatedRepairTime.Ticks * (1f + durationVariance)));

            // Higher quality repairs may cost more but take less time
            if (repairOperation.RepairQuality > 0.8f)
            {
                repairOperation.ActualCost *= 1.2f; // 20% cost increase for high quality
                repairOperation.ActualDuration = TimeSpan.FromTicks((long)(repairOperation.ActualDuration.Ticks * 0.9f)); // 10% time reduction
            }

            // Specialist repairs are more expensive but more reliable
            if (repairOperation.UseSpecialist)
            {
                repairOperation.ActualCost *= 1.5f; // 50% cost increase for specialist
                repairOperation.ActualDuration = TimeSpan.FromTicks((long)(repairOperation.ActualDuration.Ticks * 0.8f)); // 20% time reduction
            }
        }

        /// <summary>
        /// Calculate repair success probability
        /// </summary>
        private bool CalculateRepairSuccess(RepairOperation repairOperation, float repairQuality)
        {
            float successRate = _baseRepairSuccessRate;

            // Quality impact
            successRate += (repairQuality - 0.5f) * _qualityImpactFactor;

            // Specialist bonus
            if (repairOperation.UseSpecialist)
            {
                successRate += _specialistBonus;
            }

            // Complexity penalty
            float complexityPenalty = repairOperation.RepairType switch
            {
                RepairType.Software => 0.05f,
                RepairType.Electrical => 0.03f,
                RepairType.Mechanical => 0.02f,
                RepairType.Thermal => 0.01f,
                RepairType.Calibration => 0.01f,
                _ => 0f
            };

            successRate -= complexityPenalty;

            // Clamp and roll
            successRate = Mathf.Clamp01(successRate);
            return UnityEngine.Random.Range(0f, 1f) < successRate;
        }

        /// <summary>
        /// Calculate wear reduction from repair
        /// </summary>
        private float CalculateWearReduction(float repairQuality, bool repairSuccess)
        {
            if (!repairSuccess) return 0f;

            // Base wear reduction scales with quality
            float baseReduction = repairQuality * 0.3f; // Up to 30% wear reduction

            // Add some variance
            float variance = UnityEngine.Random.Range(-0.05f, 0.05f);

            return Mathf.Clamp01(baseReduction + variance);
        }

        /// <summary>
        /// Create failed repair result
        /// </summary>
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

        /// <summary>
        /// Record repair in history
        /// </summary>
        private void RecordRepairHistory(RepairResult repairResult, RepairOperation repairOperation)
        {
            _repairHistory.Add(repairResult);

            // Limit global history size
            if (_repairHistory.Count > 1000)
            {
                _repairHistory.RemoveAt(0);
            }

            // Track per-equipment history
            if (!_equipmentRepairHistory.TryGetValue(repairOperation.EquipmentId, out var equipmentHistory))
            {
                equipmentHistory = new List<RepairResult>();
                _equipmentRepairHistory[repairOperation.EquipmentId] = equipmentHistory;
            }

            equipmentHistory.Add(repairResult);

            // Limit per-equipment history size
            if (equipmentHistory.Count > 50)
            {
                equipmentHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Update repair statistics
        /// </summary>
        private void UpdateRepairStats(float processingTime, bool success, RepairType repairType)
        {
            _stats.TotalProcessingTime += processingTime;
            _stats.AverageProcessingTime = _stats.RepairsStarted > 0 ? _stats.TotalProcessingTime / _stats.RepairsStarted : 0f;

            if (processingTime > _stats.MaxProcessingTime)
                _stats.MaxProcessingTime = processingTime;

            if (success)
            {
                _stats.RepairsCompleted++;
            }
            else
            {
                _stats.RepairsFailed++;
            }

            // Update type-specific counters
            switch (repairType)
            {
                case RepairType.Mechanical:
                    _stats.MechanicalRepairs++;
                    break;
                case RepairType.Electrical:
                    _stats.ElectricalRepairs++;
                    break;
                case RepairType.Software:
                    _stats.SoftwareRepairs++;
                    break;
                case RepairType.Thermal:
                    _stats.ThermalRepairs++;
                    break;
                case RepairType.Calibration:
                    _stats.CalibrationRepairs++;
                    break;
                case RepairType.General:
                    _stats.GeneralRepairs++;
                    break;
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new MalfunctionRepairProcessorStats
            {
                RepairsStarted = 0,
                RepairsCompleted = 0,
                RepairsFailed = 0,
                RepairsCancelled = 0,
                RepairErrors = 0,
                TotalProcessingTime = 0f,
                AverageProcessingTime = 0f,
                MaxProcessingTime = 0f,
                TotalRepairCost = 0f,
                AverageRepairCost = 0f,
                AverageRepairTime = 0f,
                MechanicalRepairs = 0,
                ElectricalRepairs = 0,
                SoftwareRepairs = 0,
                ThermalRepairs = 0,
                CalibrationRepairs = 0,
                GeneralRepairs = 0
            };
        }

        #endregion
    }

    /// <summary>
    /// Repair operation data
    /// </summary>
    [System.Serializable]
    }
