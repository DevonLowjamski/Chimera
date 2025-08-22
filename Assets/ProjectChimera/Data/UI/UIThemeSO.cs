using UnityEngine;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// ScriptableObject defining UI theme colors, fonts, and visual styling.
    /// Provides comprehensive theming support for the entire UI system.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Theme", menuName = "Project Chimera/UI/Theme")]
    public class UIThemeSO : ChimeraDataSO
    {
        [Header("Theme Identity")]
        [SerializeField] private string _themeName;
        [SerializeField] private string _description;
        [SerializeField] private bool _isDarkTheme = true;
        [SerializeField] private ThemeCategory _category = ThemeCategory.Gaming;
        
        [Header("Primary Colors")]
        [SerializeField] private Color _primaryColor = new Color(0.6f, 0.4f, 1f, 1f); // Purple
        [SerializeField] private Color _secondaryColor = new Color(0.4f, 0.8f, 1f, 1f); // Blue
        [SerializeField] private Color _accentColor = new Color(1f, 0.8f, 0.4f, 1f); // Orange
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0.4f, 1f); // Yellow
        [SerializeField] private Color _errorColor = new Color(1f, 0.5f, 0.5f, 1f); // Red
        [SerializeField] private Color _successColor = new Color(0.5f, 1f, 0.5f, 1f); // Green
        
        [Header("Background Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0.03f, 0.05f, 0.08f, 1f);
        [SerializeField] private Color _surfaceColor = new Color(0.06f, 0.09f, 0.12f, 1f);
        [SerializeField] private Color _cardColor = new Color(0.09f, 0.14f, 0.18f, 1f);
        [SerializeField] private Color _modalBackgroundColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Color _overlayColor = new Color(0f, 0f, 0f, 0.5f);
        
        [Header("Text Colors")]
        [SerializeField] private Color _textColor = new Color(0.86f, 0.86f, 0.86f, 1f);
        [SerializeField] private Color _textSecondaryColor = new Color(0.67f, 0.78f, 0.89f, 1f);
        [SerializeField] private Color _textMutedColor = new Color(0.47f, 0.55f, 0.64f, 1f);
        [SerializeField] private Color _textDisabledColor = new Color(0.47f, 0.47f, 0.51f, 1f);
        
        [Header("Border Colors")]
        [SerializeField] private Color _borderColor = new Color(0.18f, 0.25f, 0.33f, 1f);
        [SerializeField] private Color _borderActiveColor = new Color(0.6f, 0.4f, 1f, 1f);
        [SerializeField] private Color _borderFocusColor = new Color(0.6f, 0.4f, 1f, 0.6f);
        [SerializeField] private Color _dividerColor = new Color(0.18f, 0.25f, 0.33f, 0.5f);
        
        [Header("Interactive Colors")]
        [SerializeField] private Color _buttonPrimaryColor = new Color(0.6f, 0.4f, 1f, 1f);
        [SerializeField] private Color _buttonSecondaryColor = new Color(0.39f, 0.59f, 0.78f, 1f);
        [SerializeField] private Color _buttonHoverColor = new Color(0.67f, 0.47f, 1f, 1f);
        [SerializeField] private Color _buttonActiveColor = new Color(0.53f, 0.33f, 0.93f, 1f);
        [SerializeField] private Color _buttonDisabledColor = new Color(0.24f, 0.24f, 0.28f, 1f);
        
        [Header("Status Colors")]
        [SerializeField] private Color _healthyColor = new Color(0.47f, 0.8f, 0.47f, 1f);
        [SerializeField] private Color _cautionColor = new Color(1f, 0.78f, 0.39f, 1f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.47f, 0.47f, 1f);
        [SerializeField] private Color _neutralColor = new Color(0.67f, 0.67f, 0.67f, 1f);
        
        [Header("Chart Colors")]
        [SerializeField] private Color[] _chartColors = new Color[]
        {
            new Color(0.6f, 0.4f, 1f, 1f),   // Purple
            new Color(0.4f, 0.8f, 1f, 1f),   // Blue
            new Color(0.47f, 0.8f, 0.47f, 1f), // Green
            new Color(1f, 0.78f, 0.39f, 1f), // Orange
            new Color(1f, 0.47f, 0.47f, 1f), // Red
            new Color(0.8f, 0.47f, 1f, 1f),  // Pink
            new Color(0.47f, 1f, 0.8f, 1f),  // Cyan
            new Color(1f, 0.8f, 0.47f, 1f)   // Yellow
        };
        
        [Header("Typography")]
        [SerializeField] private Font _primaryFont;
        [SerializeField] private Font _secondaryFont;
        [SerializeField] private Font _monospaceFont;
        [SerializeField] private int _baseFontSize = 14;
        [SerializeField] private FontStyle _defaultFontStyle = FontStyle.Normal;
        
        [Header("Spacing and Sizing")]
        [SerializeField] private float _baseSpacing = 8f;
        [SerializeField] private float _borderRadius = 6f;
        [SerializeField] private float _shadowBlur = 4f;
        [SerializeField] private Vector2 _shadowOffset = new Vector2(0f, 2f);
        [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.25f);
        
        [Header("Animation Settings")]
        [SerializeField] private float _transitionDuration = 0.2f;
        [SerializeField] private AnimationCurve _easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool _enableAnimations = true;
        
        // Properties for easy access
        public string ThemeName => _themeName;
        public string Description => _description;
        public bool IsDarkTheme => _isDarkTheme;
        public ThemeCategory Category => _category;
        
        // Color properties
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public Color AccentColor => _accentColor;
        public Color WarningColor => _warningColor;
        public Color ErrorColor => _errorColor;
        public Color SuccessColor => _successColor;
        
        public Color BackgroundColor => _backgroundColor;
        public Color SurfaceColor => _surfaceColor;
        public Color CardColor => _cardColor;
        public Color ModalBackgroundColor => _modalBackgroundColor;
        public Color OverlayColor => _overlayColor;
        
        public Color TextColor => _textColor;
        public Color TextSecondaryColor => _textSecondaryColor;
        public Color TextMutedColor => _textMutedColor;
        public Color TextDisabledColor => _textDisabledColor;
        
        public Color BorderColor => _borderColor;
        public Color BorderActiveColor => _borderActiveColor;
        public Color BorderFocusColor => _borderFocusColor;
        public Color DividerColor => _dividerColor;
        
        public Color ButtonPrimaryColor => _buttonPrimaryColor;
        public Color ButtonSecondaryColor => _buttonSecondaryColor;
        public Color ButtonHoverColor => _buttonHoverColor;
        public Color ButtonActiveColor => _buttonActiveColor;
        public Color ButtonDisabledColor => _buttonDisabledColor;
        
        public Color HealthyColor => _healthyColor;
        public Color CautionColor => _cautionColor;
        public Color CriticalColor => _criticalColor;
        public Color NeutralColor => _neutralColor;
        
        public Color[] ChartColors => _chartColors;
        
        // Typography properties
        public Font PrimaryFont => _primaryFont;
        public Font SecondaryFont => _secondaryFont;
        public Font MonospaceFont => _monospaceFont;
        public int BaseFontSize => _baseFontSize;
        public FontStyle DefaultFontStyle => _defaultFontStyle;
        
        // Layout properties
        public float BaseSpacing => _baseSpacing;
        public float BorderRadius => _borderRadius;
        public float ShadowBlur => _shadowBlur;
        public Vector2 ShadowOffset => _shadowOffset;
        public Color ShadowColor => _shadowColor;
        
        // Animation properties
        public float TransitionDuration => _transitionDuration;
        public AnimationCurve EasingCurve => _easingCurve;
        public bool EnableAnimations => _enableAnimations;
        
        /// <summary>
        /// Get color by context for status indicators
        /// </summary>
        public Color GetStatusColor(StatusType statusType)
        {
            return statusType switch
            {
                StatusType.Healthy => _healthyColor,
                StatusType.Caution => _cautionColor,
                StatusType.Critical => _criticalColor,
                StatusType.Neutral => _neutralColor,
                StatusType.Success => _successColor,
                StatusType.Warning => _warningColor,
                StatusType.Error => _errorColor,
                _ => _neutralColor
            };
        }
        
        /// <summary>
        /// Get chart color by index with cycling
        /// </summary>
        public Color GetChartColor(int index)
        {
            if (_chartColors == null || _chartColors.Length == 0)
                return _primaryColor;
            
            return _chartColors[index % _chartColors.Length];
        }
        
        /// <summary>
        /// Get spacing value multiplied by base spacing
        /// </summary>
        public float GetSpacing(int multiplier = 1)
        {
            return _baseSpacing * multiplier;
        }
        
        /// <summary>
        /// Get font size based on scale
        /// </summary>
        public int GetFontSize(FontSizeScale scale)
        {
            return scale switch
            {
                FontSizeScale.Small => Mathf.RoundToInt(_baseFontSize * 0.85f),
                FontSizeScale.Normal => _baseFontSize,
                FontSizeScale.Large => Mathf.RoundToInt(_baseFontSize * 1.2f),
                FontSizeScale.XLarge => Mathf.RoundToInt(_baseFontSize * 1.5f),
                FontSizeScale.XXLarge => Mathf.RoundToInt(_baseFontSize * 2f),
                _ => _baseFontSize
            };
        }
        
        /// <summary>
        /// Create a variant color with modified alpha
        /// </summary>
        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
        
        /// <summary>
        /// Create a lighter variant of a color
        /// </summary>
        public Color Lighten(Color color, float amount = 0.1f)
        {
            return Color.Lerp(color, Color.white, amount);
        }
        
        /// <summary>
        /// Create a darker variant of a color
        /// </summary>
        public Color Darken(Color color, float amount = 0.1f)
        {
            return Color.Lerp(color, Color.black, amount);
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_themeName))
            {
                _themeName = name;
            }
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_themeName))
            {
                LogError("Theme name cannot be empty");
                isValid = false;
            }
            
            if (_baseFontSize < 8 || _baseFontSize > 32)
            {
                LogError("Base font size should be between 8 and 32");
                isValid = false;
            }
            
            if (_baseSpacing < 0)
            {
                LogError("Base spacing cannot be negative");
                isValid = false;
            }
            
            if (_borderRadius < 0)
            {
                LogError("Border radius cannot be negative");
                isValid = false;
            }
            
            if (_transitionDuration < 0)
            {
                LogError("Transition duration cannot be negative");
                isValid = false;
            }
            
            if (_chartColors == null || _chartColors.Length == 0)
            {
                LogWarning("Chart colors array is empty - using primary color as fallback");
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Theme categories for organization
    /// </summary>
    public enum ThemeCategory
    {
        Gaming,
        Professional,
        Accessibility,
        Custom
    }
    
    /// <summary>
    /// Status types for color coding
    /// </summary>
    public enum StatusType
    {
        Healthy,
        Caution,
        Critical,
        Neutral,
        Success,
        Warning,
        Error
    }
    
    /// <summary>
    /// Font size scales
    /// </summary>
    public enum FontSizeScale
    {
        Small,
        Normal,
        Large,
        XLarge,
        XXLarge
    }
}