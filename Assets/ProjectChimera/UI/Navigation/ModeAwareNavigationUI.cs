using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Navigation
{
    /// <summary>
    /// Mode-aware navigation UI system - adapts navigation based on current gameplay mode
    /// Shows relevant navigation options and highlights active mode
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class ModeAwareNavigationUI : MonoBehaviour
    {
        [Header("Navigation Configuration")]
        [SerializeField] private bool _enableModeHighlighting = true;
        [SerializeField] private bool _showModeSpecificOptions = true;
        [SerializeField] private bool _animateTransitions = true;
        [SerializeField] private bool _debugMode = false;

        [Header("Main Navigation")]
        [SerializeField] private GameObject _mainNavigationPanel;
        [SerializeField] private Button _cultivationNavButton;
        [SerializeField] private Button _constructionNavButton;
        [SerializeField] private Button _geneticsNavButton;
        [SerializeField] private Text _currentModeLabel;

        [Header("Mode-Specific Navigation")]
        [SerializeField] private GameObject _cultivationNavPanel;
        [SerializeField] private GameObject _constructionNavPanel;
        [SerializeField] private GameObject _geneticsNavPanel;

        [Header("Cultivation Navigation")]
        [SerializeField] private Button _plantsOverviewButton;
        [SerializeField] private Button _careScheduleButton;
        [SerializeField] private Button _harvestPlannerButton;
        [SerializeField] private Button _environmentControlButton;
        [SerializeField] private Button _nutrientManagementButton;

        [Header("Construction Navigation")]
        [SerializeField] private Button _blueprintsButton;
        [SerializeField] private Button _facilityDesignButton;
        [SerializeField] private Button _utilitiesButton;
        [SerializeField] private Button _equipmentButton;
        [SerializeField] private Button _constructionProjectsButton;

        [Header("Genetics Navigation")]
        [SerializeField] private Button _breedingLabButton;
        [SerializeField] private Button _geneticAnalysisButton;
        [SerializeField] private Button _traitLibraryButton;
        [SerializeField] private Button _crossbreedingButton;
        [SerializeField] private Button _phenotypeTrackerButton;

        [Header("Visual Styling")]
        [SerializeField] private Color _activeModeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _inactiveModeColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private float _transitionDuration = 0.3f;

        [Header("Navigation Breadcrumb")]
        [SerializeField] private GameObject _breadcrumbPanel;
        [SerializeField] private Text _breadcrumbText;
        [SerializeField] private Button _backButton;

        [Header("Quick Actions")]
        [SerializeField] private GameObject _quickActionsPanel;
        [SerializeField] private Button _quickAction1Button;
        [SerializeField] private Button _quickAction2Button;
        [SerializeField] private Button _quickAction3Button;
        [SerializeField] private Text _quickAction1Label;
        [SerializeField] private Text _quickAction2Label;
        [SerializeField] private Text _quickAction3Label;

        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;

        // Services
        private IGameplayModeController _modeController;

        // State tracking
        private bool _isInitialized = false;
        private GameplayMode _currentMode = GameplayMode.Cultivation;
        private string _currentSection = "";
        private List<string> _navigationHistory = new List<string>();

        // Navigation button references for easy management
        private Dictionary<GameplayMode, Button> _modeButtons;
        private Dictionary<GameplayMode, GameObject> _modeNavPanels;
        private Dictionary<GameplayMode, List<NavigationItem>> _modeNavigationItems;

        [System.Serializable]
        public class NavigationItem
        {
            public string name;
            public Button button;
            public string targetSection;
            public bool requiresSpecialAccess;
        }

        private void Start()
        {
            InitializeNavigation();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeNavigation()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();

                if (_modeController == null)
                {
                    ChimeraLogger.LogError("[ModeAwareNavigationUI] GameplayModeController service not found!");
                    return;
                }

                // Initialize navigation dictionaries
                SetupNavigationDictionaries();

                // Subscribe to mode change events
                SubscribeToEvents();

                // Subscribe to navigation button events
                SubscribeToNavigationControls();

                // Initialize navigation state
                _currentMode = _modeController.CurrentMode;
                UpdateNavigationForMode(_currentMode);

                // Set initial UI state
                InitializeUIState();

                _isInitialized = true;

                if (_debugMode)
                {
                    ChimeraLogger.Log($"[ModeAwareNavigationUI] Initialized with mode: {_currentMode}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[ModeAwareNavigationUI] Error during initialization: {ex.Message}");
            }
        }

        private void SetupNavigationDictionaries()
        {
            // Main mode buttons
            _modeButtons = new Dictionary<GameplayMode, Button>
            {
                { GameplayMode.Cultivation, _cultivationNavButton },
                { GameplayMode.Construction, _constructionNavButton },
                { GameplayMode.Genetics, _geneticsNavButton }
            };

            // Mode-specific navigation panels
            _modeNavPanels = new Dictionary<GameplayMode, GameObject>
            {
                { GameplayMode.Cultivation, _cultivationNavPanel },
                { GameplayMode.Construction, _constructionNavPanel },
                { GameplayMode.Genetics, _geneticsNavPanel }
            };

            // Mode-specific navigation items
            _modeNavigationItems = new Dictionary<GameplayMode, List<NavigationItem>>
            {
                { GameplayMode.Cultivation, SetupCultivationNavigation() },
                { GameplayMode.Construction, SetupConstructionNavigation() },
                { GameplayMode.Genetics, SetupGeneticsNavigation() }
            };
        }

        private List<NavigationItem> SetupCultivationNavigation()
        {
            return new List<NavigationItem>
            {
                new NavigationItem { name = "Plants Overview", button = _plantsOverviewButton, targetSection = "PlantsOverview" },
                new NavigationItem { name = "Care Schedule", button = _careScheduleButton, targetSection = "CareSchedule" },
                new NavigationItem { name = "Harvest Planner", button = _harvestPlannerButton, targetSection = "HarvestPlanner" },
                new NavigationItem { name = "Environment Control", button = _environmentControlButton, targetSection = "EnvironmentControl" },
                new NavigationItem { name = "Nutrient Management", button = _nutrientManagementButton, targetSection = "NutrientManagement" }
            };
        }

        private List<NavigationItem> SetupConstructionNavigation()
        {
            return new List<NavigationItem>
            {
                new NavigationItem { name = "Blueprints", button = _blueprintsButton, targetSection = "Blueprints" },
                new NavigationItem { name = "Facility Design", button = _facilityDesignButton, targetSection = "FacilityDesign" },
                new NavigationItem { name = "Utilities", button = _utilitiesButton, targetSection = "Utilities" },
                new NavigationItem { name = "Equipment", button = _equipmentButton, targetSection = "Equipment" },
                new NavigationItem { name = "Construction Projects", button = _constructionProjectsButton, targetSection = "ConstructionProjects" }
            };
        }

        private List<NavigationItem> SetupGeneticsNavigation()
        {
            return new List<NavigationItem>
            {
                new NavigationItem { name = "Breeding Lab", button = _breedingLabButton, targetSection = "BreedingLab" },
                new NavigationItem { name = "Genetic Analysis", button = _geneticAnalysisButton, targetSection = "GeneticAnalysis" },
                new NavigationItem { name = "Trait Library", button = _traitLibraryButton, targetSection = "TraitLibrary" },
                new NavigationItem { name = "Crossbreeding", button = _crossbreedingButton, targetSection = "Crossbreeding" },
                new NavigationItem { name = "Phenotype Tracker", button = _phenotypeTrackerButton, targetSection = "PhenotypeTracker" }
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
                ChimeraLogger.LogWarning("[ModeAwareNavigationUI] ModeChangedEvent not assigned");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
        }

        private void SubscribeToNavigationControls()
        {
            // Main mode navigation buttons
            if (_cultivationNavButton != null)
            {
                _cultivationNavButton.onClick.AddListener(() => OnModeNavigationClicked(GameplayMode.Cultivation));
            }

            if (_constructionNavButton != null)
            {
                _constructionNavButton.onClick.AddListener(() => OnModeNavigationClicked(GameplayMode.Construction));
            }

            if (_geneticsNavButton != null)
            {
                _geneticsNavButton.onClick.AddListener(() => OnModeNavigationClicked(GameplayMode.Genetics));
            }

            // Mode-specific navigation buttons
            foreach (var modeNavItems in _modeNavigationItems)
            {
                foreach (var navItem in modeNavItems.Value)
                {
                    if (navItem.button != null)
                    {
                        string targetSection = navItem.targetSection;
                        navItem.button.onClick.AddListener(() => OnSectionNavigationClicked(targetSection));
                    }
                }
            }

            // Breadcrumb navigation
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackButtonClicked);
            }

            // Quick actions
            if (_quickAction1Button != null)
            {
                _quickAction1Button.onClick.AddListener(() => OnQuickActionClicked(1));
            }

            if (_quickAction2Button != null)
            {
                _quickAction2Button.onClick.AddListener(() => OnQuickActionClicked(2));
            }

            if (_quickAction3Button != null)
            {
                _quickAction3Button.onClick.AddListener(() => OnQuickActionClicked(3));
            }
        }

        private void InitializeUIState()
        {
            // Set initial button states
            UpdateModeButtonVisuals(_currentMode);

            // Update current mode label
            if (_currentModeLabel != null)
            {
                _currentModeLabel.text = GetModeDisplayName(_currentMode);
            }

            // Initialize breadcrumb
            UpdateBreadcrumb();

            // Setup quick actions
            UpdateQuickActions(_currentMode);
        }

        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareNavigationUI] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }

            _currentMode = eventData.NewMode;
            UpdateNavigationForMode(_currentMode);
        }

        private void UpdateNavigationForMode(GameplayMode mode)
        {
            // Update mode button visuals
            UpdateModeButtonVisuals(mode);

            // Show/hide mode-specific navigation panels
            UpdateModeNavigationPanels(mode);

            // Update current mode label
            if (_currentModeLabel != null)
            {
                _currentModeLabel.text = GetModeDisplayName(mode);
            }

            // Update breadcrumb
            ClearNavigationHistory();
            UpdateBreadcrumb();

            // Update quick actions
            UpdateQuickActions(mode);

            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareNavigationUI] Navigation updated for mode: {mode}");
            }
        }

        private void UpdateModeButtonVisuals(GameplayMode activeMode)
        {
            if (!_enableModeHighlighting) return;

            foreach (var kvp in _modeButtons)
            {
                var mode = kvp.Key;
                var button = kvp.Value;

                if (button == null) continue;

                var isActive = mode == activeMode;
                var targetColor = isActive ? _activeModeColor : _inactiveModeColor;

                // Update button colors
                var colors = button.colors;
                colors.normalColor = targetColor;
                colors.highlightedColor = isActive ? _activeModeColor : _hoverColor;
                colors.selectedColor = targetColor;
                button.colors = colors;

                // Update button interactability (optional: disable current mode button)
                // button.interactable = !isActive;
            }
        }

        private void UpdateModeNavigationPanels(GameplayMode activeMode)
        {
            if (!_showModeSpecificOptions) return;

            foreach (var kvp in _modeNavPanels)
            {
                var mode = kvp.Key;
                var panel = kvp.Value;

                if (panel == null) continue;

                bool shouldShow = mode == activeMode;

                if (_animateTransitions)
                {
                    // Animate panel transitions (placeholder - would use actual animation system)
                    panel.SetActive(shouldShow);
                }
                else
                {
                    panel.SetActive(shouldShow);
                }
            }
        }

        private void UpdateBreadcrumb()
        {
            if (_breadcrumbPanel == null) return;

            string breadcrumbText = GetModeDisplayName(_currentMode);

            if (!string.IsNullOrEmpty(_currentSection))
            {
                breadcrumbText += $" > {_currentSection}";
            }

            if (_breadcrumbText != null)
            {
                _breadcrumbText.text = breadcrumbText;
            }

            // Show/hide back button based on navigation history
            if (_backButton != null)
            {
                _backButton.gameObject.SetActive(_navigationHistory.Count > 0);
            }
        }

        private void UpdateQuickActions(GameplayMode mode)
        {
            if (_quickActionsPanel == null) return;

            // Configure quick actions based on current mode
            switch (mode)
            {
                case GameplayMode.Cultivation:
                    SetQuickAction(1, "Water Plants", _quickAction1Button, _quickAction1Label);
                    SetQuickAction(2, "Check Health", _quickAction2Button, _quickAction2Label);
                    SetQuickAction(3, "Harvest Ready", _quickAction3Button, _quickAction3Label);
                    break;

                case GameplayMode.Construction:
                    SetQuickAction(1, "Place Wall", _quickAction1Button, _quickAction1Label);
                    SetQuickAction(2, "Add Equipment", _quickAction2Button, _quickAction2Label);
                    SetQuickAction(3, "View Blueprint", _quickAction3Button, _quickAction3Label);
                    break;

                case GameplayMode.Genetics:
                    SetQuickAction(1, "Analyze Genetics", _quickAction1Button, _quickAction1Label);
                    SetQuickAction(2, "Start Breeding", _quickAction2Button, _quickAction2Label);
                    SetQuickAction(3, "View Traits", _quickAction3Button, _quickAction3Label);
                    break;
            }
        }

        private void SetQuickAction(int actionNumber, string actionName, Button button, Text label)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
            }

            if (label != null)
            {
                label.text = actionName;
            }
        }

        private void ClearNavigationHistory()
        {
            _navigationHistory.Clear();
            _currentSection = "";
        }

        private void AddToNavigationHistory(string section)
        {
            if (!string.IsNullOrEmpty(_currentSection))
            {
                _navigationHistory.Add(_currentSection);
            }
            _currentSection = section;
        }

        private string GetModeDisplayName(GameplayMode mode)
        {
            return mode switch
            {
                GameplayMode.Cultivation => "Cultivation",
                GameplayMode.Construction => "Construction",
                GameplayMode.Genetics => "Genetics",
                _ => mode.ToString()
            };
        }

        #region Event Handlers

        private void OnModeNavigationClicked(GameplayMode mode)
        {
            if (_modeController != null)
            {
                _modeController.SetMode(mode, "Navigation UI");
            }

            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareNavigationUI] Mode navigation clicked: {mode}");
            }
        }

        private void OnSectionNavigationClicked(string targetSection)
        {
            AddToNavigationHistory(targetSection);
            UpdateBreadcrumb();

            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareNavigationUI] Section navigation clicked: {targetSection}");
            }

            // Here you would trigger the actual section change in your game
            // For now, it's just tracked for breadcrumb navigation
        }

        private void OnBackButtonClicked()
        {
            if (_navigationHistory.Count > 0)
            {
                var previousSection = _navigationHistory[_navigationHistory.Count - 1];
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
                _currentSection = previousSection;

                UpdateBreadcrumb();

                if (_debugMode)
                {
                    ChimeraLogger.Log($"[ModeAwareNavigationUI] Back button clicked, returned to: {previousSection}");
                }
            }
            else
            {
                // Return to mode overview
                _currentSection = "";
                UpdateBreadcrumb();

                if (_debugMode)
                {
                    ChimeraLogger.Log("[ModeAwareNavigationUI] Back button clicked, returned to mode overview");
                }
            }
        }

        private void OnQuickActionClicked(int actionNumber)
        {
            string actionName = "";

            switch (_currentMode)
            {
                case GameplayMode.Cultivation:
                    actionName = actionNumber switch
                    {
                        1 => "Water Plants",
                        2 => "Check Health",
                        3 => "Harvest Ready",
                        _ => "Unknown"
                    };
                    break;

                case GameplayMode.Construction:
                    actionName = actionNumber switch
                    {
                        1 => "Place Wall",
                        2 => "Add Equipment",
                        3 => "View Blueprint",
                        _ => "Unknown"
                    };
                    break;

                case GameplayMode.Genetics:
                    actionName = actionNumber switch
                    {
                        1 => "Analyze Genetics",
                        2 => "Start Breeding",
                        3 => "View Traits",
                        _ => "Unknown"
                    };
                    break;
            }

            if (_debugMode)
            {
                ChimeraLogger.Log("SYSTEM", $"[ModeAwareNavigationUI] Quick action clicked: {actionName} (Action {actionNumber})");
            }

            // Here you would trigger the actual quick action in your game
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manually refresh the navigation UI
        /// </summary>
        public void RefreshNavigation()
        {
            if (_isInitialized && _modeController != null)
            {
                UpdateNavigationForMode(_modeController.CurrentMode);

                if (_debugMode)
                {
                    ChimeraLogger.Log("[ModeAwareNavigationUI] Navigation refreshed manually");
                }
            }
        }

        /// <summary>
        /// Navigate to a specific section programmatically
        /// </summary>
        public void NavigateToSection(string sectionName)
        {
            AddToNavigationHistory(sectionName);
            UpdateBreadcrumb();

            if (_debugMode)
            {
                ChimeraLogger.Log($"[ModeAwareNavigationUI] Programmatic navigation to: {sectionName}");
            }
        }

        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            ChimeraLogger.Log($"[ModeAwareNavigationUI] Debug mode {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Get current navigation state
        /// </summary>
        public GameplayMode CurrentMode => _currentMode;
        public string CurrentSection => _currentSection;
        public int NavigationHistoryDepth => _navigationHistory.Count;

        #endregion

        #if UNITY_EDITOR

        /// <summary>
        /// Editor-only method for testing navigation
        /// </summary>
        [ContextMenu("Test Navigation Mode Cycle")]
        private void TestNavigationModeCycle()
        {
            if (Application.isPlaying && _modeController != null)
            {
                // Manually cycle through modes since CycleMode() isn't in interface
                var currentMode = _modeController.CurrentMode;
                var nextMode = currentMode switch
                {
                    GameplayMode.Cultivation => GameplayMode.Construction, GameplayMode.Construction => GameplayMode.Genetics,
                    GameplayMode.Genetics => GameplayMode.Cultivation,
                    _ => GameplayMode.Cultivation
                };
                _modeController.SetMode(nextMode, "NavigationUI Test");
            }
            else
            {
                ChimeraLogger.Log("[ModeAwareNavigationUI] Test only works during play mode with initialized controller");
            }
        }

        #endif
    }
}
