using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Plant Strain Data Transfer Object
    /// Handles genetic strain information for save/load operations
    /// Includes cannabinoid profiles, growth characteristics, and genetic data
    /// </summary>
    [Serializable]
    public class PlantStrainDTO
    {
        [Header("Strain Identity")]
        public string StrainID;
        public string StrainName;
        public string Description;
        public StrainType Type;
        public string ParentStrain1;
        public string ParentStrain2;
        public DateTime CreationDate;

        [Header("Cannabinoid Profile")]
        public float THCContent; // Percentage
        public float CBDContent; // Percentage
        public float CBNContent; // Percentage
        public float CBGContent; // Percentage
        public List<CannabinoidDTO> Cannabinoids = new List<CannabinoidDTO>();

        [Header("Terpene Profile")]
        public List<TerpeneDTO> Terpenes = new List<TerpeneDTO>();
        public string DominantTerpene;

        [Header("Growth Characteristics")]
        public int FloweringTime; // Days
        public int VegetativeTime; // Days
        public float HeightPotential; // cm
        public float YieldPotential; // grams per plant
        public GrowthPattern GrowthPattern;
        public PlantStructure Structure;

        [Header("Environmental Preferences")]
        public float OptimalTemperature; // Celsius
        public float OptimalHumidity; // Percentage
        public float OptimalPH;
        public LightCycle PreferredLightCycle;
        public NutrientRequirements NutrientNeeds;

        [Header("Genetic Information")]
        public string GenotypeID;
        public float GeneticStability; // 0-1
        public List<string> DominantTraits = new List<string>();
        public List<string> RecessiveTraits = new List<string>();
        public Dictionary<string, float> TraitExpressions = new Dictionary<string, float>();

        [Header("Medical Applications")]
        public List<string> MedicalApplications = new List<string>();
        public List<string> TherapeuticEffects = new List<string>();
        public MedicalRating MedicalRating;

        [Header("Effects Profile")]
        public EffectType PrimaryEffect;
        public EffectType SecondaryEffect;
        public float PsychoactivePotential; // 0-1
        public float RelaxationLevel; // 0-1
        public float EnergyLevel; // 0-1
        public float Duration; // Hours

        [Header("Cultivation Notes")]
        public string CultivationNotes;
        public DifficultyLevel Difficulty;
        public List<string> SpecialRequirements = new List<string>();
        public List<string> CommonIssues = new List<string>();

        [Header("Market Information")]
        public bool IsAvailable;
        public int MarketValue; // Skill Points
        public int RarityLevel; // 1-5
        public DateTime LastUpdated;

        [Header("Statistics")]
        public int TimesPlanted;
        public int SuccessfulHarvests;
        public float AverageYield;
        public float AverageQuality; // 0-1
        public List<HarvestRecordDTO> HarvestHistory = new List<HarvestRecordDTO>();

        /// <summary>
        /// Gets the total cannabinoid content
        /// </summary>
        public float GetTotalCannabinoidContent()
        {
            return THCContent + CBDContent + CBNContent + CBGContent;
        }

        /// <summary>
        /// Gets the THC:CBD ratio
        /// </summary>
        public float GetTHCCBDRatio()
        {
            if (CBDContent == 0) return float.MaxValue;
            return THCContent / CBDContent;
        }

        /// <summary>
        /// Checks if this is a high-THC strain
        /// </summary>
        public bool IsHighTHCStrain()
        {
            return THCContent >= 15f;
        }

        /// <summary>
        /// Checks if this is a balanced strain
        /// </summary>
        public bool IsBalancedStrain()
        {
            float ratio = GetTHCCBDRatio();
            return ratio >= 0.8f && ratio <= 1.2f;
        }

        /// <summary>
        /// Checks if this is a CBD-dominant strain
        /// </summary>
        public bool IsCBDDominantStrain()
        {
            return CBDContent > THCContent;
        }

        /// <summary>
        /// Gets the success rate of harvests
        /// </summary>
        public float GetHarvestSuccessRate()
        {
            if (TimesPlanted == 0) return 0f;
            return (float)SuccessfulHarvests / TimesPlanted;
        }

        /// <summary>
        /// Gets the strain's potency rating
        /// </summary>
        public PotencyRating GetPotencyRating()
        {
            if (THCContent >= 25f) return PotencyRating.ExtremelyHigh;
            if (THCContent >= 20f) return PotencyRating.VeryHigh;
            if (THCContent >= 15f) return PotencyRating.High;
            if (THCContent >= 10f) return PotencyRating.Medium;
            return PotencyRating.Low;
        }

        /// <summary>
        /// Checks if the strain is suitable for medical use
        /// </summary>
        public bool IsMedicalStrain()
        {
            return MedicalApplications.Count > 0 && CBDContent >= 1f;
        }

        /// <summary>
        /// Gets a summary of the strain's key characteristics
        /// </summary>
        public string GetStrainSummary()
        {
            string summary = $"{StrainName} ({Type}) - THC: {THCContent:F1}%, CBD: {CBDContent:F1}%";
            summary += $"\nFlowering: {FloweringTime} days, Yield: {YieldPotential:F0}g";
            summary += $"\nDifficulty: {Difficulty}, Rarity: {RarityLevel}/5";

            if (MedicalApplications.Count > 0)
            {
                summary += $"\nMedical: {string.Join(", ", MedicalApplications)}";
            }

            return summary;
        }

        /// <summary>
        /// Records a harvest for statistics
        /// </summary>
        public void RecordHarvest(float yield, float quality, string notes = "")
        {
            var harvest = new HarvestRecordDTO
            {
                HarvestDate = DateTime.Now,
                YieldGrams = yield,
                QualityScore = quality,
                Notes = notes,
                GrowthConditions = "Standard cultivation"
            };

            HarvestHistory.Add(harvest);
            SuccessfulHarvests++;

            // Update averages
            AverageYield = ((AverageYield * (SuccessfulHarvests - 1)) + yield) / SuccessfulHarvests;
            AverageQuality = ((AverageQuality * (SuccessfulHarvests - 1)) + quality) / SuccessfulHarvests;

            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Updates market information
        /// </summary>
        public void UpdateMarketInfo(int value, int rarity)
        {
            MarketValue = value;
            RarityLevel = rarity;
            LastUpdated = DateTime.Now;
        }
    }

    /// <summary>
    /// Cannabinoid data structure
    /// </summary>
    [Serializable]
    public class CannabinoidDTO
    {
        public string Name;
        public float Content; // Percentage
        public string Effects;
    }

    /// <summary>
    /// Terpene data structure
    /// </summary>
    [Serializable]
    public class TerpeneDTO
    {
        public string Name;
        public float Content; // Percentage
        public string Aroma;
        public string Effects;
    }

    /// <summary>
    /// Nutrient requirements structure
    /// </summary>
    [Serializable]
    public struct NutrientRequirements
    {
        public float NitrogenRequirement;
        public float PhosphorusRequirement;
        public float PotassiumRequirement;
        public float MagnesiumRequirement;
        public float CalciumRequirement;
    }

    /// <summary>
    /// Strain type enumeration
    /// </summary>
    public enum StrainType
    {
        Indica,
        Sativa,
        Hybrid,
        Ruderalis,
        Unknown
    }

    /// <summary>
    /// Growth pattern enumeration
    /// </summary>
    public enum GrowthPattern
    {
        Compact,
        Tall,
        Bushy,
        Sprawling,
        Variable
    }

    /// <summary>
    /// Plant structure enumeration
    /// </summary>
    public enum PlantStructure
    {
        SingleCola,
        MultiCola,
        ChristmasTree,
        Variable
    }

    /// <summary>
    /// Light cycle enumeration
    /// </summary>
    public enum LightCycle
    {
        Autoflowering,
        Photoperiod,
        Variable
    }

    /// <summary>
    /// Effect type enumeration
    /// </summary>
    public enum EffectType
    {
        Relaxing,
        Energizing,
        Euphoric,
        Sedating,
        Creative,
        Focused,
        PainRelief,
        AntiAnxiety,
        AntiInflammatory
    }

    /// <summary>
    /// Medical rating enumeration
    /// </summary>
    public enum MedicalRating
    {
        None,
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Difficulty level enumeration
    /// </summary>
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    /// <summary>
    /// Potency rating enumeration
    /// </summary>
    public enum PotencyRating
    {
        Low,
        Medium,
        High,
        VeryHigh,
        ExtremelyHigh
    }

    /// <summary>
    /// Harvest record data structure
    /// </summary>
    [Serializable]
    public class HarvestRecordDTO
    {
        public DateTime HarvestDate;
        public float YieldGrams;
        public float QualityScore;
        public string Notes;
        public string GrowthConditions;
    }
}

