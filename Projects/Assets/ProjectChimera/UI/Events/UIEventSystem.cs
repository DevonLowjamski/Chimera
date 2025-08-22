using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
using ProjectChimera.Data;

namespace ProjectChimera.UI.Events
{
    /// <summary>
    /// UI-specific event data for button clicks
    /// </summary>
    [System.Serializable]
    public struct UIButtonClickData
    {
        public string ButtonId;
        public string PanelId;
        public Vector2 Position;
        public float Timestamp;
        
        public UIButtonClickData(string buttonId, string panelId, Vector2 position)
        {
            ButtonId = buttonId;
            PanelId = panelId;
            Position = position;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// UI-specific event data for input field changes
    /// </summary>
    [System.Serializable]
    public struct UIInputFieldData
    {
        public string FieldId;
        public string PanelId;
        public string OldValue;
        public string NewValue;
        public float Timestamp;
        
        public UIInputFieldData(string fieldId, string panelId, string oldValue, string newValue)
        {
            FieldId = fieldId;
            PanelId = panelId;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// UI-specific event data for slider changes
    /// </summary>
    [System.Serializable]
    public struct UISliderData
    {
        public string SliderId;
        public string PanelId;
        public float OldValue;
        public float NewValue;
        public float Timestamp;
        
        public UISliderData(string sliderId, string panelId, float oldValue, float newValue)
        {
            SliderId = sliderId;
            PanelId = panelId;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// UI-specific event data for dropdown changes
    /// </summary>
    [System.Serializable]
    public struct UIDropdownData
    {
        public string DropdownId;
        public string PanelId;
        public int OldIndex;
        public int NewIndex;
        public string OldValue;
        public string NewValue;
        public float Timestamp;
        
        public UIDropdownData(string dropdownId, string panelId, int oldIndex, int newIndex, string oldValue, string newValue)
        {
            DropdownId = dropdownId;
            PanelId = panelId;
            OldIndex = oldIndex;
            NewIndex = newIndex;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// UI Button Click Event Channel
    /// </summary>
    [CreateAssetMenu(fileName = "UIButtonClickEvent", menuName = "Project Chimera/Events/UI/Button Click Event")]
    public class UIButtonClickEventSO : GameEventSO<UIButtonClickData>
    {
        /// <summary>
        /// Raise event with button click data
        /// </summary>
        public void RaiseButtonClick(string buttonId, string panelId, Vector2 position)
        {
            var data = new UIButtonClickData(buttonId, panelId, position);
            Raise(data);
        }
    }
    
    /// <summary>
    /// UI Input Field Change Event Channel
    /// </summary>
    [CreateAssetMenu(fileName = "UIInputFieldEvent", menuName = "Project Chimera/Events/UI/Input Field Event")]
    public class UIInputFieldEventSO : GameEventSO<UIInputFieldData>
    {
        /// <summary>
        /// Raise event with input field data
        /// </summary>
        public void RaiseInputFieldChange(string fieldId, string panelId, string oldValue, string newValue)
        {
            var data = new UIInputFieldData(fieldId, panelId, oldValue, newValue);
            Raise(data);
        }
    }
    
    /// <summary>
    /// UI Slider Change Event Channel
    /// </summary>
    [CreateAssetMenu(fileName = "UISliderEvent", menuName = "Project Chimera/Events/UI/Slider Event")]
    public class UISliderEventSO : GameEventSO<UISliderData>
    {
        /// <summary>
        /// Raise event with slider data
        /// </summary>
        public void RaiseSliderChange(string sliderId, string panelId, float oldValue, float newValue)
        {
            var data = new UISliderData(sliderId, panelId, oldValue, newValue);
            Raise(data);
        }
    }
    
    /// <summary>
    /// UI Dropdown Change Event Channel
    /// </summary>
    [CreateAssetMenu(fileName = "UIDropdownEvent", menuName = "Project Chimera/Events/UI/Dropdown Event")]
    public class UIDropdownEventSO : GameEventSO<UIDropdownData>
    {
        /// <summary>
        /// Raise event with dropdown data
        /// </summary>
        public void RaiseDropdownChange(string dropdownId, string panelId, int oldIndex, int newIndex, string oldValue, string newValue)
        {
            var data = new UIDropdownData(dropdownId, panelId, oldIndex, newIndex, oldValue, newValue);
            Raise(data);
        }
    }
    
    /// <summary>
    /// UI Element Extensions for easy event binding
    /// </summary>
    public static class UIElementExtensions
    {
        /// <summary>
        /// Bind button click to event channel
        /// </summary>
        public static void BindToEventChannel(this Button button, string buttonId, string panelId, UIButtonClickEventSO eventChannel)
        {
            if (eventChannel == null) return;
            
            button.RegisterCallback<ClickEvent>(evt =>
            {
                var position = new Vector2(evt.position.x, evt.position.y);
                eventChannel.RaiseButtonClick(buttonId, panelId, position);
            });
        }
        
        /// <summary>
        /// Bind input field changes to event channel
        /// </summary>
        public static void BindToEventChannel(this TextField textField, string fieldId, string panelId, UIInputFieldEventSO eventChannel)
        {
            if (eventChannel == null) return;
            
            string previousValue = textField.value;
            
            textField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                eventChannel.RaiseInputFieldChange(fieldId, panelId, previousValue, evt.newValue);
                previousValue = evt.newValue;
            });
        }
        
        /// <summary>
        /// Bind slider changes to event channel
        /// </summary>
        public static void BindToEventChannel(this Slider slider, string sliderId, string panelId, UISliderEventSO eventChannel)
        {
            if (eventChannel == null) return;
            
            float previousValue = slider.value;
            
            slider.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                eventChannel.RaiseSliderChange(sliderId, panelId, previousValue, evt.newValue);
                previousValue = evt.newValue;
            });
        }
        
        /// <summary>
        /// Bind dropdown changes to event channel
        /// </summary>
        public static void BindToEventChannel(this DropdownField dropdown, string dropdownId, string panelId, UIDropdownEventSO eventChannel)
        {
            if (eventChannel == null) return;
            
            int previousIndex = dropdown.index;
            string previousValue = dropdown.value;
            
            dropdown.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                int newIndex = dropdown.index;
                eventChannel.RaiseDropdownChange(dropdownId, panelId, previousIndex, newIndex, previousValue, evt.newValue);
                previousIndex = newIndex;
                previousValue = evt.newValue;
            });
        }
        
        /// <summary>
        /// Bind toggle changes to simple event channel
        /// </summary>
        public static void BindToEventChannel(this Toggle toggle, string toggleId, string panelId, SimpleGameEventSO eventChannel)
        {
            if (eventChannel == null) return;
            
            toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                eventChannel.Raise();
            });
        }
        
