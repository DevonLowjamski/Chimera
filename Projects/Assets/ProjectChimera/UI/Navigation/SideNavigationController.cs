using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Events;

namespace ProjectChimera.UI.Navigation
{
    /// <summary>
    /// Modern UI Toolkit-based side navigation component for Project Chimera.
    /// Provides collapsible side navigation with mode-aware sections and dynamic content.
    /// Integrates with the contextual menu system and supports responsive design.
    /// </summary>
    public class SideNavigationController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _startCollapsed = false;
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private float _collapsedWidth = 60f;
        [SerializeField] private float _expandedWidth = 280f;
        
        [Header("Event Channels")]
        [SerializeField] private ProjectChimera.Core.Events.SimpleGameEventSO _onNavigationItemClicked;
        
        // UI Elements
        private VisualElement _rootElement;
        private VisualElement _navigationContainer;
        private VisualElement _headerSection;
        private VisualElement _mainNavigationSection;
        private VisualElement _contextualSection;
        private VisualElement _footerSection;
        private Button _toggleButton;
        private Label _currentModeLabel;
        private Label _currentLevelLabel;
        
        // State
        private bool _isCollapsed = false;
        private bool _isInitialized = false;
        private ProjectChimera.Data.Events.GameplayMode _currentMode = ProjectChimera.Data.Events.GameplayMode.Cultivation;
        private CameraLevel _currentLevel = CameraLevel.Facility;
        
        // Navigation items
        private Dictionary<ProjectChimera.Data.Events.GameplayMode, List<NavigationItem>> _navigationItems;
        private List<VisualElement> _navigationElements = new List<VisualElement>();
        
        public bool IsCollapsed => _isCollapsed;
        public ProjectChimera.Data.Events.GameplayMode CurrentMode => _currentMode;
        public CameraLevel CurrentLevel => _currentLevel;
        
