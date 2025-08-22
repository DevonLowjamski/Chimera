using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Core.Events
{
    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<IGameEventListener<T>> _listeners = new List<IGameEventListener<T>>();

        public void Raise(T item)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(item);
            }
        }

        public void RegisterListener(IGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IGameEventListener<T> listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
            }
        }
    }
}
