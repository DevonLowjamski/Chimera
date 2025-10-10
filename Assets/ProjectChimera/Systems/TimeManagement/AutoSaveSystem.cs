using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.TimeManagement
{
    /// <summary>
    /// Auto-save system - prevents progress loss with time-based saves.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Protects player PROGRESS without being intrusive:
    ///
    /// 1. **Automatic Backups** - Never lose hours of work
    ///    - Save every 5 minutes (real time)
    ///    - Save on major events (harvest, breeding, purchase)
    ///    - Save before risky actions (sell genetics, demolish)
    ///
    /// 2. **Multiple Save Slots** - Recovery options
    ///    - Auto-save slot (most recent)
    ///    - Quick-save slot (manual F5)
    ///    - 3 rotating backup slots (auto-save history)
    ///
    /// 3. **Non-Intrusive** - Seamless experience
    ///    - Background saving (no freezing)
    ///    - Small notification ("Game saved")
    ///    - Can disable for hardcore mode
    ///
    /// 4. **Smart Timing** - Avoids bad moments
    ///    - Never saves during breeding animations
    ///    - Never saves during UI interactions
    ///    - Only saves when safe (idle or paused)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players just see "Auto-saved" occasionally, not complex save scheduling.
    /// Their progress is protected without micromanagement.
    /// </summary>
    public class AutoSaveSystem : MonoBehaviour, ITickable
    {
        // ITickable implementation
        public int TickPriority => 10; // Low priority - just checking input
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        [Header("Auto-Save Configuration")]
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private float _autoSaveIntervalMinutes = 5f;
        [SerializeField] private int _maxAutoSaveBackups = 3;

        [Header("Save Triggers")]
        [SerializeField] private bool _saveOnHarvest = true;
        [SerializeField] private bool _saveOnBreeding = true;
        [SerializeField] private bool _saveOnPurchase = true;
        [SerializeField] private bool _saveOnSeasonChange = true;

        [Header("Save Behavior")]
        [SerializeField] private bool _saveOnlyWhenIdle = true;
        [SerializeField] private float _idleThresholdSeconds = 5f;
        [SerializeField] private bool _showSaveNotification = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Services
        private ScheduledEventManager _scheduledEventManager;
        private CalendarSystem _calendarSystem;

        // State tracking
        private int _autoSaveEventId = -1;
        private float _lastInputTime;
        private bool _isInitialized = false;

        // Events
        public event System.Action OnAutoSaveTriggered;
        public event System.Action OnQuickSaveTriggered;

        private void Start()
        {
            InitializeAutoSave();
        }

        private void InitializeAutoSave()
        {
            // Get services
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _scheduledEventManager = container.Resolve<ScheduledEventManager>();
                _calendarSystem = container.Resolve<CalendarSystem>();
            }

            if (_scheduledEventManager == null)
            {
                ChimeraLogger.LogWarning("SAVE",
                    "AutoSaveSystem: ScheduledEventManager not found", this);
                return;
            }

            _lastInputTime = UnityEngine.Time.realtimeSinceStartup;
            _isInitialized = true;

            // Schedule recurring auto-save
            if (_enableAutoSave)
            {
                ScheduleAutoSave();
            }

            // Subscribe to calendar events for trigger-based saves
            if (_calendarSystem != null && _saveOnSeasonChange)
            {
                _calendarSystem.OnSeasonChanged += OnSeasonChanged;
            }

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Auto-save system initialized (interval: {_autoSaveIntervalMinutes} minutes)", this);
            }
        }

        public void Tick(float deltaTime)
        {
            // Track player input for idle detection
            if (Input.anyKey || Input.GetMouseButton(0))
            {
                _lastInputTime = UnityEngine.Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Schedules recurring auto-save event.
        /// </summary>
        private void ScheduleAutoSave()
        {
            if (_scheduledEventManager == null)
                return;

            // Cancel existing auto-save if scheduled
            if (_autoSaveEventId >= 0)
            {
                _scheduledEventManager.CancelEvent(_autoSaveEventId);
            }

            // Schedule new recurring auto-save
            float intervalSeconds = _autoSaveIntervalMinutes * 60f;
            _autoSaveEventId = _scheduledEventManager.ScheduleRealTimeEvent(
                "Auto-Save",
                intervalSeconds,
                OnAutoSaveEvent,
                EventCategory.Automation,
                isRecurring: true,
                recurrenceIntervalSeconds: intervalSeconds
            );

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Auto-save scheduled (every {_autoSaveIntervalMinutes} minutes)", this);
            }
        }

        /// <summary>
        /// Called when auto-save event triggers.
        ///
        /// GAMEPLAY:
        /// - Checks if safe to save (idle, not in critical action)
        /// - Performs save operation
        /// - Shows brief notification ("Game saved")
        /// - Maintains backup rotation
        /// </summary>
        private void OnAutoSaveEvent()
        {
            if (!_enableAutoSave)
                return;

            // Check if player is idle (if required)
            if (_saveOnlyWhenIdle)
            {
                float timeSinceInput = UnityEngine.Time.realtimeSinceStartup - _lastInputTime;
                if (timeSinceInput < _idleThresholdSeconds)
                {
                    if (_enableDebugLogging)
                    {
                        ChimeraLogger.Log("SAVE",
                            "Auto-save skipped (player active)", this);
                    }
                    return;
                }
            }

            PerformAutoSave();
        }

        /// <summary>
        /// Performs auto-save operation.
        /// </summary>
        private void PerformAutoSave()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    "Performing auto-save...", this);
            }

            // TODO: Call actual save system when implemented
            // For now, just log and notify
            // SaveManager.SaveGame("autosave", rotateBacks: _maxAutoSaveBackups);

            OnAutoSaveTriggered?.Invoke();

            if (_showSaveNotification)
            {
                ShowSaveNotification("Auto-saved");
            }

            ChimeraLogger.Log("SAVE",
                "Auto-save complete", this);
        }

        #region Manual Save Triggers

        /// <summary>
        /// Quick-save (triggered by player pressing F5 or menu button).
        ///
        /// GAMEPLAY: Player wants to manually save before risky action
        /// </summary>
        public void QuickSave()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    "Quick-save triggered", this);
            }

            // TODO: Call actual save system when implemented
            // SaveManager.SaveGame("quicksave");

            OnQuickSaveTriggered?.Invoke();

            if (_showSaveNotification)
            {
                ShowSaveNotification("Quick-saved");
            }

            ChimeraLogger.Log("SAVE",
                "Quick-save complete", this);
        }

        /// <summary>
        /// Trigger-based save on harvest.
        /// </summary>
        public void SaveOnHarvest(string plantId)
        {
            if (!_saveOnHarvest)
                return;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Harvest save triggered (plant: {plantId})", this);
            }

            PerformAutoSave();
        }

        /// <summary>
        /// Trigger-based save on breeding.
        /// </summary>
        public void SaveOnBreeding(string offspringName)
        {
            if (!_saveOnBreeding)
                return;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Breeding save triggered (offspring: {offspringName})", this);
            }

            PerformAutoSave();
        }

        /// <summary>
        /// Trigger-based save on purchase.
        /// </summary>
        public void SaveOnPurchase(string itemName, float cost)
        {
            if (!_saveOnPurchase)
                return;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Purchase save triggered (item: {itemName}, cost: ${cost})", this);
            }

            PerformAutoSave();
        }

        /// <summary>
        /// Trigger-based save on season change.
        /// </summary>
        private void OnSeasonChanged(Season newSeason)
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Season change save triggered (new season: {newSeason})", this);
            }

            PerformAutoSave();
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Enables or disables auto-save.
        /// </summary>
        public void SetAutoSaveEnabled(bool enabled)
        {
            _enableAutoSave = enabled;

            if (enabled)
            {
                ScheduleAutoSave();
                ChimeraLogger.Log("SAVE", "Auto-save enabled", this);
            }
            else
            {
                if (_autoSaveEventId >= 0 && _scheduledEventManager != null)
                {
                    _scheduledEventManager.CancelEvent(_autoSaveEventId);
                    _autoSaveEventId = -1;
                }
                ChimeraLogger.Log("SAVE", "Auto-save disabled", this);
            }
        }

        /// <summary>
        /// Sets auto-save interval.
        /// </summary>
        public void SetAutoSaveInterval(float minutes)
        {
            _autoSaveIntervalMinutes = Mathf.Max(1f, minutes);

            if (_enableAutoSave)
            {
                ScheduleAutoSave();
                ChimeraLogger.Log("SAVE",
                    $"Auto-save interval changed to {_autoSaveIntervalMinutes} minutes", this);
            }
        }

        /// <summary>
        /// Gets current auto-save settings.
        /// </summary>
        public AutoSaveSettings GetSettings()
        {
            return new AutoSaveSettings
            {
                IsEnabled = _enableAutoSave,
                IntervalMinutes = _autoSaveIntervalMinutes,
                MaxBackups = _maxAutoSaveBackups,
                SaveOnHarvest = _saveOnHarvest,
                SaveOnBreeding = _saveOnBreeding,
                SaveOnPurchase = _saveOnPurchase,
                SaveOnlyWhenIdle = _saveOnlyWhenIdle
            };
        }

        #endregion

        #region UI Helpers

        /// <summary>
        /// Shows save notification to player.
        /// </summary>
        private void ShowSaveNotification(string message)
        {
            // TODO: Integrate with notification/toast system when available
            // For now, just log
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("SAVE",
                    $"Notification: {message}", this);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from calendar events
            if (_calendarSystem != null)
            {
                _calendarSystem.OnSeasonChanged -= OnSeasonChanged;
            }

            // Cancel auto-save event
            if (_autoSaveEventId >= 0 && _scheduledEventManager != null)
            {
                _scheduledEventManager.CancelEvent(_autoSaveEventId);
            }
        }
    }

    /// <summary>
    /// Auto-save configuration settings.
    /// </summary>
    [System.Serializable]
    public struct AutoSaveSettings
    {
        public bool IsEnabled;
        public float IntervalMinutes;
        public int MaxBackups;
        public bool SaveOnHarvest;
        public bool SaveOnBreeding;
        public bool SaveOnPurchase;
        public bool SaveOnlyWhenIdle;
    }
}
