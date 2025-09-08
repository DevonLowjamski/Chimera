using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
using ProjectChimera.Data.Cultivation;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Cultivation Manager - orchestrates all cultivation components
    /// Maintains original interface while using modular components
    /// Refactored from monolithic 938-line class into focused components
    /// </summary>
    public class CultivationManager : DIChimeraManager, ITickable, ProjectChimera.Core.IOfflineProgressionListener
    {
        [Header("Cultivation Manager Configuration")]
        [SerializeField] private bool _enableCultivationSystem = true;

        // Cultivation components
        private IPlantLifecycle _plantLifecycle;
        private IEnvironmentControl _environmentControl;
        private IPlantCare _plantCare;
        private IHarvestManager _harvestManager;

        public string ManagerName => "Cultivation Manager";

        // Delegate properties to components
        public int ActivePlantCount => _plantLifecycle?.ActivePlantCount ?? 0;
        public int TotalPlantsGrown => _plantLifecycle?.TotalPlantsGrown ?? 0;
        public int TotalPlantsHarvested => _harvestManager?.TotalPlantsHarvested ?? 0;
        public float TotalYieldHarvested => _harvestManager?.TotalYieldHarvested ?? 0f;
        public float AveragePlantHealth => CalculateAveragePlantHealth();

        public bool EnableAutoGrowth
        {
            get => _plantLifecycle?.EnableAutoGrowth ?? false;
            set { if (_plantLifecycle != null) _plantLifecycle.EnableAutoGrowth = value; }
        }

        public float TimeAcceleration
        {
            get => _plantLifecycle?.TimeAcceleration ?? 1f;
            set { if (_plantLifecycle != null) _plantLifecycle.TimeAcceleration = value; }
        }

        #region Manager Lifecycle

        protected override void OnManagerInitialize()
        {
            ChimeraLogger.LogInitialization("CultivationManager", "Initializing modular cultivation system...");

            if (!_enableCultivationSystem)
            {
                ChimeraLogger.Log("[CultivationManager] Cultivation system disabled.");
                return;
            }

            // Initialize components with dependency injection
            InitializeComponents();
            ConfigureComponentIntegrations();
            InitializeAllComponents();
            SetupEventForwarding();

            // Register with GameManager
            GameManager.Instance?.RegisterManager(this);

            ChimeraLogger.LogInitialization("CultivationManager", "Modular cultivation system initialized successfully.");
        }

        protected override void OnManagerShutdown()
        {
            ChimeraLogger.LogInitialization("CultivationManager", "Shutting down modular cultivation system...");

            // Shutdown components in reverse order
            ShutdownComponents();

            ChimeraLogger.LogInitialization("CultivationManager", "Modular cultivation system shutdown complete.");
        }

        #endregion

        #region ITickable Implementation

        public int Priority => TickPriority.CultivationManager;
        public bool Enabled => IsInitialized && _enableCultivationSystem;

        public void Tick(float deltaTime)
        {
            if (!Enabled) return;

            // Update environmental changes
            _environmentControl?.ProcessEnvironmentalChanges(deltaTime);

            // Update plant care for all plants
            foreach (var plant in GetAllPlants())
            {
                _plantCare?.UpdatePlantHealthBasedOnCare(plant);
            }
        }

        public void OnRegistered()
        {
            ChimeraLogger.LogVerbose("[CultivationManager] Registered with UpdateOrchestrator");
        }

        public void OnUnregistered()
        {
            ChimeraLogger.LogVerbose("[CultivationManager] Unregistered from UpdateOrchestrator");
        }

        #endregion

        #region IOfflineProgressionListener Implementation

        public void OnOfflineProgressionCalculated(float offlineHours)
        {
            if (!_enableCultivationSystem || offlineHours <= 0f)
            {
                return;
            }

            ChimeraLogger.Log($"[CultivationManager] Processing offline progression for {offlineHours:F1} hours");

            try
            {
                // Process offline growth
                _plantLifecycle?.ProcessOfflineGrowth(offlineHours);

                // Process offline plant care
                _plantCare?.ProcessAllPlantsOfflineCare(offlineHours);

                // Process environmental changes
                _environmentControl?.ProcessOfflineEnvironmentalChanges(offlineHours);

                // Check for plants ready for harvest
                _harvestManager?.ProcessOfflineHarvestChecks(offlineHours);

                ChimeraLogger.Log($"[CultivationManager] Offline progression completed for {offlineHours:F1} hours");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CultivationManager] Error during offline progression: {ex.Message}");
            }
        }

        #endregion

        #region Public API - Plant Management

        /// <summary>
        /// Adds a new plant to the cultivation system.
        /// </summary>
        public string AddPlant(object species, Vector3 position, string zoneId = "")
        {
            return _plantLifecycle?.AddPlant(species, position, zoneId);
        }

        /// <summary>
        /// Removes a plant from the cultivation system.
        /// </summary>
        public bool RemovePlant(string plantId, bool isHarvest = false)
        {
            return _plantLifecycle?.RemovePlant(plantId, isHarvest) ?? false;
        }

        /// <summary>
        /// Gets a plant instance by its ID.
        /// </summary>
        public PlantInstanceSO GetPlant(string plantId)
        {
            return _plantLifecycle?.GetPlant(plantId);
        }

        /// <summary>
        /// Gets all active plants.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            return _plantLifecycle?.GetAllPlants() ?? new PlantInstanceSO[0];
        }

        /// <summary>
        /// Gets all plants in a specific growth stage.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage)
        {
            return _plantLifecycle?.GetPlantsByStage(stage) ?? new PlantInstanceSO[0];
        }

        /// <summary>
        /// Gets all plants that need attention.
        /// </summary>
        public System.Collections.Generic.IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            return _plantLifecycle?.GetPlantsNeedingAttention() ?? new PlantInstanceSO[0];
        }

        #endregion

        #region Public API - Plant Care

        /// <summary>
        /// Waters a specific plant.
        /// </summary>
        public bool WaterPlant(string plantId, float waterAmount = 0.5f)
        {
            return _plantCare?.WaterPlant(plantId, waterAmount) ?? false;
        }

        /// <summary>
        /// Feeds nutrients to a specific plant.
        /// </summary>
        public bool FeedPlant(string plantId, float nutrientAmount = 0.4f)
        {
            return _plantCare?.FeedPlant(plantId, nutrientAmount) ?? false;
        }

        /// <summary>
        /// Applies training to a specific plant.
        /// </summary>
        public bool TrainPlant(string plantId, string trainingType)
        {
            return _plantCare?.TrainPlant(plantId, trainingType) ?? false;
        }

        /// <summary>
        /// Waters all plants in the cultivation system.
        /// </summary>
        public void WaterAllPlants(float waterAmount = 0.5f)
        {
            _plantCare?.WaterAllPlants(waterAmount);
        }

        /// <summary>
        /// Feeds all plants in the cultivation system.
        /// </summary>
        public void FeedAllPlants(float nutrientAmount = 0.4f)
        {
            _plantCare?.FeedAllPlants(nutrientAmount);
        }

        #endregion

        #region Public API - Environment Control

        /// <summary>
        /// Updates environmental conditions for a specific zone.
        /// </summary>
        public void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment)
        {
            _environmentControl?.SetZoneEnvironment(zoneId, environment);
        }

        /// <summary>
        /// Gets environmental conditions for a specific zone.
        /// </summary>
        public EnvironmentalConditions GetZoneEnvironment(string zoneId)
        {
            return _environmentControl?.GetZoneEnvironment(zoneId) ?? EnvironmentalConditions.CreateIndoorDefault();
        }

        #endregion

        #region Public API - Growth Processing

        /// <summary>
        /// Processes daily growth for all active plants.
        /// </summary>
        public void ProcessDailyGrowthForAllPlants()
        {
            _plantLifecycle?.ProcessDailyGrowthForAllPlants();
        }

        /// <summary>
        /// Forces an immediate growth update for testing purposes.
        /// </summary>
        public void ForceGrowthUpdate()
        {
            _plantLifecycle?.ForceGrowthUpdate();
        }

        #endregion

        #region Public API - Harvest Management

        /// <summary>
        /// Harvests a plant by ID
        /// </summary>
        public bool HarvestPlant(string plantId)
        {
            return _harvestManager?.HarvestPlant(plantId) ?? false;
        }

        #endregion

        #region Public API - Statistics

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
        public IPlantLifecycle GetPlantLifecycleManager() => _plantLifecycle;

        /// <summary>
        /// Gets the environment control component
        /// </summary>
        public IEnvironmentControl GetEnvironmentControl() => _environmentControl;

        /// <summary>
        /// Gets the plant care manager component
        /// </summary>
        public IPlantCare GetPlantCareManager() => _plantCare;

        /// <summary>
        /// Gets the harvest manager component
        /// </summary>
        public IHarvestManager GetHarvestManager() => _harvestManager;

        #endregion

        #region Component Management

        private void InitializeComponents()
        {
            // Create cultivation components
            _plantLifecycle = new PlantLifecycle();
            _environmentControl = new EnvironmentControl();
            _plantCare = new PlantCare(_plantLifecycle, _environmentControl);
            _harvestManager = new HarvestManager(_plantLifecycle);
        }

        private void ConfigureComponentIntegrations()
        {
            // Set up cross-component dependencies
            (_plantCare as PlantCare)?.SetDependencies(_plantLifecycle, _environmentControl);
            if (_harvestManager is HarvestManager harvestMgr)
            {
                harvestMgr.SetDependencies(_plantLifecycle);
            }
        }

        private void InitializeAllComponents()
        {
            // Initialize all components
            _plantLifecycle.Initialize();
            _environmentControl.Initialize();
            _plantCare.Initialize(_plantLifecycle);
            _harvestManager.Initialize(_plantLifecycle);

            // Register components with DI container for proper dependency management
            var serviceContainer = ServiceContainerFactory.Instance;
            try
            {
                serviceContainer?.RegisterSingleton<IPlantLifecycle>(_plantLifecycle);
                serviceContainer?.RegisterSingleton<IEnvironmentControl>(_environmentControl);
                serviceContainer?.RegisterSingleton<IPlantCare>(_plantCare);
                serviceContainer?.RegisterSingleton<IHarvestManager>(_harvestManager);

                ChimeraLogger.Log("[CultivationManager] All components registered with unified DI container");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CultivationManager] Failed to register components with DI container: {ex.Message}");
            }

            ChimeraLogger.Log("[CultivationManager] All cultivation components initialized");
        }

        private void SetupEventForwarding()
        {
            // Forward important events from components
            if (_plantLifecycle != null)
            {
                _plantLifecycle.OnPlantAdded += (plantId, plant) => {
                    ChimeraLogger.Log($"[CultivationManager] Plant added: {plantId}");
                };

                _plantLifecycle.OnPlantRemoved += (plantId, reason) => {
                    ChimeraLogger.Log($"[CultivationManager] Plant removed: {plantId} (reason: {reason})");
                };
            }

            if (_harvestManager != null)
            {
                _harvestManager.OnPlantHarvested += (plantId, result) => {
                    ChimeraLogger.Log($"[CultivationManager] Plant harvested: {plantId} - {result.YieldAmount:F1}g {result.Quality}");
                };
            }

            if (_plantCare != null)
            {
                _plantCare.OnMaintenanceRequired += (plantId, notes) => {
                    ChimeraLogger.LogWarning($"[CultivationManager] Plant {plantId} requires maintenance: {notes}");
                };
            }
        }

        private void ShutdownComponents()
        {
            try
            {
                _harvestManager?.Shutdown();
                _plantCare?.Shutdown();
                _environmentControl?.Shutdown();
                _plantLifecycle?.Shutdown();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CultivationManager] Error during component shutdown: {ex.Message}");
            }

            ChimeraLogger.Log("[CultivationManager] All cultivation components shutdown");
        }

        #endregion

        #region Helper Methods

        private float CalculateAveragePlantHealth()
        {
            var plants = GetAllPlants();
            if (!plants.Any())
            {
                return 0f;
            }

            return plants.Average(p => p.CurrentHealth);
        }

        #endregion

        #region Legacy Support (maintaining backward compatibility)

        // Legacy interface support for existing code that may depend on the old interfaces
        // These would delegate to the new component system

        #endregion
    }
}
