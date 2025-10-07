using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// SIMPLE: Basic notification display aligned with Project Chimera's UI needs.
    /// Focuses on essential notification showing without complex management systems.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        [Header("Basic Notification Settings")]
        [SerializeField] private bool _enableBasicNotifications = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _defaultDisplayTime = 3f;

        // Basic notification tracking
        private readonly List<NotificationData> _activeNotifications = new List<NotificationData>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for notification callbacks
        /// </summary>
        public event System.Action<NotificationData> OnNotificationShown;
        public event System.Action<NotificationData> OnNotificationHidden;

        /// <summary>
        /// Initialize basic notification system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("NotificationManager", "Basic notification system initialized");
            }
        }

        /// <summary>
        /// Show a basic notification
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 0f)
        {
            if (!_enableBasicNotifications) return;

            if (duration <= 0f)
            {
                duration = _defaultDisplayTime;
            }

            var notification = new NotificationData
            {
                Message = message,
                Type = type,
                Duration = duration,
                StartTime = Time.time
            };

            _activeNotifications.Add(notification);
            OnNotificationShown?.Invoke(notification);

            // In a real implementation, this would display the notification on screen
            // For now, just log it
            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("NotificationManager", $"Showing {type} notification: {message}");
            }

            // Auto-hide after duration
            StartCoroutine(HideNotificationAfterDelay(notification, duration));
        }

        /// <summary>
        /// Show info notification
        /// </summary>
        public void ShowInfo(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationType.Info, duration);
        }

        /// <summary>
        /// Show warning notification
        /// </summary>
        public void ShowWarning(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationType.Warning, duration);
        }

        /// <summary>
        /// Show error notification
        /// </summary>
        public void ShowError(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationType.Error, duration);
        }

        /// <summary>
        /// Show success notification
        /// </summary>
        public void ShowSuccess(string message, float duration = 0f)
        {
            ShowNotification(message, NotificationType.Success, duration);
        }

        /// <summary>
        /// Hide a notification
        /// </summary>
        public void HideNotification(NotificationData notification)
        {
            if (_activeNotifications.Remove(notification))
            {
                OnNotificationHidden?.Invoke(notification);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("NotificationManager", $"Hiding notification: {notification.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void ClearAllNotifications()
        {
            var notificationsToHide = new List<NotificationData>(_activeNotifications);
            _activeNotifications.Clear();

            foreach (var notification in notificationsToHide)
            {
                OnNotificationHidden?.Invoke(notification);
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("NotificationManager", "Cleared all notifications");
            }
        }

        /// <summary>
        /// Get active notification count
        /// </summary>
        public int GetActiveNotificationCount()
        {
            return _activeNotifications.Count;
        }

        /// <summary>
        /// Get active notifications
        /// </summary>
        public List<NotificationData> GetActiveNotifications()
        {
            return new List<NotificationData>(_activeNotifications);
        }

        /// <summary>
        /// Check if notifications are enabled
        /// </summary>
        public bool AreNotificationsEnabled()
        {
            return _enableBasicNotifications;
        }

        /// <summary>
        /// Set notifications enabled/disabled
        /// </summary>
        public void SetNotificationsEnabled(bool enabled)
        {
            _enableBasicNotifications = enabled;

            if (!enabled)
            {
                ClearAllNotifications();
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("NotificationManager", $"Notifications {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        public NotificationStatistics GetNotificationStatistics()
        {
            return new NotificationStatistics
            {
                ActiveNotifications = _activeNotifications.Count,
                IsInitialized = _isInitialized,
                NotificationsEnabled = _enableBasicNotifications,
                DefaultDisplayTime = _defaultDisplayTime
            };
        }

        #region Private Methods

        private System.Collections.IEnumerator HideNotificationAfterDelay(NotificationData notification, float delay)
        {
            yield return new WaitForSeconds(delay);
            HideNotification(notification);
        }

        #endregion
    }

    /// <summary>
    /// Notification type enum
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Basic notification data
    /// </summary>
    [System.Serializable]
    public class NotificationData
    {
        public string Message;
        public NotificationType Type;
        public float Duration;
        public float StartTime;
    }

    /// <summary>
    /// Notification statistics
    /// </summary>
    [System.Serializable]
    public class NotificationStatistics
    {
        public int ActiveNotifications;
        public bool IsInitialized;
        public bool NotificationsEnabled;
        public float DefaultDisplayTime;
    }
}
