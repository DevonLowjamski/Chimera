using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Harvest Readiness Calculator
    /// Single Responsibility: Calculate harvest readiness, yield, and potency
    /// Extracted from PlantHarvestOperator (785 lines → 4 files <500 lines each)
    /// </summary>
    public class HarvestReadinessCalculator
    {
        // Calculation parameters
        private float _harvestReadinessThreshold = 0.85f;
        private float _trichomeReadinessWeight = 0.4f;
        private float _maturityReadinessWeight = 0.35f;
        private float _environmentalReadinessWeight = 0.25f;
        private float _optimalHarvestWindow = 7f; // Days

        // Yield parameters
        private float _baseYieldPerGram = 0.7f;
        private float _environmentalYieldModifier = 1f;
        private float _geneticYieldModifier = 1f;
        private float _careQualityModifier = 1f;

        // Potency parameters
        private float _potencyBaseMultiplier = 0.15f;
        private float _qualityVarianceFactor = 0.1f;
        private bool _considerGeneticPotential = true;

        public void SetParameters(
            float readinessThreshold,
            float trichomeWeight,
            float maturityWeight,
            float envWeight,
            float optimalWindow)
        {
            _harvestReadinessThreshold = Mathf.Clamp01(readinessThreshold);
            _trichomeReadinessWeight = Mathf.Clamp01(trichomeWeight);
            _maturityReadinessWeight = Mathf.Clamp01(maturityWeight);
            _environmentalReadinessWeight = Mathf.Clamp01(envWeight);
            _optimalHarvestWindow = Mathf.Max(1f, optimalWindow);
        }

        public void SetYieldModifiers(float environmental, float genetic, float careQuality)
        {
            _environmentalYieldModifier = Mathf.Max(0.1f, environmental);
            _geneticYieldModifier = Mathf.Max(0.1f, genetic);
            _careQualityModifier = Mathf.Max(0.1f, careQuality);
        }

        /// <summary>
        /// Calculate harvest readiness factors
        /// </summary>
        public HarvestReadinessFactors CalculateReadinessFactors(float plantAge, float maturityLevel, float biomass, float healthFactor)
        {
            return new HarvestReadinessFactors
            {
                TrichomeReadiness = CalculateTrichomeReadiness(plantAge, maturityLevel),
                PistilReadiness = CalculatePistilReadiness(plantAge, maturityLevel),
                CalyxSwelling = CalculateCalyxSwelling(maturityLevel, biomass),
                MaturityScore = maturityLevel,
                EnvironmentalScore = healthFactor
            };
        }

        /// <summary>
        /// Calculate overall readiness score
        /// </summary>
        public float CalculateOverallReadiness(HarvestReadinessFactors factors)
        {
            var readinessFactors = new Dictionary<string, float>
            {
                { "Trichome", factors.TrichomeReadiness * _trichomeReadinessWeight },
                { "Maturity", factors.MaturityScore * _maturityReadinessWeight },
                { "Environmental", factors.EnvironmentalScore * _environmentalReadinessWeight }
            };

            return readinessFactors.Values.Sum();
        }

        /// <summary>
        /// Check if plant is ready for harvest
        /// </summary>
        public bool IsReadyForHarvest(float readinessScore)
        {
            return readinessScore >= _harvestReadinessThreshold;
        }

        /// <summary>
        /// Calculate yield potential
        /// </summary>
        public float CalculateYieldPotential(float biomass, float healthFactor, float maturityLevel)
        {
            var baseYield = biomass * _baseYieldPerGram;
            var modifiedYield = baseYield * _environmentalYieldModifier * _geneticYieldModifier * _careQualityModifier * healthFactor;

            // Maturity penalty for early/late harvest
            var maturityPenalty = CalculateMaturityPenalty(maturityLevel);
            var finalYield = modifiedYield * maturityPenalty;

            return Mathf.Max(0f, finalYield);
        }

        /// <summary>
        /// Calculate potency potential
        /// </summary>
        public float CalculatePotencyPotential(float maturityLevel, float healthFactor)
        {
            var basePotency = _potencyBaseMultiplier;
            var maturityBonus = GetMaturityPotencyBonus(maturityLevel);
            var healthBonus = (healthFactor - 0.5f) * 0.1f; // Health above 50% gives bonus
            var geneticModifier = _considerGeneticPotential ? _geneticYieldModifier : 1f;

            var calculatedPotency = (basePotency + maturityBonus + healthBonus) * geneticModifier;

            // Add quality variance
            var variance = UnityEngine.Random.Range(-_qualityVarianceFactor, _qualityVarianceFactor);
            calculatedPotency += variance;

            return Mathf.Clamp(calculatedPotency, 0.05f, 0.35f); // Realistic potency range 5-35%
        }

        /// <summary>
        /// Calculate optimal harvest date
        /// </summary>
        public DateTime CalculateOptimalHarvestDate(float plantAge, float maturityLevel)
        {
            // Estimate days until full maturity
            var daysUntilOptimal = Mathf.Max(0f, (1f - maturityLevel) * 30f); // Assume 30 days for full maturity

            // Adjust based on plant age (flowering stage)
            if (plantAge < 60f) // If not yet in late flowering
            {
                daysUntilOptimal += (60f - plantAge);
            }

            return DateTime.Now.AddDays(daysUntilOptimal);
        }

        /// <summary>
        /// Calculate harvest window
        /// </summary>
        public (DateTime start, DateTime end) CalculateHarvestWindow(DateTime optimalDate)
        {
            var halfWindow = _optimalHarvestWindow / 2f;
            var start = optimalDate.AddDays(-halfWindow);
            var end = optimalDate.AddDays(halfWindow);
            return (start, end);
        }

        /// <summary>
        /// Calculate actual harvest yield (with readiness penalty)
        /// </summary>
        public float CalculateActualHarvestYield(float estimatedYield, float readinessScore)
        {
            // Apply readiness penalty
            var readinessFactor = Mathf.Lerp(0.5f, 1f, readinessScore); // 50-100% of estimated
            return estimatedYield * readinessFactor;
        }

        /// <summary>
        /// Calculate actual harvest potency (with readiness penalty)
        /// </summary>
        public float CalculateActualHarvestPotency(float estimatedPotency, float readinessScore)
        {
            // Apply readiness penalty
            var readinessFactor = Mathf.Lerp(0.7f, 1f, readinessScore); // 70-100% of estimated
            return estimatedPotency * readinessFactor;
        }

        #region Private Calculation Methods

        /// <summary>
        /// Calculate trichome readiness
        /// </summary>
        private float CalculateTrichomeReadiness(float plantAge, float maturityLevel)
        {
            // Trichomes develop in flowering stage (age > 45 days typically)
            var ageReadiness = Mathf.Clamp01((plantAge - 45f) / 30f); // 30 days for full trichome development
            var maturityReadiness = maturityLevel;

            return (ageReadiness + maturityReadiness) / 2f;
        }

        /// <summary>
        /// Calculate pistil readiness
        /// </summary>
        private float CalculatePistilReadiness(float plantAge, float maturityLevel)
        {
            // Pistils change color as plant matures
            var ageReadiness = Mathf.Clamp01((plantAge - 50f) / 25f); // 25 days for pistil color change
            return ageReadiness * maturityLevel;
        }

        /// <summary>
        /// Calculate calyx swelling
        /// </summary>
        private float CalculateCalyxSwelling(float maturityLevel, float biomass)
        {
            // Calyxes swell with biomass accumulation and maturity
            var biomassInfluence = Mathf.Clamp01(biomass / 50f); // Assume 50g for full swelling
            return (maturityLevel + biomassInfluence) / 2f;
        }

        /// <summary>
        /// Calculate maturity penalty for early/late harvest
        /// </summary>
        private float CalculateMaturityPenalty(float maturityLevel)
        {
            // Optimal maturity range: 0.8 - 1.0
            if (maturityLevel >= 0.8f && maturityLevel <= 1.0f)
            {
                return 1.0f; // No penalty
            }
            else if (maturityLevel < 0.8f)
            {
                // Early harvest penalty
                return Mathf.Lerp(0.5f, 1.0f, maturityLevel / 0.8f);
            }
            else
            {
                // Late harvest penalty (degradation)
                var overMaturity = maturityLevel - 1.0f;
                return Mathf.Max(0.6f, 1.0f - (overMaturity * 0.5f));
            }
        }

        /// <summary>
        /// Get maturity potency bonus
        /// </summary>
        private float GetMaturityPotencyBonus(float maturityLevel)
        {
            // Peak potency at 90-100% maturity
            if (maturityLevel >= 0.9f && maturityLevel <= 1.0f)
            {
                return 0.1f; // 10% bonus
            }
            else if (maturityLevel >= 0.8f)
            {
                return Mathf.Lerp(0f, 0.1f, (maturityLevel - 0.8f) / 0.1f);
            }
            else
            {
                return 0f; // No bonus for immature plants
            }
        }

        #endregion

        #region Quality Prediction

        /// <summary>
        /// Predict harvest quality based on readiness
        /// </summary>
        public HarvestQualityGrade PredictHarvestQuality(float readinessScore)
        {
            if (readinessScore >= 0.95f)
                return HarvestQualityGrade.Premium;
            else if (readinessScore >= 0.85f)
                return HarvestQualityGrade.Excellent;
            else if (readinessScore >= 0.75f)
                return HarvestQualityGrade.Good;
            else if (readinessScore >= 0.60f)
                return HarvestQualityGrade.Fair;
            else
                return HarvestQualityGrade.Poor;
        }

        /// <summary>
        /// Determine harvest quality from actual results
        /// </summary>
        public HarvestQualityGrade DetermineHarvestQuality(float readinessScore, float yield, float potency)
        {
            // Base quality on readiness
            var baseQuality = PredictHarvestQuality(readinessScore);

            // Adjust based on yield and potency
            var yieldFactor = yield > 30f ? 1 : 0; // Bonus for high yield
            var potencyFactor = potency > 0.20f ? 1 : 0; // Bonus for high potency

            var qualityAdjustment = yieldFactor + potencyFactor;
            var adjustedQuality = (int)baseQuality + qualityAdjustment;

            // Clamp to valid range
            adjustedQuality = Mathf.Clamp(adjustedQuality, 0, 5);

            return (HarvestQualityGrade)adjustedQuality;
        }

        #endregion
    }
}
