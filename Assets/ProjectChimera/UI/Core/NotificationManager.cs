using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.UI.Components;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Enhanced centralized notification manager for Project Chimera.
    /// Manages notification display, queuing, positioning, and lifecycle.
    /// Provides persistent notifications, context-aware alerts, and smart queuing.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        [Header("Notification Configuration")]
        [SerializeField] private UIDocument _notificationUIDocument;
        [SerializeField] private int _maxConcurrentNotifications = 5;
        [SerializeField] private float _defaultDuration = 5f;
        [SerializeField] private bool _enablePersistentNotifications = true;
        [SerializeField] private bool _enableSoundEffects = true;
        [SerializeField] private bool _enableVibration = false;
        
        [Header("Notification Positioning")]
        [SerializeField] private NotificationPosition _defaultPosition = NotificationPosition.TopRight;
        [SerializeField] private float _notificationSpacing = 8f;
        [SerializeField] private float _maxNotificationWidth = 400f;
        [SerializeField] private float _animationDuration = 0.3f;
        
        [Header("Priority Management")]
        [SerializeField] private bool _enablePriorityQueuing = true;
        [SerializeField] private int _maxLowPriorityQueue = 10;
        [SerializeField] private int _maxHighPriorityQueue = 20;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _successSound;
        [SerializeField] private AudioClip _warningSound;
        [SerializeField] private AudioClip _errorSound;
        [SerializeField] private AudioClip _infoSound;
        
        // Internal state
        private VisualElement _notificationContainer;
        private Dictionary<NotificationPosition, VisualElement> _positionContainers;
        private List<ActiveNotification> _activeNotifications;
        private Queue<QueuedNotification> _lowPriorityQueue;
        private Queue<QueuedNotification> _highPriorityQueue;
        private Dictionary<string, PersistentNotification> _persistentNotifications;
        private AudioSource _audioSource;
        
        // Event tracking
        public System.Action<NotificationData> OnNotificationShown;
        public System.Action<NotificationData> OnNotificationDismissed;
        public System.Action<NotificationData> OnNotificationClicked;
        public System.Action<int> OnQueueSizeChanged;
        
        // Singleton access
        public static NotificationManager Instance { get; private set; }
        
        // Properties
        public int ActiveNotificationCount => _activeNotifications.Count;
        public int QueuedNotificationCount => _lowPriorityQueue.Count + _highPriorityQueue.Count;
        public bool HasPersistentNotifications => _persistentNotifications.Count > 0;
        public IReadOnlyList<ActiveNotification> ActiveNotifications => _activeNotifications.AsReadOnly();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeNotificationSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeNotificationSystem()
        {
            // Initialize collections
            _activeNotifications = new List<ActiveNotification>();
            _lowPriorityQueue = new Queue<QueuedNotification>();
            _highPriorityQueue = new Queue<QueuedNotification>();
            _persistentNotifications = new Dictionary<string, PersistentNotification>();
            _positionContainers = new Dictionary<NotificationPosition, VisualElement>();
            
            // Setup audio
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.volume = 0.5f;
            }
            
            // Setup UI
            SetupNotificationUI();
            
            Debug.Log("[NotificationManager] Enhanced notification system initialized");
        }
        
        private void SetupNotificationUI()
        {
            if (_notificationUIDocument == null)
            {
                _notificationUIDocument = GetComponent<UIDocument>();
                if (_notificationUIDocument == null)
                {
                    _notificationUIDocument = gameObject.AddComponent<UIDocument>();
                }
            }
            
            // Create main notification container
            _notificationContainer = new VisualElement();
            _notificationContainer.name = "notification-manager-root";
            _notificationContainer.style.position = Position.Absolute;
            _notificationContainer.style.top = 0;
            _notificationContainer.style.left = 0;
            _notificationContainer.style.right = 0;
            _notificationContainer.style.bottom = 0;
            _notificationContainer.pickingMode = PickingMode.Ignore;
            
            // Create position containers
            CreatePositionContainers();
            
            // Add to UI document
            if (_notificationUIDocument.rootVisualElement != null)
            {
                _notificationUIDocument.rootVisualElement.Add(_notificationContainer);
            }
        }
        
        private void CreatePositionContainers()
        {
            foreach (NotificationPosition position in System.Enum.GetValues(typeof(NotificationPosition)))
            {
                var container = new VisualElement();
                container.name = $"notifications-{position.ToString().ToLower()}";
                container.style.position = Position.Absolute;
                container.pickingMode = PickingMode.Position;
                
                // Set position-specific styles
                SetPositionContainerStyles(container, position);
                
                _positionContainers[position] = container;
                _notificationContainer.Add(container);
            }
        }
        
        private void SetPositionContainerStyles(VisualElement container, NotificationPosition position)
        {
            const float margin = 20f;
            
            switch (position)
            {
                case NotificationPosition.TopLeft:
                    container.style.top = margin;
                    container.style.left = margin;
                    container.style.flexDirection = FlexDirection.Column;
                    break;
                case NotificationPosition.TopCenter:
                    container.style.top = margin;
                    container.style.left = Length.Percent(50);
                    container.style.translate = new Translate(Length.Percent(-50), 0);
                    container.style.flexDirection = FlexDirection.Column;
                    break;
                case NotificationPosition.TopRight:
                    container.style.top = margin;
                    container.style.right = margin;
                    container.style.flexDirection = FlexDirection.Column;
                    break;
                case NotificationPosition.BottomLeft:
                    container.style.bottom = margin;
                    container.style.left = margin;
                    container.style.flexDirection = FlexDirection.ColumnReverse;
                    break;
                case NotificationPosition.BottomCenter:
                    container.style.bottom = margin;
                    container.style.left = Length.Percent(50);
                    container.style.translate = new Translate(Length.Percent(-50), 0);
                    container.style.flexDirection = FlexDirection.ColumnReverse;
                    break;
                case NotificationPosition.BottomRight:
                    container.style.bottom = margin;
                    container.style.right = margin;
                    container.style.flexDirection = FlexDirection.ColumnReverse;
                    break;
                case NotificationPosition.Center:
                    container.style.top = Length.Percent(50);
                    container.style.left = Length.Percent(50);
                    container.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                    container.style.flexDirection = FlexDirection.Column;
                    break;
            }
            
            container.style.maxWidth = _maxNotificationWidth;
        }
        
        private void Update()
        {
            ProcessNotificationQueue();
            UpdateActiveNotifications();
        }
        
        private void ProcessNotificationQueue()
        {
            // Process high priority queue first
            while (_highPriorityQueue.Count > 0 && _activeNotifications.Count < _maxConcurrentNotifications)
            {
                var queued = _highPriorityQueue.Dequeue();
                ShowNotificationInternal(queued.Data);
            }
            
            // Then process low priority queue
            while (_lowPriorityQueue.Count > 0 && _activeNotifications.Count < _maxConcurrentNotifications)
            {
                var queued = _lowPriorityQueue.Dequeue();
                ShowNotificationInternal(queued.Data);
            }
        }
        
        private void UpdateActiveNotifications()
        {
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = _activeNotifications[i];
                
                if (notification.ShouldAutoDismiss && Time.time >= notification.DismissTime)
                {
                    DismissNotification(notification.Id);
                }
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Show a simple notification with default settings
        /// </summary>
        public string ShowNotification(string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            return ShowNotification(new NotificationData
            {
                Message = message,
                Severity = severity,
                Duration = _defaultDuration,
                Position = _defaultPosition
            });
        }
        
        /// <summary>
        /// Show a notification with title and message
        /// </summary>
        public string ShowNotification(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, float duration = -1f)
        {
            return ShowNotification(new NotificationData
            {
                Title = title,
                Message = message,
                Severity = severity,
                Duration = duration > 0 ? duration : _defaultDuration,
                Position = _defaultPosition
            });
        }
        
        /// <summary>
        /// Show a notification with full configuration
        /// </summary>
        public string ShowNotification(NotificationData data)
        {
            data.Id = string.IsNullOrEmpty(data.Id) ? System.Guid.NewGuid().ToString() : data.Id;
            data.CreationTime = Time.time;
            
            // Handle priority queuing
            if (_enablePriorityQueuing && _activeNotifications.Count >= _maxConcurrentNotifications)
            {
                var queuedNotification = new QueuedNotification { Data = data, QueueTime = Time.time };
                
                if (data.Priority == NotificationPriority.High || data.Priority == NotificationPriority.Critical)
                {
                    if (_highPriorityQueue.Count < _maxHighPriorityQueue)
                    {
                        _highPriorityQueue.Enqueue(queuedNotification);
                        OnQueueSizeChanged?.Invoke(QueuedNotificationCount);
                        return data.Id;
                    }
                }
                else
                {
                    if (_lowPriorityQueue.Count < _maxLowPriorityQueue)
                    {
                        _lowPriorityQueue.Enqueue(queuedNotification);
                        OnQueueSizeChanged?.Invoke(QueuedNotificationCount);
                        return data.Id;
                    }
                }
                
                // If queues are full, dismiss oldest low priority notification
                DismissOldestLowPriorityNotification();
            }
            
            return ShowNotificationInternal(data);
        }
        
        /// <summary>
        /// Show a persistent notification that doesn't auto-dismiss
        /// </summary>
        public string ShowPersistentNotification(string key, string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            if (!_enablePersistentNotifications)
                return null;
                
            var data = new NotificationData
            {
                Id = key,
                Message = message,
                Severity = severity,
                Duration = 0, // Persistent
                Position = _defaultPosition,
                Priority = NotificationPriority.High
            };
            
            // Remove existing persistent notification with same key
            if (_persistentNotifications.ContainsKey(key))
            {
                DismissNotification(key);
            }
            
            var id = ShowNotificationInternal(data);
            _persistentNotifications[key] = new PersistentNotification { Key = key, Id = id, Data = data };
            
            return id;
        }
        
        /// <summary>
        /// Dismiss a specific notification
        /// </summary>
        public bool DismissNotification(string notificationId)
        {
            var notification = _activeNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification == null)
                return false;
                
            // Remove from active list
            _activeNotifications.Remove(notification);
            
            // Remove from persistent tracking
            var persistentKey = _persistentNotifications.FirstOrDefault(p => p.Value.Id == notificationId).Key;
            if (!string.IsNullOrEmpty(persistentKey))
            {
                _persistentNotifications.Remove(persistentKey);
            }
            
            // Animate out and remove
            if (notification.UIElement != null)
            {
                AnimateNotificationOut(notification.UIElement, () =>
                {
                    notification.UIElement.RemoveFromHierarchy();
                });
            }
            
            OnNotificationDismissed?.Invoke(notification.Data);
            return true;
        }
        
        /// <summary>
        /// Dismiss all notifications of a specific severity
        /// </summary>
        public int DismissNotificationsBySeverity(NotificationSeverity severity)
        {
            var toDismiss = _activeNotifications.Where(n => n.Data.Severity == severity).ToList();
            foreach (var notification in toDismiss)
            {
                DismissNotification(notification.Id);
            }
            return toDismiss.Count;
        }
        
        /// <summary>
        /// Dismiss all notifications
        /// </summary>
        public void DismissAllNotifications()
        {
            var allNotifications = _activeNotifications.ToList();
            foreach (var notification in allNotifications)
            {
                DismissNotification(notification.Id);
            }
        }
        
        /// <summary>
        /// Update a persistent notification
        /// </summary>
        public bool UpdatePersistentNotification(string key, string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            if (!_persistentNotifications.ContainsKey(key))
                return false;
                
            DismissNotification(_persistentNotifications[key].Id);
            ShowPersistentNotification(key, message, severity);
            return true;
        }
        
        #endregion
        
        #region Internal Implementation
        
        private string ShowNotificationInternal(NotificationData data)
        {
            // Create notification UI element
            var notificationElement = CreateNotificationElement(data);
            
            // Add to appropriate position container
            var positionContainer = _positionContainers[data.Position];
            positionContainer.Add(notificationElement);
            
            // Create active notification record
            var activeNotification = new ActiveNotification
            {
                Id = data.Id,
                Data = data,
                UIElement = notificationElement,
                ShowTime = Time.time,
                DismissTime = data.Duration > 0 ? Time.time + data.Duration : float.MaxValue,
                ShouldAutoDismiss = data.Duration > 0
            };
            
            _activeNotifications.Add(activeNotification);
            
            // Animate in
            AnimateNotificationIn(notificationElement);
            
            // Play sound
            PlayNotificationSound(data.Severity);
            
            OnNotificationShown?.Invoke(data);
            return data.Id;
        }
        
        private UINotificationToast CreateNotificationElement(NotificationData data)
        {
            var severity = ConvertSeverityToUIStatus(data.Severity);
            var notification = new UINotificationToast(data.Message, severity, () =>
            {
                DismissNotification(data.Id);
            });
            
            // Configure notification element
            notification.style.maxWidth = _maxNotificationWidth;
            notification.style.marginBottom = _notificationSpacing;
            notification.style.opacity = 0; // Start invisible for animation
            
            // Add click handler
            notification.RegisterCallback<ClickEvent>(evt =>
            {
                OnNotificationClicked?.Invoke(data);
                if (data.OnClick != null)
                {
                    data.OnClick.Invoke();
                }
            });
            
            return notification;
        }
        
        private void AnimateNotificationIn(VisualElement element)
        {
            element.style.opacity = 0;
            element.style.translate = new Translate(Length.Percent(100), 0);
            
            element.schedule.Execute(() =>
            {
                element.style.opacity = 1;
                element.style.translate = new Translate(0, 0);
            }).ExecuteLater(50);
        }
        
        private void AnimateNotificationOut(VisualElement element, System.Action onComplete)
        {
            element.style.opacity = 1;
            element.style.translate = new Translate(0, 0);
            
            element.schedule.Execute(() =>
            {
                element.style.opacity = 0;
                element.style.translate = new Translate(Length.Percent(100), 0);
            }).ExecuteLater(50);
            
            element.schedule.Execute(() =>
            {
                onComplete?.Invoke();
            }).ExecuteLater((long)(_animationDuration * 1000));
        }
        
        private void DismissOldestLowPriorityNotification()
        {
            var oldestLowPriority = _activeNotifications
                .Where(n => n.Data.Priority == NotificationPriority.Low)
                .OrderBy(n => n.ShowTime)
                .FirstOrDefault();
                
            if (oldestLowPriority != null)
            {
                DismissNotification(oldestLowPriority.Id);
            }
        }
        
        private void PlayNotificationSound(NotificationSeverity severity)
        {
            if (!_enableSoundEffects || _audioSource == null)
                return;
                
            AudioClip clip = severity switch
            {
                NotificationSeverity.Success => _successSound,
                NotificationSeverity.Warning => _warningSound,
                NotificationSeverity.Error => _errorSound,
                NotificationSeverity.Critical => _errorSound,
                _ => _infoSound
            };
            
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
        
        private UIStatus ConvertSeverityToUIStatus(NotificationSeverity severity)
        {
            return severity switch
            {
                NotificationSeverity.Success => UIStatus.Success,
                NotificationSeverity.Warning => UIStatus.Warning,
                NotificationSeverity.Error => UIStatus.Error,
                NotificationSeverity.Critical => UIStatus.Error,
                _ => UIStatus.Info
            };
        }
        
        #endregion
        
    }
    
    #region Data Structures
    
    [System.Serializable]
    public class NotificationData
    {
        public string Id;
        public string Title;
        public string Message;
        public NotificationSeverity Severity = NotificationSeverity.Info;
        public NotificationPriority Priority = NotificationPriority.Normal;
        public NotificationPosition Position = NotificationPosition.TopRight;
        public float Duration = 5f;
        public float CreationTime;
        public string ActionText;
        public System.Action OnClick;
        public System.Action OnAction;
        public Texture2D CustomIcon;
    }
    
    public class ActiveNotification
    {
        public string Id;
        public NotificationData Data;
        public UINotificationToast UIElement;
        public float ShowTime;
        public float DismissTime;
        public bool ShouldAutoDismiss;
    }
    
    public class QueuedNotification
    {
        public NotificationData Data;
        public float QueueTime;
    }
    
    public class PersistentNotification
    {
        public string Key;
        public string Id;
        public NotificationData Data;
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    
    #endregion
}