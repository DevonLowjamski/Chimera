using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Specialized UI component for notifications and alerts in Project Chimera.
    /// Provides notification-specific functionality like auto-dismiss, severity levels, and animations.
    /// </summary>
    public class UINotificationPrefab : UIComponentPrefab
    {
        [Header("Notification Configuration")]
        [SerializeField] protected NotificationSeverity _severity = NotificationSeverity.Info;
        [SerializeField] protected float _duration = 5f;
        [SerializeField] protected bool _autoDismiss = true;
        [SerializeField] protected bool _showCloseButton = true;
        [SerializeField] protected bool _showIcon = true;
        
        [Header("Notification Animation")]
        [SerializeField] protected bool _enableSlideAnimation = true;
        [SerializeField] protected float _slideInDuration = 0.3f;
        [SerializeField] protected float _slideOutDuration = 0.2f;
        [SerializeField] protected NotificationPosition _position = NotificationPosition.TopRight;
        
        [Header("Notification Content")]
        [SerializeField] protected string _title;
        [SerializeField] protected string _message;
        [SerializeField] protected string _actionText;
        [SerializeField] protected Texture2D _customIcon;
        
        protected VisualElement _notificationContainer;
        protected VisualElement _iconElement;
        protected Label _titleLabel;
        protected Label _messageLabel;
        protected Button _closeButton;
        protected Button _actionButton;
        protected ProgressBar _progressBar;
        protected float _startTime;
        protected bool _isDismissed = false;
        protected bool _isAnimating = false;
        
        // Notification Events
        public System.Action<UINotificationPrefab> OnNotificationShown;
        public System.Action<UINotificationPrefab> OnNotificationDismissed;
        public System.Action<UINotificationPrefab> OnNotificationClicked;
        public System.Action<UINotificationPrefab> OnActionClicked;
        
        // Properties
        public NotificationSeverity Severity => _severity;
        public float Duration => _duration;
        public bool AutoDismiss => _autoDismiss;
        public string Title => _title;
        public string Message => _message;
        public string ActionText => _actionText;
        public bool IsDismissed => _isDismissed;
        public float RemainingTime => _autoDismiss ? Mathf.Max(0, _duration - (Time.time - _startTime)) : _duration;
        public float Progress => _autoDismiss ? Mathf.Clamp01((Time.time - _startTime) / _duration) : 0f;
        
        protected override void SetupComponent()
        {
            CreateNotificationStructure();
            ApplyNotificationStyling();
            SetupNotificationContent();
            SetupEventHandlers();
            
            if (_autoDismiss)
            {
                _startTime = Time.time;
            }
        }
        
        /// <summary>
        /// Create the notification structure
        /// </summary>
        protected virtual void CreateNotificationStructure()
        {
            if (_rootElement == null)
                return;
                
            _rootElement.AddToClassList("notification");
            _rootElement.AddToClassList($"notification-{_severity.ToString().ToLower()}");
            
            _notificationContainer = new VisualElement();
            _notificationContainer.name = "notification-container";
            _notificationContainer.AddToClassList("notification-container");
            _rootElement.Add(_notificationContainer);
            
            // Create icon element
            if (_showIcon)
            {
                _iconElement = new VisualElement();
                _iconElement.name = "notification-icon";
                _iconElement.AddToClassList("notification-icon");
                _iconElement.AddToClassList($"icon-{_severity.ToString().ToLower()}");
                _notificationContainer.Add(_iconElement);
            }
            
            // Create content area
            var contentArea = new VisualElement();
            contentArea.name = "notification-content";
            contentArea.AddToClassList("notification-content");
            
            // Create title
            if (!string.IsNullOrEmpty(_title))
            {
                _titleLabel = new Label(_title);
                _titleLabel.name = "notification-title";
                _titleLabel.AddToClassList("notification-title");
                contentArea.Add(_titleLabel);
            }
            
            // Create message
            if (!string.IsNullOrEmpty(_message))
            {
                _messageLabel = new Label(_message);
                _messageLabel.name = "notification-message";
                _messageLabel.AddToClassList("notification-message");
                contentArea.Add(_messageLabel);
            }
            
            // Create action button
            if (!string.IsNullOrEmpty(_actionText))
            {
                _actionButton = new Button();
                _actionButton.name = "notification-action";
                _actionButton.AddToClassList("notification-action-button");
                _actionButton.text = _actionText;
                _actionButton.clicked += () => OnActionClicked?.Invoke(this);
                contentArea.Add(_actionButton);
            }
            
            _notificationContainer.Add(contentArea);
            
            // Create close button
            if (_showCloseButton)
            {
                _closeButton = new Button();
                _closeButton.name = "notification-close";
                _closeButton.AddToClassList("notification-close-button");
                _closeButton.text = "×";
                _closeButton.clicked += () => DismissNotification();
                _notificationContainer.Add(_closeButton);
            }
            
            // Create progress bar for auto-dismiss
            if (_autoDismiss && _duration > 0)
            {
                _progressBar = new ProgressBar();
                _progressBar.name = "notification-progress";
                _progressBar.AddToClassList("notification-progress");
                _progressBar.lowValue = 0;
                _progressBar.highValue = 1;
                _progressBar.value = 0;
                _rootElement.Add(_progressBar);
            }
        }
        
        /// <summary>
        /// Apply notification styling based on severity and position
        /// </summary>
        protected virtual void ApplyNotificationStyling()
        {
            if (_rootElement == null)
                return;
                
            // Apply severity styling
            _rootElement.AddToClassList($"severity-{_severity.ToString().ToLower()}");
            
            // Apply position styling
            _rootElement.AddToClassList($"position-{_position.ToString().ToLower()}");
            
            // Set custom icon if provided
            if (_customIcon != null && _iconElement != null)
            {
                _iconElement.style.backgroundImage = new StyleBackground(_customIcon);
            }
        }
        
        /// <summary>
        /// Setup notification content
        /// </summary>
        protected virtual void SetupNotificationContent()
        {
            // Default content is set during structure creation
            // Override in derived classes for specific content setup
        }
        
        /// <summary>
        /// Setup event handlers
        /// </summary>
        protected virtual void SetupEventHandlers()
        {
            _notificationContainer.RegisterCallback<ClickEvent>(OnNotificationClick);
        }
        
        /// <summary>
        /// Handle notification click
        /// </summary>
        protected virtual void OnNotificationClick(ClickEvent evt)
        {
            if (evt.target != _closeButton && evt.target != _actionButton)
            {
                OnNotificationClicked?.Invoke(this);
            }
        }
        
        protected override void UpdateComponentSpecific()
        {
            if (_isDismissed)
                return;
                
            // Update progress bar
            if (_progressBar != null && _autoDismiss)
            {
                _progressBar.value = Progress;
            }
            
            // Auto dismiss if time expired
            if (_autoDismiss && Time.time - _startTime >= _duration)
            {
                DismissNotification();
            }
        }
        
        /// <summary>
        /// Show the notification with animation
        /// </summary>
        public virtual void ShowNotification()
        {
            if (_isVisible)
                return;
                
            SetVisible(true);
            
            if (_enableSlideAnimation)
            {
                StartShowAnimation();
            }
            // else
            // {
                OnNotificationShown?.Invoke(this);
            // }
            
            LogInfo($"Notification shown: {_title ?? _message}");
        }
        
        /// <summary>
        /// Dismiss the notification with animation
        /// </summary>
        public virtual void DismissNotification()
        {
            if (_isDismissed || _isAnimating)
                return;
                
            _isDismissed = true;
            
            if (_enableSlideAnimation)
            {
                StartHideAnimation();
            }
            // else
            // {
                CompleteDismiss();
            // }
            
            LogInfo($"Notification dismissed: {_title ?? _message}");
        }
        
        /// <summary>
        /// Start show animation
        /// </summary>
        protected virtual void StartShowAnimation()
        {
            _isAnimating = true;
            
            Vector3 slideDirection = GetSlideDirection();
            
            // Set initial state
            _rootElement.style.translate = new Translate(new Length(slideDirection.x, LengthUnit.Pixel), 
                                                       new Length(slideDirection.y, LengthUnit.Pixel));
            _rootElement.style.opacity = 0;
            
            // Animate to final state - Use proper Unity UI Toolkit style assignments
            _rootElement.style.opacity = 0;
            _rootElement.style.translate = new Translate(new Length(slideDirection.x, LengthUnit.Pixel), 
                                                        new Length(slideDirection.y, LengthUnit.Pixel));
            
            // Schedule animation to final state
            _rootElement.schedule.Execute(() =>
            {
                _rootElement.style.opacity = 1;
                _rootElement.style.translate = new Translate(new Length(0), new Length(0));
            }).ExecuteLater((long)(_slideInDuration * 1000));
            
            // Complete animation
            _rootElement.schedule.Execute(() =>
            {
                _isAnimating = false;
                OnNotificationShown?.Invoke(this);
            }).ExecuteLater((long)(_slideInDuration * 1000));
        }
        
        /// <summary>
        /// Start hide animation
        /// </summary>
        protected virtual void StartHideAnimation()
        {
            _isAnimating = true;
            
            Vector3 slideDirection = GetSlideDirection();
            
            // Animate to hidden state - Use proper Unity UI Toolkit style assignments
            _rootElement.style.opacity = 1;
            _rootElement.style.translate = new Translate(new Length(0), new Length(0));
            
            // Schedule animation to hidden state
            _rootElement.schedule.Execute(() =>
            {
                _rootElement.style.opacity = 0;
                _rootElement.style.translate = new Translate(new Length(slideDirection.x, LengthUnit.Pixel), 
                                                            new Length(slideDirection.y, LengthUnit.Pixel));
            }).ExecuteLater(100);
            
            // Complete animation
            _rootElement.schedule.Execute(() =>
            {
                CompleteDismiss();
            }).ExecuteLater((long)(_slideOutDuration * 1000));
        }
        
        /// <summary>
        /// Complete dismiss operation
        /// </summary>
        protected virtual void CompleteDismiss()
        {
            _isAnimating = false;
            SetVisible(false);
            OnNotificationDismissed?.Invoke(this);
        }
        
        /// <summary>
        /// Get slide direction based on notification position
        /// </summary>
        protected virtual Vector3 GetSlideDirection()
        {
            return _position switch
            {
                NotificationPosition.TopLeft => new Vector3(-300, 0, 0),
                NotificationPosition.TopRight => new Vector3(300, 0, 0),
                NotificationPosition.TopCenter => new Vector3(0, -100, 0),
                NotificationPosition.BottomLeft => new Vector3(-300, 0, 0),
                NotificationPosition.BottomRight => new Vector3(300, 0, 0),
                NotificationPosition.BottomCenter => new Vector3(0, 100, 0),
                NotificationPosition.Center => new Vector3(0, -50, 0),
                _ => new Vector3(300, 0, 0)
            };
        }
        
        /// <summary>
        /// Set notification content
        /// </summary>
        public virtual void SetContent(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            _title = title;
            _message = message;
            _severity = severity;
            
            if (_titleLabel != null)
                _titleLabel.text = title;
            if (_messageLabel != null)
                _messageLabel.text = message;
                
            // Update styling
            _rootElement?.RemoveFromClassList($"notification-{_severity.ToString().ToLower()}");
            _rootElement?.AddToClassList($"notification-{severity.ToString().ToLower()}");
            _severity = severity;
        }
        
        /// <summary>
        /// Set action button
        /// </summary>
        public virtual void SetAction(string actionText, System.Action action)
        {
            _actionText = actionText;
            
            if (_actionButton != null)
            {
                _actionButton.text = actionText;
                _actionButton.clicked += () => action?.Invoke();
            }
        }
        
        /// <summary>
        /// Pause auto-dismiss timer
        /// </summary>
        public virtual void PauseTimer()
        {
            if (_autoDismiss)
            {
                _duration += Time.time - _startTime;
                _startTime = Time.time;
            }
        }
        
        /// <summary>
        /// Resume auto-dismiss timer
        /// </summary>
        public virtual void ResumeTimer()
        {
            if (_autoDismiss)
            {
                _startTime = Time.time;
            }
        }
        
        protected override UIComponentPrefab CreateClone()
        {
            var clone = Instantiate(this);
            
            // Copy notification-specific properties
            clone._severity = _severity;
            clone._duration = _duration;
            clone._autoDismiss = _autoDismiss;
            clone._showCloseButton = _showCloseButton;
            clone._showIcon = _showIcon;
            clone._enableSlideAnimation = _enableSlideAnimation;
            clone._slideInDuration = _slideInDuration;
            clone._slideOutDuration = _slideOutDuration;
            clone._position = _position;
            clone._title = _title;
            clone._message = _message;
            clone._actionText = _actionText;
            clone._customIcon = _customIcon;
            
            return clone;
        }
        
        protected override void UnbindEvents()
        {
            base.UnbindEvents();
            
            if (_notificationContainer != null)
            {
                _notificationContainer.UnregisterCallback<ClickEvent>(OnNotificationClick);
            }
            
            if (_closeButton != null)
            {
                _closeButton.clicked -= DismissNotification;
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            _duration = Mathf.Max(0f, _duration);
            _slideInDuration = Mathf.Max(0f, _slideInDuration);
            _slideOutDuration = Mathf.Max(0f, _slideOutDuration);
        }
    }
    
    /// <summary>
    /// Notification severity levels
    /// </summary>
    public enum NotificationSeverity
    {
        Info,
        Success,
        Warning,
        Error,
        Critical
    }
    
    /// <summary>
    /// Notification position options
    /// </summary>
    public enum NotificationPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center
    }
}