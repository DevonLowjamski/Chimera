using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Core;

namespace ProjectChimera.Systems.UI.Resources
{
    /// <summary>
    /// REFACTORED: UI Resource Service (POCO - Unity-independent core)
    /// Single Responsibility: UI resource management business logic
    /// Extracted from UIResourceManager for clean architecture compliance
    /// </summary>
    public class UIResourceService
    {
        private readonly bool _enableLogging;
        private readonly bool _enableUIPooling;
        private readonly bool _enableCanvasCulling;
        private readonly float _canvasCullDistance;
        private readonly int _initialPoolSize;
        private readonly int _maxPoolSize;
        private readonly int _poolGrowthIncrement;

        // Resource management
        private readonly List<Canvas> _managedCanvases = new List<Canvas>();
        private readonly Dictionary<string, OptimizedUIPanel> _activePanels = new Dictionary<string, OptimizedUIPanel>();
        private readonly Dictionary<Canvas, CanvasOptimizationData> _canvasOptimizationData = new Dictionary<Canvas, CanvasOptimizationData>();

        // Performance tracking
        private readonly Queue<ResourceOperation> _resourceOperations = new Queue<ResourceOperation>();
        private ResourceStats _stats = new ResourceStats();

        // Events
        public event System.Action<ResourceStats> OnResourceStatsUpdated;
        public event System.Action<string> OnResourceWarning;

        // Properties
        public bool IsInitialized { get; private set; }
        public ResourceStats Stats => _stats;

        public UIResourceService(
            bool enableLogging = false,
            bool enableUIPooling = true,
            bool enableCanvasCulling = true,
            float canvasCullDistance = 200f,
            int initialPoolSize = 50,
            int maxPoolSize = 200,
            int poolGrowthIncrement = 10)
        {
            _enableLogging = enableLogging;
            _enableUIPooling = enableUIPooling;
            _enableCanvasCulling = enableCanvasCulling;
            _canvasCullDistance = canvasCullDistance;
            _initialPoolSize = initialPoolSize;
            _maxPoolSize = maxPoolSize;
            _poolGrowthIncrement = poolGrowthIncrement;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            _stats = new ResourceStats();
            IsInitialized = true;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", "UIResourceService initialized", null);
        }

        #region Canvas Management

        /// <summary>
        /// Register a canvas for optimization management
        /// </summary>
        public void RegisterCanvas(Canvas canvas, float currentTime)
        {
            if (canvas == null || _managedCanvases.Contains(canvas))
                return;

            _managedCanvases.Add(canvas);
            _canvasOptimizationData[canvas] = new CanvasOptimizationData
            {
                OriginalSortingOrder = canvas.sortingOrder,
                IsVisible = canvas.enabled,
                LastVisibilityCheck = currentTime
            };

            _stats.TotalCanvases++;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", $"Registered canvas: {canvas.name}", null);
        }

        /// <summary>
        /// Unregister a canvas from management
        /// </summary>
        public void UnregisterCanvas(Canvas canvas)
        {
            if (canvas == null) return;

            _managedCanvases.Remove(canvas);
            _canvasOptimizationData.Remove(canvas);
            _stats.TotalCanvases--;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", $"Unregistered canvas: {canvas.name}", null);
        }

        /// <summary>
        /// Update canvas optimizations
        /// </summary>
        public void UpdateCanvasOptimizations(
            Vector3 cameraPosition,
            float currentTime,
            System.Func<Canvas, Vector3, bool> shouldBeVisibleFunc)
        {
            if (!_enableCanvasCulling)
                return;

            int culledCount = 0;

            foreach (var canvas in _managedCanvases.ToList()) // ToList to avoid modification during iteration
            {
                if (canvas == null)
                {
                    _managedCanvases.Remove(canvas);
                    continue;
                }

                var optimizationData = _canvasOptimizationData[canvas];
                var shouldBeVisible = shouldBeVisibleFunc(canvas, cameraPosition);

                if (shouldBeVisible != optimizationData.IsVisible)
                {
                    optimizationData.IsVisible = shouldBeVisible;
                    optimizationData.LastVisibilityCheck = currentTime;
                    canvas.enabled = shouldBeVisible;

                    if (!shouldBeVisible)
                        culledCount++;

                    RecordResourceOperation($"Canvas {canvas.name}", shouldBeVisible ? "Enabled" : "Culled", currentTime);
                }
            }

            _stats.CulledCanvases = culledCount;
            _stats.CanvasOptimizationRatio = _managedCanvases.Count > 0 ? (float)culledCount / _managedCanvases.Count : 0f;
        }

        /// <summary>
        /// Get all managed canvases
        /// </summary>
        public List<Canvas> GetManagedCanvases()
        {
            return new List<Canvas>(_managedCanvases);
        }

        #endregion

        #region Panel Management

        /// <summary>
        /// Register an optimized UI panel
        /// </summary>
        public void RegisterPanel(string panelId, OptimizedUIPanel panel, float currentTime)
        {
            if (string.IsNullOrEmpty(panelId) || panel == null)
                return;

            _activePanels[panelId] = panel;
            _stats.ActivePanels++;

            RecordResourceOperation("Panel", $"Registered {panelId}", currentTime);

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", $"Registered UI panel: {panelId}", null);
        }

