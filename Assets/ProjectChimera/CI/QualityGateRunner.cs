using System;
using UnityEngine;

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
            Debug.Log("🔍 Project Chimera Quality Gates - Enhanced Validation");
            Debug.Log("=" + new string('=', 60));

            try
            {
                var results = QualityGates.RunAllChecks();

                if (!results.HasViolations)
                {
                    Debug.Log("✅ ALL QUALITY GATES PASSED!");
                    Debug.Log($"🎉 Zero violations found - architecture is clean!");
                    return;
                }

                Debug.LogWarning($"⚠️ Quality Gate Violations Found: {results.TotalViolations} total");

                // Report Anti-Pattern Violations
                if (results.AntiPatternViolations?.Count > 0)
                {
                    Debug.LogError($"❌ ANTI-PATTERN VIOLATIONS: {results.AntiPatternViolations.Count}");
                    
                    if (_logViolations)
                    {
                        foreach (var violation in results.AntiPatternViolations)
                        {
                            Debug.LogError($"  💥 {violation.File}:{violation.LineNumber} - Pattern: {violation.Pattern}");
                            Debug.LogError($"      Content: {violation.Content}");
                        }
                    }
                }

                // Report File Size Violations
                if (results.FileSizeViolations?.Count > 0)
                {
                    Debug.LogWarning($"📏 FILE SIZE VIOLATIONS: {results.FileSizeViolations.Count}");
                    
                    if (_logViolations)
                    {
                        foreach (var violation in results.FileSizeViolations)
                        {
                            Debug.LogWarning($"  📄 {violation.File} - {violation.LineCount}/{violation.MaxAllowed} lines");
                        }
                    }
                }

                // Report Architecture Violations
                if (results.ArchitectureViolations?.Count > 0)
                {
                    Debug.LogError($"🏗️ ARCHITECTURE VIOLATIONS: {results.ArchitectureViolations.Count}");
                    
                    if (_logViolations)
                    {
                        foreach (var violation in results.ArchitectureViolations)
                        {
                            Debug.LogError($"  🚫 {violation.File} - {violation.Type}: {violation.Description}");
                        }
                    }
                }

                // Report Complexity Violations
                if (results.ComplexityViolations?.Count > 0)
                {
                    Debug.LogWarning($"🔥 COMPLEXITY VIOLATIONS: {results.ComplexityViolations.Count}");
                    
                    if (_logViolations)
                    {
                        foreach (var violation in results.ComplexityViolations)
                        {
                            Debug.LogWarning($"  ⚡ {violation.File}:{violation.Method} - Complexity: {violation.Complexity}/{violation.MaxAllowed}");
                        }
                    }
                }

                Debug.Log("=" + new string('=', 60));

                if (_exitOnFailure && results.AntiPatternViolations?.Count > 0)
                {
                    Debug.LogError("💥 CRITICAL VIOLATIONS DETECTED - BLOCKING COMMIT");
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
                    #endif
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"💥 Quality Gate Runner Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test individual anti-pattern detection
        /// </summary>
        [ContextMenu("Test Anti-Pattern Detection")]
        public void TestAntiPatternDetection()
        {
            Debug.Log("🔍 Testing Anti-Pattern Detection...");

            var violations = QualityGates.CheckAntiPatterns();
            Debug.Log($"Found {violations.Count} anti-pattern violations");

            foreach (var violation in violations)
            {
                Debug.LogWarning($"⚠️ {violation.File}:{violation.LineNumber} - {violation.Pattern}");
            }
        }

        /// <summary>
        /// Test file size validation
        /// </summary>
        [ContextMenu("Test File Size Validation")]
        public void TestFileSizeValidation()
        {
            Debug.Log("📏 Testing File Size Validation...");

            var violations = QualityGates.CheckFileSizes();
            Debug.Log($"Found {violations.Count} file size violations");

            foreach (var violation in violations)
            {
                Debug.LogWarning($"📄 {violation.File} - {violation.LineCount}/{violation.MaxAllowed} lines");
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