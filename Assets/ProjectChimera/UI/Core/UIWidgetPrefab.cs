using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Data.UI;
using DashboardWidget = ProjectChimera.Data.UI.DashboardWidget;
using ProjectChimera.Core.Events;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Specialized UI component for dashboard widgets in Project Chimera.
    /// Extends UIComponentPrefab with widget-specific functionality.
    /// </summary>
    public abstract class UIWidgetPrefab : UIComponentPrefab
    {
        [Header("Widget Configuration")]
        [SerializeField] protected string _widgetType;
        [SerializeField] protected bool _isResizable = true;
        [SerializeField] protected bool _isDraggable = true;
        [SerializeField] protected bool _isRemovable = true;
        [SerializeField] protected int _priority = 0;
        
        [Header("Widget Layout")]
        [SerializeField] protected Vector2Int _minSize = new Vector2Int(2, 1);
        [SerializeField] protected Vector2Int _maxSize = new Vector2Int(6, 4);
        [SerializeField] protected Vector2Int _defaultGridSize = new Vector2Int(3, 2);
        
        [Header("Widget Data")]
        [SerializeField] protected float _updateInterval = 1f;
        [SerializeField] protected bool _autoUpdate = true;
        [SerializeField] protected bool _enableDataBinding = true;
        
        protected DashboardWidget _widgetData;
        protected VisualElement _headerElement;
        protected VisualElement _contentElement;
        protected VisualElement _footerElement;
        protected Button _menuButton;
        protected Label _titleLabel;
        protected float _lastUpdateTime;
        
        // Widget Events
        public System.Action<UIWidgetPrefab> OnWidgetMoved;
        public System.Action<UIWidgetPrefab> OnWidgetResized;
        public System.Action<UIWidgetPrefab> OnWidgetRemoved;
        public System.Action<UIWidgetPrefab> OnDataUpdated;
        
        // Properties
        public string WidgetType => _widgetType;
        public bool IsResizable => _isResizable;
        public bool IsDraggable => _isDraggable;
        public bool IsRemovable => _isRemovable;
        public int Priority => _priority;
        public Vector2Int MinSize => _minSize;
        public Vector2Int MaxSize => _maxSize;
        public Vector2Int DefaultGridSize => _defaultGridSize;
        public DashboardWidget WidgetData => _widgetData;
        public VisualElement HeaderElement => _headerElement;
        public VisualElement ContentElement => _contentElement;
        public VisualElement FooterElement => _footerElement;
        
        /// <summary>
        /// Initialize widget with dashboard widget data
        /// </summary>
        public virtual void Initialize(DashboardWidget widgetData)
        {
            _widgetData = widgetData;
            
            if (widgetData != null)
            {
                _componentId = widgetData.WidgetId;
                _componentName = widgetData.DisplayName;
                _widgetType = widgetData.WidgetType;
                _isResizable = widgetData.IsResizable;
                _isDraggable = widgetData.IsDraggable;
                _isRemovable = widgetData.IsRemovable;
                _priority = widgetData.Priority;
            }
            
            base.Initialize();
        }
        
        protected override void SetupComponent()
        {
            CreateWidgetStructure();
            SetupWidgetSpecific();
            ApplyWidgetStyling();
            
            if (_autoUpdate)
            {
                _lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Create the standard widget structure (header, content, footer)
        /// </summary>
        protected virtual void CreateWidgetStructure()
        {
            if (_rootElement == null)
                return;
                
            _rootElement.AddToClassList("dashboard-widget");
            
            // Create header
            _headerElement = new VisualElement();
            _headerElement.name = "widget-header";
            _headerElement.AddToClassList("widget-header");
            
            _titleLabel = new Label(_componentName);
            _titleLabel.name = "widget-title";
            _titleLabel.AddToClassList("widget-title");
            _headerElement.Add(_titleLabel);
            
            // Create menu button if removable
            if (_isRemovable)
            {
                _menuButton = new Button();
                _menuButton.name = "widget-menu";
                _menuButton.AddToClassList("widget-menu-button");
                _menuButton.text = "⋮";
                _menuButton.clicked += ShowWidgetMenu;
                _headerElement.Add(_menuButton);
            }
            
            _rootElement.Add(_headerElement);
            
            // Create content area
            _contentElement = new VisualElement();
            _contentElement.name = "widget-content";
            _contentElement.AddToClassList("widget-content");
            _rootElement.Add(_contentElement);
            
            // Create footer (optional)
            _footerElement = new VisualElement();
            _footerElement.name = "widget-footer";
            _footerElement.AddToClassList("widget-footer");
            _rootElement.Add(_footerElement);
        }
        
        /// <summary>
        /// Setup widget-specific components
        /// </summary>
        protected abstract void SetupWidgetSpecific();
        
        /// <summary>
        /// Apply widget styling based on theme
        /// </summary>
        protected virtual void ApplyWidgetStyling()
        {
            if (_rootElement == null)
                return;
                
            // Add priority-based styling
            _rootElement.AddToClassList($"priority-{_priority}");
            
            // Add type-based styling
            if (!string.IsNullOrEmpty(_widgetType))
            {
                _rootElement.AddToClassList($"widget-type-{_widgetType.ToLower()}");
            }
            
            // Add state-based styling
            if (!_isResizable)
                _rootElement.AddToClassList("non-resizable");
            if (!_isDraggable)
                _rootElement.AddToClassList("non-draggable");
        }
        
        protected override void UpdateComponentSpecific()
        {
            if (!_autoUpdate)
                return;
                
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateWidgetData();
                _lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Update widget data and visual state
        /// </summary>
        protected virtual void UpdateWidgetData()
        {
            // Override in derived classes for specific data updating
            OnDataUpdated?.Invoke(this);
        }
        
        /// <summary>
        /// Show widget context menu
        /// </summary>
        protected virtual void ShowWidgetMenu()
        {
            var menu = new GenericDropdownMenu();
            
            if (_isResizable)
            {
                menu.AddItem("Resize", false, () => StartResize());
            }
            
            if (_isRemovable)
            {
                menu.AddSeparator("");
                menu.AddItem("Remove Widget", false, () => RemoveWidget());
            }
            
            menu.AddSeparator("");
            menu.AddItem("Widget Settings", false, () => ShowSettings());
            
            var menuPosition = _menuButton.worldBound.position;
            menu.DropDown(_menuButton.worldBound, _menuButton, true);
        }
        
        /// <summary>
        /// Start widget resize operation
        /// </summary>
        protected virtual void StartResize()
        {
            LogInfo($"Starting resize for widget: {_componentName}");
            // Implementation would depend on the dashboard system
        }
        
        /// <summary>
        /// Remove widget from dashboard
        /// </summary>
        protected virtual void RemoveWidget()
        {
            LogInfo($"Removing widget: {_componentName}");
            OnWidgetRemoved?.Invoke(this);
        }
        
        /// <summary>
        /// Show widget settings dialog
        /// </summary>
        protected virtual void ShowSettings()
        {
            LogInfo($"Showing settings for widget: {_componentName}");
            // Implementation would show a settings modal
        }
        
        /// <summary>
        /// Set widget position in grid coordinates
        /// </summary>
        public virtual void SetGridPosition(Vector2Int position)
        {
            if (_widgetData != null)
            {
                _widgetData.Position = position;
                OnWidgetMoved?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Set widget size in grid coordinates
        /// </summary>
        public virtual void SetGridSize(Vector2Int size)
        {
            if (_widgetData != null)
            {
                // Clamp size to valid range
                size.x = Mathf.Clamp(size.x, _minSize.x, _maxSize.x);
                size.y = Mathf.Clamp(size.y, _minSize.y, _maxSize.y);
                
                _widgetData.Size = size;
                OnWidgetResized?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Get widget position in grid coordinates
        /// </summary>
        public Vector2Int GetGridPosition()
        {
            return _widgetData?.Position ?? Vector2Int.zero;
        }
        
        /// <summary>
        /// Get widget size in grid coordinates
        /// </summary>
        public Vector2Int GetGridSize()
        {
            return _widgetData?.Size ?? _defaultGridSize;
        }
        
        /// <summary>
        /// Check if widget can be resized to the specified size
        /// </summary>
        public virtual bool CanResize(Vector2Int newSize)
        {
            if (!_isResizable)
                return false;
                
            return newSize.x >= _minSize.x && newSize.x <= _maxSize.x &&
                   newSize.y >= _minSize.y && newSize.y <= _maxSize.y;
        }
        
        /// <summary>
        /// Set widget title
        /// </summary>
        public virtual void SetTitle(string title)
        {
            _componentName = title;
            if (_titleLabel != null)
            {
                _titleLabel.text = title;
            }
        }
        
        /// <summary>
        /// Enable or disable widget interactions
        /// </summary>
        public virtual void SetInteractable(bool interactable)
        {
            if (_rootElement != null)
            {
                _rootElement.SetEnabled(interactable);
                
                if (interactable)
                {
                    _rootElement.RemoveFromClassList("disabled");
                }
                // else
                // {
                    _rootElement.AddToClassList("disabled");
                // }
            }
        }
        
        protected override UIComponentPrefab CreateClone()
        {
            var clone = CreateWidgetClone();
            
            // Copy widget-specific properties
            var widgetClone = clone as UIWidgetPrefab;
            if (widgetClone != null)
            {
                widgetClone._widgetType = _widgetType;
                widgetClone._isResizable = _isResizable;
                widgetClone._isDraggable = _isDraggable;
                widgetClone._isRemovable = _isRemovable;
                widgetClone._priority = _priority;
                widgetClone._minSize = _minSize;
                widgetClone._maxSize = _maxSize;
                widgetClone._defaultGridSize = _defaultGridSize;
                widgetClone._updateInterval = _updateInterval;
                widgetClone._autoUpdate = _autoUpdate;
                widgetClone._enableDataBinding = _enableDataBinding;
                
                // Clone widget data if it exists
                if (_widgetData != null)
                {
                    widgetClone._widgetData = _widgetData.CreateCopy();
                }
            }
            
            return clone;
        }
        
        /// <summary>
        /// Create widget-specific clone
        /// </summary>
        protected abstract UIWidgetPrefab CreateWidgetClone();
        
        public override bool ValidateComponent()
        {
            if (!base.ValidateComponent())
                return false;
                
            if (string.IsNullOrEmpty(_widgetType))
            {
                LogError("Widget type cannot be empty");
                return false;
            }
            
            if (_minSize.x <= 0 || _minSize.y <= 0)
            {
                LogError("Minimum size must be positive");
                return false;
            }
            
            if (_maxSize.x < _minSize.x || _maxSize.y < _minSize.y)
            {
                LogError("Maximum size must be greater than or equal to minimum size");
                return false;
            }
            
            return true;
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_widgetType))
            {
                _widgetType = GetType().Name.Replace("Widget", "").Replace("Prefab", "");
            }
            
            _minSize.x = Mathf.Max(1, _minSize.x);
            _minSize.y = Mathf.Max(1, _minSize.y);
            _maxSize.x = Mathf.Max(_minSize.x, _maxSize.x);
            _maxSize.y = Mathf.Max(_minSize.y, _maxSize.y);
            _updateInterval = Mathf.Max(0.1f, _updateInterval);
        }
    }
}