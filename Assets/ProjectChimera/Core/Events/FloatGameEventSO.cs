using UnityEngine;
using UnityEngine.Events;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Events
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [CreateAssetMenu(fileName = "New Float Game Event", menuName = "Project Chimera/Events/Float Game Event")]
    public class FloatGameEventSO : GameEventSO<float> { }
}
