using UnityEngine;
using ProjectChimera.Data.Processing;
using System;

namespace ProjectChimera.Systems.Processing
{
    /// <summary>
    /// Processing quality calculator - advanced quality analysis and predictions.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Every decision affects final quality - temperature, humidity, timing"
    ///
    /// **Quality Factors**:
    /// - Initial harvest quality (40% weight)
    /// - Drying conditions (30% weight)
    /// - Curing process (30% weight)
    ///
    /// **Quality Grading**:
    /// - Premium+ (95-100): Perfect process, premium pricing
    /// - Premium (90-95): Excellent process, high pricing
    /// - Excellent (80-90): Good process, above-average pricing
    /// - Good (70-80): Decent process, average pricing
    /// - Fair (60-70): Acceptable process, below-average pricing
    /// - Poor (<60): Issues occurred, low pricing
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Quality: 88% (Excellent) - $45/g market value"
    /// Behind scenes: Multi-factor quality models, degradation curves, market pricing.
    /// </summary>
    public static class ProcessingQualityCalculator
    {
        // Quality weights
        private const float HARVEST_QUALITY_WEIGHT = 0.40f;
        private const float DRYING_QUALITY_WEIGHT = 0.30f;
        private const float CURING_QUALITY_WEIGHT = 0.30f;

        // Quality thresholds
        private const float PREMIUM_PLUS_THRESHOLD = 95f;
        private const float PREMIUM_THRESHOLD = 90f;
        private const float EXCELLENT_THRESHOLD = 80f;
        private const float GOOD_THRESHOLD = 70f;
        private const float FAIR_THRESHOLD = 60f;

        #region Quality Prediction

        /// <summary>
        /// Predicts final quality based on current batch state.
        ///
        /// GAMEPLAY:
        /// - Player checks batch mid-process
        /// - Sees projected final quality
        /// - Can adjust conditions if needed
        /// </summary>
        public static float PredictFinalQuality(ProcessingBatch batch, DryingConditions? dryingConditions = null)
        {
            // Base quality from harvest
            float harvestQuality = batch.InitialQuality * HARVEST_QUALITY_WEIGHT;

            // Drying quality contribution
            float dryingQuality = 0f;
            if (batch.Stage >= ProcessingStage.Drying)
            {
                if (dryingConditions.HasValue)
                {
                    dryingQuality = CalculateDryingQualityContribution(batch, dryingConditions.Value);
                }
                else
                {
                    // Use current batch data
                    dryingQuality = (batch.CurrentQuality - batch.InitialQuality) * DRYING_QUALITY_WEIGHT;
                }
            }

            // Curing quality contribution (projected)
            float curingQuality = 0f;
            if (batch.Stage >= ProcessingStage.Curing)
            {
                curingQuality = CalculateCuringQualityContribution(batch);
            }
            else
            {
                // Assume ideal curing
                curingQuality = batch.InitialQuality * CURING_QUALITY_WEIGHT * 1.1f; // +10% potential
            }

            float predictedQuality = harvestQuality + dryingQuality + curingQuality;
            return Mathf.Clamp(predictedQuality, 0f, 100f);
        }

        /// <summary>
        /// Calculates drying quality contribution.
        /// </summary>
        private static float CalculateDryingQualityContribution(ProcessingBatch batch, DryingConditions conditions)
        {
            float conditionScore = conditions.GetQualityScore();

            // Perfect conditions = +10% quality
            // Poor conditions = -15% quality
            float qualityModifier = (conditionScore - 0.5f) * 50f; // -25% to +25% range

            // Apply mold/over-dry penalties
            qualityModifier -= batch.MoldRisk * 20f;
            qualityModifier -= batch.OverDryRisk * 15f;

            float contribution = (batch.InitialQuality + qualityModifier) * DRYING_QUALITY_WEIGHT;
            return Mathf.Clamp(contribution, 0f, 100f * DRYING_QUALITY_WEIGHT);
        }

        /// <summary>
        /// Calculates curing quality contribution.
        /// </summary>
        private static float CalculateCuringQualityContribution(ProcessingBatch batch)
        {
            // Base quality after drying
            float baseQuality = batch.CurrentQuality;

            // Curing improvement based on time
            float weeklyImprovement = GetWeeklyImprovement(batch.CuringWeeksElapsed);
            float totalImprovement = weeklyImprovement * batch.CuringWeeksElapsed;

            // Humidity penalties/bonuses
            float humidityModifier = 0f;
            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
            {
                humidityModifier = 5f; // +5% for perfect humidity
            }
            else if (batch.JarHumidity > 0.70f)
            {
                humidityModifier = -10f; // -10% for mold risk
            }
            else if (batch.JarHumidity < 0.55f)
            {
                humidityModifier = -5f; // -5% for over-dry
            }

            float contribution = (baseQuality + totalImprovement + humidityModifier) * CURING_QUALITY_WEIGHT;
            return Mathf.Clamp(contribution, 0f, 100f * CURING_QUALITY_WEIGHT);
        }

