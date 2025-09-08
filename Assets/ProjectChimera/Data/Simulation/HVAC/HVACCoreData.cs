using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Core data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>



    [System.Serializable]
    public enum HVACCertificationLevel
    {
        Foundation,
        Intermediate,
        Advanced,
        Expert,
        Master
    }



    public class HVACCertificationEnrollment
    {
        public string EnrollmentId;
        public HVACCertificationLevel Level;
        public System.DateTime EnrolledAt;
        public System.DateTime ExpectedCompletion;
        public float Progress;
        public CertificationStatus Status;
        public string PlayerId;
        public List<string> CompletedModules = new List<string>();
        public float CurrentScore;
    }



    public class HVACSystemSettings
    {
        [Range(0.1f, 5f)] public float DefaultTemperatureTolerance = 1f;
        [Range(0.5f, 10f)] public float DefaultHumidityTolerance = 3f;
        [Range(0.01f, 1f)] public float DefaultAirflowTolerance = 0.1f;
        public bool EnableAdvancedDiagnostics = true;
        public bool EnableEnergyOptimization = true;
        public bool EnablePredictiveControl = true;
        [Range(1f, 168f)] public float MaintenanceCheckInterval = 24f; // Hours
        [Range(1f, 72f)] public float EnergyOptimizationInterval = 8f; // Hours
        [Range(60f, 3600f)] public float AlarmResponseTime = 300f; // Seconds
    }
}
