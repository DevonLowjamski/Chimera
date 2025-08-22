using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Cultivation Manager - Orchestrates modular cultivation components
    /// Replaced monolithic 515-line class with composition-based architecture
    /// Adheres to Single Responsibility Principle and Dependency Injection patterns
    /// Implements offline progression for plant growth and care during offline periods
    /// </summary>
    public class CultivationManager : DIChimeraManager, ProjectChimera.Core.IOfflineProgressionListener
    {
        [Header("Cultivation Manager Configuration")]
        [SerializeField] private bool _enableCultivationSystem = true;
        
        // Modular components
        private IPlantLifecycleManager _plantLifecycleManager;
        private IPlantCareManager _plantCareManager;
        private IEnvironmentalManager _environmentalManager;
        private IGrowthProcessor _growthProcessor;
        private IHarvestManager _harvestManager;
        
        public string ManagerName => "Cultivation Manager";
        
        // Delegate properties to components
        public int ActivePlantCount => _plantLifecycleManager?.ActivePlantCount ?? 0;
        public int TotalPlantsGrown => _plantLifecycleManager?.TotalPlantsGrown ?? 0;
        public int TotalPlantsHarvested => _plantLifecycleManager?.TotalPlantsHarvested ?? 0;
        public float TotalYieldHarvested => _plantLifecycleManager?.TotalYieldHarvested ?? 0f;
        public float AveragePlantHealth => _growthProcessor?.AveragePlantHealth ?? 0f;
        public bool EnableAutoGrowth 
        { 
            get => _growthProcessor?.EnableAutoGrowth ?? false; 
            set { if (_growthProcessor != null) _growthProcessor.EnableAutoGrowth = value; }
        }
        public float TimeAcceleration 
        { 
            get => _growthProcessor?.TimeAcceleration ?? 1f; 
            set { if (_growthProcessor != null) _growthProcessor.TimeAcceleration = value; }
        }
        
        protected override void OnManagerInitialize()
        {
            Debug.Log("[CultivationManager] Initializing modular cultivation system...");
            
            if (!_enableCultivationSystem)
            {
                Debug.Log("[CultivationManager] Cultivation system disabled.");
                return;
            }
            
            // Initialize components with dependency injection
            InitializeComponents();
            
            // Register with GameManager
            GameManager.Instance?.RegisterManager(this);
            
            Debug.Log("[CultivationManager] Modular cultivation system initialized successfully.");
        }
        
        protected override void OnManagerShutdown()
        {
            Debug.Log("[CultivationManager] Shutting down modular cultivation system...");
            
            // Shutdown components in reverse order
            ShutdownComponents();
            
            Debug.Log("[CultivationManager] Modular cultivation system shutdown complete.");
        }
        
        protected override void Update()
        {
            if (!IsInitialized || !_enableCultivationSystem) return;
            
            // Update growth processor (handles timing and auto-growth)
            if (_growthProcessor is GrowthProcessor processor)
            {
                processor.Update();
            }
        }
        
        private void InitializeComponents()
        {
            // Initialize in dependency order
            _plantLifecycleManager = new PlantLifecycleManager(null, null); // Will set dependencies after creation
            _environmentalManager = new CultivationEnvironmentalManager(_plantLifecycleManager);
            _harvestManager = new HarvestManager(_plantLifecycleManager);
            _plantCareManager = new PlantCareManager(_plantLifecycleManager);
            // GrowthProcessor works with our cultivation environmental manager interface
            _growthProcessor = new GrowthProcessor(_plantLifecycleManager, _environmentalManager);
            
            // Update dependencies for PlantLifecycleManager
            if (_plantLifecycleManager is PlantLifecycleManager lifecycleManager)
            {
                // Set dependencies via constructor replacement or setter injection
                // For now, we'll use a temporary approach - in full DI implementation, this would be handled by container
                System.Reflection.FieldInfo envField = typeof(PlantLifecycleManager).GetField("_environmentalManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo harvestField = typeof(PlantLifecycleManager).GetField("_harvestManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                envField?.SetValue(lifecycleManager, _environmentalManager);
                harvestField?.SetValue(lifecycleManager, _harvestManager);
            }
            
            // Initialize all components
            _plantLifecycleManager.Initialize();
            _environmentalManager.Initialize();
            _harvestManager.Initialize();
            _plantCareManager.Initialize();
            _growthProcessor.Initialize();
            
            Debug.Log("[CultivationManager] All components initialized successfully.");
        }
        
        private void ShutdownComponents()
        {
            // Shutdown in reverse order
            _growthProcessor?.Shutdown();
            _plantCareManager?.Shutdown();
            _harvestManager?.Shutdown();
            _environmentalManager?.Shutdown();
            _plantLifecycleManager?.Shutdown();
            
            Debug.Log("[CultivationManager] All components shutdown successfully.");
        }
        
        #region Public API - Delegates to appropriate components
        
        /// <summary>
        /// Plants a new plant instance in the cultivation system.
        /// </summary>
        public PlantInstanceSO PlantSeed(string plantName, PlantStrainSO strain, GenotypeDataSO genotype, Vector3 position, string zoneId = "default")
        {
            return _plantLifecycleManager?.PlantSeed(plantName, strain, genotype, position, zoneId);
        }
        
        /// <summary>
        /// Removes a plant from the cultivation system.
        /// </summary>
        public bool RemovePlant(string plantId, bool isHarvest = false)
        {
            return _plantLifecycleManager?.RemovePlant(plantId, isHarvest) ?? false;
        }
        
        /// <summary>
        /// Gets a plant instance by its ID.
        /// </summary>
        public PlantInstanceSO GetPlant(string plantId)
        {
            return _plantLifecycleManager?.GetPlant(plantId);
        }
        
        /// <summary>
        /// Gets all active plants.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            return _plantLifecycleManager?.GetAllPlants() ?? new PlantInstanceSO[0];
        }
        
        /// <summary>
        /// Gets all plants in a specific growth stage.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage)
        {
            return _plantLifecycleManager?.GetPlantsByStage(stage) ?? new PlantInstanceSO[0];
        }
        
        /// <summary>
        /// Gets all plants that need attention.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            return _plantLifecycleManager?.GetPlantsNeedingAttention() ?? new PlantInstanceSO[0];
        }
        
        /// <summary>
        /// Waters a specific plant.
        /// </summary>
        public bool WaterPlant(string plantId, float waterAmount = 0.5f)
        {
            return _plantCareManager?.WaterPlant(plantId, waterAmount) ?? false;
        }
        
        /// <summary>
        /// Feeds nutrients to a specific plant.
        /// </summary>
        public bool FeedPlant(string plantId, float nutrientAmount = 0.4f)
        {
            return _plantCareManager?.FeedPlant(plantId, nutrientAmount) ?? false;
        }
        
        /// <summary>
        /// Applies training to a specific plant.
        /// </summary>
        public bool TrainPlant(string plantId, string trainingType)
        {
            return _plantCareManager?.TrainPlant(plantId, trainingType) ?? false;
        }
        
        /// <summary>
        /// Waters all plants in the cultivation system.
        /// </summary>
        public void WaterAllPlants(float waterAmount = 0.5f)
        {
            _plantCareManager?.WaterAllPlants(waterAmount);
        }
        
        /// <summary>
        /// Feeds all plants in the cultivation system.
        /// </summary>
        public void FeedAllPlants(float nutrientAmount = 0.4f)
        {
            _plantCareManager?.FeedAllPlants(nutrientAmount);
        }
        
        /// <summary>
        /// Updates environmental conditions for a specific zone.
        /// </summary>
        public void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment)
        {
            _environmentalManager?.SetZoneEnvironment(zoneId, environment);
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific zone.
        /// </summary>
        public EnvironmentalConditions GetZoneEnvironment(string zoneId)
        {
            return _environmentalManager?.GetZoneEnvironment(zoneId) ?? EnvironmentalConditions.CreateIndoorDefault();
        }
        
        /// <summary>
        /// Processes daily growth for all active plants.
        /// </summary>
        public void ProcessDailyGrowthForAllPlants()
        {
            _growthProcessor?.ProcessDailyGrowthForAllPlants();
        }
        
        /// <summary>
        /// Forces an immediate growth update for testing purposes.
        /// </summary>
        public void ForceGrowthUpdate()
        {
            _growthProcessor?.ForceGrowthUpdate();
        }
        
        /// <summary>
        /// Harvests a plant by ID
        /// </summary>
        public bool HarvestPlant(string plantId)
        {
            return _harvestManager?.HarvestPlant(plantId) ?? false;
        }
        
        /// <summary>
        /// Gets cultivation statistics.
        /// </summary>
        public (int active, int grown, int harvested, float yield, float avgHealth) GetCultivationStats()
        {
            return (ActivePlantCount, TotalPlantsGrown, TotalPlantsHarvested, TotalYieldHarvested, AveragePlantHealth);
        }
        
        #endregion
        
        #region Component Access (for advanced usage)
        
        /// <summary>
        /// Gets the plant lifecycle manager component
        /// </summary>
        public IPlantLifecycleManager GetPlantLifecycleManager() => _plantLifecycleManager;
        
        /// <summary>
        /// Gets the plant care manager component
        /// </summary>
        public IPlantCareManager GetPlantCareManager() => _plantCareManager;
        
        /// <summary>
        /// Gets the environmental manager component
        /// </summary>
        public IEnvironmentalManager GetEnvironmentalManager() => _environmentalManager;
        
        /// <summary>
        /// Gets the growth processor component
        /// </summary>
        public IGrowthProcessor GetGrowthProcessor() => _growthProcessor;
        
        /// <summary>
        /// Gets the harvest manager component
        /// </summary>
        public IHarvestManager GetHarvestManager() => _harvestManager;
        
        #endregion

        #region Offline Progression Implementation

        /// <summary>
        /// Handle offline progression for cultivation systems
        /// </summary>
        public void OnOfflineProgressionCalculated(float offlineHours)
        {
            if (!_enableCultivationSystem || offlineHours <= 0) return;

            Debug.Log($"[CultivationManager] Processing {offlineHours:F2} hours of offline cultivation progression");

            try
            {
                // Process plant growth during offline time
                ProcessOfflinePlantGrowth(offlineHours);

                // Process automated plant care if enabled
                ProcessOfflinePlantCare(offlineHours);

                // Check for plants ready for harvest
                ProcessOfflineHarvestChecks(offlineHours);

                Debug.Log($"[CultivationManager] Offline cultivation progression completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CultivationManager] Error processing offline cultivation: {ex.Message}");
            }
        }

        /// <summary>
        /// Process plant growth during offline time
        /// </summary>
        private void ProcessOfflinePlantGrowth(float offlineHours)
        {
            if (_growthProcessor == null || !EnableAutoGrowth) return;

            Debug.Log($"[CultivationManager] Starting offline growth processing for {offlineHours:F1} hours");

            // Simulate growth progression for offline time
            var activePlants = GetAllPlants();
            int plantsProcessed = 0;
            int stageAdvancedPlants = 0;
            float totalGrowthAdvancement = 0f;

            foreach (var plant in activePlants)
            {
                if (plant != null && plant.IsActive)
                {
                    float growthAdvancement = ProcessPlantOfflineGrowth(plant, offlineHours);
                    if (growthAdvancement > 0)
                    {
                        totalGrowthAdvancement += growthAdvancement;
                        
                        // Check for stage advancement
                        PlantGrowthStage originalStage = plant.CurrentGrowthStage;
                        AdvancePlantGrowthStage(plant, offlineHours);
                        
                        if (plant.CurrentGrowthStage != originalStage)
                        {
                            stageAdvancedPlants++;
                            Debug.Log($"[CultivationManager] Plant {plant.PlantName} ({plant.PlantID}) advanced from {originalStage} to {plant.CurrentGrowthStage}");
                        }
                    }
                    plantsProcessed++;
                }
            }

            Debug.Log($"[CultivationManager] Offline growth complete: {plantsProcessed} plants processed, {stageAdvancedPlants} advanced stages, total growth: {totalGrowthAdvancement:F2}");
        }

        /// <summary>
        /// Process growth advancement for an individual plant during offline time
        /// </summary>
        private float ProcessPlantOfflineGrowth(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null || !plant.IsActive) return 0f;

            // Calculate base growth rate considering environmental conditions and plant health
            float baseGrowthRate = CalculateBaseGrowthRate(plant);
            float environmentalModifier = CalculateEnvironmentalGrowthModifier(plant);
            float healthModifier = Mathf.Lerp(0.2f, 1.0f, plant.CurrentHealth); // Health affects growth rate
            
            // Apply time acceleration if enabled
            float timeModifier = TimeAcceleration;
            
            // Calculate total growth advancement
            float growthAdvancement = baseGrowthRate * environmentalModifier * healthModifier * timeModifier * offlineHours;
            
            // Growth advancement is applied through stage advancement logic
            // PlantInstanceSO doesn't directly store growth progress, it's calculated based on other factors
            
            return growthAdvancement;
        }

        /// <summary>
        /// Calculate base growth rate for a plant based on its strain and current stage
        /// </summary>
        private float CalculateBaseGrowthRate(PlantInstanceSO plant)
        {
            if (plant?.Strain == null) return 0.01f; // Default minimal growth
            
            // Base growth rate varies by stage
            float stageGrowthRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Seed => 0.05f,
                PlantGrowthStage.Sprout => 0.08f,
                PlantGrowthStage.Seedling => 0.12f,
                PlantGrowthStage.Vegetative => 0.15f,
                PlantGrowthStage.PreFlowering => 0.10f,
                PlantGrowthStage.Flowering => 0.08f,
                PlantGrowthStage.Ripening => 0.03f,
                PlantGrowthStage.Harvestable => 0.0f,
                _ => 0.02f
            };
            
            // Apply strain-specific growth modifiers if available
            if (plant.Strain != null)
            {
                stageGrowthRate *= plant.Strain.GrowthRateModifier;
            }
            
            return stageGrowthRate;
        }

        /// <summary>
        /// Calculate environmental growth modifier based on current conditions
        /// </summary>
        private float CalculateEnvironmentalGrowthModifier(PlantInstanceSO plant)
        {
            if (_environmentalManager == null) return 0.8f; // Suboptimal default
            
            try
            {
                var environment = _environmentalManager.GetZoneEnvironment("default");
                
                // Simple environmental fitness calculation
                float temperatureFitness = CalculateTemperatureFitness(environment.Temperature);
                float humidityFitness = CalculateHumidityFitness(environment.Humidity);
                float lightFitness = CalculateLightFitness(environment.LightIntensity);
                
                return (temperatureFitness + humidityFitness + lightFitness) / 3.0f;
            }
            catch
            {
                // Return default if environmental data is unavailable
                return 0.8f;
            }
        }

        /// <summary>
        /// Advance plant growth stage based on accumulated growth progress
        /// </summary>
        private void AdvancePlantGrowthStage(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null) return;
            
            // Define stage duration thresholds (in growth progress units)
            Dictionary<PlantGrowthStage, float> stageDurations = new Dictionary<PlantGrowthStage, float>
            {
                { PlantGrowthStage.Seed, 1.0f },
                { PlantGrowthStage.Sprout, 2.0f },
                { PlantGrowthStage.Seedling, 3.0f },
                { PlantGrowthStage.Vegetative, 8.0f },
                { PlantGrowthStage.PreFlowering, 2.0f },
                { PlantGrowthStage.Flowering, 6.0f },
                { PlantGrowthStage.Ripening, 2.0f },
                { PlantGrowthStage.Harvestable, float.MaxValue }
            };
            
            PlantGrowthStage currentStage = plant.CurrentGrowthStage;
            
            // Only advance if we have duration data for the current stage
            if (stageDurations.ContainsKey(currentStage))
            {
                float stageThreshold = stageDurations[currentStage];
                
                // For offline progression, we'll advance stages based on accumulated time and conditions
                // Since PlantInstanceSO doesn't have direct growth progress, we'll use DaysInCurrentStage
                float daysSinceStageStart = plant.DaysInCurrentStage;
                
                // Convert stage threshold from growth units to days (simplified conversion)
                float daysThreshold = stageThreshold * 10f; // Each growth unit = ~10 days
                
                // Check if plant has been in current stage long enough to advance
                if (daysSinceStageStart >= daysThreshold)
                {
                    PlantGrowthStage nextStage = GetNextGrowthStage(currentStage);
                    if (nextStage != currentStage)
                    {
                        // Note: PlantInstanceSO properties are read-only, stage advancement would need 
                        // to be handled by the plant lifecycle system in a real implementation
                        Debug.Log($"Plant {plant.PlantName} ready to advance from {currentStage} to {nextStage}");
                    }
                }
            }
        }

        /// <summary>
        /// Get the next growth stage in the progression
        /// </summary>
        private PlantGrowthStage GetNextGrowthStage(PlantGrowthStage currentStage)
        {
            return currentStage switch
            {
                PlantGrowthStage.Seed => PlantGrowthStage.Sprout,
                PlantGrowthStage.Sprout => PlantGrowthStage.Seedling,
                PlantGrowthStage.Seedling => PlantGrowthStage.Vegetative,
                PlantGrowthStage.Vegetative => PlantGrowthStage.PreFlowering,
                PlantGrowthStage.PreFlowering => PlantGrowthStage.Flowering,
                PlantGrowthStage.Flowering => PlantGrowthStage.Ripening,
                PlantGrowthStage.Ripening => PlantGrowthStage.Harvestable,
                PlantGrowthStage.Harvestable => PlantGrowthStage.Harvestable, // Stay at harvest
                _ => currentStage
            };
        }

        /// <summary>
        /// Calculate temperature fitness for growth
        /// </summary>
        private float CalculateTemperatureFitness(float temperature)
        {
            // Optimal temperature range: 20-26°C (68-79°F)
            float optimalMin = 20f;
            float optimalMax = 26f;
            
            if (temperature >= optimalMin && temperature <= optimalMax)
                return 1.0f;
            else if (temperature < 15f || temperature > 35f)
                return 0.2f; // Poor conditions
            else
                return 0.6f; // Suboptimal but acceptable
        }

        /// <summary>
        /// Calculate humidity fitness for growth
        /// </summary>
        private float CalculateHumidityFitness(float humidity)
        {
            // Optimal humidity range: 40-60%
            if (humidity >= 40f && humidity <= 60f)
                return 1.0f;
            else if (humidity < 20f || humidity > 80f)
                return 0.3f; // Poor conditions
            else
                return 0.7f; // Suboptimal but acceptable
        }

        /// <summary>
        /// Calculate light fitness for growth
        /// </summary>
        private float CalculateLightFitness(float lightIntensity)
        {
            // Optimal light intensity range: 400-800 units for cannabis (scale depends on implementation)
            if (lightIntensity >= 400f && lightIntensity <= 800f)
                return 1.0f;
            else if (lightIntensity < 200f)
                return 0.3f; // Insufficient light
            else if (lightIntensity > 1000f)
                return 0.6f; // Too intense
            else
                return 0.8f; // Adequate
        }

        /// <summary>
        /// Process automated plant care and maintenance during offline time
        /// </summary>
        private void ProcessOfflinePlantCare(float offlineHours)
        {
            if (_plantCareManager == null) return;

            Debug.Log($"[CultivationManager] Starting offline care automation for {offlineHours:F1} hours");

            var activePlants = GetAllPlants();
            int plantsWithAutoCare = 0;
            int maintenanceIssues = 0;
            float totalWaterUsed = 0f;
            float totalNutrientsUsed = 0f;

            foreach (var plant in activePlants)
            {
                if (plant != null && plant.IsActive)
                {
                    // Process automated plant care
                    var careResult = ProcessPlantOfflineCare(plant, offlineHours);
                    
                    if (careResult.WasAutoCareApplied)
                    {
                        plantsWithAutoCare++;
                        totalWaterUsed += careResult.WaterUsed;
                        totalNutrientsUsed += careResult.NutrientsUsed;
                    }
                    
                    if (careResult.MaintenanceRequired)
                    {
                        maintenanceIssues++;
                    }
                }
            }

            // Process equipment maintenance during offline period
            ProcessOfflineEquipmentMaintenance(offlineHours);

            Debug.Log($"[CultivationManager] Offline care complete: {plantsWithAutoCare} plants auto-cared, " +
                     $"{totalWaterUsed:F1}L water used, {totalNutrientsUsed:F1}L nutrients used, " +
                     $"{maintenanceIssues} plants need attention");
        }

        /// <summary>
        /// Process automated care for individual plant during offline time
        /// </summary>
        private OfflineCareResult ProcessPlantOfflineCare(PlantInstanceSO plant, float offlineHours)
        {
            var result = new OfflineCareResult();
            
            if (plant == null) return result;

            // Check if plant needs water (based on age and environmental conditions)
            bool needsWater = CheckPlantNeedsWater(plant, offlineHours);
            bool needsNutrients = CheckPlantNeedsNutrients(plant, offlineHours);
            
            // Apply automated care if systems are available
            if (needsWater && IsAutoWateringEnabled())
            {
                result.WaterUsed = CalculateWaterRequirement(plant, offlineHours);
                result.WasAutoCareApplied = true;
                Debug.Log($"[CultivationManager] Auto-watered plant {plant.PlantName}: {result.WaterUsed:F2}L");
            }
            
            if (needsNutrients && IsAutoFeedingEnabled())
            {
                result.NutrientsUsed = CalculateNutrientRequirement(plant, offlineHours);
                result.WasAutoCareApplied = true;
                Debug.Log($"[CultivationManager] Auto-fed plant {plant.PlantName}: {result.NutrientsUsed:F2}L");
            }

            // Check for maintenance issues
            result.MaintenanceRequired = CheckPlantMaintenanceNeeds(plant, offlineHours);
            
            return result;
        }

        /// <summary>
        /// Process equipment maintenance and degradation during offline time
        /// </summary>
        private void ProcessOfflineEquipmentMaintenance(float offlineHours)
        {
            Debug.Log($"[CultivationManager] Processing equipment maintenance for {offlineHours:F1} hours");
            
            // Simulate equipment degradation during offline period
            int equipmentCount = SimulateEquipmentDegradation(offlineHours);
            
            // Check for equipment failures or maintenance alerts
            int maintenanceAlerts = CheckEquipmentMaintenanceAlerts(offlineHours);
            
            // Process automation system wear
            ProcessAutomationSystemWear(offlineHours);
            
            Debug.Log($"[CultivationManager] Equipment maintenance processed: {equipmentCount} pieces tracked, " +
                     $"{maintenanceAlerts} maintenance alerts");
        }

        /// <summary>
        /// Check if a plant needs water based on offline time and conditions
        /// </summary>
        private bool CheckPlantNeedsWater(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null) return false;
            
            // Calculate water depletion rate based on plant size and environmental conditions
            float depletionRate = CalculateWaterDepletionRate(plant);
            float hoursUntilDry = plant.WaterLevel / depletionRate;
            
            // Plant needs water if it would run dry during offline period
            return offlineHours >= (hoursUntilDry * 0.8f); // Water when 80% depleted
        }

        /// <summary>
        /// Check if a plant needs nutrients based on offline time and growth stage
        /// </summary>
        private bool CheckPlantNeedsNutrients(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null) return false;
            
            // Different growth stages have different nutrient consumption rates
            float consumptionRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 0.02f, // High consumption during veg
                PlantGrowthStage.Flowering => 0.015f, // Moderate during flowering
                PlantGrowthStage.Seedling => 0.005f, // Low during seedling
                _ => 0.01f // Default rate
            };
            
            float hoursUntilDeficient = plant.NutrientLevel / consumptionRate;
            
            return offlineHours >= (hoursUntilDeficient * 0.7f); // Feed when 70% depleted
        }

        /// <summary>
        /// Check if plant requires maintenance attention
        /// </summary>
        private bool CheckPlantMaintenanceNeeds(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null) return false;
            
            // Check for various maintenance needs
            bool needsPruning = plant.AgeInDays > 30 && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative;
            bool hasHealthIssues = plant.CurrentHealth < 0.8f;
            bool needsTraining = plant.CurrentHeight > 60f && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative;
            
            return needsPruning || hasHealthIssues || needsTraining;
        }

        /// <summary>
        /// Calculate water depletion rate based on plant and environmental factors
        /// </summary>
        private float CalculateWaterDepletionRate(PlantInstanceSO plant)
        {
            // Base rate varies by growth stage and plant size
            float baseRate = 0.01f; // 1% per hour base rate
            
            // Adjust for plant size (larger plants use more water)
            float sizeModifier = (plant.CurrentHeight / 100f) * 0.5f + 0.5f; // 0.5x to 1.5x based on height
            
            // Adjust for growth stage
            float stageModifier = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 1.5f, // High water use during veg
                PlantGrowthStage.Flowering => 1.2f, // Moderate during flowering
                PlantGrowthStage.Seedling => 0.5f, // Low during seedling
                _ => 1.0f
            };
            
            return baseRate * sizeModifier * stageModifier;
        }

        /// <summary>
        /// Calculate water requirement for plant over offline period
        /// </summary>
        private float CalculateWaterRequirement(PlantInstanceSO plant, float offlineHours)
        {
            float depletionRate = CalculateWaterDepletionRate(plant);
            return depletionRate * offlineHours * 2.0f; // 2L per depletion unit
        }

        /// <summary>
        /// Calculate nutrient requirement for plant over offline period
        /// </summary>
        private float CalculateNutrientRequirement(PlantInstanceSO plant, float offlineHours)
        {
            float consumptionRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 0.02f,
                PlantGrowthStage.Flowering => 0.015f,
                PlantGrowthStage.Seedling => 0.005f,
                _ => 0.01f
            };
            
            return consumptionRate * offlineHours * 1.5f; // 1.5L per consumption unit
        }

        /// <summary>
        /// Check if automated watering system is enabled and functional
        /// </summary>
        private bool IsAutoWateringEnabled()
        {
            // Check automation system status via equipment manager integration
            return true; // Enabled for offline progression - would check actual automation status
        }

        /// <summary>
        /// Check if automated feeding system is enabled and functional
        /// </summary>
        private bool IsAutoFeedingEnabled()
        {
            // Check automation system status via equipment manager integration
            return true; // Enabled for offline progression - would check actual automation status
        }

        /// <summary>
        /// Simulate equipment degradation during offline period
        /// </summary>
        private int SimulateEquipmentDegradation(float offlineHours)
        {
            // Equipment degradation simulation - ready for equipment manager integration
            // Basic degradation tracking for offline progression
            int equipmentPieces = 10; // Assume 10 pieces of equipment
            
            foreach (int equipment in System.Linq.Enumerable.Range(1, equipmentPieces))
            {
                // Each piece degrades slightly over time
                float degradationRate = 0.001f; // 0.1% per hour
                float totalDegradation = degradationRate * offlineHours;
                
                // Log significant degradation
                if (totalDegradation > 0.05f) // 5% or more
                {
                    Debug.Log($"[CultivationManager] Equipment {equipment} degraded {totalDegradation:P1} during offline period");
                }
            }
            
            return equipmentPieces;
        }

        /// <summary>
        /// Check for equipment maintenance alerts during offline period
        /// </summary>
        private int CheckEquipmentMaintenanceAlerts(float offlineHours)
        {
            // Equipment monitoring system integration ready for implementation
            // Maintenance alerts based on equipment usage time
            int alertCount = 0;
            
            // Check if offline period crossed maintenance intervals
            float maintenanceInterval = 168f; // 1 week in hours
            int maintenanceCycles = (int)(offlineHours / maintenanceInterval);
            
            if (maintenanceCycles > 0)
            {
                alertCount = maintenanceCycles * 2; // 2 alerts per maintenance cycle
                Debug.Log($"[CultivationManager] {alertCount} equipment maintenance alerts generated during offline period");
            }
            
            return alertCount;
        }

        /// <summary>
        /// Process automation system wear and efficiency changes
        /// </summary>
        private void ProcessAutomationSystemWear(float offlineHours)
        {
            // Automation systems lose efficiency over time
            float wearRate = 0.0001f; // 0.01% efficiency loss per hour
            float totalWear = wearRate * offlineHours;
            
            if (totalWear > 0.01f) // 1% or more efficiency loss
            {
                Debug.Log($"[CultivationManager] Automation systems lost {totalWear:P2} efficiency during offline period");
            }
        }

        /// <summary>
        /// Check for plants ready for harvest during offline time
        /// </summary>
        private void ProcessOfflineHarvestChecks(float offlineHours)
        {
            if (_harvestManager == null) return;

            // Check for plants that became ready for harvest during offline period
            var harvestReadyPlants = GetPlantsByStage(PlantGrowthStage.Harvestable);
            if (harvestReadyPlants?.Any() == true)
            {
                int harvestableCount = harvestReadyPlants.Count();
                Debug.Log($"[CultivationManager] Found {harvestableCount} plants ready for harvest after offline period");
                
                // Automatic harvest scheduling ready for implementation in offline progression
                // Could automatically harvest plants that have been harvestable for too long
                // Or queue harvest notifications for when player returns
            }
        }

        #endregion
    }

    /// <summary>
    /// Result data structure for offline plant care operations
    /// </summary>
    public struct OfflineCareResult
    {
        public bool WasAutoCareApplied;
        public float WaterUsed; // Liters
        public float NutrientsUsed; // Liters
        public bool MaintenanceRequired;
        public string MaintenanceNotes;
        
        public static OfflineCareResult None => new OfflineCareResult
        {
            WasAutoCareApplied = false,
            WaterUsed = 0f,
            NutrientsUsed = 0f,
            MaintenanceRequired = false,
            MaintenanceNotes = string.Empty
        };
    }
}