using System;
using UnityEngine;

namespace ProjectChimera.Systems.Cultivation.IPM
{
    /// <summary>
    /// Helper utilities for IPM calculations and pest dynamics.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class IPMCalculationHelpers
    {
        /// <summary>
        /// Gets pest generation time based on type.
        /// </summary>
        public static float GetPestGenerationTime(PestType pestType, float spiderMiteGen, float aphidGen, float fungusGnatGen, float thripsGen)
        {
            return pestType switch
            {
                PestType.SpiderMites => spiderMiteGen,
                PestType.Aphids => aphidGen,
                PestType.FungusGnats => fungusGnatGen,
                PestType.Thrips => thripsGen,
                _ => 10f
            };
        }

        /// <summary>
        /// Gets treatment efficacy based on type and pest.
        /// </summary>
        public static float GetTreatmentEfficacy(TreatmentType treatment, PestType pest)
        {
            // Treatment effectiveness matrix (based on cannabis IPM research)
            return (treatment, pest) switch
            {
                (TreatmentType.NeemOil, PestType.SpiderMites) => 0.60f,
                (TreatmentType.NeemOil, PestType.Aphids) => 0.70f,
                (TreatmentType.InsecticidalSoap, PestType.Aphids) => 0.80f,
                (TreatmentType.InsecticidalSoap, PestType.Thrips) => 0.65f,
                (TreatmentType.PyrethrinsSpray, _) => 0.85f, // Broad spectrum, high efficacy
                (TreatmentType.BTi, PestType.FungusGnats) => 0.90f, // Highly specific
                _ => 0.40f // Default moderate efficacy
            };
        }

        /// <summary>
        /// Gets treatment duration (how long it remains effective).
        /// </summary>
        public static float GetTreatmentDuration(TreatmentType treatment)
        {
            return treatment switch
            {
                TreatmentType.NeemOil => 7f,
                TreatmentType.InsecticidalSoap => 3f,
                TreatmentType.PyrethrinsSpray => 5f,
                TreatmentType.BTi => 14f,
                _ => 7f
            };
        }

        /// <summary>
        /// Checks if beneficial organism is effective against pest type.
        /// </summary>
        public static bool IsBeneficialEffectiveAgainst(BeneficialType beneficial, PestType pest)
        {
            return (beneficial, pest) switch
            {
                (BeneficialType.PredatoryMites, PestType.SpiderMites) => true,
                (BeneficialType.Ladybugs, PestType.Aphids) => true,
                (BeneficialType.ParasiticWasps, PestType.Aphids) => true,
                (BeneficialType.ParasiticWasps, PestType.Thrips) => true,
                _ => false
            };
        }

        /// <summary>
        /// Calculates pest population growth multiplier.
        /// </summary>
        public static float CalculatePopulationGrowth(float deltaTimeDays, float generationDays, float environmentalMod, float predationReduction)
        {
            // Calculate exponential growth
            float generationsElapsed = deltaTimeDays / generationDays;
            float growthRate = 1.5f; // 1.5x per generation (realistic for pests)
            float populationMultiplier = Mathf.Pow(growthRate, generationsElapsed);

            // Apply modifiers
            populationMultiplier *= environmentalMod;
            populationMultiplier *= (1f - predationReduction);

            return populationMultiplier;
        }

        /// <summary>
        /// Calculates beneficial organism population change.
        /// </summary>
        public static float CalculateBeneficialPopulationChange(float pestPressure, float deltaTimeDays)
        {
            // Beneficial populations decline slowly without food (pests)
            // If pests present, population stabilizes
            float changeRate = pestPressure > 0 ? 0.01f : -0.02f; // Grow with pests, decline without
            return changeRate * deltaTimeDays;
        }
    }
}
