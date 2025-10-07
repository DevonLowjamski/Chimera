using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Systems.UI.Pooling;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: UI Element Pool Manager
    /// Single Responsibility: UI element pooling and memory optimization
    /// Extracted from OptimizedUIManager for better separation of concerns
    /// </summary>
    public class UIElementPoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;

        private UIElementPool _uiElementPool;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializePool();
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "UI Element Pool Manager initialized", this);
            }
        }

        private void InitializePool()
        {
            var poolGO = new GameObject("UIElementPool");
            poolGO.transform.SetParent(transform);
            _uiElementPool = poolGO.AddComponent<UIElementPool>();
        }

        public T GetPooledElement<T>() where T : Component
        {
            var elementGO = _uiElementPool?.GetPooledElement(typeof(T).Name);
            return elementGO?.GetComponent<T>();
        }

        public void ReturnPooledElement<T>(T element) where T : Component
        {
            if (element != null)
                _uiElementPool?.ReturnToPool(element.gameObject);
        }

        public void ClearPool()
        {
            // Clear all element types - would need to enumerate all types in a real implementation
            foreach (var elementType in System.Enum.GetValues(typeof(UIElementPool.UIElementType)))
            {
                _uiElementPool?.ClearPool((UIElementPool.UIElementType)elementType);
            }
        }

        public UIPoolStats GetPoolStats()
        {
            // Create default stats since GetStats method doesn't exist in UIElementPool
            return new UIPoolStats
            {
                TotalElements = _uiElementPool?.transform.childCount ?? 0,
                ActiveElements = 0, // Would need implementation in UIElementPool
                PoolHits = 0,       // Would need implementation in UIElementPool
                PoolMisses = 0      // Would need implementation in UIElementPool
            };
        }

        private void OnDestroy()
        {
            ClearPool();
        }
    }

    [System.Serializable]
    public struct UIPoolStats
    {
        public int ActiveElements;
        public int AvailableElements;
        public int TotalCreated;
        public float MemoryUsage;
        public int TotalElements;
        public int PoolHits;
        public int PoolMisses;
    }
}