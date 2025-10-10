using System;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.TimeManagement
{
    /// <summary>
    /// Calendar system with seasons, dates, and time progression.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes time progression MEANINGFUL and IMMERSIVE:
    ///
    /// 1. **Seasonal Gameplay** - Strategic planning
    ///    - Spring: Ideal for germination (higher success rates)
    ///    - Summer: Peak growth (faster vegetative stage)
    ///    - Fall: Harvest season (quality bonuses)
    ///    - Winter: Slower growth (higher heating costs, but indoor!)
    ///
    /// 2. **Date Tracking** - Player progression
    ///    - "Day 45 of cultivation"
    ///    - "Spring, Year 2"
    ///    - Achievement: "Survived 1 year"
    ///
    /// 3. **Time Context** - Realistic immersion
    ///    - Morning/Afternoon/Evening/Night cycles
    ///    - Weekend bonuses (more time to tend plants)
    ///    - Holiday events (special genetics, sales)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see "Spring, Day 12" not "elapsed seconds since start"
    /// They experience seasons without complex datetime math.
    /// </summary>
    public class CalendarSystem : MonoBehaviour, ITickable
    {
        [Header("Calendar Configuration")]
        [SerializeField] private int _daysPerSeason = 30;
        [SerializeField] private float _realSecondsPerGameHour = 60f;
        [SerializeField] private int _startYear = 2025;
        [SerializeField] private Season _startSeason = Season.Spring;
        [SerializeField] private int _startDayOfSeason = 1;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Current calendar state
        private int _currentYear;
        private Season _currentSeason;
        private int _currentDayOfSeason;
        private int _currentHour;
        private float _elapsedSecondsThisHour;

        // Derived values
        private int _totalDaysElapsed;
        private TimeOfDay _currentTimeOfDay;

        // Services
        private ITimeManager _timeManager;

        // Events
        public event Action<Season> OnSeasonChanged;
        public event Action<int> OnDayChanged;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<int> OnYearChanged;

        // ITickable implementation
        public int TickPriority => -80; // High priority, after TimeManager
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Start()
        {
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            // Get time manager
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _timeManager = container.Resolve<ITimeManager>();
            }

            // Initialize calendar to starting values
            _currentYear = _startYear;
            _currentSeason = _startSeason;
            _currentDayOfSeason = _startDayOfSeason;
            _currentHour = 6; // Start at 6 AM
            _elapsedSecondsThisHour = 0f;
            _totalDaysElapsed = 0;

            UpdateTimeOfDay();

            // Register with update orchestrator
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.RegisterTickable(this);
            }

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"Calendar initialized: {GetFormattedDate()}", this);
            }
        }

        public void Tick(float deltaTime)
        {
            // Don't progress time if paused
            if (_timeManager != null && _timeManager.IsPaused)
                return;

            // Apply time scale
            float scaledDelta = _timeManager != null ? deltaTime * _timeManager.TimeScale : deltaTime;

            ProgressTime(scaledDelta);
        }

        /// <summary>
        /// Progresses calendar time based on delta.
        ///
        /// GAMEPLAY:
        /// - Real seconds convert to game hours
        /// - Hours accumulate to days
        /// - Days accumulate to seasons
        /// - Seasons accumulate to years
        /// </summary>
        private void ProgressTime(float deltaTime)
        {
            _elapsedSecondsThisHour += deltaTime;

            // Check if an hour has passed
            if (_elapsedSecondsThisHour >= _realSecondsPerGameHour)
            {
                _elapsedSecondsThisHour -= _realSecondsPerGameHour;
                AdvanceHour();
            }
        }

        /// <summary>
        /// Advances time by one game hour.
        /// </summary>
        private void AdvanceHour()
        {
            _currentHour++;

            // Check time of day changes
            var previousTimeOfDay = _currentTimeOfDay;
            UpdateTimeOfDay();

            if (_currentTimeOfDay != previousTimeOfDay)
            {
                OnTimeOfDayChanged?.Invoke(_currentTimeOfDay);
            }

            // Check if a day has passed (24 hours)
            if (_currentHour >= 24)
            {
                _currentHour = 0;
                AdvanceDay();
            }
        }

        /// <summary>
        /// Advances time by one game day.
        /// </summary>
        private void AdvanceDay()
        {
            _currentDayOfSeason++;
            _totalDaysElapsed++;

            OnDayChanged?.Invoke(_currentDayOfSeason);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"New day: {GetFormattedDate()}", this);
            }

            // Check if a season has passed
            if (_currentDayOfSeason > _daysPerSeason)
            {
                _currentDayOfSeason = 1;
                AdvanceSeason();
            }
        }

        /// <summary>
        /// Advances to next season.
        /// </summary>
        private void AdvanceSeason()
        {
            var previousSeason = _currentSeason;

            _currentSeason = _currentSeason switch
            {
                Season.Spring => Season.Summer,
                Season.Summer => Season.Fall,
                Season.Fall => Season.Winter,
                Season.Winter => Season.Spring,
                _ => Season.Spring
            };

            OnSeasonChanged?.Invoke(_currentSeason);

            ChimeraLogger.Log("TIME",
                $"Season changed: {previousSeason} → {_currentSeason}", this);

            // Check if a year has passed (Winter → Spring)
            if (previousSeason == Season.Winter && _currentSeason == Season.Spring)
            {
                AdvanceYear();
            }
        }

        /// <summary>
        /// Advances to next year.
        /// </summary>
        private void AdvanceYear()
        {
            _currentYear++;
            OnYearChanged?.Invoke(_currentYear);

            ChimeraLogger.Log("TIME",
                $"New year: {_currentYear}", this);
        }

        /// <summary>
        /// Updates time of day based on current hour.
        /// </summary>
        private void UpdateTimeOfDay()
        {
            _currentTimeOfDay = _currentHour switch
            {
                >= 6 and < 12 => TimeOfDay.Morning,
                >= 12 and < 17 => TimeOfDay.Afternoon,
                >= 17 and < 21 => TimeOfDay.Evening,
                _ => TimeOfDay.Night
            };
        }

        #region Public API

        /// <summary>
        /// Gets formatted date string for UI display.
        ///
        /// GAMEPLAY: Shows player their progress
        /// Example: "Spring 12, 2025" or "Fall 28, Year 3"
        /// </summary>
        public string GetFormattedDate()
        {
            return $"{_currentSeason} {_currentDayOfSeason}, {_currentYear}";
        }

        /// <summary>
        /// Gets formatted time string for UI display.
        ///
        /// GAMEPLAY: Shows current time of day
        /// Example: "Morning (9:00)" or "Evening (19:30)"
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt((_elapsedSecondsThisHour / _realSecondsPerGameHour) * 60f);
            return $"{_currentTimeOfDay} ({_currentHour:D2}:{minutes:D2})";
        }

        /// <summary>
        /// Gets full datetime string for UI.
        /// Example: "Spring 12, 2025 - Morning (9:15)"
        /// </summary>
        public string GetFormattedDateTime()
        {
            return $"{GetFormattedDate()} - {GetFormattedTime()}";
        }

        /// <summary>
        /// Gets current season.
        /// </summary>
        public Season CurrentSeason => _currentSeason;

        /// <summary>
        /// Gets current day of season (1-30).
        /// </summary>
        public int CurrentDayOfSeason => _currentDayOfSeason;

        /// <summary>
        /// Gets current year.
        /// </summary>
        public int CurrentYear => _currentYear;

        /// <summary>
        /// Gets current hour (0-23).
        /// </summary>
        public int CurrentHour => _currentHour;

        /// <summary>
        /// Gets current time of day.
        /// </summary>
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;

        /// <summary>
        /// Gets total days elapsed since game start.
        /// </summary>
        public int TotalDaysElapsed => _totalDaysElapsed;

        /// <summary>
        /// Gets seasonal modifier for plant growth.
        ///
        /// GAMEPLAY:
        /// - Spring: +10% growth (ideal germination)
        /// - Summer: +15% growth (peak growing season)
        /// - Fall: +5% growth (harvest bonuses)
        /// - Winter: -10% growth (slower, but indoor controlled)
        /// </summary>
        public float GetSeasonalGrowthModifier()
        {
            return _currentSeason switch
            {
                Season.Spring => 1.10f,
                Season.Summer => 1.15f,
                Season.Fall => 1.05f,
                Season.Winter => 0.90f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Gets seasonal modifier for utility costs.
        ///
        /// GAMEPLAY:
        /// - Spring: Normal costs
        /// - Summer: +20% cooling costs (AC running)
        /// - Fall: Normal costs
        /// - Winter: +30% heating costs (heaters running)
        /// </summary>
        public float GetSeasonalUtilityCostModifier()
        {
            return _currentSeason switch
            {
                Season.Spring => 1.0f,
                Season.Summer => 1.2f,
                Season.Fall => 1.0f,
                Season.Winter => 1.3f,
                _ => 1.0f
            };
        }

        #endregion

        #region ITickable Callbacks

        public void OnRegistered()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "CalendarSystem registered with UpdateOrchestrator", this);
            }
        }

        public void OnUnregistered()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "CalendarSystem unregistered from UpdateOrchestrator", this);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Unregister from update orchestrator
            var container = ServiceContainerFactory.Instance;
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.UnregisterTickable(this);
            }
        }
    }

    /// <summary>
    /// Seasons with gameplay impact.
    /// </summary>
    public enum Season
    {
        Spring,  // Ideal germination, +10% growth
        Summer,  // Peak growth, +15% growth, +20% cooling cost
        Fall,    // Harvest bonuses, +5% growth
        Winter   // Slower growth, -10% growth, +30% heating cost
    }

    /// <summary>
    /// Time of day periods.
    /// </summary>
    public enum TimeOfDay
    {
        Morning,    // 6:00 - 11:59
        Afternoon,  // 12:00 - 16:59
        Evening,    // 17:00 - 20:59
        Night       // 21:00 - 5:59
    }
}
