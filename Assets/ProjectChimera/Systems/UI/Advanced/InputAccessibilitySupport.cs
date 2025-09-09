using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Accessibility support for advanced input systems.
    /// Provides screen reader support, high contrast modes, and other accessibility features.
    /// </summary>
    [RequireComponent(typeof(InputNavigationCore))]
    public class InputAccessibilitySupport : MonoBehaviour
    {
        [Header("Accessibility Configuration")]
        [SerializeField] private bool _enableScreenReader = false;
        [SerializeField] private bool _enableHighContrastMode = false;
        [SerializeField] private bool _enableSlowMotionMode = false;
        [SerializeField] private bool _enableReducedMotion = false;
        [SerializeField] private bool _enableColorBlindSupport = false;
        
        [Header("Navigation Assistance")]
        [SerializeField, Range(0.5f, 3f)] private float _navigationSpeed = 1f;
        [SerializeField, Range(0.1f, 2f)] private float _focusIndicatorScale = 1f;
        [SerializeField] private bool _enableSpatialAudio = false;
        [SerializeField] private bool _enableVoiceAnnouncements = false;
        
        [Header("Visual Accessibility")]
        [SerializeField] private Color _highContrastBorderColor = Color.yellow;
        [SerializeField] private Color _highContrastBackgroundColor = Color.black;
        [SerializeField] private Color _highContrastTextColor = Color.white;
        [SerializeField, Range(1f, 3f)] private float _textSizeMultiplier = 1f;
        
        [Header("Motor Accessibility")]
        [SerializeField, Range(0.1f, 5f)] private float _inputHoldTime = 0.5f;
        [SerializeField] private bool _enableStickyKeys = false;
        [SerializeField] private bool _enableClickAndHold = false;
        [SerializeField, Range(0.1f, 2f)] private float _doubleClickTime = 0.3f;
        
        // System references
        private InputNavigationCore _navigationCore;
        private AudioSource _audioSource;
        
        // State tracking
        private Dictionary<VisualElement, AccessibilityInfo> _accessibilityData = new Dictionary<VisualElement, AccessibilityInfo>();
        private bool _isHighContrastActive = false;
        private bool _isSlowMotionActive = false;
        private float _originalTimeScale = 1f;
        
        // Voice and audio
        private Queue<string> _announcementQueue = new Queue<string>();
        private bool _isAnnouncing = false;
        
        // Motor assistance
        private Dictionary<Key, float> _keyHoldTimes = new Dictionary<Key, float>();
        private Dictionary<Key, bool> _stickyKeyStates = new Dictionary<Key, bool>();
        
        // Events
        public event Action<bool> OnHighContrastToggled;
        public event Action<bool> OnSlowMotionToggled;
        public event Action<string> OnScreenReaderAnnouncement;
        public event Action<VisualElement> OnElementDescribed;
        
        // Properties
        public bool EnableScreenReader { get => _enableScreenReader; set => _enableScreenReader = value; }
        public bool EnableHighContrastMode { get => _enableHighContrastMode; set => SetHighContrastMode(value); }
        public bool EnableSlowMotionMode { get => _enableSlowMotionMode; set => SetSlowMotionMode(value); }
        public bool IsHighContrastActive => _isHighContrastActive;
        public bool IsSlowMotionActive => _isSlowMotionActive;
        public float NavigationSpeed { get => _navigationSpeed; set => _navigationSpeed = Mathf.Clamp(value, 0.5f, 3f); }
        
        private void Awake()
        {
            InitializeAccessibility();
        }
        
        private void Start()
        {
            SetupAccessibilityFeatures();
            StartCoroutine(AccessibilityUpdateLoop());
        }
        
        private void Update()
        {
            HandleMotorAssistance();
        }
        
        private void InitializeAccessibility()
        {
            _navigationCore = GetComponent<InputNavigationCore>();
            _audioSource = GetComponent<AudioSource>();
            
            if (_navigationCore == null)
            {
                ChimeraLogger.LogError("[InputAccessibilitySupport] InputNavigationCore component required");
                enabled = false;
                return;
            }
            
            // Create audio source if needed
            if (_audioSource == null && (_enableSpatialAudio || _enableVoiceAnnouncements))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.volume = 0.7f;
            }
            
            // Subscribe to navigation events
            _navigationCore.OnElementFocused += OnElementFocused;
            _navigationCore.OnElementSelected += OnElementSelected;
            
            ChimeraLogger.Log("[InputAccessibilitySupport] Accessibility support initialized");
        }
        
        private void SetupAccessibilityFeatures()
        {
            // Apply initial accessibility settings
            if (_enableHighContrastMode)
            {
                SetHighContrastMode(true);
            }
            
            if (_enableSlowMotionMode)
            {
                SetSlowMotionMode(true);
            }
            
            // Setup reduced motion if enabled
            if (_enableReducedMotion)
            {
                SetReducedMotion(true);
            }
        }
        
        /// <summary>
        /// Toggle high contrast mode for better visibility
        /// </summary>
        public void SetHighContrastMode(bool enabled)
        {
            if (_isHighContrastActive == enabled)
                return;
            
            _isHighContrastActive = enabled;
            _enableHighContrastMode = enabled;
            
            ApplyHighContrastStyles(enabled);
            OnHighContrastToggled?.Invoke(enabled);
            
            AnnounceToUser(enabled ? "High contrast mode enabled" : "High contrast mode disabled");
            ChimeraLogger.Log($"[InputAccessibilitySupport] High contrast mode: {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Toggle slow motion mode for better input timing
        /// </summary>
        public void SetSlowMotionMode(bool enabled)
        {
            if (_isSlowMotionActive == enabled)
                return;
            
            _isSlowMotionActive = enabled;
            _enableSlowMotionMode = enabled;
            
            if (enabled)
            {
                _originalTimeScale = Time.timeScale;
                Time.timeScale = 0.5f;
            }
            else
            {
                Time.timeScale = _originalTimeScale;
            }
            
            OnSlowMotionToggled?.Invoke(enabled);
            AnnounceToUser(enabled ? "Slow motion mode enabled" : "Slow motion mode disabled");
            ChimeraLogger.Log($"[InputAccessibilitySupport] Slow motion mode: {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Set reduced motion mode to minimize animations
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            _enableReducedMotion = enabled;
            
            // Apply reduced motion styles to UI elements
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument != null)
            {
                ApplyReducedMotionStyles(rootDocument.rootVisualElement, enabled);
            }
            
            AnnounceToUser(enabled ? "Reduced motion enabled" : "Reduced motion disabled");
            ChimeraLogger.Log($"[InputAccessibilitySupport] Reduced motion: {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Register accessibility information for an element
        /// </summary>
        public void RegisterAccessibilityInfo(VisualElement element, string description, string role = "", string instructions = "")
        {
            if (element == null)
                return;
            
            var info = new AccessibilityInfo
            {
                Description = description,
                Role = role,
                Instructions = instructions,
                IsInteractable = element.focusable
            };
            
            _accessibilityData[element] = info;
        }
        
        /// <summary>
        /// Get accessibility information for an element
        /// </summary>
        public AccessibilityInfo GetAccessibilityInfo(VisualElement element)
        {
            return _accessibilityData.TryGetValue(element, out var info) ? info : null;
        }
        
        /// <summary>
        /// Announce text to screen reader or audio system
        /// </summary>
        public void AnnounceToUser(string message, bool priority = false)
        {
            if (!_enableScreenReader && !_enableVoiceAnnouncements)
                return;
            
            if (priority)
            {
                _announcementQueue.Clear();
            }
            
            _announcementQueue.Enqueue(message);
            OnScreenReaderAnnouncement?.Invoke(message);
            
            if (!_isAnnouncing)
            {
                StartCoroutine(ProcessAnnouncementQueue());
            }
        }
        
        /// <summary>
        /// Describe the currently focused element
        /// </summary>
        public void DescribeCurrentElement()
        {
            var currentElement = _navigationCore.CurrentFocusedElement;
            if (currentElement == null)
            {
                AnnounceToUser("No element focused");
                return;
            }
            
            DescribeElement(currentElement);
        }
        
        /// <summary>
        /// Play spatial audio cue for navigation
        /// </summary>
        public void PlayNavigationCue(NavigationDirection direction)
        {
            if (!_enableSpatialAudio || _audioSource == null)
                return;
            
            // This would play different audio cues for different directions
            // Implementation would depend on available audio clips
            float pitch = direction switch
            {
                NavigationDirection.Up => 1.2f,
                NavigationDirection.Down => 0.8f,
                NavigationDirection.Left => 1.0f,
                NavigationDirection.Right => 1.1f,
                _ => 1.0f
            };
            
            _audioSource.pitch = pitch;
            // _audioSource.PlayOneShot(navigationClip); // Would need audio clip
        }
        
        private void ApplyHighContrastStyles(bool enabled)
        {
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument == null)
                return;
            
            var root = rootDocument.rootVisualElement;
            ApplyHighContrastToElement(root, enabled);
        }
        
        private void ApplyHighContrastToElement(VisualElement element, bool enabled)
        {
            if (element == null)
                return;
            
            if (enabled)
            {
                element.AddToClassList("high-contrast-mode");
                
                // Apply high contrast colors
                if (element.focusable)
                {
                    element.style.borderLeftColor = _highContrastBorderColor;
                    element.style.borderRightColor = _highContrastBorderColor;
                    element.style.borderTopColor = _highContrastBorderColor;
                    element.style.borderBottomColor = _highContrastBorderColor;
                    element.style.borderLeftWidth = 2f * _focusIndicatorScale;
                    element.style.borderRightWidth = 2f * _focusIndicatorScale;
                    element.style.borderTopWidth = 2f * _focusIndicatorScale;
                    element.style.borderBottomWidth = 2f * _focusIndicatorScale;
                }
                
                // Apply to labels and text elements
                if (element is Label label)
                {
                    label.style.color = _highContrastTextColor;
                    label.style.fontSize = label.style.fontSize.value.value * _textSizeMultiplier;
                }
            }
            else
            {
                element.RemoveFromClassList("high-contrast-mode");
                // Reset styles would go here
            }
            
            // Recursively apply to children
            foreach (var child in element.Children())
            {
                ApplyHighContrastToElement(child, enabled);
            }
        }
        
        private void ApplyReducedMotionStyles(VisualElement element, bool enabled)
        {
            if (element == null)
                return;
            
            if (enabled)
            {
                element.AddToClassList("reduced-motion");
                // Disable transitions and animations
                element.style.transitionDuration = new List<TimeValue> { 0f };
            }
            else
            {
                element.RemoveFromClassList("reduced-motion");
            }
            
            // Recursively apply to children
            foreach (var child in element.Children())
            {
                ApplyReducedMotionStyles(child, enabled);
            }
        }
        
        private void DescribeElement(VisualElement element)
        {
            if (element == null)
                return;
            
            var description = BuildElementDescription(element);
            AnnounceToUser(description);
            OnElementDescribed?.Invoke(element);
        }
        
        private string BuildElementDescription(VisualElement element)
        {
            var parts = new List<string>();
            
            // Get accessibility info if available
            if (_accessibilityData.TryGetValue(element, out var info))
            {
                if (!string.IsNullOrEmpty(info.Role))
                    parts.Add(info.Role);
                
                if (!string.IsNullOrEmpty(info.Description))
                    parts.Add(info.Description);
                
                if (!string.IsNullOrEmpty(info.Instructions))
                    parts.Add(info.Instructions);
            }
            else
            {
                // Build description from element properties
                parts.Add(GetElementType(element));
                
                var text = GetElementText(element);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
                
                if (element.focusable)
                    parts.Add("focusable");
            }
            
            return string.Join(", ", parts);
        }
        
        private string GetElementType(VisualElement element)
        {
            if (element is Button) return "Button";
            if (element is Label) return "Label";
            if (element is TextField) return "Text Field";
            if (element is Toggle) return "Toggle";
            if (element is ScrollView) return "Scroll View";
            
            return "Element";
        }
        
        private string GetElementText(VisualElement element)
        {
            if (element is Label label) return label.text;
            if (element is Button button) return button.text;
            if (element is TextField textField) return textField.value;
            
            return element.name;
        }
        
        private void HandleMotorAssistance()
        {
            if (!_enableStickyKeys && !_enableClickAndHold)
                return;
            
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            
            // Handle sticky keys
            if (_enableStickyKeys)
            {
                HandleStickyKeys(keyboard);
            }
            
            // Handle click and hold
            if (_enableClickAndHold)
            {
                HandleClickAndHold(keyboard);
            }
        }
        
        private void HandleStickyKeys(Keyboard keyboard)
        {
            // Toggle sticky state for modifier keys
            if (keyboard.ctrlKey.wasPressedThisFrame)
                _stickyKeyStates[Key.LeftCtrl] = !_stickyKeyStates.GetValueOrDefault(Key.LeftCtrl);
            
            if (keyboard.shiftKey.wasPressedThisFrame)
                _stickyKeyStates[Key.LeftShift] = !_stickyKeyStates.GetValueOrDefault(Key.LeftShift);
            
            if (keyboard.altKey.wasPressedThisFrame)
                _stickyKeyStates[Key.LeftAlt] = !_stickyKeyStates.GetValueOrDefault(Key.LeftAlt);
        }
        
        private void HandleClickAndHold(Keyboard keyboard)
        {
            // Track key hold times
            foreach (Key key in System.Enum.GetValues(typeof(Key)))
            {
                if (keyboard[key].isPressed)
                {
                    if (!_keyHoldTimes.ContainsKey(key))
                    {
                        _keyHoldTimes[key] = Time.time;
                    }
                    else if (Time.time - _keyHoldTimes[key] >= _inputHoldTime)
                    {
                        // Trigger held action
                        OnKeyHeld(key);
                        _keyHoldTimes[key] = Time.time; // Reset timer
                    }
                }
                else
                {
                    _keyHoldTimes.Remove(key);
                }
            }
        }
        
        private void OnKeyHeld(Key key)
        {
            // Handle held key actions
            switch (key)
            {
                case Key.Space:
                case Key.Enter:
                    _navigationCore.SelectCurrentElement();
                    break;
            }
        }
        
        private IEnumerator ProcessAnnouncementQueue()
        {
            _isAnnouncing = true;
            
            while (_announcementQueue.Count > 0)
            {
                var message = _announcementQueue.Dequeue();
                
                if (_enableVoiceAnnouncements && _audioSource != null)
                {
                    // This would use a text-to-speech system
                    // For now, just log the announcement
                    ChimeraLogger.Log($"[Accessibility] Announcing: {message}");
                }
                
                yield return new WaitForSeconds(1f); // Pause between announcements
            }
            
            _isAnnouncing = false;
        }
        
        private IEnumerator AccessibilityUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                
                // Periodic accessibility updates
                if (_enableScreenReader)
                {
                    // Could perform periodic screen reader updates here
                }
            }
        }
        
        // Event handlers
        private void OnElementFocused(VisualElement element)
        {
            if (_enableScreenReader)
            {
                DescribeElement(element);
            }
        }
        
        private void OnElementSelected(VisualElement element)
        {
            if (_enableVoiceAnnouncements)
            {
                AnnounceToUser($"Activated {GetElementText(element)}");
            }
        }
        
        private void OnDestroy()
        {
            // Restore time scale
            if (_isSlowMotionActive)
            {
                Time.timeScale = _originalTimeScale;
            }
            
            // Cleanup event subscriptions
            if (_navigationCore != null)
            {
                _navigationCore.OnElementFocused -= OnElementFocused;
                _navigationCore.OnElementSelected -= OnElementSelected;
            }
            
            ChimeraLogger.Log("[InputAccessibilitySupport] Accessibility support cleanup complete");
        }
    }
    
    // Supporting classes
    [System.Serializable]
    public class AccessibilityInfo
    {
        public string Description;
        public string Role;
        public string Instructions;
        public bool IsInteractable;
    }
    
    public enum NavigationDirection
    {
        Up, Down, Left, Right
    }
#endif
}