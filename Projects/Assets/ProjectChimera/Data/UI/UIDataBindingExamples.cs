using UnityEngine;
using ProjectChimera.Data.UI;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// Example configurations for UI Data Binding ScriptableObjects.
    /// This file demonstrates proper configuration patterns to prevent Unity Editor errors.
    /// </summary>
    public static class UIDataBindingExamples
    {
        /// <summary>
        /// Creates a properly configured UI Data Binding asset for Plant Health display
        /// </summary>
        public static UIDataBindingSO CreatePlantHealthBinding()
        {
            var binding = ScriptableObject.CreateInstance<UIDataBindingSO>();
            
            // Set binding name and description
            binding.name = "PlantHealthBinding";
            
            // Use reflection to set private fields (Editor-only configuration)
            #if UNITY_EDITOR
            var bindingNameField = typeof(UIDataBindingSO).GetField("_bindingName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(UIDataBindingSO).GetField("_description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourceManagerField = typeof(UIDataBindingSO).GetField("_sourceManagerType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourcePropertyField = typeof(UIDataBindingSO).GetField("_sourcePropertyPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetElementField = typeof(UIDataBindingSO).GetField("_targetUIElement", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetPropertyField = typeof(UIDataBindingSO).GetField("_targetProperty", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bindingNameField?.SetValue(binding, "PlantHealthDisplay");
            descriptionField?.SetValue(binding, "Displays current plant health percentage");
            sourceManagerField?.SetValue(binding, "PlantManager");
            sourcePropertyField?.SetValue(binding, "SelectedPlant.Health");
            targetElementField?.SetValue(binding, "HealthBar");
            targetPropertyField?.SetValue(binding, "fillAmount");
            #endif
            
            return binding;
        }
        
        /// <summary>
        /// Creates a properly configured UI Data Binding asset for Environmental Temperature
        /// </summary>
        public static UIDataBindingSO CreateTemperatureBinding()
        {
            var binding = ScriptableObject.CreateInstance<UIDataBindingSO>();
            
            binding.name = "TemperatureBinding";
            
            #if UNITY_EDITOR
            var bindingNameField = typeof(UIDataBindingSO).GetField("_bindingName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(UIDataBindingSO).GetField("_description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourceManagerField = typeof(UIDataBindingSO).GetField("_sourceManagerType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourcePropertyField = typeof(UIDataBindingSO).GetField("_sourcePropertyPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetElementField = typeof(UIDataBindingSO).GetField("_targetUIElement", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetPropertyField = typeof(UIDataBindingSO).GetField("_targetProperty", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bindingNameField?.SetValue(binding, "EnvironmentalTemperature");
            descriptionField?.SetValue(binding, "Displays current environmental temperature");
            sourceManagerField?.SetValue(binding, "EnvironmentalManager");
            sourcePropertyField?.SetValue(binding, "CurrentConditions.Temperature");
            targetElementField?.SetValue(binding, "TemperatureText");
            targetPropertyField?.SetValue(binding, "text");
            #endif
            
            return binding;
        }
        
        /// <summary>
        /// Creates a properly configured UI Data Binding asset for Currency Display
        /// </summary>
        public static UIDataBindingSO CreateCurrencyBinding()
        {
            var binding = ScriptableObject.CreateInstance<UIDataBindingSO>();
            
            binding.name = "CurrencyBinding";
            
            #if UNITY_EDITOR
            var bindingNameField = typeof(UIDataBindingSO).GetField("_bindingName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(UIDataBindingSO).GetField("_description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourceManagerField = typeof(UIDataBindingSO).GetField("_sourceManagerType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sourcePropertyField = typeof(UIDataBindingSO).GetField("_sourcePropertyPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetElementField = typeof(UIDataBindingSO).GetField("_targetUIElement", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetPropertyField = typeof(UIDataBindingSO).GetField("_targetProperty", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataConverterField = typeof(UIDataBindingSO).GetField("_dataConverter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bindingNameField?.SetValue(binding, "PlayerCurrency");
            descriptionField?.SetValue(binding, "Displays player's current currency");
            sourceManagerField?.SetValue(binding, "EconomyManager");
            sourcePropertyField?.SetValue(binding, "PlayerMoney");
            targetElementField?.SetValue(binding, "CurrencyText");
            targetPropertyField?.SetValue(binding, "text");
            dataConverterField?.SetValue(binding, DataConverter.ToCurrency);
            #endif
            
            return binding;
        }
    }
}