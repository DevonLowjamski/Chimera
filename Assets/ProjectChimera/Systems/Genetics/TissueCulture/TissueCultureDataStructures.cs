using System;

namespace ProjectChimera.Systems.Genetics.TissueCulture
{
    /// <summary>
    /// Data structures for Tissue Culture system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Tissue Culture Sample

    /// <summary>
    /// Represents a tissue culture sample (active or preserved).
    /// </summary>
    [Serializable]
    public struct TissueCultureSample
    {
        public string SampleId;                 // TC-XXXXXXXX format
        public string SourcePlantId;            // Original plant ID
        public string GenotypeName;             // Strain name (e.g., "Blue Dream")
        public string BlockchainHash;           // Genetic authentication hash
        public CultureStatus Status;            // Current status
        public float HealthPercentage;          // 0-100%
        public float ContaminationRisk;         // 0-100%
        public DateTime CreationDate;           // When culture was created
        public DateTime? PreservationDate;      // When culture was preserved (null if never preserved)
        public DateTime LastMaintenanceDate;    // Last maintenance action
    }

    #endregion

    #region Culture Status

    /// <summary>
    /// Culture sample status.
    /// </summary>
    [Serializable]
    public enum CultureStatus
    {
        Initiating,     // Being created (7 days)
        Active,         // Growing in lab (subject to degradation)
        Preserved,      // Cryogenic storage (frozen in time)
        Reactivating,   // Being thawed (3 days)
        Contaminated    // Failed (health reached 0%)
    }

    #endregion

    #region Culture Operation

    /// <summary>
    /// Represents an async culture operation (creation, reactivation).
    /// </summary>
    [Serializable]
    public struct CultureOperation
    {
        public string OperationId;
        public string SampleId;
        public CultureOperationType OperationType;
        public DateTime StartTime;
        public float DurationDays;
        public bool IsComplete;
        public DateTime? CompletionTime;
    }

    /// <summary>
    /// Culture operation types.
    /// </summary>
    [Serializable]
    public enum CultureOperationType
    {
        Creation,       // Creating new culture (7 days)
        Reactivation    // Reactivating preserved culture (3 days)
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Tissue culture system statistics for UI display.
    /// </summary>
    [Serializable]
    public struct TissueCultureStats
    {
        public int ActiveCultureCount;
        public int PreservedCultureCount;
        public int ActiveCapacity;
        public int PreservedCapacity;
        public float AverageHealth;
        public float AverageContamination;
        public int CulturesNeedingMaintenance;
    }

    #endregion
}
