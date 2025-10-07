using UnityEngine;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Malfunction Cost Estimator Coordinator
    /// Single Responsibility: Orchestrate cost estimation operations
    /// BEFORE: 782 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (CostEstimationDataStructures, RepairCostCalculator, CostDatabaseManager, this coordinator)
    /// </summary>
    public class MalfunctionCostEstimator : MonoBehaviour
    {
        [Header("Cost Estimation Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useMarketPricing = true;
        [SerializeField] private bool _includeInflation = true;
        [SerializeField] private float _inflationRate = 0.03f;

        [Header("Base Cost Settings")]
        [SerializeField] private float _laborCostPerHour = 75f;
        [SerializeField] private float _specialistCostPerHour = 150f;
        [SerializeField] private float _emergencyMultiplier = 2f;
        [SerializeField] private float _partsCostVariance = 0.15f;

        [Header("Time Estimation Settings")]
        [SerializeField] private bool _useRealisticTimeEstimates = true;

        // PHASE 0: Component-based architecture (SRP)
        private RepairCostCalculator _calculator;
        private CostDatabaseManager _databaseManager;
        private TimeEstimationEngine _timeEstimator;

        // Statistics
        private MalfunctionCostEstimatorStats _stats;
        private bool _isInitialized = false;

        // Events
        public event Action<CostEstimate> OnCostEstimateGenerated;
        public event Action<string, float> OnCostDatabaseUpdated;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public MalfunctionCostEstimatorStats Stats => _stats;

        /// <summary>
        /// Initialize cost estimator
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            var parameters = new CostEstimationParameters
            {
                LaborCostPerHour = _laborCostPerHour,
                SpecialistCostPerHour = _specialistCostPerHour,
                EmergencyMultiplier = _emergencyMultiplier,
                PartsCostVariance = _partsCostVariance,
                InflationRate = _inflationRate,
                UseMarketPricing = _useMarketPricing,
                IncludeInflation = _includeInflation,
                UseRealisticTimeEstimates = _useRealisticTimeEstimates
            };

            _calculator = new RepairCostCalculator(parameters);
            _databaseManager = new CostDatabaseManager(_enableLogging);

            // Get or create time estimator
            _timeEstimator = GetComponent<TimeEstimationEngine>();
            if (_timeEstimator == null)
            {
                _timeEstimator = gameObject.AddComponent<TimeEstimationEngine>();
            }
            _timeEstimator.Initialize();

            _stats = MalfunctionCostEstimatorStats.CreateEmpty();
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Malfunction Cost Estimator initialized", this);
            }
        }

        /// <summary>
        /// Estimate repair cost for malfunction
        /// </summary>
        public float EstimateRepairCost(
            MalfunctionType type,
            MalfunctionSeverity severity,
            EquipmentType equipmentType,
            bool requiresSpecialist = false,
            bool isEmergency = false)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot estimate cost - estimator not initialized", this);
                }
                return 0f;
            }

            var estimationStartTime = Time.realtimeSinceStartup;

            try
            {
                // Get base cost data
                var baseCostData = _databaseManager.GetBaseCostData(type);

                // Calculate repair cost
                float finalCost = _calculator.CalculateRepairCost(
                    baseCostData,
                    severity,
                    equipmentType,
                    requiresSpecialist,
                    isEmergency
                );

                // Update statistics
                var estimationTime = Time.realtimeSinceStartup - estimationStartTime;
                _stats.UpdateStats(estimationTime, finalCost);
                _stats.CostEstimatesGenerated++;

                return Mathf.Max(0f, finalCost);
            }
            catch (Exception ex)
            {
                _stats.EstimationErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error estimating repair cost: {ex.Message}", this);
                }

                return 0f;
            }
        }

        /// <summary>
        /// Estimate repair time for malfunction
        /// </summary>
        public TimeSpan EstimateRepairTime(
            MalfunctionType type,
            MalfunctionSeverity severity,
            EquipmentType equipmentType,
            bool requiresSpecialist = false)
        {
            if (!_isInitialized || _timeEstimator == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("EQUIPMENT", "Cannot estimate time - estimator not initialized", this);
                }
                return TimeSpan.Zero;
            }

            try
            {
                var timeSpan = _timeEstimator.EstimateRepairTime(type, severity, equipmentType, requiresSpecialist);
                _stats.TimeEstimatesGenerated++;
                return timeSpan;
            }
            catch (Exception ex)
            {
                _stats.EstimationErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error estimating repair time: {ex.Message}", this);
                }

                return TimeSpan.FromMinutes(60); // Default 1 hour
            }
        }

        /// <summary>
        /// Generate comprehensive cost estimate
        /// </summary>
        public CostEstimate GenerateComprehensiveCostEstimate(
            EquipmentMalfunction malfunction,
            bool requiresSpecialist = false,
            bool isEmergency = false)
        {
            if (!_isInitialized || malfunction == null)
                return null;

            try
            {
                var equipmentType = EquipmentType.Generic; // Simplified - would get from equipment instance

                // Get base cost data
                var baseCostData = _databaseManager.GetBaseCostData(malfunction.Type);

                // Estimate required parts if not provided
                var requiredParts = malfunction.RequiredParts != null && malfunction.RequiredParts.Count > 0
                    ? malfunction.RequiredParts
                    : _calculator.EstimateRequiredParts(malfunction.Type, malfunction.Severity);

                // Calculate cost breakdown
                var breakdown = _calculator.CalculateCostBreakdown(
                    malfunction.Type,
                    malfunction.Severity,
                    baseCostData,
                    requiredParts,
                    requiresSpecialist,
                    isEmergency
                );

                // Estimate times
                var repairTime = EstimateRepairTime(malfunction.Type, malfunction.Severity, equipmentType, requiresSpecialist);
                var diagnosticTime = _timeEstimator.EstimateDiagnosticTime(malfunction.Type, malfunction.Severity, equipmentType);

                // Create comprehensive estimate
                var estimate = new CostEstimate
                {
                    EstimateId = $"EST_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}",
                    MalfunctionId = malfunction.MalfunctionId,
                    EquipmentId = malfunction.EquipmentId,
                    EstimationTime = DateTime.Now,

                    // Cost breakdown
                    LaborCost = breakdown.LaborCost,
                    PartsCost = breakdown.PartsCost,
                    OverheadCost = breakdown.OverheadCost,
                    EmergencySurcharge = breakdown.EmergencySurcharge,
                    TotalCost = breakdown.TotalCost,

                    // Time estimates
                    EstimatedRepairTime = repairTime,
                    DiagnosticTime = diagnosticTime,

                    // Additional details
                    RequiresSpecialist = requiresSpecialist || malfunction.RequiresSpecialist,
                    IsEmergency = isEmergency,
                    Confidence = _calculator.CalculateEstimateConfidence(malfunction.Type, equipmentType, baseCostData),

                    // Parts and materials
                    RequiredParts = requiredParts,
                    AdditionalMaterials = _calculator.EstimateAdditionalMaterials(malfunction.Type, malfunction.Severity)
                };

                // Cache estimate
                _databaseManager.CacheEstimate(estimate);

                OnCostEstimateGenerated?.Invoke(estimate);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("EQUIPMENT",
                        $"Generated cost estimate {estimate.EstimateId}: ${estimate.TotalCost:F2} over {estimate.EstimatedRepairTime.TotalHours:F1}h",
                        this);
                }

                return estimate;
            }
            catch (Exception ex)
            {
                _stats.EstimationErrors++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("EQUIPMENT", $"Error generating comprehensive estimate: {ex.Message}", this);
                }

                return null;
            }
        }

        /// <summary>
        /// Update cost database with actual repair data
        /// </summary>
        public void UpdateCostDatabase(MalfunctionType type, float actualCost, TimeSpan actualTime, MalfunctionSeverity severity)
        {
            if (!_isInitialized) return;

            _databaseManager.UpdateCostDatabase(type, actualCost, actualTime, severity);

            if (_timeEstimator != null)
            {
                _timeEstimator.UpdateTimeDatabase(type, actualTime, severity, EquipmentType.Generic);
            }

            OnCostDatabaseUpdated?.Invoke(type.ToString(), actualCost);
        }

        /// <summary>
        /// Get cached estimate
        /// </summary>
        public CostEstimate GetCachedEstimate(string estimateId)
        {
            return _databaseManager?.GetCachedEstimate(estimateId);
        }

        /// <summary>
        /// Set cost parameters
        /// </summary>
        public void SetCostParameters(float laborCost, float specialistCost, float emergencyMultiplier, float variance)
        {
            _laborCostPerHour = Mathf.Max(0f, laborCost);
            _specialistCostPerHour = Mathf.Max(0f, specialistCost);
            _emergencyMultiplier = Mathf.Max(1f, emergencyMultiplier);
            _partsCostVariance = Mathf.Clamp01(variance);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT",
                    $"Cost parameters updated: Labor=${laborCost}/hr, Emergency={emergencyMultiplier}x",
                    this);
            }
        }
    }
}

