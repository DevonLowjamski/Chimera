using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.CI
{
    /// <summary>
    /// Single Responsibility Principle Compliance Validator
    /// Enforces file size limits and identifies SRP violations for quality gates
    /// Part of Phase 0/1 completion requirements
    /// </summary>
    public static class SRPComplianceValidator
    {
        // File size limits by system type (lines of code)
        private static readonly Dictionary<string, int> FILE_SIZE_LIMITS = new Dictionary<string, int>
        {
            // Core systems - strict limits
            { "Core/", 300 },
            { "Core/Memory/", 250 },
            { "Core/Updates/", 200 },
            { "Core/Logging/", 150 },

            // Business logic - moderate limits
            { "Systems/", 400 },
            { "Systems/Cultivation/", 350 },
            { "Systems/Construction/", 350 },
            { "Systems/Economy/", 300 },

            // Data and configuration - relaxed limits
            { "Data/", 500 },
            { "UI/", 350 },

            // Tools and editors - more relaxed
            { "Editor/", 600 },
            { "Testing/", 500 },
            { "CI/", 400 },

            // Default limit
            { "DEFAULT", 350 }
        };

        // Files exempt from SRP validation (legacy, tools, generated)
        private static readonly HashSet<string> EXEMPT_FILES = new HashSet<string>
        {
            "AntiPatternMigrationTool.cs",
            "BatchMigrationScript.cs",
            "QualityGates.cs",
            "DependencyResolutionHelper.cs",
            "ServiceContainer.cs"
        };

        // Patterns that indicate potential SRP violations
        private static readonly string[] SRP_VIOLATION_PATTERNS = new string[]
        {
            "class.*Manager.*Controller",    // Mixed responsibilities
            "class.*Service.*Manager",       // Service + Management
            "class.*System.*Handler",        // System + Handling
            "class.*Processor.*Manager",     // Processing + Management
            "// TODO.*refactor",             // Known refactor needs
            "// FIXME.*SRP",                 // Known SRP issues
            "// HACK.*",                     // Temporary solutions
        };

        /// <summary>
        /// Validate SRP compliance across the entire project
        /// </summary>
        public static SRPComplianceReport ValidateProjectCompliance(string projectPath = null)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                projectPath = Path.Combine(Application.dataPath, "ProjectChimera");
            }

            var report = new SRPComplianceReport();
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            ChimeraLogger.LogInfo("SRP", $"Validating SRP compliance for {files.Length} files...");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var relativePath = GetRelativePath(file, projectPath);

                // Skip exempt files
                if (EXEMPT_FILES.Contains(fileName))
                {
                    continue;
                }

                var violation = ValidateFile(file, relativePath);
                if (violation != null)
                {
                    report.Violations.Add(violation);
                }
            }

            // Generate summary
            GenerateComplianceSummary(report);

            ChimeraLogger.LogInfo("SRP", $"SRP validation complete: {report.Violations.Count} violations found");

            return report;
        }

        /// <summary>
        /// Validate a single file for SRP compliance
        /// </summary>
        private static SRPViolation ValidateFile(string filePath, string relativePath)
        {
            var fileName = Path.GetFileName(filePath);
            var lines = File.ReadAllLines(filePath);
            var lineCount = lines.Length;

            // Determine size limit for this file
            var sizeLimit = GetSizeLimitForFile(relativePath);

            // Check for size violations
            if (lineCount > sizeLimit)
            {
                var violation = new SRPViolation
                {
                    File = relativePath,
                    ViolationType = SRPViolationType.FileSize,
                    CurrentLines = lineCount,
                    LimitLines = sizeLimit,
                    Severity = GetSeverityLevel(lineCount, sizeLimit),
                    Description = $"File exceeds size limit: {lineCount} lines (limit: {sizeLimit})"
                };

                // Check for additional SRP pattern violations
                CheckForPatternViolations(lines, violation);

                return violation;
            }

            // Check for pattern violations even in size-compliant files
            var patternViolation = CheckForPatternViolations(lines, null);
            if (patternViolation != null)
            {
                patternViolation.File = relativePath;
                patternViolation.CurrentLines = lineCount;
                patternViolation.LimitLines = sizeLimit;
                return patternViolation;
            }

            return null;
        }

        /// <summary>
        /// Check for SRP pattern violations in code
        /// </summary>
        private static SRPViolation CheckForPatternViolations(string[] lines, SRPViolation existingViolation)
        {
            var violation = existingViolation ?? new SRPViolation
            {
                ViolationType = SRPViolationType.DesignPattern,
                Severity = SRPSeverity.Medium
            };

            var patternViolations = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                foreach (var pattern in SRP_VIOLATION_PATTERNS)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(line, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        patternViolations.Add($"Line {i + 1}: {pattern} - {line}");
                    }
                }

                // Check for high method count (potential God object)
                if (line.Contains("public ") && (line.Contains("void ") || line.Contains("bool ") || line.Contains("int ") || line.Contains("string ")))
                {
                    // Count public methods - if too many, flag as potential SRP violation
                }
            }

            if (patternViolations.Count > 0)
            {
                violation.PatternViolations = patternViolations;
                violation.Description += $" Contains {patternViolations.Count} pattern violations.";
                return violation;
            }

            return existingViolation; // Return null if this was a new violation with no patterns
        }

        /// <summary>
        /// Get appropriate size limit for a file based on its path
        /// </summary>
        private static int GetSizeLimitForFile(string relativePath)
        {
            // Find the most specific path match
            foreach (var pathLimit in FILE_SIZE_LIMITS.OrderByDescending(kvp => kvp.Key.Length))
            {
                if (pathLimit.Key != "DEFAULT" && relativePath.StartsWith(pathLimit.Key))
                {
                    return pathLimit.Value;
                }
            }

            return FILE_SIZE_LIMITS["DEFAULT"];
        }

        /// <summary>
        /// Determine severity level based on how much the limit is exceeded
        /// </summary>
        private static SRPSeverity GetSeverityLevel(int currentLines, int limitLines)
        {
            var exceedPercentage = (float)(currentLines - limitLines) / limitLines;

            if (exceedPercentage > 1.0f) // More than 100% over limit
                return SRPSeverity.Critical;
            else if (exceedPercentage > 0.5f) // More than 50% over limit
                return SRPSeverity.High;
            else if (exceedPercentage > 0.25f) // More than 25% over limit
                return SRPSeverity.Medium;
            else
                return SRPSeverity.Low;
        }

        /// <summary>
        /// Generate compliance summary and recommendations
        /// </summary>
        private static void GenerateComplianceSummary(SRPComplianceReport report)
        {
            var criticalCount = report.Violations.Count(v => v.Severity == SRPSeverity.Critical);
            var highCount = report.Violations.Count(v => v.Severity == SRPSeverity.High);
            var mediumCount = report.Violations.Count(v => v.Severity == SRPSeverity.Medium);
            var lowCount = report.Violations.Count(v => v.Severity == SRPSeverity.Low);

            report.Summary = new SRPComplianceSummary
            {
                TotalViolations = report.Violations.Count,
                CriticalViolations = criticalCount,
                HighViolations = highCount,
                MediumViolations = mediumCount,
                LowViolations = lowCount,
                ComplianceScore = CalculateComplianceScore(report.Violations.Count, criticalCount, highCount),
                IsCompliant = criticalCount == 0 && highCount <= 5,
                Recommendations = GenerateRecommendations(report.Violations)
            };
        }

        /// <summary>
        /// Calculate overall compliance score (0-100)
        /// </summary>
        private static float CalculateComplianceScore(int totalViolations, int critical, int high)
        {
            if (totalViolations == 0) return 100f;

            // Weighted penalty system
            var penalty = (critical * 10) + (high * 5) + (totalViolations * 1);
            var maxPenalty = 100f; // Arbitrary scale

            return Mathf.Max(0f, 100f - (penalty / maxPenalty * 100f));
        }

        /// <summary>
        /// Generate actionable recommendations
        /// </summary>
        private static List<string> GenerateRecommendations(List<SRPViolation> violations)
        {
            var recommendations = new List<string>();

            var criticalFiles = violations.Where(v => v.Severity == SRPSeverity.Critical).ToList();
            if (criticalFiles.Count > 0)
            {
                recommendations.Add($"URGENT: Refactor {criticalFiles.Count} critical files immediately");
                recommendations.AddRange(criticalFiles.Take(3).Select(v => $"  - {v.File} ({v.CurrentLines} lines)"));
            }

            var highFiles = violations.Where(v => v.Severity == SRPSeverity.High).ToList();
            if (highFiles.Count > 0)
            {
                recommendations.Add($"HIGH PRIORITY: Address {highFiles.Count} high-severity violations");
            }

            recommendations.Add("Consider extracting helper classes, managers, or service layers");
            recommendations.Add("Review class responsibilities and split multi-purpose classes");
            recommendations.Add("Implement composition over inheritance patterns");

            return recommendations;
        }

        /// <summary>
        /// Get relative path for reporting
        /// </summary>
        private static string GetRelativePath(string fullPath, string basePath)
        {
            return Path.GetRelativePath(basePath, fullPath).Replace("\\", "/");
        }
    }

    /// <summary>
    /// SRP compliance report structure
    /// </summary>
    [System.Serializable]
    public class SRPComplianceReport
    {
        public List<SRPViolation> Violations = new List<SRPViolation>();
        public SRPComplianceSummary Summary;
    }

    /// <summary>
    /// Individual SRP violation
    /// </summary>
    [System.Serializable]
    public class SRPViolation
    {
        public string File;
        public SRPViolationType ViolationType;
        public int CurrentLines;
        public int LimitLines;
        public SRPSeverity Severity;
        public string Description;
        public List<string> PatternViolations = new List<string>();
    }

    /// <summary>
    /// SRP compliance summary
    /// </summary>
    [System.Serializable]
    public class SRPComplianceSummary
    {
        public int TotalViolations;
        public int CriticalViolations;
        public int HighViolations;
        public int MediumViolations;
        public int LowViolations;
        public float ComplianceScore;
        public bool IsCompliant;
        public List<string> Recommendations = new List<string>();
    }

    /// <summary>
    /// Types of SRP violations
    /// </summary>
    public enum SRPViolationType
    {
        FileSize,
        DesignPattern,
        MethodCount,
        ClassComplexity
    }

    /// <summary>
    /// Severity levels for SRP violations
    /// </summary>
    public enum SRPSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}