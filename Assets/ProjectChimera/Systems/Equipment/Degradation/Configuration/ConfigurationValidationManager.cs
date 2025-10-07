using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Configuration Validation Manager Coordinator
    /// Single Responsibility: Orchestrate configuration validation operations
    /// BEFORE: 759 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (ValidationDataStructures, ConstraintValidator, ValidationRuleEngine, this coordinator)
    /// </summary>
    public class ConfigurationValidationManager
    {
        private readonly bool _enableLogging;
        private readonly bool _enableValidation;
        private readonly bool _enforceConstraints;
        private readonly bool _logValidationWarnings;
        private readonly ValidationMode _validationMode;

        // PHASE 0: Component-based architecture (SRP)
        private readonly ConstraintValidator _constraintValidator;
        private readonly ValidationRuleEngine _ruleEngine;

        // Validation statistics
        private ValidationStatistics _validationStats = new ValidationStatistics();

        // Events
        public event Action<ValidationResult> OnValidationCompleted;
        public event Action<string, ValidationFailure> OnParameterValidationFailed;
        public event Action<ValidationRule, bool> OnCustomRuleValidated;

        // Properties
        public ValidationStatistics Statistics => _validationStats;
        public bool IsValidationEnabled => _enableValidation;
        public ValidationMode Mode => _validationMode;
        public int ConstraintCount => _constraintValidator?.Constraints.Count ?? 0;
        public int ValidatorCount => _constraintValidator?.Validators.Count ?? 0;
        public int CustomRuleCount => _ruleEngine?.CustomRules.Count ?? 0;

        public ConfigurationValidationManager(
            bool enableLogging = false,
            bool enableValidation = true,
            bool enforceConstraints = true,
            bool logValidationWarnings = true,
            ValidationMode validationMode = ValidationMode.Strict)
        {
            _enableLogging = enableLogging;
            _enableValidation = enableValidation;
            _enforceConstraints = enforceConstraints;
            _logValidationWarnings = logValidationWarnings;
            _validationMode = validationMode;

            // Initialize components
            _constraintValidator = new ConstraintValidator(enableLogging, validationMode);
            _ruleEngine = new ValidationRuleEngine(enableLogging, validationMode);

            // Subscribe to events
            _ruleEngine.OnCustomRuleValidated += (rule, result) =>
            {
                OnCustomRuleValidated?.Invoke(rule, result);
            };

            // Add common cross-parameter rules
            _ruleEngine.AddCommonCrossParameterRules();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", "Configuration Validation Manager initialized", null);
        }

        #region Validation Operations

        /// <summary>
        /// Validate entire configuration profile
        /// </summary>
        public ValidationResult ValidateConfiguration(CostConfigurationProfile profile)
        {
            var result = ValidationResult.Create(profile?.Name ?? "Unknown");

            if (!_enableValidation)
            {
                result.Success = true;
                result.Message = "Validation disabled";
                return result;
            }

            if (profile == null)
            {
                result.Success = false;
                result.Message = "Profile is null";
                return result;
            }

            try
            {
                var validationErrors = new List<string>();
                var validationWarnings = new List<string>();

                // Validate all required parameters exist
                _constraintValidator.ValidateRequiredParameters(profile, validationErrors);

                // Validate individual parameters
                _constraintValidator.ValidateParameters(profile, validationErrors, validationWarnings);

                // Run custom validation rules
                _ruleEngine.ValidateCustomRules(profile, validationErrors, validationWarnings);

                // Cross-parameter validation
                _ruleEngine.ValidateCrossParameterRules(profile, validationErrors, validationWarnings);

                result.ValidationErrors = validationErrors;
                result.ValidationWarnings = validationWarnings;
                result.Success = validationErrors.Count == 0;
                result.ExecutionTime = (DateTime.Now - result.StartTime).TotalMilliseconds;

                // Update statistics
                _validationStats.TotalValidations++;
                if (result.Success)
                    _validationStats.SuccessfulValidations++;
                else
                    _validationStats.FailedValidations++;

                _validationStats.TotalErrors += validationErrors.Count;
                _validationStats.TotalWarnings += validationWarnings.Count;
                _validationStats.LastValidation = DateTime.Now;

                OnValidationCompleted?.Invoke(result);

                if (_enableLogging)
                {
                    if (result.Success)
                    {
                        ChimeraLogger.LogInfo("CONFIG_VAL", $"Configuration validation passed for '{profile.Name}'", null);
                    }
                    else
                    {
                        ChimeraLogger.LogWarning("CONFIG_VAL",
                            $"Configuration validation failed for '{profile.Name}': {validationErrors.Count} errors",
                            null);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Validation exception: {ex.Message}";
                _validationStats.ValidationExceptions++;

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_VAL", $"Validation exception: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Validate a single parameter
        /// </summary>
        public bool ValidateParameter(string parameterName, object value)
        {
            if (!_enableValidation || string.IsNullOrEmpty(parameterName))
                return true;

            try
            {
                var result = _constraintValidator.ValidateParameter(parameterName, value);

                if (!result.IsValid)
                {
                    FireParameterValidationFailed(parameterName, result.ErrorMessage, value);
                    _validationStats.ParameterValidationsFailed++;
                    return !_enforceConstraints; // Return true if not enforcing, false if enforcing
                }

                _validationStats.ParameterValidationsPassed++;
                return true;
            }
            catch (Exception ex)
            {
                _validationStats.ParameterValidationExceptions++;
                FireParameterValidationFailed(parameterName, $"Validation exception: {ex.Message}", value);

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_VAL",
                        $"Parameter validation exception for '{parameterName}': {ex.Message}",
                        null);

                return !_enforceConstraints;
            }
        }

        #endregion

        #region Constraint Management

        /// <summary>
        /// Add a configuration constraint
        /// </summary>
        public void AddConstraint(ConfigurationConstraint constraint)
        {
            _constraintValidator.AddConstraint(constraint);
        }

        /// <summary>
        /// Remove a constraint
        /// </summary>
        public bool RemoveConstraint(string parameterName)
        {
            return _constraintValidator.RemoveConstraint(parameterName);
        }

        /// <summary>
        /// Get constraint for parameter
        /// </summary>
        public ConfigurationConstraint GetConstraint(string parameterName)
        {
            return _constraintValidator.GetConstraint(parameterName);
        }

        #endregion

        #region Validator Management

        /// <summary>
        /// Add a parameter validator
        /// </summary>
        public void AddValidator(string parameterName, ParameterValidator validator)
        {
            _constraintValidator.AddValidator(parameterName, validator);
        }

        /// <summary>
        /// Remove a validator
        /// </summary>
        public bool RemoveValidator(string parameterName)
        {
            return _constraintValidator.RemoveValidator(parameterName);
        }

        #endregion

        #region Custom Rule Management

        /// <summary>
        /// Add a custom validation rule
        /// </summary>
        public void AddCustomRule(ValidationRule rule)
        {
            _ruleEngine.AddCustomRule(rule);
        }

        /// <summary>
        /// Remove a custom validation rule
        /// </summary>
        public bool RemoveCustomRule(string ruleName)
        {
            return _ruleEngine.RemoveCustomRule(ruleName);
        }

        #endregion

        #region Statistics and Utilities

        /// <summary>
        /// Reset validation statistics
        /// </summary>
        public void ResetStatistics()
        {
            _validationStats.Reset();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CONFIG_VAL", "Validation statistics reset", null);
        }

        /// <summary>
        /// Get validation summary
        /// </summary>
        public string GetValidationSummary()
        {
            return $"Validation Summary: {_validationStats.TotalValidations} total, " +
                   $"{_validationStats.SuccessfulValidations} successful, " +
                   $"{_validationStats.FailedValidations} failed, " +
                   $"{_validationStats.TotalErrors} errors, " +
                   $"{_validationStats.TotalWarnings} warnings";
        }

        /// <summary>
        /// Fire parameter validation failed event
        /// </summary>
        private void FireParameterValidationFailed(string parameterName, string errorMessage, object value)
        {
            var failure = ValidationFailure.Create(parameterName, errorMessage, value);
            OnParameterValidationFailed?.Invoke(parameterName, failure);

            if (_logValidationWarnings && _enableLogging)
            {
                ChimeraLogger.LogWarning("CONFIG_VAL",
                    $"Parameter validation failed for '{parameterName}': {errorMessage}",
                    null);
            }
        }

        #endregion
    }
}

