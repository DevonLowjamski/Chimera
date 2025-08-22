using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Base class for reusable UI component prefabs in Project Chimera.
    /// Provides common functionality for instantiating and managing UI components.
    /// </summary>
    public abstract class UIComponentPrefab : ChimeraMonoBehaviour
    {
        [Header("Component Configuration")]
        [SerializeField] protected string _componentId;
        [SerializeField] protected string _componentName;
        [SerializeField] protected bool _isReusable = true;
        [SerializeField] protected bool _enablePooling = true;
        
        [Header("Visual Assets")]
        [SerializeField] protected VisualTreeAsset _visualAsset;
        [SerializeField] protected StyleSheet _styleSheet;
        [SerializeField] protected Texture2D _icon;
        
        [Header("Component State")]
        [SerializeField] protected bool _isActive = true;
        [SerializeField] protected bool _isVisible = true;
        [SerializeField] protected float _defaultWidth = 200f;
        [SerializeField] protected float _defaultHeight = 100f;
        
        protected VisualElement _rootElement;
        protected bool _isInitialized = false;
        
        // Events
        public System.Action<UIComponentPrefab> OnComponentCreated;
        public System.Action<UIComponentPrefab> OnComponentDestroyed;
        public System.Action<UIComponentPrefab, bool> OnVisibilityChanged;
        
        // Properties
        public string ComponentId => _componentId;
        public string ComponentName => _componentName;
        public bool IsReusable => _isReusable;
        public bool EnablePooling => _enablePooling;
        public VisualTreeAsset VisualAsset => _visualAsset;
        public StyleSheet StyleSheet => _styleSheet;
        public Texture2D Icon => _icon;
        public bool IsActive => _isActive;
        public bool IsVisible => _isVisible;
        public VisualElement RootElement => _rootElement;
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Initialize the UI component
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
                return;
                
            if (string.IsNullOrEmpty(_componentId))
            {
                _componentId = System.Guid.NewGuid().ToString();
            }
            
            CreateVisualElement();
            SetupComponent();
            BindEvents();
            
            _isInitialized = true;
            OnComponentCreated?.Invoke(this);
            
            LogInfo($"UI Component '{_componentName}' initialized with ID: {_componentId}");
        }
        
        /// <summary>
        /// Create the visual element from UXML asset
        /// </summary>
        protected virtual void CreateVisualElement()
        {
            if (_visualAsset != null)
            {
                _rootElement = _visualAsset.Instantiate();
                
                if (_styleSheet != null)
                {
                    _rootElement.styleSheets.Add(_styleSheet);
                }
                
                _rootElement.name = _componentName;
                _rootElement.userData = this;
                
                // Set default size
                _rootElement.style.width = _defaultWidth;
                _rootElement.style.height = _defaultHeight;
            }
            // else
            // {
                _rootElement = new VisualElement();
                _rootElement.name = _componentName;
                _rootElement.userData = this;
            // }
        }
        
        /// <summary>
        /// Setup component-specific functionality
        /// </summary>
        protected abstract void SetupComponent();
        
        /// <summary>
        /// Bind UI events and callbacks
        /// </summary>
        protected virtual void BindEvents()
        {
            // Override in derived classes for specific event binding
        }
        
        /// <summary>
        /// Update component state
        /// </summary>
        public virtual void UpdateComponent()
        {
            if (!_isInitialized)
                return;
                
            UpdateVisualState();
            UpdateComponentSpecific();
        }
        
        /// <summary>
        /// Update visual state based on current data
        /// </summary>
        protected virtual void UpdateVisualState()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Component-specific update logic
        /// </summary>
        protected abstract void UpdateComponentSpecific();
        
        /// <summary>
        /// Set component visibility
        /// </summary>
        public virtual void SetVisible(bool visible)
        {
            if (_isVisible != visible)
            {
                _isVisible = visible;
                UpdateVisualState();
                OnVisibilityChanged?.Invoke(this, visible);
            }
        }
        
        /// <summary>
        /// Set component active state
        /// </summary>
        public virtual void SetActive(bool active)
        {
            _isActive = active;
            
            if (_rootElement != null)
            {
                _rootElement.SetEnabled(active);
            }
        }
        
        /// <summary>
        /// Cleanup component resources
        /// </summary>
        public virtual void Cleanup()
        {
            UnbindEvents();
            
            if (_rootElement != null)
            {
                _rootElement.RemoveFromHierarchy();
                _rootElement = null;
            }
            
            _isInitialized = false;
            OnComponentDestroyed?.Invoke(this);
            
            LogInfo($"UI Component '{_componentName}' cleaned up");
        }
        
        /// <summary>
        /// Unbind events during cleanup
        /// </summary>
        protected virtual void UnbindEvents()
        {
            // Override in derived classes for specific event unbinding
        }
        
        /// <summary>
        /// Get component size
        /// </summary>
        public Vector2 GetSize()
        {
            if (_rootElement != null)
            {
                return new Vector2(_rootElement.resolvedStyle.width, _rootElement.resolvedStyle.height);
            }
            return new Vector2(_defaultWidth, _defaultHeight);
        }
        
        /// <summary>
        /// Set component size
        /// </summary>
        public void SetSize(Vector2 size)
        {
            _defaultWidth = size.x;
            _defaultHeight = size.y;
            
            if (_rootElement != null)
            {
                _rootElement.style.width = size.x;
                _rootElement.style.height = size.y;
            }
        }
        
        /// <summary>
        /// Clone this component
        /// </summary>
        public virtual UIComponentPrefab Clone()
        {
            if (!_isReusable)
            {
                LogWarning($"Attempting to clone non-reusable component: {_componentName}");
                return null;
            }
            
            var clone = CreateClone();
            clone._componentId = System.Guid.NewGuid().ToString();
            clone.Initialize();
            
            return clone;
        }
        
        /// <summary>
        /// Create component-specific clone
        /// </summary>
        protected abstract UIComponentPrefab CreateClone();
        
        /// <summary>
        /// Validate component configuration
        /// </summary>
        public virtual bool ValidateComponent()
        {
            if (string.IsNullOrEmpty(_componentName))
            {
                LogError("Component name cannot be empty");
                return false;
            }
            
            if (_visualAsset == null)
            {
                LogWarning($"No visual asset assigned to component: {_componentName}");
            }
            
            return true;
        }
        
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_componentId))
            {
                _componentId = System.Guid.NewGuid().ToString();
            }
            
            if (string.IsNullOrEmpty(_componentName))
            {
                _componentName = name;
            }
            
            _defaultWidth = Mathf.Max(50f, _defaultWidth);
            _defaultHeight = Mathf.Max(20f, _defaultHeight);
        }
        
        protected virtual void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
        
        protected virtual void OnDestroy()
        {
            Cleanup();
        }
    }
}