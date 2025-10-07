using Unity.Collections;
using Unity.Jobs;
#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif
#if CHIMERA_BURST
using Unity.Burst;
#endif
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation.Jobs
{
    /// <summary>
    /// PERFORMANCE: Burst-compiled job for high-performance plant growth calculations
    /// Processes multiple plants in parallel using Unity's Job System
    /// Week 9 Day 1-3: Jobs System & Performance Foundations
    /// </summary>
    public struct PlantGrowthJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> deltaTimeArray;
        [ReadOnly] public NativeArray<PlantGrowthParameters> growthParams;
        [ReadOnly] public NativeArray<EnvironmentalData> environmentalData;

        public NativeArray<PlantGrowthData> growthData;
        public NativeArray<PlantHealthData> healthData;
        public NativeArray<PlantResourceData> resourceData;

        public void Execute(int index)
        {
            var deltaTime = deltaTimeArray[0];
            var parameters = growthParams[index];
            var environment = environmentalData[index];

            var growth = growthData[index];
            var health = healthData[index];
            var resources = resourceData[index];

            // Update plant resources
            resources = UpdateResources(resources, parameters, environment, deltaTime);

            // Calculate environmental stress factors
            health = CalculateHealth(health, environment, parameters, resources, deltaTime);

            // Calculate growth based on health and environment
            growth = CalculateGrowth(growth, parameters, health, environment, deltaTime);

            // Write back results
            growthData[index] = growth;
            healthData[index] = health;
            resourceData[index] = resources;
        }

        /// <summary>
        /// Update plant resource levels
        /// </summary>
        private static PlantResourceData UpdateResources(
            PlantResourceData resources,
            PlantGrowthParameters parameters,
            EnvironmentalData environment,
            float deltaTime)
        {
            // Water consumption based on stage and environmental demand
            float waterConsumption = CalculateWaterConsumption(parameters, environment, deltaTime);
            resources.water = UnityEngine.Mathf.Max(0f, resources.water - waterConsumption);

            // Nutrient consumption
            float nutrientConsumption = CalculateNutrientConsumption(parameters, deltaTime);
            resources.nutrients = UnityEngine.Mathf.Max(0f, resources.nutrients - nutrientConsumption);

            // Energy production from photosynthesis
            float energyProduction = CalculateEnergyProduction(environment, parameters, deltaTime);
            resources.energy = UnityEngine.Mathf.Min(1f, resources.energy + energyProduction);

            // Update timing
            resources.lastWateringDelta += deltaTime;
            resources.lastFeedingDelta += deltaTime;

            return resources;
        }

        /// <summary>
        /// Calculate plant health from various stress factors
        /// </summary>
        private static PlantHealthData CalculateHealth(
            PlantHealthData health,
            EnvironmentalData environment,
            PlantGrowthParameters parameters,
            PlantResourceData resources,
            float deltaTime)
        {
            // Water stress
            float waterOptimal = parameters.waterRequirement;
            health.waterStress = CalculateStressFromResource(resources.water, waterOptimal);

            // Light stress
            float lightOptimal = parameters.lightRequirement;
            health.lightStress = CalculateStressFromEnvironment(environment.lightIntensity, lightOptimal);

            // Temperature stress
            health.temperatureStress = CalculateTemperatureStress(environment.temperature, parameters);

            // Overall stress calculation
            health.stress = (health.waterStress + health.lightStress + health.temperatureStress) / 3f;

            // Overall health calculation (1 - stress, modified by resilience)
            float targetHealth = UnityEngine.Mathf.Clamp(1f - health.stress, 0.1f, 1f);
            float healthChange = (targetHealth - health.overall) * parameters.resilience * deltaTime;
            health.overall = UnityEngine.Mathf.Clamp(health.overall + healthChange, 0f, 1f);

            // Nutrient level affects health
            health.nutrientLevel = resources.nutrients;

            return health;
        }

        /// <summary>
        /// Calculate plant growth based on health and environmental factors
        /// </summary>
        private static PlantGrowthData CalculateGrowth(
            PlantGrowthData growth,
            PlantGrowthParameters parameters,
            PlantHealthData health,
            EnvironmentalData environment,
            float deltaTime)
        {
            // Base growth rate modified by stage
            float stageModifier = GetStageGrowthModifier(growth.currentStage);
            float baseGrowthRate = parameters.baseGrowthRate * stageModifier;

            // Environmental modifiers
            float lightModifier = UnityEngine.Mathf.Clamp(environment.lightIntensity / 100f, 0.1f, 2.0f);
            float waterModifier = UnityEngine.Mathf.Clamp(environment.waterLevel / 100f, 0.1f, 1.5f);
            float tempModifier = UnityEngine.Mathf.Clamp((environment.temperature - parameters.temperatureOptimal) / 10f + 1f, 0.5f, 1.5f);

            // Health modifier
            float healthModifier = UnityEngine.Mathf.Lerp(0.1f, 1.2f, health.overall);

            // Calculate height growth
            float heightGrowth = baseGrowthRate * deltaTime * lightModifier * waterModifier * tempModifier * healthModifier;
            growth.height += heightGrowth;

            // Calculate biomass growth
            float biomassGrowth = parameters.biomassRate * deltaTime * healthModifier;
            growth.biomass += biomassGrowth;

            // Age progression
            growth.age += deltaTime;

            // Check for stage transition
            growth = CheckStageTransition(growth);

            // Update growth progress within current stage
            growth.growthProgress = CalculateStageProgress(growth);

            return growth;
        }

        /// <summary>
        /// Calculate water consumption rate
        /// </summary>
        private static float CalculateWaterConsumption(PlantGrowthParameters parameters, EnvironmentalData environment, float deltaTime)
        {
            float baseConsumption = 0.01f * deltaTime; // 1% per time unit
            float temperatureMultiplier = UnityEngine.Mathf.Clamp(environment.temperature / 25f, 0.5f, 2f);
            float lightMultiplier = UnityEngine.Mathf.Clamp(environment.lightIntensity / 100f, 0.3f, 1.5f);

            return baseConsumption * temperatureMultiplier * lightMultiplier;
        }

        /// <summary>
        /// Calculate nutrient consumption rate
        /// </summary>
        private static float CalculateNutrientConsumption(PlantGrowthParameters parameters, float deltaTime)
        {
            float stageMultiplier = GetStageGrowthModifier(parameters.plantStage);
            return 0.005f * deltaTime * stageMultiplier; // 0.5% base per time unit
        }

        /// <summary>
        /// Calculate energy production from photosynthesis
        /// </summary>
        private static float CalculateEnergyProduction(EnvironmentalData environment, PlantGrowthParameters parameters, float deltaTime)
        {
            float lightEfficiency = UnityEngine.Mathf.Clamp(environment.lightIntensity / 100f, 0f, 1f);
            float co2Efficiency = UnityEngine.Mathf.Clamp(environment.co2Level / 400f, 0.5f, 1.2f);

            return 0.02f * deltaTime * lightEfficiency * co2Efficiency; // 2% base per time unit
        }

        /// <summary>
        /// Calculate stress from resource availability
        /// </summary>
        private static float CalculateStressFromResource(float resourceLevel, float optimalLevel)
        {
            float deviation = UnityEngine.Mathf.Abs(resourceLevel - optimalLevel);
            return UnityEngine.Mathf.Clamp(deviation / optimalLevel, 0f, 1f);
        }

        /// <summary>
        /// Calculate stress from environmental conditions
        /// </summary>
        private static float CalculateStressFromEnvironment(float currentValue, float optimalValue)
        {
            float deviation = UnityEngine.Mathf.Abs(currentValue - optimalValue);
            return UnityEngine.Mathf.Clamp(deviation / optimalValue, 0f, 1f);
        }

        /// <summary>
        /// Calculate temperature-specific stress
        /// </summary>
        private static float CalculateTemperatureStress(float temperature, PlantGrowthParameters parameters)
        {
            float deviation = UnityEngine.Mathf.Abs(temperature - parameters.temperatureOptimal);
            float maxDeviation = parameters.temperatureTolerance;

            return UnityEngine.Mathf.Clamp(deviation / maxDeviation, 0f, 1f);
        }

        /// <summary>
        /// Get growth rate modifier for different growth stages
        /// </summary>
        private static float GetStageGrowthModifier(int stage)
        {
            return stage switch
            {
                0 => 0.5f,  // Seedling
                1 => 1.0f,  // Vegetative
                2 => 0.3f,  // Flowering
                _ => 0.1f   // Default/unknown
            };
        }

        /// <summary>
        /// Check if plant should transition to next growth stage
        /// </summary>
        private static PlantGrowthData CheckStageTransition(PlantGrowthData growth)
        {
            bool shouldTransition = false;
            int newStage = growth.currentStage;

            switch (growth.currentStage)
            {
                case 0: // Seedling
                    if (growth.age >= 14f) // 14 days
                    {
                        newStage = 1; // Vegetative
                        shouldTransition = true;
                    }
                    break;
                case 1: // Vegetative
                    if (growth.age >= 44f) // 44 days total (30 in vegetative)
                    {
                        newStage = 2; // Flowering
                        shouldTransition = true;
                    }
                    break;
                case 2: // Flowering
                    // Stay in flowering
                    break;
            }

            if (shouldTransition)
            {
                growth.currentStage = newStage;
                growth.growthProgress = 0f; // Reset progress for new stage
            }

            return growth;
        }

        /// <summary>
        /// Calculate progress within current growth stage
        /// </summary>
        private static float CalculateStageProgress(PlantGrowthData growth)
        {
            float stageStartAge = growth.currentStage switch
            {
                0 => 0f,   // Seedling starts at 0
                1 => 14f,  // Vegetative starts at 14 days
                2 => 44f,  // Flowering starts at 44 days
                _ => 0f
            };

            float stageDuration = growth.currentStage switch
            {
                0 => 14f,  // Seedling lasts 14 days
                1 => 30f,  // Vegetative lasts 30 days
                2 => float.MaxValue, // Flowering indefinite
                _ => 1f
            };

            if (stageDuration == float.MaxValue) return 1f;

            float ageInStage = growth.age - stageStartAge;
            return UnityEngine.Mathf.Clamp(ageInStage / stageDuration, 0f, 1f);
        }
    }
}
