using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
// using ProjectChimera.Systems.Addressables; // Removed to avoid circular dependency
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectChimera.Core.Events;
using ProjectChimera.Shared;
using ProjectChimera.Core.Logging;


namespace ProjectChimera.Core
{
    /// <summary>
    /// Centralized event system coordinator for Project Chimera.
    /// Manages global event channels, monitors event traffic, and provides debugging capabilities.
    /// </summary>
    public class EventManager : DIChimeraManager, IGameStateListener
    {
        [Header("Event Configuration")]
        [SerializeField] private EventManagerConfigSO _config;
        [SerializeField] private bool _enableEventLogging = false;
        [SerializeField] private bool _enableEventMetrics = true;
        [SerializeField] private bool _enableEventDebugging = false;

        [Header("Event Performance")]
        [SerializeField] private int _maxEventsPerFrame = 100;
        [SerializeField] private float _eventQueueWarningThreshold = 50;

        [Header("Manager Events")]
        [SerializeField] private StringGameEventSO _onEventChannelRegistered;
        [SerializeField] private StringGameEventSO _onEventRaised;
        [SerializeField] private SimpleGameEventSO _onEventQueueOverflow;

        // Event channel registries
        private readonly Dictionary<Type, List<ChimeraEventSO>> _eventChannelsByType = new Dictionary<Type, List<ChimeraEventSO>>();
        private readonly Dictionary<string, ChimeraEventSO> _eventChannelsById = new Dictionary<string, ChimeraEventSO>();
        private readonly HashSet<ChimeraEventSO> _registeredChannels = new HashSet<ChimeraEventSO>();

        // Event metrics
        private readonly Dictionary<string, EventMetrics> _eventMetrics = new Dictionary<string, EventMetrics>();
        private readonly Queue<EventLogEntry> _eventLog = new Queue<EventLogEntry>();
        private const int MAX_EVENT_LOG_ENTRIES = 1000;

        // Performance tracking
        private int _eventsThisFrame = 0;
        private int _totalEventsRaised = 0;
        private float _lastMetricsReset = 0.0f;

        /// <summary>
        /// Total number of events raised since startup.
        /// </summary>
        public int TotalEventsRaised => _totalEventsRaised;

        /// <summary>
        /// Number of registered event channels.
        /// </summary>
        public int RegisteredChannelCount => _registeredChannels.Count;

        /// <summary>
        /// Whether event logging is enabled.
        /// </summary>
        public bool IsEventLoggingEnabled => _enableEventLogging;

        /// <summary>
        /// Whether event debugging is enabled.
        /// </summary>
        public bool IsEventDebuggingEnabled => _enableEventDebugging;

        protected override void OnManagerInitialize()
        {
            LogDebug("Initializing Event Manager");

            // Load configuration
            if (_config != null)
            {
                _enableEventLogging = _config.EnableEventLogging;
                _enableEventMetrics = _config.EnableEventMetrics;
                _enableEventDebugging = _config.EnableEventDebugging;
                _maxEventsPerFrame = _config.MaxEventsPerFrame;
                _eventQueueWarningThreshold = _config.EventQueueWarningThreshold;
            }

            // Auto-discover and register event channels
            _ = AutoDiscoverEventChannelsAsync(); // Fire and forget - don't block initialization

            _lastMetricsReset = Time.time;
            LogDebug($"Event Manager initialized - {RegisteredChannelCount} channels registered");
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("Shutting down Event Manager");

            // Clear all registries
            _eventChannelsByType.Clear();
            _eventChannelsById.Clear();
            _registeredChannels.Clear();
            _eventMetrics.Clear();
            _eventLog.Clear();

            _eventsThisFrame = 0;
            _totalEventsRaised = 0;
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            // Reset frame event counter
            _eventsThisFrame = 0;

            // Check for event queue warnings
            if (_eventLog.Count > _eventQueueWarningThreshold)
            {
                LogWarning($"Event queue size ({_eventLog.Count}) exceeds warning threshold ({_eventQueueWarningThreshold})");
                _onEventQueueOverflow?.Raise();
            }

            // Update metrics periodically
            if (_enableEventMetrics && Time.time - _lastMetricsReset > 60.0f) // Every minute
            {
                UpdateEventMetrics();
                _lastMetricsReset = Time.time;
            }
        }

