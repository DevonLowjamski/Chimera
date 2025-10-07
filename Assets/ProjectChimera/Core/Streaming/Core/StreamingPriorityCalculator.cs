using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Streaming Priority Calculator - Focused priority and distance calculations
    /// Handles streaming priority calculations and distance-based decision making
    /// Single Responsibility: Priority calculation and streaming decisions
    /// </summary>
    public class StreamingPriorityCalculator : MonoBehaviour
    {
        [Header("Priority Calculation Settings")]
        [SerializeField] private bool _enablePriorityCalculation = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("Distance Settings")]
        [SerializeField] private float _streamingRadius = 100f;
        [SerializeField] private float _unloadRadius = 150f;
        [SerializeField] private float _highPriorityRadius = 50f;
        [SerializeField] private float _lowPriorityRadius = 200f;

        [Header("Priority Modifiers")]
        [SerializeField] private float _criticalDistanceMultiplier = 1.5f;
        [SerializeField] private float _highDistanceMultiplier = 1.2f;
        [SerializeField] private float _lowDistanceMultiplier = 0.8f;
        [SerializeField] private float _veryLowDistanceMultiplier = 0.5f;

        // Statistics
        private PriorityCalculationStats _stats = new PriorityCalculationStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public PriorityCalculationStats GetStats() => _stats;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new PriorityCalculationStats();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🎯 StreamingPriorityCalculator initialized", this);
        }

        /// <summary>
        /// Calculate effective priority based on distance and base priority
        /// </summary>
        public StreamingPriority CalculateEffectivePriority(StreamedAsset asset, float distance)
        {
            if (!_enablePriorityCalculation || asset == null)
                return StreamingPriority.Medium;

            StreamingPriority basePriority = asset.Priority;
            _stats.PriorityCalculations++;

            // Distance-based priority adjustment
            StreamingPriority effectivePriority = AdjustPriorityByDistance(basePriority, distance);

            // Tag-based priority adjustment
            effectivePriority = AdjustPriorityByTags(effectivePriority, asset.Tags);

            // Time-based priority adjustment
            effectivePriority = AdjustPriorityByAge(effectivePriority, asset);

            return effectivePriority;
        }

        /// <summary>
        /// Determine if asset should be loaded based on distance and priority
        /// </summary>
        public bool ShouldAssetBeLoaded(float distance, StreamingPriority priority)
        {
            if (!_enablePriorityCalculation)
                return false;

            // Assets beyond unload radius should never be loaded
            if (distance > _unloadRadius)
            {
                _stats.RejectedByDistance++;
                return false;
            }

            // Calculate priority-adjusted load distance
            float loadDistance = CalculateLoadDistanceByPriority(priority);

            bool shouldLoad = distance <= loadDistance;

            if (shouldLoad)
                _stats.ApprovedForLoading++;
            else
                _stats.RejectedByPriority++;

            return shouldLoad;
        }

        /// <summary>
        /// Calculate load distance based on priority
        /// </summary>
        public float CalculateLoadDistanceByPriority(StreamingPriority priority)
        {
            switch (priority)
            {
                case StreamingPriority.Critical:
                    return _streamingRadius * _criticalDistanceMultiplier;
                case StreamingPriority.High:
                    return _streamingRadius * _highDistanceMultiplier;
                case StreamingPriority.Medium:
                    return _streamingRadius;
                case StreamingPriority.Low:
                    return _streamingRadius * _lowDistanceMultiplier;
                case StreamingPriority.VeryLow:
                    return _streamingRadius * _veryLowDistanceMultiplier;
                default:
                    return _streamingRadius;
            }
        }

        /// <summary>
        /// Check if asset should be unloaded based on distance and inactivity
        /// </summary>
        public bool ShouldAssetBeUnloaded(StreamedAsset asset, float distance)
        {
            if (!_enablePriorityCalculation || asset == null)
                return false;

            // Always unload if beyond unload radius
            if (distance > _unloadRadius)
                return true;

            // Check for inactivity-based unloading
            float timeSinceLastAccess = Time.time - asset.LastAccessTime;
            float inactivityThreshold = CalculateInactivityThreshold(asset.Priority);

            if (timeSinceLastAccess > inactivityThreshold && distance > _streamingRadius)
            {
                _stats.UnloadedByInactivity++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update radius settings
        /// </summary>
        public void UpdateRadiusSettings(float streamingRadius, float unloadRadius)
        {
            _streamingRadius = streamingRadius;
            _unloadRadius = unloadRadius;

            // Update derived radii
            _highPriorityRadius = streamingRadius * 0.5f;
            _lowPriorityRadius = streamingRadius * 2f;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Updated radius settings: streaming={streamingRadius}, unload={unloadRadius}", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingPriorityCalculator: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Adjust priority based on distance
        /// </summary>
        private StreamingPriority AdjustPriorityByDistance(StreamingPriority basePriority, float distance)
        {
            // Boost priority if very close
            if (distance <= _highPriorityRadius)
            {
                return (StreamingPriority)Mathf.Min((int)basePriority + 1, (int)StreamingPriority.Critical);
            }
            // Reduce priority if far away
            else if (distance >= _lowPriorityRadius)
            {
                return (StreamingPriority)Mathf.Max((int)basePriority - 1, (int)StreamingPriority.VeryLow);
            }

            return basePriority;
        }

        /// <summary>
        /// Adjust priority based on asset tags
        /// </summary>
        private StreamingPriority AdjustPriorityByTags(StreamingPriority basePriority, string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return basePriority;

            foreach (var tag in tags)
            {
                switch (tag.ToLower())
                {
                    case "critical":
                    case "essential":
                    case "player":
                        return StreamingPriority.Critical;
                    case "ui":
                    case "interface":
                        return (StreamingPriority)Mathf.Min((int)basePriority + 1, (int)StreamingPriority.Critical);
                    case "background":
                    case "ambient":
                    case "decoration":
                        return (StreamingPriority)Mathf.Max((int)basePriority - 1, (int)StreamingPriority.VeryLow);
                }
            }

            return basePriority;
        }

        /// <summary>
        /// Adjust priority based on asset age and access patterns
        /// </summary>
        private StreamingPriority AdjustPriorityByAge(StreamingPriority basePriority, StreamedAsset asset)
        {
            float timeSinceLastAccess = Time.time - asset.LastAccessTime;

            // Recently accessed assets get priority boost
            if (timeSinceLastAccess < 5f) // 5 seconds
            {
                return (StreamingPriority)Mathf.Min((int)basePriority + 1, (int)StreamingPriority.Critical);
            }
            // Old assets get priority reduction
            else if (timeSinceLastAccess > 60f) // 1 minute
            {
                return (StreamingPriority)Mathf.Max((int)basePriority - 1, (int)StreamingPriority.VeryLow);
            }

            return basePriority;
        }

        /// <summary>
        /// Calculate inactivity threshold for unloading
        /// </summary>
        private float CalculateInactivityThreshold(StreamingPriority priority)
        {
            switch (priority)
            {
                case StreamingPriority.Critical:
                    return 300f; // 5 minutes
                case StreamingPriority.High:
                    return 120f; // 2 minutes
                case StreamingPriority.Medium:
                    return 60f;  // 1 minute
                case StreamingPriority.Low:
                    return 30f;  // 30 seconds
                case StreamingPriority.VeryLow:
                    return 15f;  // 15 seconds
                default:
                    return 60f;
            }
        }

        #endregion
    }

    /// <summary>
    /// Priority calculation statistics
    /// </summary>
    [System.Serializable]
    public struct PriorityCalculationStats
    {
        public int PriorityCalculations;
        public int ApprovedForLoading;
        public int RejectedByDistance;
        public int RejectedByPriority;
        public int UnloadedByInactivity;
    }
}