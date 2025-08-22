using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Helper class for applying shared menu styles to UI elements.
    /// Bridges the gap between UXML/USS styling and code-driven layout.
    /// Provides consistent styling methods that complement the SharedMenuStyles.uss file.
    /// </summary>
    public static class UIMenuStyleHelper
    {
        // Base style class names
        public const string MenuContainerClass = "menu-container";
        public const string TabButtonClass = "tab-button";
        public const string ActionButtonClass = "action-button";
        public const string MenuSectionClass = "menu-section";
        public const string ButtonGroupClass = "button-group";
        
        // Mode-specific modifiers
        public const string ConstructionModeClass = "menu-container--construction";
        public const string CultivationModeClass = "menu-container--cultivation";
        public const string GeneticsModeClass = "menu-container--genetics";
        public const string OverviewModeClass = "menu-container--overview";
        
        // Button type modifiers
        public const string PrimaryButtonClass = "action-button--primary";
        public const string SecondaryButtonClass = "action-button--secondary";
        public const string WarningButtonClass = "action-button--warning";
        public const string DangerButtonClass = "action-button--danger";
        public const string DisabledButtonClass = "action-button--disabled";
        
        // Tab state modifiers
        public const string ActiveTabClass = "tab-button--active";
        
        // Animation classes
        public const string FadeInClass = "fade-in";
        public const string FadeInActiveClass = "fade-in--active";
        public const string SlideInRightClass = "slide-in-right";
        public const string SlideInRightActiveClass = "slide-in-right--active";
        
        /// <summary>
        /// Apply menu container styling based on gameplay mode
        /// </summary>
        public static void ApplyMenuContainerStyle(VisualElement element, ProjectChimera.Data.Events.GameplayMode mode)
        {
            if (element == null) return;
            
            // Clear existing mode classes
            element.RemoveFromClassList(ConstructionModeClass);
            element.RemoveFromClassList(CultivationModeClass);
            element.RemoveFromClassList(GeneticsModeClass);
            element.RemoveFromClassList(OverviewModeClass);
            
            // Add base class
            element.AddToClassList(MenuContainerClass);
            
            // Add mode-specific class
            string modeClass = mode switch
            {
                ProjectChimera.Data.Events.GameplayMode.Construction => ConstructionModeClass,
                ProjectChimera.Data.Events.GameplayMode.Cultivation => CultivationModeClass,
                ProjectChimera.Data.Events.GameplayMode.Genetics => GeneticsModeClass,
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(modeClass))
            {
                element.AddToClassList(modeClass);
            }
        }
        
        /// <summary>
        /// Apply tab button styling with optional active state
        /// </summary>
        public static void ApplyTabButtonStyle(Button button, bool isActive = false, ProjectChimera.Data.Events.GameplayMode mode = ProjectChimera.Data.Events.GameplayMode.Cultivation)
        {
            if (button == null) return;
            
            // Add base class
            button.AddToClassList(TabButtonClass);
            
            // Handle active state
            if (isActive)
            {
                button.AddToClassList(ActiveTabClass);
            }
            else
            {
                button.RemoveFromClassList(ActiveTabClass);
            }
            
            // Add mode-specific tab styling
            string modeTabClass = mode switch
            {
                ProjectChimera.Data.Events.GameplayMode.Construction => "tab-button--construction",
                ProjectChimera.Data.Events.GameplayMode.Cultivation => "tab-button--cultivation",
                ProjectChimera.Data.Events.GameplayMode.Genetics => "tab-button--genetics",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(modeTabClass))
            {
                button.AddToClassList(modeTabClass);
            }
        }
        
        /// <summary>
        /// Apply action button styling based on button type
        /// </summary>
        public static void ApplyActionButtonStyle(Button button, UIButtonType buttonType = UIButtonType.Default, bool isEnabled = true)
        {
            if (button == null) return;
            
            // Clear existing type classes
            button.RemoveFromClassList(PrimaryButtonClass);
            button.RemoveFromClassList(SecondaryButtonClass);
            button.RemoveFromClassList(WarningButtonClass);
            button.RemoveFromClassList(DangerButtonClass);
            button.RemoveFromClassList(DisabledButtonClass);
            
            // Add base class
            button.AddToClassList(ActionButtonClass);
            
            // Handle disabled state
            if (!isEnabled)
            {
                button.AddToClassList(DisabledButtonClass);
                button.SetEnabled(false);
                return;
            }
            
            button.SetEnabled(true);
            
            // Add type-specific class
            string typeClass = buttonType switch
            {
                UIButtonType.Primary => PrimaryButtonClass,
                UIButtonType.Secondary => SecondaryButtonClass,
                UIButtonType.Warning => WarningButtonClass,
                UIButtonType.Danger => DangerButtonClass,
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(typeClass))
            {
                button.AddToClassList(typeClass);
            }
        }
        
        /// <summary>
        /// Apply menu section styling with mode-specific theming
        /// </summary>
        public static void ApplyMenuSectionStyle(VisualElement element, ProjectChimera.Data.Events.GameplayMode mode = ProjectChimera.Data.Events.GameplayMode.Cultivation)
        {
            if (element == null) return;
            
            // Add base class
            element.AddToClassList(MenuSectionClass);
            
            // Add mode-specific section styling
            string modeSectionClass = mode switch
            {
                ProjectChimera.Data.Events.GameplayMode.Construction => "menu-section--construction",
                ProjectChimera.Data.Events.GameplayMode.Cultivation => "menu-section--cultivation",
                ProjectChimera.Data.Events.GameplayMode.Genetics => "menu-section--genetics",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(modeSectionClass))
            {
                element.AddToClassList(modeSectionClass);
            }
        }
        
        /// <summary>
        /// Apply button group styling for organizing multiple buttons
        /// </summary>
        public static void ApplyButtonGroupStyle(VisualElement element, UIButtonGroupLayout layout = UIButtonGroupLayout.Horizontal)
        {
            if (element == null) return;
            
            // Add base class
            element.AddToClassList(ButtonGroupClass);
            
            // Add layout-specific class
            string layoutClass = layout switch
            {
                UIButtonGroupLayout.Vertical => "button-group--vertical",
                UIButtonGroupLayout.Grid => "button-group--grid",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(layoutClass))
            {
                element.AddToClassList(layoutClass);
            }
        }
        
        /// <summary>
        /// Apply fade-in animation to element
        /// </summary>
        public static void ApplyFadeInAnimation(VisualElement element, bool immediate = false)
        {
            if (element == null) return;
            
            element.AddToClassList(FadeInClass);
            
            if (immediate)
            {
                element.AddToClassList(FadeInActiveClass);
            }
            else
            {
                // Trigger animation after a frame
                element.schedule.Execute(() =>
                {
                    element.AddToClassList(FadeInActiveClass);
                }).ExecuteLater(50);
            }
        }
        
        /// <summary>
        /// Apply slide-in-right animation to element
        /// </summary>
        public static void ApplySlideInAnimation(VisualElement element, bool immediate = false)
        {
            if (element == null) return;
            
            element.AddToClassList(SlideInRightClass);
            
            if (immediate)
            {
                element.AddToClassList(SlideInRightActiveClass);
            }
            else
            {
                // Trigger animation after a frame
                element.schedule.Execute(() =>
                {
                    element.AddToClassList(SlideInRightActiveClass);
                }).ExecuteLater(50);
            }
        }
        
        /// <summary>
        /// Remove all animation classes from element
        /// </summary>
        public static void ClearAnimations(VisualElement element)
        {
            if (element == null) return;
            
            element.RemoveFromClassList(FadeInClass);
            element.RemoveFromClassList(FadeInActiveClass);
            element.RemoveFromClassList(SlideInRightClass);
            element.RemoveFromClassList(SlideInRightActiveClass);
        }
        
        /// <summary>
        /// Create a styled menu divider
        /// </summary>
        public static VisualElement CreateMenuDivider()
        {
            var divider = new VisualElement();
            divider.name = "menu-divider";
            divider.AddToClassList("menu-divider");
            return divider;
        }
        
        /// <summary>
        /// Create a styled menu spacer
        /// </summary>
        public static VisualElement CreateMenuSpacer()
        {
            var spacer = new VisualElement();
            spacer.name = "menu-spacer";
            spacer.AddToClassList("menu-spacer");
            return spacer;
        }
        
        /// <summary>
        /// Apply compact menu styling for smaller screen spaces
        /// </summary>
        public static void ApplyCompactMenuStyle(VisualElement element)
        {
            if (element == null) return;
            
            element.AddToClassList("menu-container--compact");
        }
        
        /// <summary>
        /// Apply context-aware styling based on current selection/mode
        /// </summary>
        public static void ApplyContextualStyling(VisualElement element, string context)
        {
            if (element == null || string.IsNullOrEmpty(context)) return;
            
            // Clear existing context classes
            element.RemoveFromClassList("context-selected");
            element.RemoveFromClassList("context-building");
            element.RemoveFromClassList("context-planting");
            element.RemoveFromClassList("context-breeding");
            
            // Apply new context class
            element.AddToClassList($"context-{context}");
        }
    }
    
    /// <summary>
    /// UI button type enumeration for styling
    /// </summary>
    public enum UIButtonType
    {
        Default,
        Primary,
        Secondary,
        Warning,
        Danger
    }
    
    /// <summary>
    /// UI button group layout options
    /// </summary>
    public enum UIButtonGroupLayout
    {
        Horizontal,
        Vertical,
        Grid
    }
}