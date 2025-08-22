using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles animations for world space menus including fade in/out, scaling, and transitions.
    /// Extracted from WorldSpaceMenuRenderer.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class WorldSpaceMenuAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _defaultFadeInDuration = 0.3f;
        [SerializeField] private float _defaultFadeOutDuration = 0.2f;
        [SerializeField] private AnimationCurve _fadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _scaleEasing = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f), 
            new Keyframe(0.6f, 1.1f, 0f, 0f), 
            new Keyframe(1f, 1f, -0.5f, 0f)
        );
        
        // Animation tracking
        private readonly System.Collections.Generic.Dictionary<UIDocument, Coroutine> _activeAnimations = 
            new System.Collections.Generic.Dictionary<UIDocument, Coroutine>();
        
        /// <summary>
        /// Animates a menu fade in
        /// </summary>
        public void FadeInMenu(UIDocument menuDocument, float duration = -1f, Action onComplete = null)
        {
            if (menuDocument == null) return;
            
            var actualDuration = duration > 0 ? duration : _defaultFadeInDuration;
            StopAnimation(menuDocument);
            
            var animation = StartCoroutine(FadeAnimation(menuDocument, 0f, 1f, actualDuration, onComplete));
            _activeAnimations[menuDocument] = animation;
        }
        
        /// <summary>
        /// Animates a menu fade out
        /// </summary>
        public void FadeOutMenu(UIDocument menuDocument, float duration = -1f, Action onComplete = null)
        {
            if (menuDocument == null) return;
            
            var canvasGroup = menuDocument.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }
            
            var actualDuration = duration > 0 ? duration : _defaultFadeOutDuration;
            var startAlpha = canvasGroup.alpha;
            
            StopAnimation(menuDocument);
            
            var animation = StartCoroutine(FadeAnimation(menuDocument, startAlpha, 0f, actualDuration, onComplete));
            _activeAnimations[menuDocument] = animation;
        }
        
        /// <summary>
        /// Animates menu scale in with bounce effect
        /// </summary>
        public void ScaleInMenu(UIDocument menuDocument, float duration = -1f, Action onComplete = null)
        {
            if (menuDocument == null) return;
            
            var actualDuration = duration > 0 ? duration : _defaultFadeInDuration;
            StopScaleAnimation(menuDocument);
            
            var animation = StartCoroutine(ScaleAnimation(menuDocument, Vector3.zero, Vector3.one, actualDuration, onComplete));
            _activeAnimations[menuDocument] = animation;
        }
        
        /// <summary>
        /// Animates menu scale out
        /// </summary>
        public void ScaleOutMenu(UIDocument menuDocument, float duration = -1f, Action onComplete = null)
        {
            if (menuDocument == null) return;
            
            var actualDuration = duration > 0 ? duration : _defaultFadeOutDuration;
            var startScale = menuDocument.transform.localScale;
            
            StopScaleAnimation(menuDocument);
            
            var animation = StartCoroutine(ScaleAnimation(menuDocument, startScale, Vector3.zero, actualDuration, onComplete));
            _activeAnimations[menuDocument] = animation;
        }
        
        /// <summary>
        /// Performs a combined fade and scale animation
        /// </summary>
        public void AnimateMenuAppearance(UIDocument menuDocument, bool appearing, float duration = -1f, Action onComplete = null)
        {
            if (menuDocument == null) return;
            
            var actualDuration = duration > 0 ? duration : (appearing ? _defaultFadeInDuration : _defaultFadeOutDuration);
            
            StopAnimation(menuDocument);
            
            if (appearing)
            {
                var animation = StartCoroutine(CombinedAppearAnimation(menuDocument, actualDuration, onComplete));
                _activeAnimations[menuDocument] = animation;
            }
            else
            {
                var animation = StartCoroutine(CombinedDisappearAnimation(menuDocument, actualDuration, onComplete));
                _activeAnimations[menuDocument] = animation;
            }
        }
        
        /// <summary>
        /// Core fade animation coroutine
        /// </summary>
        private IEnumerator FadeAnimation(UIDocument document, float startAlpha, float targetAlpha, float duration, Action onComplete)
        {
            var canvasGroup = document.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                var easedT = _fadeEasing.Evaluate(t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
            _activeAnimations.Remove(document);
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Core scale animation coroutine
        /// </summary>
        private IEnumerator ScaleAnimation(UIDocument document, Vector3 startScale, Vector3 targetScale, float duration, Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                var easedT = _scaleEasing.Evaluate(t);
                document.transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
                yield return null;
            }
            
            document.transform.localScale = targetScale;
            _activeAnimations.Remove(document);
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Combined appearance animation (fade + scale)
        /// </summary>
        private IEnumerator CombinedAppearAnimation(UIDocument document, float duration, Action onComplete)
        {
            var canvasGroup = document.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }
            
            // Initialize starting values
            canvasGroup.alpha = 0f;
            document.transform.localScale = Vector3.zero;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                
                // Apply different easing for fade and scale
                var fadeT = _fadeEasing.Evaluate(t);
                var scaleT = _scaleEasing.Evaluate(t);
                
                canvasGroup.alpha = fadeT;
                document.transform.localScale = Vector3.one * scaleT;
                
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            document.transform.localScale = Vector3.one;
            _activeAnimations.Remove(document);
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Combined disappearance animation (fade + scale)
        /// </summary>
        private IEnumerator CombinedDisappearAnimation(UIDocument document, float duration, Action onComplete)
        {
            var canvasGroup = document.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }
            
            var startAlpha = canvasGroup.alpha;
            var startScale = document.transform.localScale;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                
                var fadeT = _fadeEasing.Evaluate(t);
                var scaleT = _scaleEasing.Evaluate(t);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeT);
                document.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);
                
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            document.transform.localScale = Vector3.zero;
            _activeAnimations.Remove(document);
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Stops any active animation for a document
        /// </summary>
        public void StopAnimation(UIDocument document)
        {
            if (document != null && _activeAnimations.TryGetValue(document, out var animation))
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
                _activeAnimations.Remove(document);
            }
        }
        
        /// <summary>
        /// Stops scale animation specifically
        /// </summary>
        private void StopScaleAnimation(UIDocument document)
        {
            StopAnimation(document); // For now, same as general stop
        }
        
        /// <summary>
        /// Stops all active animations
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var animation in _activeAnimations.Values)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }
            _activeAnimations.Clear();
        }
        
        /// <summary>
        /// Checks if a document is currently animating
        /// </summary>
        public bool IsAnimating(UIDocument document)
        {
            return document != null && _activeAnimations.ContainsKey(document);
        }
        
        /// <summary>
        /// Gets the number of active animations
        /// </summary>
        public int ActiveAnimationCount => _activeAnimations.Count;
        
        private void OnDestroy()
        {
            StopAllAnimations();
        }
    }
}