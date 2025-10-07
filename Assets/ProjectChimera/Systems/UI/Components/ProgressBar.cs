using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core.Memory;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.UI.Core;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// PERFORMANCE: Optimized progress bar for cultivation operations
    /// Displays operation progress with efficient updates and animations
    /// Week 12: Input & UI Performance
    /// </summary>
    public class ProgressBar : MonoBehaviour, IUIUpdatable
    {
        [Header("Progress Bar Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _percentageText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private bool _enableSmoothFill = true;
        [SerializeField] private float _fillAnimationSpeed = 2f;
        [SerializeField] private bool _enablePulseAnimation = true;
        [SerializeField] private float _pulseSpeed = 1f;

        // Progress state
        private string _operationId;
        private string _title;
        private float _duration;
        private float _startTime;
        private float _currentProgress = 0f;
        private float _targetProgress = 0f;
        private bool _isCompleted = false;
        private bool _isVisible = true;

        // Animation state
        private float _displayProgress = 0f;
        private float _pulseTime = 0f;

        // String optimization
        private string _cachedPercentageString;
        private int _lastPercentage = -1;

        // Events
        public System.Action<string> OnProgressCompleted;
        public System.Action<string> OnProgressCanceled;
        public System.Action OnCompleted; // For UIProgressBarManager compatibility

        public string OperationId => _operationId;
        public string Id => _operationId; // Alias for Id property
        public float Progress => _currentProgress;
        public bool IsCompleted => _isCompleted;
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Initialize progress bar
        /// </summary>
        public void Initialize(string operationId, string title, float duration)
        {
            _operationId = operationId;
            _title = title;
            _duration = duration;
            _startTime = Time.unscaledTime;
            _currentProgress = 0f;
            _targetProgress = 0f;
            _isCompleted = false;

            SetupComponents();
            UpdateDisplay();
        }

        /// <summary>
        /// Update progress value
        /// </summary>
        public void UpdateProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (_targetProgress >= 1f && !_isCompleted)
            {
                CompleteProgress();
            }
        }

        /// <summary>
        /// Set progress directly (for immediate updates)
        /// </summary>
        public void SetProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);
            _targetProgress = _currentProgress;
            _displayProgress = _currentProgress;

            if (_currentProgress >= 1f && !_isCompleted)
            {
                CompleteProgress();
            }
        }

        /// <summary>
        /// Update progress bar with value and max value (for UIProgressBarManager compatibility)
        /// </summary>
        public void UpdateProgressBar(float currentValue, float maxValue)
        {
            float normalizedProgress = maxValue > 0 ? currentValue / maxValue : 0f;
            UpdateProgress(normalizedProgress);
        }

        /// <summary>
        /// Complete progress operation
        /// </summary>
        public void CompleteProgress()
        {
            if (_isCompleted) return;

            _isCompleted = true;
            _currentProgress = 1f;
            _targetProgress = 1f;
            _displayProgress = 1f;

            OnProgressCompleted?.Invoke(_operationId);
            OnCompleted?.Invoke();

            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// Cancel progress operation
        /// </summary>
        public void CancelProgress()
        {
            _isCompleted = true;
            OnProgressCanceled?.Invoke(_operationId);

            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// Set visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Check if should update
        /// </summary>
        public bool ShouldUpdate()
        {
            return _isVisible && !_isCompleted &&
                   (Mathf.Abs(_displayProgress - _targetProgress) > 0.01f || _enablePulseAnimation);
        }

        /// <summary>
        /// Update UI display
        /// </summary>
        public void UpdateUI(float deltaTime = 0f)
        {
            UpdateProgressAnimation();
            UpdateFillBar();
            UpdatePercentageText();
            UpdatePulseAnimation();
        }

        #region Private Methods

        /// <summary>
        /// Set up UI components
        /// </summary>
        private void SetupComponents()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 40);
            }

            // Create background if not present
            if (_backgroundImage == null)
            {
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(transform, false);
                var bgRT = bgGO.AddComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = Vector2.zero;

                _backgroundImage = bgGO.AddComponent<Image>();
                _backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            // Create fill image if not present
            if (_fillImage == null)
            {
                var fillGO = new GameObject("Fill");
                fillGO.transform.SetParent(transform, false);
                var fillRT = fillGO.AddComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero;
                fillRT.anchorMax = new Vector2(0, 1);
                fillRT.sizeDelta = Vector2.zero;

                _fillImage = fillGO.AddComponent<Image>();
                _fillImage.color = new Color(0.3f, 0.7f, 0.3f, 1f);
                _fillImage.type = Image.Type.Filled;
                _fillImage.fillMethod = Image.FillMethod.Horizontal;
            }

            // Create title text if not present
            if (_titleText == null)
            {
                var titleGO = new GameObject("TitleText");
                titleGO.transform.SetParent(transform, false);
                var titleRT = titleGO.AddComponent<RectTransform>();
                titleRT.anchorMin = new Vector2(0, 0.5f);
                titleRT.anchorMax = new Vector2(0.7f, 1f);
                titleRT.sizeDelta = Vector2.zero;

                _titleText = titleGO.AddComponent<TextMeshProUGUI>();
                _titleText.text = _title;
                _titleText.fontSize = 12;
                _titleText.alignment = TextAlignmentOptions.Left;
                _titleText.color = Color.white;
            }

            // Create percentage text if not present
            if (_percentageText == null)
            {
                var percentGO = new GameObject("PercentageText");
                percentGO.transform.SetParent(transform, false);
                var percentRT = percentGO.AddComponent<RectTransform>();
                percentRT.anchorMin = new Vector2(0.7f, 0);
                percentRT.anchorMax = new Vector2(1f, 1f);
                percentRT.sizeDelta = Vector2.zero;

                _percentageText = percentGO.AddComponent<TextMeshProUGUI>();
                _percentageText.fontSize = 10;
                _percentageText.alignment = TextAlignmentOptions.Right;
                _percentageText.color = Color.white;
            }

            // Create canvas group if not present
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// Update progress animation
        /// </summary>
        private void UpdateProgressAnimation()
        {
            if (_enableSmoothFill)
            {
                _displayProgress = Mathf.Lerp(_displayProgress, _targetProgress, _fillAnimationSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                _displayProgress = _targetProgress;
            }

            _currentProgress = _displayProgress;
        }

        /// <summary>
        /// Update fill bar display
        /// </summary>
        private void UpdateFillBar()
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = _displayProgress;

                // Color based on progress
                if (_displayProgress < 0.3f)
                {
                    _fillImage.color = Color.Lerp(Color.red, Color.yellow, _displayProgress / 0.3f);
                }
                else if (_displayProgress < 0.7f)
                {
                    _fillImage.color = Color.Lerp(Color.yellow, Color.green, (_displayProgress - 0.3f) / 0.4f);
                }
                else
                {
                    _fillImage.color = Color.green;
                }
            }
        }

        /// <summary>
        /// Update percentage text with string optimization
        /// </summary>
        private void UpdatePercentageText()
        {
            if (_percentageText != null)
            {
                int currentPercentage = Mathf.RoundToInt(_displayProgress * 100f);

                if (currentPercentage != _lastPercentage)
                {
                    _cachedPercentageString = StringOptimizer.Format("{0}%", currentPercentage);
                    _percentageText.text = _cachedPercentageString;
                    _lastPercentage = currentPercentage;
                }
            }
        }

        /// <summary>
        /// Update pulse animation
        /// </summary>
        private void UpdatePulseAnimation()
        {
            if (!_enablePulseAnimation || _isCompleted) return;

            _pulseTime += Time.unscaledDeltaTime * _pulseSpeed;
            float pulseAlpha = 0.8f + (Mathf.Sin(_pulseTime) * 0.2f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = pulseAlpha;
            }
        }

        /// <summary>
        /// Update display immediately
        /// </summary>
        private void UpdateDisplay()
        {
            if (_titleText != null)
            {
                _titleText.text = StringOptimizer.Intern(_title);
            }

            UpdateUI();
        }

        /// <summary>
        /// Fade out and destroy progress bar
        /// </summary>
        private IEnumerator FadeOutAndDestroy()
        {
            if (_canvasGroup != null)
            {
                float fadeTime = 1f;
                float elapsed = 0f;
                float startAlpha = _canvasGroup.alpha;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }

            // Return to pool or destroy
            if (OptimizedUIManager.Instance != null)
            {
                // Would return to pool here if integrated with UI pooling
            }

            Destroy(gameObject);
        }

        #endregion

        private void OnDestroy()
        {
            OnProgressCompleted = null;
            OnProgressCanceled = null;
        }
    }
}
