using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core.Memory;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Core;
using NotificationType = ProjectChimera.Systems.UI.Components.NotificationType;
using NotificationData = ProjectChimera.Systems.UI.Components.NotificationData;
using NotificationElement = ProjectChimera.Systems.UI.Components.NotificationElement;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// PERFORMANCE: Optimized notification display system for cultivation alerts
    /// Uses pooling and efficient animations to minimize UI overhead
    /// Week 12: Input & UI Performance
    /// </summary>
    public class NotificationDisplay : MonoBehaviour, IUIUpdatable
    {
        [Header("Notification Settings")]
        [SerializeField] private int _maxVisibleNotifications = 5;
        [SerializeField] private float _defaultDisplayDuration = 3f;
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _notificationSpacing = 10f;

        [Header("Notification Appearance")]
        [SerializeField] private Vector2 _notificationSize = new Vector2(300, 60);
        [SerializeField] private Color _infoColor = Color.blue;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _successColor = Color.green;

        // Notification management
        private readonly Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private readonly List<NotificationElement> _activeNotifications = new List<NotificationElement>();
        private readonly Stack<NotificationElement> _notificationPool = new Stack<NotificationElement>();

        // Layout management
        private RectTransform _rectTransform;
        private VerticalLayoutGroup _layoutGroup;
        private ContentSizeFitter _sizeFitter;

        // Performance tracking
        private int _totalNotifications = 0;
        private int _pooledElements = 0;
        private bool _needsLayoutUpdate = false;

        // Update optimization
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f;

        public int ActiveNotificationCount => _activeNotifications.Count;
        public int QueuedNotificationCount => _notificationQueue.Count;

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Show notification with specified type and duration
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message)) return;

            var notification = new NotificationData
            {
                Id = System.Guid.NewGuid().ToString(),
                Message = StringOptimizer.Intern(message),
                Type = type,
                Duration = duration > 0 ? duration : _defaultDisplayDuration,
                Timestamp = Time.unscaledTime,
                Priority = GetNotificationPriority(type)
            };

            QueueNotification(notification);
        }

        /// <summary>
        /// Show plant-specific notification
        /// </summary>
        public void ShowPlantNotification(string plantId, string message, NotificationType type = NotificationType.Info)
        {
            string formattedMessage = StringOptimizer.Format("Plant {0}: {1}", plantId, message);
            ShowNotification(formattedMessage, type);
        }

        /// <summary>
        /// Show system notification with icon
        /// </summary>
        public void ShowSystemNotification(string system, string message, NotificationType type = NotificationType.Info)
        {
            string formattedMessage = StringOptimizer.Format("[{0}] {1}", system, message);
            ShowNotification(formattedMessage, type);
        }

        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void ClearAllNotifications()
        {
            foreach (var notification in _activeNotifications)
            {
                StartCoroutine(HideNotification(notification, true));
            }

            _notificationQueue.Clear();
        }

        /// <summary>
        /// Clear notifications of specific type
        /// </summary>
        public void ClearNotificationsByType(NotificationType type)
        {
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                if (_activeNotifications[i].Data.Type == type)
                {
                    StartCoroutine(HideNotification(_activeNotifications[i], true));
                }
            }
        }

        /// <summary>
        /// Check if should update
        /// </summary>
        public bool ShouldUpdate()
        {
            float currentTime = Time.unscaledTime;
            return currentTime - _lastUpdateTime >= UPDATE_INTERVAL &&
                   (_notificationQueue.Count > 0 || _needsLayoutUpdate || HasExpiredNotifications());
        }

        /// <summary>
        /// Update UI display
        /// </summary>
        public void UpdateUI(float deltaTime = 0f)
        {
            ProcessNotificationQueue();
            UpdateActiveNotifications();
            UpdateLayout();

            _lastUpdateTime = Time.unscaledTime;
            _needsLayoutUpdate = false;
        }

        #region Private Methods

        /// <summary>
        /// Initialize UI components
        /// </summary>
        private void InitializeComponents()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Set up layout group
            _layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (_layoutGroup == null)
            {
                _layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            }

            _layoutGroup.spacing = _notificationSpacing;
            _layoutGroup.childControlHeight = false;
            _layoutGroup.childControlWidth = false;
            _layoutGroup.childForceExpandHeight = false;
            _layoutGroup.childForceExpandWidth = false;
            _layoutGroup.childAlignment = TextAnchor.UpperCenter;

            // Set up content size fitter
            _sizeFitter = GetComponent<ContentSizeFitter>();
            if (_sizeFitter == null)
            {
                _sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            }

            _sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Pre-populate notification pool
            InitializeNotificationPool();
        }

        /// <summary>
        /// Initialize notification element pool
        /// </summary>
        private void InitializeNotificationPool()
        {
            for (int i = 0; i < _maxVisibleNotifications; i++)
            {
                var element = CreateNotificationElement();
                element.GameObject.SetActive(false);
                _notificationPool.Push(element);
                _pooledElements++;
            }
        }

        /// <summary>
        /// Create a new notification element
        /// </summary>
        private NotificationElement CreateNotificationElement()
        {
            var notificationGO = new GameObject("NotificationElement");
            notificationGO.transform.SetParent(transform, false);

            var rectTransform = notificationGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _notificationSize;

            // Background
            var backgroundImage = notificationGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(notificationGO.transform, false);

            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(10, 5);
            textRT.offsetMax = new Vector2(-10, -5);

            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.fontSize = 12;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.color = Color.white;
            textComponent.enableWordWrapping = true;

            // Canvas group for fading
            var canvasGroup = notificationGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            return new NotificationElement
            {
                GameObject = notificationGO,
                RectTransform = rectTransform,
                BackgroundImage = backgroundImage,
                TextComponent = textComponent,
                CanvasGroup = canvasGroup
            };
        }

        /// <summary>
        /// Queue notification for display
        /// </summary>
        private void QueueNotification(NotificationData notification)
        {
            _notificationQueue.Enqueue(notification);
            _totalNotifications++;
        }

        /// <summary>
        /// Process queued notifications
        /// </summary>
        private void ProcessNotificationQueue()
        {
            while (_notificationQueue.Count > 0 && _activeNotifications.Count < _maxVisibleNotifications)
            {
                var notification = _notificationQueue.Dequeue();
                ShowNotificationElement(notification);
            }
        }

        /// <summary>
        /// Show notification element
        /// </summary>
        private void ShowNotificationElement(NotificationData data)
        {
            var element = GetPooledNotificationElement();
            if (element == null) return;

            // Configure element
            element.Data = data;
            element.TextComponent.text = data.Message;
            element.BackgroundImage.color = GetNotificationColor(data.Type);
            element.GameObject.SetActive(true);

            _activeNotifications.Add(element);
            _needsLayoutUpdate = true;

            // Start show animation
            StartCoroutine(ShowNotificationAnimation(element));

            // Schedule hide
            StartCoroutine(HideNotificationAfterDelay(element, data.Duration));
        }

        /// <summary>
        /// Get pooled notification element
        /// </summary>
        private NotificationElement GetPooledNotificationElement()
        {
            if (_notificationPool.Count > 0)
            {
                _pooledElements--;
                return _notificationPool.Pop();
            }

            // Create new element if pool is empty
            return CreateNotificationElement();
        }

        /// <summary>
        /// Return notification element to pool
        /// </summary>
        private void ReturnToPool(NotificationElement element)
        {
            element.GameObject.SetActive(false);
            element.CanvasGroup.alpha = 0f;
            _notificationPool.Push(element);
            _pooledElements++;
        }

        /// <summary>
        /// Show notification animation
        /// </summary>
        private IEnumerator ShowNotificationAnimation(NotificationElement element)
        {
            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInDuration);
                element.CanvasGroup.alpha = alpha;
                yield return null;
            }
            element.CanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Hide notification after delay
        /// </summary>
        private IEnumerator HideNotificationAfterDelay(NotificationElement element, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (_activeNotifications.Contains(element))
            {
                StartCoroutine(HideNotification(element));
            }
        }

        /// <summary>
        /// Hide notification animation
        /// </summary>
        private IEnumerator HideNotification(NotificationElement element, bool immediate = false)
        {
            if (!immediate)
            {
                float elapsed = 0f;
                float startAlpha = element.CanvasGroup.alpha;

                while (elapsed < _fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);
                    element.CanvasGroup.alpha = alpha;
                    yield return null;
                }
            }

            _activeNotifications.Remove(element);
            ReturnToPool(element);
            _needsLayoutUpdate = true;
        }

        /// <summary>
        /// Update active notifications
        /// </summary>
        private void UpdateActiveNotifications()
        {
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = _activeNotifications[i];
                float elapsed = Time.unscaledTime - notification.Data.Timestamp;

                if (elapsed > notification.Data.Duration)
                {
                    StartCoroutine(HideNotification(notification));
                }
            }
        }

        /// <summary>
        /// Check for expired notifications
        /// </summary>
        private bool HasExpiredNotifications()
        {
            float currentTime = Time.unscaledTime;
            foreach (var notification in _activeNotifications)
            {
                if (currentTime - notification.Data.Timestamp > notification.Data.Duration)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Update layout if needed
        /// </summary>
        private void UpdateLayout()
        {
            if (_needsLayoutUpdate)
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        /// <summary>
        /// Get notification color by type
        /// </summary>
        private Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => _infoColor,
                NotificationType.Warning => _warningColor,
                NotificationType.Error => _errorColor,
                NotificationType.Success => _successColor,
                _ => _infoColor
            };
        }

        /// <summary>
        /// Get notification priority by type
        /// </summary>
        private int GetNotificationPriority(NotificationType type)
        {
            return type switch
            {
                NotificationType.Error => 3,
                NotificationType.Warning => 2,
                NotificationType.Success => 1,
                NotificationType.Info => 0,
                _ => 0
            };
        }

        #endregion

        private void OnDestroy()
        {
            _notificationQueue.Clear();
            _activeNotifications.Clear();

            while (_notificationPool.Count > 0)
            {
                var element = _notificationPool.Pop();
                if (element.GameObject != null)
                {
                    DestroyImmediate(element.GameObject);
                }
            }
        }
    }
}
