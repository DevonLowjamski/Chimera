using System.Collections.Generic;

namespace ProjectChimera.CI
{
    /// <summary>
    /// BASIC: Simple quality gates for Project Chimera's development process.
    /// Focuses on essential code quality checks without complex validation systems and forbidden patterns.
    /// </summary>
    public static class QualityGates
    {
        /// <summary>
        /// Basic quality limits by system type
        /// </summary>
        public static readonly Dictionary<string, BasicQualityLimits> SystemLimits = new Dictionary<string, BasicQualityLimits>
        {
            ["Core"] = new BasicQualityLimits { MaxLines = 500, MaxMethods = 30 },
            ["Systems"] = new BasicQualityLimits { MaxLines = 400, MaxMethods = 25 },
            ["Data"] = new BasicQualityLimits { MaxLines = 200, MaxMethods = 15 },
            ["UI"] = new BasicQualityLimits { MaxLines = 300, MaxMethods = 20 },
            ["Testing"] = new BasicQualityLimits { MaxLines = 600, MaxMethods = 40 },
            ["Genetics"] = new BasicQualityLimits { MaxLines = 500, MaxMethods = 35 },
            ["Environment"] = new BasicQualityLimits { MaxLines = 400, MaxMethods = 25 }
        };

        /// <summary>
        /// Check if file passes basic quality gates
        /// </summary>
        public static QualityCheckResult CheckFileQuality(string filePath, string systemType, int lineCount, int methodCount)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check if system type is recognized
            if (!SystemLimits.ContainsKey(systemType))
            {
                warnings.Add($"Unknown system type: {systemType}");
                return new QualityCheckResult
                {
                    IsValid = true,
                    Errors = errors,
                    Warnings = warnings,
                    CanProceed = true
                };
            }

            var limits = SystemLimits[systemType];

            // Check line count
            if (lineCount > limits.MaxLines)
            {
                errors.Add($"File exceeds maximum lines ({lineCount} > {limits.MaxLines}) for {systemType} system");
            }
            else if (lineCount > limits.MaxLines * 0.8f)
            {
                warnings.Add($"File approaching line limit ({lineCount}/{limits.MaxLines}) for {systemType} system");
            }

            // Check method count
            if (methodCount > limits.MaxMethods)
            {
                errors.Add($"File exceeds maximum methods ({methodCount} > {limits.MaxMethods}) for {systemType} system");
            }
            else if (methodCount > limits.MaxMethods * 0.8f)
            {
                warnings.Add($"File approaching method limit ({methodCount}/{limits.MaxMethods}) for {systemType} system");
            }

            return new QualityCheckResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                CanProceed = true // Allow proceeding even with warnings
            };
        }

        /// <summary>
        /// Get quality limits for system type
        /// </summary>
        public static BasicQualityLimits GetLimitsForSystem(string systemType)
        {
            return SystemLimits.TryGetValue(systemType, out var limits) ? limits :
                new BasicQualityLimits { MaxLines = 300, MaxMethods = 20 }; // Default limits
        }

        /// <summary>
        /// Check if system type is valid
        /// </summary>
        public static bool IsValidSystemType(string systemType)
        {
            return SystemLimits.ContainsKey(systemType);
        }

        /// <summary>
        /// Get all supported system types
        /// </summary>
        public static List<string> GetSupportedSystemTypes()
        {
            return new List<string>(SystemLimits.Keys);
        }

        /// <summary>
        /// Get quality gate statistics
        /// </summary>
        public static QualityGateStats GetStats()
        {
            return new QualityGateStats
            {
                SupportedSystemTypes = SystemLimits.Count,
                TotalLimitsDefined = SystemLimits.Count * 2, // lines + methods
                HasDefaultLimits = true
            };
        }
    }

    /// <summary>
    /// Basic quality limits
    /// </summary>
    [System.Serializable]
    public struct BasicQualityLimits
    {
        public int MaxLines;
        public int MaxMethods;
    }

    /// <summary>
    /// Quality check result
    /// </summary>
    [System.Serializable]
    public struct QualityCheckResult
    {
        public bool IsValid;
        public List<string> Errors;
        public List<string> Warnings;
        public bool CanProceed;
    }

    /// <summary>
    /// Quality gate statistics
    /// </summary>
    [System.Serializable]
    public struct QualityGateStats
    {
        public int SupportedSystemTypes;
        public int TotalLimitsDefined;
        public bool HasDefaultLimits;
    }
}
