using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Systems.Camera;
using ProjectChimera.Systems.UI.Pooling;
using ProjectChimera.Systems.UI.Core;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.SimpleDI;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Resources
{
    /// <summary>
    /// REFACTORED: UI Resource Manager Bridge - Thin MonoBehaviour wrapper
    /// Delegates to UIResourceService for all business logic
    /// Single Responsibility: Unity lifecycle management and dependency injection
    /// PHASE 0: Migrated to ITickable pattern for zero-tolerance compliance
    /// </summary>
    public class UIResourceManager : MonoBehaviour, ITickable
    {
        [Header("Resource Configuration")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableUIPooling = true;
        [SerializeField] private bool _enableCanvasCulling = true;
        [SerializeField] private float _canvasCullDistance = 200f;

        [Header("Pooling Settings")]
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        [SerializeField] private int _poolGrowthIncrement = 10;

        // Service and Unity-specific components
        private UIResourceService _service;
        private UIElementPool _uiElementPool;
        private UnityEngine.Camera _referenceCamera;

        // Events - forwarded from service
        public event System.Action<ResourceStats> OnResourceStatsUpdated
        {
            add { if (_service != null) _service.OnResourceStatsUpdated += value; }
            remove { if (_service != null) _service.OnResourceStatsUpdated -= value; }
        }

        public event System.Action<string> OnResourceWarning
        {
            add { if (_service != null) _service.OnResourceWarning += value; }
            remove { if (_service != null) _service.OnResourceWarning -= value; }
        }

        // Properties
        public bool IsInitialized => _service?.IsInitialized ?? false;
        public ResourceStats Stats => _service?.Stats ?? new ResourceStats();

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeService();
            InitializeResourceManager();

            // PHASE 0: Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void Start()
        {
            DiscoverAndRegisterCanvases();
        }

        private void OnDestroy()
        {
            // PHASE 0: Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.UIManager + 10;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            UpdateCanvasOptimizations();
            ProcessResourceOperations();
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            _service = new UIResourceService(
                _enableLogging,
                _enableUIPooling,
                _enableCanvasCulling,
                _canvasCullDistance,
                _initialPoolSize,
                _maxPoolSize,
                _poolGrowthIncrement
            );

            _service.Initialize();
        }

        private void InitializeResourceManager()
        {
            // Try main camera first, then ServiceContainer
            _referenceCamera = UnityEngine.Camera.main;
            if (_referenceCamera == null)
            {
                var cameraService = ServiceContainerFactory.Instance?.TryResolve<ICameraProvider>();
                _referenceCamera = cameraService?.main;

                if (_referenceCamera == null && _enableLogging)
                {
                    ChimeraLogger.LogWarning("UI_RESOURCES",
                        "No camera found - UI resource management may not work correctly", this);
                }
            }

            if (_enableUIPooling)
            {
                InitializeUIPool();
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", "UIResourceManager initialized", this);
        }

        private void InitializeUIPool()
        {
            var poolGO = new GameObject("UIElementPool");
            poolGO.transform.SetParent(transform);
            _uiElementPool = poolGO.AddComponent<UIElementPool>();

            // Configure pool settings
            _uiElementPool.Initialize(_initialPoolSize, _maxPoolSize);

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", $"UI element pool initialized with {_initialPoolSize} elements", this);
        }

        private void DiscoverAndRegisterCanvases()
        {
            // Use GameObjectRegistry for canvas discovery
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();

            if (registry != null)
            {
                // Registry-based approach - canvases should self-register
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UI_RESOURCES",
                        "Using GameObjectRegistry for canvas management - canvases should self-register", this);
                }
            }
            else
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI_RESOURCES",
                        "GameObjectRegistry not available - canvases must manually register via RegisterCanvas()", this);
                }
            }
        }

        #endregion

        #region Canvas Management (delegates to service)

        /// <summary>
        /// Register a canvas for optimization management
        /// </summary>
        public void RegisterCanvas(Canvas canvas)
            => _service?.RegisterCanvas(canvas, Time.time);

        /// <summary>
        /// Unregister a canvas from management
        /// </summary>
        public void UnregisterCanvas(Canvas canvas)
            => _service?.UnregisterCanvas(canvas);

        /// <summary>
        /// Update canvas optimizations
        /// </summary>
        private void UpdateCanvasOptimizations()
        {
            if (_referenceCamera == null || _service == null)
                return;

            var cameraPosition = _referenceCamera.transform.position;
            _service.UpdateCanvasOptimizations(
                cameraPosition,
                Time.time,
                (canvas, camPos) => ShouldCanvasBeVisible(canvas, camPos)
            );
        }

        /// <summary>
        /// Determine if a canvas should be visible
        /// </summary>
        private bool ShouldCanvasBeVisible(Canvas canvas, Vector3 cameraPosition)
        {
            // Distance-based culling
            var distance = Vector3.Distance(canvas.transform.position, cameraPosition);
            if (distance > _canvasCullDistance)
                return false;

            // Screen space canvases are always visible
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return true;

            // World space canvas visibility check
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                var bounds = GetCanvasBounds(canvas);
                return IsInCameraView(bounds, _referenceCamera);
            }

            return true;
        }

        /// <summary>
        /// Get bounds of a canvas
        /// </summary>
        private Bounds GetCanvasBounds(Canvas canvas)
        {
            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var bounds = new Bounds(rectTransform.position, rectTransform.sizeDelta);
                return bounds;
            }
            return new Bounds(canvas.transform.position, Vector3.one);
        }

        /// <summary>
        /// Check if bounds are in camera view
        /// </summary>
        private bool IsInCameraView(Bounds bounds, UnityEngine.Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        #endregion

        #region UI Element Pooling (delegates to service with Unity pool management)

        /// <summary>
        /// Get a pooled UI element
        /// </summary>
        public T GetPooledElement<T>() where T : Component
        {
            if (!_enableUIPooling || _uiElementPool == null)
                return null;

            var element = _uiElementPool.GetPooledObject<T>();
            if (element != null)
            {
                _service?.RecordPoolHit(typeof(T).Name, Time.realtimeSinceStartup);
            }
            else
            {
                _service?.RecordPoolMiss(typeof(T).Name, Time.realtimeSinceStartup);
            }

            UpdatePoolStats();
            return element;
        }

        /// <summary>
        /// Return a UI element to the pool
        /// </summary>
        public void ReturnToPool(GameObject element)
        {
            if (!_enableUIPooling || _uiElementPool == null || element == null)
                return;

            _uiElementPool.ReturnToPool(element);
            _service?.RecordPoolReturn(element.name, Time.realtimeSinceStartup);
            UpdatePoolStats();
        }

        /// <summary>
        /// Pre-warm the pool with specific element types
        /// </summary>
        public void PrewarmPool<T>(int count) where T : Component
        {
            if (!_enableUIPooling || _uiElementPool == null)
                return;

            for (int i = 0; i < count; i++)
            {
                var element = _uiElementPool.CreatePooledObject<T>();
                if (element != null)
                {
                    element.gameObject.SetActive(false);
                }
            }

            UpdatePoolStats();
            _service?.RecordPoolPrewarm(typeof(T).Name, count, Time.realtimeSinceStartup);

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", $"Prewarmed pool with {count} {typeof(T).Name} elements", this);
        }

        /// <summary>
        /// Update pool statistics
        /// </summary>
        private void UpdatePoolStats()
        {
            if (_uiElementPool != null && _service != null)
            {
                _service.UpdatePoolStats(_uiElementPool.PoolSize, _uiElementPool.ActiveCount);
            }
        }

        #endregion

        #region Panel Management (delegates to service)

        /// <summary>
        /// Register an optimized UI panel
        /// </summary>
        public void RegisterPanel(string panelId, OptimizedUIPanel panel)
            => _service?.RegisterPanel(panelId, panel, Time.realtimeSinceStartup);

        /// <summary>
        /// Unregister an optimized UI panel
        /// </summary>
        public void UnregisterPanel(string panelId)
            => _service?.UnregisterPanel(panelId, Time.realtimeSinceStartup);

        /// <summary>
        /// Get a registered panel
        /// </summary>
        public OptimizedUIPanel GetPanel(string panelId)
            => _service?.GetPanel(panelId);

        /// <summary>
        /// Get all registered panels
        /// </summary>
        public IEnumerable<OptimizedUIPanel> GetAllPanels()
            => _service?.GetAllPanels() ?? new List<OptimizedUIPanel>();

        #endregion

        #region Resource Operations (delegates to service)

        /// <summary>
        /// Process queued resource operations
        /// </summary>
        private void ProcessResourceOperations()
        {
            _service?.ProcessResourceOperations(Time.realtimeSinceStartup);
        }

        #endregion

        #region Public API (delegates to service)

        /// <summary>
        /// Get current resource statistics
        /// </summary>
        public ResourceStats GetResourceStats()
            => _service?.Stats ?? new ResourceStats();

        /// <summary>
        /// Get recent resource operations
        /// </summary>
        public ResourceOperation[] GetRecentOperations(int count = 10)
            => _service?.GetRecentOperations(count) ?? new ResourceOperation[0];

        /// <summary>
        /// Optimize all managed resources
        /// </summary>
        public void OptimizeResources()
        {
            if (_service == null) return;

            // Update canvas optimizations first
            UpdateCanvasOptimizations();

            // Optimize resources through service
            _service.OptimizeResources(
                Time.realtimeSinceStartup,
                () => _uiElementPool?.TrimPool()
            );

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", "Resource optimization completed", this);
        }

        /// <summary>
        /// Reset resource statistics
        /// </summary>
        public void ResetStats()
            => _service?.ResetStats();

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Canvas optimization tracking data
    /// </summary>
    [System.Serializable]
    public class CanvasOptimizationData
    {
        public int OriginalSortingOrder;
        public bool IsVisible;
        public float LastVisibilityCheck;
    }

    /// <summary>
    /// Resource operation tracking
    /// </summary>
    [System.Serializable]
    public struct ResourceOperation
    {
        public string Category;
        public string Operation;
        public float Timestamp;
    }

    /// <summary>
    /// Resource management statistics
    /// </summary>
    [System.Serializable]
    public class ResourceStats
    {
        public int TotalCanvases = 0;
        public int CulledCanvases = 0;
        public float CanvasOptimizationRatio = 0f;
        public int PooledElements = 0;
        public int ActiveElements = 0;
        public float PoolUtilization = 0f;
        public int PoolHits = 0;
        public int PoolMisses = 0;
        public float PoolHitRate = 0f;
        public int ActivePanels = 0;
        public float LastUpdateTime = 0f;
    }

    #endregion
}
