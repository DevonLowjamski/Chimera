using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Systems.UI.Advanced;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Visual Feedback System for Advanced Menu System
    /// Provides notifications, tooltips, progress indicators, and visual cues
    /// </summary>
    public class VisualFeedbackSystem : MonoBehaviour, IVisualFeedbackSystem
    {
        [Header("Feedback Configuration")]
        [SerializeField] private bool _enableFeedback = true;
        [SerializeField] private float _defaultFeedbackDuration = 3f;
        [SerializeField] private int _maxConcurrentFeedbacks = 5;
        
        [Header("Notification Settings")]
        [SerializeField] private Vector2 _notificationSize = new Vector2(300f, 60f);
        [SerializeField] private Vector2 _notificationOffset = new Vector2(20f, 20f);
        [SerializeField] private float _notificationSpacing = 10f;
        
        [Header("Tooltip Settings")]
        [SerializeField] private Vector2 _tooltipMaxSize = new Vector2(250f, 100f);
        [SerializeField] private Vector2 _tooltipOffset = new Vector2(10f, -10f);
        [SerializeField] private float _tooltipDelay = 0.5f;
        
        [Header("Progress Settings")]
        [SerializeField] private Vector2 _progressBarSize = new Vector2(200f, 20f);
        [SerializeField] private Color _progressColor = Color.green;
        [SerializeField] private Color _progressBackgroundColor = Color.gray;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private AnimationCurve _feedbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // UI Elements
        private VisualElement _feedbackContainer;
        private VisualElement _tooltipContainer;
        private VisualElement _progressContainer;
        
        // Active feedbacks
        private List<FeedbackElement> _activeFeedbacks = new List<FeedbackElement>();
        private FeedbackElement _currentTooltip;
        private FeedbackElement _currentProgress;
        
        // Feedback queue
        private Queue<FeedbackRequest> _feedbackQueue = new Queue<FeedbackRequest>();
        
        private void Awake()
        {
            InitializeFeedbackSystem();
        }
        
        private void Start()
        {
            SetupFeedbackUI();
            StartCoroutine(ProcessFeedbackQueue());
        }
        
        private void InitializeFeedbackSystem()
        {
            if (!_enableFeedback)
            {
                gameObject.SetActive(false);
                return;
            }
        }
        
        private void SetupFeedbackUI()
        {
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument == null)
            {
                ChimeraLogger.LogError("[VisualFeedbackSystem] UIDocument component required");
                return;
            }
            
            var root = rootDocument.rootVisualElement;
            
            // Create feedback containers
            _feedbackContainer = new VisualElement();
            _feedbackContainer.name = "feedback-container";
            _feedbackContainer.AddToClassList("feedback-container");
            _feedbackContainer.style.position = Position.Absolute;
            _feedbackContainer.style.top = _notificationOffset.y;
            _feedbackContainer.style.right = _notificationOffset.x;
            root.Add(_feedbackContainer);
            
            _tooltipContainer = new VisualElement();
            _tooltipContainer.name = "tooltip-container";
            _tooltipContainer.AddToClassList("tooltip-container");
            _tooltipContainer.style.position = Position.Absolute;
            _tooltipContainer.style.display = DisplayStyle.None;
            root.Add(_tooltipContainer);
            
            _progressContainer = new VisualElement();
            _progressContainer.name = "progress-container";
            _progressContainer.AddToClassList("progress-container");
            _progressContainer.style.position = Position.Absolute;
            _progressContainer.style.bottom = 50f;
            _progressContainer.style.left = Length.Percent(50);
            _progressContainer.style.translate = new Translate(Length.Percent(-50), 0);
            _progressContainer.style.display = DisplayStyle.None;
            root.Add(_progressContainer);
        }
        
        /// <summary>
        /// Show feedback message with specified type and duration
        /// </summary>
        public void ShowFeedback(string message, FeedbackType type, float duration = 3f)
        {
            if (!_enableFeedback || string.IsNullOrEmpty(message))
                return;
            
            var request = new FeedbackRequest
            {
                Message = message,
                Type = type,
                Duration = duration > 0 ? duration : _defaultFeedbackDuration,
                Timestamp = Time.time
            };
            
            _feedbackQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Show progress indicator with message and progress value
        /// </summary>
        public void ShowProgress(string message, float progress)
        {
            if (!_enableFeedback)
                return;
            
            if (_currentProgress != null)
            {
                UpdateProgress(message, progress);
            }
            else
            {
                CreateProgress(message, progress);
            }
        }
        
        /// <summary>
        /// Hide all feedback elements
        /// </summary>
        public void HideFeedback()
        {
            foreach (var feedback in _activeFeedbacks)
            {
                if (feedback.Coroutine != null)
                {
                    StopCoroutine(feedback.Coroutine);
                }
                feedback.Element.RemoveFromHierarchy();
            }
            
            _activeFeedbacks.Clear();
        }
        
        /// <summary>
        /// Show tooltip at world position
        /// </summary>
        public void ShowTooltip(string text, Vector3 worldPosition)
        {
            if (!_enableFeedback || string.IsNullOrEmpty(text))
                return;
            
            HideTooltip();
            
            var screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            var uiPosition = RuntimePanelUtils.ScreenToPanel(
                _tooltipContainer.panel,
                new Vector2(screenPosition.x, Screen.height - screenPosition.y)
            );
            
            CreateTooltip(text, uiPosition);
        }
        
        /// <summary>
        /// Hide current tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (_currentTooltip != null)
            {
                if (_currentTooltip.Coroutine != null)
                {
                    StopCoroutine(_currentTooltip.Coroutine);
                }
                _currentTooltip.Element.RemoveFromHierarchy();
                _currentTooltip = null;
            }
        }
        
        private void CreateFeedback(FeedbackRequest request)
        {
            if (_activeFeedbacks.Count >= _maxConcurrentFeedbacks)
            {
                RemoveOldestFeedback();
            }
            
            var feedbackElement = new VisualElement();
            feedbackElement.AddToClassList("feedback-notification");
            feedbackElement.AddToClassList($"feedback-{request.Type.ToString().ToLower()}");
            
            // Set size and styling
            feedbackElement.style.width = _notificationSize.x;
            feedbackElement.style.minHeight = _notificationSize.y;
            feedbackElement.style.marginBottom = _notificationSpacing;
            
            // Apply type-specific styling
            ApplyFeedbackStyling(feedbackElement, request.Type);
            
            // Create message label
            var messageLabel = new Label(request.Message);
            messageLabel.AddToClassList("feedback-message");
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackElement.Add(messageLabel);
            
            // Add icon based on type
            var icon = CreateFeedbackIcon(request.Type);
            if (icon != null)
            {
                feedbackElement.Insert(0, icon);
            }
            
            _feedbackContainer.Add(feedbackElement);
            
            var feedback = new FeedbackElement
            {
                Element = feedbackElement,
                Request = request,
                Coroutine = StartCoroutine(AnimateFeedback(feedbackElement, request.Duration))
            };
            
            _activeFeedbacks.Add(feedback);
        }
        
        private void CreateTooltip(string text, Vector2 position)
        {
            var tooltipElement = new VisualElement();
            tooltipElement.AddToClassList("tooltip");
            
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.maxWidth = _tooltipMaxSize.x;
            tooltipElement.Add(label);
            
            tooltipElement.style.position = Position.Absolute;
            tooltipElement.style.left = position.x + _tooltipOffset.x;
            tooltipElement.style.top = position.y + _tooltipOffset.y;
            
            _tooltipContainer.Add(tooltipElement);
            _tooltipContainer.style.display = DisplayStyle.Flex;
            
            _currentTooltip = new FeedbackElement
            {
                Element = tooltipElement,
                Coroutine = StartCoroutine(FadeInElement(tooltipElement))
            };
        }
        
        private void CreateProgress(string message, float progress)
        {
            var progressElement = new VisualElement();
            progressElement.AddToClassList("progress-indicator");
            progressElement.style.width = _progressBarSize.x;
            
            // Message label
            var messageLabel = new Label(message);
            messageLabel.AddToClassList("progress-message");
            progressElement.Add(messageLabel);
            
            // Progress bar background
            var progressBackground = new VisualElement();
            progressBackground.AddToClassList("progress-background");
            progressBackground.style.width = Length.Percent(100);
            progressBackground.style.height = _progressBarSize.y;
            progressBackground.style.backgroundColor = _progressBackgroundColor;
            
            // Progress bar fill
            var progressFill = new VisualElement();
            progressFill.AddToClassList("progress-fill");
            progressFill.style.height = Length.Percent(100);
            progressFill.style.width = Length.Percent(progress * 100);
            progressFill.style.backgroundColor = _progressColor;
            
            progressBackground.Add(progressFill);
            progressElement.Add(progressBackground);
            
            // Progress percentage
            var percentageLabel = new Label($"{Mathf.RoundToInt(progress * 100)}%");
            percentageLabel.AddToClassList("progress-percentage");
            progressElement.Add(percentageLabel);
            
            _progressContainer.Add(progressElement);
            _progressContainer.style.display = DisplayStyle.Flex;
            
            _currentProgress = new FeedbackElement
            {
                Element = progressElement,
                Coroutine = StartCoroutine(FadeInElement(progressElement))
            };
        }
        
        private void UpdateProgress(string message, float progress)
        {
            if (_currentProgress?.Element == null)
                return;
            
            var messageLabel = _currentProgress.Element.Q<Label>("progress-message");
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }
            
            var progressFill = _currentProgress.Element.Q<VisualElement>("progress-fill");
            if (progressFill != null)
            {
                progressFill.style.width = Length.Percent(progress * 100);
            }
            
            var percentageLabel = _currentProgress.Element.Q<Label>("progress-percentage");
            if (percentageLabel != null)
            {
                percentageLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Hide progress when complete
            if (progress >= 1f)
            {
                StartCoroutine(HideProgressAfterDelay(1f));
            }
        }
        
        private void ApplyFeedbackStyling(VisualElement element, FeedbackType type)
        {
            Color backgroundColor;
            Color borderColor;
            
            switch (type)
            {
                case FeedbackType.Success:
                    backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
                    borderColor = new Color(0.1f, 0.6f, 0.1f, 1f);
                    break;
                case FeedbackType.Error:
                    backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
                    borderColor = new Color(0.6f, 0.1f, 0.1f, 1f);
                    break;
                case FeedbackType.Warning:
                    backgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
                    borderColor = new Color(0.6f, 0.4f, 0.1f, 1f);
                    break;
                case FeedbackType.Info:
                    backgroundColor = new Color(0.2f, 0.6f, 0.8f, 0.9f);
                    borderColor = new Color(0.1f, 0.4f, 0.6f, 1f);
                    break;
                case FeedbackType.Progress:
                    backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
                    borderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                    break;
                default:
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
                    borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                    break;
            }
            
            element.style.backgroundColor = backgroundColor;
            element.style.borderLeftColor = borderColor;
            element.style.borderRightColor = borderColor;
            element.style.borderTopColor = borderColor;
            element.style.borderBottomColor = borderColor;
            element.style.borderLeftWidth = 2f;
            element.style.borderTopLeftRadius = 4f;
            element.style.borderTopRightRadius = 4f;
            element.style.borderBottomLeftRadius = 4f;
            element.style.borderBottomRightRadius = 4f;
            element.style.paddingLeft = 8f;
            element.style.paddingRight = 8f;
            element.style.paddingTop = 4f;
            element.style.paddingBottom = 4f;
        }
        
        private VisualElement CreateFeedbackIcon(FeedbackType type)
        {
            var icon = new VisualElement();
            icon.AddToClassList("feedback-icon");
            icon.style.width = 16f;
            icon.style.height = 16f;
            icon.style.marginRight = 8f;
            
            // In a real implementation, you would set background images or use icon fonts
            // For now, just use colored squares
            switch (type)
            {
                case FeedbackType.Success:
                    icon.style.backgroundColor = Color.green;
                    break;
                case FeedbackType.Error:
                    icon.style.backgroundColor = Color.red;
                    break;
                case FeedbackType.Warning:
                    icon.style.backgroundColor = Color.yellow;
                    break;
                case FeedbackType.Info:
                    icon.style.backgroundColor = Color.blue;
                    break;
                case FeedbackType.Progress:
                    icon.style.backgroundColor = Color.gray;
                    break;
            }
            
            return icon;
        }
        
        private void RemoveOldestFeedback()
        {
            if (_activeFeedbacks.Count > 0)
            {
                var oldest = _activeFeedbacks[0];
                if (oldest.Coroutine != null)
                {
                    StopCoroutine(oldest.Coroutine);
                }
                oldest.Element.RemoveFromHierarchy();
                _activeFeedbacks.RemoveAt(0);
            }
        }
        
        private IEnumerator ProcessFeedbackQueue()
        {
            while (true)
            {
                while (_feedbackQueue.Count > 0)
                {
                    var request = _feedbackQueue.Dequeue();
                    CreateFeedback(request);
                    yield return new WaitForSeconds(0.1f); // Small delay between feedbacks
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator AnimateFeedback(VisualElement element, float duration)
        {
            // Fade in
            yield return StartCoroutine(FadeInElement(element));
            
            // Wait for duration
            yield return new WaitForSeconds(duration);
            
            // Fade out
            yield return StartCoroutine(FadeOutElement(element));
            
            // Remove from hierarchy
            element.RemoveFromHierarchy();
            
            // Remove from active list
            _activeFeedbacks.RemoveAll(f => f.Element == element);
        }
        
        private IEnumerator FadeInElement(VisualElement element)
        {
            float elapsed = 0f;
            element.style.opacity = 0f;
            
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _fadeInDuration;
                element.style.opacity = _feedbackCurve.Evaluate(progress);
                yield return null;
            }
            
            element.style.opacity = 1f;
        }
        
        private IEnumerator FadeOutElement(VisualElement element)
        {
            float elapsed = 0f;
            element.style.opacity = 1f;
            
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _fadeOutDuration;
                element.style.opacity = 1f - _feedbackCurve.Evaluate(progress);
                yield return null;
            }
            
            element.style.opacity = 0f;
        }
        
        private IEnumerator HideProgressAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_currentProgress != null)
            {
                yield return StartCoroutine(FadeOutElement(_currentProgress.Element));
                _currentProgress.Element.RemoveFromHierarchy();
                _progressContainer.style.display = DisplayStyle.None;
                _currentProgress = null;
            }
        }
        
        private void OnDestroy()
        {
            HideFeedback();
            HideTooltip();
        }
        
        // Data structures
        private class FeedbackElement
        {
            public VisualElement Element;
            public FeedbackRequest Request;
            public Coroutine Coroutine;
        }
        
        private struct FeedbackRequest
        {
            public string Message;
            public FeedbackType Type;
            public float Duration;
            public float Timestamp;
        }
    }
}