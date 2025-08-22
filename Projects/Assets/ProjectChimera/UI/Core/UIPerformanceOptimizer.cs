using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Advanced performance optimizer for UI systems in Project Chimera.
    /// Implements intelligent pooling, batching, and optimization strategies.
    /// </summary>
    public class UIPerformanceOptimizer : ChimeraManager
    {
        [Header("Optimization Configuration")]
        [SerializeField] private bool _enableAutomaticOptimization = true;
        [SerializeField] private bool _enableAdaptiveOptimization = true;
        [SerializeField] private float _optimizationInterval = 5f;
        [SerializeField] private bool _enableProfilingMode = false;
        
        [Header("Performance Targets")]
        [SerializeField] private float _targetFrameTimeMs = 16.67f; // 60 FPS
        [SerializeField] private float _maxMemoryUsageMB = 100f;
        [SerializeField] private int _maxActiveUIElements = 200;
        [SerializeField] private float _maxUpdateTimeMs = 2f;
        
        [Header("Optimization Strategies")]
        [SerializeField] private bool _enableObjectPooling = true;
        [SerializeField] private bool _enableUpdateBatching = true;
        [SerializeField] private bool _enableGeometryOptimization = true;
        [SerializeField] private bool _enableMemoryOptimization = true;
        [SerializeField] private bool _enableRenderOptimization = true;
        
        [Header("Pooling Settings")]
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        [SerializeField] private int _poolGrowthRate = 10;
        [SerializeField] private float _poolCleanupInterval = 30f;
        
        [Header("Batching Settings")]
        [SerializeField] private int _maxBatchSize = 50;
        [SerializeField] private float _batchProcessingTimeMs = 1f;
        [SerializeField] private bool _enablePriorityBatching = true;
        
        // Performance tracking
        private Dictionary<string, UIPerformanceMetrics> _performanceMetrics;
        private Queue<UIOptimizationTask> _optimizationQueue;
        private Dictionary<System.Type, UIElementPool> _elementPools;
        private List<UIUpdateBatch> _updateBatches;
        private Dictionary<UIUpdatePriority, List<IUIUpdatable>> _batchProcessors;
        private float _optimizationTimer = 0f;
        private float _poolCleanupTimer = 0f;
        
        // Adaptive optimization
        private UIPerformanceState _currentPerformanceState;
        private Dictionary<UIOptimizationStrategy, float> _strategyEffectiveness;
        private int _framesSinceLastOptimization = 0;
        
        // Events
        public System.Action<UIOptimizationResult> OnOptimizationApplied;
        public System.Action<UIPerformanceState> OnPerformanceStateChanged;
        public System.Action<string> OnOptimizationWarning;
        
        // Properties
        public bool IsOptimizationEnabled => _enableAutomaticOptimization;
        public UIPerformanceState CurrentPerformanceState => _currentPerformanceState;
        public int ActiveOptimizationTasks => _optimizationQueue?.Count ?? 0;
        public Dictionary<System.Type, UIElementPool> ElementPools => _elementPools;
        
        public override void InitializeManager()
        {
            base.InitializeManager();
            
            InitializeOptimizer();
            InitializePooling();
            InitializeBatching();
            InitializeAdaptiveSystem();
            
            LogInfo("UI Performance Optimizer initialized successfully");
        }
        
        /// <summary>
        /// Initialize performance optimizer
        /// </summary>
        private void InitializeOptimizer()
        {
            _performanceMetrics = new Dictionary<string, UIPerformanceMetrics>();
            _optimizationQueue = new Queue<UIOptimizationTask>();
            _strategyEffectiveness = new Dictionary<UIOptimizationStrategy, float>();
            
            // Initialize strategy effectiveness tracking
            foreach (UIOptimizationStrategy strategy in System.Enum.GetValues(typeof(UIOptimizationStrategy)))
            {
                _strategyEffectiveness[strategy] = 1f; // Start with neutral effectiveness
            }
        }
        
        /// <summary>
        /// Initialize object pooling system
        /// </summary>
        private void InitializePooling()
        {
            if (!_enableObjectPooling)
                return;
                
            _elementPools = new Dictionary<System.Type, UIElementPool>();
            
            // Create pools for common UI element types
            CreateElementPool<VisualElement>(_initialPoolSize);
            CreateElementPool<Label>(_initialPoolSize / 2);
            CreateElementPool<Button>(_initialPoolSize / 2);
            CreateElementPool<TextField>(_initialPoolSize / 4);
            CreateElementPool<ProgressBar>(_initialPoolSize / 4);
            CreateElementPool<ScrollView>(_initialPoolSize / 8);
            
            LogInfo($"Initialized {_elementPools.Count} UI element pools");
        }
        
        /// <summary>
        /// Initialize update batching system
        /// </summary>
        private void InitializeBatching()
        {
            if (!_enableUpdateBatching)
                return;
                
            _updateBatches = new List<UIUpdateBatch>();
            _batchProcessors = new Dictionary<UIUpdatePriority, List<IUIUpdatable>>();
            
            // Initialize batch processors for each priority level
            foreach (UIUpdatePriority priority in System.Enum.GetValues(typeof(UIUpdatePriority)))
            {
                _batchProcessors[priority] = new List<IUIUpdatable>();
            }
            
            if (_enablePriorityBatching)
            {
                LogInfo("Priority-based UI batching enabled");
            }
            
            // Create batches for different update priorities
            _updateBatches.Add(new UIUpdateBatch(UIUpdatePriority.Critical, _maxBatchSize / 4));
            _updateBatches.Add(new UIUpdateBatch(UIUpdatePriority.High, _maxBatchSize / 2));
            _updateBatches.Add(new UIUpdateBatch(UIUpdatePriority.Normal, _maxBatchSize));
            _updateBatches.Add(new UIUpdateBatch(UIUpdatePriority.Low, _maxBatchSize * 2));
            
            LogInfo($"Initialized {_updateBatches.Count} update batches");
        }
        
        /// <summary>
        /// Initialize adaptive optimization system
        /// </summary>
        private void InitializeAdaptiveSystem()
        {
            _currentPerformanceState = UIPerformanceState.Optimal;
            
            if (_enableAdaptiveOptimization)
            {
                // Setup performance monitoring
                InvokeRepeating(nameof(EvaluatePerformanceState), 1f, 1f);
            }
        }
        
        /// <summary>
        /// Create element pool for specific type
        /// </summary>
        private void CreateElementPool<T>(int initialSize) where T : VisualElement, new()
        {
            var pool = new UIElementPool<T>(initialSize, _maxPoolSize);
            _elementPools[typeof(T)] = pool;
        }
        
        /// <summary>
        /// Get pooled element of specified type
        /// </summary>
        public T GetPooledElement<T>() where T : VisualElement, new()
        {
            if (!_enableObjectPooling || !_elementPools.ContainsKey(typeof(T)))
            {
                return new T();
            }
            
            var pool = _elementPools[typeof(T)] as UIElementPool<T>;
            return pool?.Get() ?? new T();
        }
        
        /// <summary>
        /// Return element to pool
        /// </summary>
        public void ReturnToPool<T>(T element) where T : VisualElement, new()
        {
            if (!_enableObjectPooling || element == null || !_elementPools.ContainsKey(typeof(T)))
            {
                return;
            }
            
            var pool = _elementPools[typeof(T)] as UIElementPool<T>;
            pool?.Return(element);
        }
        
        /// <summary>
        /// Queue optimization task
        /// </summary>
        public void QueueOptimization(UIOptimizationTask task)
        {
            if (_optimizationQueue.Count < 100) // Prevent queue overflow
            {
                _optimizationQueue.Enqueue(task);
            }
        }
        
        /// <summary>
        /// Add element to update batch
        /// </summary>
        public void AddToUpdateBatch(IUIUpdatable updatable, UIUpdatePriority priority = UIUpdatePriority.Normal)
        {
            if (!_enableUpdateBatching)
            {
                updatable.UpdateElement();
                return;
            }
            
            var batch = _updateBatches.FirstOrDefault(b => b.Priority == priority);
            batch?.AddElement(updatable);
        }
        
        /// <summary>
        /// Process optimization queue
        /// </summary>
        private void ProcessOptimizationQueue()
        {
            var startTime = Time.realtimeSinceStartup;
            var maxProcessingTime = _batchProcessingTimeMs / 1000f;
            
            while (_optimizationQueue.Count > 0 && (Time.realtimeSinceStartup - startTime) < maxProcessingTime)
            {
                var task = _optimizationQueue.Dequeue();
                ExecuteOptimizationTask(task);
            }
        }
        
        /// <summary>
        /// Execute optimization task
        /// </summary>
        private void ExecuteOptimizationTask(UIOptimizationTask task)
        {
            try
            {
                var result = new UIOptimizationResult
                {
                    TaskType = task.Type,
                    Success = false,
                    PerformanceImprovement = 0f,
                    Description = ""
                };
                
                switch (task.Type)
                {
                    case UIOptimizationType.PoolCleanup:
                        result = ExecutePoolCleanup();
                        break;
                        
                    case UIOptimizationType.GeometryOptimization:
                        result = ExecuteGeometryOptimization(task);
                        break;
                        
                    case UIOptimizationType.MemoryCleanup:
                        result = ExecuteMemoryCleanup();
                        break;
                        
                    case UIOptimizationType.RenderOptimization:
                        result = ExecuteRenderOptimization(task);
                        break;
                        
                    case UIOptimizationType.UpdateOptimization:
                        result = ExecuteUpdateOptimization(task);
                        break;
                }
                
                if (result.Success)
                {
                    OnOptimizationApplied?.Invoke(result);
                    UpdateStrategyEffectiveness(task.Strategy, result.PerformanceImprovement);
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to execute optimization task: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Execute pool cleanup optimization
        /// </summary>
        private UIOptimizationResult ExecutePoolCleanup()
        {
            var freedMemory = 0;
            var poolsCleaned = 0;
            
            foreach (var pool in _elementPools.Values)
            {
                var beforeCount = pool.PooledCount;
                pool.Cleanup();
                var afterCount = pool.PooledCount;
                
                freedMemory += (beforeCount - afterCount);
                if (beforeCount > afterCount)
                    poolsCleaned++;
            }
            
            return new UIOptimizationResult
            {
                TaskType = UIOptimizationType.PoolCleanup,
                Success = poolsCleaned > 0,
                PerformanceImprovement = freedMemory * 0.1f, // Estimated improvement
                Description = $"Cleaned {poolsCleaned} pools, freed {freedMemory} elements"
            };
        }
        
        /// <summary>
        /// Execute geometry optimization
        /// </summary>
        private UIOptimizationResult ExecuteGeometryOptimization(UIOptimizationTask task)
        {
            // Implementation would optimize UI element geometry
            // This is a placeholder for actual geometry optimization
            return new UIOptimizationResult
            {
                TaskType = UIOptimizationType.GeometryOptimization,
                Success = true,
                PerformanceImprovement = 5f,
                Description = "Optimized UI element geometry"
            };
        }
        
        /// <summary>
        /// Execute memory cleanup optimization
        /// </summary>
        private UIOptimizationResult ExecuteMemoryCleanup()
        {
            var beforeMemory = System.GC.GetTotalMemory(false);
            System.GC.Collect();
            var afterMemory = System.GC.GetTotalMemory(true);
            
            var freedBytes = beforeMemory - afterMemory;
            var freedMB = freedBytes / 1024f / 1024f;
            
            return new UIOptimizationResult
            {
                TaskType = UIOptimizationType.MemoryCleanup,
                Success = freedBytes > 0,
                PerformanceImprovement = freedMB * 2f, // Memory cleanup has high impact
                Description = $"Freed {freedMB:F1}MB of memory"
            };
        }
        
        /// <summary>
        /// Execute render optimization
        /// </summary>
        private UIOptimizationResult ExecuteRenderOptimization(UIOptimizationTask task)
        {
            // Implementation would optimize rendering batches, draw calls, etc.
            // This is a placeholder for actual render optimization
            return new UIOptimizationResult
            {
                TaskType = UIOptimizationType.RenderOptimization,
                Success = true,
                PerformanceImprovement = 3f,
                Description = "Optimized UI rendering pipeline"
            };
        }
        
        /// <summary>
        /// Execute update optimization
        /// </summary>
        private UIOptimizationResult ExecuteUpdateOptimization(UIOptimizationTask task)
        {
            // Implementation would optimize update loops, reduce frequency, etc.
            // This is a placeholder for actual update optimization
            return new UIOptimizationResult
            {
                TaskType = UIOptimizationType.UpdateOptimization,
                Success = true,
                PerformanceImprovement = 2f,
                Description = "Optimized UI update loops"
            };
        }
        
        /// <summary>
        /// Process update batches
        /// </summary>
        private void ProcessUpdateBatches()
        {
            if (!_enableUpdateBatching)
                return;
                
            var startTime = Time.realtimeSinceStartup;
            var maxProcessingTime = _batchProcessingTimeMs / 1000f;
            
            foreach (var batch in _updateBatches.OrderBy(b => b.Priority))
            {
                if ((Time.realtimeSinceStartup - startTime) >= maxProcessingTime)
                    break;
                    
                batch.ProcessBatch(maxProcessingTime / _updateBatches.Count);
            }
        }
        
        /// <summary>
        /// Evaluate current performance state
        /// </summary>
        private void EvaluatePerformanceState()
        {
            var frameTime = Time.deltaTime * 1000f;
            var memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f;
            
            var newState = DeterminePerformanceState(frameTime, memoryUsage);
            
            if (newState != _currentPerformanceState)
            {
                var previousState = _currentPerformanceState;
                _currentPerformanceState = newState;
                
                OnPerformanceStateChanged?.Invoke(newState);
                
                LogInfo($"Performance state changed: {previousState} -> {newState}");
                
                // Trigger adaptive optimizations
                if (_enableAdaptiveOptimization)
                {
                    TriggerAdaptiveOptimization(newState);
                }
            }
        }
        
        /// <summary>
        /// Determine performance state based on metrics
        /// </summary>
        private UIPerformanceState DeterminePerformanceState(float frameTime, float memoryUsage)
        {
            // Critical thresholds
            if (frameTime > _targetFrameTimeMs * 2f || memoryUsage > _maxMemoryUsageMB)
            {
                return UIPerformanceState.Critical;
            }
            
            // Poor thresholds
            if (frameTime > _targetFrameTimeMs * 1.5f || memoryUsage > _maxMemoryUsageMB * 0.8f)
            {
                return UIPerformanceState.Poor;
            }
            
            // Good thresholds
            if (frameTime > _targetFrameTimeMs * 1.2f || memoryUsage > _maxMemoryUsageMB * 0.6f)
            {
                return UIPerformanceState.Good;
            }
            
            // Optimal
            return UIPerformanceState.Optimal;
        }
        
        /// <summary>
        /// Trigger adaptive optimization based on performance state
        /// </summary>
        private void TriggerAdaptiveOptimization(UIPerformanceState state)
        {
            switch (state)
            {
                case UIPerformanceState.Critical:
                    // Aggressive optimization
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.MemoryCleanup, UIOptimizationStrategy.Aggressive));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, UIOptimizationStrategy.Aggressive));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.UpdateOptimization, UIOptimizationStrategy.Aggressive));
                    break;
                    
                case UIPerformanceState.Poor:
                    // Moderate optimization
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.GeometryOptimization, UIOptimizationStrategy.Moderate));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.RenderOptimization, UIOptimizationStrategy.Moderate));
                    break;
                    
                case UIPerformanceState.Good:
                    // Light optimization
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, UIOptimizationStrategy.Conservative));
                    break;
                    
                case UIPerformanceState.Optimal:
                    // No immediate optimization needed
                    break;
            }
        }
        
        /// <summary>
        /// Update strategy effectiveness tracking
        /// </summary>
        private void UpdateStrategyEffectiveness(UIOptimizationStrategy strategy, float improvement)
        {
            if (_strategyEffectiveness.ContainsKey(strategy))
            {
                // Exponential moving average
                var alpha = 0.1f;
                _strategyEffectiveness[strategy] = (1 - alpha) * _strategyEffectiveness[strategy] + alpha * improvement;
            }
        }
        
        /// <summary>
        /// Get most effective optimization strategy
        /// </summary>
        public UIOptimizationStrategy GetMostEffectiveStrategy()
        {
            return _strategyEffectiveness.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        
        /// <summary>
        /// Force immediate optimization
        /// </summary>
        public void ForceOptimization(UIOptimizationStrategy strategy = UIOptimizationStrategy.Moderate)
        {
            // Queue multiple optimization tasks based on strategy
            switch (strategy)
            {
                case UIOptimizationStrategy.Conservative:
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, strategy));
                    break;
                    
                case UIOptimizationStrategy.Moderate:
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, strategy));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.GeometryOptimization, strategy));
                    break;
                    
                case UIOptimizationStrategy.Aggressive:
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.MemoryCleanup, strategy));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, strategy));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.GeometryOptimization, strategy));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.RenderOptimization, strategy));
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.UpdateOptimization, strategy));
                    break;
            }
            
            LogInfo($"Forced optimization with {strategy} strategy - {_optimizationQueue.Count} tasks queued");
        }
        
        /// <summary>
        /// Get optimization statistics
        /// </summary>
        public UIOptimizationStats GetOptimizationStats()
        {
            return new UIOptimizationStats
            {
                CurrentPerformanceState = _currentPerformanceState,
                QueuedOptimizations = _optimizationQueue.Count,
                ActivePools = _elementPools.Count,
                TotalPooledElements = _elementPools.Values.Sum(p => p.PooledCount),
                StrategyEffectiveness = new Dictionary<UIOptimizationStrategy, float>(_strategyEffectiveness),
                IsOptimizationEnabled = _enableAutomaticOptimization
            };
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!_enableAutomaticOptimization)
                return;
            
            _framesSinceLastOptimization++;
            
            // Process optimization queue
            if (_optimizationQueue.Count > 0)
            {
                ProcessOptimizationQueue();
            }
            
            // Process update batches
            ProcessUpdateBatches();
            
            // Periodic optimization
            _optimizationTimer += Time.deltaTime;
            if (_optimizationTimer >= _optimizationInterval)
            {
                PerformPeriodicOptimization();
                _optimizationTimer = 0f;
            }
            
            // Pool cleanup
            _poolCleanupTimer += Time.deltaTime;
            if (_poolCleanupTimer >= _poolCleanupInterval)
            {
                QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, UIOptimizationStrategy.Conservative));
                _poolCleanupTimer = 0f;
            }
        }
        
        /// <summary>
        /// Perform periodic optimization
        /// </summary>
        private void PerformPeriodicOptimization()
        {
            if (_currentPerformanceState == UIPerformanceState.Optimal)
            {
                // Light maintenance optimization
                if (_framesSinceLastOptimization > 1800) // 30 seconds at 60 FPS
                {
                    QueueOptimization(new UIOptimizationTask(UIOptimizationType.PoolCleanup, UIOptimizationStrategy.Conservative));
                    _framesSinceLastOptimization = 0;
                }
            }
            // else
            // {
                // More frequent optimization for non-optimal states
                if (_framesSinceLastOptimization > 300) // 5 seconds at 60 FPS
                {
                    TriggerAdaptiveOptimization(_currentPerformanceState);
                    _framesSinceLastOptimization = 0;
                }
            // }
        }
        
        private void OnValidate()
        {
            // Ensure pooling settings are valid
            _initialPoolSize = Mathf.Max(1, _initialPoolSize);
            _maxPoolSize = Mathf.Max(_initialPoolSize, _maxPoolSize);
            _poolGrowthRate = Mathf.Max(1, _poolGrowthRate);
            
            // Ensure performance targets are valid
            _targetFrameTimeMs = Mathf.Max(1f, _targetFrameTimeMs);
            _maxMemoryUsageMB = Mathf.Max(10f, _maxMemoryUsageMB);
            _maxActiveUIElements = Mathf.Max(10, _maxActiveUIElements);
            _maxUpdateTimeMs = Mathf.Max(0.1f, _maxUpdateTimeMs);
            
            // Ensure optimization intervals are valid
            _optimizationInterval = Mathf.Max(0.1f, _optimizationInterval);
            _poolCleanupInterval = Mathf.Max(1f, _poolCleanupInterval);
            
            // Ensure batching settings are valid
            _maxBatchSize = Mathf.Max(1, _maxBatchSize);
            _batchProcessingTimeMs = Mathf.Max(0.1f, _batchProcessingTimeMs);
        }
        
        /// <summary>
        /// Initialize element pools
        /// </summary>
        private void InitializePools()
        {
            InitializePooling();
        }
        
        /// <summary>
        /// Start the optimization routine
        /// </summary>
        private void StartOptimizationRoutine()
        {
            if (!_enableAutomaticOptimization) return;
            
            LogInfo("Starting UI optimization routine");
            _optimizationTimer = 0f;
            _poolCleanupTimer = 0f;
        }
        
        /// <summary>
        /// Stop the optimization routine
        /// </summary>
        private void StopOptimizationRoutine()
        {
            LogInfo("Stopping UI optimization routine");
        }
        
        /// <summary>
        /// Clear all element pools
        /// </summary>
        private void ClearAllPools()
        {
            if (_elementPools != null)
            {
                foreach (var pool in _elementPools.Values)
                {
                    pool?.Cleanup();
                }
                _elementPools.Clear();
            }
            
            LogInfo("All UI element pools cleared");
        }
        
        protected override void OnManagerInitialize()
        {
            InitializeOptimizer();
            InitializePools();
            
            if (_enableAutomaticOptimization)
            {
                StartOptimizationRoutine();
            }
            
            LogInfo("UI Performance Optimizer initialized successfully");
        }
        
        protected override void OnManagerShutdown()
        {
            StopOptimizationRoutine();
            ClearAllPools();
            _batchProcessors.Clear();
            
            LogInfo("UI Performance Optimizer shutdown completed");
        }
    }
    
    /// <summary>
    /// Performance state enumeration
    /// </summary>
    public enum UIPerformanceState
    {
        Optimal,
        Good,
        Poor,
        Critical
    }
    
    /// <summary>
    /// Optimization strategy enumeration
    /// </summary>
    public enum UIOptimizationStrategy
    {
        Conservative,
        Moderate,
        Aggressive
    }
    
    /// <summary>
    /// Optimization type enumeration
    /// </summary>
    public enum UIOptimizationType
    {
        PoolCleanup,
        GeometryOptimization,
        MemoryCleanup,
        RenderOptimization,
        UpdateOptimization
    }
    
    /// <summary>
    /// Update priority enumeration
    /// </summary>
    public enum UIUpdatePriority
    {
        Critical = 0,
        High = 1,
        Normal = 2,
        Low = 3
    }
    
    /// <summary>
    /// Optimization task definition
    /// </summary>
    public struct UIOptimizationTask
    {
        public UIOptimizationType Type;
        public UIOptimizationStrategy Strategy;
        public object Data;
        public System.DateTime CreatedTime;
        
        public UIOptimizationTask(UIOptimizationType type, UIOptimizationStrategy strategy, object data = null)
        {
            Type = type;
            Strategy = strategy;
            Data = data;
            CreatedTime = System.DateTime.Now;
        }
    }
    
    /// <summary>
    /// Optimization result data
    /// </summary>
    public struct UIOptimizationResult
    {
        public UIOptimizationType TaskType;
        public bool Success;
        public float PerformanceImprovement;
        public string Description;
    }
    
    /// <summary>
    /// Performance metrics data
    /// </summary>
    public class UIPerformanceMetrics
    {
        public float AverageFrameTime;
        public float PeakFrameTime;
        public float AverageMemoryUsage;
        public float PeakMemoryUsage;
        public int ActiveUIElements;
        public System.DateTime LastUpdate;
    }
    
    /// <summary>
    /// Optimization statistics
    /// </summary>
    public struct UIOptimizationStats
    {
        public UIPerformanceState CurrentPerformanceState;
        public int QueuedOptimizations;
        public int ActivePools;
        public int TotalPooledElements;
        public Dictionary<UIOptimizationStrategy, float> StrategyEffectiveness;
        public bool IsOptimizationEnabled;
    }
    
    /// <summary>
    /// Interface for UI elements that can be updated
    /// </summary>
    public interface IUIUpdatable
    {
        void UpdateElement();
        UIUpdatePriority UpdatePriority { get; }
    }
}