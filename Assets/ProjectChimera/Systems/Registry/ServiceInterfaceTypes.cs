using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Registry
{
    // Missing types for ServiceInterfaces.cs - to fix CS0234/CS0246 errors
    // These replace deleted gaming system types

    /// <summary>
    /// Competition types for service interfaces
    /// </summary>
    public enum CompetitionType
    {
        Cannabis_Cup,
        Quality_Contest,
        Yield_Challenge,
        Speed_Growing,
        Innovation_Contest
    }

    /// <summary>
    /// Competition data structure
    /// </summary>
    [System.Serializable]
    public class Competition
    {
        public string CompetitionId;
        public string CompetitionName;
        public CompetitionType Type;
        public CompetitionStatus Status;
        public DateTime StartDate;
        public DateTime EndDate;
        public List<string> Participants;
        public CompetitionRules Rules;
    }

    /// <summary>
    /// Competition rules
    /// </summary>
    [System.Serializable]
    public class CompetitionRules
    {
        public Dictionary<string, object> Rules;
        public CompetitionFormat Format;
        public int MaxParticipants;
        public string Description;
    }

    /// <summary>
    /// Competition format
    /// </summary>
    public enum CompetitionFormat
    {
        Single_Elimination,
        Round_Robin,
        Swiss_System,
        Ladder,
        Tournament
    }

    /// <summary>
    /// Competition status
    /// </summary>
    public enum CompetitionStatus
    {
        Planned,
        Registration_Open,
        In_Progress,
        Judging,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Achievement data structure
    /// </summary>
    [System.Serializable]
    public class Achievement
    {
        public string AchievementId;
        public string Name;
        public string Description;
        public bool IsUnlocked;
        public DateTime UnlockDate;
        public float Progress;
        public Dictionary<string, object> Criteria;
    }

    /// <summary>
    /// Judging criteria for competitions
    /// </summary>
    [System.Serializable]
    public class JudgingCriteria
    {
        public string CriteriaId;
        public string Name;
        public float Weight;
        public float MinScore;
        public float MaxScore;
        public string Description;
    }

    /// <summary>
    /// Score breakdown for judging
    /// </summary>
    [System.Serializable]
    public class ScoreBreakdown
    {
        public Dictionary<string, float> CategoryScores;
        public float TotalScore;
        public float WeightedScore;
        public List<string> Comments;
    }

    /// <summary>
    /// Judge information
    /// </summary>
    [System.Serializable]
    public class Judge
    {
        public string JudgeId;
        public string Name;
        public float ExperienceLevel;
        public List<string> Specialties;
        public float ReliabilityScore;
    }

    /// <summary>
    /// Judge qualification level
    /// </summary>
    public enum JudgeQualificationLevel
    {
        Novice,
        Intermediate,
        Expert,
        Master,
        Legendary
    }

    /// <summary>
    /// Competition results
    /// </summary>
    [System.Serializable]
    public class CompetitionResults
    {
        public string CompetitionId;
        public List<CompetitionEntry> RankedEntries;
        public DateTime JudgingCompleted;
        public string Winner;
        public Dictionary<string, object> ResultsData;
    }

    /// <summary>
    /// Competition entry
    /// </summary>
    [System.Serializable]
    public class CompetitionEntry
    {
        public string EntryId;
        public string ParticipantId;
        public string ProductId;
        public ScoreBreakdown Scores;
        public int Rank;
        public List<string> Awards;
    }

    /// <summary>
    /// Plant ranking for competitions
    /// </summary>
    [System.Serializable]
    public class PlantRanking
    {
        public string PlantId;
        public int Rank;
        public float Score;
        public string Category;
        public List<string> Achievements;
    }

    /// <summary>
    /// Time transition state for time management
    /// </summary>
    public enum TimeTransitionState
    {
        Idle,
        Transitioning,
        Paused,
        Accelerated,
        Error
    }

    /// <summary>
    /// Plant submission for competitions
    /// </summary>
    [System.Serializable]
    public class PlantSubmission
    {
        public string SubmissionId;
        public string PlantId;
        public string CompetitionId;
        public string ParticipantId;
        public DateTime SubmissionDate;
        public Dictionary<string, object> SubmissionData;
        public string Status;
    }

    /// <summary>
    /// Judge scorecard for evaluations
    /// </summary>
    [System.Serializable]
    public class JudgeScorecard
    {
        public string ScorecardId;
        public string JudgeId;
        public string EntryId;
        public Dictionary<string, float> Scores;
        public List<string> Comments;
        public DateTime EvaluationDate;
        public bool IsComplete;
    }

    /// <summary>
    /// Participant registration information
    /// </summary>
    [System.Serializable]
    public class ParticipantRegistration
    {
        public string RegistrationId;
        public string ParticipantId;
        public string CompetitionId;
        public DateTime RegistrationDate;
        public Dictionary<string, object> RegistrationData;
        public string Status;
    }

    /// <summary>
    /// Winner selection criteria
    /// </summary>
    [System.Serializable]
    public class WinnerSelection
    {
        public string SelectionId;
        public string CompetitionId;
        public string WinnerId;
        public List<string> Runners;
        public DateTime SelectionDate;
        public Dictionary<string, object> SelectionCriteria;
    }

    /// <summary>
    /// Plant entry for competitions
    /// </summary>
    [System.Serializable]
    public class PlantEntry
    {
        public string EntryId;
        public string PlantId;
        public string CompetitionId;
        public string ParticipantId;
        public Dictionary<string, object> EntryData;
        public EntryStatus Status;
        public DateTime SubmissionDate;
    }

    /// <summary>
    /// Entry status for competition entries
    /// </summary>
    public enum EntryStatus
    {
        Submitted,
        Under_Review,
        Accepted,
        Rejected,
        Disqualified
    }

    /// <summary>
    /// Competition requirements
    /// </summary>
    [System.Serializable]
    public class CompetitionRequirements
    {
        public List<string> EligibilityCriteria;
        public Dictionary<string, object> MinimumRequirements;
        public List<string> ProhibitedItems;
        public DateTime RegistrationDeadline;
        public DateTime SubmissionDeadline;
    }

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
    /// Placement position in competitions
    /// </summary>
    [System.Serializable]
    public class PlacementPosition
    {
        public int Position;
        public string Category;
        public float Score;
        public List<string> Awards;
        public Dictionary<string, object> Details;
    }

    /// <summary>
    /// Prize information for competitions
    /// </summary>
    [System.Serializable]
    public class Prize
    {
        public string PrizeId;
        public string Name;
        public string Description;
        public float MonetaryValue;
        public Dictionary<string, object> PrizeData;
        public string Category;
    }

    /// <summary>
    /// Winner profile information
    /// </summary>
    [System.Serializable]
    public class WinnerProfile
    {
        public string WinnerId;
        public string Name;
        public string CompetitionId;
        public DateTime WinDate;
        public List<string> Achievements;
        public Dictionary<string, object> ProfileData;
        public float Score;
    }

    /// <summary>
    /// Reward history tracking
    /// </summary>
    [System.Serializable]
    public class RewardHistory
    {
        public string HistoryId;
        public string ParticipantId;
        public List<RewardEntry> Rewards;
        public DateTime LastUpdated;
        public float TotalValue;
    }

    /// <summary>
    /// Individual reward entry
    /// </summary>
    [System.Serializable]
    public class RewardEntry
    {
        public string RewardId;
        public string Name;
        public float Value;
        public DateTime ReceivedDate;
        public string CompetitionId;
        public string Category;
    }

    /// <summary>
    /// Reward statistics
    /// </summary>
    [System.Serializable]
    public class RewardStatistics
    {
        public string ParticipantId;
        public int TotalRewards;
        public float TotalValue;
        public Dictionary<string, int> RewardCategories;
        public DateTime FirstReward;
        public DateTime LastReward;
        public float AverageRewardValue;
    }
}