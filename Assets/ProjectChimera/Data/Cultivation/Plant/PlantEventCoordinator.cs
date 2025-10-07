using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using System.Linq;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Event Coordinator
    /// Single Responsibility: Event subscription management, change notification coordination, and event routing
    /// Extracted from PlantDataSynchronizer for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantEventCoordinator
    {
        [Header("Event Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableEventBatching = true;
        [SerializeField] private float _batchDelay = 0.1f; // seconds
        [SerializeField] private int _maxEventHistory = 500;

        // Component references
        private PlantIdentityManager _identityManager;
        private PlantStateCoordinator _stateCoordinator;
        private PlantResourceHandler _resourceHandler;
        private PlantGrowthProcessor _growthProcessor;
        private PlantHarvestOperator _harvestOperator;

        // Event management
        private Dictionary<EventType, List<EventSubscription>> _eventSubscriptions = new Dictionary<EventType, List<EventSubscription>>();
        private Queue<PlantEvent> _eventQueue = new Queue<PlantEvent>();
        private List<PlantEvent> _eventHistory = new List<PlantEvent>();
        private List<PlantEvent> _batchedEvents = new List<PlantEvent>();

        // State tracking
        private EventCoordinatorStats _stats = new EventCoordinatorStats();
        private bool _isInitialized = false;
        private float _lastBatchTime = 0f;

        // Events (external)
        public event System.Action<PlantEvent> OnEventReceived;
        public event System.Action<List<PlantEvent>> OnEventBatch;
        public event System.Action<EventType, int> OnSubscriptionChanged;
        public event System.Action<EventCoordinatorStats> OnStatsUpdated;

        public bool IsInitialized => _isInitialized;
        public EventCoordinatorStats Stats => _stats;
        public int QueuedEvents => _eventQueue.Count;
        public int HistoryCount => _eventHistory.Count;
        public bool EventBatchingEnabled => _enableEventBatching;

        public void Initialize(PlantIdentityManager identity, PlantStateCoordinator state,
                              PlantResourceHandler resources, PlantGrowthProcessor growth,
                              PlantHarvestOperator harvest)
        {
            if (_isInitialized) return;

            _identityManager = identity;
            _stateCoordinator = state;
            _resourceHandler = resources;
            _growthProcessor = growth;
            _harvestOperator = harvest;

            InitializeEventSubscriptions();
            SubscribeToComponentEvents();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Event Coordinator initialized with component references");
            }
        }

        /// <summary>
        /// Process queued events and handle batching
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            ProcessEventQueue();

            if (_enableEventBatching && _batchedEvents.Count > 0)
            {
                if (Time.time - _lastBatchTime >= _batchDelay)
                {
                    ProcessEventBatch();
                }
            }
        }

        /// <summary>
        /// Subscribe to specific event type
        /// </summary>
        public EventSubscription Subscribe(EventType eventType, System.Action<PlantEvent> callback, object subscriber = null)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", "Cannot subscribe - event coordinator not initialized");
                }
                return null;
            }

            var subscription = new EventSubscription
            {
                EventType = eventType,
                Callback = callback,
                Subscriber = subscriber,
                SubscriptionTime = DateTime.Now,
                IsActive = true
            };

            if (!_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventSubscriptions[eventType] = subscriptions;
            }

            subscriptions.Add(subscription);
            _stats.TotalSubscriptions++;

            OnSubscriptionChanged?.Invoke(eventType, subscriptions.Count);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Subscribed to {eventType} events ({subscriptions.Count} total subscribers)");
            }

            return subscription;
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        public bool Unsubscribe(EventSubscription subscription)
        {
            if (!_isInitialized || subscription == null) return false;

            if (_eventSubscriptions.TryGetValue(subscription.EventType, out var subscriptions))
            {
                if (subscriptions.Remove(subscription))
                {
                    subscription.IsActive = false;
                    _stats.TotalUnsubscriptions++;

                    OnSubscriptionChanged?.Invoke(subscription.EventType, subscriptions.Count);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("PLANT", $"Unsubscribed from {subscription.EventType} events ({subscriptions.Count} remaining)");
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Raise plant event
        /// </summary>
        public void RaiseEvent(EventType eventType, object eventData, string source = "Unknown")
        {
            if (!_isInitialized) return;

            var plantEvent = new PlantEvent
            {
                EventType = eventType,
                EventData = eventData,
                Source = source,
                Timestamp = DateTime.Now
            };

            QueueEvent(plantEvent);
        }

        /// <summary>
        /// Queue event for processing
        /// </summary>
        private void QueueEvent(PlantEvent plantEvent)
        {
            _eventQueue.Enqueue(plantEvent);
            _stats.EventsQueued++;

            // Add to history
            _eventHistory.Add(plantEvent);
            if (_eventHistory.Count > _maxEventHistory)
            {
                _eventHistory.RemoveAt(0);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Queued {plantEvent.EventType} event from {plantEvent.Source}");
            }
        }

        /// <summary>
        /// Process queued events
        /// </summary>
        private void ProcessEventQueue()
        {
            while (_eventQueue.Count > 0)
            {
                var plantEvent = _eventQueue.Dequeue();
                ProcessEvent(plantEvent);
            }
        }

        /// <summary>
        /// Process individual event
        /// </summary>
        private void ProcessEvent(PlantEvent plantEvent)
        {
            _stats.EventsProcessed++;

            // Add to batch if batching is enabled
            if (_enableEventBatching)
            {
                _batchedEvents.Add(plantEvent);
                _lastBatchTime = Time.time;
            }
            else
            {
                // Process immediately
                DispatchEvent(plantEvent);
            }

            OnEventReceived?.Invoke(plantEvent);
        }

        /// <summary>
        /// Process batched events
        /// </summary>
        private void ProcessEventBatch()
        {
            if (_batchedEvents.Count == 0) return;

            var batchCopy = new List<PlantEvent>(_batchedEvents);
            _batchedEvents.Clear();

            foreach (var plantEvent in batchCopy)
            {
                DispatchEvent(plantEvent);
            }

            OnEventBatch?.Invoke(batchCopy);
            _stats.EventBatches++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Processed event batch: {batchCopy.Count} events");
            }
        }

        /// <summary>
        /// Dispatch event to subscribers
        /// </summary>
        private void DispatchEvent(PlantEvent plantEvent)
        {
            if (!_eventSubscriptions.TryGetValue(plantEvent.EventType, out var subscriptions))
            {
                return;
            }

            var activeSubscriptions = 0;
            var failedCallbacks = 0;

            foreach (var subscription in subscriptions)
            {
                if (!subscription.IsActive) continue;

                activeSubscriptions++;

                try
                {
                    subscription.Callback?.Invoke(plantEvent);
                    _stats.CallbacksInvoked++;
                }
                catch (Exception ex)
                {
                    failedCallbacks++;
                    _stats.CallbackFailures++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("PLANT", $"Event callback failed for {plantEvent.EventType}: {ex.Message}");
                    }
                }
            }

            _stats.EventsDispatched++;

            if (_enableLogging && failedCallbacks > 0)
            {
                ChimeraLogger.LogWarning("PLANT", $"Event dispatch completed: {activeSubscriptions - failedCallbacks}/{activeSubscriptions} callbacks succeeded");
            }
        }

        /// <summary>
        /// Subscribe to all component events
        /// </summary>
        private void SubscribeToComponentEvents()
        {
            // Identity events
            if (_identityManager != null)
            {
                _identityManager.OnIdentityChanged += (oldId, newId) =>
                {
                    RaiseEvent(EventType.IdentityChanged, new IdentityChangeData { OldId = oldId, NewId = newId }, "IdentityManager");
                };

                _identityManager.OnValidationFailed += (errors) =>
                {
                    RaiseEvent(EventType.ValidationFailed, errors, "IdentityManager");
                };
            }

            // State events
            if (_stateCoordinator != null)
            {
                _stateCoordinator.OnGrowthStageChanged += (oldStage, newStage) =>
                {
                    RaiseEvent(EventType.GrowthStageChanged, new GrowthStageChangeData { OldStage = oldStage, NewStage = newStage }, "StateCoordinator");
                };

                _stateCoordinator.OnHealthChanged += (health) =>
                {
                    RaiseEvent(EventType.HealthChanged, health, "StateCoordinator");
                };

                _stateCoordinator.OnStressLevelChanged += (stress) =>
                {
                    RaiseEvent(EventType.StressChanged, stress, "StateCoordinator");
                };
            }

            // Resource events
            if (_resourceHandler != null)
            {
                _resourceHandler.OnWaterLevelChanged += (oldLevel, newLevel) =>
                {
                    RaiseEvent(EventType.ResourceChanged, new ResourceChangeData { ResourceType = "Water", OldLevel = oldLevel, NewLevel = newLevel }, "ResourceHandler");
                };

                _resourceHandler.OnNutrientLevelChanged += (oldLevel, newLevel) =>
                {
                    RaiseEvent(EventType.ResourceChanged, new ResourceChangeData { ResourceType = "Nutrient", OldLevel = oldLevel, NewLevel = newLevel }, "ResourceHandler");
                };

                _resourceHandler.OnCriticalResourceLevel += (resource) =>
                {
                    RaiseEvent(EventType.ResourceDeficiency, new ResourceDeficiencyData { ResourceType = resource, Severity = 1f }, "ResourceHandler");
                };
            }

            // Growth events
            if (_growthProcessor != null)
            {
                _growthProcessor.OnGrowthProgressChanged += (oldProgress, newProgress) =>
                {
                    RaiseEvent(EventType.GrowthProgressChanged, new GrowthProgressChangeData { OldProgress = oldProgress, NewProgress = newProgress }, "GrowthProcessor");
                };

                _growthProcessor.OnStageTransitionRecommended += (currentStage, recommendedStage) =>
                {
                    RaiseEvent(EventType.GrowthMilestone, new GrowthStageChangeData { OldStage = currentStage, NewStage = recommendedStage }, "GrowthProcessor");
                };
            }

            // Harvest events
            if (_harvestOperator != null)
            {
                _harvestOperator.OnReadinessChanged += (readiness) =>
                {
                    RaiseEvent(EventType.HarvestReadinessChanged, readiness, "HarvestOperator");
                };

                _harvestOperator.OnHarvestCompleted += (result) =>
                {
                    RaiseEvent(EventType.HarvestCompleted, result, "HarvestOperator");
                };
            }
        }

        /// <summary>
        /// Get event statistics
        /// </summary>
        public EventTypeStatistics GetEventTypeStatistics(EventType eventType)
        {
            var subscriptionCount = 0;
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
            {
                subscriptionCount = subscriptions.Count;
            }

            var eventCount = _eventHistory.Count(e => e.EventType == eventType);

            return new EventTypeStatistics
            {
                EventType = eventType,
                SubscriberCount = subscriptionCount,
                TotalEventsRaised = eventCount,
                LastEventTime = _eventHistory.LastOrDefault(e => e.EventType == eventType).Timestamp
            };
        }

        /// <summary>
        /// Get recent events of specific type
        /// </summary>
        public List<PlantEvent> GetRecentEvents(EventType? eventType = null, int maxEvents = 50)
        {
            var events = eventType.HasValue
                ? _eventHistory.Where(e => e.EventType == eventType.Value)
                : _eventHistory;

            return events.TakeLast(maxEvents).ToList();
        }

        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearEventHistory()
        {
            _eventHistory.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Event history cleared");
            }
        }

        /// <summary>
        /// Set event batching configuration
        /// </summary>
        public void SetEventBatching(bool enabled, float batchDelay = 0.1f)
        {
            _enableEventBatching = enabled;
            _batchDelay = Mathf.Max(0.01f, batchDelay);

            if (!enabled && _batchedEvents.Count > 0)
            {
                ProcessEventBatch();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Event batching {(enabled ? "enabled" : "disabled")} with {_batchDelay:F2}s delay");
            }
        }

        /// <summary>
        /// Initialize event subscription containers
        /// </summary>
        private void InitializeEventSubscriptions()
        {
            _eventSubscriptions.Clear();

            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                _eventSubscriptions[eventType] = new List<EventSubscription>();
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new EventCoordinatorStats();
        }

        /// <summary>
        /// Force process all pending events
        /// </summary>
        public void FlushEvents()
        {
            ProcessEventQueue();

            if (_batchedEvents.Count > 0)
            {
                ProcessEventBatch();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "All pending events flushed");
            }
        }

        /// <summary>
        /// Get comprehensive event summary
        /// </summary>
        public EventCoordinatorSummary GetEventSummary()
        {
            var typeStats = new Dictionary<EventType, EventTypeStatistics>();

            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                typeStats[eventType] = GetEventTypeStatistics(eventType);
            }

            return new EventCoordinatorSummary
            {
                Stats = _stats,
                EventTypeStatistics = typeStats,
                QueuedEvents = _eventQueue.Count,
                BatchedEvents = _batchedEvents.Count,
                HistoryEvents = _eventHistory.Count,
                IsInitialized = _isInitialized,
                BatchingEnabled = _enableEventBatching,
                LastUpdateTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Plant event types
    /// </summary>
    public enum EventType
    {
        IdentityChanged = 0,
        ValidationFailed = 1,
        GrowthStageChanged = 2,
        HealthChanged = 3,
        StressChanged = 4,
        ResourceChanged = 5,
        ResourceDeficiency = 6,
        GrowthProgressChanged = 7,
        GrowthMilestone = 8,
        HarvestReadinessChanged = 9,
        HarvestCompleted = 10,
        SyncOperationComplete = 11,
        PerformanceAlert = 12
    }

    /// <summary>
    /// Plant event structure
    /// </summary>
    [System.Serializable]
    public struct PlantEvent
    {
        public EventType EventType;
        public object EventData;
        public string Source;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Event subscription
    /// </summary>
    public class EventSubscription
    {
        public EventType EventType;
        public System.Action<PlantEvent> Callback;
        public object Subscriber;
        public DateTime SubscriptionTime;
        public bool IsActive;
    }

    /// <summary>
    /// Event coordinator statistics
    /// </summary>
    [System.Serializable]
    public struct EventCoordinatorStats
    {
        public int EventsQueued;
        public int EventsProcessed;
        public int EventsDispatched;
        public int EventBatches;
        public int TotalSubscriptions;
        public int TotalUnsubscriptions;
        public int CallbacksInvoked;
        public int CallbackFailures;
    }

    /// <summary>
    /// Event type statistics
    /// </summary>
    [System.Serializable]
    public struct EventTypeStatistics
    {
        public EventType EventType;
        public int SubscriberCount;
        public int TotalEventsRaised;
        public DateTime LastEventTime;
    }

    /// <summary>
    /// Event coordinator summary
    /// </summary>
    [System.Serializable]
    public struct EventCoordinatorSummary
    {
        public EventCoordinatorStats Stats;
        public Dictionary<EventType, EventTypeStatistics> EventTypeStatistics;
        public int QueuedEvents;
        public int BatchedEvents;
        public int HistoryEvents;
        public bool IsInitialized;
        public bool BatchingEnabled;
        public DateTime LastUpdateTime;
    }

    // Event data structures
    [System.Serializable]
    public struct IdentityChangeData
    {
        public string OldId;
        public string NewId;
    }

    [System.Serializable]
    public struct GrowthStageChangeData
    {
        public PlantGrowthStage OldStage;
        public PlantGrowthStage NewStage;
    }

    [System.Serializable]
    public struct ResourceChangeData
    {
        public string ResourceType;
        public float OldLevel;
        public float NewLevel;
    }

    [System.Serializable]
    public struct ResourceDeficiencyData
    {
        public string ResourceType;
        public float Severity;
    }

    [System.Serializable]
    public struct GrowthProgressChangeData
    {
        public float OldProgress;
        public float NewProgress;
    }
}
