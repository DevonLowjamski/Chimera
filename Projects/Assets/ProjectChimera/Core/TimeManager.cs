using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Events;
using ProjectChimera.Shared;


namespace ProjectChimera.Core
{
    /// <summary>
    /// Discrete time speed levels for game progression
    /// </summary>
    public enum TimeSpeedLevel
    {
        Slow = 0,      // 0.5x speed
        Normal = 1,    // 1x speed  
        Fast = 2,      // 2x speed
        VeryFast = 3,  // 4x speed
        Maximum = 4    // 8x speed
    }

    /// <summary>
    /// Time display format options for UI systems
    /// </summary>
    public enum TimeDisplayFormat
    {
        Adaptive,      // Automatically choose best format based on duration
        Compact,       // Shortest possible format (e.g., "2h 15m")
        Detailed,      // Include seconds when relevant (e.g., "2h 15m 30s")
        Precise        // Always show seconds (e.g., "2h 15m 30s")
    }

    /// <summary>
    /// Manages game time acceleration, offline progression, and coordinates all time-dependent systems.
    /// Critical for Project Chimera's simulation mechanics where plants grow over real-world time.
    /// </summary>
    public class TimeManager : DIChimeraManager, IGameStateListener, IPausable
    {

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

        public void OnPause()
        {
            Pause();
        }

        public void OnResume()
        {
            Resume();
        }



        public bool IsPaused => _isTimePaused;
        // Speed level to multiplier mapping
        private static readonly float[] SpeedMultipliers = { 0.5f, 1.0f, 2.0f, 4.0f, 8.0f };
        
        // Risk/reward penalty multipliers for each speed level
        // Higher speeds reduce genetic potential, yield quality, and increase failure rates
        private static readonly float[] PenaltyMultipliers = { 1.1f, 1.0f, 0.95f, 0.85f, 0.7f };
        
        // Event for notifying systems about speed tier changes for penalty application
        [SerializeField] private SimpleGameEventSO _onSpeedPenaltyChanged;
        
        // Risk/reward system settings
        private bool _enableSpeedPenalties = true;
        private float _penaltySeverityMultiplier = 1.0f;
        
        // UI display settings
        private bool _showCombinedTimeDisplay = true;
        private bool _showTimeEfficiencyRatio = true;
        private bool _showPenaltyInformation = true;
        private TimeDisplayFormat _timeFormat = TimeDisplayFormat.Adaptive;
        [Header("Time Configuration")]
        [SerializeField] private TimeConfigSO _timeConfig;
        [SerializeField] private bool _enableOfflineProgression = true;
        [SerializeField] private TimeSpeedLevel _defaultSpeedLevel = TimeSpeedLevel.Normal;

        [Header("Time Scale Events")]
        [SerializeField] private FloatGameEventSO _onTimeScaleChanged;
        [SerializeField] private SimpleGameEventSO _onTimePaused;
        [SerializeField] private SimpleGameEventSO _onTimeResumed;
        [SerializeField] private FloatGameEventSO _onOfflineProgressionCalculated;

        [Header("Debug Settings")]
        [SerializeField] private bool _enableTimeDebug = false;

        // Time tracking
        private DateTime _gameStartTime;
        private DateTime _lastSaveTime;
        private DateTime _sessionStartTime;
        private TimeSpeedLevel _currentSpeedLevel = TimeSpeedLevel.Normal;
        private float _currentTimeScale = 1.0f;
        private bool _isTimePaused = false;
        private bool _wasTimeScaledBeforePause = false;
        private TimeSpeedLevel _speedLevelBeforePause = TimeSpeedLevel.Normal;

        // Performance tracking
        private float _accumulatedGameTime = 0.0f;
        private float _accumulatedRealTime = 0.0f;
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private const int MAX_FRAME_HISTORY = 60;

