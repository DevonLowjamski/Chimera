using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ProjectChimera.UI.Components;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles all UI rendering for contextual menus including UXML template loading,
    /// menu item creation, styling, and UI pooling for performance.
    /// Extracted from ContextualMenuController.cs for better maintainability.
    /// </summary>
    public class ContextualMenuRenderer
    {
        private readonly VisualElement _rootElement;
        private readonly Dictionary<string, VisualElement> _pooledElements = new Dictionary<string, VisualElement>();

        // UI References - Managed by renderer
        private VisualElement _topBar;
        private VisualElement _bottomBar;
        private VisualElement _sidePanel;
        private VisualElement _centerHUD;
        private VisualElement _notificationArea;

        // UI Data Cards and Components
        private UIDataCard _cashCard;
        private UIDataCard _timeCard;
        private UIProgressBar _overallProgressBar;

        public ContextualMenuRenderer(VisualElement rootElement)
        {
            _rootElement = rootElement;
        }

        /// <summary>
        /// Creates the main HUD layout structure
        /// </summary>
        public void CreateHUDLayout()
        {
            _rootElement.Clear();

            // Main container with flex layout
            var mainContainer = new VisualElement();
            mainContainer.name = "hud-main-container";
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Column;

            // Create UI sections
            CreateTopBarContainer();
            CreateCenterContainer(mainContainer);
            CreateBottomBarContainer();

            _rootElement.Add(mainContainer);
        }

        /// <summary>
        /// Creates the top bar container
        /// </summary>
        private void CreateTopBarContainer()
        {
            _topBar = GetOrCreatePooledElement("top-bar");
            _topBar.style.height = 60;
            _topBar.style.backgroundColor = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.9f);
            _topBar.style.flexDirection = FlexDirection.Row;
            _topBar.style.alignItems = Align.Center;
            _topBar.style.justifyContent = Justify.SpaceBetween;
            _topBar.style.paddingLeft = 16;
            _topBar.style.paddingRight = 16;
        }

        /// <summary>
        /// Creates the center container with HUD and side panel
        /// </summary>
        private void CreateCenterContainer(VisualElement mainContainer)
        {
            var centerContainer = new VisualElement();
            centerContainer.name = "center-container";
            centerContainer.style.flexGrow = 1;
            centerContainer.style.flexDirection = FlexDirection.Row;

            // Center HUD
            _centerHUD = GetOrCreatePooledElement("center-hud");
            _centerHUD.style.flexGrow = 1;
            _centerHUD.style.position = Position.Relative;

            // Side panel
            _sidePanel = GetOrCreatePooledElement("side-panel");
            _sidePanel.style.width = 300;
            _sidePanel.style.backgroundColor = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.8f);
            _sidePanel.style.paddingTop = 16;
            _sidePanel.style.paddingBottom = 16;
            _sidePanel.style.paddingLeft = 16;
            _sidePanel.style.paddingRight = 16;

            centerContainer.Add(_centerHUD);
            centerContainer.Add(_sidePanel);
            mainContainer.Add(_topBar);
            mainContainer.Add(centerContainer);
        }

        /// <summary>
        /// Creates the bottom bar container
        /// </summary>
        private void CreateBottomBarContainer()
        {
            _bottomBar = GetOrCreatePooledElement("bottom-bar");
            _bottomBar.style.height = 80;
            _bottomBar.style.backgroundColor = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.9f);
            _bottomBar.style.flexDirection = FlexDirection.Row;
            _bottomBar.style.alignItems = Align.Center;
            _bottomBar.style.justifyContent = Justify.Center;
            _bottomBar.style.paddingLeft = 16;
            _bottomBar.style.paddingRight = 16;
        }

        /// <summary>
        /// Builds the top bar with game information and controls
        /// </summary>
        public void BuildTopBar()
        {
            _topBar.Clear();

            // Left section - Game status
            var leftSection = CreateTopBarLeftSection();

            // Right section - Controls
            var rightSection = CreateTopBarRightSection();

            _topBar.Add(leftSection);
            _topBar.Add(rightSection);
        }

        /// <summary>
        /// Creates the left section of the top bar
        /// </summary>
        private VisualElement CreateTopBarLeftSection()
        {
            var leftSection = new VisualElement();
            leftSection.name = "top-bar-left";
            leftSection.style.flexDirection = FlexDirection.Row;
            leftSection.style.alignItems = Align.Center;

            // Cash display
            _cashCard = new UIDataCard("Cash", "$0", "");
            _cashCard.style.marginRight = 16;

            // Time display
            _timeCard = new UIDataCard("Day", "1", "");
            _timeCard.style.marginRight = 16;

            // Overall progress
            _overallProgressBar = new UIProgressBar(100f);
            _overallProgressBar.Format = "Progress: {0:F0}%";
            _overallProgressBar.style.minWidth = 200;

            leftSection.Add(_cashCard);
            leftSection.Add(_timeCard);
            leftSection.Add(_overallProgressBar);

            return leftSection;
        }

        /// <summary>
        /// Creates the right section of the top bar
        /// </summary>
        private VisualElement CreateTopBarRightSection()
        {
            var rightSection = new VisualElement();
            rightSection.name = "top-bar-right";
            rightSection.style.flexDirection = FlexDirection.Row;
            rightSection.style.alignItems = Align.Center;

            // Control buttons will be added by event handler
            return rightSection;
        }

        /// <summary>
        /// Builds contextual menu items for a specific mode
        /// </summary>
        public VisualElement BuildContextualMenu(string mode, List<string> menuItems)
        {
            var menuContainer = GetOrCreatePooledElement($"contextual-menu-{mode}");
            menuContainer.Clear();

            menuContainer.style.flexDirection = FlexDirection.Column;
            menuContainer.style.paddingTop = 8;
            menuContainer.style.paddingBottom = 8;
            menuContainer.style.paddingLeft = 8;
            menuContainer.style.paddingRight = 8;

            foreach (var item in menuItems)
            {
                var menuItem = CreateMenuItem(item, mode);
                menuContainer.Add(menuItem);
            }

            return menuContainer;
        }

        /// <summary>
        /// Creates a single menu item with styling
        /// </summary>
        private VisualElement CreateMenuItem(string itemText, string mode)
        {
            var menuItem = new Button();
            menuItem.text = itemText;
            menuItem.name = $"menu-item-{itemText.Replace(" ", "-").ToLower()}";

            // Apply styling based on mode
            ApplyMenuItemStyling(menuItem, mode);

            return menuItem;
        }

        /// <summary>
        /// Applies styling to menu items based on mode
        /// </summary>
        private void ApplyMenuItemStyling(Button menuItem, string mode)
        {
            // Base styling
            menuItem.style.height = 32;
            menuItem.style.marginBottom = 4;
            menuItem.style.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 0.8f);
            menuItem.style.borderTopWidth = 1;
            menuItem.style.borderBottomWidth = 1;
            menuItem.style.borderLeftWidth = 1;
            menuItem.style.borderRightWidth = 1;
            menuItem.style.borderTopColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f, 1f);
            menuItem.style.borderBottomColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f, 1f);
            menuItem.style.borderLeftColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f, 1f);
            menuItem.style.borderRightColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f, 1f);

            // Mode-specific styling
            switch (mode.ToLower())
            {
                case "construction":
                    menuItem.style.borderLeftColor = new UnityEngine.Color(0.8f, 0.6f, 0.2f, 1f);
                    menuItem.style.borderLeftWidth = 3;
                    break;
                case "cultivation":
                    menuItem.style.borderLeftColor = new UnityEngine.Color(0.2f, 0.8f, 0.3f, 1f);
                    menuItem.style.borderLeftWidth = 3;
                    break;
                case "genetics":
                    menuItem.style.borderLeftColor = new UnityEngine.Color(0.6f, 0.2f, 0.8f, 1f);
                    menuItem.style.borderLeftWidth = 3;
                    break;
            }
        }

        /// <summary>
        /// UI pooling implementation for performance
        /// </summary>
        private VisualElement GetOrCreatePooledElement(string elementName)
        {
            if (_pooledElements.TryGetValue(elementName, out var pooledElement))
            {
                return pooledElement;
            }

            var newElement = new VisualElement();
            newElement.name = elementName;
            _pooledElements[elementName] = newElement;
            return newElement;
        }

        /// <summary>
        /// Returns an element to the pool for reuse
        /// </summary>
        public void ReturnToPool(string elementName, VisualElement element)
        {
            if (element != null)
            {
                element.Clear();
                element.parent?.Remove(element);
                _pooledElements[elementName] = element;
            }
        }

        /// <summary>
        /// Clears the element pool
        /// </summary>
        public void ClearPool()
        {
            _pooledElements.Clear();
        }

        /// <summary>
        /// Show notification message
        /// </summary>
        public void ShowNotification(string message, UIStatus type = UIStatus.Info, float duration = 5f)
        {
            // Implementation would create and show notification in notification area
            ChimeraLogger.Log("SYSTEM", $"[ContextualMenuRenderer] Notification: {message} ({type})");
        }

        /// <summary>
        /// Show enhanced notification with title
        /// </summary>
        public void ShowNotificationEnhanced(string title, string message, UIStatus type = UIStatus.Info, float duration = 5f)
        {
            // Implementation would create enhanced notification
            ChimeraLogger.Log("SYSTEM", $"[ContextualMenuRenderer] Enhanced Notification: {title} - {message} ({type})");
        }

        /// <summary>
        /// Show persistent notification that stays until dismissed
        /// </summary>
        public void ShowPersistentNotification(string key, string message, UIStatus type = UIStatus.Warning)
        {
            // Implementation would create persistent notification
            ChimeraLogger.Log("SYSTEM", $"[ContextualMenuRenderer] Persistent Notification [{key}]: {message} ({type})");
        }

        /// <summary>
        /// Dismiss persistent notification by key
        /// </summary>
        public void DismissPersistentNotification(string key)
        {
            // Implementation would remove persistent notification
            ChimeraLogger.Log($"[ContextualMenuRenderer] Dismissing Persistent Notification: {key}");
        }

        /// <summary>
        /// Gets reference to UI components for external updates
        /// </summary>
        public UIDataCard CashCard => _cashCard;
        public UIDataCard TimeCard => _timeCard;
        public UIProgressBar OverallProgressBar => _overallProgressBar;
        public VisualElement TopBar => _topBar;
        public VisualElement BottomBar => _bottomBar;
        public VisualElement SidePanel => _sidePanel;
        public VisualElement CenterHUD => _centerHUD;
        public VisualElement NotificationArea => _notificationArea;
    }
}
