using UnityEngine;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// Minimal adaptive LOD policy to replace missing LODAdaptiveSystem.
    /// Encapsulates any adaptive adjustments to calculated LOD levels.
    /// </summary>
    public class LODAdaptivePolicy : MonoBehaviour
    {
        [SerializeField] private int _maxLODLevel = 4;
        [SerializeField] private int _minLODLevel = 0;

        public int ApplyAdaptiveAdjustment(int calculatedLod)
        {
            return Mathf.Clamp(calculatedLod, _minLODLevel, _maxLODLevel);
        }

        public void SetAdaptiveLODEnabled(bool enabled)
        {
            // Minimal no-op to satisfy legacy calls
            this.enabled = enabled;
        }
    }
}


