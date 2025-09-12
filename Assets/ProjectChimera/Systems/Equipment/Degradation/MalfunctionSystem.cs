using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Advanced malfunction detection and diagnostic system for equipment degradation.
    /// Simulates realistic equipment failures, provides diagnostic capabilities,
    /// and estimates repair requirements based on equipment type and failure mode.
    /// </summary>
    public static class MalfunctionSystem
    {
        private static readonly Dictionary<string, EquipmentMalfunction> _activeMalfunctions = new Dictionary<string, EquipmentMalfunction>();

        /// <summary>
        /// Evaluates equipment for potential malfunctions based on wear and conditions
        /// </summary>
        public static MalfunctionRiskAssessment EvaluateMalfunctionRisk(string equipmentId, EquipmentType equipmentType, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
            if (profile == null)
            {
                return new MalfunctionRiskAssessment
                {
                    EquipmentId = equipmentId,
                    OverallRisk = 0f,
                    RiskLevel = RiskLevel.Low,
                    MostLikelyMalfunction = MalfunctionType.WearAndTear
                };
            }

            // Calculate base risk from wear
            float wearRisk = wearLevel * 0.005f;

            // Calculate environmental risk
            float environmentalRisk = CalculateEnvironmentalMalfunctionRisk(equipmentType, stressFactors) * 0.003f;

            // Calculate maintenance risk
            float maintenanceRisk = CalculateMaintenanceMalfunctionRisk(equipmentId) * 0.002f;

            float totalRisk = wearRisk + environmentalRisk + maintenanceRisk;

            // Determine risk level
            var riskLevel = totalRisk switch
            {
                < 0.001f => RiskLevel.Low,
                < 0.003f => RiskLevel.Medium,
                < 0.008f => RiskLevel.High,
                _ => RiskLevel.Critical
            };

            // Determine most likely malfunction type
            var mostLikelyMalfunction = SelectMostLikelyMalfunction(profile, wearLevel, stressFactors);

            return new MalfunctionRiskAssessment
            {
                EquipmentId = equipmentId,
                OverallRisk = totalRisk,
                RiskLevel = riskLevel,
                MostLikelyMalfunction = mostLikelyMalfunction,
                ContributingFactors = GetContributingFactors(wearLevel, stressFactors),
                RecommendedActions = GetRiskMitigationActions(riskLevel, mostLikelyMalfunction)
            };
        }

        /// <summary>
        /// Generates a realistic malfunction for equipment
        /// </summary>
        public static EquipmentMalfunction GenerateMalfunction(
            string equipmentId,
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors,
            OperationalStatus currentStatus)
        {
            var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
            if (profile == null) return null;

            // Select malfunction type based on equipment profile
            var malfunctionType = SelectMalfunctionType(profile, wearLevel, stressFactors);

            // Determine severity based on equipment condition
            var severity = DetermineMalfunctionSeverity(equipmentType, wearLevel, stressFactors, currentStatus);

            // Create malfunction record
            var malfunction = new EquipmentMalfunction
            {
                MalfunctionId = Guid.NewGuid().ToString("N")[..8],
                EquipmentId = equipmentId,
                Type = malfunctionType,
                Severity = severity,
                OccurrenceTime = DateTime.Now,
                CauseAnalysis = GenerateCauseAnalysis(equipmentType, malfunctionType, wearLevel, stressFactors),
                Symptoms = GenerateSymptoms(malfunctionType, severity, equipmentType),
                ImpactOnPerformance = CalculatePerformanceImpact(severity),
                RepairCost = EstimateRepairCost(malfunctionType, severity, equipmentType),
                EstimatedRepairTime = EstimateRepairTime(malfunctionType, severity, equipmentType),
                RequiresSpecialist = DetermineSpecialistRequirement(malfunctionType, severity),
                Complexity = DetermineRepairComplexity(malfunctionType, severity),
                RequiredParts = GenerateRequiredParts(malfunctionType, equipmentType)
            };

            // Register malfunction
            _activeMalfunctions[malfunction.MalfunctionId] = malfunction;

            // Add to equipment's active issues
            EquipmentInstance.AddActiveIssue(equipmentId, malfunction.MalfunctionId);

            UnityEngine.Debug.Log($"[MalfunctionSystem] Malfunction generated: {equipmentId} - {malfunctionType} ({severity})");

            return malfunction;
        }

        /// <summary>
        /// Repairs a malfunction and updates equipment status
        /// </summary>
        public static RepairResult RepairMalfunction(string malfunctionId, float repairQuality)
        {
            if (!_activeMalfunctions.TryGetValue(malfunctionId, out var malfunction))
            {
                return new RepairResult
                {
                    Success = false,
                    Reason = "Malfunction not found"
                };
            }

            // Calculate repair effectiveness
            float wearReduction = malfunction.RepairCost / malfunction.RepairCost * 0.2f * repairQuality;

            // Apply repair to equipment
            bool repairSuccess = EquipmentInstance.RepairEquipment(malfunction.EquipmentId, repairQuality, wearReduction);

            if (repairSuccess)
            {
                // Remove malfunction from active list
                _activeMalfunctions.Remove(malfunctionId);
                EquipmentInstance.RemoveActiveIssue(malfunction.EquipmentId, malfunctionId);

                UnityEngine.Debug.Log($"[MalfunctionSystem] Malfunction repaired: {malfunctionId} (Quality: {repairQuality:F2})");

                return new RepairResult
                {
                    Success = true,
                    MalfunctionId = malfunctionId,
                    RepairCost = malfunction.RepairCost,
                    RepairTime = malfunction.EstimatedRepairTime,
                    WearReduction = wearReduction,
                    EquipmentRestored = true
                };
            }

            return new RepairResult
            {
                Success = false,
                Reason = "Equipment repair failed"
            };
        }

        /// <summary>
        /// Gets all active malfunctions
        /// </summary>
        public static List<EquipmentMalfunction> GetActiveMalfunctions()
        {
            return _activeMalfunctions.Values.ToList();
        }

        // Helper methods (simplified for brevity)
        private static float CalculateEnvironmentalMalfunctionRisk(EquipmentType equipmentType, EnvironmentalStressFactors stressFactors)
        {
            var profile = EquipmentTypes.GetReliabilityProfile(equipmentType);
            if (profile == null) return 0f;

            float totalRisk = 0f;
            float totalSensitivity = 0f;

            // Temperature stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Temperature", out float tempSensitivity))
            {
                totalRisk += stressFactors.TemperatureStress * tempSensitivity;
                totalSensitivity += tempSensitivity;
            }

            // Humidity stress
            if (profile.EnvironmentalSensitivity.TryGetValue("Humidity", out float humiditySensitivity))
            {
                totalRisk += stressFactors.HumidityStress * humiditySensitivity;
                totalSensitivity += humiditySensitivity;
            }

            return totalSensitivity > 0 ? totalRisk / totalSensitivity : totalRisk;
        }

        private static float CalculateMaintenanceMalfunctionRisk(string equipmentId)
        {
            var equipment = EquipmentInstance.GetEquipment(equipmentId);
            if (equipment == null) return 0f;

            TimeSpan timeSinceMaintenance = DateTime.Now - equipment.LastMaintenance;
            float daysSinceMaintenance = (float)timeSinceMaintenance.TotalDays;

            // Risk increases exponentially with time since maintenance
            return Mathf.Clamp01(daysSinceMaintenance / 180f); // 180 days maximum
        }

        private static MalfunctionType SelectMalfunctionType(EquipmentReliabilityProfile profile, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            // Select based on weighted probabilities
            float totalWeight = profile.CommonFailureModes.Values.Sum();
            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var failureMode in profile.CommonFailureModes)
            {
                cumulative += failureMode.Value;
                if (random <= cumulative)
                {
                    return failureMode.Key;
                }
            }

            return MalfunctionType.WearAndTear;
        }

        private static MalfunctionType SelectMostLikelyMalfunction(EquipmentReliabilityProfile profile, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            return SelectMalfunctionType(profile, wearLevel, stressFactors);
        }

        private static MalfunctionSeverity DetermineMalfunctionSeverity(
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors,
            OperationalStatus currentStatus)
        {
            float severityRoll = UnityEngine.Random.Range(0f, 1f);
            float wearModifier = wearLevel * 0.3f;
            float stressModifier = (stressFactors.TemperatureStress + stressFactors.HumidityStress) * 0.2f;
            float adjustedRoll = severityRoll + wearModifier + stressModifier;

            return adjustedRoll switch
            {
                < 0.4f => MalfunctionSeverity.Minor,
                < 0.7f => MalfunctionSeverity.Moderate,
                < 0.9f => MalfunctionSeverity.Major,
                < 0.98f => MalfunctionSeverity.Critical,
                _ => MalfunctionSeverity.Catastrophic
            };
        }

        private static string GenerateCauseAnalysis(
            EquipmentType equipmentType,
            MalfunctionType type,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            return type switch
            {
                MalfunctionType.MechanicalFailure => "Mechanical component failure due to wear and operational stress",
                MalfunctionType.ElectricalFailure => "Electrical system failure, possibly due to power fluctuations or component aging",
                MalfunctionType.SensorDrift => "Sensor calibration drift due to environmental exposure and age",
                MalfunctionType.OverheatingProblem => "Overheating due to inadequate cooling or increased load",
                _ => "Equipment malfunction requiring investigation"
            };
        }

        private static List<string> GenerateSymptoms(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            var symptoms = new List<string>();

            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    symptoms.AddRange(new[] { "Unusual vibration", "Grinding noises", "Reduced output" });
                    break;
                case MalfunctionType.ElectricalFailure:
                    symptoms.AddRange(new[] { "Power fluctuations", "Intermittent operation", "Error codes" });
                    break;
                case MalfunctionType.SensorDrift:
                    symptoms.AddRange(new[] { "Inaccurate readings", "Inconsistent data", "Calibration warnings" });
                    break;
                case MalfunctionType.OverheatingProblem:
                    symptoms.AddRange(new[] { "High temperature warnings", "Thermal shutdowns", "Reduced efficiency" });
                    break;
            }

            return symptoms;
        }

        private static float CalculatePerformanceImpact(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => 0.1f,
                MalfunctionSeverity.Moderate => 0.25f,
                MalfunctionSeverity.Major => 0.5f,
                MalfunctionSeverity.Critical => 0.75f,
                MalfunctionSeverity.Catastrophic => 1f,
                _ => 0.1f
            };
        }

        private static float EstimateRepairCost(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            float baseCost = type switch
            {
                MalfunctionType.MechanicalFailure => 500f,
                MalfunctionType.ElectricalFailure => 300f,
                MalfunctionType.SensorDrift => 100f,
                MalfunctionType.OverheatingProblem => 400f,
                _ => 200f
            };

            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 15f,
                _ => 1f
            };

            return baseCost * severityMultiplier;
        }

        private static TimeSpan EstimateRepairTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            int baseMinutes = type switch
            {
                MalfunctionType.MechanicalFailure => 120,
                MalfunctionType.ElectricalFailure => 90,
                MalfunctionType.SensorDrift => 30,
                MalfunctionType.OverheatingProblem => 60,
                _ => 60
            };

            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => 1f,
                MalfunctionSeverity.Moderate => 2f,
                MalfunctionSeverity.Major => 4f,
                MalfunctionSeverity.Critical => 8f,
                MalfunctionSeverity.Catastrophic => 12f,
                _ => 1f
            };

            return TimeSpan.FromMinutes(baseMinutes * severityMultiplier);
        }

        private static bool DetermineSpecialistRequirement(MalfunctionType type, MalfunctionSeverity severity)
        {
            return severity >= MalfunctionSeverity.Major || 
                   type == MalfunctionType.ElectricalFailure || 
                   type == MalfunctionType.SoftwareError;
        }

        private static RepairComplexity DetermineRepairComplexity(MalfunctionType type, MalfunctionSeverity severity)
        {
            if (severity == MalfunctionSeverity.Catastrophic)
                return RepairComplexity.ManufacturerService;
            if (severity == MalfunctionSeverity.Critical)
                return RepairComplexity.ExpertRequired;
            if (severity == MalfunctionSeverity.Major)
                return RepairComplexity.Complex;
            if (severity == MalfunctionSeverity.Moderate)
                return RepairComplexity.Moderate;
            return RepairComplexity.Simple;
        }

        private static List<string> GenerateRequiredParts(MalfunctionType type, EquipmentType equipmentType)
        {
            var parts = new List<string>();

            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    parts.AddRange(new[] { "Bearings", "Seals", "Gaskets" });
                    break;
                case MalfunctionType.ElectricalFailure:
                    parts.AddRange(new[] { "Fuses", "Relays", "Wiring" });
                    break;
                case MalfunctionType.SensorDrift:
                    parts.AddRange(new[] { "Sensor", "Calibration kit" });
                    break;
                case MalfunctionType.OverheatingProblem:
                    parts.AddRange(new[] { "Cooling fan", "Heat sink", "Thermal paste" });
                    break;
            }

            return parts;
        }

        private static List<string> GetContributingFactors(float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            var factors = new List<string>();

            if (wearLevel > 0.7f)
                factors.Add("High equipment wear");

            if (stressFactors.TemperatureStress > 0.5f)
                factors.Add("Temperature stress");

            if (stressFactors.HumidityStress > 0.5f)
                factors.Add("Humidity stress");

            if (stressFactors.DustAccumulation > 0.5f)
                factors.Add("Dust accumulation");

            if (stressFactors.ElectricalStress > 0.5f)
                factors.Add("Electrical stress");

            return factors;
        }

        private static List<string> GetRiskMitigationActions(RiskLevel riskLevel, MalfunctionType malfunctionType)
        {
            var actions = new List<string>();

            switch (riskLevel)
            {
                case RiskLevel.Critical:
                    actions.Add("Immediate maintenance scheduling");
                    actions.Add("Monitor equipment closely");
                    break;
                case RiskLevel.High:
                    actions.Add("Schedule maintenance within 7 days");
                    actions.Add("Increase monitoring frequency");
                    break;
                case RiskLevel.Medium:
                    actions.Add("Schedule maintenance within 30 days");
                    actions.Add("Review maintenance history");
                    break;
                case RiskLevel.Low:
                    actions.Add("Continue regular maintenance schedule");
                    break;
            }

            return actions;
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class EquipmentMalfunction
    {
        public string MalfunctionId;
        public string EquipmentId;
        public MalfunctionType Type;
        public MalfunctionSeverity Severity;
        public DateTime OccurrenceTime;
        public string CauseAnalysis;
        public List<string> Symptoms;
        public float ImpactOnPerformance;
        public float RepairCost;
        public TimeSpan EstimatedRepairTime;
        public bool RequiresSpecialist;
        public RepairComplexity Complexity;
        public List<string> RequiredParts;
    }

    [System.Serializable]
    public class MalfunctionRiskAssessment
    {
        public string EquipmentId;
        public float OverallRisk;
        public RiskLevel RiskLevel;
        public MalfunctionType MostLikelyMalfunction;
        public List<string> ContributingFactors;
        public List<string> RecommendedActions;
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    [System.Serializable]
    public class RepairResult
    {
        public bool Success;
        public string Reason;
        public string MalfunctionId;
        public float RepairCost;
        public TimeSpan RepairTime;
        public float WearReduction;
        public bool EquipmentRestored;
    }
}
