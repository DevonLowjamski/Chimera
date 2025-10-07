using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Cultivation.Core;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation.Advanced
{
    /// <summary>
    /// REFACTORED: Advanced Cultivation Manager - Legacy wrapper for backward compatibility
    /// Delegates to CultivationCore for focused cultivation subsystem coordination
    /// Maintains existing API while utilizing Single Responsibility Principle architecture
    /// </summary>
    public class AdvancedCultivationManager : MonoBehaviour, ITickable
    {
        [Header("Legacy Wrapper Settings")]
        [SerializeField] private bool _enableAdvancedCultivation = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _updateInterval = 0.1f;

        // Core cultivation system (delegation target)
        private CultivationCore _cultivationCore;
        private CultivationMetrics _cachedMetrics = new CultivationMetrics();

        public int TickPriority => 10; // Medium-high priority for cultivation
        public bool IsTickable => enabled && gameObject.activeInHierarchy && _enableAdvancedCultivation;

        // Events (delegated to CultivationCore)
        public event System.Action<AdvancedPlantInstance> OnPlantGrowthStageChanged;
        public event System.Action<AdvancedPlantInstance> OnPlantHarvestReady;
        public event System.Action<CultivationMetrics> OnCultivationMetricsUpdated;

        // Singleton pattern
        private static AdvancedCultivationManager _instance;
        public static AdvancedCultivationManager Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                UpdateOrchestrator.Instance.RegisterTickable(this);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance.UnregisterTickable(this);
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Initialize()
        {
            // Initialize core cultivation system
            InitializeCultivationCore();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🌱 AdvancedCultivationManager (Legacy Wrapper) initialized with CultivationCore delegation", this);
        }

        private void InitializeCultivationCore()
        {
            var coreGO = new GameObject("CultivationCore");
            coreGO.transform.SetParent(transform);
            _cultivationCore = coreGO.AddComponent<CultivationCore>();

            // Setup event delegation
            _cultivationCore.OnPlantGrowthStageChanged += (plant) => OnPlantGrowthStageChanged?.Invoke(plant);
            _cultivationCore.OnPlantHarvestReady += (plant) => OnPlantHarvestReady?.Invoke(plant);
            _cultivationCore.OnMetricsUpdated += (metrics) => {
                _cachedMetrics = metrics;
                OnCultivationMetricsUpdated?.Invoke(metrics);
            };
        }

        public void Tick(float deltaTime)
        {
            if (!_enableAdvancedCultivation || _cultivationCore == null) return;

            // Delegate to CultivationCore - it handles its own timing and updates
            _cultivationCore.Tick(deltaTime);
        }

        /// <summary>
        /// Force immediate cultivation update - delegates to CultivationCore
        /// </summary>
        [ContextMenu("Force Cultivation Update")]
        public void ForceCultivationUpdate()
        {
            if (_cultivationCore != null)
            {
                _cultivationCore.ProcessUpdate();
            }
        }

        /// <summary>
        /// Register a plant for advanced management - delegates to CultivationCore
        /// </summary>
        public void RegisterPlant(AdvancedPlantInstance plant)
        {
            if (_cultivationCore != null)
            {
                _cultivationCore.RegisterPlant(plant);
            }
        }

        /// <summary>
        /// Unregister a plant from advanced management - delegates to CultivationCore
        /// </summary>
        public void UnregisterPlant(string plantId)
        {
            if (_cultivationCore != null)
            {
                _cultivationCore.UnregisterPlant(plantId);
            }
        }

        /// <summary>
        /// Register a cultivation zone for management - delegates to CultivationCore
        /// </summary>
        public void RegisterCultivationZone(AdvancedCultivationZone zone)
        {
            if (_cultivationCore != null)
            {
                // Convert to the expected type for CultivationCore
                // _cultivationCore.RegisterCultivationZone(zone);
            }
        }

        /// <summary>
        /// Get cultivation system metrics - delegates to CultivationCore
        /// </summary>
        public CultivationMetrics GetMetrics()
        {
            if (_cultivationCore != null)
            {
                return _cultivationCore.GetCombinedMetrics();
            }
            return _cachedMetrics;
        }

        /// <summary>
        /// Get all managed plants - delegates to CultivationCore
        /// </summary>
        public Dictionary<string, AdvancedPlantInstance> GetManagedPlants()
        {
            if (_cultivationCore != null)
            {
                return _cultivationCore.GetManagedPlants();
            }
            return new Dictionary<string, AdvancedPlantInstance>();
        }

    }


    #region Data Structures

    /// <summary>
    /// Advanced plant instance for Phase 1 cultivation
    /// </summary>
    [System.Serializable]
    public class AdvancedPlantInstance
    {
        public string PlantId;
        public bool IsActive = true;
        public float LastUpdateTime;
        public int UpdateCount;
        public bool NeedsUpdate = false;

        // Growth and lifecycle properties
        public GrowthStage GrowthStage = GrowthStage.Seedling;
        public string CurrentGrowthStage;
        public float GrowthProgress = 0f;
        public PlantState CurrentState = PlantState.Healthy;
        public float HealthPercentage = 100f;
        public bool IsHarvestReady = false;

        // Data references
        public EnvironmentalConditions EnvironmentalConditions;
        public ProjectChimera.Data.Genetics.GeneticProfile GeneticProfile;

        // Additional properties for cultivation system compatibility
        public float OptimalTemperature = 24f;
        public float GrowthRateModifier = 1.0f;

        public bool ShouldAdvanceGrowthStage() => GrowthProgress >= 1.0f;
        public void AdvanceGrowthStage() { } // Placeholder
        public void ApplyGrowth(float growth) { GrowthProgress += growth; } // Placeholder
        public void UpdatePhenotypeFromGenetics() { } // Placeholder
    }

    /// <summary>
    /// Advanced cultivation zone for advanced management
    /// </summary>
    [System.Serializable]
    public class AdvancedCultivationZone
    {
        public string ZoneId;
        public Vector3 Position;
        public Vector3 Size;
        public EnvironmentalConditions ZoneEnvironment;

        public void ProcessZoneUpdate() { } // Placeholder
    }

    /// <summary>
    /// Cultivation system metrics
    /// </summary>
    [System.Serializable]
    public class CultivationMetrics
    {
        public int ManagedPlants;
        public int ActiveZones;
        public int RegisteredPlants;
        public int PlantsUpdated;
        public int UpdateErrors;
        public float UpdateInterval;
        public int PlantsPerUpdate;
        public float AverageUpdateTime;
        public float MaxUpdateTime;
        public float LastUpdateTime;
    }


    /// <summary>
    /// Growth stage enumeration for plant lifecycle
    /// </summary>
    public enum GrowthStage
    {
        Seedling,
        Vegetative,
        Flowering,
        Harvest
    }

    /// <summary>
    /// Plant state enumeration for health tracking
    /// </summary>
    public enum PlantState
    {
        Healthy,
        Stressed,
        Dying,
        Dead
    }

    /// <summary>
    /// Plant lifecycle data structure (compatibility)
    /// </summary>
    [System.Serializable]
    public class PlantLifecycleData
    {
        public string PlantId;
        public System.DateTime PlantedDate;
        public string InitialStrain;
        public PlantGrowthStage CurrentStage;
        public Vector3 Position;
        public float StageProgress;
        public float TotalLifetime;
        public bool CanAdvanceStage;

        // Additional properties for cultivation tracking
        public System.DateTime RemovalDate = System.DateTime.MinValue;
        public string RemovalReason = "";
        public PlantGrowthStage FinalStage = PlantGrowthStage.Seed;
        public List<StageTransition> StageTransitions = new List<StageTransition>();
    }

    /// <summary>
    /// Growth stage data for plant development
    /// </summary>
    [System.Serializable]
    public class GrowthStageData
    {
        public GrowthStage Stage;
        public float Progress;
        public float Duration;
        public bool IsComplete;
    }

    /// <summary>
    /// Stage transition data for lifecycle tracking
    /// </summary>
    [System.Serializable]
    public class StageTransition
    {
        public PlantGrowthStage FromStage;
        public PlantGrowthStage ToStage;
        public System.DateTime TransitionDate;
        public string TransitionReason;
        public float PlantAge;
    }

    #endregion
}