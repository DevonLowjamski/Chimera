using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: LOD Adaptive System
    /// Focused component for adaptive LOD quality based on performance metrics
    /// </summary>
    public class LODAdaptiveSystem : MonoBehaviour
    {
        [Header("Adaptive Settings")]
        [SerializeField] private bool _enableAdaptiveLOD = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _targetFrameTime = 0.016f; // 60 FPS
        [SerializeField] private float _adaptationSpeed = 0.5f;

        [Header("LOD Multiplier Limits")]
        [SerializeField] private float _minLODMultiplier = 0.5f;
        [SerializeField] private float _maxLODMultiplier = 2f;
        [SerializeField] private float _performanceBuffer = 0.2f; // 20% buffer

        // Performance tracking
        private float _averageFrameTime = 0.016f;
        private int _frameTimeSamples = 0;
        private float _dynamicLODMultiplier = 1f;

        // Adaptive thresholds
        private float _poorPerformanceThreshold;
        private float _goodPerformanceThreshold;

        // Properties
        public bool IsEnabled => _enableAdaptiveLOD;
        public float CurrentLODMultiplier => _dynamicLODMultiplier;
        public float TargetFrameTime => _targetFrameTime;
        public float AverageFrameTime => _averageFrameTime;

        // Events
        public System.Action<float> OnLODMultiplierChanged;
        public System.Action<AdaptiveQualityLevel> OnQualityLevelChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            UpdatePerformanceThresholds();

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LOD Adaptive System initialized", this);
        }

        /// <summary>
        /// Update adaptive LOD based on current performance
        /// </summary>
        public void UpdateAdaptiveLOD(float deltaTime)
        {
            if (!_enableAdaptiveLOD) return;

            UpdateFrameTimeTracking(deltaTime);
            UpdateLODMultiplier();
        }

        /// <summary>
        /// Apply adaptive adjustment to LOD level
        /// </summary>
        public int ApplyAdaptiveAdjustment(int baseLODLevel)
        {
            if (!_enableAdaptiveLOD) return baseLODLevel;

            // Apply dynamic LOD multiplier
            if (_dynamicLODMultiplier < 1f)
            {
                // Poor performance - increase LOD level (lower quality)
                return Mathf.Min(baseLODLevel + 1, 4);
            }
            else if (_dynamicLODMultiplier > 1.2f)
            {
                // Good performance - potentially decrease LOD level (higher quality)
                return Mathf.Max(baseLODLevel - 1, 0);
            }

            return baseLODLevel;
        }

        /// <summary>
        /// Set adaptive LOD enabled/disabled
        /// </summary>
        public void SetAdaptiveLODEnabled(bool enabled)
        {
            _enableAdaptiveLOD = enabled;

            if (!enabled)
            {
                _dynamicLODMultiplier = 1f;
            }

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Adaptive LOD: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set target frame time for adaptive adjustments
        /// </summary>
        public void SetTargetFrameTime(float targetFPS)
        {
            _targetFrameTime = 1f / targetFPS;
            UpdatePerformanceThresholds();

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Target frame time set to: {_targetFrameTime * 1000f:F2}ms ({targetFPS} FPS)", this);
        }

        /// <summary>
        /// Set adaptation speed
        /// </summary>
        public void SetAdaptationSpeed(float speed)
        {
            _adaptationSpeed = Mathf.Clamp(speed, 0.1f, 2f);

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Adaptation speed set to: {_adaptationSpeed}", this);
        }

        /// <summary>
        /// Reset adaptive system to defaults
        /// </summary>
        public void ResetAdaptiveSystem()
        {
            _dynamicLODMultiplier = 1f;
            _averageFrameTime = _targetFrameTime;
            _frameTimeSamples = 0;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "Adaptive system reset to defaults", this);
        }

        /// <summary>
        /// Get current adaptive quality level
        /// </summary>
        public AdaptiveQualityLevel GetCurrentQualityLevel()
        {
            if (_averageFrameTime <= _goodPerformanceThreshold)
                return AdaptiveQualityLevel.High;
            else if (_averageFrameTime >= _poorPerformanceThreshold)
                return AdaptiveQualityLevel.Low;
            else
                return AdaptiveQualityLevel.Medium;
        }

        /// <summary>
        /// Force specific LOD multiplier
        /// </summary>
        public void ForceLODMultiplier(float multiplier)
        {
            _dynamicLODMultiplier = Mathf.Clamp(multiplier, _minLODMultiplier, _maxLODMultiplier);
            OnLODMultiplierChanged?.Invoke(_dynamicLODMultiplier);

            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"LOD multiplier forced to: {_dynamicLODMultiplier:F2}", this);
        }

        private void UpdateFrameTimeTracking(float deltaTime)
        {
            float currentFrameTime = deltaTime;
            _averageFrameTime = (_averageFrameTime * _frameTimeSamples + currentFrameTime) / (_frameTimeSamples + 1);
            _frameTimeSamples = Mathf.Min(_frameTimeSamples + 1, 60); // Average over last 60 frames
        }

        private void UpdateLODMultiplier()
        {
            float previousMultiplier = _dynamicLODMultiplier;

            if (_averageFrameTime > _poorPerformanceThreshold)
            {
                // Performance is poor - reduce LOD multiplier (increase LOD distance)
                _dynamicLODMultiplier = Mathf.Max(
                    _dynamicLODMultiplier - _adaptationSpeed * Time.deltaTime,
                    _minLODMultiplier
                );
            }
            else if (_averageFrameTime < _goodPerformanceThreshold)
            {
                // Performance is good - increase LOD multiplier (decrease LOD distance)
                _dynamicLODMultiplier = Mathf.Min(
                    _dynamicLODMultiplier + _adaptationSpeed * 0.5f * Time.deltaTime,
                    _maxLODMultiplier
                );
            }

            // Notify if multiplier changed significantly
            if (Mathf.Abs(_dynamicLODMultiplier - previousMultiplier) > 0.05f)
            {
                OnLODMultiplierChanged?.Invoke(_dynamicLODMultiplier);

                var qualityLevel = GetCurrentQualityLevel();
                OnQualityLevelChanged?.Invoke(qualityLevel);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("LOD",
                        $"LOD multiplier adjusted: {previousMultiplier:F2} → {_dynamicLODMultiplier:F2} " +
                        $"(Quality: {qualityLevel}, Frame time: {_averageFrameTime * 1000f:F2}ms)", this);
                }
            }
        }

        private void UpdatePerformanceThresholds()
        {
            _poorPerformanceThreshold = _targetFrameTime * (1f + _performanceBuffer);
            _goodPerformanceThreshold = _targetFrameTime * (1f - _performanceBuffer);
        }

        /// <summary>
        /// Get adaptive system performance statistics
        /// </summary>
        public AdaptiveSystemStats GetPerformanceStats()
        {
            return new AdaptiveSystemStats
            {
                IsEnabled = _enableAdaptiveLOD,
                CurrentLODMultiplier = _dynamicLODMultiplier,
                TargetFrameTime = _targetFrameTime,
                AverageFrameTime = _averageFrameTime,
                CurrentQualityLevel = GetCurrentQualityLevel(),
                AdaptationSpeed = _adaptationSpeed,
                MinLODMultiplier = _minLODMultiplier,
                MaxLODMultiplier = _maxLODMultiplier
            };
        }
    }

    /// <summary>
    /// Adaptive quality levels
    /// </summary>
    public enum AdaptiveQualityLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Adaptive system performance statistics
    /// </summary>
    [System.Serializable]
    public struct AdaptiveSystemStats
    {
        public bool IsEnabled;
        public float CurrentLODMultiplier;
        public float TargetFrameTime;
        public float AverageFrameTime;
        public AdaptiveQualityLevel CurrentQualityLevel;
        public float AdaptationSpeed;
        public float MinLODMultiplier;
        public float MaxLODMultiplier;
    }
}