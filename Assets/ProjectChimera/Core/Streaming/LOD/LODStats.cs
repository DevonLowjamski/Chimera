using UnityEngine;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// Minimal statistics tracker to replace missing LODStatistics.
    /// Tracks basic counts and events to enable logging/monitoring.
    /// </summary>
    // NOTE: This MonoBehaviour definition conflicts with LODStatistics.LODStats struct.
    // Renaming to avoid CS0101 duplicate type definition within the same namespace.
    public class LODStatsBehaviour : MonoBehaviour
    {
        [SerializeField] private int _registeredObjects;
        [SerializeField] private int _unregisteredObjects;
        [SerializeField] private int _lodChanges;
        [SerializeField] private int _updateCycles;

        public int RegisteredObjects => _registeredObjects;
        public int UnregisteredObjects => _unregisteredObjects;
        public int LODChanges => _lodChanges;
        public int UpdateCycles => _updateCycles;

        public void OnObjectRegistered() => _registeredObjects++;
        public void OnObjectUnregistered() => _unregisteredObjects++;
        public void OnLODChanged(int previousLod, int newLod) => _lodChanges++;
        public void OnUpdateCycle() => _updateCycles++;
    }
}


