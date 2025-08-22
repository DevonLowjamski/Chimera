using UnityEngine;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Events
{
    /// <summary>
    /// Generic typed event channel that can carry data with the event
    /// Base class for creating specific typed event channels
    /// </summary>
    /// <typeparam name="T">The type of data to pass with the event</typeparam>
    public abstract class TypedGameEventSO<T> : ChimeraDataSO
    {
        private System.Action<T> _listeners;

        /// <summary>
        /// Invoke the event with the specified data
        /// </summary>
        /// <param name="data">The data to pass to listeners</param>
        public virtual void Invoke(T data)
        {
            _listeners?.Invoke(data);
        }

        /// <summary>
        /// Subscribe to the event with a typed listener
        /// </summary>
        /// <param name="listener">The listener callback that accepts the event data</param>
        public virtual void Subscribe(System.Action<T> listener)
        {
            _listeners += listener;
        }

        /// <summary>
        /// Unsubscribe from the event
        /// </summary>
        /// <param name="listener">The listener to remove</param>
        public virtual void Unsubscribe(System.Action<T> listener)
        {
            _listeners -= listener;
        }

        /// <summary>
        /// Remove all listeners
        /// </summary>
        public virtual void Clear()
        {
            _listeners = null;
        }

        /// <summary>
        /// Get the number of active listeners
        /// </summary>
        public int ListenerCount => _listeners?.GetInvocationList().Length ?? 0;

        /// <summary>
        /// Check if there are any active listeners
        /// </summary>
        public bool HasListeners => _listeners != null;

        protected override void OnValidate()
        {
            // Validation can be overridden in derived classes
        }

        private void OnDisable()
        {
            // Clear listeners when the ScriptableObject is disabled
            Clear();
        }
    }
}