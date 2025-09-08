using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Result of data validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string ValidationDetails { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        
        public static ValidationResult CreateSuccess(string message = "Validation successful")
        {
            return new ValidationResult
            {
                IsValid = true,
                Message = message
            };
        }
        
        public static ValidationResult CreateFailure(string message = "Validation failed")
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message,
                ErrorMessage = message
            };
        }
    }
}