// REFACTORED: Data Structures
// Extracted from PlantDataValidationEngine.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    public class ValidationRules
    {
        public Dictionary<string, FloatRange> FloatRanges = new Dictionary<string, FloatRange>();
        public Dictionary<string, string[]> StringValidValues = new Dictionary<string, string[]>();
        public List<string> RequiredFields = new List<string>();
    }

    public struct FloatRange
    {
        public float Min;
        public float Max;
    }

    public struct ValidationError
    {
        public string FieldName;
        public string ErrorMessage;
        public ValidationSeverity Severity;
        public DateTime Timestamp;
        public object OriginalValue;
        public object CorrectedValue;
    }

    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    public struct ValidationStats
    {
        public int TotalValidations;
        public int SuccessfulValidations;
        public int FailedValidations;
        public int FieldValidations;
        public int TotalErrors;
        public int IdentityValidations;
        public int StateValidations;
        public int ResourceValidations;
        public int GrowthValidations;
        public int HarvestValidations;
        public int ConsistencyValidations;
        public float TotalValidationTime;
    }

    public struct ValidationReport
    {
        public bool IsValid;
        public string ErrorMessage;
        public List<ValidationError> Errors;
        public float ValidationTime;
        public int FieldsValidated;
        public int CriticalErrors;
        public int WarningCount;
    }

    public struct ValidationResult
    {
        public string FieldName;
        public object Value;
        public bool IsValid;
        public string ErrorMessage;
        public object CorrectedValue;
        public bool WasCorrected;
    }

}
