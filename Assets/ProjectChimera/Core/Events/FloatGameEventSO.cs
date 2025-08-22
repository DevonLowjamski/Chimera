using UnityEngine;
using UnityEngine.Events;

namespace ProjectChimera.Core.Events
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [CreateAssetMenu(fileName = "New Float Game Event", menuName = "Project Chimera/Events/Float Game Event")]
    public class FloatGameEventSO : GameEventSO<float> { }
}
