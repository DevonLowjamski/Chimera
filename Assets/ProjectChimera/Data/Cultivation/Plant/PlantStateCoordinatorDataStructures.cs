// REFACTORED: Plant State Coordinator Data Structures
// Extracted from PlantStateCoordinator for better separation of concerns

using System;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Plant state coordination statistics
    /// </summary>
    public struct PlantStateStats
    {
        public int AgeUpdates;
        public int StageTransitions;
        public int HealthChanges;
        public int StressChanges;
        public int PositionChanges;
        public int VitalityUpdates;
        public int GrowthUpdates;
        public int PhysicalUpdates;
        public int EnvironmentChanges;
        public int SnapshotsTaken;
    }

    /// <summary>
    /// Plant state snapshot for history tracking
    /// </summary>
    [Serializable]
    public struct PlantStateSnapshot
    {
        public DateTime Timestamp;
        public string Reason;
        public PlantGrowthStage GrowthStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public float Health;
        public float Vigor;
        public float StressLevel;
        public float Height;
        public float Width;
        public float MaturityLevel;
        public float GrowthProgress;
        public Vector3 Position;
    }

    /// <summary>
    /// Plant state summary
    /// </summary>
    [Serializable]
    public struct PlantStateSummary
    {
        public string PlantID;
        public PlantGrowthStage CurrentStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public float OverallHealth;
        public float Vigor;
        public float StressLevel;
        public float MaturityLevel;
        public float CurrentHeight;
        public float CurrentWidth;
        public float LeafArea;
        public bool IsActive;
        public DateTime LastUpdate;
        public float CumulativeStressDays;
        public float OptimalDays;
    }
}

