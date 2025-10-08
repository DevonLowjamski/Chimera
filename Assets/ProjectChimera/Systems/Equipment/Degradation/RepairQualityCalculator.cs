// REFACTORED: Repair Quality Calculator
// Extracted from MalfunctionRepairProcessor for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// Calculates repair success rates, quality, and costs
    /// </summary>
    public class RepairQualityCalculator
    {
        private readonly float _baseRepairSuccessRate;
        private readonly float _specialistBonus;
        private readonly float _qualityImpactFactor;
        private readonly float _repairQualityVariance;

        public RepairQualityCalculator(
            float baseRepairSuccessRate,
            float specialistBonus,
            float qualityImpactFactor,
            float repairQualityVariance)
        {
            _baseRepairSuccessRate = baseRepairSuccessRate;
            _specialistBonus = specialistBonus;
            _qualityImpactFactor = qualityImpactFactor;
            _repairQualityVariance = repairQualityVariance;
        }

        public float CalculateRepairSuccessRate(float repairQuality, bool useSpecialist)
        {
            float successRate = _baseRepairSuccessRate;

            // Quality impact
            float qualityModifier = (repairQuality - 0.5f) * _qualityImpactFactor;
            successRate += qualityModifier;

            // Specialist bonus
            if (useSpecialist)
                successRate += _specialistBonus;

            // Clamp to valid range
            return Mathf.Clamp01(successRate);
        }

        public float CalculateEffectiveRepairQuality(float baseQuality)
        {
            // Add variance to repair quality
            float variance = UnityEngine.Random.Range(-_repairQualityVariance, _repairQualityVariance);
            return Mathf.Clamp01(baseQuality + variance);
        }

        public float CalculateRepairCost(float baseCost, float severity, bool useSpecialist)
        {
            float cost = baseCost;

            // Severity multiplier
            cost *= (1f + severity * 0.5f);

            // Specialist cost increase
            if (useSpecialist)
                cost *= 1.3f;

            return cost;
        }

        public float CalculateRepairTime(float baseTime, MalfunctionType type, float severity, bool useSpecialist)
        {
            float time = baseTime;

            // Type-specific multipliers
            time *= type switch
            {
                MalfunctionType.SoftwareError => 1.0f,
                MalfunctionType.SensorDrift => 1.2f,
                MalfunctionType.MechanicalFailure => 1.5f,
                MalfunctionType.ElectricalFailure => 1.3f,
                MalfunctionType.OverheatingProblem => 1.4f,
                MalfunctionType.WearAndTear => 0.8f,
                _ => 1.0f
            };

            // Severity impact
            time *= (1f + severity * 0.3f);

            // Specialist reduces time
            if (useSpecialist)
                time *= 0.7f;

            return time;
        }
    }
}

