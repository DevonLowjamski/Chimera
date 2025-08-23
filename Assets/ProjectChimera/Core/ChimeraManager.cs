using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Generic metrics class for all manager types.
    /// Provides basic performance and status information.
    /// </summary>
    public class ManagerMetrics
    {
        public string ManagerName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; } = true;
        public float Performance { get; set; } = 1f;
        public int ManagedItems { get; set; } = 0;
        public float Uptime { get; set; } = 0f;
        public string LastActivity { get; set; } = "Initialized";
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
    /// 
    /// SINGLE RESPONSIBILITY: Manages the lifecycle and health monitoring of game system managers.
    /// 
    /// This class is responsible ONLY for:
    /// - Manager initialization and shutdown lifecycle
    /// - Health monitoring and diagnostics
    /// - Basic metrics collection and reporting
    /// - Unity MonoBehaviour integration
    /// 
    /// This class does NOT handle:
    /// - Domain-specific business logic (handled by derived managers)
    /// - Update loops (handled by centralized update bus system)
    /// - Direct game object manipulation (handled by service components)
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

        /// <summary>
        /// Gets comprehensive metrics for this manager.
        /// Override in derived classes to provide specific metrics.
        /// </summary>
        public virtual ManagerMetrics GetMetrics()
        {
            return new ManagerMetrics
            {
                ManagerName = ManagerName,
                IsHealthy = ValidateHealth(),
                Performance = GetEfficiency(),
                ManagedItems = GetManagedItemCount(),
                Uptime = GetUptime(),
                LastActivity = GetLastActivity()
            };
        }
        
        /// <summary>
        /// Gets the current operational status of this manager.
        /// Override in derived classes to provide specific status information.
        /// </summary>
        public virtual string GetStatus() => IsInitialized ? "Active" : "Inactive";
        
        /// <summary>
        /// Gets the current efficiency rating (0.0 to 1.0).
        /// Override in derived classes to provide specific efficiency calculations.
        /// </summary>
        public virtual float GetEfficiency() => IsInitialized ? 1f : 0f;
        
        /// <summary>
        /// Gets the number of items this manager is currently managing.
        /// Override in derived classes to provide specific counts.
        /// </summary>
        protected virtual int GetManagedItemCount() => 0;
        
        /// <summary>
        /// Gets the uptime in hours since initialization.
        /// Override in derived classes for more precise tracking.
        /// </summary>
        protected virtual float GetUptime() => IsInitialized ? Time.time / 3600f : 0f;
        
        /// <summary>
        /// Gets a description of the last significant activity.
        /// Override in derived classes to track specific activities.
        /// </summary>
        protected virtual string GetLastActivity() => IsInitialized ? "Running" : "Not Initialized";

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
        /// Validates that the manager is functioning correctly.
        /// Override in derived classes to provide specific health checks.
        /// </summary>
        public virtual bool ValidateHealth()
        {
            return IsInitialized;
        }

        /// <summary>
        /// Gets detailed diagnostic information about the manager.
        /// Override in derived classes to provide specific diagnostics.
        /// </summary>
        public virtual string GetDiagnostics()
        {
            return $"Manager: {ManagerName}, Initialized: {IsInitialized}, Priority: {Priority}";
        }


    }
}