        /// <summary>
        /// Auto-discovers and registers all event channels in the project.
        /// </summary>
        private async Task AutoDiscoverEventChannelsAsync()
        {
            LogDebug("Auto-discovering event channels");
            
            await Task.Yield(); // Make it async for consistency

            try
            {
#if UNITY_ADDRESSABLES
                // Use Addressables for proper async asset loading
                ChimeraLogger.Log("[EventManager] Loading event channels via Addressables");

                // Find all event channel assets using Addressables
                var eventChannelsHandle = Addressables.LoadAssetsAsync<ChimeraEventSO>("events", null);
                var eventChannelsList = await eventChannelsHandle.Task;

                foreach (var channel in eventChannelsList)
                {
                    RegisterEventChannel(channel);
                }

                ChimeraLogger.Log($"Auto-discovered {eventChannelsList.Count} event channels via Addressables");
#else
                // Fallback to Resources if Addressables not available
                ChimeraLogger.Log("[EventManager] Addressables not available, using Resources fallback");
                var eventChannels = Resources.LoadAll<ChimeraEventSO>("Events");

                foreach (var channel in eventChannels)
                {
                    RegisterEventChannel(channel);
                }

                ChimeraLogger.Log($"Auto-discovered {eventChannels.Length} event channels via Resources");
#endif
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[EventManager] Failed to auto-discover event channels: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers an event channel with the manager.
        /// </summary>
        public void RegisterEventChannel(ChimeraEventSO eventChannel)
        {
            if (eventChannel == null) return;

            // Avoid duplicate registration
            if (_registeredChannels.Contains(eventChannel))
            {
                return;
            }

            _registeredChannels.Add(eventChannel);

            // Register by type
            Type channelType = eventChannel.GetType();
            if (!_eventChannelsByType.ContainsKey(channelType))
            {
                _eventChannelsByType[channelType] = new List<ChimeraEventSO>();
            }
            _eventChannelsByType[channelType].Add(eventChannel);

            // Register by ID
            string channelId = eventChannel.UniqueID;
            if (!string.IsNullOrEmpty(channelId))
            {
                if (_eventChannelsById.ContainsKey(channelId))
                {
                    LogWarning($"Duplicate event channel ID detected: {channelId}");
                }
                _eventChannelsById[channelId] = eventChannel;
            }

            // Subscribe to the channel's events if we can
            SubscribeToEventChannel(eventChannel);

            if (_enableEventLogging)
            {
                LogDebug($"Registered event channel: {channelType.Name} - {eventChannel.name}");
            }

            _onEventChannelRegistered?.Raise($"{channelType.Name}:{eventChannel.name}");
        }

        /// <summary>
        /// Unregisters an event channel from the manager.
        /// </summary>
        public void UnregisterEventChannel(ChimeraEventSO eventChannel)
        {
            if (eventChannel == null || !_registeredChannels.Contains(eventChannel))
            {
                return;
            }

            _registeredChannels.Remove(eventChannel);

            // Remove from type registry
            Type channelType = eventChannel.GetType();
            if (_eventChannelsByType.TryGetValue(channelType, out List<ChimeraEventSO> channels))
            {
                channels.Remove(eventChannel);
                if (channels.Count == 0)
                {
                    _eventChannelsByType.Remove(channelType);
                }
            }

            // Remove from ID registry
            string channelId = eventChannel.UniqueID;
            if (!string.IsNullOrEmpty(channelId))
            {
                _eventChannelsById.Remove(channelId);
            }

            // Unsubscribe from the channel's events
            UnsubscribeFromEventChannel(eventChannel);

            if (_enableEventLogging)
            {
                LogDebug($"Unregistered event channel: {channelType.Name} - {eventChannel.name}");
            }
        }

        /// <summary>
        /// Subscribes to an event channel for monitoring purposes.
        /// </summary>
        private void SubscribeToEventChannel(ChimeraEventSO eventChannel)
        {
            // This would require extending the base event system to support manager monitoring
            // For now, we'll track events when they're raised through our monitoring system
        }

        /// <summary>
        /// Unsubscribes from an event channel.
        /// </summary>
        private void UnsubscribeFromEventChannel(ChimeraEventSO eventChannel)
        {
            // Counterpart to SubscribeToEventChannel
        }

        /// <summary>
        /// Called when an event is raised. Used for tracking and debugging.
        /// </summary>
        public void OnEventRaised(ChimeraEventSO eventChannel, object eventData = null)
        {
            if (eventChannel == null) return;

            _eventsThisFrame++;
            _totalEventsRaised++;

            // Check frame event limit
            if (_eventsThisFrame > _maxEventsPerFrame)
            {
                LogWarning($"Event limit exceeded this frame: {_eventsThisFrame}/{_maxEventsPerFrame}");
                return;
            }

            // Update metrics
            if (_enableEventMetrics)
            {
                UpdateEventMetrics(eventChannel);
            }

            // Log event
            if (_enableEventLogging || _enableEventDebugging)
            {
                LogEventRaised(eventChannel, eventData);
            }

            // Raise monitoring event
            _onEventRaised?.Raise($"{eventChannel.GetType().Name}:{eventChannel.name}");
        }

        /// <summary>
        /// Logs an event that was raised.
        /// </summary>
        private void LogEventRaised(ChimeraEventSO eventChannel, object eventData)
        {
            var logEntry = new EventLogEntry
            {
                Timestamp = Time.time,
                EventChannelName = eventChannel.name,
                EventChannelType = eventChannel.GetType().Name,
                EventData = eventData?.ToString() ?? "null",
                Frame = Time.frameCount
            };

            _eventLog.Enqueue(logEntry);

            // Limit log size
            while (_eventLog.Count > MAX_EVENT_LOG_ENTRIES)
            {
                _eventLog.Dequeue();
            }

            if (_enableEventDebugging)
            {
                LogDebug($"Event Raised: {logEntry.EventChannelType}.{logEntry.EventChannelName} | Data: {logEntry.EventData}");
            }
        }

        /// <summary>
        /// Triggers an event by name and data. Used by other systems to raise events.
        /// </summary>
        public void TriggerEvent(string eventName, object eventData = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                LogWarning("TriggerEvent called with null or empty event name");
                return;
            }

            // Find event channel by name
            var eventChannel = _eventChannelsById.Values.FirstOrDefault(ch => ch.name == eventName);
            if (eventChannel == null)
            {
                LogWarning($"Event channel not found: {eventName}");
                return;
            }

            // Trigger the event
            OnEventRaised(eventChannel, eventData);

            if (_enableEventDebugging)
            {
                LogDebug($"Triggered event: {eventName} with data: {eventData}");
            }
        }

        /// <summary>
        /// Updates metrics for a specific event channel.
        /// </summary>
        private void UpdateEventMetrics(ChimeraEventSO eventChannel)
        {
            string key = $"{eventChannel.GetType().Name}:{eventChannel.name}";
            
            if (!_eventMetrics.TryGetValue(key, out EventMetrics metrics))
            {
                metrics = new EventMetrics();
                _eventMetrics[key] = metrics;
            }

            metrics.TotalRaised++;
            metrics.LastRaisedTime = Time.time;
            metrics.RaisedThisSession++;
        }

        /// <summary>
        /// Updates all event metrics.
        /// </summary>
        private void UpdateEventMetrics()
        {
            foreach (var kvp in _eventMetrics.ToList())
            {
                var metrics = kvp.Value;
                float timeSinceLastRaised = Time.time - metrics.LastRaisedTime;
                
                // Calculate events per minute
                if (metrics.RaisedThisSession > 0)
                {
                    float sessionTime = Time.time - _lastMetricsReset;
                    metrics.EventsPerMinute = (metrics.RaisedThisSession / sessionTime) * 60.0f;
                }

                // Reset session counter
                metrics.RaisedThisSession = 0;
            }
        }

        /// <summary>
        /// Gets an event channel by its unique ID.
        /// </summary>
        public T GetEventChannel<T>(string channelId) where T : ChimeraEventSO
        {
            if (string.IsNullOrEmpty(channelId)) return null;

            if (_eventChannelsById.TryGetValue(channelId, out ChimeraEventSO channel))
            {
                return channel as T;
            }

            return null;
        }

        /// <summary>
        /// Gets all event channels of a specific type.
        /// </summary>
        public List<T> GetEventChannels<T>() where T : ChimeraEventSO
        {
            Type targetType = typeof(T);
            var results = new List<T>();

            if (_eventChannelsByType.TryGetValue(targetType, out List<ChimeraEventSO> channels))
            {
                foreach (var channel in channels)
                {
                    if (channel is T typedChannel)
                    {
                        results.Add(typedChannel);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets metrics for a specific event channel.
        /// </summary>
        public EventMetrics GetEventMetrics(ChimeraEventSO eventChannel)
        {
            if (eventChannel == null) return new EventMetrics();

            string key = $"{eventChannel.GetType().Name}:{eventChannel.name}";
            return _eventMetrics.TryGetValue(key, out EventMetrics metrics) ? metrics : new EventMetrics();
        }

        /// <summary>
        /// Gets all event metrics.
        /// </summary>
        public Dictionary<string, EventMetrics> GetAllEventMetrics()
        {
            return new Dictionary<string, EventMetrics>(_eventMetrics);
        }

        /// <summary>
        /// Gets recent event log entries.
        /// </summary>
        public List<EventLogEntry> GetRecentEventLog(int maxEntries = 50)
        {
            var entries = _eventLog.ToList();
            return entries.TakeLast(Mathf.Min(maxEntries, entries.Count)).ToList();
        }

        /// <summary>
        /// Clears all event metrics.
        /// </summary>
        public void ClearEventMetrics()
        {
            _eventMetrics.Clear();
            _totalEventsRaised = 0;
            LogDebug("Event metrics cleared");
        }

        /// <summary>
        /// Clears the event log.
        /// </summary>
        public void ClearEventLog()
        {
            _eventLog.Clear();
            LogDebug("Event log cleared");
        }

        /// <summary>
        /// Handles game state changes.
        /// </summary>
        public void OnGameStateChanged(GameState gameState)
        {
            if (_enableEventDebugging)
            {
                LogDebug($"Event Manager: Game state changed to {gameState}");
            }

            switch (gameState)
            {
                case GameState.Paused:
                    LogDebug("Event Manager: Game paused");
                    break;
                case GameState.Error:
                    LogWarning("Event Manager: Game entered error state, enabling event debugging");
                    if (!_enableEventDebugging)
                    {
                        _enableEventDebugging = true;
                    }
                    break;
                case GameState.Shutdown:
                    LogDebug("Event Manager: Game shutting down");
                    break;
            }
        }

        /// <summary>
        /// Handles game state changes with previous state context.
        /// </summary>
        public void OnGameStateChanged(GameState previousState, GameState newState)
        {
            if (_enableEventDebugging)
            {
                LogDebug($"Game state changed: {previousState} -> {newState}");
            }

            OnGameStateChanged(newState);

            switch (newState)
            {
                case GameState.Paused:
                    // Could pause event processing if needed
                    break;
                case GameState.Error:
                    // Could enable additional event logging
                    if (!_enableEventDebugging)
                    {
                        LogWarning("Enabling event debugging due to error state");
                        _enableEventDebugging = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Enables or disables event logging at runtime.
        /// </summary>
        public void SetEventLogging(bool enabled)
        {
            _enableEventLogging = enabled;
            LogDebug($"Event logging {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Enables or disables event debugging at runtime.
        /// </summary>
        public void SetEventDebugging(bool enabled)
        {
            _enableEventDebugging = enabled;
            LogDebug($"Event debugging {(enabled ? "enabled" : "disabled")}");
        }


    }

    /// <summary>
    /// Metrics for tracking event usage.
    /// </summary>
    [Serializable]
    public struct EventMetrics
    {
        public int TotalRaised;
        public int RaisedThisSession;
        public float LastRaisedTime;
        public float EventsPerMinute;
    }

    /// <summary>
    /// Log entry for event debugging.
    /// </summary>
    [Serializable]
    public struct EventLogEntry
    {
        public float Timestamp;
        public string EventChannelName;
        public string EventChannelType;
        public string EventData;
        public int Frame;
    }

    /// <summary>
    /// Configuration for Event Manager behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "Event Manager Config", menuName = "Project Chimera/Core/Event Manager Config")]
    public class EventManagerConfigSO : ChimeraConfigSO
    {
        [Header("Logging Settings")]
        public bool EnableEventLogging = false;
        public bool EnableEventMetrics = true;
        public bool EnableEventDebugging = false;

        [Header("Performance Settings")]
        [Range(10, 1000)]
        public int MaxEventsPerFrame = 100;
        
        [Range(10, 200)]
        public float EventQueueWarningThreshold = 50;

        [Header("Debug Settings")]
        public bool AutoEnableDebugOnError = true;
        public bool LogChannelRegistration = true;
        
        [Range(100, 10000)]
        public int MaxEventLogEntries = 1000;

        [Header("Metrics Settings")]
        [Range(10.0f, 300.0f)]
        public float MetricsUpdateInterval = 60.0f;
        
        public bool EnablePerformanceMetrics = true;
    }
}