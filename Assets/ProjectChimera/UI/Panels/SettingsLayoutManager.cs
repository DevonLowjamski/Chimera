using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages the layout and visual structure of the Settings panel.
    /// Handles container creation, styling, and layout organization.
    /// </summary>
    public static class SettingsLayoutManager
    {
        /// <summary>
        /// Create the main settings layout structure
        /// </summary>
        public static SettingsLayoutElements CreateMainLayout(VisualElement rootElement)
        {
            rootElement.Clear();
            
            var elements = new SettingsLayoutElements();
            
            // Main container
            var mainContainer = new VisualElement();
            mainContainer.name = "settings-main-container";
            mainContainer.style.flexGrow = 1;
            mainContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            mainContainer.style.flexDirection = FlexDirection.Column;
            
            // Content area
            var contentArea = new VisualElement();
            contentArea.name = "settings-content-area";
            contentArea.style.flexGrow = 1;
            contentArea.style.flexDirection = FlexDirection.Row;
            contentArea.style.maxWidth = 1000;
            contentArea.style.alignSelf = Align.Center;
            contentArea.style.marginTop = 50;
            contentArea.style.marginBottom = 50;
            contentArea.style.marginLeft = 50;
            contentArea.style.marginRight = 50;
            
            // Create main layout sections
            elements.headerContainer = CreateHeaderContainer();
            elements.contentContainer = CreateContentContainer();
            elements.footerContainer = CreateFooterContainer();
            
            // Assemble layout
            contentArea.Add(elements.headerContainer);
            contentArea.Add(elements.contentContainer);
            contentArea.Add(elements.footerContainer);
            
            mainContainer.Add(contentArea);
            rootElement.Add(mainContainer);
            
            return elements;
        }
        
        /// <summary>
        /// Create the header container
        /// </summary>
        private static VisualElement CreateHeaderContainer()
        {
            var headerContainer = new VisualElement();
            headerContainer.name = "settings-header";
            headerContainer.style.height = 60;
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.paddingLeft = 24;
            headerContainer.style.paddingRight = 24;
            headerContainer.style.borderTopLeftRadius = 12;
            headerContainer.style.borderTopRightRadius = 12;
            
            return headerContainer;
        }
        
        /// <summary>
        /// Create the content container
        /// </summary>
        private static VisualElement CreateContentContainer()
        {
            var contentContainer = new VisualElement();
            contentContainer.name = "settings-content";
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexDirection = FlexDirection.Row;
            
            return contentContainer;
        }
        
        /// <summary>
        /// Create the footer container
        /// </summary>
        private static VisualElement CreateFooterContainer()
        {
            var footerContainer = new VisualElement();
            footerContainer.name = "settings-footer";
            footerContainer.style.height = 60;
            footerContainer.style.flexDirection = FlexDirection.Row;
            footerContainer.style.alignItems = Align.Center;
            footerContainer.style.justifyContent = Justify.SpaceBetween;
            footerContainer.style.paddingLeft = 24;
            footerContainer.style.paddingRight = 24;
            footerContainer.style.borderBottomLeftRadius = 12;
            footerContainer.style.borderBottomRightRadius = 12;
            
            return footerContainer;
        }
        
        /// <summary>
        /// Create header elements (title and close button)
        /// </summary>
        public static SettingsHeaderElements CreateHeaderElements()
        {
            var elements = new SettingsHeaderElements();
            
            // Title
            elements.titleLabel = new Label("Settings");
            elements.titleLabel.name = "settings-title";
            elements.titleLabel.style.fontSize = 24;
            elements.titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Close button
            elements.closeButton = new Button();
            elements.closeButton.name = "settings-close-button";
            elements.closeButton.text = "✕";
            elements.closeButton.style.width = 32;
            elements.closeButton.style.height = 32;
            elements.closeButton.style.fontSize = 18;
            elements.closeButton.style.backgroundColor = Color.clear;
            elements.closeButton.style.borderTopWidth = 0;
            elements.closeButton.style.borderRightWidth = 0;
            elements.closeButton.style.borderBottomWidth = 0;
            elements.closeButton.style.borderLeftWidth = 0;
            
            return elements;
        }
        
        /// <summary>
        /// Create footer elements (buttons)
        /// </summary>
        public static SettingsFooterElements CreateFooterElements()
        {
            var elements = new SettingsFooterElements();
            
            // Reset button (left side)
            elements.resetButton = new Button();
            elements.resetButton.name = "settings-reset-button";
            elements.resetButton.text = "Reset to Defaults";
            
            // Action buttons container (right side)
            elements.actionContainer = new VisualElement();
            elements.actionContainer.style.flexDirection = FlexDirection.Row;
            
            // Cancel button
            elements.cancelButton = new Button();
            elements.cancelButton.name = "settings-cancel-button";
            elements.cancelButton.text = "Cancel";
            elements.cancelButton.style.marginRight = 8;
            
            // Apply button
            elements.applyButton = new Button();
            elements.applyButton.name = "settings-apply-button";
            elements.applyButton.text = "Apply";
            elements.applyButton.style.marginRight = 8;
            
            // Save button
            elements.saveButton = new Button();
            elements.saveButton.name = "settings-save-button";
            elements.saveButton.text = "Save";
            
            // Add buttons to action container
            elements.actionContainer.Add(elements.cancelButton);
            elements.actionContainer.Add(elements.applyButton);
            elements.actionContainer.Add(elements.saveButton);
            
            return elements;
        }
        
        /// <summary>
        /// Create category navigation container
        /// </summary>
        public static VisualElement CreateCategoryContainer()
        {
            var categoryContainer = new VisualElement();
            categoryContainer.name = "category-container";
            categoryContainer.style.width = 200;
            categoryContainer.style.paddingTop = 24;
            categoryContainer.style.paddingBottom = 24;
            categoryContainer.style.paddingLeft = 16;
            categoryContainer.style.paddingRight = 16;
            
            return categoryContainer;
        }
        
        /// <summary>
        /// Create settings content container
        /// </summary>
        public static VisualElement CreateSettingsContainer()
        {
            var settingsContainer = new VisualElement();
            settingsContainer.name = "settings-container";
            settingsContainer.style.flexGrow = 1;
            settingsContainer.style.paddingTop = 24;
            settingsContainer.style.paddingBottom = 24;
            settingsContainer.style.paddingLeft = 24;
            settingsContainer.style.paddingRight = 24;
            
            return settingsContainer;
        }
        
        /// <summary>
        /// Create a settings section container
        /// </summary>
        public static VisualElement CreateSettingsSection(string title)
        {
            var section = new VisualElement();
            section.name = title.ToLower().Replace(" ", "-") + "-section";
            section.style.display = DisplayStyle.None;
            
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            
            section.Add(titleLabel);
            return section;
        }
        
        /// <summary>
        /// Create category indicator
        /// </summary>
        public static VisualElement CreateCategoryIndicator()
        {
            var indicator = new VisualElement();
            indicator.name = "category-indicator";
            indicator.style.position = Position.Absolute;
            indicator.style.width = 4;
            indicator.style.height = 40;
            indicator.style.left = 0;
            indicator.style.borderTopRightRadius = 2;
            indicator.style.borderBottomRightRadius = 2;
            
            return indicator;
        }
        
        /// <summary>
        /// Apply button styling for different states
        /// </summary>
        public static void ApplyButtonStyling(Button button, bool isEnabled, bool isPrimary = false)
        {
            button.SetEnabled(isEnabled);
            
            if (isEnabled)
            {
                button.style.opacity = 1f;
            }
            else
            {
                button.style.opacity = 0.5f;
            }
            
            // Additional styling could be applied based on design system
            if (isPrimary && isEnabled)
            {
                // Primary button styling
            }
            else
            {
                // Secondary button styling
            }
        }
        
        /// <summary>
        /// Update category indicator position
        /// </summary>
        public static void UpdateCategoryIndicatorPosition(VisualElement indicator, SettingsCategory category)
        {
            var buttonIndex = (int)category;
            var topOffset = 24 + (buttonIndex * 48); // Account for padding and button spacing
            indicator.style.top = topOffset;
        }
    }
    
    /// <summary>
    /// Container for main layout elements
    /// </summary>
    public class SettingsLayoutElements
    {
        public VisualElement headerContainer;
        public VisualElement contentContainer;
        public VisualElement footerContainer;
    }
    
    /// <summary>
    /// Container for header elements
    /// </summary>
    public class SettingsHeaderElements
    {
        public Label titleLabel;
        public Button closeButton;
    }
    
    /// <summary>
    /// Container for footer elements
    /// </summary>
    public class SettingsFooterElements
    {
        public Button resetButton;
        public Button cancelButton;
        public Button applyButton;
        public Button saveButton;
        public VisualElement actionContainer;
    }
}