using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Simple
{
    /// <summary>
    /// Simple UI manager aligned with Project Chimera vision
    /// Basic UI management for cultivation gameplay
    /// </summary>
    public class SimpleUIManager : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private UIDocument _mainUIDocument;
        [SerializeField] private bool _enableNotifications = true;
        [SerializeField] private float _notificationDuration = 3f;

        // UI State
        private VisualElement _rootElement;
        private VisualElement _mainContainer;
        private Label _notificationLabel;
        private bool _isInitialized;

        // Screen management
        private Dictionary<string, VisualElement> _screens = new Dictionary<string, VisualElement>();
        private string _currentScreenId;

        // Events
        public event Action<string> OnScreenChanged;
        public event Action<string> OnNotificationShown;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (_mainUIDocument == null)
            {
                ChimeraLogger.LogInfo("SimpleUIManager", "$1");
                return;
            }

            _rootElement = _mainUIDocument.rootVisualElement;

            // Create basic UI structure
            _mainContainer = new VisualElement();
            _mainContainer.name = "main-container";
            _mainContainer.style.flexGrow = 1;
            _rootElement.Add(_mainContainer);

            // Create notification area
            var notificationContainer = new VisualElement();
            notificationContainer.name = "notification-container";
            notificationContainer.style.position = Position.Absolute;
            notificationContainer.style.top = 20;
            notificationContainer.style.right = 20;
            notificationContainer.style.width = 300;
            notificationContainer.style.height = 100;

            _notificationLabel = new Label();
            _notificationLabel.name = "notification-label";
            _notificationLabel.style.color = Color.white;
            _notificationLabel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            _notificationLabel.style.paddingTop = 5;
            _notificationLabel.style.paddingBottom = 5;
            _notificationLabel.style.paddingLeft = 10;
            _notificationLabel.style.paddingRight = 10;
            _notificationLabel.style.borderTopLeftRadius = 5;
            _notificationLabel.style.borderTopRightRadius = 5;
            _notificationLabel.style.borderBottomLeftRadius = 5;
            _notificationLabel.style.borderBottomRightRadius = 5;
            _notificationLabel.style.display = DisplayStyle.None;

            notificationContainer.Add(_notificationLabel);
            _rootElement.Add(notificationContainer);

            _isInitialized = true;
            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        #region Screen Management

        /// <summary>
        /// Register a UI screen
        /// </summary>
        public void RegisterScreen(string screenId, VisualElement screenElement)
        {
            if (!_isInitialized) return;

            if (_screens.ContainsKey(screenId))
            {
                ChimeraLogger.LogInfo("SimpleUIManager", "$1");
                _mainContainer.Remove(_screens[screenId]);
            }

            _screens[screenId] = screenElement;
            screenElement.style.display = DisplayStyle.None;
            _mainContainer.Add(screenElement);

            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        /// <summary>
        /// Show a specific screen
        /// </summary>
        public void ShowScreen(string screenId)
        {
            if (!_isInitialized || !_screens.ContainsKey(screenId)) return;

            // Hide current screen
            if (!string.IsNullOrEmpty(_currentScreenId) && _screens.ContainsKey(_currentScreenId))
            {
                _screens[_currentScreenId].style.display = DisplayStyle.None;
            }

            // Show new screen
            _screens[screenId].style.display = DisplayStyle.Flex;
            _currentScreenId = screenId;

            OnScreenChanged?.Invoke(screenId);
            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        /// <summary>
        /// Hide all screens
        /// </summary>
        public void HideAllScreens()
        {
            foreach (var screen in _screens.Values)
            {
                screen.style.display = DisplayStyle.None;
            }
            _currentScreenId = null;
            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        /// <summary>
        /// Get current screen ID
        /// </summary>
        public string GetCurrentScreen()
        {
            return _currentScreenId;
        }

        #endregion

        #region Notification System

        /// <summary>
        /// Show a notification message
        /// </summary>
        public void ShowNotification(string message, float duration = 0f)
        {
            if (!_isInitialized || !_enableNotifications) return;

            _notificationLabel.text = message;
            _notificationLabel.style.display = DisplayStyle.Flex;

            float actualDuration = duration > 0 ? duration : _notificationDuration;

            // Auto-hide after duration
            StartCoroutine(HideNotificationAfterDelay(actualDuration));

            OnNotificationShown?.Invoke(message);
            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        private System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideNotification();
        }

        /// <summary>
        /// Hide current notification
        /// </summary>
        public void HideNotification()
        {
            if (!_isInitialized) return;

            _notificationLabel.style.display = DisplayStyle.None;
            ChimeraLogger.LogInfo("SimpleUIManager", "$1");
        }

        #endregion

        #region Cultivation UI Helpers

        /// <summary>
        /// Create a simple cultivation status display
        /// </summary>
        public VisualElement CreateCultivationStatus(string plantType, float health, float growth)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;

            var plantLabel = new Label($"Plant: {plantType}");
            plantLabel.style.flexGrow = 1;

            var healthLabel = new Label($"Health: {(health * 100):F0}%");
            healthLabel.style.color = health > 0.7f ? Color.green : health > 0.3f ? Color.yellow : Color.red;

            var growthLabel = new Label($"Growth: {(growth * 100):F0}%");
            growthLabel.style.color = Color.blue;

            container.Add(plantLabel);
            container.Add(healthLabel);
            container.Add(growthLabel);

            return container;
        }

        /// <summary>
        /// Create a simple mode indicator
        /// </summary>
        public VisualElement CreateModeIndicator(string currentMode)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingTop = 10;
            container.style.paddingBottom = 10;

            var modeLabel = new Label($"Mode: {currentMode}");
            modeLabel.style.fontSize = 18;
            modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Color code modes
            switch (currentMode.ToLower())
            {
                case "construction":
                    modeLabel.style.color = Color.blue;
                    break;
                case "cultivation":
                    modeLabel.style.color = Color.green;
                    break;
                case "genetics":
                    modeLabel.style.color = Color.magenta;
                    break;
                default:
                    modeLabel.style.color = Color.white;
                    break;
            }

            container.Add(modeLabel);
            return container;
        }

        /// <summary>
        /// Create a simple resource display
        /// </summary>
        public VisualElement CreateResourceDisplay(float money, int skillPoints)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;

            var moneyLabel = new Label($"Money: ${money:F0}");
            moneyLabel.style.color = Color.yellow;

            var skillLabel = new Label($"Skill Points: {skillPoints}");
            skillLabel.style.color = Color.cyan;

            container.Add(moneyLabel);
            container.Add(skillLabel);

            return container;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create a styled button
        /// </summary>
        public Button CreateStyledButton(string text, Action clickAction)
        {
            var button = new Button(clickAction);
            button.text = text;
            button.style.paddingTop = 5;
            button.style.paddingBottom = 5;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.marginTop = 2;
            button.style.marginBottom = 2;
            return button;
        }

        /// <summary>
        /// Create a styled label
        /// </summary>
        public Label CreateStyledLabel(string text, Color color = default)
        {
            var label = new Label(text);
            if (color != default)
                label.style.color = color;
            label.style.paddingTop = 2;
            label.style.paddingBottom = 2;
            return label;
        }

        #endregion
    }
}
