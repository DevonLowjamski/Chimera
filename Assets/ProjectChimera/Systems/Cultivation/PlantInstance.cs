using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using EnvironmentalStressSO = ProjectChimera.Data.Simulation.EnvironmentalStressSO;
using System;
using System.Collections.Generic;
using HarvestResults = ProjectChimera.Data.Cultivation.HarvestResults;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// REFACTORED: Core plant functionality has been decomposed into focused system components.
    /// This file now serves as a reference implementation using the new component structure.
    ///
    /// New Component Structure:
    /// - PlantInstanceCore.cs: Core plant instance infrastructure and component coordination
    /// - PlantGrowthSystem.cs: Growth progression, stage advancement, and yield calculation
    /// - PlantHealthSystem.cs: Health management, stress response, and disease resistance
    /// - PlantEnvironmentalSystem.cs: Environmental adaptation, condition processing, and GxE interactions
    /// - PlantGeneticsSystem.cs: Genetic expression, trait inheritance, and breeding value calculation
    /// - PlantVisualizationSystem.cs: Visual representation, size updates, and rendering coordination
    ///
    /// Represents an individual plant instance with genetic characteristics, growth state,
    /// environmental responses, and health status.
    /// </summary>
    public class PlantInstance : PlantInstanceCore, IPlantInstance
    {
        [Header("PlantInstance Legacy Settings")]
        [SerializeField] private bool _enableLegacyCompatibility = true;
        [SerializeField] private bool _enableSystemIntegration = true;
        [SerializeField] private bool _enableEventForwarding = true;
        // Legacy compatibility events
        public event Action<PlantInstance> OnGrowthStageChanged;
        public event Action<PlantInstance> OnHealthChanged;
        public event Action<PlantInstance> OnPlantDied;
        public event Action<PlantInstance> OnEnvironmentChanged;
        // Legacy compatibility properties
        public string PlantId { get => PlantID; set => PlantID = value; }
        public object PlantStrain { get => Strain; set => InitializeFromStrain(value); }
        public PlantGrowthStage CurrentStage { get => GrowthSystem?.CurrentGrowthStage ?? PlantGrowthStage.Seed; set => GrowthSystem?.SetGrowthStage(value); }
        public float Health { get => HealthSystem?.CurrentHealth ?? 1f; set => HealthSystem?.SetHealth(value); }
        public float WaterLevel { get => HealthSystem?.WaterLevel ?? 1f; set => HealthSystem?.SetWaterLevel(value); }
        public float NutrientLevel { get => HealthSystem?.NutrientLevel ?? 1f; set => HealthSystem?.SetNutrientLevel(value); }
        public object GeneticProfile { get => Strain; set => InitializeFromStrain(value); }
        public string StrainName
        {
            get => (Strain as ProjectChimera.Data.Cultivation.PlantStrainSO)?.StrainName ?? "Unknown";
            set => InitializeFromStrain(value);
        }
        public int TotalDaysGrown => (int)(GrowthSystem?.DaysSincePlanted ?? 0);
        // Additional legacy properties
        public PlantGrowthStage CurrentGrowthStage
        {
            get => GrowthSystem?.CurrentGrowthStage ?? PlantGrowthStage.Seed;
            set => GrowthSystem?.SetGrowthStage(value);
        }
        public float GrowthProgress
        {
            get => GrowthSystem?.GrowthProgress ?? 0f;
            set => GrowthSystem?.SetGrowthProgress(value);
        }
        public float OverallGrowthProgress => GrowthSystem?.OverallGrowthProgress ?? 0f;
        public int DaysSincePlanted => (int)(GrowthSystem?.DaysSincePlanted ?? 0);
        public Vector3 PlantSize => new Vector3(GrowthSystem?.PlantSize ?? 1f, GrowthSystem?.PlantSize ?? 1f, GrowthSystem?.PlantSize ?? 1f);
        public float CurrentHealth => HealthSystem?.CurrentHealth ?? 1f;
        public float MaxHealth => HealthSystem?.MaxHealth ?? 1f;
        public float StressLevel => HealthSystem?.StressLevel ?? 0f;
        public EnvironmentalConditions CurrentEnvironment => EnvironmentalSystem?.CurrentEnvironment ?? new EnvironmentalConditions();
        public float EnvironmentalFitness => EnvironmentalSystem?.EnvironmentalFitness ?? 1f;
        public Dictionary<string, object> ExpressedTraits => GeneticsSystem?.ExpressedTraits ?? new Dictionary<string, object>();
        public float YieldPotential => GrowthSystem?.YieldPotential ?? 1f;
        public float QualityPotential => GrowthSystem?.QualityPotential ?? 1f;
        public bool IsHarvestable => GrowthSystem?.IsHarvestable ?? false;
        // Missing properties from error messages
        public Vector3 Position { get; set; } = Vector3.zero;
        public int AgeInDays
        {
            get => DaysSincePlanted;
            set => GrowthSystem?.SetDaysSincePlanted(value);
        }
        public float Age
        {
            get => DaysSincePlanted;
            set => GrowthSystem?.SetDaysSincePlanted((int)value);
        }
        public float Height { get; set; } = 1.0f;
        public float Biomass { get; set; } = 1.0f;
        public PlantGrowthStage GrowthStage
        {
            get => CurrentGrowthStage;
            set => GrowthSystem?.SetGrowthStage(value);
        }
        public float Temperature { get; set; } = 22.0f;
        public float Humidity { get; set; } = 60.0f;
        public float LightIntensity { get; set; } = 400.0f;
        public float CO2Level { get; set; } = 400.0f;
        public DateTime LastWatering { get; set; } = DateTime.Now;
        public DateTime LastFeeding { get; set; } = DateTime.Now;
        public float Stress
        {
            get => StressLevel;
            set => HealthSystem?.SetStressLevel(value);
        }
        public float EnergyLevel { get; set; } = 1.0f;
        public EnvironmentalConditions EnvironmentData => CurrentEnvironment;
        public bool IsAlive => HealthSystem?.IsAlive() ?? true;
        // Genetic properties for compatibility
        public CannabisGenotype Genotype
        {
            get => GeneticsSystem?.Genotype;
            set { if (GeneticsSystem != null) GeneticsSystem.SetGenotype(value); }
        }
        public DateTime LastTraitExpression => GeneticsSystem?.LastTraitExpression ?? DateTime.Now;
        protected override void Awake()
        {
            base.Awake();

            if (_enableSystemIntegration)
            {
                SetupSystemIntegration();
            }
        }
        protected override void Start()
        {
            base.Start();

            if (_enableEventForwarding)
            {
                SetupEventForwarding();
            }
        }
        // Setup integration between plant systems
        private void SetupSystemIntegration()
        {
            // Integration between systems will be handled through the base PlantInstanceCore
            LogPlantAction("System integration configured");
        }
        // Setup event forwarding for legacy compatibility
        private void SetupEventForwarding()
        {
            // Forward growth system events
            if (GrowthSystem != null)
            {
                GrowthSystem.OnGrowthStageChanged += (fromStage, toStage) => OnGrowthStageChanged?.Invoke(this);
            }
            // Forward health system events
            if (HealthSystem != null)
            {
                HealthSystem.OnHealthChanged += (health) => OnHealthChanged?.Invoke(this);
                HealthSystem.OnPlantDied += () => OnPlantDied?.Invoke(this);
            }
            // Forward environmental system events
            if (EnvironmentalSystem != null)
            {
                EnvironmentalSystem.OnEnvironmentChanged += (conditions) => OnEnvironmentChanged?.Invoke(this);
            }
        }
        // Legacy method: Set the last trait expression result for this plant.
        public void SetLastTraitExpression(DateTime traitExpression)
        {
            GeneticsSystem?.SetLastTraitExpression(traitExpression);
        }
        // Legacy method: Apply height growth modifier based on genetic expression.
        public void ApplyHeightGrowthModifier(float heightModifier, float deltaTime = 0f)
        {
            GeneticsSystem?.ApplyHeightGrowthModifier(heightModifier);
        }
        // Legacy method: Apply potency modifier based on THC expression.
        public void ApplyPotencyModifier(float potencyModifier)
        {
            GeneticsSystem?.ApplyPotencyModifier(potencyModifier);
        }
        // Legacy method: Apply CBD modifier based on CBD expression.
        public void ApplyCBDModifier(float cbdModifier)
        {
            GeneticsSystem?.ApplyCBDModifier(cbdModifier);
        }
        // Legacy method: Apply yield modifier based on yield expression.
        public void ApplyYieldModifier(float yieldModifier)
        {
            GeneticsSystem?.ApplyYieldModifier(yieldModifier);
        }
        // Legacy method: Apply genetic fitness modifier based on overall fitness expression.
        public void ApplyGeneticFitnessModifier(float fitnessModifier)
        {
            GeneticsSystem?.ApplyGeneticFitnessModifier(fitnessModifier);
        }
        // Legacy method: Apply health change to the plant.
        public void ApplyHealthChange(float healthChange)
        {
            HealthSystem?.ApplyHealthChange(healthChange);
        }
        // Legacy method: Apply stress types to the plant.
        public void ApplyTemperatureStress(float stressSeverity, float deltaTime)
        {
            HealthSystem?.ApplyTemperatureStress(stressSeverity, deltaTime);
        }
        public void ApplyLightStress(float stressSeverity, float deltaTime)
        {
            HealthSystem?.ApplyLightStress(stressSeverity, deltaTime);
        }
        public void ApplyWaterStress(float stressSeverity, float deltaTime)
        {
            HealthSystem?.ApplyWaterStress(stressSeverity, deltaTime);
        }
        public void ApplyNutrientStress(float stressSeverity, float deltaTime)
        {
            HealthSystem?.ApplyNutrientStress(stressSeverity, deltaTime);
        }

        public void ApplyAtmosphericStress(float stressSeverity, float deltaTime)
        {
            HealthSystem?.ApplyAtmosphericStress(stressSeverity, deltaTime);
        }
        // Legacy method: Make the plant sprout (transition from seed to germination)
        public void Sprout()
        {
            GrowthSystem?.Sprout();
        }
        // Legacy method: Updates the plant's growth, health, and environmental responses.
        public void UpdatePlant(float deltaTime, float globalGrowthModifier = 1f)
        {
            // The base class UpdatePlantSystems handles this through the individual systems
            // But we provide this method for legacy compatibility
            if (IsActive && IsInitialized)
            {
                GrowthSystem?.ApplyGrowthRate(globalGrowthModifier);
            }
        }
        // Legacy method: Updates the plant's environmental conditions.
        public void UpdateEnvironmentalConditions(EnvironmentalConditions newConditions)
        {
            EnvironmentalSystem?.UpdateEnvironmentalConditions(newConditions);
        }
        // Legacy method: Gets the current environmental conditions for this plant.
        public EnvironmentalConditions GetCurrentEnvironmentalConditions()
        {
            return EnvironmentalSystem?.GetCurrentEnvironmentalConditions() ?? EnvironmentalConditions.CreateIndoorDefault();
        }
        // Legacy method: Updates environmental adaptation for this plant based on current conditions.
        public void UpdateEnvironmentalAdaptation(EnvironmentalConditions conditions)
        {
            EnvironmentalSystem?.ProcessAdaptation(0.01f);
        }
        // Legacy method: Applies stress to the plant.
        public bool ApplyStress(EnvironmentalStressSO stressSource, float intensity)
        {
            HealthSystem?.ApplyStress(intensity);
            return true;
        }
        // Legacy method: Removes a specific stress source.
        public void RemoveStress(EnvironmentalStressSO stressSource)
        {
            HealthSystem?.RemoveStress(1.0f); // Convert EnvironmentalStressSO to float value
        }
        // Legacy method: Checks if the plant has any active stressors.
        public bool HasActiveStressors()
        {
            return (HealthSystem?.ActiveStressors?.Count ?? 0) > 0;
        }
        // Legacy method: Harvests the plant and returns harvest results.
        public ProjectChimera.Data.Cultivation.HarvestResults Harvest()
        {
            float yieldAmount = GrowthSystem?.Harvest() ?? 0f;
            return new ProjectChimera.Data.Cultivation.HarvestResults(yieldAmount, 1.0f, PlantId, true);
        }
        // Legacy method: Advances the plant to the next growth stage if conditions are met.
        public bool AdvanceGrowthStage()
        {
            if (GrowthSystem != null)
            {
                GrowthSystem.AdvanceGrowthStage();
                return true;
            }
            return false;
        }
        // Legacy method: Calculates breeding value for genetic algorithms.
        public float CalculateBreedingValue()
        {
            return GeneticsSystem?.CalculateBreedingValue() ?? 0f;
        }
        // Legacy method: Creates a new plant instance from a strain definition.
        public static PlantInstance CreateFromStrain(object strain, Vector3 position, Transform parent = null)
        {
            var plantObject = new GameObject($"Plant_{GetStrainNameFromObject(strain)}_{GenerateShortID()}");
            plantObject.transform.position = position;

            if (parent != null)
                plantObject.transform.SetParent(parent);

            var plantInstance = plantObject.AddComponent<PlantInstance>();
            plantInstance.InitializeFromStrain(strain);

            return plantInstance;
        }
        // Legacy method: Apply growth rate modification
        public void ApplyGrowthRate(float growthRate, float deltaTime)
        {
            GrowthSystem?.ApplyGrowthRate(growthRate);
        }
        // Legacy method: Set current health value
        public void SetCurrentHealth(float health)
        {
            HealthSystem?.SetCurrentHealth(health);
        }
        // Legacy method: Set environmental fitness value
        public void SetEnvironmentalFitness(float fitness)
        {
            EnvironmentalSystem?.SetEnvironmentalFitness(fitness);
        }
        // Legacy method: Water the plant (for PlantCareManager compatibility)
        public bool Water(float waterAmount, DateTime timestamp)
        {
            if (HealthSystem == null) return false;

            HealthSystem.SetWaterLevel(Mathf.Clamp01(WaterLevel + waterAmount));
            LastWatering = timestamp;
            LogPlantAction($"Plant watered: {waterAmount:F2}");
            return true;
        }
        // Legacy method: Feed the plant nutrients (for PlantCareManager compatibility)
        public bool Feed(float nutrientAmount, DateTime timestamp)
        {
            if (HealthSystem == null) return false;

            HealthSystem.SetNutrientLevel(Mathf.Clamp01(NutrientLevel + nutrientAmount));
            LastFeeding = timestamp;
            LogPlantAction($"Plant fed: {nutrientAmount:F2}");
            return true;
        }
        // Legacy method: Apply training to the plant (for PlantCareManager compatibility)
        public bool ApplyTraining(string trainingType, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(trainingType) || HealthSystem == null) return false;

            // Reduce stress as training helps plant management
            HealthSystem.SetStressLevel(Mathf.Max(0f, StressLevel - 0.1f));
            LogPlantAction($"Training applied: {trainingType}");
            return true;
        }
        // Get strain name from strain object
        protected static string GetStrainNameFromObject(object strain)
        {
            return (strain as ProjectChimera.Data.Cultivation.PlantStrainSO)?.StrainName ?? "Unknown";
        }
        // Generate short ID for plant naming
        protected static string GenerateShortID()
        {
            return UnityEngine.Random.Range(1000, 9999).ToString();
        }
        // Get comprehensive plant metrics from all systems
        public PlantInstanceMetrics GetComprehensivePlantMetrics()
        {
            var baseMetrics = GetPlantMetrics();

            return new PlantInstanceMetrics
            {
                PlantID = baseMetrics.PlantID,
                PlantName = baseMetrics.PlantName,
                IsActive = baseMetrics.IsActive,
                IsInitialized = baseMetrics.IsInitialized,
                InitializationTime = baseMetrics.InitializationTime,
                DaysSincePlanted = baseMetrics.DaysSincePlanted,
                SystemCount = baseMetrics.SystemCount,
                GrowthSystemActive = baseMetrics.GrowthSystemActive,
                HealthSystemActive = baseMetrics.HealthSystemActive,
                EnvironmentalSystemActive = baseMetrics.EnvironmentalSystemActive,
                GeneticsSystemActive = baseMetrics.GeneticsSystemActive,
                VisualizationSystemActive = baseMetrics.VisualizationSystemActive,

                // Additional metrics from individual systems
                GrowthMetrics = GrowthSystem?.GetGrowthMetrics() as PlantGrowthMetrics,
                HealthMetrics = HealthSystem?.GetHealthMetrics() as PlantHealthMetrics,
                EnvironmentalMetrics = EnvironmentalSystem?.GetEnvironmentalMetrics(),
                GeneticsMetrics = GeneticsSystem?.GetGeneticsMetrics() as PlantGeneticsMetrics,
                VisualizationMetrics = VisualizationSystem?.GetVisualizationMetrics() as PlantVisualizationMetrics
            };
        }

        #region IPlantInstance Interface Implementation

        // IPlantInstance required properties
        string IPlantInstance.Id => PlantID;
        string IPlantInstance.StrainName => StrainName;
        Vector3 IPlantInstance.Position => Position;
        PlantGrowthStage IPlantInstance.GrowthStage => CurrentGrowthStage;
        float IPlantInstance.Health => CurrentHealth;
        float IPlantInstance.Age => DaysSincePlanted;
        bool IPlantInstance.IsAlive => IsAlive;

        // Initialize plant with strain and position (IPlantInstance interface implementation)
        public void Initialize(ProjectChimera.Systems.Cultivation.PlantStrainSO strain, Vector3 position)
        {
            InitializeFromStrain(strain);
            Position = position;
        }
        // Update plant growth based on delta time (IPlantInstance interface implementation)
        public void UpdateGrowth(float deltaTime)
        {
            GrowthSystem?.UpdateGrowthProgress(deltaTime);
        }
        // Apply environmental effects to the plant (IPlantInstance interface implementation)
        public void ApplyEnvironmentalEffects(ProjectChimera.Systems.Cultivation.EnvironmentalData environmentalData)
        {
            if (environmentalData == null || EnvironmentalSystem == null) return;

            // Convert EnvironmentalData to EnvironmentalConditions
            var conditions = new EnvironmentalConditions
            {
                Temperature = environmentalData.Temperature,
                Humidity = environmentalData.Humidity,
                LightIntensity = environmentalData.LightIntensity,
                CO2Level = environmentalData.CO2Level
            };

            EnvironmentalSystem.UpdateEnvironmentalConditions(conditions);
            EnvironmentalSystem.ProcessEnvironmentalConditions(Time.deltaTime);
        }
        // Harvest the plant (IPlantInstance interface implementation)
        void IPlantInstance.Harvest()
        {
            // Mark plant as harvested and trigger harvest events
            if (GrowthSystem != null)
            {
                GrowthSystem.SetGrowthStage(PlantGrowthStage.Harvested);
            }

            // Trigger legacy harvest event
            OnPlantDied?.Invoke(this);

            // Log harvest action
            LogPlantAction($"Plant {PlantID} harvested");
        }
        // Destroy the plant instance (IPlantInstance interface implementation)
        public void Destroy()
        {
            OnPlantDied?.Invoke(this);
            LogPlantAction($"Plant {PlantID} destroyed");

            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        #endregion

        #if UNITY_EDITOR
        // Editor-only method for testing plant instance
        [UnityEngine.ContextMenu("Test Plant Instance")]
        private void TestPlantInstance()
        {
            if (UnityEngine.Application.isPlaying)
            {
                LogPlantAction("Testing plant instance...");
                LogPlantAction($"Plant ID: {PlantID}");
                LogPlantAction($"Strain: {StrainName}");
                LogPlantAction($"Growth Stage: {CurrentGrowthStage}");
                LogPlantAction($"Health: {CurrentHealth:F2}");
                LogPlantAction($"Environmental Fitness: {EnvironmentalFitness:F2}");
                LogPlantAction($"Systems Active: G:{(GrowthSystem != null ? "Y" : "N")} H:{(HealthSystem != null ? "Y" : "N")} E:{(EnvironmentalSystem != null ? "Y" : "N")} G:{(GeneticsSystem != null ? "Y" : "N")} V:{(VisualizationSystem != null ? "Y" : "N")}");
            }
            else
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }
        #endif

        // Extended plant instance metrics data structure
        public new class PlantInstanceMetrics : PlantInstanceCore.PlantInstanceMetrics
        {
            public PlantGrowthMetrics GrowthMetrics;
            public PlantHealthMetrics HealthMetrics;
            public PlantEnvironmentalMetrics EnvironmentalMetrics;
            public PlantGeneticsMetrics GeneticsMetrics;
            public PlantVisualizationMetrics VisualizationMetrics;
        }
    }

    /// <summary>
    /// GxE response data for environmental interactions.
    /// Maintained for legacy compatibility.
    /// </summary>
    [System.Serializable]
    public class GxEResponseData
    {
        public float TemperatureResponse = 1f;
        public float HumidityResponse = 1f;
        public float LightResponse = 1f;
        public float NutrientResponse = 1f;
        public float CO2Response = 1f;
    }
}
