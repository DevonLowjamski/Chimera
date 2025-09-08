using System;
using System.Collections.Generic;
using UnityEngine;
// Removed heavy cross-module dependencies for UI data

namespace ProjectChimera.Data.UI
{
    // Competitive/Leaderboard types are now defined in SharedDataStructures.cs

    [System.Serializable]
    public class LeaderboardDisplayData
    {
        public int Rank;
        public string PlayerName;
        public float Score;
        public string Achievement;
        public DateTime LastUpdate;

        // Additional properties for UI compatibility
        public bool IsCurrentPlayer = false;
        public string Badge = "";
        public DateTime LastActive => LastUpdate; // Alias for compatibility
        public string FormattedScore => Score.ToString("F0"); // Formatted score display
        public string TrophyIcon => GetTrophyIconForRank(Rank);
        public Color RankColor => GetRankColor(Rank);

        private string GetTrophyIconForRank(int rank)
        {
            return rank switch
            {
                1 => "🥇", // Gold
                2 => "🥈", // Silver
                3 => "🥉", // Bronze
                _ when rank <= 10 => "🏆", // Top 10
                _ => ""
            };
        }

        private Color GetRankColor(int rank)
        {
            return rank switch
            {
                1 => new Color(1f, 0.84f, 0f, 1f), // Gold
                2 => new Color(0.75f, 0.75f, 0.75f, 1f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f, 1f), // Bronze
                _ when rank <= 10 => new Color(0.2f, 0.8f, 0.2f, 1f), // Green for top 10
                _ => new Color(0.7f, 0.7f, 0.7f, 1f) // Gray for others
            };
        }
    }

    [System.Serializable]
    public class CompetitionEvent
    {
        public string Id;
        public string Name;
        public string Description;
        public DateTime StartTime;
        public DateTime EndTime;
        public CompetitionStatus Status;
        public List<string> Participants;
        public Dictionary<string, float> Rewards;

        // Compatibility property for UI
        public bool IsActive => Status == CompetitionStatus.Active;
    }

    public enum CompetitionStatus
    {
        Upcoming,
        Active,
        Completed,
        Cancelled
    }

    [System.Serializable]
    public class CompetitiveStatsSummary
    {
        // Rankings
        public int OverallRank;
        public int CultivationRank;
        public int EconomicRank;
        public int QualityRank;

        // Legacy properties for compatibility
        public int CurrentRank;
        public float TotalScore;
        public int CompetitionsWon;
        public int CompetitionsParticipated;
        public float WinRate;
        public List<PersonalRecord> Records;

        // New properties to match CompetitiveManager version
        public int TotalCompetitionsEntered;
        public int TotalWins;
        public int TotalPodiumFinishes;
        public PersonalRecords PersonalRecords;

        // Level progression
        public int CompetitiveLevel;
        public float NextLevelProgress;
    }

    [System.Serializable]
    public class PersonalRecords
    {
        public float HighestSingleHarvest = 0f;
        public float HighestQualityRating = 0f;
        public float FastestGrowthCycle = 0f;
        public float LargestSingleProfit = 0f;
        public int MostStrainsMastered = 0;
        public int LongestWinStreak = 0;
        public DateTime FirstRecordSet;
        public DateTime LastRecordSet;
    }

    [System.Serializable]
    public class PersonalRecord
    {
        public RecordType Type;
        public float Value;
        public DateTime AchievedDate;
        public string Description;
    }

    public enum RecordType
    {
        HighestYield,
        BestQuality,
        FastestGrowth,
        MostEfficient,
        LongestStreak,
        HighestProfit,
        // Additional record types for competitive panel
        HighestQuality,
        ProfitGenerated,
        StrainMastery
    }

    // UI Status for notifications - using UIStatus from ProjectChimera.UI.Components

    // Progress bar component - using UIProgressBar from ProjectChimera.UI.Components

    // Random Events types
    [System.Serializable]
    public class RandomEventData
    {
        public string Id;
        public string Title;
        public string Description;
        public RandomEventType Type;
        public RandomEventSeverity Severity;
        public DateTime OccurredAt;
        public bool IsResolved;
        public List<EventChoice> Choices;
    }

    // Additional event types for RandomEventsPanel
    [System.Serializable]
    public class EventDisplayData
    {
        public string EventId;
        public string Title;
        public string Description;
        public EventSeverity Severity;
        public RandomEventType Type;
        public bool HasTimeLimit;
        public TimeSpan TimeRemaining;
        public List<EventChoice> Choices;
        public DateTime StartTime;
        public bool IsActive;

        // Additional UI properties
        public string StoryContext;
        public string CategoryIcon => GetCategoryIcon(Type);
        public Color SeverityColor => GetSeverityColor(Severity);

        private string GetCategoryIcon(RandomEventType type)
        {
            return type switch
            {
                RandomEventType.Environmental => "🌱",
                RandomEventType.Economic => "💰",
                RandomEventType.Technical => "⚙️",
                RandomEventType.Social => "👥",
                RandomEventType.Regulatory => "📋",
                RandomEventType.Natural => "🌪️",
                _ => "❓"
            };
        }

        private Color GetSeverityColor(EventSeverity severity)
        {
            return severity switch
            {
                EventSeverity.Positive => new Color(0.2f, 0.8f, 0.2f, 1f), // Green
                EventSeverity.Low => new Color(0.8f, 0.8f, 0.2f, 1f), // Yellow
                EventSeverity.Medium => new Color(1f, 0.6f, 0.2f, 1f), // Orange
                EventSeverity.High => new Color(1f, 0.4f, 0.2f, 1f), // Red-Orange
                EventSeverity.Critical => new Color(1f, 0.2f, 0.2f, 1f), // Red
                _ => new Color(0.5f, 0.5f, 0.5f, 1f) // Gray
            };
        }
    }