        /// <summary>
        /// Add hover effects to any visual element
        /// </summary>
        public static void AddHoverEffects(this VisualElement element, Color hoverColor, Color normalColor)
        {
            element.RegisterCallback<MouseEnterEvent>(evt =>
            {
                element.style.backgroundColor = hoverColor;
            });
            
            element.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                element.style.backgroundColor = normalColor;
            });
        }
        
        /// <summary>
        /// Add click animation to button
        /// </summary>
        public static void AddClickAnimation(this Button button, float scaleAmount = 0.95f, float duration = 0.1f)
        {
            Vector3 originalScale = button.transform.scale;
            
            button.RegisterCallback<MouseDownEvent>(evt =>
            {
                button.style.scale = new Scale(Vector3.one * scaleAmount);
            });
            
            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                button.style.scale = new Scale(originalScale);
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.scale = new Scale(originalScale);
            });
        }
        
        /// <summary>
        /// Set up tooltip for any visual element
        /// </summary>
        public static void SetupTooltip(this VisualElement element, string tooltipText, VisualElement tooltipContainer)
        {
            if (string.IsNullOrEmpty(tooltipText) || tooltipContainer == null) return;
            
            var tooltip = new Label(tooltipText);
            tooltip.name = "tooltip";
            tooltip.style.position = Position.Absolute;
            tooltip.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            tooltip.style.color = Color.white;
            tooltip.style.paddingTop = 4;
            tooltip.style.paddingBottom = 4;
            tooltip.style.paddingLeft = 8;
            tooltip.style.paddingRight = 8;
            tooltip.style.borderTopLeftRadius = 4;
            tooltip.style.borderTopRightRadius = 4;
            tooltip.style.borderBottomLeftRadius = 4;
            tooltip.style.borderBottomRightRadius = 4;
            tooltip.style.display = DisplayStyle.None;
            
            element.RegisterCallback<MouseEnterEvent>(evt =>
            {
                tooltip.style.display = DisplayStyle.Flex;
                tooltip.style.left = evt.mousePosition.x + 10;
                tooltip.style.top = evt.mousePosition.y - 30;
                tooltipContainer.Add(tooltip);
            });
            
            element.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                tooltip.style.display = DisplayStyle.None;
                tooltip.RemoveFromHierarchy();
            });
            
            element.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (tooltip.style.display == DisplayStyle.Flex)
                {
                    tooltip.style.left = evt.mousePosition.x + 10;
                    tooltip.style.top = evt.mousePosition.y - 30;
                }
            });
        }
        
        /// <summary>
        /// Apply consistent button styling
        /// </summary>
        public static void ApplyButtonStyle(this Button button, UIButtonStyle style, Color primaryColor, Color textColor)
        {
            switch (style)
            {
                case UIButtonStyle.Primary:
                    button.style.backgroundColor = primaryColor;
                    button.style.color = textColor;
                    button.style.borderTopWidth = 0;
                    button.style.borderRightWidth = 0;
                    button.style.borderBottomWidth = 0;
                    button.style.borderLeftWidth = 0;
                    break;
                    
                case UIButtonStyle.Secondary:
                    button.style.backgroundColor = Color.clear;
                    button.style.color = primaryColor;
                    button.style.borderTopColor = primaryColor;
                    button.style.borderRightColor = primaryColor;
                    button.style.borderBottomColor = primaryColor;
                    button.style.borderLeftColor = primaryColor;
                    button.style.borderTopWidth = 1;
                    button.style.borderRightWidth = 1;
                    button.style.borderBottomWidth = 1;
                    button.style.borderLeftWidth = 1;
                    break;
                    
                case UIButtonStyle.Ghost:
                    button.style.backgroundColor = Color.clear;
                    button.style.color = primaryColor;
                    button.style.borderTopWidth = 0;
                    button.style.borderRightWidth = 0;
                    button.style.borderBottomWidth = 0;
                    button.style.borderLeftWidth = 0;
                    break;
            }
            
            // Add consistent border radius
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            
            // Add consistent padding
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 16;
            button.style.paddingRight = 16;
        }
    }
    
    /// <summary>
    /// UI Button style enumeration
    /// </summary>
    public enum UIButtonStyle
    {
        Primary,
        Secondary,
        Ghost
    }
}