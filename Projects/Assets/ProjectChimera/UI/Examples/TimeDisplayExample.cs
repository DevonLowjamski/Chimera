using UnityEngine;
using ProjectChimera.UI.Components;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Examples
{
    /// <summary>
    /// Example integration of TimeDisplayComponent with game systems.
    /// Shows how to connect time display to game events and penalty conditions.
    /// </summary>
    public class TimeDisplayExample : MonoBehaviour
    {
        [Header("Time Display Integration")]
        [SerializeField] private TimeDisplayComponent _timeDisplay;
        [SerializeField] private NotificationManager _notificationManager;
        
        [Header("Test Configuration")]
        [SerializeField] private bool _enableTestControls = true;
        [SerializeField] private float _testPenaltyDuration = 5f;
        [SerializeField] private KeyCode _pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode _speedUpKey = KeyCode.Plus;
        [SerializeField] private KeyCode _slowDownKey = KeyCode.Minus;
        [SerializeField] private KeyCode _penaltyKey = KeyCode.P;
        
        private float _testPenaltyTimer = 0f;
        private bool _testPenaltyActive = false;
        
        private void Start()
        {
            InitializeTimeDisplay();
            SubscribeToTimeEvents();
        }
        
        private void Update()
        {
            if (_enableTestControls)
            {
                HandleTestControls();
            }
            
            UpdateTestPenalties();
        }
        
        private void InitializeTimeDisplay()
        {
            if (_timeDisplay == null)
            {
                _timeDisplay = FindObjectOfType<TimeDisplayComponent>();
            }
            
            if (_timeDisplay == null)
            {
                Debug.LogError("[TimeDisplayExample] TimeDisplayComponent not found!");
                return;
            }
            
            // Initialize with starting values
            _timeDisplay.SetGameTime(0f);
            _timeDisplay.SetTimeAcceleration(1f);
            _timeDisplay.SetTimePaused(false);
            
            Debug.Log("[TimeDisplayExample] TimeDisplay initialized successfully");
        }
        
        private void SubscribeToTimeEvents()
        {
            if (_timeDisplay == null) return;
            
            // Subscribe to time events
            _timeDisplay.OnGameTimeChanged += HandleGameTimeChanged;
            _timeDisplay.OnGameDayChanged += HandleGameDayChanged;
            _timeDisplay.OnTimeAccelerationChanged += HandleTimeAccelerationChanged;
            _timeDisplay.OnPenaltyLevelChanged += HandlePenaltyLevelChanged;
            _timeDisplay.OnTimeEventTriggered += HandleTimeEventTriggered;
        }
        
        private void HandleTestControls()
        {
            if (_timeDisplay == null) return;
            
            // Pause/Resume
            if (Input.GetKeyDown(_pauseKey))
            {
                _timeDisplay.SetTimePaused(!_timeDisplay.IsTimePaused);
            }
            
            // Speed up
            if (Input.GetKeyDown(_speedUpKey))
            {
                float newSpeed = Mathf.Min(_timeDisplay.TimeAcceleration * 2f, 16f);
                _timeDisplay.SetTimeAcceleration(newSpeed);
            }
            
            // Slow down
            if (Input.GetKeyDown(_slowDownKey))
            {
                float newSpeed = Mathf.Max(_timeDisplay.TimeAcceleration * 0.5f, 0.25f);
                _timeDisplay.SetTimeAcceleration(newSpeed);
            }
            
            // Test penalty
            if (Input.GetKeyDown(_penaltyKey))
            {
                TriggerTestPenalty();
            }
        }
        
        private void UpdateTestPenalties()
        {
            if (!_testPenaltyActive || _timeDisplay == null) return;
            
            _testPenaltyTimer += Time.deltaTime;
            
            // Cycle through penalty levels during test
            float normalizedTime = _testPenaltyTimer / _testPenaltyDuration;
            
            if (normalizedTime < 0.25f)
            {
                _timeDisplay.SetPenaltyLevel(TimePenaltyLevel.Warning, false);
            }
            else if (normalizedTime < 0.5f)
            {
                _timeDisplay.SetPenaltyLevel(TimePenaltyLevel.Penalty, false);
            }
            else if (normalizedTime < 0.75f)
            {
                _timeDisplay.SetPenaltyLevel(TimePenaltyLevel.Critical, false);
            }
            else
            {
                // End test penalty
                _timeDisplay.ClearPenalties();
                _testPenaltyActive = false;
                _testPenaltyTimer = 0f;
            }
        }
        
        private void TriggerTestPenalty()
        {
            _testPenaltyActive = true;
            _testPenaltyTimer = 0f;
            
            // Show notification about penalty test
            if (_notificationManager != null)
            {
                _notificationManager.ShowNotification("Testing penalty signaling system", NotificationSeverity.Info);
            }
        }
        
        #region Time Event Handlers
        
        private void HandleGameTimeChanged(float gameTime)
        {
            // Example: Check for specific time-based events
            var timeSpan = System.TimeSpan.FromSeconds(gameTime);
            
            // Example: Morning notification (6 AM game time)
            if (timeSpan.Hours == 6 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0)
            {
                if (_notificationManager != null)
                {
                    _notificationManager.ShowNotification("Good morning! New day begins.", NotificationSeverity.Info);
                }
            }
            
            // Example: Evening notification (6 PM game time)
            if (timeSpan.Hours == 18 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0)
            {
                if (_notificationManager != null)
                {
                    _notificationManager.ShowNotification("Evening time - consider plant care.", NotificationSeverity.Info);
                }
            }
        }
        
        private void HandleGameDayChanged(int newDay)
        {
            Debug.Log($"[TimeDisplayExample] New game day: {newDay}");
            
            // Show day change notification
            if (_notificationManager != null)
            {
                _notificationManager.ShowNotification($"Day {newDay} has begun!", NotificationSeverity.Success);
            }
            
            // Example: Weekly events (every 7 days)
            if (newDay % 7 == 1 && newDay > 1)
            {
                if (_notificationManager != null)
                {
                    _notificationManager.ShowPersistentNotification(
                        "weekly_report",
                        "Weekly report available - check your progress!",
                        NotificationSeverity.Info
                    );
                }
            }
        }
        
        private void HandleTimeAccelerationChanged(float newAcceleration)
        {
            Debug.Log($"[TimeDisplayExample] Time acceleration changed to: {newAcceleration}x");
            
            // Example: Show notification for significant speed changes
            if (newAcceleration >= 8f)
            {
                if (_notificationManager != null)
                {
                    _notificationManager.ShowNotification("High speed mode active - monitor your operations!", NotificationSeverity.Warning);
                }
            }
        }
        
        private void HandlePenaltyLevelChanged(TimePenaltyLevel newLevel)
        {
            Debug.Log($"[TimeDisplayExample] Penalty level changed to: {newLevel}");
            
            // Show notifications for penalty changes
            string message = newLevel switch
            {
                TimePenaltyLevel.Warning => "Time pressure detected - optimize your workflow!",
                TimePenaltyLevel.Penalty => "Time penalty active - efficiency reduced!",
                TimePenaltyLevel.Critical => "CRITICAL: Severe time violations detected!",
                TimePenaltyLevel.None => "Time penalties cleared - good work!",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(message) && _notificationManager != null)
            {
                var severity = newLevel switch
                {
                    TimePenaltyLevel.Warning => NotificationSeverity.Warning,
                    TimePenaltyLevel.Penalty => NotificationSeverity.Error,
                    TimePenaltyLevel.Critical => NotificationSeverity.Critical,
                    _ => NotificationSeverity.Success
                };
                
                _notificationManager.ShowNotification(message, severity);
            }
        }
        
        private void HandleTimeEventTriggered(string eventMessage)
        {
            Debug.Log($"[TimeDisplayExample] Time event: {eventMessage}");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_timeDisplay != null)
            {
                _timeDisplay.OnGameTimeChanged -= HandleGameTimeChanged;
                _timeDisplay.OnGameDayChanged -= HandleGameDayChanged;
                _timeDisplay.OnTimeAccelerationChanged -= HandleTimeAccelerationChanged;
                _timeDisplay.OnPenaltyLevelChanged -= HandlePenaltyLevelChanged;
                _timeDisplay.OnTimeEventTriggered -= HandleTimeEventTriggered;
            }
        }
        
        #region Public API for Integration
        
        /// <summary>
        /// Set penalty based on game conditions
        /// </summary>
        public void SetPenaltyFromGameCondition(string condition, TimePenaltyLevel level)
        {
            if (_timeDisplay != null)
            {
                _timeDisplay.SetPenaltyLevel(level);
                
                if (_notificationManager != null)
                {
                    _notificationManager.ShowNotification($"Time penalty: {condition}", NotificationSeverity.Warning);
                }
            }
        }
        
        /// <summary>
        /// Clear penalty when conditions improve
        /// </summary>
        public void ClearPenaltyCondition(string condition)
        {
            if (_timeDisplay != null)
            {
                _timeDisplay.ClearPenalties();
                
                if (_notificationManager != null)
                {
                    _notificationManager.ShowNotification($"Penalty cleared: {condition}", NotificationSeverity.Success);
                }
            }
        }
        
        #endregion
        
        #if UNITY_EDITOR
        [Header("Editor Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        
        private void OnGUI()
        {
            if (!_showDebugInfo || _timeDisplay == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            var boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            
            GUILayout.Label("Time Display Debug", boldStyle);
            GUILayout.Label($"Game Time: {_timeDisplay.GetFormattedGameTime()}");
            GUILayout.Label($"Game Day: {_timeDisplay.CurrentGameDay}");
            GUILayout.Label($"Acceleration: {_timeDisplay.TimeAcceleration}x");
            GUILayout.Label($"Paused: {_timeDisplay.IsTimePaused}");
            GUILayout.Label($"Penalty Level: {_timeDisplay.CurrentPenaltyLevel}");
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:", boldStyle);
            GUILayout.Label($"Pause/Resume: {_pauseKey}");
            GUILayout.Label($"Speed Up: {_speedUpKey}");
            GUILayout.Label($"Slow Down: {_slowDownKey}");
            GUILayout.Label($"Test Penalty: {_penaltyKey}");
            GUILayout.EndArea();
        }
        #endif
    }
}

// Additional styles class for UI Toolkit styling
namespace ProjectChimera.UI.Styles
{
    public static class TimeDisplayStyles
    {
        public static class USS
        {
            public const string TimeDisplayRoot = @"
                .time-display-root {
                    position: absolute;
                    top: 20px;
                    right: 20px;
                    min-width: 200px;
                }
                
                .time-container {
                    background-color: rgba(26, 26, 26, 0.8);
                    border-radius: 8px;
                    padding: 12px 16px;
                    border-width: 2px;
                    border-color: rgb(204, 204, 204);
                    backdrop-filter: blur(4px);
                }
                
                .time-container--warning {
                    border-color: rgb(230, 179, 51);
                    animation: warning-pulse 0.5s ease-in-out infinite alternate;
                }
                
                .time-container--penalty {
                    border-color: rgb(204, 51, 51);
                    animation: penalty-pulse 0.25s ease-in-out infinite alternate;
                }
                
                .time-container--critical {
                    border-color: rgb(255, 26, 26);
                    animation: critical-pulse 0.1s ease-in-out infinite alternate;
                }
                
                @keyframes warning-pulse {
                    from { border-color: rgb(230, 179, 51); }
                    to { border-color: rgba(230, 179, 51, 0.3); }
                }
                
                @keyframes penalty-pulse {
                    from { border-color: rgb(204, 51, 51); }
                    to { border-color: rgba(204, 51, 51, 0.3); }
                }
                
                @keyframes critical-pulse {
                    from { border-color: rgb(255, 26, 26); }
                    to { border-color: rgba(255, 26, 26, 0.3); }
                }
                
                .game-time-label {
                    font-size: 16px;
                    -unity-font-style: bold;
                    color: rgb(204, 204, 204);
                    margin-bottom: 4px;
                }
                
                .real-time-label {
                    font-size: 12px;
                    color: rgb(179, 179, 179);
                    margin-bottom: 4px;
                }
                
                .time-acceleration-label {
                    font-size: 11px;
                    color: rgb(153, 204, 230);
                    margin-top: 4px;
                }
                
                .penalty-indicator {
                    height: 4px;
                    margin-top: 8px;
                    border-radius: 2px;
                    background-color: rgb(204, 204, 204);
                }
                
                .warning-flash {
                    position: absolute;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background-color: rgba(255, 0, 0, 0.3);
                    border-radius: 8px;
                    display: none;
                }
            ";
        }
    }
}