using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.UI.Core;
using ProjectChimera.UI.Panels;
using ProjectChimera.UI.Components;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Lightweight orchestrator for contextual menu system.
    /// Delegates all functionality to specialized components.
    /// Refactored from 4,734-line monolith to modular architecture.
    /// </summary>
    public class ContextualMenuController : UIPanel
    {
        [Header("Component References")]
        [SerializeField] private ContextualMenuStateManager _stateManager;
        [SerializeField] private ContextualMenuEventHandler _eventHandler;
        [SerializeField] private ContextualMenuRenderer _renderer;
        [SerializeField] private WorldSpaceMenuRenderer _worldSpaceRenderer;
        [SerializeField] private ConstructionContextMenu _constructionMenu;
        [SerializeField] private CultivationContextMenu _cultivationMenu;
        [SerializeField] private GeneticsContextMenu _geneticsMenu;
        
        [Header("Configuration")]
        [SerializeField] private bool _enableWorldSpaceUI = true;
        [SerializeField] private bool _enableDebugLogging = false;
        
        // Public API - maintains compatibility with existing code
        public bool IsMenuOpen => _stateManager?.IsMenuOpen ?? false;
        public string CurrentMode => _stateManager?.CurrentMode ?? "default";
        
        // Events - forward from components
        public System.Action<string> OnMenuOpened;
        public System.Action<string> OnMenuClosed;
        public System.Action<string, string> OnMenuItemSelected;
        
        protected override void SetupUIElements()
        {
            base.SetupUIElements();
            InitializeComponents();
            WireComponentEvents();
        }
        
        private void InitializeComponents()
        {
            // Initialize or find components
            if (_stateManager == null) _stateManager = new ContextualMenuStateManager();
            if (_eventHandler == null) _eventHandler = GetComponent<ContextualMenuEventHandler>();
            if (_renderer == null) _renderer = GetComponent<ContextualMenuRenderer>();
            
            // Initialize World Space UI if enabled
            if (_enableWorldSpaceUI && _worldSpaceRenderer == null)
                _worldSpaceRenderer = GetComponent<WorldSpaceMenuRenderer>();
                
            LogDebug("Components initialized successfully");
        }
        
        private void WireComponentEvents()
        {
            // Wire state manager events
            if (_stateManager != null)
            {
                _stateManager.OnMenuOpened += HandleMenuOpened;
                _stateManager.OnMenuClosed += HandleMenuClosed;
                _stateManager.OnMenuItemSelected += HandleMenuItemSelected;
            }
            
            // Wire event handler
            if (_eventHandler != null)
            {
                _eventHandler.OnMenuItemClicked += HandleMenuItemClicked;
            }
            
            LogDebug("Component events wired successfully");
        }
        
        // Public API methods - delegate to components
        public void OpenMenu(string mode, Vector2? position = null)
        {
            float? posX = position?.x;
            float? posY = position?.y;
            _stateManager?.OpenMenu(mode, posX, posY);
        }
        
        public void CloseMenu() => _stateManager?.CloseMenu();
        
        public void SelectMenuItem(string itemId) => _stateManager?.SelectMenuItem(itemId);
        
        // Legacy public API methods - maintain compatibility
        public void ShowNotification(string message, UIStatus type = UIStatus.Info, float duration = 5f)
        {
            _renderer?.ShowNotification(message, type, duration);
        }
        
        public void ShowNotificationEnhanced(string title, string message, UIStatus type = UIStatus.Info, float duration = 5f)
        {
            _renderer?.ShowNotificationEnhanced(title, message, type, duration);
        }
        
        public void ShowPersistentNotification(string key, string message, UIStatus type = UIStatus.Warning)
        {
            _renderer?.ShowPersistentNotification(key, message, type);
        }
        
        public void DismissPersistentNotification(string key)
        {
            _renderer?.DismissPersistentNotification(key);
        }
        
        public void SetUpdatesPaused(bool paused)
        {
            _stateManager?.SetUpdatesPaused(paused);
        }
        
        public void OnSelectionChanged(Transform newSelection)
        {
            _stateManager?.OnSelectionChanged(newSelection);
        }
        
        // Event handlers - orchestrate component interactions
        private void HandleMenuOpened(string mode)
        {
            LogDebug($"Menu opened: {mode}");
            OnMenuOpened?.Invoke(mode);
        }
        
        private void HandleMenuClosed(string mode)
        {
            LogDebug($"Menu closed: {mode}");
            OnMenuClosed?.Invoke(mode);
        }
        
        private void HandleMenuItemSelected(string mode, string itemId)
        {
            LogDebug($"Menu item selected: {itemId} in mode {mode}");
            OnMenuItemSelected?.Invoke(mode, itemId);
        }
        
        private void HandleMenuItemClicked(string mode, string itemId)
        {
            // Route to appropriate mode-specific handler
            switch (mode.ToLower())
            {
                case "construction":
                    _constructionMenu?.HandleItemClicked(itemId);
                    break;
                case "cultivation":
                    _cultivationMenu?.HandleItemClicked(itemId);
                    break;
                case "genetics":
                    _geneticsMenu?.HandleItemClicked(itemId);
                    break;
                default:
                    LogDebug($"Unknown mode: {mode}");
                    break;
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[ContextualMenuController] {message}");
        }
        
        // Cleanup
        protected override void OnDestroy()
        {
            if (_stateManager != null)
            {
                _stateManager.OnMenuOpened -= HandleMenuOpened;
                _stateManager.OnMenuClosed -= HandleMenuClosed;
                _stateManager.OnMenuItemSelected -= HandleMenuItemSelected;
            }
            
            base.OnDestroy();
        }
    }
}