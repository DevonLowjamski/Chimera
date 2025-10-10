using System;

namespace ProjectChimera.Systems.Cultivation.Processing
{
    /// <summary>
    /// Data structures for Processing system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Drying Structures

    /// <summary>
    /// Represents a batch being dried.
    /// </summary>
    [Serializable]
    public struct DryingBatch
    {
        public string BatchId;
        public DryingMethod Method;
        public float WetWeightGrams;
        public float DryWeightGrams;
        public float CurrentMoisture;       // 0-1 (0.75 = 75%)
        public float TargetMoisture;        // 0-1 (0.11 = 11%)
        public float DurationDays;
        public float ElapsedDays;
        public DateTime StartDate;
        public DateTime? CompletionDate;
        public bool IsComplete;
    }

    /// <summary>
    /// Drying methods available to players.
    /// </summary>
    [Serializable]
    public enum DryingMethod
    {
        HangDry,        // 10 days, +5% quality
        RackDry,        // 8 days, standard
        FreezeDry       // 3 days, +10% terpenes
    }

    #endregion

    #region Curing Structures

    /// <summary>
    /// Represents a batch being cured.
    /// </summary>
    [Serializable]
    public struct CuringBatch
    {
        public string BatchId;
        public CuringMethod Method;
        public float WeightGrams;
        public float TerpenePreservation;   // 0-1
        public float BaseQuality;           // 0-1
        public float CurrentQuality;        // 0-1
        public float ElapsedWeeks;
        public int BurpCount;
        public DateTime LastBurpDate;
        public bool NeedsBurping;
        public DateTime StartDate;
        public DateTime? CompletionDate;
        public ProcessingQuality FinalQuality;
        public bool IsComplete;
    }

    /// <summary>
    /// Curing methods available to players.
    /// </summary>
    [Serializable]
    public enum CuringMethod
    {
        JarCuring,      // Requires burping, 1.0x quality
        TurkeyBag,      // Lower maintenance, 0.95x quality
        GroveBag        // Premium, self-burping, 1.05x quality
    }

    #endregion

    #region Quality Grading

    /// <summary>
    /// Final quality grades affecting market value.
    /// </summary>
    [Serializable]
    public enum ProcessingQuality
    {
        Poor,           // 0.4x market value
        Average,        // 0.7x market value
        Good,           // 1.0x market value
        Premium,        // 1.5x market value
        PremiumPlus     // 2.0x market value
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Processing system statistics for UI display.
    /// </summary>
    [Serializable]
    public struct ProcessingStats
    {
        public int ActiveDryingBatches;
        public int ActiveCuringBatches;
        public float TotalWeightDrying;
        public float TotalWeightCuring;
        public float AverageCuringQuality;
        public int CompletedBatches;
        public float AverageFinalQuality;
    }

    #endregion
}
