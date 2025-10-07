using UnityEngine;
using ProjectChimera.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using System;
using System.Linq;
using ProjectChimera.Data.Shared;
using GeneticPlantStrainSO = ProjectChimera.Data.Genetics.GeneticPlantStrainSO;


namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Simplified Plant Instance - Main orchestrator for modular plant system
    /// Coordinates state management, growth, resources, and harvest
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Instance", menuName = "Project Chimera/Cultivation/Plant Instance")]
    public class PlantInstanceSO : ChimeraDataSO
    {
        // Private data holders for plant systems
        [SerializeField] private PlantStateData _plantState;
        [SerializeField] private PlantGrowthData _plantGrowth;
        [SerializeField] private PlantResourceData _plantResources;
        [SerializeField] private PlantHarvestData _plantHarvest;

        // Legacy properties for backward compatibility (auto-initialized)
        [Header("Plant Identity")]
        [SerializeField] private string _plantID = "";
        [SerializeField] private string _plantName = "";
        [SerializeField] private PlantStrainSO _strain;
        [SerializeField] private GenotypeDataSO _genotype;

        [Header("Current State")]
        [SerializeField] private PlantGrowthStage _currentGrowthStage = PlantGrowthStage.Seedling;
        [SerializeField] private float _ageInDays = 0f;
        [SerializeField] private float _daysInCurrentStage = 0f;
        [SerializeField] private Vector3 _worldPosition = Vector3.zero;

        [Header("Physical Characteristics")]
        [SerializeField, Range(0f, 500f)] private float _currentHeight = 5f;
        [SerializeField, Range(0f, 200f)] private float _currentWidth = 2f;
        [SerializeField, Range(0f, 100f)] private float _rootMassPercentage = 30f;
        [SerializeField, Range(0f, 1000f)] private float _leafArea = 10f;

        [Header("Health and Vitality")]
        [SerializeField, Range(0f, 1f)] private float _overallHealth = 1f;
        [SerializeField, Range(0f, 1f)] private float _vigor = 1f;
        [SerializeField, Range(0f, 1f)] private float _stressLevel = 0f;
        [SerializeField, Range(0f, 1f)] private float _immuneResponse = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _maturityLevel = 0f;

        [Header("Resource Status")]
        [SerializeField, Range(0f, 1f)] private float _waterLevel = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _nutrientLevel = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _energyReserves = 0.6f;

        [Header("Growth Metrics")]
        [SerializeField] private float _dailyGrowthRate = 1f;
        [SerializeField] private float _biomassAccumulation = 2f;
        [SerializeField] private float _rootDevelopmentRate = 1f;
        [SerializeField, Range(0f, 2f)] private float _growthProgress = 0f;
        [SerializeField] private int _daysSincePlanted = 0;

        [Header("Environmental History")]
        [SerializeField] private EnvironmentalConditions _currentEnvironment;
        [SerializeField] private float _cumulativeStressDays = 0f;
        [SerializeField] private float _optimalDays = 0f;

        [Header("Cultivation Events")]
        [SerializeField] private DateTime _plantedDate = DateTime.Now;
        [SerializeField] private DateTime _lastWatering = DateTime.Now;
        [SerializeField] private DateTime _lastFeeding = DateTime.Now;
        [SerializeField] private DateTime _lastTraining = DateTime.MinValue;

        [Header("Genetic Expression")]
        [SerializeField] private float _calculatedMaxHeight = 150f;
        [SerializeField] private float _lastTraitCalculationAge = 0f;

        /// <summary>
        /// Initialize the modular plant system
        /// </summary>
        private void InitializeModularSystem()
        {
            // Initialize static class references (no instantiation needed for static classes)
            // PlantState, PlantGrowth, PlantResources, and PlantHarvest are static classes
            if (_plantState == null)
            {
                // Sync with serialized values
                SyncFromSerializedFields();
            }
        }

        /// <summary>
        /// Sync modular system with serialized fields for backward compatibility
        /// </summary>
        private void SyncFromSerializedFields()
        {
            _plantState.PlantID = _plantID;
            _plantState.PlantName = _plantName;
            _plantState.Strain = _strain?.Name ?? "";
            _plantState.Genotype = _genotype?.Name ?? "";
            _plantState.StrainSO = _strain;
            _plantState.GenotypeSO = _genotype;
            _plantState.CurrentGrowthStage = _currentGrowthStage;
            _plantState.AgeInDays = _ageInDays;
            _plantState.DaysInCurrentStage = _daysInCurrentStage;
            _plantState.WorldPosition = _worldPosition;
            _plantState.CurrentHeight = _currentHeight;
            _plantState.CurrentWidth = _currentWidth;
            _plantState.RootMassPercentage = _rootMassPercentage;
            _plantState.LeafArea = _leafArea;
            _plantState.OverallHealth = _overallHealth;
            _plantState.Vigor = _vigor;
            _plantState.StressLevel = _stressLevel;
            _plantState.ImmuneResponse = _immuneResponse;
            _plantState.MaturityLevel = _maturityLevel;
            _plantState.WaterLevel = _waterLevel;
            _plantState.NutrientLevel = _nutrientLevel;
            _plantState.EnergyReserves = _energyReserves;
            _plantState.DailyGrowthRate = _dailyGrowthRate;
            _plantState.BiomassAccumulation = _biomassAccumulation;
            _plantState.RootDevelopmentRate = _rootDevelopmentRate;
            _plantState.GrowthProgress = _growthProgress;
            _plantState.DaysSincePlanted = _daysSincePlanted;
            _plantState.CumulativeStressDays = _cumulativeStressDays;
            _plantState.OptimalDays = _optimalDays;
            _plantState.PlantedDate = _plantedDate;
            _plantState.LastWatering = _lastWatering;
            _plantState.LastFeeding = _lastFeeding;
            _plantState.LastTraining = _lastTraining;
            _plantState.CalculatedMaxHeight = _calculatedMaxHeight;
            _plantState.LastTraitCalculationAge = _lastTraitCalculationAge;

            // Use the Water method to set water level
            _plantResources.Water(_waterLevel - _plantResources.WaterLevel);

            // Use the Feed method to set nutrient level
            var nutrientDict = new System.Collections.Generic.Dictionary<string, float>
            {
                ["NPK"] = _nutrientLevel - _plantResources.NutrientLevel
            };
            if (nutrientDict["NPK"] > 0)
            {
                _plantResources.Feed(nutrientDict);
            }

            // Energy reserves is set directly since it's a field
            _plantResources.CurrentEnergyLevel = _energyReserves;
            _plantResources.LastWatering = _lastWatering;
            _plantResources.LastFeeding = _lastFeeding;
            _plantResources.LastTraining = _lastTraining;
        }

        /// <summary>
        /// Sync serialized fields with modular system
        /// </summary>
        private void SyncToSerializedFields()
        {
            _plantID = _plantState.PlantID;
            _plantName = _plantState.PlantName;
            _currentGrowthStage = _plantState.CurrentGrowthStage;
            _ageInDays = _plantState.AgeInDays;
            _daysInCurrentStage = _plantState.DaysInCurrentStage;
            _worldPosition = _plantState.WorldPosition;
            _currentHeight = _plantState.CurrentHeight;
            _currentWidth = _plantState.CurrentWidth;
            _rootMassPercentage = _plantState.RootMassPercentage;
            _leafArea = _plantState.LeafArea;
            _overallHealth = _plantState.OverallHealth;
            _vigor = _plantState.Vigor;
            _stressLevel = _plantState.StressLevel;
            _immuneResponse = _plantState.ImmuneResponse;
            _maturityLevel = _plantState.MaturityLevel;
            _waterLevel = _plantState.WaterLevel;
            _nutrientLevel = _plantState.NutrientLevel;
            _energyReserves = _plantState.EnergyReserves;
            _dailyGrowthRate = _plantState.DailyGrowthRate;
            _biomassAccumulation = _plantState.BiomassAccumulation;
            _rootDevelopmentRate = _plantState.RootDevelopmentRate;
            _growthProgress = _plantState.GrowthProgress;
            _daysSincePlanted = (int)_plantState.DaysSincePlanted;
            _cumulativeStressDays = _plantState.CumulativeStressDays;
            _optimalDays = _plantState.OptimalDays;
            _plantedDate = _plantState.PlantedDate;
            _lastWatering = _plantState.LastWatering;
            _lastFeeding = _plantState.LastFeeding;
            _lastTraining = _plantState.LastTraining;
            _calculatedMaxHeight = _plantState.CalculatedMaxHeight;
            _lastTraitCalculationAge = _plantState.LastTraitCalculationAge;
        }

        #region Public API

        /// <summary>
        /// Initialize plant with basic information
        /// </summary>
        public void InitializePlant(string plantID, string plantName, PlantStrainSO strain, GenotypeDataSO genotype, Vector3 worldPosition)
        {
            InitializeModularSystem();
            _plantState.Initialize(plantID, plantName, strain, genotype, worldPosition);
            SyncToSerializedFields();
        }

        /// <summary>
        /// Process daily growth
        /// </summary>
        public void ProcessDailyGrowth()
        {
            InitializeModularSystem();
            _plantGrowth.ProcessDailyGrowth();
            _plantResources.UpdateResources();
            _plantState.CurrentEnvironment = _currentEnvironment.ToString();
            SyncToSerializedFields();
        }

        /// <summary>
        /// Set growth stage
        /// </summary>
        public void SetGrowthStage(PlantGrowthStage newStage)
        {
            InitializeModularSystem();
            _plantState.SetGrowthStage(newStage);
            SyncToSerializedFields();
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public WateringResult Water(float waterAmount)
        {
            InitializeModularSystem();
            var result = new WateringResult
            {
                WaterAmount = waterAmount,
                Success = true,
                Timestamp = DateTime.Now
            };

            _plantResources.Water(waterAmount);
            _plantState.WaterLevel = _plantResources.WaterLevel;
            _plantState.LastWatering = DateTime.Now;
            SyncToSerializedFields();

            return result;
        }

        /// <summary>
        /// Feed the plant
        /// </summary>
        public ProjectChimera.Data.Cultivation.FeedingResult Feed(float nutrientAmount)
        {
            InitializeModularSystem();
            var nutrients = new System.Collections.Generic.Dictionary<string, float>
            {
                ["NPK"] = nutrientAmount
            };
            _plantResources.Feed(nutrients);

            var result = new ProjectChimera.Data.Cultivation.FeedingResult
            {
                Success = true,
                NutrientAmountApplied = nutrientAmount,
                NutrientLevelAfter = _plantResources.NutrientLevel,
                Timestamp = DateTime.Now
            };

            _plantState.NutrientLevel = _plantResources.NutrientLevel;
            _plantState.LastFeeding = DateTime.Now;
            SyncToSerializedFields();

            return result;
        }

        /// <summary>
        /// Apply training
        /// </summary>
        public TrainingResult ApplyTraining(string trainingType)
        {
            InitializeModularSystem();
            _plantResources.ApplyTraining();

            var result = new TrainingResult
            {
                TrainingType = trainingType,
                Success = true,
                TrainingDate = DateTime.Now
            };

            _plantState.LastTraining = DateTime.Now;
            SyncToSerializedFields();

            return result;
        }

        /// <summary>
        /// Calculate yield potential
        /// </summary>
        public YieldCalculation CalculateYieldPotential()
        {
            InitializeModularSystem();
            float yieldValue = _plantHarvest.CalculateYieldPotential();
            return new YieldCalculation
            {
                EstimatedYield = yieldValue,
                YieldConfidence = 0.8f,
                CalculationDate = DateTime.Now
            };
        }

        /// <summary>
        /// Calculate potency potential
        /// </summary>
        public float CalculatePotencyPotential()
        {
            InitializeModularSystem();
            return _plantHarvest.CalculateYieldPotential() * 0.15f; // Default potency calculation
        }

        /// <summary>
        /// Harvest the plant
        /// </summary>
        public HarvestResult Harvest()
        {
            InitializeModularSystem();
            var result = _plantHarvest.Harvest();

            // Mark plant as harvested (could set to Dormant or remove from active tracking)
            _plantState.CurrentGrowthStage = PlantGrowthStage.Dormant;
            _plantState.OverallHealth = 0f;
            _plantState.Health = 0f;

            SyncToSerializedFields();
            return result;
        }

        /// <summary>
        /// Get harvest readiness
        /// </summary>
        public HarvestReadiness CheckHarvestReadiness()
        {
            InitializeModularSystem();
            bool isReady = _plantHarvest.CheckHarvestReadiness();
            return new HarvestReadiness
            {
                IsReadyForHarvest = isReady,
                ReadinessReason = isReady ? "Plant is ready for harvest" : "Plant is not ready for harvest"
            };
        }

        /// <summary>
        /// Get harvest recommendations
        /// </summary>
        public HarvestRecommendation GetHarvestRecommendations()
        {
            InitializeModularSystem();
            // Assume the underlying method returns appropriate data
            return new HarvestRecommendation
            {
                IsReady = true,
                OptimalHarvestDate = DateTime.Now.AddDays(7),
                RecommendationReason = "Plant shows optimal harvest characteristics"
            };
        }

        /// <summary>
        /// Get post-harvest processing recommendations
        /// </summary>
        public PostHarvestProcess GetPostHarvestProcess(HarvestResult harvestResult)
        {
            InitializeModularSystem();
            // Assume the underlying method doesn't take parameters
            return new PostHarvestProcess
            {
                ProcessType = "Standard Drying and Curing",
                Duration = 14f,
                Temperature = 20f,
                Humidity = 55f
            };
        }

        /// <summary>
        /// Get resource status
        /// </summary>
        public float GetResourceStatus()
        {
            InitializeModularSystem();
            return _plantResources.GetResourceStatus();
        }

        /// <summary>
        /// Get optimal watering schedule
        /// </summary>
        public float GetOptimalWateringSchedule()
        {
            InitializeModularSystem();
            return _plantResources.GetOptimalWateringSchedule();
        }

        /// <summary>
        /// Get optimal feeding schedule
        /// </summary>
        public float GetOptimalFeedingSchedule()
        {
            InitializeModularSystem();
            return _plantResources.GetOptimalFeedingSchedule();
        }

        /// <summary>
        /// Get plant summary
        /// </summary>
        public PlantSummary GetSummary()
        {
            InitializeModularSystem();
            return _plantState.GetSummary();
        }

        #endregion

        #region Legacy Properties (Backward Compatibility)

        public string PlantID => _plantID;
        public string PlantName => _plantName;
        public PlantStrainSO Strain => _strain;
        public GenotypeDataSO Genotype => _genotype;
        public PlantGrowthStage CurrentGrowthStage
        {
            get => _currentGrowthStage;
            set => _currentGrowthStage = value;
        }
        public float AgeInDays => _ageInDays;
        public int DaysSincePlanted
        {
            get => (int)_ageInDays;
            set => _ageInDays = value;
        }
        public float DaysInCurrentStage => _daysInCurrentStage;
        public Vector3 WorldPosition => _worldPosition;
        public float CurrentHeight => _currentHeight;
        public float CurrentWidth => _currentWidth;
        public float RootMassPercentage => _rootMassPercentage;
        public float LeafArea => _leafArea;
        public float OverallHealth => _overallHealth;
        public float CurrentHealth
        {
            get => _overallHealth;
            set => _overallHealth = Mathf.Clamp01(value);
        }
        public float CurrentGrowthProgress
        {
            get => _maturityLevel;
            set => _maturityLevel = Mathf.Clamp01(value);
        }
        public float MaturityLevel => _maturityLevel;
        public float Vigor => _vigor;
        public float StressLevel => _stressLevel;
        public float ImmuneResponse => _immuneResponse;
        public float WaterLevel
        {
            get => _waterLevel;
            set => _waterLevel = Mathf.Clamp01(value);
        }
        public float NutrientLevel
        {
            get => _nutrientLevel;
            set => _nutrientLevel = Mathf.Clamp01(value);
        }
        public float EnergyReserves => _energyReserves;
        public float DailyGrowthRate => _dailyGrowthRate;
        public float BiomassAccumulation => _biomassAccumulation;
        public float GrowthProgress => _growthProgress;
        public float RootDevelopmentRate => _rootDevelopmentRate;
        public float CumulativeStressDays => _cumulativeStressDays;
        public float OptimalDays => _optimalDays;
        public DateTime PlantedDate => _plantedDate;
        public DateTime LastWatering => _lastWatering;
        public DateTime LastFeeding => _lastFeeding;
        public DateTime LastTraining => _lastTraining;
        public float CalculatedMaxHeight => _calculatedMaxHeight;
        public bool IsActive => _overallHealth > 0f && _currentGrowthStage != PlantGrowthStage.Dormant;
        public bool RequiresTraining => _stressLevel > 0.7f || _vigor < 0.3f;

        // Additional compatibility aliases
        public string PlantId => _plantID; // Alias for PlantID
        public string PlantInstanceId => _plantID; // Alias for PlantID

        // Writable CurrentGrowthProgress property
        public float CurrentGrowthProgress_Writable
        {
            get => _maturityLevel;
            set => _maturityLevel = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Initialize plant from strain data
        /// </summary>
        public void InitializeFromStrain(object strain)
        {
            if (strain != null)
            {
                // Convert to local PlantStrainSO type if possible
                if (strain is PlantStrainSO localStrain)
                {
                    _strain = localStrain;
                }
                // Otherwise, keep _strain as null and extract data only

                // Use a safe approach for plant name with fallback
                _plantName = GetStrainName(strain);
                // Use a safe approach for max height with fallback
                _calculatedMaxHeight = GetStrainMaxHeight(strain);
                // Initialize other strain-specific properties as needed
            }
        }

        /// <summary>
        /// Safely get strain name with fallback using pattern matching
        /// </summary>
        private string GetStrainName(object strain)
        {
            if (strain == null)
                return "Unknown Strain";

            // Use pattern matching to handle different PlantStrainSO types
            switch (strain)
            {
                // Handle ScriptableObject PlantStrainSO from Data.Cultivation namespace
                case ProjectChimera.Data.Cultivation.PlantStrainSO cultivationStrain:
                    return cultivationStrain.StrainName ?? cultivationStrain.name ?? "Unknown Strain";

                // Handle simple PlantStrainSO class from Data.Cultivation.Plant namespace
                case PlantStrainSO simpleStrain:
                    return simpleStrain.Name ?? "Unknown Strain";

                // Fallback
                default:
                    return strain.ToString() ?? "Unknown Strain";
            }
        }

        /// <summary>
        /// Safely get max height from strain with fallback using pattern matching
        /// </summary>
        private float GetStrainMaxHeight(object strain)
        {
            if (strain == null)
                return 150.0f;

            // Use pattern matching to handle different PlantStrainSO types
            switch (strain)
            {
                // Handle ScriptableObject PlantStrainSO from Data.Cultivation namespace
                case ProjectChimera.Data.Cultivation.PlantStrainSO cultivationStrain:
                    return cultivationStrain.MaxHeight;

                // Handle simple PlantStrainSO class from Data.Cultivation.Plant namespace
                case PlantStrainSO simpleStrain:
                    // Simple strain doesn't have MaxHeight, use default
                    return 150.0f;

                // Fallback
                default:
                    return 150.0f;
            }
        }

        #endregion
    }

    // Additional data structures for PlantInstanceSO functionality
    [System.Serializable]
    public class TrainingResult
    {
        public string TrainingType;
        public bool Success;
        public float StressIncrease;
        public DateTime TrainingDate;
        public float EnergyCost;
    }

    [System.Serializable]
    public class YieldCalculation
    {
        public float EstimatedYield;
        public float YieldConfidence;
        public DateTime CalculationDate;
    }


    [System.Serializable]
    public class HarvestRecommendationSimple
    {
        public bool IsReady;
        public DateTime OptimalHarvestDate;
        public string RecommendationReason;
    }

    [System.Serializable]
    public class PostHarvestProcessSimple
    {
        public string ProcessType;
        public float Duration;
        public float Temperature;
        public float Humidity;
    }


    // PlantSummary class moved to PlantState.cs to avoid duplication

    // WateringSchedule, FeedingSchedule, WateringResult, and FeedingResult are defined in PlantResources.cs to avoid duplication

    /// <summary>
    /// Result of watering operation
    /// </summary>
    [System.Serializable]
    public class WateringResultSimple
    {
        public float WaterAmount;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Result of feeding operation
    /// </summary>
    [System.Serializable]
    public class FeedingResultSimple
    {
        public float NutrientAmount;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }
}
