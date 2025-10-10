using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.TimeManagement
{
    /// <summary>
    /// Time acceleration UI controller - gives players control over time flow.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Makes time management POWERFUL and CONVENIENT:
    ///
    /// 1. **Speed Control** - Skip boring parts
    ///    - 1x: Normal speed (for active gameplay)
    ///    - 2x: Faster plant growth (skip waiting)
    ///    - 5x: Skip days (fast-forward to harvest)
    ///    - 10x: Time-lapse mode (watch facility evolve)
    ///    - Pause: Plan strategy, inspect plants
    ///
    /// 2. **Visual Feedback** - Clear current state
    ///    - Color-coded buttons (green = active)
    ///    - Current speed displayed prominently
    ///    - Pause shows "PAUSED" overlay
    ///
    /// 3. **Quick Access** - Always available
    ///    - Bottom-right corner (non-intrusive)
    ///    - Keyboard shortcuts (Space = pause, +/- = speed)
    ///    - Remembers last speed setting
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see simple speed buttons, not Unity Time.timeScale manipulation.
    /// They experience smooth time control without technical details.
    /// </summary>
    public class TimeAccelerationController : MonoBehaviour, ITickable
    {
        // ITickable implementation
        public int TickPriority => 5; // Very low priority - UI input checking
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _currentSpeedText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speed1xButton;
        [SerializeField] private Button _speed2xButton;
        [SerializeField] private Button _speed5xButton;
        [SerializeField] private Button _speed10xButton;
        [SerializeField] private GameObject _pausedOverlay;

        [Header("Button Colors")]
        [SerializeField] private Color _activeButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _inactiveButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Keyboard Shortcuts")]
        [SerializeField] private bool _enableKeyboardShortcuts = true;
        [SerializeField] private KeyCode _pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode _speedUpKey = KeyCode.Equals;
        [SerializeField] private KeyCode _slowDownKey = KeyCode.Minus;

        private ITimeManager _timeManager;
        private float _currentSpeed = 1f;
        private bool _isPaused = false;

        private void Start()
        {
            InitializeController();
            SetupButtonListeners();
            UpdateUI();
        }

        private void InitializeController()
        {
            // Get time manager from service container
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _timeManager = container.Resolve<ITimeManager>();
            }

            if (_timeManager == null)
            {
                ChimeraLogger.LogWarning("TIME",
                    "TimeAccelerationController: ITimeManager not found", this);
            }

            // Hide paused overlay by default
            if (_pausedOverlay != null)
                _pausedOverlay.SetActive(false);

            ChimeraLogger.Log("TIME",
                "Time acceleration controller initialized", this);
        }

        private void SetupButtonListeners()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnPauseClicked);

            if (_speed1xButton != null)
                _speed1xButton.onClick.AddListener(() => SetSpeed(1f));

            if (_speed2xButton != null)
                _speed2xButton.onClick.AddListener(() => SetSpeed(2f));

            if (_speed5xButton != null)
                _speed5xButton.onClick.AddListener(() => SetSpeed(5f));

            if (_speed10xButton != null)
                _speed10xButton.onClick.AddListener(() => SetSpeed(10f));
        }

        public void Tick(float deltaTime)
        {
            if (!_enableKeyboardShortcuts)
                return;

            // Keyboard shortcuts for time control
            if (Input.GetKeyDown(_pauseKey))
            {
                OnPauseClicked();
            }
            else if (Input.GetKeyDown(_speedUpKey))
            {
                IncreaseSpeed();
            }
            else if (Input.GetKeyDown(_slowDownKey))
            {
                DecreaseSpeed();
            }
        }

        /// <summary>
        /// Sets time speed to specified value.
        ///
        /// GAMEPLAY: Player clicks speed button → Time adjusts instantly
        /// Visual feedback shows new speed is active
        /// </summary>
        private void SetSpeed(float speed)
        {
            if (_timeManager == null)
                return;

            _currentSpeed = speed;

            // If currently paused, just remember the speed for when we resume
            if (!_isPaused)
            {
                _timeManager.SetTimeScale(speed);
            }

            UpdateUI();

            ChimeraLogger.Log("TIME",
                $"Time speed set to {speed}x", this);
        }

        /// <summary>
        /// Toggles pause state.
        ///
        /// GAMEPLAY:
        /// - Paused: Time freezes, player can plan/inspect
        /// - Resumed: Time flows at last selected speed
        /// - Keyboard shortcut (Space) for quick access
        /// </summary>
        private void OnPauseClicked()
        {
            if (_timeManager == null)
                return;

            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _timeManager.Pause();
            }
            else
            {
                _timeManager.Resume();
                _timeManager.SetTimeScale(_currentSpeed);
            }

            UpdateUI();

            ChimeraLogger.Log("TIME",
                $"Time {(_isPaused ? "paused" : "resumed")}", this);
        }

        /// <summary>
        /// Increases speed to next tier (1x → 2x → 5x → 10x).
        /// </summary>
        private void IncreaseSpeed()
        {
            if (_currentSpeed >= 10f)
                return;

            float newSpeed = _currentSpeed switch
            {
                < 2f => 2f,
                < 5f => 5f,
                < 10f => 10f,
                _ => 10f
            };

            SetSpeed(newSpeed);
        }

        /// <summary>
        /// Decreases speed to previous tier (10x → 5x → 2x → 1x).
        /// </summary>
        private void DecreaseSpeed()
        {
            if (_currentSpeed <= 1f)
                return;

            float newSpeed = _currentSpeed switch
            {
                > 5f => 5f,
                > 2f => 2f,
                > 1f => 1f,
                _ => 1f
            };

            SetSpeed(newSpeed);
        }

        /// <summary>
        /// Updates UI to reflect current time state.
        ///
        /// GAMEPLAY: Clear visual feedback
        /// - Active button highlighted in green
        /// - Current speed shown in text
        /// - "PAUSED" overlay when paused
        /// </summary>
        private void UpdateUI()
        {
            // Update speed text
            if (_currentSpeedText != null)
            {
                if (_isPaused)
                {
                    _currentSpeedText.text = "PAUSED";
                    _currentSpeedText.color = Color.yellow;
                }
                else
                {
                    _currentSpeedText.text = $"{_currentSpeed:F0}x Speed";
                    _currentSpeedText.color = Color.white;
                }
            }

            // Update button colors to show active state
            UpdateButtonColor(_speed1xButton, Mathf.Approximately(_currentSpeed, 1f));
            UpdateButtonColor(_speed2xButton, Mathf.Approximately(_currentSpeed, 2f));
            UpdateButtonColor(_speed5xButton, Mathf.Approximately(_currentSpeed, 5f));
            UpdateButtonColor(_speed10xButton, Mathf.Approximately(_currentSpeed, 10f));

            // Update pause button appearance
            if (_pauseButton != null)
            {
                var buttonText = _pauseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _isPaused ? "Resume" : "Pause";
                }

                var buttonColors = _pauseButton.colors;
                buttonColors.normalColor = _isPaused ? _activeButtonColor : _inactiveButtonColor;
                _pauseButton.colors = buttonColors;
            }

            // Show/hide paused overlay
            if (_pausedOverlay != null)
            {
                _pausedOverlay.SetActive(_isPaused);
            }
        }

        /// <summary>
        /// Updates button color based on active state.
        /// </summary>
        private void UpdateButtonColor(Button button, bool isActive)
        {
            if (button == null)
                return;

            var colors = button.colors;
            colors.normalColor = isActive ? _activeButtonColor : _inactiveButtonColor;
            button.colors = colors;
        }

        /// <summary>
        /// Shows the time control panel.
        /// </summary>
        public void Show()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
        }

        /// <summary>
        /// Hides the time control panel.
        /// </summary>
        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        /// <summary>
        /// Quick check if panel is visible.
        /// </summary>
        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(OnPauseClicked);

            if (_speed1xButton != null)
                _speed1xButton.onClick.RemoveAllListeners();

            if (_speed2xButton != null)
                _speed2xButton.onClick.RemoveAllListeners();

            if (_speed5xButton != null)
                _speed5xButton.onClick.RemoveAllListeners();

            if (_speed10xButton != null)
                _speed10xButton.onClick.RemoveAllListeners();
        }
    }
}
