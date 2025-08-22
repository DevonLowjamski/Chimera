using UnityEngine;
using UnityEngine.Events;

namespace ProjectChimera.Core.Events
{
    public abstract class GameEventListener<T, E, UER> : MonoBehaviour, IGameEventListener<T> where E : GameEventSO<T> where UER : UnityEvent<T>
    {
        [SerializeField] private E _event;
        public E Event
        {
            get { return _event; }
            set { _event = value; }
        }

        [SerializeField] private UER _response;

        private void OnEnable()
        {
            if (Event == null) return;
            Event.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (Event == null) return;
            Event.UnregisterListener(this);
        }

        public void OnEventRaised(T item)
        {
            _response.Invoke(item);
        }
    }

    public abstract class GameEventListener : MonoBehaviour
    {
        public abstract void OnEventRaised();
    }
}
