using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Updates
{
    /// <summary>
    /// Central update orchestrator that manages all ITickable objects
    /// Replaces scattered MonoBehaviour Update() calls with unified tick management
    /// Part of Phase 0.5 Central Update Bus implementation
    /// </summary>
    public class UpdateOrchestrator : MonoBehaviour, IUpdateOrchestrator
    {
        [Header("Orchestrator Settings")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _enablePerformanceTracking = true;
        [SerializeField] private int _performanceReportInterval = 300; // frames
        
        [Header("Runtime Statistics")]
        [SerializeField, ReadOnly] private int _registeredTickables = 0;
        [SerializeField, ReadOnly] private int _activeTickables = 0;
        [SerializeField, ReadOnly] private float _lastUpdateTime = 0f;
        [SerializeField, ReadOnly] private float _averageUpdateTime = 0f;
        
        // Tickable collections organized by priority for efficient processing
        private SortedList<int, List<ITickable>> _tickables = new SortedList<int, List<ITickable>>();
        private SortedList<int, List<IFixedTickable>> _fixedTickables = new SortedList<int, List<IFixedTickable>>();
        private SortedList<int, List<ILateTickable>> _lateTickables = new SortedList<int, List<ILateTickable>>();
        
        // Performance tracking
        private float _updateTimeAccumulator = 0f;
        private int _updateFrameCount = 0;
        private int _performanceReportCounter = 0;
        
        // Deferred operations to avoid collection modification during iteration
        private Queue<System.Action> _deferredOperations = new Queue<System.Action>();
        
        // Singleton pattern
        private static UpdateOrchestrator _instance;
        public static UpdateOrchestrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try ServiceContainer first, then fall back to scene search, finally create if needed
                    _instance = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("UpdateOrchestrator");
                        _instance = go.AddComponent<UpdateOrchestrator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                ChimeraLogger.LogWarning("[UpdateOrchestrator] Multiple instances detected - destroying duplicate");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogDebug("UpdateOrchestrator initialized");
        }
        
        private void Update()
        {
            var startTime = Time.realtimeSinceStartup;
            
            // Process deferred operations first
            ProcessDeferredOperations();
            
            // Tick all registered ITickable objects in priority order
            TickAll(Time.deltaTime);
            
            // Performance tracking
            if (_enablePerformanceTracking)
            {
                UpdatePerformanceTracking(startTime);
            }
        }
        
        private void FixedUpdate()
        {
            // Process deferred operations
            ProcessDeferredOperations();
            
            // Fixed tick all registered IFixedTickable objects in priority order
            FixedTickAll(Time.fixedDeltaTime);
        }
        
        private void LateUpdate()
        {
            // Process deferred operations
            ProcessDeferredOperations();
            
            // Late tick all registered ILateTickable objects in priority order
            LateTickAll(Time.deltaTime);
        }
        
        #endregion
        
        #region Registration Management
        
        #region IUpdateOrchestrator Implementation
        
        /// <summary>
        /// Register an ITickable for centralized updates (generic object version for DI)
        /// </summary>
        public void RegisterTickable(object tickable)
        {
            if (tickable is ITickable tickableObj)
            {
                RegisterTickable(tickableObj);
            }
            else
            {
                ChimeraLogger.LogError($"[UpdateOrchestrator] Object {tickable?.GetType().Name} does not implement ITickable");
            }
        }

        /// <summary>
        /// Unregister an ITickable from centralized updates (generic object version for DI)
        /// </summary>
        public void UnregisterTickable(object tickable)
        {
            if (tickable is ITickable tickableObj)
            {
                UnregisterTickable(tickableObj);
            }
        }

        /// <summary>
        /// Register an IFixedTickable for centralized fixed updates (generic object version for DI)
        /// </summary>
        public void RegisterFixedTickable(object fixedTickable)
        {
            if (fixedTickable is IFixedTickable fixedTickableObj)
            {
                RegisterFixedTickable(fixedTickableObj);
            }
            else
            {
                ChimeraLogger.LogError($"[UpdateOrchestrator] Object {fixedTickable?.GetType().Name} does not implement IFixedTickable");
            }
        }

        /// <summary>
        /// Unregister an IFixedTickable from centralized fixed updates (generic object version for DI)
        /// </summary>
        public void UnregisterFixedTickable(object fixedTickable)
        {
            if (fixedTickable is IFixedTickable fixedTickableObj)
            {
                UnregisterFixedTickable(fixedTickableObj);
            }
        }

        /// <summary>
        /// Register an ILateTickable for centralized late updates (generic object version for DI)
        /// </summary>
        public void RegisterLateTickable(object lateTickable)
        {
            if (lateTickable is ILateTickable lateTickableObj)
            {
                RegisterLateTickable(lateTickableObj);
            }
            else
            {
                ChimeraLogger.LogError($"[UpdateOrchestrator] Object {lateTickable?.GetType().Name} does not implement ILateTickable");
            }
        }

        /// <summary>
        /// Unregister an ILateTickable from centralized late updates (generic object version for DI)
        /// </summary>
        public void UnregisterLateTickable(object lateTickable)
        {
            if (lateTickable is ILateTickable lateTickableObj)
            {
                UnregisterLateTickable(lateTickableObj);
            }
        }

        /// <summary>
        /// Get current orchestrator statistics (generic object version for DI)
        /// </summary>
        object IUpdateOrchestrator.GetStatistics()
        {
            return GetStatistics();
        }

        #endregion

        #region Typed Registration Methods

        /// <summary>
        /// Register an ITickable for centralized updates
        /// </summary>
        public void RegisterTickable(ITickable tickable)
        {
            if (tickable == null)
            {
                ChimeraLogger.LogError("[UpdateOrchestrator] Cannot register null tickable");
                return;
            }
            
            _deferredOperations.Enqueue(() => RegisterTickableImmediate(tickable));
        }
        
        /// <summary>
        /// Unregister an ITickable from centralized updates
        /// </summary>
        public void UnregisterTickable(ITickable tickable)
        {
            if (tickable == null) return;
            
            _deferredOperations.Enqueue(() => UnregisterTickableImmediate(tickable));
        }
        
        /// <summary>
        /// Register an IFixedTickable for centralized fixed updates
        /// </summary>
        public void RegisterFixedTickable(IFixedTickable fixedTickable)
        {
            if (fixedTickable == null)
            {
                ChimeraLogger.LogError("[UpdateOrchestrator] Cannot register null fixed tickable");
                return;
            }
            
            _deferredOperations.Enqueue(() => RegisterFixedTickableImmediate(fixedTickable));
        }
        
        /// <summary>
        /// Unregister an IFixedTickable from centralized fixed updates
        /// </summary>
        public void UnregisterFixedTickable(IFixedTickable fixedTickable)
        {
            if (fixedTickable == null) return;
            
            _deferredOperations.Enqueue(() => UnregisterFixedTickableImmediate(fixedTickable));
        }
        
        /// <summary>
        /// Register an ILateTickable for centralized late updates
        /// </summary>
        public void RegisterLateTickable(ILateTickable lateTickable)
        {
            if (lateTickable == null)
            {
                ChimeraLogger.LogError("[UpdateOrchestrator] Cannot register null late tickable");
                return;
            }
            
            _deferredOperations.Enqueue(() => RegisterLateTickableImmediate(lateTickable));
        }
        
        /// <summary>
        /// Unregister an ILateTickable from centralized late updates
        /// </summary>
        public void UnregisterLateTickable(ILateTickable lateTickable)
        {
            if (lateTickable == null) return;
            
            _deferredOperations.Enqueue(() => UnregisterLateTickableImmediate(lateTickable));
        }
        
        #endregion
        
        #endregion
        
        #region Tick Processing
        
        /// <summary>
        /// Tick all registered ITickable objects in priority order
        /// </summary>
        private void TickAll(float deltaTime)
        {
            _activeTickables = 0;
            
            foreach (var priorityGroup in _tickables)
            {
                var tickableList = priorityGroup.Value;
                for (int i = tickableList.Count - 1; i >= 0; i--)
                {
                    var tickable = tickableList[i];
                    if (tickable == null)
                    {
                        // Clean up null references
                        tickableList.RemoveAt(i);
                        continue;
                    }
                    
                    if (tickable.Enabled)
                    {
                        try
                        {
                            tickable.Tick(deltaTime);
                            _activeTickables++;
                        }
                        catch (System.Exception ex)
                        {
                            ChimeraLogger.LogError($"[UpdateOrchestrator] Error in {tickable.GetType().Name}.Tick(): {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fixed tick all registered IFixedTickable objects in priority order
        /// </summary>
        private void FixedTickAll(float fixedDeltaTime)
        {
            foreach (var priorityGroup in _fixedTickables)
            {
                var fixedTickableList = priorityGroup.Value;
                for (int i = fixedTickableList.Count - 1; i >= 0; i--)
                {
                    var fixedTickable = fixedTickableList[i];
                    if (fixedTickable == null)
                    {
                        // Clean up null references
                        fixedTickableList.RemoveAt(i);
                        continue;
                    }
                    
                    if (fixedTickable.FixedEnabled)
                    {
                        try
                        {
                            fixedTickable.FixedTick(fixedDeltaTime);
                        }
                        catch (System.Exception ex)
                        {
                            ChimeraLogger.LogError($"[UpdateOrchestrator] Error in {fixedTickable.GetType().Name}.FixedTick(): {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Late tick all registered ILateTickable objects in priority order
        /// </summary>
        private void LateTickAll(float deltaTime)
        {
            foreach (var priorityGroup in _lateTickables)
            {
                var lateTickableList = priorityGroup.Value;
                for (int i = lateTickableList.Count - 1; i >= 0; i--)
                {
                    var lateTickable = lateTickableList[i];
                    if (lateTickable == null)
                    {
                        // Clean up null references
                        lateTickableList.RemoveAt(i);
                        continue;
                    }
                    
                    if (lateTickable.LateEnabled)
                    {
                        try
                        {
                            lateTickable.LateTick(deltaTime);
                        }
                        catch (System.Exception ex)
                        {
                            ChimeraLogger.LogError($"[UpdateOrchestrator] Error in {lateTickable.GetType().Name}.LateTick(): {ex.Message}");
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Internal Implementation
        
        private void RegisterTickableImmediate(ITickable tickable)
        {
            var priority = tickable.Priority;
            
            if (!_tickables.TryGetValue(priority, out var tickableList))
            {
                tickableList = new List<ITickable>();
                _tickables[priority] = tickableList;
            }
            
            if (!tickableList.Contains(tickable))
            {
                tickableList.Add(tickable);
                _registeredTickables++;
                
                try
                {
                    tickable.OnRegistered();
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError($"[UpdateOrchestrator] Error in {tickable.GetType().Name}.OnRegistered(): {ex.Message}");
                }
                
                LogDebug($"Registered {tickable.GetType().Name} with priority {priority}");
            }
        }
        
        private void UnregisterTickableImmediate(ITickable tickable)
        {
            var priority = tickable.Priority;
            
            if (_tickables.TryGetValue(priority, out var tickableList))
            {
                if (tickableList.Remove(tickable))
                {
                    _registeredTickables--;
                    
                    try
                    {
                        tickable.OnUnregistered();
                    }
                    catch (System.Exception ex)
                    {
                        ChimeraLogger.LogError($"[UpdateOrchestrator] Error in {tickable.GetType().Name}.OnUnregistered(): {ex.Message}");
                    }
                    
                    LogDebug($"Unregistered {tickable.GetType().Name}");
                }
                
                // Clean up empty priority groups
                if (tickableList.Count == 0)
                {
                    _tickables.Remove(priority);
                }
            }
        }
        
        private void RegisterFixedTickableImmediate(IFixedTickable fixedTickable)
        {
            var priority = fixedTickable.FixedPriority;
            
            if (!_fixedTickables.TryGetValue(priority, out var fixedTickableList))
            {
                fixedTickableList = new List<IFixedTickable>();
                _fixedTickables[priority] = fixedTickableList;
            }
            
            if (!fixedTickableList.Contains(fixedTickable))
            {
                fixedTickableList.Add(fixedTickable);
                LogDebug($"Registered {fixedTickable.GetType().Name} for fixed updates with priority {priority}");
            }
        }
        
        private void UnregisterFixedTickableImmediate(IFixedTickable fixedTickable)
        {
            var priority = fixedTickable.FixedPriority;
            
            if (_fixedTickables.TryGetValue(priority, out var fixedTickableList))
            {
                if (fixedTickableList.Remove(fixedTickable))
                {
                    LogDebug($"Unregistered {fixedTickable.GetType().Name} from fixed updates");
                }
                
                // Clean up empty priority groups
                if (fixedTickableList.Count == 0)
                {
                    _fixedTickables.Remove(priority);
                }
            }
        }
        
        private void RegisterLateTickableImmediate(ILateTickable lateTickable)
        {
            var priority = lateTickable.LatePriority;
            
            if (!_lateTickables.TryGetValue(priority, out var lateTickableList))
            {
                lateTickableList = new List<ILateTickable>();
                _lateTickables[priority] = lateTickableList;
            }
            
            if (!lateTickableList.Contains(lateTickable))
            {
                lateTickableList.Add(lateTickable);
                LogDebug($"Registered {lateTickable.GetType().Name} for late updates with priority {priority}");
            }
        }
        
        private void UnregisterLateTickableImmediate(ILateTickable lateTickable)
        {
            var priority = lateTickable.LatePriority;
            
            if (_lateTickables.TryGetValue(priority, out var lateTickableList))
            {
                if (lateTickableList.Remove(lateTickable))
                {
                    LogDebug($"Unregistered {lateTickable.GetType().Name} from late updates");
                }
                
                // Clean up empty priority groups
                if (lateTickableList.Count == 0)
                {
                    _lateTickables.Remove(priority);
                }
            }
        }
        
        private void ProcessDeferredOperations()
        {
            while (_deferredOperations.Count > 0)
            {
                var operation = _deferredOperations.Dequeue();
                try
                {
                    operation();
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError($"[UpdateOrchestrator] Error processing deferred operation: {ex.Message}");
                }
            }
        }
        
        private void UpdatePerformanceTracking(float startTime)
        {
            _lastUpdateTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to milliseconds
            _updateTimeAccumulator += _lastUpdateTime;
            _updateFrameCount++;
            
            _performanceReportCounter++;
            if (_performanceReportCounter >= _performanceReportInterval)
            {
                _averageUpdateTime = _updateTimeAccumulator / _updateFrameCount;
                
                if (_enableDebugLogging)
                {
                    LogDebug($"Performance Report - Avg: {_averageUpdateTime:F2}ms, Last: {_lastUpdateTime:F2}ms, Active: {_activeTickables}/{_registeredTickables}");
                }
                
                // Reset counters
                _updateTimeAccumulator = 0f;
                _updateFrameCount = 0;
                _performanceReportCounter = 0;
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log($"[UpdateOrchestrator] {message}");
            }
        }
        
        #endregion
        
        #region IChimeraManager Implementation
        
        public string ManagerName => "Update Orchestrator";
        public bool IsInitialized => _instance != null;
        
        public void Initialize()
        {
            // UpdateOrchestrator initializes automatically via Awake
            LogDebug("UpdateOrchestrator Initialize() called - already initialized via Awake");
        }
        
        public void Shutdown()
        {
            LogDebug("UpdateOrchestrator shutting down");
            ClearAll();
        }
        
        public ManagerMetrics GetMetrics()
        {
            return new ManagerMetrics
            {
                ManagerName = ManagerName,
                IsHealthy = true,
                Performance = _averageUpdateTime > 0 ? 1f / (_averageUpdateTime / 1000f) : 0f, // FPS approximation
                ManagedItems = _registeredTickables,
                Uptime = Time.time,
                LastActivity = $"Processing {_activeTickables}/{_registeredTickables} tickables"
            };
        }
        
        public string GetStatus()
        {
            return $"Active: {_activeTickables}/{_registeredTickables} tickables, Avg: {_averageUpdateTime:F2}ms";
        }
        
        public bool ValidateHealth()
        {
            // UpdateOrchestrator is healthy if it's processing updates within reasonable time
            return _averageUpdateTime < 16.67f; // Under 60 FPS threshold
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get current orchestrator statistics
        /// </summary>
        public OrchestratorStatistics GetStatistics()
        {
            return new OrchestratorStatistics
            {
                RegisteredTickables = _registeredTickables,
                ActiveTickables = _activeTickables,
                RegisteredFixedTickables = _fixedTickables.Values.Sum(list => list.Count),
                RegisteredLateTickables = _lateTickables.Values.Sum(list => list.Count),
                LastUpdateTime = _lastUpdateTime,
                AverageUpdateTime = _averageUpdateTime,
                PriorityGroups = _tickables.Keys.ToArray()
            };
        }
        
        /// <summary>
        /// Clear all registered tickables (useful for cleanup/testing)
        /// </summary>
        public void ClearAll()
        {
            _tickables.Clear();
            _fixedTickables.Clear();
            _lateTickables.Clear();
            _registeredTickables = 0;
            _activeTickables = 0;
            
            LogDebug("Cleared all registered tickables");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics about the UpdateOrchestrator performance and state
    /// </summary>
    [System.Serializable]
    public class OrchestratorStatistics
    {
        public int RegisteredTickables;
        public int ActiveTickables;
        public int RegisteredFixedTickables;
        public int RegisteredLateTickables;
        public float LastUpdateTime;
        public float AverageUpdateTime;
        public int[] PriorityGroups;
    }
}