using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Processing
{
    /// <summary>
    /// Processing pipeline data structures.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// Post-harvest processing determines final product quality and value.
    /// Players manage:
    /// - Drying (7-14 days, temperature/humidity sensitive)
    /// - Curing (2-8 weeks, jar burping mechanics)
    /// - Quality preservation (avoid mold, over-drying)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Drying: Day 5/10, 62% humidity - Perfect!"
    /// Behind scenes: Complex moisture loss curves, mold risk calculations
    /// </summary>

    /// <summary>
    /// Processing stage enum.
    /// </summary>
    public enum ProcessingStage
    {
        Fresh,          // Just harvested
        Drying,         // Active drying process
        Dried,          // Drying complete
        Curing,         // Active curing process
        Cured,          // Curing complete, ready to sell
        Spoiled         // Mold or degradation occurred
    }

    /// <summary>
    /// Processing batch - represents harvested material going through processing.
    /// </summary>
    [Serializable]
    public class ProcessingBatch
    {
        public string BatchId;
        public string StrainName;
        public float WeightGrams;
        public float InitialQuality;         // Quality at harvest (0-100)
        public float CurrentQuality;         // Current quality (degrades over time)
        public ProcessingStage Stage;

        // Drying data
        public float MoistureContent;        // 0-1 (1 = wet, 0 = bone dry)
        public int DryingDaysElapsed;
        public int TargetDryingDays;

        // Curing data
        public int CuringWeeksElapsed;
        public int TargetCuringWeeks;
        public float JarHumidity;            // 0-1
        public DateTime LastBurpTime;

        // Environment tracking
        public float AverageTemp;            // Celsius
        public float AverageHumidity;        // 0-1

        // Risk factors
        public float MoldRisk;               // 0-1 (0 = no risk, 1 = moldy)
        public float OverDryRisk;            // 0-1 (0 = no risk, 1 = too dry)

        // Timestamps
        public DateTime HarvestDate;
        public DateTime DryingStartDate;
        public DateTime CuringStartDate;
        public DateTime CompletionDate;

        // Genetic fingerprint (from blockchain)
        public string GeneticHash;

        /// <summary>
        /// Creates a fresh batch from harvest.
        /// </summary>
        public static ProcessingBatch FromHarvest(string batchId, string strainName,
            float weightGrams, float quality, string geneticHash = null)
        {
            return new ProcessingBatch
            {
                BatchId = batchId,
                StrainName = strainName,
                WeightGrams = weightGrams,
                InitialQuality = quality,
                CurrentQuality = quality,
                Stage = ProcessingStage.Fresh,
                MoistureContent = 0.75f,         // 75% moisture when fresh
                DryingDaysElapsed = 0,
                TargetDryingDays = 10,           // Default 10 days
                CuringWeeksElapsed = 0,
                TargetCuringWeeks = 4,           // Default 4 weeks
                JarHumidity = 0f,
                AverageTemp = 21f,               // 21°C ideal
                AverageHumidity = 0.55f,         // 55% ideal
                MoldRisk = 0f,
                OverDryRisk = 0f,
                HarvestDate = DateTime.Now,
                GeneticHash = geneticHash ?? "UNVERIFIED"
            };
        }
    }

    /// <summary>
    /// Drying environment conditions.
    /// </summary>
    [Serializable]
    public struct DryingConditions
    {
        public float Temperature;            // Celsius (18-24°C ideal)
        public float Humidity;               // 0-1 (45-55% ideal)
        public float Airflow;                // 0-1 (gentle airflow)
        public bool DarknessProvided;        // Light degrades cannabinoids

        /// <summary>
        /// Gets ideal drying conditions.
        /// </summary>
        public static DryingConditions Ideal => new DryingConditions
        {
            Temperature = 21f,
            Humidity = 0.50f,
            Airflow = 0.5f,
            DarknessProvided = true
        };

        /// <summary>
        /// Calculates condition quality score (0-1).
        /// </summary>
        public float GetQualityScore()
        {
            float tempScore = 1f - Mathf.Abs(Temperature - 21f) / 10f; // ±10°C tolerance
            float humidityScore = 1f - Mathf.Abs(Humidity - 0.50f) * 2f; // ±50% tolerance
            float airflowScore = Airflow; // More is better (to a point)
            float darknessScore = DarknessProvided ? 1f : 0.7f;

            tempScore = Mathf.Clamp01(tempScore);
            humidityScore = Mathf.Clamp01(humidityScore);
            airflowScore = Mathf.Clamp01(airflowScore);

            return (tempScore * 0.4f + humidityScore * 0.4f + airflowScore * 0.1f + darknessScore * 0.1f);
        }
    }

    /// <summary>
    /// Curing jar configuration.
    /// </summary>
    [Serializable]
    public struct CuringJarConfig
    {
        public float JarSizeGrams;           // Jar capacity
        public float FillPercentage;         // 0-1 (75% is ideal)
        public float HumidityLevel;          // 0-1 (62% is ideal)
        public int BurpFrequencyHours;       // How often to burp (open jar)
        public DateTime LastBurpTime;

        /// <summary>
        /// Gets ideal curing jar config.
        /// </summary>
        public static CuringJarConfig Ideal => new CuringJarConfig
        {
            JarSizeGrams = 1000f,
            FillPercentage = 0.75f,
            HumidityLevel = 0.62f,
            BurpFrequencyHours = 24,
            LastBurpTime = DateTime.Now
        };

        /// <summary>
        /// Checks if jar needs burping.
        /// </summary>
        public bool NeedsBurping()
        {
            return (DateTime.Now - LastBurpTime).TotalHours >= BurpFrequencyHours;
        }
    }

    /// <summary>
    /// Processing quality report.
    /// </summary>
    [Serializable]
    public struct ProcessingQualityReport
    {
        public string BatchId;
        public float FinalQuality;           // 0-100
        public float QualityLoss;            // How much quality was lost (0-100)
        public string QualityGrade;          // "Premium", "Excellent", "Good", "Fair", "Poor"

        // Attribute changes
        public float PotencyRetention;       // 0-1 (1 = no loss)
        public float TerpeneRetention;       // 0-1 (1 = no loss)
        public float AppearanceScore;        // 0-1 (1 = perfect)
        public float AromaScore;             // 0-1 (1 = perfect)

        // Process metrics
        public int TotalDryingDays;
        public int TotalCuringWeeks;
        public float AverageDryingTemp;
        public float AverageDryingHumidity;
        public float AverageCuringHumidity;

        // Issues encountered
        public List<string> Issues;          // "Mold detected", "Over-dried", etc.
        public List<string> Achievements;    // "Perfect dry", "Premium cure", etc.

        /// <summary>
        /// Gets quality grade from score.
        /// </summary>
        public static string GetGrade(float qualityScore)
        {
            if (qualityScore >= 95f) return "Premium+";
            if (qualityScore >= 90f) return "Premium";
            if (qualityScore >= 80f) return "Excellent";
            if (qualityScore >= 70f) return "Good";
            if (qualityScore >= 60f) return "Fair";
            return "Poor";
        }
    }

    /// <summary>
    /// Processing event for logging.
    /// </summary>
    [Serializable]
    public struct ProcessingEvent
    {
        public string BatchId;
        public DateTime Timestamp;
        public ProcessingEventType EventType;
        public string Description;
        public float QualityImpact;          // +/- quality change

        public enum ProcessingEventType
        {
            BatchCreated,
            DryingStarted,
            DryingProgress,
            DryingComplete,
            CuringStarted,
            JarBurped,
            CuringProgress,
            CuringComplete,
            QualityDegradation,
            MoldDetected,
            OverDried,
            PerfectConditions,
            BatchSpoiled,
            BatchCompleted
        }
    }

    /// <summary>
    /// Drying metrics for analytics.
    /// </summary>
    [Serializable]
    public struct DryingMetrics
    {
        public float MoisturePercentage;     // Current moisture (0-100%)
        public float TargetMoisture;         // Target moisture (10-12%)
        public float DryingRate;             // % per day
        public int DaysRemaining;
        public float ConditionQuality;       // 0-1
        public float MoldRisk;               // 0-1
        public float OverDryRisk;            // 0-1
        public string Status;                // "Too wet", "Perfect", "Too dry"

        /// <summary>
        /// Gets status message.
        /// </summary>
        public string GetStatusMessage()
        {
            if (MoisturePercentage > 70f) return "Too wet - High mold risk";
            if (MoisturePercentage > 60f) return "Drying well - Monitor humidity";
            if (MoisturePercentage > 12f) return "Nearly done - Perfect range";
            if (MoisturePercentage > 8f) return "Ideal moisture - Ready to cure";
            return "Too dry - Quality loss";
        }
    }

    /// <summary>
    /// Curing metrics for analytics.
    /// </summary>
    [Serializable]
    public struct CuringMetrics
    {
        public int WeeksElapsed;
        public int WeeksRemaining;
        public float JarHumidity;            // 0-1
        public bool NeedsBurping;
        public float QualityImprovement;     // How much quality has improved
        public float TerpenePreservation;    // 0-1
        public string Status;                // "Needs burp", "Curing well", etc.

        /// <summary>
        /// Gets status message.
        /// </summary>
        public string GetStatusMessage()
        {
            if (NeedsBurping) return "Needs burping - Open jar to release moisture";
            if (JarHumidity > 0.65f) return "Too humid - Burp more frequently";
            if (JarHumidity < 0.58f) return "Too dry - Reduce burp frequency";
            if (JarHumidity >= 0.60f && JarHumidity <= 0.64f) return "Perfect humidity - Ideal curing";
            return "Curing in progress";
        }
    }
}