        // Time listeners
        private readonly List<ITimeScaleListener> _timeScaleListeners = new List<ITimeScaleListener>();
        private readonly List<IOfflineProgressionListener> _offlineProgressionListeners = new List<IOfflineProgressionListener>();
        private readonly List<ISpeedPenaltyListener> _speedPenaltyListeners = new List<ISpeedPenaltyListener>();

        /// <summary>
        /// Current time speed level.
        /// </summary>
        public TimeSpeedLevel CurrentSpeedLevel 
        { 
            get => _currentSpeedLevel; 
            private set 
            {
                _currentSpeedLevel = value;
                _currentTimeScale = SpeedMultipliers[(int)value];
            }
        }

        /// <summary>
        /// Current time scale multiplier affecting all game time calculations.
        /// </summary>
        public float CurrentTimeScale => _currentTimeScale;

        /// <summary>
        /// Current penalty multiplier applied to simulation outcomes based on speed level.
        /// Values below 1.0 indicate penalties, values above 1.0 indicate bonuses.
        /// </summary>
        public float CurrentPenaltyMultiplier 
        { 
            get
            {
                if (!_enableSpeedPenalties) return 1.0f;
                
                float basePenalty = PenaltyMultipliers[(int)CurrentSpeedLevel];
                // Apply severity multiplier (values further from 1.0 are more affected)
                float deviation = basePenalty - 1.0f;
                return 1.0f + (deviation * _penaltySeverityMultiplier);
            }
        }

        /// <summary>
        /// Whether the current speed level applies a penalty to simulation outcomes.
        /// </summary>
        public bool HasSpeedPenalty => _enableSpeedPenalties && CurrentSpeedLevel > TimeSpeedLevel.Normal;

        /// <summary>
        /// Whether the speed penalty system is enabled.
        /// </summary>
        public bool SpeedPenaltiesEnabled => _enableSpeedPenalties;

        /// <summary>
        /// Whether time progression is currently paused.
        /// </summary>
        public bool IsTimePaused => _isTimePaused;

        /// <summary>
        /// Time when the current game session started.
        /// </summary>
        public DateTime SessionStartTime => _sessionStartTime;

        /// <summary>
        /// Time when the game world was first created.
        /// </summary>
        public DateTime GameStartTime => _gameStartTime;

        /// <summary>
        /// Total real-world time the game has been running.
        /// </summary>
        public TimeSpan TotalGameTime => DateTime.Now - _gameStartTime;

        /// <summary>
        /// Current session duration.
        /// </summary>
        public TimeSpan SessionDuration => DateTime.Now - _sessionStartTime;

        /// <summary>
        /// Accelerated game time (affected by time scale).
        /// </summary>
        public float AcceleratedGameTime => _accumulatedGameTime;

