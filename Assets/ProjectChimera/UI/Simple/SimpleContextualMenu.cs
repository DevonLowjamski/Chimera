using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Simple
{
    /// <summary>
    /// Simple contextual menu aligned with Project Chimera vision
    /// Basic menu system for cultivation actions
    /// </summary>
    public class SimpleContextualMenu : MonoBehaviour
    {
        [Header("Menu Configuration")]
        [SerializeField] private UIDocument _menuDocument;
        [SerializeField] private string _constructionModeColor = "#4A90E2";
        [SerializeField] private string _cultivationModeColor = "#7ED321";
        [SerializeField] private string _geneticsModeColor = "#D0021B";

        // Menu state
        private VisualElement _rootElement;
        private VisualElement _menuContainer;
        private Label _modeIndicator;
        private ScrollView _actionList;
        private bool _isMenuVisible;

        // Menu actions
        private Dictionary<string, Action> _menuActions = new Dictionary<string, Action>();
        private string _currentMode = "Construction";

        // Events
        public event Action<string> OnModeChanged;
        public event Action<string> OnActionSelected;

        private void Start()
        {
            InitializeMenu();
            SetupDefaultActions();
        }

        private void InitializeMenu()
        {
            if (_menuDocument == null)
            {
                ChimeraLogger.LogError("[SimpleContextualMenu] No UIDocument assigned!");
                return;
            }

            _rootElement = _menuDocument.rootVisualElement;

            // Create menu structure
            _menuContainer = new VisualElement();
            _menuContainer.name = "contextual-menu";
            _menuContainer.style.position = Position.Absolute;
            _menuContainer.style.right = 20;
            _menuContainer.style.bottom = 20;
            _menuContainer.style.width = 300;
            _menuContainer.style.height = 400;
            _menuContainer.style.backgroundColor = new Color(0, 0, 0, 0.9f);
            _menuContainer.style.borderRadius = 10;
            _menuContainer.style.paddingTop = 10;
            _menuContainer.style.paddingBottom = 10;
            _menuContainer.style.paddingLeft = 15;
            _menuContainer.style.paddingRight = 15;

            // Mode indicator
            _modeIndicator = new Label($"Mode: {_currentMode}");
            _modeIndicator.style.fontSize = 18;
            _modeIndicator.style.unityFontStyleAndWeight = FontStyle.Bold;
            _modeIndicator.style.marginBottom = 10;
            UpdateModeColor();

            // Action list
            _actionList = new ScrollView();
            _actionList.style.flexGrow = 1;

            // Mode buttons
            var modeButtonContainer = new VisualElement();
            modeButtonContainer.style.flexDirection = FlexDirection.Row;
            modeButtonContainer.style.justifyContent = Justify.SpaceBetween;
            modeButtonContainer.style.marginTop = 10;

            var constructionButton = CreateModeButton("Construction", () => SetMode("Construction"));
            var cultivationButton = CreateModeButton("Cultivation", () => SetMode("Cultivation"));
            var geneticsButton = CreateModeButton("Genetics", () => SetMode("Genetics"));

            modeButtonContainer.Add(constructionButton);
            modeButtonContainer.Add(cultivationButton);
            modeButtonContainer.Add(geneticsButton);

            _menuContainer.Add(_modeIndicator);
            _menuContainer.Add(_actionList);
            _menuContainer.Add(modeButtonContainer);
            _rootElement.Add(_menuContainer);

            _isMenuVisible = true;
            ChimeraLogger.LogVerbose("[SimpleContextualMenu] Menu initialized");
        }

        private Button CreateModeButton(string modeName, Action clickAction)
        {
            var button = new Button(clickAction);
            button.text = modeName;
            button.style.flexGrow = 1;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        #region Mode Management

        /// <summary>
        /// Set the current mode and update menu actions
        /// </summary>
        public void SetMode(string mode)
        {
            _currentMode = mode;
            _modeIndicator.text = $"Mode: {mode}";
            UpdateModeColor();

            // Clear current actions
            _actionList.Clear();
            _menuActions.Clear();

            // Add mode-specific actions
            switch (mode.ToLower())
            {
                case "construction":
                    SetupConstructionActions();
                    break;
                case "cultivation":
                    SetupCultivationActions();
                    break;
                case "genetics":
                    SetupGeneticsActions();
                    break;
            }

            OnModeChanged?.Invoke(mode);
            ChimeraLogger.LogVerbose($"[SimpleContextualMenu] Mode changed to: {mode}");
        }

        private void UpdateModeColor()
        {
            switch (_currentMode.ToLower())
            {
                case "construction":
                    _modeIndicator.style.color = HexToColor(_constructionModeColor);
                    break;
                case "cultivation":
                    _modeIndicator.style.color = HexToColor(_cultivationModeColor);
                    break;
                case "genetics":
                    _modeIndicator.style.color = HexToColor(_geneticsModeColor);
                    break;
                default:
                    _modeIndicator.style.color = Color.white;
                    break;
            }
        }

        private Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }

        #endregion

        #region Action Management

        private void SetupDefaultActions()
        {
            // Add some basic actions that work in all modes
            AddMenuAction("Save Game", () => ExecuteAction("Save Game"));
            AddMenuAction("Load Game", () => ExecuteAction("Load Game"));
            AddMenuAction("Settings", () => ExecuteAction("Settings"));
        }

        private void SetupConstructionActions()
        {
            AddMenuAction("Place Wall", () => ExecuteAction("Place Wall"));
            AddMenuAction("Place Light", () => ExecuteAction("Place Light"));
            AddMenuAction("Place Pot", () => ExecuteAction("Place Pot"));
            AddMenuAction("Install HVAC", () => ExecuteAction("Install HVAC"));
            AddMenuAction("Add Irrigation", () => ExecuteAction("Add Irrigation"));
            AddMenuAction("Create Schematic", () => ExecuteAction("Create Schematic"));
        }

        private void SetupCultivationActions()
        {
            AddMenuAction("Water Plants", () => ExecuteAction("Water Plants"));
            AddMenuAction("Add Nutrients", () => ExecuteAction("Add Nutrients"));
            AddMenuAction("Prune Plants", () => ExecuteAction("Prune Plants"));
            AddMenuAction("Check Health", () => ExecuteAction("Check Health"));
            AddMenuAction("Harvest Ready Plants", () => ExecuteAction("Harvest Ready Plants"));
            AddMenuAction("Adjust Environment", () => ExecuteAction("Adjust Environment"));
        }

        private void SetupGeneticsActions()
        {
            AddMenuAction("View Seed Bank", () => ExecuteAction("View Seed Bank"));
            AddMenuAction("Create Tissue Culture", () => ExecuteAction("Create Tissue Culture"));
            AddMenuAction("Start Micropropagation", () => ExecuteAction("Start Micropropagation"));
            AddMenuAction("Cross Plants", () => ExecuteAction("Cross Plants"));
            AddMenuAction("Select Phenotype", () => ExecuteAction("Select Phenotype"));
            AddMenuAction("Analyze Genetics", () => ExecuteAction("Analyze Genetics"));
        }

        /// <summary>
        /// Add an action to the current menu
        /// </summary>
        public void AddMenuAction(string actionName, Action action)
        {
            _menuActions[actionName] = action;

            var button = new Button(() => ExecuteAction(actionName));
            button.text = actionName;
            button.style.width = Length.Percent(100);
            button.style.marginBottom = 5;

            _actionList.Add(button);
        }

        /// <summary>
        /// Execute a menu action
        /// </summary>
        private void ExecuteAction(string actionName)
        {
            if (_menuActions.ContainsKey(actionName))
            {
                _menuActions[actionName]?.Invoke();
                OnActionSelected?.Invoke(actionName);
                ChimeraLogger.LogVerbose($"[SimpleContextualMenu] Executed action: {actionName}");
            }
        }

        #endregion

        #region Menu Visibility

        /// <summary>
        /// Toggle menu visibility
        /// </summary>
        public void ToggleMenu()
        {
            _isMenuVisible = !_isMenuVisible;
            _menuContainer.style.display = _isMenuVisible ? DisplayStyle.Flex : DisplayStyle.None;
            ChimeraLogger.LogVerbose($"[SimpleContextualMenu] Menu visibility: {_isMenuVisible}");
        }

        /// <summary>
        /// Show the menu
        /// </summary>
        public void ShowMenu()
        {
            _isMenuVisible = true;
            _menuContainer.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hide the menu
        /// </summary>
        public void HideMenu()
        {
            _isMenuVisible = false;
            _menuContainer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Check if menu is visible
        /// </summary>
        public bool IsMenuVisible => _isMenuVisible;

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get current mode
        /// </summary>
        public string GetCurrentMode()
        {
            return _currentMode;
        }

        /// <summary>
        /// Clear all menu actions
        /// </summary>
        public void ClearActions()
        {
            _actionList.Clear();
            _menuActions.Clear();
            ChimeraLogger.LogVerbose("[SimpleContextualMenu] All actions cleared");
        }

        #endregion
    }
}
