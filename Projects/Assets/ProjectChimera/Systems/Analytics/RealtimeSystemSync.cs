using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Genetics;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Phase 2.4.2: Real-time System Synchronization
    /// Provides seamless data synchronization between all game systems with
    /// conflict resolution, state management, and real-time updates
    /// </summary>
    public class RealtimeSystemSync : MonoBehaviour
    {
        [Header("Synchronization Configuration")]
        [SerializeField] private bool _enableRealTimeSync = true;
        [SerializeField] private bool _enableConflictResolution = true;
        [SerializeField] private bool _enableStateValidation = true;
        [SerializeField] private bool _enableRollbackSupport = true;
        
        [Header("Sync Settings")]
        [SerializeField] private float _syncInterval = 0.1f;
        [SerializeField] private int _maxSyncOperationsPerFrame = 10;
        [SerializeField] private float _conflictResolutionTimeout = 5f;
        [SerializeField] private bool _enableBatchSync = true;
        
        [Header("State Management")]
        [SerializeField] private int _maxStateHistorySize = 100;
        [SerializeField] private float _stateSnapshotInterval = 1f;
        [SerializeField] private bool _enableStateCompression = true;
        [SerializeField] private bool _enableStatePersistence = false;
        
        [Header("Network Configuration")]
        [SerializeField] private bool _enableNetworkSync = false;
        [SerializeField] private float _networkSyncInterval = 0.5f;
        [SerializeField] private int _maxNetworkRetries = 3;
        [SerializeField] private float _networkTimeout = 10f;
        
        // System references
        private DataPipelineIntegration _dataPipeline;
        private ServiceLayerCoordinator _serviceCoordinator;
        private AdvancedMenuSystem _menuSystem;
        private List<ISyncableSystem> _syncableSystems = new List<ISyncableSystem>();
        
        // Synchronization state
        private Dictionary<string, SystemState> _systemStates = new Dictionary<string, SystemState>();
        private Queue<SyncOperation> _syncQueue = new Queue<SyncOperation>();
        private List<StateSnapshot> _stateHistory = new List<StateSnapshot>();
        private Dictionary<string, ConflictResolutionContext> _activeConflicts = new Dictionary<string, ConflictResolutionContext>();
        
        // Performance tracking
        private SyncMetrics _syncMetrics = new SyncMetrics();
        private float _lastSyncTime;
        private float _lastSnapshotTime;
        
        // Events
        public event Action<SyncOperation> OnSyncOperationCompleted;
        public event Action<StateConflict> OnConflictDetected;
        public event Action<string, SystemState> OnSystemStateChanged;
        public event Action<StateSnapshot> OnStateSnapshotCreated;
        
        private void Awake()
        {
            InitializeSyncSystem();
        }
        
        private void Start()
        {
            RegisterSyncableSystems();
            StartSynchronization();
            StartCoroutine(SyncUpdateLoop());
        }
        
        private void InitializeSyncSystem()
        {
            _dataPipeline = UnityEngine.Object.FindObjectOfType<DataPipelineIntegration>();
            _serviceCoordinator = UnityEngine.Object.FindObjectOfType<ServiceLayerCoordinator>();
            _menuSystem = UnityEngine.Object.FindObjectOfType<AdvancedMenuSystem>();
            
            if (_dataPipeline == null)
            {
                Debug.LogWarning("[RealtimeSystemSync] DataPipelineIntegration not found - some features may be limited");
            }
        }
        
        private void RegisterSyncableSystems()
        {
            // Find and register all systems that implement ISyncableSystem
            var syncableSystems = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISyncableSystem>();
            
            foreach (var system in syncableSystems)
            {
                RegisterSystem(system);
            }
            
            // Register core systems as syncable adapters
            if (_serviceCoordinator != null)
            {
                RegisterSystem(new ServiceCoordinatorSyncAdapter(_serviceCoordinator));
            }
            
            if (_menuSystem != null)
            {
                RegisterSystem(new MenuSystemSyncAdapter(_menuSystem));
            }
            
            Debug.Log($"[RealtimeSystemSync] Registered {_syncableSystems.Count} syncable systems");
        }
        
        private void StartSynchronization()
        {
            if (!_enableRealTimeSync)
            {
                Debug.LogWarning("[RealtimeSystemSync] Real-time sync disabled");
                return;
            }
            
            // Initialize system states
            foreach (var system in _syncableSystems)
            {
                var systemId = system.GetSystemId();
                var initialState = system.GetCurrentState();
                
                _systemStates[systemId] = new SystemState
                {
                    SystemId = systemId,
                    Version = 1,
                    Data = initialState,
                    LastModified = DateTime.UtcNow,
                    IsValid = true
                };
                
                // Subscribe to system state changes
                system.OnStateChanged += OnSystemStateUpdated;
            }
            
            Debug.Log("[RealtimeSystemSync] Synchronization started");
        }
        
        /// <summary>
        /// Register a system for synchronization
        /// </summary>
        public void RegisterSystem(ISyncableSystem system)
        {
            if (system == null)
            {
                Debug.LogError("[RealtimeSystemSync] Cannot register null system");
                return;
            }
            
            var systemId = system.GetSystemId();
            if (_syncableSystems.Any(s => s.GetSystemId() == systemId))
            {
                Debug.LogWarning($"[RealtimeSystemSync] System {systemId} already registered");
                return;
            }
            
            _syncableSystems.Add(system);
            system.OnStateChanged += OnSystemStateUpdated;
            
            Debug.Log($"[RealtimeSystemSync] Registered system: {systemId}");
        }
        
        /// <summary>
        /// Unregister a system from synchronization
        /// </summary>
        public void UnregisterSystem(string systemId)
        {
            var targetSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
            if (targetSystem != null)
            {
                targetSystem.OnStateChanged -= OnSystemStateUpdated;
                _syncableSystems.Remove(targetSystem);
                _systemStates.Remove(systemId);
                
                Debug.Log($"[RealtimeSystemSync] Unregistered system: {systemId}");
            }
        }
        
        /// <summary>
        /// Queue a synchronization operation
        /// </summary>
        public void QueueSyncOperation(SyncOperation operation)
        {
            if (!_enableRealTimeSync)
                return;
            
            operation.QueuedAt = DateTime.UtcNow;
            _syncQueue.Enqueue(operation);
            
            // Collect telemetry
            _dataPipeline?.CollectEvent(
                "sync_telemetry",
                "sync_operation_queued",
                new { 
                    operation_type = operation.OperationType,
                    system_id = operation.SystemId,
                    priority = operation.Priority
                }
            );
        }
        
        /// <summary>
        /// Force synchronization of a specific system
        /// </summary>
        public async Task<bool> ForceSyncSystem(string systemId)
        {
            var targetSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
            if (targetSystem == null)
            {
                Debug.LogError($"[RealtimeSystemSync] System {systemId} not found for forced sync");
                return false;
            }
            
            try
            {
                var currentState = targetSystem.GetCurrentState();
                var syncResult = await ApplyStateChange(systemId, currentState, SyncMode.Force);
                
                return syncResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeSystemSync] Force sync failed for {systemId}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Create a state snapshot of all systems
        /// </summary>
        public StateSnapshot CreateStateSnapshot()
        {
            var snapshot = new StateSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                SystemStates = new Dictionary<string, object>()
            };
            
            foreach (var system in _syncableSystems)
            {
                var systemId = system.GetSystemId();
                var state = system.GetCurrentState();
                snapshot.SystemStates[systemId] = state;
            }
            
            // Add to history
            _stateHistory.Add(snapshot);
            
            // Maintain history size limit
            if (_stateHistory.Count > _maxStateHistorySize)
            {
                _stateHistory.RemoveAt(0);
            }
            
            OnStateSnapshotCreated?.Invoke(snapshot);
            
            // Collect telemetry
            _dataPipeline?.CollectEvent(
                "sync_telemetry",
                "state_snapshot_created",
                new { 
                    snapshot_id = snapshot.SnapshotId,
                    system_count = snapshot.SystemStates.Count,
                    size_estimate = EstimateSnapshotSize(snapshot)
                }
            );
            
            return snapshot;
        }
        
        /// <summary>
        /// Restore from a state snapshot
        /// </summary>
        public async Task<bool> RestoreFromSnapshot(string snapshotId)
        {
            var snapshot = _stateHistory.FirstOrDefault(s => s.SnapshotId == snapshotId);
            if (snapshot == null)
            {
                Debug.LogError($"[RealtimeSystemSync] Snapshot {snapshotId} not found");
                return false;
            }
            
            try
            {
                var restoredSystems = 0;
                
                foreach (var systemState in snapshot.SystemStates)
                {
                    var targetSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemState.Key);
                    if (targetSystem != null)
                    {
                        var result = await targetSystem.RestoreState(systemState.Value);
                        if (result)
                        {
                            restoredSystems++;
                        }
                    }
                }
                
                Debug.Log($"[RealtimeSystemSync] Restored {restoredSystems} systems from snapshot {snapshotId}");
                return restoredSystems > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeSystemSync] Failed to restore from snapshot {snapshotId}: {ex.Message}");
                return false;
            }
        }
        
        private async void OnSystemStateUpdated(string systemId, object newState)
        {
            if (!_enableRealTimeSync)
                return;
            
            var operation = new SyncOperation
            {
                OperationId = Guid.NewGuid().ToString(),
                SystemId = systemId,
                OperationType = SyncOperationType.StateUpdate,
                Data = newState,
                Priority = 1,
                QueuedAt = DateTime.UtcNow
            };
            
            QueueSyncOperation(operation);
        }
        
        private async Task<SyncResult> ApplyStateChange(string systemId, object newState, SyncMode mode = SyncMode.Normal)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Get current system state
                if (!_systemStates.TryGetValue(systemId, out var currentSystemState))
                {
                    currentSystemState = new SystemState
                    {
                        SystemId = systemId,
                        Version = 0,
                        Data = null,
                        LastModified = DateTime.UtcNow,
                        IsValid = true
                    };
                    _systemStates[systemId] = currentSystemState;
                }
                
                // Detect conflicts
                if (_enableConflictResolution && mode != SyncMode.Force)
                {
                    var conflict = DetectConflicts(systemId, newState, currentSystemState);
                    if (conflict != null)
                    {
                        var resolutionResult = await ResolveConflict(conflict);
                        if (!resolutionResult.Success)
                        {
                            return new SyncResult
                            {
                                Success = false,
                                ErrorMessage = "Conflict resolution failed",
                                ConflictDetected = true
                            };
                        }
                        newState = resolutionResult.ResolvedState;
                    }
                }
                
                // Validate state if enabled
                if (_enableStateValidation)
                {
                    var validationSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
                    if (validationSystem != null && !validationSystem.ValidateState(newState))
                    {
                        return new SyncResult
                        {
                            Success = false,
                            ErrorMessage = "State validation failed"
                        };
                    }
                }
                
                // Apply state change
                var applySystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
                if (applySystem != null)
                {
                    await applySystem.ApplyState(newState);
                }
                
                // Update system state
                currentSystemState.Data = newState;
                currentSystemState.Version++;
                currentSystemState.LastModified = DateTime.UtcNow;
                currentSystemState.IsValid = true;
                
                OnSystemStateChanged?.Invoke(systemId, currentSystemState);
                
                // Update metrics
                _syncMetrics.SuccessfulSyncs++;
                _syncMetrics.LastSyncTime = DateTime.UtcNow;
                _syncMetrics.AverageLatency = UpdateAverageLatency((float)(DateTime.UtcNow - startTime).TotalMilliseconds);
                
                return new SyncResult
                {
                    Success = true,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _syncMetrics.FailedSyncs++;
                
                Debug.LogError($"[RealtimeSystemSync] Failed to apply state change for {systemId}: {ex.Message}");
                
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }
        
        private StateConflict DetectConflicts(string systemId, object newState, SystemState currentState)
        {
            // Simple conflict detection based on timestamps and versions
            // In a real implementation, this would be more sophisticated
            
            var conflictSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
            if (conflictSystem == null) return null;
            
            var currentSystemState = conflictSystem.GetCurrentState();
            
            // Check if the current state has changed since our last known state
            if (!CompareStates(currentState.Data, currentSystemState))
            {
                return new StateConflict
                {
                    ConflictId = Guid.NewGuid().ToString(),
                    SystemId = systemId,
                    ConflictType = ConflictType.ConcurrentModification,
                    LocalState = newState,
                    RemoteState = currentSystemState,
                    BaseState = currentState.Data,
                    DetectedAt = DateTime.UtcNow
                };
            }
            
            return null;
        }
        
        private async Task<ConflictResolutionResult> ResolveConflict(StateConflict conflict)
        {
            var contextId = conflict.ConflictId;
            
            // Track active conflict
            var context = new ConflictResolutionContext
            {
                Conflict = conflict,
                StartTime = DateTime.UtcNow,
                ResolutionStrategy = ConflictResolutionStrategy.LastWriterWins // Default strategy
            };
            
            _activeConflicts[contextId] = context;
            
            OnConflictDetected?.Invoke(conflict);
            
            try
            {
                object resolvedState = null;
                
                // Apply resolution strategy
                switch (context.ResolutionStrategy)
                {
                    case ConflictResolutionStrategy.LastWriterWins:
                        resolvedState = conflict.LocalState;
                        break;
                        
                    case ConflictResolutionStrategy.FirstWriterWins:
                        resolvedState = conflict.RemoteState;
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        resolvedState = await MergeStates(conflict.LocalState, conflict.RemoteState, conflict.BaseState);
                        break;
                        
                    case ConflictResolutionStrategy.ManualResolution:
                        // Wait for manual resolution or timeout
                        resolvedState = await WaitForManualResolution(context);
                        break;
                }
                
                context.ResolvedState = resolvedState;
                context.EndTime = DateTime.UtcNow;
                context.Success = true;
                
                _activeConflicts.Remove(contextId);
                
                // Collect telemetry
                _dataPipeline?.CollectEvent(
                    "sync_telemetry",
                    "conflict_resolved",
                    new { 
                        conflict_id = conflict.ConflictId,
                        system_id = conflict.SystemId,
                        resolution_strategy = context.ResolutionStrategy,
                        resolution_time = (context.EndTime - context.StartTime).TotalMilliseconds
                    }
                );
                
                return new ConflictResolutionResult
                {
                    Success = true,
                    ResolvedState = resolvedState
                };
            }
            catch (Exception ex)
            {
                context.Success = false;
                context.ErrorMessage = ex.Message;
                _activeConflicts.Remove(contextId);
                
                Debug.LogError($"[RealtimeSystemSync] Conflict resolution failed: {ex.Message}");
                
                return new ConflictResolutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private SyncResult ApplyStateChangeSync(string systemId, object newState)
        {
            var targetSystem = _syncableSystems.FirstOrDefault(s => s.GetSystemId() == systemId);
            
            if (targetSystem == null)
            {
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = $"System {systemId} not found",
                    Duration = TimeSpan.Zero
                };
            }
            
            try
            {
                // Apply state change synchronously
                targetSystem.ApplyState(newState);
                
                // Update local state tracking if available
                if (_systemStates.ContainsKey(systemId))
                {
                    var existingState = _systemStates[systemId];
                    existingState.Data = newState;
                    existingState.LastModified = DateTime.UtcNow;
                    existingState.Version++;
                    _systemStates[systemId] = existingState;
                }
                
                // Notify about successful change
                _dataPipeline?.CollectEvent(
                    "sync_telemetry",
                    "state_applied_sync",
                    new { system_id = systemId, timestamp = DateTime.UtcNow }
                );
                
                return new SyncResult
                {
                    Success = true,
                    StateChanged = true,
                    Duration = TimeSpan.Zero
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealtimeSystemSync] Failed to apply state change synchronously: {ex.Message}");
                
                return new SyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = TimeSpan.Zero
                };
            }
        }
        
        private async Task<object> MergeStates(object localState, object remoteState, object baseState)
        {
            // Simplified merge strategy - in a real implementation, this would be more sophisticated
            // For now, return the local state
            // Process queue synchronously to avoid async/await issues in coroutines
            return localState;
        }
        
        private async Task<object> WaitForManualResolution(ConflictResolutionContext context)
        {
            var timeout = DateTime.UtcNow.AddSeconds(_conflictResolutionTimeout);
            
            while (DateTime.UtcNow < timeout && context.ResolvedState == null)
            {
                await Task.Delay(100);
            }
            
            if (context.ResolvedState == null)
            {
                throw new TimeoutException("Manual conflict resolution timed out");
            }
            
            return context.ResolvedState;
        }
        
        private bool CompareStates(object state1, object state2)
        {
            // Simplified state comparison
            // In a real implementation, this would do deep comparison
            return state1?.ToString() == state2?.ToString();
        }
        
        private float UpdateAverageLatency(float newLatency)
        {
            const float alpha = 0.1f; // Exponential moving average factor
            return _syncMetrics.AverageLatency * (1 - alpha) + newLatency * alpha;
        }
        
        private long EstimateSnapshotSize(StateSnapshot snapshot)
        {
            // Simplified size estimation
            long size = 0;
            foreach (var state in snapshot.SystemStates)
            {
                size += state.Key?.Length * 2 ?? 0; // String key
                size += EstimateObjectSize(state.Value);
            }
            return size;
        }
        
        private long EstimateObjectSize(object obj)
        {
            if (obj == null) return 0;
            
            switch (obj)
            {
                case string str: return str.Length * 2;
                case int: return 4;
                case long: return 8;
                case float: return 4;
                case double: return 8;
                case bool: return 1;
                case Vector3: return 12;
                case Vector2: return 8;
                default: return 128; // Default estimation for complex objects
            }
        }
        
        private IEnumerator SyncUpdateLoop()
        {
            while (_enableRealTimeSync)
            {
                yield return new WaitForSeconds(_syncInterval);
                
                // Process sync queue
                ProcessSyncQueueSync();
                
                // Create periodic snapshots
                if (Time.time - _lastSnapshotTime >= _stateSnapshotInterval)
                {
                    CreateStateSnapshot();
                    _lastSnapshotTime = Time.time;
                }
                
                // Update metrics
                UpdateSyncMetrics();
                
                // Cleanup old conflicts
                CleanupOldConflicts();
            }
        }
        
        private void ProcessSyncQueueSync()
        {
            int processedOperations = 0;
            
            while (_syncQueue.Count > 0 && processedOperations < _maxSyncOperationsPerFrame)
            {
                var operation = _syncQueue.Dequeue();
                
                try
                {
                    operation.StartedAt = DateTime.UtcNow;
                    
                    var result = ApplyStateChangeSync(operation.SystemId, operation.Data);
                    
                    operation.CompletedAt = DateTime.UtcNow;
                    operation.Success = result.Success;
                    operation.ErrorMessage = result.ErrorMessage;
                    
                    OnSyncOperationCompleted?.Invoke(operation);
                    
                    processedOperations++;
                }
                catch (Exception ex)
                {
                    operation.Success = false;
                    operation.ErrorMessage = ex.Message;
                    operation.CompletedAt = DateTime.UtcNow;
                    
                    Debug.LogError($"[RealtimeSystemSync] Sync operation failed: {ex.Message}");
                }
            }
        }
        
        private void UpdateSyncMetrics()
        {
            _syncMetrics.ActiveConflicts = _activeConflicts.Count;
            _syncMetrics.QueueSize = _syncQueue.Count;
            _syncMetrics.RegisteredSystems = _syncableSystems.Count;
            _syncMetrics.StateHistorySize = _stateHistory.Count;
        }
        
        private void CleanupOldConflicts()
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-_conflictResolutionTimeout);
            
            var expiredConflicts = _activeConflicts.Where(kvp => kvp.Value.StartTime < cutoffTime).ToList();
            
            foreach (var expired in expiredConflicts)
            {
                _activeConflicts.Remove(expired.Key);
                Debug.LogWarning($"[RealtimeSystemSync] Conflict {expired.Key} timed out and was removed");
            }
        }
        
        private void OnDestroy()
        {
            _enableRealTimeSync = false;
            
            // Unsubscribe from all systems
            foreach (var system in _syncableSystems)
            {
                system.OnStateChanged -= OnSystemStateUpdated;
            }
        }
        
        // Public API
        public SyncMetrics GetSyncMetrics() => _syncMetrics;
        public int GetActiveConflictCount() => _activeConflicts.Count;
        public int GetQueueSize() => _syncQueue.Count;
        public bool IsSystemRegistered(string systemId) => _syncableSystems.Any(s => s.GetSystemId() == systemId);
        public StateSnapshot[] GetStateHistory() => _stateHistory.ToArray();
        public void SetSyncEnabled(bool enabled) => _enableRealTimeSync = enabled;
        public void SetConflictResolutionEnabled(bool enabled) => _enableConflictResolution = enabled;
        public void ClearSyncQueue() => _syncQueue.Clear();
        public SystemState GetSystemState(string systemId) => _systemStates.TryGetValue(systemId, out var state) ? state : null;
    }
}