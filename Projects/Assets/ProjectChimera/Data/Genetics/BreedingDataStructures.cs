using UnityEngine;
using System.Collections.Generic;

// Explicit aliases for MutationRecord reference
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Breeding data structures for genetic breeding systems.
    /// </summary>
    
    [System.Serializable]
    public class BreedingGoal
    {
        public string GoalID;
        public string GoalName;
        public string Description;
        public BreedingGoalType GoalType;
        public List<BreedingObjective> Objectives = new List<BreedingObjective>();
        public int Priority = 1;
        public float SuccessThreshold = 0.8f;
        public bool IsCompleted = false;
        
        // Missing properties for Systems layer
        public string GoalId { get; set; } // Settable for compatibility
        public List<TargetTrait> TargetTraits = new List<TargetTrait>();
        public System.DateTime Deadline;
        public System.DateTime CreatedAt = System.DateTime.Now;
        public System.DateTime? CompletedAt { get; set; }
        public BreedingGoalStatus Status = BreedingGoalStatus.Active;
    }
    
    [System.Serializable]
    public class BreedingStrategy
    {
        public string StrategyID;
        public string StrategyName;
        public BreedingStrategyType StrategyType;
        public List<BreedingStep> Steps = new List<BreedingStep>();
        public BreedingGoal TargetGoal;
        public float EstimatedGenerations = 3f;
        public float SuccessProbability = 0.7f;
        
        // Missing properties for Systems layer
        public string StrategyId { get; set; } // Settable for compatibility
        public string GoalId { get; set; } = "";
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;
        public List<RecommendedCross> RecommendedCrosses { get; set; } = new List<RecommendedCross>();
        public int GenerationsPlanned { get; set; } = 3;
        public float MutationRate { get; set; } = 0.02f;
        public float SelectionPressure { get; set; } = 0.8f;
        public System.DateTime LastOptimized { get; set; } = System.DateTime.Now;
        public int OptimizationCount { get; set; } = 0;
    }
    
    [System.Serializable]
    public class BreedingObjective
    {
        public TraitType TargetTrait;
        public float TargetValue;
        public float MinAcceptableValue;
        public float Weight = 1f;
        public bool IsRequired = true;
    }
    
    [System.Serializable]
    public class BreedingStep
    {
        public int StepNumber;
        public string StepName;
        public BreedingAction Action;
        public PlantStrainSO ParentA;
        public PlantStrainSO ParentB;
        public string ExpectedOutcome;
        public float EstimatedSuccess = 0.8f;
    }
    
    public enum BreedingGoalType
    {
        YieldMaximization,
        PotencyOptimization,
        DiseaseResistance,
        FlavorEnhancement,
        NovelTrait,
        StabilityImprovement,
        CustomObjective
    }
    
    public enum BreedingStrategyType
    {
        DirectCross,
        Backcross,
        LineBreeding,
        Outcrossing,
        HybridVigor,
        MarkerAssisted,
        RandomMating,
        SelectiveBreeding,
        SingleTrait,
        MultiTraitBalanced
    }
    
    public enum BreedingAction
    {
        Cross,
        Selfing,
        Selection,
        Backcross,
        TestCross,
        Evaluation,
        Stabilization,
        Documentation
    }
    
    [System.Serializable]
    public class BreedingGoalConfiguration
    {
        public string ConfigurationID;
        public string ConfigurationName;
        public List<BreedingGoal> DefaultGoals = new List<BreedingGoal>();
        public BreedingDifficulty DifficultyLevel = BreedingDifficulty.Intermediate;
        public float TimeConstraint = 0f; // Days
        public List<BreedingRestriction> Restrictions = new List<BreedingRestriction>();
        
        // Missing properties for Systems layer
        public string GoalName => ConfigurationName; // Alias for compatibility
        public string Description = "";
        public List<TargetTrait> TargetTraits = new List<TargetTrait>();
        public int Priority = 1;
        public System.DateTime Deadline;
    }
    
    [System.Serializable]
    public class BreedingEvaluation
    {
        public string EvaluationID;
        public GenotypeDataSO EvaluatedGenotype;
        public List<TraitEvaluation> TraitEvaluations = new List<TraitEvaluation>();
        public float OverallScore;
        public BreedingGoal EvaluatedAgainst;
        public System.DateTime EvaluationDate;
        
        // Missing properties for Systems layer
        public PlantStrainSO Parent1;
        public PlantStrainSO Parent2;
        public float EvaluatedAt;
        public Dictionary<string, float> GoalScores = new Dictionary<string, float>();
        public List<string> StrategyRecommendations = new List<string>();
    }
    
    [System.Serializable]
    public class BreedingAttempt
    {
        public string AttemptID;
        public PlantStrainSO ParentA;
        public PlantStrainSO ParentB;
        public GenotypeDataSO ResultingGenotype;
        public BreedingStrategy UsedStrategy;
        public bool IsSuccessful;
        public float SuccessScore;
        public System.DateTime AttemptDate;
        public string Notes;
        
        // Missing properties for Systems layer
        public bool Success => IsSuccessful;
        public System.DateTime AttemptTime => AttemptDate;
        public Dictionary<TraitType, float> OffspringTraits = new Dictionary<TraitType, float>();
    }
    
    [System.Serializable]
    public class BreedingGoalSuggestion
    {
        public BreedingGoal SuggestedGoal;
        public float Difficulty;
        public float EstimatedTime;
        public string Reasoning;
        public List<BreedingStrategy> RecommendedStrategies = new List<BreedingStrategy>();
        
        // Missing properties for Systems layer
        public string SuggestionId { get; set; } = System.Guid.NewGuid().ToString();
        public string GoalName { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; } = 1;
        public float EstimatedGenerations { get; set; } = 3f;
        public float SuccessProbability { get; set; } = 0.7f;
    }
    
    [System.Serializable]
    public class TraitEvaluation
    {
        public TraitType EvaluatedTrait;
        public float CurrentValue;
        public float TargetValue;
        public float Score; // 0-1
        public bool MeetsTarget;
    }
    
    [System.Serializable]
    public class BreedingRestriction
    {
        public string RestrictionType;
        public string Description;
        public bool IsActive = true;
    }
    
    [System.Serializable]
    public class TargetTrait
    {
        public TraitType Trait;
        public float TargetValue;
        public float MinValue;
        public float MaxValue;
        public float Weight = 1f;
        public bool IsRequired = true;
    }
    
    [System.Serializable]
    public class RecommendedCross
    {
        public PlantStrainSO ParentA;
        public PlantStrainSO ParentB;
        public float PredictedSuccess;
        public List<TraitType> TargetTraits = new List<TraitType>();
        public string Reasoning;
        
        // Missing properties for Systems layer
        public string CrossName { get; set; } = "";
        public float EstimatedSuccessRate { get; set; } = 0.7f;
        public int GenerationsRequired { get; set; } = 2;
        public string Notes { get; set; } = "";
    }
    
    [System.Serializable]
    public class TraitGap
    {
        public TraitType Trait;
        public float CurrentValue;
        public float TargetValue;
        public float Gap;
        public float Priority;
        public bool IsCritical;
        
        // Missing properties for Systems layer
        public string TraitName { get; set; } = "";
        public float CurrentRange { get; set; } = 0f;
        public float OptimalRange { get; set; } = 1f;
    }
    
    [System.Serializable]
    public class BreedingPriority
    {
        public TraitType Trait;
        public float Priority; // 0-1
        public string Reason;
        public bool IsUrgent;
        public float TimeConstraint;
        
        // Missing property for Systems layer
        public BreedingPriorityLevel PriorityLevel { get; set; } = BreedingPriorityLevel.Medium;
    }
    
    [System.Serializable]
    public class BreedingProgress
    {
        public BreedingGoal Goal;
        public float CompletionPercentage;
        public List<TraitGap> RemainingGaps = new List<TraitGap>();
        public int GenerationsCompleted;
        public int EstimatedGenerationsRemaining;
        public System.DateTime LastUpdate;
        
        // Missing properties for Systems layer
        public int TotalAttempts { get; set; } = 0;
        public int SuccessfulAttempts { get; set; } = 0;
        public float SuccessRate { get; set; } = 0f;
        public Dictionary<TraitType, float> TraitProgress { get; set; } = new Dictionary<TraitType, float>();
        public float OverallProgress { get; set; } = 0f;
        public System.DateTime LastUpdated { get; set; } = System.DateTime.Now;
    }
    
    [System.Serializable]
    public class BreedingGoalTemplate
    {
        public string TemplateID;
        public string TemplateName;
        public List<BreedingObjective> DefaultObjectives = new List<BreedingObjective>();
        public BreedingDifficulty DifficultyLevel;
        public float EstimatedTime;
        public string Description;
        
        // Missing properties for Systems layer
        public string TemplateId { get; set; } // Settable property
        public List<TargetTrait> DefaultTargetTraits { get; set; } = new List<TargetTrait>();
        
        // Missing method for Systems layer  
        public BreedingGoalConfiguration CreateConfiguration(int maxGoals)
        {
            return new BreedingGoalConfiguration
            {
                ConfigurationID = TemplateId ?? TemplateID,
                ConfigurationName = TemplateName,
                Description = Description,
                DifficultyLevel = DifficultyLevel,
                TimeConstraint = EstimatedTime
            };
        }
    }
    
    public enum BreedingDifficulty
    {
        Beginner,
        Novice,
        Intermediate,
        Advanced,
        Expert,
        Master
    }
    
    public enum BreedingGoalStatus
    {
        Active,
        Completed,
        Failed,
        Paused,
        Archived
    }
    
    public enum BreedingPriorityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    
    // Additional missing data structures for Systems layer - removed duplicates
    
    [System.Serializable]
    public class BreedingCompatibility
    {
        public string Parent1ID = "";
        public string Parent2ID = "";
        public float CompatibilityScore = 0f;
        public float GeneticDistance = 0f;
        public float InbreedingRisk = 0f;
        public float ExpectedHeterosis = 0f;
        
        // Additional properties for GeneticAnalysisEngine
        public float ComplementarityScore = 0f;
        public float PredictedHeterosis = 0f;
    }
    
    [System.Serializable]
    public class BreedingResult
    {
        public PlantGenotype Parent1Genotype;
        public PlantGenotype Parent2Genotype;
        public string ParentId;
        public string Parent2Id;
        public List<PlantGenotype> OffspringGenotypes = new List<PlantGenotype>();
        public List<MutationRecord> MutationsOccurred = new List<MutationRecord>();
        public float BreedingSuccess = 1.0f;
        public System.DateTime BreedingDate = System.DateTime.Now;
        public string BreedingNotes = "";
        
        // PC014-FIX-24: Added missing properties for GeneticsManager compatibility
        public string Parent1Id
        {
            get => ParentId;
            set => ParentId = value;
        }
        
        // Note: Parent2Id already exists as a field, but need property for object initializer syntax
        
        // Alias property for CannabisGeneticsOrchestrator compatibility
        public List<CannabisGenotype> Offspring
        {
            get
            {
                var list = new List<CannabisGenotype>();
                foreach (var genotype in OffspringGenotypes)
                {
                    if (genotype is CannabisGenotype cannabis)
                        list.Add(cannabis);
                }
                return list;
            }
        }
    }
    
    [System.Serializable]
    public class CurrentRange
    {
        public float MinValue = 0f;
        public float MaxValue = 1f;
        public float Range => MaxValue - MinValue;
    }
    
    [System.Serializable]
    public class OptimalRange
    {
        public float MinValue = 0f;
        public float MaxValue = 1f;
        public float Range => MaxValue - MinValue;
    }
    
}