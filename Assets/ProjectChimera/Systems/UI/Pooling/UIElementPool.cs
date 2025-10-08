using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.Pooling;
using System.Collections.Generic;

using ProjectChimera.Core.Logging;
namespace ProjectChimera.Systems.UI.Pooling
{
    /// <summary>
    /// PERFORMANCE: Specialized object pool for UI elements
    /// Optimizes UI creation/destruction for dynamic interfaces
    /// Week 9 Day 4-5: Object Pooling System Implementation
    /// </summary>
    public class UIElementPool : MonoBehaviour
    {
        [Header("UI Pool Settings")]
        [SerializeField] private UIPoolConfig[] _uiPoolConfigs;
        [SerializeField] private Canvas _poolCanvas;
        [SerializeField] private bool _enableLogging = true;

        // Pool storage
        private Dictionary<UIElementType, ObjectPool<RectTransform>> _uiPools;
        private Transform _poolParent;
        private bool _isInitialized;

        // Statistics
        private Dictionary<UIElementType, UIPoolStats> _poolStats;

        /// <summary>
        /// UI element types for pooling
        /// </summary>
        public enum UIElementType
        {
            PlantInfoPanel,
            ProgressBar,
            AlertDialog,
            ContextMenu,
            TooltipPanel,
            ListItem,
            Button,
            Icon,
            NotificationBanner
        }

        /// <summary>
        /// Initialize UI element pools
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Create pool parent
            _poolParent = new GameObject("UIPools").transform;
            _poolParent.SetParent(transform);

            // Create pools dictionary
            _uiPools = new Dictionary<UIElementType, ObjectPool<RectTransform>>();
            _poolStats = new Dictionary<UIElementType, UIPoolStats>();

            // Initialize pools from configuration
            foreach (var config in _uiPoolConfigs)
            {
                CreateUIPool(config);
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("UIElementPool", "Initialized UIElementPool");
            }
        }

        /// <summary>
        /// Get UI element from pool
        /// </summary>
        public RectTransform GetUIElement(UIElementType elementType, Transform parent = null)
        {
            if (!_isInitialized || !_uiPools.ContainsKey(elementType))
                return null;

            var startTime = Time.realtimeSinceStartup;
            var element = _uiPools[elementType].Get();

            if (element != null)
            {
                // Set parent if specified
                if (parent != null)
                {
                    element.SetParent(parent, false);
                }

                // Reset anchors and position
                ResetUIElement(element);

                // Update statistics
                if (!_poolStats.ContainsKey(elementType))
                {
                    _poolStats[elementType] = new UIPoolStats();
                }

                var stats = _poolStats[elementType];
                stats.TotalGets++;
                var getTime = Time.realtimeSinceStartup - startTime;
                stats.AverageGetTime = (stats.AverageGetTime * (stats.TotalGets - 1) + getTime) / stats.TotalGets;
                _poolStats[elementType] = stats;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UIElementPool", $"Got {elementType} element");
                }
            }

