using ProjectChimera.Core.Logging;
using UnityEngine;
using System;
using System.Collections;
using ProjectChimera.Core.Events;
using ProjectChimera.Core.Updates;
using ProjectChimera.Shared;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Time Manager - orchestrates all time-related components
    /// Maintains original interface while using modular components
    /// Refactored from monolithic 1,066-line class into focused components
    /// </summary>
    public class TimeManager : DIChimeraManager, IGameStateListener, IPausable, ITickable
    {
        [Header("Time Configuration")]
        [SerializeField] private TimeConfigSO _timeConfig;
        [SerializeField] private bool _enableOfflineProgression = true;
        [SerializeField] private TimeSpeedLevel _defaultSpeedLevel = TimeSpeedLevel.Normal;

        [Header("Time Scale Events")]
        [SerializeField] private FloatGameEventSO _onTimeScaleChanged;
        [SerializeField] private SimpleGameEventSO _onTimePaused;
        [SerializeField] private SimpleGameEventSO _onTimeResumed;
        [SerializeField] private FloatGameEventSO _onOfflineProgressionCalculated;
        [SerializeField] private SimpleGameEventSO _onSpeedPenaltyChanged;

        [Header("Debug Settings")]
        [SerializeField] private bool _enableTimeDebug = false;

        // Time components
        private ITimeScale _timeScale;
        private IOfflineProgression _offlineProgression;
        private ITimeEvents _timeEvents;
        private ISaveTime _saveTime;

        // Pause state
        private bool _isTimePaused = false;
        private bool _wasTimeScaledBeforePause = false;
        private TimeSpeedLevel _speedLevelBeforePause = TimeSpeedLevel.Normal;

        #region Public Properties

        public TimeSpeedLevel CurrentSpeedLevel => _timeScale?.CurrentSpeedLevel ?? TimeSpeedLevel.Normal;
        public float CurrentTimeScale => _timeScale?.CurrentTimeScale ?? 1.0f;
        public float CurrentPenaltyMultiplier => _timeScale?.CurrentPenaltyMultiplier ?? 1.0f;
        public bool HasSpeedPenalty => _timeScale?.HasSpeedPenalty ?? false;
        public bool SpeedPenaltiesEnabled => _timeScale?.SpeedPenaltiesEnabled ?? false;
        public bool IsTimePaused => _isTimePaused;
        public bool IsPaused => _isTimePaused;
        public DateTime SessionStartTime => _saveTime?.SessionStartTime ?? DateTime.Now;
        public DateTime GameStartTime => _saveTime?.GameStartTime ?? DateTime.Now;
        public TimeSpan TotalGameTime => _saveTime?.TotalGameTime ?? TimeSpan.Zero;
        public TimeSpan SessionDuration => _saveTime?.SessionDuration ?? TimeSpan.Zero;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        protected override void OnManagerInitialize()
        {
            LogDebug("Initializing Time Manager");

            try
            {
                InitializeAllComponents();
                ConfigureEventReferences();

                // Initialize game start time (this would normally be loaded from save data)
                _saveTime.SetGameStartTime(DateTime.Now);
                _saveTime.SetSessionStartTime(DateTime.Now);

                // Calculate offline progression if enabled
                if (_enableOfflineProgression)
                {
                    StartCoroutine(_offlineProgression.CalculateOfflineProgressionCoroutine());
                }

                LogDebug("Time Manager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize Time Manager: {ex.Message}");
            }
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("Shutting down Time Manager");

            try
            {
                _timeScale = null;
                _offlineProgression?.Shutdown();
                _timeEvents?.Shutdown();
                _saveTime?.Shutdown();
            }
            catch (Exception ex)
            {
                LogError($"Error during Time Manager shutdown: {ex.Message}");
            }
        }

        #endregion

        #region ITickable Implementation

        public int Priority => TickPriority.TimeManager;
        public bool Enabled => true;

        public void Tick(float deltaTime)
        {
            if (_saveTime != null)
            {
                _saveTime.UpdateAccumulatedTimes(deltaTime, CurrentTimeScale, _isTimePaused);
            }
        }

        public void OnRegistered()
        {
            // Called when registered with UpdateOrchestrator
        }

        public void OnUnregistered()
        {
            // Called when unregistered from UpdateOrchestrator
        }

        #endregion

        #region IGameStateListener Implementation

        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Paused:
                    Pause();
                    break;
                case GameState.InGame:
                    Resume();
                    break;
                case GameState.MainMenu:
                    // Might want to pause or reset time here
                    break;
            }
        }

        #endregion

        #region IPausable Implementation

        public void OnPause()
        {
            Pause();
        }

        public void OnResume()
        {
            Resume();
        }

        #endregion

        #region Time Scale Management

        public void SetSpeedLevel(TimeSpeedLevel newSpeedLevel)
        {
            _timeScale?.SetSpeedLevel(newSpeedLevel);
        }

        public void IncreaseSpeedLevel()
        {
            _timeScale?.IncreaseSpeedLevel();
        }

        public void DecreaseSpeedLevel()
        {
            _timeScale?.DecreaseSpeedLevel();
        }

        public void ResetSpeedLevel()
        {
            _timeScale?.ResetSpeedLevel();
        }

        public void Pause()
        {
            if (_isTimePaused) return;

            _wasTimeScaledBeforePause = CurrentSpeedLevel != TimeSpeedLevel.Normal;
            _speedLevelBeforePause = CurrentSpeedLevel;
            _isTimePaused = true;

            LogDebug("Time paused");
            _timeEvents?.TriggerTimePaused();
        }

        public void Resume()
        {
            if (!_isTimePaused) return;

            _isTimePaused = false;

            if (_wasTimeScaledBeforePause)
            {
                SetSpeedLevel(_speedLevelBeforePause);
            }

            LogDebug($"Time resumed at {CurrentTimeScale}x scale");
            _timeEvents?.TriggerTimeResumed();
        }

        #endregion

        #region Time Calculations

        public float RealTimeToGameTime(float realTime)
        {
            return _timeScale?.RealTimeToGameTime(realTime) ?? realTime;
        }

        public float GameTimeToRealTime(float gameTime)
        {
            return _timeScale?.GameTimeToRealTime(gameTime) ?? gameTime;
        }

        public float GetScaledDeltaTime()
        {
            return _timeScale?.GetScaledDeltaTime() ?? Time.unscaledDeltaTime;
        }

        public float ApplySpeedPenalty(float baseValue)
        {
            return _timeScale?.ApplySpeedPenalty(baseValue) ?? baseValue;
        }

        public (float min, float max) ApplySpeedPenaltyToRange(float minValue, float maxValue)
        {
            return _timeScale?.ApplySpeedPenaltyToRange(minValue, maxValue) ?? (minValue, maxValue);
        }

        #endregion

        #region Display Methods

        public string GetSpeedLevelDisplayString()
        {
            return _timeScale?.GetSpeedLevelDisplayString() ?? "1x (Normal)";
        }

        public string GetPenaltyDescription()
        {
            return _timeScale?.GetPenaltyDescription() ?? "No penalty";
        }

        public int GetPenaltyPercentage()
        {
            return _timeScale?.GetPenaltyPercentage() ?? 0;
        }

        public string GetGameTimeString()
        {
            return _saveTime?.GetGameTimeString() ?? "0s";
        }

        public string GetRealTimeString()
        {
            return _saveTime?.GetRealTimeString() ?? "0s";
        }

        public string GetCombinedTimeString()
        {
            return _saveTime?.GetCombinedTimeString() ?? "Game: 0s | Real: 0s";
        }

        public string GetTimeEfficiencyString()
        {
            return _saveTime?.GetTimeEfficiencyString() ?? "1:1";
        }

        public string GetCompactTimeStatus()
        {
            return _saveTime?.GetCompactTimeStatus() ?? "0s";
        }

        public string GetDetailedTimeStatus()
        {
            return _saveTime?.GetDetailedTimeStatus() ?? "";
        }

        public TimeDisplayData GetTimeDisplayData()
        {
            return _saveTime?.GetTimeDisplayData() ?? new TimeDisplayData();
        }

        public string GetEstimatedRealTime(float gameTimeSeconds)
        {
            return _timeScale?.GetEstimatedRealTime(gameTimeSeconds) ?? "Unknown";
        }

        #endregion

        #region Listener Management

        public void RegisterTimeScaleListener(ITimeScaleListener listener)
        {
            _timeEvents?.RegisterTimeScaleListener(listener);
        }

        public void UnregisterTimeScaleListener(ITimeScaleListener listener)
        {
            _timeEvents?.UnregisterTimeScaleListener(listener);
        }

        public void RegisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            _offlineProgression?.RegisterOfflineProgressionListener(listener);
        }

        public void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            _offlineProgression?.UnregisterOfflineProgressionListener(listener);
        }

        public void RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            _timeEvents?.RegisterSpeedPenaltyListener(listener);
        }

        public void UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            _timeEvents?.UnregisterSpeedPenaltyListener(listener);
        }

        #endregion

        #region Testing Support

        public bool CanTriggerOfflineEvents()
        {
            return _offlineProgression?.CanTriggerOfflineEvents() ?? false;
        }

        public bool CanCalculateOfflineTime()
        {
            return _offlineProgression?.CanCalculateOfflineTime() ?? false;
        }

        public int GetOfflineProgressionListenerCount()
        {
            return _offlineProgression?.GetOfflineProgressionListenerCount() ?? 0;
        }

        public void TriggerOfflineProgressionForTesting(float offlineHours)
        {
            _offlineProgression?.TriggerOfflineProgressionForTesting(offlineHours);
        }

        #endregion

        #region Static Formatting Methods

        public static string FormatDuration(float seconds)
        {
            return FormatDuration(seconds, TimeDisplayFormat.Adaptive);
        }

        public static string FormatDuration(float seconds, TimeDisplayFormat format)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            
            switch (format)
            {
                case TimeDisplayFormat.Compact:
                    if (duration.TotalDays >= 1)
                        return $"{(int)duration.TotalDays}d {duration.Hours}h";
                    else if (duration.TotalHours >= 1)
                        return $"{duration.Hours}h {duration.Minutes}m";
                    else if (duration.TotalMinutes >= 1)
                        return $"{duration.Minutes}m";
                    else
                        return $"{duration.Seconds}s";

                case TimeDisplayFormat.Detailed:
                    if (duration.TotalHours >= 1)
                        return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
                    else if (duration.TotalMinutes >= 1)
                        return $"{duration.Minutes}m {duration.Seconds:D2}s";
                    else
                        return $"{duration.Seconds}s";

                case TimeDisplayFormat.Precise:
                    return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m {duration.Seconds:D2}s";

                case TimeDisplayFormat.Adaptive:
                default:
                    if (duration.TotalDays >= 1)
                        return $"{(int)duration.TotalDays}d {duration.Hours:D2}h {duration.Minutes:D2}m";
                    else if (duration.TotalHours >= 1)
                        return $"{duration.Hours}h {duration.Minutes:D2}m";
                    else if (duration.TotalMinutes >= 1)
                        return $"{duration.Minutes}m {duration.Seconds:D2}s";
                    else
                        return $"{duration.Seconds}s";
            }
        }

        #endregion

        #region Component Initialization

        private void InitializeComponents()
        {
            // Create time components
            _timeEvents = new TimeEvents();
            _timeScale = new TimeScale(_timeEvents);
            _offlineProgression = new OfflineProgression();
            _saveTime = new SaveTime();
        }

        private void InitializeAllComponents()
        {
            // Initialize all components
            _timeEvents.Initialize();
            _timeScale.SetSpeedLevel(_defaultSpeedLevel);
            _offlineProgression.EnableOfflineProgression = _enableOfflineProgression;
            _offlineProgression.Initialize();
            _saveTime.Initialize();

            LogDebug("All time components initialized");
        }

        private void ConfigureEventReferences()
        {
            // Configure event references
            _timeEvents.SetTimeScaleEvent(_onTimeScaleChanged);
            _timeEvents.SetTimePausedEvent(_onTimePaused);
            _timeEvents.SetTimeResumedEvent(_onTimeResumed);
            _timeEvents.SetSpeedPenaltyEvent(_onSpeedPenaltyChanged);
            
            (_offlineProgression as OfflineProgression)?.SetOfflineProgressionEvent(_onOfflineProgressionCalculated);
        }

        #endregion

        #region Logging Helpers

        private void LogDebug(string message)
        {
            if (_enableTimeDebug)
                ChimeraLogger.Log($"[TimeManager] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[TimeManager] {message}");
        }

        #endregion
    }
}
