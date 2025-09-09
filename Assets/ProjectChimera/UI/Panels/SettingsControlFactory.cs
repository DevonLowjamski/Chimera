using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Factory class for creating settings UI controls.
    /// Handles creation of toggles, sliders, dropdowns, and other setting controls.
    /// </summary>
    public static class SettingsControlFactory
    {
        /// <summary>
        /// Create a toggle setting control
        /// </summary>
        public static VisualElement CreateToggleSetting(string label, string description, string key, bool defaultValue, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key);
            
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1;
            
            var labelElement = CreateSettingLabel(label);
            var descElement = CreateSettingDescription(description);
            
            labelContainer.Add(labelElement);
            labelContainer.Add(descElement);
            
            var toggle = new Toggle();
            toggle.name = key + "-toggle";
            toggle.value = defaultValue;
            
            if (onValueChanged != null)
            {
                toggle.RegisterCallback<ChangeEvent<bool>>(evt => onValueChanged(key, evt.newValue));
            }
            
            container.Add(labelContainer);
            container.Add(toggle);
            
            return container;
        }
        
        /// <summary>
        /// Create a slider setting control
        /// </summary>
        public static VisualElement CreateSliderSetting(string label, string description, string key, float defaultValue, float min, float max, string unit, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key, false); // Vertical layout for sliders
            
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.marginBottom = 8;
            
            var labelElement = CreateSettingLabel(label);
            var valueLabel = new Label($"{defaultValue:F0} {unit}");
            valueLabel.name = key + "-value-label";
            valueLabel.style.fontSize = 14;
            
            headerContainer.Add(labelElement);
            headerContainer.Add(valueLabel);
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            var slider = new Slider(min, max);
            slider.name = key + "-slider";
            slider.value = defaultValue;
            
            if (onValueChanged != null)
            {
                slider.RegisterCallback<ChangeEvent<float>>(evt => {
                    onValueChanged(key, evt.newValue);
                    valueLabel.text = $"{evt.newValue:F0} {unit}";
                });
            }
            
            container.Add(headerContainer);
            container.Add(descElement);
            container.Add(slider);
            
            return container;
        }
        
        /// <summary>
        /// Create a dropdown setting control
        /// </summary>
        public static VisualElement CreateDropdownSetting(string label, string description, string key, string[] options, int defaultIndex, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key, false); // Vertical layout for dropdowns
            
            var labelElement = CreateSettingLabel(label);
            labelElement.style.marginBottom = 4;
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            var dropdown = new DropdownField(options.ToList(), defaultIndex);
            dropdown.name = key + "-dropdown";
            
            if (onValueChanged != null)
            {
                dropdown.RegisterCallback<ChangeEvent<string>>(evt => {
                    var index = System.Array.IndexOf(options, evt.newValue);
                    onValueChanged(key, index);
                });
            }
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(dropdown);
            
            return container;
        }
        
        /// <summary>
        /// Create a text field setting control
        /// </summary>
        public static VisualElement CreateTextFieldSetting(string label, string description, string key, string defaultValue, Action<string, object> onValueChanged = null, bool isPassword = false)
        {
            var container = CreateSettingContainer(key, false);
            
            var labelElement = CreateSettingLabel(label);
            labelElement.style.marginBottom = 4;
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            var textField = new TextField();
            textField.name = key + "-textfield";
            textField.value = defaultValue;
            textField.isPasswordField = isPassword;
            
            if (onValueChanged != null)
            {
                textField.RegisterCallback<ChangeEvent<string>>(evt => onValueChanged(key, evt.newValue));
            }
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(textField);
            
            return container;
        }
        
        /// <summary>
        /// Create a integer field setting control
        /// </summary>
        public static VisualElement CreateIntegerFieldSetting(string label, string description, string key, int defaultValue, int min = int.MinValue, int max = int.MaxValue, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key, false);
            
            var labelElement = CreateSettingLabel(label);
            labelElement.style.marginBottom = 4;
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            var intField = new IntegerField();
            intField.name = key + "-integerfield";
            intField.value = defaultValue;
            
            if (onValueChanged != null)
            {
                intField.RegisterCallback<ChangeEvent<int>>(evt => {
                    var clampedValue = Mathf.Clamp(evt.newValue, min, max);
                    if (clampedValue != evt.newValue)
                    {
                        intField.value = clampedValue;
                    }
                    onValueChanged(key, clampedValue);
                });
            }
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(intField);
            
            return container;
        }
        
        /// <summary>
        /// Create a button setting control
        /// </summary>
        public static VisualElement CreateButtonSetting(string label, string description, string buttonText, Action onClicked)
        {
            var container = CreateSettingContainer("button-setting", false);
            
            var labelElement = CreateSettingLabel(label);
            labelElement.style.marginBottom = 4;
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            var button = new Button(onClicked);
            button.text = buttonText;
            button.style.alignSelf = Align.FlexStart;
            button.style.minWidth = 120;
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(button);
            
            return container;
        }
        
        /// <summary>
        /// Create a color field setting control
        /// </summary>
        public static VisualElement CreateColorFieldSetting(string label, string description, string key, Color defaultValue, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key, false);
            
            var labelElement = CreateSettingLabel(label);
            labelElement.style.marginBottom = 4;
            
            var descElement = CreateSettingDescription(description);
            descElement.style.marginBottom = 8;
            
            // Note: ColorField is not available in UI Toolkit by default
            // This would need to be implemented as a custom control or using a different approach
            var colorContainer = new VisualElement();
            colorContainer.style.height = 30;
            colorContainer.style.backgroundColor = defaultValue;
            colorContainer.style.borderTopWidth = 1;
            colorContainer.style.borderRightWidth = 1;
            colorContainer.style.borderBottomWidth = 1;
            colorContainer.style.borderLeftWidth = 1;
            colorContainer.style.borderTopColor = Color.gray;
            colorContainer.style.borderRightColor = Color.gray;
            colorContainer.style.borderBottomColor = Color.gray;
            colorContainer.style.borderLeftColor = Color.gray;
            colorContainer.style.borderTopLeftRadius = 4;
            colorContainer.style.borderTopRightRadius = 4;
            colorContainer.style.borderBottomLeftRadius = 4;
            colorContainer.style.borderBottomRightRadius = 4;
            
            // Add click handler for color picker
            colorContainer.RegisterCallback<ClickEvent>(evt => {
                // This would open a color picker dialog
                // Implementation depends on available color picker solution
            });
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(colorContainer);
            
            return container;
        }
        
        /// <summary>
        /// Create a key binding setting control
        /// </summary>
        public static VisualElement CreateKeyBindingSetting(string label, string description, string key, KeyCode defaultKey, Action<string, object> onValueChanged = null)
        {
            var container = CreateSettingContainer(key);
            
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1;
            
            var labelElement = CreateSettingLabel(label);
            var descElement = CreateSettingDescription(description);
            
            labelContainer.Add(labelElement);
            labelContainer.Add(descElement);
            
            var keyButton = new Button();
            keyButton.name = key + "-keybinding";
            keyButton.text = defaultKey.ToString();
            keyButton.style.minWidth = 100;
            
            bool isBinding = false;
            
            keyButton.RegisterCallback<ClickEvent>(evt => {
                if (!isBinding)
                {
                    isBinding = true;
                    keyButton.text = "Press Key...";
                    // Here you would implement key capture logic
                    // This is a simplified representation
                }
            });
            
            container.Add(labelContainer);
            container.Add(keyButton);
            
            return container;
        }
        
        /// <summary>
        /// Create a setting container with consistent styling
        /// </summary>
        private static VisualElement CreateSettingContainer(string key, bool horizontalLayout = true)
        {
            var container = new VisualElement();
            container.name = key + "-setting";
            container.style.marginBottom = 16;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            
            if (horizontalLayout)
            {
                container.style.flexDirection = FlexDirection.Row;
                container.style.justifyContent = Justify.SpaceBetween;
                container.style.alignItems = Align.Center;
            }
            else
            {
                container.style.flexDirection = FlexDirection.Column;
            }
            
            return container;
        }
        
        /// <summary>
        /// Create a setting label with consistent styling
        /// </summary>
        private static Label CreateSettingLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            return label;
        }
        
        /// <summary>
        /// Create a setting description with consistent styling
        /// </summary>
        private static Label CreateSettingDescription(string text)
        {
            var description = new Label(text);
            description.style.fontSize = 12;
            description.style.marginTop = 4;
            
            return description;
        }
        
        /// <summary>
        /// Validate and clamp numeric values
        /// </summary>
        public static T ClampValue<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }
        
        /// <summary>
        /// Create a settings group container
        /// </summary>
        public static VisualElement CreateSettingsGroup(string title, params VisualElement[] settings)
        {
            var group = new VisualElement();
            group.name = title.ToLower().Replace(" ", "-") + "-group";
            group.style.marginBottom = 24;
            
            if (!string.IsNullOrEmpty(title))
            {
                var groupTitle = new Label(title);
                groupTitle.style.fontSize = 16;
                groupTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                groupTitle.style.marginBottom = 12;
                group.Add(groupTitle);
            }
            
            foreach (var setting in settings)
            {
                group.Add(setting);
            }
            
            return group;
        }
        
        /// <summary>
        /// Create a horizontal separator
        /// </summary>
        public static VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            separator.style.marginTop = 8;
            separator.style.marginBottom = 8;
            
            return separator;
        }
    }
}