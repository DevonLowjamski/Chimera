using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.TimeManagement
{
    /// <summary>
    /// Scheduled event manager - handles reminders, timers, and notifications.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// =====================================
    /// Keeps players INFORMED and ORGANIZED:
    ///
    /// 1. **Harvest Reminders** - Never miss optimal harvest
    ///    - "Blue Dream ready to harvest in 2 days"
    ///    - "OG Kush entered flush period"
    ///    - Notification when plant reaches peak ripeness
    ///
    /// 2. **Breeding Timers** - Track genetics projects
    ///    - "Tissue culture viability dropping (48 hours left)"
    ///    - "F2 seeds ready to germinate"
    ///    - "Pollination window closing (12 hours)"
    ///
    /// 3. **Maintenance Alerts** - Facility upkeep
    ///    - "Dehumidifier filter needs cleaning"
    ///    - "HVAC system maintenance due"
    ///    - "Reservoir water change recommended"
    ///
    /// 4. **Scheduled Actions** - Automation
    ///    - Auto-feed nutrients every 3 days
    ///    - Auto-save game every 5 minutes
    ///    - Weekly financial report
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see simple reminders, not complex event scheduling systems.
    /// They stay organized without micromanagement stress.
    /// </summary>
    public class ScheduledEventManager : MonoBehaviour, ITickable
    {
        [Header("Event Configuration")]
        [SerializeField] private int _maxActiveEvents = 100;
        [SerializeField] private bool _enableNotifications = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        // Active scheduled events
        private List<ScheduledEvent> _activeEvents = new List<ScheduledEvent>();
        private int _nextEventId = 1;

        // Services
        private CalendarSystem _calendarSystem;

        // Events for notifications
        public event Action<ScheduledEvent> OnEventTriggered;
        public event Action<ScheduledEvent> OnEventCancelled;

        // ITickable implementation
        public int TickPriority => -70; // After TimeManager and CalendarSystem
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Start()
        {
            InitializeManager();
        }

        private void InitializeManager()
        {
            // Get calendar system
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _calendarSystem = container.Resolve<CalendarSystem>();
            }

            if (_calendarSystem == null)
            {
                ChimeraLogger.LogWarning("TIME",
                    "ScheduledEventManager: CalendarSystem not found", this);
            }

            // Register with update orchestrator
            var orchestrator = container?.Resolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.RegisterTickable(this);
            }

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "ScheduledEventManager initialized", this);
            }
        }

        public void Tick(float deltaTime)
        {
            if (_calendarSystem == null)
                return;

            CheckScheduledEvents();
        }

        /// <summary>
        /// Checks all active events and triggers those that are due.
        /// </summary>
        private void CheckScheduledEvents()
        {
            var eventsToRemove = new List<ScheduledEvent>();

            foreach (var scheduledEvent in _activeEvents)
            {
                if (IsEventDue(scheduledEvent))
                {
                    TriggerEvent(scheduledEvent);

                    // Remove one-time events, keep recurring events
                    if (!scheduledEvent.IsRecurring)
                    {
                        eventsToRemove.Add(scheduledEvent);
                    }
                    else
                    {
                        // Schedule next occurrence
                        RescheduleRecurringEvent(scheduledEvent);
                    }
                }
            }

            // Remove triggered one-time events
            foreach (var eventToRemove in eventsToRemove)
            {
                _activeEvents.Remove(eventToRemove);
            }
        }

        /// <summary>
        /// Checks if an event is due to trigger.
        /// </summary>
        private bool IsEventDue(ScheduledEvent scheduledEvent)
        {
            switch (scheduledEvent.TriggerType)
            {
                case EventTriggerType.GameTime:
                    return _calendarSystem.TotalDaysElapsed >= scheduledEvent.TriggerDay &&
                           _calendarSystem.CurrentHour >= scheduledEvent.TriggerHour;

                case EventTriggerType.RealTime:
                    return UnityEngine.Time.realtimeSinceStartup >= scheduledEvent.TriggerRealTime;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Triggers an event and notifies listeners.
        /// </summary>
        private void TriggerEvent(ScheduledEvent scheduledEvent)
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"Event triggered: {scheduledEvent.EventName}", this);
            }

            // Invoke event callback
            scheduledEvent.Callback?.Invoke();

            // Notify listeners
            if (_enableNotifications)
            {
                OnEventTriggered?.Invoke(scheduledEvent);
            }
        }

        /// <summary>
        /// Reschedules a recurring event for its next occurrence.
        /// </summary>
        private void RescheduleRecurringEvent(ScheduledEvent scheduledEvent)
        {
            switch (scheduledEvent.TriggerType)
            {
                case EventTriggerType.GameTime:
                    scheduledEvent.TriggerDay += scheduledEvent.RecurrenceIntervalDays;
                    break;

                case EventTriggerType.RealTime:
                    scheduledEvent.TriggerRealTime += scheduledEvent.RecurrenceIntervalSeconds;
                    break;
            }

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"Recurring event rescheduled: {scheduledEvent.EventName}", this);
            }
        }

        #region Public API

        /// <summary>
        /// Schedules an event to trigger at a specific game time.
        ///
        /// GAMEPLAY:
        /// "Remind me when this plant is ready to harvest (in 7 game days)"
        /// "Schedule auto-feed for day 15"
        /// </summary>
        public int ScheduleGameTimeEvent(
            string eventName,
            int daysFromNow,
            int hour,
            Action callback,
            EventCategory category = EventCategory.General,
            bool isRecurring = false,
            int recurrenceIntervalDays = 1)
        {
            if (_activeEvents.Count >= _maxActiveEvents)
            {
                ChimeraLogger.LogWarning("TIME",
                    "Cannot schedule event: Maximum active events reached", this);
                return -1;
            }

            var scheduledEvent = new ScheduledEvent
            {
                EventId = _nextEventId++,
                EventName = eventName,
                Category = category,
                TriggerType = EventTriggerType.GameTime,
                TriggerDay = _calendarSystem.TotalDaysElapsed + daysFromNow,
                TriggerHour = hour,
                Callback = callback,
                IsRecurring = isRecurring,
                RecurrenceIntervalDays = recurrenceIntervalDays
            };

            _activeEvents.Add(scheduledEvent);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"Event scheduled: {eventName} (triggers in {daysFromNow} days at {hour}:00)", this);
            }

            return scheduledEvent.EventId;
        }

        /// <summary>
        /// Schedules an event to trigger after a real-time delay.
        ///
        /// GAMEPLAY:
        /// "Auto-save game every 5 minutes (real time)"
        /// "Show daily login bonus after 24 real hours"
        /// </summary>
        public int ScheduleRealTimeEvent(
            string eventName,
            float secondsFromNow,
            Action callback,
            EventCategory category = EventCategory.General,
            bool isRecurring = false,
            float recurrenceIntervalSeconds = 60f)
        {
            if (_activeEvents.Count >= _maxActiveEvents)
            {
                ChimeraLogger.LogWarning("TIME",
                    "Cannot schedule event: Maximum active events reached", this);
                return -1;
            }

            var scheduledEvent = new ScheduledEvent
            {
                EventId = _nextEventId++,
                EventName = eventName,
                Category = category,
                TriggerType = EventTriggerType.RealTime,
                TriggerRealTime = UnityEngine.Time.realtimeSinceStartup + secondsFromNow,
                Callback = callback,
                IsRecurring = isRecurring,
                RecurrenceIntervalSeconds = recurrenceIntervalSeconds
            };

            _activeEvents.Add(scheduledEvent);

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    $"Event scheduled: {eventName} (triggers in {secondsFromNow} seconds)", this);
            }

            return scheduledEvent.EventId;
        }

        /// <summary>
        /// Cancels a scheduled event.
        /// </summary>
        public bool CancelEvent(int eventId)
        {
            var eventToCancel = _activeEvents.Find(e => e.EventId == eventId);
            if (eventToCancel != null)
            {
                _activeEvents.Remove(eventToCancel);
                OnEventCancelled?.Invoke(eventToCancel);

                if (_enableDebugLogging)
                {
                    ChimeraLogger.Log("TIME",
                        $"Event cancelled: {eventToCancel.EventName}", this);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all active events.
        /// </summary>
        public List<ScheduledEvent> GetActiveEvents()
        {
            return new List<ScheduledEvent>(_activeEvents);
        }

        /// <summary>
        /// Gets active events by category.
        /// </summary>
        public List<ScheduledEvent> GetEventsByCategory(EventCategory category)
        {
            return _activeEvents.FindAll(e => e.Category == category);
        }

        /// <summary>
        /// Clears all scheduled events.
        /// </summary>
        public void ClearAllEvents()
        {
            _activeEvents.Clear();

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "All scheduled events cleared", this);
            }
        }

        #endregion

        #region ITickable Callbacks

        public void OnRegistered()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "ScheduledEventManager registered with UpdateOrchestrator", this);
            }
        }

        public void OnUnregistered()
        {
            if (_enableDebugLogging)
            {
                ChimeraLogger.Log("TIME",
                    "ScheduledEventManager unregistered from UpdateOrchestrator", this);
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
    /// Represents a scheduled event.
    /// </summary>
    [Serializable]
    public class ScheduledEvent
    {
        public int EventId;
        public string EventName;
        public EventCategory Category;
        public EventTriggerType TriggerType;

        // Game time trigger
        public int TriggerDay;
        public int TriggerHour;

        // Real time trigger
        public float TriggerRealTime;

        // Callback
        public Action Callback;

        // Recurring events
        public bool IsRecurring;
        public int RecurrenceIntervalDays;
        public float RecurrenceIntervalSeconds;
    }

    /// <summary>
    /// Event categories for organization.
    /// </summary>
    public enum EventCategory
    {
        General,
        Harvest,
        Breeding,
        Maintenance,
        Finance,
        Automation
    }

    /// <summary>
    /// Event trigger type.
    /// </summary>
    public enum EventTriggerType
    {
        GameTime,   // Triggers based on calendar system
        RealTime    // Triggers based on real elapsed time
    }
}