        private void Start()
        {
            InitializeSideNavigation();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeSideNavigation()
        {
            try
            {
                // Create the UI structure
                CreateNavigationUI();
                
                // Setup navigation data
                SetupNavigationItems();
                
                // Subscribe to events
                SubscribeToEvents();
                
                // Set initial state
                _isCollapsed = _startCollapsed;
                SetCollapsedState(_isCollapsed, false); // No animation on startup
                
                // Initialize with current mode and level
                UpdateNavigationContent();
                
                _isInitialized = true;
                
                if (_enableDebugLogging)
                {
                    Debug.Log("[SideNavigationController] Initialized successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SideNavigationController] Error during initialization: {ex.Message}");
            }
        }
        
        private void CreateNavigationUI()
        {
            // Get or create the root UI document
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }
            
            // Create root container
            _rootElement = new VisualElement();
            _rootElement.name = "side-navigation-root";
            _rootElement.style.position = Position.Absolute;
            _rootElement.style.left = 0;
            _rootElement.style.top = 0;
            _rootElement.style.bottom = 0;
            _rootElement.style.width = _expandedWidth;
            _rootElement.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            _rootElement.style.borderRightWidth = 1;
            _rootElement.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Create navigation container
            _navigationContainer = new VisualElement();
            _navigationContainer.name = "navigation-container";
            _navigationContainer.style.flexGrow = 1;
            _navigationContainer.style.paddingTop = 8;
            _navigationContainer.style.paddingBottom = 8;
            _navigationContainer.style.paddingLeft = 8;
            _navigationContainer.style.paddingRight = 8;
            _rootElement.Add(_navigationContainer);
            
            // Create header section
            CreateHeaderSection();
            
            // Create main navigation section
            CreateMainNavigationSection();
            
            // Create contextual section
            CreateContextualSection();
            
            // Create footer section
            CreateFooterSection();
            
            // Add to UI document
            if (uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.Add(_rootElement);
            }
        }
        
        private void CreateHeaderSection()
        {
            _headerSection = new VisualElement();
            _headerSection.name = "header-section";
            _headerSection.style.marginBottom = 12;
            _navigationContainer.Add(_headerSection);
            
            // Toggle button
            _toggleButton = new Button(OnToggleClicked);
            _toggleButton.name = "toggle-button";
            _toggleButton.text = "☰";
            _toggleButton.style.height = 32;
            _toggleButton.style.marginBottom = 8;
            _toggleButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            _toggleButton.style.borderTopWidth = 1;
            _toggleButton.style.borderRightWidth = 1;
            _toggleButton.style.borderBottomWidth = 1;
            _toggleButton.style.borderLeftWidth = 1;
            _toggleButton.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            _toggleButton.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            _toggleButton.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            _toggleButton.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            _toggleButton.style.borderTopLeftRadius = 4;
            _toggleButton.style.borderTopRightRadius = 4;
            _toggleButton.style.borderBottomLeftRadius = 4;
            _toggleButton.style.borderBottomRightRadius = 4;
            _headerSection.Add(_toggleButton);
            
            // Current mode label
            _currentModeLabel = new Label("Cultivation");
            _currentModeLabel.name = "current-mode-label";
            _currentModeLabel.style.fontSize = 14;
            _currentModeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _currentModeLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _currentModeLabel.style.marginBottom = 4;
            _headerSection.Add(_currentModeLabel);
            
            // Current level label
            _currentLevelLabel = new Label("Facility Level");
            _currentLevelLabel.name = "current-level-label";
            _currentLevelLabel.style.fontSize = 11;
            _currentLevelLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            _headerSection.Add(_currentLevelLabel);
        }
        
        private void CreateMainNavigationSection()
        {
            _mainNavigationSection = new VisualElement();
            _mainNavigationSection.name = "main-navigation-section";
            _mainNavigationSection.style.marginBottom = 16;
            _navigationContainer.Add(_mainNavigationSection);
            
            // Section title
            var sectionTitle = new Label("Main Navigation");
            sectionTitle.style.fontSize = 12;
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            sectionTitle.style.marginBottom = 8;
            _mainNavigationSection.Add(sectionTitle);
        }
        
        private void CreateContextualSection()
        {
            _contextualSection = new VisualElement();
            _contextualSection.name = "contextual-section";
            _contextualSection.style.marginBottom = 16;
            _navigationContainer.Add(_contextualSection);
            
            // Section title
            var sectionTitle = new Label("Quick Actions");
            sectionTitle.style.fontSize = 12;
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            sectionTitle.style.marginBottom = 8;
            _contextualSection.Add(sectionTitle);
        }
        
        private void CreateFooterSection()
        {
            _footerSection = new VisualElement();
            _footerSection.name = "footer-section";
            _footerSection.style.marginTop = StyleKeyword.Auto; // Push to bottom
            _navigationContainer.Add(_footerSection);
            
            // Settings button
            var settingsButton = new Button(() => OnNavigationItemClicked("Settings"));
            settingsButton.text = "⚙ Settings";
            settingsButton.style.height = 28;
            settingsButton.style.marginBottom = 4;
            settingsButton.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            settingsButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            ApplyButtonStyling(settingsButton);
            _footerSection.Add(settingsButton);
            
            // Help button
            var helpButton = new Button(() => OnNavigationItemClicked("Help"));
            helpButton.text = "? Help";
            helpButton.style.height = 28;
            helpButton.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            helpButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            ApplyButtonStyling(helpButton);
            _footerSection.Add(helpButton);
        }
        
        private void SetupNavigationItems()
        {
            _navigationItems = new Dictionary<ProjectChimera.Data.Events.GameplayMode, List<NavigationItem>>
            {
                {
                    ProjectChimera.Data.Events.GameplayMode.Cultivation,
                    new List<NavigationItem>
                    {
                        new NavigationItem { Name = "🌱 Plants Overview", Action = "PlantsOverview", Icon = "🌱" },
                        new NavigationItem { Name = "📅 Care Schedule", Action = "CareSchedule", Icon = "📅" },
                        new NavigationItem { Name = "🌾 Harvest Planner", Action = "HarvestPlanner", Icon = "🌾" },
                        new NavigationItem { Name = "🌡 Environment", Action = "Environment", Icon = "🌡" },
                        new NavigationItem { Name = "💧 Nutrients", Action = "Nutrients", Icon = "💧" }
                    }
                },
                {
                    ProjectChimera.Data.Events.GameplayMode.Construction,
                    new List<NavigationItem>
                    {
                        new NavigationItem { Name = "📐 Blueprints", Action = "Blueprints", Icon = "📐" },
                        new NavigationItem { Name = "🏗 Projects", Action = "Projects", Icon = "🏗" },
                        new NavigationItem { Name = "🔧 Equipment", Action = "Equipment", Icon = "🔧" },
                        new NavigationItem { Name = "⚡ Utilities", Action = "Utilities", Icon = "⚡" },
                        new NavigationItem { Name = "💰 Budget", Action = "Budget", Icon = "💰" }
                    }
                },
                {
                    ProjectChimera.Data.Events.GameplayMode.Genetics,
                    new List<NavigationItem>
                    {
                        new NavigationItem { Name = "🧬 Breeding Lab", Action = "BreedingLab", Icon = "🧬" },
                        new NavigationItem { Name = "🔬 Analysis", Action = "Analysis", Icon = "🔬" },
                        new NavigationItem { Name = "📚 Trait Library", Action = "TraitLibrary", Icon = "📚" },
                        new NavigationItem { Name = "🌿 Seed Bank", Action = "SeedBank", Icon = "🌿" },
                        new NavigationItem { Name = "⚗️ Tissue Culture", Action = "TissueCulture", Icon = "⚗️" }
                    }
                }
            };
        }
        
        private void SubscribeToEvents()
        {
            // TODO: Subscribe to mode and camera level change events when available
            // For now, navigation will be controlled manually via public methods
        }
        
        private void UnsubscribeFromEvents()
        {
            // TODO: Unsubscribe from events when available
        }
        
        private void UpdateNavigationContent()
        {
            // Clear existing navigation elements
            ClearNavigationElements();
            
            // Update labels
            if (_currentModeLabel != null)
            {
                _currentModeLabel.text = GetModeDisplayName(_currentMode);
            }
            
            if (_currentLevelLabel != null)
            {
                _currentLevelLabel.text = GetLevelDisplayName(_currentLevel);
            }
            
            // Build main navigation
            BuildMainNavigation();
            
            // Build contextual actions
            BuildContextualActions();
        }
        
        private void ClearNavigationElements()
        {
            foreach (var element in _navigationElements)
            {
                element?.RemoveFromHierarchy();
            }
            _navigationElements.Clear();
        }
        
        private void BuildMainNavigation()
        {
            if (!_navigationItems.TryGetValue(_currentMode, out var items)) return;
            
            foreach (var item in items)
            {
                var button = CreateNavigationButton(item);
                _mainNavigationSection.Add(button);
                _navigationElements.Add(button);
            }
        }
        
        private void BuildContextualActions()
        {
            // Add level-specific quick actions
            var quickActions = GetQuickActionsForLevel(_currentLevel);
            
            foreach (var action in quickActions)
            {
                var button = CreateQuickActionButton(action);
                _contextualSection.Add(button);
                _navigationElements.Add(button);
            }
        }
        
        private Button CreateNavigationButton(NavigationItem item)
        {
            var button = new Button(() => OnNavigationItemClicked(item.Action));
            button.text = _isCollapsed ? item.Icon : item.Name;
            button.style.height = 32;
            button.style.marginBottom = 2;
            button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            ApplyButtonStyling(button);
            
            // Add hover effects
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            });
            
