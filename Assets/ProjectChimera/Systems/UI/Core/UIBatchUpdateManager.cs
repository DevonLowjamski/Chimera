using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: UI Batch Update Manager
    /// Single Responsibility: Batched UI update processing and performance optimization
    /// Extracted from OptimizedUIManager for better separation of concerns
    /// </summary>
    public class UIBatchUpdateManager : MonoBehaviour
    {
        [Header("Batch Update Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableBatchedUpdates = true;
        [SerializeField] private int _maxUIUpdatesPerFrame = 50;
        [SerializeField] private float _updateInterval = 0.016f; // ~60 FPS

        // Batched updates by type
        private readonly Dictionary<UIUpdateType, List<IUIUpdatable>> _batchedUpdates = new Dictionary<UIUpdateType, List<IUIUpdatable>>();
        private readonly ConcurrentQueue<UIUpdateRequest> _updateQueue = new ConcurrentQueue<UIUpdateRequest>();

        // Performance tracking
        private int _uiUpdatesThisFrame;
        private float _lastUpdateTime;
        private bool _isInitialized = false;

        // Statistics
        private UIBatchUpdateStats _stats = new UIBatchUpdateStats();

        // Events
        public event System.Action<UIUpdateType, int> OnBatchProcessed;
        public event System.Action<UIUpdateRequest> OnUpdateProcessed;

        public bool IsInitialized => _isInitialized;
        public UIBatchUpdateStats Stats => _stats;
        public int QueuedUpdateCount => _updateQueue.Count;
        public int UpdatesThisFrame => _uiUpdatesThisFrame;

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializeBatchedUpdates();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "UI Batch Update Manager initialized", this);
            }
        }

        /// <summary>
        /// Initialize batched update collections
        /// </summary>
        private void InitializeBatchedUpdates()
        {
            _batchedUpdates.Clear();

            // Initialize collections for each update type
            foreach (UIUpdateType updateType in System.Enum.GetValues(typeof(UIUpdateType)))
            {
                _batchedUpdates[updateType] = new List<IUIUpdatable>();
            }
        }

        /// <summary>
        /// Process batched updates (call from Update or ITickable)
        /// </summary>
        public void ProcessBatchedUpdates(float deltaTime)
        {
            if (!_isInitialized || !_enableBatchedUpdates) return;

            // Reset frame counter
            _uiUpdatesThisFrame = 0;
            _lastUpdateTime = Time.time;

            // Process update queue first
            ProcessUpdateQueue();

            // Process batched updates by type
            ProcessBatchedUpdatesByType(deltaTime);

            // Update statistics
            _stats.TotalFramesProcessed++;
            _stats.LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Process update queue
        /// </summary>
        private void ProcessUpdateQueue()
        {
            var processedRequests = 0;

            while (_updateQueue.TryDequeue(out var request) && 
                   _uiUpdatesThisFrame < _maxUIUpdatesPerFrame)
            {
                ProcessUIUpdateRequest(request);
                _uiUpdatesThisFrame++;
                processedRequests++;
            }

            _stats.QueuedUpdatesProcessed += processedRequests;
        }

        /// <summary>
        /// Process batched updates by type
        /// </summary>
        private void ProcessBatchedUpdatesByType(float deltaTime)
        {
            foreach (var kvp in _batchedUpdates)
            {
                if (_uiUpdatesThisFrame >= _maxUIUpdatesPerFrame)
                    break;

                var updateType = kvp.Key;
                var updatables = kvp.Value;

                if (updatables.Count == 0)
                    continue;

                var processedCount = ProcessBatchForType(updateType, updatables, deltaTime);
                
                if (processedCount > 0)
                {
                    OnBatchProcessed?.Invoke(updateType, processedCount);
                    _stats.BatchesProcessed++;
                }
            }
        }

        /// <summary>
        /// Process batch for specific update type
        /// </summary>
        private int ProcessBatchForType(UIUpdateType updateType, List<IUIUpdatable> updatables, float deltaTime)
        {
            int updatesThisType = 0;
            int maxUpdatesPerType = (_maxUIUpdatesPerFrame - _uiUpdatesThisFrame) / _batchedUpdates.Count;
            maxUpdatesPerType = Mathf.Max(1, maxUpdatesPerType); // Ensure at least 1 update per type

            var toRemove = new List<IUIUpdatable>();

            foreach (var updatable in updatables)
            {
                if (updatesThisType >= maxUpdatesPerType)
                    break;

                if (updatable == null)
                {
                    toRemove.Add(updatable);
                    continue;
                }

                if (updatable.ShouldUpdate())
                {
                    try
                    {
                        updatable.UpdateUI(deltaTime);
                        updatesThisType++;
                        _uiUpdatesThisFrame++;
                    }
                    catch (System.Exception ex)
                    {
                        if (_enableLogging)
                        {
                            ChimeraLogger.LogError("UI", $"Error updating UI element of type {updateType}: {ex.Message}", this);
                        }
                        toRemove.Add(updatable);
                    }
                }
            }

            // Clean up null or errored updatables
            foreach (var updatable in toRemove)
            {
                updatables.Remove(updatable);
            }

            return updatesThisType;
        }

        /// <summary>
        /// Process individual UI update request
        /// </summary>
        private void ProcessUIUpdateRequest(UIUpdateRequest request)
        {
            try
            {
                // Process based on request type
                switch (request.Type)
                {
                    case UIUpdateType.PlantInfo:
                        ProcessPlantInfoUpdate(request);
                        break;
                    case UIUpdateType.Progress:
                        ProcessProgressUpdate(request);
                        break;
                    case UIUpdateType.Notification:
                        ProcessNotificationUpdate(request);
                        break;
                    default:
                        ProcessGenericUpdate(request);
                        break;
                }

                OnUpdateProcessed?.Invoke(request);
                _stats.UpdatesProcessed++;
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("UI", $"Error processing UI update request {request.Type}: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Queue UI update request
        /// </summary>
        public void QueueUIUpdate(UIUpdateRequest request)
        {
            if (request == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot queue null update request", this);
                }
                return;
            }

            _updateQueue.Enqueue(request);
            _stats.UpdatesQueued++;
        }

        /// <summary>
        /// Register for batched updates
        /// </summary>
        public bool RegisterForBatchedUpdate(UIUpdateType updateType, IUIUpdatable updatable)
        {
            if (updatable == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot register null updatable for batched updates", this);
                }
                return false;
            }

            if (!_enableBatchedUpdates || !_batchedUpdates.ContainsKey(updateType))
            {
                return false;
            }

            var updatables = _batchedUpdates[updateType];
            if (!updatables.Contains(updatable))
            {
                updatables.Add(updatable);
                _stats.RegisteredUpdatables++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Registered updatable for {updateType} batch updates", this);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unregister from batched updates
        /// </summary>
        public bool UnregisterFromBatchedUpdate(UIUpdateType updateType, IUIUpdatable updatable)
        {
            if (updatable == null || !_batchedUpdates.ContainsKey(updateType))
                return false;

            var updatables = _batchedUpdates[updateType];
            if (updatables.Remove(updatable))
            {
                _stats.RegisteredUpdatables--;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Unregistered updatable from {updateType} batch updates", this);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set maximum updates per frame
        /// </summary>
        public void SetMaxUpdatesPerFrame(int maxUpdates)
        {
            _maxUIUpdatesPerFrame = Mathf.Max(1, maxUpdates);
            
            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Max UI updates per frame set to {_maxUIUpdatesPerFrame}", this);
            }
        }

        /// <summary>
        /// Enable or disable batched updates
        /// </summary>
        public void SetBatchedUpdatesEnabled(bool enabled)
        {
            _enableBatchedUpdates = enabled;
            
            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Batched updates {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Clear all batched updatables
        /// </summary>
        public void ClearAllBatchedUpdatables()
        {
            foreach (var kvp in _batchedUpdates)
            {
                kvp.Value.Clear();
            }
            
            _stats.RegisteredUpdatables = 0;
            
            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "All batched updatables cleared", this);
            }
        }

        /// <summary>
        /// Get registered count for update type
        /// </summary>
        public int GetRegisteredCount(UIUpdateType updateType)
        {
            if (_batchedUpdates.TryGetValue(updateType, out var updatables))
            {
                return updatables.Count;
            }
            return 0;
        }

        // Specific update processors
        private void ProcessPlantInfoUpdate(UIUpdateRequest request)
        {
            // Handle plant info specific updates
        }

        private void ProcessProgressUpdate(UIUpdateRequest request)
        {
            // Handle progress bar specific updates
        }

        private void ProcessNotificationUpdate(UIUpdateRequest request)
        {
            // Handle notification specific updates
        }

        private void ProcessGenericUpdate(UIUpdateRequest request)
        {
            // Handle generic UI updates
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new UIBatchUpdateStats
            {
                UpdatesQueued = 0,
                UpdatesProcessed = 0,
                QueuedUpdatesProcessed = 0,
                BatchesProcessed = 0,
                TotalFramesProcessed = 0,
                RegisteredUpdatables = 0,
                LastUpdateTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllBatchedUpdatables();
        }
    }

    /// <summary>
    /// UI update types
    /// </summary>
    public enum UIUpdateType
    {
        PlantInfo,
        Progress,
        Notification,
        Panel,
        Canvas,
        Generic,
        Transform,
        Visibility,
        Content
    }

    /// <summary>
    /// UI update request data
    /// </summary>
    [System.Serializable]
    public class UIUpdateRequest
    {
        public UIUpdateType Type;
        public string ElementId;
        public object Data;
        public float Timestamp;
        public GameObject Target;
        public Vector3 Position;
        public bool IsVisible;
    }

    /// <summary>
    /// Interface for UI updatable elements
    /// </summary>
    public interface IUIUpdatable
    {
        bool ShouldUpdate();
        void UpdateUI(float deltaTime = 0f);
    }

    /// <summary>
    /// Batch update statistics
    /// </summary>
    [System.Serializable]
    public struct UIBatchUpdateStats
    {
        public int UpdatesQueued;
        public int UpdatesProcessed;
        public int QueuedUpdatesProcessed;
        public int BatchesProcessed;
        public int TotalFramesProcessed;
        public int RegisteredUpdatables;
        public float LastUpdateTime;
    }
}