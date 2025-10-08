// REFACTORED: Memory Optimized Cultivation Data Structures
// Extracted from MemoryOptimizedCultivationManager for better separation of concerns

using System;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Cultivation event data
    /// </summary>
    public class CultivationEvent
    {
        public CultivationEventType EventType;
        public string PlantId;
        public string Description;
        public float Timestamp;
    }

    /// <summary>
    /// Plant update data for memory optimization
    /// </summary>
    public class PlantUpdateData
    {
        public string PlantId;
        public float DeltaTime;
    }

    /// <summary>
    /// Types of cultivation events
    /// </summary>
    public enum CultivationEventType
    {
        PlantAdded,
        PlantRemoved,
        PlantWatered,
        PlantHarvested,
        GrowthStageChanged,
        HealthChanged
    }

    /// <summary>
    /// Statistics for memory-optimized cultivation
    /// </summary>
    [Serializable]
    public struct MemoryOptimizedCultivationStats
    {
        public int PlantCount;
        public int PlantCapacity;
        public int QueuedEvents;
        public int TotalPlantUpdates;
        public int TotalEventsProcessed;
        public float CurrentMemoryUsage;
        public float MemoryDelta;
        public StringCacheStats StringCacheStats;
        public bool MemoryOptimizationEnabled;
    }
}

