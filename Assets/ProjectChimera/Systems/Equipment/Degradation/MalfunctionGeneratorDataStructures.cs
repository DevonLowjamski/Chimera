using System;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Malfunction Generator Data Structures
    /// Single Responsibility: Define all malfunction generation data types
    /// Extracted from MalfunctionGenerator (717 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Malfunction generator statistics
    /// </summary>
    [Serializable]
    public struct MalfunctionGeneratorStats
    {
        public int TotalMalfunctionsGenerated;
        public int GenerationsWithoutProfile;
        public int GenerationErrors;
        public int MalfunctionsRemoved;

        public float TotalGenerationTime;
        public float AverageGenerationTime;
        public float MaxGenerationTime;

        // Type counters
        public int MechanicalFailures;
        public int ElectricalFailures;
        public int SensorFailures;
        public int OverheatingIssues;
        public int SoftwareErrors;

        // Severity counters
        public int MinorSeverity;
        public int ModerateSeverity;
        public int MajorSeverity;
        public int CriticalSeverity;
        public int CatastrophicSeverity;

        /// <summary>
        /// Get total malfunctions by type
        /// </summary>
        public readonly int TotalByType => MechanicalFailures + ElectricalFailures + SensorFailures +
                                           OverheatingIssues + SoftwareErrors;

        /// <summary>
        /// Get total malfunctions by severity
        /// </summary>
        public readonly int TotalBySeverity => MinorSeverity + ModerateSeverity + MajorSeverity +
                                               CriticalSeverity + CatastrophicSeverity;

        /// <summary>
        /// Get generation success rate
        /// </summary>
        public readonly float SuccessRate => TotalMalfunctionsGenerated > 0
            ? (float)(TotalMalfunctionsGenerated - GenerationErrors) / TotalMalfunctionsGenerated
            : 0f;

        /// <summary>
        /// Create empty stats
        /// </summary>
        public static MalfunctionGeneratorStats CreateEmpty()
        {
            return new MalfunctionGeneratorStats
            {
                TotalMalfunctionsGenerated = 0,
                GenerationsWithoutProfile = 0,
                GenerationErrors = 0,
                MalfunctionsRemoved = 0,
                TotalGenerationTime = 0f,
                AverageGenerationTime = 0f,
                MaxGenerationTime = 0f,
                MechanicalFailures = 0,
                ElectricalFailures = 0,
                SensorFailures = 0,
                OverheatingIssues = 0,
                SoftwareErrors = 0,
                MinorSeverity = 0,
                ModerateSeverity = 0,
                MajorSeverity = 0,
                CriticalSeverity = 0,
                CatastrophicSeverity = 0
            };
        }
    }

    /// <summary>
    /// Generation parameters for malfunction generator
    /// </summary>
    [Serializable]
    public struct MalfunctionGenerationParameters
    {
        public float SeverityVariabilityFactor;
        public float WearSeverityModifier;
        public float StressSeverityModifier;
        public float RandomSeverityVariance;
        public bool GenerateDetailedSymptoms;
        public bool UseRealisticGeneration;

        /// <summary>
        /// Create default parameters
        /// </summary>
        public static MalfunctionGenerationParameters CreateDefault()
        {
            return new MalfunctionGenerationParameters
            {
                SeverityVariabilityFactor = 0.2f,
                WearSeverityModifier = 0.3f,
                StressSeverityModifier = 0.2f,
                RandomSeverityVariance = 0.1f,
                GenerateDetailedSymptoms = true,
                UseRealisticGeneration = true
            };
        }

        /// <summary>
        /// Validate parameters
        /// </summary>
        public readonly bool IsValid()
        {
            return SeverityVariabilityFactor >= 0f && SeverityVariabilityFactor <= 1f &&
                   WearSeverityModifier >= 0f && WearSeverityModifier <= 1f &&
                   StressSeverityModifier >= 0f && StressSeverityModifier <= 1f &&
                   RandomSeverityVariance >= 0f && RandomSeverityVariance <= 1f;
        }
    }
}

