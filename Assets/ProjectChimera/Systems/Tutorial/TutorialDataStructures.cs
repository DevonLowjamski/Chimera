using System;
using UnityEngine;

namespace ProjectChimera.Systems.Tutorial
{
    /// <summary>
    /// Data structures for Tutorial system.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>

    #region Tutorial Step Types

    /// <summary>
    /// Types of tutorial steps available.
    /// </summary>
    [Serializable]
    public enum TutorialStepType
    {
        CameraControls,             // Navigate using mouse/keyboard
        ModeToggling,               // Switch between Construction/Cultivation/Genetics modes
        PlantInspection,            // Click plants to view details
        WateringPlant,              // Use cultivation tools to water
        EnvironmentalAdjustment,    // Adjust temperature/humidity controls
        ConstructionBasics,         // Place walls and structures
        GeneticsIntroduction,       // View seed bank and strain details
        BreedingBasics,             // Perform basic breeding operation
        HarvestingProcess,          // Harvest and process plants
        TimeControl,                // Change time scale settings
        ResourceManagement,         // Monitor currency and skill points
        FacilityProgression         // Understanding facility tiers
    }

    #endregion

    #region Tutorial Step Data

    /// <summary>
    /// Configuration for a single tutorial step.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        public TutorialStepType StepType;
        public string InstructionTitle;
        public string InstructionText;
        public float EstimatedDuration; // seconds
        public bool RequiresPlayerAction;
        public string TargetObjectName; // Optional: GameObject to highlight
    }

    #endregion

    #region Tutorial Progress

    /// <summary>
    /// Tracks player progress through tutorial.
    /// </summary>
    [Serializable]
    public class TutorialProgress
    {
        public bool TutorialStarted;
        public bool TutorialCompleted;
        public int CurrentStepIndex;
        public int TotalStepsCompleted;
        public float TimeSpentInTutorial; // seconds
        public DateTime StartTime;
        public DateTime? CompletionTime;
        public bool[] StepCompletionStatus;
    }

    #endregion

    #region Tutorial Configuration

    /// <summary>
    /// Overall tutorial configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Chimera/Tutorial/Tutorial Configuration")]
    public class TutorialConfiguration : ScriptableObject
    {
        [Header("Tutorial Settings")]
        public bool EnableTutorialOnFirstLaunch = true;
        public bool AllowSkipTutorial = true;
        public string TutorialSceneName = "MassiveCustomFacility_Tutorial";
        public string PostTutorialSceneName = "SmallStorageBay_15x15";

        [Header("Tutorial Steps")]
        public TutorialStep[] Steps;

        [Header("Rewards")]
        public int StartingCurrency = 10000;
        public int StartingSkillPoints = 5;
        public string[] StartingStrains;
    }

    #endregion
}
