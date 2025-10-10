using System;

namespace ProjectChimera.Systems.Genetics.Micropropagation
{
    /// <summary>
    /// Data structures for Micropropagation system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Micropropagation Batch

    /// <summary>
    /// Represents a micropropagation batch (1-100 clones).
    /// </summary>
    [Serializable]
    public struct MicropropagationBatch
    {
        public string BatchId;                      // MP-XXXXXXXX format
        public string CultureSampleId;              // Source tissue culture
        public string GenotypeName;                 // Strain name
        public string BlockchainHash;               // Genetic authentication hash
        public int TargetQuantity;                  // Number of clones requested (1-100)
        public MicropropagationStage CurrentStage;  // Current processing stage
        public float StageProgress;                 // Overall progress (0-100%)
        public float TotalDurationDays;             // Total estimated duration
        public float ElapsedDays;                   // Time elapsed since start
        public float ExpectedSuccessRate;           // Expected success rate (0-1)
        public int SuccessfulClones;                // Actual clones produced (set on completion)
        public DateTime StartDate;                  // When batch was created
        public DateTime? CompletionDate;            // When batch completed (null if in progress)
        public bool IsComplete;                     // Completion flag
    }

    #endregion

    #region Micropropagation Stage

    /// <summary>
    /// Micropropagation processing stages.
    /// </summary>
    [Serializable]
    public enum MicropropagationStage
    {
        Multiplication,     // Stage 1: 50% of time - rapid cell division
        Rooting,            // Stage 2: 30% of time - root formation
        Acclimatization     // Stage 3: 20% of time - hardening off for transplant
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Micropropagation system statistics for UI display.
    /// </summary>
    [Serializable]
    public struct MicropropagationStats
    {
        public int ActiveBatchCount;
        public int CompletedBatchCount;
        public int TotalClonesInProgress;
        public int TotalClonesCompleted;
        public float AverageSuccessRate;
        public float AverageProgress;
    }

    /// <summary>
    /// Batch estimate for UI display before creating batch.
    /// </summary>
    [Serializable]
    public struct MicropropagationEstimate
    {
        public string CultureSampleId;
        public string GenotypeName;
        public int RequestedQuantity;
        public float EstimatedDurationDays;
        public float EstimatedCost;
        public float ExpectedSuccessRate;
        public int ExpectedClones;
    }

    #endregion
}
