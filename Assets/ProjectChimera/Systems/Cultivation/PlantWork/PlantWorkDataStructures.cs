using System;

namespace ProjectChimera.Systems.Cultivation.PlantWork
{
    /// <summary>
    /// Data structures for Plant Work system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Plant Work Record

    /// <summary>
    /// Represents a work operation performed on a plant.
    /// </summary>
    [Serializable]
    public struct PlantWorkRecord
    {
        public string RecordId;
        public string PlantId;
        public PlantWorkType WorkType;
        public DateTime ApplicationDate;
        public GrowthStage GrowthStage;
        public int DaysIntoStage;
    }

    #endregion

    #region Plant Work Types

    /// <summary>
    /// Types of plant work operations.
    /// </summary>
    [Serializable]
    public enum PlantWorkType
    {
        // Pruning (0-3)
        Topping,            // Remove main stem tip → +15% yield, +2 colas
        FIMming,            // Partial topping → +12% yield, 3-5 colas
        Lollipopping,       // Remove lower growth → +8% yield, +12% quality
        Defoliation,        // Remove fan leaves → +5% yield, +15% light

        // Training (4-7)
        LST,                // Low Stress Training → +10% yield, -30% height
        HST,                // High Stress Training → +15% yield, -40% height
        ScrOG,              // Screen of Green → +20% yield, canopy control
        Supercropping       // Stem bending → +12% yield, +15% nutrient uptake
    }

    #endregion

    #region Plant Work Effects

    /// <summary>
    /// Active effects from plant work operations.
    /// </summary>
    [Serializable]
    public struct PlantWorkEffects
    {
        public string PlantId;
        public float YieldMultiplier;       // 1.0 = baseline (e.g., 1.15 = +15% yield)
        public float HeightMultiplier;      // 1.0 = baseline (e.g., 0.80 = -20% height)
        public float QualityMultiplier;     // 1.0 = baseline (e.g., 1.12 = +12% quality)
        public int ColaCount;               // Number of main colas (1 = untrained)
        public float LightPenetration;      // 0-1 (additional light reaching lower buds)
        public float NutrientUptake;        // 1.0 = baseline (e.g., 1.15 = +15% uptake)
        public float CurrentStress;         // 0-1 (accumulated stress from operations)
        public bool IsScrogged;             // ScrOG net active
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Plant work system statistics for UI display.
    /// </summary>
    [Serializable]
    public struct PlantWorkStats
    {
        public int TotalOperations;
        public int PruningOperations;
        public int TrainingOperations;
        public float AverageYieldBoost;
        public int PlantsWithWork;
        public PlantWorkType MostCommonWork;
    }

    #endregion

    #region Growth Stage (if not already defined)

    /// <summary>
    /// Plant growth stages (used for timing validation).
    /// </summary>
    [Serializable]
    public enum GrowthStage
    {
        Seedling,
        Vegetative,
        Flowering,
        Harvest
    }

    #endregion
}
