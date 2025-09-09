using UnityEngine;
using ProjectChimera.Core.Updates;
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
    /// Core menu infrastructure and component coordination for contextual menu system.
    /// Handles base menu functionality, initialization, and component orchestration.
    /// </summary>
    public class MenuCore : MonoBehaviour, ITickable
    {
        [Header("Menu Configuration")]
        [SerializeField] protected bool _enableModeContextualMenus = true;
        [SerializeField] protected bool _showObjectSpecificActions = true;
        [SerializeField] protected bool _enableQuickActions = true;
        [SerializeField] protected bool _debugMode = false;

        [Header("Core Menu Elements")]
        [SerializeField] protected GameObject _contextMenuPanel;
        [SerializeField] protected Transform _menuItemsContainer;
        [SerializeField] protected UnityEngine.UI.Button _contextMenuItemPrefab;

        [Header("Menu Styling")]
        [SerializeField] protected Color _menuBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] protected Color _menuItemColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] protected Color _menuItemHoverColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] protected Color _disabledItemColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        [Header("Animation Settings")]
        [SerializeField] protected float _menuFadeInDuration = 0.2f;
        [SerializeField] protected float _menuFadeOutDuration = 0.15f;
        [SerializeField] protected AnimationCurve _menuAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Event Channels")]
        [SerializeField] protected ModeChangedEventSO _modeChangedEvent;
        [SerializeField] protected CameraLevelChangedEventSO _cameraLevelChangedEvent;

        // Core services
        protected IGameplayModeController _modeController;
        
        // Core state
        protected bool _isInitialized = false;
        protected GameplayMode _currentMode = GameplayMode.Cultivation;
        protected CameraLevel _currentCameraLevel = CameraLevel.Room;
        protected GameObject _selectedObject = null;
        protected Vector3 _menuPosition = Vector3.zero;
        protected bool _isMenuVisible = false;

        // Component references
        protected MenuActionProvider _actionProvider;
        protected MenuRenderer _menuRenderer;
        protected MenuInputHandler _inputHandler;
        protected MenuAnimationController _animationController;

        // Menu management
        protected List<ContextMenuItem> _currentMenuItems = new List<ContextMenuItem>();

        public virtual void Start()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeMenu();
        }

        public virtual void Tick(float deltaTime)
        {
            if (_inputHandler != null)
            {
                _inputHandler.HandleInput(deltaTime);
            }
        }

        protected virtual void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            UnsubscribeFromEvents();
        }

        protected virtual void InitializeMenu()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();

                if (_modeController == null)
                {
                    LogError("GameplayModeController service not found!");
                    return;
                }

                // Initialize components
                InitializeComponents();

                // Subscribe to mode change events
                SubscribeToEvents();

                // Initialize menu state
                _currentMode = _modeController.CurrentMode;

                // Hide all menus initially
                HideAllMenus();

                _isInitialized = true;

                LogInfo($"Menu core initialized with mode: {_currentMode}");
            }
            catch (System.Exception ex)
            {
                LogError($"Error during menu initialization: {ex.Message}");
            }
        }

        protected virtual void InitializeComponents()
        {
            // Initialize menu components
            _actionProvider = GetComponent<MenuActionProvider>();
            if (_actionProvider == null)
            {
                _actionProvider = gameObject.AddComponent<MenuActionProvider>();
            }
            _actionProvider.Initialize(this);

            _menuRenderer = GetComponent<MenuRenderer>();
            if (_menuRenderer == null)
            {
                _menuRenderer = gameObject.AddComponent<MenuRenderer>();
            }
            _menuRenderer.Initialize(this);

            _inputHandler = GetComponent<MenuInputHandler>();
            if (_inputHandler == null)
            {
                _inputHandler = gameObject.AddComponent<MenuInputHandler>();
            }
            _inputHandler.Initialize(this);

            _animationController = GetComponent<MenuAnimationController>();
            if (_animationController == null)
            {
                _animationController = gameObject.AddComponent<MenuAnimationController>();
            }
            _animationController.Initialize(this);
        }

        protected virtual void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                LogWarning("ModeChangedEvent not assigned");
            }

            if (_cameraLevelChangedEvent != null)
            {
                _cameraLevelChangedEvent.Subscribe(OnCameraLevelChanged);
            }
            else
            {
                LogWarning("CameraLevelChangedEvent not assigned");
            }
        }

        protected virtual void UnsubscribeFromEvents()
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

        protected virtual void OnModeChanged(ModeChangeEventData eventData)
        {
            LogDebug($"Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");

            _currentMode = eventData.NewMode;

            // Hide current menu if visible (it may no longer be valid)
            if (_isMenuVisible)
            {
                HideContextMenu();
            }

            // Notify action provider about mode change
            _actionProvider?.OnModeChanged(_currentMode);
        }

        protected virtual void OnCameraLevelChanged(CameraLevelChangeEventData eventData)
        {
            LogDebug($"Camera level changed: {eventData.PreviousLevel} → {eventData.NewLevel}");

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
        }

        public virtual void ShowContextMenu(Vector3 screenPosition, GameObject targetObject = null)
        {
            _selectedObject = targetObject;
            _menuPosition = screenPosition;

            // Get valid menu items for current context
            List<ContextMenuItem> validMenuItems = _actionProvider?.GetValidMenuItems(targetObject) ?? new List<ContextMenuItem>();

            if (validMenuItems.Count == 0)
            {
                LogDebug("No valid menu items for current context");
                return;
            }

            // Show the context menu
            _menuRenderer?.DisplayContextMenu(validMenuItems, screenPosition);
            _currentMenuItems = validMenuItems;
            _isMenuVisible = true;

            LogDebug($"Context menu shown at {screenPosition} for object: {(targetObject ? targetObject.name : "None")}");
        }

        public virtual void HideContextMenu()
        {
            if (!_isMenuVisible) return;

            // Animate menu disappearance
            _animationController?.AnimateMenuOut();

            // Clear menu items
            _menuRenderer?.ClearMenuItems();
            _currentMenuItems.Clear();

            // Hide menu panel
            if (_contextMenuPanel != null)
            {
                _contextMenuPanel.SetActive(false);
            }

            _isMenuVisible = false;
            _selectedObject = null;

            LogDebug("Context menu hidden");
        }

        protected virtual void HideAllMenus()
        {
            if (_contextMenuPanel != null) _contextMenuPanel.SetActive(false);
            _isMenuVisible = false;
        }

        public virtual void OnMenuItemClicked(ContextMenuItem menuItem)
        {
            LogDebug($"Menu item clicked: {menuItem.actionName}");

            // Execute menu item action
            _actionProvider?.ExecuteMenuAction(menuItem.actionName, _selectedObject);

            // Hide menu after action
            HideContextMenu();
        }

        #region Getters and Setters

        public bool EnableModeContextualMenus
        {
            get => _enableModeContextualMenus;
            set => _enableModeContextualMenus = value;
        }

        public bool ShowObjectSpecificActions
        {
            get => _showObjectSpecificActions;
            set => _showObjectSpecificActions = value;
        }

        public bool EnableQuickActions
        {
            get => _enableQuickActions;
            set => _enableQuickActions = value;
        }

        public bool DebugMode
        {
            get => _debugMode;
            set => _debugMode = value;
        }

        public GameObject ContextMenuPanel => _contextMenuPanel;
        public Transform MenuItemsContainer => _menuItemsContainer;
        public UnityEngine.UI.Button ContextMenuItemPrefab => _contextMenuItemPrefab;

        public Color MenuBackgroundColor => _menuBackgroundColor;
        public Color MenuItemColor => _menuItemColor;
        public Color MenuItemHoverColor => _menuItemHoverColor;
        public Color DisabledItemColor => _disabledItemColor;

        public float MenuFadeInDuration => _menuFadeInDuration;
        public float MenuFadeOutDuration => _menuFadeOutDuration;
        public AnimationCurve MenuAnimationCurve => _menuAnimationCurve;

        public GameplayMode CurrentMode => _currentMode;
        public CameraLevel CurrentCameraLevel => _currentCameraLevel;
        public GameObject SelectedObject => _selectedObject;
        public bool IsMenuVisible => _isMenuVisible;

        #endregion

        #region Logging Helpers

        protected void LogInfo(string message)
        {
            ChimeraLogger.Log($"[MenuCore] {message}");
        }

        protected void LogWarning(string message)
        {
            ChimeraLogger.LogWarning($"[MenuCore] {message}");
        }

        protected void LogError(string message)
        {
            ChimeraLogger.LogError($"[MenuCore] {message}");
        }

        protected void LogDebug(string message)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[MenuCore] {message}");
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
    }

    /// <summary>
    /// Context menu item data structure
    /// </summary>
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
}