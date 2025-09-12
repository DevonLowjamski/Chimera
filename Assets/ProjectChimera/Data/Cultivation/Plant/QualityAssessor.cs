using UnityEngine;
using System;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Quality Assessor - Assesses harvest quality and characteristics.
    /// Analyzes trichome development, cannabinoid profiles, terpene content,
    /// appearance, aroma, and overall quality metrics for harvested cannabis.
    /// </summary>
    public static class QualityAssessor
    {
        /// <summary>
        /// Calculates harvest quality based on plant state
        /// </summary>
        public static HarvestQuality CalculateHarvestQuality(PlantStateData plantState)
        {
            var quality = new HarvestQuality
            {
                PlantID = plantState.PlantID,
                OverallQualityScore = 0f,
                QualityGrade = QualityGrade.Fair,
                QualityFactors = new QualityFactors()
            };

            // Calculate individual quality components
            quality.QualityFactors.TrichomeQuality = AssessTrichomeQuality(plantState);
            quality.QualityFactors.CannabinoidProfile = AssessCannabinoidProfile(plantState);
            quality.QualityFactors.TerpeneProfile = AssessTerpeneProfile(plantState);
            quality.QualityFactors.Appearance = AssessAppearance(plantState);
            quality.QualityFactors.Aroma = AssessAroma(plantState);

            // Calculate weighted overall score
            quality.OverallQualityScore = CalculateWeightedScore(quality.QualityFactors);

            // Determine quality grade
            quality.QualityGrade = DetermineQualityGrade(quality.OverallQualityScore);

            // Generate quality description
            quality.QualityDescription = GenerateQualityDescription(quality);

            // Calculate quality metrics
            quality.QualityMetrics = CalculateQualityMetrics(plantState, quality);

            return quality;
        }

        /// <summary>
        /// Assesses trichome quality and development
        /// </summary>
        private static float AssessTrichomeQuality(PlantStateData plantState)
        {
            float trichomeQuality = plantState.MaturityLevel;

            // Trichome size and density contribute to quality
            if (plantState.Strain?.TrichomeDensity != null)
            {
                trichomeQuality *= plantState.Strain.TrichomeDensity;
            }

            // Harvest timing affects trichome quality
            if (plantState.CurrentGrowthStage == PlantGrowthStage.Ripening)
            {
                trichomeQuality *= 1.1f; // Optimal timing
            }
            else if (plantState.CurrentGrowthStage == PlantGrowthStage.Flowering)
            {
                trichomeQuality *= 0.9f; // Slightly early
            }

            // Environmental factors
            if (plantState.Environment?.Humidity < 40f)
            {
                trichomeQuality *= 0.95f; // Low humidity can stress trichomes
            }

            return Mathf.Clamp01(trichomeQuality);
        }

        /// <summary>
        /// Assesses cannabinoid profile quality
        /// </summary>
        private static float AssessCannabinoidProfile(PlantStateData plantState)
        {
            float cannabinoidQuality = 0.7f; // Base quality

            if (plantState.Strain != null)
            {
                // THC content affects quality perception
                if (plantState.Strain.THCPotential > 0)
                {
                    cannabinoidQuality = Mathf.Lerp(0.6f, 1.0f, plantState.Strain.THCPotential);
                }

                // CBD content for balanced profiles
                if (plantState.Strain.CBDPotential > 0)
                {
                    cannabinoidQuality *= 1.1f; // Balanced profiles often preferred
                }
            }

            // Harvest timing affects cannabinoid expression
            if (plantState.MaturityLevel > 0.8f)
            {
                cannabinoidQuality *= 1.05f; // Peak cannabinoid expression
            }

            // Environmental stress can affect cannabinoid production
            if (plantState.Environment?.Temperature > 30f)
            {
                cannabinoidQuality *= 0.9f; // Heat stress reduces quality
            }

            return Mathf.Clamp01(cannabinoidQuality);
        }

        /// <summary>
        /// Assesses terpene profile quality
        /// </summary>
        private static float AssessTerpeneProfile(PlantStateData plantState)
        {
            float terpeneQuality = 0.6f; // Base quality

            if (plantState.Strain?.TerpeneProfile != null)
            {
                // Rich terpene profiles contribute to quality
                terpeneQuality = plantState.Strain.TerpeneProfile.ComplexityScore;
            }

            // Environmental conditions affect terpene production
            if (plantState.Environment?.Temperature >= 20f && plantState.Environment?.Temperature <= 28f)
            {
                terpeneQuality *= 1.1f; // Optimal temperature for terpenes
            }

            // Light quality affects terpene production
            if (plantState.Environment?.LightIntensity >= 0.7f)
            {
                terpeneQuality *= 1.05f; // Good light for terpene development
            }

            return Mathf.Clamp01(terpeneQuality);
        }

        /// <summary>
        /// Assesses visual appearance quality
        /// </summary>
        private static float AssessAppearance(PlantStateData plantState)
        {
            float appearanceQuality = 0.8f; // Base quality

            // Plant structure and symmetry
            if (plantState.TrainingTechniques != null && plantState.TrainingTechniques.Count > 0)
            {
                appearanceQuality *= 1.1f; // Well-trained plants look better
            }

            // Color and pigmentation
            if (plantState.MaturityLevel > 0.7f)
            {
                appearanceQuality *= 1.05f; // Mature plants have better coloration
            }

            // Overall plant health affects appearance
            appearanceQuality *= plantState.OverallHealth;

            return Mathf.Clamp01(appearanceQuality);
        }

        /// <summary>
        /// Assesses aroma quality
        /// </summary>
        private static float AssessAroma(PlantStateData plantState)
        {
            float aromaQuality = 0.7f; // Base quality

            // Terpene profile directly affects aroma
            if (plantState.Strain?.TerpeneProfile != null)
            {
                aromaQuality = plantState.Strain.TerpeneProfile.AromaIntensity;
            }

            // Curing process affects aroma development
            if (plantState.MaturityLevel > 0.8f)
            {
                aromaQuality *= 1.1f; // Proper maturation enhances aroma
            }

            // Environmental conditions during growth
            if (plantState.Environment?.Humidity >= 40f && plantState.Environment?.Humidity <= 60f)
            {
                aromaQuality *= 1.05f; // Optimal humidity for aroma development
            }

            return Mathf.Clamp01(aromaQuality);
        }

        /// <summary>
        /// Calculates weighted overall quality score
        /// </summary>
        private static float CalculateWeightedScore(QualityFactors factors)
        {
            // Weighted calculation based on industry standards
            float weightedScore =
                (factors.TrichomeQuality * 0.3f) +      // 30% - Trichomes most important
                (factors.CannabinoidProfile * 0.25f) +   // 25% - Cannabinoids key
                (factors.TerpeneProfile * 0.2f) +        // 20% - Terpenes for aroma/flavor
                (factors.Appearance * 0.15f) +           // 15% - Visual appeal
                (factors.Aroma * 0.1f);                  // 10% - Aroma quality

            return Mathf.Clamp01(weightedScore);
        }

        /// <summary>
        /// Determines quality grade from score
        /// </summary>
        private static QualityGrade DetermineQualityGrade(float score)
        {
            if (score >= 0.9f) return QualityGrade.Premium;
            if (score >= 0.8f) return QualityGrade.Excellent;
            if (score >= 0.7f) return QualityGrade.Good;
            if (score >= 0.6f) return QualityGrade.Fair;
            return QualityGrade.Poor;
        }

        /// <summary>
        /// Generates quality description
        /// </summary>
        private static string GenerateQualityDescription(HarvestQuality quality)
        {
            switch (quality.QualityGradeEnum)
            {
                case QualityGrade.Premium:
                    return "Exceptional quality with outstanding trichome development, balanced cannabinoid profile, and rich terpene expression. Premium market material.";
                case QualityGrade.Excellent:
                    return "High-quality harvest with well-developed trichomes, good cannabinoid content, and pleasant aroma profile.";
                case QualityGrade.Good:
                    return "Solid quality harvest with decent trichome coverage and acceptable cannabinoid/terpene profiles.";
                case QualityGrade.Fair:
                    return "Average quality harvest that meets basic standards but lacks premium characteristics.";
                case QualityGrade.Poor:
                    return "Below-standard quality with underdeveloped trichomes and weak cannabinoid/terpene profiles.";
                default:
                    return "Quality assessment incomplete";
            }
        }

        /// <summary>
        /// Calculates detailed quality metrics
        /// </summary>
        private static QualityMetrics CalculateQualityMetrics(PlantStateData plantState, HarvestQuality quality)
        {
            var metrics = new QualityMetrics();

            // Potency metrics
            if (plantState.Strain != null)
            {
                metrics.EstimatedTHC = plantState.Strain.THCPotential * quality.OverallQualityScore * 25f; // Max 25%
                metrics.EstimatedCBD = plantState.Strain.CBDPotential * quality.OverallQualityScore * 20f; // Max 20%
            }

            // Terpene content estimate
            metrics.EstimatedTerpeneContent = quality.QualityFactors.TerpeneProfile * 2.5f; // Max 2.5%

            // Market value indicators
            metrics.MarketGrade = DetermineMarketGrade(quality.QualityGrade);

            // Quality consistency
            metrics.ConsistencyScore = CalculateConsistencyScore(plantState, quality);

            return metrics;
        }

        /// <summary>
        /// Determines market grade from quality grade
        /// </summary>
        private static string DetermineMarketGrade(QualityGrade qualityGrade)
        {
            switch (qualityGrade)
            {
                case QualityGrade.Premium: return "AAA Premium";
                case QualityGrade.Excellent: return "AA Grade";
                case QualityGrade.Good: return "A Grade";
                case QualityGrade.Fair: return "B Grade";
                case QualityGrade.Poor: return "C Grade";
                default: return "Ungraded";
            }
        }

        /// <summary>
        /// Calculates quality consistency score
        /// </summary>
        private static float CalculateConsistencyScore(PlantStateData plantState, HarvestQuality quality)
        {
            // Consistency based on cultivation practices and environmental stability
            float consistency = 0.8f; // Base consistency

            // Environmental stability affects consistency
            if (plantState.Environment != null)
            {
                if (Mathf.Abs(plantState.Environment.Temperature - 25f) < 5f)
                    consistency += 0.1f;
                if (Mathf.Abs(plantState.Environment.Humidity - 60f) < 10f)
                    consistency += 0.1f;
            }

            // Cultivation consistency
            if (plantState.IrrigationConsistency > 0.8f)
                consistency += 0.1f;
            if (plantState.NutrientConsistency > 0.8f)
                consistency += 0.1f;

            return Mathf.Clamp01(consistency);
        }

        /// <summary>
        /// Compares quality between different harvests
        /// </summary>
        public static QualityComparison CompareQuality(HarvestQuality quality1, HarvestQuality quality2)
        {
            var comparison = new QualityComparison
            {
                Quality1 = quality1,
                Quality2 = quality2,
                ScoreDifference = quality2.OverallQualityScore - quality1.OverallQualityScore,
                GradeDifference = (int)quality2.QualityGrade - (int)quality1.QualityGrade
            };

            // Determine improvement direction
            if (comparison.ScoreDifference > 0.1f)
            {
                comparison.ImprovementDirection = "Significant Improvement";
                comparison.ImprovementColor = Color.green;
            }
            else if (comparison.ScoreDifference > 0.05f)
            {
                comparison.ImprovementDirection = "Moderate Improvement";
                comparison.ImprovementColor = new Color(0.5f, 1f, 0.5f);
            }
            else if (comparison.ScoreDifference > -0.05f)
            {
                comparison.ImprovementDirection = "Stable Quality";
                comparison.ImprovementColor = Color.yellow;
            }
            else if (comparison.ScoreDifference > -0.1f)
            {
                comparison.ImprovementDirection = "Moderate Decline";
                comparison.ImprovementColor = new Color(1f, 0.7f, 0f);
            }
            else
            {
                comparison.ImprovementDirection = "Significant Decline";
                comparison.ImprovementColor = Color.red;
            }

            return comparison;
        }
    }
}
