using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Malfunction Record Factory
    /// Single Responsibility: Create comprehensive malfunction records with symptoms, costs, and repair data
    /// Extracted from MalfunctionGenerator (717 lines → 4 files <500 lines each)
    /// </summary>
    public class MalfunctionRecordFactory
    {
        private readonly MalfunctionGenerationParameters _parameters;
        private readonly bool _enableLogging;

        public MalfunctionRecordFactory(MalfunctionGenerationParameters parameters, bool enableLogging = false)
        {
            _parameters = parameters;
            _enableLogging = enableLogging;
        }

        #region Record Creation

        /// <summary>
        /// Create comprehensive malfunction record
        /// </summary>
        public EquipmentMalfunction CreateMalfunctionRecord(
            string equipmentId,
            EquipmentType equipmentType,
            MalfunctionType malfunctionType,
            MalfunctionSeverity severity,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            var malfunction = new EquipmentMalfunction
            {
                MalfunctionId = GenerateMalfunctionId(),
                EquipmentId = equipmentId,
                EquipmentType = equipmentType,
                Type = malfunctionType,
                Severity = severity,
                OccurrenceTime = DateTime.Now,
                PerformanceImpact = CalculatePerformanceImpact(severity),
                EstimatedRepairCost = EstimateRepairCost(malfunctionType, severity, equipmentType),
                EstimatedRepairTime = EstimateRepairTime(malfunctionType, severity, equipmentType),
                CauseAnalysis = GenerateCauseAnalysis(equipmentType, malfunctionType, wearLevel, stressFactors)
            };

            // Generate detailed symptoms if enabled
            if (_parameters.GenerateDetailedSymptoms)
            {
                malfunction.Symptoms = GenerateSymptoms(malfunctionType, severity, equipmentType);
            }
            else
            {
                malfunction.Symptoms = GenerateBasicSymptoms(malfunctionType, severity);
            }

            // Generate required parts list
            malfunction.RequiredParts = GenerateRequiredParts(malfunctionType, equipmentType);

            if (_enableLogging)
                ChimeraLogger.LogInfo("EQUIPMENT",
                    $"Created malfunction record {malfunction.MalfunctionId}: {severity} {malfunctionType}", null);

            return malfunction;
        }

        /// <summary>
        /// Create default malfunction when profile is unavailable
        /// </summary>
        public EquipmentMalfunction CreateDefaultMalfunction(string equipmentId, EquipmentType equipmentType)
        {
            return new EquipmentMalfunction
            {
                MalfunctionId = GenerateMalfunctionId(),
                EquipmentId = equipmentId,
                EquipmentType = equipmentType,
                Type = MalfunctionType.WearAndTear,
                Severity = MalfunctionSeverity.Minor,
                OccurrenceTime = DateTime.Now,
                PerformanceImpact = 0.1f,
                EstimatedRepairCost = 100f,
                EstimatedRepairTime = TimeSpan.FromMinutes(30),
                CauseAnalysis = "General wear and tear",
                Symptoms = new List<string> { "Minor performance degradation" },
                RequiredParts = new List<string>()
            };
        }

        #endregion

        #region ID Generation

        /// <summary>
        /// Generate unique malfunction ID
        /// </summary>
        private string GenerateMalfunctionId()
        {
            return $"MAL_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        #endregion

        #region Cause Analysis

        /// <summary>
        /// Generate cause analysis text
        /// </summary>
        private string GenerateCauseAnalysis(
            EquipmentType equipmentType,
            MalfunctionType type,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            var baseCause = type switch
            {
                MalfunctionType.MechanicalFailure => "Mechanical component failure",
                MalfunctionType.ElectricalFailure => "Electrical system failure",
                MalfunctionType.SensorDrift => "Sensor calibration drift",
                MalfunctionType.OverheatingProblem => "Overheating condition",
                MalfunctionType.SoftwareError => "Software or control system error",
                _ => "Equipment malfunction"
            };

            // Add contributing factors
            var factors = new List<string>();
            if (wearLevel > 0.7f) factors.Add("high wear level");
            if (stressFactors.TemperatureStress > 0.5f) factors.Add("temperature stress");
            if (stressFactors.HumidityStress > 0.5f) factors.Add("humidity exposure");
            if (stressFactors.DustAccumulation > 0.5f) factors.Add("dust accumulation");
            if (stressFactors.ElectricalStress > 0.5f) factors.Add("electrical stress");

            if (factors.Count > 0)
            {
                return $"{baseCause} attributed to {string.Join(", ", factors)}";
            }

            return $"{baseCause} due to normal operational conditions";
        }

        #endregion

        #region Symptom Generation

        /// <summary>
        /// Generate realistic symptoms based on malfunction type and severity
        /// </summary>
        private List<string> GenerateSymptoms(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            var symptoms = new List<string>();

            // Base symptoms by type
            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    symptoms.AddRange(new[] { "Unusual vibration", "Grinding noises", "Reduced output" });
                    if (severity >= MalfunctionSeverity.Major) symptoms.Add("Complete mechanical stoppage");
                    break;

                case MalfunctionType.ElectricalFailure:
                    symptoms.AddRange(new[] { "Power fluctuations", "Intermittent operation", "Error codes" });
                    if (severity >= MalfunctionSeverity.Major) symptoms.Add("Total power loss");
                    break;

                case MalfunctionType.SensorDrift:
                    symptoms.AddRange(new[] { "Inaccurate readings", "Inconsistent data", "Calibration warnings" });
                    if (severity >= MalfunctionSeverity.Major) symptoms.Add("Sensor failure");
                    break;

                case MalfunctionType.OverheatingProblem:
                    symptoms.AddRange(new[] { "High temperature warnings", "Thermal shutdowns", "Reduced efficiency" });
                    if (severity >= MalfunctionSeverity.Major) symptoms.Add("Thermal damage");
                    break;

                case MalfunctionType.SoftwareError:
                    symptoms.AddRange(new[] { "System errors", "Unexpected behavior", "Control issues" });
                    if (severity >= MalfunctionSeverity.Major) symptoms.Add("System crashes");
                    break;
            }

            // Add severity-specific symptoms
            if (severity >= MalfunctionSeverity.Critical)
            {
                symptoms.Add("Equipment shutdown required");
                symptoms.Add("Safety systems activated");
            }

            return symptoms;
        }

        /// <summary>
        /// Generate basic symptoms (simplified version)
        /// </summary>
        private List<string> GenerateBasicSymptoms(MalfunctionType type, MalfunctionSeverity severity)
        {
            var symptoms = new List<string>();

            symptoms.Add($"{type} detected");
            symptoms.Add($"Severity level: {severity}");

            return symptoms;
        }

        #endregion

        #region Performance Impact

        /// <summary>
        /// Calculate performance impact percentage
        /// </summary>
        private float CalculatePerformanceImpact(MalfunctionSeverity severity)
        {
            return severity switch
            {
                MalfunctionSeverity.Minor => UnityEngine.Random.Range(0.05f, 0.15f),
                MalfunctionSeverity.Moderate => UnityEngine.Random.Range(0.15f, 0.35f),
                MalfunctionSeverity.Major => UnityEngine.Random.Range(0.35f, 0.65f),
                MalfunctionSeverity.Critical => UnityEngine.Random.Range(0.65f, 0.85f),
                MalfunctionSeverity.Catastrophic => UnityEngine.Random.Range(0.85f, 1f),
                _ => 0.1f
            };
        }

        #endregion

        #region Cost Estimation

        /// <summary>
        /// Estimate repair cost
        /// </summary>
        private float EstimateRepairCost(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            float baseCost = type switch
            {
                MalfunctionType.MechanicalFailure => UnityEngine.Random.Range(400f, 600f),
                MalfunctionType.ElectricalFailure => UnityEngine.Random.Range(250f, 350f),
                MalfunctionType.SensorDrift => UnityEngine.Random.Range(80f, 120f),
                MalfunctionType.OverheatingProblem => UnityEngine.Random.Range(300f, 500f),
                MalfunctionType.SoftwareError => UnityEngine.Random.Range(150f, 250f),
                _ => UnityEngine.Random.Range(150f, 250f)
            };

            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => UnityEngine.Random.Range(0.8f, 1.2f),
                MalfunctionSeverity.Moderate => UnityEngine.Random.Range(1.5f, 2.5f),
                MalfunctionSeverity.Major => UnityEngine.Random.Range(3f, 5f),
                MalfunctionSeverity.Critical => UnityEngine.Random.Range(6f, 10f),
                MalfunctionSeverity.Catastrophic => UnityEngine.Random.Range(12f, 18f),
                _ => 1f
            };

            // Apply equipment type modifier
            float equipmentModifier = GetEquipmentCostModifier(equipmentType);

            return baseCost * severityMultiplier * equipmentModifier;
        }

        /// <summary>
        /// Get equipment type cost modifier
        /// </summary>
        private float GetEquipmentCostModifier(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.HVAC => 1.3f,
                EquipmentType.Lighting => 0.8f,
                EquipmentType.Irrigation => 1.0f,
                EquipmentType.Monitoring => 1.5f,
                _ => 1.0f
            };
        }

        #endregion

        #region Time Estimation

        /// <summary>
        /// Estimate repair time
        /// </summary>
        private TimeSpan EstimateRepairTime(MalfunctionType type, MalfunctionSeverity severity, EquipmentType equipmentType)
        {
            int baseMinutes = type switch
            {
                MalfunctionType.MechanicalFailure => UnityEngine.Random.Range(90, 150),
                MalfunctionType.ElectricalFailure => UnityEngine.Random.Range(60, 120),
                MalfunctionType.SensorDrift => UnityEngine.Random.Range(20, 40),
                MalfunctionType.OverheatingProblem => UnityEngine.Random.Range(45, 75),
                MalfunctionType.SoftwareError => UnityEngine.Random.Range(30, 90),
                _ => UnityEngine.Random.Range(45, 75)
            };

            float severityMultiplier = severity switch
            {
                MalfunctionSeverity.Minor => UnityEngine.Random.Range(0.8f, 1.2f),
                MalfunctionSeverity.Moderate => UnityEngine.Random.Range(1.5f, 2.5f),
                MalfunctionSeverity.Major => UnityEngine.Random.Range(3f, 5f),
                MalfunctionSeverity.Critical => UnityEngine.Random.Range(6f, 10f),
                MalfunctionSeverity.Catastrophic => UnityEngine.Random.Range(10f, 15f),
                _ => 1f
            };

            int totalMinutes = Mathf.RoundToInt(baseMinutes * severityMultiplier);
            return TimeSpan.FromMinutes(totalMinutes);
        }

        #endregion

        #region Parts Generation

        /// <summary>
        /// Generate list of required parts
        /// </summary>
        private List<string> GenerateRequiredParts(MalfunctionType type, EquipmentType equipmentType)
        {
            var parts = new List<string>();

            // Base parts by malfunction type
            switch (type)
            {
                case MalfunctionType.MechanicalFailure:
                    parts.AddRange(new[] { "Bearings", "Belts", "Gaskets", "Lubricant" });
                    break;

                case MalfunctionType.ElectricalFailure:
                    parts.AddRange(new[] { "Wiring", "Connectors", "Fuses", "Relays" });
                    break;

                case MalfunctionType.SensorDrift:
                    parts.AddRange(new[] { "Sensor calibration kit", "Replacement sensor" });
                    break;

                case MalfunctionType.OverheatingProblem:
                    parts.AddRange(new[] { "Thermal paste", "Cooling fan", "Heat sink" });
                    break;

                case MalfunctionType.SoftwareError:
                    parts.AddRange(new[] { "Firmware update", "Configuration backup" });
                    break;
            }

            // Add equipment-specific parts
            switch (equipmentType)
            {
                case EquipmentType.HVAC:
                    if (type == MalfunctionType.MechanicalFailure)
                        parts.Add("HVAC motor assembly");
                    break;

                case EquipmentType.Lighting:
                    if (type == MalfunctionType.ElectricalFailure)
                        parts.Add("LED driver board");
                    break;

                case EquipmentType.Irrigation:
                    if (type == MalfunctionType.MechanicalFailure)
                        parts.Add("Pump impeller");
                    break;

                case EquipmentType.Monitoring:
                    if (type == MalfunctionType.SensorDrift)
                        parts.Add("Calibrated sensor module");
                    break;
            }

            return parts;
        }

        #endregion
    }
}

