using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Shared;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    // Missing types for cultivation system - consolidated to fix CS0234/CS0246 errors

    /// <summary>
    /// Skill node types for cultivation progression
    /// </summary>
    public enum SkillNodeType
    {
        Basic,
        Intermediate,
        Advanced,
        Expert,
        Master
    }

    /// <summary>
    /// Care quality levels for plant care actions
    /// </summary>
    public enum CareQuality
    {
        Poor,
        Fair,
        Good,
        Excellent,
        Perfect,
        Failed,
        Average,
        Adequate,
        Suboptimal
    }

    /// <summary>
    /// Results of plant care actions
    /// </summary>
    [System.Serializable]
    public class PlantCareResult
    {
        public string PlantId;
        public CareQuality Quality;
        public float EffectivenessScore;
        public bool Success;
        public string Message;
        public DateTime CareTime;

        // Static properties for care result status
        public static bool Failed { get; } = false;
        public static bool Perfect { get; } = true;
        public static bool Successful { get; } = true;
        public static bool Adequate { get; } = true;
        public static bool Suboptimal { get; } = false;

        // Instance properties for care result status
        public bool IsFailed => !Success;
        public bool IsPerfect => Quality == CareQuality.Perfect;
        public bool IsSuccessful => Success;
        public bool IsAdequate => Quality == CareQuality.Adequate;
        public bool IsSuboptimal => Quality == CareQuality.Suboptimal;
    }

    /// <summary>
    /// Automation benefits for cultivation
    /// </summary>
    [System.Serializable]
    public class AutomationBenefits
    {
        public float EfficiencyBonus;
        public float QualityImprovement;
        public float TimeReduction;
        public float CostReduction;
        public bool UnlocksAdvancedFeatures;

        // Additional properties expected by InteractivePlantCareSystem
        public float QualityOptimization;
        public float EfficiencyGains;
    }

    /// <summary>
    /// Game time scale for time acceleration
    /// </summary>
    public enum GameTimeScale
    {
        Paused,
        Slow,
        SlowMotion,
        Baseline,
        Normal,
        Standard,
        Fast,
        VeryFast,
        UltraFast,
        Lightning
    }

    /// <summary>
    /// Time acceleration gaming configuration
    /// </summary>
    [CreateAssetMenu(fileName = "Time Acceleration Config", menuName = "Project Chimera/Cultivation/Time Acceleration Config")]
    public class TimeAccelerationGamingConfigSO : ScriptableObject
    {
        [Header("Time Scale Settings")]
        public float NormalSpeed = 1f;
        public float FastSpeed = 2f;
        public float VeryFastSpeed = 4f;
        public float UltraFastSpeed = 8f;

        [Header("Acceleration Limits")]
        public float MaxAcceleration = 10f;
        public bool AllowPausing = true;

        public float GetSpeedMultiplier(GameTimeScale scale)
        {
            return scale switch
            {
                GameTimeScale.Paused => 0f,
                GameTimeScale.Normal => NormalSpeed,
                GameTimeScale.Fast => FastSpeed,
                GameTimeScale.VeryFast => VeryFastSpeed,
                GameTimeScale.UltraFast => UltraFastSpeed,
                _ => NormalSpeed
            };
        }
    }

    /// <summary>
    /// Automatic desire level for plant care
    /// </summary>
    public enum AutomaticDesireLever
    {
        None,
        Low,
        Medium,
        High,
        Maximum
    }

    /// <summary>
    /// Event player choice event data
    /// </summary>
    [System.Serializable]
    public class EventPlayerChoiceEventData
    {
        public string EventId;
        public string ChoiceId;
        public string ChoiceText;
        public Dictionary<string, object> ChoiceData;
        public DateTime ChoiceTime;
    }

    /// <summary>
    /// Consequence types for actions
    /// </summary>
    public enum ConsequenceType
    {
        Positive,
        Negative,
        Neutral,
        Mixed
    }

    // Note: CareAction already defined in other files - removed to avoid CS0101 duplicate definition error

    /// <summary>
    /// Qualification result for various checks
    /// </summary>
    [System.Serializable]
    public class QualificationResult
    {
        public bool IsQualified;
        public float Score;
        public string Reason;
        public List<string> Requirements;
        public List<string> MissingRequirements;
    }

    /// <summary>
    /// Time transition state for time management
    /// </summary>
    public enum TimeTransitionState
    {
        Idle,
        Starting,
        Stable,
        Transitioning,
        Paused,
        Accelerated,
        Error
    }

    /// <summary>
    /// Time transition configuration
    /// </summary>
    [CreateAssetMenu(fileName = "Time Transition Config", menuName = "Project Chimera/Cultivation/Time Transition Config")]
    public class TimeTransitionConfigSO : ScriptableObject
    {
        [Header("Transition Settings")]
        public float TransitionDuration = 1f;
        public AnimationCurve TransitionCurve;
        public bool AllowInstantTransitions = false;

        [Header("Time Scale Limits")]
        public float MinTimeScale = 0f;
        public float MaxTimeScale = 10f;
    }

    /// <summary>
    /// Plant achievement service implementation for cultivation gaming
    /// </summary>
    public class PlantAchievementService : MonoBehaviour
    {
        private CultivationEventTracker _tracker = new CultivationEventTracker();

        public bool IsInitialized { get; private set; }
        public bool EnableAchievementTracking { get; set; } = true;

        // Achievement statistics properties
        public int TotalPlantsCreated => _tracker.TotalPlantsCreated;
        public int TotalPlantsHarvested => _tracker.TotalPlantsHarvested;
        public float TotalYieldHarvested => _tracker.TotalYieldHarvested;
        public float HighestQualityAchieved => _tracker.HighestQualityAchieved;
        public int HealthyPlantsCount => _tracker.HealthyPlantsCount;
        public int StrainDiversity => _tracker.StrainDiversity;

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Shutdown()
        {
            IsInitialized = false;
        }

        public void TrackPlantCreation(PlantInstance plant)
        {
            if (EnableAchievementTracking)
                _tracker.OnPlantCreated(plant);
        }

        public void TrackPlantHarvest(PlantInstance plant, HarvestResults results)
        {
            if (EnableAchievementTracking)
                _tracker.OnPlantHarvested(plant, results);
        }

        public void TrackPlantDeath(PlantInstance plant)
        {
            if (EnableAchievementTracking)
                _tracker.OnPlantDied(plant);
        }

        public void TrackPlantHealthChange(PlantInstance plant)
        {
            if (EnableAchievementTracking)
                _tracker.OnPlantHealthChanged(plant);
        }

        public void TrackPlantGrowthStageChange(PlantInstance plant)
        {
            if (EnableAchievementTracking)
                _tracker.OnPlantGrowthStageChanged(plant);
        }

        public PlantAchievementStats GetAchievementStats()
        {
            return new PlantAchievementStats
            {
                TotalPlantsCreated = TotalPlantsCreated,
                TotalPlantsHarvested = TotalPlantsHarvested,
                TotalYieldHarvested = TotalYieldHarvested,
                HighestQualityAchieved = HighestQualityAchieved,
                HealthyPlantsCount = HealthyPlantsCount,
                StrainDiversity = StrainDiversity,
                SurvivalRate = TotalPlantsCreated > 0 ? (float)TotalPlantsHarvested / TotalPlantsCreated : 0f,
                AverageYieldPerPlant = TotalPlantsHarvested > 0 ? TotalYieldHarvested / TotalPlantsHarvested : 0f
            };
        }

        // Legacy methods for compatibility
        public void ProcessAchievement(string achievementId) { }
        public bool IsAchievementUnlocked(string achievementId) { return false; }
        public void UnlockAchievement(string achievementId) { }
    }

    /// <summary>
    /// Enhanced cultivation gaming manager placeholder
    /// </summary>
    public class EnhancedCultivationGamingManager : MonoBehaviour
    {
        // Placeholder implementation for deleted gaming system
        public void InitializeGamingSystem() { }
        public void ProcessCultivationAction(string actionId) { }
        public void UpdateGameState() { }
    }

    /// <summary>
    /// Skill progression event data placeholder
    /// </summary>
    [System.Serializable]
    public class SkillProgressionEventData
    {
        public CultivationTaskType TaskType;
        public SkillMilestone Milestone;
        public float CurrentSkillLevel;
        public float Timestamp;
    }

    /// <summary>
    /// Equipment status for cultivation systems
    /// </summary>
    public enum EquipmentStatus
    {
        Offline,
        Online,
        Maintenance,
        Error,
        Installing,
        Operational
    }

    /// <summary>
    /// Plant instance data structure for runtime use
    /// </summary>
    [System.Serializable]
    public class PlantInstanceData
    {
        public string PlantID;
        public string PlantName;
        // public object Strain; // object not available
        public ProjectChimera.Data.Shared.PlantGrowthStage CurrentGrowthStage;
        public float AgeInDays;
        public float DaysInCurrentStage;
        public Vector3 WorldPosition;
        public float CurrentHeight;
        public float CurrentWidth;
        public float OverallHealth = 0.8f;
        public float Vigor = 0.8f;
        public float StressLevel = 0.1f;
        public float WaterLevel = 1f;
        public float NutrientLevel = 1f;
        public float EnergyReserves = 1f;
        public GameObject PlantObject;
        public MonoBehaviour PlantComponent;

        public PlantInstanceData()
        {
            PlantID = System.Guid.NewGuid().ToString();
            CurrentGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage.Seed;
        }

        public PlantInstanceData(object strain, Vector3 position)
        {
            PlantID = System.Guid.NewGuid().ToString();
            // Strain = strain; // object not available
            WorldPosition = position;
            PlantName = "Unknown Plant"; // object not available
            CurrentGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage.Seed;
        }
    }

    /// <summary>
    /// Harvest results data structure
    /// </summary>
    [System.Serializable]
    public class HarvestResults
    {
        public string PlantId;
        public float TotalYield;
        public float QualityScore;
        public float Potency;
        public DateTime HarvestDate;
        public Dictionary<string, float> SecondaryMetrics;
        public Dictionary<string, float> CannabinoidProfile;
        public Dictionary<string, float> Terpenes;
        public int FloweringDays;
        public float FinalHealth;

        public HarvestResults()
        {
            SecondaryMetrics = new Dictionary<string, float>();
            CannabinoidProfile = new Dictionary<string, float>();
            Terpenes = new Dictionary<string, float>();
            HarvestDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Plant achievement statistics
    /// </summary>
    [System.Serializable]
    public class PlantAchievementStats
    {
        public int TotalPlantsCreated;
        public int TotalPlantsHarvested;
        public float TotalYieldHarvested;
        public float HighestQualityAchieved;
        public int HealthyPlantsCount;
        public int StrainDiversity;
        public float SurvivalRate;
        public float AverageYieldPerPlant;
    }

    /// <summary>
    /// Cultivation event tracker for achievement system
    /// </summary>
    [System.Serializable]
    public class CultivationEventTracker
    {
        public int TotalPlantsCreated { get; private set; }
        public int TotalPlantsHarvested { get; private set; }
        public float TotalYieldHarvested { get; private set; }
        public float HighestQualityAchieved { get; private set; }
        public int HealthyPlantsCount { get; private set; }
        public int StrainDiversity { get; private set; }

        private HashSet<string> _uniqueStrains = new HashSet<string>();

        public void OnPlantCreated(PlantInstance plant)
        {
            TotalPlantsCreated++;
            if (plant.Strain != null)
            {
                _uniqueStrains.Add((plant.Strain as UnityEngine.Object)?.name ?? "Unknown");
                StrainDiversity = _uniqueStrains.Count;
            }
        }

        public void OnPlantHarvested(PlantInstance plant, HarvestResults results)
        {
            TotalPlantsHarvested++;
            TotalYieldHarvested += results.TotalYield;
            if (results.QualityScore > HighestQualityAchieved)
            {
                HighestQualityAchieved = results.QualityScore;
            }
        }

        public void OnPlantDied(PlantInstance plant)
        {
            // Track plant deaths for statistics
        }

        public void OnPlantHealthChanged(PlantInstance plant)
        {
            // Update health tracking if needed
        }

        public void OnPlantGrowthStageChanged(PlantInstance plant)
        {
            // Track growth stage changes
        }
    }

    /// <summary>
    /// Skill milestone for cultivation progression
    /// </summary>
    public enum SkillMilestone
    {
        Beginner,
        Novice,
        Intermediate,
        Advanced,
        Expert,
        Master
    }

    /// <summary>
    /// Cultivation task type
    /// </summary>
    public enum CultivationTaskType
    {
        Watering,
        Feeding,
        Pruning,
        Training,
        Harvesting,
        Monitoring
    }
}
