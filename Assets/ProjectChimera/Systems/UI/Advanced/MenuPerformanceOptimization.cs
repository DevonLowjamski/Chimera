using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using ProjectChimera.Systems.UI.Advanced;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Phase 2.3.5: Performance Optimization for Advanced Menu System
    /// Provides comprehensive performance optimization including object pooling,
    /// LOD system, batch processing, and memory management
    /// </summary>
    public class MenuPerformanceOptimization : MonoBehaviour
    {
        [Header("Performance Configuration")]
        [SerializeField] private bool _enableOptimizations = true;
        [SerializeField] private bool _enableObjectPooling = true;
        [SerializeField] private bool _enableLODSystem = true;
        [SerializeField] private bool _enableBatchProcessing = true;
        [SerializeField] private bool _enableMemoryOptimization = true;
        
        [Header("Object Pooling")]
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        [SerializeField] private bool _allowPoolExpansion = true;
        [SerializeField] private float _poolCleanupInterval = 30f;
        
        [Header("LOD System")]
        [SerializeField] private float _lodNearDistance = 5f;
        [SerializeField] private float _lodFarDistance = 20f;
        [SerializeField] private bool _enableDetailCulling = true;
        [SerializeField] private bool _enableAnimationLOD = true;
        
        [Header("Batch Processing")]
        [SerializeField] private int _maxUpdatesPerFrame = 10;
        [SerializeField] private int _maxAnimationsPerFrame = 5;
        [SerializeField] private float _batchUpdateInterval = 0.016f; // ~60 FPS
        [SerializeField] private bool _prioritizeVisibleElements = true;
        
        [Header("Memory Optimization")]
        [SerializeField] private bool _enableGarbageCollection = true;
        [SerializeField] private float _gcInterval = 60f;
        [SerializeField] private bool _enableTextureStreaming = true;
        [SerializeField] private bool _enableAssetUnloading = true;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private bool _showPerformanceStats = false;
        [SerializeField] private float _profilingUpdateInterval = 1f;
        
        // System references
        private AdvancedMenuSystem _menuSystem;
        private VisualFeedbackIntegration _visualFeedback;
        private ContextAwareActionFilter _actionFilter;
        
        // Object pooling
        private Dictionary<Type, Queue<VisualElement>> _elementPools = new Dictionary<Type, Queue<VisualElement>>();
        private Dictionary<VisualElement, float> _elementLastUsed = new Dictionary<VisualElement, float>();
        private HashSet<VisualElement> _activeElements = new HashSet<VisualElement>();
        
        // LOD system
        private Dictionary<VisualElement, LODLevel> _elementLOD = new Dictionary<VisualElement, LODLevel>();
        private List<VisualElement> _visibleElements = new List<VisualElement>();
        
        // Batch processing
        private Queue<IUpdateable> _updateQueue = new Queue<IUpdateable>();
        private Queue<IAnimatable> _animationQueue = new Queue<IAnimatable>();
        private List<VisualElement> _pendingUpdates = new List<VisualElement>();
        
        // Performance tracking
        private PerformanceMetrics _metrics = new PerformanceMetrics();
        private float _lastProfilingUpdate;
        private int _frameCount;
        
        // Memory management
        private float _lastGCTime;
        private HashSet<Texture2D> _loadedTextures = new HashSet<Texture2D>();
        private Dictionary<string, WeakReference> _assetCache = new Dictionary<string, WeakReference>();
        
        // Events
        public event Action<PerformanceMetrics> OnPerformanceUpdated;
        public event Action<string> OnOptimizationApplied;
        
        private void Awake()
        {
            InitializeOptimizationSystem();
        }
        
        private void Start()
        {
            SetupPerformanceOptimization();
            StartCoroutine(OptimizationUpdateLoop());
            
            if (_enableObjectPooling)
            {
                InitializeObjectPools();
            }
        }
        
        private void Update()
        {
            if (!_enableOptimizations)
                return;
            
            _frameCount++;
            
            // Update performance metrics
            UpdatePerformanceMetrics();
            
            // Process batch updates
            if (_enableBatchProcessing)
            {
                ProcessBatchUpdates();
            }
            
            // Update LOD system
            if (_enableLODSystem)
            {
                UpdateLODSystem();
            }
            
            // Memory management
            if (_enableMemoryOptimization)
            {
                CheckMemoryOptimization();
            }
        }
        
        private void InitializeOptimizationSystem()
        {
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            _visualFeedback = GetComponent<VisualFeedbackIntegration>();
            _actionFilter = GetComponent<ContextAwareActionFilter>();
            
            if (_menuSystem == null)
            {
                Debug.LogError("[MenuPerformanceOptimization] AdvancedMenuSystem component required");
                enabled = false;
                return;
            }
            
            // Subscribe to system events
            _menuSystem.OnMenuOpened += OnMenuOpened;
            _menuSystem.OnMenuClosed += OnMenuClosed;
            _menuSystem.OnActionExecuted += OnActionExecuted;
        }
        
        private void SetupPerformanceOptimization()
        {
            // Configure Unity's UI Toolkit performance settings
            var panelSettings = GetComponent<PanelSettings>();
            if (panelSettings != null)
            {
                // Enable GPU acceleration if available
                panelSettings.clearColor = true;
                panelSettings.sortingOrder = 1000; // Ensure UI renders on top
            }
            
            // Set target frame rate for consistent performance
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }
        
        private void InitializeObjectPools()
        {
            // Initialize pools for common element types
            CreateElementPool<Button>(_initialPoolSize);
            CreateElementPool<Label>(_initialPoolSize);
            CreateElementPool<VisualElement>(_initialPoolSize * 2);
            CreateElementPool<ScrollView>(_initialPoolSize / 5);
            CreateElementPool<ListView>(_initialPoolSize / 10);
        }
        
        /// <summary>
        /// Get an element from the object pool or create a new one
        /// </summary>
        public T GetPooledElement<T>() where T : VisualElement, new()
        {
            var elementType = typeof(T);
            
            if (_elementPools.TryGetValue(elementType, out var pool) && pool.Count > 0)
            {
                var element = (T)pool.Dequeue();
                _activeElements.Add(element);
                _elementLastUsed[element] = Time.time;
                
                // Reset element state
                ResetElementState(element);
                
                return element;
            }
            
            // Create new element if pool is empty
            var newElement = new T();
            _activeElements.Add(newElement);
            _elementLastUsed[newElement] = Time.time;
            
            OnOptimizationApplied?.Invoke($"Created new {elementType.Name}");
            return newElement;
        }
        
        /// <summary>
        /// Return an element to the object pool
        /// </summary>
        public void ReturnToPool(VisualElement element)
        {
            if (element == null)
                return;
            
            var elementType = element.GetType();
            
            // Remove from active set
            _activeElements.Remove(element);
            
            // Return to pool if not at capacity
            if (_elementPools.TryGetValue(elementType, out var pool))
            {
                if (pool.Count < _maxPoolSize)
                {
                    ResetElementState(element);
                    element.RemoveFromHierarchy();
                    pool.Enqueue(element);
                }
                else if (!_allowPoolExpansion)
                {
                    // Destroy element if pool is full and expansion is disabled
                    DestroyElement(element);
                }
            }
            else
            {
                // Create new pool for this type
                CreateElementPool(elementType, 1);
                _elementPools[elementType].Enqueue(element);
            }
        }
        
        /// <summary>
        /// Update LOD (Level of Detail) for elements based on distance and visibility
        /// </summary>
        public void UpdateElementLOD(VisualElement element, float distance)
        {
            if (!_enableLODSystem || element == null)
                return;
            
            LODLevel newLOD;
            
            if (distance <= _lodNearDistance)
                newLOD = LODLevel.High;
            else if (distance <= _lodFarDistance)
                newLOD = LODLevel.Medium;
            else
                newLOD = LODLevel.Low;
            
            if (!_elementLOD.TryGetValue(element, out var currentLOD) || currentLOD != newLOD)
            {
                _elementLOD[element] = newLOD;
                ApplyLOD(element, newLOD);
            }
        }
        
        /// <summary>
        /// Add element to batch update queue
        /// </summary>
        public void QueueForUpdate(IUpdateable updateable)
        {
            if (_enableBatchProcessing && updateable != null)
            {
                _updateQueue.Enqueue(updateable);
            }
        }
        
        /// <summary>
        /// Add animation to batch processing queue
        /// </summary>
        public void QueueAnimation(IAnimatable animatable)
        {
            if (_enableBatchProcessing && animatable != null)
            {
                _animationQueue.Enqueue(animatable);
            }
        }
        
        /// <summary>
        /// Optimize texture memory usage
        /// </summary>
        public void OptimizeTextureMemory()
        {
            if (!_enableTextureStreaming)
                return;
            
            // Unload unused textures
            var texturesToUnload = new List<Texture2D>();
            
            foreach (var texture in _loadedTextures)
            {
                if (texture != null)
                {
                    texturesToUnload.Add(texture);
                }
            }
            
            foreach (var texture in texturesToUnload)
            {
                Resources.UnloadAsset(texture);
                _loadedTextures.Remove(texture);
            }
            
            if (texturesToUnload.Count > 0)
            {
                OnOptimizationApplied?.Invoke($"Unloaded {texturesToUnload.Count} textures");
            }
        }
        
        /// <summary>
        /// Force garbage collection if memory usage is high
        /// </summary>
        public void ForceGarbageCollection()
        {
            if (!_enableGarbageCollection)
                return;
            
            var beforeMemory = Profiler.GetTotalAllocatedMemory();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemory = Profiler.GetTotalAllocatedMemory();
            var freedMemory = beforeMemory - afterMemory;
            
            _lastGCTime = Time.time;
            
            OnOptimizationApplied?.Invoke($"GC freed {freedMemory / (1024 * 1024)} MB");
        }
        
        private void CreateElementPool<T>(int initialSize) where T : VisualElement, new()
        {
            CreateElementPool(typeof(T), initialSize);
        }
        
        private void CreateElementPool(Type elementType, int initialSize)
        {
            if (_elementPools.ContainsKey(elementType))
                return;
            
            var pool = new Queue<VisualElement>();
            
            for (int i = 0; i < initialSize; i++)
            {
                var element = (VisualElement)Activator.CreateInstance(elementType);
                ResetElementState(element);
                pool.Enqueue(element);
            }
            
            _elementPools[elementType] = pool;
        }
        
        private void ResetElementState(VisualElement element)
        {
            if (element == null)
                return;
            
            // Reset common properties
            element.style.opacity = 1f;
            element.style.display = DisplayStyle.Flex;
            element.style.visibility = Visibility.Visible;
            element.style.scale = StyleKeyword.Null;
            element.style.rotate = StyleKeyword.Null;
            element.style.translate = StyleKeyword.Null;
            element.style.backgroundColor = StyleKeyword.Null;
            
            // Clear event callbacks
            element.UnregisterCallback<ClickEvent>(null);
            element.UnregisterCallback<MouseEnterEvent>(null);
            element.UnregisterCallback<MouseLeaveEvent>(null);
            
            // Reset text content
            if (element is Label label)
            {
                label.text = string.Empty;
            }
            
            // Clear user data
            element.userData = null;
            
            // Clear class list
            element.ClearClassList();
        }
        
        private void DestroyElement(VisualElement element)
        {
            if (element == null)
                return;
            
            element.RemoveFromHierarchy();
            _activeElements.Remove(element);
            _elementLastUsed.Remove(element);
            _elementLOD.Remove(element);
        }
        
        private void ApplyLOD(VisualElement element, LODLevel lod)
        {
            switch (lod)
            {
                case LODLevel.High:
                    // Full quality - no changes needed
                    element.style.opacity = 1f;
                    EnableAnimations(element, true);
                    EnableDetailedVisuals(element, true);
                    break;
                
                case LODLevel.Medium:
                    // Reduced quality
                    element.style.opacity = 0.9f;
                    EnableAnimations(element, false);
                    EnableDetailedVisuals(element, true);
                    break;
                
                case LODLevel.Low:
                    // Minimal quality
                    element.style.opacity = 0.7f;
                    EnableAnimations(element, false);
                    EnableDetailedVisuals(element, false);
                    break;
            }
        }
        
        private void EnableAnimations(VisualElement element, bool enabled)
        {
            if (!_enableAnimationLOD)
                return;
            
            // Disable/enable animations based on LOD
            if (enabled)
            {
                element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(0.3f) });
            }
            else
            {
                element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(0f) });
            }
        }
        
        private void EnableDetailedVisuals(VisualElement element, bool enabled)
        {
            if (!_enableDetailCulling)
                return;
            
            // Show/hide detailed visual elements
            var detailElements = element.Query(className: "detail-element").ToList();
            
            foreach (var detail in detailElements)
            {
                detail.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private void ProcessBatchUpdates()
        {
            Profiler.BeginSample("MenuSystem.ProcessBatchUpdates");
            
            int processedUpdates = 0;
            int processedAnimations = 0;
            
            // Process updates
            while (_updateQueue.Count > 0 && processedUpdates < _maxUpdatesPerFrame)
            {
                var updateable = _updateQueue.Dequeue();
                if (updateable != null)
                {
                    updateable.UpdateElement();
                    processedUpdates++;
                }
            }
            
            // Process animations
            while (_animationQueue.Count > 0 && processedAnimations < _maxAnimationsPerFrame)
            {
                var animatable = _animationQueue.Dequeue();
                if (animatable != null)
                {
                    animatable.UpdateAnimation();
                    processedAnimations++;
                }
            }
            
            Profiler.EndSample();
            
            // Update metrics
            _metrics.ProcessedUpdatesThisFrame = processedUpdates;
            _metrics.ProcessedAnimationsThisFrame = processedAnimations;
        }
        
        private void UpdateLODSystem()
        {
            Profiler.BeginSample("MenuSystem.UpdateLODSystem");
            
            var cameraPosition = Camera.main?.transform.position ?? Vector3.zero;
            
            foreach (var element in _activeElements.ToList()) // ToList to avoid modification during enumeration
            {
                if (element == null || element.parent == null)
                    continue;
                
                // Calculate distance from camera (simplified)
                var elementRect = element.layout;
                var elementWorldPos = new Vector3(elementRect.center.x, elementRect.center.y, 0);
                var distance = Vector3.Distance(cameraPosition, elementWorldPos);
                
                UpdateElementLOD(element, distance);
            }
            
            Profiler.EndSample();
        }
        
        private void UpdatePerformanceMetrics()
        {
            _metrics.FPS = 1.0f / Time.deltaTime;
            _metrics.FrameTime = Time.deltaTime * 1000f; // Convert to milliseconds
            _metrics.ActiveElements = _activeElements.Count;
            _metrics.PooledElements = _elementPools.Values.Sum(pool => pool.Count);
            _metrics.UpdateQueueSize = _updateQueue.Count;
            _metrics.AnimationQueueSize = _animationQueue.Count;
            _metrics.AllocatedMemory = Profiler.GetTotalAllocatedMemory() / (1024 * 1024); // Convert to MB
            
            // Update profiling data
            if (_enableProfiling && Time.time - _lastProfilingUpdate >= _profilingUpdateInterval)
            {
                _lastProfilingUpdate = Time.time;
                OnPerformanceUpdated?.Invoke(_metrics);
                
                if (_showPerformanceStats)
                {
                    LogPerformanceStats();
                }
            }
        }
        
        private void CheckMemoryOptimization()
        {
            // Garbage collection check
            if (_enableGarbageCollection && Time.time - _lastGCTime >= _gcInterval)
            {
                if (_metrics.AllocatedMemory > 100) // More than 100 MB
                {
                    ForceGarbageCollection();
                }
            }
            
            // Texture optimization check
            if (_enableTextureStreaming && _loadedTextures.Count > 50)
            {
                OptimizeTextureMemory();
            }
            
            // Asset cache cleanup
            if (_enableAssetUnloading)
            {
                CleanupAssetCache();
            }
        }
        
        private void CleanupAssetCache()
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _assetCache)
            {
                if (kvp.Value == null || !kvp.Value.IsAlive)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _assetCache.Remove(key);
            }
            
            if (keysToRemove.Count > 0)
            {
                OnOptimizationApplied?.Invoke($"Cleaned up {keysToRemove.Count} asset references");
            }
        }
        
        private void LogPerformanceStats()
        {
            Debug.Log($"[MenuPerformanceOptimization] FPS: {_metrics.FPS:F1}, " +
                     $"Frame Time: {_metrics.FrameTime:F2}ms, " +
                     $"Active Elements: {_metrics.ActiveElements}, " +
                     $"Memory: {_metrics.AllocatedMemory:F1}MB");
        }
        
        private System.Collections.IEnumerator OptimizationUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_poolCleanupInterval);
                
                // Cleanup unused pooled elements
                CleanupObjectPools();
                
                // Cleanup expired LOD data
                CleanupLODData();
            }
        }
        
        private void CleanupObjectPools()
        {
            var elementsToRemove = new List<VisualElement>();
            var cutoffTime = Time.time - _poolCleanupInterval;
            
            foreach (var kvp in _elementLastUsed.ToList())
            {
                if (kvp.Value < cutoffTime && !_activeElements.Contains(kvp.Key))
                {
                    elementsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var element in elementsToRemove)
            {
                DestroyElement(element);
            }
            
            if (elementsToRemove.Count > 0)
            {
                OnOptimizationApplied?.Invoke($"Cleaned up {elementsToRemove.Count} unused elements");
            }
        }
        
        private void CleanupLODData()
        {
            var elementsToRemove = new List<VisualElement>();
            
            foreach (var element in _elementLOD.Keys.ToList())
            {
                if (element == null || element.parent == null)
                {
                    elementsToRemove.Add(element);
                }
            }
            
            foreach (var element in elementsToRemove)
            {
                _elementLOD.Remove(element);
            }
        }
        
        // Event handlers
        private void OnMenuOpened(string menuId)
        {
            // Clear update queues when menu opens
            _updateQueue.Clear();
            _animationQueue.Clear();
        }
        
        private void OnMenuClosed(string menuId)
        {
            // Return elements to pools when menu closes
            var elementsToReturn = _activeElements.Where(e => e.parent == null).ToList();
            
            foreach (var element in elementsToReturn)
            {
                ReturnToPool(element);
            }
        }
        
        private void OnActionExecuted(string actionId, MenuAction action)
        {
            // Track action execution for performance analysis
            _metrics.ActionsExecutedThisFrame++;
        }
        
        private void OnDestroy()
        {
            // Cleanup all resources
            foreach (var pool in _elementPools.Values)
            {
                while (pool.Count > 0)
                {
                    var element = pool.Dequeue();
                    DestroyElement(element);
                }
            }
            
            _elementPools.Clear();
            _activeElements.Clear();
            _elementLastUsed.Clear();
            _elementLOD.Clear();
        }
        
        // Public API
        public PerformanceMetrics GetPerformanceMetrics() => _metrics;
        public int GetActiveElementCount() => _activeElements.Count;
        public int GetPooledElementCount() => _elementPools.Values.Sum(pool => pool.Count);
        public void SetOptimizationsEnabled(bool enabled) => _enableOptimizations = enabled;
        public void SetLODEnabled(bool enabled) => _enableLODSystem = enabled;
        public void SetBatchProcessingEnabled(bool enabled) => _enableBatchProcessing = enabled;
    }
    
    // Supporting interfaces and structures
    public interface IUpdateable
    {
        void UpdateElement();
    }
    
    public interface IAnimatable
    {
        void UpdateAnimation();
    }
    
    public enum LODLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
    
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float FPS;
        public float FrameTime; // in milliseconds
        public int ActiveElements;
        public int PooledElements;
        public int UpdateQueueSize;
        public int AnimationQueueSize;
        public long AllocatedMemory; // in MB
        public int ProcessedUpdatesThisFrame;
        public int ProcessedAnimationsThisFrame;
        public int ActionsExecutedThisFrame;
        
        public void Reset()
        {
            ProcessedUpdatesThisFrame = 0;
            ProcessedAnimationsThisFrame = 0;
            ActionsExecutedThisFrame = 0;
        }
    }
}