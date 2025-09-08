using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Analytics data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>

    /// <summary>
    /// Comprehensive data structures for HVAC system management in Project Chimera.
    /// Includes zone management, equipment control, automation, and energy optimization.
    /// </summary>

    // Enums for atmospheric breakthrough types and impacts
    public enum BreakthroughType
    {
        TechnicalInnovation,
        ProcessOptimization,
        EnergyEfficiency,
        ClimateControl,
        AutomationAdvancement,
        SustainabilityImprovement,
        CostReduction,
        PerformanceEnhancement,
        SafetyImprovement,
        QualityOptimization
    }



    public enum BreakthroughImpact
    {
        Low,
        Medium,
        High,
        Revolutionary,
        IndustryChanging
    }



    [System.Serializable]
    public class ZoneEnergyReport
    {
        public string ZoneId;
        public string ZoneName;
        public float TotalEnergyConsumption;
        public Dictionary<string, float> EquipmentConsumption = new Dictionary<string, float>();
        public float EfficiencyRating;
        public List<string> OptimizationOpportunities = new List<string>();
    }


    [System.Serializable]
    public class EnvironmentalGamingMetrics
    {
        public int OptimizationsCompleted;
        public float EfficiencyGained;
        public int BreakthroughsAchieved;
        public DateTime LastUpdated;
        public float AveragePerformanceScore;
        public int TotalExperiments;
        
        // Missing properties referenced in EnhancedEnvironmentalGamingManager.cs
        public int ActiveChallenges;
        public int ActiveCollaborations;
        public int TotalPlayers;
        public int TotalInnovations;
        public int TotalBreakthroughs;
        
        // Missing method referenced in EnhancedEnvironmentalGamingManager.cs line 303
        public void UpdateScore(string processorId, float score)
        {
            AveragePerformanceScore = (AveragePerformanceScore + score) / 2f;
            LastUpdated = DateTime.Now;
        }
    }
}