        /// <summary>
        /// Average frame time over the last 60 frames.
        /// </summary>
        public float AverageFrameTime
        {
            get
            {
                if (_frameTimeHistory.Count == 0) return 0.0f;
                float sum = 0.0f;
                foreach (float frameTime in _frameTimeHistory)
                {
                    sum += frameTime;
                }
                return sum / _frameTimeHistory.Count;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _sessionStartTime = DateTime.Now;
        }

        protected override void OnManagerInitialize()
        {
            LogDebug("Initializing Time Manager");

            // Load time configuration
            if (_timeConfig != null)
            {
                _defaultSpeedLevel = _timeConfig.DefaultSpeedLevel;
                _enableOfflineProgression = _timeConfig.EnableOfflineProgression;
                _enableSpeedPenalties = _timeConfig.EnableSpeedPenalties;
                _penaltySeverityMultiplier = _timeConfig.PenaltySeverityMultiplier;
                _showCombinedTimeDisplay = _timeConfig.ShowCombinedTimeDisplay;
                _showTimeEfficiencyRatio = _timeConfig.ShowTimeEfficiencyRatio;
                _showPenaltyInformation = _timeConfig.ShowPenaltyInformation;
                _timeFormat = _timeConfig.TimeFormat;
            }

            // Set initial speed level
            SetSpeedLevel(_defaultSpeedLevel);

            // Initialize game start time (this would normally be loaded from save data)
            _gameStartTime = DateTime.Now;
            _lastSaveTime = DateTime.Now;

            // Calculate offline progression if enabled
            if (_enableOfflineProgression)
            {
                StartCoroutine(CalculateOfflineProgressionCoroutine());
            }

            LogDebug($"Time Manager initialized - Time Scale: {CurrentTimeScale}x");
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("Shutting down Time Manager");

            // Record shutdown time for offline progression
            _lastSaveTime = DateTime.Now;

            // Clear listeners
            _timeScaleListeners.Clear();
            _offlineProgressionListeners.Clear();
            _speedPenaltyListeners.Clear();
            _frameTimeHistory.Clear();
        }

        private new void Update()
        {
            if (!IsInitialized) return;

            // Track performance
            TrackFrameTime();

            // Update accelerated game time
            if (!_isTimePaused)
            {
                float deltaTime = Time.unscaledDeltaTime;
                _accumulatedGameTime += deltaTime * CurrentTimeScale;
                _accumulatedRealTime += deltaTime;
            }

            // Debug output
            if (_enableTimeDebug && Time.frameCount % 60 == 0) // Every 60 frames
            {
                LogDebug($"Time Scale: {CurrentTimeScale}x | Game Time: {_accumulatedGameTime:F1}s | Real Time: {_accumulatedRealTime:F1}s");
            }
        }

        /// <summary>
        /// Sets the time speed level for the game world.
        /// </summary>
        public void SetSpeedLevel(TimeSpeedLevel newSpeedLevel)
        {
            TimeSpeedLevel previousSpeedLevel = CurrentSpeedLevel;
            float previousTimeScale = CurrentTimeScale;
            
            CurrentSpeedLevel = newSpeedLevel;

            if (!_isTimePaused)
            {
                Time.timeScale = CurrentTimeScale;
            }

            LogDebug($"Time speed changed: {previousSpeedLevel} ({previousTimeScale}x) -> {CurrentSpeedLevel} ({CurrentTimeScale}x)");
            LogDebug($"Speed penalty multiplier: {CurrentPenaltyMultiplier:F2}x");

            // Notify listeners
            NotifyTimeScaleListeners(previousTimeScale, CurrentTimeScale);
            _onTimeScaleChanged?.Raise(CurrentTimeScale);
            
            // Notify systems about penalty changes
            if (previousSpeedLevel != CurrentSpeedLevel)
            {
                NotifySpeedPenaltyListeners(CurrentSpeedLevel, CurrentPenaltyMultiplier);
                _onSpeedPenaltyChanged?.Raise();
            }
        }

        /// <summary>
        /// Increases speed level by one step (if possible).
        /// </summary>
        public void IncreaseSpeedLevel()
        {
            if (CurrentSpeedLevel < TimeSpeedLevel.Maximum)
            {
                SetSpeedLevel(CurrentSpeedLevel + 1);
            }
        }

        /// <summary>
        /// Decreases speed level by one step (if possible).
        /// </summary>
        public void DecreaseSpeedLevel()
        {
            if (CurrentSpeedLevel > TimeSpeedLevel.Slow)
            {
                SetSpeedLevel(CurrentSpeedLevel - 1);
            }
        }

        /// <summary>
        /// Resets speed level to the default value.
        /// </summary>
        public void ResetSpeedLevel()
        {
            SetSpeedLevel(_defaultSpeedLevel);
        }

        /// <summary>
        /// Pauses time progression.
        /// </summary>
        public void Pause()
        {
            if (_isTimePaused) return;

            _isTimePaused = true;
            _speedLevelBeforePause = CurrentSpeedLevel;
            _wasTimeScaledBeforePause = CurrentSpeedLevel != TimeSpeedLevel.Normal;

            Time.timeScale = 0.0f;

            LogDebug("Time paused");
            _onTimePaused?.Raise();
        }

        /// <summary>
        /// Resumes time progression.
        /// </summary>
        public void Resume()
        {
            if (!_isTimePaused) return;

            _isTimePaused = false;
            Time.timeScale = CurrentTimeScale;

            LogDebug($"Time resumed at {CurrentTimeScale}x scale");
            _onTimeResumed?.Raise();
        }

        /// <summary>
        /// Calculates offline progression since last save.
        /// </summary>
        private IEnumerator CalculateOfflineProgressionCoroutine()
        {
            yield return new WaitForEndOfFrame(); // Wait for other systems to initialize

            // This would normally load the last save time from save data
            // For now, we'll simulate no offline time
            DateTime lastPlayTime = _lastSaveTime;
            DateTime currentTime = DateTime.Now;
            TimeSpan offlineTime = currentTime - lastPlayTime;

            if (offlineTime.TotalMinutes > 1.0) // Only calculate if offline for more than 1 minute
            {
                LogDebug($"Calculating offline progression for {offlineTime.TotalHours:F2} hours");

                float offlineHours = (float)offlineTime.TotalHours;
                
                // Notify offline progression listeners
                NotifyOfflineProgressionListeners(offlineHours);
                _onOfflineProgressionCalculated?.Raise(offlineHours);

                LogDebug($"Offline progression calculated: {offlineHours:F2} hours processed");
            }
            else
            {
                LogDebug("No significant offline time detected");
            }
        }

        /// <summary>
        /// Tracks frame time for performance monitoring.
        /// </summary>
        private void TrackFrameTime()
        {
            _frameTimeHistory.Enqueue(Time.unscaledDeltaTime);
            
            if (_frameTimeHistory.Count > MAX_FRAME_HISTORY)
            {
                _frameTimeHistory.Dequeue();
            }
        }

        /// <summary>
        /// Registers a listener for time scale changes.
        /// </summary>
        public void RegisterTimeScaleListener(ITimeScaleListener listener)
        {
            if (listener != null && !_timeScaleListeners.Contains(listener))
            {
                _timeScaleListeners.Add(listener);
                LogDebug($"Registered time scale listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Unregisters a time scale listener.
        /// </summary>
        public void UnregisterTimeScaleListener(ITimeScaleListener listener)
        {
            if (_timeScaleListeners.Remove(listener))
            {
                LogDebug($"Unregistered time scale listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Registers a listener for offline progression events.
        /// </summary>
        public void RegisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (listener != null && !_offlineProgressionListeners.Contains(listener))
            {
                _offlineProgressionListeners.Add(listener);
                LogDebug($"Registered offline progression listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Unregisters an offline progression listener.
        /// </summary>
        public void UnregisterOfflineProgressionListener(IOfflineProgressionListener listener)
        {
            if (_offlineProgressionListeners.Remove(listener))
            {
                LogDebug($"Unregistered offline progression listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Registers a listener for speed penalty changes.
        /// </summary>
        public void RegisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            if (listener != null && !_speedPenaltyListeners.Contains(listener))
            {
                _speedPenaltyListeners.Add(listener);
                LogDebug($"Registered speed penalty listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Unregisters a speed penalty listener.
        /// </summary>
        public void UnregisterSpeedPenaltyListener(ISpeedPenaltyListener listener)
        {
            if (_speedPenaltyListeners.Remove(listener))
            {
                LogDebug($"Unregistered speed penalty listener: {listener.GetType().Name}");
            }
        }

        /// <summary>
        /// Notifies all time scale listeners of a change.
        /// </summary>
        private void NotifyTimeScaleListeners(float previousScale, float newScale)
        {
            for (int i = _timeScaleListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _timeScaleListeners[i]?.OnTimeScaleChanged(previousScale, newScale);
                }
                catch (Exception e)
                {
                    LogError($"Error notifying time scale listener: {e.Message}");
                    _timeScaleListeners.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Notifies all offline progression listeners.
        /// </summary>
        private void NotifyOfflineProgressionListeners(float offlineHours)
        {
            for (int i = _offlineProgressionListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _offlineProgressionListeners[i]?.OnOfflineProgressionCalculated(offlineHours);
                }
                catch (Exception e)
                {
                    LogError($"Error notifying offline progression listener: {e.Message}");
                    _offlineProgressionListeners.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Notifies all speed penalty listeners.
        /// </summary>
        private void NotifySpeedPenaltyListeners(TimeSpeedLevel speedLevel, float penaltyMultiplier)
        {
            for (int i = _speedPenaltyListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _speedPenaltyListeners[i]?.OnSpeedPenaltyChanged(speedLevel, penaltyMultiplier);
                }
                catch (Exception e)
                {
                    LogError($"Error notifying speed penalty listener: {e.Message}");
                    _speedPenaltyListeners.RemoveAt(i);
                }
            }
        }



        /// <summary>
        /// Converts real-world time to accelerated game time.
        /// </summary>
        public float RealTimeToGameTime(float realTime)
        {
            return realTime * CurrentTimeScale;
        }

        /// <summary>
        /// Converts accelerated game time to real-world time.
        /// </summary>
        public float GameTimeToRealTime(float gameTime)
        {
            return gameTime / CurrentTimeScale;
        }

        /// <summary>
        /// Gets the time scale-adjusted delta time for frame-rate independent calculations.
        /// </summary>
        public float GetScaledDeltaTime()
        {
            return Time.unscaledDeltaTime * CurrentTimeScale;
        }

        /// <summary>
        /// Gets the current speed level as a formatted string for UI display.
        /// </summary>
        public string GetSpeedLevelDisplayString()
        {
            return CurrentSpeedLevel switch
            {
                TimeSpeedLevel.Slow => "0.5x (Slow)",
                TimeSpeedLevel.Normal => "1x (Normal)", 
                TimeSpeedLevel.Fast => "2x (Fast)",
                TimeSpeedLevel.VeryFast => "4x (Very Fast)",
                TimeSpeedLevel.Maximum => "8x (Maximum)",
                _ => $"{CurrentTimeScale:F1}x"
            };
        }

        /// <summary>
        /// Applies the current speed penalty to a value (typically genetic potential, yield quality, etc.).
        /// </summary>
        /// <param name="baseValue">The base value to apply penalty to</param>
        /// <returns>Value with speed penalty applied</returns>
        public float ApplySpeedPenalty(float baseValue)
        {
            return baseValue * CurrentPenaltyMultiplier;
        }

        /// <summary>
        /// Applies the current speed penalty to a range of values.
        /// </summary>
        /// <param name="minValue">Minimum value in range</param>
        /// <param name="maxValue">Maximum value in range</param>
        /// <returns>Tuple with penalty-adjusted range</returns>
        public (float min, float max) ApplySpeedPenaltyToRange(float minValue, float maxValue)
        {
            float penalty = CurrentPenaltyMultiplier;
            return (minValue * penalty, maxValue * penalty);
        }

        /// <summary>
        /// Gets the penalty description for UI display.
        /// </summary>
        public string GetPenaltyDescription()
        {
            if (!_enableSpeedPenalties)
            {
                return "Speed penalties disabled";
            }

            int percentage = GetPenaltyPercentage();
            if (percentage > 0)
            {
                return $"+{percentage}% quality bonus";
            }
            else if (percentage < 0)
            {
                return $"{percentage}% quality penalty";
            }
            else
            {
                return "No penalty or bonus";
            }
        }

        /// <summary>
        /// Gets the penalty percentage as an integer for UI display.
        /// </summary>
        public int GetPenaltyPercentage()
        {
            return Mathf.RoundToInt((CurrentPenaltyMultiplier - 1.0f) * 100);
        }

        /// <summary>
        /// Gets the current game time as a formatted string for UI display.
        /// </summary>
        public string GetGameTimeString()
        {
            TimeSpan gameTime = TimeSpan.FromSeconds(_accumulatedGameTime);
            
            if (gameTime.TotalDays >= 1)
            {
                return $"{(int)gameTime.TotalDays}d {gameTime.Hours:D2}h {gameTime.Minutes:D2}m";
            }
            else if (gameTime.TotalHours >= 1)
            {
                return $"{gameTime.Hours}h {gameTime.Minutes:D2}m {gameTime.Seconds:D2}s";
            }
            else if (gameTime.TotalMinutes >= 1)
            {
                return $"{gameTime.Minutes}m {gameTime.Seconds:D2}s";
            }
            else
            {
                return $"{gameTime.Seconds}s";
            }
        }

        /// <summary>
        /// Gets the current real-world session time as a formatted string for UI display.
        /// </summary>
        public string GetRealTimeString()
        {
            TimeSpan realTime = SessionDuration;
            
            if (realTime.TotalHours >= 1)
            {
                return $"{(int)realTime.TotalHours}h {realTime.Minutes:D2}m";
            }
            else if (realTime.TotalMinutes >= 1)
            {
                return $"{realTime.Minutes}m {realTime.Seconds:D2}s";
            }
            else
            {
                return $"{realTime.Seconds}s";
            }
        }

        /// <summary>
        /// Gets a combined time display string showing both game time and real time.
        /// </summary>
        public string GetCombinedTimeString()
        {
            return $"Game: {GetGameTimeString()} | Real: {GetRealTimeString()}";
        }

        /// <summary>
        /// Gets the time efficiency ratio (accelerated time vs real time) as a formatted string.
        /// </summary>
        public string GetTimeEfficiencyString()
        {
            if (_accumulatedRealTime <= 0) return "1:1";
            
            float ratio = _accumulatedGameTime / _accumulatedRealTime;
            return $"{ratio:F1}:1";
        }

        /// <summary>
        /// Gets detailed time information for advanced UI displays.
        /// </summary>
        public TimeDisplayData GetTimeDisplayData()
        {
            return new TimeDisplayData
            {
                GameTime = TimeSpan.FromSeconds(_accumulatedGameTime),
                RealTime = SessionDuration,
                TotalGameTime = TotalGameTime,
                CurrentSpeedLevel = CurrentSpeedLevel,
                CurrentTimeScale = CurrentTimeScale,
                TimeEfficiencyRatio = _accumulatedRealTime > 0 ? _accumulatedGameTime / _accumulatedRealTime : 1.0f,
                IsPaused = IsTimePaused,
                HasSpeedPenalty = HasSpeedPenalty,
                PenaltyMultiplier = CurrentPenaltyMultiplier,
                SpeedLevelDisplay = GetSpeedLevelDisplayString(),
                PenaltyDescription = GetPenaltyDescription()
            };
        }

        /// <summary>
        /// Formats a duration in seconds to a user-friendly string.
        /// </summary>
        public static string FormatDuration(float seconds)
        {
            return FormatDuration(seconds, TimeDisplayFormat.Adaptive);
        }

        /// <summary>
        /// Formats a duration in seconds using the specified display format.
        /// </summary>
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
                    if (duration.TotalDays >= 1)
                        return $"{(int)duration.TotalDays}d {duration.Hours:D2}h {duration.Minutes:D2}m";
                    else if (duration.TotalHours >= 1)
                        return $"{duration.Hours}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
                    else if (duration.TotalMinutes >= 1)
                        return $"{duration.Minutes}m {duration.Seconds:D2}s";
                    else
                        return $"{duration.Seconds}s";

                case TimeDisplayFormat.Precise:
                    if (duration.TotalDays >= 1)
                        return $"{(int)duration.TotalDays}d {duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
                    else if (duration.TotalHours >= 1)
                        return $"{duration.Hours}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
                    else if (duration.TotalMinutes >= 1)
                        return $"{duration.Minutes}m {duration.Seconds:D2}s";
                    else
                        return $"{duration.Seconds}s";

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

        /// <summary>
        /// Formats a duration using the configured time display format.
        /// </summary>
        public string FormatDurationWithConfig(float seconds)
        {
            return FormatDuration(seconds, _timeFormat);
        }

        /// <summary>
        /// Estimates how long a real-world duration will take given the current time scale.
        /// </summary>
        public string GetEstimatedRealTime(float gameTimeSeconds)
        {
            if (CurrentTimeScale <= 0) return "∞";
            
            float realTimeSeconds = gameTimeSeconds / CurrentTimeScale;
            return FormatDuration(realTimeSeconds);
        }

        /// <summary>
        /// Gets the current time status for compact UI displays (e.g., HUD corner).
        /// </summary>
        public string GetCompactTimeStatus()
        {
            if (IsTimePaused)
            {
                return $"⏸ {GetGameTimeString()}";
            }
            else
            {
                return $"{GetSpeedLevelDisplayString()} | {GetGameTimeString()}";
            }
        }

        /// <summary>
        /// Gets detailed time status for expanded UI displays (e.g., time management panel).
        /// </summary>
        public string GetDetailedTimeStatus()
        {
            var status = new System.Text.StringBuilder();
            
            status.AppendLine($"Speed: {GetSpeedLevelDisplayString()}");
            status.AppendLine($"Game Time: {GetGameTimeString()}");
            status.AppendLine($"Real Time: {GetRealTimeString()}");
            status.AppendLine($"Efficiency: {GetTimeEfficiencyString()}");
            
            if (HasSpeedPenalty)
            {
                status.AppendLine($"Quality: {GetPenaltyDescription()}");
            }
            
            if (IsTimePaused)
            {
                status.AppendLine("⏸ PAUSED");
            }
            
            return status.ToString().TrimEnd();
        }

        #region Testing Support Methods

        /// <summary>
        /// Check if TimeManager can trigger offline progression events (for testing purposes)
        /// </summary>
        public bool CanTriggerOfflineEvents()
        {
            return _enableOfflineProgression && _offlineProgressionListeners.Count > 0;
        }

        /// <summary>
        /// Check if TimeManager can calculate offline time periods (for testing purposes)
        /// </summary>
        public bool CanCalculateOfflineTime()
        {
            return _enableOfflineProgression && _lastSaveTime != default(DateTime);
        }

        /// <summary>
        /// Get the number of registered offline progression listeners (for testing purposes)
        /// </summary>
        public int GetOfflineProgressionListenerCount()
        {
            return _offlineProgressionListeners.Count;
        }

        /// <summary>
        /// Simulate offline progression event trigger (for testing purposes)
        /// </summary>
        public void TriggerOfflineProgressionForTesting(float offlineHours)
        {
            if (!CanTriggerOfflineEvents())
            {
                LogWarning("Cannot trigger offline progression - prerequisites not met");
                return;
            }

            LogInfo($"Triggering offline progression test for {offlineHours:F2} hours with {_offlineProgressionListeners.Count} listeners");

            foreach (var listener in _offlineProgressionListeners.ToArray()) // ToArray to avoid modification during iteration
            {
                try
                {
                    listener.OnOfflineProgressionCalculated(offlineHours);
                    LogDebug($"Offline progression test processed for {listener.GetType().Name}");
                }
                catch (Exception ex)
                {
                    LogError($"Error processing offline progression test for {listener.GetType().Name}: {ex.Message}");
                }
            }

            // Trigger offline progression event
            if (_onOfflineProgressionCalculated != null)
            {
                _onOfflineProgressionCalculated.Raise(offlineHours);
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface for systems that need to respond to time scale changes.
    /// </summary>
    public interface ITimeScaleListener
    {
        void OnTimeScaleChanged(float previousScale, float newScale);
    }

    /// <summary>
    /// Interface for systems that need to process offline progression.
    /// </summary>
    public interface IOfflineProgressionListener
    {
        void OnOfflineProgressionCalculated(float offlineHours);
    }

    /// <summary>
    /// Interface for systems that need to respond to speed penalty changes.
    /// </summary>
    public interface ISpeedPenaltyListener
    {
        void OnSpeedPenaltyChanged(TimeSpeedLevel speedLevel, float penaltyMultiplier);
    }

    /// <summary>
    /// Comprehensive time display data for UI systems.
    /// </summary>
    [System.Serializable]
    public struct TimeDisplayData
    {
        /// <summary>
        /// Current accelerated game time since session start.
        /// </summary>
        public TimeSpan GameTime;

        /// <summary>
        /// Real-world time elapsed in current session.
        /// </summary>
        public TimeSpan RealTime;

        /// <summary>
        /// Total game time since world creation.
        /// </summary>
        public TimeSpan TotalGameTime;

        /// <summary>
        /// Current discrete speed level.
        /// </summary>
        public TimeSpeedLevel CurrentSpeedLevel;

        /// <summary>
        /// Current time scale multiplier.
        /// </summary>
        public float CurrentTimeScale;

        /// <summary>
        /// Ratio of accelerated game time to real time (e.g., 2.1 means 2.1 hours of game time per 1 hour real time).
        /// </summary>
        public float TimeEfficiencyRatio;

        /// <summary>
        /// Whether time progression is currently paused.
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// Whether current speed level applies a quality penalty.
        /// </summary>
        public bool HasSpeedPenalty;

        /// <summary>
        /// Current penalty/bonus multiplier for simulation quality.
        /// </summary>
        public float PenaltyMultiplier;

        /// <summary>
        /// User-friendly speed level display string (e.g., "2x (Fast)").
        /// </summary>
        public string SpeedLevelDisplay;

        /// <summary>
        /// User-friendly penalty description (e.g., "-15% quality penalty").
        /// </summary>
        public string PenaltyDescription;
    }

    /// <summary>
    /// Configuration for time management behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "Time Config", menuName = "Project Chimera/Core/Time Config")]
    public class TimeConfigSO : ChimeraConfigSO
    {
        [Header("Time Speed Settings")]
        public TimeSpeedLevel DefaultSpeedLevel = TimeSpeedLevel.Normal;

        [Header("Risk/Reward System")]
        [Tooltip("Enable speed-based quality penalties/bonuses")]
        public bool EnableSpeedPenalties = true;
        
        [Tooltip("Multiplier for penalty severity (1.0 = default, >1.0 = harsher penalties)")]
        [Range(0.1f, 2.0f)]
        public float PenaltySeverityMultiplier = 1.0f;

        [Header("Offline Progression")]
        public bool EnableOfflineProgression = true;
        
        [Range(1.0f, 168.0f)] // 1 hour to 1 week
        public float MaxOfflineHours = 72.0f;
        
        [Range(0.1f, 10.0f)]
        public float OfflineProgressionMultiplier = 0.5f;

        [Header("UI Display Settings")]
        [Tooltip("Show both game time and real time in UI")]
        public bool ShowCombinedTimeDisplay = true;
        
        [Tooltip("Include time efficiency ratio in displays")]
        public bool ShowTimeEfficiencyRatio = true;
        
        [Tooltip("Show penalty information in time displays")]
        public bool ShowPenaltyInformation = true;
        
        [Tooltip("Time format for UI displays")]
        public TimeDisplayFormat TimeFormat = TimeDisplayFormat.Adaptive;

        [Header("Performance")]
        public bool EnableFrameTimeTracking = true;
        public bool EnableTimeDebugLogging = false;
        
        [Range(30, 120)]
        public int FrameHistorySize = 60;
    }
}