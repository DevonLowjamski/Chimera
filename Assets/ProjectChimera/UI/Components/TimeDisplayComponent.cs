using UnityEngine;
using UnityEngine.UIElements;
using System;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Components
{
    /// <summary>
    /// Advanced time display component with penalty signaling for Project Chimera.
    /// Displays game time, real time, time acceleration, and provides visual/audio feedback
    /// for time-sensitive events, penalties, and deadlines.
    /// </summary>
    public class TimeDisplayComponent : MonoBehaviour
    {
        [Header("Time Display Configuration")]
        [SerializeField] private bool _showGameTime = true;
        [SerializeField] private bool _showRealTime = false;
        [SerializeField] private bool _showTimeAcceleration = true;
        [SerializeField] private bool _show24HourFormat = true;
        [SerializeField] private bool _enablePenaltySignaling = true;
        
        [Header("Penalty Signaling")]
        [SerializeField] private Color _normalTimeColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _warningTimeColor = new Color(0.9f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color _penaltyTimeColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _criticalTimeColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private float _warningBlinkRate = 0.5f;
        [SerializeField] private float _penaltyBlinkRate = 0.25f;
        [SerializeField] private float _criticalBlinkRate = 0.1f;
        
        [Header("Audio Configuration")]
        [SerializeField] private AudioClip _warningBeep;
        [SerializeField] private AudioClip _penaltyAlarm;
        [SerializeField] private AudioClip _criticalSiren;
        [SerializeField] private bool _enableAudioAlerts = true;
        [SerializeField] private float _audioVolume = 0.7f;
        
        [Header("Time Format Configuration")]
        [SerializeField] private string _gameTimePrefix = "Day ";
        [SerializeField] private string _timeFormat24 = "HH:mm:ss";
        [SerializeField] private string _timeFormat12 = "h:mm:ss tt";
        [SerializeField] private string _accelerationFormat = "x{0:F1}";
        
        // UI Elements
        private UIDocument _uiDocument;
        private VisualElement _rootElement;
        private VisualElement _timeContainer;
        private Label _gameTimeLabel;
        private Label _realTimeLabel;
        private Label _timeAccelerationLabel;
        private VisualElement _penaltyIndicator;
        private VisualElement _warningFlash;
        
        // Time tracking
        private float _gameTime = 0f;
        private float _timeAcceleration = 1f;
        private int _currentGameDay = 1;
        private bool _isTimePaused = false;
        
        // Penalty state
        private TimePenaltyLevel _currentPenaltyLevel = TimePenaltyLevel.None;
        private float _penaltyStartTime = 0f;
        private float _lastBlinkTime = 0f;
        private bool _isBlinking = false;
        private bool _blinkState = false;
        
        // Audio
        private AudioSource _audioSource;
        private float _lastAudioAlert = 0f;
        private float _audioAlertCooldown = 2f;
        
        // Events
        public Action<float> OnGameTimeChanged;
        public Action<int> OnGameDayChanged;
        public Action<float> OnTimeAccelerationChanged;
        public Action<TimePenaltyLevel> OnPenaltyLevelChanged;
        public Action<string> OnTimeEventTriggered;
        
        // Properties
        public float GameTime => _gameTime;
        public int CurrentGameDay => _currentGameDay;
        public float TimeAcceleration => _timeAcceleration;
        public bool IsTimePaused => _isTimePaused;
        public TimePenaltyLevel CurrentPenaltyLevel => _currentPenaltyLevel;
        
        private void Awake()
        {
            InitializeTimeDisplay();
        }
        
        private void Start()
        {
            SetupTimeUI();
            UpdateTimeDisplay();
        }
        
        private void Update()
        {
            if (!_isTimePaused)
            {
                UpdateGameTime();
            }
            
            UpdateTimeDisplay();
            UpdatePenaltyEffects();
        }
        
        private void InitializeTimeDisplay()
        {
            // Setup audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.volume = _audioVolume;
            }
            
            // Initialize time values
            _gameTime = 0f;
            _currentGameDay = 1;
            _timeAcceleration = 1f;
            _currentPenaltyLevel = TimePenaltyLevel.None;
        }
        
        private void SetupTimeUI()
        {
            // Get or create UI Document
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }
            
            // Create root container
            _rootElement = new VisualElement();
            _rootElement.name = "time-display-root";
            _rootElement.style.position = Position.Absolute;
            _rootElement.style.top = 20;
            _rootElement.style.right = 20;
            _rootElement.style.minWidth = 200;
            
            // Create time container
            _timeContainer = new VisualElement();
            _timeContainer.name = "time-container";
            _timeContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            _timeContainer.style.borderTopLeftRadius = 8;
            _timeContainer.style.borderTopRightRadius = 8;
            _timeContainer.style.borderBottomLeftRadius = 8;
            _timeContainer.style.borderBottomRightRadius = 8;
            _timeContainer.style.paddingTop = 12;
            _timeContainer.style.paddingBottom = 12;
            _timeContainer.style.paddingLeft = 16;
            _timeContainer.style.paddingRight = 16;
            _timeContainer.style.borderTopWidth = 2;
            _timeContainer.style.borderRightWidth = 2;
            _timeContainer.style.borderBottomWidth = 2;
            _timeContainer.style.borderLeftWidth = 2;
            _timeContainer.style.borderTopColor = _normalTimeColor;
            _timeContainer.style.borderRightColor = _normalTimeColor;
            _timeContainer.style.borderBottomColor = _normalTimeColor;
            _timeContainer.style.borderLeftColor = _normalTimeColor;
            _rootElement.Add(_timeContainer);
            
            // Create warning flash overlay
            _warningFlash = new VisualElement();
            _warningFlash.name = "warning-flash";
            _warningFlash.style.position = Position.Absolute;
            _warningFlash.style.top = 0;
            _warningFlash.style.left = 0;
            _warningFlash.style.right = 0;
            _warningFlash.style.bottom = 0;
            _warningFlash.style.backgroundColor = new Color(1f, 0f, 0f, 0.3f);
            _warningFlash.style.borderTopLeftRadius = 8;
            _warningFlash.style.borderTopRightRadius = 8;
            _warningFlash.style.borderBottomLeftRadius = 8;
            _warningFlash.style.borderBottomRightRadius = 8;
            _warningFlash.style.display = DisplayStyle.None;
            _timeContainer.Add(_warningFlash);
            
            // Create game time label
            if (_showGameTime)
            {
                _gameTimeLabel = new Label();
                _gameTimeLabel.name = "game-time-label";
                _gameTimeLabel.style.fontSize = 16;
                _gameTimeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _gameTimeLabel.style.color = _normalTimeColor;
                _gameTimeLabel.style.marginBottom = 4;
                _timeContainer.Add(_gameTimeLabel);
            }
            
            // Create real time label
            if (_showRealTime)
            {
                _realTimeLabel = new Label();
                _realTimeLabel.name = "real-time-label";
                _realTimeLabel.style.fontSize = 12;
                _realTimeLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                _realTimeLabel.style.marginBottom = 4;
                _timeContainer.Add(_realTimeLabel);
            }
            
            // Create time acceleration label
            if (_showTimeAcceleration)
            {
                _timeAccelerationLabel = new Label();
                _timeAccelerationLabel.name = "time-acceleration-label";
                _timeAccelerationLabel.style.fontSize = 11;
                _timeAccelerationLabel.style.color = new Color(0.6f, 0.8f, 0.9f, 1f);
                _timeAccelerationLabel.style.marginTop = 4;
                _timeContainer.Add(_timeAccelerationLabel);
            }
            
            // Create penalty indicator
            _penaltyIndicator = new VisualElement();
            _penaltyIndicator.name = "penalty-indicator";
            _penaltyIndicator.style.height = 4;
            _penaltyIndicator.style.backgroundColor = _normalTimeColor;
            _penaltyIndicator.style.marginTop = 8;
            _penaltyIndicator.style.borderTopLeftRadius = 2;
            _penaltyIndicator.style.borderTopRightRadius = 2;
            _penaltyIndicator.style.borderBottomLeftRadius = 2;
            _penaltyIndicator.style.borderBottomRightRadius = 2;
            _timeContainer.Add(_penaltyIndicator);
            
            // Add to UI document
            if (_uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.Add(_rootElement);
            }
        }
        
        private void UpdateGameTime()
        {
            float deltaTime = Time.deltaTime * _timeAcceleration;
            _gameTime += deltaTime;
            
            // Calculate current day (assuming 24 game hours = 1 day)
            int newGameDay = Mathf.FloorToInt(_gameTime / (24f * 3600f)) + 1;
            if (newGameDay != _currentGameDay)
            {
                _currentGameDay = newGameDay;
                OnGameDayChanged?.Invoke(_currentGameDay);
                OnTimeEventTriggered?.Invoke($"Day {_currentGameDay} began");
            }
            
            OnGameTimeChanged?.Invoke(_gameTime);
        }
        
        private void UpdateTimeDisplay()
        {
            // Update game time display
            if (_gameTimeLabel != null && _showGameTime)
            {
                var timeOfDay = TimeSpan.FromSeconds(_gameTime % (24f * 3600f));
                string timeFormat = _show24HourFormat ? _timeFormat24 : _timeFormat12;
                string formattedTime = DateTime.Today.Add(timeOfDay).ToString(timeFormat);
                _gameTimeLabel.text = $"{_gameTimePrefix}{_currentGameDay}\n{formattedTime}";
            }
            
            // Update real time display
            if (_realTimeLabel != null && _showRealTime)
            {
                string timeFormat = _show24HourFormat ? _timeFormat24 : _timeFormat12;
                _realTimeLabel.text = $"Real: {DateTime.Now.ToString(timeFormat)}";
            }
            
            // Update time acceleration display
            if (_timeAccelerationLabel != null && _showTimeAcceleration)
            {
                string accelerationText = _isTimePaused ? "PAUSED" : string.Format(_accelerationFormat, _timeAcceleration);
                _timeAccelerationLabel.text = accelerationText;
            }
        }
        
        private void UpdatePenaltyEffects()
        {
            if (!_enablePenaltySignaling) return;
            
            float currentTime = Time.time;
            Color currentColor = GetPenaltyColor(_currentPenaltyLevel);
            float blinkRate = GetBlinkRate(_currentPenaltyLevel);
            
            // Update border color
            _timeContainer.style.borderTopColor = currentColor;
            _timeContainer.style.borderRightColor = currentColor;
            _timeContainer.style.borderBottomColor = currentColor;
            _timeContainer.style.borderLeftColor = currentColor;
            
            // Update penalty indicator
            _penaltyIndicator.style.backgroundColor = currentColor;
            
            // Handle blinking effects
            if (_currentPenaltyLevel != TimePenaltyLevel.None && blinkRate > 0)
            {
                if (currentTime - _lastBlinkTime >= blinkRate)
                {
                    _blinkState = !_blinkState;
                    _lastBlinkTime = currentTime;
                    
                    // Apply blinking to game time label
                    if (_gameTimeLabel != null)
                    {
                        _gameTimeLabel.style.color = _blinkState ? currentColor : _normalTimeColor;
                    }
                    
                    // Show/hide warning flash for critical penalties
                    if (_currentPenaltyLevel == TimePenaltyLevel.Critical)
                    {
                        _warningFlash.style.display = _blinkState ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
            else
            {
                // Reset to normal color when no penalty
                if (_gameTimeLabel != null)
                {
                    _gameTimeLabel.style.color = currentColor;
                }
                _warningFlash.style.display = DisplayStyle.None;
            }
        }
        
        private Color GetPenaltyColor(TimePenaltyLevel level)
        {
            return level switch
            {
                TimePenaltyLevel.Warning => _warningTimeColor,
                TimePenaltyLevel.Penalty => _penaltyTimeColor,
                TimePenaltyLevel.Critical => _criticalTimeColor,
                _ => _normalTimeColor
            };
        }
        
        private float GetBlinkRate(TimePenaltyLevel level)
        {
            return level switch
            {
                TimePenaltyLevel.Warning => _warningBlinkRate,
                TimePenaltyLevel.Penalty => _penaltyBlinkRate,
                TimePenaltyLevel.Critical => _criticalBlinkRate,
                _ => 0f
            };
        }
        
        private void PlayPenaltyAudio(TimePenaltyLevel level)
        {
            if (!_enableAudioAlerts || _audioSource == null) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastAudioAlert < _audioAlertCooldown) return;
            
            AudioClip clipToPlay = level switch
            {
                TimePenaltyLevel.Warning => _warningBeep,
                TimePenaltyLevel.Penalty => _penaltyAlarm,
                TimePenaltyLevel.Critical => _criticalSiren,
                _ => null
            };
            
            if (clipToPlay != null)
            {
                _audioSource.PlayOneShot(clipToPlay, _audioVolume);
                _lastAudioAlert = currentTime;
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Set the current game time in seconds
        /// </summary>
        public void SetGameTime(float timeInSeconds)
        {
            _gameTime = timeInSeconds;
            _currentGameDay = Mathf.FloorToInt(_gameTime / (24f * 3600f)) + 1;
            OnGameTimeChanged?.Invoke(_gameTime);
        }
        
        /// <summary>
        /// Set time acceleration multiplier
        /// </summary>
        public void SetTimeAcceleration(float acceleration)
        {
            _timeAcceleration = Mathf.Max(0f, acceleration);
            OnTimeAccelerationChanged?.Invoke(_timeAcceleration);
        }
        
        /// <summary>
        /// Pause or resume time
        /// </summary>
        public void SetTimePaused(bool paused)
        {
            _isTimePaused = paused;
            OnTimeEventTriggered?.Invoke(paused ? "Time paused" : "Time resumed");
        }
        
        /// <summary>
        /// Set penalty level with optional audio alert
        /// </summary>
        public void SetPenaltyLevel(TimePenaltyLevel level, bool playAudio = true)
        {
            if (_currentPenaltyLevel != level)
            {
                _currentPenaltyLevel = level;
                _penaltyStartTime = Time.time;
                
                if (playAudio && level != TimePenaltyLevel.None)
                {
                    PlayPenaltyAudio(level);
                }
                
                OnPenaltyLevelChanged?.Invoke(level);
                OnTimeEventTriggered?.Invoke($"Penalty level changed to: {level}");
            }
        }
        
        /// <summary>
        /// Clear all penalties
        /// </summary>
        public void ClearPenalties()
        {
            SetPenaltyLevel(TimePenaltyLevel.None, false);
        }
        
        /// <summary>
        /// Get formatted time string
        /// </summary>
        public string GetFormattedGameTime()
        {
            var timeOfDay = TimeSpan.FromSeconds(_gameTime % (24f * 3600f));
            string timeFormat = _show24HourFormat ? _timeFormat24 : _timeFormat12;
            return DateTime.Today.Add(timeOfDay).ToString(timeFormat);
        }
        
        /// <summary>
        /// Get game time as TimeSpan
        /// </summary>
        public TimeSpan GetGameTimeSpan()
        {
            return TimeSpan.FromSeconds(_gameTime);
        }
        
        /// <summary>
        /// Advance game time by specified amount
        /// </summary>
        public void AdvanceTime(float seconds)
        {
            _gameTime += seconds;
            int newGameDay = Mathf.FloorToInt(_gameTime / (24f * 3600f)) + 1;
            if (newGameDay != _currentGameDay)
            {
                _currentGameDay = newGameDay;
                OnGameDayChanged?.Invoke(_currentGameDay);
            }
            OnGameTimeChanged?.Invoke(_gameTime);
        }
        
        /// <summary>
        /// Toggle time display format between 12h and 24h
        /// </summary>
        public void ToggleTimeFormat()
        {
            _show24HourFormat = !_show24HourFormat;
        }
        
        /// <summary>
        /// Enable/disable specific display elements
        /// </summary>
        public void SetDisplayOptions(bool showGameTime, bool showRealTime, bool showAcceleration)
        {
            _showGameTime = showGameTime;
            _showRealTime = showRealTime;
            _showTimeAcceleration = showAcceleration;
            
            if (_gameTimeLabel != null)
                _gameTimeLabel.style.display = showGameTime ? DisplayStyle.Flex : DisplayStyle.None;
            if (_realTimeLabel != null)
                _realTimeLabel.style.display = showRealTime ? DisplayStyle.Flex : DisplayStyle.None;
            if (_timeAccelerationLabel != null)
                _timeAccelerationLabel.style.display = showAcceleration ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up events
            OnGameTimeChanged = null;
            OnGameDayChanged = null;
            OnTimeAccelerationChanged = null;
            OnPenaltyLevelChanged = null;
            OnTimeEventTriggered = null;
        }
    }
    
    /// <summary>
    /// Time penalty levels for visual/audio signaling
    /// </summary>
    public enum TimePenaltyLevel
    {
        None = 0,       // Normal operation
        Warning = 1,    // Minor time pressure
        Penalty = 2,    // Active penalty state
        Critical = 3    // Critical time violation
    }
}