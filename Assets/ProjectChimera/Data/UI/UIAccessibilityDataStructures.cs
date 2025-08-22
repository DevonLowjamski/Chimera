using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// UI announcement data for screen reader
    /// </summary>
    public struct UIAnnouncement
    {
        public string Message;
        public UIAnnouncementPriority Priority;
        public float Timestamp;
    }
    
    /// <summary>
    /// Priority levels for screen reader announcements
    /// </summary>
    public enum UIAnnouncementPriority
    {
        Background = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }
    
    /// <summary>
    /// Accessibility information for UI elements
    /// </summary>
    [System.Serializable]
    public class UIAccessibilityInfo
    {
        public UIAriaRole Role = UIAriaRole.Generic;
        public string Label = "";
        public string Description = "";
        public bool IsFocusable = false;
        public int TabIndex = 0;
        public UILiveRegionType LiveRegion = UILiveRegionType.None;
        public KeyCode KeyboardShortcut = KeyCode.None;
    }
    
    /// <summary>
    /// ARIA roles for accessibility
    /// </summary>
    public enum UIAriaRole
    {
        Generic,
        Button,
        Text,
        TextBox,
        CheckBox,
        ComboBox,
        Slider,
        ProgressBar,
        ScrollBar,
        List,
        ListItem,
        Menu,
        MenuItem,
        Dialog,
        Alert,
        Banner,
        Navigation,
        Main,
        Complementary,
        ContentInfo
    }
    
    /// <summary>
    /// Live region types for dynamic content
    /// </summary>
    public enum UILiveRegionType
    {
        None,
        Polite,
        Assertive,
        Off
    }
    
    /// <summary>
    /// Color blindness types for accessibility support
    /// </summary>
    public enum ColorBlindnessType
    {
        None,
        Protanopia,     // Red-blind
        Deuteranopia,   // Green-blind
        Tritanopia,     // Blue-blind
        Monochromacy    // Complete color blindness
    }
    
    /// <summary>
    /// Accessibility event data
    /// </summary>
    [System.Serializable]
    public struct UIAccessibilityEvent
    {
        public string EventType;
        public VisualElement TargetElement;
        public string Message;
        public UIAnnouncementPriority Priority;
        public float Timestamp;
    }
    
    /// <summary>
    /// Text scaling information
    /// </summary>
    [System.Serializable]
    public struct UITextScaling
    {
        public float OriginalSize;
        public float ScaledSize;
        public float ScaleMultiplier;
    }
    
    /// <summary>
    /// Color adjustment information for color blindness support
    /// </summary>
    [System.Serializable]
    public struct UIColorAdjustment
    {
        public Color OriginalTextColor;
        public Color OriginalBackgroundColor;
        public Color AdjustedTextColor;
        public Color AdjustedBackgroundColor;
        public ColorBlindnessType ColorBlindnessType;
    }
    
    /// <summary>
    /// Live region configuration
    /// </summary>
    [System.Serializable]
    public class UILiveRegion
    {
        public string RegionId;
        public UILiveRegionType Type;
        public VisualElement Container;
        public bool IsActive;
        public float LastUpdateTime;
    }
    
    /// <summary>
    /// Accessibility statistics
    /// </summary>
    public struct UIAccessibilityStats
    {
        public int RegisteredElements;
        public int FocusableElements;
        public bool ScreenReaderEnabled;
        public bool KeyboardNavigationEnabled;
        public bool HighContrastEnabled;
        public bool ColorBlindSupportEnabled;
        public float TextScaleMultiplier;
        public int PendingAnnouncements;
    }
    
    /// <summary>
    /// Interface for UI elements that can be updated
    /// </summary>
    public interface IUIUpdatable
    {
        void UpdateElement();
    }
    
    /// <summary>
    /// Update priority for batched UI updates
    /// </summary>
    public enum UIUpdatePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}