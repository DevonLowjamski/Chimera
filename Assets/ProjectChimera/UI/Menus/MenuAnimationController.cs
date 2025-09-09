using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.Logging;
using System.Collections;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// Handles menu animations, transitions, and visual effects.
    /// Manages fade in/out, scaling, and advanced animation sequences.
    /// </summary>
    public class MenuAnimationController : MonoBehaviour
    {
        private MenuCore _menuCore;
        
        [Header("Animation Settings")]
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private bool _enableParticleEffects = false;
        [SerializeField] private bool _enableSoundEffects = true;

        [Header("Fade Animations")]
        [SerializeField] private bool _useFadeAnimation = true;
        [SerializeField] private float _fadeSpeed = 3f;

        [Header("Scale Animations")]
        [SerializeField] private bool _useScaleAnimation = true;
        [SerializeField] private float _scaleSpeed = 4f;
        [SerializeField] private Vector3 _startScale = new Vector3(0.8f, 0.8f, 1f);
        [SerializeField] private Vector3 _endScale = Vector3.one;

        [Header("Slide Animations")]
        [SerializeField] private bool _useSlideAnimation = false;
        [SerializeField] private float _slideSpeed = 2f;
        [SerializeField] private Vector2 _slideOffset = new Vector2(0, -50f);

        [Header("Advanced Effects")]
        [SerializeField] private bool _useElasticEffect = false;
        [SerializeField] private float _elasticStrength = 0.2f;
        [SerializeField] private bool _useGlowEffect = false;
        [SerializeField] private Color _glowColor = Color.white;

        // Animation state
        private Coroutine _currentAnimation = null;
        private CanvasGroup _menuCanvasGroup = null;
        private RectTransform _menuRectTransform = null;
        private Image _menuBackground = null;

        // Original values for restoration
        private Vector3 _originalScale = Vector3.one;
        private Vector3 _originalPosition = Vector3.zero;
        private float _originalAlpha = 1f;
        private Color _originalBackgroundColor;

        public void Initialize(MenuCore menuCore)
        {
            _menuCore = menuCore;
            SetupAnimationComponents();
        }

        public void AnimateMenuIn()
        {
            if (!_enableAnimations || _menuCore.ContextMenuPanel == null) return;

            // Stop any current animation
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            // Start fade in animation
            _currentAnimation = StartCoroutine(AnimateMenuInCoroutine());
        }

        public void AnimateMenuOut()
        {
            if (!_enableAnimations || _menuCore.ContextMenuPanel == null) return;

            // Stop any current animation
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            // Start fade out animation
            _currentAnimation = StartCoroutine(AnimateMenuOutCoroutine());
        }

        public void SetMenuOpacity(float opacity)
        {
            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = opacity;
            }
        }

        public void SetMenuScale(Vector3 scale)
        {
            if (_menuRectTransform != null)
            {
                _menuRectTransform.localScale = scale;
            }
        }

        public void PlayMenuAppearEffect()
        {
            if (_enableParticleEffects)
            {
                // Play particle effect when menu appears
                CreateAppearanceParticles();
            }

            if (_enableSoundEffects)
            {
                // Play sound effect
                PlayMenuAppearSound();
            }
        }

        public void PlayMenuDisappearEffect()
        {
            if (_enableParticleEffects)
            {
                // Play particle effect when menu disappears
                CreateDisappearanceParticles();
            }

            if (_enableSoundEffects)
            {
                // Play sound effect
                PlayMenuDisappearSound();
            }
        }

        public void EnableGlowEffect(bool enable)
        {
            if (!_useGlowEffect || _menuBackground == null) return;

            if (enable)
            {
                StartCoroutine(AnimateGlowEffect());
            }
            else
            {
                RestoreOriginalBackgroundColor();
            }
        }

        private void SetupAnimationComponents()
        {
            if (_menuCore.ContextMenuPanel == null) return;

            // Get or create CanvasGroup for fade animations
            _menuCanvasGroup = _menuCore.ContextMenuPanel.GetComponent<CanvasGroup>();
            if (_menuCanvasGroup == null)
            {
                _menuCanvasGroup = _menuCore.ContextMenuPanel.AddComponent<CanvasGroup>();
            }

            // Get RectTransform for scale and position animations
            _menuRectTransform = _menuCore.ContextMenuPanel.GetComponent<RectTransform>();

            // Get background Image for color animations
            _menuBackground = _menuCore.ContextMenuPanel.GetComponent<Image>();

            // Store original values
            StoreOriginalValues();
        }

        private void StoreOriginalValues()
        {
            if (_menuRectTransform != null)
            {
                _originalScale = _menuRectTransform.localScale;
                _originalPosition = _menuRectTransform.localPosition;
            }

            if (_menuCanvasGroup != null)
            {
                _originalAlpha = _menuCanvasGroup.alpha;
            }

            if (_menuBackground != null)
            {
                _originalBackgroundColor = _menuBackground.color;
            }
        }

        private IEnumerator AnimateMenuInCoroutine()
        {
            LogDebug("Starting menu in animation");

            // Set initial state
            SetInitialAnimationState();

            // Play appearance effect
            PlayMenuAppearEffect();

            float animationTime = 0f;
            float duration = _menuCore.MenuFadeInDuration;

            while (animationTime < duration)
            {
                float progress = animationTime / duration;
                float easedProgress = _menuCore.MenuAnimationCurve.Evaluate(progress);

                // Apply animations
                ApplyFadeInAnimation(easedProgress);
                ApplyScaleInAnimation(easedProgress);
                ApplySlideInAnimation(easedProgress);

                // Apply elastic effect if enabled
                if (_useElasticEffect)
                {
                    ApplyElasticEffect(easedProgress);
                }

                animationTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final state
            SetFinalAnimationState();

            // Enable glow effect if configured
            if (_useGlowEffect)
            {
                EnableGlowEffect(true);
            }

            _currentAnimation = null;
            LogDebug("Menu in animation completed");
        }

        private IEnumerator AnimateMenuOutCoroutine()
        {
            LogDebug("Starting menu out animation");

            // Disable glow effect
            EnableGlowEffect(false);

            // Play disappearance effect
            PlayMenuDisappearEffect();

            float animationTime = 0f;
            float duration = _menuCore.MenuFadeOutDuration;

            while (animationTime < duration)
            {
                float progress = animationTime / duration;
                float easedProgress = _menuCore.MenuAnimationCurve.Evaluate(progress);
                float reverseProgress = 1f - easedProgress;

                // Apply reverse animations
                ApplyFadeInAnimation(reverseProgress);
                ApplyScaleInAnimation(reverseProgress);
                ApplySlideInAnimation(reverseProgress);

                animationTime += Time.deltaTime;
                yield return null;
            }

            // Ensure menu is hidden
            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 0f;
            }

            _currentAnimation = null;
            LogDebug("Menu out animation completed");
        }

        private void SetInitialAnimationState()
        {
            if (_useFadeAnimation && _menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 0f;
            }

            if (_useScaleAnimation && _menuRectTransform != null)
            {
                _menuRectTransform.localScale = _startScale;
            }

            if (_useSlideAnimation && _menuRectTransform != null)
            {
                Vector3 slidePosition = _originalPosition + new Vector3(_slideOffset.x, _slideOffset.y, 0);
                _menuRectTransform.localPosition = slidePosition;
            }
        }

        private void SetFinalAnimationState()
        {
            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 1f;
            }

            if (_menuRectTransform != null)
            {
                _menuRectTransform.localScale = _endScale;
                _menuRectTransform.localPosition = _originalPosition;
            }
        }

        private void ApplyFadeInAnimation(float progress)
        {
            if (_useFadeAnimation && _menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = progress;
            }
        }

        private void ApplyScaleInAnimation(float progress)
        {
            if (_useScaleAnimation && _menuRectTransform != null)
            {
                Vector3 currentScale = Vector3.Lerp(_startScale, _endScale, progress);
                _menuRectTransform.localScale = currentScale;
            }
        }

        private void ApplySlideInAnimation(float progress)
        {
            if (_useSlideAnimation && _menuRectTransform != null)
            {
                Vector3 slidePosition = _originalPosition + new Vector3(_slideOffset.x, _slideOffset.y, 0);
                Vector3 currentPosition = Vector3.Lerp(slidePosition, _originalPosition, progress);
                _menuRectTransform.localPosition = currentPosition;
            }
        }

        private void ApplyElasticEffect(float progress)
        {
            if (_menuRectTransform != null)
            {
                float elasticScale = 1f + Mathf.Sin(progress * Mathf.PI * 4f) * _elasticStrength * (1f - progress);
                Vector3 currentScale = _menuRectTransform.localScale;
                currentScale *= elasticScale;
                _menuRectTransform.localScale = currentScale;
            }
        }

        private IEnumerator AnimateGlowEffect()
        {
            if (_menuBackground == null) yield break;

            float glowTime = 0f;
            float glowDuration = 2f;

            while (_useGlowEffect && _menuBackground != null)
            {
                float glowIntensity = (Mathf.Sin(glowTime * Mathf.PI) + 1f) * 0.5f;
                Color glowedColor = Color.Lerp(_originalBackgroundColor, _glowColor, glowIntensity * 0.3f);
                _menuBackground.color = glowedColor;

                glowTime += Time.deltaTime;
                if (glowTime >= glowDuration) glowTime = 0f;

                yield return null;
            }
        }

        private void RestoreOriginalBackgroundColor()
        {
            if (_menuBackground != null)
            {
                _menuBackground.color = _originalBackgroundColor;
            }
        }

        private void CreateAppearanceParticles()
        {
            // Placeholder for particle effect creation
            LogDebug("Creating appearance particle effect");
        }

        private void CreateDisappearanceParticles()
        {
            // Placeholder for particle effect creation
            LogDebug("Creating disappearance particle effect");
        }

        private void PlayMenuAppearSound()
        {
            // Placeholder for sound effect
            LogDebug("Playing menu appear sound");
        }

        private void PlayMenuDisappearSound()
        {
            // Placeholder for sound effect
            LogDebug("Playing menu disappear sound");
        }

        #region Public Interface

        public void SetAnimationsEnabled(bool enabled)
        {
            _enableAnimations = enabled;
            LogDebug($"Animations {(enabled ? "enabled" : "disabled")}");
        }

        public void SetFadeAnimationEnabled(bool enabled)
        {
            _useFadeAnimation = enabled;
        }

        public void SetScaleAnimationEnabled(bool enabled)
        {
            _useScaleAnimation = enabled;
        }

        public void SetSlideAnimationEnabled(bool enabled)
        {
            _useSlideAnimation = enabled;
        }

        public void SetElasticEffectEnabled(bool enabled)
        {
            _useElasticEffect = enabled;
        }

        public void SetGlowEffectEnabled(bool enabled)
        {
            _useGlowEffect = enabled;
            if (!enabled)
            {
                RestoreOriginalBackgroundColor();
            }
        }

        public void SetFadeSpeed(float speed)
        {
            _fadeSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetScaleSpeed(float speed)
        {
            _scaleSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetSlideSpeed(float speed)
        {
            _slideSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetStartScale(Vector3 scale)
        {
            _startScale = scale;
        }

        public void SetEndScale(Vector3 scale)
        {
            _endScale = scale;
        }

        public void SetSlideOffset(Vector2 offset)
        {
            _slideOffset = offset;
        }

        public void SetGlowColor(Color color)
        {
            _glowColor = color;
        }

        public bool IsAnimating()
        {
            return _currentAnimation != null;
        }

        public void StopAllAnimations()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            // Restore original state
            SetFinalAnimationState();
        }

        #endregion

        private void LogDebug(string message)
        {
            if (_menuCore.DebugMode)
            {
                ChimeraLogger.Log($"[MenuAnimationController] {message}");
            }
        }
    }
}