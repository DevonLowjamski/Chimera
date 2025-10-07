// REFACTORED: Data Structures
// Extracted from CostDatabaseStorageManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;
using EquipmentType = ProjectChimera.Data.Equipment.EquipmentType;
using MalfunctionType = ProjectChimera.Systems.Equipment.Degradation.MalfunctionType;
using MalfunctionSeverity = ProjectChimera.Systems.Equipment.Degradation.MalfunctionSeverity;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    public class DatabaseStorageStatistics
    {
        public int QueriesHandled = 0;
        public int SuccessfulQueries = 0;
        public int MissedQueries = 0;
        public int DatabaseUpdates = 0;
        public int UpdateErrors = 0;
        public int ProfileQueries = 0;
        public int ProfileUpdates = 0;
        public int ProfileUpdateErrors = 0;
        public int DatabaseLoads = 0;
        public int LoadErrors = 0;
        public DateTime LastOperation = DateTime.MinValue;
    }

    public class CostDatabaseEntry
    {
        public MalfunctionType MalfunctionType;
        public float BaseCost;
        public float BaseRepairTime;
        public float AverageCost;
        public float AverageRepairTime;
        public float MinCost;
        public float MaxCost;
        public float CostStandardDeviation;
        public float Confidence;
        public int SampleCount;
        public int AccessCount;
        public DateTime LastUpdated;
        public DateTime LastAccessed;
        public List<CostDataPoint> CostHistory = new List<CostDataPoint>();
    }

    public class EquipmentCostProfile
    {
        public EquipmentType EquipmentType;
        public float CostMultiplier;
        public float ComplexityFactor;
        public float PartAvailability;
        public int MaintenanceFrequency; // Days between maintenance
        public int AverageLifespan; // Years
        public int AccessCount;
        public DateTime LastProfileUpdate;
        public DateTime LastAccessed;
    }

    public struct CostDataPoint
    {
        public DateTime Timestamp;
        public float ActualCost;
        public TimeSpan ActualTime;
        public MalfunctionSeverity Severity;
        public EquipmentType EquipmentType;
        public MalfunctionType MalfunctionType;
    }

}
