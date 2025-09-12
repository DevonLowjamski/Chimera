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
    public class PlantInstance : PlantInstanceCore
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
        public string PlantId => PlantID;
        public object PlantStrain { get => Strain; set => InitializeFromStrain(value); }
        public PlantGrowthStage CurrentStage { get => GrowthSystem?.CurrentGrowthStage ?? PlantGrowthStage.Seed; set => GrowthSystem?.SetGrowthStage(value); }
        public float Health => HealthSystem?.CurrentHealth ?? 1f;
        public float WaterLevel { get => HealthSystem?.WaterLevel ?? 1f; set => HealthSystem?.SetWaterLevel(value); }
        public float NutrientLevel { get => HealthSystem?.NutrientLevel ?? 1f; set => HealthSystem?.SetNutrientLevel(value); }
        public object GeneticProfile { get => Strain; set => InitializeFromStrain(value); }
        public string StrainName => (Strain as ProjectChimera.Data.Cultivation.PlantStrainSO)?.StrainName ?? "Unknown";
        public int TotalDaysGrown => GrowthSystem?.DaysSincePlanted ?? 0;

        // Additional legacy properties
        public PlantGrowthStage CurrentGrowthStage => GrowthSystem?.CurrentGrowthStage ?? PlantGrowthStage.Seed;
        public float GrowthProgress => GrowthSystem?.GrowthProgress ?? 0f;
        public float OverallGrowthProgress => GrowthSystem?.OverallGrowthProgress ?? 0f;
        public int DaysSincePlanted => GrowthSystem?.DaysSincePlanted ?? 0;
        public Vector3 PlantSize => GrowthSystem?.PlantSize ?? Vector3.one;
        public float CurrentHealth => HealthSystem?.CurrentHealth ?? 1f;
        public float MaxHealth => HealthSystem?.MaxHealth ?? 1f;
        public float StressLevel => HealthSystem?.StressLevel ?? 0f;
        public EnvironmentalConditions CurrentEnvironment => EnvironmentalSystem?.CurrentEnvironment ?? new EnvironmentalConditions();
        public float EnvironmentalFitness => EnvironmentalSystem?.EnvironmentalFitness ?? 1f;
        public PhenotypicTraits ExpressedTraits => GeneticsSystem?.ExpressedTraits;
        public float YieldPotential => GrowthSystem?.YieldPotential ?? 1f;
        public float QualityPotential => GrowthSystem?.QualityPotential ?? 1f;
        public bool IsHarvestable => GrowthSystem?.IsHarvestable ?? false;

        // Genetic properties for compatibility
        public GenotypeDataSO Genotype
        {
            get => GeneticsSystem?.Genotype;
            set { if (GeneticsSystem != null) GeneticsSystem.GetType().GetField("_genotype", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(GeneticsSystem, value); }
        }

        public TraitExpressionResult LastTraitExpression => GeneticsSystem?.LastTraitExpression;

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

        /// <summary>
        /// Setup integration between plant systems
        /// </summary>
        private void SetupSystemIntegration()
        {
            // Integration between systems will be handled through the base PlantInstanceCore
            LogPlantAction("System integration configured");
        }

        /// <summary>
        /// Setup event forwarding for legacy compatibility
        /// </summary>
        private void SetupEventForwarding()
        {
            // Forward growth system events
            if (GrowthSystem != null)
            {
                GrowthSystem.OnGrowthStageChanged += (stage) => OnGrowthStageChanged?.Invoke(this);
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

        /// <summary>
        /// Legacy method: Set the last trait expression result for this plant.
        /// </summary>
        public void SetLastTraitExpression(TraitExpressionResult traitExpression)
        {
            GeneticsSystem?.SetLastTraitExpression(traitExpression);
        }

        /// <summary>
        /// Legacy method: Apply height growth modifier based on genetic expression.
        /// </summary>
        public void ApplyHeightGrowthModifier(float heightModifier, float deltaTime)
        {
            GeneticsSystem?.ApplyHeightGrowthModifier(heightModifier, deltaTime);
        }

        /// <summary>
        /// Legacy method: Apply potency modifier based on THC expression.
        /// </summary>
        public void ApplyPotencyModifier(float potencyModifier)
        {
            GeneticsSystem?.ApplyPotencyModifier(potencyModifier);
        }

        /// <summary>
        /// Legacy method: Apply CBD modifier based on CBD expression.
        /// </summary>
        public void ApplyCBDModifier(float cbdModifier)
        {
            GeneticsSystem?.ApplyCBDModifier(cbdModifier);
        }

        /// <summary>
        /// Legacy method: Apply yield modifier based on yield expression.
        /// </summary>
        public void ApplyYieldModifier(float yieldModifier)
        {
            GeneticsSystem?.ApplyYieldModifier(yieldModifier);
        }

        /// <summary>
        /// Legacy method: Apply genetic fitness modifier based on overall fitness expression.
        /// </summary>
        public void ApplyGeneticFitnessModifier(float fitnessModifier)
        {
            GeneticsSystem?.ApplyGeneticFitnessModifier(fitnessModifier);
        }

        /// <summary>
        /// Legacy method: Apply health change to the plant.
        /// </summary>
        public void ApplyHealthChange(float healthChange)
        {
            HealthSystem?.ApplyHealthChange(healthChange);
        }

        /// <summary>
        /// Legacy method: Apply stress types to the plant.
        /// </summary>
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

        /// <summary>
        /// Legacy method: Make the plant sprout (transition from seed to germination)
        /// </summary>
        public void Sprout()
        {
            GrowthSystem?.Sprout();
        }

        /// <summary>
        /// Legacy method: Updates the plant's growth, health, and environmental responses.
        /// </summary>
        public void UpdatePlant(float deltaTime, float globalGrowthModifier = 1f)
        {
            // The base class UpdatePlantSystems handles this through the individual systems
            // But we provide this method for legacy compatibility
            if (IsActive && IsInitialized)
            {
                GrowthSystem?.ApplyGrowthRate(globalGrowthModifier, deltaTime);
            }
        }

        /// <summary>
        /// Legacy method: Updates the plant's environmental conditions.
        /// </summary>
        public void UpdateEnvironmentalConditions(EnvironmentalConditions newConditions)
        {
            EnvironmentalSystem?.UpdateEnvironmentalConditions(newConditions);
        }

        /// <summary>
        /// Legacy method: Gets the current environmental conditions for this plant.
        /// </summary>
        public EnvironmentalConditions GetCurrentEnvironmentalConditions()
        {
            return EnvironmentalSystem?.GetCurrentEnvironmentalConditions() ?? EnvironmentalConditions.CreateIndoorDefault();
        }

        /// <summary>
        /// Legacy method: Updates environmental adaptation for this plant based on current conditions.
        /// </summary>
        public void UpdateEnvironmentalAdaptation(EnvironmentalConditions conditions)
        {
            EnvironmentalSystem?.ProcessAdaptation(conditions, 0.01f);
        }

        /// <summary>
        /// Legacy method: Applies stress to the plant.
        /// </summary>
        public bool ApplyStress(EnvironmentalStressSO stressSource, float intensity)
        {
            return HealthSystem?.ApplyStress(stressSource, intensity) ?? false;
        }

        /// <summary>
        /// Legacy method: Removes a specific stress source.
        /// </summary>
        public void RemoveStress(EnvironmentalStressSO stressSource)
        {
            HealthSystem?.RemoveStress(stressSource);
        }

        /// <summary>
        /// Legacy method: Checks if the plant has any active stressors.
        /// </summary>
        public bool HasActiveStressors()
        {
            return (HealthSystem?.ActiveStressors?.Count ?? 0) > 0;
        }

        /// <summary>
        /// Legacy method: Harvests the plant and returns harvest results.
        /// </summary>
        public HarvestResults Harvest()
        {
            return GrowthSystem?.Harvest();
        }

        /// <summary>
        /// Legacy method: Advances the plant to the next growth stage if conditions are met.
        /// </summary>
        public bool AdvanceGrowthStage()
        {
            return GrowthSystem?.AdvanceGrowthStage() ?? false;
        }

        /// <summary>
        /// Legacy method: Calculates breeding value for genetic algorithms.
        /// </summary>
        public float CalculateBreedingValue()
        {
            return GeneticsSystem?.CalculateBreedingValue() ?? 0f;
        }

        /// <summary>
        /// Legacy method: Creates a new plant instance from a strain definition.
        /// </summary>
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

        /// <summary>
        /// Legacy method: Apply growth rate modification
        /// </summary>
        public void ApplyGrowthRate(float growthRate, float deltaTime)
        {
            GrowthSystem?.ApplyGrowthRate(growthRate, deltaTime);
        }

        /// <summary>
        /// Legacy method: Set current health value
        /// </summary>
        public void SetCurrentHealth(float health)
        {
            HealthSystem?.SetCurrentHealth(health);
        }

        /// <summary>
        /// Legacy method: Set environmental fitness value
        /// </summary>
        public void SetEnvironmentalFitness(float fitness)
        {
            EnvironmentalSystem?.SetEnvironmentalFitness(fitness);
        }

        /// <summary>
        /// Get strain name from strain object
        /// </summary>
        protected static string GetStrainNameFromObject(object strain)
        {
            return (strain as ProjectChimera.Data.Cultivation.PlantStrainSO)?.StrainName ?? "Unknown";
        }

        /// <summary>
        /// Generate short ID for plant naming
        /// </summary>
        protected static string GenerateShortID()
        {
            return UnityEngine.Random.Range(1000, 9999).ToString();
        }

        /// <summary>
        /// Get comprehensive plant metrics from all systems
        /// </summary>
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
                GrowthMetrics = GrowthSystem?.GetGrowthMetrics(),
                HealthMetrics = HealthSystem?.GetHealthMetrics(),
                EnvironmentalMetrics = EnvironmentalSystem?.GetEnvironmentalMetrics(),
                GeneticsMetrics = GeneticsSystem?.GetGeneticsMetrics(),
                VisualizationMetrics = VisualizationSystem?.GetVisualizationMetrics()
            };
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method for testing plant instance
        /// </summary>
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
                ChimeraLogger.Log("[PlantInstance] Test only works during play mode");
            }
        }
        #endif

        /// <summary>
        /// Extended plant instance metrics data structure
        /// </summary>
        public new class PlantInstanceMetrics : PlantInstanceCore.PlantInstanceMetrics
        {
            public PlantGrowthSystem.PlantGrowthMetrics GrowthMetrics;
            public PlantHealthSystem.PlantHealthMetrics HealthMetrics;
            public PlantEnvironmentalSystem.PlantEnvironmentalMetrics EnvironmentalMetrics;
            public PlantGeneticsSystem.PlantGeneticsMetrics GeneticsMetrics;
            public PlantVisualizationSystem.PlantVisualizationMetrics VisualizationMetrics;
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