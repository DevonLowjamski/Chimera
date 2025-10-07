using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Cultivation.Advanced;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Cultivation Core - Central coordination for cultivation subsystems
    /// Manages plant lifecycle, growth simulation, environmental response, and yield optimization
    /// Follows Single Responsibility Principle with focused subsystem coordination
    /// </summary>
    public class CultivationCore : MonoBehaviour, ITickable
    {
        [Header("Core Cultivation Settings")]
        [SerializeField] private bool _enableAdvancedCultivation = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _updateInterval = 0.1f;

        // Core subsystems
        private PlantLifecycleManager _plantLifecycleManager;
        private GrowthSimulationManager _growthSimulationManager;
        private EnvironmentalResponseManager _environmentalResponseManager;
        private YieldOptimizationManager _yieldOptimizationManager;
        private CultivationZoneManager _zoneManager;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int TickPriority => 10; // Medium-high priority for cultivation
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled && _enableAdvancedCultivation;

        // Statistics aggregation
        public CultivationMetrics GetCombinedMetrics()
        {
            var metrics = new CultivationMetrics();

            if (_plantLifecycleManager != null)
            {
                var plantStats = _plantLifecycleManager.GetStats();
                metrics.ManagedPlants = plantStats.ManagedPlants;
                metrics.RegisteredPlants = plantStats.RegisteredPlants;
                metrics.PlantsUpdated = plantStats.PlantsUpdated;
                metrics.UpdateErrors = plantStats.UpdateErrors;
            }

            if (_zoneManager != null)
            {
                metrics.ActiveZones = _zoneManager.GetActiveZoneCount();
            }

            if (_growthSimulationManager != null)
            {
                var growthStats = _growthSimulationManager.GetStats();
                metrics.AverageUpdateTime = growthStats.AverageGrowthTime;
                metrics.MaxUpdateTime = growthStats.MaxGrowthTime;
            }

            metrics.UpdateInterval = _updateInterval;
            metrics.LastUpdateTime = Time.time;
            return metrics;
        }

        // Events
        public System.Action<AdvancedPlantInstance> OnPlantGrowthStageChanged;
        public System.Action<AdvancedPlantInstance> OnPlantHarvestReady;
        public System.Action<CultivationMetrics> OnMetricsUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🌱 Initializing CultivationCore subsystems...", this);

            // Initialize subsystems in dependency order
            InitializePlantLifecycleManager();
            InitializeGrowthSimulationManager();
            InitializeEnvironmentalResponseManager();
            InitializeYieldOptimizationManager();
            InitializeCultivationZoneManager();

            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ CultivationCore initialized with all subsystems", this);
        }

        private void InitializePlantLifecycleManager()
        {
            var plantGO = new GameObject("PlantLifecycleManager");
            plantGO.transform.SetParent(transform);
            _plantLifecycleManager = plantGO.AddComponent<PlantLifecycleManager>();

            _plantLifecycleManager.OnPlantGrowthStageChanged += (plant) => OnPlantGrowthStageChanged?.Invoke(plant);
            _plantLifecycleManager.OnPlantHarvestReady += (plant) => OnPlantHarvestReady?.Invoke(plant);
        }

        private void InitializeGrowthSimulationManager()
        {
            var growthGO = new GameObject("GrowthSimulationManager");
            growthGO.transform.SetParent(transform);
            _growthSimulationManager = growthGO.AddComponent<GrowthSimulationManager>();
        }

        private void InitializeEnvironmentalResponseManager()
        {
            var envGO = new GameObject("EnvironmentalResponseManager");
            envGO.transform.SetParent(transform);
            _environmentalResponseManager = envGO.AddComponent<EnvironmentalResponseManager>();
        }

        private void InitializeYieldOptimizationManager()
        {
            var yieldGO = new GameObject("YieldOptimizationManager");
            yieldGO.transform.SetParent(transform);
            _yieldOptimizationManager = yieldGO.AddComponent<YieldOptimizationManager>();
        }

        private void InitializeCultivationZoneManager()
        {
            var zoneGO = new GameObject("CultivationZoneManager");
            zoneGO.transform.SetParent(transform);
            _zoneManager = zoneGO.AddComponent<CultivationZoneManager>();
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableAdvancedCultivation) return;

            // Coordinate subsystem updates
            if (_plantLifecycleManager != null)
                _plantLifecycleManager.UpdatePlants();

            if (_growthSimulationManager != null)
                _growthSimulationManager.ProcessGrowthSimulation();

            if (_environmentalResponseManager != null)
                _environmentalResponseManager.ProcessEnvironmentalResponse();

            if (_yieldOptimizationManager != null)
                _yieldOptimizationManager.OptimizeYields();

            if (_zoneManager != null)
                _zoneManager.UpdateZones();

            // Update combined metrics
            OnMetricsUpdated?.Invoke(GetCombinedMetrics());
        }

        /// <summary>
        /// Register plant through PlantLifecycleManager
        /// </summary>
        public void RegisterPlant(AdvancedPlantInstance plant)
        {
            _plantLifecycleManager?.RegisterPlant(plant);
        }

        /// <summary>
        /// Unregister plant through PlantLifecycleManager
        /// </summary>
        public void UnregisterPlant(string plantId)
        {
            _plantLifecycleManager?.UnregisterPlant(plantId);
        }

        /// <summary>
        /// Register cultivation zone through ZoneManager
        /// </summary>
        public void RegisterCultivationZone(CultivationZone zone)
        {
            _zoneManager?.RegisterZone(zone);
        }

        /// <summary>
        /// Get managed plants through PlantLifecycleManager
        /// </summary>
        public Dictionary<string, AdvancedPlantInstance> GetManagedPlants()
        {
            return _plantLifecycleManager?.GetManagedPlants() ?? new Dictionary<string, AdvancedPlantInstance>();
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_plantLifecycleManager != null) _plantLifecycleManager.SetEnabled(enabled);
            if (_growthSimulationManager != null) _growthSimulationManager.SetEnabled(enabled);
            if (_environmentalResponseManager != null) _environmentalResponseManager.SetEnabled(enabled);
            if (_yieldOptimizationManager != null) _yieldOptimizationManager.SetEnabled(enabled);
            if (_zoneManager != null) _zoneManager.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"CultivationCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set update interval for all subsystems
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Mathf.Max(0.01f, interval);

            if (_plantLifecycleManager != null) _plantLifecycleManager.SetUpdateInterval(_updateInterval);
            if (_growthSimulationManager != null) _growthSimulationManager.SetUpdateInterval(_updateInterval);
        }

        /// <summary>
        /// Force immediate cultivation update
        /// </summary>
        public void ProcessUpdate()
        {
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance.UnregisterTickable(this);
        }
    }
}
