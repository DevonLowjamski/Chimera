using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Streaming.LOD;

namespace ProjectChimera.Core.Streaming
{
    /// <summary>
    /// REFACTORED: Legacy LOD Manager - Now delegates to LODCore
    /// Maintains backward compatibility while using the new focused architecture
    /// </summary>
    public class LODManager : MonoBehaviour, ITickable
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useLegacyMode = false;

        // Core LOD system
        private LODCore _lodCore;

        // Singleton pattern
        private static LODManager _instance;
        public static LODManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var serviceContainer = ServiceContainerFactory.Instance;
                    _instance = serviceContainer?.TryResolve<LODManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("LODManager");
                        _instance = go.AddComponent<LODManager>();
                        DontDestroyOnLoad(go);
                        serviceContainer?.RegisterSingleton<LODManager>(_instance);
                    }
                }
                return _instance;
            }
        }

        // Legacy compatibility properties
        public bool IsInitialized => _lodCore?.IsInitialized ?? false;
        public Transform LODCenter => _lodCore?.LODCenter;
        public int RegisteredObjectCount => _lodCore?.RegisteredObjectCount ?? 0;

        // ITickable implementation
        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (_useLegacyMode)
            {
                // Legacy mode - keep original behavior
                if (_enableLogging)
                    ChimeraLogger.Log("LOD", "⚠️ Using legacy LOD mode", this);
                return;
            }
            // New core system handles its own updates via ITickable
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize LOD manager - now delegates to LODCore
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            if (_useLegacyMode)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("LOD", "⚠️ Using legacy LOD management mode", this);
                return;
            }

            // Initialize the new core system
            if (_lodCore == null)
            {
                _lodCore = LODCore.Instance;
            }

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LOD Manager initialized with new architecture", this);
        }

        /// <summary>
        /// Set the LOD center (usually main camera) - delegates to LODCore
        /// </summary>
        public void SetLODCenter(Transform center)
        {
            if (_lodCore != null)
            {
                _lodCore.SetLODCenter(center);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("LOD", "⚠️ Cannot set LOD center: LODCore not initialized", this);
            }
        }

        /// <summary>
        /// Register object for LOD management - delegates to LODCore
        /// </summary>
        public int RegisterLODObject(GameObject gameObject, LODObjectType objectType = LODObjectType.Standard, float customBias = 1f)
        {
            if (_lodCore != null)
            {
                var mappedType = (ProjectChimera.Core.Streaming.LOD.LODObjectType)System.Enum.Parse(typeof(ProjectChimera.Core.Streaming.LOD.LODObjectType), objectType.ToString());
                return _lodCore.RegisterLODObject(gameObject, mappedType, customBias);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("LOD", "⚠️ Cannot register LOD object: LODCore not initialized", this);
            }
            return -1;
        }

        /// <summary>
        /// Unregister object from LOD management - delegates to LODCore
        /// </summary>
        public void UnregisterLODObject(int objectId)
        {
            if (_lodCore != null)
            {
                _lodCore.UnregisterLODObject(objectId);
            }
        }

        /// <summary>
        /// Force update LOD level for specific object - delegates to LODCore
        /// </summary>
        public void ForceUpdateLOD(int objectId, int lodLevel = -1)
        {
            if (_lodCore != null)
            {
                _lodCore.ForceUpdateLOD(objectId, lodLevel);
            }
        }

        /// <summary>
        /// Set adaptive LOD enabled - delegates to LODCore
        /// </summary>
        public void SetAdaptiveLOD(bool enabled)
        {
            if (_lodCore?.AdaptiveSystem != null)
            {
                _lodCore.AdaptiveSystem.SetAdaptiveLODEnabled(enabled);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("LOD", "⚠️ Cannot set adaptive LOD: AdaptiveSystem not initialized", this);
            }
        }

        /// <summary>
        /// Get LOD statistics - delegates to LODCore
        /// </summary>
        public ProjectChimera.Core.Streaming.LOD.LODStats GetStats()
        {
            if (_lodCore?.Statistics != null)
            {
                return _lodCore.Statistics.CurrentStats;
            }
            return default(ProjectChimera.Core.Streaming.LOD.LODStats);
        }

        /// <summary>
        /// Set LOD quality profile - delegates to LODCore
        /// </summary>
        public void SetQualityProfile(int profileIndex)
        {
            if (_lodCore?.DistanceCalculator != null)
            {
                _lodCore.DistanceCalculator.SetQualityProfile(profileIndex);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("LOD", "⚠️ Cannot set quality profile: DistanceCalculator not initialized", this);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "LOD Manager destroyed", this);
        }
    }

    #region Legacy Compatibility Types

    /// <summary>
    /// Legacy LOD object types - kept for backwards compatibility
    /// New code should use ProjectChimera.Core.Streaming.LOD.LODObjectType
    /// </summary>
    public enum LODObjectType
    {
        Standard,
        Plant,
        Building,
        Equipment,
        UI,
        Effect
    }

    #endregion
}