        /// <summary>
        /// Gets weekly quality improvement rate.
        /// </summary>
        private static float GetWeeklyImprovement(int weeksElapsed)
        {
            if (weeksElapsed < 2) return 2.0f;
            if (weeksElapsed < 4) return 1.5f;
            if (weeksElapsed < 6) return 1.0f;
            if (weeksElapsed < 8) return 0.5f;
            return 0.2f;
        }

        #endregion

        #region Quality Analysis

        /// <summary>
        /// Analyzes batch quality and provides breakdown.
        /// </summary>
        public static QualityAnalysis AnalyzeQuality(ProcessingBatch batch)
        {
            var analysis = new QualityAnalysis
            {
                BatchId = batch.BatchId,
                CurrentQuality = batch.CurrentQuality,
                QualityGrade = GetQualityGrade(batch.CurrentQuality),
                HarvestQualityScore = batch.InitialQuality,
                DryingQualityScore = CalculateDryingScore(batch),
                CuringQualityScore = CalculateCuringScore(batch),
                PotencyRetention = CalculatePotencyRetention(batch),
                TerpeneRetention = CalculateTerpeneRetention(batch),
                AppearanceScore = CalculateAppearanceScore(batch),
                AromaScore = CalculateAromaScore(batch),
                MoldRisk = batch.MoldRisk,
                OverDryRisk = batch.OverDryRisk,
                MarketValueMultiplier = GetMarketValueMultiplier(batch.CurrentQuality)
            };

            return analysis;
        }

        /// <summary>
        /// Gets quality grade from score.
        /// </summary>
        public static string GetQualityGrade(float qualityScore)
        {
            if (qualityScore >= PREMIUM_PLUS_THRESHOLD) return "Premium+";
            if (qualityScore >= PREMIUM_THRESHOLD) return "Premium";
            if (qualityScore >= EXCELLENT_THRESHOLD) return "Excellent";
            if (qualityScore >= GOOD_THRESHOLD) return "Good";
            if (qualityScore >= FAIR_THRESHOLD) return "Fair";
            return "Poor";
        }

        /// <summary>
        /// Calculates drying process score (0-100).
        /// </summary>
        private static float CalculateDryingScore(ProcessingBatch batch)
        {
            float baseScore = 70f; // Start at 70%

            // Moisture target achievement
            if (batch.MoistureContent >= 0.10f && batch.MoistureContent <= 0.12f)
                baseScore += 15f; // +15% for hitting target
            else
                baseScore -= Mathf.Abs(batch.MoistureContent - 0.11f) * 50f;

            // Time efficiency
            if (batch.DryingDaysElapsed >= 7 && batch.DryingDaysElapsed <= 10)
                baseScore += 10f; // +10% for ideal time
            else if (batch.DryingDaysElapsed > 14)
                baseScore -= 5f; // -5% for too long

            // Risk penalties
            baseScore -= batch.MoldRisk * 20f;
            baseScore -= batch.OverDryRisk * 15f;

            return Mathf.Clamp(baseScore, 0f, 100f);
        }

        /// <summary>
        /// Calculates curing process score (0-100).
        /// </summary>
        private static float CalculateCuringScore(ProcessingBatch batch)
        {
            if (batch.Stage < ProcessingStage.Curing)
                return 0f;

            float baseScore = 70f; // Start at 70%

            // Duration bonus
            if (batch.CuringWeeksElapsed >= 6)
                baseScore += 15f; // +15% for extended cure
            else if (batch.CuringWeeksElapsed >= 4)
                baseScore += 10f; // +10% for optimal cure
            else if (batch.CuringWeeksElapsed >= 2)
                baseScore += 5f;  // +5% for minimum cure

            // Humidity control
            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
                baseScore += 10f; // +10% for perfect humidity
            else if (batch.JarHumidity > 0.70f)
                baseScore -= 15f; // -15% for mold risk
            else if (batch.JarHumidity < 0.55f)
                baseScore -= 10f; // -10% for over-dry

            // Risk penalties
            baseScore -= batch.MoldRisk * 15f;

            return Mathf.Clamp(baseScore, 0f, 100f);
        }

        /// <summary>
        /// Calculates potency retention (0-1).
        /// </summary>
        private static float CalculatePotencyRetention(ProcessingBatch batch)
        {
            float retention = 1.0f;

            // Drying affects potency
            retention -= batch.OverDryRisk * 0.2f;    // Over-drying degrades cannabinoids
            retention -= batch.MoldRisk * 0.3f;       // Mold damages cannabinoids

            // Light exposure (if tracked)
            // retention -= lightExposure * 0.15f;

            return Mathf.Clamp01(retention);
        }

