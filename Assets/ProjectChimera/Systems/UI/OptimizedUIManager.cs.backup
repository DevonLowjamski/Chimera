using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.Input;
using ProjectChimera.Systems.UI.Pooling;
using ProjectChimera.Systems.UI.Core;
using ProjectChimera.Systems.UI.Performance;
using ProjectChimera.Systems.UI.Events;
using ProjectChimera.Systems.UI.Resources;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using IUIUpdatable = ProjectChimera.Systems.UI.Core.IUIUpdatable;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// REFACTORED: Optimized UI Manager - Coordinator using SRP-compliant components
    /// Single Responsibility: Coordinating UI optimization, performance monitoring, and resource management
    /// Uses composition with UIPerformanceMonitor, UIEventHandler, and UIResourceManager
    /// Reduced from 770 lines to maintain SRP compliance
    /// </summary>
    public class OptimizedUIManager : MonoBehaviour, ITickable
    {
        [Header("UI Optimization Settings")]
        [SerializeField] private bool _enableUIOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _uiUpdateInterval = 0.1f; // 10 FPS for UI updates
        [SerializeField] private int _maxUIUpdatesPerFrame = 20;

        [Header("Canvas Optimization")]
        [SerializeField] private bool _enableCanvasCulling = true;
        [SerializeField] private bool _enableBatchedUpdates = true;
        [SerializeField] private float _canvasCullDistance = 200f;

        [Header("Element Pooling")]
        [SerializeField] private bool _enableUIPooling = true;
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;

        // Composition: Delegate responsibilities to focused components
        private ProjectChimera.Systems.UI.Performance.UIPerformanceMonitor _performanceMonitor;
        private UIEventHandler _eventHandler;
        private UIResourceManager _resourceManager;

        // Coordinator state
        private readonly MemoryOptimizedQueue<UIUpdateRequest> _updateQueue = new MemoryOptimizedQueue<UIUpdateRequest>();
        private readonly Dictionary<UIUpdateType, List<IUIUpdatable>> _batchedUpdates = new Dictionary<UIUpdateType, List<IUIUpdatable>>();
        private float _lastUIUpdateTime;
        private int _uiUpdatesThisFrame;
        private bool _isInitialized = false;

        // UI Elements for cultivation system
        private readonly Dictionary<string, PlantInfoPanel> _plantInfoPanels = new Dictionary<string, PlantInfoPanel>();
        private readonly List<ProgressBar> _activeProgressBars = new List<ProgressBar>();
        private readonly Queue<NotificationDisplay> _notificationQueue = new Queue<NotificationDisplay>();

        public static OptimizedUIManager Instance { get; private set; }
        public bool IsInitialized => _isInitialized;

        // Events
        public System.Action<string> OnPanelOpened;
        public System.Action<string> OnPanelClosed;
        public System.Action<UIPerformanceStats> OnPerformanceUpdate;

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.UIManager;
        public bool IsTickable => _enableUIOptimization && _isInitialized && enabled;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComponents();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            try
            {
                // Initialize components using composition
                _performanceMonitor = new ProjectChimera.Systems.UI.Performance.UIPerformanceMonitor(_enableLogging, _uiUpdateInterval);

                var eventHandlerGO = new GameObject("UIEventHandler");
                eventHandlerGO.transform.SetParent(transform);
                _eventHandler = eventHandlerGO.AddComponent<UIEventHandler>();

                var resourceManagerGO = new GameObject("UIResourceManager");
                resourceManagerGO.transform.SetParent(transform);
                _resourceManager = resourceManagerGO.AddComponent<UIResourceManager>();

                // Wire up events between components
                _performanceMonitor.OnPerformanceStatsUpdated += OnPerformanceStatsUpdatedInternal;
                _performanceMonitor.OnPerformanceWarning += OnPerformanceWarningInternal;
                _eventHandler.OnUIEventProcessed += OnUIEventProcessedInternal;
                _resourceManager.OnResourceStatsUpdated += OnResourceStatsUpdatedInternal;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UI", "OptimizedUIManager components initialized", this);
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("UI", $"Failed to initialize OptimizedUIManager components: {ex.Message}", this);
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize batched update system
                InitializeBatchedUpdateSystem();

                // Register performance monitor with UpdateOrchestrator
                UpdateOrchestrator.Instance?.RegisterTickable(_performanceMonitor);

                _lastUIUpdateTime = Time.unscaledTime;
                _isInitialized = true;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UI", "OptimizedUIManager initialized with composition pattern", this);
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("UI", $"OptimizedUIManager initialization failed: {ex.Message}", this);
            }
        }

        private void InitializeBatchedUpdateSystem()
        {
            // Initialize batched update categories
            System.Enum.GetValues(typeof(UIUpdateType)).Cast<UIUpdateType>().ToList().ForEach(type =>
            {
                _batchedUpdates[type] = new List<IUIUpdatable>();
            });

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI", "Batched update system initialized", this);
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (!_enableUIOptimization || !_isInitialized) return;

            _uiUpdatesThisFrame = 0;

            // Process batched updates at controlled intervals
            if (Time.unscaledTime - _lastUIUpdateTime >= _uiUpdateInterval)
            {
                ProcessBatchedUpdates();
                ProcessUpdateQueue();
                _lastUIUpdateTime = Time.unscaledTime;
            }

            // Process notifications every frame (but throttled)
            ProcessNotifications();
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("UI", "OptimizedUIManager registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("UI", "OptimizedUIManager unregistered from UpdateOrchestrator", this);
        }

        #endregion

        #region Update Processing

        private void ProcessBatchedUpdates()
        {
            if (!_enableBatchedUpdates) return;

            foreach (var updateType in _batchedUpdates.Keys.ToList())
            {
                var updatables = _batchedUpdates[updateType];
                var processed = 0;

                for (int i = updatables.Count - 1; i >= 0 && processed < _maxUIUpdatesPerFrame; i--)
                {
                    var updatable = updatables[i];
                    if (updatable == null)
                    {
                        updatables.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        using (_performanceMonitor.StartTracking($"BatchUpdate_{updateType}"))
                        {
                            updatable.UpdateUI();
                        }
                        processed++;
                        _uiUpdatesThisFrame++;
                    }
                    catch (System.Exception ex)
                    {
                        ChimeraLogger.LogError("UI", $"Error updating {updatable.GetType().Name}: {ex.Message}", this);
                        updatables.RemoveAt(i);
                    }
                }
            }
        }

        private void ProcessUpdateQueue()
        {
            int processed = 0;
            while (_updateQueue.Count > 0 && processed < _maxUIUpdatesPerFrame)
            {
                var request = _updateQueue.Dequeue();
                ProcessUIUpdateRequest(request);
                processed++;
                _uiUpdatesThisFrame++;
            }
        }

        private void ProcessUIUpdateRequest(UIUpdateRequest request)
        {
            try
            {
                using (_performanceMonitor.StartTracking($"UpdateRequest_{request.Type}"))
                {
                    switch (request.Type)
                    {
                        case UIUpdateType.PlantInfo:
                            UpdatePlantInfoPanel(request);
                            break;
                        case UIUpdateType.Progress:
                            UpdateProgressBar(request);
                            break;
                        case UIUpdateType.Notification:
                            ShowNotification(request);
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("UI", $"Error processing UI update request: {ex.Message}", this);
            }
        }

        private void ProcessNotifications()
        {
            if (_notificationQueue.Count > 0)
            {
                var notification = _notificationQueue.Dequeue();
                if (notification != null)
                {
                    // Display notification using resource manager
                    var notificationPanel = _resourceManager.GetPooledElement<NotificationDisplay>();
                    if (notificationPanel != null)
                    {
                        notificationPanel.gameObject.SetActive(true);
                        // Configure notification display
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnPerformanceStatsUpdatedInternal(UIPerformanceStats stats)
        {
            OnPerformanceUpdate?.Invoke(stats);
        }

        private void OnPerformanceWarningInternal(PerformanceWarning warning)
        {
            if (_enableLogging)
                ChimeraLogger.LogWarning("UI_PERF", $"Performance warning: {warning.Message}", this);
        }

        private void OnUIEventProcessedInternal(UIEvent uiEvent)
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_EVENTS", $"Processed UI event: {uiEvent.Type}", this);
        }

        private void OnResourceStatsUpdatedInternal(ResourceStats stats)
        {
            // Update performance monitor with resource statistics
            _performanceMonitor.RecordPoolingStats(stats.PooledElements, stats.ActiveElements, stats.PoolHits, stats.PoolMisses);
            _performanceMonitor.RecordCanvasOptimization(stats.TotalCanvases, stats.CulledCanvases);
        }

        #endregion

        #region Public API - Delegates to Components

        /// <summary>
        /// Register a UI panel for optimization
        /// </summary>
        public void RegisterPanel(string panelId, OptimizedUIPanel panel)
        {
            _resourceManager?.RegisterPanel(panelId, panel);
            OnPanelOpened?.Invoke(panelId);
        }

        /// <summary>
        /// Unregister a UI panel
        /// </summary>
        public void UnregisterPanel(string panelId)
        {
            _resourceManager?.UnregisterPanel(panelId);
            OnPanelClosed?.Invoke(panelId);
        }

        /// <summary>
        /// Get a pooled UI element
        /// </summary>
        public T GetPooledElement<T>() where T : Component
        {
            return _resourceManager?.GetPooledElement<T>();
        }

        /// <summary>
        /// Return element to pool
        /// </summary>
        public void ReturnToPool(GameObject element)
        {
            _resourceManager?.ReturnToPool(element);
        }

        /// <summary>
        /// Queue a UI update request
        /// </summary>
        public void QueueUIUpdate(UIUpdateRequest request)
        {
            _updateQueue.Enqueue(request);
        }

        /// <summary>
        /// Register for batched updates
        /// </summary>
        public void RegisterForBatchedUpdates(UIUpdateType type, IUIUpdatable updatable)
        {
            if (_batchedUpdates.TryGetValue(type, out var list))
            {
                list.Add(updatable);
            }
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public UIPerformanceStats GetPerformanceStats()
        {
            return _performanceMonitor?.GetPerformanceStats() ?? new UIPerformanceStats();
        }

        /// <summary>
        /// Get resource statistics
        /// </summary>
        public ResourceStats GetResourceStats()
        {
            return _resourceManager?.GetResourceStats() ?? new ResourceStats();
        }

        #endregion

        #region UI Update Helpers

        private void UpdatePlantInfoPanel(UIUpdateRequest request)
        {
            // Implementation for plant info panel updates
            if (request.Data is PlantInfoData plantData)
            {
                var panelId = $"PlantInfo_{plantData.PlantId}";
                if (_plantInfoPanels.TryGetValue(panelId, out var panel))
                {
                    panel.UpdatePlantData(plantData);
                }
            }
        }

        private void UpdateProgressBar(UIUpdateRequest request)
        {
            // Implementation for progress bar updates
            if (request.Data is ProgressData progressData)
            {
                var progressBar = _activeProgressBars.FirstOrDefault(pb => pb.Id == progressData.Id);
                if (progressBar != null)
                {
                    progressBar.UpdateProgressBar(progressData.Value, progressData.MaxValue);
                }
            }
        }

        private void ShowNotification(UIUpdateRequest request)
        {
            // Implementation for notification display
            if (request.Data is NotificationData notificationData)
            {
                var notification = _resourceManager.GetPooledElement<NotificationDisplay>();
                if (notification != null)
                {
                    notification.ShowNotification(notificationData.Message, notificationData.Type, notificationData.Duration);
                    _notificationQueue.Enqueue(notification);
                }
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Cleanup event handlers
                if (_performanceMonitor != null)
                {
                    _performanceMonitor.OnPerformanceStatsUpdated -= OnPerformanceStatsUpdatedInternal;
                    _performanceMonitor.OnPerformanceWarning -= OnPerformanceWarningInternal;
                    UpdateOrchestrator.Instance?.UnregisterTickable(_performanceMonitor);
                }

                if (_eventHandler != null)
                {
                    _eventHandler.OnUIEventProcessed -= OnUIEventProcessedInternal;
                }

                if (_resourceManager != null)
                {
                    _resourceManager.OnResourceStatsUpdated -= OnResourceStatsUpdatedInternal;
                }

                // Clear collections
                _updateQueue.Clear();
                _batchedUpdates.Clear();
                _plantInfoPanels.Clear();
                _activeProgressBars.Clear();
                _notificationQueue.Clear();

                if (_enableLogging)
                    ChimeraLogger.LogInfo("UI", "OptimizedUIManager cleanup completed", this);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("UI", $"Error during OptimizedUIManager cleanup: {ex.Message}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// UI update request
    /// </summary>
    [System.Serializable]
    public struct UIUpdateRequest
    {
        public UIUpdateType Type;
        public object Data;
        public float Priority;

        // Additional properties for UI performance optimizer
        public GameObject Target;
        public Vector3 Position;
        public bool IsVisible;
    }

    /// <summary>
    /// UI update types
    /// </summary>
    public enum UIUpdateType
    {
        PlantInfo,
        Progress,
        Notification,
        Canvas,
        Animation,
        Transform,
        Visibility,
        Content
    }


    /// <summary>
    /// Plant info data structure
    /// </summary>
    [System.Serializable]
    public struct PlantInfoData
    {
        public string PlantId;
        public string PlantName;
        public float Health;
        public float Growth;
    }

    /// <summary>
    /// Progress data structure
    /// </summary>
    [System.Serializable]
    public struct ProgressData
    {
        public string Id;
        public float Value;
        public float MaxValue;
    }


    #endregion
}
