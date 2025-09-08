using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Object pool for UI documents used in world space menus.
    /// Extracted from WorldSpaceMenuRenderer.cs to reduce file size and improve reusability.
    /// </summary>
    public class WorldSpaceUIPool : MonoBehaviour
    {
        private readonly Queue<UIDocument> _pool = new Queue<UIDocument>();
        private Transform _poolContainer;
        
        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private int _maxPoolSize = 50;
        
        public int PoolSize => _pool.Count;
        public int MaxPoolSize => _maxPoolSize;
        
        private void Awake()
        {
            InitializePool();
        }
        
        /// <summary>
        /// Initializes the UI document pool
        /// </summary>
        private void InitializePool()
        {
            // Create pool container
            var poolObject = new GameObject("WorldSpaceUI_Pool");
            poolObject.transform.SetParent(transform);
            _poolContainer = poolObject.transform;
            
            // Pre-populate pool
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var document = CreateUIDocument();
                ResetUIDocument(document);
                _pool.Enqueue(document);
            }
            
            ChimeraLogger.Log($"[WorldSpaceUIPool] Initialized with {_initialPoolSize} UI documents");
        }
        
        /// <summary>
        /// Gets a UI document from the pool
        /// </summary>
        public UIDocument Get()
        {
            if (_pool.Count > 0)
            {
                var document = _pool.Dequeue();
                document.gameObject.SetActive(true);
                return document;
            }
            
            // Create new if pool is empty
            ChimeraLogger.Log("[WorldSpaceUIPool] Pool empty, creating new UI document");
            return CreateUIDocument();
        }
        
        /// <summary>
        /// Returns a UI document to the pool
        /// </summary>
        public void Return(UIDocument document)
        {
            if (document == null) return;
            
            // Don't exceed max pool size
            if (_pool.Count >= _maxPoolSize)
            {
                DestroyUIDocument(document);
                return;
            }
            
            ResetUIDocument(document);
            _pool.Enqueue(document);
        }
        
        /// <summary>
        /// Creates a new UI document for world space use
        /// </summary>
        private UIDocument CreateUIDocument()
        {
            var menuObject = new GameObject("WorldSpaceMenu");
            menuObject.transform.SetParent(_poolContainer);
            
            var uiDocument = menuObject.AddComponent<UIDocument>();
            var canvasGroup = menuObject.AddComponent<CanvasGroup>();
            
            // Configure for world space rendering
            uiDocument.sortingOrder = 100;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            return uiDocument;
        }
        
        /// <summary>
        /// Resets a UI document for reuse
        /// </summary>
        private void ResetUIDocument(UIDocument document)
        {
            if (document == null) return;
            
            document.gameObject.SetActive(false);
            document.visualTreeAsset = null;
            document.transform.SetParent(_poolContainer);
            document.transform.localPosition = Vector3.zero;
            document.transform.localRotation = Quaternion.identity;
            document.transform.localScale = Vector3.one;
            
            var canvasGroup = document.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
        
        /// <summary>
        /// Destroys a UI document
        /// </summary>
        private void DestroyUIDocument(UIDocument document)
        {
            if (document != null && document.gameObject != null)
            {
                DestroyImmediate(document.gameObject);
            }
        }
        
        /// <summary>
        /// Clears the entire pool
        /// </summary>
        public void ClearPool()
        {
            while (_pool.Count > 0)
            {
                var document = _pool.Dequeue();
                DestroyUIDocument(document);
            }
            
            ChimeraLogger.Log("[WorldSpaceUIPool] Pool cleared");
        }
        
        /// <summary>
        /// Preloads additional UI documents
        /// </summary>
        public void PreloadDocuments(int count)
        {
            for (int i = 0; i < count && _pool.Count < _maxPoolSize; i++)
            {
                var document = CreateUIDocument();
                ResetUIDocument(document);
                _pool.Enqueue(document);
            }
            
            ChimeraLogger.Log($"[WorldSpaceUIPool] Preloaded {count} documents, pool size: {_pool.Count}");
        }
        
        private void OnDestroy()
        {
            ClearPool();
        }
    }
    
    /// <summary>
    /// Configuration data for world space menus
    /// </summary>
    [System.Serializable]
    public class WorldSpaceMenuConfig
    {
        [Header("Positioning")]
        public Vector3 menuOffset = Vector3.up * 1.5f;
        public float menuDistance = 2.0f;
        public float menuScale = 1.0f;
        
        [Header("Behavior")]
        public bool billboardMode = true;
        public bool adaptiveScaling = true;
        public bool enableDepthOcclusion = true;
        
        [Header("Visibility")]
        public float fadeDistance = 50.0f;
        public LayerMask facilityLayers = 1 << 8;
        public AnimationCurve distanceScaleCurve = AnimationCurve.Linear(0f, 1f, 50f, 0.3f);
        
        [Header("Animation")]
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.2f;
        public AnimationCurve fadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
    
    /// <summary>
    /// Data for a world space menu instance
    /// </summary>
    public class WorldSpaceMenuData
    {
        public GameObject Target { get; set; }
        public WorldSpaceMenuType MenuType { get; set; }
        public List<string> MenuItems { get; set; } = new List<string>();
        public UIDocument UIDocument { get; set; }
        public float CreationTime { get; set; }
        public bool IsAnimating { get; set; }
        public Vector3 LastPosition { get; set; }
        public float LastDistance { get; set; }
    }
    
    /// <summary>
    /// Types of world space menus
    /// </summary>
    public enum WorldSpaceMenuType
    {
        Facility,
        Plant,
        Equipment,
        Generic
    }
}