    [System.Serializable]
    public class ActiveRandomEvent
    {
        public string Id;
        public string Title;
        public string Description;
        public EventSeverity Severity;
        public RandomEventType Type;
        public DateTime StartTime;
        public DateTime? EndTime;
        public bool IsResolved;
        public List<EventChoice> Choices;
        public Dictionary<string, object> Context;
    }

    [System.Serializable]
    public class EventChoice
    {
        public string Id;
        public string Text;
        public string Description;
        public Dictionary<string, float> Consequences;
        public float Cost;
        public bool IsAvailable;
        public List<string> Requirements;
    }

    public enum EventSeverity
    {
        Positive,
        Low,
        Medium,
        High,
        Critical
    }

    public enum RandomEventType
    {
        Environmental,
        Economic,
        Technical,
        Social,
        Regulatory,
        Natural
    }

    public enum RandomEventSeverity
    {
        Minor,
        Moderate,
        Major,
        Critical
    }

    // Save/Load types
    [System.Serializable]
    public class SaveSlotData
    {
        public string SlotId;
        public string SaveName;
        public DateTime SaveTime;
        public string PreviewImage;
        public Dictionary<string, object> Metadata;
        public long FileSize;
        public bool IsValid;
    }

    // Plant breeding types
    [System.Serializable]
    public class PlantBreedingData
    {
        public string PlantId;
        public string StrainName;
        public List<string> ParentStrains;
        public Dictionary<string, float> Traits;
        public int Generation;
        public float BreedingProgress;
        public bool IsStable;
    }

    // Facility management types
    [System.Serializable]
    public class FacilityData
    {
        public string FacilityId;
        public string Name;
        public FacilityType Type;
        public Vector3 Position;
        public Vector3 Size;
        public FacilityStatus Status;
        public Dictionary<string, float> Resources;
        public List<string> ConnectedSystems;
    }

    public enum FacilityType
    {
        GrowRoom,
        ProcessingRoom,
        StorageRoom,
        Laboratory,
        Office,
        Utility
    }

    public enum FacilityStatus
    {
        Operational,
        Maintenance,
        Offline,
        Construction,
        Damaged
    }

    // Plant cultivation types
    // NOTE: PlantInstance moved to Systems/Cultivation/PlantInstance.cs to avoid CS0101 duplicate definition

    // Breeding result types
    [System.Serializable]
        public class BreedingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int OffspringCount { get; set; }
        public float BreedingTime { get; set; }
            public List<object> OffspringGenotypes { get; set; } = new List<object>();
        public List<TraitPrediction> TraitPredictions { get; set; } = new List<TraitPrediction>();
        public float HybridVigorFactor { get; set; }
        public DateTime CompletionTime { get; set; }

        // Compatibility properties for different systems
            public object Parent1Genotype { get; set; }
            public object Parent2Genotype { get; set; }
        public BreedingMethod BreedingMethod { get; set; }
    }

    [System.Serializable]
    public class TraitPrediction
    {
        public string TraitName;
        public float PredictedValue;
        public float Confidence;
        public string Description;
    }

    public enum BreedingMethod
    {
        Traditional,
        Selective,
        Backcross,
        SelfPollination,
        HybridCross
    }

    // Objectives and challenges types
    [System.Serializable]
    public class ObjectiveProgressData
    {
        public string ObjectiveId;
        public string Title;
        public string Description;
        public float Progress;
        public float Target;
        public ObjectiveDifficulty Difficulty;
        public DateTime StartTime;
        public DateTime? CompletionTime;
        public Dictionary<string, object> Rewards;

        // Compatibility properties for UI
        public float CurrentProgress => Progress;
        public float TargetProgress => Target;
        public float ProgressPercentage => Target > 0 ? Progress / Target : 0f;
        public TimeSpan TimeRemaining => CompletionTime.HasValue ? TimeSpan.Zero : TimeSpan.FromDays(7); // Default 7 days if no completion time
        public string RewardPreview => Rewards != null && Rewards.Count > 0 ? "Rewards Available" : "No Rewards";
    }

    [System.Serializable]
    public class ChallengeProgressData
    {
        public string ChallengeId;
        public string Title;
        public string Description;
        public float Progress;
        public float Target;
        public ObjectiveDifficulty Difficulty;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool IsCompleted;
        public Dictionary<string, object> Rewards;

        // Compatibility properties for UI
        public float CurrentProgress => Progress;
        public float TargetProgress => Target;
        public float ProgressPercentage => Target > 0 ? Progress / Target : 0f;
        public string RewardPreview => Rewards != null && Rewards.Count > 0 ? "Challenge Rewards" : "No Rewards";
    }

    public enum ObjectiveDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert,
        Legendary
    }

    [System.Serializable]
    public class ActiveObjective
    {
        public string Id;
        public string Title;
        public string Description;
        public ObjectiveDifficulty Difficulty;
        public float Progress;
        public float Target;
        public bool IsCompleted;
        public DateTime StartTime;
        public Dictionary<string, object> Requirements;
        public Dictionary<string, object> Rewards;
    }

    [System.Serializable]
    public class ActiveChallenge
    {
        public string Id;
        public string Title;
        public string Description;
        public ObjectiveDifficulty Difficulty;
        public float Progress;
        public float Target;
        public bool IsCompleted;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool IsTimeExpired;
        public Dictionary<string, object> Requirements;
        public Dictionary<string, object> Rewards;
    }
}
