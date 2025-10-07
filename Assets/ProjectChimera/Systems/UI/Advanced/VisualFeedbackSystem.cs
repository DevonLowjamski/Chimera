using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// BASIC: Simple visual feedback system for Project Chimera's UI.
    /// Focuses on essential visual feedback without complex notification systems and animations.
    /// </summary>
    public class VisualFeedbackSystem : MonoBehaviour
    {
        [Header("Basic Feedback Settings")]
        [SerializeField] private bool _enableBasicFeedback = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _defaultDisplayTime = 2.0f;
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        // Basic feedback components
        [Header("UI Components")]
        [SerializeField] private Text _feedbackText;
        [SerializeField] private Image _feedbackBackground;
        [SerializeField] private CanvasGroup _feedbackCanvasGroup;

        // Basic feedback state
        private bool _isInitialized = false;
        private Coroutine _currentFeedbackCoroutine;

        /// <summary>
        /// Events for feedback operations
        /// </summary>
        public event System.Action<string> OnFeedbackShown;
        public event System.Action OnFeedbackHidden;

        /// <summary>
        /// Initialize basic feedback system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Auto-find components if not assigned
            if (_feedbackText == null)
                _feedbackText = GetComponentInChildren<Text>();

            if (_feedbackBackground == null)
                _feedbackBackground = GetComponentInChildren<Image>();

            if (_feedbackCanvasGroup == null)
                _feedbackCanvasGroup = GetComponent<CanvasGroup>();

            // Create canvas group if needed
            if (_feedbackCanvasGroup == null)
                _feedbackCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            if (_feedbackCanvasGroup != null)
                _feedbackCanvasGroup.alpha = 0f;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("VisualFeedbackSystem", "$1");
            }
        }

        /// <summary>
        /// Show success feedback
        /// </summary>
        public void ShowSuccess(string message, float duration = 0f)
        {
            ShowFeedback(message, FeedbackType.Success, duration > 0 ? duration : _defaultDisplayTime);
        }

        /// <summary>
        /// Show warning feedback
        /// </summary>
        public void ShowWarning(string message, float duration = 0f)
        {
            ShowFeedback(message, FeedbackType.Warning, duration > 0 ? duration : _defaultDisplayTime);
        }

        /// <summary>
        /// Show error feedback
        /// </summary>
        public void ShowError(string message, float duration = 0f)
        {
            ShowFeedback(message, FeedbackType.Error, duration > 0 ? duration : _defaultDisplayTime);
        }

        /// <summary>
        /// Show info feedback
        /// </summary>
        public void ShowInfo(string message, float duration = 0f)
        {
            ShowFeedback(message, FeedbackType.Info, duration > 0 ? duration : _defaultDisplayTime);
        }

        /// <summary>
        /// Show custom feedback
        /// </summary>
        public void ShowCustom(string message, Color color, float duration = 0f)
        {
            if (!_enableBasicFeedback || !_isInitialized) return;

            UpdateFeedbackDisplay(message, color);
            StartFeedbackCoroutine(duration > 0 ? duration : _defaultDisplayTime);
        }

        /// <summary>
        /// Hide feedback immediately
        /// </summary>
        public void HideFeedback()
        {
            if (_currentFeedbackCoroutine != null)
            {
                StopCoroutine(_currentFeedbackCoroutine);
                _currentFeedbackCoroutine = null;
            }

            if (_feedbackCanvasGroup != null)
                _feedbackCanvasGroup.alpha = 0f;

            OnFeedbackHidden?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("VisualFeedbackSystem", "$1");
            }
        }

        /// <summary>
        /// Check if feedback is currently visible
        /// </summary>
        public bool IsFeedbackVisible()
        {
            return _feedbackCanvasGroup != null && _feedbackCanvasGroup.alpha > 0f;
        }

        /// <summary>
        /// Set feedback enabled state
        /// </summary>
        public void SetFeedbackEnabled(bool enabled)
        {
            _enableBasicFeedback = enabled;

            if (!enabled)
            {
                HideFeedback();
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("VisualFeedbackSystem", "$1");
            }
        }

        /// <summary>
        /// Get feedback system statistics
        /// </summary>
        public FeedbackStats GetStats()
        {
            return new FeedbackStats
            {
                IsEnabled = _enableBasicFeedback,
                IsInitialized = _isInitialized,
                IsVisible = IsFeedbackVisible(),
                DefaultDisplayTime = _defaultDisplayTime
            };
        }

        #region Private Methods

        private void ShowFeedback(string message, FeedbackType type, float duration)
        {
            if (!_enableBasicFeedback || !_isInitialized) return;

            Color color = GetColorForType(type);
            UpdateFeedbackDisplay(message, color);
            StartFeedbackCoroutine(duration);
        }

        private Color GetColorForType(FeedbackType type)
        {
            switch (type)
            {
                case FeedbackType.Success: return _successColor;
                case FeedbackType.Warning: return _warningColor;
                case FeedbackType.Error: return _errorColor;
                case FeedbackType.Info: return Color.white;
                default: return Color.white;
            }
        }

        private void UpdateFeedbackDisplay(string message, Color color)
        {
            if (_feedbackText != null)
                _feedbackText.text = message;

            if (_feedbackBackground != null)
                _feedbackBackground.color = color;

            OnFeedbackShown?.Invoke(message);
        }

        private void StartFeedbackCoroutine(float duration)
        {
            if (_currentFeedbackCoroutine != null)
            {
                StopCoroutine(_currentFeedbackCoroutine);
            }

            _currentFeedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(duration));
        }

        private IEnumerator ShowFeedbackCoroutine(float duration)
        {
            // Fade in
            if (_feedbackCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.deltaTime;
                    _feedbackCanvasGroup.alpha = elapsed / 0.2f;
                    yield return null;
                }
                _feedbackCanvasGroup.alpha = 1f;
            }

            // Wait for display time
            yield return new WaitForSeconds(duration);

            // Fade out
            if (_feedbackCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.deltaTime;
                    _feedbackCanvasGroup.alpha = 1f - (elapsed / 0.2f);
                    yield return null;
                }
                _feedbackCanvasGroup.alpha = 0f;
            }

            _currentFeedbackCoroutine = null;
            OnFeedbackHidden?.Invoke();
        }

        #endregion
    }

    // FeedbackType is defined in Advanced.MenuDataStructures. Use that definition to avoid duplication.

    /// <summary>
    /// Feedback statistics
    /// </summary>
    [System.Serializable]
    public struct FeedbackStats
    {
        public bool IsEnabled;
        public bool IsInitialized;
        public bool IsVisible;
        public float DefaultDisplayTime;
    }
}
