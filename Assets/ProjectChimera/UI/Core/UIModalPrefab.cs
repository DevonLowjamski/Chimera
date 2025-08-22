using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental; // Added for StyleValues
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Specialized UI component for modal dialogs in Project Chimera.
    /// Provides modal-specific functionality like backdrop, focus management, and animations.
    /// </summary>
    public abstract class UIModalPrefab : UIComponentPrefab
    {
        [Header("Modal Configuration")]
        [SerializeField] protected bool _closeOnBackdropClick = true;
        [SerializeField] protected bool _closeOnEscapeKey = true;
        [SerializeField] protected bool _blockInteraction = true;
        [SerializeField] protected bool _enableBackdrop = true;
        
        [Header("Modal Animation")]
        [SerializeField] protected bool _enableAnimation = true;
        [SerializeField] protected float _fadeInDuration = 0.3f;
        [SerializeField] protected float _fadeOutDuration = 0.2f;
        [SerializeField] protected AnimationCurve _easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Modal Layout")]
        [SerializeField] protected ModalSize _modalSize = ModalSize.Medium;
        [SerializeField] protected Vector2 _customSize = new Vector2(500, 400);
        [SerializeField] protected bool _centerModal = true;
        [SerializeField] protected Vector2 _modalOffset = Vector2.zero;
        
        protected VisualElement _backdropElement;
        protected VisualElement _modalContainer;
        protected VisualElement _modalHeader;
        protected VisualElement _modalBody;
        protected VisualElement _modalFooter;
        protected Button _closeButton;
        protected Label _titleLabel;
        protected bool _isShowing = false;
        protected bool _isAnimating = false;
        
        // Modal Events
        public System.Action<UIModalPrefab> OnModalOpened;
        public System.Action<UIModalPrefab> OnModalClosed;
        public System.Action<UIModalPrefab> OnModalConfirmed;
        public System.Action<UIModalPrefab> OnModalCancelled;
        
        // Properties
        public bool CloseOnBackdropClick => _closeOnBackdropClick;
        public bool CloseOnEscapeKey => _closeOnEscapeKey;
        public bool BlockInteraction => _blockInteraction;
        public bool EnableBackdrop => _enableBackdrop;
        public bool IsShowing => _isShowing;
        public bool IsAnimating => _isAnimating;
        public VisualElement ModalContainer => _modalContainer;
        public VisualElement ModalHeader => _modalHeader;
        public VisualElement ModalBody => _modalBody;
        public VisualElement ModalFooter => _modalFooter;
        
        protected override void SetupComponent()
        {
            CreateModalStructure();
            SetupModalSpecific();
            ApplyModalStyling();
            SetupEventHandlers();
        }
        
        /// <summary>
        /// Create the modal structure (backdrop, container, header, body, footer)
        /// </summary>
        protected virtual void CreateModalStructure()
        {
            if (_rootElement == null)
                return;
                
            _rootElement.AddToClassList("modal-overlay");
            _rootElement.style.position = Position.Absolute;
            _rootElement.style.left = 0;
            _rootElement.style.top = 0;
            _rootElement.style.right = 0;
            _rootElement.style.bottom = 0;
            _rootElement.style.alignItems = Align.Center;
            _rootElement.style.justifyContent = Justify.Center;
            
            // Create backdrop
            if (_enableBackdrop)
            {
                _backdropElement = new VisualElement();
                _backdropElement.name = "modal-backdrop";
                _backdropElement.AddToClassList("modal-backdrop");
                _backdropElement.style.position = Position.Absolute;
                _backdropElement.style.left = 0;
                _backdropElement.style.top = 0;
                _backdropElement.style.right = 0;
                _backdropElement.style.bottom = 0;
                _rootElement.Add(_backdropElement);
            }
            
            // Create modal container
            _modalContainer = new VisualElement();
            _modalContainer.name = "modal-container";
            _modalContainer.AddToClassList("modal-container");
            ApplyModalSize();
            _rootElement.Add(_modalContainer);
            
            // Create header
            _modalHeader = new VisualElement();
            _modalHeader.name = "modal-header";
            _modalHeader.AddToClassList("modal-header");
            
            _titleLabel = new Label(_componentName);
            _titleLabel.name = "modal-title";
            _titleLabel.AddToClassList("modal-title");
            _modalHeader.Add(_titleLabel);
            
            _closeButton = new Button();
            _closeButton.name = "modal-close";
            _closeButton.AddToClassList("modal-close-button");
            _closeButton.text = "×";
            _closeButton.clicked += () => CloseModal(false);
            _modalHeader.Add(_closeButton);
            
            _modalContainer.Add(_modalHeader);
            
            // Create body
            _modalBody = new VisualElement();
            _modalBody.name = "modal-body";
            _modalBody.AddToClassList("modal-body");
            _modalContainer.Add(_modalBody);
            
            // Create footer
            _modalFooter = new VisualElement();
            _modalFooter.name = "modal-footer";
            _modalFooter.AddToClassList("modal-footer");
            _modalContainer.Add(_modalFooter);
        }
        
        /// <summary>
        /// Setup modal-specific components
        /// </summary>
        protected abstract void SetupModalSpecific();
        
        /// <summary>
        /// Apply modal styling and size
        /// </summary>
        protected virtual void ApplyModalStyling()
        {
            if (_modalContainer == null)
                return;
                
            _modalContainer.AddToClassList($"modal-size-{_modalSize.ToString().ToLower()}");
            
            if (_centerModal)
            {
                _modalContainer.style.alignSelf = Align.Center;
            }
        }
        
        /// <summary>
        /// Apply modal size based on configuration
        /// </summary>
        protected virtual void ApplyModalSize()
        {
            if (_modalContainer == null)
                return;
                
            Vector2 size = _modalSize switch
            {
                ModalSize.Small => new Vector2(300, 200),
                ModalSize.Medium => new Vector2(500, 400),
                ModalSize.Large => new Vector2(800, 600),
                ModalSize.ExtraLarge => new Vector2(1000, 700),
                ModalSize.Fullscreen => new Vector2(Screen.width * 0.9f, Screen.height * 0.9f),
                ModalSize.Custom => _customSize,
                _ => new Vector2(500, 400)
            };
            
            _modalContainer.style.width = size.x;
            _modalContainer.style.height = size.y;
        }
        
        /// <summary>
        /// Setup event handlers for modal interactions
        /// </summary>
        protected virtual void SetupEventHandlers()
        {
            if (_closeOnBackdropClick && _backdropElement != null)
            {
                _backdropElement.RegisterCallback<ClickEvent>(OnBackdropClick);
            }
            
            if (_closeOnEscapeKey)
            {
                _rootElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
            
            // Focus management
            _rootElement.focusable = true;
            _rootElement.RegisterCallback<FocusInEvent>(OnFocusIn);
        }
        
        /// <summary>
        /// Handle backdrop click
        /// </summary>
        protected virtual void OnBackdropClick(ClickEvent evt)
        {
            if (evt.target == _backdropElement)
            {
                CloseModal(false);
            }
        }
        
        /// <summary>
        /// Handle key down events
        /// </summary>
        protected virtual void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                CloseModal(false);
                evt.StopPropagation();
            }
        }
        
        /// <summary>
        /// Handle focus management
        /// </summary>
        protected virtual void OnFocusIn(FocusInEvent evt)
        {
            // Keep focus within modal
            if (!_modalContainer.Contains(evt.target as VisualElement))
            {
                _modalContainer.Focus();
            }
        }
        
        /// <summary>
        /// Show the modal
        /// </summary>
        public virtual void ShowModal()
        {
            if (_isShowing || _isAnimating)
                return;
                
            _isShowing = true;
            SetVisible(true);
            
            if (_enableAnimation)
            {
                StartShowAnimation();
            }
            // else
            // {
                OnModalOpened?.Invoke(this);
            // }
            
            // Focus the modal
            _rootElement.Focus();
            
            LogInfo($"Modal '{_componentName}' shown");
        }
        
        /// <summary>
        /// Close the modal
        /// </summary>
        public virtual void CloseModal(bool confirmed = false)
        {
            if (!_isShowing || _isAnimating)
                return;
                
            _isShowing = false;
            
            if (_enableAnimation)
            {
                StartHideAnimation(confirmed);
            }
            // else
            // {
                CompleteHide(confirmed);
            // }
            
            LogInfo($"Modal '{_componentName}' closed (confirmed: {confirmed})");
        }
        
        /// <summary>
        /// Start show animation
        /// </summary>
        protected virtual void StartShowAnimation()
        {
            _isAnimating = true;
            
            // Set initial state
            _rootElement.style.opacity = 0;
            _modalContainer.style.scale = new Scale(Vector3.one * 0.8f);
            
            // Animate to final state using direct style assignment (Unity UI Toolkit compatible)
            _rootElement.style.opacity = 1;
            _modalContainer.style.scale = new Scale(Vector3.one);
            
            // Complete animation
            _rootElement.schedule.Execute(() =>
            {
                _isAnimating = false;
                OnModalOpened?.Invoke(this);
            }).ExecuteLater((long)(_fadeInDuration * 1000));
        }
        
        /// <summary>
        /// Start hide animation
        /// </summary>
        protected virtual void StartHideAnimation(bool confirmed)
        {
            _isAnimating = true;
            
            // Animate to hidden state using direct style assignment
            _rootElement.style.opacity = 0;
            _modalContainer.style.scale = new Scale(Vector3.one * 0.8f);
            
            // Complete animation
            _rootElement.schedule.Execute(() =>
            {
                CompleteHide(confirmed);
            }).ExecuteLater((long)(_fadeOutDuration * 1000));
        }
        
        /// <summary>
        /// Complete hide operation
        /// </summary>
        protected virtual void CompleteHide(bool confirmed)
        {
            _isAnimating = false;
            SetVisible(false);
            
            if (confirmed)
            {
                OnModalConfirmed?.Invoke(this);
            }
            // else
            // {
                OnModalCancelled?.Invoke(this);
            // }
            
            OnModalClosed?.Invoke(this);
        }
        
        /// <summary>
        /// Set modal title
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
        /// Add content to modal body
        /// </summary>
        public virtual void AddContent(VisualElement content)
        {
            _modalBody?.Add(content);
        }
        
        /// <summary>
        /// Clear modal body content
        /// </summary>
        public virtual void ClearContent()
        {
            _modalBody?.Clear();
        }
        
        /// <summary>
        /// Add button to modal footer
        /// </summary>
        public virtual Button AddFooterButton(string text, System.Action onClick, bool isPrimary = false)
        {
            var button = new Button(onClick);
            button.text = text;
            button.AddToClassList(isPrimary ? "modal-button-primary" : "modal-button-secondary");
            
            _modalFooter?.Add(button);
            return button;
        }
        
        protected override UIComponentPrefab CreateClone()
        {
            var clone = CreateModalClone();
            
            // Copy modal-specific properties
            var modalClone = clone as UIModalPrefab;
            if (modalClone != null)
            {
                modalClone._closeOnBackdropClick = _closeOnBackdropClick;
                modalClone._closeOnEscapeKey = _closeOnEscapeKey;
                modalClone._blockInteraction = _blockInteraction;
                modalClone._enableBackdrop = _enableBackdrop;
                modalClone._enableAnimation = _enableAnimation;
                modalClone._fadeInDuration = _fadeInDuration;
                modalClone._fadeOutDuration = _fadeOutDuration;
                modalClone._easingCurve = _easingCurve;
                modalClone._modalSize = _modalSize;
                modalClone._customSize = _customSize;
                modalClone._centerModal = _centerModal;
                modalClone._modalOffset = _modalOffset;
            }
            
            return clone;
        }
        
        /// <summary>
        /// Create modal-specific clone
        /// </summary>
        protected abstract UIModalPrefab CreateModalClone();
        
        protected override void UnbindEvents()
        {
            base.UnbindEvents();
            
            if (_backdropElement != null)
            {
                _backdropElement.UnregisterCallback<ClickEvent>(OnBackdropClick);
            }
            
            if (_rootElement != null)
            {
                _rootElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
                _rootElement.UnregisterCallback<FocusInEvent>(OnFocusIn);
            }
            
            if (_closeButton != null)
            {
                _closeButton.clicked -= () => CloseModal(false);
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            _fadeInDuration = Mathf.Max(0f, _fadeInDuration);
            _fadeOutDuration = Mathf.Max(0f, _fadeOutDuration);
            _customSize.x = Mathf.Max(100f, _customSize.x);
            _customSize.y = Mathf.Max(50f, _customSize.y);
        }
    }
    
    /// <summary>
    /// Modal size presets
    /// </summary>
    public enum ModalSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge,
        Fullscreen,
        Custom
    }
}