// REFACTORED: Plant Event Data Structures
// Extracted from PlantEventCoordinator for better separation of concerns

using System;
using System.Collections.Generic;
using ProjectChimera.Data.Cultivation;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Types of plant events
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

