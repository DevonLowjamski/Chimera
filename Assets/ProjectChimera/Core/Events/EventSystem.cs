using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Events
{
    /// <summary>
    /// Core Event System for Project Chimera
    /// Provides centralized event management and messaging
    /// </summary>
    public static class EventSystem
    {
        private static Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();
        private static Queue<IEvent> eventQueue = new Queue<IEvent>();
        private static bool isProcessingEvents = false;

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
                eventHandlers[eventType] = new List<Delegate>();

            eventHandlers[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            var eventType = typeof(T);

            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);

                if (eventHandlers[eventType].Count == 0)
                    eventHandlers.Remove(eventType);
            }
        }

        /// <summary>
        /// Publish an event immediately
        /// </summary>
        public static void Publish<T>(T eventData) where T : IEvent
        {
            var eventType = typeof(T);

            if (eventHandlers.ContainsKey(eventType))
            {
                foreach (var handler in eventHandlers[eventType])
                {
                    try
                    {
                        ((Action<T>)handler).Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("EventSystem", $"Handler exception: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Queue an event for later processing
        /// </summary>
        public static void QueueEvent<T>(T eventData) where T : IEvent
        {
            eventQueue.Enqueue(eventData);
        }

        /// <summary>
        /// Process all queued events
        /// </summary>
        public static void ProcessQueuedEvents()
        {
            if (isProcessingEvents) return;

            isProcessingEvents = true;

            while (eventQueue.Count > 0)
            {
                var eventData = eventQueue.Dequeue();
                PublishEvent(eventData);
            }

            isProcessingEvents = false;
        }

        private static void PublishEvent(IEvent eventData)
        {
            var eventType = eventData.GetType();

            if (eventHandlers.ContainsKey(eventType))
            {
                foreach (var handler in eventHandlers[eventType])
                {
                    try
                    {
                        handler.DynamicInvoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("EventSystem", $"DynamicInvoke exception: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clear all event handlers (useful for cleanup)
        /// </summary>
        public static void ClearAllHandlers()
        {
            eventHandlers.Clear();
            eventQueue.Clear();
        }

        /// <summary>
        /// Get the number of handlers for a specific event type
        /// </summary>
        public static int GetHandlerCount<T>() where T : IEvent
        {
            var eventType = typeof(T);
            return eventHandlers.ContainsKey(eventType) ? eventHandlers[eventType].Count : 0;
        }
    }

    /// <summary>
    /// Base interface for all events
    /// </summary>
    public interface IEvent
    {
        DateTime Timestamp { get; }
        string EventId { get; }
    }

    /// <summary>
    /// Base event implementation
    /// </summary>
    [System.Serializable]
    public abstract class BaseEvent : IEvent
    {
        public DateTime Timestamp { get; private set; }
        public string EventId { get; private set; }

        protected BaseEvent()
        {
            Timestamp = DateTime.Now;
            EventId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Event Manager for Unity integration
    /// </summary>
    public class EventManager : ChimeraManager, ProjectChimera.Core.Updates.ITickable{
        private static EventManager instance;
        public static EventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("EventManager");
                    instance = go.AddComponent<EventManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        protected override void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            base.Awake(); // Call ChimeraManager's Awake
        }

        public void Tick(float deltaTime)


        {
            // Process queued events each frame
            EventSystem.ProcessQueuedEvents();
        }

        protected override void OnManagerInitialize()
        {
            // Manager-specific initialization
            Logger.LogInfo("EventSystem", "EventManager initialized");
        }

        protected override void OnManagerShutdown()
        {
            // Manager-specific shutdown logic
            if (instance == this)
            {
                EventSystem.ClearAllHandlers();
                instance = null;
            }
        }

        protected override void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        /// <summary>
        /// ITickable Priority property
        /// </summary>
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.EventManager;

        /// <summary>
        /// ITickable Enabled property
        /// </summary>
        public bool IsTickable => isActiveAndEnabled;

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        // ITickable implementation removed - using the properties above

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }
    }
}
