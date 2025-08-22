using UnityEngine;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Controls menu transitions and animation logic for contextual menus.
    /// Extracted from ContextualMenuStateManager.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class MenuTransitionController
    {
        // Animation and Transition State
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;
        private string _currentTransition = MenuTransition.None;
        private bool _isOpening = false;
        private float _transitionStartTime = 0f;
        private float _transitionDuration = 0.2f;
        
        // Events
        public event Action<string, float> OnTransitionUpdate;
        public event Action<string, bool> OnTransitionComplete;
        public event Action<string> OnTransitionStarted;
        
        public bool IsTransitioning => _isTransitioning;
        public float TransitionProgress => _transitionProgress;
        public string CurrentTransition => _currentTransition;
        public bool IsOpening => _isOpening;
        
        /// <summary>
        /// Starts a menu transition
        /// </summary>
        public void StartTransition(string transitionType, bool opening, float duration = 0.2f)
        {
            if (string.IsNullOrEmpty(transitionType))
            {
                transitionType = MenuTransition.Fade;
            }
            
            _isTransitioning = true;
            _currentTransition = transitionType;
            _isOpening = opening;
            _transitionProgress = 0f;
            _transitionStartTime = Time.time;
            _transitionDuration = Mathf.Max(0.01f, duration); // Minimum duration
            
            OnTransitionStarted?.Invoke(transitionType);
            Debug.Log($"[MenuTransitionController] Started {transitionType} transition (opening: {opening}, duration: {duration}s)");
        }
        
        /// <summary>
        /// Updates transition progress (called by animation system or Update loop)
        /// </summary>
        public void UpdateTransition()
        {
            if (!_isTransitioning)
            {
                return;
            }
            
            // Calculate progress based on time
            var elapsed = Time.time - _transitionStartTime;
            _transitionProgress = Mathf.Clamp01(elapsed / _transitionDuration);
            
            // Apply easing based on transition type
            var easedProgress = ApplyEasing(_transitionProgress, _currentTransition);
            
            // Fire update event
            OnTransitionUpdate?.Invoke(_currentTransition, easedProgress);
            
            // Check if transition is complete
            if (_transitionProgress >= 1f)
            {
                CompleteTransition();
            }
        }
        
        /// <summary>
        /// Manually sets transition progress (for external animation systems)
        /// </summary>
        public void SetTransitionProgress(float progress)
        {
            if (!_isTransitioning)
            {
                return;
            }
            
            _transitionProgress = Mathf.Clamp01(progress);
            
            // Apply easing
            var easedProgress = ApplyEasing(_transitionProgress, _currentTransition);
            OnTransitionUpdate?.Invoke(_currentTransition, easedProgress);
            
            if (_transitionProgress >= 1f)
            {
                CompleteTransition();
            }
        }
        
        /// <summary>
        /// Cancels the current transition
        /// </summary>
        public void CancelTransition()
        {
            if (!_isTransitioning)
            {
                return;
            }
            
            var wasOpening = _isOpening;
            var transitionType = _currentTransition;
            
            _isTransitioning = false;
            _currentTransition = MenuTransition.None;
            _transitionProgress = 0f;
            
            OnTransitionComplete?.Invoke(transitionType, wasOpening);
            Debug.Log($"[MenuTransitionController] Cancelled {transitionType} transition");
        }
        
        /// <summary>
        /// Instantly completes the current transition
        /// </summary>
        public void CompleteTransition()
        {
            if (!_isTransitioning)
            {
                return;
            }
            
            var wasOpening = _isOpening;
            var transitionType = _currentTransition;
            
            // Set final progress
            _transitionProgress = 1f;
            OnTransitionUpdate?.Invoke(_currentTransition, 1f);
            
            // Clean up state
            _isTransitioning = false;
            _currentTransition = MenuTransition.None;
            
            OnTransitionComplete?.Invoke(transitionType, wasOpening);
            Debug.Log($"[MenuTransitionController] Completed {transitionType} transition (opening: {wasOpening})");
        }
        
        /// <summary>
        /// Applies easing functions based on transition type
        /// </summary>
        private float ApplyEasing(float progress, string transitionType)
        {
            switch (transitionType)
            {
                case MenuTransition.Fade:
                    return ApplyFadeEasing(progress);
                case MenuTransition.Slide:
                    return ApplySlideEasing(progress);
                case MenuTransition.Scale:
                    return ApplyScaleEasing(progress);
                case MenuTransition.None:
                default:
                    return progress; // Linear
            }
        }
        
        /// <summary>
        /// Applies fade-specific easing (smooth in/out)
        /// </summary>
        private float ApplyFadeEasing(float t)
        {
            // Smooth ease in/out
            return t * t * (3f - 2f * t);
        }
        
        /// <summary>
        /// Applies slide-specific easing (ease out)
        /// </summary>
        private float ApplySlideEasing(float t)
        {
            // Ease out cubic
            return 1f - Mathf.Pow(1f - t, 3f);
        }
        
        /// <summary>
        /// Applies scale-specific easing (bounce effect)
        /// </summary>
        private float ApplyScaleEasing(float t)
        {
            // Elastic ease out for scale
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            
            var period = 0.3f;
            var amplitude = 1f;
            
            return amplitude * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - period / 4f) * (2f * Mathf.PI) / period) + 1f;
        }
        
        /// <summary>
        /// Gets transition parameters for a specific transition type
        /// </summary>
        public TransitionParams GetTransitionParams(string transitionType)
        {
            switch (transitionType)
            {
                case MenuTransition.Fade:
                    return new TransitionParams
                    {
                        TransitionType = transitionType,
                        UsesAlpha = true,
                        UsesScale = false,
                        UsesPosition = false,
                        DefaultDuration = 0.2f
                    };
                case MenuTransition.Slide:
                    return new TransitionParams
                    {
                        TransitionType = transitionType,
                        UsesAlpha = false,
                        UsesScale = false,
                        UsesPosition = true,
                        DefaultDuration = 0.25f
                    };
                case MenuTransition.Scale:
                    return new TransitionParams
                    {
                        TransitionType = transitionType,
                        UsesAlpha = true,
                        UsesScale = true,
                        UsesPosition = false,
                        DefaultDuration = 0.15f
                    };
                case MenuTransition.None:
                default:
                    return new TransitionParams
                    {
                        TransitionType = MenuTransition.None,
                        UsesAlpha = false,
                        UsesScale = false,
                        UsesPosition = false,
                        DefaultDuration = 0f
                    };
            }
        }
        
        /// <summary>
        /// Resets the transition controller to default state
        /// </summary>
        public void Reset()
        {
            _isTransitioning = false;
            _currentTransition = MenuTransition.None;
            _transitionProgress = 0f;
            _isOpening = false;
            _transitionStartTime = 0f;
            _transitionDuration = 0.2f;
            
            Debug.Log("[MenuTransitionController] Reset to default state");
        }
        
        /// <summary>
        /// Gets current transition state information
        /// </summary>
        public TransitionState GetCurrentState()
        {
            return new TransitionState
            {
                IsTransitioning = _isTransitioning,
                TransitionType = _currentTransition,
                Progress = _transitionProgress,
                IsOpening = _isOpening,
                Duration = _transitionDuration,
                ElapsedTime = _isTransitioning ? Time.time - _transitionStartTime : 0f
            };
        }
    }
    
    /// <summary>
    /// Parameters for a specific transition type
    /// </summary>
    public class TransitionParams
    {
        public string TransitionType { get; set; }
        public bool UsesAlpha { get; set; }
        public bool UsesScale { get; set; }
        public bool UsesPosition { get; set; }
        public float DefaultDuration { get; set; }
    }
    
    /// <summary>
    /// Current state of a menu transition
    /// </summary>
    public class TransitionState
    {
        public bool IsTransitioning { get; set; }
        public string TransitionType { get; set; }
        public float Progress { get; set; }
        public bool IsOpening { get; set; }
        public float Duration { get; set; }
        public float ElapsedTime { get; set; }
    }
}