using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core.Memory;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Streaming.LOD;
using ProjectChimera.Core.Streaming.Subsystems;
using System.Linq;
using ProjectChimera.Core.Streaming.Core;
using SubStreamingCore = ProjectChimera.Core.Streaming.Subsystems.StreamingCore;

namespace ProjectChimera.Core.Streaming
{
    /// <summary>
    /// REFACTORED: Streaming Coordinator - Legacy wrapper for backward compatibility
    /// Delegates to specialized StreamingCore for all streaming coordination
    /// Single Responsibility: Backward compatibility delegation
    /// </summary>
    public class StreamingCoordinator : MonoBehaviour, ITickable
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableLegacyMode = true;

        // Delegation target - the actual streaming coordination system
        private SubStreamingCore _streamingCore;

        // Legacy properties for backward compatibility
        public bool IsInitialized => _streamingCore?.IsInitialized ?? false;
        public StreamingCoordinatorStats Stats => _streamingCore?.Stats ?? new StreamingCoordinatorStats();

        // Legacy events for backward compatibility
        public System.Action OnStreamingInitialized;
        public System.Action<int> OnQualityChanged;
        public System.Action<StreamingSystemHealth> OnHealthChanged;

        // Legacy singleton pattern for backward compatibility
        private static StreamingCoordinator _instance;
        public static StreamingCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ServiceContainerFactory.Instance?.TryResolve<StreamingCoordinator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("StreamingCoordinator");
                        _instance = go.AddComponent<StreamingCoordinator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public int TickPriority => 70;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeStreamingCore();
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the streaming core system
        /// </summary>
        private void InitializeStreamingCore()
        {
            _streamingCore = GetComponent<SubStreamingCore>();
            if (_streamingCore == null)
            {
                _streamingCore = gameObject.AddComponent<SubStreamingCore>();
            }

            // Connect legacy events to the new core
            ConnectLegacyEvents();

            // Start initialization
            StartCoroutine(InitializeStreamingSystems());
        }

        /// <summary>
        /// Connect legacy events to the new streaming core
        /// </summary>
        private void ConnectLegacyEvents()
        {
            if (_streamingCore != null)
            {
                _streamingCore.OnStreamingInitialized += () => OnStreamingInitialized?.Invoke();
                _streamingCore.OnQualityChanged += (index) => OnQualityChanged?.Invoke(index);
                _streamingCore.OnHealthChanged += (health) => OnHealthChanged?.Invoke(health);
            }
        }