        /// <summary>
        /// Unregister an optimized UI panel
        /// </summary>
        public void UnregisterPanel(string panelId, float currentTime)
        {
            if (string.IsNullOrEmpty(panelId))
                return;

            if (_activePanels.Remove(panelId))
            {
                _stats.ActivePanels--;
                RecordResourceOperation("Panel", $"Unregistered {panelId}", currentTime);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("UI_RESOURCES", $"Unregistered UI panel: {panelId}", null);
            }
        }

        /// <summary>
        /// Get a registered panel
        /// </summary>
        public OptimizedUIPanel GetPanel(string panelId)
        {
            return _activePanels.TryGetValue(panelId, out var panel) ? panel : null;
        }

        /// <summary>
        /// Get all registered panels
        /// </summary>
        public IEnumerable<OptimizedUIPanel> GetAllPanels()
        {
            return _activePanels.Values;
        }

        #endregion

        #region Pool Statistics

        /// <summary>
        /// Record pool hit
        /// </summary>
        public void RecordPoolHit(string elementType, float currentTime)
        {
            _stats.PoolHits++;
            RecordResourceOperation("Pool", $"Retrieved {elementType}", currentTime);
            UpdatePoolStats(0, 0); // Stats will be updated externally
        }

        /// <summary>
        /// Record pool miss
        /// </summary>
        public void RecordPoolMiss(string elementType, float currentTime)
        {
            _stats.PoolMisses++;
            RecordResourceOperation("Pool", $"Miss for {elementType}", currentTime);
            UpdatePoolStats(0, 0); // Stats will be updated externally
        }

        /// <summary>
        /// Record pool return
        /// </summary>
        public void RecordPoolReturn(string elementName, float currentTime)
        {
            RecordResourceOperation("Pool", $"Returned {elementName}", currentTime);
        }

        /// <summary>
        /// Record pool prewarm
        /// </summary>
        public void RecordPoolPrewarm(string elementType, int count, float currentTime)
        {
            RecordResourceOperation("Pool", $"Prewarmed {count} {elementType} elements", currentTime);
        }

        /// <summary>
        /// Update pool statistics
        /// </summary>
        public void UpdatePoolStats(int pooledElements, int activeElements)
        {
            _stats.PooledElements = pooledElements;
            _stats.ActiveElements = activeElements;
            _stats.PoolUtilization = _stats.PooledElements > 0 ? (float)_stats.ActiveElements / _stats.PooledElements : 0f;
            _stats.PoolHitRate = (_stats.PoolHits + _stats.PoolMisses) > 0 ? (float)_stats.PoolHits / (_stats.PoolHits + _stats.PoolMisses) : 0f;
        }

        #endregion

        #region Resource Operations

        /// <summary>
        /// Record a resource operation for tracking
        /// </summary>
        private void RecordResourceOperation(string category, string operation, float timestamp)
        {
            var resourceOperation = new ResourceOperation
            {
                Category = category,
                Operation = operation,
                Timestamp = timestamp
            };

            _resourceOperations.Enqueue(resourceOperation);

            // Maintain queue size
            while (_resourceOperations.Count > 100)
            {
                _resourceOperations.Dequeue();
            }
        }

        /// <summary>
        /// Process queued resource operations
        /// </summary>
        public void ProcessResourceOperations(float currentTime)
        {
            // Update stats periodically
            _stats.LastUpdateTime = currentTime;

            // Check for resource warnings
            CheckResourceWarnings();

            // Emit stats update event
            OnResourceStatsUpdated?.Invoke(_stats);
        }

        /// <summary>
        /// Check for resource-related warnings
        /// </summary>
        private void CheckResourceWarnings()
        {
            // Check pool utilization
            if (_stats.PoolUtilization > 0.9f)
            {
                OnResourceWarning?.Invoke($"Pool utilization high: {_stats.PoolUtilization:F2}");
            }

            // Check pool hit rate
            if (_stats.PoolHitRate < 0.5f && (_stats.PoolHits + _stats.PoolMisses) > 10)
            {
                OnResourceWarning?.Invoke($"Pool hit rate low: {_stats.PoolHitRate:F2}");
            }

            // Check canvas count
            if (_managedCanvases.Count > 20)
            {
                OnResourceWarning?.Invoke($"Too many active canvases: {_managedCanvases.Count}");
            }
        }

        /// <summary>
        /// Get recent resource operations
        /// </summary>
        public ResourceOperation[] GetRecentOperations(int count = 10)
        {
            return _resourceOperations.TakeLast(count).ToArray();
        }

        #endregion

        #region Resource Optimization

        /// <summary>
        /// Optimize managed resources
        /// </summary>
        public List<string> OptimizeResources(float currentTime, System.Action trimPoolAction)
        {
            var inactivePanels = _activePanels.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var panelId in inactivePanels)
            {
                UnregisterPanel(panelId, currentTime);
            }

            // Optimize pool if needed
            if (_stats.PoolUtilization < 0.3f)
            {
                trimPoolAction?.Invoke();
            }

            RecordResourceOperation("System", "Resource optimization completed", currentTime);

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", "Resource optimization completed", null);

            return inactivePanels;
        }

        /// <summary>
        /// Reset resource statistics
        /// </summary>
        public void ResetStats()
        {
            _stats = new ResourceStats
            {
                TotalCanvases = _managedCanvases.Count,
                ActivePanels = _activePanels.Count
            };

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_RESOURCES", "Resource statistics reset", null);
        }

        #endregion
    }
}
