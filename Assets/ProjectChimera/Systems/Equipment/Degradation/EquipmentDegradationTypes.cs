using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    // Minimal enums and structs used by degradation systems
    public enum OperationalStatus
    {
        Online,
        Offline,
        Maintenance_Required
    }

    public enum MalfunctionType
    {
        WearAndTear,
        MechanicalFailure,
        ElectricalFailure,
        SensorDrift,
        OverheatingProblem,
        SoftwareError,
        // Additional values for cost database
        ElectricalFault,
        MechanicalWear,
        SoftwareGlitch,
        Overheating,
        Corrosion,
        Contamination,
        Calibration,
        SensorFailure,
        PowerSupplyIssue,
        NetworkConnectivity
    }

    public enum MalfunctionSeverity
    {
        Minor,
        Moderate,
        Major,
        Critical,
        Catastrophic
    }

    public enum RepairComplexity
    {
        Simple,
        Moderate,
        Complex,
        ExpertRequired,
        ManufacturerService
    }

    [Serializable]
    public struct EnvironmentalStressFactors
    {
        public float TemperatureStress;
        public float HumidityStress;
        public float DustAccumulation;
        public float ElectricalStress;
    }

    [Serializable]
    public class EquipmentReliabilityProfile
    {
        public Data.Equipment.EquipmentType Type;
        public float MeanTimeBetweenFailures;
        public float AverageLifespan;
        public float FailureRate;
        public float WearProgressionRate;
        public float CriticalWearThreshold;
        public Dictionary<MalfunctionType, float> CommonFailureModes = new Dictionary<MalfunctionType, float>
        {
            { MalfunctionType.WearAndTear, 1f },
            { MalfunctionType.MechanicalFailure, 0.5f },
            { MalfunctionType.ElectricalFailure, 0.5f },
            { MalfunctionType.SensorDrift, 0.25f }
        };
        public Dictionary<string, float> EnvironmentalSensitivity = new Dictionary<string, float>
        {
            { "Temperature", 1f },
            { "Humidity", 1f }
        };
    }

    // Minimal analysis record used by detection wrapper
    [Serializable]
    public struct MalfunctionAnalysis
    {
        public string EquipmentId;
        public MalfunctionType MalfunctionType;
        public float Confidence;
    }
}


