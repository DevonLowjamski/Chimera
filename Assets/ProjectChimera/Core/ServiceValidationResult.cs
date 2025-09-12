using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Result of service validation operations
    /// </summary>
    [System.Serializable]
    public class ServiceValidationResult
    {
        public string ServiceName { get; set; }
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public bool IsValid { get; set; }
        public bool IsCritical { get; set; }
        public bool IsNullImplementation { get; set; }
        public string ValidationMessage { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime ValidationTime { get; set; }
        public float ValidationDuration { get; set; } // milliseconds

        /// <summary>
        /// Create a successful validation result
        /// </summary>
        public static ServiceValidationResult Success(string serviceName, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceValidationResult
            {
                ServiceName = serviceName,
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsValid = true,
                ValidationMessage = "Service validation passed",
                ValidationTime = DateTime.Now
            };
        }

        /// <summary>
        /// Create a failed validation result
        /// </summary>
        public static ServiceValidationResult Failure(string serviceName, string message, bool isCritical = false)
        {
            return new ServiceValidationResult
            {
                ServiceName = serviceName,
                IsValid = false,
                IsCritical = isCritical,
                ValidationMessage = message,
                ValidationTime = DateTime.Now
            };
        }

        /// <summary>
        /// Create a null implementation result
        /// </summary>
        public static ServiceValidationResult NullImplementation(string serviceName, Type serviceType)
        {
            return new ServiceValidationResult
            {
                ServiceName = serviceName,
                ServiceType = serviceType,
                IsValid = true,
                IsNullImplementation = true,
                ValidationMessage = "Service using null implementation",
                ValidationTime = DateTime.Now
            };
        }

        /// <summary>
        /// Add an issue to the validation result
        /// </summary>
        public void AddIssue(string issue)
        {
            Issues.Add(issue);
            if (IsValid)
            {
                IsValid = false;
                ValidationMessage = "Service validation failed with issues";
            }
        }

        /// <summary>
        /// Add a warning to the validation result
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Get summary of the validation result
        /// </summary>
        public string GetSummary()
        {
            string status = IsValid ? "PASS" : "FAIL";
            if (IsNullImplementation) status += " (NULL)";
            if (IsCritical && !IsValid) status += " (CRITICAL)";
            
            return $"{ServiceName}: {status} - {ValidationMessage}";
        }
    }

    // Note: ServiceLifetime enum is defined in ServiceTypes.cs
}