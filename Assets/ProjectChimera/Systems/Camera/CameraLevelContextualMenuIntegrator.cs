using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Camera;
using ProjectChimera.Data.Events;
using ProjectChimera.Data.UI;
using ProjectChimera.Systems.Gameplay;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Integrates camera level changes with the contextual menu system
    /// Provides level-aware contextual menu options based on current camera level and selected targets
    /// Phase 3 implementation following roadmap requirements
    /// </summary>
    public class CameraLevelContextualMenuIntegrator : MonoBehaviour
    {
        [Header("Integration Configuration")]
        [SerializeField] private bool _enableLevelBasedMenus = true;
        [SerializeField] private bool _enableTargetSpecificActions = true;
        [SerializeField] private bool _enableLevelTransitionActions = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Component References")]
        [SerializeField] private AdvancedCameraController _cameraController;
        [SerializeField] private MonoBehaviour _contextualMenuComponent;
        
        [Header("Event Channels")]
        [SerializeField] private CameraLevelChangedEventSO _cameraLevelChangedEvent;
        
        [Header("Menu Configuration")]
        [SerializeField] private bool _showLevelInMenuTitle = true;
        [SerializeField] private bool _addQuickZoomActions = true;
        [SerializeField] private bool _addFocusActions = true;
        [SerializeField] private Color _levelSpecificMenuColor = Color.cyan;
        
        // State tracking
        private CameraLevel _currentCameraLevel = CameraLevel.Room;
        private Transform _currentTarget;
        private bool _isInitialized = false;
        private Dictionary<CameraLevel, List<CameraLevelMenuItem>> _levelMenuItems;
        private List<CameraLevelMenuItem> _activeMenuItems = new List<CameraLevelMenuItem>();
        
        // Interface reference
        private IContextualMenuProvider _contextualMenu;
        
        [System.Serializable]
        public class CameraLevelMenuItem
        {
            public string displayName;
            public string actionName;
            public CameraLevel[] validLevels;
            public bool requiresTarget;
            public string targetTag;
            public System.Action<Transform> action;
            
            public CameraLevelMenuItem(string name, string action, CameraLevel[] levels, bool needsTarget = false, string tag = "")
            {
                displayName = name;
                actionName = action;
                validLevels = levels;
                requiresTarget = needsTarget;
                targetTag = tag;
            }
        }
        
        private void Start()
        {
            InitializeIntegration();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeIntegration()
        {
            try
            {
                // Auto-find components if not assigned
                if (_cameraController == null)
                {
                    _cameraController = ServiceContainerFactory.Instance?.TryResolve<AdvancedCameraController>();
                }
                
                // Get contextual menu interface from the assigned component
                if (_contextualMenuComponent != null)
                {
                    _contextualMenu = _contextualMenuComponent.GetComponent<IContextualMenuProvider>();
                    if (_contextualMenu == null)
                    {
                        ChimeraLogger.LogError("[CameraLevelContextualMenuIntegrator] Assigned component does not implement IContextualMenuProvider!");
                        return;
                    }
                }
                else
                {
                    // Try to find any component that implements the interface
                    MonoBehaviour[] providers = /* TODO: Replace FindObjectsOfType with ServiceContainer.GetAll<MonoBehaviour>() */ new MonoBehaviour[0];
                    foreach (var provider in providers)
                    {
                        if (provider is IContextualMenuProvider menuProvider)
                        {
                            _contextualMenu = menuProvider;
                            _contextualMenuComponent = provider;
                            break;
                        }
                    }
                }
                
                if (_cameraController == null)
                {
                    ChimeraLogger.LogError("[CameraLevelContextualMenuIntegrator] AdvancedCameraController not found!");
                    return;
                }
                
                if (_contextualMenu == null)
                {
                    ChimeraLogger.LogError("[CameraLevelContextualMenuIntegrator] No IContextualMenuProvider found!");
                    return;
                }
                
                // Get current camera level
                _currentCameraLevel = _cameraController.CurrentCameraLevel;
                
                // Setup level-specific menu items
                SetupLevelMenuItems();
                
                // Subscribe to camera level change events
                SubscribeToEvents();
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Initialized at camera level: {_currentCameraLevel}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CameraLevelContextualMenuIntegrator] Error during initialization: {ex.Message}");
            }
        }
        
        private void SetupLevelMenuItems()
        {
            _levelMenuItems = new Dictionary<CameraLevel, List<CameraLevelMenuItem>>
            {
                { CameraLevel.Facility, CreateFacilityLevelMenuItems() },
                { CameraLevel.Room, CreateRoomLevelMenuItems() },
                { CameraLevel.Bench, CreateBenchLevelMenuItems() },
                { CameraLevel.Plant, CreatePlantLevelMenuItems() }
            };
        }
        
        private List<CameraLevelMenuItem> CreateFacilityLevelMenuItems()
        {
            var items = new List<CameraLevelMenuItem>();
            
            if (_enableLevelTransitionActions)
            {
                items.Add(new CameraLevelMenuItem("Zoom to Room Level", "ZoomToRoom", new[] { CameraLevel.Facility }));
                items.Add(new CameraLevelMenuItem("Focus on Room", "FocusOnRoom", new[] { CameraLevel.Facility }, true, "Room"));
            }
            
            // Facility-wide actions
            items.Add(new CameraLevelMenuItem("View Facility Overview", "FacilityOverview", new[] { CameraLevel.Facility }));
            items.Add(new CameraLevelMenuItem("Facility Statistics", "FacilityStats", new[] { CameraLevel.Facility }));
            items.Add(new CameraLevelMenuItem("Facility Settings", "FacilitySettings", new[] { CameraLevel.Facility }));
            items.Add(new CameraLevelMenuItem("Environmental Overview", "EnvironmentalOverview", new[] { CameraLevel.Facility }));
            
            return items;
        }
        
        private List<CameraLevelMenuItem> CreateRoomLevelMenuItems()
        {
            var items = new List<CameraLevelMenuItem>();
            
            if (_enableLevelTransitionActions)
            {
                items.Add(new CameraLevelMenuItem("Zoom Out to Facility", "ZoomToFacility", new[] { CameraLevel.Room }));
                items.Add(new CameraLevelMenuItem("Zoom to Bench Level", "ZoomToBench", new[] { CameraLevel.Room }));
                items.Add(new CameraLevelMenuItem("Focus on Bench", "FocusOnBench", new[] { CameraLevel.Room }, true, "Bench"));
            }
            
            // Room-level actions
            items.Add(new CameraLevelMenuItem("Room Environment", "RoomEnvironment", new[] { CameraLevel.Room }));
            items.Add(new CameraLevelMenuItem("HVAC Controls", "HVACControls", new[] { CameraLevel.Room }));
            items.Add(new CameraLevelMenuItem("Lighting Controls", "LightingControls", new[] { CameraLevel.Room }));
            items.Add(new CameraLevelMenuItem("Room Layout", "RoomLayout", new[] { CameraLevel.Room }));
            items.Add(new CameraLevelMenuItem("Equipment Overview", "EquipmentOverview", new[] { CameraLevel.Room }));
            
            return items;
        }
        
        private List<CameraLevelMenuItem> CreateBenchLevelMenuItems()
        {
            var items = new List<CameraLevelMenuItem>();
            
            if (_enableLevelTransitionActions)
            {
                items.Add(new CameraLevelMenuItem("Zoom Out to Room", "ZoomToRoom", new[] { CameraLevel.Bench }));
                items.Add(new CameraLevelMenuItem("Zoom to Plant Level", "ZoomToPlant", new[] { CameraLevel.Bench }));
                items.Add(new CameraLevelMenuItem("Focus on Plant", "FocusOnPlant", new[] { CameraLevel.Bench }, true, "Plant"));
            }
            
            // Bench-level actions
            items.Add(new CameraLevelMenuItem("Bench Overview", "BenchOverview", new[] { CameraLevel.Bench }));
            items.Add(new CameraLevelMenuItem("Plant Care Schedule", "PlantCareSchedule", new[] { CameraLevel.Bench }));
            items.Add(new CameraLevelMenuItem("Bench Environment", "BenchEnvironment", new[] { CameraLevel.Bench }));
            items.Add(new CameraLevelMenuItem("Nutrition System", "NutritionSystem", new[] { CameraLevel.Bench }));
            items.Add(new CameraLevelMenuItem("Water System", "WaterSystem", new[] { CameraLevel.Bench }));
            
            return items;
        }
        
        private List<CameraLevelMenuItem> CreatePlantLevelMenuItems()
        {
            var items = new List<CameraLevelMenuItem>();
            
            if (_enableLevelTransitionActions)
            {
                items.Add(new CameraLevelMenuItem("Zoom Out to Bench", "ZoomToBench", new[] { CameraLevel.Plant }));
                items.Add(new CameraLevelMenuItem("Zoom Out to Room", "ZoomToRoom", new[] { CameraLevel.Plant }));
            }
            
            if (_enableTargetSpecificActions)
            {
                items.Add(new CameraLevelMenuItem("Plant Details", "PlantDetails", new[] { CameraLevel.Plant }, true, "Plant"));
                items.Add(new CameraLevelMenuItem("Plant Health", "PlantHealth", new[] { CameraLevel.Plant }, true, "Plant"));
                items.Add(new CameraLevelMenuItem("Growth Progress", "GrowthProgress", new[] { CameraLevel.Plant }, true, "Plant"));
                items.Add(new CameraLevelMenuItem("Genetics Info", "GeneticsInfo", new[] { CameraLevel.Plant }, true, "Plant"));
                items.Add(new CameraLevelMenuItem("Care Actions", "CareActions", new[] { CameraLevel.Plant }, true, "Plant"));
            }
            
            return items;
        }
        
        private void SubscribeToEvents()
        {
            if (_cameraLevelChangedEvent != null)
            {
                _cameraLevelChangedEvent.Subscribe(OnCameraLevelChanged);
            }
            else
            {
                ChimeraLogger.LogWarning("[CameraLevelContextualMenuIntegrator] CameraLevelChangedEvent not assigned");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_cameraLevelChangedEvent != null)
            {
                _cameraLevelChangedEvent.Unsubscribe(OnCameraLevelChanged);
            }
        }
        
        private void OnCameraLevelChanged(CameraLevelChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Camera level changed: {eventData.PreviousLevel} → {eventData.NewLevel}");
            }
            
            _currentCameraLevel = eventData.NewLevel;
            _currentTarget = eventData.TargetObject;
            
            // Update active menu items for new level
            UpdateActiveMenuItems();
            
            // If menu is currently visible, refresh it with new level context
            if (_contextualMenu != null && _contextualMenu.IsMenuVisible)
            {
                RefreshContextualMenu();
            }
        }
        
        private void UpdateActiveMenuItems()
        {
            _activeMenuItems.Clear();
            
            if (_levelMenuItems.TryGetValue(_currentCameraLevel, out var levelItems))
            {
                foreach (var item in levelItems)
                {
                    if (IsMenuItemValidForCurrentContext(item))
                    {
                        _activeMenuItems.Add(item);
                    }
                }
            }
            
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Updated menu items for level {_currentCameraLevel}: {_activeMenuItems.Count} items available");
            }
        }
        
        private bool IsMenuItemValidForCurrentContext(CameraLevelMenuItem item)
        {
            // Check if current level is valid for this item
            bool levelValid = false;
            foreach (var validLevel in item.validLevels)
            {
                if (validLevel == _currentCameraLevel)
                {
                    levelValid = true;
                    break;
                }
            }
            if (!levelValid) return false;
            
            // Check if item requires target and we have appropriate target
            if (item.requiresTarget)
            {
                if (_currentTarget == null) return false;
                
                if (!string.IsNullOrEmpty(item.targetTag) && !_currentTarget.CompareTag(item.targetTag))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void RefreshContextualMenu()
        {
            // Hide current menu and show updated one
            if (_contextualMenu != null)
            {
                Vector3 lastMenuPosition = Input.mousePosition; // Use current mouse position
                _contextualMenu.HideMenu();
                
                // Small delay to allow menu to hide before showing new one
                Invoke(nameof(ShowUpdatedMenu), 0.1f);
            }
        }
        
        private void ShowUpdatedMenu()
        {
            if (_contextualMenu != null)
            {
                _contextualMenu.ShowContextMenu(Input.mousePosition, _currentTarget ? _currentTarget.gameObject : null);
            }
        }
        
        /// <summary>
        /// Execute camera level specific menu action
        /// </summary>
        public void ExecuteCameraLevelAction(string actionName, Transform target = null)
        {
            if (_debugMode)
            {
                string targetName = target ? target.name : "None";
                ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Executing camera level action '{actionName}' with target: {targetName}");
            }
            
            switch (actionName)
            {
                // Level transition actions
                case "ZoomToFacility":
                    _cameraController?.ZoomTo(CameraLevel.Facility);
                    break;
                case "ZoomToRoom":
                    _cameraController?.ZoomTo(CameraLevel.Room);
                    break;
                case "ZoomToBench":
                    _cameraController?.ZoomTo(CameraLevel.Bench);
                    break;
                case "ZoomToPlant":
                    _cameraController?.ZoomTo(CameraLevel.Plant);
                    break;
                    
                // Focus actions
                case "FocusOnRoom":
                case "FocusOnBench":
                case "FocusOnPlant":
                    if (target != null)
                    {
                        var targetLevel = GetTargetCameraLevel(target);
                        _cameraController?.ZoomTo(targetLevel, target);
                    }
                    break;
                    
                // Level-specific information actions
                case "FacilityOverview":
                case "FacilityStats":
                case "FacilitySettings":
                case "EnvironmentalOverview":
                case "RoomEnvironment":
                case "HVACControls":
                case "LightingControls":
                case "RoomLayout":
                case "EquipmentOverview":
                case "BenchOverview":
                case "PlantCareSchedule":
                case "BenchEnvironment":
                case "NutritionSystem":
                case "WaterSystem":
                case "PlantDetails":
                case "PlantHealth":
                case "GrowthProgress":
                case "GeneticsInfo":
                case "CareActions":
                    ExecuteInformationAction(actionName, target);
                    break;
                    
                default:
                    if (_debugMode)
                    {
                        ChimeraLogger.LogWarning($"[CameraLevelContextualMenuIntegrator] Unknown camera level action: {actionName}");
                    }
                    break;
            }
        }
        
        private void ExecuteInformationAction(string actionName, Transform target)
        {
            // This would integrate with actual UI panels and information systems
            // For now, provide debug feedback
            
            if (_debugMode)
            {
                string targetInfo = target ? $" for {target.name}" : "";
                string levelInfo = _showLevelInMenuTitle ? $" (Level: {_currentCameraLevel})" : "";
                ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Would show {actionName} panel{targetInfo}{levelInfo}");
            }
            
            // TODO: Integration with actual UI panels
            // Example: UIManager.ShowPanel(actionName, target, _currentCameraLevel);
        }
        
        private CameraLevel GetTargetCameraLevel(Transform target)
        {
            if (target.CompareTag("Plant")) return CameraLevel.Plant;
            if (target.CompareTag("Bench")) return CameraLevel.Bench;
            if (target.CompareTag("Room")) return CameraLevel.Room;
            if (target.CompareTag("Facility")) return CameraLevel.Facility;
            
            // Default based on target type inference
            if (target.name.ToLower().Contains("plant")) return CameraLevel.Plant;
            if (target.name.ToLower().Contains("bench") || target.name.ToLower().Contains("table")) return CameraLevel.Bench;
            if (target.name.ToLower().Contains("room")) return CameraLevel.Room;
            
            return CameraLevel.Room; // Default fallback
        }
        
        #region Public Interface
        
        /// <summary>
        /// Get menu items valid for current camera level
        /// </summary>
        public List<CameraLevelMenuItem> GetActiveMenuItems()
        {
            return new List<CameraLevelMenuItem>(_activeMenuItems);
        }
        
        /// <summary>
        /// Check if level-based menus are enabled
        /// </summary>
        public bool AreLevelBasedMenusEnabled => _enableLevelBasedMenus && _isInitialized;
        
        /// <summary>
        /// Get current camera level for menu context
        /// </summary>
        public CameraLevel CurrentCameraLevel => _currentCameraLevel;
        
        /// <summary>
        /// Get current selected target
        /// </summary>
        public Transform CurrentTarget => _currentTarget;
        
        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Manually refresh the contextual menu with current level context
        /// </summary>
        public void RefreshMenu()
        {
            UpdateActiveMenuItems();
            
            if (_contextualMenu != null && _contextualMenu.IsMenuVisible)
            {
                RefreshContextualMenu();
            }
        }
        
        #endregion
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for testing camera level menu integration
        /// </summary>
        [ContextMenu("Test Camera Level Menu")]
        private void TestCameraLevelMenu()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ChimeraLogger.Log($"[CameraLevelContextualMenuIntegrator] Testing menu for level: {_currentCameraLevel}");
                ChimeraLogger.Log($"Available menu items: {_activeMenuItems.Count}");
                foreach (var item in _activeMenuItems)
                {
                    ChimeraLogger.Log($"  - {item.displayName} ({item.actionName})");
                }
            }
            else
            {
                ChimeraLogger.Log("[CameraLevelContextualMenuIntegrator] Test only works during play mode after initialization");
            }
        }
        
        #endif
    }
}