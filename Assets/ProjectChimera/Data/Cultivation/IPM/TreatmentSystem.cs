using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation.IPM
{
    /// <summary>
    /// BASIC: Simple treatment system for Project Chimera's pest management.
    /// Focuses on essential pest control without complex treatment hierarchies and protocols.
    /// </summary>
    public static class TreatmentSystem
    {
        /// <summary>
        /// Basic treatment options
        /// </summary>
        public static readonly TreatmentOption[] BasicTreatments = new TreatmentOption[]
        {
            new TreatmentOption
            {
                Name = "Neem Oil Spray",
                Type = TreatmentType.Organic,
                TargetPests = new[] { "Aphids", "Spider Mites", "Thrips" },
                Effectiveness = 0.8f,
                Cost = 15f,
                ApplicationTime = 5f
            },
            new TreatmentOption
            {
                Name = "Soap Spray",
                Type = TreatmentType.Organic,
                TargetPests = new[] { "Aphids", "Mealybugs" },
                Effectiveness = 0.6f,
                Cost = 5f,
                ApplicationTime = 3f
            },
            new TreatmentOption
            {
                Name = "Insecticidal Soap",
                Type = TreatmentType.Chemical,
                TargetPests = new[] { "Various insects" },
                Effectiveness = 0.9f,
                Cost = 25f,
                ApplicationTime = 10f
            },
            new TreatmentOption
            {
                Name = "Beneficial Insects",
                Type = TreatmentType.Biological,
                TargetPests = new[] { "Aphids", "Thrips" },
                Effectiveness = 0.7f,
                Cost = 30f,
                ApplicationTime = 15f
            }
        };

        /// <summary>
        /// Get treatments for specific pest
        /// </summary>
        public static TreatmentOption[] GetTreatmentsForPest(string pestName)
        {
            var treatments = new List<TreatmentOption>();

            foreach (var treatment in BasicTreatments)
            {
                if (System.Array.Exists(treatment.TargetPests, pest => pest == pestName))
                {
                    treatments.Add(treatment);
                }
            }

            return treatments.ToArray();
        }

        /// <summary>
        /// Get recommended treatment for pest
        /// </summary>
        public static TreatmentOption GetRecommendedTreatment(string pestName, TreatmentPreference preference = TreatmentPreference.Effective)
        {
            var availableTreatments = GetTreatmentsForPest(pestName);

            if (availableTreatments.Length == 0) return null;

            switch (preference)
            {
                case TreatmentPreference.Effective:
                    return GetMostEffectiveTreatment(availableTreatments);
                case TreatmentPreference.Cheap:
                    return GetCheapestTreatment(availableTreatments);
                case TreatmentPreference.Quick:
                    return GetQuickestTreatment(availableTreatments);
                case TreatmentPreference.Organic:
                    return GetOrganicTreatment(availableTreatments);
                default:
                    return availableTreatments[0];
            }
        }

        /// <summary>
        /// Apply treatment to plant
        /// </summary>
        public static TreatmentResult ApplyTreatment(string plantId, TreatmentOption treatment, string pestName)
        {
            // Simple treatment application logic
            float effectiveness = treatment.Effectiveness;

            // Some treatments are more effective against specific pests
            if (treatment.Name.Contains("Neem") && pestName == "Spider Mites")
            {
                effectiveness += 0.1f; // Bonus effectiveness
            }

            bool success = Random.value <= effectiveness;

            return new TreatmentResult
            {
                PlantId = plantId,
                TreatmentName = treatment.Name,
                TargetPest = pestName,
                Success = success,
                Effectiveness = effectiveness,
                Cost = treatment.Cost,
                ApplicationTime = treatment.ApplicationTime,
                Timestamp = System.DateTime.Now
            };
        }

        /// <summary>
        /// Get treatment statistics
        /// </summary>
        public static TreatmentStats GetTreatmentStats()
        {
            return new TreatmentStats
            {
                TotalTreatments = BasicTreatments.Length,
                OrganicTreatments = BasicTreatments.Count(t => t.Type == TreatmentType.Organic),
                ChemicalTreatments = BasicTreatments.Count(t => t.Type == TreatmentType.Chemical),
                BiologicalTreatments = BasicTreatments.Count(t => t.Type == TreatmentType.Biological),
                AverageEffectiveness = CalculateAverageEffectiveness(),
                AverageCost = CalculateAverageCost()
            };
        }

        /// <summary>
        /// Check if treatment is safe for harvest
        /// </summary>
        public static bool IsSafeForHarvest(TreatmentOption treatment, float daysSinceTreatment)
        {
            // Simple safety check - organic treatments are generally safer
            if (treatment.Type == TreatmentType.Organic || treatment.Type == TreatmentType.Biological)
            {
                return daysSinceTreatment >= 1f; // 1 day for organic
            }
            else
            {
                return daysSinceTreatment >= 7f; // 7 days for chemical
            }
        }

        /// <summary>
        /// Get pest control advice
        /// </summary>
        public static string GetPestControlAdvice(string pestName, PestSeverity severity)
        {
            string baseAdvice = $"Detected {severity.ToString().ToLower()} {pestName} infestation. ";

            switch (severity)
            {
                case PestSeverity.Low:
                    baseAdvice += "Monitor closely and consider preventive measures.";
                    break;
                case PestSeverity.Medium:
                    baseAdvice += "Apply organic treatment immediately.";
                    break;
                case PestSeverity.High:
                    baseAdvice += "Apply strongest available treatment and isolate affected plants.";
                    break;
                case PestSeverity.Critical:
                    baseAdvice += "Remove affected plants immediately to prevent spread.";
                    break;
            }

            return baseAdvice;
        }

        #region Private Methods

        private static TreatmentOption GetMostEffectiveTreatment(TreatmentOption[] treatments)
        {
            TreatmentOption best = treatments[0];
            foreach (var treatment in treatments)
            {
                if (treatment.Effectiveness > best.Effectiveness)
                {
                    best = treatment;
                }
            }
            return best;
        }

        private static TreatmentOption GetCheapestTreatment(TreatmentOption[] treatments)
        {
            TreatmentOption cheapest = treatments[0];
            foreach (var treatment in treatments)
            {
                if (treatment.Cost < cheapest.Cost)
                {
                    cheapest = treatment;
                }
            }
            return cheapest;
        }

        private static TreatmentOption GetQuickestTreatment(TreatmentOption[] treatments)
        {
            TreatmentOption quickest = treatments[0];
            foreach (var treatment in treatments)
            {
                if (treatment.ApplicationTime < quickest.ApplicationTime)
                {
                    quickest = treatment;
                }
            }
            return quickest;
        }

        private static TreatmentOption GetOrganicTreatment(TreatmentOption[] treatments)
        {
            foreach (var treatment in treatments)
            {
                if (treatment.Type == TreatmentType.Organic || treatment.Type == TreatmentType.Biological)
                {
                    return treatment;
                }
            }
            return treatments[0]; // Fallback to first treatment
        }

        private static float CalculateAverageEffectiveness()
        {
            if (BasicTreatments.Length == 0) return 0f;

            float total = 0f;
            foreach (var treatment in BasicTreatments)
            {
                total += treatment.Effectiveness;
            }
            return total / BasicTreatments.Length;
        }

        private static float CalculateAverageCost()
        {
            if (BasicTreatments.Length == 0) return 0f;

            float total = 0f;
            foreach (var treatment in BasicTreatments)
            {
                total += treatment.Cost;
            }
            return total / BasicTreatments.Length;
        }

        #endregion
    }

    /// <summary>
    /// Treatment option
    /// </summary>
    [System.Serializable]
    public class TreatmentOption
    {
        public string Name;
        public TreatmentType Type;
        public string[] TargetPests;
        public float Effectiveness; // 0-1
        public float Cost;
        public float ApplicationTime; // minutes
    }

    /// <summary>
    /// Treatment result
    /// </summary>
    [System.Serializable]
    public class TreatmentResult
    {
        public string PlantId;
        public string TreatmentName;
        public string TargetPest;
        public bool Success;
        public float Effectiveness;
        public float Cost;
        public float ApplicationTime;
        public System.DateTime Timestamp;
    }

    // TreatmentType enum moved to IPMEnums.cs to avoid duplication

    /// <summary>
    /// Treatment preferences
    /// </summary>
    public enum TreatmentPreference
    {
        Effective,
        Cheap,
        Quick,
        Organic
    }

    /// <summary>
    /// Pest severity levels
    /// </summary>
    public enum PestSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Treatment statistics
    /// </summary>
    [System.Serializable]
    public struct TreatmentStats
    {
        public int TotalTreatments;
        public int OrganicTreatments;
        public int ChemicalTreatments;
        public int BiologicalTreatments;
        public float AverageEffectiveness;
        public float AverageCost;
    }
}
