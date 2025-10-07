// REFACTORED: Data Structures
// Extracted from MalfunctionRepairProcessor.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    public class RepairOperation
    {
        public string RepairId;
        public string MalfunctionId;
        public string EquipmentId;
        public RepairType RepairType;
        public DateTime StartTime;
        public TimeSpan EstimatedDuration;
        public TimeSpan ActualDuration;
        public float RepairQuality;
        public bool UseSpecialist;
        public List<string> RequiredParts;
        public float EstimatedCost;
        public float ActualCost;
        public RepairStatus Status;
        public float Progress;
    }

    public enum RepairType
    {
        General,
        Mechanical,
        Electrical,
        Software,
        Thermal,
        Calibration
    }

    public enum RepairStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    public struct RepairStatistics
    {
        public int TotalRepairs;
        public int SuccessfulRepairs;
        public int FailedRepairs;
        public float SuccessRate;
        public float AverageRepairTime;
        public float AverageRepairCost;
        public float TotalRepairCost;
    }

    public struct MalfunctionRepairProcessorStats
    {
        public int RepairsStarted;
        public int RepairsCompleted;
        public int RepairsFailed;
        public int RepairsCancelled;
        public int RepairErrors;

        public float TotalProcessingTime;
        public float AverageProcessingTime;
        public float MaxProcessingTime;

        public float TotalRepairCost;
        public float AverageRepairCost;
        public float AverageRepairTime;

        // Repair type counters
        public int MechanicalRepairs;
        public int ElectricalRepairs;
        public int SoftwareRepairs;
        public int ThermalRepairs;
        public int CalibrationRepairs;
        public int GeneralRepairs;
    }

}