            return element;
        }

        /// <summary>
        /// Return UI element to pool
        /// </summary>
        public void ReturnUIElement(UIElementType elementType, RectTransform element)
        {
            if (!_isInitialized || element == null || !_uiPools.ContainsKey(elementType))
                return;

            _uiPools[elementType].Return(element);

            // Update statistics
            if (_poolStats.ContainsKey(elementType))
            {
                var stats = _poolStats[elementType];
                stats.TotalReturns++;
                _poolStats[elementType] = stats;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("UIElementPool", $"Returned {elementType} element");
            }
        }

        /// <summary>
        /// Get UI element with automatic type detection
        /// </summary>
        public RectTransform GetUIElement(GameObject prefab, Transform parent = null)
        {
            var elementType = DetectUIElementType(prefab);
            return GetUIElement(elementType, parent);
        }

        /// <summary>
        /// Return UI element with automatic type detection
        /// </summary>
        public void ReturnUIElement(RectTransform element)
        {
            var elementType = DetectUIElementType(element.gameObject);
            ReturnUIElement(elementType, element);
        }

        /// <summary>
        /// Pre-warm specific pool
        /// </summary>
        public void PrewarmPool(UIElementType elementType, int count)
        {
            if (!_isInitialized || !_uiPools.ContainsKey(elementType))
                return;

            var pool = _uiPools[elementType];
            var targetSize = pool.CountInactive + pool.CountActive + count;
            pool.Resize(targetSize);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("UIElementPool", $"Prewarmed {count} elements for {elementType}");
            }
        }

        /// <summary>
        /// Get pool statistics for specific element type
        /// </summary>
        public UIPoolStats GetPoolStats(UIElementType elementType)
        {
            if (_poolStats.ContainsKey(elementType))
            {
                var stats = _poolStats[elementType];
                if (_uiPools.ContainsKey(elementType))
                {
                    var pool = _uiPools[elementType];
                    stats.CountInactive = pool.CountInactive;
                    stats.CountActive = pool.CountActive;
                    stats.CountAll = pool.CountAll;
                }
                return stats;
            }

            return new UIPoolStats();
        }

        /// <summary>
        /// Get all pool statistics
        /// </summary>
        public Dictionary<UIElementType, UIPoolStats> GetAllPoolStats()
        {
            var allStats = new Dictionary<UIElementType, UIPoolStats>();

            foreach (var elementType in _uiPools.Keys)
            {
                allStats[elementType] = GetPoolStats(elementType);
            }

            return allStats;
        }

        /// <summary>
        /// Clear specific pool
        /// </summary>
        public void ClearPool(UIElementType elementType)
        {
            if (_uiPools.ContainsKey(elementType))
            {
                _uiPools[elementType].Clear();

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("UIElementPool", $"Cleared pool {elementType}");
                }
            }
        }

        /// <summary>
        /// Clear all UI pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _uiPools.Values)
            {
                pool.Clear();
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("UIElementPool", "Cleared all pools");
            }
        }

        // --- Compatibility shims for legacy callers (string/generic-based API) ---

        // Initialize with sizes (falls back to default Initialize)
        public void Initialize(int initialPoolSize, int maxPoolSize)
        {
            // Current implementation uses Scriptable config; simply ensure initialized
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        // Get pooled element by string key
        public GameObject GetPooledElement(string elementType)
        {
            var type = ParseElementType(elementType);
            var rt = GetUIElement(type);
            return rt != null ? rt.gameObject : null;
        }

        // Generic legacy accessor used by UIResourceManager
        public T GetPooledObject<T>() where T : Component
        {
            // Best-effort: try to pull by inferred enum from type name
            var inferred = ParseElementType(typeof(T).Name);
            var rt = GetUIElement(inferred);
            return rt != null ? rt.GetComponent<T>() : null;
        }

        // Return pooled element by object
        public void ReturnToPool(GameObject element)
        {
            if (element == null) return;
            ReturnUIElement(element.GetComponent<RectTransform>());
        }

        // Return pooled element with explicit type
        public void ReturnToPool(GameObject element, string elementType)
        {
            if (element == null) return;
            var type = ParseElementType(elementType);
            ReturnUIElement(type, element.GetComponent<RectTransform>());
        }

        // Create pooled object of generic type (no-op fallback)
        public T CreatePooledObject<T>() where T : Component
        {
            // Without a prefab reference, we cannot create; return null to allow caller fallback
            return null;
        }

        // Legacy stats wrappers
        public int PoolSize
        {
            get
            {
                int total = 0;
                if (_uiPools != null)
                {
                    foreach (var pool in _uiPools.Values) total += pool.CountAll;
                }
                return total;
            }
        }

        public int ActiveCount
        {
            get
            {
                int total = 0;
                if (_uiPools != null)
                {
                    foreach (var pool in _uiPools.Values) total += pool.CountActive;
                }
                return total;
            }
        }

        public int GetTotalPooledCount()
        {
            int total = 0;
            if (_uiPools != null)
            {
                foreach (var pool in _uiPools.Values) total += pool.CountInactive;
            }
            return total;
        }

        public void TrimPool()
        {
            // No-op trim for compatibility
        }

        public void InitializePool(string elementType, GameObject prefab, int initialCount)
        {
            // Compatibility stub – full configuration is done via _uiPoolConfigs
        }

        // Helper to map string keys to enum values
        private UIElementType ParseElementType(string elementType)
        {
            if (System.Enum.TryParse<UIElementType>(elementType, out var parsed))
            {
                return parsed;
            }
            // Heuristic mapping by common names
            var key = (elementType ?? string.Empty).ToLowerInvariant();
            if (key.Contains("progress")) return UIElementType.ProgressBar;
            if (key.Contains("notify")) return UIElementType.NotificationBanner;
            if (key.Contains("plant")) return UIElementType.PlantInfoPanel;
            return UIElementType.ListItem;
        }

        #region Private Methods

        /// <summary>
        /// Create pool for specific UI element type
        /// </summary>
        private void CreateUIPool(UIPoolConfig config)
        {
            if (config.prefab == null) return;

            var poolParent = new GameObject($"UIPool_{config.elementType}").transform;
            poolParent.SetParent(_poolParent);

            // Create pool with UI-specific actions
            var pool = new ObjectPool<RectTransform>(
                config.prefab,
                config.initialSize,
                config.maxSize,
                config.expandable,
                poolParent,
                createFunc: () => Instantiate(config.prefab, poolParent),
                onGetAction: OnUIElementGet,
                onReturnAction: OnUIElementReturn,
                onDestroyAction: OnUIElementDestroy
            );

            _uiPools[config.elementType] = pool;
        }

        /// <summary>
        /// Detect UI element type from GameObject
        /// </summary>
        private UIElementType DetectUIElementType(GameObject obj)
        {
            // Simple detection based on component types or names
            if (obj.GetComponent<Button>()) return UIElementType.Button;
            if (obj.GetComponent<Slider>()) return UIElementType.ProgressBar;
            if (obj.name.Contains("Panel")) return UIElementType.PlantInfoPanel;
            if (obj.name.Contains("Icon")) return UIElementType.Icon;
            if (obj.name.Contains("Tooltip")) return UIElementType.TooltipPanel;

            // Default
            return UIElementType.ListItem;
        }

        /// <summary>
        /// Reset UI element to default state
        /// </summary>
        private void ResetUIElement(RectTransform element)
        {
            // Reset transform
            element.anchoredPosition = Vector2.zero;
            element.localScale = Vector3.one;
            element.localRotation = Quaternion.identity;

            // Reset common UI components
            var canvasGroup = element.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Reset any IPoolable components
            var poolables = element.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnGetFromPool();
            }
        }

        /// <summary>
        /// Called when UI element is retrieved from pool
        /// </summary>
        private void OnUIElementGet(RectTransform element)
        {
            if (element != null)
            {
                element.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Called when UI element is returned to pool
        /// </summary>
        private void OnUIElementReturn(RectTransform element)
        {
            if (element != null)
            {
                element.gameObject.SetActive(false);
                element.SetParent(_poolParent);

                // Call OnReturnToPool on any IPoolable components
                var poolables = element.GetComponentsInChildren<IPoolable>(true);
                foreach (var poolable in poolables)
                {
                    poolable.OnReturnToPool();
                }
            }
        }

        /// <summary>
        /// Called when UI element is destroyed from pool
        /// </summary>
        private void OnUIElementDestroy(RectTransform element)
        {
            // Clean up any resources before destruction
        }

        #endregion

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}
