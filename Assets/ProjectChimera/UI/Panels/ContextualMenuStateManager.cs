using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core.Logging;

// Assembly reference fix applied - enums converted to const strings, Vector2 replaced with float components

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages contextual menu state including open/close logic, active item tracking,
    /// selection state, visibility, and focus management.
    /// Refactored to use composition with specialized components for better maintainability.
    /// </summary>
    public class ContextualMenuStateManager
    {
        // Core Components
        private readonly MenuConfigurationManager _configManager;
        private readonly MenuTransitionController _transitionController;
        private readonly MenuStateCore _stateCore;

        // Event forwarding from core components
        public event Action<string> OnMenuOpened
        {
            add { _stateCore.OnMenuOpened += value; }
            remove { _stateCore.OnMenuOpened -= value; }
        }

        public event Action<string> OnMenuClosed
        {
            add { _stateCore.OnMenuClosed += value; }
            remove { _stateCore.OnMenuClosed -= value; }
        }

        public event Action<string, string> OnMenuItemSelected
        {
            add { _stateCore.OnMenuItemSelected += value; }
            remove { _stateCore.OnMenuItemSelected -= value; }
        }

        public event Action<string> OnMenuModeChanged
        {
            add { _stateCore.OnMenuModeChanged += value; }
            remove { _stateCore.OnMenuModeChanged -= value; }
        }

        public event Action OnMenuVisibilityChanged
        {
            add { _stateCore.OnMenuVisibilityChanged += value; }
            remove { _stateCore.OnMenuVisibilityChanged -= value; }
        }

        // State property forwarding
        public string CurrentMode => _stateCore.CurrentMode;
        public bool IsMenuOpen => _stateCore.IsMenuOpen;
        public bool IsMenuVisible => _stateCore.IsMenuVisible;
        public bool HasFocus => _stateCore.HasFocus;
        public string SelectedItemId => _stateCore.SelectedItemId;
        public float MenuPositionX => _stateCore.MenuPositionX;
        public float MenuPositionY => _stateCore.MenuPositionY;
        public bool IsTransitioning => _transitionController.IsTransitioning;
        public float TransitionProgress => _transitionController.TransitionProgress;

        public ContextualMenuStateManager()
        {
            // Initialize components
            _configManager = new MenuConfigurationManager();
            _transitionController = new MenuTransitionController();
            _stateCore = new MenuStateCore(_configManager, _transitionController);

            // Set up automatic transition updates
            ConnectTransitionUpdates();
        }

        /// <summary>
        /// Opens a contextual menu for the specified mode
        /// </summary>
        public bool OpenMenu(string mode, float? positionX = null, float? positionY = null)
        {
            return _stateCore.OpenMenu(mode, positionX, positionY);
        }

        /// <summary>
        /// Closes the currently open menu
        /// </summary>
        public bool CloseMenu()
        {
            return _stateCore.CloseMenu();
        }

        /// <summary>
        /// Selects a menu item
        /// </summary>
        public bool SelectMenuItem(string itemId)
        {
            return _stateCore.SelectMenuItem(itemId);
        }

        /// <summary>
        /// Changes the current menu mode
        /// </summary>
        public bool ChangeMode(string newMode)
        {
            return _stateCore.ChangeMode(newMode);
        }

        /// <summary>
        /// Sets menu visibility without changing open state
        /// </summary>
        public void SetVisibility(bool visible)
        {
            _stateCore.SetVisibility(visible);
        }

        /// <summary>
        /// Sets menu focus state
        /// </summary>
        public void SetFocus(bool hasFocus)
        {
            _stateCore.SetFocus(hasFocus);
        }

        /// <summary>
        /// Updates menu position
        /// </summary>
        public void SetPosition(float x, float y)
        {
            _stateCore.SetPosition(x, y);
        }

        /// <summary>
        /// Gets menu configuration for a mode
        /// </summary>
        public MenuConfig GetMenuConfig(string mode)
        {
            return _configManager.GetMenuConfig(mode);
        }

        /// <summary>
        /// Registers a new menu mode with configuration
        /// </summary>
        public void RegisterMode(string mode, MenuConfig config)
        {
            _configManager.RegisterMode(mode, config);
        }

        /// <summary>
        /// Gets available menu modes
        /// </summary>
        public IEnumerable<string> GetAvailableModes()
        {
            return _configManager.AvailableModes;
        }

        /// <summary>
        /// Gets menu history for a mode
        /// </summary>
        public List<string> GetMenuHistory(string mode)
        {
            return _configManager.GetMenuHistory(mode);
        }

        /// <summary>
        /// Clears all menu state
        /// </summary>
        public void Reset()
        {
            _stateCore.Reset();
            _transitionController.Reset();
            _configManager.ClearHistory();
        }

        /// <summary>
        /// Updates transition progress (called by animation system)
        /// </summary>
        public void UpdateTransition(float progress)
        {
            _transitionController.SetTransitionProgress(progress);
        }

        /// <summary>
        /// Updates transition automatically (call from Update loop if needed)
        /// </summary>
        public void UpdateTransitionAutomatic()
        {
            _transitionController.UpdateTransition();
        }

        /// <summary>
        /// Gets transition parameters for UI systems
        /// </summary>
        public TransitionParams GetTransitionParams(string transitionType)
        {
            return _transitionController.GetTransitionParams(transitionType);
        }

        /// <summary>
        /// Gets current state information
        /// </summary>
        public MenuStateInfo GetCurrentState()
        {
            return _stateCore.GetCurrentState();
        }

        /// <summary>
        /// Gets configuration manager for advanced operations
        /// </summary>
        public MenuConfigurationManager GetConfigurationManager()
        {
            return _configManager;
        }

        /// <summary>
        /// Gets transition controller for advanced operations
        /// </summary>
        public MenuTransitionController GetTransitionController()
        {
            return _transitionController;
        }



        /// <summary>
        /// Set updates paused state
        /// </summary>
        public void SetUpdatesPaused(bool paused)
        {
            _stateCore.SetUpdatesPaused(paused);
        }

        /// <summary>
        /// Handle selection changed event
        /// </summary>
        public void OnSelectionChanged(Transform newSelection)
        {
            _stateCore.OnSelectionChanged(newSelection);
        }



        /// <summary>
        /// Connects transition controller updates to provide automatic progress updates
        /// </summary>
        private void ConnectTransitionUpdates()
        {
            // The transition controller will handle its own updates
            // UI systems can subscribe to transition events for visual updates
            _transitionController.OnTransitionUpdate += (transitionType, progress) => {
                // This can be used by UI systems to update visual transitions
                ChimeraLogger.Log($"[ContextualMenuStateManager] Transition update: {transitionType} at {progress:P1}");
            };

            _transitionController.OnTransitionComplete += (transitionType, wasOpening) => {
                ChimeraLogger.Log("SYSTEM", $"[ContextualMenuStateManager] Transition complete: {transitionType} (was opening: {wasOpening})");
            };
        }
    }

    /// <summary>
    /// Configuration for a contextual menu mode
    /// </summary>
    [System.Serializable]
    public class MenuConfig
    {
        public string Mode = "default";
        public bool AutoCloseOnSelection = true;
        public bool AllowMultipleSelection = false;
        public int MaxMenuItems = 10;
        public string DefaultPosition = MenuPosition.Cursor;
        public string TransitionType = MenuTransition.Fade;
        public float TransitionDuration = 0.2f;
    }

    /// <summary>
    /// Menu position types
    /// </summary>
    public static class MenuPosition
    {
        public const string Cursor = "Cursor";
        public const string Center = "Center";
        public const string Fixed = "Fixed";
        public const string Context = "Context";
    }

    /// <summary>
    /// Menu transition types
    /// </summary>
    public static class MenuTransition
    {
        public const string None = "None";
        public const string Fade = "Fade";
        public const string Slide = "Slide";
        public const string Scale = "Scale";
    }
}
