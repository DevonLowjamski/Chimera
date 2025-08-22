using UnityEngine;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Events
{
    /// <summary>
    /// Simple event channel that can be invoked without parameters
    /// Used for basic facility management events
    /// </summary>
    [CreateAssetMenu(fileName = "New Simple Game Event", menuName = "Project Chimera/Events/Simple Game Event", order = 100)]
    public class SimpleGameEventSO : ChimeraDataSO
    {
        private System.Action _listeners;

        /// <summary>
        /// Invoke the event, notifying all listeners
        /// </summary>
        public virtual void Invoke()
        {
            _listeners?.Invoke();
        }

        /// <summary>
        /// Subscribe to the event
        /// </summary>
        public virtual void Subscribe(System.Action listener)
        {
            _listeners += listener;
        }

        /// <summary>
        /// Unsubscribe from the event
        /// </summary>
        public virtual void Unsubscribe(System.Action listener)
        {
            _listeners -= listener;
        }

        /// <summary>
        /// Clear all listeners (useful for cleanup)
        /// </summary>
        public virtual void ClearAllListeners()
        {
            _listeners = null;
        }

        private void OnDestroy()
        {
            ClearAllListeners();
        }
    }
}