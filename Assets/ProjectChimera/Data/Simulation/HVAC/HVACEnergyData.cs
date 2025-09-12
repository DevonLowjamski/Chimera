using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Energy data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>



    [System.Serializable]
    public class EnergyDataPoint
    {
        public float Consumption;
        public System.DateTime Timestamp;
    }



    [System.Serializable]
    public class EnergyOptimizationResult
    {
        public float PotentialSavings;
        public List<EquipmentOptimization> EquipmentOptimizations = new List<EquipmentOptimization>();
        public List<string> RecommendedActions = new List<string>();
        public float ImplementationCost;
        public float PaybackPeriodMonths;
    }



    [System.Serializable]
    public class ZoneEnergyReport
    {
        public string ZoneId;
        public string ZoneName;
        public float TotalEnergyConsumption;
        public Dictionary<string, float> EquipmentConsumption = new Dictionary<string, float>();
        public float EnergyEfficiency;
        public float TemperatureEfficiency;
        public List<string> OptimizationOpportunities = new List<string>();
    }

    [System.Serializable]
    public class EnergyConsumptionReport
    {
        public System.DateTime ReportDate;
        public System.TimeSpan ReportingPeriod;
        public List<ZoneEnergyReport> ZoneReports = new List<ZoneEnergyReport>();
        public float TotalEnergyConsumption;
        public float TotalEnergyCost;
        public float AverageEfficiency;
    }

    [System.Serializable]
    public class EquipmentOptimization
    {
        public string EquipmentId;
        public string OptimizationType;
        public float PotentialSavings;
        public string Description;
    }
}
