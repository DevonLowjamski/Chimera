using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for event management operations
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        /// Subscribe to an event
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="handler">The event handler</param>
        void Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="handler">The event handler</param>
        void Unsubscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Publish an event
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="eventData">The event data</param>
        void Publish<T>(T eventData) where T : class;

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Get the number of subscribers for an event type
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <returns>Number of subscribers</returns>
        int GetSubscriberCount<T>() where T : class;
    }
}