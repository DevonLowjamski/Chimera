using UnityEngine;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core;

namespace ProjectChimera.Core
{
    /// <summary>
    /// ENHANCED: Time manager for Project Chimera with ITickable integration.
    /// Migrated from Update() to centralized tick system for better performance.
    /// Focuses on essential time control without complex offline progression and speed penalties.
    /// </summary>
    public class TimeManager : MonoBehaviour, Updates.ITickable, ITimeManager
    {
        [Header("Basic Time Settings")]
        [SerializeField] private bool _enableTimeControl = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _defaultTimeScale = 1.0f;
        [SerializeField] private float _maxTimeScale = 4.0f;

        // Basic time tracking
        private float _currentTimeScale = 1.0f;
        private TimeSpeedLevel _currentSpeedLevel = TimeSpeedLevel.Normal;

        // ITickable implementation
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
        /// ITickable implementation - high priority for core time management
        /// </summary>
        public int TickPriority => -90; // High priority core system
        public bool IsTickable => _isInitialized && isActiveAndEnabled;
        public bool IsActive => _isInitialized && isActiveAndEnabled;

        /// <summary>
        /// Initialize basic time manager and register with UpdateOrchestrator
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _currentTimeScale = _defaultTimeScale;
            _sessionStartTime = Time.realtimeSinceStartup;
            _isInitialized = true;

            // Register with centralized update system
            var orchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.RegisterTickable(this);
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("TimeManager", "$1");
                }
            }


            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("TimeManager", "$1");
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
                    ChimeraLogger.LogInfo("TimeManager", "$1");
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
                    ChimeraLogger.LogInfo("TimeManager", "$1");
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
        /// ITickable implementation - track paused time for accurate session time calculation
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Track paused time using unscaled delta time for accuracy
            if (_isPaused)
            {
                _totalPausedTime += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Unity lifecycle - ensure proper cleanup
        /// </summary>
        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator if available
            var orchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.UnregisterTickable(this);
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("TimeManager", "$1");
                }
            }
        }
    // Explicit ITimeManager implementation to satisfy DI registrations
    float ITimeManager.TimeScale
    {
        get => _currentTimeScale;
        set => SetTimeScale(value);
    }

    bool ITimeManager.IsPaused => _isPaused;

    DateTime ITimeManager.CurrentGameTime => DateTime.Now;

    TimeSpan ITimeManager.ElapsedGameTime => TimeSpan.FromSeconds(GetSessionPlayTime());

    void ITimeManager.Initialize() => Initialize();

    void ITimeManager.Pause() => SetPaused(true);

    void ITimeManager.Resume() => SetPaused(false);

    bool ITimeManager.SetTimeScale(float scale)
    {
        SetTimeScale(scale);
        return true;
    }

    string ITimeManager.FormatCurrentTime(TimeDisplayFormat format)
    {
        return DateTime.Now.ToString();
    }

    void ITimeManager.RegisterOfflineProgressionListener(IOfflineProgressionListener listener) { }
    void ITimeManager.UnregisterOfflineProgressionListener(IOfflineProgressionListener listener) { }
    void ITimeManager.RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener) { }
    void ITimeManager.UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener) { }

    // New interface members
    TimeSpeedLevel ITimeManager.CurrentSpeedLevel => _currentSpeedLevel;
    float ITimeManager.CurrentTimeScale => _currentTimeScale;
    bool ITimeManager.IsTimePaused => _isPaused;

    void ITimeManager.SetSpeedLevel(TimeSpeedLevel speedLevel)
    {
        _currentSpeedLevel = speedLevel;

        // Convert speed level to time scale based on the enum values
        float newTimeScale = speedLevel switch
        {
            TimeSpeedLevel.Slow => 0.5f,        // Slow = 0
            TimeSpeedLevel.Normal => 1.0f,      // Normal = 1
            TimeSpeedLevel.Fast => 2.0f,        // Fast = 2
            TimeSpeedLevel.VeryFast => 4.0f,    // VeryFast = 4
            TimeSpeedLevel.Maximum => 8.0f,     // Maximum = 8
            _ => 1.0f
        };

        SetTimeScale(newTimeScale);

        if (_enableLogging)
        {
            ChimeraLogger.LogInfo("TimeManager", $"Speed level changed to {speedLevel} (scale: {newTimeScale}x)");
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
