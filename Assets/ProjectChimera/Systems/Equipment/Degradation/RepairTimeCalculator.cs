using System;
using UnityEngine;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Repair Time Calculator
    /// Single Responsibility: Calculate repair and diagnostic time estimates
    /// Extracted from TimeEstimationEngine (866 lines → 4 files <500 lines each)
    /// </summary>
    public class RepairTimeCalculator
    {
        private readonly TimeEstimationData _data;
        private readonly RepairTimeParameters _parameters;

        public RepairTimeCalculator(TimeEstimationData data, RepairTimeParameters parameters)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// Calculate repair time for malfunction
        /// </summary>
        public TimeSpan CalculateRepairTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType, bool requiresSpecialist)
        {
            // Get base time data
            var baseTimeData = _data.GetBaseTimeData(type);

            // Calculate base time with severity
            float baseMinutes = CalculateBaseTimeMinutes(baseTimeData, severity);

            // Apply severity multiplier
            float severityMultiplier = GetSeverityTimeMultiplier(severity);
            float adjustedMinutes = baseMinutes * severityMultiplier;

            // Apply complexity modifier
            float complexityModifier = GetComplexityTimeModifier(type, equipmentType);
            adjustedMinutes *= complexityModifier;

            // Apply equipment type modifier
            float equipmentModifier = _data.GetEquipmentTimeModifier(equipmentType);
            adjustedMinutes *= equipmentModifier;

            // Apply specialist efficiency bonus
            if (requiresSpecialist)
            {
                adjustedMinutes *= _parameters.SpecialistEfficiencyBonus;
            }

            // Apply variance for realistic estimates
            if (_parameters.UseRealisticTimeEstimates)
            {
                adjustedMinutes = ApplyTimeVariance(adjustedMinutes);
            }

            // Ensure minimum time
            adjustedMinutes = Mathf.Max(_parameters.MinimumRepairTimeMinutes, adjustedMinutes);

            return TimeSpan.FromMinutes(adjustedMinutes);
        }

        /// <summary>
        /// Calculate diagnostic time for malfunction
        /// </summary>
        public TimeSpan CalculateDiagnosticTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            // Get diagnostic time data
            var diagnosticData = _data.GetDiagnosticTimeData(type);

            // Calculate base diagnostic time
            int baseMinutes = diagnosticData.BaseMinutes;

            // Apply severity multiplier for complex diagnostics
            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Critical => _parameters.SeverityDiagnosticMultiplier,
                MalfunctionSeverity.Catastrophic => _parameters.SeverityDiagnosticMultiplier * 1.5f,
                _ => 1f
            };

            // Apply equipment complexity
            float equipmentComplexity = GetDiagnosticComplexityModifier(equipmentType);

            float totalMinutes = baseMinutes * severityMultiplier * equipmentComplexity;

            return TimeSpan.FromMinutes(Mathf.Max(5f, totalMinutes));
        }

        /// <summary>
        /// Generate comprehensive time breakdown
        /// </summary>
        public TimeBreakdown GenerateTimeBreakdown(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType, bool requiresSpecialist)
        {
            var breakdown = new TimeBreakdown
            {
                BreakdownId = GenerateBreakdownId(),
                MalfunctionType = type,
                Severity = severity,
                EquipmentType = equipmentType,
                RequiresSpecialist = requiresSpecialist,
                GenerationTime = DateTime.Now,

                // Core time estimates
                DiagnosticTime = CalculateDiagnosticTime(type, severity, equipmentType),
                RepairTime = CalculateRepairTime(type, severity, equipmentType, requiresSpecialist),

                // Additional time components
                PreparationTime = EstimatePreparationTime(severity),
                TestingTime = EstimateTestingTime(type, severity),
                CleanupTime = EstimateCleanupTime(severity),
                DocumentationTime = EstimateDocumentationTime(severity)
            };

            // Calculate total time
            breakdown.TotalTime = breakdown.DiagnosticTime + breakdown.RepairTime +
                                breakdown.PreparationTime + breakdown.TestingTime +
                                breakdown.CleanupTime + breakdown.DocumentationTime;

            // Calculate confidence
            var baseTimeData = _data.GetBaseTimeData(type);
            breakdown.Confidence = CalculateTimeEstimateConfidence(type, equipmentType, baseTimeData);

            return breakdown;
        }

        /// <summary>
        /// Calculate time estimate confidence
        /// </summary>
        public float CalculateTimeEstimateConfidence(MalfunctionType type, EquipmentType equipmentType, BaseTimeData timeData)
        {
            float baseConfidence = 0.8f;

            // Reduce confidence for complex types
            if (type == MalfunctionType.SoftwareError) baseConfidence -= 0.1f;
            if (equipmentType == EquipmentType.Security) baseConfidence -= 0.05f;

            // Increase confidence with more historical data
            if (timeData.ActualTimeHistory.Count > 10)
            {
                baseConfidence += 0.1f;
            }

            // Factor in variance
            baseConfidence -= timeData.VarianceFactor * 0.2f;

            return Mathf.Clamp01(baseConfidence);
        }

        #region Private Calculation Methods

        /// <summary>
        /// Calculate base time from severity
        /// </summary>
        private float CalculateBaseTimeMinutes(BaseTimeData timeData, MalfunctionSeverity severity)
        {
            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => UnityEngine.Random.Range(0.8f, 1.2f),
                MalfunctionSeverity.Moderate => UnityEngine.Random.Range(1.5f, 2.5f),
                MalfunctionSeverity.Major => UnityEngine.Random.Range(3f, 5f),
                MalfunctionSeverity.Critical => UnityEngine.Random.Range(6f, 10f),
                MalfunctionSeverity.Catastrophic => UnityEngine.Random.Range(10f, 15f),
                _ => 1f
            };

            float baseTime = timeData.BaseMinutes * severityMultiplier;
            return Mathf.Clamp(baseTime, timeData.MinimumMinutes, timeData.MaximumMinutes);
        }

        /// <summary>
        /// Get severity time multiplier
        /// </summary>
        private float GetSeverityTimeMultiplier(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 12f,
                _ => 1f
            };
        }

        /// <summary>
        /// Get complexity time modifier
        /// </summary>
        private float GetComplexityTimeModifier(MalfunctionType type, EquipmentType equipmentType)
        {
            float baseModifier = type switch
            {
                MalfunctionType.SoftwareError => 1.2f,
                MalfunctionType.ElectricalFailure => 1.1f,
                MalfunctionType.MechanicalFailure => 1.0f,
                MalfunctionType.OverheatingProblem => 0.9f,
                MalfunctionType.SensorDrift => 0.8f,
                _ => 1.0f
            };

            float equipmentModifier = equipmentType switch
            {
                EquipmentType.Security => 1.3f,
                EquipmentType.Climate => 1.2f,
                EquipmentType.Power => 1.1f,
                EquipmentType.HVAC => 1.1f,
                _ => 1.0f
            };

            return baseModifier * equipmentModifier;
        }

        /// <summary>
        /// Apply time variance for realistic estimates
        /// </summary>
        private float ApplyTimeVariance(float baseTime)
        {
            float variance = UnityEngine.Random.Range(-_parameters.TimeVarianceFactor, _parameters.TimeVarianceFactor);
            return baseTime * (1f + variance);
        }

        /// <summary>
        /// Get diagnostic complexity modifier
        /// </summary>
        private float GetDiagnosticComplexityModifier(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.Security => _parameters.DiagnosticComplexityMultiplier * 1.3f,
                EquipmentType.Climate => _parameters.DiagnosticComplexityMultiplier * 1.2f,
                EquipmentType.Power => _parameters.DiagnosticComplexityMultiplier * 1.1f,
                _ => _parameters.DiagnosticComplexityMultiplier
            };
        }

        /// <summary>
        /// Estimate preparation time
        /// </summary>
        private TimeSpan EstimatePreparationTime(MalfunctionSeverity severity)
        {
            int baseMinutes = severity switch
            {
                MalfunctionSeverity.Major => 20,
                MalfunctionSeverity.Critical => 30,
                MalfunctionSeverity.Catastrophic => 45,
                _ => 10
            };

            return TimeSpan.FromMinutes(baseMinutes);
        }

        /// <summary>
        /// Estimate testing time
        /// </summary>
        private TimeSpan EstimateTestingTime(MalfunctionType type, MalfunctionSeverity severity)
        {
            int baseMinutes = type switch
            {
                MalfunctionType.SoftwareError => 30,
                MalfunctionType.ElectricalFailure => 20,
                MalfunctionType.SensorDrift => 15,
                _ => 25
            };

            float severityMultiplier = severity >= MalfunctionSeverity.Major ? 1.5f : 1f;
            return TimeSpan.FromMinutes(baseMinutes * severityMultiplier);
        }

        /// <summary>
        /// Estimate cleanup time
        /// </summary>
        private TimeSpan EstimateCleanupTime(MalfunctionSeverity severity)
        {
            int baseMinutes = severity switch
            {
                MalfunctionSeverity.Major => 15,
                MalfunctionSeverity.Critical => 20,
                MalfunctionSeverity.Catastrophic => 30,
                _ => 5
            };

            return TimeSpan.FromMinutes(baseMinutes);
        }

        /// <summary>
        /// Estimate documentation time
        /// </summary>
        private TimeSpan EstimateDocumentationTime(MalfunctionSeverity severity)
        {
            int baseMinutes = severity switch
            {
                MalfunctionSeverity.Critical => 20,
                MalfunctionSeverity.Catastrophic => 30,
                _ => 10
            };

            return TimeSpan.FromMinutes(baseMinutes);
        }

        /// <summary>
        /// Generate unique breakdown ID
        /// </summary>
        private string GenerateBreakdownId()
        {
            return $"TBD_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        #endregion
    }

    /// <summary>
    /// Repair time calculation parameters
    /// </summary>
    public class RepairTimeParameters
    {
        public bool UseRealisticTimeEstimates { get; set; } = true;
        public float TimeVarianceFactor { get; set; } = 0.2f;
        public float ComplexityTimeMultiplier { get; set; } = 1.5f;
        public float SpecialistEfficiencyBonus { get; set; } = 0.8f; // 20% time reduction
        public int MinimumRepairTimeMinutes { get; set; } = 5;
        public int BaseDiagnosticTimeMinutes { get; set; } = 30;
        public float DiagnosticComplexityMultiplier { get; set; } = 1.2f;
        public float SeverityDiagnosticMultiplier { get; set; } = 2f;

        /// <summary>
        /// Update parameters with validation
        /// </summary>
        public void UpdateParameters(bool useRealistic, float varianceFactor, float complexityMultiplier, float specialistBonus)
        {
            UseRealisticTimeEstimates = useRealistic;
            TimeVarianceFactor = Mathf.Clamp01(varianceFactor);
            ComplexityTimeMultiplier = Mathf.Max(1f, complexityMultiplier);
            SpecialistEfficiencyBonus = Mathf.Clamp(specialistBonus, 0.5f, 1f);
        }
    }
}

