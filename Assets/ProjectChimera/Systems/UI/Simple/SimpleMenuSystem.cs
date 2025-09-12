using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Systems.UI.Simple
{
    /// <summary>
    /// Simple Menu System - Aligned with Project Chimera's vision
    /// Provides the contextual menu system as described in gameplay document
    /// Focuses on mode-specific tabs and simple navigation without complex features
    /// </summary>
    public class SimpleMenuSystem : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _menuTemplate;

        [Header("Menu Settings")]
        [SerializeField] private float _menuWidth = 800f;
        [SerializeField] private float _menuHeight = 120f;
        [SerializeField] private float _animationDuration = 0.3f;

        [Header("Color Settings")]
        [SerializeField] private Color _constructionModeColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color _cultivationModeColor = new Color(0.9f, 1f, 0.8f);
        [SerializeField] private Color _geneticsModeColor = new Color(1f, 0.9f, 0.8f);

        // Menu state
        private VisualElement _menuContainer;
        private VisualElement _tabContainer;
        private Label _modeIndicator;
        private Dictionary<string, VisualElement> _tabs = new Dictionary<string, VisualElement>();
        private string _currentMode = "cultivation";
        private string _currentTab = "";

        // Menu content definitions
        private readonly Dictionary<string, MenuDefinition> _menuDefinitions = new Dictionary<string, MenuDefinition>();

        private void Awake()
        {
            InitializeMenuDefinitions();
            InitializeUI();
        }

        /// <summary>
        /// Initializes the menu definitions for each mode
        /// </summary>
        private void InitializeMenuDefinitions()
        {
            // Construction mode menu
            _menuDefinitions["construction"] = new MenuDefinition
            {
                ModeName = "Construction",
                ModeColor = _constructionModeColor,
                Tabs = new Dictionary<string, TabDefinition>
                {
                    ["rooms"] = new TabDefinition
                    {
                        TabName = "Rooms",
                        Description = "Structural components like walls and roofs",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Wall", Description = "Build facility walls", Icon = "wall_icon", Cost = 10 },
                            new MenuItem { Name = "Door", Description = "Add entry/exit points", Icon = "door_icon", Cost = 25 },
                            new MenuItem { Name = "Roof", Description = "Complete facility structure", Icon = "roof_icon", Cost = 50 }
                        }
                    },
                    ["equipment"] = new TabDefinition
                    {
                        TabName = "Equipment",
                        Description = "Lights, HVAC, irrigation systems",
                        SubTabs = new Dictionary<string, List<MenuItem>>
                        {
                            ["lights"] = new List<MenuItem>
                            {
                                new MenuItem { Name = "LED Grow Light", Description = "Energy-efficient lighting", Icon = "light_icon", Cost = 100 },
                                new MenuItem { Name = "HPS Light", Description = "High-intensity discharge", Icon = "hps_icon", Cost = 150 }
                            },
                            ["hvac"] = new List<MenuItem>
                            {
                                new MenuItem { Name = "Air Conditioner", Description = "Temperature control", Icon = "ac_icon", Cost = 200 },
                                new MenuItem { Name = "Ventilation Fan", Description = "Air circulation", Icon = "fan_icon", Cost = 75 }
                            },
                            ["irrigation"] = new List<MenuItem>
                            {
                                new MenuItem { Name = "Drip System", Description = "Precise water delivery", Icon = "drip_icon", Cost = 120 },
                                new MenuItem { Name = "Sprinkler", Description = "Area coverage irrigation", Icon = "sprinkler_icon", Cost = 90 }
                            }
                        }
                    },
                    ["utilities"] = new TabDefinition
                    {
                        TabName = "Utilities",
                        Description = "Electrical, plumbing systems",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Power Outlet", Description = "Electrical connection", Icon = "outlet_icon", Cost = 15 },
                            new MenuItem { Name = "Water Pipe", Description = "Plumbing connection", Icon = "pipe_icon", Cost = 20 }
                        }
                    },
                    ["schematics"] = new TabDefinition
                    {
                        TabName = "Schematics",
                        Description = "Saved facility layouts",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Load Schematic", Description = "Apply saved layout", Icon = "load_icon", Cost = 0 },
                            new MenuItem { Name = "Save Schematic", Description = "Save current layout", Icon = "save_icon", Cost = 0 }
                        }
                    }
                }
            };

            // Cultivation mode menu
            _menuDefinitions["cultivation"] = new MenuDefinition
            {
                ModeName = "Cultivation",
                ModeColor = _cultivationModeColor,
                Tabs = new Dictionary<string, TabDefinition>
                {
                    ["tools"] = new TabDefinition
                    {
                        TabName = "Tools",
                        Description = "Watering, pruning tools",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Watering Can", Description = "Water plants", Icon = "water_icon", Cost = 5 },
                            new MenuItem { Name = "Pruning Shears", Description = "Trim plant growth", Icon = "prune_icon", Cost = 10 },
                            new MenuItem { Name = "pH Meter", Description = "Measure nutrient pH", Icon = "ph_icon", Cost = 25 }
                        }
                    },
                    ["environmental"] = new TabDefinition
                    {
                        TabName = "Environmental",
                        Description = "Temperature, humidity controls",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Temp Control", Description = "Adjust temperature", Icon = "temp_icon", Cost = 0 },
                            new MenuItem { Name = "Humidity Control", Description = "Adjust humidity", Icon = "humidity_icon", Cost = 0 },
                            new MenuItem { Name = "Light Control", Description = "Adjust lighting", Icon = "light_control_icon", Cost = 0 }
                        }
                    },
                    ["plantcare"] = new TabDefinition
                    {
                        TabName = "Plant Care",
                        Description = "Fertilizer, pest control",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Fertilizer", Description = "Apply nutrients", Icon = "fertilizer_icon", Cost = 15 },
                            new MenuItem { Name = "Pest Control", Description = "Treat pests", Icon = "pest_icon", Cost = 20 },
                            new MenuItem { Name = "Growth Monitor", Description = "Check plant health", Icon = "monitor_icon", Cost = 0 }
                        }
                    }
                }
            };

            // Genetics mode menu
            _menuDefinitions["genetics"] = new MenuDefinition
            {
                ModeName = "Genetics",
                ModeColor = _geneticsModeColor,
                Tabs = new Dictionary<string, TabDefinition>
                {
                    ["seedbank"] = new TabDefinition
                    {
                        TabName = "Seed Bank",
                        Description = "Genetic strains collection",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Browse Strains", Description = "View available genetics", Icon = "browse_icon", Cost = 0 },
                            new MenuItem { Name = "Plant Seed", Description = "Plant genetic material", Icon = "plant_icon", Cost = 50 }
                        }
                    },
                    ["tissueculture"] = new TabDefinition
                    {
                        TabName = "Tissue Culture",
                        Description = "Preserve genetics",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Create Culture", Description = "Save genetic material", Icon = "culture_icon", Cost = 100 },
                            new MenuItem { Name = "Clone Plant", Description = "Create genetic copy", Icon = "clone_icon", Cost = 75 }
                        }
                    },
                    ["micropropagation"] = new TabDefinition
                    {
                        TabName = "Micropropagation",
                        Description = "Rapid genetic multiplication",
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Name = "Start Propagation", Description = "Begin multiplication", Icon = "propagate_icon", Cost = 200 },
                            new MenuItem { Name = "Harvest Clones", Description = "Collect new plants", Icon = "harvest_icon", Cost = 0 }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Initializes the UI components
        /// </summary>
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ChimeraLogger.LogWarning("[SimpleMenuSystem] No UI document assigned");
                return;
            }

            var root = _uiDocument.rootVisualElement;

            // Create menu container
            _menuContainer = new VisualElement();
            _menuContainer.name = "contextual-menu";
            _menuContainer.AddToClassList("contextual-menu");
            _menuContainer.style.width = _menuWidth;
            _menuContainer.style.height = _menuHeight;
            _menuContainer.style.position = Position.Absolute;
            _menuContainer.style.bottom = 20;
            _menuContainer.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _menuContainer.style.marginLeft = new StyleLength(new Length(-_menuWidth / 2, LengthUnit.Pixel));

            // Create mode indicator
            _modeIndicator = new Label();
            _modeIndicator.name = "mode-indicator";
            _modeIndicator.AddToClassList("mode-indicator");

            // Create tab container
            _tabContainer = new VisualElement();
            _tabContainer.name = "tab-container";
            _tabContainer.AddToClassList("tab-container");

            _menuContainer.Add(_modeIndicator);
            _menuContainer.Add(_tabContainer);
            root.Add(_menuContainer);

            // Initially hide menu
            _menuContainer.style.display = DisplayStyle.None;

            ChimeraLogger.Log("[SimpleMenuSystem] UI initialized");
        }

        /// <summary>
        /// Switches the active menu mode
        /// </summary>
        public void SwitchMode(string newMode)
        {
            if (!_menuDefinitions.ContainsKey(newMode))
            {
                ChimeraLogger.LogWarning($"[SimpleMenuSystem] Unknown mode: {newMode}");
                return;
            }

            _currentMode = newMode;
            UpdateMenuDisplay();

            ChimeraLogger.Log($"[SimpleMenuSystem] Switched to {newMode} mode");
        }

        /// <summary>
        /// Shows the menu
        /// </summary>
        public void ShowMenu()
        {
            if (_menuContainer != null)
            {
                _menuContainer.style.display = DisplayStyle.Flex;
                UpdateMenuDisplay();
            }
        }

        /// <summary>
        /// Hides the menu
        /// </summary>
        public void HideMenu()
        {
            if (_menuContainer != null)
            {
                _menuContainer.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Updates the menu display for the current mode
        /// </summary>
        private void UpdateMenuDisplay()
        {
            if (!_menuDefinitions.TryGetValue(_currentMode, out var menuDef))
                return;

            // Update mode indicator
            if (_modeIndicator != null)
            {
                _modeIndicator.text = menuDef.ModeName.ToUpper();
                _modeIndicator.style.color = new StyleColor(menuDef.ModeColor);
                _menuContainer.style.backgroundColor = new StyleColor(menuDef.ModeColor * 0.1f);
            }

            // Clear existing tabs
            _tabContainer.Clear();
            _tabs.Clear();

            // Create tabs for current mode
            foreach (var tabPair in menuDef.Tabs)
            {
                var tab = CreateTab(tabPair.Key, tabPair.Value);
                _tabContainer.Add(tab);
                _tabs[tabPair.Key] = tab;
            }

            // Select first tab by default
            if (menuDef.Tabs.Count > 0)
            {
                SelectTab(menuDef.Tabs.Keys.First());
            }
        }

        /// <summary>
        /// Creates a tab element
        /// </summary>
        private VisualElement CreateTab(string tabId, TabDefinition tabDef)
        {
            var tab = new Button();
            tab.text = tabDef.TabName;
            tab.tooltip = tabDef.Description;
            tab.AddToClassList("menu-tab");
            tab.clicked += () => SelectTab(tabId);

            return tab;
        }

        /// <summary>
        /// Selects a tab and shows its content
        /// </summary>
        private void SelectTab(string tabId)
        {
            if (!_menuDefinitions.TryGetValue(_currentMode, out var menuDef))
                return;

            if (!menuDef.Tabs.TryGetValue(tabId, out var tabDef))
                return;

            _currentTab = tabId;

            // Update tab selection visual
            foreach (var tab in _tabs)
            {
                if (tab.Key == tabId)
                {
                    tab.Value.AddToClassList("selected");
                }
                else
                {
                    tab.Value.RemoveFromClassList("selected");
                }
            }

            // Show tab content (for now, just log - would integrate with actual systems)
            ChimeraLogger.Log($"[SimpleMenuSystem] Selected tab: {tabDef.TabName}");

            // Here you would show the actual menu items for the selected tab
            // This would integrate with the game systems for construction, cultivation, etc.
        }

        /// <summary>
        /// Gets the current menu mode
        /// </summary>
        public string GetCurrentMode()
        {
            return _currentMode;
        }

        /// <summary>
        /// Gets the current selected tab
        /// </summary>
        public string GetCurrentTab()
        {
            return _currentTab;
        }

        /// <summary>
        /// Checks if the menu is currently visible
        /// </summary>
        public bool IsMenuVisible()
        {
            return _menuContainer != null &&
                   _menuContainer.style.display == DisplayStyle.Flex;
        }

        /// <summary>
        /// Handles menu item selection (would integrate with game systems)
        /// </summary>
        public void OnMenuItemSelected(string itemName)
        {
            ChimeraLogger.Log($"[SimpleMenuSystem] Menu item selected: {itemName}");

            // Here you would integrate with the actual game systems:
            // - Construction system for placing equipment
            // - Cultivation system for plant care actions
            // - Genetics system for breeding operations
        }
    }

    // Menu data structures

    [Serializable]
    public class MenuDefinition
    {
        public string ModeName;
        public Color ModeColor;
        public Dictionary<string, TabDefinition> Tabs = new Dictionary<string, TabDefinition>();
    }

    [Serializable]
    public class TabDefinition
    {
        public string TabName;
        public string Description;
        public List<MenuItem> Items = new List<MenuItem>();
        public Dictionary<string, List<MenuItem>> SubTabs = new Dictionary<string, List<MenuItem>>();
    }

    [Serializable]
    public class MenuItem
    {
        public string Name;
        public string Description;
        public string Icon;
        public int Cost;
        public bool IsAvailable = true;
    }
}
