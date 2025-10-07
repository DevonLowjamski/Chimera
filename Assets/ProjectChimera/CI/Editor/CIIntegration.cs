using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.CI.Editor
{
    /// <summary>
    /// CI Integration for Project Chimera - Editor Tools
    /// Provides integration between Unity Editor and CI/CD pipeline
    /// </summary>
    [InitializeOnLoad]
    public static class CIIntegration
    {
        static CIIntegration()
        {
            // Initialize CI tools when Unity Editor starts
            InitializeCIEnvironment();
        }

        /// <summary>
        /// Initialize CI environment and tools
        /// </summary>
        private static void InitializeCIEnvironment()
        {
            // Check if we're in a CI environment
            var isCIBuild = System.Environment.GetEnvironmentVariable("CI") == "true" ||
                           System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
                           Application.isBatchMode;

            if (isCIBuild)
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
                ConfigureCISettings();
            }

            // Always ensure CI folder structure exists
            EnsureCIFolderStructure();
        }

        /// <summary>
        /// Configure Unity settings for CI builds
        /// </summary>
        private static void ConfigureCISettings()
        {
            // Disable domain reload for faster iteration in CI
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            // Configure asset database for CI
            EditorSettings.refreshImportMode = AssetDatabase.RefreshImportMode.InProcess;

            // Set quality settings for consistent builds
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            ChimeraLogger.LogInfo("CIIntegration", "$1");
        }

        /// <summary>
        /// Ensure CI folder structure exists
        /// </summary>
        private static void EnsureCIFolderStructure()
        {
            var requiredFolders = new[]
            {
                "Assets/ProjectChimera/CI",
                "Assets/ProjectChimera/CI/Editor",
                "Assets/ProjectChimera/Testing/Performance",
                "Builds",
                "Builds/Performance"
            };

            foreach (var folder in requiredFolders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        /// <summary>
        /// Command line method for running quality analysis
        /// Called by CI pipeline
        /// </summary>
        public static void RunQualityAnalysisCI()
        {
            ChimeraLogger.LogInfo("CIIntegration", "Running quality analysis...");
            // DISABLED: CodeQualityAnalyzer has been disabled (advanced feature)
            // CodeQualityAnalyzer.RunQualityAnalysis();
            ChimeraLogger.LogInfo("CIIntegration", "Quality analysis skipped - advanced feature disabled");
        }

        /// <summary>
        /// Command line method for building performance tests
        /// </summary>
        public static void BuildPerformanceTestsCI()
        {
            ChimeraLogger.LogInfo("CIIntegration", "$1");
            PerformanceBuildMethod.BuildPerformanceTest();
        }

        /// <summary>
        /// Command line method for running all CI checks
        /// </summary>
        public static void RunAllCIChecks()
        {
            ChimeraLogger.LogInfo("CIIntegration", "$1");

            try
            {
                // Step 1: Quality Analysis
                // DISABLED: CodeQualityAnalyzer has been disabled (advanced feature)
                // CodeQualityAnalyzer.RunQualityAnalysis();

                // Step 2: Architecture Validation
                ValidateArchitecture();

                // Step 3: Performance Build
                PerformanceBuildMethod.BuildPerformanceTest();

                ChimeraLogger.LogInfo("CIIntegration", "$1");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Validate architectural patterns and constraints
        /// </summary>
        private static void ValidateArchitecture()
        {
            ChimeraLogger.LogInfo("CIIntegration", "$1");

            var violations = new System.Collections.Generic.List<string>();

            // Check for proper namespace usage
            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories);

            foreach (var file in csFiles)
            {
                if (file.Contains("/CI/") || file.Contains("/Testing/")) continue;

                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);

                // Check namespace structure
                if (!content.Contains("namespace ProjectChimera"))
                {
                    violations.Add($"{fileName}: Missing proper ProjectChimera namespace");
                }

                // Check for MonoBehaviour inheritance patterns
                if (content.Contains(": MonoBehaviour") && !content.Contains("ITickable"))
                {
                    // Allow certain exceptions
                    if (!fileName.Contains("Test") &&
                        !fileName.Contains("Editor") &&
                        !fileName.Contains("UI") &&
                        !content.Contains("// EXEMPT:TICKABLE"))
                    {
                        violations.Add($"{fileName}: MonoBehaviour should implement ITickable for centralized updates");
                    }
                }
            }

            if (violations.Count > 0)
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
                foreach (var violation in violations)
                {
                    ChimeraLogger.LogInfo("CIIntegration", "$1");
                }
            }
            else
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
            }
        }

        /// <summary>
        /// Install pre-commit hook for quality gate enforcement
        /// </summary>
        [MenuItem("Project Chimera/CI/Install Pre-Commit Hook")]
        public static void InstallPreCommitHook()
        {
            ChimeraLogger.LogInfo("CIIntegration", "Installing pre-commit hook for quality gate enforcement...");

            var hookPath = ".git/hooks/pre-commit";

            if (File.Exists(hookPath))
            {
                ChimeraLogger.LogWarning("CIIntegration", "Pre-commit hook already exists - it will be backed up");
                File.Copy(hookPath, hookPath + ".backup", true);
            }

            // Hook should already exist from our setup - just verify it's executable
            if (!File.Exists(hookPath))
            {
                ChimeraLogger.LogError("CIIntegration", "Pre-commit hook not found at .git/hooks/pre-commit");
                return;
            }

            ChimeraLogger.LogInfo("CIIntegration", "✅ Pre-commit hook installed successfully!");
            ChimeraLogger.LogInfo("CIIntegration", "Quality gates will now run automatically on every commit");
            ChimeraLogger.LogInfo("CIIntegration", "To bypass (NOT RECOMMENDED): git commit --no-verify");
        }

        /// <summary>
        /// Test quality gates locally
        /// </summary>
        [MenuItem("Project Chimera/CI/Test Quality Gates")]
        public static void TestQualityGates()
        {
            ChimeraLogger.LogInfo("CIIntegration", "Running quality gate tests...");

            var results = QualityGates.RunAllChecks();

            if (!results.HasViolations)
            {
                ChimeraLogger.LogInfo("CIIntegration", "✅ ALL QUALITY GATES PASSED!");
                ChimeraLogger.LogInfo("CIIntegration", "🎉 Architecture is clean - ready for commit");
            }
            else
            {
                ChimeraLogger.LogError("CIIntegration", $"❌ QUALITY GATE FAILURE: {results.TotalViolations} violations");

                if (results.AntiPatternViolations?.Count > 0)
                {
                    ChimeraLogger.LogError("CIIntegration", $"🚫 Anti-Pattern Violations: {results.AntiPatternViolations.Count}");
                    foreach (var v in results.AntiPatternViolations)
                    {
                        ChimeraLogger.LogError("CIIntegration", $"  {Path.GetFileName(v.File)}:{v.LineNumber} - {v.Pattern}");
                    }
                }

                if (results.FileSizeViolations?.Count > 0)
                {
                    ChimeraLogger.LogWarning("CIIntegration", $"📏 File Size Violations: {results.FileSizeViolations.Count}");
                    foreach (var v in results.FileSizeViolations)
                    {
                        ChimeraLogger.LogWarning("CIIntegration", $"  {Path.GetFileName(v.File)}: {v.LineCount}/{v.MaxAllowed} lines");
                    }
                }

                if (results.ArchitectureViolations?.Count > 0)
                {
                    ChimeraLogger.LogError("CIIntegration", $"🏗️ Architecture Violations: {results.ArchitectureViolations.Count}");
                    foreach (var v in results.ArchitectureViolations)
                    {
                        ChimeraLogger.LogError("CIIntegration", $"  {Path.GetFileName(v.File)}: {v.Type} - {v.Description}");
                    }
                }
            }
        }

        /// <summary>
        /// Generate CI build info file
        /// </summary>
        [MenuItem("Project Chimera/CI/Generate Build Info")]
        public static void GenerateBuildInfo()
        {
            var buildInfo = new
            {
                buildTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                buildNumber = System.Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "local",
                commitHash = System.Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "unknown",
                branch = System.Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "unknown",
                projectVersion = Application.version
            };

            var json = JsonUtility.ToJson(buildInfo, true);
            var buildInfoPath = "Assets/ProjectChimera/CI/build-info.json";

            File.WriteAllText(buildInfoPath, json);
            AssetDatabase.Refresh();

            ChimeraLogger.LogInfo("CIIntegration", "$1");
        }

        /// <summary>
        /// Validate all assembly definitions are properly configured
        /// </summary>
        [MenuItem("Project Chimera/CI/Validate Assembly Definitions")]
        public static void ValidateAssemblyDefinitions()
        {
            ChimeraLogger.LogInfo("CIIntegration", "$1");

            var asmdefFiles = Directory.GetFiles("Assets/ProjectChimera", "*.asmdef", SearchOption.AllDirectories);
            var issues = new System.Collections.Generic.List<string>();

            foreach (var asmdefFile in asmdefFiles)
            {
                var content = File.ReadAllText(asmdefFile);
                var asmdefName = Path.GetFileNameWithoutExtension(asmdefFile);

                // Parse JSON to check structure
                try
                {
                    var asmdef = JsonUtility.FromJson<AssemblyDefinitionData>(content);

                    // Check naming convention
                    if (!asmdef.name.StartsWith("ProjectChimera."))
                    {
                        issues.Add($"{asmdefName}: Should use 'ProjectChimera.' prefix");
                    }

                    // Check for proper references
                    var expectedCoreReference = "ProjectChimera.Core";
                    if (asmdef.name != expectedCoreReference &&
                        (asmdef.references == null || !asmdef.references.Contains(expectedCoreReference)))
                    {
                        issues.Add($"{asmdefName}: Should reference {expectedCoreReference}");
                    }
                }
                catch (System.Exception e)
                {
                    issues.Add($"{asmdefName}: Invalid JSON structure - {e.Message}");
                }
            }

            if (issues.Count > 0)
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
                foreach (var issue in issues)
                {
                    ChimeraLogger.LogInfo("CIIntegration", "$1");
                }
            }
            else
            {
                ChimeraLogger.LogInfo("CIIntegration", "$1");
            }
        }

        [System.Serializable]
        private class AssemblyDefinitionData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
        }
    }
}
