using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Components
{
    /// <summary>
    /// BASIC: Simple time display component for Project Chimera's UI.
    /// Focuses on essential time display without complex penalty signaling and audio alerts.
    /// </summary>
    public class TimeDisplayComponent : MonoBehaviour
    {
        [Header("Basic Time Display Settings")]
        [SerializeField] private bool _enableBasicDisplay = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _showGameTime = true;
        [SerializeField] private bool _showRealTime = false;
        [SerializeField] private bool _showTimeScale = true;
        [SerializeField] private string _timeFormat = "HH:mm:ss";
        [SerializeField] private string _dayPrefix = "Day ";

        // Basic UI components
        [Header("UI Components")]
        [SerializeField] private Text _gameTimeText;
        [SerializeField] private Text _realTimeText;
        [SerializeField] private Text _timeScaleText;
        [SerializeField] private Image _backgroundImage;

        // Basic time tracking
        private float _gameTime = 0f;
        private int _currentDay = 1;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for time display
        /// </summary>
        public event System.Action<float> OnGameTimeUpdated;
        public event System.Action<int> OnDayChanged;

        /// <summary>
        /// Initialize basic time display
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Auto-find text components if not assigned
            if (_gameTimeText == null)
                _gameTimeText = GetComponentInChildren<Text>();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[TimeDisplayComponent] Initialized successfully");
            }
        }

        /// <summary>
        /// Update time display
        /// </summary>
        private void Update()
        {
            if (!_enableBasicDisplay || !_isInitialized) return;

            // Update game time (simulated)
            _gameTime += Time.deltaTime;

            // Check for day change (simple 24-hour days)
            int newDay = Mathf.FloorToInt(_gameTime / 86400f) + 1;
            if (newDay != _currentDay)
            {
                _currentDay = newDay;
                OnDayChanged?.Invoke(_currentDay);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[TimeDisplayComponent] New day: {_currentDay}");
                }
            }

            UpdateDisplay();
            OnGameTimeUpdated?.Invoke(_gameTime);
        }

        /// <summary>
        /// Set game time manually
        /// </summary>
        public void SetGameTime(float gameTime)
        {
            _gameTime = gameTime;
            _currentDay = Mathf.FloorToInt(_gameTime / 86400f) + 1;
            UpdateDisplay();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[TimeDisplayComponent] Game time set to {_gameTime:F0} seconds (Day {_currentDay})");
            }
        }

        /// <summary>
        /// Set current day
        /// </summary>
        public void SetCurrentDay(int day)
        {
            _currentDay = Mathf.Max(1, day);
            UpdateDisplay();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[TimeDisplayComponent] Day set to {_currentDay}");
            }
        }

        /// <summary>
        /// Get current game time
        /// </summary>
        public float GetGameTime()
        {
            return _gameTime;
        }

        /// <summary>
        /// Get current day
        /// </summary>
        public int GetCurrentDay()
        {
            return _currentDay;
        }

        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetFormattedTime()
        {
            // Convert game time to hours/minutes/seconds
            int totalSeconds = Mathf.FloorToInt(_gameTime);
            int hours = (totalSeconds / 3600) % 24;
            int minutes = (totalSeconds / 60) % 60;
            int seconds = totalSeconds % 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }

        /// <summary>
        /// Set display options
        /// </summary>
        public void SetDisplayOptions(bool showGameTime, bool showRealTime, bool showTimeScale)
        {
            _showGameTime = showGameTime;
            _showRealTime = showRealTime;
            _showTimeScale = showTimeScale;
            UpdateDisplay();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[TimeDisplayComponent] Display options updated");
            }
        }

        /// <summary>
        /// Set time format
        /// </summary>
        public void SetTimeFormat(string format)
        {
            _timeFormat = format;
            UpdateDisplay();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[TimeDisplayComponent] Time format set to {format}");
            }
        }

        /// <summary>
        /// Get display statistics
        /// </summary>
        public TimeDisplayStats GetStats()
        {
            return new TimeDisplayStats
            {
                CurrentGameTime = _gameTime,
                CurrentDay = _currentDay,
                IsDisplayEnabled = _enableBasicDisplay,
                IsInitialized = _isInitialized,
                ShowsGameTime = _showGameTime,
                ShowsRealTime = _showRealTime,
                ShowsTimeScale = _showTimeScale
            };
        }

        #region Private Methods

        private void UpdateDisplay()
        {
            if (!_isInitialized) return;

            string displayText = "";

            // Game time
            if (_showGameTime)
            {
                displayText += $"{_dayPrefix}{_currentDay} - {GetFormattedTime()}";
            }

            // Real time
            if (_showRealTime)
            {
                if (!string.IsNullOrEmpty(displayText)) displayText += "\n";
                displayText += $"Real: {System.DateTime.Now.ToString(_timeFormat)}";
            }

            // Time scale
            if (_showTimeScale)
            {
                if (!string.IsNullOrEmpty(displayText)) displayText += "\n";
                displayText += $"Speed: {Time.timeScale:F1}x";
            }

            // Update UI
            if (_gameTimeText != null)
            {
                _gameTimeText.text = displayText;
            }
        }

        #endregion
    }

    /// <summary>
    /// Time display statistics
    /// </summary>
    [System.Serializable]
    public struct TimeDisplayStats
    {
        public float CurrentGameTime;
        public int CurrentDay;
        public bool IsDisplayEnabled;
        public bool IsInitialized;
        public bool ShowsGameTime;
        public bool ShowsRealTime;
        public bool ShowsTimeScale;
    }
}
