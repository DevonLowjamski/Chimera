using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Validation Data Structures
    /// Single Responsibility: Define all validation-related data types
    /// Extracted from ConfigurationValidationManager (759 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Validation mode
    /// </summary>
    public enum ValidationMode
    {
        Strict,    // All validations must pass
        Lenient,   // Warnings allowed
        Disabled   // No validation
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// Configuration constraint definition
    /// </summary>
    [Serializable]
    public class ConfigurationConstraint
    {
        public string ParameterName;
        public object MinValue;
        public object MaxValue;
        public bool IsRequired;
        public Type DataType;
        public string Description;

        /// <summary>
        /// Create a numeric range constraint
        /// </summary>
        public static ConfigurationConstraint CreateRangeConstraint(
            string parameterName,
            float minValue,
            float maxValue,
            bool isRequired = true,
            string description = null)
        {
            return new ConfigurationConstraint
            {
                ParameterName = parameterName,
                MinValue = minValue,
                MaxValue = maxValue,
                IsRequired = isRequired,
                DataType = typeof(float),
                Description = description ?? $"{parameterName} must be between {minValue} and {maxValue}"
            };
        }
    }

    /// <summary>
    /// Parameter validator definition
    /// </summary>
    [Serializable]
    public class ParameterValidator
    {
        public Func<object, ParameterValidationResult> ValidatorFunction;
        public string Description;

        /// <summary>
        /// Create a simple validator
        /// </summary>
        public static ParameterValidator Create(
            Func<object, ParameterValidationResult> validatorFunction,
            string description)
        {
            return new ParameterValidator
            {
                ValidatorFunction = validatorFunction,
                Description = description
            };
        }
    }

    /// <summary>
    /// Custom validation rule
    /// </summary>
    [Serializable]
    public class ValidationRule
    {
        public string Name;
        public Func<CostConfigurationProfile, ParameterValidationResult> ValidationFunction;
        public ValidationSeverity Severity = ValidationSeverity.Error;
        public string Description;

        /// <summary>
        /// Create a validation rule
        /// </summary>
        public static ValidationRule Create(
            string name,
            Func<CostConfigurationProfile, ParameterValidationResult> validationFunction,
            ValidationSeverity severity = ValidationSeverity.Error,
            string description = null)
        {
            return new ValidationRule
            {
                Name = name,
                ValidationFunction = validationFunction,
                Severity = severity,
                Description = description ?? name
            };
        }
    }

    /// <summary>
    /// Parameter validation result
    /// </summary>
    [Serializable]
    public struct ParameterValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;

        /// <summary>
        /// Create a successful validation result
        /// </summary>
        public static ParameterValidationResult Success()
        {
            return new ParameterValidationResult
            {
                IsValid = true,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Create a failed validation result
        /// </summary>
        public static ParameterValidationResult Failure(string errorMessage)
        {
            return new ParameterValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Comprehensive validation result
    /// </summary>
    [Serializable]
    public struct ValidationResult
    {
        public bool Success;
        public string ProfileName;
        public DateTime StartTime;
        public double ExecutionTime;
        public List<string> ValidationErrors;
        public List<string> ValidationWarnings;
        public string Message;

        /// <summary>
        /// Create an empty validation result
        /// </summary>
        public static ValidationResult Create(string profileName)
        {
            return new ValidationResult
            {
                ProfileName = profileName,
                StartTime = DateTime.Now,
                ValidationErrors = new List<string>(),
                ValidationWarnings = new List<string>()
            };
        }

        /// <summary>
        /// Has any issues (errors or warnings)
        /// </summary>
        public bool HasIssues => (ValidationErrors?.Count ?? 0) > 0 || (ValidationWarnings?.Count ?? 0) > 0;
    }

    /// <summary>
    /// Validation failure information
    /// </summary>
    [Serializable]
    public struct ValidationFailure
    {
        public string ParameterName;
        public string ErrorMessage;
        public object FailedValue;
        public DateTime Timestamp;

        /// <summary>
        /// Create a validation failure
        /// </summary>
        public static ValidationFailure Create(string parameterName, string errorMessage, object failedValue)
        {
            return new ValidationFailure
            {
                ParameterName = parameterName,
                ErrorMessage = errorMessage,
                FailedValue = failedValue,
                Timestamp = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Validation statistics tracking
    /// </summary>
    [Serializable]
    public class ValidationStatistics
    {
        public int TotalValidations = 0;
        public int SuccessfulValidations = 0;
        public int FailedValidations = 0;
        public int TotalErrors = 0;
        public int TotalWarnings = 0;
        public int ValidationExceptions = 0;
        public int ParameterValidationsPassed = 0;
        public int ParameterValidationsFailed = 0;
        public int ParameterValidationExceptions = 0;
        public DateTime LastValidation = DateTime.MinValue;

        /// <summary>
        /// Success rate
        /// </summary>
        public float SuccessRate => TotalValidations > 0
            ? (float)SuccessfulValidations / TotalValidations
            : 0f;

        /// <summary>
        /// Parameter success rate
        /// </summary>
        public float ParameterSuccessRate => (ParameterValidationsPassed + ParameterValidationsFailed) > 0
            ? (float)ParameterValidationsPassed / (ParameterValidationsPassed + ParameterValidationsFailed)
            : 0f;

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void Reset()
        {
            TotalValidations = 0;
            SuccessfulValidations = 0;
            FailedValidations = 0;
            TotalErrors = 0;
            TotalWarnings = 0;
            ValidationExceptions = 0;
            ParameterValidationsPassed = 0;
            ParameterValidationsFailed = 0;
            ParameterValidationExceptions = 0;
            LastValidation = DateTime.MinValue;
        }
    }

}

