using System;
using UnityEngine;

using ProjectChimera.Core.Logging;
namespace ProjectChimera.CI
{
    /// <summary>
    /// Quality Gate Runner - Tests enhanced quality gates and reports violations
    /// Used for manual validation and CI/CD integration
    /// </summary>
    public class QualityGateRunner : MonoBehaviour
    {
        [Header("Quality Gate Settings")]
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private bool _logViolations = true;
        [SerializeField] private bool _exitOnFailure = false;

        private void Start()
        {
            if (_runOnStart)
            {
                RunQualityGates();
            }
        }

        /// <summary>
        /// Run all quality gate checks and report results
        /// </summary>
        [ContextMenu("Run Quality Gates")]
        public void RunQualityGates()
        {
            ChimeraLogger.LogInfo("QualityGateRunner", "Starting Quality Gate validation...");
            ChimeraLogger.LogInfo("QualityGateRunner", "===========================================");

            try
            {
                var results = QualityGates.RunAllChecks();

                if (!results.HasViolations)
                {
                    ChimeraLogger.LogInfo("QualityGateRunner", "✅ All Quality Gates PASSED!");
                    ChimeraLogger.LogInfo("QualityGateRunner", "Project is ready for deployment.");
                    return;
                }

                ChimeraLogger.LogWarning("QualityGateRunner", "⚠️ Quality Gate violations detected:");

                // Report Anti-Pattern Violations
                if (results.AntiPatternViolations?.Count > 0)
                {
                    ChimeraLogger.LogError("QualityGateRunner", $"❌ Anti-Pattern Violations: {results.AntiPatternViolations.Count}");

                    if (_logViolations)
                    {
                        foreach (var violation in results.AntiPatternViolations)
                        {
                            ChimeraLogger.LogError("QualityGateRunner", $"  • {violation.Pattern} in {violation.File}:{violation.LineNumber}");
                            ChimeraLogger.LogError("QualityGateRunner", $"    Content: {violation.Content}");
                        }
                    }
                }

                // Report File Size Violations
                if (results.FileSizeViolations?.Count > 0)
                {
                    ChimeraLogger.LogWarning("QualityGateRunner", $"⚠️ File Size Violations: {results.FileSizeViolations.Count}");

                    if (_logViolations)
                    {
                        foreach (var violation in results.FileSizeViolations)
                        {
                            ChimeraLogger.LogWarning("QualityGateRunner", $"  • {violation.File}: {violation.LineCount} lines (limit: {violation.MaxAllowed})");
                        }
                    }
                }

                // Report Architecture Violations
                if (results.ArchitectureViolations?.Count > 0)
                {
                    ChimeraLogger.LogError("QualityGateRunner", $"Architecture violations detected: {results.ArchitectureViolations.Count}");

                    if (_logViolations)
                    {
                        foreach (var violation in results.ArchitectureViolations)
                        {
                            ChimeraLogger.LogError("QualityGateRunner", $"Architecture violation: {violation}");
                        }
                    }
                }

                // Report Complexity Violations
                if (results.ComplexityViolations?.Count > 0)
                {
                    ChimeraLogger.LogWarning("QualityGateRunner", $"Complexity violations detected: {results.ComplexityViolations.Count}");

                    if (_logViolations)
                    {
                        foreach (var violation in results.ComplexityViolations)
                        {
                            ChimeraLogger.LogWarning("QualityGateRunner", $"Complexity violation: {violation}");
                        }
                    }
                }

                ChimeraLogger.LogInfo("QualityGateRunner", "Quality gate checks completed");

                if (_exitOnFailure && results.AntiPatternViolations?.Count > 0)
                {
                    ChimeraLogger.LogError("QualityGateRunner", "$1");
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
                    #endif
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("QualityGateRunner", $"Error running quality gates: {ex.Message}");
            }
        }

        /// <summary>
        /// Test individual anti-pattern detection
        /// </summary>
        [ContextMenu("Test Anti-Pattern Detection")]
        public void TestAntiPatternDetection()
        {
            ChimeraLogger.LogInfo("QualityGateRunner", "Testing anti-pattern detection...");

            var violations = QualityGates.CheckAntiPatterns();
            ChimeraLogger.LogInfo("QualityGateRunner", $"Found {violations.Count} anti-pattern violations");

            foreach (var violation in violations)
            {
                ChimeraLogger.LogWarning("QualityGateRunner", $"Anti-pattern: {violation}");
            }
        }

        /// <summary>
        /// Test file size validation
        /// </summary>
        [ContextMenu("Test File Size Validation")]
        public void TestFileSizeValidation()
        {
            ChimeraLogger.LogInfo("QualityGateRunner", "Testing file size validation...");

            var violations = QualityGates.CheckFileSizes();
            ChimeraLogger.LogInfo("QualityGateRunner", $"Found {violations.Count} file size violations");

            foreach (var violation in violations)
            {
                ChimeraLogger.LogWarning("QualityGateRunner", $"File size violation: {violation}");
            }
        }

        /// <summary>
        /// Get quality gate summary
        /// </summary>
        public string GetQualityGateSummary()
        {
            var results = QualityGates.RunAllChecks();
            
            if (!results.HasViolations)
            {
                return "✅ All quality gates passed - architecture is clean!";
            }

            return $"⚠️ {results.TotalViolations} violations found: " +
                   $"{results.AntiPatternViolations?.Count ?? 0} anti-patterns, " +
                   $"{results.FileSizeViolations?.Count ?? 0} file sizes, " +
                   $"{results.ArchitectureViolations?.Count ?? 0} architecture";
        }
    }
}