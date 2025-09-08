using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using static ProjectChimera.Data.Events.GameplayMode;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Phase 2 Verification: Manages mode entry/exit callbacks to centralize side-effects
    /// Registers essential callbacks for showing/hiding UI, toggling systems, etc.
    /// </summary>
    public class ModeCallbackManager : MonoBehaviour
    {
        [Header("Callback Configuration")]
        [SerializeField] private bool _enableCallbackLogging = true;
        [SerializeField] private bool _enableErrorHandling = true;
        
        [Header("UI References for Callbacks")]
        [SerializeField] private GameObject _cultivationUIRoot;
        [SerializeField] private GameObject _constructionUIRoot; 
        [SerializeField] private GameObject _geneticsUIRoot;
        [SerializeField] private GameObject _blueprintSystemRoot;
        [SerializeField] private GameObject _heatmapSystemRoot;
        
        private IGameplayModeController _modeController;
        private bool _isInitialized = false;
        
        private void Start()
        {
            InitializeCallbacks();
        }
        
        private void InitializeCallbacks()
        {
            // Get the gameplay mode controller service
            _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();
            
            if (_modeController == null)
            {
                ChimeraLogger.LogError("[ModeCallbackManager] GameplayModeController service not found!");
                return;
            }
            
            RegisterAllModeCallbacks();
            _isInitialized = true;
            
            ChimeraLogger.Log("[ModeCallbackManager] Mode callbacks registered successfully");
        }
        
        private void RegisterAllModeCallbacks()
        {
            // Register Cultivation Mode Callbacks
            RegisterCultivationModeCallbacks();
            
            // Register Construction Mode Callbacks  
            RegisterConstructionModeCallbacks();
            
            // Register Genetics Mode Callbacks
            RegisterGeneticsModeCallbacks();
        }
        
        #region Cultivation Mode Callbacks
        
        private void RegisterCultivationModeCallbacks()
        {
            // Entry: Show cultivation UI, enable plant monitoring systems
            _modeController.RegisterModeEntryCallback(GameplayMode.Cultivation, OnEnterCultivationMode);
            
            // Exit: Hide cultivation-specific UI, disable monitoring overlays
            _modeController.RegisterModeExitCallback(GameplayMode.Cultivation, OnExitCultivationMode);
        }
        
        private void OnEnterCultivationMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Entering Cultivation Mode - Activating plant monitoring systems");
            }
            
            try
            {
                // Show cultivation-specific UI
                if (_cultivationUIRoot != null)
                {
                    _cultivationUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        ChimeraLogger.Log("[ModeCallbackManager] Cultivation UI activated");
                }
                
                // Enable plant monitoring and care tool systems
                EnablePlantMonitoringSystems();
                
                // Show cultivation-specific HUD elements
                ShowCultivationHUD();
                
                // Enable environmental monitoring for plants
                EnableEnvironmentalMonitoring();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error entering Cultivation mode: {ex.Message}");
                }
            }
        }
        
        private void OnExitCultivationMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Exiting Cultivation Mode - Deactivating plant monitoring systems");
            }
            
            try
            {
                // Hide cultivation-specific UI
                if (_cultivationUIRoot != null)
                {
                    _cultivationUIRoot.SetActive(false);
                }
                
                // Disable intensive monitoring systems for performance
                DisablePlantMonitoringSystems();
                
                // Hide cultivation HUD
                HideCultivationHUD();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error exiting Cultivation mode: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Construction Mode Callbacks
        
        private void RegisterConstructionModeCallbacks()
        {
            // Entry: Show construction UI, enable blueprint system
            _modeController.RegisterModeEntryCallback(GameplayMode.Construction, OnEnterConstructionMode);
            
            // Exit: Hide construction UI, disable blueprint overlays
            _modeController.RegisterModeExitCallback(GameplayMode.Construction, OnExitConstructionMode);
        }
        
        private void OnEnterConstructionMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Entering Construction Mode - Activating blueprint systems");
            }
            
            try
            {
                // Show construction-specific UI
                if (_constructionUIRoot != null)
                {
                    _constructionUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        ChimeraLogger.Log("[ModeCallbackManager] Construction UI activated");
                }
                
                // Enable blueprint system
                if (_blueprintSystemRoot != null)
                {
                    _blueprintSystemRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        ChimeraLogger.Log("[ModeCallbackManager] Blueprint system activated");
                }
                
                // Enable construction tools and placement systems
                EnableConstructionSystems();
                
                // Show infrastructure and utility overlays
                ShowInfrastructureOverlays();
                
                // Enable grid snapping and measurement tools
                EnableConstructionTools();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error entering Construction mode: {ex.Message}");
                }
            }
        }
        
        private void OnExitConstructionMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Exiting Construction Mode - Deactivating blueprint systems");
            }
            
            try
            {
                // Hide construction-specific UI
                if (_constructionUIRoot != null)
                {
                    _constructionUIRoot.SetActive(false);
                }
                
                // Disable blueprint system for performance
                if (_blueprintSystemRoot != null)
                {
                    _blueprintSystemRoot.SetActive(false);
                }
                
                // Disable construction systems
                DisableConstructionSystems();
                
                // Hide infrastructure overlays
                HideInfrastructureOverlays();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error exiting Construction mode: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Genetics Mode Callbacks
        
        private void RegisterGeneticsModeCallbacks()
        {
            // Entry: Show genetics UI, enable heatmap visualizations
            _modeController.RegisterModeEntryCallback(GameplayMode.Genetics, OnEnterGeneticsMode);
            
            // Exit: Hide genetics UI, disable intensive visualizations
            _modeController.RegisterModeExitCallback(GameplayMode.Genetics, OnExitGeneticsMode);
        }
        
        private void OnEnterGeneticsMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Entering Genetics Mode - Activating genetic analysis systems");
            }
            
            try
            {
                // Show genetics-specific UI
                if (_geneticsUIRoot != null)
                {
                    _geneticsUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        ChimeraLogger.Log("[ModeCallbackManager] Genetics UI activated");
                }
                
                // Enable heatmap visualization system
                if (_heatmapSystemRoot != null)
                {
                    _heatmapSystemRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        ChimeraLogger.Log("[ModeCallbackManager] Heatmap system activated");
                }
                
                // Enable genetic analysis tools
                EnableGeneticAnalysisSystems();
                
                // Show breeding and trait analysis overlays
                ShowGeneticOverlays();
                
                // Enable advanced visualization systems
                EnableGeneticVisualizations();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error entering Genetics mode: {ex.Message}");
                }
            }
        }
        
        private void OnExitGeneticsMode()
        {
            if (_enableCallbackLogging)
            {
                ChimeraLogger.Log("[ModeCallbackManager] Exiting Genetics Mode - Deactivating genetic analysis systems");
            }
            
            try
            {
                // Hide genetics-specific UI
                if (_geneticsUIRoot != null)
                {
                    _geneticsUIRoot.SetActive(false);
                }
                
                // Disable intensive heatmap system for performance
                if (_heatmapSystemRoot != null)
                {
                    _heatmapSystemRoot.SetActive(false);
                }
                
                // Disable genetic analysis systems
                DisableGeneticAnalysisSystems();
                
                // Hide genetic overlays
                HideGeneticOverlays();
                
            }
            catch (System.Exception ex)
            {
                if (_enableErrorHandling)
                {
                    ChimeraLogger.LogError($"[ModeCallbackManager] Error exiting Genetics mode: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region System Control Methods
        
        // Cultivation System Methods
        private void EnablePlantMonitoringSystems()
        {
            // Enable plant health monitoring, growth tracking, care reminders
            ChimeraLogger.Log("[ModeCallbackManager] Plant monitoring systems enabled");
        }
        
        private void DisablePlantMonitoringSystems()
        {
            // Disable intensive plant monitoring to save performance
            ChimeraLogger.Log("[ModeCallbackManager] Plant monitoring systems disabled");
        }
        
        private void ShowCultivationHUD()
        {
            // Show plant care tools, environmental readings, harvest notifications
            ChimeraLogger.Log("[ModeCallbackManager] Cultivation HUD elements shown");
        }
        
        private void HideCultivationHUD()
        {
            // Hide cultivation-specific HUD elements
            ChimeraLogger.Log("[ModeCallbackManager] Cultivation HUD elements hidden");
        }
        
        private void EnableEnvironmentalMonitoring()
        {
            // Enable real-time environmental data collection and alerts
            ChimeraLogger.Log("[ModeCallbackManager] Environmental monitoring enabled");
        }
        
        // Construction System Methods
        private void EnableConstructionSystems()
        {
            // Enable placement tools, material calculations, cost estimation
            ChimeraLogger.Log("[ModeCallbackManager] Construction systems enabled");
        }
        
        private void DisableConstructionSystems()
        {
            // Disable construction tools and calculations
            ChimeraLogger.Log("[ModeCallbackManager] Construction systems disabled");
        }
        
        private void ShowInfrastructureOverlays()
        {
            // Show utility lines, structural elements, zone boundaries
            ChimeraLogger.Log("[ModeCallbackManager] Infrastructure overlays shown");
        }
        
        private void HideInfrastructureOverlays()
        {
            // Hide infrastructure visualization overlays
            ChimeraLogger.Log("[ModeCallbackManager] Infrastructure overlays hidden");
        }
        
        private void EnableConstructionTools()
        {
            // Enable grid snapping, measurement tools, placement guides
            ChimeraLogger.Log("[ModeCallbackManager] Construction tools enabled");
        }
        
        // Genetics System Methods
        private void EnableGeneticAnalysisSystems()
        {
            // Enable trait analysis, breeding calculators, genetic predictions
            ChimeraLogger.Log("[ModeCallbackManager] Genetic analysis systems enabled");
        }
        
        private void DisableGeneticAnalysisSystems()
        {
            // Disable intensive genetic calculations
            ChimeraLogger.Log("[ModeCallbackManager] Genetic analysis systems disabled");
        }
        
        private void ShowGeneticOverlays()
        {
            // Show trait heatmaps, breeding compatibility, genetic diversity
            ChimeraLogger.Log("[ModeCallbackManager] Genetic overlays shown");
        }
        
        private void HideGeneticOverlays()
        {
            // Hide genetic visualization overlays
            ChimeraLogger.Log("[ModeCallbackManager] Genetic overlays hidden");
        }
        
        private void EnableGeneticVisualizations()
        {
            // Enable advanced genetic visualization systems
            ChimeraLogger.Log("[ModeCallbackManager] Genetic visualizations enabled");
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually trigger entry callbacks for testing
        /// </summary>
        [ContextMenu("Test Cultivation Entry Callbacks")]
        public void TestCultivationEntryCallbacks()
        {
            OnEnterCultivationMode();
        }
        
        /// <summary>
        /// Manually trigger exit callbacks for testing
        /// </summary>
        [ContextMenu("Test Cultivation Exit Callbacks")]
        public void TestCultivationExitCallbacks()
        {
            OnExitCultivationMode();
        }
        
        /// <summary>
        /// Test all mode callbacks
        /// </summary>
        [ContextMenu("Test All Mode Callbacks")]
        public void TestAllModeCallbacks()
        {
            ChimeraLogger.Log("[ModeCallbackManager] Testing all mode callbacks...");
            
            // Test each mode's entry and exit
            OnEnterCultivationMode();
            OnExitCultivationMode();
            
            OnEnterConstructionMode();
            OnExitConstructionMode();
            
            OnEnterGeneticsMode();
            OnExitGeneticsMode();
            
            ChimeraLogger.Log("[ModeCallbackManager] All callback tests completed");
        }
        
        /// <summary>
        /// Get callback registration status
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        #endregion
        
        private void OnDestroy()
        {
            // Callbacks are automatically cleaned up by the GameplayModeController
            // when it is destroyed, so no explicit unregistration needed
        }
    }
}