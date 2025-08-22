using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles performance optimization and throttled updates for the analytics dashboard in Project Chimera's game.
    /// Manages frame budget allocation, update queuing, and adaptive performance tuning for cannabis cultivation analytics.
    /// </summary>
    public class DataDashboardPerformanceManager : MonoBehaviour
    {
        [Header("Performance Configuration")]
        [SerializeField] private float _updateBudgetMs = 16.6f; // Target: 60 FPS (16.6ms per frame)
        [SerializeField] private int _maxUpdatesPerFrame = 3;
        [SerializeField] private bool _enableAdaptivePerformance = true;
        [SerializeField] private bool _enableDebugLogging = false;

        // Throttled update system
        private Queue<System.Action> _updateQueue;
        private bool _isProcessingUpdates;
        private float _frameStartTime;
        private int _currentFrameUpdates;

        // Performance monitoring
        private float _averageUpdateTime;
        private Queue<float> _updateTimeSamples;
        private const int MAX_TIME_SAMPLES = 30;

        // Performance metrics
        private float _lastFrameTime;
        private int _totalUpdatesProcessed;
        private float _totalProcessingTime;
        private int _droppedUpdates;

        // Events
        public System.Action<float> OnPerformanceMetricsUpdated;
        public System.Action<string> OnPerformanceWarning;

        // Properties
        public float AverageUpdateTime => _averageUpdateTime;
        public int QueuedUpdatesCount => _updateQueue?.Count ?? 0;
        public int MaxUpdatesPerFrame => _maxUpdatesPerFrame;
        public float UpdateBudgetMs => _updateBudgetMs;
        public bool IsProcessingUpdates => _isProcessingUpdates;
        public float PerformanceScore => CalculatePerformanceScore();

        private void Awake()
        {
            InitializePerformanceSystem();
        }

        private void Update()
        {
            UpdateFrameTracking();
            ProcessThrottledUpdates();
            MonitorPerformance();
        }

        #region Performance System Initialization

        private void InitializePerformanceSystem()
        {
            _updateQueue = new Queue<System.Action>();
            _updateTimeSamples = new Queue<float>();
            _isProcessingUpdates = false;
            _currentFrameUpdates = 0;
            _totalUpdatesProcessed = 0;
            _totalProcessingTime = 0f;
            _droppedUpdates = 0;

            LogInfo("Performance management system initialized for cannabis cultivation dashboard");
        }

        #endregion

        #region Frame Tracking and Budget Management

        private void UpdateFrameTracking()
        {
            // Reset frame counters at start of each frame
            float currentTime = Time.realtimeSinceStartup;
            if (_frameStartTime != currentTime)
            {
                _lastFrameTime = currentTime - _frameStartTime;
                _frameStartTime = currentTime;
                _currentFrameUpdates = 0;
            }
        }

        private void MonitorPerformance()
        {
            // Check for performance issues
            if (_averageUpdateTime > _updateBudgetMs * 0.9f)
            {
                OnPerformanceWarning?.Invoke($"Dashboard update time ({_averageUpdateTime:F2}ms) approaching budget limit ({_updateBudgetMs:F2}ms)");
            }

            if (_updateQueue.Count > 50)
            {
                OnPerformanceWarning?.Invoke($"Large update queue detected: {_updateQueue.Count} pending updates");
            }

            // Trigger performance metrics update
            OnPerformanceMetricsUpdated?.Invoke(_averageUpdateTime);
        }

        #endregion

        #region Throttled Update System

        public void QueueUpdate(System.Action updateAction, bool highPriority = false)
        {
            if (updateAction == null) return;

            // Check queue size limit to prevent memory issues
            if (_updateQueue.Count >= 100)
            {
                // Drop oldest updates if queue is too large
                _updateQueue.Dequeue();
                _droppedUpdates++;
                LogWarning("Dropped update due to queue overflow");
            }

            if (highPriority)
            {
                // Convert to list, insert at front, then rebuild queue
                var queueList = new List<System.Action>(_updateQueue);
                queueList.Insert(0, updateAction);
                _updateQueue = new Queue<System.Action>(queueList);
            }
            else
            {
                _updateQueue.Enqueue(updateAction);
            }
        }

        private void ProcessThrottledUpdates()
        {
            if (_isProcessingUpdates || _updateQueue.Count == 0)
                return;

            _isProcessingUpdates = true;
            float startTime = Time.realtimeSinceStartup * 1000f; // Convert to milliseconds

            try
            {
                // Process updates within frame budget and update limit
                while (_updateQueue.Count > 0 && 
                       _currentFrameUpdates < _maxUpdatesPerFrame &&
                       (Time.realtimeSinceStartup * 1000f - startTime) < _updateBudgetMs)
                {
                    var updateAction = _updateQueue.Dequeue();
                    
                    try
                    {
                        updateAction?.Invoke();
                        _currentFrameUpdates++;
                        _totalUpdatesProcessed++;
                    }
                    catch (System.Exception ex)
                    {
                        LogWarning($"Error executing queued update: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogWarning($"Error processing throttled updates: {ex.Message}");
            }
            finally
            {
                float updateTime = (Time.realtimeSinceStartup * 1000f) - startTime;
                _totalProcessingTime += updateTime;
                RecordUpdateTime(updateTime);
                _isProcessingUpdates = false;
            }
        }

        private void RecordUpdateTime(float timeMs)
        {
            _updateTimeSamples.Enqueue(timeMs);
            
            if (_updateTimeSamples.Count > MAX_TIME_SAMPLES)
                _updateTimeSamples.Dequeue();

            // Calculate rolling average
            float total = 0f;
            foreach (float sample in _updateTimeSamples)
                total += sample;
            
            _averageUpdateTime = total / _updateTimeSamples.Count;

            // Adaptive performance tuning
            if (_enableAdaptivePerformance)
            {
                PerformAdaptiveTuning();
            }
        }

        #endregion

        #region Adaptive Performance Tuning

        private void PerformAdaptiveTuning()
        {
            // If we're consistently slow, reduce updates per frame
            if (_averageUpdateTime > _updateBudgetMs * 0.8f)
            {
                if (_maxUpdatesPerFrame > 1)
                {
                    _maxUpdatesPerFrame = Mathf.Max(1, _maxUpdatesPerFrame - 1);
                    LogInfo($"Reduced max updates per frame to {_maxUpdatesPerFrame} due to performance");
                }
            }
            // If we have headroom, increase updates per frame
            else if (_averageUpdateTime < _updateBudgetMs * 0.4f)
            {
                if (_maxUpdatesPerFrame < 10)
                {
                    _maxUpdatesPerFrame = Mathf.Min(10, _maxUpdatesPerFrame + 1);
                    LogInfo($"Increased max updates per frame to {_maxUpdatesPerFrame} due to good performance");
                }
            }

            // Adjust budget based on frame rate
            float targetFrameTime = 1000f / 60f; // 60 FPS target
            if (_lastFrameTime * 1000f > targetFrameTime * 1.2f)
            {
                // Reduce budget if frames are taking too long
                _updateBudgetMs = Mathf.Max(5f, _updateBudgetMs * 0.9f);
            }
            else if (_lastFrameTime * 1000f < targetFrameTime * 0.8f)
            {
                // Increase budget if we have frame time to spare
                _updateBudgetMs = Mathf.Min(30f, _updateBudgetMs * 1.1f);
            }
        }

        #endregion

        #region Performance Metrics

        private float CalculatePerformanceScore()
        {
            if (_totalUpdatesProcessed == 0) return 1.0f;

            // Score based on average update time vs budget
            float timeScore = 1.0f - Mathf.Clamp01(_averageUpdateTime / _updateBudgetMs);
            
            // Score based on queue efficiency
            float queueScore = _updateQueue.Count < 10 ? 1.0f : Mathf.Clamp01(1.0f - (_updateQueue.Count / 50f));
            
            // Score based on dropped updates
            float dropScore = _droppedUpdates == 0 ? 1.0f : Mathf.Clamp01(1.0f - (_droppedUpdates / (float)_totalUpdatesProcessed));

            return (timeScore + queueScore + dropScore) / 3.0f;
        }

        public PerformanceMetrics GetDetailedMetrics()
        {
            return new PerformanceMetrics
            {
                AverageUpdateTimeMs = _averageUpdateTime,
                QueuedUpdates = _updateQueue.Count,
                MaxUpdatesPerFrame = _maxUpdatesPerFrame,
                UpdateBudgetMs = _updateBudgetMs,
                TotalUpdatesProcessed = _totalUpdatesProcessed,
                TotalProcessingTimeMs = _totalProcessingTime,
                DroppedUpdates = _droppedUpdates,
                PerformanceScore = CalculatePerformanceScore(),
                LastFrameTimeMs = _lastFrameTime * 1000f,
                IsAdaptiveEnabled = _enableAdaptivePerformance
            };
        }

        #endregion

        #region Configuration Methods

        public void SetUpdateBudget(float budgetMs)
        {
            _updateBudgetMs = Mathf.Clamp(budgetMs, 5f, 50f);
            LogInfo($"Update budget set to {_updateBudgetMs:F2}ms");
        }

        public void SetMaxUpdatesPerFrame(int maxUpdates)
        {
            _maxUpdatesPerFrame = Mathf.Clamp(maxUpdates, 1, 20);
            LogInfo($"Max updates per frame set to {_maxUpdatesPerFrame}");
        }

        public void SetAdaptivePerformance(bool enabled)
        {
            _enableAdaptivePerformance = enabled;
            LogInfo($"Adaptive performance tuning {(enabled ? "enabled" : "disabled")}");
        }

        #endregion

        #region Emergency Operations

        public void FlushUpdateQueue()
        {
            if (_updateQueue == null) return;

            int processedCount = 0;
            int maxFlushUpdates = 50; // Safety limit to prevent hanging

            LogInfo($"Flushing {_updateQueue.Count} queued updates");

            while (_updateQueue.Count > 0 && processedCount < maxFlushUpdates)
            {
                try
                {
                    var updateAction = _updateQueue.Dequeue();
                    updateAction?.Invoke();
                    processedCount++;
                    _totalUpdatesProcessed++;
                }
                catch (System.Exception ex)
                {
                    LogWarning($"Error in flush update: {ex.Message}");
                    break;
                }
            }

            LogInfo($"Flushed {processedCount} updates, {_updateQueue.Count} remaining");
        }

        public void ClearUpdateQueue()
        {
            int clearedCount = _updateQueue?.Count ?? 0;
            _updateQueue?.Clear();
            _droppedUpdates += clearedCount;
            
            LogWarning($"Cleared {clearedCount} pending updates from queue");
        }

        public void PauseProcessing()
        {
            _isProcessingUpdates = true;
            LogInfo("Update processing paused");
        }

        public void ResumeProcessing()
        {
            _isProcessingUpdates = false;
            LogInfo("Update processing resumed");
        }

        #endregion

        #region Performance Presets

        public void ApplyPerformancePreset(PerformancePreset preset)
        {
            switch (preset)
            {
                case PerformancePreset.HighPerformance:
                    SetUpdateBudget(25f);
                    SetMaxUpdatesPerFrame(8);
                    SetAdaptivePerformance(true);
                    break;
                    
                case PerformancePreset.Balanced:
                    SetUpdateBudget(16.6f);
                    SetMaxUpdatesPerFrame(4);
                    SetAdaptivePerformance(true);
                    break;
                    
                case PerformancePreset.LowEnd:
                    SetUpdateBudget(10f);
                    SetMaxUpdatesPerFrame(2);
                    SetAdaptivePerformance(true);
                    break;
                    
                case PerformancePreset.Conservative:
                    SetUpdateBudget(8f);
                    SetMaxUpdatesPerFrame(1);
                    SetAdaptivePerformance(false);
                    break;
            }
            
            LogInfo($"Applied performance preset: {preset}");
        }

        public enum PerformancePreset
        {
            HighPerformance,
            Balanced,
            LowEnd,
            Conservative
        }

        #endregion

        #region Public API

        public bool IsPerformingWell()
        {
            return _averageUpdateTime < _updateBudgetMs * 0.7f && _updateQueue.Count < 20;
        }

        public void ResetPerformanceMetrics()
        {
            _updateTimeSamples.Clear();
            _averageUpdateTime = 0f;
            _totalUpdatesProcessed = 0;
            _totalProcessingTime = 0f;
            _droppedUpdates = 0;
            
            LogInfo("Performance metrics reset");
        }

        public float GetEstimatedProcessingTime()
        {
            if (_updateQueue.Count == 0) return 0f;
            
            float avgTimePerUpdate = _averageUpdateTime / Mathf.Max(1, _maxUpdatesPerFrame);
            return _updateQueue.Count * avgTimePerUpdate;
        }

        #endregion

        private void LogInfo(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[DataDashboardPerformance] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[DataDashboardPerformance] {message}");
        }

        [System.Serializable]
        public struct PerformanceMetrics
        {
            public float AverageUpdateTimeMs;
            public int QueuedUpdates;
            public int MaxUpdatesPerFrame;
            public float UpdateBudgetMs;
            public int TotalUpdatesProcessed;
            public float TotalProcessingTimeMs;
            public int DroppedUpdates;
            public float PerformanceScore;
            public float LastFrameTimeMs;
            public bool IsAdaptiveEnabled;
        }
    }
}