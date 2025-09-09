using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// Handles visual rendering and positioning of contextual menus.
    /// Manages menu button creation, styling, layout, and screen positioning.
    /// </summary>
    public class MenuRenderer : MonoBehaviour
    {
        private MenuCore _menuCore;
        
        // Menu visual elements
        private List<Button> _activeMenuButtons = new List<Button>();

        public void Initialize(MenuCore menuCore)
        {
            _menuCore = menuCore;
        }

        public void DisplayContextMenu(List<ContextMenuItem> menuItems, Vector3 screenPosition)
        {
            // Clear existing menu items
            ClearMenuItems();

            // Show context menu panel
            ShowMenuPanel(screenPosition);

            // Create menu item buttons
            CreateMenuItemButtons(menuItems);

            LogDebug($"Displayed context menu with {menuItems.Count} items at {screenPosition}");
        }

        public void ClearMenuItems()
        {
            // Destroy all existing menu item GameObjects
            foreach (var button in _activeMenuButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _activeMenuButtons.Clear();

            // Clear container
            if (_menuCore.MenuItemsContainer != null)
            {
                foreach (Transform child in _menuCore.MenuItemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void UpdateMenuVisibility(bool visible)
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                _menuCore.ContextMenuPanel.SetActive(visible);
            }
        }

        public void UpdateMenuPosition(Vector3 screenPosition)
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                RectTransform menuRect = _menuCore.ContextMenuPanel.GetComponent<RectTransform>();
                if (menuRect != null)
                {
                    menuRect.position = screenPosition;
                    AdjustMenuPosition(menuRect);
                }
            }
        }

        public void ApplyMenuStyling()
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                // Apply background styling
                var backgroundImage = _menuCore.ContextMenuPanel.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.color = _menuCore.MenuBackgroundColor;
                }

                // Apply menu item styling to all active buttons
                foreach (var button in _activeMenuButtons)
                {
                    ApplyButtonStyling(button, true);
                }
            }
        }

        private void ShowMenuPanel(Vector3 screenPosition)
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                _menuCore.ContextMenuPanel.SetActive(true);

                // Position the menu at cursor position
                RectTransform menuRect = _menuCore.ContextMenuPanel.GetComponent<RectTransform>();
                if (menuRect != null)
                {
                    menuRect.position = screenPosition;

                    // Adjust position if menu goes off-screen
                    AdjustMenuPosition(menuRect);
                }

                // Apply background styling
                ApplyBackgroundStyling();
            }
        }

        private void CreateMenuItemButtons(List<ContextMenuItem> menuItems)
        {
            if (_menuCore.ContextMenuItemPrefab == null || _menuCore.MenuItemsContainer == null) return;

            foreach (var menuItem in menuItems)
            {
                CreateMenuItemButton(menuItem);
            }
        }

        private void CreateMenuItemButton(ContextMenuItem menuItem)
        {
            // Instantiate menu item button
            Button menuButton = Instantiate(_menuCore.ContextMenuItemPrefab, _menuCore.MenuItemsContainer);
            _activeMenuButtons.Add(menuButton);

            // Set button text
            SetButtonText(menuButton, menuItem.displayName);

            // Set button icon if available
            SetButtonIcon(menuButton, menuItem.icon);

            // Apply button styling
            ApplyButtonStyling(menuButton, menuItem.isEnabled);

            // Set button interactivity
            menuButton.interactable = menuItem.isEnabled;

            // Add click handler
            if (menuItem.isEnabled)
            {
                menuButton.onClick.AddListener(() => _menuCore.OnMenuItemClicked(menuItem));
            }

            LogDebug($"Created menu button for: {menuItem.displayName}");
        }

        private void SetButtonText(Button button, string text)
        {
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }

        private void SetButtonIcon(Button button, Sprite icon)
        {
            if (icon == null) return;

            Image iconImage = button.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
        }

        private void ApplyButtonStyling(Button button, bool isEnabled)
        {
            // Set button colors
            var colors = button.colors;
            colors.normalColor = isEnabled ? _menuCore.MenuItemColor : _menuCore.DisabledItemColor;
            colors.highlightedColor = _menuCore.MenuItemHoverColor;
            colors.pressedColor = _menuCore.MenuItemHoverColor * 0.8f;
            colors.disabledColor = _menuCore.DisabledItemColor;
            button.colors = colors;

            // Apply text styling
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.color = isEnabled ? Color.white : Color.gray;
            }
        }

        private void ApplyBackgroundStyling()
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                var backgroundImage = _menuCore.ContextMenuPanel.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.color = _menuCore.MenuBackgroundColor;
                }

                // Apply rounded corners if available
                ApplyRoundedCorners(backgroundImage);
            }
        }

        private void ApplyRoundedCorners(Image backgroundImage)
        {
            // This would implement rounded corner styling if a rounded sprite is available
            // For now, just ensure the image type is appropriate
            if (backgroundImage.sprite != null)
            {
                backgroundImage.type = Image.Type.Sliced;
            }
        }

        private void AdjustMenuPosition(RectTransform menuRect)
        {
            // Get screen bounds
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 menuSize = GetMenuSize(menuRect);

            Vector3 adjustedPosition = menuRect.position;

            // Adjust X position if menu goes off right edge
            if (adjustedPosition.x + menuSize.x > screenSize.x)
            {
                adjustedPosition.x = screenSize.x - menuSize.x - 10f; // Add small margin
            }

            // Ensure menu doesn't go off left edge
            if (adjustedPosition.x < 0)
            {
                adjustedPosition.x = 10f; // Add small margin
            }

            // Adjust Y position if menu goes off bottom edge
            if (adjustedPosition.y - menuSize.y < 0)
            {
                adjustedPosition.y = menuSize.y + 10f; // Add small margin
            }

            // Ensure menu doesn't go off top edge
            if (adjustedPosition.y > screenSize.y)
            {
                adjustedPosition.y = screenSize.y - 10f; // Add small margin
            }

            menuRect.position = adjustedPosition;
        }

        private Vector2 GetMenuSize(RectTransform menuRect)
        {
            // Calculate actual menu size including all items
            Vector2 calculatedSize = menuRect.sizeDelta;

            // If size is not set, calculate based on content
            if (calculatedSize.x <= 0 || calculatedSize.y <= 0)
            {
                // Default size estimate
                int itemCount = _activeMenuButtons.Count;
                float itemHeight = 30f; // Estimated item height
                float menuWidth = 200f; // Estimated menu width

                calculatedSize = new Vector2(menuWidth, itemCount * itemHeight + 20f); // Add padding
            }

            return calculatedSize;
        }

        public void UpdateMenuLayout()
        {
            // Force layout rebuild if using layout groups
            if (_menuCore.MenuItemsContainer != null)
            {
                var layoutGroup = _menuCore.MenuItemsContainer.GetComponent<LayoutGroup>();
                if (layoutGroup != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_menuCore.MenuItemsContainer as RectTransform);
                }
            }
        }

        public void SetMenuOpacity(float opacity)
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                var canvasGroup = _menuCore.ContextMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = _menuCore.ContextMenuPanel.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = opacity;
            }
        }

        public float GetMenuOpacity()
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                var canvasGroup = _menuCore.ContextMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    return canvasGroup.alpha;
                }
            }
            return 1f;
        }

        public Vector3 GetMenuPosition()
        {
            if (_menuCore.ContextMenuPanel != null)
            {
                return _menuCore.ContextMenuPanel.transform.position;
            }
            return Vector3.zero;
        }

        public bool IsMenuDisplayed()
        {
            return _menuCore.ContextMenuPanel != null && _menuCore.ContextMenuPanel.activeInHierarchy;
        }

        public int GetActiveButtonCount()
        {
            return _activeMenuButtons.Count;
        }

        private void LogDebug(string message)
        {
            if (_menuCore.DebugMode)
            {
                ChimeraLogger.Log($"[MenuRenderer] {message}");
            }
        }
    }
}