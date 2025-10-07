using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Equipment.Degradation.Cache;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// REFACTORED: Estimate Cache Manager - Coordinator using SRP-compliant components
    /// Single Responsibility: Coordinating cache storage, optimization, and validation
    /// Uses composition with CacheStorageManager, CacheOptimizationManager, and CacheValidationManager
    /// Reduced from 1181 lines to maintain SRP compliance
    /// </summary>
    public class EstimateCacheManager : MonoBehaviour, ITickable
    {
        [Header("Cache Configuration")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 1000;
        [SerializeField] private float _defaultCacheExpiration = 3600f; // 1 hour

        [Header("Optimization Settings")]
        [SerializeField] private bool _enableCacheOptimization = true;
        [SerializeField] private float _optimizationInterval = 600f; // 10 minutes
        [SerializeField] private float _memoryThreshold = 0.8f; // 80% memory usage
        [SerializeField] private CacheEvictionPolicy _evictionPolicy = CacheEvictionPolicy.LRU;

        [Header("Validation Settings")]
        [SerializeField] private bool _enableCacheValidation = true;
        [SerializeField] private float _validationInterval = 1800f; // 30 minutes
        [SerializeField] private float _accuracyThreshold = 0.85f; // 85% accuracy threshold
        [SerializeField] private int _validationSampleSize = 20;

        // Composition: Delegate responsibilities to focused components
        private CacheStorageManager _storageManager;
        private CacheOptimizationManager _optimizationManager;
        private CacheValidationManager _validationManager;

        // Coordinator state
        private bool _isInitialized = false;
        private float _lastUpdateTime;

        // Events
        public System.Action<CacheStatistics> OnCacheStatsUpdated;
        public System.Action<OptimizationResult> OnOptimizationCompleted;
        public System.Action<ValidationResult> OnValidationCompleted;

        public static EstimateCacheManager Instance { get; private set; }
        public bool IsInitialized => _isInitialized;

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager;
        public bool IsTickable => _enableCaching && _isInitialized && enabled;

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
                _storageManager = new CacheStorageManager(_enableLogging, _maxCacheSize, _defaultCacheExpiration);
                _optimizationManager = new CacheOptimizationManager(_enableLogging, _optimizationInterval, _memoryThreshold, 100, _evictionPolicy);
                _validationManager = new CacheValidationManager(_enableLogging, _enableCacheValidation, _validationInterval, _accuracyThreshold, _validationSampleSize);

                // Wire up events between components
                _storageManager.OnStatisticsUpdated += OnCacheStatsUpdatedInternal;
                _optimizationManager.OnOptimizationCompleted += OnOptimizationCompletedInternal;
                _validationManager.OnValidationCompleted += OnValidationCompletedInternal;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CACHE", "EstimateCacheManager components initialized", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"Failed to initialize EstimateCacheManager components: {ex.Message}", this);
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize optimization manager with storage manager dependency
                _optimizationManager.Initialize(_storageManager);
                _validationManager.Initialize(_storageManager);

                // Register managers with UpdateOrchestrator
                if (_enableCacheOptimization)
                    UpdateOrchestrator.Instance?.RegisterTickable(_optimizationManager);

                if (_enableCacheValidation)
                    UpdateOrchestrator.Instance?.RegisterTickable(_validationManager);

                _lastUpdateTime = Time.time;
                _isInitialized = true;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CACHE", "EstimateCacheManager initialized with composition pattern", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"EstimateCacheManager initialization failed: {ex.Message}", this);
            }
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (!_enableCaching || !_isInitialized) return;

            // Coordinator tick - mainly for statistics updates
            if (Time.time - _lastUpdateTime >= 1.0f) // Update every second
            {
                var stats = _storageManager.GetStatistics();
                OnCacheStatsUpdated?.Invoke(stats);
                _lastUpdateTime = Time.time;
            }
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", "EstimateCacheManager registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", "EstimateCacheManager unregistered from UpdateOrchestrator", this);
        }

        #endregion

        #region Event Handlers

        private void OnCacheStatsUpdatedInternal(CacheStatistics stats)
        {
            OnCacheStatsUpdated?.Invoke(stats);
        }

        private void OnOptimizationCompletedInternal(OptimizationResult result)
        {
            OnOptimizationCompleted?.Invoke(result);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", $"Cache optimization completed: {result.ItemsEvicted} items evicted", this);
        }

        private void OnValidationCompletedInternal(ValidationResult result)
        {
            OnValidationCompleted?.Invoke(result);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", $"Cache validation completed: {result.OverallAccuracy:F2} accuracy", this);
        }

        #endregion

        #region Public API - Delegates to Components

        /// <summary>
        /// Store an estimate in the cache
        /// </summary>
        public bool StoreEstimate(string key, CachedEstimate estimate)
        {
            return _storageManager?.StoreEstimate(key, estimate) ?? false;
        }

        /// <summary>
        /// Retrieve an estimate from the cache
        /// </summary>
        public CachedEstimate GetEstimate(string key)
        {
            return _storageManager?.GetEstimate(key);
        }

        /// <summary>
        /// Remove an estimate from the cache
        /// </summary>
        public bool RemoveEstimate(string key)
        {
            return _storageManager?.RemoveEstimate(key) ?? false;
        }

        /// <summary>
        /// Check if an estimate exists in the cache
        /// </summary>
        public bool ContainsEstimate(string key)
        {
            return _storageManager?.ContainsEstimate(key) ?? false;
        }

        /// <summary>
        /// Store multiple estimates at once
        /// </summary>
        public int StoreBulkEstimates(Dictionary<string, CachedEstimate> estimates)
        {
            return _storageManager?.StoreBulkEstimates(estimates) ?? 0;
        }

        /// <summary>
        /// Clear all estimates from the cache
        /// </summary>
        public void ClearAll()
        {
            _storageManager?.ClearAll();
        }

        /// <summary>
        /// Get current cache statistics
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return _storageManager?.GetStatistics() ?? new CacheStatistics();
        }

        /// <summary>
        /// Get cache utilization information
        /// </summary>
        public CacheUtilization GetCacheUtilization()
        {
            return _storageManager?.GetUtilization() ?? new CacheUtilization();
        }

        /// <summary>
        /// Force immediate cache optimization
        /// </summary>
        public OptimizationResult ForceOptimization()
        {
            return _optimizationManager?.ForceOptimization() ?? new OptimizationResult { Success = false };
        }

        /// <summary>
        /// Force immediate cache validation
        /// </summary>
        public ValidationResult ForceValidation()
        {
            return _validationManager?.ForceValidation() ?? new ValidationResult { Success = false };
        }

        /// <summary>
        /// Get optimization statistics
        /// </summary>
        public OptimizationStatistics GetOptimizationStatistics()
        {
            return _optimizationManager?.GetOptimizationStatistics() ?? new OptimizationStatistics();
        }

        /// <summary>
        /// Get validation statistics
        /// </summary>
        public ValidationStatistics GetValidationStatistics()
        {
            return _validationManager?.GetValidationStatistics() ?? new ValidationStatistics();
        }

        /// <summary>
        /// Get comprehensive cache report
        /// </summary>
        public CacheReport GenerateCacheReport()
        {
            return new CacheReport
            {
                GeneratedAt = DateTime.Now,
                CacheStatistics = GetCacheStatistics(),
                CacheUtilization = GetCacheUtilization(),
                OptimizationStatistics = GetOptimizationStatistics(),
                ValidationStatistics = GetValidationStatistics(),
                IsHealthy = IsHealthy()
            };
        }

        /// <summary>
        /// Check if cache is in healthy state
        /// </summary>
        public bool IsHealthy()
        {
            var stats = GetCacheStatistics();
            var utilization = GetCacheUtilization();
            var validationStats = GetValidationStatistics();

            // Check various health indicators
            var hitRateHealthy = stats.CacheHitRate >= 0.5f;
            var utilizationHealthy = utilization.UtilizationPercentage < 0.9f;
            var accuracyHealthy = validationStats.AverageAccuracy >= _accuracyThreshold;

            return hitRateHealthy && utilizationHealthy && accuracyHealthy;
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Cleanup event handlers
                if (_storageManager != null)
                {
                    _storageManager.OnStatisticsUpdated -= OnCacheStatsUpdatedInternal;
                }

                if (_optimizationManager != null)
                {
                    _optimizationManager.OnOptimizationCompleted -= OnOptimizationCompletedInternal;
                    UpdateOrchestrator.Instance?.UnregisterTickable(_optimizationManager);
                }

                if (_validationManager != null)
                {
                    _validationManager.OnValidationCompleted -= OnValidationCompletedInternal;
                    UpdateOrchestrator.Instance?.UnregisterTickable(_validationManager);
                }

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CACHE", "EstimateCacheManager cleanup completed", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"Error during EstimateCacheManager cleanup: {ex.Message}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Comprehensive cache report
    /// </summary>
    [System.Serializable]
    public struct CacheReport
    {
        public DateTime GeneratedAt;
        public CacheStatistics CacheStatistics;
        public CacheUtilization CacheUtilization;
        public OptimizationStatistics OptimizationStatistics;
        public ValidationStatistics ValidationStatistics;
        public bool IsHealthy;
    }

    #endregion
}
