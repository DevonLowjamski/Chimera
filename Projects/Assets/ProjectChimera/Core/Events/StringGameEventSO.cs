using UnityEngine;
using UnityEngine.Events;

namespace ProjectChimera.Core.Events
{
    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    [CreateAssetMenu(fileName = "New String Game Event", menuName = "Project Chimera/Events/String Game Event")]
    public class StringGameEventSO : GameEventSO<string> { }
}
