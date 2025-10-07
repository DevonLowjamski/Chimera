using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using System.Linq;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Event Coordinator - Coordinator
    /// Single Responsibility: Event subscription management and change notification coordination
    /// Uses: PlantEventDataStructures.cs for data types
    /// </summary>
    [System.Serializable]
    public class PlantEventCoordinator
    {
        [Header("Event Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableEventBatching = true;
        [SerializeField] private float _batchDelay = 0.1f;
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

        // Events
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
            _stats = new EventCoordinatorStats();
            _isInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("PLANT", "Event Coordinator initialized");
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            ProcessEventQueue();

            if (_enableEventBatching && _batchedEvents.Count > 0)
            {
                if (Time.time - _lastBatchTime >= _batchDelay)
                    ProcessEventBatch();
            }
        }

        public EventSubscription Subscribe(EventType eventType, System.Action<PlantEvent> callback, object subscriber = null)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("PLANT", "Cannot subscribe - coordinator not initialized");
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
                ChimeraLogger.Log("PLANT", $"Subscribed to {eventType} ({subscriptions.Count} subscribers)");

            return subscription;
        }

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
                        ChimeraLogger.Log("PLANT", $"Unsubscribed from {subscription.EventType}");

                    return true;
                }
            }

            return false;
        }

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

        public List<PlantEvent> GetEventHistory(EventType? filterType = null, int? maxResults = null)
        {
            IEnumerable<PlantEvent> query = _eventHistory;

            if (filterType.HasValue)
                query = query.Where(e => e.EventType == filterType.Value);

            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return query.ToList();
        }

        public EventCoordinatorSummary GetSummary()
        {
            return new EventCoordinatorSummary
            {
                Stats = _stats,
                EventTypeStatistics = GetEventTypeStatistics(),
                QueuedEvents = _eventQueue.Count,
                BatchedEvents = _batchedEvents.Count,
                HistoryEvents = _eventHistory.Count,
                IsInitialized = _isInitialized,
                BatchingEnabled = _enableEventBatching,
                LastUpdateTime = DateTime.Now
            };
        }

        private void QueueEvent(PlantEvent plantEvent)
        {
            _eventQueue.Enqueue(plantEvent);
            _stats.EventsQueued++;

            _eventHistory.Add(plantEvent);
            if (_eventHistory.Count > _maxEventHistory)
                _eventHistory.RemoveAt(0);

            if (_enableLogging)
                ChimeraLogger.Log("PLANT", $"Queued {plantEvent.EventType} from {plantEvent.Source}");
        }

        private void ProcessEventQueue()
        {
            while (_eventQueue.Count > 0)
            {
                var plantEvent = _eventQueue.Dequeue();
                ProcessEvent(plantEvent);
            }
        }

        private void ProcessEvent(PlantEvent plantEvent)
        {
            _stats.EventsProcessed++;

            if (_enableEventBatching)
            {
                _batchedEvents.Add(plantEvent);
                _lastBatchTime = Time.time;
            }
            else
            {
                DispatchEvent(plantEvent);
            }

            OnEventReceived?.Invoke(plantEvent);
        }

        private void ProcessEventBatch()
        {
            if (_batchedEvents.Count == 0) return;

            var batchCopy = new List<PlantEvent>(_batchedEvents);
            _batchedEvents.Clear();

            foreach (var plantEvent in batchCopy)
                DispatchEvent(plantEvent);

            _stats.EventBatches++;
            OnEventBatch?.Invoke(batchCopy);

            if (_enableLogging)
                ChimeraLogger.Log("PLANT", $"Processed event batch: {batchCopy.Count} events");
        }

        private void DispatchEvent(PlantEvent plantEvent)
        {
            if (!_eventSubscriptions.TryGetValue(plantEvent.EventType, out var subscriptions)) return;

            _stats.EventsDispatched++;

            foreach (var subscription in subscriptions.Where(s => s.IsActive).ToList())
            {
                try
                {
                    subscription.Callback?.Invoke(plantEvent);
                    _stats.CallbacksInvoked++;
                }
                catch (Exception ex)
                {
                    _stats.CallbackFailures++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("PLANT", $"Event callback failed: {ex.Message}", null);
                }
            }
        }

        private void InitializeEventSubscriptions()
        {
            _eventSubscriptions.Clear();
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
                _eventSubscriptions[eventType] = new List<EventSubscription>();
        }

        private void SubscribeToComponentEvents()
        {
            if (_identityManager != null)
            {
                _identityManager.OnIdentityChanged += (oldId, newId) =>
                    RaiseEvent(EventType.IdentityChanged, new IdentityChangeData { OldId = oldId, NewId = newId }, "IdentityManager");
            }

            if (_stateCoordinator != null)
            {
                _stateCoordinator.OnGrowthStageChanged += (oldStage, newStage) =>
                    RaiseEvent(EventType.GrowthStageChanged, new GrowthStageChangeData { OldStage = oldStage, NewStage = newStage }, "StateCoordinator");
            }

            if (_resourceHandler != null)
            {
                _resourceHandler.OnWaterLevelChanged += (oldLevel, newLevel) =>
                    RaiseEvent(EventType.ResourceChanged, new ResourceChangeData { ResourceType = "Water", OldLevel = oldLevel, NewLevel = newLevel }, "ResourceHandler");

                _resourceHandler.OnNutrientLevelChanged += (oldLevel, newLevel) =>
                    RaiseEvent(EventType.ResourceChanged, new ResourceChangeData { ResourceType = "Nutrients", OldLevel = oldLevel, NewLevel = newLevel }, "ResourceHandler");

                _resourceHandler.OnCriticalResourceLevel += (resourceType) =>
                    RaiseEvent(EventType.ResourceDeficiency, new ResourceDeficiencyData { ResourceType = resourceType, Severity = 1.0f }, "ResourceHandler");
            }

            if (_growthProcessor != null)
            {
                _growthProcessor.OnGrowthProgressChanged += (oldProgress, newProgress) =>
                    RaiseEvent(EventType.GrowthProgressChanged, new GrowthProgressChangeData { OldProgress = oldProgress, NewProgress = newProgress }, "GrowthProcessor");
            }

            if (_harvestOperator != null)
            {
                _harvestOperator.OnHarvestCompleted += (result) =>
                    RaiseEvent(EventType.HarvestCompleted, result, "HarvestOperator");
            }

            if (_enableLogging)
                ChimeraLogger.Log("PLANT", "Subscribed to all component events");
        }

        private Dictionary<EventType, EventTypeStatistics> GetEventTypeStatistics()
        {
            var stats = new Dictionary<EventType, EventTypeStatistics>();

            foreach (var eventType in _eventSubscriptions.Keys)
            {
                var typeEvents = _eventHistory.Where(e => e.EventType == eventType).ToList();
                stats[eventType] = new EventTypeStatistics
                {
                    EventType = eventType,
                    SubscriberCount = _eventSubscriptions[eventType].Count(s => s.IsActive),
                    TotalEventsRaised = typeEvents.Count,
                    LastEventTime = typeEvents.Any() ? typeEvents.Max(e => e.Timestamp) : DateTime.MinValue
                };
            }

            return stats;
        }
    }
}

