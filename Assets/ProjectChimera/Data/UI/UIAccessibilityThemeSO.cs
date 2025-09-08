using UnityEngine;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// Accessibility theme configuration for UI systems in Project Chimera.
    /// Defines accessibility preferences and visual enhancements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Accessibility Theme", menuName = "Project Chimera/UI/Accessibility Theme")]
    public class UIAccessibilityThemeSO : ChimeraDataSO
    {
        [Header("Text Accessibility")]
        [SerializeField] private float _textScaleMultiplier = 1f;
        [SerializeField] private bool _enableHighContrast = false;
        [SerializeField] private bool _enableBoldText = false;
        [SerializeField] private FontWeight _minimumFontWeight = FontWeight.Normal;
        
        [Header("Color Accessibility")]
        [SerializeField] private bool _enableColorBlindSupport = false;
        [SerializeField] private ColorBlindnessType _colorBlindnessType = ColorBlindnessType.None;
        [SerializeField] private Color _highContrastTextColor = Color.white;
        [SerializeField] private Color _highContrastBackgroundColor = Color.black;
        [SerializeField] private Color _focusIndicatorColor = Color.yellow;
        
        [Header("Navigation Accessibility")]
        [SerializeField] private bool _enableKeyboardNavigation = true;
        [SerializeField] private bool _enableTabNavigation = true;
        [SerializeField] private bool _enableArrowKeyNavigation = true;
        [SerializeField] private float _navigationRepeatDelay = 0.5f;
        [SerializeField] private bool _enableFocusIndicators = true;
        [SerializeField] private float _focusIndicatorWidth = 3f;
        
        [Header("Screen Reader")]
        [SerializeField] private bool _enableScreenReader = true;
        [SerializeField] private bool _enableLiveRegions = true;
        [SerializeField] private bool _enableAriaLabels = true;
        [SerializeField] private float _announceDelay = 0.5f;
        [SerializeField] private int _maxAnnouncementLength = 200;
        
        [Header("Audio Accessibility")]
        [SerializeField] private bool _enableAudioCues = true;
        [SerializeField] private float _audioVolume = 0.7f;
        [SerializeField] private AudioClip _focusSound;
        [SerializeField] private AudioClip _selectSound;
        [SerializeField] private AudioClip _errorSound;
        [SerializeField] private AudioClip _successSound;
        
        [Header("Motion Accessibility")]
        [SerializeField] private bool _reduceMotion = false;
        [SerializeField] private bool _disableAnimations = false;
        [SerializeField] private float _animationSpeedMultiplier = 1f;
        [SerializeField] private bool _enableSystemAnimationSettings = true;
        
        // Properties
        public float TextScaleMultiplier => _textScaleMultiplier;
        public bool EnableHighContrast => _enableHighContrast;
        public bool EnableBoldText => _enableBoldText;
        public FontWeight MinimumFontWeight => _minimumFontWeight;
        
        public bool EnableColorBlindSupport => _enableColorBlindSupport;
        public ColorBlindnessType ColorBlindnessType => _colorBlindnessType;
        public Color HighContrastTextColor => _highContrastTextColor;
        public Color HighContrastBackgroundColor => _highContrastBackgroundColor;
        public Color FocusIndicatorColor => _focusIndicatorColor;
        
        public bool EnableKeyboardNavigation => _enableKeyboardNavigation;
        public bool EnableTabNavigation => _enableTabNavigation;
        public bool EnableArrowKeyNavigation => _enableArrowKeyNavigation;
        public float NavigationRepeatDelay => _navigationRepeatDelay;
        public bool EnableFocusIndicators => _enableFocusIndicators;
        public float FocusIndicatorWidth => _focusIndicatorWidth;
        
        public bool EnableScreenReader => _enableScreenReader;
        public bool EnableLiveRegions => _enableLiveRegions;
        public bool EnableAriaLabels => _enableAriaLabels;
        public float AnnounceDelay => _announceDelay;
        public int MaxAnnouncementLength => _maxAnnouncementLength;
        
        public bool EnableAudioCues => _enableAudioCues;
        public float AudioVolume => _audioVolume;
        public AudioClip FocusSound => _focusSound;
        public AudioClip SelectSound => _selectSound;
        public AudioClip ErrorSound => _errorSound;
        public AudioClip SuccessSound => _successSound;
        
        public bool ReduceMotion => _reduceMotion;
        public bool DisableAnimations => _disableAnimations;
        public float AnimationSpeedMultiplier => _animationSpeedMultiplier;
        public bool EnableSystemAnimationSettings => _enableSystemAnimationSettings;
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            _textScaleMultiplier = Mathf.Clamp(_textScaleMultiplier, 0.5f, 3f);
            _navigationRepeatDelay = Mathf.Max(0.1f, _navigationRepeatDelay);
            _announceDelay = Mathf.Max(0.1f, _announceDelay);
            _maxAnnouncementLength = Mathf.Max(50, _maxAnnouncementLength);
            _audioVolume = Mathf.Clamp01(_audioVolume);
            _focusIndicatorWidth = Mathf.Max(1f, _focusIndicatorWidth);
            _animationSpeedMultiplier = Mathf.Clamp(_animationSpeedMultiplier, 0.1f, 5f);
            
            // Add validation checks
            if (_textScaleMultiplier < 0.5f || _textScaleMultiplier > 3f)
            {
                LogError("Text scale multiplier must be between 0.5 and 3.0");
                isValid = false;
            }
            
            if (_navigationRepeatDelay < 0.1f)
            {
                LogError("Navigation repeat delay must be at least 0.1 seconds");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Apply accessibility theme to system
        /// </summary>
        public void ApplyTheme()
        {
            // In a real implementation, this would apply the theme settings
            // to the UI accessibility system
            SharedLogger.Log($"Applied accessibility theme: {name}");
        }
        
        /// <summary>
        /// Check if theme supports specific accessibility feature
        /// </summary>
        public bool SupportsFeature(UIAccessibilityFeature feature)
        {
            return feature switch
            {
                UIAccessibilityFeature.ScreenReader => _enableScreenReader,
                UIAccessibilityFeature.KeyboardNavigation => _enableKeyboardNavigation,
                UIAccessibilityFeature.HighContrast => _enableHighContrast,
                UIAccessibilityFeature.ColorBlindSupport => _enableColorBlindSupport,
                UIAccessibilityFeature.AudioCues => _enableAudioCues,
                UIAccessibilityFeature.ReducedMotion => _reduceMotion,
                UIAccessibilityFeature.TextScaling => _textScaleMultiplier != 1f,
                _ => false
            };
        }
        
        /// <summary>
        /// Get theme configuration summary
        /// </summary>
        public UIAccessibilityThemeInfo GetThemeInfo()
        {
            return new UIAccessibilityThemeInfo
            {
                ThemeName = name,
                TextScaleMultiplier = _textScaleMultiplier,
                EnableHighContrast = _enableHighContrast,
                EnableColorBlindSupport = _enableColorBlindSupport,
                ColorBlindnessType = _colorBlindnessType,
                EnableScreenReader = _enableScreenReader,
                EnableKeyboardNavigation = _enableKeyboardNavigation,
                EnableAudioCues = _enableAudioCues,
                ReduceMotion = _reduceMotion
            };
        }
    }
    
    /// <summary>
    /// Accessibility features enumeration
    /// </summary>
    public enum UIAccessibilityFeature
    {
        ScreenReader,
        KeyboardNavigation,
        HighContrast,
        ColorBlindSupport,
        AudioCues,
        ReducedMotion,
        TextScaling,
        FocusIndicators
    }
    
    /// <summary>
    /// Font weight enumeration for accessibility
    /// </summary>
    public enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900
    }
    
    /// <summary>
    /// Accessibility theme information
    /// </summary>
    public struct UIAccessibilityThemeInfo
    {
        public string ThemeName;
        public float TextScaleMultiplier;
        public bool EnableHighContrast;
        public bool EnableColorBlindSupport;
        public ColorBlindnessType ColorBlindnessType;
        public bool EnableScreenReader;
        public bool EnableKeyboardNavigation;
        public bool EnableAudioCues;
        public bool ReduceMotion;
    }
}