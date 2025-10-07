using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

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
                    Logger.LogError("GAMEPLAY", "IGameplayModeController not available", this);
                    return;
                }

                // Validate component references
                if (!ValidateComponents())
                {
                    Logger.LogError("GAMEPLAY", "Genetics overlay components missing", this);
                    return;
                }

                // Subscribe to mode change events
                SubscribeToEvents();

                // Initialize overlay visibility based on current mode
                UpdateOverlayVisibility(_modeController.CurrentMode);

                _isInitialized = true;

                if (_debugMode)
                {
                    Logger.Log("GAMEPLAY", "Genetics overlay initialized", this);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError("GAMEPLAY", $"Genetics overlay initialization error: {ex.Message}", this);
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
                Logger.LogWarning("GAMEPLAY", "GeneticVisualizationManager not assigned", this);
                allValid = false;
            }

            if (_enableGeneticsToolbar && _geneticsToolbarManager == null)
            {
                Logger.LogWarning("GAMEPLAY", "GeneticsToolbarManager not assigned", this);
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
                Logger.LogWarning("GAMEPLAY", "ModeChangedEventSO not assigned", this);
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
                Logger.Log("GAMEPLAY", $"Genetics mode changed to {eventData.NewMode}", this);
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
                Logger.Log("GAMEPLAY", $"Genetics overlay visibility set: {shouldShowOverlay}", this);
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
                    Logger.Log("OTHER", "$1", this);
                }
            }
        }

        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            Logger.Log("GAMEPLAY", $"Genetics overlay debug mode: {(enabled ? "ON" : "OFF")}", this);
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

        public int TickPriority => 0;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

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
                Logger.LogWarning("GAMEPLAY", "IGameplayModeController not available for editor toggle", this);
            }
        }

        #endif
    }
}
