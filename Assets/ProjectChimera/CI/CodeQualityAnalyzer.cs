using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.CI
{
    /// <summary>
    /// Code Quality Analyzer for Project Chimera CI/CD Pipeline
    /// Performs static analysis and enforces architectural patterns
    /// </summary>
    [InitializeOnLoad]
    public static class CodeQualityAnalyzer
    {
        private const string QUALITY_REPORT_PATH = "Assets/ProjectChimera/CI/quality-report.json";
        private static readonly string[] EXCLUDE_PATHS = { "/Testing/", "/Tests/", "/Editor/", "/CI/" };

        static CodeQualityAnalyzer()
        {
#if CHIMERA_CI_BUILD
            // Run quality analysis automatically in CI builds
            RunQualityAnalysis();
#endif
        }

        /// <summary>
        /// Main entry point for quality analysis
        /// </summary>
        [MenuItem("Project Chimera/CI/Run Quality Analysis")]
        public static void RunQualityAnalysis()
        {
            ChimeraLogger.Log("🔍 Starting Code Quality Analysis...");

            var overallReport = new QualityAnalysisReport
            {
                analysisTime = DateTime.Now,
                fileReports = new List<QualityReport>(),
                summary = new QualitySummary()
            };

            // Analyze all C# files in the project
            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsExcludedPath(f))
                .ToArray();

            ChimeraLogger.Log($"Analyzing {csFiles.Length} C# files...");

            int criticalViolations = 0;
            int majorViolations = 0;
            int minorViolations = 0;

            foreach (var filePath in csFiles)
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var report = QualityGates.AnalyzeFile(filePath, content);

                    overallReport.fileReports.Add(report);

                    // Count violations by severity
                    foreach (var violation in report.Violations)
                    {
                        switch (violation.Severity)
                        {
                            case ViolationSeverity.Critical:
                                criticalViolations++;
                                break;
                            case ViolationSeverity.Major:
                                majorViolations++;
                                break;
                            case ViolationSeverity.Minor:
                                minorViolations++;
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    ChimeraLogger.LogError($"Failed to analyze {filePath}: {e.Message}");
                }
            }

            // Compile summary
            overallReport.summary = new QualitySummary
            {
                totalFiles = csFiles.Length,
                totalViolations = criticalViolations + majorViolations + minorViolations,
                criticalViolations = criticalViolations,
                majorViolations = majorViolations,
                minorViolations = minorViolations,
                filesWithViolations = overallReport.fileReports.Count(r => r.Violations.Count > 0),
                averageComplexityScore = CalculateAverageComplexity(overallReport.fileReports),
                qualityGrade = CalculateQualityGrade(criticalViolations, majorViolations, minorViolations, csFiles.Length),
                passesQualityGates = criticalViolations == 0 && majorViolations <= csFiles.Length * 0.1 // Max 10% files with major violations
            };

            // Generate reports
            GenerateJsonReport(overallReport);
            GenerateConsoleReport(overallReport);
            GenerateUnityReport(overallReport);

            // Exit with appropriate code for CI
#if CHIMERA_CI_BUILD
            if (!overallReport.summary.passesQualityGates)
            {
                ChimeraLogger.LogError("❌ Quality gates failed - build should not proceed");
                EditorApplication.Exit(1);
            }
            else
            {
                ChimeraLogger.Log("✅ Quality gates passed");
                EditorApplication.Exit(0);
            }
#endif
        }

        /// <summary>
        /// Check if a file path should be excluded from analysis
        /// </summary>
        private static bool IsExcludedPath(string filePath)
        {
            var normalizedPath = filePath.Replace('\\', '/');
            return EXCLUDE_PATHS.Any(excludePath => normalizedPath.Contains(excludePath));
        }

        /// <summary>
        /// Calculate average complexity score across all files
        /// </summary>
        private static float CalculateAverageComplexity(List<QualityReport> reports)
        {
            if (reports.Count == 0) return 0f;

            var totalComplexity = reports.Sum(r =>
                (r.Metrics.CyclomaticComplexity * 2) +
                (r.Metrics.LineCount / 100f) +
                (r.Metrics.MethodCount / 10f));

            return totalComplexity / reports.Count;
        }

        /// <summary>
        /// Calculate overall quality grade
        /// </summary>
        private static string CalculateQualityGrade(int critical, int major, int minor, int totalFiles)
        {
            if (critical > 0) return "F";

            var violationRatio = (double)(major + minor) / totalFiles;

            if (violationRatio <= 0.05) return "A";
            if (violationRatio <= 0.1) return "B";
            if (violationRatio <= 0.2) return "C";
            if (violationRatio <= 0.35) return "D";
            return "F";
        }

        /// <summary>
        /// Generate JSON report for CI consumption
        /// </summary>
        private static void GenerateJsonReport(QualityAnalysisReport report)
        {
            try
            {
                var jsonReport = JsonUtility.ToJson(report, true);
                var directory = Path.GetDirectoryName(QUALITY_REPORT_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(QUALITY_REPORT_PATH, jsonReport);
                ChimeraLogger.Log($"Quality report saved to: {QUALITY_REPORT_PATH}");
            }
            catch (Exception e)
            {
                ChimeraLogger.LogError($"Failed to save quality report: {e.Message}");
            }
        }

        /// <summary>
        /// Generate console report for CI logs
        /// </summary>
        private static void GenerateConsoleReport(QualityAnalysisReport report)
        {
            var summary = report.summary;

            ChimeraLogger.Log("═══════════════════════════════════════");
            ChimeraLogger.Log("📊 PROJECT CHIMERA QUALITY ANALYSIS");
            ChimeraLogger.Log("═══════════════════════════════════════");
            ChimeraLogger.Log($"Analysis Time: {report.analysisTime:yyyy-MM-dd HH:mm:ss}");
            ChimeraLogger.Log($"Files Analyzed: {summary.totalFiles}");
            ChimeraLogger.Log($"Quality Grade: {summary.qualityGrade}");
            ChimeraLogger.Log($"Average Complexity: {summary.averageComplexityScore:F2}");
            ChimeraLogger.Log("");

            ChimeraLogger.Log("📋 VIOLATION SUMMARY");
            ChimeraLogger.Log("─────────────────────");
            ChimeraLogger.Log($"🔴 Critical: {summary.criticalViolations}");
            ChimeraLogger.Log($"🟡 Major:    {summary.majorViolations}");
            ChimeraLogger.Log($"🟢 Minor:    {summary.minorViolations}");
            ChimeraLogger.Log($"📁 Files with violations: {summary.filesWithViolations}/{summary.totalFiles}");
            ChimeraLogger.Log("");

            if (summary.criticalViolations > 0)
            {
                ChimeraLogger.Log("🔴 CRITICAL VIOLATIONS (MUST FIX)");
                ChimeraLogger.Log("─────────────────────────────────");
                LogViolationsByType(report, ViolationSeverity.Critical);
                ChimeraLogger.Log("");
            }

            if (summary.majorViolations > 0)
            {
                ChimeraLogger.Log("🟡 MAJOR VIOLATIONS (HIGH PRIORITY)");
                ChimeraLogger.Log("───────────────────────────────────");
                LogViolationsByType(report, ViolationSeverity.Major);
                ChimeraLogger.Log("");
            }

            var gateStatus = summary.passesQualityGates ? "✅ PASSED" : "❌ FAILED";
            ChimeraLogger.Log($"🚪 QUALITY GATES: {gateStatus}");

            if (!summary.passesQualityGates)
            {
                ChimeraLogger.Log("Quality gates criteria:");
                ChimeraLogger.Log("- Zero critical violations ✓/❌");
                ChimeraLogger.Log($"- Major violations ≤ 10% of files ({summary.majorViolations}/{summary.totalFiles * 0.1:F0}) ✓/❌");
            }

            ChimeraLogger.Log("═══════════════════════════════════════");
        }

        /// <summary>
        /// Log violations of specific severity level
        /// </summary>
        private static void LogViolationsByType(QualityAnalysisReport report, ViolationSeverity severity)
        {
            var violationsOfType = report.fileReports
                .SelectMany(fr => fr.Violations.Where(v => v.Severity == severity)
                    .Select(v => new { File = fr.FilePath, Violation = v }))
                .Take(20) // Limit output
                .ToList();

            foreach (var item in violationsOfType)
            {
                var fileName = Path.GetFileName(item.File);
                var lineInfo = item.Violation.LineNumber > 0 ? $":{item.Violation.LineNumber}" : "";
                ChimeraLogger.Log($"  • {fileName}{lineInfo} - {item.Violation.Message}");

                if (!string.IsNullOrEmpty(item.Violation.Suggestion))
                {
                    ChimeraLogger.Log($"    💡 {item.Violation.Suggestion}");
                }
            }

            if (violationsOfType.Count > 20)
            {
                ChimeraLogger.Log($"    ... and {violationsOfType.Count - 20} more violations");
            }
        }

        /// <summary>
        /// Generate Unity-specific report in the console
        /// </summary>
        private static void GenerateUnityReport(QualityAnalysisReport report)
        {
            // Create scriptable object report for Unity Inspector viewing
            var unityReport = ScriptableObject.CreateInstance<UnityQualityReport>();
            unityReport.Initialize(report);

            var reportAssetPath = "Assets/ProjectChimera/CI/QualityReport.asset";
            AssetDatabase.CreateAsset(unityReport, reportAssetPath);
            AssetDatabase.SaveAssets();

            ChimeraLogger.Log($"Unity quality report created: {reportAssetPath}");
        }

        /// <summary>
        /// Get top violations by file for focused improvements
        /// </summary>
        [MenuItem("Project Chimera/CI/Show Top Violating Files")]
        public static void ShowTopViolatingFiles()
        {
            if (!File.Exists(QUALITY_REPORT_PATH))
            {
                ChimeraLogger.LogWarning("No quality report found. Run quality analysis first.");
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(QUALITY_REPORT_PATH);
                var report = JsonUtility.FromJson<QualityAnalysisReport>(jsonContent);

                var topViolatingFiles = report.fileReports
                    .Where(fr => fr.Violations.Count > 0)
                    .OrderByDescending(fr => fr.Violations.Count(v => v.Severity == ViolationSeverity.Critical) * 10 +
                                             fr.Violations.Count(v => v.Severity == ViolationSeverity.Major) * 3 +
                                             fr.Violations.Count(v => v.Severity == ViolationSeverity.Minor))
                    .Take(10)
                    .ToList();

                ChimeraLogger.Log("🎯 TOP 10 FILES NEEDING ATTENTION");
                ChimeraLogger.Log("═════════════════════════════════");

                foreach (var fileReport in topViolatingFiles)
                {
                    var fileName = Path.GetFileName(fileReport.FilePath);
                    var critical = fileReport.Violations.Count(v => v.Severity == ViolationSeverity.Critical);
                    var major = fileReport.Violations.Count(v => v.Severity == ViolationSeverity.Major);
                    var minor = fileReport.Violations.Count(v => v.Severity == ViolationSeverity.Minor);

                    ChimeraLogger.Log($"📄 {fileName} ({fileReport.SystemType})");
                    ChimeraLogger.Log($"   Lines: {fileReport.Metrics.LineCount}, Methods: {fileReport.Metrics.MethodCount}");
                    ChimeraLogger.Log($"   Violations: 🔴{critical} 🟡{major} 🟢{minor}");
                    ChimeraLogger.Log("");
                }
            }
            catch (Exception e)
            {
                ChimeraLogger.LogError($"Failed to load quality report: {e.Message}");
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// Overall quality analysis report
    /// </summary>
    [Serializable]
    public class QualityAnalysisReport
    {
        public DateTime analysisTime;
        public List<QualityReport> fileReports;
        public QualitySummary summary;
    }

    /// <summary>
    /// Quality analysis summary statistics
    /// </summary>
    [Serializable]
    public class QualitySummary
    {
        public int totalFiles;
        public int totalViolations;
        public int criticalViolations;
        public int majorViolations;
        public int minorViolations;
        public int filesWithViolations;
        public float averageComplexityScore;
        public string qualityGrade;
        public bool passesQualityGates;
    }

    /// <summary>
    /// Unity ScriptableObject for viewing quality reports in Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "QualityReport", menuName = "Project Chimera/Quality Report")]
    public class UnityQualityReport : ScriptableObject
    {
        [Header("Quality Analysis Summary")]
        public string analysisTime;
        public int totalFiles;
        public string qualityGrade;
        public float averageComplexityScore;
        public bool passesQualityGates;

        [Header("Violation Counts")]
        public int criticalViolations;
        public int majorViolations;
        public int minorViolations;
        public int filesWithViolations;

        [Header("System Breakdown")]
        [SerializeField] private SystemQualityBreakdown[] systemBreakdown;

        public void Initialize(QualityAnalysisReport report)
        {
            analysisTime = report.analysisTime.ToString("yyyy-MM-dd HH:mm:ss");
            totalFiles = report.summary.totalFiles;
            qualityGrade = report.summary.qualityGrade;
            averageComplexityScore = report.summary.averageComplexityScore;
            passesQualityGates = report.summary.passesQualityGates;
            criticalViolations = report.summary.criticalViolations;
            majorViolations = report.summary.majorViolations;
            minorViolations = report.summary.minorViolations;
            filesWithViolations = report.summary.filesWithViolations;

            // Create system breakdown
            var systemGroups = report.fileReports.GroupBy(fr => fr.SystemType).ToList();
            systemBreakdown = systemGroups.Select(g => new SystemQualityBreakdown
            {
                systemName = g.Key,
                fileCount = g.Count(),
                averageComplexity = (float)g.Average(fr => fr.Metrics.CyclomaticComplexity),
                totalViolations = g.Sum(fr => fr.Violations.Count),
                worstFile = g.OrderByDescending(fr => fr.Violations.Count).First().FilePath
            }).ToArray();
        }

        [Serializable]
        public class SystemQualityBreakdown
        {
            public string systemName;
            public int fileCount;
            public float averageComplexity;
            public int totalViolations;
            public string worstFile;
        }
    }

    #endregion
}
