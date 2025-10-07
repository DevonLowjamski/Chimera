// REFACTORED: Data Structures
// Extracted from CacheOptimizationManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    public enum CacheEvictionPolicy
    {
        LRU,    // Least Recently Used
        LFU,    // Least Frequently Used
        FIFO,   // First In First Out
        Random  // Random eviction
    }

    public enum MemoryPressureLevel
    {
        Normal,
        Medium,
        High,
        Critical
    }

    public struct OptimizationResult
    {
        public bool Success;
        public DateTime StartTime;
        public float ExecutionTime;
        public int ExpiredItemsRemoved;
        public int ItemsEvicted;
        public string ErrorMessage;
    }

    public class OptimizationStatistics
    {
        public int TotalOptimizations = 0;
        public int TotalItemsEvicted = 0;
        public int TotalExpiredItemsRemoved = 0;
        public DateTime LastOptimizationTime = DateTime.MinValue;
        public float CurrentCacheUtilization = 0f;
    }

}
