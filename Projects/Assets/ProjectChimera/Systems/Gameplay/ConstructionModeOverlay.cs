using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Construction mode overlay system - shows blueprints and utility visibility toggles
    /// Responds to gameplay mode changes and provides construction-specific UI elements
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class ConstructionModeOverlay : MonoBehaviour
    {
        [Header("Overlay Configuration")]
        [SerializeField] private bool _enableBlueprintOverlay = true;
        [SerializeField] private bool _enableUtilityOverlay = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Blueprint Overlay")]
        [SerializeField] private GameObject _blueprintPanel;
        [SerializeField] private Toggle _blueprintToggle;
        [SerializeField] private Slider _blueprintOpacity;
        [SerializeField] private Button _placementModeButton;
        
        [Header("Utility Overlay")]
        [SerializeField] private GameObject _utilityPanel;
        [SerializeField] private Toggle _electricalToggle;
        [SerializeField] private Toggle _plumbingToggle;
        [SerializeField] private Toggle _ventilationToggle;
        [SerializeField] private Toggle _lightingGridToggle;
        
        [Header("Construction Tools")]
        [SerializeField] private GameObject _constructionToolbar;
        [SerializeField] private Button _wallToolButton;
        [SerializeField] private Button _doorToolButton;
        [SerializeField] private Button _windowToolButton;
        [SerializeField] private Button _utilityToolButton;
        
        [Header("Visual Overlays")]
        [SerializeField] private GameObject _blueprintOverlayRoot;
        [SerializeField] private GameObject _utilityOverlayRoot;
        [SerializeField] private Material _blueprintMaterial;
        [SerializeField] private Material _utilityMaterial;
        
        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        
        // Services
        private IGameplayModeController _modeController;
        
        // State tracking
        private bool _isInitialized = false;
        private bool _isConstructionModeActive = false;
        private bool _blueprintVisible = false;
        private bool _utilitiesVisible = false;
        
        // Visual overlay management
        private Dictionary<string, GameObject> _blueprintObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _utilityObjects = new Dictionary<string, GameObject>();
        
        private void Start()
        {
            InitializeOverlay();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupOverlayObjects();
        }
        
        private void InitializeOverlay()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance.GetService<IGameplayModeController>();
                
                if (_modeController == null)
                {
                    Debug.LogError("[ConstructionModeOverlay] GameplayModeController service not found!");
                    return;
                }
                
                // Subscribe to mode change events
                SubscribeToEvents();
                
                // Subscribe to UI control events
                SubscribeToUIControls();
                
                // Initialize overlay visibility based on current mode
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                // Set initial UI state
                InitializeUIState();
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    Debug.Log($"[ConstructionModeOverlay] Initialized with current mode: {_modeController.CurrentMode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConstructionModeOverlay] Error during initialization: {ex.Message}");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                Debug.LogWarning("[ConstructionModeOverlay] ModeChangedEvent not assigned");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
        }
        
        private void SubscribeToUIControls()
        {
            // Blueprint controls
            if (_blueprintToggle != null)
            {
                _blueprintToggle.onValueChanged.AddListener(OnBlueprintToggleChanged);
            }
            
            if (_blueprintOpacity != null)
            {
                _blueprintOpacity.onValueChanged.AddListener(OnBlueprintOpacityChanged);
            }
            
            if (_placementModeButton != null)
            {
                _placementModeButton.onClick.AddListener(OnPlacementModeClicked);
            }
            
            // Utility controls
            if (_electricalToggle != null)
            {
                _electricalToggle.onValueChanged.AddListener((enabled) => OnUtilityToggleChanged("Electrical", enabled));
            }
            
            if (_plumbingToggle != null)
            {
                _plumbingToggle.onValueChanged.AddListener((enabled) => OnUtilityToggleChanged("Plumbing", enabled));
            }
            
            if (_ventilationToggle != null)
            {
                _ventilationToggle.onValueChanged.AddListener((enabled) => OnUtilityToggleChanged("Ventilation", enabled));
            }
            
            if (_lightingGridToggle != null)
            {
                _lightingGridToggle.onValueChanged.AddListener((enabled) => OnUtilityToggleChanged("LightingGrid", enabled));
            }
            
            // Construction tool buttons
            if (_wallToolButton != null)
            {
                _wallToolButton.onClick.AddListener(() => OnConstructionToolSelected("Wall"));
            }
            
            if (_doorToolButton != null)
            {
                _doorToolButton.onClick.AddListener(() => OnConstructionToolSelected("Door"));
            }
            
            if (_windowToolButton != null)
            {
                _windowToolButton.onClick.AddListener(() => OnConstructionToolSelected("Window"));
            }
            
            if (_utilityToolButton != null)
            {
                _utilityToolButton.onClick.AddListener(() => OnConstructionToolSelected("Utility"));
            }
        }
        
        private void InitializeUIState()
        {
            // Set default toggle states
            if (_blueprintToggle != null)
            {
                _blueprintToggle.isOn = _enableBlueprintOverlay;
            }
            
            // Set default opacity
            if (_blueprintOpacity != null)
            {
                _blueprintOpacity.value = 0.7f; // Default 70% opacity
            }
            
            // Initialize utility toggles as off
            if (_electricalToggle != null) _electricalToggle.isOn = false;
            if (_plumbingToggle != null) _plumbingToggle.isOn = false;
            if (_ventilationToggle != null) _ventilationToggle.isOn = false;
            if (_lightingGridToggle != null) _lightingGridToggle.isOn = false;
        }
        
        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }
            
            UpdateOverlayVisibility(eventData.NewMode);
        }
        
        private void UpdateOverlayVisibility(GameplayMode currentMode)
        {
            bool shouldShowOverlay = currentMode == GameplayMode.Construction;
            
            if (_isConstructionModeActive == shouldShowOverlay) return;
            
            _isConstructionModeActive = shouldShowOverlay;
            
            // Show/hide main overlay panels
            if (_blueprintPanel != null)
            {
                _blueprintPanel.SetActive(shouldShowOverlay && _enableBlueprintOverlay);
            }
            
            if (_utilityPanel != null)
            {
                _utilityPanel.SetActive(shouldShowOverlay && _enableUtilityOverlay);
            }
            
            if (_constructionToolbar != null)
            {
                _constructionToolbar.SetActive(shouldShowOverlay);
            }
            
            // Handle visual overlays
            if (shouldShowOverlay)
            {
                ShowConstructionOverlays();
            }
            else
            {
                HideConstructionOverlays();
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Construction mode overlay {(shouldShowOverlay ? "shown" : "hidden")}");
            }
        }
        
        private void ShowConstructionOverlays()
        {
            // Show blueprint overlay if enabled
            if (_enableBlueprintOverlay && _blueprintToggle != null && _blueprintToggle.isOn)
            {
                ShowBlueprintOverlay();
            }
            
            // Show utility overlays based on toggle states
            ShowUtilityOverlays();
        }
        
        private void HideConstructionOverlays()
        {
            // Hide all visual overlays
            HideBlueprintOverlay();
            HideUtilityOverlays();
        }
        
        private void ShowBlueprintOverlay()
        {
            if (_blueprintOverlayRoot != null)
            {
                _blueprintOverlayRoot.SetActive(true);
            }
            
            // Create placeholder blueprint visuals (these would be replaced with actual blueprint data)
            CreatePlaceholderBlueprints();
            
            _blueprintVisible = true;
            
            if (_debugMode)
            {
                Debug.Log("[ConstructionModeOverlay] Blueprint overlay shown");
            }
        }
        
        private void HideBlueprintOverlay()
        {
            if (_blueprintOverlayRoot != null)
            {
                _blueprintOverlayRoot.SetActive(false);
            }
            
            // Clean up blueprint objects
            foreach (var blueprintObj in _blueprintObjects.Values)
            {
                if (blueprintObj != null)
                {
                    blueprintObj.SetActive(false);
                }
            }
            
            _blueprintVisible = false;
            
            if (_debugMode)
            {
                Debug.Log("[ConstructionModeOverlay] Blueprint overlay hidden");
            }
        }
        
        private void ShowUtilityOverlays()
        {
            if (_utilityOverlayRoot != null)
            {
                _utilityOverlayRoot.SetActive(true);
            }
            
            // Show specific utility overlays based on toggle states
            if (_electricalToggle != null && _electricalToggle.isOn)
            {
                ShowUtilityOverlay("Electrical");
            }
            
            if (_plumbingToggle != null && _plumbingToggle.isOn)
            {
                ShowUtilityOverlay("Plumbing");
            }
            
            if (_ventilationToggle != null && _ventilationToggle.isOn)
            {
                ShowUtilityOverlay("Ventilation");
            }
            
            if (_lightingGridToggle != null && _lightingGridToggle.isOn)
            {
                ShowUtilityOverlay("LightingGrid");
            }
            
            _utilitiesVisible = true;
        }
        
        private void HideUtilityOverlays()
        {
            if (_utilityOverlayRoot != null)
            {
                _utilityOverlayRoot.SetActive(false);
            }
            
            // Hide all utility overlays
            foreach (var utilityObj in _utilityObjects.Values)
            {
                if (utilityObj != null)
                {
                    utilityObj.SetActive(false);
                }
            }
            
            _utilitiesVisible = false;
        }
        
        private void ShowUtilityOverlay(string utilityType)
        {
            if (_utilityObjects.TryGetValue(utilityType, out var utilityObj))
            {
                if (utilityObj != null)
                {
                    utilityObj.SetActive(true);
                }
            }
            else
            {
                // Create placeholder utility overlay
                CreatePlaceholderUtilityOverlay(utilityType);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] {utilityType} utility overlay shown");
            }
        }
        
        private void CreatePlaceholderBlueprints()
        {
            // This is a placeholder implementation - in a real game this would load actual blueprint data
            if (!_blueprintObjects.ContainsKey("MainBlueprint"))
            {
                var blueprintObj = new GameObject("BlueprintOverlay_Main");
                blueprintObj.transform.SetParent(_blueprintOverlayRoot?.transform ?? transform);
                
                // Add visual component (placeholder)
                var renderer = blueprintObj.AddComponent<MeshRenderer>();
                if (_blueprintMaterial != null)
                {
                    renderer.material = _blueprintMaterial;
                }
                
                _blueprintObjects["MainBlueprint"] = blueprintObj;
                
                if (_debugMode)
                {
                    Debug.Log("[ConstructionModeOverlay] Created placeholder blueprint overlay");
                }
            }
        }
        
        private void CreatePlaceholderUtilityOverlay(string utilityType)
        {
            // This is a placeholder implementation - in a real game this would load actual utility data
            var utilityObj = new GameObject($"UtilityOverlay_{utilityType}");
            utilityObj.transform.SetParent(_utilityOverlayRoot?.transform ?? transform);
            
            // Add visual component (placeholder)
            var renderer = utilityObj.AddComponent<MeshRenderer>();
            if (_utilityMaterial != null)
            {
                renderer.material = _utilityMaterial;
            }
            
            _utilityObjects[utilityType] = utilityObj;
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Created placeholder {utilityType} utility overlay");
            }
        }
        
        private void CleanupOverlayObjects()
        {
            // Clean up blueprint objects
            foreach (var blueprintObj in _blueprintObjects.Values)
            {
                if (blueprintObj != null)
                {
                    DestroyImmediate(blueprintObj);
                }
            }
            _blueprintObjects.Clear();
            
            // Clean up utility objects
            foreach (var utilityObj in _utilityObjects.Values)
            {
                if (utilityObj != null)
                {
                    DestroyImmediate(utilityObj);
                }
            }
            _utilityObjects.Clear();
        }
        
        #region UI Event Handlers
        
        private void OnBlueprintToggleChanged(bool enabled)
        {
            if (!_isConstructionModeActive) return;
            
            if (enabled)
            {
                ShowBlueprintOverlay();
            }
            else
            {
                HideBlueprintOverlay();
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Blueprint overlay toggled: {enabled}");
            }
        }
        
        private void OnBlueprintOpacityChanged(float opacity)
        {
            if (!_blueprintVisible) return;
            
            // Apply opacity to blueprint materials
            foreach (var blueprintObj in _blueprintObjects.Values)
            {
                if (blueprintObj != null)
                {
                    var renderer = blueprintObj.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        var color = renderer.material.color;
                        color.a = opacity;
                        renderer.material.color = color;
                    }
                }
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Blueprint opacity changed: {opacity:F2}");
            }
        }
        
        private void OnPlacementModeClicked()
        {
            // Toggle placement mode (placeholder implementation)
            if (_debugMode)
            {
                Debug.Log("[ConstructionModeOverlay] Placement mode activated");
            }
        }
        
        private void OnUtilityToggleChanged(string utilityType, bool enabled)
        {
            if (!_isConstructionModeActive) return;
            
            if (enabled)
            {
                ShowUtilityOverlay(utilityType);
            }
            else if (_utilityObjects.TryGetValue(utilityType, out var utilityObj) && utilityObj != null)
            {
                utilityObj.SetActive(false);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] {utilityType} utility overlay toggled: {enabled}");
            }
        }
        
        private void OnConstructionToolSelected(string toolName)
        {
            if (_debugMode)
            {
                Debug.Log($"[ConstructionModeOverlay] Construction tool selected: {toolName}");
            }
            
            // Placeholder for tool selection logic
            // In a real implementation, this would activate the appropriate construction tool
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually refresh the overlay state (for debugging)
        /// </summary>
        public void RefreshOverlay()
        {
            if (_isInitialized && _modeController != null)
            {
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                if (_debugMode)
                {
                    Debug.Log("[ConstructionModeOverlay] Overlay refreshed manually");
                }
            }
        }
        
        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            Debug.Log($"[ConstructionModeOverlay] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get current overlay visibility state
        /// </summary>
        public bool IsConstructionModeActive => _isConstructionModeActive;
        public bool IsBlueprintVisible => _blueprintVisible;
        public bool AreUtilitiesVisible => _utilitiesVisible;
        
        #endregion
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for testing overlay visibility
        /// </summary>
        [ContextMenu("Test Construction Mode Toggle")]
        private void TestConstructionModeToggle()
        {
            if (Application.isPlaying && _modeController != null)
            {
                var currentMode = _modeController.CurrentMode;
                var newMode = currentMode == GameplayMode.Construction ? GameplayMode.Cultivation : GameplayMode.Construction;
                _modeController.SetMode(newMode, "Debug Test");
            }
            else
            {
                Debug.Log("[ConstructionModeOverlay] Test only works during play mode with initialized controller");
            }
        }
        
        #endif
    }
}