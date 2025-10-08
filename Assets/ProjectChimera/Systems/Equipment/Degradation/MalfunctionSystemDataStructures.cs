// REFACTORED: Malfunction System Data Structures
// Extracted from MalfunctionSystem for better separation of concerns

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Malfunction detection system helper
    /// </summary>
    public class MalfunctionDetectionSystem
    {
        public System.Action<EquipmentMalfunction> OnMalfunctionDetected;

        public void InitializeDefaultRules()
        {
            // No-op for minimal implementation
        }

        public List<MalfunctionAnalysis> AnalyzeEquipment(BasicEquipmentData equipment)
        {
            // Minimal analysis producing low-confidence wear-and-tear suggestion
            return new List<MalfunctionAnalysis>
            {
                new MalfunctionAnalysis
                {
                    EquipmentId = equipment.EquipmentId,
                    MalfunctionType = MalfunctionType.WearAndTear,
                    Confidence = 0.1f
                }
            };
        }

        public EquipmentMalfunction GenerateMalfunction(MalfunctionAnalysis analysis, BasicEquipmentData equipment)
        {
            var malfunction = MalfunctionSystem.GenerateMalfunction(
                analysis.EquipmentId,
                equipment.Type,
                wearLevel: 1f - Mathf.Clamp01(equipment.Efficiency),
                stressFactors: new EnvironmentalStressFactors(),
                currentStatus: OperationalStatus.Online);
            OnMalfunctionDetected?.Invoke(malfunction);
            return malfunction;
        }
    }

    /// <summary>
    /// Equipment malfunction data
    /// </summary>
    [System.Serializable]
    public class EquipmentMalfunction
    {
        public string MalfunctionId;
        public string EquipmentId;
        public Data.Equipment.EquipmentType EquipmentType;
        public MalfunctionType Type;
        public MalfunctionSeverity Severity;
        public DateTime OccurrenceTime;
        public string CauseAnalysis;
        public List<string> Symptoms;
        public float ImpactOnPerformance;
        public float PerformanceImpact { get => ImpactOnPerformance; set => ImpactOnPerformance = value; } // Alias
        public float RepairCost;
        public float EstimatedRepairCost { get => RepairCost; set => RepairCost = value; } // Alias
        public TimeSpan EstimatedRepairTime;
        public bool RequiresSpecialist;
        public RepairComplexity Complexity;
        public List<string> RequiredParts;
    }

    /// <summary>
    /// Malfunction risk assessment data
    /// </summary>
    [System.Serializable]
    public class MalfunctionRiskAssessment
    {
        public string EquipmentId;
        public float OverallRisk;
        public RiskLevel RiskLevel;
        public MalfunctionType MostLikelyMalfunction;
        public List<string> ContributingFactors;
        public List<string> RecommendedActions;
        public RiskComponents RiskComponents;
        public float AssessmentTime;
    }

    /// <summary>
    /// Risk level enumeration
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Repair result data
    /// </summary>
    [System.Serializable]
    public class RepairResult
    {
        public bool Success;
        public string Reason;
        public string MalfunctionId;
        public string RepairId;
        public float RepairCost;
        public TimeSpan RepairTime;
        public float WearReduction;
        public bool EquipmentRestored;
        public float RepairQuality;
        public DateTime CompletionTime;
    }
}

