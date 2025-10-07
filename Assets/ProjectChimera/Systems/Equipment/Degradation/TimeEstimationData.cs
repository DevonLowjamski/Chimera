using System;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Time Estimation Data Management
    /// Single Responsibility: Manage time estimation databases and data structures
    /// Extracted from TimeEstimationEngine (866 lines → 4 files <500 lines each)
    /// </summary>
    public class TimeEstimationData
    {
        // Core time estimation databases
        private readonly Dictionary<MalfunctionType, BaseTimeData> _baseTimeDatabase = new Dictionary<MalfunctionType, BaseTimeData>();
        private readonly Dictionary<EquipmentType, float> _equipmentTimeModifiers = new Dictionary<EquipmentType, float>();
        private readonly Dictionary<MalfunctionType, DiagnosticTimeData> _diagnosticTimeDatabase = new Dictionary<MalfunctionType, DiagnosticTimeData>();

        public IReadOnlyDictionary<MalfunctionType, BaseTimeData> BaseTimeDatabase => _baseTimeDatabase;
        public IReadOnlyDictionary<EquipmentType, float> EquipmentTimeModifiers => _equipmentTimeModifiers;
        public IReadOnlyDictionary<MalfunctionType, DiagnosticTimeData> DiagnosticTimeDatabase => _diagnosticTimeDatabase;

        /// <summary>
        /// Initialize all time estimation databases
        /// </summary>
        public void Initialize()
        {
            InitializeBaseTimeDatabase();
            InitializeEquipmentTimeModifiers();
            InitializeDiagnosticTimeDatabase();
        }

        /// <summary>
        /// Get base time data for malfunction type (with fallback)
        /// </summary>
        public BaseTimeData GetBaseTimeData(MalfunctionType type)
        {
            if (_baseTimeDatabase.TryGetValue(type, out var data))
            {
                return data;
            }

            // Fallback to WearAndTear
            return _baseTimeDatabase[MalfunctionType.WearAndTear];
        }

        /// <summary>
        /// Get diagnostic time data for malfunction type (with fallback)
        /// </summary>
        public DiagnosticTimeData GetDiagnosticTimeData(MalfunctionType type)
        {
            if (_diagnosticTimeDatabase.TryGetValue(type, out var data))
            {
                return data;
            }

            // Fallback to WearAndTear
            return _diagnosticTimeDatabase[MalfunctionType.WearAndTear];
        }

        /// <summary>
        /// Get equipment time modifier
        /// </summary>
        public float GetEquipmentTimeModifier(EquipmentType equipmentType)
        {
            return _equipmentTimeModifiers.TryGetValue(equipmentType, out var modifier) ? modifier : 1.0f;
        }

        /// <summary>
        /// Update base time data with actual repair time
        /// </summary>
        public void UpdateBaseTimeData(MalfunctionType type, float actualMinutes, int maxHistorySize)
        {
            if (!_baseTimeDatabase.TryGetValue(type, out var timeData))
                return;

            // Add to history
            timeData.ActualTimeHistory.Add(actualMinutes);

            // Maintain history size limit
            if (timeData.ActualTimeHistory.Count > maxHistorySize)
            {
                timeData.ActualTimeHistory.RemoveAt(0);
            }

            // Recalculate average
            float sum = 0f;
            foreach (var time in timeData.ActualTimeHistory)
            {
                sum += time;
            }
            timeData.AverageActualTime = sum / timeData.ActualTimeHistory.Count;
        }

        /// <summary>
        /// Check if base time needs updating based on recent data
        /// </summary>
        public bool ShouldUpdateBaseTime(MalfunctionType type, out int newBaseMinutes)
        {
            newBaseMinutes = 0;

            if (!_baseTimeDatabase.TryGetValue(type, out var timeData))
                return false;

            if (timeData.ActualTimeHistory.Count < 5)
                return false;

            // Calculate recent average (last 5 entries)
            float recentSum = 0f;
            int count = Math.Min(5, timeData.ActualTimeHistory.Count);
            for (int i = timeData.ActualTimeHistory.Count - count; i < timeData.ActualTimeHistory.Count; i++)
            {
                recentSum += timeData.ActualTimeHistory[i];
            }
            float recentAverage = recentSum / count;

            // Check for significant deviation
            float deviation = Math.Abs(recentAverage - timeData.BaseMinutes) / timeData.BaseMinutes;

            if (deviation > 0.2f) // 20% deviation threshold
            {
                newBaseMinutes = UnityEngine.Mathf.RoundToInt(recentAverage);
                timeData.BaseMinutes = newBaseMinutes;
                return true;
            }

            return false;
        }

        #region Database Initialization

        /// <summary>
        /// Initialize base time database with default values
        /// </summary>
        private void InitializeBaseTimeDatabase()
        {
            _baseTimeDatabase[MalfunctionType.MechanicalFailure] = new BaseTimeData
            {
                BaseMinutes = 120,
                MinimumMinutes = 30,
                MaximumMinutes = 480,
                VarianceFactor = 0.25f,
                ActualTimeHistory = new List<float>()
            };

            _baseTimeDatabase[MalfunctionType.ElectricalFailure] = new BaseTimeData
            {
                BaseMinutes = 90,
                MinimumMinutes = 20,
                MaximumMinutes = 360,
                VarianceFactor = 0.2f,
                ActualTimeHistory = new List<float>()
            };

            _baseTimeDatabase[MalfunctionType.SensorDrift] = new BaseTimeData
            {
                BaseMinutes = 30,
                MinimumMinutes = 10,
                MaximumMinutes = 120,
                VarianceFactor = 0.15f,
                ActualTimeHistory = new List<float>()
            };

            _baseTimeDatabase[MalfunctionType.OverheatingProblem] = new BaseTimeData
            {
                BaseMinutes = 60,
                MinimumMinutes = 15,
                MaximumMinutes = 240,
                VarianceFactor = 0.18f,
                ActualTimeHistory = new List<float>()
            };

            _baseTimeDatabase[MalfunctionType.SoftwareError] = new BaseTimeData
            {
                BaseMinutes = 45,
                MinimumMinutes = 5,
                MaximumMinutes = 180,
                VarianceFactor = 0.3f,
                ActualTimeHistory = new List<float>()
            };

            _baseTimeDatabase[MalfunctionType.WearAndTear] = new BaseTimeData
            {
                BaseMinutes = 75,
                MinimumMinutes = 20,
                MaximumMinutes = 300,
                VarianceFactor = 0.22f,
                ActualTimeHistory = new List<float>()
            };
        }

        /// <summary>
        /// Initialize equipment time modifiers
        /// </summary>
        private void InitializeEquipmentTimeModifiers()
        {
            _equipmentTimeModifiers[EquipmentType.Generic] = 1.0f;
            _equipmentTimeModifiers[EquipmentType.HVAC] = 1.2f;
            _equipmentTimeModifiers[EquipmentType.Lighting] = 0.8f;
            _equipmentTimeModifiers[EquipmentType.Irrigation] = 1.1f;
            _equipmentTimeModifiers[EquipmentType.Climate] = 1.3f;
            _equipmentTimeModifiers[EquipmentType.Security] = 1.4f;
            _equipmentTimeModifiers[EquipmentType.Power] = 1.25f;
        }

        /// <summary>
        /// Initialize diagnostic time database
        /// </summary>
        private void InitializeDiagnosticTimeDatabase()
        {
            _diagnosticTimeDatabase[MalfunctionType.MechanicalFailure] = new DiagnosticTimeData
            {
                BaseMinutes = 60,
                ComplexityMultiplier = 1.1f
            };

            _diagnosticTimeDatabase[MalfunctionType.ElectricalFailure] = new DiagnosticTimeData
            {
                BaseMinutes = 45,
                ComplexityMultiplier = 1.2f
            };

            _diagnosticTimeDatabase[MalfunctionType.SensorDrift] = new DiagnosticTimeData
            {
                BaseMinutes = 15,
                ComplexityMultiplier = 0.8f
            };

            _diagnosticTimeDatabase[MalfunctionType.OverheatingProblem] = new DiagnosticTimeData
            {
                BaseMinutes = 20,
                ComplexityMultiplier = 0.9f
            };

            _diagnosticTimeDatabase[MalfunctionType.SoftwareError] = new DiagnosticTimeData
            {
                BaseMinutes = 30,
                ComplexityMultiplier = 1.3f
            };

            _diagnosticTimeDatabase[MalfunctionType.WearAndTear] = new DiagnosticTimeData
            {
                BaseMinutes = 40,
                ComplexityMultiplier = 1.0f
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Base time data for malfunction types
    /// </summary>
    [Serializable]
    public class BaseTimeData
    {
        public int BaseMinutes;
        public int MinimumMinutes;
        public int MaximumMinutes;
        public float VarianceFactor;
        public float AverageActualTime;
        public List<float> ActualTimeHistory = new List<float>();
    }

    /// <summary>
    /// Diagnostic time data
    /// </summary>
    [Serializable]
    public class DiagnosticTimeData
    {
        public int BaseMinutes;
        public float ComplexityMultiplier;
    }

    /// <summary>
    /// Time estimate result
    /// </summary>
    [Serializable]
    public class TimeEstimate
    {
        public MalfunctionType MalfunctionType;
        public MalfunctionSeverity Severity;
        public EquipmentType EquipmentType;
        public bool RequiresSpecialist;
        public TimeSpan EstimatedTime;
        public DateTime EstimationTimestamp;
        public float Confidence;
    }

    /// <summary>
    /// Comprehensive time breakdown
    /// </summary>
    [Serializable]
    public class TimeBreakdown
    {
        public string BreakdownId;
        public MalfunctionType MalfunctionType;
        public MalfunctionSeverity Severity;
        public EquipmentType EquipmentType;
        public bool RequiresSpecialist;
        public DateTime GenerationTime;

        public TimeSpan DiagnosticTime;
        public TimeSpan RepairTime;
        public TimeSpan PreparationTime;
        public TimeSpan TestingTime;
        public TimeSpan CleanupTime;
        public TimeSpan DocumentationTime;
        public TimeSpan TotalTime;

        public float Confidence;
    }

    /// <summary>
    /// Time estimate accuracy tracking
    /// </summary>
    [Serializable]
    public class TimeEstimateAccuracy
    {
        public MalfunctionType MalfunctionType;
        public MalfunctionSeverity Severity;
        public EquipmentType EquipmentType;
        public TimeSpan EstimatedTime;
        public TimeSpan ActualTime;
        public float AccuracyPercent;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Time estimation engine statistics
    /// </summary>
    [Serializable]
    public struct TimeEstimationStats
    {
        public int TimeEstimatesGenerated;
        public int DiagnosticEstimatesGenerated;
        public int TimeBreakdownsGenerated;
        public int EstimatesWithoutBaseData;
        public int EstimationErrors;

        public float TotalEstimationTime;
        public float AverageEstimationTime;
        public float MaxEstimationTime;

        public float TotalEstimatedTime;
        public float AverageEstimatedTime;

        public int AccuracyTrackingCount;
        public float TotalAccuracyScore;
        public float AverageAccuracy;
        public float BestAccuracy;
        public float WorstAccuracy;
    }

    #endregion
}

