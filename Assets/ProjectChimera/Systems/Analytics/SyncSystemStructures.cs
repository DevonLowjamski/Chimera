using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Systems.UI.Advanced;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Interface for systems that can be synchronized
    /// </summary>
    public interface ISyncableSystem
    {
        string GetSystemId();
        object GetCurrentState();
        Task<bool> ApplyState(object state);
        Task<bool> RestoreState(object state);
        bool ValidateState(object state);
        event Action<string, object> OnStateChanged;
    }
    
    /// <summary>
    /// System state representation
    /// </summary>
    [System.Serializable]
    public class SystemState
    {
        public string SystemId;
        public int Version;
        public object Data;
        public DateTime LastModified;
        public bool IsValid;
        public Dictionary<string, object> Metadata;
        
        public SystemState()
        {
            Metadata = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// State snapshot containing all system states at a point in time
    /// </summary>
    [System.Serializable]
    public class StateSnapshot
    {
        public string SnapshotId;
        public DateTime Timestamp;
        public Dictionary<string, object> SystemStates;
        public long EstimatedSize;
        public bool IsCompressed;
        public Dictionary<string, object> Metadata;
        
        public StateSnapshot()
        {
            SystemStates = new Dictionary<string, object>();
            Metadata = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Synchronization operation
    /// </summary>
    [System.Serializable]
    public class SyncOperation
    {
        public string OperationId;
        public string SystemId;
        public SyncOperationType OperationType;
        public object Data;
        public int Priority;
        public DateTime QueuedAt;
        public DateTime StartedAt;
        public DateTime CompletedAt;
        public bool Success;
        public string ErrorMessage;
        public Dictionary<string, object> Metadata;
        
        public SyncOperation()
        {
            Metadata = new Dictionary<string, object>();
        }
        
        public TimeSpan GetProcessingTime()
        {
            if (StartedAt == default || CompletedAt == default)
                return TimeSpan.Zero;
            
            return CompletedAt - StartedAt;
        }
    }
    
    /// <summary>
    /// Types of synchronization operations
    /// </summary>
    public enum SyncOperationType
    {
        StateUpdate,
        StateRestore,
        ConflictResolution,
        SystemRegistration,
        SystemDeregistration,
        SnapshotCreation,
        SnapshotRestore
    }
    
    /// <summary>
    /// Synchronization modes
    /// </summary>
    public enum SyncMode
    {
        Normal,
        Force,
        Optimistic,
        Pessimistic
    }
    
    /// <summary>
    /// State conflict representation
    /// </summary>
    [System.Serializable]
    public class StateConflict
    {
        public string ConflictId;
        public string SystemId;
        public ConflictType ConflictType;
        public object LocalState;
        public object RemoteState;
        public object BaseState;
        public DateTime DetectedAt;
        public Dictionary<string, object> ConflictMetadata;
        
        public StateConflict()
        {
            ConflictMetadata = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Types of state conflicts
    /// </summary>
    public enum ConflictType
    {
        ConcurrentModification,
        VersionMismatch,
        DataCorruption,
        NetworkPartition,
        ValidationFailure
    }
    
    /// <summary>
    /// Conflict resolution strategies
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        LastWriterWins,
        FirstWriterWins,
        Merge,
        ManualResolution,
        Rollback
    }
    
    /// <summary>
    /// Conflict resolution context
    /// </summary>
    [System.Serializable]
    public class ConflictResolutionContext
    {
        public StateConflict Conflict;
        public ConflictResolutionStrategy ResolutionStrategy;
        public DateTime StartTime;
        public DateTime EndTime;
        public object ResolvedState;
        public bool Success;
        public string ErrorMessage;
    }
    
    /// <summary>
    /// Result of conflict resolution
    /// </summary>
    public struct ConflictResolutionResult
    {
        public bool Success;
        public object ResolvedState;
        public string ErrorMessage;
        
        public static ConflictResolutionResult Successful(object resolvedState)
        {
            return new ConflictResolutionResult
            {
                Success = true,
                ResolvedState = resolvedState
            };
        }
        
        public static ConflictResolutionResult Failed(string errorMessage)
        {
            return new ConflictResolutionResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
    
    /// <summary>
    /// Synchronization result
    /// </summary>
    public struct SyncResult
    {
        public bool Success;
        public string ErrorMessage;
        public TimeSpan Duration;
        public bool ConflictDetected;
        public bool StateChanged;
        
        public static SyncResult Successful(TimeSpan duration, bool stateChanged = true)
        {
            return new SyncResult
            {
                Success = true,
                Duration = duration,
                StateChanged = stateChanged
            };
        }
        
        public static SyncResult Failed(string errorMessage, TimeSpan duration = default)
        {
            return new SyncResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Duration = duration
            };
        }
    }
    
    /// <summary>
    /// Synchronization metrics
    /// </summary>
    [System.Serializable]
    public class SyncMetrics
    {
        public long SuccessfulSyncs;
        public long FailedSyncs;
        public float AverageLatency;
        public DateTime LastSyncTime;
        public int ActiveConflicts;
        public int QueueSize;
        public int RegisteredSystems;
        public int StateHistorySize;
        public Dictionary<string, long> OperationCounts;
        public Dictionary<string, float> SystemLatencies;
        
        public SyncMetrics()
        {
            OperationCounts = new Dictionary<string, long>();
            SystemLatencies = new Dictionary<string, float>();
        }
        
        public float GetSuccessRate()
        {
            var totalOperations = SuccessfulSyncs + FailedSyncs;
            return totalOperations > 0 ? (float)SuccessfulSyncs / totalOperations : 0f;
        }
    }
    
    /// <summary>
    /// Adapter for Service Layer Coordinator
    /// </summary>
    public class ServiceCoordinatorSyncAdapter : ISyncableSystem
    {
        private readonly ServiceLayerCoordinator _coordinator;
        
        public event Action<string, object> OnStateChanged;
        
        public ServiceCoordinatorSyncAdapter(ServiceLayerCoordinator coordinator)
        {
            _coordinator = coordinator;
        }
        
        public string GetSystemId()
        {
            return "service_coordinator";
        }
        
        public object GetCurrentState()
        {
            // Return a representation of the current service state
            return new
            {
                timestamp = DateTime.UtcNow,
                active_services = GetActiveServiceCount(),
                last_command = GetLastCommandTimestamp()
            };
        }
        
        public async Task<bool> ApplyState(object state)
        {
            // Service coordinator state is typically read-only
            // but we can update internal metrics
            await Task.Delay(1);
            return true;
        }
        
        public async Task<bool> RestoreState(object state)
        {
            // Restore service coordinator state if needed
            await Task.Delay(1);
            return true;
        }
        
        public bool ValidateState(object state)
        {
            // Validate service coordinator state
            return state != null;
        }
        
        private int GetActiveServiceCount()
        {
            // In a real implementation, query the coordinator for active services
            return 3; // Mock value
        }
        
        private DateTime GetLastCommandTimestamp()
        {
            // In a real implementation, get the last command timestamp
            return DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Adapter for Advanced Menu System
    /// </summary>
    public class MenuSystemSyncAdapter : ISyncableSystem
    {
        private readonly AdvancedMenuSystem _menuSystem;
        
        public event Action<string, object> OnStateChanged;
        
        public MenuSystemSyncAdapter(AdvancedMenuSystem menuSystem)
        {
            _menuSystem = menuSystem;
            
            // Subscribe to menu events to detect state changes
            _menuSystem.OnMenuOpened += OnMenuStateChanged;
            _menuSystem.OnMenuClosed += OnMenuStateChanged;
            _menuSystem.OnActionExecuted += OnActionExecuted;
        }
        
        public string GetSystemId()
        {
            return "advanced_menu_system";
        }
        
        public object GetCurrentState()
        {
            return new
            {
                timestamp = DateTime.UtcNow,
                is_menu_open = _menuSystem.IsMenuOpen(),
                active_menu_count = _menuSystem.GetActiveMenuCount(),
                category_count = _menuSystem.GetCategoryCount(),
                action_count = _menuSystem.GetActionCount()
            };
        }
        
        public async Task<bool> ApplyState(object state)
        {
            // Menu system state changes are typically user-driven
            // but we can update internal state if needed
            await Task.Delay(1);
            return true;
        }
        
        public async Task<bool> RestoreState(object state)
        {
            // Restore menu system state
            await Task.Delay(1);
            
            // In a real implementation, restore menu state from the provided data
            return true;
        }
        
        public bool ValidateState(object state)
        {
            // Validate menu system state
            return state != null;
        }
        
        private void OnMenuStateChanged(string menuId)
        {
            OnStateChanged?.Invoke(GetSystemId(), GetCurrentState());
        }
        
        private void OnActionExecuted(string actionId, MenuAction action)
        {
            OnStateChanged?.Invoke(GetSystemId(), GetCurrentState());
        }
    }
    
    /// <summary>
    /// Generic syncable system adapter
    /// </summary>
    public class GenericSystemAdapter : ISyncableSystem
    {
        private readonly string _systemId;
        private readonly Func<object> _getStateFunc;
        private readonly Func<object, Task<bool>> _applyStateFunc;
        private readonly Func<object, Task<bool>> _restoreStateFunc;
        private readonly Func<object, bool> _validateStateFunc;
        
        public event Action<string, object> OnStateChanged;
        
        public GenericSystemAdapter(
            string systemId,
            Func<object> getState,
            Func<object, Task<bool>> applyState = null,
            Func<object, Task<bool>> restoreState = null,
            Func<object, bool> validateState = null)
        {
            _systemId = systemId;
            _getStateFunc = getState;
            _applyStateFunc = applyState ?? (async _ => { await Task.Delay(1); return true; });
            _restoreStateFunc = restoreState ?? (async _ => { await Task.Delay(1); return true; });
            _validateStateFunc = validateState ?? (_ => true);
        }
        
        public string GetSystemId() => _systemId;
        
        public object GetCurrentState() => _getStateFunc?.Invoke();
        
        public async Task<bool> ApplyState(object state) => await _applyStateFunc(state);
        
        public async Task<bool> RestoreState(object state) => await _restoreStateFunc(state);
        
        public bool ValidateState(object state) => _validateStateFunc(state);
        
        public void TriggerStateChange()
        {
            OnStateChanged?.Invoke(_systemId, GetCurrentState());
        }
    }
}