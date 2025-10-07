using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;
using System.Collections;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation Initializer - Focused system initialization and startup management
    /// Handles system initialization sequencing, dependency resolution, and startup coordination
    /// Single Responsibility: System initialization and startup orchestration
    /// </summary>
    public class FoundationInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool _enableInitialization = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableDependencyOrdering = true;
        [SerializeField] private float _initializationTimeout = 30f;

        [Header("Initialization Strategy")]
        [SerializeField] private InitializationMode _initializationMode = InitializationMode.Sequential;
        [SerializeField] private int _maxConcurrentInitializations = 3;
        [SerializeField] private float _initializationInterval = 0.1f;
        [SerializeField] private bool _stopOnFirstFailure = false;

        // Initialization tracking
        private readonly Dictionary<string, InitializationStatus> _initializationStatuses = new Dictionary<string, InitializationStatus>();
        private readonly Queue<string> _initializationQueue = new Queue<string>();
        private readonly HashSet<string> _currentlyInitializing = new HashSet<string>();
        private readonly List<string> _failedInitializations = new List<string>();

        // System references
        private FoundationSystemRegistry _systemRegistry;

        // Statistics
        private InitializerStats _stats = new InitializerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsInitializing { get; private set; } = false;
        public InitializerStats GetStats() => _stats;

        // Events
        public System.Action<IFoundationSystem> OnSystemInitialized;
        public System.Action<string, string> OnSystemInitializationFailed;
        public System.Action OnAllSystemsInitialized;
        public System.Action<float> OnInitializationProgress;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new InitializerStats();

            // Use SafeResolve helper for clean dependency injection with fallback
            _systemRegistry = DependencyResolutionHelper.SafeResolve<FoundationSystemRegistry>(this, "FOUNDATION");

            if (_systemRegistry == null)
            {
                ChimeraLogger.LogError("FOUNDATION", "Critical dependency FoundationSystemRegistry not found", this);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🚀 FoundationInitializer initialized", this);
        }

        /// <summary>
        /// Initialize specific system by name
        /// </summary>
        public bool InitializeSystem(string systemName)
        {
            if (!IsEnabled || !_enableInitialization || string.IsNullOrEmpty(systemName))
                return false;

            if (_systemRegistry == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", "System registry not found for initialization", this);
                return false;
            }

            var system = _systemRegistry.GetSystem(systemName);
            if (system == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"System not found for initialization: {systemName}", this);
                return false;
            }

            if (system.IsInitialized)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"System already initialized: {systemName}", this);
                return true;
            }

            return StartSingleSystemInitialization(system);
        }

        /// <summary>
        /// Initialize all registered systems
        /// </summary>
        public bool InitializeAllSystems()
        {
            if (!IsEnabled || !_enableInitialization || IsInitializing)
                return false;

            if (_systemRegistry == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", "System registry not found for bulk initialization", this);
                return false;
            }

            var systems = _systemRegistry.GetRegisteredSystems();
            if (systems.Length == 0)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", "No systems registered for initialization", this);
                return true;
            }

            StartBulkInitialization(systems);
            return true;
        }

        /// <summary>
        /// Initialize systems by category
        /// </summary>
        public bool InitializeSystemsByCategory(SystemCategory category)
        {
            if (!IsEnabled || !_enableInitialization)
                return false;

            var systems = _systemRegistry?.GetSystemsByCategory(category);
            if (systems == null || systems.Length == 0)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"No systems found for category: {category}", this);
                return true;
            }

            StartBulkInitialization(systems);
            return true;
        }

        /// <summary>
        /// Get initialization status for system
        /// </summary>
        public InitializationStatus GetInitializationStatus(string systemName)
        {
            if (_initializationStatuses.TryGetValue(systemName, out var status))
                return status;

            return InitializationStatus.NotStarted;
        }

        /// <summary>
        /// Check if system is currently initializing
        /// </summary>
        public bool IsSystemInitializing(string systemName)
        {
            return _currentlyInitializing.Contains(systemName);
        }

        /// <summary>
        /// Get failed initialization systems
        /// </summary>
        public string[] GetFailedInitializations()
        {
            return _failedInitializations.ToArray();
        }

        /// <summary>
        /// Retry failed system initialization
        /// </summary>
        public bool RetrySystemInitialization(string systemName)
        {
            if (!_failedInitializations.Contains(systemName))
                return false;

            _failedInitializations.Remove(systemName);
            _initializationStatuses.Remove(systemName);

            return InitializeSystem(systemName);
        }

        /// <summary>
        /// Get initialization progress (0.0 to 1.0)
        /// </summary>
        public float GetInitializationProgress()
        {
            if (_initializationStatuses.Count == 0)
                return 1.0f;

            int completed = _initializationStatuses.Values.Count(s => s == InitializationStatus.Completed || s == InitializationStatus.Failed);
            return (float)completed / _initializationStatuses.Count;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled && IsInitializing)
            {
                StopAllCoroutines();
                IsInitializing = false;
                _currentlyInitializing.Clear();
                _initializationQueue.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationInitializer: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Start single system initialization
        /// </summary>
        private bool StartSingleSystemInitialization(IFoundationSystem system)
        {
            if (_currentlyInitializing.Contains(system.SystemName))
                return false;

            // Check dependencies if enabled
            if (_enableDependencyOrdering && _systemRegistry != null)
            {
                if (!_systemRegistry.AreSystemDependenciesSatisfied(system.SystemName))
                {
                    if (_enableLogging)
                        ChimeraLogger.LogWarning("FOUNDATION", $"Dependencies not satisfied for: {system.SystemName}", this);
                    return false;
                }
            }

            StartCoroutine(InitializeSystemCoroutine(system));
            return true;
        }

        /// <summary>
        /// Start bulk system initialization
        /// </summary>
        private void StartBulkInitialization(IFoundationSystem[] systems)
        {
            IsInitializing = true;
            _initializationQueue.Clear();
            _currentlyInitializing.Clear();
            _failedInitializations.Clear();
            _initializationStatuses.Clear();

            // Prepare systems for initialization
            var systemsToInit = systems.Where(s => !s.IsInitialized).ToArray();

            foreach (var system in systemsToInit)
            {
                _initializationStatuses[system.SystemName] = InitializationStatus.Queued;
            }

            // Order systems based on dependencies if enabled
            if (_enableDependencyOrdering && _systemRegistry != null)
            {
                var orderedNames = _systemRegistry.GetSystemsInDependencyOrder();
                var orderedSystems = orderedNames
                    .Select(name => systems.FirstOrDefault(s => s.SystemName == name))
                    .Where(s => s != null && !s.IsInitialized);

                foreach (var system in orderedSystems)
                {
                    _initializationQueue.Enqueue(system.SystemName);
                }
            }
            else
            {
                foreach (var system in systemsToInit)
                {
                    _initializationQueue.Enqueue(system.SystemName);
                }
            }

            _stats.TotalSystemsToInitialize = _initializationQueue.Count;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Starting bulk initialization of {_initializationQueue.Count} systems", this);

            StartCoroutine(ProcessInitializationQueue());
        }

        /// <summary>
        /// Process initialization queue
        /// </summary>
        private IEnumerator ProcessInitializationQueue()
        {
            while (_initializationQueue.Count > 0 || _currentlyInitializing.Count > 0)
            {
                // Start new initializations if under concurrent limit
                while (_currentlyInitializing.Count < _maxConcurrentInitializations && _initializationQueue.Count > 0)
                {
                    string systemName = _initializationQueue.Dequeue();
                    var system = _systemRegistry?.GetSystem(systemName);

                    if (system != null && !system.IsInitialized)
                    {
                        StartCoroutine(InitializeSystemCoroutine(system));
                    }
                }

                // Update progress
                OnInitializationProgress?.Invoke(GetInitializationProgress());

                yield return new WaitForSeconds(_initializationInterval);

                // Check for timeout or failure conditions
                if (_stopOnFirstFailure && _failedInitializations.Count > 0)
                {
                    if (_enableLogging)
                        ChimeraLogger.LogError("FOUNDATION", "Stopping initialization due to failure", this);
                    break;
                }
            }

            // Complete initialization
            IsInitializing = false;
            OnAllSystemsInitialized?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION",
                    $"Bulk initialization completed. Success: {_stats.InitializedSystems}, Failed: {_stats.FailedInitializations}", this);
            }
        }

        /// <summary>
        /// Initialize individual system coroutine
        /// </summary>
        private IEnumerator InitializeSystemCoroutine(IFoundationSystem system)
        {
            _currentlyInitializing.Add(system.SystemName);
            _initializationStatuses[system.SystemName] = InitializationStatus.Initializing;

            float startTime = Time.time;
            bool success = false;
            string errorMessage = null;

            try
            {
                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"Initializing system: {system.SystemName}", this);

                success = system.Initialize();
            }
            catch (System.Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }

            // Yield outside of try/catch to comply with coroutine rules
            if (success)
            {
                // Wait a frame to allow initialization to complete
                yield return null;

                if (!system.IsInitialized)
                {
                    // Wait a bit more for async initialization
                    float waitTime = 0;
                    while (!system.IsInitialized && waitTime < 5f)
                    {
                        yield return new WaitForSeconds(0.1f);
                        waitTime += 0.1f;
                    }
                    success = system.IsInitialized;
                }
            }

            float duration = Time.time - startTime;

            if (success)
            {
                _initializationStatuses[system.SystemName] = InitializationStatus.Completed;
                _stats.InitializedSystems++;
                _stats.TotalInitializationTime += duration;

                OnSystemInitialized?.Invoke(system);

                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"Successfully initialized: {system.SystemName} ({duration:F2}s)", this);
            }
            else
            {
                _initializationStatuses[system.SystemName] = InitializationStatus.Failed;
                _failedInitializations.Add(system.SystemName);
                _stats.FailedInitializations++;

                OnSystemInitializationFailed?.Invoke(system.SystemName, errorMessage ?? "Initialization failed");

                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", $"Failed to initialize: {system.SystemName} - {errorMessage}", this);
            }

            _currentlyInitializing.Remove(system.SystemName);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Initialization mode enumeration
    /// </summary>
    public enum InitializationMode
    {
        Sequential,
        Parallel,
        DependencyOrdered
    }

    /// <summary>
    /// Initialization status enumeration
    /// </summary>
    public enum InitializationStatus
    {
        NotStarted,
        Queued,
        Initializing,
        Completed,
        Failed
    }

    /// <summary>
    /// Initializer statistics
    /// </summary>
    [System.Serializable]
    public struct InitializerStats
    {
        public int TotalSystemsToInitialize;
        public int InitializedSystems;
        public int FailedInitializations;
        public float TotalInitializationTime;
        public float AverageInitializationTime;
    }

    #endregion
}
