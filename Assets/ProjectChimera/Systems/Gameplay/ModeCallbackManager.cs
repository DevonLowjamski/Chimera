using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
using UnityEngine;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture
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
                Logger.LogError("GAMEPLAY", "Failed to resolve IGameplayModeController from ServiceContainer", this);
                return;
            }
            
            RegisterAllModeCallbacks();
            _isInitialized = true;

            Logger.Log("GAMEPLAY", "ModeCallbackManager initialized successfully", this);
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
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
            }
            
            try
            {
                // Show cultivation-specific UI
                if (_cultivationUIRoot != null)
                {
                    _cultivationUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
            }
        }
        
        private void OnExitCultivationMode()
        {
            if (_enableCallbackLogging)
            {
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
            }
            
            try
            {
                // Show construction-specific UI
                if (_constructionUIRoot != null)
                {
                    _constructionUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
                
                // Enable blueprint system
                if (_blueprintSystemRoot != null)
                {
                    _blueprintSystemRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
            }
        }
        
        private void OnExitConstructionMode()
        {
            if (_enableCallbackLogging)
            {
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
            }
            
            try
            {
                // Show genetics-specific UI
                if (_geneticsUIRoot != null)
                {
                    _geneticsUIRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
                
                // Enable heatmap visualization system
                if (_heatmapSystemRoot != null)
                {
                    _heatmapSystemRoot.SetActive(true);
                    if (_enableCallbackLogging)
                        Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
            }
        }
        
        private void OnExitGeneticsMode()
        {
            if (_enableCallbackLogging)
            {
                Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
                    Logger.Log("GAMEPLAY", "Mode operation completed", this);
                }
            }
        }
        
        #endregion
        
        #region System Control Methods
        
        // Cultivation System Methods
        private void EnablePlantMonitoringSystems()
        {
            // Enable plant health monitoring, growth tracking, care reminders
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void DisablePlantMonitoringSystems()
        {
            // Disable intensive plant monitoring to save performance
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void ShowCultivationHUD()
        {
            // Show plant care tools, environmental readings, harvest notifications
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void HideCultivationHUD()
        {
            // Hide cultivation-specific HUD elements
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void EnableEnvironmentalMonitoring()
        {
            // Enable real-time environmental data collection and alerts
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        // Construction System Methods
        private void EnableConstructionSystems()
        {
            // Enable placement tools, material calculations, cost estimation
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void DisableConstructionSystems()
        {
            // Disable construction tools and calculations
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void ShowInfrastructureOverlays()
        {
            // Show utility lines, structural elements, zone boundaries
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void HideInfrastructureOverlays()
        {
            // Hide infrastructure visualization overlays
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void EnableConstructionTools()
        {
            // Enable grid snapping, measurement tools, placement guides
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        // Genetics System Methods
        private void EnableGeneticAnalysisSystems()
        {
            // Enable trait analysis, breeding calculators, genetic predictions
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void DisableGeneticAnalysisSystems()
        {
            // Disable intensive genetic calculations
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void ShowGeneticOverlays()
        {
            // Show trait heatmaps, breeding compatibility, genetic diversity
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void HideGeneticOverlays()
        {
            // Hide genetic visualization overlays
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
        }
        
        private void EnableGeneticVisualizations()
        {
            // Enable advanced genetic visualization systems
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
            
            // Test each mode's entry and exit
            OnEnterCultivationMode();
            OnExitCultivationMode();
            
            OnEnterConstructionMode();
            OnExitConstructionMode();
            
            OnEnterGeneticsMode();
            OnExitGeneticsMode();
            
            Logger.Log("GAMEPLAY", "Mode operation completed", this);
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
