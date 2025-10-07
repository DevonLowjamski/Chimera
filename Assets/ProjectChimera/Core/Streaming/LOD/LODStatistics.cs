using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: LOD Statistics
    /// Focused component for tracking and reporting LOD system performance metrics
    /// </summary>
    public class LODStatistics : MonoBehaviour, ITickable
    {
        [Header("Statistics Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedStats = false;
        [SerializeField] private float _statsUpdateInterval = 1f;

        // Core statistics
        private LODStats _stats = new LODStats();
        private float _lastStatsUpdate;

        // Detailed tracking
        private readonly Dictionary<LODObjectType, int> _objectsByType = new Dictionary<LODObjectType, int>();
        private readonly List<float> _frameTimeHistory = new List<float>();
        private readonly int _maxHistorySize = 100;

        // Performance metrics
        private float _peakFrameTime;
        private float _minFrameTime = float.MaxValue;
        private int _totalLODChanges;
        private int _totalVisibilityChanges;

        // Properties
        public LODStats CurrentStats => _stats;
        public float AverageFrameTime => _stats.AverageFrameTime;
        public int TotalRegisteredObjects => _stats.RegisteredObjects;

        // Events
        public System.Action<LODStats> OnStatsUpdated;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        private void Initialize()
        {
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LOD Statistics initialized", this);
        }

        #region ITickable Implementation

        /// <summary>
        /// Statistics collection priority - runs after LOD systems
        /// </summary>
        public int TickPriority => 20;

        /// <summary>
        /// Whether this tickable should be updated this frame
        /// </summary>
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Compatibility alias for older call sites
        public bool ShouldTick => IsTickable;

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastStatsUpdate >= _statsUpdateInterval)
            {
                UpdateStatistics();
                _lastStatsUpdate = Time.time;
            }
        }

        #endregion

        /// <summary>
        /// Record object registration
        /// </summary>
        public void OnObjectRegistered(LODObjectType objectType = LODObjectType.Standard)
        {
            _stats.RegisteredObjects++;

            if (_objectsByType.ContainsKey(objectType))
                _objectsByType[objectType]++;
            else
                _objectsByType[objectType] = 1;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Object registered - Type: {objectType}, Total: {_stats.RegisteredObjects}", this);
        }

        /// <summary>
        /// Record object unregistration
        /// </summary>
        public void OnObjectUnregistered(LODObjectType objectType = LODObjectType.Standard)
        {
            _stats.RegisteredObjects--;

            if (_objectsByType.ContainsKey(objectType))
                _objectsByType[objectType] = Mathf.Max(0, _objectsByType[objectType] - 1);

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Object unregistered - Type: {objectType}, Total: {_stats.RegisteredObjects}", this);
        }

        /// <summary>
        /// Record LOD system update cycle
        /// </summary>
        public void OnUpdateCycle()
        {
            _stats.UpdateCycles++;
        }

        /// <summary>
        /// Record LOD level change
        /// </summary>
        public void OnLODChanged(int fromLOD, int toLOD)
        {
            _stats.LODChanges++;
            _totalLODChanges++;

            // Track LOD distribution
            if (_stats.ObjectsAtLOD == null)
            {
                _stats.ObjectsAtLOD = new int[5]; // 0-4 LOD levels
            }

            if (fromLOD >= 0 && fromLOD < _stats.ObjectsAtLOD.Length)
                _stats.ObjectsAtLOD[fromLOD]--;

            if (toLOD >= 0 && toLOD < _stats.ObjectsAtLOD.Length)
                _stats.ObjectsAtLOD[toLOD]++;

            if (_enableDetailedStats && _enableLogging)
                ChimeraLogger.Log("LOD", $"LOD changed: {fromLOD} → {toLOD}", this);
        }

        /// <summary>
        /// Record visibility change
        /// </summary>
        public void OnVisibilityChanged(bool isVisible)
        {
            _stats.VisibilityChanges++;
            _totalVisibilityChanges++;

            if (isVisible)
                _stats.VisibleObjects++;
            else
                _stats.VisibleObjects = Mathf.Max(0, _stats.VisibleObjects - 1);

            if (_enableDetailedStats && _enableLogging)
                ChimeraLogger.Log("LOD", $"Visibility changed - Visible: {isVisible}, Total visible: {_stats.VisibleObjects}", this);
        }

        /// <summary>
        /// Update frame time tracking
        /// </summary>
        public void UpdateFrameTime(float frameTime)
        {
            _stats.AverageFrameTime = frameTime;

            // Track frame time history
            if (_enableDetailedStats)
            {
                _frameTimeHistory.Add(frameTime);
                if (_frameTimeHistory.Count > _maxHistorySize)
                {
                    _frameTimeHistory.RemoveAt(0);
                }

                // Update peak and min frame times
                if (frameTime > _peakFrameTime)
                    _peakFrameTime = frameTime;
                if (frameTime < _minFrameTime)
                    _minFrameTime = frameTime;
            }
        }

        /// <summary>
        /// Update dynamic LOD multiplier
        /// </summary>
        public void UpdateLODMultiplier(float multiplier)
        {
            _stats.DynamicLODMultiplier = multiplier;
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStats()
        {
            _stats = new LODStats
            {
                RegisteredObjects = 0,
                VisibleObjects = 0,
                UpdateCycles = 0,
                LODChanges = 0,
                VisibilityChanges = 0,
                ObjectsAtLOD = new int[5],
                AverageFrameTime = 0.016f,
                DynamicLODMultiplier = 1f
            };

            _objectsByType.Clear();
            _frameTimeHistory.Clear();
            _peakFrameTime = 0f;
            _minFrameTime = float.MaxValue;
            _totalLODChanges = 0;
            _totalVisibilityChanges = 0;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "Statistics reset", this);
        }

        /// <summary>
        /// Get detailed performance report
        /// </summary>
        public LODPerformanceReport GetDetailedReport()
        {
            var report = new LODPerformanceReport
            {
                BasicStats = _stats,
                ObjectsByType = new Dictionary<LODObjectType, int>(_objectsByType),
                PeakFrameTime = _peakFrameTime,
                MinFrameTime = _minFrameTime == float.MaxValue ? 0f : _minFrameTime,
                TotalLODChanges = _totalLODChanges,
                TotalVisibilityChanges = _totalVisibilityChanges,
                FrameTimeHistory = new List<float>(_frameTimeHistory)
            };

            // Calculate efficiency metrics
            if (_stats.UpdateCycles > 0)
            {
                report.AverageLODChangesPerUpdate = (float)_totalLODChanges / _stats.UpdateCycles;
                report.AverageVisibilityChangesPerUpdate = (float)_totalVisibilityChanges / _stats.UpdateCycles;
            }

            // Calculate frame time variance
            if (_frameTimeHistory.Count > 1)
            {
                float sum = 0f;
                float sumSquares = 0f;
                foreach (float frameTime in _frameTimeHistory)
                {
                    sum += frameTime;
                    sumSquares += frameTime * frameTime;
                }
                float mean = sum / _frameTimeHistory.Count;
                report.FrameTimeVariance = (sumSquares / _frameTimeHistory.Count) - (mean * mean);
            }

            return report;
        }

        /// <summary>
        /// Log current statistics
        /// </summary>
        public void LogCurrentStats()
        {
            if (!_enableLogging) return;

            ChimeraLogger.Log("LOD",
                $"LOD Statistics - Objects: {_stats.RegisteredObjects}, Visible: {_stats.VisibleObjects}, " +
                $"Updates: {_stats.UpdateCycles}, LOD Changes: {_stats.LODChanges}, " +
                $"Frame Time: {_stats.AverageFrameTime * 1000f:F2}ms, Multiplier: {_stats.DynamicLODMultiplier:F2}", this);

            if (_enableDetailedStats)
            {
                ChimeraLogger.Log("LOD", $"Peak Frame Time: {_peakFrameTime * 1000f:F2}ms, " +
                    $"Min Frame Time: {(_minFrameTime == float.MaxValue ? 0f : _minFrameTime) * 1000f:F2}ms", this);
            }
        }

        private void UpdateStatistics()
        {
            OnStatsUpdated?.Invoke(_stats);

            if (_enableDetailedStats)
            {
                LogCurrentStats();
            }
        }

        /// <summary>
        /// Enable/disable detailed statistics tracking
        /// </summary>
        public void SetDetailedStatsEnabled(bool enabled)
        {
            _enableDetailedStats = enabled;

            if (!enabled)
            {
                _frameTimeHistory.Clear();
                _peakFrameTime = 0f;
                _minFrameTime = float.MaxValue;
            }

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Detailed stats: {(enabled ? "enabled" : "disabled")}", this);
        }
    }

    /// <summary>
    /// Core LOD statistics
    /// </summary>
    [System.Serializable]
    public struct LODStats
    {
        public int RegisteredObjects;
        public int VisibleObjects;
        public int UpdateCycles;
        public int LODChanges;
        public int VisibilityChanges;
        public int[] ObjectsAtLOD;
        public float AverageFrameTime;
        public float DynamicLODMultiplier;
    }

    /// <summary>
    /// Detailed LOD performance report
    /// </summary>
    [System.Serializable]
    public struct LODPerformanceReport
    {
        public LODStats BasicStats;
        public Dictionary<LODObjectType, int> ObjectsByType;
        public float PeakFrameTime;
        public float MinFrameTime;
        public float FrameTimeVariance;
        public int TotalLODChanges;
        public int TotalVisibilityChanges;
        public float AverageLODChangesPerUpdate;
        public float AverageVisibilityChangesPerUpdate;
        public List<float> FrameTimeHistory;
    }


}