            return button;
        }
        
        private Button CreateQuickActionButton(QuickAction action)
        {
            var button = new Button(() => OnNavigationItemClicked(action.Action));
            button.text = _isCollapsed ? action.Icon : $"{action.Icon} {action.Name}";
            button.style.height = 28;
            button.style.marginBottom = 2;
            button.style.backgroundColor = new Color(0.15f, 0.25f, 0.35f, 1f);
            button.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            ApplyButtonStyling(button);
            
            return button;
        }
        
        private void ApplyButtonStyling(Button button)
        {
            button.style.borderTopWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            button.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            button.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            button.style.borderTopLeftRadius = 3;
            button.style.borderTopRightRadius = 3;
            button.style.borderBottomLeftRadius = 3;
            button.style.borderBottomRightRadius = 3;
            button.style.fontSize = 11;
        }
        
        private List<QuickAction> GetQuickActionsForLevel(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => new List<QuickAction>
                {
                    new QuickAction { Name = "System Status", Action = "SystemStatus", Icon = "📊" },
                    new QuickAction { Name = "Alerts", Action = "Alerts", Icon = "⚠️" }
                },
                CameraLevel.Room => new List<QuickAction>
                {
                    new QuickAction { Name = "Room Controls", Action = "RoomControls", Icon = "🎛" },
                    new QuickAction { Name = "Environment", Action = "Environment", Icon = "🌡" }
                },
                CameraLevel.Bench => new List<QuickAction>
                {
                    new QuickAction { Name = "Bench Status", Action = "BenchStatus", Icon = "📋" },
                    new QuickAction { Name = "Quick Care", Action = "QuickCare", Icon = "🛠" }
                },
                CameraLevel.Plant => new List<QuickAction>
                {
                    new QuickAction { Name = "Plant Health", Action = "PlantHealth", Icon = "❤️" },
                    new QuickAction { Name = "Actions", Action = "PlantActions", Icon = "⚡" }
                },
                _ => new List<QuickAction>()
            };
        }
        
        private void SetCollapsedState(bool collapsed, bool animate = true)
        {
            _isCollapsed = collapsed;
            
            var targetWidth = collapsed ? _collapsedWidth : _expandedWidth;
            _rootElement.style.width = targetWidth;
            
            // Update button texts
            foreach (var element in _navigationElements)
            {
                if (element is Button button)
                {
                    // Update button text based on collapsed state
                    // This would need to be enhanced to track original NavigationItem data
                }
            }
            
            // Update toggle button
            _toggleButton.text = collapsed ? "▶" : "◀";
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[SideNavigationController] Navigation {(collapsed ? "collapsed" : "expanded")}");
            }
        }
        
        private string GetModeDisplayName(ProjectChimera.Data.Events.GameplayMode mode)
        {
            return mode switch
            {
                ProjectChimera.Data.Events.GameplayMode.Cultivation => "Cultivation",
                ProjectChimera.Data.Events.GameplayMode.Construction => "Construction", 
                ProjectChimera.Data.Events.GameplayMode.Genetics => "Genetics",
                _ => mode.ToString()
            };
        }
        
        private string GetLevelDisplayName(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => "Facility Overview",
                CameraLevel.Room => "Room View",
                CameraLevel.Bench => "Bench Level",
                CameraLevel.Plant => "Plant Detail",
                _ => level.ToString()
            };
        }
        
        #region Event Handlers
        
        private void OnToggleClicked()
        {
            SetCollapsedState(!_isCollapsed);
        }
        
        /// <summary>
        /// Manually set the current mode - used until event system is available
        /// </summary>
        public void SetCurrentMode(ProjectChimera.Data.Events.GameplayMode mode)
        {
            _currentMode = mode;
            UpdateNavigationContent();
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[SideNavigationController] Mode changed to {_currentMode}");
            }
        }
        
        /// <summary>
        /// Manually set the current camera level - used until event system is available
        /// </summary>
        public void SetCurrentLevel(CameraLevel level)
        {
            _currentLevel = level;
            UpdateNavigationContent();
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[SideNavigationController] Camera level changed to {_currentLevel}");
            }
        }
        
        private void OnNavigationItemClicked(string action)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[SideNavigationController] Navigation item clicked: {action}");
            }
            
            // Raise event for other systems to respond to
            _onNavigationItemClicked?.Raise();
            
            // Here you would implement the actual navigation logic
            // For now, it just logs the action
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Toggle the collapsed state of the navigation
        /// </summary>
        public void ToggleCollapsed()
        {
            SetCollapsedState(!_isCollapsed);
        }
        
        /// <summary>
        /// Set the collapsed state explicitly
        /// </summary>
        public void SetCollapsed(bool collapsed)
        {
            SetCollapsedState(collapsed);
        }
        
        /// <summary>
        /// Refresh the navigation content
        /// </summary>
        public void RefreshNavigation()
        {
            if (_isInitialized)
            {
                UpdateNavigationContent();
            }
        }
        
        #endregion
        
        // Data structures
        [System.Serializable]
        public class NavigationItem
        {
            public string Name;
            public string Action;
            public string Icon;
        }
        
        [System.Serializable]
        public class QuickAction
        {
            public string Name;
            public string Action;
            public string Icon;
        }
    }
    
    // Supporting types
    public enum CameraLevel
    {
        Facility = 0,
        Room = 1,
        Bench = 2,
        Plant = 3
    }
}