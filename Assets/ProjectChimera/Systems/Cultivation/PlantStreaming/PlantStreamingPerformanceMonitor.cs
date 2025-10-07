using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation.PlantStreaming
{
    /// <summary>
    /// REFACTORED: Plant Streaming Performance Monitor
    /// Focused component for tracking and reporting plant streaming performance metrics
    /// </summary>
    public class PlantStreamingPerformanceMonitor : MonoBehaviour, ITickable
    {
        [Header("Performance Monitoring Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedTracking = false;
        [SerializeField] private float _statsUpdateInterval = 1f;
        [SerializeField] private float _memoryPressureThreshold = 0.8f;

        // Performance statistics
        private PlantStreamingStats _currentStats = new PlantStreamingStats();
        private float _lastStatsUpdate;

        // Memory tracking
        private long _lastMemoryUsage;
        private readonly List<float> _frameTimeHistory = new List<float>();
        private readonly int _maxFrameHistory = 60;

        // Performance thresholds
        private float _targetFrameTime = 0.016f; // 60 FPS
        private int _performanceWarningCount;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public PlantStreamingStats CurrentStats => _currentStats;
        public float MemoryPressureThreshold => _memoryPressureThreshold;
        public bool IsMemoryPressureHigh => GetCurrentMemoryPressure() > _memoryPressureThreshold;

        // Events
        public System.Action<PlantStreamingStats> OnStatsUpdated;
        public System.Action<float> OnMemoryPressureChanged;
        public System.Action<PerformanceWarning> OnPerformanceWarning;

        // ITickable implementation
        public int TickPriority => 5; // Performance monitoring should run early but not critical
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        private void Start()
        {
            Initialize();
            RegisterWithUpdateOrchestrator();
        }

        private void Initialize()
        {
            ResetStats();
            _lastStatsUpdate = Time.time;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "✅ Plant Streaming Performance Monitor initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastStatsUpdate >= _statsUpdateInterval)
            {
                UpdatePerformanceStats();
                _lastStatsUpdate = Time.time;
            }

            if (_enableDetailedTracking)
            {
                TrackFrameTime();
                CheckPerformanceWarnings();
            }
        }

        /// <summary>
        /// Update statistics based on current streamed plants
        /// </summary>
        public void UpdateStats(Dictionary<string, StreamedPlant> streamedPlants)
        {
            if (!IsEnabled) return;

            _currentStats.RegisteredPlants = streamedPlants.Count;
            _currentStats.LoadedPlants = 0;
            _currentStats.VisiblePlants = 0;
            _currentStats.PlantsByLODLevel = new int[5]; // Assume max 5 LOD levels

            // Calculate distance distribution
            var distances = new List<float>();

            foreach (var streamedPlant in streamedPlants.Values)
            {
                if (streamedPlant.IsLoaded)
                {
                    _currentStats.LoadedPlants++;
                    distances.Add(streamedPlant.DistanceFromViewer);
                }

                if (streamedPlant.IsVisible)
                {
                    _currentStats.VisiblePlants++;
                }

                // Track LOD distribution
                if (streamedPlant.CurrentLODLevel >= 0 && streamedPlant.CurrentLODLevel < _currentStats.PlantsByLODLevel.Length)
                {
                    _currentStats.PlantsByLODLevel[streamedPlant.CurrentLODLevel]++;
                }
            }

            // Calculate average distance
            if (distances.Count > 0)
            {
                _currentStats.AverageLoadedPlantDistance = distances.Average();
            }

            // Update memory usage
            _currentStats.CurrentMemoryUsage = GetCurrentMemoryUsage();
            _currentStats.MemoryPressure = GetCurrentMemoryPressure();
        }

        /// <summary>
        /// Record streaming event
        /// </summary>
        public void RecordStreamingEvent(StreamingEventType eventType)
        {
            if (!IsEnabled) return;

            switch (eventType)
            {
                case StreamingEventType.PlantLoaded:
                    _currentStats.PlantsLoadedThisSession++;
                    break;
                case StreamingEventType.PlantUnloaded:
                    _currentStats.PlantsUnloadedThisSession++;
                    break;
                case StreamingEventType.LODChanged:
                    _currentStats.LODChangesThisSession++;
                    break;
                case StreamingEventType.MemoryWarning:
                    _currentStats.MemoryWarnings++;
                    OnMemoryPressureChanged?.Invoke(_currentStats.MemoryPressure);
                    break;
            }
        }

        /// <summary>
        /// Get detailed performance report
        /// </summary>
        public PlantStreamingDetailedReport GetDetailedReport()
        {
            var report = new PlantStreamingDetailedReport
            {
                BasicStats = _currentStats,
                AverageFrameTime = _frameTimeHistory.Count > 0 ? _frameTimeHistory.Average() : 0f,
                MinFrameTime = _frameTimeHistory.Count > 0 ? _frameTimeHistory.Min() : 0f,
                MaxFrameTime = _frameTimeHistory.Count > 0 ? _frameTimeHistory.Max() : 0f,
                PerformanceWarningCount = _performanceWarningCount,
                IsPerformingWell = IsPerformingWell()
            };

            // Calculate streaming efficiency
            int totalStreamingEvents = _currentStats.PlantsLoadedThisSession + _currentStats.PlantsUnloadedThisSession;
            if (totalStreamingEvents > 0)
            {
                report.StreamingEfficiency = (float)_currentStats.LoadedPlants / totalStreamingEvents;
            }

            return report;
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStats()
        {
            _currentStats = new PlantStreamingStats
            {
                RegisteredPlants = 0,
                LoadedPlants = 0,
                VisiblePlants = 0,
                PlantsByLODLevel = new int[5],
                AverageLoadedPlantDistance = 0f,
                PlantsLoadedThisSession = 0,
                PlantsUnloadedThisSession = 0,
                LODChangesThisSession = 0,
                MemoryWarnings = 0,
                CurrentMemoryUsage = 0,
                MemoryPressure = 0f
            };

            _frameTimeHistory.Clear();
            _performanceWarningCount = 0;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Performance statistics reset", this);
        }

        private void RegisterWithUpdateOrchestrator()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "Performance Monitor registered with UpdateOrchestrator", this);
        }

        private void OnDestroy()
        {
            var orchestrator = UpdateOrchestrator.Instance;
            orchestrator?.UnregisterTickable(this);
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }

        /// <summary>
        /// Set performance monitoring enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _frameTimeHistory.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Performance monitoring: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set memory pressure threshold
        /// </summary>
        public void SetMemoryPressureThreshold(float threshold)
        {
            _memoryPressureThreshold = Mathf.Clamp01(threshold);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Memory pressure threshold set to: {_memoryPressureThreshold:F2}", this);
        }

        private void UpdatePerformanceStats()
        {
            OnStatsUpdated?.Invoke(_currentStats);

            if (_enableLogging)
            {
                LogCurrentStats();
            }
        }

        private void TrackFrameTime()
        {
            _frameTimeHistory.Add(Time.deltaTime);

            if (_frameTimeHistory.Count > _maxFrameHistory)
            {
                _frameTimeHistory.RemoveAt(0);
            }
        }

        private void CheckPerformanceWarnings()
        {
            // Check frame time performance
            if (_frameTimeHistory.Count > 10)
            {
                float recentAverageFrameTime = _frameTimeHistory.Skip(_frameTimeHistory.Count - 10).Take(10).Average();
                if (recentAverageFrameTime > _targetFrameTime * 1.5f) // 50% worse than target
                {
                    TriggerPerformanceWarning(PerformanceWarning.HighFrameTime);
                }
            }

            // Check memory pressure
            if (_currentStats.MemoryPressure > _memoryPressureThreshold)
            {
                TriggerPerformanceWarning(PerformanceWarning.HighMemoryPressure);
            }

            // Check streaming efficiency
            if (_currentStats.LoadedPlants > 0 && (float)_currentStats.VisiblePlants / _currentStats.LoadedPlants < 0.3f)
            {
                TriggerPerformanceWarning(PerformanceWarning.LowStreamingEfficiency);
            }
        }

        private void TriggerPerformanceWarning(PerformanceWarning warning)
        {
            _performanceWarningCount++;
            OnPerformanceWarning?.Invoke(warning);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Performance warning: {warning}", this);
        }

        private bool IsPerformingWell()
        {
            bool frameTimeGood = _frameTimeHistory.Count == 0 || _frameTimeHistory.Average() <= _targetFrameTime * 1.2f;
            bool memoryGood = _currentStats.MemoryPressure <= _memoryPressureThreshold;
            bool streamingEfficient = _currentStats.LoadedPlants == 0 || (float)_currentStats.VisiblePlants / _currentStats.LoadedPlants >= 0.5f;

            return frameTimeGood && memoryGood && streamingEfficient;
        }

        private long GetCurrentMemoryUsage()
        {
            // In a real implementation, this would get actual memory usage
            // For now, return a simulated value
            return System.GC.GetTotalMemory(false);
        }

        private float GetCurrentMemoryPressure()
        {
            // Simulate memory pressure calculation
            long currentMemory = GetCurrentMemoryUsage();
            long availableMemory = 1024 * 1024 * 1024; // 1GB simulated available memory

            return Mathf.Clamp01((float)currentMemory / availableMemory);
        }

        private void LogCurrentStats()
        {
            ChimeraLogger.Log("CULTIVATION",
                $"Plant Streaming Stats - Registered: {_currentStats.RegisteredPlants}, " +
                $"Loaded: {_currentStats.LoadedPlants}, Visible: {_currentStats.VisiblePlants}, " +
                $"Memory Pressure: {_currentStats.MemoryPressure:F2}", this);
        }
    }

    /// <summary>
    /// Plant streaming statistics
    /// </summary>
    [System.Serializable]
    public struct PlantStreamingStats
    {
        public int RegisteredPlants;
        public int LoadedPlants;
        public int VisiblePlants;
        public int[] PlantsByLODLevel;
        public float AverageLoadedPlantDistance;
        public int PlantsLoadedThisSession;
        public int PlantsUnloadedThisSession;
        public int LODChangesThisSession;
        public int MemoryWarnings;
        public long CurrentMemoryUsage;
        public float MemoryPressure;
    }

    /// <summary>
    /// Detailed plant streaming performance report
    /// </summary>
    [System.Serializable]
    public struct PlantStreamingDetailedReport
    {
        public PlantStreamingStats BasicStats;
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public float StreamingEfficiency;
        public int PerformanceWarningCount;
        public bool IsPerformingWell;
    }

    /// <summary>
    /// Streaming event types for performance tracking
    /// </summary>
    public enum StreamingEventType
    {
        PlantLoaded,
        PlantUnloaded,
        LODChanged,
        MemoryWarning
    }

    /// <summary>
    /// Performance warning types
    /// </summary>
    public enum PerformanceWarning
    {
        HighFrameTime,
        HighMemoryPressure,
        LowStreamingEfficiency,
        ExcessiveLODChanges
    }
}