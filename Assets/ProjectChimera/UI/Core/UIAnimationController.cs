using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System;
// Reduce coupling: define a minimal local UIAnimationType and stub animation classes

namespace ProjectChimera.UI.Core
{
    public enum UIAnimationType { Fade, Position, Scale, Bounce }
    // Minimal previews (not used now) intentionally removed to avoid duplicate class names below
    /// <summary>
    /// Handles UI animations and transitions using Unity UI Toolkit.
    /// Provides smooth animations for panels, widgets, and UI elements.
    /// </summary>
    public class UIAnimationController : IDisposable
    {
        private List<UIAnimation> _activeAnimations = new List<UIAnimation>();
        private Dictionary<VisualElement, UIAnimation> _elementAnimations = new Dictionary<VisualElement, UIAnimation>();
        
        private const float DEFAULT_ANIMATION_DURATION = 0.3f;
        
        public void AnimateIn(VisualElement element, UIAnimationType animationType = UIAnimationType.Fade, 
                            float duration = DEFAULT_ANIMATION_DURATION, System.Action onComplete = null)
        {
            StopAnimation(element);
            
            var animation = CreateAnimation(element, animationType, true, duration, onComplete);
            StartAnimation(animation);
        }
        
        public void AnimateOut(VisualElement element, System.Action onComplete = null, 
                             UIAnimationType animationType = UIAnimationType.Fade, 
                             float duration = DEFAULT_ANIMATION_DURATION)
        {
            StopAnimation(element);
            
            var animation = CreateAnimation(element, animationType, false, duration, onComplete);
            StartAnimation(animation);
        }
        
        public void AnimateProperty(VisualElement element, string propertyName, 
                                  object fromValue, object toValue, float duration, 
                                  System.Action onComplete = null)
        {
            var animation = new PropertyUIAnimation
            {
                Element = element,
                PropertyName = propertyName,
                FromValue = fromValue,
                ToValue = toValue,
                Duration = duration,
                StartTime = Time.time,
                OnComplete = onComplete,
                IsPlaying = true
            };
            
            StartAnimation(animation);
        }
        
        public void PulseElement(VisualElement element, float pulseScale = 1.1f, float duration = 0.5f)
        {
            var pulseAnimation = new PulseUIAnimation
            {
                Element = element,
                OriginalScale = Vector3.one,
                PulseScale = Vector3.one * pulseScale,
                Duration = duration,
                StartTime = Time.time,
                IsPlaying = true
            };
            
            StartAnimation(pulseAnimation);
        }
        
        public void ShakeElement(VisualElement element, float intensity = 10f, float duration = 0.5f)
        {
            var shakeAnimation = new ShakeUIAnimation
            {
                Element = element,
                OriginalPosition = element.transform.position,
                Intensity = intensity,
                Duration = duration,
                StartTime = Time.time,
                IsPlaying = true
            };
            
            StartAnimation(shakeAnimation);
        }
        
        private UIAnimation CreateAnimation(VisualElement element, UIAnimationType type, 
                                         bool animateIn, float duration, System.Action onComplete)
        {
            switch (type)
            {
                case UIAnimationType.Fade:
                    return new FadeUIAnimation
                    {
                        Element = element,
                        FromOpacity = animateIn ? 0f : 1f,
                        ToOpacity = animateIn ? 1f : 0f,
                        Duration = duration,
                        StartTime = Time.time,
                        OnComplete = onComplete,
                        IsPlaying = true
                    };
                    
                case UIAnimationType.Position:
                    var slideDistance = animateIn ? -100f : 100f;
                    return new SlideUIAnimation
                    {
                        Element = element,
                        FromPosition = animateIn ? Vector3.left * slideDistance : Vector3.zero,
                        ToPosition = animateIn ? Vector3.zero : Vector3.left * slideDistance,
                        Duration = duration,
                        StartTime = Time.time,
                        OnComplete = onComplete,
                        IsPlaying = true
                    };
                    
                case UIAnimationType.Scale:
                    return new ScaleUIAnimation
                    {
                        Element = element,
                        FromScale = animateIn ? Vector3.zero : Vector3.one,
                        ToScale = animateIn ? Vector3.one : Vector3.zero,
                        Duration = duration,
                        StartTime = Time.time,
                        OnComplete = onComplete,
                        IsPlaying = true
                    };
                    
                case UIAnimationType.Bounce:
                    return new BounceUIAnimation
                    {
                        Element = element,
                        FromScale = Vector3.one * 0.8f,
                        ToScale = Vector3.one,
                        Duration = duration,
                        StartTime = Time.time,
                        OnComplete = onComplete,
                        IsPlaying = true
                    };
                    
                default:
                    return new FadeUIAnimation
                    {
                        Element = element,
                        FromOpacity = animateIn ? 0f : 1f,
                        ToOpacity = animateIn ? 1f : 0f,
                        Duration = duration,
                        StartTime = Time.time,
                        OnComplete = onComplete,
                        IsPlaying = true
                    };
            }
        }
        
