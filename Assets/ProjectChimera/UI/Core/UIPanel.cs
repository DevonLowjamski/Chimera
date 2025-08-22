using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Base class for all UI panels in Project Chimera.
    /// Provides common functionality for panel management, transitions, and lifecycle.
    /// </summary>
    public abstract class UIPanel : ChimeraMonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private string _panelId;
        [SerializeField] private UIPanelType _panelType = UIPanelType.Standard;
        [SerializeField] private bool _closeOnBackgroundClick = false;
        [SerializeField] private bool _pauseGameWhenShown = false;
        
        [Header("Animation Settings")]
        [SerializeField] private bool _useTransitions = true;
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _hideDuration = 0.25f;
        [SerializeField] private UIPanelTransition _showTransition = UIPanelTransition.FadeIn;
        [SerializeField] private UIPanelTransition _hideTransition = UIPanelTransition.FadeOut;
        
        [Header("UI Elements")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _rootElementName = "root";
        
        // Protected members for derived classes
        // protected UIManager _uiManager;
        protected VisualElement _rootElement;
        protected VisualElement _contentContainer;
        protected bool _isVisible;
        protected bool _isInitialized;
        
        // Properties
        public string PanelId => _panelId;
        public UIPanelType PanelType => _panelType;
        public bool IsVisible => _isVisible;
        public bool IsInitialized => _isInitialized;
        public UIDocument UIDocument => _uiDocument;
        public VisualElement RootElement => _rootElement;
        
        protected virtual void Awake()
        {
            ValidatePanelId();
        }
        
        protected virtual void Start()
        {
            // Initialize panel if not already initialized
            if (!_isInitialized)
            {
                SetupUIDocument();
                SetupUIElements();
                BindUIEvents();
                OnPanelInitialized();
                
                _isInitialized = true;
                LogInfo($"Panel {_panelId} initialized successfully");
            }
        }
        
        /// <summary>
        /// Initialize the panel with UI Manager reference
        /// </summary>
        // public virtual void Initialize(UIManager uiManager)
        // {
            // if (_isInitialized)
            // {
                // LogWarning($"Panel {_panelId} is already initialized");
                // return;
            // }
            
            // _uiManager = uiManager;
            
            // SetupUIDocument();
            // SetupUIElements();
            // BindUIEvents();
            // OnPanelInitialized();
            
            // _isInitialized = true;
            // LogInfo($"Panel {_panelId} initialized successfully");
        // }
        
        /// <summary>
        /// Setup UI Document and find root element
        /// </summary>
        private void SetupUIDocument()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    LogError($"Panel {_panelId} has no UIDocument component!");
                    return;
                }
            }
            
            _rootElement = _uiDocument.rootVisualElement;
            
            // Find root element by name if specified
            if (!string.IsNullOrEmpty(_rootElementName))
            {
                var namedRoot = _rootElement.Q(_rootElementName);
                if (namedRoot != null)
                {
                    _rootElement = namedRoot;
                }
            }
            
            // Create content container
            _contentContainer = _rootElement.Q("content") ?? _rootElement;
            
            // Initially hide the panel
            _rootElement.style.display = DisplayStyle.None;
            _isVisible = false;
        }
        
        /// <summary>
        /// Setup UI elements - override in derived classes
        /// </summary>
        protected virtual void SetupUIElements()
        {
            // Apply design system styles if available
            // if (_uiManager?.DesignSystem != null)
            // {
                ApplyDesignSystemStyles();
            // }
        }
        
        /// <summary>
        /// Bind UI events - override in derived classes
        /// </summary>
        protected virtual void BindUIEvents()
        {
            // Setup background click handling
            if (_closeOnBackgroundClick && _panelType == UIPanelType.Modal)
            {
                SetupBackgroundClickClose();
            }
        }
        
        /// <summary>
        /// Called after panel initialization - override in derived classes
        /// </summary>
        protected virtual void OnPanelInitialized()
        {
            // Override in derived classes for custom initialization
        }
        
        /// <summary>
        /// Show the panel
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;
            
            if (!_isInitialized)
            {
                LogError($"Cannot show panel {_panelId} - panel not initialized");
                return;
            }
            
            StartCoroutine(ShowCoroutine());
        }
        
        /// <summary>
        /// Show panel coroutine with transitions
        /// </summary>
        public IEnumerator ShowCoroutine()
        {
            if (_isVisible) yield break;
            
            // Pre-show setup
            OnBeforeShow();
            
            // Make visible
            _rootElement.style.display = DisplayStyle.Flex;
            _isVisible = true;
            
            // Handle game pause
            if (_pauseGameWhenShown)
            {
                Time.timeScale = 0f;
            }
            
            // Play show transition
            if (_useTransitions)
            {
                yield return StartCoroutine(PlayShowTransition());
            }
            
            // Post-show setup
            OnAfterShow();
            
            LogInfo($"Panel {_panelId} shown");
        }
        
        /// <summary>
        /// Hide the panel
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;
            
            StartCoroutine(HideCoroutine());
        }
        
        /// <summary>
        /// Hide panel coroutine with transitions
        /// </summary>
        public IEnumerator HideCoroutine()
        {
            if (!_isVisible) yield break;
            
            // Pre-hide setup
            OnBeforeHide();
            
            // Play hide transition
            if (_useTransitions)
            {
                yield return StartCoroutine(PlayHideTransition());
            }
            
            // Make invisible
            _rootElement.style.display = DisplayStyle.None;
            _isVisible = false;
            
            // Handle game unpause
            if (_pauseGameWhenShown)
            {
                Time.timeScale = 1f;
            }
            
            // Post-hide setup
            OnAfterHide();
            
            LogInfo($"Panel {_panelId} hidden");
        }
        
        /// <summary>
        /// Play show transition animation
        /// </summary>
        private IEnumerator PlayShowTransition()
        {
            switch (_showTransition)
            {
                case UIPanelTransition.FadeIn:
                    yield return StartCoroutine(FadeIn());
                    break;
                    
                case UIPanelTransition.SlideInFromLeft:
                    yield return StartCoroutine(SlideInFromLeft());
                    break;
                    
                case UIPanelTransition.SlideInFromRight:
                    yield return StartCoroutine(SlideInFromRight());
                    break;
                    
                case UIPanelTransition.SlideInFromTop:
                    yield return StartCoroutine(SlideInFromTop());
                    break;
                    
                case UIPanelTransition.SlideInFromBottom:
                    yield return StartCoroutine(SlideInFromBottom());
                    break;
                    
                case UIPanelTransition.ScaleIn:
                    yield return StartCoroutine(ScaleIn());
                    break;
                    
                case UIPanelTransition.None:
                default:
                    // No animation
                    break;
            }
        }
        
        /// <summary>
        /// Play hide transition animation
        /// </summary>
        private IEnumerator PlayHideTransition()
        {
            switch (_hideTransition)
            {
                case UIPanelTransition.FadeOut:
                    yield return StartCoroutine(FadeOut());
                    break;
                    
                case UIPanelTransition.SlideOutToLeft:
                    yield return StartCoroutine(SlideOutToLeft());
                    break;
                    
                case UIPanelTransition.SlideOutToRight:
                    yield return StartCoroutine(SlideOutToRight());
                    break;
                    
                case UIPanelTransition.SlideOutToTop:
                    yield return StartCoroutine(SlideOutToTop());
                    break;
                    
                case UIPanelTransition.SlideOutToBottom:
                    yield return StartCoroutine(SlideOutToBottom());
                    break;
                    
                case UIPanelTransition.ScaleOut:
                    yield return StartCoroutine(ScaleOut());
                    break;
                    
                case UIPanelTransition.None:
                default:
                    // No animation
                    break;
            }
        }
        
        /// <summary>
        /// Fade in animation
        /// </summary>
        private IEnumerator FadeIn()
        {
            _rootElement.style.opacity = 0f;
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                _rootElement.style.opacity = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }
            
            _rootElement.style.opacity = 1f;
        }
        
        /// <summary>
        /// Fade out animation
        /// </summary>
        private IEnumerator FadeOut()
        {
            _rootElement.style.opacity = 1f;
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                _rootElement.style.opacity = Mathf.Lerp(1f, 0f, progress);
                yield return null;
            }
            
            _rootElement.style.opacity = 0f;
        }
        
        /// <summary>
        /// Slide in from left animation
        /// </summary>
        private IEnumerator SlideInFromLeft()
        {
            var startPos = -_rootElement.resolvedStyle.width;
            _rootElement.style.left = startPos;
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                _rootElement.style.left = Mathf.Lerp(startPos, 0f, progress);
                yield return null;
            }
            
            _rootElement.style.left = 0f;
        }
        
        /// <summary>
        /// Slide in from right animation
        /// </summary>
        private IEnumerator SlideInFromRight()
        {
            var startPos = _rootElement.resolvedStyle.width;
            _rootElement.style.left = startPos;
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                _rootElement.style.left = Mathf.Lerp(startPos, 0f, progress);
                yield return null;
            }
            
            _rootElement.style.left = 0f;
        }
        
        /// <summary>
        /// Slide in from top animation
        /// </summary>
        private IEnumerator SlideInFromTop()
        {
            var startPos = -_rootElement.resolvedStyle.height;
            _rootElement.style.top = startPos;
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                _rootElement.style.top = Mathf.Lerp(startPos, 0f, progress);
                yield return null;
            }
            
            _rootElement.style.top = 0f;
        }
        
        /// <summary>
        /// Slide in from bottom animation
        /// </summary>
        private IEnumerator SlideInFromBottom()
        {
            var startPos = _rootElement.resolvedStyle.height;
            _rootElement.style.top = startPos;
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                _rootElement.style.top = Mathf.Lerp(startPos, 0f, progress);
                yield return null;
            }
            
            _rootElement.style.top = 0f;
        }
        
        /// <summary>
        /// Scale in animation
        /// </summary>
        private IEnumerator ScaleIn()
        {
            _rootElement.style.scale = new Scale(Vector3.zero);
            
            float elapsed = 0f;
            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _showDuration;
                float scale = Mathf.Lerp(0f, 1f, progress);
                _rootElement.style.scale = new Scale(Vector3.one * scale);
                yield return null;
            }
            
            _rootElement.style.scale = new Scale(Vector3.one);
        }
        
        /// <summary>
        /// Scale out animation
        /// </summary>
        private IEnumerator ScaleOut()
        {
            _rootElement.style.scale = new Scale(Vector3.one);
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                float scale = Mathf.Lerp(1f, 0f, progress);
                _rootElement.style.scale = new Scale(Vector3.one * scale);
                yield return null;
            }
            
            _rootElement.style.scale = new Scale(Vector3.zero);
        }
        
        /// <summary>
        /// Slide out to left animation
        /// </summary>
        private IEnumerator SlideOutToLeft()
        {
            var endPos = -_rootElement.resolvedStyle.width;
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                _rootElement.style.left = Mathf.Lerp(0f, endPos, progress);
                yield return null;
            }
        }
        
        /// <summary>
        /// Slide out to right animation
        /// </summary>
        private IEnumerator SlideOutToRight()
        {
            var endPos = _rootElement.resolvedStyle.width;
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                _rootElement.style.left = Mathf.Lerp(0f, endPos, progress);
                yield return null;
            }
        }
        
        /// <summary>
        /// Slide out to top animation
        /// </summary>
        private IEnumerator SlideOutToTop()
        {
            var endPos = -_rootElement.resolvedStyle.height;
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                _rootElement.style.top = Mathf.Lerp(0f, endPos, progress);
                yield return null;
            }
        }
        
        /// <summary>
        /// Slide out to bottom animation
        /// </summary>
        private IEnumerator SlideOutToBottom()
        {
            var endPos = _rootElement.resolvedStyle.height;
            
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / _hideDuration;
                _rootElement.style.top = Mathf.Lerp(0f, endPos, progress);
                yield return null;
            }
        }
        
        /// <summary>
        /// Setup background click to close functionality
        /// </summary>
        private void SetupBackgroundClickClose()
        {
            _rootElement.RegisterCallback<ClickEvent>(OnBackgroundClick);
        }
        
        /// <summary>
        /// Handle background click
        /// </summary>
        private void OnBackgroundClick(ClickEvent evt)
        {
            if (evt.target == _rootElement && _closeOnBackgroundClick)
            {
                Hide();
                evt.StopPropagation();
            }
        }
        
        /// <summary>
        /// Apply design system styles
        /// </summary>
        protected virtual void ApplyDesignSystemStyles()
        {
            // if (_uiManager?.DesignSystem == null) return;
            
            // Apply panel styling based on type
            switch (_panelType)
            {
                case UIPanelType.Standard:
                    // _uiManager.ApplyDesignSystemStyle(_rootElement, UIStyleToken.Panel);
                    break;
                    
                case UIPanelType.Modal:
                    // _uiManager.ApplyDesignSystemStyle(_rootElement, UIStyleToken.Card);
                    break;
            }
        }
        
        /// <summary>
        /// Validate panel ID
        /// </summary>
        private void ValidatePanelId()
        {
            if (string.IsNullOrEmpty(_panelId))
            {
                _panelId = gameObject.name.ToLower().Replace(" ", "-");
                LogWarning($"Panel ID was empty, auto-generated: {_panelId}");
            }
        }
        
        /// <summary>
        /// Called before panel is shown - override in derived classes
        /// </summary>
        protected virtual void OnBeforeShow()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called after panel is shown - override in derived classes
        /// </summary>
        protected virtual void OnAfterShow()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called before panel is hidden - override in derived classes
        /// </summary>
        protected virtual void OnBeforeHide()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called after panel is hidden - override in derived classes
        /// </summary>
        protected virtual void OnAfterHide()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Get UI element by name
        /// </summary>
        protected T GetUIElement<T>(string elementName) where T : VisualElement
        {
            return _rootElement.Q<T>(elementName);
        }
        
        /// <summary>
        /// Get UI element by class name
        /// </summary>
        protected T GetUIElementByClass<T>(string className) where T : VisualElement
        {
            return _rootElement.Q<T>(className: className);
        }
        
        /// <summary>
        /// Find all UI elements by class name
        /// </summary>
        protected UQueryBuilder<T> GetUIElementsByClass<T>(string className) where T : VisualElement
        {
            return _rootElement.Query<T>(className: className);
        }
        
        /// <summary>
        /// Dispose of the panel and clean up resources
        /// </summary>
        public virtual void Dispose()
        {
            if (_isVisible)
            {
                Hide();
            }
            
            // Cleanup UI elements
            if (_rootElement != null)
            {
                _rootElement.RemoveFromHierarchy();
            }
            
            // Reset state
            _isInitialized = false;
            _isVisible = false;
            
            LogInfo($"Panel {_panelId} disposed");
        }
    }
    
    /// <summary>
    /// UI Panel types
    /// </summary>
    public enum UIPanelType
    {
        Standard,    // Regular full-screen or section panels
        Modal,       // Modal dialogs that overlay other content
        Overlay,     // HUD overlays that don't block interaction
        Popup        // Small popup windows
    }
    
    /// <summary>
    /// UI Panel transition types
    /// </summary>
    public enum UIPanelTransition
    {
        None,
        FadeIn,
        FadeOut,
        SlideInFromLeft,
        SlideInFromRight,
        SlideInFromTop,
        SlideInFromBottom,
        SlideOutToLeft,
        SlideOutToRight,
        SlideOutToTop,
        SlideOutToBottom,
        ScaleIn,
        ScaleOut
    }
}