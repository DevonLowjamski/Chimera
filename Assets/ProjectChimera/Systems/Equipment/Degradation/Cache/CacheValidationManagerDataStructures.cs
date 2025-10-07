// REFACTORED: Data Structures
// Extracted from CacheValidationManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    public struct ValidationResult
    {
        public bool Success;
        public DateTime StartTime;
        public float ExecutionTime;
        public int SampleSize;
        public int ValidatedSamples;
        public int AccurateEstimates;
        public float OverallAccuracy;
        public float AverageErrorMargin;
        public string Message;
    }

    public struct SampleValidationResult
    {
        public string Key;
        public float CachedCost;
        public float RecalculatedCost;
        public float ErrorMargin;
        public bool IsAccurate;
        public DateTime ValidationTime;

        // Additional properties for compatibility
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

    public class ValidationRecord
    {
        public int ValidationCount = 0;
        public int AccurateValidations = 0;
        public float AccuracyRate = 0f;
        public float TotalErrorMargin = 0f;
        public float AverageErrorMargin = 0f;
        public DateTime LastValidationTime = DateTime.MinValue;
    }

    public struct ValidationSample
    {
        public string Key;
        public float ErrorMargin;
        public bool IsAccurate;
        public DateTime Timestamp;
    }

    public class ValidationStatistics
    {
        public int TotalValidations = 0;
        public int TotalSamplesValidated = 0;
        public int TotalAccurateSamples = 0;
        public float AverageAccuracy = 0f;
        public float RecentAccuracy = 0f;
        public DateTime LastValidationTime = DateTime.MinValue;
    }

    public struct AccuracyTrend
    {
        public TrendDirection Trend;
        public float Confidence;
        public float FirstPeriodAccuracy;
        public float SecondPeriodAccuracy;
    }

    public enum TrendDirection
    {
        Declining,
        Stable,
        Improving
    }

}