        private void StartAnimation(UIAnimation animation)
        {
            _activeAnimations.Add(animation);
            _elementAnimations[animation.Element] = animation;
            
            // Set initial state
            animation.OnStart();
        }
        
        public void StopAnimation(VisualElement element)
        {
            if (_elementAnimations.TryGetValue(element, out var animation))
            {
                animation.IsPlaying = false;
                _activeAnimations.Remove(animation);
                _elementAnimations.Remove(element);
            }
        }
        
        public void StopAllAnimations()
        {
            foreach (var animation in _activeAnimations)
            {
                animation.IsPlaying = false;
            }
            
            _activeAnimations.Clear();
            _elementAnimations.Clear();
        }
        
        public void Update()
        {
            for (int i = _activeAnimations.Count - 1; i >= 0; i--)
            {
                var animation = _activeAnimations[i];
                
                if (!animation.IsPlaying || animation.Element == null)
                {
                    _activeAnimations.RemoveAt(i);
                    continue;
                }
                
                float elapsed = Time.time - animation.StartTime;
                float t = Mathf.Clamp01(elapsed / animation.Duration);
                
                animation.UpdateAnimation(t);
                
                if (t >= 1f)
                {
                    animation.OnComplete?.Invoke();
                    animation.IsPlaying = false;
                    _activeAnimations.RemoveAt(i);
                    
                    if (_elementAnimations.ContainsKey(animation.Element))
                    {
                        _elementAnimations.Remove(animation.Element);
                    }
                }
            }
        }
        
        public bool IsAnimating(VisualElement element)
        {
            return _elementAnimations.ContainsKey(element);
        }
        
        public void Dispose()
        {
            StopAllAnimations();
        }
    }
    
    // Animation base class
    public abstract class UIAnimation
    {
        public VisualElement Element;
        public float Duration;
        public float StartTime;
        public bool IsPlaying;
        public System.Action OnComplete;
        
        public virtual void OnStart() { }
        public abstract void UpdateAnimation(float t);
    }
    
    // Specific animation implementations
    public class FadeUIAnimation : UIAnimation
    {
        public float FromOpacity;
        public float ToOpacity;
        
        public override void OnStart()
        {
            Element.style.opacity = FromOpacity;
        }
        
        public override void UpdateAnimation(float t)
        {
            float opacity = Mathf.Lerp(FromOpacity, ToOpacity, EaseInOut(t));
            Element.style.opacity = opacity;
        }
        
        private float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
    
    public class SlideUIAnimation : UIAnimation
    {
        public Vector3 FromPosition;
        public Vector3 ToPosition;
        
        public override void OnStart()
        {
            Element.transform.position = FromPosition;
        }
        
        public override void UpdateAnimation(float t)
        {
            Vector3 position = Vector3.Lerp(FromPosition, ToPosition, EaseOutCubic(t));
            Element.transform.position = position;
        }
        
        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
    
    public class ScaleUIAnimation : UIAnimation
    {
        public Vector3 FromScale;
        public Vector3 ToScale;
        
        public override void OnStart()
        {
            Element.transform.scale = FromScale;
        }
        
        public override void UpdateAnimation(float t)
        {
            Vector3 scale = Vector3.Lerp(FromScale, ToScale, EaseOutBack(t));
            Element.transform.scale = scale;
        }
        
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
    
    public class BounceUIAnimation : UIAnimation
    {
        public Vector3 FromScale;
        public Vector3 ToScale;
        
        public override void OnStart()
        {
            Element.transform.scale = FromScale;
        }
        
        public override void UpdateAnimation(float t)
        {
            Vector3 scale = Vector3.Lerp(FromScale, ToScale, EaseOutBounce(t));
            Element.transform.scale = scale;
        }
        
        private float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            
            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }
    }
    
    public class PulseUIAnimation : UIAnimation
    {
        public Vector3 OriginalScale;
        public Vector3 PulseScale;
        
        public override void UpdateAnimation(float t)
        {
            float pulseT = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
            Vector3 scale = Vector3.Lerp(OriginalScale, PulseScale, pulseT);
            Element.transform.scale = scale;
        }
    }
    
    public class ShakeUIAnimation : UIAnimation
    {
        public Vector3 OriginalPosition;
        public float Intensity;
        
        public override void UpdateAnimation(float t)
        {
            float shakeAmount = Intensity * (1f - t);
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f) * shakeAmount,
                UnityEngine.Random.Range(-1f, 1f) * shakeAmount,
                0f
            );
            
            Element.transform.position = OriginalPosition + randomOffset;
        }
    }
    
    public class PropertyUIAnimation : UIAnimation
    {
        public string PropertyName;
        public object FromValue;
        public object ToValue;
        
        public override void UpdateAnimation(float t)
        {
            // Property-specific interpolation logic would go here
            // This is a simplified implementation
        }
    }
}