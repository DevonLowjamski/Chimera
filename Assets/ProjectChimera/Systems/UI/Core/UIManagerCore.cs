using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: Core UI Manager with focused responsibilities
    /// Handles core UI system initialization and coordination only
    /// </summary>
    public class UIManagerCore : MonoBehaviour, ITickable
    {
        [Header("Core UI Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _uiUpdateInterval = 0.1f;

        // Core components
        private UIPanelManager _panelManager;
        private UIPerformanceOptimizer _performanceOptimizer;
        private UIElementFactory _elementFactory;
        private UIPerformanceMonitor _performanceMonitor;

        // Singleton pattern
        public static UIManagerCore Instance { get; private set; }

        // Properties
        public bool IsInitialized { get; private set; }
        public UIPanelManager PanelManager => _panelManager;
        public UIPerformanceOptimizer PerformanceOptimizer => _performanceOptimizer;
        public UIElementFactory ElementFactory => _elementFactory;

        // ITickable implementation
        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public System.Action OnUISystemInitialized;
        public System.Action OnUISystemShutdown;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                OnUISystemShutdown?.Invoke();
            }
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("UI", "Initializing core UI manager", this);

            // Initialize sub-components
            InitializePanelManager();
            InitializePerformanceOptimizer();
            InitializeElementFactory();
            InitializePerformanceMonitor();

            IsInitialized = true;
            OnUISystemInitialized?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "✅ Core UI manager initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            // Update sub-components
            _panelManager?.UpdatePanels(deltaTime);
            _performanceOptimizer?.UpdateOptimizations(deltaTime);
            // Performance monitor handles its own updates through ITickable
        }

        private void InitializePanelManager()
        {
            var panelManagerGO = new GameObject("UIPanelManager");
            panelManagerGO.transform.SetParent(transform);
            _panelManager = panelManagerGO.AddComponent<UIPanelManager>();
        }

        private void InitializePerformanceOptimizer()
        {
            var optimizerGO = new GameObject("UIPerformanceOptimizer");
            optimizerGO.transform.SetParent(transform);
            _performanceOptimizer = optimizerGO.AddComponent<UIPerformanceOptimizer>();
        }

        private void InitializeElementFactory()
        {
            var factoryGO = new GameObject("UIElementFactory");
            factoryGO.transform.SetParent(transform);
            _elementFactory = factoryGO.AddComponent<UIElementFactory>();
        }

        private void InitializePerformanceMonitor()
        {
            var monitorGO = new GameObject("UIPerformanceMonitor");
            monitorGO.transform.SetParent(transform);
            _performanceMonitor = monitorGO.AddComponent<UIPerformanceMonitor>();
        }

        /// <summary>
        /// Get UI system status for debugging
        /// </summary>
        public UISystemStatus GetSystemStatus()
        {
            return new UISystemStatus
            {
                IsInitialized = IsInitialized,
                ManagedPanels = _panelManager?.GetManagedPanelCount() ?? 0,
                ActiveOptimizations = _performanceOptimizer?.GetActiveOptimizationCount() ?? 0,
                ElementsInPool = _elementFactory?.GetPooledElementCount() ?? 0
            };
        }
    }

    /// <summary>
    /// UI system status data
    /// </summary>
    [System.Serializable]
    public struct UISystemStatus
    {
        public bool IsInitialized;
        public int ManagedPanels;
        public int ActiveOptimizations;
        public int ElementsInPool;
    }
}