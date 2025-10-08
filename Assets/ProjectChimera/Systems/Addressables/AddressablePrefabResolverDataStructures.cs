// REFACTORED: Addressable Prefab Resolver Data Structures
// Extracted from AddressablePrefabResolver for better separation of concerns

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// Prefab resolver statistics
    /// </summary>
    public struct PrefabResolverStats
    {
        public bool IsInitialized;
        public int LoadedPrefabs;
        public int ActiveInstances;
        public int PooledInstances;
        public int PreloadedPrefabs;
        public bool PoolingEnabled;
        public int MaxPoolSize;
    }
}

