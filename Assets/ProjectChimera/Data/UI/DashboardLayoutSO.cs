using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// ScriptableObject defining dashboard layout, widgets, and configuration.
    /// Provides flexible dashboard customization for different user needs.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dashboard Layout", menuName = "Project Chimera/UI/Dashboard Layout")]
    public class DashboardLayoutSO : ChimeraDataSO
    {
        [Header("Layout Identity")]
        [SerializeField] private string _layoutName;
        [SerializeField] private string _description;
        [SerializeField] private DashboardType _dashboardType = DashboardType.Facility;
        [SerializeField] private bool _isDefault = false;
        
        [Header("Grid Configuration")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(12, 8);
        [SerializeField] private float _gridCellSize = 100f;
        [SerializeField] private float _gridSpacing = 8f;
        [SerializeField] private bool _enableGridSnap = true;
        [SerializeField] private bool _allowOverlap = false;
        
        [Header("Widget Configuration")]
        [SerializeField] private List<DashboardWidget> _widgets = new List<DashboardWidget>();
        [SerializeField] private int _maxWidgets = 20;
        [SerializeField] private bool _enableWidgetResize = true;
        [SerializeField] private bool _enableWidgetDrag = true;
        [SerializeField] private bool _enableWidgetMenu = true;
        
        [Header("Quick Actions")]
        [SerializeField] private List<QuickAction> _quickActions = new List<QuickAction>();
        [SerializeField] private QuickActionPlacement _quickActionPlacement = QuickActionPlacement.Header;
        [SerializeField] private int _maxQuickActions = 8;
        
        [Header("Status Indicators")]
        [SerializeField] private List<StatusIndicator> _statusIndicators = new List<StatusIndicator>();
        [SerializeField] private bool _enableStatusBar = true;
        [SerializeField] private StatusBarPosition _statusBarPosition = StatusBarPosition.Bottom;
        
        [Header("Alerts Configuration")]
        [SerializeField] private bool _enableAlerts = true;
        [SerializeField] private AlertPlacement _alertPlacement = AlertPlacement.TopRight;
        [SerializeField] private int _maxVisibleAlerts = 5;
        [SerializeField] private float _alertDuration = 10f;
        [SerializeField] private bool _enableAlertGrouping = true;
        
        [Header("Navigation")]
        [SerializeField] private bool _enableQuickNavigation = true;
        [SerializeField] private List<NavigationItem> _navigationItems = new List<NavigationItem>();
        [SerializeField] private NavigationStyle _navigationStyle = NavigationStyle.Sidebar;
        
        [Header("Customization")]
        [SerializeField] private bool _allowUserCustomization = true;
        [SerializeField] private bool _saveUserLayouts = true;
        [SerializeField] private bool _enableLayoutPresets = true;
        [SerializeField] private List<string> _restrictedWidgets = new List<string>();
        
        // Properties
        public string LayoutName => _layoutName;
        public string Description => _description;
        public DashboardType Type => _dashboardType;
        public bool IsDefault => _isDefault;
        
        public Vector2Int GridSize => _gridSize;
        public float GridCellSize => _gridCellSize;
        public float GridSpacing => _gridSpacing;
        public bool EnableGridSnap => _enableGridSnap;
        public bool AllowOverlap => _allowOverlap;
        
        public List<DashboardWidget> Widgets => _widgets;
        public int MaxWidgets => _maxWidgets;
        public bool EnableWidgetResize => _enableWidgetResize;
        public bool EnableWidgetDrag => _enableWidgetDrag;
        public bool EnableWidgetMenu => _enableWidgetMenu;
        
        public List<QuickAction> QuickActions => _quickActions;
        public QuickActionPlacement QuickActionPlacement => _quickActionPlacement;
        public int MaxQuickActions => _maxQuickActions;
        
        public List<StatusIndicator> StatusIndicators => _statusIndicators;
        public bool EnableStatusBar => _enableStatusBar;
        public StatusBarPosition StatusBarPosition => _statusBarPosition;
        
        public bool EnableAlerts => _enableAlerts;
        public AlertPlacement AlertPlacement => _alertPlacement;
        public int MaxVisibleAlerts => _maxVisibleAlerts;
        public float AlertDuration => _alertDuration;
        public bool EnableAlertGrouping => _enableAlertGrouping;
        
        public bool EnableQuickNavigation => _enableQuickNavigation;
        public List<NavigationItem> NavigationItems => _navigationItems;
        public NavigationStyle NavigationStyle => _navigationStyle;
        
        public bool AllowUserCustomization => _allowUserCustomization;
        public bool SaveUserLayouts => _saveUserLayouts;
        public bool EnableLayoutPresets => _enableLayoutPresets;
        public List<string> RestrictedWidgets => _restrictedWidgets;
        
        /// <summary>
        /// Get widget by ID
        /// </summary>
        public DashboardWidget GetWidget(string widgetId)
        {
            return _widgets.Find(w => w.WidgetId == widgetId);
        }
        
        /// <summary>
        /// Add widget to dashboard
        /// </summary>
        public bool AddWidget(DashboardWidget widget)
        {
            if (_widgets.Count >= _maxWidgets)
                return false;
                
            if (_restrictedWidgets.Contains(widget.WidgetType))
                return false;
                
            if (!IsPositionValid(widget.Position, widget.Size))
                return false;
            
            _widgets.Add(widget);
            return true;
        }
        
        /// <summary>
        /// Remove widget from dashboard
        /// </summary>
        public bool RemoveWidget(string widgetId)
        {
            var widget = GetWidget(widgetId);
            if (widget != null)
            {
                _widgets.Remove(widget);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Check if position is valid for widget placement
        /// </summary>
        public bool IsPositionValid(Vector2Int position, Vector2Int size)
        {
            // Check bounds
            if (position.x < 0 || position.y < 0)
                return false;
                
            if (position.x + size.x > _gridSize.x || position.y + size.y > _gridSize.y)
                return false;
            
            // Check overlap if not allowed
            if (!_allowOverlap)
            {
                var rect = new RectInt(position, size);
                foreach (var widget in _widgets)
                {
                    var widgetRect = new RectInt(widget.Position, widget.Size);
                    if (rect.Overlaps(widgetRect))
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get all widgets of a specific type
        /// </summary>
        public List<DashboardWidget> GetWidgetsByType(string widgetType)
        {
            return _widgets.FindAll(w => w.WidgetType == widgetType);
        }
        
        /// <summary>
        /// Get widgets in a specific area
        /// </summary>
        public List<DashboardWidget> GetWidgetsInArea(RectInt area)
        {
            var result = new List<DashboardWidget>();
            
            foreach (var widget in _widgets)
            {
                var widgetRect = new RectInt(widget.Position, widget.Size);
                if (area.Overlaps(widgetRect))
                {
                    result.Add(widget);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Find optimal position for new widget
        /// </summary>
        public Vector2Int FindOptimalPosition(Vector2Int size)
        {
            for (int y = 0; y <= _gridSize.y - size.y; y++)
            {
                for (int x = 0; x <= _gridSize.x - size.x; x++)
                {
                    var position = new Vector2Int(x, y);
                    if (IsPositionValid(position, size))
                    {
                        return position;
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // No valid position found
        }
        
        /// <summary>
        /// Create a copy of this layout
        /// </summary>
        public DashboardLayoutSO CreateCopy(string newName)
        {
            var copy = CreateInstance<DashboardLayoutSO>();
            copy._layoutName = newName;
            copy._description = _description;
            copy._dashboardType = _dashboardType;
            copy._isDefault = false;
            
            copy._gridSize = _gridSize;
            copy._gridCellSize = _gridCellSize;
            copy._gridSpacing = _gridSpacing;
            copy._enableGridSnap = _enableGridSnap;
            copy._allowOverlap = _allowOverlap;
            
            // Deep copy widgets
            copy._widgets = new List<DashboardWidget>();
            foreach (var widget in _widgets)
            {
                copy._widgets.Add(widget.CreateCopy());
            }
            
            // Copy other settings
            copy._maxWidgets = _maxWidgets;
            copy._enableWidgetResize = _enableWidgetResize;
            copy._enableWidgetDrag = _enableWidgetDrag;
            copy._enableWidgetMenu = _enableWidgetMenu;
            
            return copy;
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_layoutName))
            {
                _layoutName = name;
            }
            
            // Ensure grid size is valid
            _gridSize.x = Mathf.Max(1, _gridSize.x);
            _gridSize.y = Mathf.Max(1, _gridSize.y);
            
            // Ensure cell size is positive
            _gridCellSize = Mathf.Max(10f, _gridCellSize);
            
            // Ensure spacing is non-negative
            _gridSpacing = Mathf.Max(0f, _gridSpacing);
            
            // Ensure max widgets is reasonable
            _maxWidgets = Mathf.Clamp(_maxWidgets, 1, 50);
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_layoutName))
            {
                LogError("Layout name cannot be empty");
                isValid = false;
            }
            
            if (_gridSize.x <= 0 || _gridSize.y <= 0)
            {
                LogError("Grid size must be positive");
                isValid = false;
            }
            
            if (_gridCellSize <= 0)
            {
                LogError("Grid cell size must be positive");
                isValid = false;
            }
            
            if (_maxWidgets <= 0)
            {
                LogError("Max widgets must be positive");
                isValid = false;
            }
            
            // Validate widget positions
            foreach (var widget in _widgets)
            {
                if (!IsPositionValid(widget.Position, widget.Size))
                {
                    LogWarning($"Widget {widget.WidgetId} has invalid position or size");
                }
            }
            
            // Check for duplicate widget IDs
            var widgetIds = new HashSet<string>();
            foreach (var widget in _widgets)
            {
                if (widgetIds.Contains(widget.WidgetId))
                {
                    LogError($"Duplicate widget ID: {widget.WidgetId}");
                    isValid = false;
                }
                widgetIds.Add(widget.WidgetId);
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Dashboard widget configuration
    /// </summary>
    [System.Serializable]
    public class DashboardWidget
    {
        [SerializeField] public string WidgetId;
        [SerializeField] public string WidgetType;
        [SerializeField] public string DisplayName;
        [SerializeField] public Vector2Int Position;
        [SerializeField] public Vector2Int Size;
        [SerializeField] public bool IsResizable = true;
        [SerializeField] public bool IsDraggable = true;
        [SerializeField] public bool IsRemovable = true;
        [SerializeField] public int Priority = 0;
        [SerializeField] public Dictionary<string, object> Settings = new Dictionary<string, object>();
        
        public DashboardWidget CreateCopy()
        {
            return new DashboardWidget
            {
                WidgetId = System.Guid.NewGuid().ToString(),
                WidgetType = WidgetType,
                DisplayName = DisplayName,
                Position = Position,
                Size = Size,
                IsResizable = IsResizable,
                IsDraggable = IsDraggable,
                IsRemovable = IsRemovable,
                Priority = Priority,
                Settings = new Dictionary<string, object>(Settings)
            };
        }
    }
    
    /// <summary>
    /// Quick action configuration
    /// </summary>
    [System.Serializable]
    public class QuickAction
    {
        [SerializeField] public string ActionId;
        [SerializeField] public string DisplayName;
        [SerializeField] public string IconName;
        [SerializeField] public string Description;
        [SerializeField] public KeyCode Shortcut = KeyCode.None;
        [SerializeField] public bool RequireConfirmation = false;
        [SerializeField] public int Priority = 0;
    }
    
    /// <summary>
    /// Status indicator configuration
    /// </summary>
    [System.Serializable]
    public class StatusIndicator
    {
        [SerializeField] public string IndicatorId;
        [SerializeField] public string DisplayName;
        [SerializeField] public string DataSource;
        [SerializeField] public StatusType StatusType;
        [SerializeField] public bool ShowValue = true;
        [SerializeField] public bool ShowIcon = true;
        [SerializeField] public int Priority = 0;
    }
    
    /// <summary>
    /// Navigation item configuration
    /// </summary>
    [System.Serializable]
    public class NavigationItem
    {
        [SerializeField] public string ItemId;
        [SerializeField] public string DisplayName;
        [SerializeField] public string TargetPanel;
        [SerializeField] public string IconName;
        [SerializeField] public KeyCode Shortcut = KeyCode.None;
        [SerializeField] public int Priority = 0;
        [SerializeField] public bool IsVisible = true;
    }
    
    /// <summary>
    /// Dashboard types
    /// </summary>
    public enum DashboardType
    {
        Facility,
        Financial,
        Research,
        Production,
        Analytics,
        Custom
    }
    
    /// <summary>
    /// Quick action placement options
    /// </summary>
    public enum QuickActionPlacement
    {
        Header,
        Sidebar,
        Footer,
        FloatingBar
    }
    
    /// <summary>
    /// Status bar position options
    /// </summary>
    public enum StatusBarPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Hidden
    }
    
    /// <summary>
    /// Alert placement options
    /// </summary>
    public enum AlertPlacement
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }
    
    /// <summary>
    /// Navigation style options
    /// </summary>
    public enum NavigationStyle
    {
        Sidebar,
        TopBar,
        TabBar,
        Breadcrumb,
        Hidden
    }
}