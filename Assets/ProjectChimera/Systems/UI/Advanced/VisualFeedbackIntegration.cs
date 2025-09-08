using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Systems.UI.Advanced;
using ProjectChimera.Systems.Services.Core;
using System.Collections.Generic;
using System.Collections;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// Visual states for UI elements
    /// </summary>
    public enum VisualState
    {
        Normal,
        Hovered,
        Selected,
        Disabled,
        Loading,
        Error,
        Success,
        Highlighted
    }

    /// <summary>
    /// Phase 2.3.3: Visual Feedback Integration
    /// Provides comprehensive visual feedback for menu system including animations,
    /// hover effects, selection feedback, and contextual visual cues
    /// </summary>
    public class VisualFeedbackIntegration : MonoBehaviour
    {
        // Stub methods for test compilation - to be implemented by UI team
        public bool IsSystemReady() => true;

        public void OnHoverEnter(string elementId) { }
        public void OnHoverExit(string elementId) { }

        public void PlayFadeInAnimation(string elementId, float duration) { }
        public void PlayFadeInAnimation(string elementId) { }
        public void PlayScaleAnimation(string elementId, Vector3 scale, float duration, string ease) { }
        public void PlayScaleAnimation(string elementId, float scaleX, float scaleY, float scaleZ, float duration) { }
        public void PlayScaleAnimation(string elementId, Vector3 scale, float duration, float ease) { }
        public void PlayScaleAnimation(string elementId, Vector3 scale, string ease) { }
        public void PlayScaleAnimation(string elementId, Vector3 fromScale, Vector3 toScale, float duration) { }
        public void PlaySlideAnimation(string elementId, Vector3 direction, float duration, string ease) { }
        public void PlaySlideAnimation(string elementId, float deltaX, float deltaY, float deltaZ, float duration) { }
        public void PlaySlideAnimation(string elementId, Vector3 direction, float duration, float ease) { }
        public void PlaySlideAnimation(string elementId, Vector3 direction, string ease) { }
        public void PlaySlideAnimation(string elementId, Vector3 fromPosition, Vector3 toPosition, float duration) { }

        public void SetVisualState(string elementId, VisualState state) { }
        public void SetVisualState(string elementId, string stateName) { }
        public void TransitionToState(string elementId, VisualState state, float duration) { }
        public void TransitionToState(string elementId, string stateName, float duration) { }

        public void OnMenuOpened() { }
    public event Action OnAnimationCompletedEvent;
    public Action OnAnimationCompletedDelegate { get; set; }
    public Action OnAnimationCompleted { get; set; }
        public void OnMenuContentChanged() { }
        [Header("Visual Feedback Configuration")]
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private bool _enableHoverEffects = true;
        [SerializeField] private bool _enableSelectionFeedback = true;
        [SerializeField] private bool _enableContextualCues = true;
        [SerializeField] private bool _enableProgressIndicators = true;

        [Header("Animation Settings")]
        [SerializeField] private float _menuShowDuration = 0.3f;
        [SerializeField] private float _menuHideDuration = 0.2f;
        [SerializeField] private float _itemHoverDuration = 0.15f;
        [SerializeField] private AnimationCurve _showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Hover Effects")]
        [SerializeField] private float _hoverScaleMultiplier = 1.05f;
        [SerializeField] private Color _hoverColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private float _hoverBorderWidth = 2f;
        [SerializeField] private Color _hoverBorderColor = new Color(0.3f, 0.6f, 1f, 1f);

        [Header("Selection Effects")]
        [SerializeField] private Color _selectionColor = new Color(0.2f, 0.5f, 1f, 0.3f);
        [SerializeField] private float _selectionPulseSpeed = 2f;
        [SerializeField] private float _selectionGlowIntensity = 1.2f;

        [Header("Contextual Cues")]
        [SerializeField] private bool _showAvailabilityIndicators = true;
        [SerializeField] private bool _showRelevanceScoring = true;
        [SerializeField] private bool _showResourceRequirements = true;
        [SerializeField] private bool _showSkillRequirements = true;

        [Header("Icon and Badge System")]
        [SerializeField] private bool _enableIconSystem = true;
        [SerializeField] private bool _enableBadgeSystem = true;
        [SerializeField] private Sprite[] _pillarIcons;
        [SerializeField] private Sprite[] _statusIcons;

        // System references
        private AdvancedMenuSystem _menuSystem;
        private ContextAwareActionFilter _actionFilter;
        private VisualFeedbackSystem _feedbackSystem;

        // Animation tracking
        private Dictionary<VisualElement, Coroutine> _activeAnimations = new Dictionary<VisualElement, Coroutine>();
        private Dictionary<VisualElement, VisualFeedbackState> _elementStates = new Dictionary<VisualElement, VisualFeedbackState>();

        // Visual effect pools
        private Queue<VisualElement> _hoverEffectPool = new Queue<VisualElement>();
        private Queue<VisualElement> _selectionEffectPool = new Queue<VisualElement>();

        // Events
        public event Action<VisualElement, string> OnVisualFeedbackTriggered;
        public event Action<VisualElement> OnElementAnimationCompleted;

        private void Awake()
        {
            InitializeVisualSystem();
        }

        private void Start()
        {
            SetupVisualIntegration();
            CreateEffectPools();
        }

        private void InitializeVisualSystem()
        {
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            _actionFilter = GetComponent<ContextAwareActionFilter>();
            _feedbackSystem = GetComponent<VisualFeedbackSystem>();

            if (_menuSystem == null)
            {
                ChimeraLogger.LogError("[VisualFeedbackIntegration] AdvancedMenuSystem component required");
                enabled = false;
                return;
            }
        }

        private void SetupVisualIntegration()
        {
            // Subscribe to menu system events
            _menuSystem.OnMenuOpened += OnMenuOpened;
            _menuSystem.OnMenuClosed += OnMenuClosed;
            _menuSystem.OnActionExecuted += OnActionExecuted;

            // Subscribe to action filter events if available
            if (_actionFilter != null)
            {
                _actionFilter.OnActionsFiltered += OnActionsFiltered;
                _actionFilter.OnRelevanceScoreUpdated += OnRelevanceScoreUpdated;
            }
        }

        /// <summary>
        /// Apply visual feedback to a menu element
        /// </summary>
        public void ApplyVisualFeedback(VisualElement element, MenuAction action, MenuContext context)
        {
            if (element == null || action == null)
                return;

            // Initialize element state
            InitializeElementState(element, action);

            // Apply base styling
            ApplyBaseVisualStyling(element, action);

            // Apply contextual cues
            if (_enableContextualCues)
            {
                ApplyContextualCues(element, action, context);
            }

            // Setup interaction effects
            SetupInteractionEffects(element, action);

            // Apply relevance-based visual indicators
            ApplyRelevanceIndicators(element, action);
        }

        /// <summary>
        /// Apply visual feedback to a menu category
        /// </summary>
        public void ApplyVisualFeedback(VisualElement element, MenuCategory category, MenuContext context)
        {
            if (element == null || category == null)
                return;

            // Initialize element state
            InitializeElementState(element, category);

            // Apply base styling
            ApplyBaseCategoryStyling(element, category);

            // Apply pillar-specific styling
            ApplyPillarStyling(element, category);

            // Setup interaction effects
            SetupCategoryInteractionEffects(element, category);
        }

        private void InitializeElementState(VisualElement element, object data)
        {
            if (!_elementStates.ContainsKey(element))
            {
                _elementStates[element] = new VisualFeedbackState
                {
                    Element = element,
                    OriginalScale = Vector3.one,
                    OriginalColor = Color.white,
                    IsHovered = false,
                    IsSelected = false,
                    CreationTime = Time.time
                };
            }
        }

        private void ApplyBaseVisualStyling(VisualElement element, MenuAction action)
        {
            // Base element styling
            element.AddToClassList("menu-action-item");
            element.style.borderTopLeftRadius = 4f;
            element.style.borderTopRightRadius = 4f;
            element.style.borderBottomLeftRadius = 4f;
            element.style.borderBottomRightRadius = 4f;
            element.style.paddingLeft = 8f;
            element.style.paddingRight = 8f;
            element.style.paddingTop = 4f;
            element.style.paddingBottom = 4f;
            element.style.marginBottom = 2f;

            // Apply enabled/disabled state
            if (!action.IsEnabled)
            {
                element.AddToClassList("action-disabled");
                element.style.opacity = 0.5f;
            }
            else
            {
                element.RemoveFromClassList("action-disabled");
                element.style.opacity = 1f;
            }

            // Apply visibility
            element.style.display = action.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyBaseCategoryStyling(VisualElement element, MenuCategory category)
        {
            // Base category styling
            element.AddToClassList("menu-category-item");
            element.style.borderTopLeftRadius = 6f;
            element.style.borderTopRightRadius = 6f;
            element.style.borderBottomLeftRadius = 6f;
            element.style.borderBottomRightRadius = 6f;
            element.style.paddingLeft = 12f;
            element.style.paddingRight = 12f;
            element.style.paddingTop = 8f;
            element.style.paddingBottom = 8f;
            element.style.marginBottom = 4f;
            element.style.borderLeftWidth = 3f;

            // Apply visibility
            element.style.display = category.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyPillarStyling(VisualElement element, MenuCategory category)
        {
            Color pillarColor = GetPillarColor(category.PillarType);

            element.style.borderLeftColor = pillarColor;
            element.style.backgroundColor = new StyleColor(new Color(pillarColor.r, pillarColor.g, pillarColor.b, 0.1f));

            // Add pillar icon if enabled
            if (_enableIconSystem && _pillarIcons != null && _pillarIcons.Length > 0)
            {
                var iconIndex = GetPillarIconIndex(category.PillarType);
                if (iconIndex >= 0 && iconIndex < _pillarIcons.Length)
                {
                    var icon = CreateIconElement(_pillarIcons[iconIndex]);
                    element.Insert(0, icon);
                }
            }
        }

        private void ApplyContextualCues(VisualElement element, MenuAction action, MenuContext context)
        {
            // Show availability indicators
            if (_showAvailabilityIndicators)
            {
                var isAvailable = CanExecuteAction(action, context);
                var availabilityIndicator = CreateAvailabilityIndicator(isAvailable);
                element.Add(availabilityIndicator);
            }

            // Show resource requirements
            if (_showResourceRequirements && action.ResourceRequirements != null)
            {
                foreach (var requirement in action.ResourceRequirements)
                {
                    var indicator = CreateResourceIndicator(requirement);
                    element.Add(indicator);
                }
            }

            // Show skill requirements
            if (_showSkillRequirements && action.RequiredSkills != null && action.RequiredSkills.Length > 0)
            {
                var skillIndicator = CreateSkillIndicator(action.RequiredSkills);
                element.Add(skillIndicator);
            }
        }

        private void ApplyRelevanceIndicators(VisualElement element, MenuAction action)
        {
            if (!_showRelevanceScoring)
                return;

            if (action.Parameters.TryGetValue("RelevanceScore", out var scoreObj) && scoreObj is float score)
            {
                // Apply relevance-based opacity
                var relevanceOpacity = Mathf.Lerp(0.6f, 1f, score);
                element.style.opacity = relevanceOpacity;

                // Add relevance border
                var borderColor = Color.Lerp(Color.gray, Color.green, score);
                element.style.borderTopColor = borderColor;
                element.style.borderTopWidth = 1f;

                // Add relevance score badge if high relevance
                if (score > 0.8f && _enableBadgeSystem)
                {
                    var badge = CreateRelevanceBadge(score);
                    element.Add(badge);
                }
            }
        }

        private void SetupInteractionEffects(VisualElement element, MenuAction action)
        {
            if (_enableHoverEffects)
            {
                element.RegisterCallback<MouseEnterEvent>(evt => OnElementHovered(element, true));
                element.RegisterCallback<MouseLeaveEvent>(evt => OnElementHovered(element, false));
            }

            if (_enableSelectionFeedback)
            {
                element.RegisterCallback<MouseDownEvent>(evt => OnElementSelected(element, true));
                element.RegisterCallback<MouseUpEvent>(evt => OnElementSelected(element, false));
            }
        }

        private void SetupCategoryInteractionEffects(VisualElement element, MenuCategory category)
        {
            if (_enableHoverEffects)
            {
                element.RegisterCallback<MouseEnterEvent>(evt => OnCategoryHovered(element, true));
                element.RegisterCallback<MouseLeaveEvent>(evt => OnCategoryHovered(element, false));
            }

            if (_enableSelectionFeedback)
            {
                element.RegisterCallback<ClickEvent>(evt => OnCategoryClicked(element, category));
            }
        }

        private void OnElementHovered(VisualElement element, bool isHovered)
        {
            if (!_elementStates.TryGetValue(element, out var state))
                return;

            state.IsHovered = isHovered;

            if (isHovered)
            {
                ApplyHoverEffect(element);
            }
            else
            {
                RemoveHoverEffect(element);
            }

            OnVisualFeedbackTriggered?.Invoke(element, isHovered ? "Hovered" : "Unhovered");
        }

        private void OnCategoryHovered(VisualElement element, bool isHovered)
        {
            if (!_elementStates.TryGetValue(element, out var state))
                return;

            state.IsHovered = isHovered;

            if (isHovered)
            {
                ApplyCategoryHoverEffect(element);
            }
            else
            {
                RemoveCategoryHoverEffect(element);
            }
        }

        private void OnElementSelected(VisualElement element, bool isSelected)
        {
            if (!_elementStates.TryGetValue(element, out var state))
                return;

            state.IsSelected = isSelected;

            if (isSelected)
            {
                ApplySelectionEffect(element);
            }
            else
            {
                RemoveSelectionEffect(element);
            }
        }

        private void OnCategoryClicked(VisualElement element, MenuCategory category)
        {
            // Create click ripple effect
            CreateRippleEffect(element);

            // Flash effect
            StartCoroutine(FlashEffect(element, GetPillarColor(category.PillarType)));
        }

        private void ApplyHoverEffect(VisualElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
            }

            _activeAnimations[element] = StartCoroutine(AnimateHoverIn(element));
        }

        private void RemoveHoverEffect(VisualElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
            }

            _activeAnimations[element] = StartCoroutine(AnimateHoverOut(element));
        }

        private void ApplyCategoryHoverEffect(VisualElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
            }

            _activeAnimations[element] = StartCoroutine(AnimateCategoryHover(element, true));
        }

        private void RemoveCategoryHoverEffect(VisualElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
            }

            _activeAnimations[element] = StartCoroutine(AnimateCategoryHover(element, false));
        }

        private void ApplySelectionEffect(VisualElement element)
        {
            element.style.backgroundColor = _selectionColor;

            // Add selection pulse effect
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
            }

            _activeAnimations[element] = StartCoroutine(PulseEffect(element));
        }

        private void RemoveSelectionEffect(VisualElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                StopCoroutine(_activeAnimations[element]);
                _activeAnimations.Remove(element);
            }

            // Reset background color
            element.style.backgroundColor = StyleKeyword.Null;
        }

        private void CreateRippleEffect(VisualElement element)
        {
            var ripple = new VisualElement();
            ripple.AddToClassList("ripple-effect");
            ripple.style.position = Position.Absolute;
            ripple.style.width = 0;
            ripple.style.height = 0;
            ripple.style.borderTopLeftRadius = Length.Percent(50);
            ripple.style.borderTopRightRadius = Length.Percent(50);
            ripple.style.borderBottomLeftRadius = Length.Percent(50);
            ripple.style.borderBottomRightRadius = Length.Percent(50);
            ripple.style.backgroundColor = new Color(1f, 1f, 1f, 0.3f);

            element.Add(ripple);

            StartCoroutine(AnimateRipple(ripple, element));
        }

        private VisualElement CreateAvailabilityIndicator(bool isAvailable)
        {
            var indicator = new VisualElement();
            indicator.AddToClassList("availability-indicator");
            indicator.style.width = 8f;
            indicator.style.height = 8f;
            indicator.style.borderTopLeftRadius = Length.Percent(50);
            indicator.style.borderTopRightRadius = Length.Percent(50);
            indicator.style.borderBottomLeftRadius = Length.Percent(50);
            indicator.style.borderBottomRightRadius = Length.Percent(50);
            indicator.style.backgroundColor = isAvailable ? Color.green : Color.red;
            indicator.style.position = Position.Absolute;
            indicator.style.top = 4f;
            indicator.style.right = 4f;

            return indicator;
        }

        private VisualElement CreateResourceIndicator(ResourceRequirement requirement)
        {
            var indicator = new Label($"{requirement.ResourceType}: {requirement.Amount}");
            indicator.AddToClassList("resource-indicator");
            indicator.style.fontSize = 10f;
            indicator.style.color = Color.yellow;
            indicator.style.position = Position.Absolute;
            indicator.style.bottom = 2f;
            indicator.style.right = 2f;

            return indicator;
        }

        private VisualElement CreateSkillIndicator(string[] skills)
        {
            var indicator = new Label($"Skills: {string.Join(", ", skills)}");
            indicator.AddToClassList("skill-indicator");
            indicator.style.fontSize = 9f;
            indicator.style.color = Color.cyan;
            indicator.style.position = Position.Absolute;
            indicator.style.bottom = 12f;
            indicator.style.right = 2f;

            return indicator;
        }

        private VisualElement CreateRelevanceBadge(float score)
        {
            var badge = new Label("★");
            badge.AddToClassList("relevance-badge");
            badge.style.fontSize = 14f;
            badge.style.color = Color.gold;
            badge.style.position = Position.Absolute;
            badge.style.top = -2f;
            badge.style.right = -2f;

            return badge;
        }

        private VisualElement CreateIconElement(Sprite icon)
        {
            var iconElement = new VisualElement();
            iconElement.AddToClassList("pillar-icon");
            iconElement.style.width = 16f;
            iconElement.style.height = 16f;
            iconElement.style.marginRight = 4f;

            // In a real implementation, you would set the background image
            iconElement.style.backgroundColor = Color.white;

            return iconElement;
        }

        private Color GetPillarColor(string pillarType)
        {
            return pillarType?.ToLower() switch
            {
                "construction" => new Color(1f, 0.6f, 0.2f, 1f),
                "cultivation" => new Color(0.2f, 0.8f, 0.2f, 1f),
                "genetics" => new Color(0.6f, 0.2f, 1f, 1f),
                _ => Color.white
            };
        }

        private int GetPillarIconIndex(string pillarType)
        {
            return pillarType?.ToLower() switch
            {
                "construction" => 0,
                "cultivation" => 1,
                "genetics" => 2,
                _ => -1
            };
        }

        private bool CanExecuteAction(MenuAction action, MenuContext context)
        {
            // Simplified availability check
            return action.IsEnabled && (action.ConditionCallback?.Invoke(context) ?? true);
        }

        // Animation coroutines
        private IEnumerator AnimateHoverIn(VisualElement element)
        {
            float elapsed = 0f;

            while (elapsed < _itemHoverDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _itemHoverDuration;

                // Scale effect
                float scale = Mathf.Lerp(1f, _hoverScaleMultiplier, progress);
                element.style.scale = new StyleScale(new Scale(Vector3.one * scale));

                // Background color effect
                var bgColor = Color.Lerp(Color.clear, _hoverColor, progress);
                element.style.backgroundColor = bgColor;

                // Border effect
                element.style.borderTopColor = _hoverBorderColor;
                element.style.borderTopWidth = Mathf.Lerp(0f, _hoverBorderWidth, progress);

                yield return null;
            }

            _activeAnimations.Remove(element);
        }

        private IEnumerator AnimateHoverOut(VisualElement element)
        {
            float elapsed = 0f;

            while (elapsed < _itemHoverDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _itemHoverDuration;

                // Scale effect
                float scale = Mathf.Lerp(_hoverScaleMultiplier, 1f, progress);
                element.style.scale = new StyleScale(new Scale(Vector3.one * scale));

                // Background color effect
                var bgColor = Color.Lerp(_hoverColor, Color.clear, progress);
                element.style.backgroundColor = bgColor;

                // Border effect
                element.style.borderTopWidth = Mathf.Lerp(_hoverBorderWidth, 0f, progress);

                yield return null;
            }

            // Reset to defaults
            element.style.scale = StyleKeyword.Null;
            element.style.backgroundColor = StyleKeyword.Null;
            element.style.borderTopWidth = 0f;

            _activeAnimations.Remove(element);
        }

        private IEnumerator AnimateCategoryHover(VisualElement element, bool hoverIn)
        {
            float elapsed = 0f;
            float startOpacity = hoverIn ? 1f : 1.2f;
            float endOpacity = hoverIn ? 1.2f : 1f;

            while (elapsed < _itemHoverDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _itemHoverDuration;

                float opacity = Mathf.Lerp(startOpacity, endOpacity, progress);
                element.style.opacity = opacity;

                yield return null;
            }

            _activeAnimations.Remove(element);
        }

        private IEnumerator PulseEffect(VisualElement element)
        {
            while (_elementStates.TryGetValue(element, out var state) && state.IsSelected)
            {
                float pulse = (Mathf.Sin(Time.time * _selectionPulseSpeed) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(1f, _selectionGlowIntensity, pulse);

                var color = _selectionColor;
                color.a *= intensity;
                element.style.backgroundColor = color;

                yield return null;
            }

            _activeAnimations.Remove(element);
        }

        private IEnumerator FlashEffect(VisualElement element, Color flashColor)
        {
            var originalColor = element.style.backgroundColor;

            // Flash to color
            element.style.backgroundColor = flashColor;
            yield return new WaitForSeconds(0.1f);

            // Fade back to original
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                var currentColor = Color.Lerp(flashColor, Color.clear, progress);
                element.style.backgroundColor = currentColor;

                yield return null;
            }

            element.style.backgroundColor = originalColor;
        }

        private IEnumerator AnimateRipple(VisualElement ripple, VisualElement parent)
        {
            var parentRect = parent.layout;
            float maxSize = Mathf.Max(parentRect.width, parentRect.height) * 1.5f;

            float elapsed = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                float size = Mathf.Lerp(0f, maxSize, progress);
                float opacity = Mathf.Lerp(0.3f, 0f, progress);

                ripple.style.width = size;
                ripple.style.height = size;
                ripple.style.left = (parentRect.width - size) * 0.5f;
                ripple.style.top = (parentRect.height - size) * 0.5f;

                var color = ripple.style.backgroundColor.value;
                color.a = opacity;
                ripple.style.backgroundColor = color;

                yield return null;
            }

            ripple.RemoveFromHierarchy();
        }

        private void CreateEffectPools()
        {
            // Pre-create effect elements for performance
            for (int i = 0; i < 10; i++)
            {
                var hoverEffect = new VisualElement();
                hoverEffect.AddToClassList("hover-effect");
                _hoverEffectPool.Enqueue(hoverEffect);

                var selectionEffect = new VisualElement();
                selectionEffect.AddToClassList("selection-effect");
                _selectionEffectPool.Enqueue(selectionEffect);
            }
        }

        // Event handlers
        private void OnMenuOpened(string menuId)
        {
            if (_enableAnimations)
            {
                // Menu opening animation would be handled by the menu system itself
                // This is for additional feedback
                _feedbackSystem?.ShowFeedback("Menu opened", FeedbackType.Info, 1f);
            }
        }

        private void OnMenuClosed()
        {
            if (_enableAnimations)
            {
                _feedbackSystem?.ShowFeedback("Menu closed", FeedbackType.Info, 1f);
            }
        }

        private void OnActionExecuted(string actionId, MenuAction action)
        {
            _feedbackSystem?.ShowFeedback($"Executed: {action.DisplayName}", FeedbackType.Success, 2f);
        }

        private void OnActionsFiltered(MenuContext context, List<MenuAction> actions)
        {
            // Update visual indicators based on filtered actions
        }

        private void OnRelevanceScoreUpdated(string actionId, float score)
        {
            // Update relevance indicators for the action
        }

        private void OnDestroy()
        {
            // Clean up animations
            foreach (var animation in _activeAnimations.Values)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }
            _activeAnimations.Clear();
        }

        // Public API
        public void SetHoverEnabled(bool enabled) => _enableHoverEffects = enabled;
        public void SetAnimationsEnabled(bool enabled) => _enableAnimations = enabled;
        public void SetSelectionFeedbackEnabled(bool enabled) => _enableSelectionFeedback = enabled;
        public bool IsAnimating(VisualElement element) => _activeAnimations.ContainsKey(element);
        public int GetActiveAnimationCount() => _activeAnimations.Count;
    }

    // Supporting data structure
    public class VisualFeedbackState
    {
        public VisualElement Element;
        public Vector3 OriginalScale;
        public Color OriginalColor;
        public bool IsHovered;
        public bool IsSelected;
        public float CreationTime;
        public Dictionary<string, object> CustomProperties = new Dictionary<string, object>();
    }
}
