using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core.Streaming.Subsystems
{
    /// <summary>
    /// REFACTORED: Streaming Quality Manager - Focused quality profile management and optimization
    /// Handles quality profiles, dynamic quality adjustment, and performance-based quality scaling
    /// Single Responsibility: Quality profile management and optimization
    /// </summary>
    public class StreamingQualityManager : MonoBehaviour
    {
        [Header("Quality Management Settings")]
        [SerializeField] private bool _enableQualityManagement = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDynamicQuality = true;
        [SerializeField] private float _qualityAdjustmentInterval = 5f;

        [Header("Quality Profiles")]
        [SerializeField] private StreamingQualityProfile[] _qualityProfiles;
        [SerializeField] private int _defaultQualityIndex = 1; // Medium quality
        [SerializeField] private bool _allowQualityDowngrade = true;
        [SerializeField] private bool _allowQualityUpgrade = true;

        [Header("Performance Thresholds")]
        [SerializeField] private float _performanceUpgradeThreshold = 0.8f; // 80%
        [SerializeField] private float _performanceDowngradeThreshold = 0.4f; // 40%
        [SerializeField] private int _consecutiveFramesForAdjustment = 30;

        // System references
        private StreamingPerformanceMonitor _performanceMonitor;
        private AssetStreamingManager _assetStreaming;
        private LODManager _lodManager;

        // Quality state
        private int _currentQualityIndex = 1;
        private StreamingQualityProfile _currentProfile;
        private float _lastQualityAdjustment;
        private readonly Queue<float> _recentPerformanceScores = new Queue<float>();

        // Statistics
        private QualityManagerStats _stats = new QualityManagerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int CurrentQualityIndex => _currentQualityIndex;
        public StreamingQualityProfile CurrentProfile => _currentProfile;
        public QualityManagerStats GetStats() => _stats;

        // Events
        public System.Action<int, StreamingQualityProfile> OnQualityChanged;
        public System.Action<StreamingQualityProfile> OnQualityProfileApplied;
        public System.Action<float> OnPerformanceBasedAdjustment;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeSystemReferences();
            InitializeDefaultQualityProfiles();
            ApplyQualityProfile(_defaultQualityIndex);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "⚙️ StreamingQualityManager initialized", this);
        }

        /// <summary>
        /// Initialize system references via ServiceContainer DI
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Resolve via ServiceContainer with optional dependency pattern
            _performanceMonitor = ServiceContainerFactory.Instance?.TryResolve<StreamingPerformanceMonitor>();
            _assetStreaming = ServiceContainerFactory.Instance?.TryResolve<AssetStreamingManager>();
            _lodManager = ServiceContainerFactory.Instance?.TryResolve<LODManager>();

            if (_performanceMonitor == null && _enableLogging)
                ChimeraLogger.LogWarning("STREAMING", "StreamingPerformanceMonitor not registered - dynamic quality disabled", this);

            if (_assetStreaming == null && _enableLogging)
                ChimeraLogger.LogWarning("STREAMING", "AssetStreamingManager not registered - asset streaming quality disabled", this);

            if (_lodManager == null && _enableLogging)
                ChimeraLogger.LogWarning("STREAMING", "LODManager not registered - LOD quality management disabled", this);
        }

        /// <summary>
        /// Update quality management (called from coordinator)
        /// </summary>
        public void UpdateQualityManagement()
        {
            if (!IsEnabled || !_enableQualityManagement) return;

            if (_enableDynamicQuality && Time.time - _lastQualityAdjustment >= _qualityAdjustmentInterval)
            {
                ProcessDynamicQualityAdjustment();
            }
        }

        /// <summary>
        /// Set specific quality profile
        /// </summary>
        public void SetQualityProfile(int profileIndex)
        {
            if (profileIndex >= 0 && profileIndex < _qualityProfiles.Length && profileIndex != _currentQualityIndex)
            {
                ApplyQualityProfile(profileIndex);
                _stats.ManualAdjustments++;

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Quality profile manually set to: {_qualityProfiles[profileIndex].profileName}", this);
            }
        }

        /// <summary>
        /// Get available quality profiles
        /// </summary>
        public StreamingQualityProfile[] GetAvailableProfiles()
        {
            return _qualityProfiles?.ToArray() ?? new StreamingQualityProfile[0];
        }

        /// <summary>
        /// Get quality profile by index
        /// </summary>
        public StreamingQualityProfile GetQualityProfile(int index)
        {
            if (index >= 0 && index < _qualityProfiles.Length)
                return _qualityProfiles[index];

            return new StreamingQualityProfile();
        }

        /// <summary>
        /// Add custom quality profile
        /// </summary>
        public void AddCustomQualityProfile(StreamingQualityProfile profile)
        {
            if (_qualityProfiles == null)
            {
                _qualityProfiles = new StreamingQualityProfile[] { profile };
            }
            else
            {
                var profilesList = _qualityProfiles.ToList();
                profilesList.Add(profile);
                _qualityProfiles = profilesList.ToArray();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Added custom quality profile: {profile.profileName}", this);
        }

        /// <summary>
        /// Force quality upgrade if possible
        /// </summary>
        public bool ForceQualityUpgrade()
        {
            if (!_allowQualityUpgrade || _currentQualityIndex >= _qualityProfiles.Length - 1)
                return false;

            ApplyQualityProfile(_currentQualityIndex + 1);
            _stats.ForcedUpgrades++;
            return true;
        }

        /// <summary>
        /// Force quality downgrade if possible
        /// </summary>
        public bool ForceQualityDowngrade()
        {
            if (!_allowQualityDowngrade || _currentQualityIndex <= 0)
                return false;

            ApplyQualityProfile(_currentQualityIndex - 1);
            _stats.ForcedDowngrades++;
            return true;
        }

        /// <summary>
        /// Reset to default quality
        /// </summary>
        public void ResetToDefaultQuality()
        {
            ApplyQualityProfile(_defaultQualityIndex);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Quality reset to default", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _recentPerformanceScores.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingQualityManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize default quality profiles if none are set
        /// </summary>
        private void InitializeDefaultQualityProfiles()
        {
            if (_qualityProfiles == null || _qualityProfiles.Length == 0)
            {
                _qualityProfiles = new StreamingQualityProfile[]
                {
                    CreateLowQualityProfile(),
                    CreateMediumQualityProfile(),
                    CreateHighQualityProfile(),
                    CreateUltraQualityProfile()
                };

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", "Initialized default quality profiles", this);
            }

            // Validate default quality index
            if (_defaultQualityIndex >= _qualityProfiles.Length)
                _defaultQualityIndex = _qualityProfiles.Length - 1;
        }

        /// <summary>
        /// Apply quality profile
        /// </summary>
        private void ApplyQualityProfile(int profileIndex)
        {
            if (profileIndex < 0 || profileIndex >= _qualityProfiles.Length)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("STREAMING", $"Invalid quality profile index: {profileIndex}", this);
                return;
            }

            var previousIndex = _currentQualityIndex;
            _currentQualityIndex = profileIndex;
            _currentProfile = _qualityProfiles[profileIndex];

            // Apply profile to systems
            ApplyProfileToSystems(_currentProfile);

            // Fire events
            OnQualityChanged?.Invoke(_currentQualityIndex, _currentProfile);
            OnQualityProfileApplied?.Invoke(_currentProfile);

            // Update statistics
            if (profileIndex > previousIndex)
                _stats.QualityUpgrades++;
            else if (profileIndex < previousIndex)
                _stats.QualityDowngrades++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Applied quality profile: {_currentProfile.profileName} (Index: {profileIndex})", this);
        }

        /// <summary>
        /// Apply profile settings to streaming systems
        /// </summary>
        private void ApplyProfileToSystems(StreamingQualityProfile profile)
        {
            // Apply to Asset Streaming
            if (_assetStreaming != null)
            {
                // Apply asset streaming settings from profile
                // This would contain actual implementation details
            }

            // Apply to LOD Manager
            if (_lodManager != null)
            {
                // Apply LOD settings from profile
                // This would contain actual implementation details
            }

            // Apply general streaming settings
            // This would apply other profile-specific settings
        }

        /// <summary>
        /// Process dynamic quality adjustment based on performance
        /// </summary>
        private void ProcessDynamicQualityAdjustment()
        {
            if (_performanceMonitor == null) return;

            var currentPerformance = GetCurrentPerformanceScore();
            _recentPerformanceScores.Enqueue(currentPerformance);

            // Maintain performance history
            while (_recentPerformanceScores.Count > _consecutiveFramesForAdjustment)
            {
                _recentPerformanceScores.Dequeue();
            }

            // Check if we have enough data for adjustment
            if (_recentPerformanceScores.Count >= _consecutiveFramesForAdjustment)
            {
                var averagePerformance = _recentPerformanceScores.Average();

                if (ShouldUpgradeQuality(averagePerformance))
                {
                    UpgradeQualityIfPossible(averagePerformance);
                }
                else if (ShouldDowngradeQuality(averagePerformance))
                {
                    DowngradeQualityIfPossible(averagePerformance);
                }
            }

            _lastQualityAdjustment = Time.time;
        }

        /// <summary>
        /// Get current performance score
        /// </summary>
        private float GetCurrentPerformanceScore()
        {
            // Simplified performance scoring
            // In real implementation, this would aggregate multiple metrics
            var frameRate = 1f / Time.deltaTime;
            var targetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
            return Mathf.Clamp01(frameRate / targetFrameRate);
        }

        /// <summary>
        /// Check if quality should be upgraded
        /// </summary>
        private bool ShouldUpgradeQuality(float averagePerformance)
        {
            return _allowQualityUpgrade &&
                   _currentQualityIndex < _qualityProfiles.Length - 1 &&
                   averagePerformance >= _performanceUpgradeThreshold;
        }

        /// <summary>
        /// Check if quality should be downgraded
        /// </summary>
        private bool ShouldDowngradeQuality(float averagePerformance)
        {
            return _allowQualityDowngrade &&
                   _currentQualityIndex > 0 &&
                   averagePerformance <= _performanceDowngradeThreshold;
        }

        /// <summary>
        /// Upgrade quality if possible
        /// </summary>
        private void UpgradeQualityIfPossible(float performance)
        {
            ApplyQualityProfile(_currentQualityIndex + 1);
            _stats.AutoUpgrades++;
            OnPerformanceBasedAdjustment?.Invoke(performance);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Quality auto-upgraded due to good performance: {performance:P2}", this);
        }

        /// <summary>
        /// Downgrade quality if possible
        /// </summary>
        private void DowngradeQualityIfPossible(float performance)
        {
            ApplyQualityProfile(_currentQualityIndex - 1);
            _stats.AutoDowngrades++;
            OnPerformanceBasedAdjustment?.Invoke(performance);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Quality auto-downgraded due to poor performance: {performance:P2}", this);
        }

        /// <summary>
        /// Create low quality profile
        /// </summary>
        private StreamingQualityProfile CreateLowQualityProfile()
        {
            return new StreamingQualityProfile
            {
                profileName = "Low",
                lodBias = 0.5f,
                maxStreamingDistance = 50f,
                textureQuality = 0,
                shadowDistance = 25f,
                particleQuality = 0
            };
        }

        /// <summary>
        /// Create medium quality profile
        /// </summary>
        private StreamingQualityProfile CreateMediumQualityProfile()
        {
            return new StreamingQualityProfile
            {
                profileName = "Medium",
                lodBias = 1.0f,
                maxStreamingDistance = 100f,
                textureQuality = 1,
                shadowDistance = 50f,
                particleQuality = 1
            };
        }

        /// <summary>
        /// Create high quality profile
        /// </summary>
        private StreamingQualityProfile CreateHighQualityProfile()
        {
            return new StreamingQualityProfile
            {
                profileName = "High",
                lodBias = 1.5f,
                maxStreamingDistance = 200f,
                textureQuality = 2,
                shadowDistance = 100f,
                particleQuality = 2
            };
        }

        /// <summary>
        /// Create ultra quality profile
        /// </summary>
        private StreamingQualityProfile CreateUltraQualityProfile()
        {
            return new StreamingQualityProfile
            {
                profileName = "Ultra",
                lodBias = 2.0f,
                maxStreamingDistance = 300f,
                textureQuality = 3,
                shadowDistance = 200f,
                particleQuality = 3
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Streaming quality profile
    /// </summary>
    [System.Serializable]
    public struct StreamingQualityProfile
    {
        public string profileName;
        public float lodBias;
        public float maxStreamingDistance;
        public int textureQuality;
        public float shadowDistance;
        public int particleQuality;
    }

    /// <summary>
    /// Quality manager statistics
    /// </summary>
    [System.Serializable]
    public struct QualityManagerStats
    {
        public int QualityUpgrades;
        public int QualityDowngrades;
        public int AutoUpgrades;
        public int AutoDowngrades;
        public int ManualAdjustments;
        public int ForcedUpgrades;
        public int ForcedDowngrades;
    }

    #endregion
}