using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// BASIC: Simple progression data for Project Chimera's save system.
    /// Focuses on essential progression information without complex systems.
    /// </summary>

    /// <summary>
    /// Basic progression data
    /// </summary>
    [System.Serializable]
    public class ProgressionData
    {
        public string PlayerName = "Player";
        public int PlayerLevel = 1;
        public float Experience = 0f;
        public float ExperienceToNextLevel = 100f;
        public int SkillPoints = 0;
        public List<string> CompletedAchievements = new List<string>();
        public List<string> UnlockedFeatures = new List<string>();
        public System.DateTime FirstPlayTime;
        public System.DateTime LastPlayTime;
        public float TotalPlayTime = 0f;
        public int SessionCount = 0;
    }

    /// <summary>
    /// Basic skill data
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        public string SkillName;
        public string SkillDescription;
        public int SkillLevel = 1;
        public float SkillProgress = 0f; // 0-1
        public bool IsUnlocked = false;
    }

    /// <summary>
    /// Basic achievement data
    /// </summary>
    [System.Serializable]
    public class AchievementData
    {
        public string AchievementId;
        public string AchievementName;
        public string Description;
        public bool IsCompleted = false;
        public System.DateTime CompletionDate;
        public int Progress = 0;
        public int TargetProgress = 1;
    }

    /// <summary>
    /// Progression utilities
    /// </summary>
    public static class ProgressionUtilities
    {
        /// <summary>
        /// Add experience to player
        /// </summary>
        public static void AddExperience(ProgressionData data, float amount)
        {
            data.Experience += amount;

            // Check for level up
            while (data.Experience >= data.ExperienceToNextLevel)
            {
                data.Experience -= data.ExperienceToNextLevel;
                data.PlayerLevel++;
                data.SkillPoints += 1; // Grant skill point per level
                data.ExperienceToNextLevel = CalculateExperienceForLevel(data.PlayerLevel);
            }
        }

        /// <summary>
        /// Calculate experience needed for level
        /// </summary>
        public static float CalculateExperienceForLevel(int level)
        {
            // Simple exponential growth
            return 100f * Mathf.Pow(1.2f, level - 1);
        }

        /// <summary>
        /// Check if achievement is completed
        /// </summary>
        public static bool IsAchievementCompleted(ProgressionData data, string achievementId)
        {
            return data.CompletedAchievements.Contains(achievementId);
        }

        /// <summary>
        /// Complete an achievement
        /// </summary>
        public static void CompleteAchievement(ProgressionData data, string achievementId)
        {
            if (!data.CompletedAchievements.Contains(achievementId))
            {
                data.CompletedAchievements.Add(achievementId);
            }
        }

        /// <summary>
        /// Check if feature is unlocked
        /// </summary>
        public static bool IsFeatureUnlocked(ProgressionData data, string featureId)
        {
            return data.UnlockedFeatures.Contains(featureId);
        }

        /// <summary>
        /// Unlock a feature
        /// </summary>
        public static void UnlockFeature(ProgressionData data, string featureId)
        {
            if (!data.UnlockedFeatures.Contains(featureId))
            {
                data.UnlockedFeatures.Add(featureId);
            }
        }

        /// <summary>
        /// Get level progress (0-1)
        /// </summary>
        public static float GetLevelProgress(ProgressionData data)
        {
            return data.Experience / data.ExperienceToNextLevel;
        }

        /// <summary>
        /// Get overall completion percentage
        /// </summary>
        public static float GetCompletionPercentage(ProgressionData data, int totalAchievements)
        {
            if (totalAchievements == 0) return 0f;
            return (float)data.CompletedAchievements.Count / totalAchievements;
        }

        /// <summary>
        /// Create new progression data
        /// </summary>
        public static ProgressionData CreateNewProgression(string playerName)
        {
            return new ProgressionData
            {
                PlayerName = playerName,
                PlayerLevel = 1,
                Experience = 0f,
                ExperienceToNextLevel = 100f,
                SkillPoints = 0,
                FirstPlayTime = System.DateTime.Now,
                LastPlayTime = System.DateTime.Now,
                SessionCount = 1
            };
        }

        /// <summary>
        /// Update play session
        /// </summary>
        public static void UpdatePlaySession(ProgressionData data, float sessionLength)
        {
            data.LastPlayTime = System.DateTime.Now;
            data.TotalPlayTime += sessionLength;
            data.SessionCount++;
        }

        /// <summary>
        /// Get progression statistics
        /// </summary>
        public static ProgressionStats GetProgressionStats(ProgressionData data, int totalAchievements, int totalFeatures)
        {
            return new ProgressionStats
            {
                CurrentLevel = data.PlayerLevel,
                CurrentXP = data.Experience,
                XPToNextLevel = data.ExperienceToNextLevel,
                LevelProgress = GetLevelProgress(data),
                AvailableSkillPoints = data.SkillPoints,
                CompletedAchievements = data.CompletedAchievements.Count,
                TotalAchievements = totalAchievements,
                AchievementCompletion = GetCompletionPercentage(data, totalAchievements),
                UnlockedFeatures = data.UnlockedFeatures.Count,
                TotalFeatures = totalFeatures,
                TotalPlayTime = data.TotalPlayTime,
                SessionCount = data.SessionCount,
                AverageSessionLength = data.TotalPlayTime / Mathf.Max(1, data.SessionCount)
            };
        }
    }

    /// <summary>
    /// Progression statistics
    /// </summary>
    [System.Serializable]
    public struct ProgressionStats
    {
        public int CurrentLevel;
        public float CurrentXP;
        public float XPToNextLevel;
        public float LevelProgress;
        public int AvailableSkillPoints;
        public int CompletedAchievements;
        public int TotalAchievements;
        public float AchievementCompletion;
        public int UnlockedFeatures;
        public int TotalFeatures;
        public float TotalPlayTime;
        public int SessionCount;
        public float AverageSessionLength;
    }
}
