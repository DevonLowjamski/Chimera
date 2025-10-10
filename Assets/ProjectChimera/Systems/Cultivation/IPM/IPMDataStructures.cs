using System;

namespace ProjectChimera.Systems.Cultivation.IPM
{
    /// <summary>
    /// Data structures for Active IPM system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Plant Infestation

    /// <summary>
    /// Represents a pest infestation on a plant.
    /// </summary>
    [Serializable]
    public struct PlantInfestation
    {
        public string PlantId;
        public PestType PestType;
        public float Severity;                  // 0-1, represents population density
        public DateTime FirstInfestationDate;
        public DateTime? DetectionDate;         // Null until severity reaches detection threshold
        public int GenerationCount;             // Number of pest generations elapsed
    }

    #endregion

    #region Pest Types

    /// <summary>
    /// Common cannabis pests.
    /// </summary>
    [Serializable]
    public enum PestType
    {
        SpiderMites,    // Most common, rapid reproduction (7 days/generation)
        Aphids,         // Sap feeders, transmit diseases (10 days/generation)
        FungusGnats,    // Larvae damage roots (14 days/generation)
        Thrips          // Damage leaves, spread viruses (12 days/generation)
    }

    #endregion

    #region Treatment Application

    /// <summary>
    /// Represents a pest treatment application.
    /// </summary>
    [Serializable]
    public struct TreatmentApplication
    {
        public string TreatmentId;
        public string PlantId;
        public TreatmentType TreatmentType;
        public DateTime ApplicationDate;
        public float Efficacy;                  // 0-1, treatment effectiveness
        public float DurationDays;              // How long treatment remains effective
    }

    /// <summary>
    /// Treatment types for pest control.
    /// </summary>
    [Serializable]
    public enum TreatmentType
    {
        NeemOil,                // Organic, broad spectrum (60-70% efficacy)
        InsecticidalSoap,       // Contact killer (65-80% efficacy)
        PyrethrinsSpray,        // Fast-acting botanical (85% efficacy)
        BTi                     // Bacillus thuringiensis israelensis, targets fungus gnats (90% efficacy)
    }

    #endregion

    #region Beneficial Organisms

    /// <summary>
    /// Represents a beneficial organism colony for biological control.
    /// </summary>
    [Serializable]
    public struct BeneficialColony
    {
        public string ColonyId;
        public string ZoneId;                   // Zone where colony is active
        public BeneficialType BeneficialType;
        public float PopulationStrength;        // 0-2 (1.0 = sustainable, 2.0 = thriving)
        public DateTime ReleaseDate;
        public DateTime LastUpdateDate;
    }

    /// <summary>
    /// Beneficial organism types.
    /// </summary>
    [Serializable]
    public enum BeneficialType
    {
        PredatoryMites,     // Target spider mites (Phytoseiulus persimilis)
        Ladybugs,           // Target aphids (Hippodamia convergens)
        ParasiticWasps      // Target aphids and thrips (Aphidius colemani)
    }

    #endregion

    #region Statistics

    /// <summary>
    /// IPM system statistics for UI display.
    /// </summary>
    [Serializable]
    public struct IPMStats
    {
        public int TotalInfestations;
        public int DetectableInfestations;
        public float AverageSeverity;
        public int BeneficialColonies;
        public int ActiveTreatments;
        public PestType MostCommonPest;
    }

    #endregion
}
