using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Cultivation Path Data - Defines cultivation progression paths and learning routes
    /// Contains structured learning paths for different cultivation specializations
    /// </summary>
    
    [System.Serializable]
    public class CultivationPathData
    {
        [Header("Path Identity")]
        public string PathId;
        public string PathName;
        [TextArea(2, 4)] public string Description;
        public CultivationSpecialization Specialization;
        public CultivationApproach Approach;
        
        [Header("Path Structure")]
        public PathDifficulty Difficulty = PathDifficulty.Beginner;
        [Range(1, 100)] public int EstimatedDurationHours = 20;
        [Range(1, 10)] public int PathTier = 1;
        public bool IsLinearPath = true;
        
        [Header("Learning Stages")]
        public LearningStage[] LearningStages = new LearningStage[0];
        public PathMilestone[] Milestones = new PathMilestone[0];
        
        [Header("Requirements")]
        public string[] Prerequisites = new string[0];
        [Range(1, 100)] public int MinimumPlayerLevel = 1;
        public string[] RequiredSkillNodes = new string[0];
        
        [Header("Rewards")]
        public PathReward[] CompletionRewards = new PathReward[0];
        public string[] UnlockedPaths = new string[0];
        public string[] UnlockedFeatures = new string[0];
        public string CertificationAwarded;
        
        [Header("Path Effects")]
        public Dictionary<string, float> PathModifiers = new Dictionary<string, float>();
        
        [Header("Visual")]
        public Sprite PathIcon;
        public Color PathColor = Color.white;
        public Texture2D PathBanner;
        
        // Runtime Data
        public PathProgressData ProgressData = new PathProgressData();
        
        /// <summary>
        /// Get current progress percentage as a property
        /// </summary>
        public float ProgressPercentage => GetCompletionPercentage();
        
        /// <summary>
        /// Get current learning stage based on progress
        /// </summary>
        public LearningStage GetCurrentStage()
        {
            if (LearningStages == null || LearningStages.Length == 0)
                return null;
            
            var currentStageIndex = Mathf.Clamp(ProgressData.CurrentStageIndex, 0, LearningStages.Length - 1);
            return LearningStages[currentStageIndex];
        }
        
        /// <summary>
        /// Get next learning stage
        /// </summary>
        public LearningStage GetNextStage()
        {
            var nextIndex = ProgressData.CurrentStageIndex + 1;
            if (nextIndex >= LearningStages.Length)
                return null;
            
            return LearningStages[nextIndex];
        }
        
        /// <summary>
        /// Calculate overall path completion percentage
        /// </summary>
        public float GetCompletionPercentage()
        {
            if (LearningStages == null || LearningStages.Length == 0)
                return 0f;
            
            var totalStages = LearningStages.Length;
            var completedStages = ProgressData.CompletedStages.Count;
            var currentStageProgress = GetCurrentStage()?.GetCompletionPercentage() ?? 0f;
            
            return (completedStages + currentStageProgress) / totalStages;
        }
        
        /// <summary>
        /// Check if path is completed
        /// </summary>
        public bool IsCompleted()
        {
            return ProgressData.IsCompleted;
        }
        
        /// <summary>
        /// Check if prerequisites are met
        /// </summary>
        public bool ArePrerequisitesMet(List<string> completedPaths, List<string> unlockedSkillNodes, int playerLevel)
        {
            // Check player level
            if (playerLevel < MinimumPlayerLevel)
                return false;
            
            // Check prerequisite paths
            if (Prerequisites != null)
            {
                foreach (var prerequisite in Prerequisites)
                {
                    if (!completedPaths.Contains(prerequisite))
                        return false;
                }
            }
            
            // Check required skill nodes
            if (RequiredSkillNodes != null)
            {
                foreach (var skillNode in RequiredSkillNodes)
                {
                    if (!unlockedSkillNodes.Contains(skillNode))
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Advance to next stage
        /// </summary>
        public bool AdvanceToNextStage()
        {
            var currentStage = GetCurrentStage();
            if (currentStage == null || !currentStage.IsCompleted())
                return false;
            
            ProgressData.CompletedStages.Add(ProgressData.CurrentStageIndex);
            ProgressData.CurrentStageIndex++;
            
            // Check if path is completed
            if (ProgressData.CurrentStageIndex >= LearningStages.Length)
            {
                ProgressData.IsCompleted = true;
                ProgressData.CompletionDate = DateTime.Now;
            }
            
            return true;
        }
    }
    
    [System.Serializable]
    public class LearningStage
    {
        [Header("Stage Identity")]
        public string StageId;
        public string StageName;
        [TextArea(2, 3)] public string Description;
        [Range(1, 20)] public int StageOrder = 1;
        
        [Header("Learning Objectives")]
        public LearningObjective[] Objectives = new LearningObjective[0];
        public string[] KeyConcepts = new string[0];
        public string[] PracticalSkills = new string[0];
        
        [Header("Activities")]
        public LearningActivity[] Activities = new LearningActivity[0];
        public AssessmentCriteria[] Assessments = new AssessmentCriteria[0];
        
        [Header("Resources")]
        public string[] RequiredResources = new string[0];
        public string[] RecommendedReading = new string[0];
        public string[] VideoTutorials = new string[0];
        
        [Header("Progression")]
        [Range(1, 100)] public int EstimatedHours = 5;
        [Range(0f, 1f)] public float PassingScore = 0.7f;
        public bool AllowRetry = true;
        
        // Runtime Data
        public StageProgressData ProgressData = new StageProgressData();
        
        /// <summary>
        /// Get completion percentage for this stage
        /// </summary>
        public float GetCompletionPercentage()
        {
            if (Objectives == null || Objectives.Length == 0)
                return ProgressData.IsCompleted ? 1f : 0f;
            
            var completedObjectives = 0;
            foreach (var objective in Objectives)
            {
                if (objective.IsCompleted())
                    completedObjectives++;
            }
            
            return (float)completedObjectives / Objectives.Length;
        }
        
        /// <summary>
        /// Check if stage is completed
        /// </summary>
        public bool IsCompleted()
        {
            return ProgressData.IsCompleted || GetCompletionPercentage() >= PassingScore;
        }
        
        /// <summary>
        /// Get current learning objective
        /// </summary>
        public LearningObjective GetCurrentObjective()
        {
            if (Objectives == null || Objectives.Length == 0)
                return null;
            
            foreach (var objective in Objectives)
            {
                if (!objective.IsCompleted())
                    return objective;
            }
            
            return null; // All completed
        }
    }
    
    [System.Serializable]
    public class LearningObjective
    {
        [Header("Objective Definition")]
        public string ObjectiveId;
        public string ObjectiveName;
        [TextArea(1, 3)] public string Description;
        public ObjectiveType Type = ObjectiveType.Knowledge;
        
        [Header("Success Criteria")]
        public SuccessCriteria[] Criteria = new SuccessCriteria[0];
        [Range(0f, 1f)] public float RequiredAccuracy = 0.8f;
        [Range(1, 10)] public int MinimumAttempts = 1;
        
        [Header("Measurement")]
        public string MeasurementMethod;
        public string[] ExpectedOutcomes = new string[0];
        
        // Runtime Data
        public ObjectiveProgressData ProgressData = new ObjectiveProgressData();
        
        /// <summary>
        /// Check if objective is completed
        /// </summary>
        public bool IsCompleted()
        {
            return ProgressData.IsCompleted;
        }
        
        /// <summary>
        /// Update objective progress
        /// </summary>
        public void UpdateProgress(float progress, bool isCompleted = false)
        {
            ProgressData.Progress = Mathf.Clamp01(progress);
            ProgressData.AttemptCount++;
            
            if (isCompleted || progress >= RequiredAccuracy)
            {
                ProgressData.IsCompleted = true;
                ProgressData.CompletionDate = DateTime.Now;
            }
        }
    }
    
    [System.Serializable]
    public class LearningActivity
    {
        [Header("Activity Definition")]
        public string ActivityId;
        public string ActivityName;
        [TextArea(1, 3)] public string Description;
        public ActivityType Type = ActivityType.Practical;
        
        [Header("Requirements")]
        public string[] RequiredTools = new string[0];
        public string[] RequiredMaterials = new string[0];
        [Range(1, 480)] public int EstimatedMinutes = 30;
        
        [Header("Instructions")]
        [TextArea(3, 6)] public string Instructions;
        public string[] Steps = new string[0];
        public string[] SafetyNotes = new string[0];
        
        [Header("Assessment")]
        public string[] SuccessIndicators = new string[0];
        public string[] CommonMistakes = new string[0];
        
        // Runtime Data
        public ActivityProgressData ProgressData = new ActivityProgressData();
    }
    
    [System.Serializable]
    public class PathMilestone
    {
        [Header("Milestone Definition")]
        public string MilestoneId;
        public string MilestoneName;
        [TextArea(1, 2)] public string Description;
        [Range(0f, 1f)] public float ProgressThreshold = 0.5f;
        
        [Header("Rewards")]
        public MilestoneReward[] Rewards = new MilestoneReward[0];
        public string[] UnlockedContent = new string[0];
        
        [Header("Recognition")]
        public string BadgeAwarded;
        public string CertificateTemplate;
        public bool IsPublicAchievement = true;
        
        // Runtime Data
        public bool IsAchieved = false;
        public DateTime AchievementDate;
    }
    
    #region Progress Data Classes
    
    [System.Serializable]
    public class PathProgressData
    {
        public bool IsStarted = false;
        public bool IsCompleted = false;
        public int CurrentStageIndex = 0;
        public List<int> CompletedStages = new List<int>();
        public DateTime StartDate;
        public DateTime CompletionDate;
        public float TotalTimeSpent = 0f; // Hours
        public int TotalAttempts = 0;
        public float BestScore = 0f;
    }
    
    [System.Serializable]
    public class StageProgressData
    {
        public bool IsStarted = false;
        public bool IsCompleted = false;
        public DateTime StartDate;
        public DateTime CompletionDate;
        public float TimeSpent = 0f; // Hours
        public int AttemptCount = 0;
        public float BestScore = 0f;
        public Dictionary<string, float> ObjectiveScores = new Dictionary<string, float>();
    }
    
    [System.Serializable]
    public class ObjectiveProgressData
    {
        public bool IsCompleted = false;
        public float Progress = 0f;
        public int AttemptCount = 0;
        public DateTime CompletionDate;
        public float BestScore = 0f;
        public string[] CompletedCriteria = new string[0];
    }
    
    [System.Serializable]
    public class ActivityProgressData
    {
        public bool IsCompleted = false;
        public DateTime StartDate;
        public DateTime CompletionDate;
        public float TimeSpent = 0f; // Minutes
        public int AttemptCount = 0;
        public float QualityScore = 0f;
        public string[] NotesFromInstructor = new string[0];
    }
    
    #endregion
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class PathReward
    {
        public RewardType Type = RewardType.Experience;
        public float Value;
        public string Description;
        public string ItemId; // For item rewards
    }
    
    [System.Serializable]
    public class MilestoneReward
    {
        public RewardType Type = RewardType.Badge;
        public string RewardId;
        public string RewardName;
        public float Value;
    }
    
    [System.Serializable]
    public class AssessmentCriteria
    {
        public string CriteriaName;
        [TextArea(1, 2)] public string Description;
        [Range(0f, 1f)] public float Weight = 1f;
        [Range(0f, 1f)] public float PassingThreshold = 0.7f;
        public AssessmentMethod Method = AssessmentMethod.Observation;
    }
    
    [System.Serializable]
    public class SuccessCriteria
    {
        public string CriteriaName;
        public string MeasurementMethod;
        public float TargetValue;
        public string TargetUnit;
        public bool IsOptional = false;
    }
    
    #endregion
    
    #region Enums
    
    public enum CultivationSpecialization
    {
        GeneralCultivation,
        HydroponicSystems,
        OrganicMethods,
        AutomatedSystems,
        CommercialProduction,
        ResearchAndDevelopment,
        QualityControl,
        GeneticsAndBreeding
    }
    
    public enum PathDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Master
    }
    
    public enum ObjectiveType
    {
        Knowledge,      // Understanding concepts
        Skill,          // Practical ability
        Application,    // Applying knowledge
        Analysis,       // Analyzing data/situations
        Synthesis,      // Creating new solutions
        Evaluation      // Judging quality/effectiveness
    }
    
    public enum ActivityType
    {
        Reading,
        Practical,
        Observation,
        Experiment,
        Discussion,
        Assessment,
        Project,
        Simulation
    }
    
    public enum AssessmentMethod
    {
        Observation,
        Testing,
        Portfolio,
        Demonstration,
        PeerReview,
        SelfAssessment,
        AutomatedCheck
    }
    
    public enum RewardType
    {
        Experience,
        Currency,
        Item,
        Badge,
        Certificate,
        UnlockAccess,
        SkillBonus
    }
    
    #endregion
}