using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;
// Note: Avoid direct using of Updates to prevent namespace resolution issues in some build setups
using ProjectChimera.Core;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// PERFORMANCE: Automated performance optimization system
    /// Provides real-time optimizations based on performance monitoring data
    /// Phase 1: Performance Foundation Implementation
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Optimization Settings")]
        [SerializeField] private bool _enableOptimizations = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _optimizationInterval = 2.0f;

        [Header("Frame Rate Optimization")]
        [SerializeField] private float _targetFrameRate = 60f;
        [SerializeField] private float _minAcceptableFrameRate = 30f;
        [SerializeField] private int _frameRateHistorySize = 30;

        [Header("Memory Optimization")]
        [SerializeField] private long _memoryPressureThreshold = 400 * 1024 * 1024; // 400MB
        [SerializeField] private long _criticalMemoryThreshold = 600 * 1024 * 1024; // 600MB

        [Header("Quality Settings")]
        [SerializeField] private bool _allowQualityReduction = true;
        [SerializeField] private bool _allowFrameRateReduction = false;

        private AdvancedPerformanceMonitor _performanceMonitor;
        private float _lastOptimizationTime;
        private readonly Queue<float> _recentFrameRates = new Queue<float>();

        // Optimization state
        private int _currentQualityLevel;
        private float _currentTargetFrameRate;
        private bool _hasReducedQuality = false;


        // Events
        public event System.Action<OptimizationAction> OnOptimizationApplied;

        private void Awake()
        {
            Initialize();

            // Register with UpdateOrchestrator for centralized update management
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            if (ProjectChimera.Core.Updates.UpdateOrchestrator.Instance != null)
            {
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        #region ITickable Implementation

        /// <summary>
        /// Performance optimization priority - runs after monitoring but before gameplay
        /// </summary>
        public int TickPriority => -40;

        /// <summary>
        /// Should tick when optimizations are enabled
        /// </summary>
        public bool IsTickable => _enableOptimizations && enabled;

        #endregion

        private void Initialize()
        {
            // Monitor may be injected by a coordinator at runtime
            // (Avoid direct DI container dependency to maintain assembly boundaries)

            _currentQualityLevel = QualitySettings.GetQualityLevel();
            _currentTargetFrameRate = _targetFrameRate;

            if (_enableLogging)
            {
                if (_performanceMonitor != null)
                {
                    SharedLogger.Log("PERFORMANCE", "✅ Performance optimizer initialized with monitor", this);
                }
                else
                {
                    SharedLogger.LogWarning("PERFORMANCE", "⚠️ Performance optimizer initialized without monitor - performance data unavailable", this);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_enableOptimizations) return;

            // Track frame rate history
            UpdateFrameRateHistory(Time.deltaTime);

            // Perform optimizations at intervals
            if (Time.time - _lastOptimizationTime >= _optimizationInterval)
            {
                PerformOptimizationPass();
                _lastOptimizationTime = Time.time;
            }
        }

        private void UpdateFrameRateHistory(float deltaTime)
        {
            float currentFPS = 1f / deltaTime;
            _recentFrameRates.Enqueue(currentFPS);

            while (_recentFrameRates.Count > _frameRateHistorySize)
            {
                _recentFrameRates.Dequeue();
            }
        }

        private void PerformOptimizationPass()
        {
            if (_performanceMonitor == null || !_performanceMonitor.IsMonitoring)
                return;

            var currentMetrics = _performanceMonitor.GetCurrentMetrics();
            if (currentMetrics == null) return;

            var analysis = _performanceMonitor.LastAnalysis;
            if (analysis == null) return;

            // Check for frame rate issues
            if (_recentFrameRates.Count > 10)
            {
                float averageFPS = _recentFrameRates.Average();
                OptimizeFrameRate(averageFPS, analysis);
            }

            // Check for memory pressure
            OptimizeMemoryUsage(currentMetrics.GCMemory, analysis);

            // Check for rendering bottlenecks
            OptimizeRendering(currentMetrics, analysis);
        }

        private void OptimizeFrameRate(float averageFPS, PerformanceAnalysis analysis)
        {
            if (averageFPS < _minAcceptableFrameRate)
            {
                // Critical frame rate - apply aggressive optimizations
                ApplyFrameRateOptimizations(OptimizationLevel.Aggressive);
            }
            else if (averageFPS < _targetFrameRate * 0.8f)
            {
                // Below target - apply moderate optimizations
                ApplyFrameRateOptimizations(OptimizationLevel.Moderate);
            }
            else if (averageFPS > _targetFrameRate && _hasReducedQuality)
            {
                // Performance recovered - restore quality
                RestoreQualitySettings();
            }
        }

        private void OptimizeMemoryUsage(long currentMemory, PerformanceAnalysis analysis)
        {
            if (currentMemory > _criticalMemoryThreshold)
            {
                // Force garbage collection
                System.GC.Collect();

                ApplyOptimizationAction(new OptimizationAction
                {
                    Type = OptimizationType.MemoryOptimization,
                    Level = OptimizationLevel.Aggressive,
                    Description = "Forced garbage collection due to critical memory usage",
                    Value = currentMemory / (1024f * 1024f) // MB
                });

                if (_enableLogging)
                {
                    SharedLogger.LogWarning("PERFORMANCE", $"Critical memory usage: {currentMemory / (1024 * 1024):F1}MB - Forcing GC", this);
                }
            }
            else if (currentMemory > _memoryPressureThreshold)
            {
                // Suggest garbage collection
                if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
                {
                    System.GC.Collect();

                    ApplyOptimizationAction(new OptimizationAction
                    {
                        Type = OptimizationType.MemoryOptimization,
                        Level = OptimizationLevel.Moderate,
                        Description = "Periodic garbage collection due to memory pressure",
                        Value = currentMemory / (1024f * 1024f) // MB
                    });
                }
            }
        }

        private void OptimizeRendering(FramePerformanceData metrics, PerformanceAnalysis analysis)
        {
            // Check draw calls
            if (metrics.DrawCalls > 2000)
            {
                ApplyOptimizationAction(new OptimizationAction
                {
                    Type = OptimizationType.RenderingOptimization,
                    Level = OptimizationLevel.Moderate,
                    Description = $"High draw calls detected: {metrics.DrawCalls}",
                    Value = metrics.DrawCalls
                });

                if (_enableLogging)
                {
                    SharedLogger.LogWarning("PERFORMANCE", $"High draw calls: {metrics.DrawCalls} - Consider batching", this);
                }
            }

            // Check triangle count
            if (metrics.Triangles > 500000)
            {
                ApplyOptimizationAction(new OptimizationAction
                {
                    Type = OptimizationType.RenderingOptimization,
                    Level = OptimizationLevel.Moderate,
                    Description = $"High triangle count: {metrics.Triangles}",
                    Value = metrics.Triangles
                });

                if (_enableLogging)
                {
                    SharedLogger.LogWarning("PERFORMANCE", $"High triangle count: {metrics.Triangles} - Consider LOD", this);
                }
            }
        }

        private void ApplyFrameRateOptimizations(OptimizationLevel level)
        {
            if (!_allowQualityReduction) return;

            switch (level)
            {
                case OptimizationLevel.Moderate:
                    if (_currentQualityLevel > 1)
                    {
                        QualitySettings.SetQualityLevel(_currentQualityLevel - 1, true);
                        _hasReducedQuality = true;

                        ApplyOptimizationAction(new OptimizationAction
                        {
                            Type = OptimizationType.QualityReduction,
                            Level = level,
                            Description = $"Reduced quality level to {_currentQualityLevel - 1}",
                            Value = _currentQualityLevel - 1
                        });
                    }
                    break;

                case OptimizationLevel.Aggressive:
                    if (_currentQualityLevel > 0)
                    {
                        QualitySettings.SetQualityLevel(Mathf.Max(0, _currentQualityLevel - 2), true);
                        _hasReducedQuality = true;

                        ApplyOptimizationAction(new OptimizationAction
                        {
                            Type = OptimizationType.QualityReduction,
                            Level = level,
                            Description = $"Aggressively reduced quality level to {Mathf.Max(0, _currentQualityLevel - 2)}",
                            Value = Mathf.Max(0, _currentQualityLevel - 2)
                        });
                    }

                    // Also reduce target frame rate if allowed
                    if (_allowFrameRateReduction && Application.targetFrameRate > 30)
                    {
                        Application.targetFrameRate = 30;

                        ApplyOptimizationAction(new OptimizationAction
                        {
                            Type = OptimizationType.FrameRateReduction,
                            Level = level,
                            Description = "Reduced target frame rate to 30 FPS",
                            Value = 30
                        });
                    }
                    break;
            }
        }

        private void RestoreQualitySettings()
        {
            if (!_hasReducedQuality) return;

            // Gradually restore quality
            int targetQuality = Mathf.Min(_currentQualityLevel, QualitySettings.GetQualityLevel() + 1);
            QualitySettings.SetQualityLevel(targetQuality, true);

            ApplyOptimizationAction(new OptimizationAction
            {
                Type = OptimizationType.QualityRestoration,
                Level = OptimizationLevel.Moderate,
                Description = $"Restored quality level to {targetQuality}",
                Value = targetQuality
            });

            if (targetQuality >= _currentQualityLevel)
            {
                _hasReducedQuality = false;
            }

            // Restore target frame rate if it was reduced
            if (Application.targetFrameRate < _currentTargetFrameRate)
            {
                Application.targetFrameRate = (int)_currentTargetFrameRate;

                ApplyOptimizationAction(new OptimizationAction
                {
                    Type = OptimizationType.FrameRateRestoration,
                    Level = OptimizationLevel.Moderate,
                    Description = $"Restored target frame rate to {_currentTargetFrameRate} FPS",
                    Value = _currentTargetFrameRate
                });
            }
        }

        private void ApplyOptimizationAction(OptimizationAction action)
        {
            action.Timestamp = Time.time;
            OnOptimizationApplied?.Invoke(action);

            if (_enableLogging)
            {
                SharedLogger.Log("PERFORMANCE", $"OPTIMIZATION: {action.Description}", this);
            }
        }

        /// <summary>
        /// Get current optimization status
        /// </summary>
        public OptimizationStatus GetOptimizationStatus()
        {
            return new OptimizationStatus
            {
                IsOptimizing = _enableOptimizations,
                CurrentQualityLevel = QualitySettings.GetQualityLevel(),
                OriginalQualityLevel = _currentQualityLevel,
                HasReducedQuality = _hasReducedQuality,
                AverageFrameRate = _recentFrameRates.Count > 0 ? _recentFrameRates.Average() : 0f,
                TargetFrameRate = _currentTargetFrameRate
            };
        }

        /// <summary>
        /// Force immediate optimization pass
        /// </summary>
        public void ForceOptimizationPass()
        {
            PerformOptimizationPass();
        }

        /// <summary>
        /// Reset all optimizations to original settings
        /// </summary>
        [ContextMenu("Reset Optimizations")]
        public void ResetOptimizations()
        {
            QualitySettings.SetQualityLevel(_currentQualityLevel, true);
            Application.targetFrameRate = (int)_currentTargetFrameRate;
            _hasReducedQuality = false;

            if (_enableLogging)
            {
                SharedLogger.Log("PERFORMANCE", "All optimizations reset to original settings", this);
            }
        }
    }

    #region Optimization Data Structures

    /// <summary>
    /// Represents an optimization action taken by the system
    /// </summary>
    [System.Serializable]
    public class OptimizationAction
    {
        public OptimizationType Type;
        public OptimizationLevel Level;
        public string Description;
        public float Value;
        public float Timestamp;
    }

    /// <summary>
    /// Current status of the optimization system
    /// </summary>
    [System.Serializable]
    public class OptimizationStatus
    {
        public bool IsOptimizing;
        public int CurrentQualityLevel;
        public int OriginalQualityLevel;
        public bool HasReducedQuality;
        public float AverageFrameRate;
        public float TargetFrameRate;
    }

    /// <summary>
    /// Types of optimizations that can be applied
    /// </summary>
    public enum OptimizationType
    {
        QualityReduction,
        QualityRestoration,
        FrameRateReduction,
        FrameRateRestoration,
        MemoryOptimization,
        RenderingOptimization
    }

    /// <summary>
    /// Optimization intensity levels
    /// </summary>
    public enum OptimizationLevel
    {
        Light,
        Moderate,
        Aggressive
    }

    #endregion
}
