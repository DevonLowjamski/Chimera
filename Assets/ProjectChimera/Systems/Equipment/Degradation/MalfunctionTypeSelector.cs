using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Malfunction Type Selector
    /// Single Responsibility: Select malfunction types and determine severity based on conditions
    /// Extracted from MalfunctionGenerator (717 lines → 4 files <500 lines each)
    /// </summary>
    public class MalfunctionTypeSelector
    {
        private readonly MalfunctionGenerationParameters _parameters;
        private readonly bool _enableLogging;

        public MalfunctionTypeSelector(MalfunctionGenerationParameters parameters, bool enableLogging = false)
        {
            _parameters = parameters;
            _enableLogging = enableLogging;
        }

        #region Type Selection

        /// <summary>
        /// Select malfunction type based on equipment profile and conditions
        /// </summary>
        public MalfunctionType SelectMalfunctionType(EquipmentReliabilityProfile profile, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            if (profile == null || profile.CommonFailureModes == null || profile.CommonFailureModes.Count == 0)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("EQUIPMENT", "No failure modes available, defaulting to WearAndTear", null);
                return MalfunctionType.WearAndTear;
            }

            // Apply condition modifiers to failure mode probabilities
            var adjustedFailureModes = CalculateAdjustedFailureModes(profile.CommonFailureModes, wearLevel, stressFactors);

            // Select based on weighted random selection
            var selectedType = SelectWeightedRandom(adjustedFailureModes);

            if (_enableLogging)
                ChimeraLogger.LogInfo("EQUIPMENT",
                    $"Selected malfunction type: {selectedType} (Wear: {wearLevel:F2}, Temp Stress: {stressFactors.TemperatureStress:F2})", null);

            return selectedType;
        }

        /// <summary>
        /// Calculate adjusted failure mode probabilities based on conditions
        /// </summary>
        private Dictionary<MalfunctionType, float> CalculateAdjustedFailureModes(
            Dictionary<MalfunctionType, float> baseFailureModes,
            float wearLevel,
            EnvironmentalStressFactors stressFactors)
        {
            var adjustedFailureModes = new Dictionary<MalfunctionType, float>();

            foreach (var failureMode in baseFailureModes)
            {
                float adjustedWeight = failureMode.Value;

                // Adjust based on conditions
                adjustedWeight *= GetConditionMultiplier(failureMode.Key, wearLevel, stressFactors);

                adjustedFailureModes[failureMode.Key] = adjustedWeight;
            }

            return adjustedFailureModes;
        }

        /// <summary>
        /// Get condition multiplier for specific malfunction type
        /// </summary>
        private float GetConditionMultiplier(MalfunctionType type, float wearLevel, EnvironmentalStressFactors stressFactors)
        {
            return type switch
            {
                MalfunctionType.WearAndTear => 1f + wearLevel,
                MalfunctionType.OverheatingProblem => 1f + stressFactors.TemperatureStress,
                MalfunctionType.ElectricalFailure => 1f + stressFactors.ElectricalStress,
                MalfunctionType.SensorDrift => 1f + stressFactors.HumidityStress,
                MalfunctionType.MechanicalFailure => 1f + (wearLevel * 0.5f),
                _ => 1f
            };
        }

        /// <summary>
        /// Select malfunction type based on weighted random selection
        /// </summary>
        private MalfunctionType SelectWeightedRandom(Dictionary<MalfunctionType, float> weightedOptions)
        {
            float totalWeight = weightedOptions.Values.Sum();
            if (totalWeight <= 0f) return MalfunctionType.WearAndTear;

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var option in weightedOptions)
            {
                cumulative += option.Value;
                if (random <= cumulative)
                {
                    return option.Key;
                }
            }

            return MalfunctionType.WearAndTear;
        }

        #endregion

        #region Severity Determination

        /// <summary>
        /// Determine malfunction severity based on conditions
        /// </summary>
        public MalfunctionSeverity DetermineMalfunctionSeverity(
            EquipmentType equipmentType,
            float wearLevel,
            EnvironmentalStressFactors stressFactors,
            OperationalStatus currentStatus)
        {
            // Base severity roll
            float severityRoll = Random.Range(0f, 1f);

            // Apply modifiers
            float wearModifier = wearLevel * _parameters.WearSeverityModifier;
            float stressModifier = CalculateAverageStress(stressFactors) * _parameters.StressSeverityModifier;

            // Operational status modifier
            float statusModifier = currentStatus == OperationalStatus.Offline ? 0.1f : 0f;

            // Random variance
            float randomVariance = Random.Range(-_parameters.RandomSeverityVariance, _parameters.RandomSeverityVariance);

            // Combined severity score
            float severityScore = Mathf.Clamp01(severityRoll + wearModifier + stressModifier + statusModifier + randomVariance);

            // Apply variability factor
            severityScore += Random.Range(-_parameters.SeverityVariabilityFactor, _parameters.SeverityVariabilityFactor);
            severityScore = Mathf.Clamp01(severityScore);

            // Map to severity level
            var severity = MapScoreToSeverity(severityScore);

            if (_enableLogging)
                ChimeraLogger.LogInfo("EQUIPMENT",
                    $"Determined severity: {severity} (Score: {severityScore:F2}, Wear: {wearLevel:F2})", null);

            return severity;
        }

        /// <summary>
        /// Calculate average stress from environmental factors
        /// </summary>
        private float CalculateAverageStress(EnvironmentalStressFactors stressFactors)
        {
            return (stressFactors.TemperatureStress +
                    stressFactors.HumidityStress +
                    stressFactors.DustAccumulation +
                    stressFactors.ElectricalStress) * 0.25f;
        }

        /// <summary>
        /// Map severity score to severity level
        /// </summary>
        private MalfunctionSeverity MapScoreToSeverity(float score)
        {
            if (score < 0.15f) return MalfunctionSeverity.Minor;
            if (score < 0.35f) return MalfunctionSeverity.Moderate;
            if (score < 0.60f) return MalfunctionSeverity.Major;
            if (score < 0.85f) return MalfunctionSeverity.Critical;
            return MalfunctionSeverity.Catastrophic;
        }

        /// <summary>
        /// Convert risk level to severity
        /// </summary>
        public MalfunctionSeverity ConvertRiskToSeverity(RiskLevel riskLevel)
        {
            return riskLevel switch
            {
                RiskLevel.Low => MalfunctionSeverity.Minor,
                RiskLevel.Medium => MalfunctionSeverity.Moderate,
                RiskLevel.High => MalfunctionSeverity.Major,
                RiskLevel.Critical => MalfunctionSeverity.Catastrophic,
                _ => MalfunctionSeverity.Moderate
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate environmental stress factors
        /// </summary>
        public bool ValidateStressFactors(EnvironmentalStressFactors stressFactors, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (stressFactors.TemperatureStress < 0f || stressFactors.TemperatureStress > 1f)
            {
                errorMessage = "Temperature stress must be between 0 and 1";
                return false;
            }

            if (stressFactors.HumidityStress < 0f || stressFactors.HumidityStress > 1f)
            {
                errorMessage = "Humidity stress must be between 0 and 1";
                return false;
            }

            if (stressFactors.DustAccumulation < 0f || stressFactors.DustAccumulation > 1f)
            {
                errorMessage = "Dust accumulation must be between 0 and 1";
                return false;
            }

            if (stressFactors.ElectricalStress < 0f || stressFactors.ElectricalStress > 1f)
            {
                errorMessage = "Electrical stress must be between 0 and 1";
                return false;
            }

            return true;
        }

        #endregion
    }
}

