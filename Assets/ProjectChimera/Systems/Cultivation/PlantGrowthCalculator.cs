using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;
using GeneticPlantStrainSO = ProjectChimera.Data.Genetics.GeneticPlantStrainSO;
using CultivationPlantStrainSO = ProjectChimera.Data.Cultivation.PlantStrainSO;
// HarvestResults class is defined in CultivationSystemTypes.cs within the same namespace

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Calculates plant growth rates and progression based on multiple factors.
    /// Handles growth rate calculations, harvest results, and phenotypic modifiers.
    /// </summary>
    public class PlantGrowthCalculator
    {
        private object _strain;
        private PhenotypicTraits _traits;
        private AnimationCurve _growthCurve;
        private float _strainGrowthModifier = 1f;
        private PlantUpdateConfiguration _configuration;

        /// <summary>
        /// Initialize the growth calculator with strain and trait data
        /// </summary>
        public void Initialize(object strain, PhenotypicTraits traits, PlantUpdateConfiguration configuration = null)
        {
            _strain = strain;
            _traits = traits ?? new PhenotypicTraits();
            _configuration = configuration ?? PlantUpdateConfiguration.CreateDefault();

            // Initialize growth curve from strain data or use default
            _growthCurve = CreateGrowthCurveFromStrain(strain) ?? CreateDefaultGrowthCurve();

            // Extract strain growth modifier
            ExtractStrainModifiers(strain);

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        /// <summary>
        /// Calculates the growth rate for the current frame
        /// </summary>
        public float CalculateGrowthRate(PlantGrowthStage stage, float environmentalFitness, float health, float globalModifier)
        {
            // Base growth rate from strain genetics
            float baseRate = GetBaseGrowthRateForStage(stage);

            // Apply environmental fitness
            float environmentalModifier = CalculateEnvironmentalModifier(environmentalFitness, stage);

            // Apply health modifier with stage-specific sensitivity
            float healthModifier = CalculateHealthModifier(health, stage);

            // Apply strain-specific growth characteristics
            float strainModifier = _strainGrowthModifier;

            // Apply phenotypic expression
            float phenotypeModifier = CalculatePhenotypeGrowthModifier(stage);

            // Apply growth curve evaluation based on plant age/development
            float curveModifier = EvaluateGrowthCurve(stage);

            // Combine all modifiers
            float finalRate = baseRate * environmentalModifier * healthModifier *
                             strainModifier * phenotypeModifier * curveModifier * globalModifier;

            // Ensure non-negative growth rate
            float clampedRate = Mathf.Max(0f, finalRate);

            // Log significant growth rate changes for debugging
            if (_configuration.EnablePerformanceOptimization && clampedRate != finalRate)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }

            return clampedRate;
        }

        /// <summary>
        /// Calculate stage-specific growth progression
        /// </summary>
        public float CalculateStageProgression(PlantGrowthStage stage, float currentProgress, float deltaTime, float growthRate)
        {
            float stageDuration = GetStageDuration(stage);
            float progressionRate = growthRate / stageDuration;

            // Apply diminishing returns as stage nears completion
            float diminishingFactor = CalculateDiminishingReturns(currentProgress);

            float progressDelta = progressionRate * diminishingFactor * deltaTime;
            float newProgress = Mathf.Clamp01(currentProgress + progressDelta);

            return newProgress;
        }

        /// <summary>
        /// Calculates harvest results based on plant's final state
        /// </summary>
        public HarvestResults CalculateHarvestResults(float finalHealth, float qualityPotential, PhenotypicTraits traits,
            float environmentalFitness, int totalDaysGrown)
        {
            var results = new HarvestResults
            {
                FinalHealth = finalHealth,
                QualityScore = CalculateQualityScore(finalHealth, qualityPotential, environmentalFitness),
                FloweringDays = (int)traits.FloweringTime,
                HarvestDate = System.DateTime.Now,
                TotalDaysGrown = totalDaysGrown
            };

            // Calculate yield based on multiple factors
            results.TotalYield = CalculateYield(finalHealth, traits, environmentalFitness);

            // Calculate bud quality metrics
            results.BudDensity = CalculateBudDensity(finalHealth, traits);
            results.TrichomeProduction = CalculateTrichomeProduction(finalHealth, traits, qualityPotential);

            // Calculate cannabinoid and terpene profiles
            var cannabinoidProfile = CalculateCannabinoidProfile(finalHealth, qualityPotential, environmentalFitness);
            results.CannabinoidProfile["THC"] = cannabinoidProfile.THC;
            results.CannabinoidProfile["CBD"] = cannabinoidProfile.CBD;
            results.CannabinoidProfile["CBG"] = cannabinoidProfile.CBG;
            results.CannabinoidProfile["CBN"] = cannabinoidProfile.CBN;

            var terpeneProfile = CalculateTerpeneProfile(finalHealth, qualityPotential, environmentalFitness);
            results.Terpenes["Myrcene"] = terpeneProfile.Myrcene;
            results.Terpenes["Limonene"] = terpeneProfile.Limonene;
            results.Terpenes["Pinene"] = terpeneProfile.Pinene;
            results.Terpenes["Linalool"] = terpeneProfile.Linalool;

            // Calculate market value based on quality and yield
            results.EstimatedValue = CalculateMarketValue(results);

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

            return results;
        }

        #region Private Growth Calculation Methods

        private float GetBaseGrowthRateForStage(PlantGrowthStage stage)
        {
            // Different stages have different base growth rates
            return stage switch
            {
                PlantGrowthStage.Seed => 0.005f, // Very slow initial growth
                PlantGrowthStage.Germination => 0.01f,
                PlantGrowthStage.Seedling => 0.015f,
                PlantGrowthStage.Vegetative => 0.02f, // Peak growth rate
                PlantGrowthStage.Flowering => 0.012f, // Slower during flowering
                PlantGrowthStage.Harvest => 0f, // No growth when ready for harvest
                _ => 0.01f
            };
        }

        private float CalculateEnvironmentalModifier(float environmentalFitness, PlantGrowthStage stage)
        {
            // Environmental effects vary by growth stage
            float baseModifier = Mathf.Lerp(0.2f, 1.5f, environmentalFitness);

            // Some stages are more sensitive to environment
            float stageSensitivity = stage switch
            {
                PlantGrowthStage.Seedling => 1.3f, // Very sensitive
                PlantGrowthStage.Flowering => 1.2f, // Sensitive
                PlantGrowthStage.Vegetative => 1.0f, // Normal
                _ => 0.8f // Less sensitive
            };

            return baseModifier * stageSensitivity;
        }

        private float CalculateHealthModifier(float health, PlantGrowthStage stage)
        {
            float baseModifier = Mathf.Lerp(0.1f, 1.2f, health);

            // Health effects are more pronounced in later stages
            float stageHealthSensitivity = stage switch
            {
                PlantGrowthStage.Flowering => 1.3f, // Health critical during flowering
                PlantGrowthStage.Vegetative => 1.1f, // Somewhat important
                _ => 1.0f // Normal importance
            };

            return baseModifier * stageHealthSensitivity;
        }

        private float CalculatePhenotypeGrowthModifier(PlantGrowthStage stage)
        {
            if (_traits == null)
                return 1f;

            // Different traits affect growth at different stages
            return stage switch
            {
                PlantGrowthStage.Vegetative => Mathf.Lerp(0.8f, 1.3f, _traits.YieldMultiplier * _traits.GrowthRateMultiplier),
                PlantGrowthStage.Flowering => Mathf.Lerp(0.9f, 1.2f, _traits.PotencyMultiplier),
                PlantGrowthStage.Seedling => Mathf.Lerp(0.9f, 1.1f, _traits.GrowthRateMultiplier),
                _ => 1f
            };
        }

        private float EvaluateGrowthCurve(PlantGrowthStage stage)
        {
            if (_growthCurve == null)
                return 1f;

            // Map growth stage to curve time (0-1)
            float curveTime = stage switch
            {
                PlantGrowthStage.Seed => 0f,
                PlantGrowthStage.Germination => 0.1f,
                PlantGrowthStage.Seedling => 0.25f,
                PlantGrowthStage.Vegetative => 0.6f,
                PlantGrowthStage.Flowering => 0.9f,
                PlantGrowthStage.Harvest => 1f,
                _ => 0.5f
            };

            return _growthCurve.Evaluate(curveTime);
        }

        private float GetStageDuration(PlantGrowthStage stage)
        {
            // Duration in days for each stage
            return stage switch
            {
                PlantGrowthStage.Seed => 1f,
                PlantGrowthStage.Germination => 3f,
                PlantGrowthStage.Seedling => 7f,
                PlantGrowthStage.Vegetative => 35f,
                PlantGrowthStage.Flowering => _traits?.FloweringTime ?? 60f,
                PlantGrowthStage.Harvest => 1f,
                _ => 7f
            };
        }

        private float CalculateDiminishingReturns(float progress)
        {
            // Diminishing returns as stage nears completion
            if (progress < 0.8f)
                return 1f; // Normal growth
            else
                return Mathf.Lerp(1f, 0.3f, (progress - 0.8f) / 0.2f); // Slow down near completion
        }

        #endregion

        #region Private Harvest Calculation Methods

        private float CalculateYield(float health, PhenotypicTraits traits, float environmentalFitness)
        {
            float baseYield = (_strain as CultivationPlantStrainSO)?.BaseYieldGrams ?? 100f;
            float healthModifier = Mathf.Lerp(0.3f, 1.2f, health);
            float traitModifier = traits.YieldMultiplier;
            float environmentalModifier = Mathf.Lerp(0.5f, 1.1f, environmentalFitness);

            return baseYield * healthModifier * traitModifier * environmentalModifier;
        }

        private float CalculateQualityScore(float health, float qualityPotential, float environmentalFitness)
        {
            float healthComponent = health * 0.3f;
            float potentialComponent = qualityPotential * 0.4f;
            float environmentalComponent = environmentalFitness * 0.3f;

            return Mathf.Clamp01(healthComponent + potentialComponent + environmentalComponent);
        }

        private float CalculateBudDensity(float health, PhenotypicTraits traits)
        {
            float baseDensity = traits.BudDensity;
            float healthModifier = Mathf.Lerp(0.6f, 1.0f, health);

            return baseDensity * healthModifier;
        }

        private float CalculateTrichomeProduction(float health, PhenotypicTraits traits, float qualityPotential)
        {
            float baseProduction = traits.TrichromeProduction;
            float healthModifier = Mathf.Lerp(0.4f, 1.0f, health);
            float qualityModifier = Mathf.Lerp(0.8f, 1.2f, qualityPotential);

            return baseProduction * healthModifier * qualityModifier;
        }

        private CannabinoidProfile CalculateCannabinoidProfile(float health, float quality, float environmentalFitness)
        {
            if (_strain == null)
                return new CannabinoidProfile();

            var strainSO = _strain as ProjectChimera.Data.Genetics.GeneticPlantStrainSO;
            float combinedModifier = health * quality * environmentalFitness;

            var profile = new CannabinoidProfile
            {
                THC = (strainSO?.thcContent ?? 15f) * combinedModifier / 100f, // Convert percentage to decimal
                CBD = (strainSO?.cbdContent ?? 1f) * combinedModifier / 100f, // Convert percentage to decimal
                CBG = 0.5f * combinedModifier / 100f, // Default CBG value
                CBN = 0.1f * combinedModifier / 100f  // Default CBN value
            };

            return profile;
        }

        private TerpeneProfile CalculateTerpeneProfile(float health, float quality, float environmentalFitness)
        {
            if (_strain == null)
                return new TerpeneProfile();

            float combinedModifier = health * quality * environmentalFitness;

            var profile = new TerpeneProfile
            {
                Myrcene = 0.5f * combinedModifier,     // Default Myrcene value
                Limonene = 0.3f * combinedModifier,    // Default Limonene value
                Pinene = 0.2f * combinedModifier,      // Default Pinene value
                Linalool = 0.1f * combinedModifier     // Default Linalool value
            };

            return profile;
        }

        private float CalculateMarketValue(HarvestResults results)
        {
            // Base price per gram (this would come from market data)
            float basePricePerGram = 10f;

            // Quality multiplier
            float qualityMultiplier = Mathf.Lerp(0.5f, 2.0f, results.QualityScore);

            // Premium for high THC
            float thcPremium = 1f;
            if (results.CannabinoidProfile.TryGetValue("THC", out float thc) && thc > 0.2f)
            {
                thcPremium = 1.2f; // 20% premium for high THC
            }

            return results.TotalYield * basePricePerGram * qualityMultiplier * thcPremium;
        }

        #endregion

        #region Private Helper Methods

        private void ExtractStrainModifiers(object strain)
        {
            try
            {
                // Try to extract growth modifier from strain
                if (strain is PlantStrainSO strainSO)
                {
                    // _strainGrowthModifier = strainSO.GrowthRateModifier; // Property doesn't exist yet
                    _strainGrowthModifier = 1f; // Default until property is implemented
                }
                else
                {
                    _strainGrowthModifier = 1f;
                }
            }
            catch
            {
                _strainGrowthModifier = 1f;
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        private AnimationCurve CreateGrowthCurveFromStrain(object strain)
        {
            // TODO: Extract growth curve from strain data when available
            // For now, return null to use default curve
            return null;
        }

        private AnimationCurve CreateDefaultGrowthCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);     // Seed/Germination
            curve.AddKey(0.2f, 0.1f); // Early seedling
            curve.AddKey(0.5f, 0.5f); // Vegetative growth peak
            curve.AddKey(0.8f, 0.9f); // Late vegetative
            curve.AddKey(1f, 1f);     // Flowering complete

            // Smooth the curve
            for (int i = 0; i < curve.keys.Length; i++)
            {
                curve.SmoothTangents(i, 0.3f);
            }

            return curve;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Get the expected yield range for current configuration
        /// </summary>
        public (float min, float max) GetExpectedYieldRange()
        {
            if (_strain == null || _traits == null)
                return (50f, 150f); // Default range

            float baseYield = (_strain as CultivationPlantStrainSO)?.BaseYieldGrams ?? 100f;
            float traitModifier = _traits.YieldMultiplier;

            float minYield = baseYield * traitModifier * 0.3f; // Worst case (poor health/environment)
            float maxYield = baseYield * traitModifier * 1.3f; // Best case (perfect conditions)

            return (minYield, maxYield);
        }

        /// <summary>
        /// Get the expected flowering duration
        /// </summary>
        public float GetExpectedFloweringDuration()
        {
            return _traits?.FloweringTime ?? 60f;
        }

        /// <summary>
        /// Check if the strain is considered high-yielding
        /// </summary>
        public bool IsHighYieldingStrain()
        {
            var (min, max) = GetExpectedYieldRange();
            return max > 120f; // Consider high-yielding if max yield is over 120g
        }

        #endregion
    }
}
