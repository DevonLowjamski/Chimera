using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core.Streaming.Subsystems
{
    /// <summary>
    /// REFACTORED: Streaming Core - Central coordination for streaming subsystems
    /// Coordinates streaming initialization, quality management, memory management, and system health
    /// Single Responsibility: Central streaming system coordination
    /// </summary>
    public class StreamingCore : MonoBehaviour, ITickable
    {
        [Header("Core Settings")]
        [SerializeField] private bool _enableStreamingCoordination = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 1f;

        // Subsystem references
        private StreamingInitializationManager _initializationManager;
        private StreamingQualityManager _qualityManager;
        private StreamingMemoryManager _memoryManager;

        // Timing
        private float _lastUpdate;

        // System health
        private StreamingSystemHealth _systemHealth = StreamingSystemHealth.Healthy;
        private StreamingCoordinatorStats _stats = new StreamingCoordinatorStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsInitialized => _initializationManager?.IsInitialized ?? false;
        public StreamingSystemHealth SystemHealth => _systemHealth;
        public StreamingCoordinatorStats Stats => _stats;

        // Singleton pattern for backward compatibility - migrated to ServiceContainer
        private static StreamingCore _instance;
        public static StreamingCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to resolve via ServiceContainer first
                    _instance = ServiceContainerFactory.Instance?.TryResolve<StreamingCore>();

                    if (_instance == null)
                    {
                        ChimeraLogger.LogWarning("STREAMING",
                            "StreamingCore not registered in ServiceContainer - singleton pattern may not work correctly",
                            null);
                    }
                }
                return _instance;
            }
        }

        // Events for backward compatibility
        public System.Action OnStreamingInitialized;
        public System.Action<int> OnQualityChanged;
        public System.Action<StreamingSystemHealth> OnHealthChanged;

        public int TickPriority => 70;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;

                // Register with ServiceContainer as singleton
                ServiceContainerFactory.Instance?.RegisterSingleton<StreamingCore>(this);
            }
            else if (_instance != this)
            {
                Destroy(this);
                return;
            }
        }

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void Initialize()
        {
            InitializeSubsystems();
            ConnectEventHandlers();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "⚡ StreamingCore initialized", this);
        }

        /// <summary>
        /// Initialize all streaming subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _initializationManager = GetOrCreateComponent<StreamingInitializationManager>();
            _qualityManager = GetOrCreateComponent<StreamingQualityManager>();
            _memoryManager = GetOrCreateComponent<StreamingMemoryManager>();

            // Configure subsystems
            _initializationManager?.SetEnabled(_enableStreamingCoordination);
            _qualityManager?.SetEnabled(_enableStreamingCoordination);
            _memoryManager?.SetEnabled(_enableStreamingCoordination);
        }

        /// <summary>
        /// Connect event handlers between subsystems
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_initializationManager != null)
            {
                _initializationManager.OnInitializationCompleted += HandleInitializationCompleted;
                _initializationManager.OnSceneLoadedEvent += HandleSceneLoaded;
            }

            if (_qualityManager != null)
            {
                _qualityManager.OnQualityChanged += HandleQualityChanged;
            }

            if (_memoryManager != null)
            {
                _memoryManager.OnMemoryPressureDetected += HandleMemoryPressureDetected;
                _memoryManager.OnCriticalMemoryReached += HandleCriticalMemoryReached;
            }
        }

        /// <summary>
        /// Get or create subsystem component
        /// </summary>
        private T GetOrCreateComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableStreamingCoordination) return;

            if (Time.time - _lastUpdate < _updateInterval) return;

            ProcessStreamingCoordination();
            UpdateSystemHealth();

            _lastUpdate = Time.time;
        }

        /// <summary>
        /// Process streaming system coordination
        /// </summary>
        private void ProcessStreamingCoordination()
        {
            // Update quality management
            _qualityManager?.UpdateQualityManagement();

            // Update memory management
            _memoryManager?.UpdateMemoryManagement();

            // Update statistics
            UpdateStatistics();
        }

        /// <summary>
        /// Update system health based on subsystem status
        /// </summary>
        private void UpdateSystemHealth()
        {
            var previousHealth = _systemHealth;
            _systemHealth = DetermineSystemHealth();

            if (_systemHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(_systemHealth);

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"System health changed: {previousHealth} -> {_systemHealth}", this);
            }
        }

        /// <summary>
        /// Determine overall system health
        /// </summary>
        private StreamingSystemHealth DetermineSystemHealth()
        {
            // Check memory pressure
            if (_memoryManager != null && _memoryManager.IsMemoryPressureDetected)
            {
                var pressureLevel = _memoryManager.GetMemoryPressureLevel();
                if (pressureLevel == MemoryPressureLevel.Critical)
                    return StreamingSystemHealth.Critical;
                else if (pressureLevel >= MemoryPressureLevel.High)
                    return StreamingSystemHealth.Warning;
            }

            // Check initialization status
            if (!IsInitialized)
                return StreamingSystemHealth.Initializing;

            return StreamingSystemHealth.Healthy;
        }

        /// <summary>
        /// Update coordination statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.IsInitialized = IsInitialized;
            _stats.SystemHealth = _systemHealth;
            _stats.CurrentQualityIndex = _qualityManager?.CurrentQualityIndex ?? 1;
            _stats.MemoryUsageMB = (_memoryManager?.CurrentMemoryUsage ?? 0) / (1024 * 1024);
        }

        /// <summary>
        /// Start streaming systems initialization
        /// </summary>
        public void StartInitialization()
        {
            _initializationManager?.StartInitialization();
        }

        /// <summary>
        /// Set streaming quality profile
        /// </summary>
        public void SetQualityProfile(int profileIndex)
        {
            _qualityManager?.SetQualityProfile(profileIndex);
        }

        /// <summary>
        /// Force garbage collection
        /// </summary>
        public void ForceGarbageCollection()
        {
            _memoryManager?.ForceGarbageCollection();
        }

        /// <summary>
        /// Optimize streaming systems
        /// </summary>
        public void OptimizeStreaming()
        {
            _memoryManager?.OptimizeStreamingMemory();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Streaming optimization initiated", this);
        }

        /// <summary>
        /// Get streaming quality profiles
        /// </summary>
        public StreamingQualityProfile[] GetAvailableQualityProfiles()
        {
            return _qualityManager?.GetAvailableProfiles() ?? new StreamingQualityProfile[0];
        }

        /// <summary>
        /// Get current quality profile
        /// </summary>
        public StreamingQualityProfile GetCurrentQualityProfile()
        {
            return _qualityManager?.CurrentProfile ?? new StreamingQualityProfile();
        }

        /// <summary>
        /// Get memory usage information
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return _memoryManager?.CurrentMemoryUsage ?? 0;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _initializationManager?.SetEnabled(enabled);
            _qualityManager?.SetEnabled(enabled);
            _memoryManager?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Event Handlers

        private void HandleInitializationCompleted()
        {
            OnStreamingInitialized?.Invoke();
            _stats.InitializationTime = Time.time;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "All streaming systems initialized successfully", this);
        }

        private void HandleQualityChanged(int qualityIndex, StreamingQualityProfile profile)
        {
            OnQualityChanged?.Invoke(qualityIndex);
            _stats.QualityChanges++;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _stats.ScenesLoaded++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Scene loaded, reinitializing streaming systems: {scene.name}", this);
        }

        private void HandleMemoryPressureDetected(long memoryUsage)
        {
            _stats.MemoryPressureEvents++;

            if (_enableLogging)
                ChimeraLogger.LogWarning("STREAMING", $"Memory pressure detected: {memoryUsage / (1024 * 1024)} MB", this);
        }

        private void HandleCriticalMemoryReached(long memoryUsage)
        {
            _stats.CriticalMemoryEvents++;
            _systemHealth = StreamingSystemHealth.Critical;

            if (_enableLogging)
                ChimeraLogger.LogError("STREAMING", $"Critical memory reached: {memoryUsage / (1024 * 1024)} MB", this);
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            // Unregister from ServiceContainer
            if (_instance == this)
            {
                ServiceContainerFactory.Instance?.Unregister<StreamingCore>();
                _instance = null;
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// Streaming system health enumeration
    /// </summary>
    public enum StreamingSystemHealth
    {
        Initializing,
        Healthy,
        Warning,
        Critical,
        Failed
    }

    /// <summary>
    /// Streaming coordinator statistics
    /// </summary>
    [System.Serializable]
    public struct StreamingCoordinatorStats
    {
        public bool IsInitialized;
        public float InitializationTime;
        public StreamingSystemHealth SystemHealth;
        public int CurrentQualityIndex;
        public long MemoryUsageMB;
        public int QualityChanges;
        public int MemoryPressureEvents;
        public int CriticalMemoryEvents;
        public int ScenesLoaded;
    }

    #endregion
}