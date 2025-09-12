using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// REFACTORED: Genetics mode overlay system decomposed into focused components.
    /// This file now serves as a coordinator for specialized genetics overlay components.
    ///
    /// New Component Structure:
    /// - GeneticVisualizationManager.cs: Handles trait overlays and heatmap visualizations
    /// - GeneticsToolbarManager.cs: Manages genetics tools and menu interface
    /// - GeneticsModeOverlay.cs: Coordinates the genetics mode overlay system
    /// </summary>
    public class GeneticsModeOverlay : MonoBehaviour, ITickable
    {
        [Header("Overlay Configuration")]
        [SerializeField] private bool _enableGeneticVisualization = true;
        [SerializeField] private bool _enableGeneticsToolbar = true;
        [SerializeField] private bool _debugMode = false;

        [Header("Component References")]
        [SerializeField] private GeneticVisualizationManager _geneticVisualizationManager;
        [SerializeField] private GeneticsToolbarManager _geneticsToolbarManager;

        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;

        // Services
        private IGameplayModeController _modeController;

        // State tracking
        private bool _isInitialized = false;
        private bool _isGeneticsModeActive = false;

        private void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeOverlay();
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
            UnsubscribeFromEvents();
        }

        private void InitializeOverlay()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();

                if (_modeController == null)
                {
                    ChimeraLogger.LogError("[GeneticsModeOverlay] GameplayModeController service not found!");
                    return;
                }

                // Validate component references
                if (!ValidateComponents())
                {
                    ChimeraLogger.LogError("[GeneticsModeOverlay] Component validation failed!");
                    return;
                }

                // Subscribe to mode change events
                SubscribeToEvents();

                // Initialize overlay visibility based on current mode
                UpdateOverlayVisibility(_modeController.CurrentMode);

                _isInitialized = true;

                if (_debugMode)
                {
                    ChimeraLogger.Log($"[GeneticsModeOverlay] Initialized with current mode: {_modeController.CurrentMode}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[GeneticsModeOverlay] Error during initialization: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that all required components are properly assigned
        /// </summary>
        private bool ValidateComponents()
        {
            bool allValid = true;

            if (_enableGeneticVisualization && _geneticVisualizationManager == null)
            {
                ChimeraLogger.LogError("[GeneticsModeOverlay] GeneticVisualizationManager component is required but not assigned!");
                allValid = false;
            }

            if (_enableGeneticsToolbar && _geneticsToolbarManager == null)
            {
                ChimeraLogger.LogError("[GeneticsModeOverlay] GeneticsToolbarManager component is required but not assigned!");
                allValid = false;
            }

            return allValid;
        }

        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                ChimeraLogger.LogWarning("[GeneticsModeOverlay] ModeChangedEvent not assigned");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
        }

        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[GeneticsModeOverlay] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }

            UpdateOverlayVisibility(eventData.NewMode);
        }

        private void UpdateOverlayVisibility(GameplayMode currentMode)
        {
            bool shouldShowOverlay = currentMode == GameplayMode.Genetics;

            if (_isGeneticsModeActive == shouldShowOverlay) return;

            _isGeneticsModeActive = shouldShowOverlay;

            // Control component visibility through their respective interfaces
            if (shouldShowOverlay)
            {
                // Show genetic visualization
                if (_geneticVisualizationManager != null && _enableGeneticVisualization)
                {
                    _geneticVisualizationManager.SetTraitOverlaysEnabled(true);
                    _geneticVisualizationManager.SetHeatmapsEnabled(true);
                }

                // Show genetics toolbar
                if (_geneticsToolbarManager != null && _enableGeneticsToolbar)
                {
                    _geneticsToolbarManager.ShowToolbar();
                }
            }
            else
            {
                // Hide genetic visualization
                if (_geneticVisualizationManager != null)
                {
                    _geneticVisualizationManager.SetTraitOverlaysEnabled(false);
                    _geneticVisualizationManager.SetHeatmapsEnabled(false);
                }

                // Hide genetics toolbar
                if (_geneticsToolbarManager != null)
                {
                    _geneticsToolbarManager.HideToolbar();
                }
            }

            if (_debugMode)
            {
                ChimeraLogger.Log($"[GeneticsModeOverlay] Genetics mode overlay {(shouldShowOverlay ? "shown" : "hidden")}");
            }
        }

        #region Public Interface

        /// <summary>
        /// Manually refresh the genetics overlay
        /// </summary>
        public void RefreshOverlay()
        {
            if (_isInitialized && _modeController != null)
            {
                UpdateOverlayVisibility(_modeController.CurrentMode);

                if (_debugMode)
                {
                    ChimeraLogger.Log("[GeneticsModeOverlay] Overlay refreshed manually");
                }
            }
        }

        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            ChimeraLogger.Log($"[GeneticsModeOverlay] Debug mode {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get current overlay state
        /// </summary>
        public bool IsGeneticsModeActive => _isGeneticsModeActive;

        /// <summary>
        /// Get visualization statistics
        /// </summary>
        public int GetVisualizedPlantCount()
        {
            return _geneticVisualizationManager != null ? _geneticVisualizationManager.GetVisualizedPlantCount() : 0;
        }

        /// <summary>
        /// Get toolbar information
        /// </summary>
        public string GetCurrentToolbarTab()
        {
            return _geneticsToolbarManager != null ? _geneticsToolbarManager.GetCurrentTab() : "none";
        }

        /// <summary>
        /// Get available strains count
        /// </summary>
        public int GetAvailableStrainsCount()
        {
            return _geneticsToolbarManager != null ? _geneticsToolbarManager.GetAvailableStrainsCount() : 0;
        }

        /// <summary>
        /// Get tissue cultures count
        /// </summary>
        public int GetTissueCulturesCount()
        {
            return _geneticsToolbarManager != null ? _geneticsToolbarManager.GetTissueCulturesCount() : 0;
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

        #if UNITY_EDITOR

        /// <summary>
        /// Editor-only method for testing genetics mode toggle
        /// </summary>
        [ContextMenu("Test Genetics Mode Toggle")]
        private void TestGeneticsModeToggle()
        {
            if (Application.isPlaying && _modeController != null)
            {
                var currentMode = _modeController.CurrentMode;
                var newMode = currentMode == GameplayMode.Genetics ? GameplayMode.Cultivation : GameplayMode.Genetics;
                _modeController.SetMode(newMode, "Debug Test");
            }
            else
            {
                ChimeraLogger.Log("[GeneticsModeOverlay] Test only works during play mode with initialized controller");
            }
        }

        #endif
    }
}
