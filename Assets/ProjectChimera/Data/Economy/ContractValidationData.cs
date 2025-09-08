using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Contract completion validation result
    /// </summary>
    [System.Serializable]
    public class ContractCompletionValidation
    {
        public string ContractId = "";
        public bool IsValid = false;
        public bool QuantityMet = false;
        public bool QualityMet = false;
        public bool DeadlineMet = false;
        public List<string> ValidationErrors = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public QualityGrade AchievedQuality = QualityGrade.Standard;
        public float CompletionPercentage = 0f;
        public DateTime ValidationDate = DateTime.Now;
        public string Reason = ""; // Reason for validation result
    }

    /// <summary>
    /// Advanced contract validator for complex validation logic
    /// </summary>
    [System.Serializable]
    public class AdvancedContractValidator
    {
        public string ValidatorId = "";
        public string ValidatorName = "";
        public List<string> ValidationRules = new List<string>();
        public Dictionary<string, object> ValidationParameters = new Dictionary<string, object>();
        public bool IsEnabled = true;
        public int Priority = 0;
        public string ValidatorType = ""; // e.g., "Quality", "Quantity", "Timing"
    }

    /// <summary>
    /// Contract specific validation check
    /// </summary>
    [System.Serializable]
    public class ContractSpecificCheck
    {
        public string CheckId = "";
        public string CheckName = "";
        public string CheckType = ""; // e.g., "Quality", "Quantity", "Timing", "Custom"
        public bool IsRequired = true;
        public Dictionary<string, object> CheckParameters = new Dictionary<string, object>();
        public bool CheckPassed = false;
        public bool Passed = false; // Alias for CheckPassed
        public string CheckResult = "";
        public DateTime CheckDate = DateTime.Now;
        public string CheckDetails = "";
        public string Details { get => CheckDetails; set => CheckDetails = value; } // Alias for CheckDetails
    }

    /// <summary>
    /// Advanced contract validation for complex business logic
    /// </summary>
    [System.Serializable]
    public class AdvancedContractValidation
    {
        public string ValidationId = "";
        public string ContractId = "";
        public bool IsValid = false;
        public List<string> ValidationErrors = new List<string>();
        public List<string> ValidationWarnings = new List<string>();
        public Dictionary<string, bool> ValidationChecks = new Dictionary<string, bool>();
        public float ValidationScore = 0f;
        public DateTime ValidationTimestamp = DateTime.Now;
        public DateTime ValidatorDate = DateTime.Now; // When validation was performed
        public DateTime ValidationDate = DateTime.Now; // Alias for ValidationTimestamp
        public string ValidationType = ""; // e.g., "Comprehensive", "Quick", "Custom"
        public string FailureReason = ""; // Primary reason for validation failure
        public List<AdvancedContractValidator> ValidatorsUsed = new List<AdvancedContractValidator>();
        public List<ContractSpecificCheck> ContractSpecificChecks = new List<ContractSpecificCheck>(); // Contract-specific validation checks
        public BatchValidationResult BatchValidation = new BatchValidationResult(); // Batch validation result
        public string QualityAssessment = ""; // Quality assessment text
        public bool MeetsQualityStandards = false; // Whether validation meets quality standards
        public Dictionary<string, object> ValidationMetadata = new Dictionary<string, object>();
        
        public AdvancedContractValidation()
        {
            ValidationId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Batch validation result for multiple contracts
    /// </summary>
    [System.Serializable]
    public class BatchValidationResult
    {
        public string BatchId = "";
        public string ContractId = ""; // Contract ID for this batch
        public int BatchSize = 0; // Number of items in batch
        public List<string> Issues = new List<string>(); // Validation issues
        public List<AdvancedContractValidation> ValidationResults = new List<AdvancedContractValidation>();
        public int TotalValidated = 0;
        public int PassedValidation = 0;
        public int FailedValidation = 0;
        public float OverallSuccessRate = 0f;
        public bool IsValid = false; // Whether the batch validation passed overall
        public DateTime BatchProcessedDate = DateTime.Now;
        public TimeSpan ProcessingTime = TimeSpan.Zero;
        public List<string> BatchErrors = new List<string>();
        public Dictionary<string, int> ValidationSummary = new Dictionary<string, int>();
        
        public BatchValidationResult()
        {
            BatchId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Batch validation request for processing multiple contracts
    /// </summary>
    [System.Serializable]
    public class BatchValidationRequest
    {
        public string RequestId = "";
        public List<string> ContractIds = new List<string>();
        public string ValidationType = "Standard";
        public bool HighPriority = false;
        public Dictionary<string, object> ValidationParameters = new Dictionary<string, object>();
        public DateTime RequestedDate = DateTime.Now;
        public string RequestedBy = "";
        public List<string> ValidatorTypes = new List<string>();
        
        public BatchValidationRequest()
        {
            RequestId = Guid.NewGuid().ToString();
        }
    }
}