        /// <summary>
        /// Calculates terpene retention (0-1).
        /// </summary>
        private static float CalculateTerpeneRetention(ProcessingBatch batch)
        {
            float retention = 1.0f;

            // Terpenes are volatile - very sensitive to conditions
            retention -= batch.OverDryRisk * 0.4f;    // Over-drying evaporates terpenes
            retention -= batch.MoldRisk * 0.2f;       // Mold damages terpenes

            // High temps degrade terpenes
            if (batch.AverageTemp > 24f)
                retention -= (batch.AverageTemp - 24f) * 0.05f;

            // Ideal curing preserves terpenes
            if (batch.Stage >= ProcessingStage.Curing && batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
                retention += 0.05f; // +5% for ideal cure

            return Mathf.Clamp01(retention);
        }

        /// <summary>
        /// Calculates appearance score (0-1).
        /// </summary>
        private static float CalculateAppearanceScore(ProcessingBatch batch)
        {
            float score = 0.8f; // Start at 80%

            // Perfect moisture = perfect appearance
            if (batch.MoistureContent >= 0.10f && batch.MoistureContent <= 0.12f)
                score += 0.1f;

            // Over-dry = brittle/crumbly
            if (batch.MoistureContent < 0.08f)
                score -= 0.3f;

            // Mold = visual damage
            score -= batch.MoldRisk * 0.4f;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Calculates aroma score (0-1).
        /// </summary>
        private static float CalculateAromaScore(ProcessingBatch batch)
        {
            // Aroma is directly tied to terpene retention
            float terpeneScore = CalculateTerpeneRetention(batch);

            // Curing enhances aroma
            if (batch.Stage >= ProcessingStage.Curing && batch.CuringWeeksElapsed >= 4)
                terpeneScore += 0.1f;

            return Mathf.Clamp01(terpeneScore);
        }

        /// <summary>
        /// Gets market value multiplier based on quality.
        /// </summary>
        private static float GetMarketValueMultiplier(float qualityScore)
        {
            if (qualityScore >= PREMIUM_PLUS_THRESHOLD) return 2.0f;  // 2x base price
            if (qualityScore >= PREMIUM_THRESHOLD) return 1.75f;      // 1.75x base price
            if (qualityScore >= EXCELLENT_THRESHOLD) return 1.4f;     // 1.4x base price
            if (qualityScore >= GOOD_THRESHOLD) return 1.0f;          // 1x base price
            if (qualityScore >= FAIR_THRESHOLD) return 0.7f;          // 0.7x base price
            return 0.4f;                                               // 0.4x base price
        }

        #endregion

        #region Degradation

        /// <summary>
        /// Calculates quality degradation over time for stored product.
        ///
        /// GAMEPLAY:
        /// - Cured product can be stored
        /// - Quality slowly degrades if not sold
        /// - Proper storage slows degradation
        /// </summary>
        public static float CalculateDegradation(ProcessingBatch batch, float daysSinceCompletion,
            float storageTemp, float storageHumidity)
        {
            // Base degradation: 0.5% per month
            float monthsStored = daysSinceCompletion / 30f;
            float baseDegradation = monthsStored * 0.5f;

            // Temperature effect
            float tempDegradation = 0f;
            if (storageTemp > 20f)
                tempDegradation = (storageTemp - 20f) * 0.1f * monthsStored;

            // Humidity effect
            float humidityDegradation = 0f;
            if (storageHumidity > 0.65f)
                humidityDegradation = (storageHumidity - 0.65f) * 5f * monthsStored;
            else if (storageHumidity < 0.55f)
                humidityDegradation = (0.55f - storageHumidity) * 3f * monthsStored;

            float totalDegradation = baseDegradation + tempDegradation + humidityDegradation;
            return Mathf.Clamp(totalDegradation, 0f, 50f); // Max 50% degradation
        }

        #endregion
    }

    /// <summary>
    /// Quality analysis result.
    /// </summary>
    [Serializable]
    public struct QualityAnalysis
    {
        public string BatchId;
        public float CurrentQuality;
        public string QualityGrade;

        // Component scores
        public float HarvestQualityScore;
        public float DryingQualityScore;
        public float CuringQualityScore;

        // Attribute scores
        public float PotencyRetention;       // 0-1
        public float TerpeneRetention;       // 0-1
        public float AppearanceScore;        // 0-1
        public float AromaScore;             // 0-1

        // Risk factors
        public float MoldRisk;               // 0-1
        public float OverDryRisk;            // 0-1

        // Market data
        public float MarketValueMultiplier;  // Price multiplier
    }
}
