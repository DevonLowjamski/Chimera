using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Plants System Save/Load Operations
    /// These DTOs capture the state of the cultivation system including plant instances,
    /// genetics, lifecycle management, and cultivation activities.
    /// </summary>
    
    /// <summary>
    /// Main cultivation state DTO for the entire plant management system
    /// </summary>
    [System.Serializable]
    public class CultivationStateDTO
    {
        [Header("Cultivation System State")]
        public PlantLifecycleStateDTO LifecycleState;
        public HarvestStateDTO HarvestState;
        public CultivationEnvironmentalStateDTO EnvironmentalState;
        public PlantCareStateDTO PlantCareState;
        
        [Header("Active Plants")]
        public List<PlantInstanceDTO> ActivePlants = new List<PlantInstanceDTO>();
        public Dictionary<string, Vector3> PlantPositions = new Dictionary<string, Vector3>();
        public Dictionary<string, string> PlantZoneAssignments = new Dictionary<string, string>();
        
        [Header("Cultivation Statistics")]
        public CultivationMetricsDTO Metrics;
        public CultivationPerformanceDTO Performance;
        
        [Header("Genetic Library")]
        public List<PlantStrainDTO> AvailableStrains = new List<PlantStrainDTO>();
        public List<GenotypeDTO> StoredGenotypes = new List<GenotypeDTO>();
        public GeneticLibraryStateDTO GeneticLibrary;
        
        [Header("Cultivation Zones")]
        public List<CultivationZoneDTO> CultivationZones = new List<CultivationZoneDTO>();
        
        [Header("System Configuration")]
        public bool EnableCultivationSystem = true;
        public int MaxPlantsPerGrow = 50;
        public bool EnableAutoGrowth = true;
        public float TimeAcceleration = 1.0f;
        public float GrowthUpdateInterval = 1.0f;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for individual plant instances
    /// </summary>
    [System.Serializable]
    public class PlantInstanceDTO
    {
        [Header("Plant Identity")]
        public string PlantID;
        public string PlantName;
        public string StrainId;
        public string GenotypeId;
        public string ZoneId = "default";
        
        [Header("Current State")]
        public string CurrentGrowthStage; // "Seed", "Germination", "Seedling", etc.
        public float AgeInDays;
        public float DaysInCurrentStage;
        public Vector3 WorldPosition;
        
        [Header("Physical Characteristics")]
        public float CurrentHeight; // cm
        public float CurrentWidth; // cm
        public float RootMassPercentage; // % of total plant mass
        public float LeafArea; // cm²
        public float BiomassAccumulation; // g/day
        
        [Header("Health and Vitality")]
        public float OverallHealth;
        public float Vigor; // Growth energy
        public float StressLevel;
        public float ImmuneResponse; // Disease/pest resistance
        public float MaturityLevel; // Overall plant maturity
        
        [Header("Resource Status")]
        public float WaterLevel; // Current hydration
        public float NutrientLevel; // Current nutrient status
        public float EnergyReserves; // Stored energy for growth
        
        [Header("Growth Metrics")]
        public float DailyGrowthRate; // cm/day
        public float RootDevelopmentRate; // Root expansion rate
        public float CalculatedMaxHeight; // Genetically determined max height
        
        [Header("Environmental History")]
        public CultivationEnvironmentalDataDTO CurrentEnvironment;
        public float CumulativeStressDays; // Total stress exposure
        public float OptimalDays; // Days in optimal conditions
        
        [Header("Cultivation Events")]
        public DateTime PlantedDate;
        public DateTime LastWatering;
        public DateTime LastFeeding;
        public DateTime LastTraining; // LST, topping, etc.
        public DateTime LastStageTransition;
        
        [Header("Genetic Expression")]
        public float LastTraitCalculationAge; // Age when traits were last calculated
        public List<TraitExpressionDTO> ExpressedTraits = new List<TraitExpressionDTO>();
        
        [Header("Yield Prediction")]
        public float PredictedYield; // Estimated final yield in grams
        public float QualityPrediction; // Estimated quality score
        public float PotencyPrediction; // Estimated potency percentage
        public DateTime EstimatedHarvestDate;
        
        [Header("Care History")]
        public List<PlantCareEventDTO> CareHistory = new List<PlantCareEventDTO>();
        public int TotalWaterings;
        public int TotalFeedings;
        public int TotalTrainingSessions;
        
        [Header("Health Events")]
        public List<PlantHealthEventDTO> HealthEvents = new List<PlantHealthEventDTO>();
        public bool HasActivePests = false;
        public bool HasActiveDiseases = false;
        public List<string> ActiveIssues = new List<string>();
    }
    
    /// <summary>
    /// DTO for plant strain information
    /// </summary>
    [System.Serializable]
    public class PlantStrainDTO
    {
        [Header("Strain Identity")]
        public string StrainId;
        public string StrainName;
        public string BreederName;
        public string OriginRegion;
        public string StrainDescription;
        public string StrainType; // "Sativa", "Indica", "Hybrid"
        public bool IsFounderStrain = false;
        public bool IsCustomStrain = false;
        
        [Header("Breeding Lineage")]
        public string ParentStrain1Id;
        public string ParentStrain2Id;
        public int GenerationNumber = 1; // F1, F2, etc.
        public bool IsLandrace = false;
        public bool IsStabilized = false;
        public float BreedingStability = 0.5f;
        public float GeneticDiversity = 0.5f;
        public float RarityScore = 0.5f;
        
        [Header("Genetic Modifiers")]
        public float HeightModifier = 1.0f;
        public float WidthModifier = 1.0f;
        public float YieldModifier = 1.0f;
        public float GrowthRateModifier = 1.0f;
        
        [Header("Flowering Characteristics")]
        public string PhotoperiodSensitivity; // "Photoperiod", "Autoflower"
        public float FloweringTimeModifier = 1.0f;
        public bool Autoflowering = false;
        public int AutofloweringTriggerDays = 0;
        public int BaseFloweringTime = 60; // days
        
        [Header("Chemical Profiles")]
        public CannabinoidProfileDTO CannabinoidProfile;
        public TerpeneProfileDTO TerpeneProfile;
        
        [Header("Physical Traits")]
        public string LeafStructure; // "Broad", "Narrow", "Medium"
        public string BudStructure; // "Dense", "Loose", "Medium"
        public Color LeafColor;
        public Color BudColor;
        public float ResinProductionModifier = 1.0f;
        
        [Header("Environmental Tolerances")]
        public float HeatToleranceModifier = 0.0f;
        public float ColdToleranceModifier = 0.0f;
        public float DroughtToleranceModifier = 0.0f;
        public float DiseaseResistanceModifier = 0.0f;
        
        [Header("Cultivation Properties")]
        public string CultivationDifficulty; // "Beginner", "Intermediate", "Advanced"
        public float BeginnerFriendliness = 0.5f;
        public float EnvironmentalSensitivity = 0.5f;
        public float BaseHealthModifier = 1.0f;
        public float HealthRecoveryRate = 0.1f;
        public float BaseYieldGrams = 100f;
        public float BaseQualityModifier = 1.0f;
        public float BasePotencyModifier = 1.0f;
        public float BaseHeight = 1.5f;
        
        [Header("Commercial Properties")]
        public float MarketValue = 10f; // per gram
        public float MarketDemand = 0.5f;
        public bool SeedsAvailable = true;
        public bool ClonesAvailable = false;
        
        [Header("Effects and Medical")]
        public EffectsProfileDTO EffectsProfile;
        public List<string> MedicalApplications = new List<string>();
    }
    
    /// <summary>
    /// DTO for genotype information
    /// </summary>
    [System.Serializable]
    public class GenotypeDTO
    {
        [Header("Genotype Identity")]
        public string GenotypeId;
        public string IndividualID;
        public string ParentStrainId;
        public string GenotypeType; // "Individual", "Population", "Elite"
        public string GenotypeDescription;
        
        [Header("Genetic Composition")]
        public List<GenePairDTO> GenePairs = new List<GenePairDTO>();
        public int TotalGeneCount;
        public int HomozygousCount;
        public int HeterozygousCount;
        public float GeneticDiversity = 0.5f;
        
        [Header("Inheritance Information")]
        public string Parent1GenotypeId;
        public string Parent2GenotypeId;
        public int Generation = 1; // F1, F2, etc.
        public bool IsInbred = false;
        public float InbreedingCoefficient = 0.0f;
        
        [Header("Genetic Fitness")]
        public float OverallFitness = 1.0f;
        public float ReproductiveFitness = 1.0f;
        public float ViabilityFitness = 1.0f;
        public bool IsViable = true;
        public bool HasLethalCombinations = false;
        
        [Header("Mutation Tracking")]
        public List<MutationEventDTO> MutationHistory = new List<MutationEventDTO>();
        public float OverallMutationRate = 0.001f;
        public bool HasBeneficialMutations = false;
        public bool HasDetrimentalMutations = false;
        
        [Header("Phenotype Prediction")]
        public List<PredictedTraitDTO> PredictedTraits = new List<PredictedTraitDTO>();
        public bool PhenotypeCalculated = false;
        public float LastCalculationTime = 0f;
        
        [Header("Breeding Value")]
        public float BreedingValue = 50f;
        public List<BreedingTraitDTO> BreedingTraits = new List<BreedingTraitDTO>();
        public bool IsEliteGenotype = false;
        public bool RecommendedForBreeding = true;
    }
    
    /// <summary>
    /// DTO for gene pairs
    /// </summary>
    [System.Serializable]
    public class GenePairDTO
    {
        public string GeneId;
        public string GeneName;
        public string Allele1Id;
        public string Allele2Id;
        public bool IsHomozygous;
        public string DominancePattern; // "Dominant", "Recessive", "Codominant"
        public float ExpressionLevel;
        public Dictionary<string, object> GeneProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for trait expression
    /// </summary>
    [System.Serializable]
    public class TraitExpressionDTO
    {
        public string TraitName;
        public string TraitType; // "Morphological", "Physiological", "Chemical"
        public float ExpressionValue;
        public float ExpressionStrength;
        public bool IsActivelyExpressed;
        public DateTime LastCalculated;
        public Dictionary<string, float> EnvironmentalModifiers = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for environmental conditions in cultivation system
    /// </summary>
    [System.Serializable]
    public class CultivationEnvironmentalDataDTO
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirFlow;
        public float AirVelocity;
        public float PhotoperiodHours;
        public float pH;
        public float WaterAvailability;
        public float ElectricalConductivity;
        public float DailyLightIntegral;
        public float VaporPressureDeficit;
        public float BarometricPressure;
        public float AirQualityIndex;
        public DateTime LastMeasurement;
        public Dictionary<string, float> CustomParameters = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for cannabinoid profiles
    /// </summary>
    [System.Serializable]
    public class CannabinoidProfileDTO
    {
        public float THCPercentage;
        public float CBDPercentage;
        public float CBGPercentage;
        public float CBNPercentage;
        public float CBCPercentage;
        public float THCVPercentage;
        public float CBDVPercentage;
        public Dictionary<string, float> MinorCannabinoids = new Dictionary<string, float>();
        public float TotalCannabinoids;
        public string ProfileType; // "THC-Dominant", "CBD-Dominant", "Balanced"
    }
    
    /// <summary>
    /// DTO for terpene profiles
    /// </summary>
    [System.Serializable]
    public class TerpeneProfileDTO
    {
        public float Myrcene;
        public float Limonene;
        public float Pinene;
        public float Linalool;
        public float Caryophyllene;
        public float Humulene;
        public float Terpinolene;
        public float Ocimene;
        public Dictionary<string, float> MinorTerpenes = new Dictionary<string, float>();
        public float TotalTerpenes;
        public string AromaticProfile; // "Citrus", "Pine", "Floral", "Earthy"
    }
    
    /// <summary>
    /// DTO for effects profiles
    /// </summary>
    [System.Serializable]
    public class EffectsProfileDTO
    {
        public string PrimaryEffect; // "Energizing", "Relaxing", "Balanced"
        public float EnergeticLevel;
        public float RelaxingLevel;
        public float EuphoricLevel;
        public float CreativeLevel;
        public float FocusLevel;
        public float SedativeLevel;
        public List<string> PositiveEffects = new List<string>();
        public List<string> NegativeEffects = new List<string>();
        public int OnsetTime; // minutes
        public int Duration; // minutes
        public string IntensityLevel; // "Mild", "Moderate", "Strong"
    }
    
    /// <summary>
    /// DTO for plant care events
    /// </summary>
    [System.Serializable]
    public class PlantCareEventDTO
    {
        public string EventId;
        public string EventType; // "Watering", "Feeding", "Training", "Pruning"
        public DateTime EventTime;
        public string PlantId;
        public float Amount; // Water amount, nutrient amount, etc.
        public string CareToolUsed;
        public string Notes;
        public bool WasSuccessful = true;
        public float HealthImpact;
        public Dictionary<string, object> EventParameters = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for plant health events
    /// </summary>
    [System.Serializable]
    public class PlantHealthEventDTO
    {
        public string EventId;
        public string EventType; // "Pest", "Disease", "Deficiency", "Stress"
        public DateTime EventTime;
        public string PlantId;
        public string Description;
        public string Severity; // "Minor", "Moderate", "Severe", "Critical"
        public bool IsResolved = false;
        public DateTime ResolvedTime;
        public string Treatment;
        public float HealthImpact;
        public List<string> Symptoms = new List<string>();
    }
    
    /// <summary>
    /// DTO for mutation events
    /// </summary>
    [System.Serializable]
    public class MutationEventDTO
    {
        public string MutationId;
        public string MutationType; // "Point", "Insertion", "Deletion", "Duplication"
        public DateTime MutationTime;
        public string AffectedGeneId;
        public string MutationDescription;
        public bool IsBeneficial;
        public bool IsDetrimental;
        public float EffectStrength;
        public List<string> AffectedTraits = new List<string>();
    }
    
    /// <summary>
    /// DTO for predicted traits
    /// </summary>
    [System.Serializable]
    public class PredictedTraitDTO
    {
        public string TraitName;
        public float PredictedValue;
        public float Confidence;
        public string PredictionMethod; // "Genetic", "Environmental", "Combined"
        public DateTime PredictionTime;
        public Dictionary<string, float> InfluencingFactors = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for breeding traits
    /// </summary>
    [System.Serializable]
    public class BreedingTraitDTO
    {
        public string TraitName;
        public float BreedingValue;
        public float Heritability;
        public string SelectionCriteria; // "High", "Medium", "Low"
        public bool IsPrimaryTarget;
        public float GeneticVariance;
        public float EnvironmentalVariance;
    }
    
    /// <summary>
    /// DTO for cultivation zones
    /// </summary>
    [System.Serializable]
    public class CultivationZoneDTO
    {
        public string ZoneId;
        public string ZoneName;
        public string ZoneType; // "Propagation", "Vegetative", "Flowering", "Drying"
        public Vector3 ZonePosition;
        public Vector3 ZoneSize;
        public int MaxPlantCapacity;
        public int CurrentPlantCount;
        public List<string> PlantIds = new List<string>();
        public CultivationEnvironmentalDataDTO ZoneEnvironment;
        public bool IsActive = true;
        public bool HasAutomation = false;
        public List<string> ConnectedEquipmentIds = new List<string>();
        public DateTime LastUpdated;
    }
    
    /// <summary>
    /// DTO for plant lifecycle state
    /// </summary>
    [System.Serializable]
    public class PlantLifecycleStateDTO
    {
        public int MaxPlantsPerGrow = 50;
        public int TotalPlantsGrown;
        public int TotalPlantsHarvested;
        public float TotalYieldHarvested;
        public Dictionary<string, int> PlantsByStage = new Dictionary<string, int>();
        public List<string> PlantsNeedingAttention = new List<string>();
        public DateTime LastLifecycleUpdate;
        public bool IsInitialized = false;
    }
    
    /// <summary>
    /// DTO for harvest state
    /// </summary>
    [System.Serializable]
    public class HarvestStateDTO
    {
        public List<HarvestRecordDTO> HarvestHistory = new List<HarvestRecordDTO>();
        public float TotalYieldHarvested;
        public float AverageYieldPerPlant;
        public float AverageQualityScore;
        public float AveragePotency;
        public int TotalHarvests;
        public DateTime LastHarvest;
        public bool IsInitialized = false;
    }
    
    /// <summary>
    /// DTO for harvest records
    /// </summary>
    [System.Serializable]
    public class HarvestRecordDTO
    {
        public string HarvestId;
        public string PlantId;
        public DateTime HarvestDate;
        public float YieldAmount; // grams
        public float QualityScore; // 0-100
        public float Potency; // percentage
        public string HarvestStage; // "Early", "Peak", "Late"
        public List<string> HarvestNotes = new List<string>();
        public string ProcessingMethod;
        public float DryingTime; // days
        public float CuringTime; // days
        public Dictionary<string, float> ChemicalAnalysis = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for cultivation environmental state
    /// </summary>
    [System.Serializable]
    public class CultivationEnvironmentalStateDTO
    {
        public CultivationEnvironmentalDataDTO DefaultEnvironment;
        public Dictionary<string, CultivationEnvironmentalDataDTO> ZoneEnvironments = new Dictionary<string, CultivationEnvironmentalDataDTO>();
        public List<EnvironmentalEventDTO> EnvironmentalHistory = new List<EnvironmentalEventDTO>();
        public bool IsInitialized = false;
        public DateTime LastEnvironmentalUpdate;
    }
    
    /// <summary>
    /// DTO for environmental events
    /// </summary>
    [System.Serializable]
    public class EnvironmentalEventDTO
    {
        public string EventId;
        public string EventType; // "TemperatureSpike", "HumidityDrop", "LightFailure"
        public DateTime EventTime;
        public string ZoneId;
        public string Parameter; // "Temperature", "Humidity", etc.
        public float OldValue;
        public float NewValue;
        public string Severity; // "Minor", "Moderate", "Severe"
        public bool WasAutoCorrected = false;
        public string CorrectionAction;
    }
    
    /// <summary>
    /// DTO for plant care state
    /// </summary>
    [System.Serializable]
    public class PlantCareStateDTO
    {
        public Dictionary<string, DateTime> LastWaterTimes = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> LastFeedTimes = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> LastTrainingTimes = new Dictionary<string, DateTime>();
        public Dictionary<string, float> WaterLevels = new Dictionary<string, float>();
        public Dictionary<string, float> NutrientLevels = new Dictionary<string, float>();
        public List<string> PlantsNeedingWater = new List<string>();
        public List<string> PlantsNeedingFood = new List<string>();
        public bool IsInitialized = false;
        public DateTime LastCareUpdate;
    }
    
    /// <summary>
    /// DTO for genetic library state
    /// </summary>
    [System.Serializable]
    public class GeneticLibraryStateDTO
    {
        public List<string> AvailableStrainIds = new List<string>();
        public List<string> StoredGenotypeIds = new List<string>();
        public List<string> EliteGenotypes = new List<string>();
        public Dictionary<string, int> StrainUsageCount = new Dictionary<string, int>();
        public Dictionary<string, float> StrainPerformanceRatings = new Dictionary<string, float>();
        public int TotalStrainsAvailable;
        public int TotalGenotypesStored;
        public DateTime LastLibraryUpdate;
    }
    
    /// <summary>
    /// DTO for cultivation metrics
    /// </summary>
    [System.Serializable]
    public class CultivationMetricsDTO
    {
        [Header("Plant Statistics")]
        public int TotalPlantsCultivated;
        public int ActivePlants;
        public int PlantsHarvested;
        public int PlantDeaths;
        public float PlantSurvivalRate;
        public float AveragePlantLifespan; // days
        
        [Header("Yield Statistics")]
        public float TotalYieldProduced; // grams
        public float AverageYieldPerPlant; // grams
        public float YieldPerSquareMeter; // grams/m²
        public float YieldEfficiency; // yield per input cost
        public float BestSinglePlantYield; // grams
        
        [Header("Quality Statistics")]
        public float AverageQualityScore; // 0-100
        public float AveragePotency; // percentage
        public float BestQualityAchieved;
        public float BestPotencyAchieved;
        public int PremiumQualityHarvests; // Count of harvests >80% quality
        
        [Header("Efficiency Metrics")]
        public float WaterEfficiency; // yield per liter of water
        public float NutrientEfficiency; // yield per unit of nutrients
        public float EnergyEfficiency; // yield per kWh
        public float SpaceUtilization; // percentage of available space used
        public float TimeToHarvest; // average days from seed to harvest
        
        [Header("Genetic Diversity")]
        public int UniqueStrainsGrown;
        public int UniqueGenotypesCreated;
        public float GeneticDiversityIndex;
        public int SuccessfulBreedingAttempts;
        
        [Header("Environmental Performance")]
        public float OptimalConditionUptime; // percentage of time in optimal conditions
        public int EnvironmentalIncidents;
        public float EnvironmentalStabilityScore;
        
        [Header("Care and Maintenance")]
        public int TotalWateringEvents;
        public int TotalFeedingEvents;
        public int TotalTrainingEvents;
        public float CareConsistencyScore;
        public int HealthIssuesResolved;
        
        [Header("Temporal Data")]
        public DateTime FirstPlantDate;
        public DateTime LastHarvestDate;
        public DateTime LastMetricsUpdate;
        public int DaysOfActiveCultivation;
    }
    
    /// <summary>
    /// DTO for cultivation performance analysis
    /// </summary>
    [System.Serializable]
    public class CultivationPerformanceDTO
    {
        [Header("Performance Trends")]
        public float YieldTrend; // positive = improving, negative = declining
        public float QualityTrend;
        public float EfficiencyTrend;
        public float PlantHealthTrend;
        
        [Header("Benchmarking")]
        public float PerformanceVsTarget; // percentage of target performance
        public Dictionary<string, float> StrainPerformanceComparison = new Dictionary<string, float>();
        public Dictionary<string, float> SeasonalPerformance = new Dictionary<string, float>();
        
        [Header("Optimization Opportunities")]
        public List<string> ImprovementAreas = new List<string>();
        public List<string> BestPractices = new List<string>();
        public float OptimizationPotential; // estimated improvement potential
        
        [Header("Predictive Analytics")]
        public float PredictedNextHarvestYield;
        public DateTime PredictedNextHarvestDate;
        public float PredictedQualityScore;
        public List<string> PredictedChallenges = new List<string>();
        
        [Header("ROI Analysis")]
        public float ReturnOnInvestment;
        public float CostPerGram;
        public float RevenuePerPlant;
        public float ProfitMargin;
        
        [Header("Learning Insights")]
        public Dictionary<string, string> LessonsLearned = new Dictionary<string, string>();
        public List<string> RecommendedActions = new List<string>();
        public float ExperienceLevel; // skill progression score
    }
    
    /// <summary>
    /// Result DTO for cultivation save operations
    /// </summary>
    [System.Serializable]
    public class CultivationSaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int PlantsSaved;
        public int StrainsSaved;
        public int GenotypesSaved;
        public int ZonesSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for cultivation load operations
    /// </summary>
    [System.Serializable]
    public class CultivationLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int PlantsLoaded;
        public int StrainsLoaded;
        public int GenotypesLoaded;
        public int ZonesLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public CultivationStateDTO CultivationState;
    }
    
    /// <summary>
    /// DTO for cultivation system validation
    /// </summary>
    [System.Serializable]
    public class CultivationValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("Plant Validation")]
        public bool PlantsValid;
        public bool PlantPositionsValid;
        public bool PlantStatesValid;
        
        [Header("Genetic Validation")]
        public bool StrainsValid;
        public bool GenotypesValid;
        public bool GeneticConsistency;
        
        [Header("Environmental Validation")]
        public bool EnvironmentalDataValid;
        public bool ZoneConfigurationValid;
        
        [Header("Data Integrity")]
        public int TotalPlants;
        public int ValidPlants;
        public int TotalStrains;
        public int ValidStrains;
        public int TotalGenotypes;
        public int ValidGenotypes;
        public float DataIntegrityScore;
    }
}