using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Diagnostics
{
    /// <summary>
    /// SIMPLIFIED: Basic development monitoring coordinator aligned with Project Chimera's vision.
    /// Provides essential development tools while maintaining focus on core gameplay pillars.
    /// Removed over-engineered monitoring systems that don't align with direct player control.
    ///
    /// Coordinator Structure:
    /// - PerformanceProfiler.cs: Basic FPS and performance monitoring
    /// - DebugOverlayManager.cs: Simple debug overlay for development
    /// - DevelopmentMonitoring.cs: Coordinates development monitoring system
    ///
    /// NOTE: All components are properly gated for development builds only (#if DEVELOPMENT_BUILD || UNITY_EDITOR)
    /// </summary>
    public class DevelopmentMonitoring : MonoBehaviour, ITickable
    {
        [Header("Development Monitoring Configuration")]
        [SerializeField] private bool _enableDevelopmentMonitoring = true;
        [SerializeField] private bool _enableDebugOverlays = true;
        [SerializeField] private bool _enablePerformanceProfiling = true;

        [Header("Component References")]
        [SerializeField] private PerformanceProfiler _performanceProfiler;
        [SerializeField] private DebugOverlayManager _debugOverlayManager;

        // State tracking
        private bool _isInitialized = false;

        private void Awake()
        {
            InitializeDevelopmentMonitoring();
        }

        private void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        public void Tick(float deltaTime)
        {
            // Component updates are handled by their respective systems
            // This coordinator ensures proper orchestration between components
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        private void InitializeDevelopmentMonitoring()
        {
            // Only enable in development builds
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            _enableDevelopmentMonitoring = false;
            return;
#endif

            // Validate component references
            if (!ValidateComponents())
            {
                ChimeraLogger.LogError("[DevelopmentMonitoring] Component validation failed!");
                return;
            }

            _isInitialized = true;
            ChimeraLogger.Log("[DevelopmentMonitoring] Development monitoring coordinator initialized");
        }

        /// <summary>
        /// Validates that all required components are properly assigned
        /// </summary>
        private bool ValidateComponents()
        {
            bool allValid = true;

            if (_enablePerformanceProfiling && _performanceProfiler == null)
            {
                ChimeraLogger.LogError("[DevelopmentMonitoring] PerformanceProfiler component is required but not assigned!");
                allValid = false;
            }

            if (_enableDebugOverlays && _debugOverlayManager == null)
            {
                ChimeraLogger.LogError("[DevelopmentMonitoring] DebugOverlayManager component is required but not assigned!");
                allValid = false;
            }

            return allValid;
        }

        #region Public Interface

        /// <summary>
        /// Manually refresh the development monitoring system
        /// </summary>
        public void RefreshMonitoring()
        {
            if (_isInitialized && _enableDevelopmentMonitoring)
            {
                if (_performanceProfiler != null && _enablePerformanceProfiling)
                {
                    // Performance profiler handles its own refresh
                }

                if (_debugOverlayManager != null && _enableDebugOverlays)
                {
                    // Debug overlay manager handles its own refresh
                }

                ChimeraLogger.Log("[DevelopmentMonitoring] Monitoring system refreshed manually");
            }
        }

        /// <summary>
        /// Enable/disable development monitoring at runtime
        /// </summary>
        public void SetDevelopmentMonitoringEnabled(bool enabled)
        {
            _enableDevelopmentMonitoring = enabled;
            ChimeraLogger.Log($"[DevelopmentMonitoring] Development monitoring {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get current monitoring status
        /// </summary>
        public bool IsDevelopmentMonitoringActive => _enableDevelopmentMonitoring && _isInitialized;

        /// <summary>
        /// Get performance statistics from the profiler
        /// </summary>
        public string GetPerformanceStats()
        {
            if (_performanceProfiler != null)
            {
                return _performanceProfiler.GetPerformanceSummary();
            }
            return "Performance profiler not available";
        }

        /// <summary>
        /// Toggle debug overlay visibility
        /// </summary>
        public void ToggleDebugOverlay()
        {
            if (_debugOverlayManager != null)
            {
                _debugOverlayManager.ToggleOverlay();
            }
        }

        #endregion

        #region ITickable Implementation

        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion

