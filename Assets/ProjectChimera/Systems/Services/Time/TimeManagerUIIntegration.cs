using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Services.Time
{
    /// <summary>
    /// Phase 2.1: TimeManager UI Integration
    /// Connects TimeManager to all UI displays and implements time-based event scheduling
    /// Handles the time display requirements from the gameplay document
    /// </summary>
    public class TimeManagerUIIntegration : MonoBehaviour, ITickable
    {
        [Header("Time Display Configuration")]
        [SerializeField] private bool _showCombinedTimeDisplay = true;
        [SerializeField] private bool _showRealWorldTime = false;
        [SerializeField] private bool _showTimeEfficiencyRatio = true;
        [SerializeField] private bool _showSpeedPenaltyInfo = true;

        [Header("UI Element Selectors")]
        [SerializeField] private string _timeDisplaySelector = "#time-display";
        [SerializeField] private string _speedControlSelector = "#speed-control";
        [SerializeField] private string _pauseButtonSelector = "#pause-button";
        [SerializeField] private string _speedIndicatorSelector = "#speed-indicator";
        [SerializeField] private string _efficiencyDisplaySelector = "#efficiency-display";

        [Header("Time Event Configuration")]
        [SerializeField] private float _uiUpdateInterval = 1.0f;
        [SerializeField] private bool _enableTimeEventScheduling = true;

        // Core references
        private ITimeManager _timeManager;
        private VisualElement _rootElement;

        // UI Elements
        private Label _timeDisplayLabel;
        private Button _pauseButton;
        private SliderInt _speedSlider;
        private Label _speedIndicatorLabel;
        private Label _efficiencyLabel;

        // Time display state
        private bool _isShowingGameTime = true;
        private float _lastUIUpdate = 0f;

        // Time formatting
        private readonly string[] _speedLevelNames = { "Slow (0.5x)", "Normal (1x)", "Fast (2x)", "Very Fast (4x)", "Maximum (8x)" };
        private readonly string[] _speedLevelColors = { "#90EE90", "#FFFFFF", "#FFD700", "#FFA500", "#FF6347" };

        // Events for time-based scheduling
        public event Action OnGameTimeUpdate;
        public event Action<TimeSpeedLevel> OnSpeedLevelChanged;
        public event Action<bool> OnTimePaused;

        private void Awake()
        {
            // Find root UI element
            _rootElement = GetComponent<UIDocument>()?.rootVisualElement;
            if (_rootElement == null)
            {
                ChimeraLogger.Log("UI", "TimeManagerUIIntegration: UIDocument not found", this);
                return;
            }
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeTimeIntegration();
        }

        /// <summary>
        /// Initialize the time manager UI integration
        /// </summary>
        public void InitializeTimeIntegration()
        {
            try
            {
                // Resolve TimeManager dependency
                ResolveDependencies();

                // Setup UI elements
                SetupUIElements();

                // Connect time manager events
                ConnectTimeManagerEvents();

                // Start time-based updates
                StartTimeUpdates();

                ChimeraLogger.Log("TIME", "TimeManager UI integration initialized", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("TIME", $"TimeManager UI integration failed: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Resolve TimeManager dependency through DI system
        /// </summary>
        private void ResolveDependencies()
        {
            var gameManager = ServiceContainerFactory.Instance?.TryResolve<GameManager>();
            if (gameManager == null)
            {
                throw new InvalidOperationException("GameManager not found - required for TimeManager resolution");
            }

            // Find TimeManager using reflection to avoid generic constraint issues
            _timeManager = FindTimeManagerComponent();
            if (_timeManager == null)
            {
                throw new InvalidOperationException("TimeManager not found in scene");
            }
        }

        /// <summary>
        /// Setup UI elements for time display and controls
        /// </summary>
        private void SetupUIElements()
        {
            // Find UI elements
            _timeDisplayLabel = _rootElement?.Q<Label>(_timeDisplaySelector);
            _pauseButton = _rootElement?.Q<Button>(_pauseButtonSelector);
            _speedSlider = _rootElement?.Q<SliderInt>(_speedControlSelector);
            _speedIndicatorLabel = _rootElement?.Q<Label>(_speedIndicatorSelector);
            _efficiencyLabel = _rootElement?.Q<Label>(_efficiencyDisplaySelector);

            // Setup time display (clickable to toggle between game time and real time)
            if (_timeDisplayLabel != null)
            {
                _timeDisplayLabel.RegisterCallback<ClickEvent>(ToggleTimeDisplay);
                _timeDisplayLabel.AddToClassList("clickable");
                UpdateTimeDisplay();
            }

            // Setup pause button
            if (_pauseButton != null)
            {
                _pauseButton.RegisterCallback<ClickEvent>(TogglePause);
                UpdatePauseButton();
            }

            // Setup speed control slider
            if (_speedSlider != null)
            {
                _speedSlider.lowValue = 0;
                _speedSlider.highValue = 4; // 5 speed levels (0-4)
                _speedSlider.value = (int)_timeManager.CurrentSpeedLevel;
                _speedSlider.RegisterCallback<ChangeEvent<int>>(OnSpeedSliderChanged);
            }

            // Setup speed indicator
            UpdateSpeedIndicator();

            // Setup efficiency display
            if (_showTimeEfficiencyRatio)
            {
                UpdateEfficiencyDisplay();
            }
        }

        /// <summary>
        /// Connect to TimeManager events
        /// </summary>
        private void ConnectTimeManagerEvents()
        {
            // Note: TimeManager would need to expose these events
            // For now, we'll poll for changes in Update()
        }

        /// <summary>
        /// Start time-based UI updates
        /// </summary>
        private void StartTimeUpdates()
        {
            // Reset update timer
            _lastUIUpdate = 0f;
        }

            public void Tick(float deltaTime)
    {
            if (_timeManager == null) return;

            // Update UI elements at specified interval
            _lastUIUpdate += UnityEngine.Time.unscaledDeltaTime;
            if (_lastUIUpdate >= _uiUpdateInterval)
            {
                UpdateTimeDisplay();
                UpdateSpeedIndicator();
                if (_showTimeEfficiencyRatio)
                {
                    UpdateEfficiencyDisplay();

    }

                // Trigger time event for other systems
                if (_enableTimeEventScheduling)
                {
                    OnGameTimeUpdate?.Invoke();
                }

                _lastUIUpdate = 0f;
            }
        }

        /// <summary>
        /// Update the time display label
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (_timeDisplayLabel == null) return;

            string timeText;

            if (_isShowingGameTime)
            {
                // Show game time using simplified calendar (6-day weeks, 30-day months)
                var currentTime = DateTime.Now; // In real implementation, get from TimeManager
                var gameTime = FormatGameTime(currentTime);
                timeText = $"Game Time: {gameTime}";

                if (_showCombinedTimeDisplay)
                {
                    var realTime = DateTime.Now.ToString("HH:mm");
                    timeText += $" (Real: {realTime})";
                }
            }
            else
            {
                // Show real-world time
                timeText = $"Real Time: {DateTime.Now:HH:mm:ss}";
            }

            _timeDisplayLabel.text = timeText;
        }

        /// <summary>
        /// Format game time using simplified calendar system
        /// </summary>
        private string FormatGameTime(DateTime realTime)
        {
            // Simplified game time formatting - in real implementation this would use TimeManager's game time
            var gameDay = (realTime.DayOfYear % 6) + 1; // 6-day weeks
            var gameWeek = (realTime.DayOfYear / 6) + 1;
            var gameMonth = (realTime.Month - 1) + 1; // Simplified month

            return $"Day {gameDay}, Week {gameWeek}, Month {gameMonth} - {realTime:HH:mm}";
        }

        /// <summary>
        /// Update speed indicator display
        /// </summary>
        private void UpdateSpeedIndicator()
        {
            if (_speedIndicatorLabel == null) return;

            var currentSpeed = _timeManager.CurrentSpeedLevel;
            var speedIndex = (int)currentSpeed;

            if (speedIndex >= 0 && speedIndex < _speedLevelNames.Length)
            {
                _speedIndicatorLabel.text = _speedLevelNames[speedIndex];
                _speedIndicatorLabel.style.color = new StyleColor(ColorUtility.TryParseHtmlString(_speedLevelColors[speedIndex], out var color) ? color : Color.white);
            }
        }

        /// <summary>
        /// Update efficiency display showing speed penalties
        /// </summary>
        private void UpdateEfficiencyDisplay()
        {
            if (_efficiencyLabel == null) return;

            var currentSpeed = _timeManager.CurrentSpeedLevel;
            var timeScale = _timeManager.CurrentTimeScale;

            // Calculate efficiency penalty (based on gameplay document)
            float efficiency = 1.0f;
            switch (currentSpeed)
            {
                case TimeSpeedLevel.Slow: efficiency = 1.1f; break;    // 10% bonus
                case TimeSpeedLevel.Normal: efficiency = 1.0f; break;  // No penalty
                case TimeSpeedLevel.Fast: efficiency = 0.95f; break;   // 5% penalty
                case TimeSpeedLevel.VeryFast: efficiency = 0.85f; break; // 15% penalty
                case TimeSpeedLevel.Maximum: efficiency = 0.7f; break;  // 30% penalty
            }

            var efficiencyPercent = Mathf.RoundToInt(efficiency * 100f);
            _efficiencyLabel.text = $"Efficiency: {efficiencyPercent}% (Scale: {timeScale:F1}x)";

            // Color code based on efficiency
            if (efficiency >= 1.0f)
                _efficiencyLabel.style.color = StyleKeyword.Initial; // Default color
            else if (efficiency >= 0.9f)
                _efficiencyLabel.style.color = new StyleColor(Color.yellow);
            else
                _efficiencyLabel.style.color = new StyleColor(Color.red);
        }

        /// <summary>
        /// Update pause button text and state
        /// </summary>
        private void UpdatePauseButton()
        {
            if (_pauseButton == null) return;

            _pauseButton.text = _timeManager.IsTimePaused ? "Resume" : "Pause";
        }

        /// <summary>
        /// Toggle between game time and real time display
        /// </summary>
        private void ToggleTimeDisplay(ClickEvent evt)
        {
            _isShowingGameTime = !_isShowingGameTime;
            UpdateTimeDisplay();
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        private void TogglePause(ClickEvent evt)
        {
            if (_timeManager.IsTimePaused)
            {
                _timeManager.Resume();
            }
            else
            {
                _timeManager.Pause();
            }

            UpdatePauseButton();
            OnTimePaused?.Invoke(_timeManager.IsTimePaused);
        }

        /// <summary>
        /// Handle speed slider changes
        /// </summary>
        private void OnSpeedSliderChanged(ChangeEvent<int> evt)
        {
            var newSpeedLevel = (TimeSpeedLevel)evt.newValue;
            _timeManager.SetSpeedLevel(newSpeedLevel);

            UpdateSpeedIndicator();
            OnSpeedLevelChanged?.Invoke(newSpeedLevel);
        }

        /// <summary>
        /// Schedule a time-based event
        /// </summary>
        public void ScheduleEvent(Action callback, float delayInGameTime)
        {
            if (_enableTimeEventScheduling)
            {
                StartCoroutine(ScheduleEventCoroutine(callback, delayInGameTime));
            }
        }

        /// <summary>
        /// Coroutine for scheduling time-based events
        /// </summary>
        private System.Collections.IEnumerator ScheduleEventCoroutine(Action callback, float delayInGameTime)
        {
            float elapsed = 0f;

            while (elapsed < delayInGameTime)
            {
                if (!_timeManager.IsTimePaused)
                {
                    elapsed += UnityEngine.Time.deltaTime * _timeManager.CurrentTimeScale;
                }
                yield return null;
            }

            callback?.Invoke();
        }

        /// <summary>
        /// Get current display settings for external access
        /// </summary>
        public TimeDisplaySettings GetDisplaySettings()
        {
            return new TimeDisplaySettings
            {
                ShowCombinedDisplay = _showCombinedTimeDisplay,
                ShowRealWorldTime = _showRealWorldTime,
                ShowEfficiencyRatio = _showTimeEfficiencyRatio,
                ShowSpeedPenaltyInfo = _showSpeedPenaltyInfo,
                IsShowingGameTime = _isShowingGameTime
            };
        }

        /// <summary>
        /// Update display settings
        /// </summary>
        public void UpdateDisplaySettings(TimeDisplaySettings settings)
        {
            _showCombinedTimeDisplay = settings.ShowCombinedDisplay;
            _showRealWorldTime = settings.ShowRealWorldTime;
            _showTimeEfficiencyRatio = settings.ShowEfficiencyRatio;
            _showSpeedPenaltyInfo = settings.ShowSpeedPenaltyInfo;
            _isShowingGameTime = settings.IsShowingGameTime;

            UpdateTimeDisplay();
            UpdateEfficiencyDisplay();
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            // Cleanup event registrations
            if (_timeDisplayLabel != null)
            {
                _timeDisplayLabel.UnregisterCallback<ClickEvent>(ToggleTimeDisplay);
            }

            if (_pauseButton != null)
            {
                _pauseButton.UnregisterCallback<ClickEvent>(TogglePause);
            }

            if (_speedSlider != null)
            {
                _speedSlider.UnregisterCallback<ChangeEvent<int>>(OnSpeedSliderChanged);
            }
        }

        /// <summary>
        /// Helper method to find TimeManager component using ServiceContainer resolution
        /// </summary>
        private ITimeManager FindTimeManagerComponent()
        {
            // Primary: Try ServiceContainer resolution for ITimeManager interface
            var timeManager = ServiceContainerFactory.Instance.TryResolve<ITimeManager>();
            if (timeManager != null)
            {
                ChimeraLogger.Log("TIME", "Resolved ITimeManager from DI container", this);
                return timeManager;
            }

            ChimeraLogger.LogWarning("TIME", "ITimeManager not found in ServiceContainer - ensure it is registered during initialization", this);
            return null;
        }

    // ITickable implementation
    public int TickPriority => ProjectChimera.Core.Updates.TickPriority.TimeManager;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}

    /// <summary>
    /// Time display settings structure
    /// </summary>
    [Serializable]
    public struct TimeDisplaySettings
    {
        public bool ShowCombinedDisplay;
        public bool ShowRealWorldTime;
        public bool ShowEfficiencyRatio;
        public bool ShowSpeedPenaltyInfo;
        public bool IsShowingGameTime;
    }
}
