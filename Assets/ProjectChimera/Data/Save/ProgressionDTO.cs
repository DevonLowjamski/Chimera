using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Progression System Save/Load Operations (STUB)
    /// These DTOs capture basic player progression state including experience, skills,
    /// achievements, and unlocks. This is a minimal implementation to support save/load
    /// operations until full progression systems are implemented.
    /// </summary>
    
    /// <summary>
    /// Main progression state DTO for the basic progression system
    /// </summary>
    [System.Serializable]
    public class ProgressionStateDTO
    {
        [Header("Player Progress")]
        public PlayerProgressDTO PlayerProgress;
        
        [Header("Skills System")]
        public SkillSystemDTO SkillSystem;
        
        [Header("Achievements System")]
        public AchievementSystemDTO AchievementSystem;
        
        [Header("Unlocks and Content")]
        public UnlockSystemDTO UnlockSystem;
        
        [Header("Experience Tracking")]
        public ExperienceSystemDTO ExperienceSystem;
        
        [Header("Tutorial and Learning")]
        public TutorialSystemDTO TutorialSystem;
        
        [Header("System Configuration")]
        public bool EnableProgressionTracking = true;
        public bool EnableSkillSystem = true;
        public bool EnableAchievements = true;
        public bool EnableExperienceGain = true;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for overall player progress tracking
    /// </summary>
    [System.Serializable]
    public class PlayerProgressDTO
    {
        [Header("Player Level")]
        public int PlayerLevel = 1;
        public float TotalExperience = 0f;
        public float CurrentLevelExperience = 0f;
        public float ExperienceToNextLevel = 100f;
        public float ExperienceMultiplier = 1.0f;
        
        [Header("Playtime Tracking")]
        public float TotalPlaytimeHours = 0f;
        public DateTime FirstPlayTime;
        public DateTime LastPlayTime;
        public int SessionCount = 0;
        public float AverageSessionLength = 0f;
        
        [Header("General Progress")]
        public float OverallCompletionPercentage = 0f;
        public int MilestonesCompleted = 0;
        public int TotalMilestones = 100;
        public List<string> CompletedMilestones = new List<string>();
        
        [Header("Activity Counters")]
        public Dictionary<string, int> ActivityCounts = new Dictionary<string, int>();
        public Dictionary<string, float> ActivityValues = new Dictionary<string, float>();
        
        [Header("Progress History")]
        public List<ProgressEventDTO> ProgressHistory = new List<ProgressEventDTO>();
        
        [Header("Player Preferences")]
        public PlayerPreferencesDTO PlayerPreferences;
    }
    
    /// <summary>
    /// DTO for skill system state
    /// </summary>
    [System.Serializable]
    public class SkillSystemDTO
    {
        [Header("Skill Categories")]
        public List<SkillCategoryDTO> SkillCategories = new List<SkillCategoryDTO>();
        
        [Header("Individual Skills")]
        public List<PlayerSkillDTO> PlayerSkills = new List<PlayerSkillDTO>();
        
        [Header("Skill Points")]
        public int AvailableSkillPoints = 0;
        public int TotalSkillPointsEarned = 0;
        public int TotalSkillPointsSpent = 0;
        
        [Header("Skill Modifiers")]
        public Dictionary<string, float> SkillModifiers = new Dictionary<string, float>();
        public Dictionary<string, float> SkillBonuses = new Dictionary<string, float>();
        
        [Header("Skill Unlocks")]
        public List<string> UnlockedSkills = new List<string>();
        public List<string> AvailableSkills = new List<string>();
        
        [Header("Skill Tree Progress")]
        public Dictionary<string, SkillTreeProgressDTO> SkillTreeProgress = new Dictionary<string, SkillTreeProgressDTO>();
    }
    
    /// <summary>
    /// DTO for individual skill data
    /// </summary>
    [System.Serializable]
    public class PlayerSkillDTO
    {
        public string SkillId;
        public string SkillName;
        public string SkillCategory;
        public int SkillLevel = 1;
        public float SkillExperience = 0f;
        public float ExperienceToNextLevel = 100f;
        public int MaxLevel = 100;
        public bool IsUnlocked = false;
        public bool IsMaxed = false;
        public DateTime FirstUnlocked;
        public DateTime LastLevelUp;
        public float SkillEfficiency = 1.0f;
        public List<string> UnlockedAbilities = new List<string>();
        public Dictionary<string, object> SkillProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for skill categories
    /// </summary>
    [System.Serializable]
    public class SkillCategoryDTO
    {
        public string CategoryId;
        public string CategoryName;
        public string Description;
        public int CategoryLevel = 1;
        public float CategoryProgress = 0f;
        public List<string> SkillIds = new List<string>();
        public bool IsUnlocked = true;
        public string IconName;
        public Color CategoryColor = Color.white;
    }
    
    /// <summary>
    /// DTO for achievement system state
    /// </summary>
    [System.Serializable]
    public class AchievementSystemDTO
    {
        [Header("Achievement Progress")]
        public List<AchievementProgressDTO> AchievementProgress = new List<AchievementProgressDTO>();
        
        [Header("Achievement Statistics")]
        public int TotalAchievements = 0;
        public int UnlockedAchievements = 0;
        public int CompletedAchievements = 0;
        public float CompletionPercentage = 0f;
        
        [Header("Achievement Categories")]
        public List<AchievementCategoryDTO> AchievementCategories = new List<AchievementCategoryDTO>();
        
        [Header("Recent Achievements")]
        public List<string> RecentlyCompletedAchievements = new List<string>();
        public DateTime LastAchievementCompleted;
        
        [Header("Achievement Rewards")]
        public List<AchievementRewardDTO> PendingRewards = new List<AchievementRewardDTO>();
        public List<AchievementRewardDTO> ClaimedRewards = new List<AchievementRewardDTO>();
    }
    
    /// <summary>
    /// DTO for individual achievement progress
    /// </summary>
    [System.Serializable]
    public class AchievementProgressDTO
    {
        public string AchievementId;
        public string AchievementName;
        public string Description;
        public string Category;
        public float CurrentProgress = 0f;
        public float RequiredProgress = 100f;
        public float ProgressPercentage = 0f;
        public bool IsCompleted = false;
        public bool IsUnlocked = false;
        public bool IsHidden = false;
        public DateTime CompletedDate;
        public DateTime UnlockedDate;
        public string Difficulty; // "Easy", "Medium", "Hard", "Legendary"
        public int PointValue = 10;
        public AchievementRewardDTO Reward;
        public Dictionary<string, float> ProgressTrackers = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for achievement categories
    /// </summary>
    [System.Serializable]
    public class AchievementCategoryDTO
    {
        public string CategoryId;
        public string CategoryName;
        public string Description;
        public List<string> AchievementIds = new List<string>();
        public int CompletedCount = 0;
        public int TotalCount = 0;
        public float CategoryProgress = 0f;
        public string IconName;
        public Color CategoryColor = Color.white;
    }
    
    /// <summary>
    /// DTO for unlock system state
    /// </summary>
    [System.Serializable]
    public class UnlockSystemDTO
    {
        [Header("Content Unlocks")]
        public List<ContentUnlockDTO> ContentUnlocks = new List<ContentUnlockDTO>();
        
        [Header("Feature Unlocks")]
        public List<string> UnlockedFeatures = new List<string>();
        public List<string> AvailableFeatures = new List<string>();
        
        [Header("Equipment Unlocks")]
        public List<string> UnlockedEquipment = new List<string>();
        public List<string> UnlockedStrains = new List<string>();
        public List<string> UnlockedRecipes = new List<string>();
        
        [Header("Area Unlocks")]
        public List<string> UnlockedAreas = new List<string>();
        public List<string> UnlockedRooms = new List<string>();
        public List<string> UnlockedFacilities = new List<string>();
        
        [Header("System Unlocks")]
        public List<string> UnlockedSystems = new List<string>();
        public List<string> UnlockedModes = new List<string>();
        
        [Header("Unlock Conditions")]
        public Dictionary<string, UnlockConditionDTO> UnlockConditions = new Dictionary<string, UnlockConditionDTO>();
    }
    
    /// <summary>
    /// DTO for content unlocks
    /// </summary>
    [System.Serializable]
    public class ContentUnlockDTO
    {
        public string UnlockId;
        public string ContentId;
        public string ContentName;
        public string ContentType; // "Equipment", "Strain", "Recipe", "Area", "Feature"
        public bool IsUnlocked = false;
        public DateTime UnlockDate;
        public string UnlockMethod; // "Level", "Achievement", "Purchase", "Quest"
        public UnlockConditionDTO UnlockCondition;
        public bool IsVisible = true;
        public string PreviewDescription;
    }
    
    /// <summary>
    /// DTO for unlock conditions
    /// </summary>
    [System.Serializable]
    public class UnlockConditionDTO
    {
        public string ConditionType; // "Level", "Skill", "Achievement", "Currency", "Item"
        public string RequiredId; // skill id, achievement id, etc.
        public float RequiredValue; // level, amount, etc.
        public string RequiredOperator; // ">=", "==", "!=", "<="
        public bool IsMet = false;
        public DateTime LastChecked;
        public string Description;
    }
    
    /// <summary>
    /// DTO for experience system state
    /// </summary>
    [System.Serializable]
    public class ExperienceSystemDTO
    {
        [Header("Experience Sources")]
        public Dictionary<string, ExperienceSourceDTO> ExperienceSources = new Dictionary<string, ExperienceSourceDTO>();
        
        [Header("Experience History")]
        public List<ExperienceGainEventDTO> ExperienceHistory = new List<ExperienceGainEventDTO>();
        
        [Header("Experience Multipliers")]
        public Dictionary<string, float> ExperienceMultipliers = new Dictionary<string, float>();
        public float GlobalExperienceMultiplier = 1.0f;
        
        [Header("Experience Curves")]
        public Dictionary<string, ExperienceCurveDTO> ExperienceCurves = new Dictionary<string, ExperienceCurveDTO>();
        
        [Header("Level Rewards")]
        public Dictionary<int, List<LevelRewardDTO>> LevelRewards = new Dictionary<int, List<LevelRewardDTO>>();
        public List<LevelRewardDTO> PendingRewards = new List<LevelRewardDTO>();
    }
    
    /// <summary>
    /// DTO for experience sources
    /// </summary>
    [System.Serializable]
    public class ExperienceSourceDTO
    {
        public string SourceId;
        public string SourceName;
        public string Category;
        public float BaseExperienceValue;
        public float CurrentMultiplier = 1.0f;
        public int TimesTriggered = 0;
        public DateTime LastTriggered;
        public bool IsEnabled = true;
        public Dictionary<string, object> SourceProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for experience gain events
    /// </summary>
    [System.Serializable]
    public class ExperienceGainEventDTO
    {
        public string EventId;
        public DateTime EventTime;
        public string SourceId;
        public string SourceName;
        public float ExperienceGained;
        public float Multiplier;
        public string Description;
        public bool WasLevelUp = false;
        public int NewLevel = 0;
    }
    
    /// <summary>
    /// DTO for tutorial system state
    /// </summary>
    [System.Serializable]
    public class TutorialSystemDTO
    {
        [Header("Tutorial Progress")]
        public List<TutorialProgressDTO> TutorialProgress = new List<TutorialProgressDTO>();
        
        [Header("Tutorial Settings")]
        public bool TutorialsEnabled = true;
        public bool ShowHints = true;
        public bool AutoAdvanceTutorials = false;
        public float TutorialSpeed = 1.0f;
        
        [Header("Help System")]
        public List<string> ViewedHelpTopics = new List<string>();
        public Dictionary<string, int> HelpTopicViewCounts = new Dictionary<string, int>();
        
        [Header("Onboarding")]
        public bool OnboardingCompleted = false;
        public float OnboardingProgress = 0f;
        public List<string> CompletedOnboardingSteps = new List<string>();
    }
    
    /// <summary>
    /// DTO for individual tutorial progress
    /// </summary>
    [System.Serializable]
    public class TutorialProgressDTO
    {
        public string TutorialId;
        public string TutorialName;
        public string Category;
        public bool IsStarted = false;
        public bool IsCompleted = false;
        public float Progress = 0f;
        public int CurrentStep = 0;
        public int TotalSteps = 1;
        public DateTime StartedDate;
        public DateTime CompletedDate;
        public List<string> CompletedSteps = new List<string>();
        public bool WasSkipped = false;
    }
    
    /// <summary>
    /// DTO for progress events
    /// </summary>
    [System.Serializable]
    public class ProgressEventDTO
    {
        public string EventId;
        public DateTime EventTime;
        public string EventType; // "LevelUp", "SkillUnlock", "Achievement", "Milestone"
        public string Description;
        public Dictionary<string, object> EventData = new Dictionary<string, object>();
        public string Category;
        public float ExperienceGained = 0f;
        public int SkillPointsGained = 0;
    }
    
    /// <summary>
    /// DTO for achievement rewards
    /// </summary>
    [System.Serializable]
    public class AchievementRewardDTO
    {
        public string RewardId;
        public string RewardType; // "Experience", "Currency", "Item", "Unlock", "SkillPoints"
        public string RewardName;
        public float RewardValue;
        public string RewardDescription;
        public bool IsClaimed = false;
        public DateTime ClaimedDate;
        public Dictionary<string, object> RewardData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for experience curves
    /// </summary>
    [System.Serializable]
    public class ExperienceCurveDTO
    {
        public string CurveId;
        public string CurveName;
        public string CurveType; // "Linear", "Exponential", "Logarithmic", "Custom"
        public float BaseValue = 100f;
        public float GrowthRate = 1.5f;
        public int MaxLevel = 100;
        public List<float> LevelThresholds = new List<float>();
    }
    
    /// <summary>
    /// DTO for level rewards
    /// </summary>
    [System.Serializable]
    public class LevelRewardDTO
    {
        public string RewardId;
        public int RequiredLevel;
        public string RewardType;
        public string RewardName;
        public float RewardValue;
        public string Description;
        public bool IsClaimed = false;
        public DateTime ClaimedDate;
    }
    
    /// <summary>
    /// DTO for skill tree progress
    /// </summary>
    [System.Serializable]
    public class SkillTreeProgressDTO
    {
        public string TreeId;
        public string TreeName;
        public List<string> UnlockedNodes = new List<string>();
        public List<string> AvailableNodes = new List<string>();
        public int TotalNodes = 0;
        public int UnlockedNodeCount = 0;
        public float TreeProgress = 0f;
        public int SkillPointsInvested = 0;
    }
    
    /// <summary>
    /// DTO for player preferences
    /// </summary>
    [System.Serializable]
    public class PlayerPreferencesDTO
    {
        [Header("UI Preferences")]
        public bool ShowExperienceNumbers = true;
        public bool ShowLevelUpAnimations = true;
        public bool ShowAchievementNotifications = true;
        public bool ShowProgressNotifications = true;
        
        [Header("Gameplay Preferences")]
        public string DifficultyLevel = "Normal"; // "Easy", "Normal", "Hard", "Expert"
        public bool EnableProgressionHelp = true;
        public bool AutoClaimRewards = false;
        
        [Header("Notification Preferences")]
        public bool EnableProgressionNotifications = true;
        public bool EnableSkillNotifications = true;
        public bool EnableAchievementNotifications = true;
        public bool EnableLevelUpNotifications = true;
        
        [Header("Privacy Preferences")]
        public bool ShareProgressWithFriends = false;
        public bool ShowInLeaderboards = false;
        public bool AllowProgressionTracking = true;
    }
    
    /// <summary>
    /// Result DTO for progression save operations
    /// </summary>
    [System.Serializable]
    public class ProgressionSaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int SkillsSaved;
        public int AchievementsSaved;
        public int UnlocksSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for progression load operations
    /// </summary>
    [System.Serializable]
    public class ProgressionLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int SkillsLoaded;
        public int AchievementsLoaded;
        public int UnlocksLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public ProgressionStateDTO ProgressionState;
    }
    
    /// <summary>
    /// DTO for progression system validation
    /// </summary>
    [System.Serializable]
    public class ProgressionValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("Progress Validation")]
        public bool PlayerProgressValid;
        public bool ExperienceDataValid;
        public bool LevelDataValid;
        
        [Header("Skills Validation")]
        public bool SkillSystemValid;
        public bool SkillLevelsValid;
        public bool SkillUnlocksValid;
        
        [Header("Achievements Validation")]
        public bool AchievementSystemValid;
        public bool AchievementProgressValid;
        public bool AchievementRewardsValid;
        
        [Header("Data Integrity")]
        public int TotalSkills;
        public int ValidSkills;
        public int TotalAchievements;
        public int ValidAchievements;
        public float DataIntegrityScore;
    }
}