using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Streaming.LOD;
using ProjectChimera.Core.Memory;

namespace ProjectChimera.Core.Streaming.Subsystems
{
    /// <summary>
    /// REFACTORED: Streaming Initialization Manager - Focused system initialization and lifecycle management
    /// Handles initialization sequencing, dependency management, and system startup coordination
    /// Single Responsibility: System initialization and lifecycle management
    /// </summary>
    public class StreamingInitializationManager : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool _enableInitialization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _initializationDelay = 1f;
        [SerializeField] private bool _waitForSceneReady = true;

        [Header("Initialization Sequence")]
        [SerializeField] private bool _initializeAssetStreaming = true;
        [SerializeField] private bool _initializeLODSystem = true;
        [SerializeField] private bool _initializePerformanceMonitoring = true;
        [SerializeField] private bool _initializePlantStreaming = true;
        [SerializeField] private bool _initializeMemoryManagement = true;

        // System references
        private AssetStreamingManager _assetStreaming;
        private LODManager _lodManager;
        private StreamingPerformanceMonitor _performanceMonitor;
        private GCOptimizationManager _gcManager;

        // Initialization state
        private bool _isInitialized = false;
        private readonly Dictionary<string, bool> _systemInitializationStatus = new Dictionary<string, bool>();
        private float _initializationStartTime;

        // Properties
        public bool IsInitialized => _isInitialized;
        public bool IsEnabled { get; private set; } = true;
        public Dictionary<string, bool> GetInitializationStatus() => new Dictionary<string, bool>(_systemInitializationStatus);

