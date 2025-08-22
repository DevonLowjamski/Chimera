using UnityEngine;

namespace ProjectChimera.Core.Events
{
    [CreateAssetMenu(fileName = "New Game Event Channel", menuName = "Project Chimera/Events/Game Event Channel")]
    public class GameEventChannelSO : ChimeraEventSO
    {
        public void Raise()
        {
            // Implementation for raising a generic game event
        }
    }
}
