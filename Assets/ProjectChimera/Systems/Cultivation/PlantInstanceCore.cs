using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Core plant instance infrastructure and component coordination.
    /// Handles plant identity, lifecycle management, and system integration.
    /// </summary>
    public class PlantInstanceCore : MonoBehaviour, ITickable
    {
        [Header("Plant Identity")]
        [SerializeField] protected string _plantID;
        [SerializeField] protected object _strain;
        [SerializeField] protected string _plantName;
        [SerializeField] protected DateTime _plantedDate;
        [SerializeField] protected DateTime _lastWatered;
        [SerializeField] protected DateTime _lastFed;
        [SerializeField] protected int _generationNumber = 1;

        [Header("Plant Configuration")]
        [SerializeField] protected bool _enableGrowthSystem = true;
        [SerializeField] protected bool _enableHealthSystem = true;
        [SerializeField] protected bool _enableEnvironmentalSystem = true;
        [SerializeField] protected bool _enableGeneticsSystem = true;
        [SerializeField] protected bool _enableVisualizationSystem = true;

        // Plant system components
        protected PlantGrowthSystem _growthSystem;
        protected PlantHealthSystem _healthSystem;
        protected PlantEnvironmentalSystem _environmentalSystem;
        protected PlantGeneticsSystem _geneticsSystem;
        protected PlantVisualizationSystem _visualizationSystem;

        // Plant state
        protected bool _isActive = true;
        protected bool _isInitialized = false;
        protected DateTime _initializationTime;

        // Events
        public event Action<PlantInstanceCore> OnPlantInitialized;
        public event Action<PlantInstanceCore> OnPlantDestroyed;
        public event Action<PlantInstanceCore> OnSystemsUpdated;

        /// <summary>
        /// Core plant properties
        /// </summary>
        public string PlantID { get => _plantID; set => _plantID = value; }
        public object Strain => _strain;
        public string PlantName => _plantName;
        public DateTime PlantedDate { get => _plantedDate; set => _plantedDate = value; }
        public DateTime LastWatered { get => _lastWatered; set => _lastWatered = value; }
        public DateTime LastFed { get => _lastFed; set => _lastFed = value; }
        public int GenerationNumber => _generationNumber;
        public bool IsActive => _isActive;
        public bool IsInitialized => _isInitialized;

        // System access properties
        public PlantGrowthSystem GrowthSystem => _growthSystem;
        public PlantHealthSystem HealthSystem => _healthSystem;
        public PlantEnvironmentalSystem EnvironmentalSystem => _environmentalSystem;
        public PlantGeneticsSystem GeneticsSystem => _geneticsSystem;
        public PlantVisualizationSystem VisualizationSystem => _visualizationSystem;

        /// <summary>
        /// Initialize plant system components
        /// </summary>
        protected virtual void InitializePlantSystems()
        {
            // Initialize growth system
            if (_enableGrowthSystem)
            {
                _growthSystem = GetComponent<PlantGrowthSystem>();
                if (_growthSystem == null)
                {
                    _growthSystem = gameObject.AddComponent<PlantGrowthSystem>();
                }
                _growthSystem.Initialize(_plantID, ProjectChimera.Data.Shared.PlantGrowthStage.Seedling);
            }

            // Initialize health system
            if (_enableHealthSystem)
            {
                _healthSystem = GetComponent<PlantHealthSystem>();
                if (_healthSystem == null)
                {
                    _healthSystem = gameObject.AddComponent<PlantHealthSystem>();
                }
                _healthSystem.Initialize();
            }

            // Initialize environmental system
            if (_enableEnvironmentalSystem)
            {
                _environmentalSystem = GetComponent<PlantEnvironmentalSystem>();
                if (_environmentalSystem == null)
                {
                    _environmentalSystem = gameObject.AddComponent<PlantEnvironmentalSystem>();
                }
                _environmentalSystem.Initialize();
            }

            // Initialize genetics system
            if (_enableGeneticsSystem)
            {
                _geneticsSystem = GetComponent<PlantGeneticsSystem>();
                if (_geneticsSystem == null)
                {
                    _geneticsSystem = gameObject.AddComponent<PlantGeneticsSystem>();
                }
                _geneticsSystem.Initialize();
            }

            // Initialize visualization system
            if (_enableVisualizationSystem)
            {
                _visualizationSystem = GetComponent<PlantVisualizationSystem>();
                if (_visualizationSystem == null)
                {
                    _visualizationSystem = gameObject.AddComponent<PlantVisualizationSystem>();
                }
                _visualizationSystem.Initialize();
            }
        }

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_plantID))
                _plantID = GenerateUniqueID();

            InitializePlantSystems();
        }

        protected virtual void Start()
        {
            if (_strain != null)
            {
                InitializeFromStrain();
            }

            _isInitialized = true;
            _initializationTime = DateTime.Now;
            OnPlantInitialized?.Invoke(this);
        }

        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (!_isActive || !_isInitialized)
                return;

            UpdatePlantSystems(deltaTime);
        }

        protected virtual void OnDestroy()
        {
            OnPlantDestroyed?.Invoke(this);
        }

        /// <summary>
        /// Update all plant systems
        /// </summary>
        protected virtual void UpdatePlantSystems(float deltaTime)
        {
            // Update environmental system first (provides data to other systems)
            if (_enableEnvironmentalSystem && _environmentalSystem != null)
            {
                _environmentalSystem.UpdateEnvironmentalMonitoring(deltaTime);
            }

            // Update genetics system (affects other systems)
            if (_enableGeneticsSystem && _geneticsSystem != null)
            {
                _geneticsSystem.UpdateAllGenetics(deltaTime);
            }

            // Update health system (affects growth)
            if (_enableHealthSystem && _healthSystem != null)
            {
                _healthSystem.UpdateHealthStatus(deltaTime);
            }

            // Update growth system (depends on health and environment)
            if (_enableGrowthSystem && _growthSystem != null)
            {
                _growthSystem.UpdateGrowthProgress(deltaTime);
            }

            // Update visualization system last (reflects state changes)
            if (_enableVisualizationSystem && _visualizationSystem != null)
            {
                _visualizationSystem.UpdateVisualization(deltaTime);
            }

            OnSystemsUpdated?.Invoke(this);
        }

        /// <summary>
        /// Initialize plant from strain definition
        /// </summary>
        public virtual void InitializeFromStrain(object strain = null)
        {
            if (strain != null)
                _strain = strain;

            if (_strain == null)
            {
                LogPlantAction($"Cannot initialize plant {_plantID}: no strain assigned");
                return;
            }

            _plantName = $"{GetStrainName()}_{GenerateShortID()}";
            _plantedDate = DateTime.Now;

            // Initialize systems with strain data
            InitializeSystemsFromStrain();

            LogPlantAction($"Initialized plant {_plantID} from strain {GetStrainName()}");
        }

        /// <summary>
        /// Initialize systems from strain data
        /// </summary>
        protected virtual void InitializeSystemsFromStrain()
        {
            // Initialize growth system
            if (_enableGrowthSystem && _growthSystem != null)
            {
                _growthSystem.Initialize(PlantID, ProjectChimera.Data.Shared.PlantGrowthStage.Seedling);
            }

            // Initialize health system
            if (_enableHealthSystem && _healthSystem != null)
            {
                _healthSystem.Initialize();
            }

            // Initialize environmental system
            if (_enableEnvironmentalSystem && _environmentalSystem != null)
            {
                _environmentalSystem.Initialize();
            }

            // Initialize genetics system
            if (_enableGeneticsSystem && _geneticsSystem != null)
            {
                _geneticsSystem.Initialize();
            }

            // Initialize visualization system
            if (_enableVisualizationSystem && _visualizationSystem != null)
            {
                _visualizationSystem.Initialize();
            }
        }

        /// <summary>
        /// Create a new plant instance from strain
        /// </summary>
        public static PlantInstanceCore CreateFromStrain(object strain, Vector3 position, Transform parent = null)
        {
            var plantObject = new GameObject($"Plant_{GetStrainNameFromObject(strain)}_{GenerateShortID()}");
            plantObject.transform.position = position;

            if (parent != null)
                plantObject.transform.SetParent(parent);

            var plantInstance = plantObject.AddComponent<PlantInstanceCore>();
            plantInstance.InitializeFromStrain(strain);

            return plantInstance;
        }

        /// <summary>
        /// Deactivate the plant
        /// </summary>
        public virtual void DeactivatePlant()
        {
            _isActive = false;
            LogPlantAction($"Plant {_plantID} deactivated");
        }

        /// <summary>
        /// Reactivate the plant
        /// </summary>
        public virtual void ReactivatePlant()
        {
            _isActive = true;
            LogPlantAction($"Plant {_plantID} reactivated");
        }

        /// <summary>
        /// Get comprehensive plant metrics
        /// </summary>
        public virtual PlantInstanceMetrics GetPlantMetrics()
        {
            return new PlantInstanceMetrics
            {
                PlantID = _plantID,
                PlantName = _plantName,
                IsActive = _isActive,
                IsInitialized = _isInitialized,
                InitializationTime = _isInitialized ? (DateTime.Now - _initializationTime).TotalSeconds : 0,
                DaysSincePlanted = (DateTime.Now - _plantedDate).Days,
                SystemCount = GetActiveSystemCount(),
                GrowthSystemActive = _enableGrowthSystem && _growthSystem != null,
                HealthSystemActive = _enableHealthSystem && _healthSystem != null,
                EnvironmentalSystemActive = _enableEnvironmentalSystem && _environmentalSystem != null,
                GeneticsSystemActive = _enableGeneticsSystem && _geneticsSystem != null,
                VisualizationSystemActive = _enableVisualizationSystem && _visualizationSystem != null
            };
        }

        /// <summary>
        /// Get strain name from plant strain
        /// </summary>
        protected virtual string GetStrainName()
        {
            return GetStrainNameFromObject(_strain);
        }

        /// <summary>
        /// Get strain name from strain object
        /// </summary>
        protected static string GetStrainNameFromObject(object strain)
        {
            return (strain as ProjectChimera.Data.Cultivation.PlantStrainSO)?.StrainName ?? "Unknown";
        }

        /// <summary>
        /// Get count of active systems
        /// </summary>
        protected virtual int GetActiveSystemCount()
        {
            int count = 0;
            if (_enableGrowthSystem && _growthSystem != null) count++;
            if (_enableHealthSystem && _healthSystem != null) count++;
            if (_enableEnvironmentalSystem && _environmentalSystem != null) count++;
            if (_enableGeneticsSystem && _geneticsSystem != null) count++;
            if (_enableVisualizationSystem && _visualizationSystem != null) count++;
            return count;
        }

        /// <summary>
        /// Generate unique plant ID
        /// </summary>
        protected static string GenerateUniqueID()
        {
            return $"PLANT_{DateTime.Now.Ticks:X}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        /// <summary>
        /// Generate short ID
        /// </summary>
        protected static string GenerateShortID()
        {
            return UnityEngine.Random.Range(1000, 9999).ToString();
        }

        /// <summary>
        /// Plant logging utility
        /// </summary>
        protected void LogPlantAction(string message)
        {
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Plant instance metrics data structure
        /// </summary>
        public class PlantInstanceMetrics
        {
            public string PlantID { get; set; }
            public string PlantName { get; set; }
            public bool IsActive { get; set; }
            public bool IsInitialized { get; set; }
            public double InitializationTime { get; set; }
            public int DaysSincePlanted { get; set; }
            public int SystemCount { get; set; }
            public bool GrowthSystemActive { get; set; }
            public bool HealthSystemActive { get; set; }
            public bool EnvironmentalSystemActive { get; set; }
            public bool GeneticsSystemActive { get; set; }
            public bool VisualizationSystemActive { get; set; }
        }
    }

    /// <summary>
    /// Base class for plant systems
    /// </summary>
    public abstract class PlantSystemBase : MonoBehaviour
    {
        protected PlantInstanceCore _plantCore;
        protected bool _isInitialized = false;

        public virtual void Initialize(PlantInstanceCore plantCore)
        {
            _plantCore = plantCore;
            _isInitialized = true;
        }

        public virtual void InitializeFromStrain(object strain)
        {
            // Override in derived classes to handle strain-specific initialization
        }

        public abstract void UpdateSystem(float deltaTime);

        public virtual bool IsSystemHealthy()
        {
            return _isInitialized && _plantCore != null;
        }

        protected void LogSystemAction(string message)
        {
            if (_plantCore != null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        protected void LogSystemWarning(string message)
        {
            if (_plantCore != null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        protected void LogSystemError(string message)
        {
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }
    }
}