        /// <summary>
        /// Initialize streaming systems (Legacy method - delegates to StreamingCore)
        /// </summary>
        private IEnumerator InitializeStreamingSystems()
        {
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🔄 StreamingCoordinator (Legacy) initialization started", this);

            // Delegate to the streaming core - it handles all initialization
            _streamingCore?.StartInitialization();

            // Wait for initialization to complete
            yield return new WaitUntil(() => _streamingCore?.IsInitialized ?? false);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "StreamingCoordinator (Legacy) initialization delegated to StreamingCore", this);
        }

        /// <summary>
        /// Set streaming quality profile (Legacy method - delegates to StreamingCore)
        /// </summary>
        public void SetQualityProfile(int profileIndex)
        {
            _streamingCore?.SetQualityProfile(profileIndex);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Quality profile set to index {profileIndex} via legacy method", this);
        }

        /// <summary>
        /// Get system health (Legacy method - delegates to StreamingCore)
        /// </summary>
        public StreamingSystemHealth GetSystemHealth()
        {
            return _streamingCore?.SystemHealth ?? StreamingSystemHealth.Healthy;
        }

        /// <summary>
        /// Force garbage collection (Legacy method - delegates to StreamingCore)
        /// </summary>
        public void ForceGarbageCollection()
        {
            _streamingCore?.ForceGarbageCollection();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Garbage collection forced via legacy method", this);
        }

        /// <summary>
        /// Optimize streaming systems (Legacy method - delegates to StreamingCore)
        /// </summary>
        public void OptimizeStreaming()
        {
            _streamingCore?.OptimizeStreaming();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Streaming optimization triggered via legacy method", this);
        }

        public void Tick(float deltaTime)
        {
            if (!_enableLegacyMode) return;

            // Delegate to streaming core - it handles all coordination
            // The core system automatically runs coordination via its own Tick method
            if (_enableLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                ChimeraLogger.Log("STREAMING", "Legacy coordinator tick delegated to StreamingCore", this);
        }

        /// <summary>
        /// Get available quality profiles (Legacy method - delegates to StreamingCore)
        /// </summary>
        public StreamingQualityProfile[] GetAvailableQualityProfiles()
        {
            return _streamingCore?.GetAvailableQualityProfiles() ?? new StreamingQualityProfile[0];
        }

        /// <summary>
        /// Get current quality profile (Legacy method - delegates to StreamingCore)
        /// </summary>
        public StreamingQualityProfile GetCurrentQualityProfile()
        {
            return _streamingCore?.GetCurrentQualityProfile() ?? new StreamingQualityProfile();
        }

        /// <summary>
        /// Get current memory usage (Legacy method - delegates to StreamingCore)
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return _streamingCore?.GetCurrentMemoryUsage() ?? 0;
        }

        #region Private Methods - Legacy Compatibility Helpers

        /// <summary>
        /// Legacy method - no longer needed as StreamingCore handles all coordination
        /// </summary>
        [System.Obsolete("Use StreamingCore instead", false)]
        private void CoordinateStreamingSystems()
        {
            // All coordination is now handled by StreamingCore
        }

        /// <summary>
        /// Legacy method - no longer needed as StreamingCore handles health monitoring
        /// </summary>
        [System.Obsolete("Use StreamingCore instead", false)]
        private void UpdateSystemHealth()
        {
            // All health monitoring is now handled by StreamingCore
        }

        /// <summary>
        /// Legacy method - no longer needed as StreamingCore handles health determination
        /// </summary>
        [System.Obsolete("Use StreamingCore instead", false)]
        private StreamingSystemHealth DetermineSystemHealth()
        {
            return _streamingCore?.SystemHealth ?? StreamingSystemHealth.Healthy;
        }

        // All initialization methods are now handled by StreamingInitializationManager
        // Legacy methods kept for reference but marked as obsolete

        [System.Obsolete("Use StreamingInitializationManager instead", false)]
        private IEnumerator InitializeAssetStreaming()
        {
            // Now handled by StreamingInitializationManager
            yield return null;
        }

        [System.Obsolete("Use StreamingInitializationManager instead", false)]
        private IEnumerator InitializeLODSystem()
        {
            // Now handled by StreamingInitializationManager
            yield return null;
        }

        [System.Obsolete("Use StreamingInitializationManager instead", false)]
        private IEnumerator InitializePerformanceMonitoring()
        {
            // Now handled by StreamingInitializationManager
            yield return null;
        }

        [System.Obsolete("Use StreamingInitializationManager instead", false)]
        private IEnumerator InitializePlantStreaming()
        {
            // Now handled by StreamingInitializationManager
            yield return null;
        }

        [System.Obsolete("Use StreamingInitializationManager instead", false)]
        private IEnumerator InitializeMemoryManagement()
        {
            // Now handled by StreamingInitializationManager
            yield return null;
        }

        [System.Obsolete("Use StreamingQualityManager instead", false)]
        private void ApplyQualityProfile(int profileIndex)
        {
            // Now handled by StreamingQualityManager
        }

        [System.Obsolete("Use StreamingQualityManager instead", false)]
        private void InitializeDefaultQualityProfiles()
        {
            // Now handled by StreamingQualityManager
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures - Legacy Compatibility

    /// <summary>
    /// Legacy streaming quality profile (use StreamingQualityProfile from subsystems instead)
    /// </summary>
    [System.Serializable]
    [System.Obsolete("Use ProjectChimera.Core.Streaming.Subsystems.StreamingQualityProfile instead")]
    public struct LegacyStreamingQualityProfile
    {
        public string profileName;
        public float lodBias;
        public float maxStreamingDistance;
        public int textureQuality;
        public float shadowDistance;
        public int particleQuality;
    }

    /// <summary>
    /// System health status (legacy - use StreamingSystemHealth from subsystems)
    /// </summary>
    public enum SystemHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Failed
    }

    #endregion
}
