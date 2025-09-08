using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Monitoring data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>



    public enum CertificationStatus
    {
        InProgress,
        Completed,
        Failed,
        Expired
    }



    public enum SessionStatus
    {
        Planning,
        Active,
        Paused,
        Completed,
        Aborted
    }



    [System.Serializable]
    public class HVACSystemStatus
    {
        public int TotalZones;
        public int ActiveZones;
        public int TotalEquipment;
        public int RunningEquipment;
        public float SystemEfficiency;
        public float TotalEnergyConsumption;
        public int ActiveAlarms;
        public System.DateTime LastUpdated;
    }



    [System.Serializable]
    public class VPDOptimizationStatus
    {
        public float CurrentVPD;
        public float TargetVPD;
        public float VPDDeviation;
        public bool IsOptimal;
        public List<string> OptimizationActions = new List<string>();
    }



    public enum HVACEquipmentStatus
    {
        Off,
        Standby,
        Running,
        Fault,
        Maintenance
    }



    public enum HVACAlarmStatus
    {
        Active,
        Acknowledged,
        Cleared,
        Disabled
    }



    public enum MaintenanceScheduleStatus
    {
        Scheduled,
        In_Progress,
        Completed,
        Overdue,
        Cancelled
    }



    public enum MaintenanceStatus
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }


    
    public enum ConnectionStatus
    {
        Pending,
        Accepted,
        Declined,
        Active,
        Inactive
    }
}
