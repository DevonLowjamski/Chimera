using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Shared;
using static ProjectChimera.Data.Genetics.TraitType;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Configuration settings for cannabis breeding operations.
    /// Controls breeding mechanics, selection criteria, and breeding program parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "CannabisBreedingConfig", menuName = "Project Chimera/Genetics/Cannabis Breeding Config")]
    public class CannabisBreedingConfigSO : ChimeraConfigSO
    {
        [Header("Breeding Methods")]
        [SerializeField] private bool _enableStandardCrossing = true;
        [SerializeField] private bool _enableBackcrossing = true;
        [SerializeField] private bool _enableSelfPollination = true;
        [SerializeField] private bool _enableLineBreeding = true;
        [SerializeField] private bool _enableOutcrossing = true;
        [SerializeField] private bool _enableHybridCrossing = true;

        [Header("Crossing Parameters")]
        [SerializeField, Range(0f, 1f)] private float _crossingSuccessRate = 0.85f;
        [SerializeField, Range(1, 20)] private int _averageOffspringCount = 8;
        [SerializeField, Range(0f, 0.5f)] private float _offspringVariability = 0.2f;
        [SerializeField] private int _maxCrossesPerGeneration = 50;
        [SerializeField, Range(7, 21)] private int _crossingCooldownDays = 14;

        [Header("Selection Criteria")]
        [SerializeField] private bool _enableFitnessSelection = true;
        [SerializeField] private bool _enableTraitSelection = true;
        [SerializeField] private bool _enableYieldSelection = true;
        [SerializeField] private bool _enableQualitySelection = true;
        [SerializeField] private bool _enableResistanceSelection = true;
        [SerializeField, Range(0f, 1f)] private float _selectionPressure = 0.7f;

        [Header("Breeding Goals")]
        [SerializeField] private BreedingObjective _primaryObjective = new BreedingObjective { TargetTrait = TraitType.Yield, TargetValue = 100f, Weight = 1f };
        [SerializeField] private BreedingObjective _secondaryObjective = new BreedingObjective { TargetTrait = TraitType.DiseaseResistance, TargetValue = 80f, Weight = 0.8f };
        [SerializeField, Range(0.1f, 3f)] private float _objectiveWeight = 1.5f;
        [SerializeField] private bool _maintainGeneticDiversity = true;

        [Header("Generation Management")]
        [SerializeField, Range(1, 50)] private int _maxBreedingGenerations = 20;
        [SerializeField, Range(0f, 1f)] private float _generationAdvancementThreshold = 0.6f;
        [SerializeField] private bool _enableGenerationTracking = true;
        [SerializeField] private bool _preserveFounderLines = true;

        [Header("Inbreeding Control")]
        [SerializeField, Range(0f, 1f)] private float _maxInbreedingCoefficient = 0.6f;
        [SerializeField] private bool _preventSiblingMating = true;
        [SerializeField] private bool _preventParentOffspringMating = true;
        [SerializeField, Range(1, 10)] private int _minimumOutcrossingGenerations = 3;

        [Header("Hybrid Vigor")]
        [SerializeField] private bool _enableHeterosisTracking = true;
        [SerializeField, Range(1f, 2f)] private float _hybridVigorMultiplier = 1.25f;
        [SerializeField, Range(0f, 1f)] private float _heterosisDecayRate = 0.1f;
        [SerializeField] private bool _maintainHybridVigor = true;

        [Header("Trait Heritability")]
        [SerializeField, Range(0f, 1f)] private float _yieldHeritability = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _qualityHeritability = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _resistanceHeritability = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _growthHeritability = 0.75f;

        [Header("Environmental Factors")]
        [SerializeField] private bool _considerEnvironmentalAdaptation = true;
        [SerializeField, Range(0f, 1f)] private float _environmentalInfluence = 0.3f;
        [SerializeField] private bool _enableGxESelection = true;
        [SerializeField] private int _testEnvironments = 3;

        [Header("Quality Control")]
        [SerializeField] private bool _enableGeneticValidation = true;
        [SerializeField] private bool _enablePerformanceTesting = true;
        [SerializeField] private bool _enableStabilityTesting = true;
        [SerializeField, Range(0f, 1f)] private float _minimumPerformanceThreshold = 0.5f;

        [Header("Advanced Breeding")]
        [SerializeField] private bool _enableMarkerAssistedSelection = false;
        [SerializeField] private bool _enableGenomicSelection = false;
        [SerializeField] private bool _enableDoubleHaploids = false;
        [SerializeField] private bool _enableCytogeneticAnalysis = false;

        [Header("Time and Scheduling")]
        [SerializeField, Range(30, 365)] private int _breedingSeasonLength = 120; // days
        [SerializeField, Range(1, 12)] private int _breedingCyclesPerYear = 2;
        [SerializeField] private bool _enableContinuousBreeding = false;
        [SerializeField, Range(1, 30)] private int _evaluationPeriodDays = 14;

        // Public Properties
        public bool EnableStandardCrossing => _enableStandardCrossing;
        public bool EnableBackcrossing => _enableBackcrossing;
        public bool EnableSelfPollination => _enableSelfPollination;
        public bool EnableLineBreeding => _enableLineBreeding;
        public bool EnableOutcrossing => _enableOutcrossing;
        public bool EnableHybridCrossing => _enableHybridCrossing;

        public float CrossingSuccessRate => _crossingSuccessRate;
        public int AverageOffspringCount => _averageOffspringCount;
        public float OffspringVariability => _offspringVariability;
        public int MaxCrossesPerGeneration => _maxCrossesPerGeneration;
        public int CrossingCooldownDays => _crossingCooldownDays;

        public bool EnableFitnessSelection => _enableFitnessSelection;
        public bool EnableTraitSelection => _enableTraitSelection;
        public bool EnableYieldSelection => _enableYieldSelection;
        public bool EnableQualitySelection => _enableQualitySelection;
        public bool EnableResistanceSelection => _enableResistanceSelection;
        public float SelectionPressure => _selectionPressure;

        public BreedingObjective PrimaryObjective => _primaryObjective;
        public BreedingObjective SecondaryObjective => _secondaryObjective;
        public float ObjectiveWeight => _objectiveWeight;
        public bool MaintainGeneticDiversity => _maintainGeneticDiversity;

        public int MaxBreedingGenerations => _maxBreedingGenerations;
        public float GenerationAdvancementThreshold => _generationAdvancementThreshold;
        public bool EnableGenerationTracking => _enableGenerationTracking;
        public bool PreserveFounderLines => _preserveFounderLines;

        public float MaxInbreedingCoefficient => _maxInbreedingCoefficient;
        public bool PreventSiblingMating => _preventSiblingMating;
        public bool PreventParentOffspringMating => _preventParentOffspringMating;
        public int MinimumOutcrossingGenerations => _minimumOutcrossingGenerations;

        public bool EnableHeterosisTracking => _enableHeterosisTracking;
        public float HybridVigorMultiplier => _hybridVigorMultiplier;
        public float HeterosisDecayRate => _heterosisDecayRate;
        public bool MaintainHybridVigor => _maintainHybridVigor;

        public float YieldHeritability => _yieldHeritability;
        public float QualityHeritability => _qualityHeritability;
        public float ResistanceHeritability => _resistanceHeritability;
        public float GrowthHeritability => _growthHeritability;

        public bool ConsiderEnvironmentalAdaptation => _considerEnvironmentalAdaptation;
        public float EnvironmentalInfluence => _environmentalInfluence;
        public bool EnableGxESelection => _enableGxESelection;
        public int TestEnvironments => _testEnvironments;

        public bool EnableGeneticValidation => _enableGeneticValidation;
        public bool EnablePerformanceTesting => _enablePerformanceTesting;
        public bool EnableStabilityTesting => _enableStabilityTesting;
        public float MinimumPerformanceThreshold => _minimumPerformanceThreshold;

        public bool EnableMarkerAssistedSelection => _enableMarkerAssistedSelection;
        public bool EnableGenomicSelection => _enableGenomicSelection;
        public bool EnableDoubleHaploids => _enableDoubleHaploids;
        public bool EnableCytogeneticAnalysis => _enableCytogeneticAnalysis;

        public int BreedingSeasonLength => _breedingSeasonLength;
        public int BreedingCyclesPerYear => _breedingCyclesPerYear;
        public bool EnableContinuousBreeding => _enableContinuousBreeding;
        public int EvaluationPeriodDays => _evaluationPeriodDays;

        /// <summary>
        /// Calculate breeding compatibility between two genotypes
        /// </summary>
        public float CalculateBreedingCompatibility(CannabisGenotype parent1, CannabisGenotype parent2)
        {
            float compatibility = 1f;

            // Check inbreeding constraints
            if (_preventSiblingMating && AreSiblings(parent1, parent2))
                return 0f;

            if (_preventParentOffspringMating && IsParentOffspring(parent1, parent2))
                return 0f;

            // Calculate genetic distance
            float geneticDistance = CalculateGeneticDistance(parent1, parent2);
            if (geneticDistance < _maxInbreedingCoefficient)
                compatibility *= (geneticDistance / _maxInbreedingCoefficient);

            // Consider breeding objectives
            compatibility *= CalculateObjectiveCompatibility(parent1, parent2);

            return Mathf.Clamp01(compatibility);
        }

        /// <summary>
        /// Get heritability for a specific trait
        /// </summary>
        public float GetTraitHeritability(TraitType trait)
        {
            return trait switch
            {
                TraitType.BiomassProduction => _yieldHeritability,
                TraitType.FlowerYield => _yieldHeritability,
                TraitType.Potency => _qualityHeritability,
                TraitType.AromaIntensity => _qualityHeritability,
                TraitType.DiseaseResistance => _resistanceHeritability,
                TraitType.SpiderMiteResistance => _resistanceHeritability,
                TraitType.VegetativeGrowthRate => _growthHeritability,
                TraitType.PlantHeight => _growthHeritability,
                _ => 0.6f // Default heritability
            };
        }

        /// <summary>
        /// Calculate expected breeding value for offspring
        /// </summary>
        public float CalculateExpectedBreedingValue(CannabisGenotype parent1, CannabisGenotype parent2, TraitType trait)
        {
            float heritability = GetTraitHeritability(trait);
            float parent1Value = GetTraitValue(parent1, trait);
            float parent2Value = GetTraitValue(parent2, trait);
            
            float midParentValue = (parent1Value + parent2Value) / 2f;
            
            // Apply heritability
            float expectedValue = midParentValue * heritability;
            
            // Apply hybrid vigor if applicable
            if (_enableHeterosisTracking && !AreSiblings(parent1, parent2))
            {
                expectedValue *= _hybridVigorMultiplier;
            }
            
            return expectedValue;
        }

        /// <summary>
        /// Determine if breeding method is available for given parents
        /// </summary>
        public bool IsBreedingMethodAvailable(BreedingMethod method, CannabisGenotype parent1, CannabisGenotype parent2 = null)
        {
            return method switch
            {
                BreedingMethod.StandardCross => _enableStandardCrossing && parent2 != null,
                BreedingMethod.Backcross => _enableBackcrossing && parent2 != null,
                BreedingMethod.SelfPollination => _enableSelfPollination && parent2 == null,
                BreedingMethod.LineBreeding => _enableLineBreeding && parent2 != null && AreRelated(parent1, parent2),
                BreedingMethod.OutCross => _enableOutcrossing && parent2 != null && !AreRelated(parent1, parent2),
                BreedingMethod.HybridCross => _enableHybridCrossing && parent2 != null && AreDifferentStrains(parent1, parent2),
                _ => false
            };
        }

        /// <summary>
        /// Calculate selection intensity for current generation
        /// </summary>
        public float CalculateSelectionIntensity(int generationNumber)
        {
            // Reduce selection pressure in later generations to maintain diversity
            float generationFactor = 1f - (generationNumber * 0.02f);
            return _selectionPressure * Mathf.Clamp01(generationFactor);
        }

        /// <summary>
        /// Get recommended breeding method for objectives
        /// </summary>
        public BreedingMethod GetRecommendedBreedingMethod(CannabisGenotype parent1, CannabisGenotype parent2)
        {
            // Prioritize methods based on objectives and genetic relationship
            if (_primaryObjective?.TargetTrait == TraitType.Yield && !AreRelated(parent1, parent2))
                return BreedingMethod.OutCross;
            
            if (_primaryObjective?.TargetTrait == TraitType.StabilityIndex && AreRelated(parent1, parent2))
                return BreedingMethod.LineBreeding;
            
            if (_maintainGeneticDiversity && AreDifferentStrains(parent1, parent2))
                return BreedingMethod.HybridCross;
            
            return BreedingMethod.StandardCross;
        }

        // Helper methods
        private bool AreSiblings(CannabisGenotype genotype1, CannabisGenotype genotype2)
        {
            // Implementation would check lineage data
            return false; // Simplified for now
        }

        private bool IsParentOffspring(CannabisGenotype genotype1, CannabisGenotype genotype2)
        {
            // Implementation would check generational relationship
            return false; // Simplified for now
        }

        private bool AreRelated(CannabisGenotype genotype1, CannabisGenotype genotype2)
        {
            // Implementation would check genetic relationship
            return false; // Simplified for now
        }

        private bool AreDifferentStrains(CannabisGenotype genotype1, CannabisGenotype genotype2)
        {
            return genotype1.ParentStrain != genotype2.ParentStrain;
        }

        private float CalculateGeneticDistance(CannabisGenotype genotype1, CannabisGenotype genotype2)
        {
            // Implementation would calculate actual genetic distance
            return Random.Range(0.3f, 1f); // Placeholder
        }

        private float CalculateObjectiveCompatibility(CannabisGenotype parent1, CannabisGenotype parent2)
        {
            // Implementation would evaluate compatibility based on breeding objectives
            return 0.8f; // Placeholder
        }

        private float GetTraitValue(CannabisGenotype genotype, TraitType trait)
        {
            // Implementation would extract trait value from genotype
            return Random.Range(0.5f, 1.5f); // Placeholder
        }

        public override bool ValidateData()
        {
            bool isValid = base.ValidateData();

            if (_crossingSuccessRate <= 0f || _crossingSuccessRate > 1f)
            {
                LogError("Crossing success rate must be between 0 and 1");
                isValid = false;
            }

            if (_averageOffspringCount < 1)
            {
                LogError("Average offspring count must be at least 1");
                isValid = false;
            }

            if (_maxInbreedingCoefficient > 1f)
            {
                LogError("Maximum inbreeding coefficient cannot exceed 1");
                isValid = false;
            }

            if (_hybridVigorMultiplier < 1f)
            {
                LogWarning("Hybrid vigor multiplier less than 1 will reduce offspring performance");
            }

            return isValid;
        }

        /// <summary>
        /// Reset to default breeding configuration
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            _enableStandardCrossing = true;
            _enableBackcrossing = true;
            _crossingSuccessRate = 0.85f;
            _averageOffspringCount = 8;
            _selectionPressure = 0.7f;
            _hybridVigorMultiplier = 1.25f;
            _maxInbreedingCoefficient = 0.6f;
            
            LogInfo("Cannabis breeding config reset to default values");
        }
    }

    // BreedingObjective moved to BreedingDataStructures.cs as class to resolve namespace conflict

    /// <summary>
    /// Available breeding methods
    /// </summary>
    public enum BreedingMethod
    {
        StandardCross,
        Backcross,
        SelfPollination,
        LineBreeding,
        OutCross,
        HybridCross
    }
}