using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.UI
{
    /// <summary>
    /// ScriptableObject for managing UI data binding configurations.
    /// Handles automatic synchronization between game data and UI elements.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Data Binding", menuName = "Project Chimera/UI/Data Binding")]
    public class UIDataBindingSO : ChimeraDataSO
    {
        [Header("Binding Configuration")]
        [SerializeField] private string _bindingName;
        [SerializeField] private string _description;
        [SerializeField] private BindingMode _bindingMode = BindingMode.TwoWay;
        [SerializeField] private float _updateInterval = 0.1f;
        
        [Header("Source Configuration")]
        [SerializeField] private string _sourceManagerType;
        [SerializeField] private string _sourcePropertyPath;
        // Serialized for inspector; read through public API when enabled
        #pragma warning disable 0414
        [SerializeField] private bool _enableAutoRefresh = true;
        #pragma warning restore 0414
        
        [Header("Target Configuration")]
        [SerializeField] private string _targetUIElement;
        [SerializeField] private string _targetProperty;
        [SerializeField] private DataConverter _dataConverter = DataConverter.None;
        
        [Header("Binding Rules")]
        [SerializeField] private List<BindingRule> _bindingRules = new List<BindingRule>();
        [SerializeField] private List<BindingValidator> _validators = new List<BindingValidator>();
        
        [Header("Events")]
        [SerializeField] private bool _logBindingEvents = false;
        // Note: Event channel integration will be handled at runtime via dependency injection
        
        // Runtime data
        private Dictionary<string, object> _cachedValues = new Dictionary<string, object>();
        private DateTime _lastUpdate = DateTime.MinValue;
        private bool _isActive = false;
        
        // Properties
        public string BindingName => _bindingName;
        public string Description => _description;
        public BindingMode Mode => _bindingMode;
        public float UpdateInterval => _updateInterval;
        public string SourceManagerType => _sourceManagerType;
        public string SourcePropertyPath => _sourcePropertyPath;
        public string TargetUIElement => _targetUIElement;
        public string TargetProperty => _targetProperty;
        public bool IsActive => _isActive;
        
        // Events
        public event Action<object> OnValueChanged;
        public event Action<string> OnBindingError;
        public event Action OnBindingActivated;
        public event Action OnBindingDeactivated;
        
        /// <summary>
        /// Activate the data binding
        /// </summary>
        public void ActivateBinding()
        {
            if (_isActive) return;
            
            _isActive = true;
            _lastUpdate = DateTime.Now;
            
            if (_logBindingEvents)
            {
                Debug.Log($"[UIDataBinding] Activated binding: {_bindingName}");
            }
            
            OnBindingActivated?.Invoke();
        }
        
        /// <summary>
        /// Deactivate the data binding
        /// </summary>
        public void DeactivateBinding()
        {
            if (!_isActive) return;
            
            _isActive = false;
            
            if (_logBindingEvents)
            {
                Debug.Log($"[UIDataBinding] Deactivated binding: {_bindingName}");
            }
            
            OnBindingDeactivated?.Invoke();
        }
        
        /// <summary>
        /// Update the binding with new value from source
        /// </summary>
        public void UpdateFromSource(object value)
        {
            if (!_isActive) return;
            
            // Check if enough time has passed for update
            if ((DateTime.Now - _lastUpdate).TotalSeconds < _updateInterval)
                return;
            
            // Validate the value
            if (!ValidateValue(value))
            {
                OnBindingError?.Invoke($"Value validation failed for {_bindingName}");
                return;
            }
            
            // Apply data conversion
            var convertedValue = ConvertValue(value);
            
            // Apply binding rules
            convertedValue = ApplyBindingRules(convertedValue);
            
            // Cache the value
            _cachedValues[_sourcePropertyPath] = convertedValue;
            _lastUpdate = DateTime.Now;
            
            OnValueChanged?.Invoke(convertedValue);
            
            if (_logBindingEvents)
            {
                Debug.Log($"[UIDataBinding] Updated {_bindingName}: {value} -> {convertedValue}");
            }
        }
        
        /// <summary>
        /// Update the source from UI value (for two-way binding)
        /// </summary>
        public void UpdateToSource(object value)
        {
            if (!_isActive || _bindingMode == BindingMode.OneWay) return;
            
            // Validate the value
            if (!ValidateValue(value))
            {
                OnBindingError?.Invoke($"UI value validation failed for {_bindingName}");
                return;
            }
            
            // Apply data conversion
            var convertedValue = ConvertValue(value);
            
            // Apply binding rules
            convertedValue = ApplyBindingRules(convertedValue);
            
            // Cache the value
            _cachedValues[_sourcePropertyPath] = convertedValue;
            _lastUpdate = DateTime.Now;
            
            OnValueChanged?.Invoke(convertedValue);
            
            if (_logBindingEvents)
            {
                Debug.Log($"[UIDataBinding] Updating source {_bindingName}: {value} -> {convertedValue}");
            }
        }
        
        /// <summary>
        /// Get cached value for a property
        /// </summary>
        public T GetCachedValue<T>(string propertyPath, T defaultValue = default(T))
        {
            if (_cachedValues.TryGetValue(propertyPath, out var value) && value is T)
            {
                return (T)value;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Clear cached values
        /// </summary>
        public void ClearCache()
        {
            _cachedValues.Clear();
            
            if (_logBindingEvents)
            {
                Debug.Log($"[UIDataBinding] Cleared cache for {_bindingName}");
            }
        }
        
        private bool ValidateValue(object value)
        {
            foreach (var validator in _validators)
            {
                if (!validator.Validate(value))
                {
                    return false;
                }
            }
            return true;
        }
        
        private object ConvertValue(object value)
        {
            switch (_dataConverter)
            {
                case DataConverter.ToString:
                    return value?.ToString() ?? "";
                    
                case DataConverter.ToFloat:
                    if (float.TryParse(value?.ToString(), out var floatResult))
                        return floatResult;
                    break;
                    
                case DataConverter.ToInt:
                    if (int.TryParse(value?.ToString(), out var intResult))
                        return intResult;
                    break;
                    
                case DataConverter.ToBool:
                    if (bool.TryParse(value?.ToString(), out var boolResult))
                        return boolResult;
                    break;
                    
                case DataConverter.ToPercentage:
                    if (value is float f)
                        return $"{f * 100:F1}%";
                    break;
                    
                case DataConverter.ToCurrency:
                    if (value is float currency)
                        return $"${currency:F2}";
                    break;
            }
            
            return value;
        }
        
        private object ApplyBindingRules(object value)
        {
            foreach (var rule in _bindingRules)
            {
                value = rule.ApplyRule(value);
            }
            return value;
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (string.IsNullOrEmpty(_bindingName))
            {
                _bindingName = name;
            }
        }
        
        protected override bool ValidateDataSpecific()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(_bindingName))
            {
                LogError("Binding name cannot be empty");
                isValid = false;
            }
            
            if (string.IsNullOrEmpty(_sourceManagerType))
            {
                LogError("Source manager type must be specified");
                isValid = false;
            }
            
            if (string.IsNullOrEmpty(_sourcePropertyPath))
            {
                LogError("Source property path must be specified");
                isValid = false;
            }
            
            if (string.IsNullOrEmpty(_targetUIElement))
            {
                LogError("Target UI element must be specified");
                isValid = false;
            }
            
            if (_updateInterval < 0)
            {
                LogError("Update interval cannot be negative");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Binding modes for data flow
    /// </summary>
    public enum BindingMode
    {
        OneWay,     // Source to UI only
        TwoWay,     // Source to UI and UI to Source
        OneTime     // Source to UI once
    }
    
    /// <summary>
    /// Data conversion types
    /// </summary>
    public enum DataConverter
    {
        None,
        ToString,
        ToFloat,
        ToInt,
        ToBool,
        ToPercentage,
        ToCurrency,
        Custom
    }
    
    /// <summary>
    /// Binding rule for value transformation
    /// </summary>
    [System.Serializable]
    public class BindingRule
    {
        [SerializeField] private string _ruleName;
        [SerializeField] private RuleType _type;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private string _format;
        
        public object ApplyRule(object value)
        {
            switch (_type)
            {
                case RuleType.Clamp:
                    if (value is float f)
                        return Mathf.Clamp(f, _minValue, _maxValue);
                    break;
                    
                case RuleType.Format:
                    if (!string.IsNullOrEmpty(_format))
                        return string.Format(_format, value);
                    break;
                    
                case RuleType.Round:
                    if (value is float rf)
                        return Mathf.Round(rf);
                    break;
            }
            
            return value;
        }
    }
    
    /// <summary>
    /// Types of binding rules
    /// </summary>
    public enum RuleType
    {
        None,
        Clamp,
        Format,
        Round,
        Custom
    }
    
    /// <summary>
    /// Validator for binding values
    /// </summary>
    [System.Serializable]
    public class BindingValidator
    {
        [SerializeField] private string _validatorName;
        [SerializeField] private ValidationType _type;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private string _pattern;
        
        public bool Validate(object value)
        {
            switch (_type)
            {
                case ValidationType.Range:
                    if (value is float f)
                        return f >= _minValue && f <= _maxValue;
                    break;
                    
                case ValidationType.NotNull:
                    return value != null;
                    
                case ValidationType.NotEmpty:
                    return !string.IsNullOrEmpty(value?.ToString());
                    
                case ValidationType.Pattern:
                    if (!string.IsNullOrEmpty(_pattern))
                        return System.Text.RegularExpressions.Regex.IsMatch(value?.ToString() ?? "", _pattern);
                    break;
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Types of validation
    /// </summary>
    public enum ValidationType
    {
        None,
        Range,
        NotNull,
        NotEmpty,
        Pattern,
        Custom
    }
    
    /// <summary>
    /// Request for updating a property value
    /// </summary>
    [System.Serializable]
    public class PropertyUpdateRequest
    {
        public string PropertyPath;
        public object Value;
        public string BindingName;
        public DateTime Timestamp = DateTime.Now;
    }
}