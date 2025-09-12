using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// BASIC: Simple time manager for Project Chimera.
    /// Focuses on essential time control without complex offline progression and speed penalties.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        [Header("Basic Time Settings")]
        [SerializeField] private bool _enableTimeControl = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _defaultTimeScale = 1.0f;
        [SerializeField] private float _maxTimeScale = 4.0f;

        // Basic time tracking
        private float _currentTimeScale = 1.0f;
        private bool _isPaused = false;
        private float _sessionStartTime;
        private float _totalPausedTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for time changes
        /// </summary>
        public event System.Action<float> OnTimeScaleChanged;
        public event System.Action<bool> OnPauseStateChanged;

        /// <summary>
        /// Initialize basic time manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _currentTimeScale = _defaultTimeScale;
            _sessionStartTime = Time.realtimeSinceStartup;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[TimeManager] Initialized successfully");
            }
        }

        /// <summary>
        /// Set time scale
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            if (!_enableTimeControl || !_isInitialized) return;

            timeScale = Mathf.Clamp(timeScale, 0.1f, _maxTimeScale);
            if (timeScale != _currentTimeScale)
            {
                _currentTimeScale = timeScale;
                Time.timeScale = _isPaused ? 0f : _currentTimeScale;

                OnTimeScaleChanged?.Invoke(_currentTimeScale);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[TimeManager] Time scale set to {_currentTimeScale:F2}");
                }
            }
        }

        /// <summary>
        /// Pause/unpause time
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (!_enableTimeControl || !_isInitialized) return;

            if (paused != _isPaused)
            {
                _isPaused = paused;

                if (_isPaused)
                {
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = _currentTimeScale;
                }

                OnPauseStateChanged?.Invoke(_isPaused);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[TimeManager] Time {(paused ? "paused" : "resumed")}");
                }
            }
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!_isPaused);
        }

        /// <summary>
        /// Reset time scale to default
        /// </summary>
        public void ResetTimeScale()
        {
            SetTimeScale(_defaultTimeScale);
        }

        /// <summary>
        /// Speed up time
        /// </summary>
        public void SpeedUp(float multiplier = 2f)
        {
            SetTimeScale(_currentTimeScale * multiplier);
        }

        /// <summary>
        /// Slow down time
        /// </summary>
        public void SlowDown(float divisor = 2f)
        {
            SetTimeScale(_currentTimeScale / divisor);
        }

        /// <summary>
        /// Get current time scale
        /// </summary>
        public float GetCurrentTimeScale()
        {
            return _currentTimeScale;
        }

        /// <summary>
        /// Check if time is paused
        /// </summary>
        public bool IsPaused()
        {
            return _isPaused;
        }

        /// <summary>
        /// Get session play time (excluding paused time)
        /// </summary>
        public float GetSessionPlayTime()
        {
            if (!_isInitialized) return 0f;

            float totalTime = Time.realtimeSinceStartup - _sessionStartTime;
            return totalTime - _totalPausedTime;
        }

        /// <summary>
        /// Get time statistics
        /// </summary>
        public TimeStats GetStats()
        {
            return new TimeStats
            {
                CurrentTimeScale = _currentTimeScale,
                IsPaused = _isPaused,
                SessionPlayTime = GetSessionPlayTime(),
                MaxTimeScale = _maxTimeScale,
                DefaultTimeScale = _defaultTimeScale,
                IsTimeControlEnabled = _enableTimeControl,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Force update paused time tracking
        /// </summary>
        private void Update()
        {
            // Track paused time for accurate session time calculation
            if (_isPaused)
            {
                _totalPausedTime += Time.unscaledDeltaTime;
            }
        }
    }

    /// <summary>
    /// Time statistics
    /// </summary>
    [System.Serializable]
    public struct TimeStats
    {
        public float CurrentTimeScale;
        public bool IsPaused;
        public float SessionPlayTime;
        public float MaxTimeScale;
        public float DefaultTimeScale;
        public bool IsTimeControlEnabled;
        public bool IsInitialized;
    }
}