        // Events
        public System.Action OnInitializationStarted;
        public System.Action OnInitializationCompleted;
        public System.Action<string, bool> OnSystemInitialized;
        public System.Action<Scene, LoadSceneMode> OnSceneLoadedEvent;
        public System.Action<Scene> OnSceneUnloadedEvent;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeSystemReferences();
            SetupSceneEventHandlers();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🚀 StreamingInitializationManager initialized", this);
        }

        /// <summary>
        /// Initialize system references using dependency injection
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _assetStreaming = DependencyResolutionHelper.SafeResolve<AssetStreamingManager>(this, "STREAMING");
            _lodManager = DependencyResolutionHelper.SafeResolve<LODManager>(this, "STREAMING");
            _performanceMonitor = DependencyResolutionHelper.SafeResolve<StreamingPerformanceMonitor>(this, "STREAMING");
            _gcManager = DependencyResolutionHelper.SafeResolve<GCOptimizationManager>(this, "STREAMING");

            // Log missing dependencies
            if (_assetStreaming == null && _initializeAssetStreaming)
                ChimeraLogger.LogWarning("STREAMING", "AssetStreamingManager not found - asset streaming disabled", this);

            if (_lodManager == null && _initializeLODSystem)
                ChimeraLogger.LogWarning("STREAMING", "LODManager not found - LOD system disabled", this);

            if (_performanceMonitor == null && _initializePerformanceMonitoring)
                ChimeraLogger.LogWarning("STREAMING", "StreamingPerformanceMonitor not found - performance monitoring disabled", this);

            if (_gcManager == null && _initializeMemoryManagement)
                ChimeraLogger.LogWarning("STREAMING", "GCOptimizationManager not found - memory management disabled", this);
        }

        /// <summary>
        /// Set up scene event handlers
        /// </summary>
        private void SetupSceneEventHandlers()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        /// <summary>
        /// Start streaming systems initialization
        /// </summary>
        public void StartInitialization()
        {
            if (!IsEnabled || !_enableInitialization) return;

            if (_isInitialized)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", "StreamingInitializationManager already initialized", this);
                return;
            }

            StartCoroutine(InitializeStreamingSystems());
        }

        /// <summary>
        /// Force reinitialization of all systems
        /// </summary>
        public void ForceReinitialization()
        {
            if (!IsEnabled) return;

            _isInitialized = false;
            _systemInitializationStatus.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Forcing reinitialization of streaming systems", this);

            StartCoroutine(InitializeStreamingSystems());
        }

        /// <summary>
        /// Check if specific system is initialized
        /// </summary>
        public bool IsSystemInitialized(string systemName)
        {
            return _systemInitializationStatus.TryGetValue(systemName, out var initialized) && initialized;
        }

        /// <summary>
        /// Get initialization progress (0.0 to 1.0)
        /// </summary>
        public float GetInitializationProgress()
        {
            if (_systemInitializationStatus.Count == 0) return 0f;

            var initializedCount = 0;
            foreach (var status in _systemInitializationStatus.Values)
            {
                if (status) initializedCount++;
            }

            return (float)initializedCount / _systemInitializationStatus.Count;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _isInitialized = false;
                _systemInitializationStatus.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingInitializationManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize all streaming systems in sequence
        /// </summary>
        private IEnumerator InitializeStreamingSystems()
        {
            _initializationStartTime = Time.time;
            OnInitializationStarted?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Starting streaming systems initialization sequence", this);

            // Wait for initial delay
            yield return new WaitForSeconds(_initializationDelay);

            // Wait for scene to be ready if required
            if (_waitForSceneReady)
            {
                yield return new WaitUntil(() => Time.time > 2f); // Basic scene readiness check
            }

            // Initialize systems in dependency order
            if (_initializeAssetStreaming)
                yield return StartCoroutine(InitializeAssetStreaming());

            if (_initializeLODSystem)
                yield return StartCoroutine(InitializeLODSystem());

            if (_initializePerformanceMonitoring)
                yield return StartCoroutine(InitializePerformanceMonitoring());

            if (_initializePlantStreaming)
                yield return StartCoroutine(InitializePlantStreaming());

            if (_initializeMemoryManagement)
                yield return StartCoroutine(InitializeMemoryManagement());

            // Complete initialization
            CompleteInitialization();
        }

        /// <summary>
        /// Initialize asset streaming system
        /// </summary>
        private IEnumerator InitializeAssetStreaming()
        {
            const string systemName = "AssetStreaming";
            _systemInitializationStatus[systemName] = false;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Initializing Asset Streaming system", this);

            if (_assetStreaming != null)
            {
                // Initialize asset streaming (simplified)
                yield return new WaitForEndOfFrame();
                _systemInitializationStatus[systemName] = true;
                OnSystemInitialized?.Invoke(systemName, true);
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", "AssetStreamingManager not found", this);
                OnSystemInitialized?.Invoke(systemName, false);
            }
        }

        /// <summary>
        /// Initialize LOD system
        /// </summary>
        private IEnumerator InitializeLODSystem()
        {
            const string systemName = "LODSystem";
            _systemInitializationStatus[systemName] = false;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Initializing LOD system", this);

            if (_lodManager != null)
            {
                // Initialize LOD system (simplified)
                yield return new WaitForEndOfFrame();
                _systemInitializationStatus[systemName] = true;
                OnSystemInitialized?.Invoke(systemName, true);
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", "LODManager not found", this);
                OnSystemInitialized?.Invoke(systemName, false);
            }
        }

        /// <summary>
        /// Initialize performance monitoring
        /// </summary>
        private IEnumerator InitializePerformanceMonitoring()
        {
            const string systemName = "PerformanceMonitoring";
            _systemInitializationStatus[systemName] = false;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Initializing Performance Monitoring", this);

            if (_performanceMonitor != null)
            {
                // Initialize performance monitoring (simplified)
                yield return new WaitForEndOfFrame();
                _systemInitializationStatus[systemName] = true;
                OnSystemInitialized?.Invoke(systemName, true);
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", "StreamingPerformanceMonitor not found", this);
                OnSystemInitialized?.Invoke(systemName, false);
            }
        }

        /// <summary>
        /// Initialize plant streaming
        /// </summary>
        private IEnumerator InitializePlantStreaming()
        {
            const string systemName = "PlantStreaming";
            _systemInitializationStatus[systemName] = false;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Initializing Plant Streaming", this);

            // Plant streaming initialization (simplified)
            yield return new WaitForEndOfFrame();
            _systemInitializationStatus[systemName] = true;
            OnSystemInitialized?.Invoke(systemName, true);
        }

        /// <summary>
        /// Initialize memory management
        /// </summary>
        private IEnumerator InitializeMemoryManagement()
        {
            const string systemName = "MemoryManagement";
            _systemInitializationStatus[systemName] = false;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Initializing Memory Management", this);

            if (_gcManager != null)
            {
                // Initialize memory management (simplified)
                yield return new WaitForEndOfFrame();
                _systemInitializationStatus[systemName] = true;
                OnSystemInitialized?.Invoke(systemName, true);
            }
            else
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", "GCOptimizationManager not found", this);
                OnSystemInitialized?.Invoke(systemName, false);
            }
        }

        /// <summary>
        /// Complete initialization process
        /// </summary>
        private void CompleteInitialization()
        {
            _isInitialized = true;
            var initializationTime = Time.time - _initializationStartTime;

            OnInitializationCompleted?.Invoke();

            if (_enableLogging)
            {
                var successCount = 0;
                foreach (var status in _systemInitializationStatus.Values)
                {
                    if (status) successCount++;
                }

                ChimeraLogger.Log("STREAMING",
                    $"Streaming systems initialization completed: {successCount}/{_systemInitializationStatus.Count} systems initialized in {initializationTime:F2}s",
                    this);
            }
        }

        /// <summary>
        /// Handle scene loaded event
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Scene loaded: {scene.name} (Mode: {mode})", this);

            OnSceneLoadedEvent?.Invoke(scene, mode);
        }

        /// <summary>
        /// Handle scene unloaded event
        /// </summary>
        private void HandleSceneUnloaded(Scene scene)
        {
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Scene unloaded: {scene.name}", this);

            OnSceneUnloadedEvent?.Invoke(scene);
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up scene event handlers
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }
    }
}
