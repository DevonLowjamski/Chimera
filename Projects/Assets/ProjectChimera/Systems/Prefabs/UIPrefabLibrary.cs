using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Specialized prefab library for UI components including panels, widgets,
    /// HUD elements, and complete UI interface templates using UI Toolkit.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Prefab Library", menuName = "Project Chimera/Prefabs/UI Library")]
    public class UIPrefabLibrarySO : ScriptableObject
    {
        [Header("UI Component Categories")]
        [SerializeField] private List<UIPrefabEntry> _uiPrefabs = new List<UIPrefabEntry>();
        [SerializeField] private List<UITemplateSet> _uiTemplates = new List<UITemplateSet>();
        
        [Header("UI Toolkit Assets")]
        [SerializeField] private List<VisualTreeAsset> _visualTreeAssets = new List<VisualTreeAsset>();
        [SerializeField] private List<StyleSheet> _styleSheets = new List<StyleSheet>();
        [SerializeField] private List<UITheme> _uiThemes = new List<UITheme>();
        
        [Header("Interface Configuration")]
        [SerializeField] private bool _enableResponsiveDesign = true;
        [SerializeField] private bool _supportMultipleResolutions = true;
        [SerializeField] private bool _enableAccessibility = true;
        [SerializeField] private Vector2Int _baseResolution = new Vector2Int(1920, 1080);
        
        // Cached lookup tables
        private Dictionary<string, UIPrefabEntry> _prefabLookup;
        private Dictionary<UIComponentType, List<UIPrefabEntry>> _typeLookup;
        private Dictionary<string, UITemplateSet> _templateLookup;
        
        public List<UIPrefabEntry> UIPrefabs => _uiPrefabs;
        
        public void InitializeDefaults()
        {
            if (_uiPrefabs.Count == 0)
            {
                CreateDefaultUIPrefabs();
            }
            
            if (_uiTemplates.Count == 0)
            {
                CreateDefaultUITemplates();
            }
            
            BuildLookupTables();
        }
        
        private void CreateDefaultUIPrefabs()
        {
            CreateHUDElements();
            CreatePanelComponents();
            CreateWidgetComponents();
            CreateMenuComponents();
            CreateDataVisualizationComponents();
        }
        
        private void CreateHUDElements()
        {
            // Environmental Status HUD
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "hud_environmental_status",
                PrefabName = "Environmental Status HUD",
                Prefab = null,
                UIComponentType = UIComponentType.HUD,
                ScreenPosition = UIScreenPosition.TopLeft,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "EnvironmentalHUD.uss" },
                RequiredScripts = new List<string> { "EnvironmentalHUDController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 300f,
                    Height = 200f,
                    Opacity = 0.9f,
                    CanBeDragged = true,
                    CanBeResized = false,
                    AutoHide = false,
                    UpdateFrequency = 1f
                },
                DataBindings = new List<string> { "Temperature", "Humidity", "CO2Level", "LightIntensity", "AirFlow" },
                InteractionElements = new List<string> { "SettingsButton", "MinimizeButton", "RefreshButton" }
            });
            
            // Plant Status HUD
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "hud_plant_status",
                PrefabName = "Plant Status HUD",
                Prefab = null,
                UIComponentType = UIComponentType.HUD,
                ScreenPosition = UIScreenPosition.TopRight,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "PlantHUD.uss" },
                RequiredScripts = new List<string> { "PlantHUDController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 280f,
                    Height = 180f,
                    Opacity = 0.9f,
                    CanBeDragged = true,
                    CanBeResized = false,
                    AutoHide = false,
                    UpdateFrequency = 0.5f
                },
                DataBindings = new List<string> { "PlantHealth", "GrowthStage", "WaterLevel", "NutrientLevel", "DaysToHarvest" },
                InteractionElements = new List<string> { "PlantSelector", "DetailsButton", "WateringButton" }
            });
            
            // System Performance HUD
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "hud_system_performance",
                PrefabName = "System Performance HUD",
                Prefab = null,
                UIComponentType = UIComponentType.HUD,
                ScreenPosition = UIScreenPosition.BottomLeft,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "SystemHUD.uss" },
                RequiredScripts = new List<string> { "SystemPerformanceHUDController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 250f,
                    Height = 120f,
                    Opacity = 0.8f,
                    CanBeDragged = true,
                    CanBeResized = false,
                    AutoHide = true,
                    UpdateFrequency = 2f
                },
                DataBindings = new List<string> { "PowerConsumption", "EnergyEfficiency", "SystemLoad", "Alerts" },
                InteractionElements = new List<string> { "SystemMenu", "AlertsPanel" }
            });
        }
        
        private void CreatePanelComponents()
        {
            // Facility Management Panel
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "panel_facility_management",
                PrefabName = "Facility Management Panel",
                Prefab = null,
                UIComponentType = UIComponentType.Panel,
                ScreenPosition = UIScreenPosition.Center,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "FacilityPanel.uss", "CommonPanels.uss" },
                RequiredScripts = new List<string> { "FacilityManagementController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 800f,
                    Height = 600f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    IsModal = true
                },
                DataBindings = new List<string> { "FacilityData", "RoomList", "EquipmentStatus", "ConstructionProjects" },
                InteractionElements = new List<string> { "RoomTabs", "EquipmentControls", "ConstructionQueue", "CloseButton" }
            });
            
            // Plant Genetics Panel
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "panel_plant_genetics",
                PrefabName = "Plant Genetics Panel",
                Prefab = null,
                UIComponentType = UIComponentType.Panel,
                ScreenPosition = UIScreenPosition.Center,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "GeneticsPanel.uss", "DataVisualization.uss" },
                RequiredScripts = new List<string> { "PlantGeneticsController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 900f,
                    Height = 700f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    IsModal = true
                },
                DataBindings = new List<string> { "StrainData", "GeneticTraits", "BreedingCombinations", "TraitPredictions" },
                InteractionElements = new List<string> { "StrainSelector", "TraitChart", "BreedingControls", "SaveButton" }
            });
            
            // Environmental Control Panel
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "panel_environmental_control",
                PrefabName = "Environmental Control Panel",
                Prefab = null,
                UIComponentType = UIComponentType.Panel,
                ScreenPosition = UIScreenPosition.Center,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "EnvironmentalPanel.uss", "ControlElements.uss" },
                RequiredScripts = new List<string> { "EnvironmentalControlController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 750f,
                    Height = 550f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    IsModal = false
                },
                DataBindings = new List<string> { "EnvironmentalTargets", "SensorReadings", "SystemStatus", "Schedules" },
                InteractionElements = new List<string> { "TemperatureSlider", "HumiditySlider", "CO2Controls", "LightingControls" }
            });
        }
        
        private void CreateWidgetComponents()
        {
            // Real-time Chart Widget
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "widget_realtime_chart",
                PrefabName = "Real-time Chart Widget",
                Prefab = null,
                UIComponentType = UIComponentType.Widget,
                ScreenPosition = UIScreenPosition.Flexible,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "ChartWidget.uss" },
                RequiredScripts = new List<string> { "ChartWidgetController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 400f,
                    Height = 250f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    UpdateFrequency = 1f
                },
                DataBindings = new List<string> { "ChartData", "TimeRange", "YAxisRange", "DataSeries" },
                InteractionElements = new List<string> { "ZoomControls", "TimeRangeSelector", "SeriesToggle" }
            });
            
            // Equipment Status Widget
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "widget_equipment_status",
                PrefabName = "Equipment Status Widget",
                Prefab = null,
                UIComponentType = UIComponentType.Widget,
                ScreenPosition = UIScreenPosition.Flexible,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "EquipmentWidget.uss" },
                RequiredScripts = new List<string> { "EquipmentStatusController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 300f,
                    Height = 200f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = false,
                    AutoHide = false,
                    UpdateFrequency = 5f
                },
                DataBindings = new List<string> { "EquipmentList", "StatusIndicators", "PowerConsumption", "Alerts" },
                InteractionElements = new List<string> { "EquipmentButtons", "StatusFilters", "PowerToggle" }
            });
            
            // Plant Growth Progress Widget
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "widget_plant_progress",
                PrefabName = "Plant Growth Progress Widget",
                Prefab = null,
                UIComponentType = UIComponentType.Widget,
                ScreenPosition = UIScreenPosition.Flexible,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "PlantProgressWidget.uss" },
                RequiredScripts = new List<string> { "PlantProgressController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 350f,
                    Height = 180f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = false,
                    AutoHide = false,
                    UpdateFrequency = 30f
                },
                DataBindings = new List<string> { "GrowthProgress", "GrowthStage", "TimeToNextStage", "PlantImage" },
                InteractionElements = new List<string> { "PlantSelector", "ProgressBar", "StageIndicator" }
            });
        }
        
        private void CreateMenuComponents()
        {
            // Main Menu
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "menu_main",
                PrefabName = "Main Menu",
                Prefab = null,
                UIComponentType = UIComponentType.Menu,
                ScreenPosition = UIScreenPosition.FullScreen,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "MainMenu.uss", "MenuTransitions.uss" },
                RequiredScripts = new List<string> { "MainMenuController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 1920f,
                    Height = 1080f,
                    Opacity = 1f,
                    CanBeDragged = false,
                    CanBeResized = false,
                    AutoHide = false,
                    IsModal = true
                },
                DataBindings = new List<string> { "GameVersion", "SaveFiles", "UserSettings" },
                InteractionElements = new List<string> { "NewGameButton", "LoadGameButton", "SettingsButton", "ExitButton" }
            });
            
            // Settings Menu
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "menu_settings",
                PrefabName = "Settings Menu",
                Prefab = null,
                UIComponentType = UIComponentType.Menu,
                ScreenPosition = UIScreenPosition.Center,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "SettingsMenu.uss" },
                RequiredScripts = new List<string> { "SettingsMenuController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 600f,
                    Height = 500f,
                    Opacity = 1f,
                    CanBeDragged = false,
                    CanBeResized = false,
                    AutoHide = false,
                    IsModal = true
                },
                DataBindings = new List<string> { "GraphicsSettings", "AudioSettings", "InputSettings", "GameplaySettings" },
                InteractionElements = new List<string> { "SettingsTabs", "ControlSliders", "DropdownMenus", "ApplyButton" }
            });
        }
        
        private void CreateDataVisualizationComponents()
        {
            // Heat Map Visualization
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "visualization_heatmap",
                PrefabName = "Heat Map Visualization",
                Prefab = null,
                UIComponentType = UIComponentType.Visualization,
                ScreenPosition = UIScreenPosition.Flexible,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "HeatMap.uss", "DataVisualization.uss" },
                RequiredScripts = new List<string> { "HeatMapController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 500f,
                    Height = 400f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    UpdateFrequency = 10f
                },
                DataBindings = new List<string> { "TemperatureData", "ColorGradient", "GridSize", "DataPoints" },
                InteractionElements = new List<string> { "ZoomControls", "ColorScale", "DataTypeSelector" }
            });
            
            // Growth Timeline
            _uiPrefabs.Add(new UIPrefabEntry
            {
                PrefabId = "visualization_growth_timeline",
                PrefabName = "Growth Timeline Visualization",
                Prefab = null,
                UIComponentType = UIComponentType.Visualization,
                ScreenPosition = UIScreenPosition.Flexible,
                ResponsiveLayout = true,
                RequiredStyleSheets = new List<string> { "Timeline.uss", "DataVisualization.uss" },
                RequiredScripts = new List<string> { "TimelineController" },
                UIProperties = new UIComponentProperties
                {
                    Width = 700f,
                    Height = 200f,
                    Opacity = 1f,
                    CanBeDragged = true,
                    CanBeResized = true,
                    AutoHide = false,
                    UpdateFrequency = 60f
                },
                DataBindings = new List<string> { "TimelineData", "Milestones", "CurrentTime", "PredictedEvents" },
                InteractionElements = new List<string> { "TimelineSlider", "MilestoneMarkers", "ZoomButtons" }
            });
        }
        
        private void CreateDefaultUITemplates()
        {
            // Dashboard Template
            _uiTemplates.Add(new UITemplateSet
            {
                TemplateId = "template_dashboard_main",
                TemplateName = "Main Dashboard Template",
                TemplateType = UITemplateType.Dashboard,
                LayoutConfiguration = new UILayoutConfiguration
                {
                    LayoutType = UILayoutType.Grid,
                    Columns = 3,
                    Rows = 2,
                    GridSpacing = 10f,
                    ResponsiveBreakpoints = new List<ResponsiveBreakpoint>
                    {
                        new ResponsiveBreakpoint { ScreenWidth = 1920, Columns = 3 },
                        new ResponsiveBreakpoint { ScreenWidth = 1366, Columns = 2 },
                        new ResponsiveBreakpoint { ScreenWidth = 1024, Columns = 1 }
                    }
                },
                ComponentPlacements = new List<UIComponentPlacement>
                {
                    new UIComponentPlacement { ComponentId = "hud_environmental_status", GridPosition = new Vector2Int(0, 0) },
                    new UIComponentPlacement { ComponentId = "hud_plant_status", GridPosition = new Vector2Int(1, 0) },
                    new UIComponentPlacement { ComponentId = "widget_realtime_chart", GridPosition = new Vector2Int(2, 0) },
                    new UIComponentPlacement { ComponentId = "widget_equipment_status", GridPosition = new Vector2Int(0, 1) },
                    new UIComponentPlacement { ComponentId = "widget_plant_progress", GridPosition = new Vector2Int(1, 1) },
                    new UIComponentPlacement { ComponentId = "visualization_heatmap", GridPosition = new Vector2Int(2, 1) }
                },
                RequiredAssets = new List<string> { "Dashboard.uxml", "Dashboard.uss" }
            });
            
            // Mobile Template
            _uiTemplates.Add(new UITemplateSet
            {
                TemplateId = "template_mobile_interface",
                TemplateName = "Mobile Interface Template",
                TemplateType = UITemplateType.Mobile,
                LayoutConfiguration = new UILayoutConfiguration
                {
                    LayoutType = UILayoutType.Stack,
                    Columns = 1,
                    Rows = 0, // Dynamic
                    GridSpacing = 5f,
                    ResponsiveBreakpoints = new List<ResponsiveBreakpoint>
                    {
                        new ResponsiveBreakpoint { ScreenWidth = 768, Columns = 1 }
                    }
                },
                ComponentPlacements = new List<UIComponentPlacement>
                {
                    new UIComponentPlacement { ComponentId = "hud_environmental_status", Priority = 1 },
                    new UIComponentPlacement { ComponentId = "hud_plant_status", Priority = 2 },
                    new UIComponentPlacement { ComponentId = "widget_equipment_status", Priority = 3 }
                },
                RequiredAssets = new List<string> { "Mobile.uxml", "Mobile.uss" }
            });
        }
        
        private void BuildLookupTables()
        {
            _prefabLookup = _uiPrefabs.ToDictionary(u => u.PrefabId, u => u);
            
            _typeLookup = _uiPrefabs.GroupBy(u => u.UIComponentType)
                                   .ToDictionary(g => g.Key, g => g.ToList());
            
            _templateLookup = _uiTemplates.ToDictionary(t => t.TemplateId, t => t);
        }
        
        public UIPrefabEntry GetUIPrefab(UIComponentType componentType, string prefabId = null)
        {
            if (!string.IsNullOrEmpty(prefabId) && _prefabLookup.TryGetValue(prefabId, out var specificPrefab))
            {
                return specificPrefab;
            }
            
            if (_typeLookup.TryGetValue(componentType, out var typePrefabs) && typePrefabs.Count > 0)
            {
                return typePrefabs[0];
            }
            
            return null;
        }
        
        public List<UIPrefabEntry> GetUIPrefabsByType(UIComponentType componentType)
        {
            return _typeLookup.TryGetValue(componentType, out var prefabs) ? prefabs : new List<UIPrefabEntry>();
        }
        
        public UITemplateSet GetUITemplate(string templateId)
        {
            return _templateLookup.TryGetValue(templateId, out var template) ? template : null;
        }
        
        public List<UITemplateSet> GetTemplatesByType(UITemplateType templateType)
        {
            return _uiTemplates.Where(t => t.TemplateType == templateType).ToList();
        }
        
        public UILayoutValidationResult ValidateUILayout(UITemplateSet template)
        {
            var result = new UILayoutValidationResult
            {
                IsValid = true,
                ValidationIssues = new List<string>(),
                PerformanceWarnings = new List<string>()
            };
            
            // Check component overlaps
            ValidateComponentOverlaps(template, result);
            
            // Check responsive design
            if (_enableResponsiveDesign)
            {
                ValidateResponsiveDesign(template, result);
            }
            
            // Check accessibility requirements
            if (_enableAccessibility)
            {
                ValidateAccessibility(template, result);
            }
            
            // Check performance implications
            ValidatePerformance(template, result);
            
            return result;
        }
        
        private void ValidateComponentOverlaps(UITemplateSet template, UILayoutValidationResult result)
        {
            for (int i = 0; i < template.ComponentPlacements.Count; i++)
            {
                for (int j = i + 1; j < template.ComponentPlacements.Count; j++)
                {
                    var placement1 = template.ComponentPlacements[i];
                    var placement2 = template.ComponentPlacements[j];
                    
                    if (placement1.GridPosition == placement2.GridPosition)
                    {
                        result.ValidationIssues.Add($"Components {placement1.ComponentId} and {placement2.ComponentId} overlap at position {placement1.GridPosition}");
                        result.IsValid = false;
                    }
                }
            }
        }
        
        private void ValidateResponsiveDesign(UITemplateSet template, UILayoutValidationResult result)
        {
            if (template.LayoutConfiguration.ResponsiveBreakpoints.Count == 0)
            {
                result.ValidationIssues.Add("No responsive breakpoints defined for template");
            }
            
            // Check if components fit within smallest breakpoint
            var smallestBreakpoint = template.LayoutConfiguration.ResponsiveBreakpoints.OrderBy(b => b.ScreenWidth).FirstOrDefault();
            if (smallestBreakpoint != null && smallestBreakpoint.Columns < template.LayoutConfiguration.Columns)
            {
                result.PerformanceWarnings.Add("Some components may not be visible on smaller screens");
            }
        }
        
        private void ValidateAccessibility(UITemplateSet template, UILayoutValidationResult result)
        {
            // Check if components have accessible navigation
            foreach (var placement in template.ComponentPlacements)
            {
                var prefab = _prefabLookup.TryGetValue(placement.ComponentId, out var p) ? p : null;
                if (prefab != null && prefab.InteractionElements.Count > 0)
                {
                    // Components with interactions should have keyboard navigation
                    if (!prefab.RequiredScripts.Any(s => s.Contains("Accessibility")))
                    {
                        result.PerformanceWarnings.Add($"Component {placement.ComponentId} may not support accessibility features");
                    }
                }
            }
        }
        
        private void ValidatePerformance(UITemplateSet template, UILayoutValidationResult result)
        {
            int totalComponents = template.ComponentPlacements.Count;
            
            if (totalComponents > 10)
            {
                result.PerformanceWarnings.Add("Large number of UI components may impact performance");
            }
            
            // Check update frequencies
            foreach (var placement in template.ComponentPlacements)
            {
                var prefab = _prefabLookup.TryGetValue(placement.ComponentId, out var p) ? p : null;
                if (prefab?.UIProperties.UpdateFrequency < 1f)
                {
                    result.PerformanceWarnings.Add($"Component {placement.ComponentId} has high update frequency");
                }
            }
        }
        
        public UIAdaptationResult AdaptUIForResolution(UITemplateSet template, Vector2Int targetResolution)
        {
            var result = new UIAdaptationResult
            {
                AdaptedTemplate = template,
                RequiredChanges = new List<string>(),
                ScalingFactor = 1f
            };
            
            // Calculate scaling factor
            result.ScalingFactor = (float)targetResolution.x / _baseResolution.x;
            
            // Find appropriate responsive breakpoint
            var breakpoint = template.LayoutConfiguration.ResponsiveBreakpoints
                .Where(b => b.ScreenWidth <= targetResolution.x)
                .OrderByDescending(b => b.ScreenWidth)
                .FirstOrDefault();
            
            if (breakpoint != null)
            {
                result.AdaptedTemplate.LayoutConfiguration.Columns = breakpoint.Columns;
                result.RequiredChanges.Add($"Adjusted layout to {breakpoint.Columns} columns for {targetResolution.x}px width");
            }
            
            // Scale component sizes
            foreach (var placement in result.AdaptedTemplate.ComponentPlacements)
            {
                var prefab = _prefabLookup.TryGetValue(placement.ComponentId, out var p) ? p : null;
                if (prefab != null)
                {
                    prefab.UIProperties.Width *= result.ScalingFactor;
                    prefab.UIProperties.Height *= result.ScalingFactor;
                }
            }
            
            return result;
        }
        
        public UILibraryStats GetLibraryStats()
        {
            return new UILibraryStats
            {
                TotalUIPrefabs = _uiPrefabs.Count,
                TotalTemplates = _uiTemplates.Count,
                TotalVisualTreeAssets = _visualTreeAssets.Count,
                TotalStyleSheets = _styleSheets.Count,
                TypeDistribution = _typeLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
                ResponsiveDesignEnabled = _enableResponsiveDesign,
                AccessibilitySupported = _enableAccessibility
            };
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                BuildLookupTables();
            }
        }
    }
    
    // Supporting data structures
    [System.Serializable]
    public class UIPrefabEntry
    {
        public string PrefabId;
        public string PrefabName;
        public GameObject Prefab;
        public UIComponentType UIComponentType;
        public UIScreenPosition ScreenPosition;
        public bool ResponsiveLayout;
        public List<string> RequiredStyleSheets = new List<string>();
        public List<string> RequiredScripts = new List<string>();
        public UIComponentProperties UIProperties;
        public List<string> DataBindings = new List<string>();
        public List<string> InteractionElements = new List<string>();
    }
    
    [System.Serializable]
    public class UIComponentProperties
    {
        public float Width = 100f;
        public float Height = 100f;
        public float Opacity = 1f;
        public bool CanBeDragged = false;
        public bool CanBeResized = false;
        public bool AutoHide = false;
        public bool IsModal = false;
        public float UpdateFrequency = 1f; // seconds
        public UIAnimationType AnimationType = UIAnimationType.None;
        public float AnimationDuration = 0.3f;
    }
    
    [System.Serializable]
    public class UITemplateSet
    {
        public string TemplateId;
        public string TemplateName;
        public UITemplateType TemplateType;
        public UILayoutConfiguration LayoutConfiguration;
        public List<UIComponentPlacement> ComponentPlacements = new List<UIComponentPlacement>();
        public List<string> RequiredAssets = new List<string>();
    }
    
    [System.Serializable]
    public class UILayoutConfiguration
    {
        public UILayoutType LayoutType;
        public int Columns = 1;
        public int Rows = 1;
        public float GridSpacing = 10f;
        public List<ResponsiveBreakpoint> ResponsiveBreakpoints = new List<ResponsiveBreakpoint>();
        public bool EnableAutoScaling = true;
        public Vector2 MinComponentSize = new Vector2(100f, 50f);
    }
    
    [System.Serializable]
    public class UIComponentPlacement
    {
        public string ComponentId;
        public Vector2Int GridPosition = Vector2Int.zero;
        public int Priority = 0;
        public Vector2 CustomPosition = Vector2.zero;
        public Vector2 CustomSize = Vector2.zero;
        public bool UseCustomPositioning = false;
    }
    
    [System.Serializable]
    public class ResponsiveBreakpoint
    {
        public int ScreenWidth;
        public int Columns;
        public float ScalingFactor = 1f;
        public bool HideComponents = false;
        public List<string> HiddenComponentIds = new List<string>();
    }
    
    [System.Serializable]
    public class UITheme
    {
        public string ThemeId;
        public string ThemeName;
        public Color PrimaryColor = Color.blue;
        public Color SecondaryColor = Color.gray;
        public Color AccentColor = Color.green;
        public Color BackgroundColor = Color.black;
        public Color TextColor = Color.white;
        public List<StyleSheet> ThemeStyleSheets = new List<StyleSheet>();
    }
    
    [System.Serializable]
    public class UILayoutValidationResult
    {
        public bool IsValid;
        public List<string> ValidationIssues = new List<string>();
        public List<string> PerformanceWarnings = new List<string>();
        public float PerformanceScore;
    }
    
    [System.Serializable]
    public class UIAdaptationResult
    {
        public UITemplateSet AdaptedTemplate;
        public List<string> RequiredChanges = new List<string>();
        public float ScalingFactor;
        public bool RequiresAssetReload;
    }
    
    [System.Serializable]
    public class UILibraryStats
    {
        public int TotalUIPrefabs;
        public int TotalTemplates;
        public int TotalVisualTreeAssets;
        public int TotalStyleSheets;
        public Dictionary<UIComponentType, int> TypeDistribution;
        public bool ResponsiveDesignEnabled;
        public bool AccessibilitySupported;
    }
    
    // Enums
    public enum UIComponentType
    {
        HUD,
        Panel,
        Widget,
        Menu,
        Popup,
        Tooltip,
        Notification,
        Visualization,
        Control,
        Navigation
    }
    
    public enum UIScreenPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Flexible,
        FullScreen
    }
    
    public enum UITemplateType
    {
        Dashboard,
        Mobile,
        Desktop,
        Tablet,
        VR,
        Console,
        Minimal
    }
    
    public enum UILayoutType
    {
        Grid,
        Stack,
        Flow,
        Absolute,
        Flex
    }
    
    public enum UIAnimationType
    {
        None,
        Fade,
        Slide,
        Scale,
        Rotate,
        Bounce
    }
}