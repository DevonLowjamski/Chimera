using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Core
{
    /// <summary>
    /// REFACTORED: UI Notification Manager
    /// Single Responsibility: Notification display, queuing, and lifecycle management
    /// Extracted from OptimizedUIManager for better separation of concerns
    /// </summary>
    public class UINotificationManager : MonoBehaviour
    {
        [Header("Notification Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxConcurrentNotifications = 5;
        [SerializeField] private float _defaultDuration = 3f;
        [SerializeField] private Transform _notificationParent;

        // Notification queue and active notifications
        private readonly Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private readonly List<NotificationDisplay> _activeNotifications = new List<NotificationDisplay>();

        // Pooling support
        private UIElementPoolManager _poolManager;
        private bool _isInitialized = false;

        // Statistics
        private UINotificationStats _stats = new UINotificationStats();

        // Events
        public event System.Action<NotificationData> OnNotificationShown;
        public event System.Action<NotificationData> OnNotificationHidden;

        public bool IsInitialized => _isInitialized;
        public UINotificationStats Stats => _stats;
        public int QueuedCount => _notificationQueue.Count;
        public int ActiveCount => _activeNotifications.Count;

        public void Initialize(UIElementPoolManager poolManager = null)
        {
            if (_isInitialized) return;

            _poolManager = poolManager;
            _notificationQueue.Clear();
            _activeNotifications.Clear();
            ResetStats();

            // Setup notification parent if not assigned
            if (_notificationParent == null)
            {
                var notificationCanvas = GameObject.Find("NotificationCanvas");
                if (notificationCanvas != null)
                {
                    _notificationParent = notificationCanvas.transform;
                }
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "UI Notification Manager initialized", this);
            }
        }

        /// <summary>
        /// Show a notification message
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot show notification: message is null or empty", this);
                }
                return;
            }

            var actualDuration = duration > 0 ? duration : _defaultDuration;

            var notification = new NotificationData
            {
                Message = message,
                Type = type,
                Duration = actualDuration,
                Timestamp = Time.unscaledTime
            };

            QueueNotification(notification);
        }

        /// <summary>
        /// Show notification with rich data
        /// </summary>
        public void ShowNotification(NotificationData notification)
        {
            if (notification == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("UI", "Cannot show notification: notification data is null", this);
                }
                return;
            }

            QueueNotification(notification);
        }

        /// <summary>
        /// Queue notification for display
        /// </summary>
        private void QueueNotification(NotificationData notification)
        {
            _notificationQueue.Enqueue(notification);
            _stats.TotalQueued++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Queued notification: {notification.Message} ({_notificationQueue.Count} in queue)", this);
            }
        }

        /// <summary>
        /// Process notification queue (call from Update or ITickable)
        /// </summary>
        public void ProcessNotifications()
        {
            // Clean up expired notifications
            CleanupExpiredNotifications();

            // Show new notifications if we have capacity
            while (_notificationQueue.Count > 0 && _activeNotifications.Count < _maxConcurrentNotifications)
            {
                var notification = _notificationQueue.Dequeue();
                DisplayNotification(notification);
            }
        }

        /// <summary>
        /// Display individual notification
        /// </summary>
        private void DisplayNotification(NotificationData notification)
        {
            GameObject notificationGO = null;

            // Try to get from pool first
            if (_poolManager != null)
            {
                var pooledElement = _poolManager.GetPooledElement<RectTransform>();
                if (pooledElement != null)
                {
                    notificationGO = pooledElement.gameObject;
                }
            }

            // Create new if pooling failed
            if (notificationGO == null)
            {
                notificationGO = CreateNotificationGameObject();
            }

            var notificationDisplay = notificationGO.GetComponent<NotificationDisplay>();
            if (notificationDisplay == null)
            {
                notificationDisplay = notificationGO.AddComponent<NotificationDisplay>();
            }

            // Setup the notification
            notificationDisplay.Setup(notification, _notificationParent);
            _activeNotifications.Add(notificationDisplay);

            // Update statistics
            _stats.TotalDisplayed++;
            _stats.CurrentlyVisible++;

            // Notify listeners
            OnNotificationShown?.Invoke(notification);

            // Start auto-hide coroutine
            StartCoroutine(HideNotificationAfterDelay(notificationDisplay, notification.Duration));

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Displayed notification: {notification.Message} ({_activeNotifications.Count} active)", this);
            }
        }

        /// <summary>
        /// Create notification GameObject
        /// </summary>
        private GameObject CreateNotificationGameObject()
        {
            var notificationGO = new GameObject("Notification");
            var rectTransform = notificationGO.AddComponent<RectTransform>();

            // Set basic properties
            if (_notificationParent != null)
            {
                rectTransform.SetParent(_notificationParent);
            }

            return notificationGO;
        }

        /// <summary>
        /// Hide notification after delay
        /// </summary>
        private IEnumerator HideNotificationAfterDelay(NotificationDisplay notification, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (notification != null)
            {
                HideNotification(notification);
            }
        }

        /// <summary>
        /// Hide specific notification
        /// </summary>
        public void HideNotification(NotificationDisplay notification)
        {
            if (notification == null) return;

            _activeNotifications.Remove(notification);
            _stats.CurrentlyVisible--;

            // Notify listeners before cleanup
            OnNotificationHidden?.Invoke(notification.NotificationData);

            // Return to pool or destroy
            if (_poolManager != null)
            {
                _poolManager.ReturnPooledElement(notification.GetComponent<RectTransform>());
            }
            else
            {
                Destroy(notification.gameObject);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", $"Hidden notification ({_activeNotifications.Count} remaining active)", this);
            }
        }

        /// <summary>
        /// Clean up expired notifications
        /// </summary>
        private void CleanupExpiredNotifications()
        {
            var currentTime = Time.unscaledTime;
            var toRemove = new List<NotificationDisplay>();

            foreach (var notification in _activeNotifications)
            {
                if (notification == null ||
                    (notification.NotificationData != null &&
                     currentTime - notification.NotificationData.Timestamp > notification.NotificationData.Duration + 0.1f))
                {
                    toRemove.Add(notification);
                }
            }

            foreach (var notification in toRemove)
            {
                HideNotification(notification);
            }
        }

        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void ClearAllNotifications()
        {
            // Clear queue
            _notificationQueue.Clear();

            // Hide active notifications
            var activeToHide = new List<NotificationDisplay>(_activeNotifications);
            foreach (var notification in activeToHide)
            {
                HideNotification(notification);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "All notifications cleared", this);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new UINotificationStats
            {
                TotalQueued = 0,
                TotalDisplayed = 0,
                CurrentlyVisible = 0,
                LastUpdateTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllNotifications();
        }
    }

    /// <summary>
    /// Notification data structure
    /// </summary>
    [System.Serializable]
    public class NotificationData
    {
        public string Message;
        public NotificationType Type;
        public float Duration;
        public float Timestamp;
        public object CustomData;
    }

    /// <summary>
    /// Notification types (canonical)
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success,
        Achievement
    }

    /// <summary>
    /// Notification statistics
    /// </summary>
    [System.Serializable]
    public struct UINotificationStats
    {
        public int TotalQueued;
        public int TotalDisplayed;
        public int CurrentlyVisible;
        public float LastUpdateTime;
    }

    /// <summary>
    /// Notification display component
    /// </summary>
    public class NotificationDisplay : MonoBehaviour
    {
        public NotificationData NotificationData { get; private set; }

        public void Setup(NotificationData data, Transform parent)
        {
            NotificationData = data;
            transform.SetParent(parent);
        }
    }
}
