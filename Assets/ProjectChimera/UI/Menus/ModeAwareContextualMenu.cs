using UnityEngine;
using ProjectChimera.Core.Updates;
using UnityEngine.UI;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Systems.Camera;
using ProjectChimera.Data.Events;
using ProjectChimera.Data.Camera;
using ProjectChimera.Data.UI;
using System.Collections.Generic;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// Mode-aware contextual menu system - adapts menu content based on current gameplay mode
    /// Shows relevant actions and options for the active mode and selected objects
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class ModeAwareContextualMenu : MonoBehaviour, ITickable, IContextualMenuProvider
    {
        [Header("Menu Configuration")]
        [SerializeField] private bool _enableModeContextualMenus = true;
        [SerializeField] private bool _showObjectSpecificActions = true;
        [SerializeField] private bool _enableQuickActions = true;
        [SerializeField] private bool _debugMode = false;

        [Header("Main Context Menu")]
        [SerializeField] private GameObject _contextMenuPanel;
        [SerializeField] private Transform _menuItemsContainer;
        [SerializeField] private Button _contextMenuItemPrefab;

        [Header("Right-Click Menu")]
        [SerializeField] private GameObject _rightClickMenuPanel;
        [SerializeField] private Transform _rightClickItemsContainer;
        [SerializeField] private Button _rightClickItemPrefab;

        [Header("Mode-Specific Menus")]
        [SerializeField] private GameObject _cultivationContextMenu;
        [SerializeField] private GameObject _constructionContextMenu;
        [SerializeField] private GameObject _geneticsContextMenu;

        [Header("Object-Specific Menus")]
        [SerializeField] private GameObject _plantContextMenu;
        [SerializeField] private GameObject _equipmentContextMenu;
        [SerializeField] private GameObject _facilityContextMenu;

        [Header("Menu Styling")]
        [SerializeField] private Color _menuBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color _menuItemColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _menuItemHoverColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _disabledItemColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        [Header("Animation Settings")]
        [SerializeField] private float _menuFadeInDuration = 0.2f;
        [SerializeField] private float _menuFadeOutDuration = 0.15f;
        [SerializeField] private AnimationCurve _menuAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        [SerializeField] private CameraLevelChangedEventSO _cameraLevelChangedEvent;

        // Services
        private IGameplayModeController _modeController;

        // State tracking
        private bool _isInitialized = false;
        private GameplayMode _currentMode = GameplayMode.Cultivation;
        private CameraLevel _currentCameraLevel = CameraLevel.Room;
        private GameObject _selectedObject = null;
        private Vector3 _menuPosition = Vector3.zero;
        private bool _isMenuVisible = false;

        // Camera level integration
        private CameraLevelContextualMenuIntegrator _cameraIntegrator;

        // Menu management
        private List<ContextMenuItem> _currentMenuItems = new List<ContextMenuItem>();
        private Dictionary<GameplayMode, List<ContextMenuItem>> _modeMenuItems;
        private Dictionary<string, List<ContextMenuItem>> _objectTypeMenuItems;

        [System.Serializable]
        public class ContextMenuItem
        {
            public string displayName;
            public string actionName;
            public Sprite icon;
            public bool isEnabled;
            public bool requiresSelection;
            public GameplayMode[] validModes;
            public string[] validObjectTypes;
            public System.Action<GameObject> action;

            public ContextMenuItem(string name, string action, bool enabled = true)
            {
                displayName = name;
                actionName = action;
                isEnabled = enabled;
                requiresSelection = false;
                validModes = new GameplayMode[] { GameplayMode.Cultivation, GameplayMode.Construction, GameplayMode.Genetics };
                validObjectTypes = new string[] { };
            }
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeContextualMenu();
        }

            public void Tick(float deltaTime)
    {
            HandleInput();

    }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            UnsubscribeFromEvents();
        }

        private void InitializeContextualMenu()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();

                if (_modeController == null)
                {
                    ChimeraLogger.LogError("[ModeAwareContextualMenu] GameplayModeController service not found!");
                    return;
                }

                // Initialize menu items
                SetupModeMenuItems();
                SetupObjectTypeMenuItems();

                // Find camera level integrator
                _cameraIntegrator = ServiceContainerFactory.Instance?.TryResolve<CameraLevelContextualMenuIntegrator>();

                // Subscribe to mode change events
                SubscribeToEvents();

                // Initialize menu state
                _currentMode = _modeController.CurrentMode;

                // Hide all menus initially
                HideAllMenus();

                _isInitialized = true;

                if (_debugMode)
                {
                    ChimeraLogger.Log($"[ModeAwareContextualMenu] Initialized with mode: {_currentMode}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[ModeAwareContextualMenu] Error during initialization: {ex.Message}");
            }
        }

        private void SetupModeMenuItems()
        {
            _modeMenuItems = new Dictionary<GameplayMode, List<ContextMenuItem>>
            {
                { GameplayMode.Cultivation, CreateCultivationMenuItems() },
                { GameplayMode.Construction, CreateConstructionMenuItems() },
                { GameplayMode.Genetics, CreateGeneticsMenuItems() }
            };
        }

        private List<ContextMenuItem> CreateCultivationMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Water Plant", "WaterPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Add Nutrients", "AddNutrients") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Inspect Plant", "InspectPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Harvest Plant", "HarvestPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Prune Plant", "PrunePlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Plant Seed", "PlantSeed"),
                new ContextMenuItem("Check Environment", "CheckEnvironment"),
                new ContextMenuItem("Schedule Care", "ScheduleCare")
            };
        }

        private List<ContextMenuItem> CreateConstructionMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Place Wall", "PlaceWall"),
                new ContextMenuItem("Add Door", "AddDoor"),
                new ContextMenuItem("Install Window", "InstallWindow"),
                new ContextMenuItem("Add Equipment", "AddEquipment"),
                new ContextMenuItem("Edit Blueprint", "EditBlueprint"),
                new ContextMenuItem("Remove Structure", "RemoveStructure") { requiresSelection = true, validObjectTypes = new[] { "Wall", "Door", "Window", "Equipment" } },
                new ContextMenuItem("View Utilities", "ViewUtilities"),
                new ContextMenuItem("Check Connections", "CheckConnections") { requiresSelection = true, validObjectTypes = new[] { "Equipment" } }
            };
        }

        private List<ContextMenuItem> CreateGeneticsMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Analyze Genetics", "AnalyzeGenetics") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("View Lineage", "ViewLineage") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Start Breeding", "StartBreeding"),
                new ContextMenuItem("Compare Traits", "CompareTraits") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Create Cross", "CreateCross"),
                new ContextMenuItem("View Phenotype", "ViewPhenotype") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Track Inheritance", "TrackInheritance") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Export Genetics", "ExportGenetics") { requiresSelection = true, validObjectTypes = new[] { "Plant" } }
            };
        }

        private void SetupObjectTypeMenuItems()
        {
            _objectTypeMenuItems = new Dictionary<string, List<ContextMenuItem>>
            {
                { "Plant", CreatePlantMenuItems() },
                { "Equipment", CreateEquipmentMenuItems() },
                { "Facility", CreateFacilityMenuItems() }
            };
        }

        private List<ContextMenuItem> CreatePlantMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Select Plant", "SelectPlant"),
                new ContextMenuItem("Move Plant", "MovePlant"),
                new ContextMenuItem("Clone Plant", "ClonePlant") { validModes = new[] { GameplayMode.Genetics } },
                new ContextMenuItem("Remove Plant", "RemovePlant"),
                new ContextMenuItem("Tag Plant", "TagPlant")
            };
        }

        private List<ContextMenuItem> CreateEquipmentMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Configure Equipment", "ConfigureEquipment"),
                new ContextMenuItem("Move Equipment", "MoveEquipment") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("Repair Equipment", "RepairEquipment"),
                new ContextMenuItem("Upgrade Equipment", "UpgradeEquipment"),
                new ContextMenuItem("Remove Equipment", "RemoveEquipment") { validModes = new[] { GameplayMode.Construction } }
            };
        }

        private List<ContextMenuItem> CreateFacilityMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Expand Facility", "ExpandFacility") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("Modify Layout", "ModifyLayout") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("View Statistics", "ViewStatistics"),
                new ContextMenuItem("Facility Settings", "FacilitySettings")
            };
        }

        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                ChimeraLogger.LogWarning("[ModeAwareContextualMenu] ModeChangedEvent not assigned");
            }

            if (_cameraLevelChangedEvent != null)
            {
                _cameraLevelChangedEvent.Subscribe(OnCameraLevelChanged);
            }
            else
            {
                ChimeraLogger.LogWarning("[ModeAwareContextualMenu] CameraLevelChangedEvent not assigned");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }

            if (_cameraLevelChangedEvent != null)
            {
                _cameraLevelChangedEvent.Unsubscribe(OnCameraLevelChanged);
            }
        }

        private void HandleInput()
        {
            // Handle right-click for context menu
            if (Input.GetMouseButtonDown(1) && _enableModeContextualMenus) // Right mouse button
            {
                Vector3 mousePosition = Input.mousePosition;
                ShowContextMenuAtPosition(mousePosition);
            }

            // Hide menu when clicking elsewhere or pressing escape
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isMenuVisible)
                {
                    HideContextMenu();
                }
            }
        }

        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }

            _currentMode = eventData.NewMode;

            // Hide current menu if visible (it may no longer be valid)
            if (_isMenuVisible)
            {
                HideContextMenu();
            }

            // Update mode-specific menu visibility
            UpdateModeSpecificMenus(_currentMode);
        }

        private void OnCameraLevelChanged(CameraLevelChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Camera level changed: {eventData.PreviousLevel} → {eventData.NewLevel}");
            }

            _currentCameraLevel = eventData.NewLevel;

            // Update selected object if provided
            if (eventData.TargetObject != null)
            {
                _selectedObject = eventData.TargetObject.gameObject;
            }

            // Hide current menu if visible (it may no longer be valid for new level)
            if (_isMenuVisible)
            {
                HideContextMenu();
            }

            // Notify camera integrator about the level change
            if (_cameraIntegrator != null)
            {
                _cameraIntegrator.RefreshMenu();
            }
        }

        private void UpdateModeSpecificMenus(GameplayMode mode)
        {
            // Show/hide mode-specific context menu panels
            if (_cultivationContextMenu != null)
            {
                _cultivationContextMenu.SetActive(mode == GameplayMode.Cultivation);
            }

            if (_constructionContextMenu != null)
            {
                _constructionContextMenu.SetActive(mode == GameplayMode.Construction);
            }

            if (_geneticsContextMenu != null)
            {
                _geneticsContextMenu.SetActive(mode == GameplayMode.Genetics);
            }
        }

        private void ShowContextMenuAtPosition(Vector3 screenPosition)
        {
            // Detect what object is under the cursor
            GameObject targetObject = GetObjectUnderCursor();
            _selectedObject = targetObject;
            _menuPosition = screenPosition;

            // Build menu items for current context
            List<ContextMenuItem> validMenuItems = GetValidMenuItems(targetObject);

            if (validMenuItems.Count == 0)
            {
                if (_debugMode)
                {
                    ChimeraLogger.Log("[ModeAwareContextualMenu] No valid menu items for current context");
                }
                return;
            }

            // Show the context menu
            DisplayContextMenu(validMenuItems, screenPosition);

            if (_debugMode)
            {
                string objectName = targetObject ? targetObject.name : "None";
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Context menu shown at {screenPosition} for object: {objectName}");
            }
        }

        private GameObject GetObjectUnderCursor()
        {
            // Cast a ray from camera through mouse position
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return null;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        private List<ContextMenuItem> GetValidMenuItems(GameObject targetObject)
        {
            List<ContextMenuItem> validItems = new List<ContextMenuItem>();

            // Add camera level-specific menu items first
            if (_cameraIntegrator != null && _cameraIntegrator.AreLevelBasedMenusEnabled)
            {
                var cameraLevelItems = _cameraIntegrator.GetActiveMenuItems();
                foreach (var cameraItem in cameraLevelItems)
                {
                    // Convert camera level menu item to context menu item
                    var contextItem = new ContextMenuItem(cameraItem.displayName, cameraItem.actionName);
                    contextItem.requiresSelection = cameraItem.requiresTarget;
                    if (!string.IsNullOrEmpty(cameraItem.targetTag))
                    {
                        contextItem.validObjectTypes = new[] { cameraItem.targetTag };
                    }

                    if (IsMenuItemValid(contextItem, targetObject))
                    {
                        validItems.Add(contextItem);
                    }
                }
            }

            // Add mode-specific menu items
            if (_modeMenuItems.TryGetValue(_currentMode, out var modeItems))
            {
                foreach (var item in modeItems)
                {
                    if (IsMenuItemValid(item, targetObject))
                    {
                        validItems.Add(item);
                    }
                }
            }

            // Add object-specific menu items
            if (targetObject != null && _showObjectSpecificActions)
            {
                string objectType = GetObjectType(targetObject);
                if (_objectTypeMenuItems.TryGetValue(objectType, out var objectItems))
                {
                    foreach (var item in objectItems)
                    {
                        if (IsMenuItemValid(item, targetObject))
                        {
                            validItems.Add(item);
                        }
                    }
                }
            }

            return validItems;
        }

        private bool IsMenuItemValid(ContextMenuItem item, GameObject targetObject)
        {
            // Check if item is enabled
            if (!item.isEnabled) return false;

            // Check if item requires selection but no object is selected
            if (item.requiresSelection && targetObject == null) return false;

            // Check if current mode is valid for this item
            bool modeValid = false;
            foreach (var validMode in item.validModes)
            {
                if (validMode == _currentMode)
                {
                    modeValid = true;
                    break;
                }
            }
            if (!modeValid) return false;

            // Check if object type is valid for this item
            if (targetObject != null && item.validObjectTypes.Length > 0)
            {
                string objectType = GetObjectType(targetObject);
                bool objectTypeValid = false;
                foreach (var validObjectType in item.validObjectTypes)
                {
                    if (validObjectType == objectType)
                    {
                        objectTypeValid = true;
                        break;
                    }
                }
                if (!objectTypeValid) return false;
            }

            return true;
        }

        private string GetObjectType(GameObject obj)
        {
            // Determine object type based on tags or component types
            if (obj.CompareTag("Plant")) return "Plant";
            if (obj.CompareTag("Equipment")) return "Equipment";
            if (obj.CompareTag("Facility")) return "Facility";
            if (obj.name.Contains("Wall") || obj.name.Contains("Door") || obj.name.Contains("Window")) return obj.name.Split('_')[0];

            return "Unknown";
        }

        private void DisplayContextMenu(List<ContextMenuItem> menuItems, Vector3 screenPosition)
        {
            // Clear existing menu items
            ClearMenuItems();

            // Show context menu panel
            if (_contextMenuPanel != null)
            {
                _contextMenuPanel.SetActive(true);

                // Position the menu at cursor position
                RectTransform menuRect = _contextMenuPanel.GetComponent<RectTransform>();
                if (menuRect != null)
                {
                    menuRect.position = screenPosition;

                    // Adjust position if menu goes off-screen
                    AdjustMenuPosition(menuRect);
                }
            }

            // Create menu item buttons
            foreach (var menuItem in menuItems)
            {
                CreateMenuItemButton(menuItem);
            }

            _currentMenuItems = menuItems;
            _isMenuVisible = true;

            // Animate menu appearance (placeholder)
            AnimateMenuIn();
        }

        private void CreateMenuItemButton(ContextMenuItem menuItem)
        {
            if (_contextMenuItemPrefab == null || _menuItemsContainer == null) return;

            // Instantiate menu item button
            Button menuButton = Instantiate(_contextMenuItemPrefab, _menuItemsContainer);

            // Set button text
            Text buttonText = menuButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = menuItem.displayName;
            }

            // Set button colors
            var colors = menuButton.colors;
            colors.normalColor = menuItem.isEnabled ? _menuItemColor : _disabledItemColor;
            colors.highlightedColor = _menuItemHoverColor;
            menuButton.colors = colors;

            // Set button interactivity
            menuButton.interactable = menuItem.isEnabled;

            // Add click handler
            if (menuItem.isEnabled)
            {
                menuButton.onClick.AddListener(() => OnMenuItemClicked(menuItem));
            }
        }

        private void AdjustMenuPosition(RectTransform menuRect)
        {
            // Get screen bounds
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 menuSize = menuRect.sizeDelta;

            Vector3 adjustedPosition = menuRect.position;

            // Adjust X position if menu goes off right edge
            if (adjustedPosition.x + menuSize.x > screenSize.x)
            {
                adjustedPosition.x = screenSize.x - menuSize.x;
            }

            // Adjust Y position if menu goes off bottom edge
            if (adjustedPosition.y - menuSize.y < 0)
            {
                adjustedPosition.y = menuSize.y;
            }

            menuRect.position = adjustedPosition;
        }

        private void ClearMenuItems()
        {
            if (_menuItemsContainer == null) return;

            // Destroy all existing menu item GameObjects
            foreach (Transform child in _menuItemsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void HideContextMenu()
        {
            if (!_isMenuVisible) return;

            // Animate menu disappearance (placeholder)
            AnimateMenuOut();

            // Clear menu items
            ClearMenuItems();
            _currentMenuItems.Clear();

            // Hide menu panel
            if (_contextMenuPanel != null)
            {
                _contextMenuPanel.SetActive(false);
            }

            _isMenuVisible = false;
            _selectedObject = null;

            if (_debugMode)
            {
                ChimeraLogger.Log("[ModeAwareContextualMenu] Context menu hidden");
            }
        }

        private void HideAllMenus()
        {
            if (_contextMenuPanel != null) _contextMenuPanel.SetActive(false);
            if (_rightClickMenuPanel != null) _rightClickMenuPanel.SetActive(false);
            if (_cultivationContextMenu != null) _cultivationContextMenu.SetActive(false);
            if (_constructionContextMenu != null) _constructionContextMenu.SetActive(false);
            if (_geneticsContextMenu != null) _geneticsContextMenu.SetActive(false);

            _isMenuVisible = false;
        }

        private void AnimateMenuIn()
        {
            // Placeholder for menu animation - would use actual animation system
            if (_contextMenuPanel != null)
            {
                var canvasGroup = _contextMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = _contextMenuPanel.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 1f;
            }
        }

        private void AnimateMenuOut()
        {
            // Placeholder for menu animation - would use actual animation system
            if (_contextMenuPanel != null)
            {
                var canvasGroup = _contextMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        private void OnMenuItemClicked(ContextMenuItem menuItem)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Menu item clicked: {menuItem.actionName}");
            }

            // Execute menu item action
            ExecuteMenuAction(menuItem.actionName, _selectedObject);

            // Hide menu after action
            HideContextMenu();
        }

        private void ExecuteMenuAction(string actionName, GameObject targetObject)
        {
            // Check if this is a camera level action first
            if (_cameraIntegrator != null && IsCameraLevelAction(actionName))
            {
                Transform target = targetObject ? targetObject.transform : null;
                _cameraIntegrator.ExecuteCameraLevelAction(actionName, target);
                return;
            }

            // This would integrate with your actual game systems
            // For now, it's a placeholder implementation

            switch (actionName)
            {
                // Cultivation actions
                case "WaterPlant":
                case "AddNutrients":
                case "InspectPlant":
                case "HarvestPlant":
                case "PrunePlant":
                case "PlantSeed":
                case "CheckEnvironment":
                case "ScheduleCare":
                    ExecuteCultivationAction(actionName, targetObject);
                    break;

                // Construction actions
                case "PlaceWall":
                case "AddDoor":
                case "InstallWindow":
                case "AddEquipment":
                case "EditBlueprint":
                case "RemoveStructure":
                case "ViewUtilities":
                case "CheckConnections":
                    ExecuteConstructionAction(actionName, targetObject);
                    break;

                // Genetics actions
                case "AnalyzeGenetics":
                case "ViewLineage":
                case "StartBreeding":
                case "CompareTraits":
                case "CreateCross":
                case "ViewPhenotype":
                case "TrackInheritance":
                case "ExportGenetics":
                    ExecuteGeneticsAction(actionName, targetObject);
                    break;

                // Object-specific actions
                case "SelectPlant":
                case "MovePlant":
                case "ClonePlant":
                case "RemovePlant":
                case "TagPlant":
                case "ConfigureEquipment":
                case "MoveEquipment":
                case "RepairEquipment":
                case "UpgradeEquipment":
                case "RemoveEquipment":
                case "ExpandFacility":
                case "ModifyLayout":
                case "ViewStatistics":
                case "FacilitySettings":
                    ExecuteObjectAction(actionName, targetObject);
                    break;

                default:
                    if (_debugMode)
                    {
                        ChimeraLogger.LogWarning($"[ModeAwareContextualMenu] Unknown action: {actionName}");
                    }
                    break;
            }
        }

        private void ExecuteCultivationAction(string actionName, GameObject targetObject)
        {
            if (_debugMode)
            {
                string objectName = targetObject ? targetObject.name : "None";
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Executing cultivation action '{actionName}' on object: {objectName}");
            }

            // Placeholder for actual cultivation system integration
        }

        private void ExecuteConstructionAction(string actionName, GameObject targetObject)
        {
            if (_debugMode)
            {
                string objectName = targetObject ? targetObject.name : "None";
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Executing construction action '{actionName}' on object: {objectName}");
            }

            // Placeholder for actual construction system integration
        }

        private void ExecuteGeneticsAction(string actionName, GameObject targetObject)
        {
            if (_debugMode)
            {
                string objectName = targetObject ? targetObject.name : "None";
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Executing genetics action '{actionName}' on object: {objectName}");
            }

            // Placeholder for actual genetics system integration
        }

        private void ExecuteObjectAction(string actionName, GameObject targetObject)
        {
            if (_debugMode)
            {
                string objectName = targetObject ? targetObject.name : "None";
                ChimeraLogger.Log($"[ModeAwareContextualMenu] Executing object action '{actionName}' on object: {objectName}");
            }

            // Placeholder for actual object manipulation system integration
        }

        private bool IsCameraLevelAction(string actionName)
        {
            // Check if action is a camera level specific action
            return actionName.StartsWith("ZoomTo") || actionName.StartsWith("FocusOn") ||
                   actionName.Contains("Overview") || actionName.Contains("Environment") ||
                   actionName.Contains("Controls") || actionName.Contains("Layout") ||
                   actionName.Contains("System") || actionName.Contains("Details") ||
                   actionName.Contains("Health") || actionName.Contains("Progress") ||
                   actionName.Contains("Info") || actionName.Contains("Actions") ||
                   actionName.Contains("Stats") || actionName.Contains("Settings");
        }

        #region Public Interface

        /// <summary>
        /// Manually show context menu at specified position
        /// </summary>
        public void ShowContextMenu(Vector3 screenPosition, GameObject targetObject = null)
        {
            _selectedObject = targetObject;
            ShowContextMenuAtPosition(screenPosition);
        }

        /// <summary>
        /// Hide the context menu if visible
        /// </summary>
        public void HideMenu()
        {
            if (_isMenuVisible)
            {
                HideContextMenu();
            }
        }

        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            ChimeraLogger.Log($"[ModeAwareContextualMenu] Debug mode {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get current menu state
        /// </summary>
        public bool IsMenuVisible => _isMenuVisible;
        public GameplayMode CurrentMode => _currentMode;
        public CameraLevel CurrentCameraLevel => _currentCameraLevel;
        public GameObject SelectedObject => _selectedObject;

        #endregion

        #if UNITY_EDITOR

        /// <summary>
        /// Editor-only method for testing context menu
        /// </summary>
        [ContextMenu("Test Context Menu")]
        private void TestContextMenu()
        {
            if (Application.isPlaying)
            {
                Vector3 testPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                ShowContextMenuAtPosition(testPosition);
            }
            else
            {
                ChimeraLogger.Log("[ModeAwareContextualMenu] Test only works during play mode");
            }
        }

        #endif

    // ITickable implementation
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
}
}
