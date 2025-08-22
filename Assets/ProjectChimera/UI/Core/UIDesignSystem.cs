using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Shared;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// UI Design System for Project Chimera.
    /// Defines consistent visual language, colors, typography, and design tokens.
    /// </summary>
    [CreateAssetMenu(fileName = "UIDesignSystemSO", menuName = "Project Chimera/UI/Design System")]
    public class UIDesignSystemSO : ChimeraConfigSO
    {
        [Header("Color Palette")]
        [SerializeField] private UIColorPalette _colorPalette;
        
        [Header("Typography")]
        [SerializeField] private UITypography _typography;
        
        [Header("Spacing & Layout")]
        [SerializeField] private UISpacing _spacing;
        
        [Header("Component Styles")]
        [SerializeField] private UIComponentStyles _componentStyles;
        
        [Header("Animation Settings")]
        [SerializeField] private UIAnimationSettings _animationSettings;
        
        [Header("Responsive Breakpoints")]
        [SerializeField] private UIResponsiveSettings _responsiveSettings;
        
        // Properties
        public UIColorPalette ColorPalette => _colorPalette;
        public UITypography Typography => _typography;
        public UISpacing Spacing => _spacing;
        public UIComponentStyles ComponentStyles => _componentStyles;
        public UIAnimationSettings AnimationSettings => _animationSettings;
        public UIResponsiveSettings ResponsiveSettings => _responsiveSettings;
        
        protected void OnEnable()
        {
            InitializeDefaults();
        }
        
        /// <summary>
        /// Initialize default design system values
        /// </summary>
        private void InitializeDefaults()
        {
            if (_colorPalette == null)
            {
                _colorPalette = new UIColorPalette();
                _colorPalette.InitializeDefaults();
            }
            
            if (_typography == null)
            {
                _typography = new UITypography();
                _typography.InitializeDefaults();
            }
            
            if (_spacing == null)
            {
                _spacing = new UISpacing();
                _spacing.InitializeDefaults();
            }
            
            if (_componentStyles == null)
            {
                _componentStyles = new UIComponentStyles();
                _componentStyles.InitializeDefaults();
            }
            
            if (_animationSettings == null)
            {
                _animationSettings = new UIAnimationSettings();
                _animationSettings.InitializeDefaults();
            }
            
            if (_responsiveSettings == null)
            {
                _responsiveSettings = new UIResponsiveSettings();
                _responsiveSettings.InitializeDefaults();
            }
        }
        
        /// <summary>
        /// Get color by semantic name
        /// </summary>
        public Color GetColor(UIColorToken colorToken)
        {
            return _colorPalette.GetColor(colorToken);
        }
        
        /// <summary>
        /// Get font size by scale
        /// </summary>
        public float GetFontSize(UIFontScale scale)
        {
            return _typography.GetFontSize(scale);
        }
        
        /// <summary>
        /// Get spacing value
        /// </summary>
        public float GetSpacing(UISpacingScale scale)
        {
            return _spacing.GetSpacing(scale);
        }
        
        /// <summary>
        /// Validate design system configuration
        /// </summary>
        /*
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (_colorPalette == null)
            {
                LogError("Color palette is not configured");
                isValid = false;
            }
            // else if (!_colorPalette.ValidateColors())
            // {
                LogError("Color palette validation failed");
                isValid = false;
            // }
            
            if (_typography == null)
            {
                LogError("Typography is not configured");
                isValid = false;
            }
            // else if (!_typography.ValidateTypography())
            // {
                LogError("Typography validation failed");
                isValid = false;
            // }
            
            if (_spacing == null)
            {
                LogError("Spacing system is not configured");
                isValid = false;
            }
            
            if (_componentStyles == null)
            {
                LogWarning("Component styles are not configured");
            }
            
            if (_animationSettings == null)
            {
                LogWarning("Animation settings are not configured");
            }
            
            if (_responsiveSettings == null)
            {
                LogWarning("Responsive settings are not configured");
            }
            
            return isValid;
        }
        */
    }
    
    /// <summary>
    /// UI Color Palette with semantic color tokens
    /// </summary>
    [System.Serializable]
    public class UIColorPalette
    {
        [Header("Brand Colors")]
        public Color PrimaryGreen = new Color(0.2f, 0.7f, 0.3f, 1f);        // Cannabis green
        public Color SecondaryGreen = new Color(0.1f, 0.5f, 0.2f, 1f);      // Darker green
        public Color AccentGold = new Color(0.9f, 0.8f, 0.3f, 1f);          // Premium gold
        
        [Header("Neutral Colors")]
        public Color BackgroundDark = new Color(0.1f, 0.1f, 0.1f, 1f);      // Dark background
        public Color BackgroundMedium = new Color(0.15f, 0.15f, 0.15f, 1f); // Medium background
        public Color BackgroundLight = new Color(0.9f, 0.9f, 0.9f, 1f);     // Light background
        public Color SurfaceDark = new Color(0.2f, 0.2f, 0.2f, 1f);         // Panel background
        public Color SurfaceLight = new Color(0.95f, 0.95f, 0.95f, 1f);     // Light panels
        
        [Header("Text Colors")]
        public Color TextPrimary = new Color(0.95f, 0.95f, 0.95f, 1f);      // Main text
        public Color TextSecondary = new Color(0.7f, 0.7f, 0.7f, 1f);       // Secondary text
        public Color TextDisabled = new Color(0.5f, 0.5f, 0.5f, 1f);        // Disabled text
        public Color TextOnPrimary = new Color(1f, 1f, 1f, 1f);             // Text on primary
        
        [Header("State Colors")]
        public Color Success = new Color(0.2f, 0.8f, 0.3f, 1f);             // Success green
        public Color Warning = new Color(0.9f, 0.7f, 0.2f, 1f);             // Warning yellow
        public Color Error = new Color(0.8f, 0.2f, 0.2f, 1f);               // Error red
        public Color Info = new Color(0.3f, 0.6f, 0.9f, 1f);                // Info blue
        
        [Header("Interactive Colors")]
        public Color Interactive = new Color(0.4f, 0.7f, 0.9f, 1f);         // Interactive blue
        public Color InteractiveHover = new Color(0.5f, 0.8f, 1f, 1f);      // Hover state
        public Color InteractivePressed = new Color(0.3f, 0.6f, 0.8f, 1f);  // Pressed state
        public Color InteractiveDisabled = new Color(0.3f, 0.3f, 0.3f, 1f); // Disabled state
        
        public void InitializeDefaults()
        {
            // Colors are already initialized with default values above
        }
        
        public Color GetColor(UIColorToken token)
        {
            return token switch
            {
                UIColorToken.PrimaryGreen => PrimaryGreen,
                UIColorToken.SecondaryGreen => SecondaryGreen,
                UIColorToken.AccentGold => AccentGold,
                UIColorToken.BackgroundDark => BackgroundDark,
                UIColorToken.BackgroundMedium => BackgroundMedium,
                UIColorToken.BackgroundLight => BackgroundLight,
                UIColorToken.SurfaceDark => SurfaceDark,
                UIColorToken.SurfaceLight => SurfaceLight,
                UIColorToken.TextPrimary => TextPrimary,
                UIColorToken.TextSecondary => TextSecondary,
                UIColorToken.TextDisabled => TextDisabled,
                UIColorToken.TextOnPrimary => TextOnPrimary,
                UIColorToken.Success => Success,
                UIColorToken.Warning => Warning,
                UIColorToken.Error => Error,
                UIColorToken.Info => Info,
                UIColorToken.Interactive => Interactive,
                UIColorToken.InteractiveHover => InteractiveHover,
                UIColorToken.InteractivePressed => InteractivePressed,
                UIColorToken.InteractiveDisabled => InteractiveDisabled,
                _ => Color.magenta // Debug color for missing tokens
            };
        }
        
        public bool ValidateColors()
        {
            // Validate that colors have appropriate alpha values
            var colors = new[] { PrimaryGreen, SecondaryGreen, AccentGold, BackgroundDark, 
                               BackgroundMedium, TextPrimary, Success, Warning, Error };
            
            foreach (var color in colors)
            {
                if (color.a <= 0)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Typography system with font scales and styles
    /// </summary>
    [System.Serializable]
    public class UITypography
    {
        [Header("Font Assets")]
        public Font PrimaryFont;
        public Font SecondaryFont;
        public Font MonospaceFont;
        
        [Header("Font Sizes")]
        public float DisplayLarge = 32f;     // Large headings
        public float DisplayMedium = 28f;    // Medium headings
        public float DisplaySmall = 24f;     // Small headings
        public float HeadlineLarge = 20f;    // Section headers
        public float HeadlineMedium = 18f;   // Subsection headers
        public float HeadlineSmall = 16f;    // Minor headers
        public float BodyLarge = 14f;        // Large body text
        public float BodyMedium = 12f;       // Standard body text
        public float BodySmall = 10f;        // Small body text
        public float LabelLarge = 12f;       // Large labels
        public float LabelMedium = 10f;      // Standard labels
        public float LabelSmall = 8f;        // Small labels
        
        [Header("Line Heights")]
        public float DisplayLineHeight = 1.2f;
        public float HeadlineLineHeight = 1.3f;
        public float BodyLineHeight = 1.4f;
        public float LabelLineHeight = 1.2f;
        
        public void InitializeDefaults()
        {
            // Font sizes are already initialized with default values above
        }
        
        public float GetFontSize(UIFontScale scale)
        {
            return scale switch
            {
                UIFontScale.DisplayLarge => DisplayLarge,
                UIFontScale.DisplayMedium => DisplayMedium,
                UIFontScale.DisplaySmall => DisplaySmall,
                UIFontScale.HeadlineLarge => HeadlineLarge,
                UIFontScale.HeadlineMedium => HeadlineMedium,
                UIFontScale.HeadlineSmall => HeadlineSmall,
                UIFontScale.BodyLarge => BodyLarge,
                UIFontScale.BodyMedium => BodyMedium,
                UIFontScale.BodySmall => BodySmall,
                UIFontScale.LabelLarge => LabelLarge,
                UIFontScale.LabelMedium => LabelMedium,
                UIFontScale.LabelSmall => LabelSmall,
                _ => BodyMedium
            };
        }
        
        public float GetLineHeight(UIFontScale scale)
        {
            return scale switch
            {
                UIFontScale.DisplayLarge or UIFontScale.DisplayMedium or UIFontScale.DisplaySmall => DisplayLineHeight,
                UIFontScale.HeadlineLarge or UIFontScale.HeadlineMedium or UIFontScale.HeadlineSmall => HeadlineLineHeight,
                UIFontScale.BodyLarge or UIFontScale.BodyMedium or UIFontScale.BodySmall => BodyLineHeight,
                UIFontScale.LabelLarge or UIFontScale.LabelMedium or UIFontScale.LabelSmall => LabelLineHeight,
                _ => BodyLineHeight
            };
        }
        
        public bool ValidateTypography()
        {
            // Validate that font sizes are positive and in logical order
            return DisplayLarge > DisplayMedium && DisplayMedium > DisplaySmall &&
                   HeadlineLarge > HeadlineMedium && HeadlineMedium > HeadlineSmall &&
                   BodyLarge > BodyMedium && BodyMedium > BodySmall;
        }
    }
    
    /// <summary>
    /// Spacing system with consistent spacing scales
    /// </summary>
    [System.Serializable]
    public class UISpacing
    {
        [Header("Base Spacing Unit")]
        public float BaseUnit = 8f;
        
        [Header("Spacing Scale")]
        public float ExtraSmall = 4f;   // 0.5x
        public float Small = 8f;        // 1x
        public float Medium = 16f;      // 2x
        public float Large = 24f;       // 3x
        public float ExtraLarge = 32f;  // 4x
        public float Huge = 48f;        // 6x
        public float Massive = 64f;     // 8x
        
        public void InitializeDefaults()
        {
            // Spacing values are already initialized above
        }
        
        public float GetSpacing(UISpacingScale scale)
        {
            return scale switch
            {
                UISpacingScale.ExtraSmall => ExtraSmall,
                UISpacingScale.Small => Small,
                UISpacingScale.Medium => Medium,
                UISpacingScale.Large => Large,
                UISpacingScale.ExtraLarge => ExtraLarge,
                UISpacingScale.Huge => Huge,
                UISpacingScale.Massive => Massive,
                _ => Medium
            };
        }
    }
    
    /// <summary>
    /// Component-specific style configurations
    /// </summary>
    [System.Serializable]
    public class UIComponentStyles
    {
        [Header("Border Radius")]
        public float BorderRadiusSmall = 4f;
        public float BorderRadiusMedium = 8f;
        public float BorderRadiusLarge = 12f;
        
        [Header("Border Width")]
        public float BorderWidthThin = 1f;
        public float BorderWidthMedium = 2f;
        public float BorderWidthThick = 3f;
        
        [Header("Shadow")]
        public float ShadowOffsetX = 0f;
        public float ShadowOffsetY = 2f;
        public float ShadowBlur = 4f;
        public Color ShadowColor = new Color(0f, 0f, 0f, 0.2f);
        
        public void InitializeDefaults()
        {
            // Values are already initialized above
        }
    }
    
    /// <summary>
    /// Animation and transition settings
    /// </summary>
    [System.Serializable]
    public class UIAnimationSettings
    {
        [Header("Duration")]
        public float DurationFast = 0.15f;
        public float DurationMedium = 0.25f;
        public float DurationSlow = 0.35f;
        
        [Header("Easing")]
        public AnimationCurve EaseInOut = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve EaseIn = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        public AnimationCurve EaseOut = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        
        public void InitializeDefaults()
        {
            // Values are already initialized above
        }
    }
    
    /// <summary>
    /// Responsive design breakpoints and settings
    /// </summary>
    [System.Serializable]
    public class UIResponsiveSettings
    {
        [Header("Breakpoints")]
        public float MobileBreakpoint = 768f;
        public float TabletBreakpoint = 1024f;
        public float DesktopBreakpoint = 1440f;
        
        [Header("Scale Factors")]
        public float MobileScale = 0.8f;
        public float TabletScale = 0.9f;
        public float DesktopScale = 1f;
        
        public void InitializeDefaults()
        {
            // Values are already initialized above
        }
        
        public UIBreakpoint GetCurrentBreakpoint(float screenWidth)
        {
            if (screenWidth < MobileBreakpoint)
                return UIBreakpoint.Mobile;
            else if (screenWidth < TabletBreakpoint)
                return UIBreakpoint.Tablet;
            else
                return UIBreakpoint.Desktop;
        }
        
        public float GetScaleForBreakpoint(UIBreakpoint breakpoint)
        {
            return breakpoint switch
            {
                UIBreakpoint.Mobile => MobileScale,
                UIBreakpoint.Tablet => TabletScale,
                UIBreakpoint.Desktop => DesktopScale,
                _ => DesktopScale
            };
        }
    }
    
    // Enums for design tokens
    public enum UIColorToken
    {
        PrimaryGreen,
        SecondaryGreen,
        AccentGold,
        BackgroundDark,
        BackgroundMedium,
        BackgroundLight,
        SurfaceDark,
        SurfaceLight,
        TextPrimary,
        TextSecondary,
        TextDisabled,
        TextOnPrimary,
        Success,
        Warning,
        Error,
        Info,
        Interactive,
        InteractiveHover,
        InteractivePressed,
        InteractiveDisabled
    }
    
    public enum UIFontScale
    {
        DisplayLarge,
        DisplayMedium,
        DisplaySmall,
        HeadlineLarge,
        HeadlineMedium,
        HeadlineSmall,
        BodyLarge,
        BodyMedium,
        BodySmall,
        LabelLarge,
        LabelMedium,
        LabelSmall
    }
    
    public enum UISpacingScale
    {
        ExtraSmall,
        Small,
        Medium,
        Large,
        ExtraLarge,
        Huge,
        Massive
    }
    
    public enum UIBreakpoint
    {
        Mobile,
        Tablet,
        Desktop
    }
}