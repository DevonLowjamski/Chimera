using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Constraint Validator
    /// Single Responsibility: Validate parameters against constraints and built-in validators
    /// Extracted from ConfigurationValidationManager (759 lines → 4 files <500 lines each)
    /// </summary>
    public class ConstraintValidator
    {
        private readonly List<ConfigurationConstraint> _constraints;
        private readonly Dictionary<string, ParameterValidator> _validators;
        private readonly bool _enableLogging;
        private readonly ValidationMode _validationMode;

        public IReadOnlyList<ConfigurationConstraint> Constraints => _constraints;
        public IReadOnlyDictionary<string, ParameterValidator> Validators => _validators;

        public ConstraintValidator(bool enableLogging, ValidationMode validationMode)
        {
            _constraints = new List<ConfigurationConstraint>();
            _validators = new Dictionary<string, ParameterValidator>();
            _enableLogging = enableLogging;
            _validationMode = validationMode;

            InitializeDefaultConstraints();
            InitializeBuiltInValidators();
        }

        /// <summary>
        /// Add a configuration constraint
        /// </summary>
        public void AddConstraint(ConfigurationConstraint constraint)
        {
            if (constraint == null || string.IsNullOrEmpty(constraint.ParameterName))
                return;

            // Remove existing constraint for the same parameter
            _constraints.RemoveAll(c => c.ParameterName == constraint.ParameterName);
            _constraints.Add(constraint);

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Added constraint for parameter '{constraint.ParameterName}'", null);
        }

        /// <summary>
        /// Remove a constraint
        /// </summary>
        public bool RemoveConstraint(string parameterName)
        {
            var removed = _constraints.RemoveAll(c => c.ParameterName == parameterName);

            if (removed > 0 && _enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Removed constraint for parameter '{parameterName}'", null);

            return removed > 0;
        }

        /// <summary>
        /// Get constraint for parameter
        /// </summary>
        public ConfigurationConstraint GetConstraint(string parameterName)
        {
            return _constraints.FirstOrDefault(c => c.ParameterName == parameterName);
        }

        /// <summary>
        /// Add a parameter validator
        /// </summary>
        public void AddValidator(string parameterName, ParameterValidator validator)
        {
            if (string.IsNullOrEmpty(parameterName) || validator == null)
                return;

            _validators[parameterName] = validator;

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Added validator for parameter '{parameterName}'", null);
        }

        /// <summary>
        /// Remove a validator
        /// </summary>
        public bool RemoveValidator(string parameterName)
        {
            var removed = _validators.Remove(parameterName);

            if (removed && _enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Removed validator for parameter '{parameterName}'", null);

            return removed;
        }

        /// <summary>
        /// Validate parameter against constraints and validators
        /// </summary>
        public ParameterValidationResult ValidateParameter(string parameterName, object value)
        {
            // Check constraints
            var constraint = GetConstraint(parameterName);
            if (constraint != null)
            {
                var constraintResult = ValidateAgainstConstraint(parameterName, value, constraint);
                if (!constraintResult.IsValid)
                {
                    return constraintResult;
                }
            }

            // Check custom validators
            if (_validators.TryGetValue(parameterName, out var validator))
            {
                return validator.ValidatorFunction(value);
            }

            return ParameterValidationResult.Success();
        }

        /// <summary>
        /// Validate all parameters in profile
        /// </summary>
        public void ValidateParameters(CostConfigurationProfile profile, List<string> errors, List<string> warnings)
        {
            foreach (var parameter in profile.Parameters)
            {
                var result = ValidateParameter(parameter.Key, parameter.Value);
                if (!result.IsValid)
                {
                    if (_validationMode == ValidationMode.Strict)
                        errors.Add(result.ErrorMessage);
                    else
                        warnings.Add(result.ErrorMessage);
                }
            }
        }

        /// <summary>
        /// Validate required parameters exist
        /// </summary>
        public void ValidateRequiredParameters(CostConfigurationProfile profile, List<string> errors)
        {
            foreach (var constraint in _constraints.Where(c => c.IsRequired))
            {
                if (!profile.HasParameter(constraint.ParameterName))
                {
                    errors.Add($"Required parameter '{constraint.ParameterName}' is missing");
                }
            }
        }

        #region Private Methods

        /// <summary>
        /// Initialize default validation constraints
        /// </summary>
        private void InitializeDefaultConstraints()
        {
            // Cost-related constraints
            AddConstraint(ConfigurationConstraint.CreateRangeConstraint("BaseCost", 0.0f, 50000.0f, true));
            AddConstraint(ConfigurationConstraint.CreateRangeConstraint("LaborRatePerHour", 10.0f, 500.0f, true));
            AddConstraint(ConfigurationConstraint.CreateRangeConstraint("MaterialMarkup", 1.0f, 10.0f, true));
            AddConstraint(ConfigurationConstraint.CreateRangeConstraint("UrgencyMultiplier", 1.0f, 5.0f, true));
            AddConstraint(ConfigurationConstraint.CreateRangeConstraint("ComplexityFactor", 0.1f, 10.0f, false));

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Initialized {_constraints.Count} default constraints", null);
        }

        /// <summary>
        /// Initialize built-in parameter validators
        /// </summary>
        private void InitializeBuiltInValidators()
        {
            // Cost validators
            AddValidator("BaseCost", ParameterValidator.Create(
                value => ValidatePositiveFloat(value, "BaseCost"),
                "Validates that base cost is a positive float value"));

            AddValidator("LaborRatePerHour", ParameterValidator.Create(
                value => ValidateRangeFloat(value, 10.0f, 500.0f, "LaborRatePerHour"),
                "Validates labor rate is within acceptable range"));

            AddValidator("MaterialMarkup", ParameterValidator.Create(
                value => ValidateMultiplier(value, "MaterialMarkup"),
                "Validates material markup is a proper multiplier"));

            AddValidator("ConsistencyCheck", ParameterValidator.Create(
                ValidateParameterConsistency,
                "Validates cross-parameter consistency"));

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", $"Initialized {_validators.Count} built-in validators", null);
        }

        /// <summary>
        /// Validate parameter against constraint
        /// </summary>
        private ParameterValidationResult ValidateAgainstConstraint(
            string parameterName,
            object value,
            ConfigurationConstraint constraint)
        {
            // Type validation
            if (constraint.DataType != null && value != null && !constraint.DataType.IsAssignableFrom(value.GetType()))
            {
                return ParameterValidationResult.Failure(
                    $"Parameter '{parameterName}' expected type {constraint.DataType.Name}, got {value.GetType().Name}");
            }

            // Range validation for numeric types
            if (value is IComparable comparableValue)
            {
                if (constraint.MinValue != null && comparableValue.CompareTo(constraint.MinValue) < 0)
                {
                    return ParameterValidationResult.Failure(
                        $"Parameter '{parameterName}' value {value} is below minimum {constraint.MinValue}");
                }

                if (constraint.MaxValue != null && comparableValue.CompareTo(constraint.MaxValue) > 0)
                {
                    return ParameterValidationResult.Failure(
                        $"Parameter '{parameterName}' value {value} exceeds maximum {constraint.MaxValue}");
                }
            }

            return ParameterValidationResult.Success();
        }

        #endregion

        #region Built-in Validators

        /// <summary>
        /// Validate positive float value
        /// </summary>
        private ParameterValidationResult ValidatePositiveFloat(object value, string parameterName)
        {
            if (value is float floatValue)
            {
                if (floatValue >= 0)
                {
                    return ParameterValidationResult.Success();
                }
                else
                {
                    return ParameterValidationResult.Failure(
                        $"Parameter '{parameterName}' must be non-negative, got {floatValue}");
                }
            }

            return ParameterValidationResult.Failure(
                $"Parameter '{parameterName}' must be a float value");
        }

        /// <summary>
        /// Validate float within range
        /// </summary>
        private ParameterValidationResult ValidateRangeFloat(object value, float min, float max, string parameterName)
        {
            if (value is float floatValue)
            {
                if (floatValue >= min && floatValue <= max)
                {
                    return ParameterValidationResult.Success();
                }
                else
                {
                    return ParameterValidationResult.Failure(
                        $"Parameter '{parameterName}' must be between {min} and {max}, got {floatValue}");
                }
            }

            return ParameterValidationResult.Failure(
                $"Parameter '{parameterName}' must be a float value");
        }

        /// <summary>
        /// Validate multiplier (must be >= 1.0)
        /// </summary>
        private ParameterValidationResult ValidateMultiplier(object value, string parameterName)
        {
            if (value is float floatValue)
            {
                if (floatValue >= 1.0f)
                {
                    return ParameterValidationResult.Success();
                }
                else
                {
                    return ParameterValidationResult.Failure(
                        $"Multiplier '{parameterName}' must be >= 1.0, got {floatValue}");
                }
            }

            return ParameterValidationResult.Failure(
                $"Multiplier '{parameterName}' must be a float value");
        }

        /// <summary>
        /// Validate parameter consistency
        /// </summary>
        private ParameterValidationResult ValidateParameterConsistency(object value)
        {
            // Placeholder for complex consistency validation
            return ParameterValidationResult.Success();
        }

        #endregion
    }
}

