using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Simple metrics class for UI compatibility
    /// </summary>
    public class ManagerMetrics
    {
        public virtual object ActiveProjects { get; set; } = 0;
        public virtual object CompletedProjects { get; set; } = 0;
        public virtual object TotalValue { get; set; } = 0f;
        public virtual object ActiveWorkers { get; set; } = 0;
        public virtual object ConstructionEfficiency { get; set; } = 1f;
    }

    /// <summary>
    /// Priority levels for manager initialization and update ordering.
    /// </summary>
    public enum ManagerPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Base class for all Project Chimera manager components.
    /// Managers coordinate major game systems and maintain singleton-like behavior.
    /// </summary>
    public abstract class ChimeraManager : ChimeraMonoBehaviour, IChimeraManager
    {
        [Header("Manager Properties")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _persistAcrossScenes = false;

        /// <summary>
        /// Human-readable name of this manager.
        /// </summary>
        public virtual string ManagerName => GetType().Name;

        /// <summary>
        /// Whether this manager is currently initialized and running.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Whether this manager should persist across scene loads.
        /// </summary>
        public bool PersistAcrossScenes => _persistAcrossScenes;

        /// <summary>
        /// Priority of this manager for initialization and update ordering.
        /// </summary>
        public virtual ManagerPriority Priority => ManagerPriority.Normal;

        // Events that UI systems expect to be available
        public virtual event System.Action<object> OnMarketConditionsChanged;
        public virtual event System.Action<object> OnPlantDeletedObject;
        public virtual event System.Action<object> OnPlantHarvestedObject;
        public virtual event System.Action<object> OnPlantStageChangedObject;
        public virtual event System.Action<object> OnResearchCompletedObject;
        public virtual event System.Action<object> OnResearchStartedObject;
        public virtual event System.Action<object> OnSaleCompletedObject;
        public virtual event System.Action<object> OnProjectStartedObject;
        public virtual event System.Action<object> OnProjectCompletedObject;
        public virtual event System.Action<object> OnConstructionIssueObject;
        public virtual event System.Action<object> OnConditionsChangedObject;
        public virtual event System.Action<object> OnAlertTriggeredObject;
        
        // Additional events without Object suffix that UI systems may expect
        public virtual event System.Action<object> OnPlantAdded;
        public virtual event System.Action<object> OnPlantHarvested;
        public virtual event System.Action<object> OnPlantStageChanged;
        public virtual event System.Action<object> OnResearchCompleted;
        public virtual event System.Action<object> OnResearchStarted;
        public virtual event System.Action<object> OnSaleCompleted;
        public virtual event System.Action<object> OnProjectStarted;
        public virtual event System.Action<object> OnProjectCompleted;
        public virtual event System.Action<object> OnConstructionIssue;
        
        // Properties that UI systems may access
        public virtual object PlayerFunds { get; protected set; }
        public virtual object PowerConsumption { get; protected set; }
        public virtual object PerformanceMetrics { get; protected set; }
        public virtual object Metrics { get; protected set; } = new { ActiveProjects = 0, CompletedProjects = 0, ConstructionEfficiency = 1f, TotalValue = 0f, ActiveWorkers = 0 };
        public virtual object AllProjects { get; protected set; }
        
        // Additional properties that UI systems expect
        public virtual object GetAtlData { get; protected set; } = new object();
        public virtual object ProjectName { get; protected set; } = "Default Project";
        public virtual object Status { get; protected set; } = "Active";
        public virtual object Efficiency { get; protected set; } = 1f;
        public virtual object ActiveProjects { get; protected set; } = 0;
        public virtual object CompletedProjects { get; protected set; } = 0;
        public virtual object TotalValue { get; protected set; } = 0f;
        public virtual object ActiveWorkers { get; protected set; } = 0;
        public virtual object ConstructionEfficiency { get; protected set; } = 1f;

        protected override void Awake()
        {
            base.Awake();

            // Handle persistence across scenes
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Initialize immediately if configured to do so
            if (_initializeOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize the manager and prepare it for use (IChimeraManager interface implementation).
        /// Should be idempotent - calling multiple times should not cause issues.
        /// </summary>
        public virtual void Initialize()
        {
            InitializeManager();
        }

        /// <summary>
        /// Shutdown the manager and clean up resources (IChimeraManager interface implementation).
        /// Should be safe to call even if not initialized.
        /// </summary>
        public virtual void Shutdown()
        {
            ShutdownManager();
        }

        /// <summary>
        /// Initializes the manager. Can be called manually or automatically on Awake.
        /// </summary>
        public virtual void InitializeManager()
        {
            if (IsInitialized)
            {
                LogWarning("Manager is already initialized");
                return;
            }

            LogDebug("Initializing manager");
            
            OnManagerInitialize();
            IsInitialized = true;
            
            LogDebug("Manager initialization complete");
        }

        /// <summary>
        /// Shuts down the manager and cleans up resources.
        /// </summary>
        public virtual void ShutdownManager()
        {
            if (!IsInitialized)
            {
                LogWarning("Manager is not initialized");
                return;
            }

            LogDebug("Shutting down manager");
            
            OnManagerShutdown();
            IsInitialized = false;
            
            LogDebug("Manager shutdown complete");
        }

        /// <summary>
        /// Override this method to implement manager-specific initialization logic.
        /// </summary>
        protected abstract void OnManagerInitialize();

        /// <summary>
        /// Override this method to implement manager-specific shutdown logic.
        /// </summary>
        protected abstract void OnManagerShutdown();

        protected override void OnDestroy()
        {
            if (IsInitialized)
            {
                Shutdown();
            }
            base.OnDestroy();
        }

        /// <summary>
        /// Called every frame if the manager needs to perform updates.
        /// Override in derived classes that need frame-by-frame updates.
        /// </summary>
        protected virtual void Update()
        {
            if (!IsInitialized) return;

            OnManagerUpdate();
        }

        /// <summary>
        /// Override this method to implement manager-specific update logic.
        /// Only called when the manager is initialized.
        /// </summary>
        protected virtual void OnManagerUpdate()
        {
            // Base implementation - override in derived classes
        }

        /// <summary>
        /// Called at fixed intervals for physics-related updates.
        /// Override in derived classes that need fixed timestep updates.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            if (!IsInitialized) return;

            OnManagerFixedUpdate();
        }

        /// <summary>
        /// Override this method to implement manager-specific fixed update logic.
        /// Only called when the manager is initialized.
        /// </summary>
        protected virtual void OnManagerFixedUpdate()
        {
            // Base implementation - override in derived classes
        }


    }
}