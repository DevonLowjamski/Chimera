using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectChimera.CI
{
    /// <summary>
    /// Quality Gates implementation for Project Chimera CI/CD pipeline
    /// Enforces architectural patterns and prevents anti-patterns as per Phase 0-1 requirements
    /// </summary>
    public static class QualityGates
    {
        /// <summary>
        /// System-specific complexity and quality limits
        /// Based on architectural patterns and system responsibilities
        /// </summary>
        public static readonly Dictionary<string, QualityLimits> SystemLimits = new Dictionary<string, QualityLimits>
        {
            // Core systems - highest complexity allowed due to foundational nature
            ["Core"] = new QualityLimits
            {
                MaxLines = 800,
                MaxMethods = 50,
                MaxCyclomaticComplexity = 15,
                MaxNestingDepth = 5,
                RequireDocumentation = true,
                AllowComplexConstructors = true
            },
            
            // Systems - moderate complexity for business logic
            ["Systems"] = new QualityLimits
            {
                MaxLines = 600,
                MaxMethods = 40,
                MaxCyclomaticComplexity = 12,
                MaxNestingDepth = 4,
                RequireDocumentation = true,
                AllowComplexConstructors = false
            },
            
            // Data classes - simple structure, low complexity
            ["Data"] = new QualityLimits
            {
                MaxLines = 400,
                MaxMethods = 30,
                MaxCyclomaticComplexity = 8,
                MaxNestingDepth = 3,
                RequireDocumentation = false,
                AllowComplexConstructors = false
            },
            
            // UI systems - moderate complexity, event-driven
            ["UI"] = new QualityLimits
            {
                MaxLines = 500,
                MaxMethods = 35,
                MaxCyclomaticComplexity = 10,
                MaxNestingDepth = 4,
                RequireDocumentation = true,
                AllowComplexConstructors = false
            },
            
            // Testing - higher limits for comprehensive test scenarios
            ["Testing"] = new QualityLimits
            {
                MaxLines = 1000,
                MaxMethods = 100,
                MaxCyclomaticComplexity = 20,
                MaxNestingDepth = 6,
                RequireDocumentation = false,
                AllowComplexConstructors = true
            },
            
            // Specialized high-complexity systems
            ["Genetics"] = new QualityLimits
            {
                MaxLines = 900,  // Genetics calculations can be complex
                MaxMethods = 45,
                MaxCyclomaticComplexity = 18,
                MaxNestingDepth = 5,
                RequireDocumentation = true,
                AllowComplexConstructors = true
            },
            
            ["Environment"] = new QualityLimits
            {
                MaxLines = 700,  // Environmental simulations
                MaxMethods = 42,
                MaxCyclomaticComplexity = 14,
                MaxNestingDepth = 4,
                RequireDocumentation = true,
                AllowComplexConstructors = false
            }
        };

        /// <summary>
        /// Forbidden patterns that must never appear in the codebase
        /// Based on Phase 0 anti-pattern elimination requirements
        /// </summary>
        public static readonly ForbiddenPattern[] ForbiddenPatterns = new ForbiddenPattern[]
        {
            // Phase 0: Eliminate FindObjectOfType
            new ForbiddenPattern
            {
                Pattern = @"FindObjectOfType\s*<",
                Message = "FindObjectOfType detected - use ServiceContainer dependency injection instead",
                Severity = ViolationSeverity.Critical,
                Replacement = "ServiceContainerFactory.Instance.Resolve<T>()"
            },
            
            new ForbiddenPattern
            {
                Pattern = @"FindObjectsOfType\s*<",
                Message = "FindObjectsOfType detected - use ServiceContainer.GetAll<T>() instead",
                Severity = ViolationSeverity.Critical,
                Replacement = "ServiceContainerFactory.Instance.GetAll<T>()"
            },

            // Phase 0: Eliminate Resources.Load
            new ForbiddenPattern
            {
                Pattern = @"Resources\.Load\s*<",
                Message = "Resources.Load detected - use Addressables system instead",
                Severity = ViolationSeverity.Critical,
                Replacement = "await Addressables.LoadAssetAsync<T>(key)"
            },

            // Phase 1: Eliminate raw Debug.Log
            new ForbiddenPattern
            {
                Pattern = @"Debug\.(Log|LogError|LogWarning|LogException)\s*\(",
                Message = "Raw Debug.Log detected - use ChimeraLogger with proper categorization",
                Severity = ViolationSeverity.Major,
                Replacement = "ChimeraLogger.Log() / LogError() / LogWarning()"
            },

            // Phase 0: Eliminate GameObject.Find
            new ForbiddenPattern
            {
                Pattern = @"GameObject\.Find\s*\(",
                Message = "GameObject.Find detected - use ServiceContainer or explicit references",
                Severity = ViolationSeverity.Critical,
                Replacement = "Constructor injection or ServiceContainer.Resolve<T>()"
            },

            // Phase 0: Eliminate legacy ServiceLocator
            new ForbiddenPattern
            {
                Pattern = @"ServiceLocator\.",
                Message = "Legacy ServiceLocator usage detected - migrate to unified ServiceContainer",
                Severity = ViolationSeverity.Critical,
                Replacement = "ServiceContainerFactory.Instance"
            },

            // Performance: Eliminate per-frame string concatenation
            new ForbiddenPattern
            {
                Pattern = @"Update\s*\(\s*\)[\s\S]*?"".*""\s*\+",
                Message = "String concatenation in Update() method - use StringBuilder or cache strings",
                Severity = ViolationSeverity.Major,
                Replacement = "StringBuilder or cached string variables"
            },

            // Performance: Eliminate LINQ in Update
            new ForbiddenPattern
            {
                Pattern = @"Update\s*\(\s*\)[\s\S]*?\.(Where|Select|First|Any|Count)\s*\(",
                Message = "LINQ usage in Update() method - cache results or use for loops",
                Severity = ViolationSeverity.Major,
                Replacement = "Cached collections or traditional loops"
            },

            // Reflection safety
            new ForbiddenPattern
            {
                Pattern = @"typeof\s*\(\s*\w+\s*\)\.GetField\s*\(",
                Message = "Direct reflection field access - use ServiceContainer or explicit properties",
                Severity = ViolationSeverity.Major,
                Replacement = "Strongly-typed interfaces and properties"
            }
        };

        /// <summary>
        /// Analyze a C# source file for quality violations
        /// </summary>
        public static QualityReport AnalyzeFile(string filePath, string content)
        {
            var report = new QualityReport
            {
                FilePath = filePath,
                SystemType = GetSystemType(filePath),
                Violations = new List<QualityViolation>()
            };

            var limits = GetLimitsForFile(filePath);
            
            // Analyze complexity metrics
            var metrics = CalculateComplexityMetrics(content);
            report.Metrics = metrics;

            // Check complexity violations
            CheckComplexityViolations(report, metrics, limits);
            
            // Check forbidden patterns
            CheckForbiddenPatterns(report, content);
            
            // Check documentation requirements
            if (limits.RequireDocumentation)
            {
                CheckDocumentationRequirements(report, content);
            }

            return report;
        }

        /// <summary>
        /// Determine system type from file path for appropriate limits
        /// </summary>
        private static string GetSystemType(string filePath)
        {
            var normalizedPath = filePath.Replace('\\', '/');
            
            if (normalizedPath.Contains("/Core/"))
                return "Core";
            if (normalizedPath.Contains("/Systems/Genetics/"))
                return "Genetics";
            if (normalizedPath.Contains("/Systems/Environment/"))
                return "Environment";
            if (normalizedPath.Contains("/Systems/"))
                return "Systems";
            if (normalizedPath.Contains("/Data/"))
                return "Data";
            if (normalizedPath.Contains("/UI/"))
                return "UI";
            if (normalizedPath.Contains("/Testing/") || normalizedPath.Contains("/Tests/"))
                return "Testing";
                
            return "Systems"; // Default
        }

        /// <summary>
        /// Get quality limits for a specific file
        /// </summary>
        private static QualityLimits GetLimitsForFile(string filePath)
        {
            var systemType = GetSystemType(filePath);
            return SystemLimits.TryGetValue(systemType, out var limits) ? limits : SystemLimits["Systems"];
        }

        /// <summary>
        /// Calculate complexity metrics for source code
        /// </summary>
        private static ComplexityMetrics CalculateComplexityMetrics(string content)
        {
            var lines = content.Split('\n');
            var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//")).ToArray();
            
            // Count methods (simplified pattern matching)
            var methodPattern = @"(public|private|protected|internal)\s+.*\s+\w+\s*\([^)]*\)\s*{";
            var methods = Regex.Matches(content, methodPattern, RegexOptions.Multiline).Count;
            
            // Calculate cyclomatic complexity (simplified)
            var complexityKeywords = new[] { "if ", "while ", "for ", "foreach ", "switch ", "catch ", "case ", "&&", "||" };
            var cyclomaticComplexity = complexityKeywords.Sum(keyword => 
                Regex.Matches(content, Regex.Escape(keyword), RegexOptions.IgnoreCase).Count);
            
            // Calculate nesting depth (simplified)
            var maxNesting = CalculateMaxNestingDepth(content);
            
            return new ComplexityMetrics
            {
                LineCount = nonEmptyLines.Length,
                MethodCount = methods,
                CyclomaticComplexity = cyclomaticComplexity,
                MaxNestingDepth = maxNesting
            };
        }

        /// <summary>
        /// Calculate maximum nesting depth in the code
        /// </summary>
        private static int CalculateMaxNestingDepth(string content)
        {
            int maxDepth = 0;
            int currentDepth = 0;
            
            foreach (char c in content)
            {
                if (c == '{')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}')
                {
                    currentDepth--;
                }
            }
            
            return maxDepth;
        }

        /// <summary>
        /// Check for complexity limit violations
        /// </summary>
        private static void CheckComplexityViolations(QualityReport report, ComplexityMetrics metrics, QualityLimits limits)
        {
            if (metrics.LineCount > limits.MaxLines)
            {
                report.Violations.Add(new QualityViolation
                {
                    Type = ViolationType.Complexity,
                    Severity = ViolationSeverity.Major,
                    Message = $"File has {metrics.LineCount} lines (limit: {limits.MaxLines} for {report.SystemType})",
                    Suggestion = "Consider breaking this class into smaller, more focused components"
                });
            }
            
            if (metrics.MethodCount > limits.MaxMethods)
            {
                report.Violations.Add(new QualityViolation
                {
                    Type = ViolationType.Complexity,
                    Severity = ViolationSeverity.Major,
                    Message = $"File has {metrics.MethodCount} methods (limit: {limits.MaxMethods} for {report.SystemType})",
                    Suggestion = "Extract related methods into separate classes or use composition"
                });
            }
            
            if (metrics.CyclomaticComplexity > limits.MaxCyclomaticComplexity)
            {
                report.Violations.Add(new QualityViolation
                {
                    Type = ViolationType.Complexity,
                    Severity = ViolationSeverity.Major,
                    Message = $"Cyclomatic complexity is {metrics.CyclomaticComplexity} (limit: {limits.MaxCyclomaticComplexity} for {report.SystemType})",
                    Suggestion = "Simplify conditional logic or extract methods to reduce complexity"
                });
            }
            
            if (metrics.MaxNestingDepth > limits.MaxNestingDepth)
            {
                report.Violations.Add(new QualityViolation
                {
                    Type = ViolationType.Complexity,
                    Severity = ViolationSeverity.Minor,
                    Message = $"Maximum nesting depth is {metrics.MaxNestingDepth} (limit: {limits.MaxNestingDepth} for {report.SystemType})",
                    Suggestion = "Reduce nesting by using early returns or extracting methods"
                });
            }
        }

        /// <summary>
        /// Check for forbidden pattern violations
        /// </summary>
        private static void CheckForbiddenPatterns(QualityReport report, string content)
        {
            foreach (var forbiddenPattern in ForbiddenPatterns)
            {
                var matches = Regex.Matches(content, forbiddenPattern.Pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                    
                    report.Violations.Add(new QualityViolation
                    {
                        Type = ViolationType.ForbiddenPattern,
                        Severity = forbiddenPattern.Severity,
                        LineNumber = lineNumber,
                        Message = forbiddenPattern.Message,
                        Suggestion = $"Replace with: {forbiddenPattern.Replacement}",
                        Code = match.Value
                    });
                }
            }
        }

        /// <summary>
        /// Check documentation requirements for public APIs
        /// </summary>
        private static void CheckDocumentationRequirements(QualityReport report, string content)
        {
            // Check for public classes/interfaces without documentation
            var publicDeclarations = new[]
            {
                @"public\s+class\s+(\w+)",
                @"public\s+interface\s+(\w+)",
                @"public\s+enum\s+(\w+)",
                @"public\s+struct\s+(\w+)"
            };

            foreach (var pattern in publicDeclarations)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.Multiline);
                
                foreach (Match match in matches)
                {
                    var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                    
                    // Check if there's documentation before this declaration
                    var lines = content.Split('\n');
                    var declarationLineIndex = lineNumber - 1;
                    
                    bool hasDocumentation = false;
                    for (int i = declarationLineIndex - 1; i >= 0; i--)
                    {
                        var line = lines[i].Trim();
                        if (line.StartsWith("/// <summary>"))
                        {
                            hasDocumentation = true;
                            break;
                        }
                        if (!line.StartsWith("///") && !line.StartsWith("[") && !string.IsNullOrWhiteSpace(line))
                        {
                            break; // Hit non-documentation/attribute line
                        }
                    }
                    
                    if (!hasDocumentation)
                    {
                        report.Violations.Add(new QualityViolation
                        {
                            Type = ViolationType.Documentation,
                            Severity = ViolationSeverity.Minor,
                            LineNumber = lineNumber,
                            Message = $"Public API '{match.Groups[1].Value}' missing XML documentation",
                            Suggestion = "Add /// <summary> documentation for public APIs"
                        });
                    }
                }
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// Quality limits for different system types
    /// </summary>
    public class QualityLimits
    {
        public int MaxLines { get; set; }
        public int MaxMethods { get; set; }
        public int MaxCyclomaticComplexity { get; set; }
        public int MaxNestingDepth { get; set; }
        public bool RequireDocumentation { get; set; }
        public bool AllowComplexConstructors { get; set; }
    }

    /// <summary>
    /// Forbidden pattern definition
    /// </summary>
    public class ForbiddenPattern
    {
        public string Pattern { get; set; }
        public string Message { get; set; }
        public ViolationSeverity Severity { get; set; }
        public string Replacement { get; set; }
    }

    /// <summary>
    /// Quality analysis report for a single file
    /// </summary>
    public class QualityReport
    {
        public string FilePath { get; set; }
        public string SystemType { get; set; }
        public ComplexityMetrics Metrics { get; set; }
        public List<QualityViolation> Violations { get; set; }
        
        public bool HasCriticalViolations => Violations.Any(v => v.Severity == ViolationSeverity.Critical);
        public bool HasMajorViolations => Violations.Any(v => v.Severity == ViolationSeverity.Major);
    }

    /// <summary>
    /// Complexity metrics for source code
    /// </summary>
    public class ComplexityMetrics
    {
        public int LineCount { get; set; }
        public int MethodCount { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int MaxNestingDepth { get; set; }
    }

    /// <summary>
    /// Individual quality violation
    /// </summary>
    public class QualityViolation
    {
        public ViolationType Type { get; set; }
        public ViolationSeverity Severity { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// Types of quality violations
    /// </summary>
    public enum ViolationType
    {
        Complexity,
        ForbiddenPattern,
        Documentation,
        Architecture
    }

    /// <summary>
    /// Severity levels for violations
    /// </summary>
    public enum ViolationSeverity
    {
        Critical,  // Build should fail
        Major,     // Should be addressed soon
        Minor      // Nice to have
    }

    #endregion
}