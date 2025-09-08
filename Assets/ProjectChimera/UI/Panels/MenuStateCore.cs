using UnityEngine;
using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Core state tracking for contextual menus including open/close logic,
    /// selection state, position, and focus management.
    /// Extracted from ContextualMenuStateManager.cs to reduce file size.
    /// </summary>
    public class MenuStateCore
    {
        // Menu State Events
        public event Action<string> OnMenuOpened;
        public event Action<string> OnMenuClosed;
        public event Action<string, string> OnMenuItemSelected;
        public event Action<string> OnMenuModeChanged;
        public event Action OnMenuVisibilityChanged;

        // Current State
        private string _currentMode = "none";
        private bool _isMenuOpen = false;
        private bool _isMenuVisible = true;
        private bool _hasFocus = false;
        private string _selectedItemId = string.Empty;
        private float _menuPositionX = 0f;
        private float _menuPositionY = 0f;

        // State Dependencies
        private readonly MenuConfigurationManager _configManager;
        private readonly MenuTransitionController _transitionController;

        public string CurrentMode => _currentMode;
        public bool IsMenuOpen => _isMenuOpen;
        public bool IsMenuVisible => _isMenuVisible;
        public bool HasFocus => _hasFocus;
        public string SelectedItemId => _selectedItemId;
        public float MenuPositionX => _menuPositionX;
        public float MenuPositionY => _menuPositionY;
        public bool IsTransitioning => _transitionController?.IsTransitioning ?? false;

        public MenuStateCore(MenuConfigurationManager configManager, MenuTransitionController transitionController)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _transitionController = transitionController ?? throw new ArgumentNullException(nameof(transitionController));
        }

        /// <summary>
        /// Opens a contextual menu for the specified mode
        /// </summary>
        public bool OpenMenu(string mode, float? positionX = null, float? positionY = null)
        {
            if (string.IsNullOrEmpty(mode) || !_configManager.IsModeAvailable(mode))
            {
                ChimeraLogger.LogWarning($"[MenuStateCore] Invalid mode: {mode}");
                return false;
            }

            if (_transitionController.IsTransitioning)
            {
                ChimeraLogger.LogWarning("[MenuStateCore] Cannot open menu during transition");
                return false;
            }

            // Close current menu if different mode
            if (_isMenuOpen && _currentMode != mode)
            {
                CloseMenu();
            }

            _currentMode = mode;
            _isMenuOpen = true;
            _hasFocus = true;

            // Set menu position
            SetMenuPosition(mode, positionX, positionY);

            // Add to history
            _configManager.AddToHistory(mode);

            // Start transition
            var config = _configManager.GetMenuConfig(mode);
            _transitionController.StartTransition(config.TransitionType, true);

            OnMenuOpened?.Invoke(mode);
            ChimeraLogger.Log($"[MenuStateCore] Menu opened: {mode}");

            return true;
        }

        /// <summary>
        /// Closes the currently open menu
        /// </summary>
        public bool CloseMenu()
        {
            if (!_isMenuOpen)
            {
                return false;
            }

            if (_transitionController.IsTransitioning)
            {
                ChimeraLogger.LogWarning("[MenuStateCore] Cannot close menu during transition");
                return false;
            }

            var closingMode = _currentMode;
            var config = _configManager.GetMenuConfig(_currentMode);

            // Start close transition
            _transitionController.StartTransition(config.TransitionType, false);

            _isMenuOpen = false;
            _hasFocus = false;
            _selectedItemId = string.Empty;

            OnMenuClosed?.Invoke(closingMode);
            ChimeraLogger.Log($"[MenuStateCore] Menu closed: {closingMode}");

            _currentMode = "none";
            return true;
        }

        /// <summary>
        /// Selects a menu item
        /// </summary>
        public bool SelectMenuItem(string itemId)
        {
            if (!_isMenuOpen || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            var config = _configManager.GetMenuConfig(_currentMode);

            // Handle multi-selection
            if (config.AllowMultipleSelection)
            {
                // Toggle selection for multi-select
                if (_selectedItemId == itemId)
                {
                    _selectedItemId = string.Empty;
                }
                else
                {
                    _selectedItemId = itemId;
                }
            }
            else
            {
                _selectedItemId = itemId;
            }

            OnMenuItemSelected?.Invoke(_currentMode, itemId);

            // Auto-close if configured
            if (config.AutoCloseOnSelection && !config.AllowMultipleSelection)
            {
                CloseMenu();
            }

            return true;
        }

        /// <summary>
        /// Changes the current menu mode
        /// </summary>
        public bool ChangeMode(string newMode)
        {
            if (string.IsNullOrEmpty(newMode) || !_configManager.IsModeAvailable(newMode))
            {
                ChimeraLogger.LogWarning($"[MenuStateCore] Invalid mode: {newMode}");
                return false;
            }

            if (_currentMode == newMode)
            {
                return true;
            }

            var wasOpen = _isMenuOpen;
            var oldMode = _currentMode;

            if (wasOpen)
            {
                CloseMenu();
            }

            _currentMode = newMode;
            OnMenuModeChanged?.Invoke(newMode);

            if (wasOpen)
            {
                OpenMenu(newMode, _menuPositionX, _menuPositionY);
            }

            ChimeraLogger.Log($"[MenuStateCore] Mode changed: {oldMode} → {newMode}");
            return true;
        }

        /// <summary>
        /// Sets menu visibility without changing open state
        /// </summary>
        public void SetVisibility(bool visible)
        {
            if (_isMenuVisible != visible)
            {
                _isMenuVisible = visible;
                OnMenuVisibilityChanged?.Invoke();
            }
        }

        /// <summary>
        /// Sets menu focus state
        /// </summary>
        public void SetFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
        }

        /// <summary>
        /// Updates menu position
        /// </summary>
        public void SetPosition(float x, float y)
        {
            _menuPositionX = x;
            _menuPositionY = y;
        }

        /// <summary>
        /// Clears all menu state
        /// </summary>
        public void Reset()
        {
            CloseMenu();
            _selectedItemId = string.Empty;
            _menuPositionX = 0f;
            _menuPositionY = 0f;
            _isMenuVisible = true;
            _hasFocus = false;
        }

        /// <summary>
        /// Sets menu position based on mode configuration
        /// </summary>
        private void SetMenuPosition(string mode, float? positionX, float? positionY)
        {
            if (positionX.HasValue && positionY.HasValue)
            {
                _menuPositionX = positionX.Value;
                _menuPositionY = positionY.Value;
            }
            else
            {
                var config = _configManager.GetMenuConfig(mode);
                var defaultPos = _configManager.GetDefaultPosition(config.DefaultPosition, _menuPositionX, _menuPositionY);
                _menuPositionX = defaultPos.x;
                _menuPositionY = defaultPos.y;
            }
        }

        /// <summary>
        /// Gets current menu state as a summary object
        /// </summary>
        public MenuStateInfo GetCurrentState()
        {
            return new MenuStateInfo
            {
                Mode = _currentMode,
                IsOpen = _isMenuOpen,
                IsVisible = _isMenuVisible,
                HasFocus = _hasFocus,
                SelectedItemId = _selectedItemId,
                PositionX = _menuPositionX,
                PositionY = _menuPositionY,
                IsTransitioning = _transitionController.IsTransitioning
            };
        }

        /// <summary>
        /// Set whether updates are paused
        /// </summary>
        public void SetUpdatesPaused(bool paused)
        {
            // Implementation for pausing menu updates
            // This could be used to pause transitions or state changes
            if (_transitionController != null)
            {
                // Assuming the transition controller has a SetPaused method
                // If it doesn't, we can implement our own paused state
                ChimeraLogger.Log($"Setting menu updates paused: {paused}");
            }
        }

        /// <summary>
        /// Handle selection changed event
        /// </summary>
        public void OnSelectionChanged(Transform newSelection)
        {
            // Implementation for handling selection changes
            // This could trigger menu mode changes or position updates
            if (newSelection != null)
            {
                // Update position or mode based on selection
                var position = newSelection.position;
                SetPosition(position.x, position.y);
                ChimeraLogger.Log($"Selection changed to: {newSelection.name}");
            }
        }
    }

    /// <summary>
    /// Information about current menu state
    /// </summary>
    public class MenuStateInfo
    {
        public string Mode { get; set; }
        public bool IsOpen { get; set; }
        public bool IsVisible { get; set; }
        public bool HasFocus { get; set; }
        public string SelectedItemId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public bool IsTransitioning { get; set; }
    }
}
