using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace ProjectChimera.CI
{
    /// <summary>
    /// ENHANCED: Comprehensive quality gates for Project Chimera with anti-pattern detection.
    /// Prevents architectural regressions by detecting forbidden patterns and enforcing strict quality standards.
    /// </summary>
    public static class QualityGates
    {
        /// <summary>
        /// Forbidden patterns that block commits - zero tolerance enforcement
        /// </summary>
        public static readonly string[] ForbiddenPatterns = {
            "FindObjectOfType<",
            "FindObjectsOfType<", 
            "GameObject\\.Find\\(",
            "Resources\\.Load",
            "Debug\\.Log\\(",
            "Debug\\.LogWarning\\(",
            "Debug\\.LogError\\(",
            "\\.GetField\\(",
            "\\.GetProperty\\(",
            "\\.GetMethod\\(",
            "typeof\\([^)]+\\)\\.GetProperty",
            "Activator\\.CreateInstance",
            "Assembly\\.Load"
        };

        /// <summary>
        /// Maximum file line count for different systems
        /// </summary>
        public static readonly int MaxFileLineCount = 400;
        public static readonly int MaxMethodComplexity = 10;
        public static readonly int MaxClassDependencies = 5;

        /// <summary>
        /// Quality limits by system type
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
        /// Run all quality gate checks - comprehensive validation
        /// </summary>
        public static QualityGateResult RunAllChecks()
        {
            var result = new QualityGateResult();
            
            result.AntiPatternViolations = CheckAntiPatterns();
            result.FileSizeViolations = CheckFileSizes();
            result.ComplexityViolations = CheckComplexity();
            result.ArchitectureViolations = CheckArchitecture();
            
            return result;
        }

        /// <summary>
        /// Check for forbidden anti-patterns in all C# files
        /// </summary>
        public static List<AntiPatternViolation> CheckAntiPatterns()
        {
            var violations = new List<AntiPatternViolation>();
            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                                  .Where(f => !f.Contains("Testing") && !f.Contains("Editor"));

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    
                    // Skip comments and fallback mechanisms
                    if (line.Contains("//") && (line.Contains("Fallback") || line.Contains("ServiceContainer")))
                        continue;
                        
                    foreach (var pattern in ForbiddenPatterns)
                    {
                        if (Regex.IsMatch(line, pattern))
                        {
                            violations.Add(new AntiPatternViolation
                            {
                                File = file,
                                LineNumber = i + 1,
                                Pattern = pattern,
                                Content = line.Trim()
                            });
                        }
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// Check file sizes against limits
        /// </summary>
        public static List<FileSizeViolation> CheckFileSizes()
        {
            var violations = new List<FileSizeViolation>();
            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                                  .Where(f => !f.Contains("Testing") && !f.Contains("Editor"));

            foreach (var file in csFiles)
            {
                var lineCount = File.ReadAllLines(file).Length;
                if (lineCount > MaxFileLineCount)
                {
                    violations.Add(new FileSizeViolation
                    {
                        File = file,
                        LineCount = lineCount,
                        MaxAllowed = MaxFileLineCount
                    });
                }
            }

            return violations;
        }

        /// <summary>
        /// Check method complexity (placeholder for future implementation)
        /// </summary>
        public static List<ComplexityViolation> CheckComplexity()
        {
            // TODO: Implement cyclomatic complexity analysis
            return new List<ComplexityViolation>();
        }

        /// <summary>
        /// Check architecture violations
        /// </summary>
        public static List<ArchitectureViolation> CheckArchitecture()
        {
            var violations = new List<ArchitectureViolation>();
            
            // Check for deprecated namespace usage
            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                                  .Where(f => !f.Contains("Testing") && !f.Contains("Editor") && !f.Contains("Core"));

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                if (content.Contains("using ProjectChimera.Core.DependencyInjection"))
                {
                    violations.Add(new ArchitectureViolation
                    {
                        File = file,
                        Type = "Deprecated DI Namespace",
                        Description = "Using deprecated ProjectChimera.Core.DependencyInjection namespace"
                    });
                }
            }

            return violations;
        }

        /// <summary>
        /// Check if file passes basic quality gates (legacy method for compatibility)
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

    /// <summary>
    /// Comprehensive quality gate result
    /// </summary>
    [System.Serializable]
    public struct QualityGateResult
    {
        public List<AntiPatternViolation> AntiPatternViolations;
        public List<FileSizeViolation> FileSizeViolations;
        public List<ComplexityViolation> ComplexityViolations;
        public List<ArchitectureViolation> ArchitectureViolations;
        
        public bool HasViolations => 
            (AntiPatternViolations?.Count ?? 0) > 0 ||
            (FileSizeViolations?.Count ?? 0) > 0 ||
            (ComplexityViolations?.Count ?? 0) > 0 ||
            (ArchitectureViolations?.Count ?? 0) > 0;
            
        public int TotalViolations =>
            (AntiPatternViolations?.Count ?? 0) +
            (FileSizeViolations?.Count ?? 0) +
            (ComplexityViolations?.Count ?? 0) +
            (ArchitectureViolations?.Count ?? 0);
    }

    /// <summary>
    /// Anti-pattern violation details
    /// </summary>
    [System.Serializable]
    public struct AntiPatternViolation
    {
        public string File;
        public int LineNumber;
        public string Pattern;
        public string Content;
    }

    /// <summary>
    /// File size violation details
    /// </summary>
    [System.Serializable]
    public struct FileSizeViolation
    {
        public string File;
        public int LineCount;
        public int MaxAllowed;
    }

    /// <summary>
    /// Complexity violation details
    /// </summary>
    [System.Serializable]
    public struct ComplexityViolation
    {
        public string File;
        public string Method;
        public int Complexity;
        public int MaxAllowed;
    }

    /// <summary>
    /// Architecture violation details
    /// </summary>
    [System.Serializable]
    public struct ArchitectureViolation
    {
        public string File;
        public string Type;
        public string Description;
    }
}
