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
                ChimeraLogger.Log("🔧 CI Environment detected - configuring tools...");
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

            ChimeraLogger.Log("✅ CI settings configured");
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
            ChimeraLogger.Log("🔍 Running CI Quality Analysis...");
            CodeQualityAnalyzer.RunQualityAnalysis();
        }

        /// <summary>
        /// Command line method for building performance tests
        /// </summary>
        public static void BuildPerformanceTestsCI()
        {
            ChimeraLogger.Log("🎯 Building Performance Tests for CI...");
            PerformanceBuildMethod.BuildPerformanceTest();
        }

        /// <summary>
        /// Command line method for running all CI checks
        /// </summary>
        public static void RunAllCIChecks()
        {
            ChimeraLogger.Log("🚀 Running Complete CI Analysis Pipeline...");

            try
            {
                // Step 1: Quality Analysis
                CodeQualityAnalyzer.RunQualityAnalysis();

                // Step 2: Architecture Validation
                ValidateArchitecture();

                // Step 3: Performance Build
                PerformanceBuildMethod.BuildPerformanceTest();

                ChimeraLogger.Log("✅ All CI checks completed successfully");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                ChimeraLogger.LogError($"❌ CI checks failed: {e.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Validate architectural patterns and constraints
        /// </summary>
        private static void ValidateArchitecture()
        {
            ChimeraLogger.Log("🏗️ Validating architectural patterns...");

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
                ChimeraLogger.LogWarning("⚠️ Architecture validation found issues:");
                foreach (var violation in violations)
                {
                    ChimeraLogger.LogWarning($"  • {violation}");
                }
            }
            else
            {
                ChimeraLogger.Log("✅ Architecture validation passed");
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

            ChimeraLogger.Log($"✅ Build info generated: {buildInfoPath}");
        }

        /// <summary>
        /// Validate all assembly definitions are properly configured
        /// </summary>
        [MenuItem("Project Chimera/CI/Validate Assembly Definitions")]
        public static void ValidateAssemblyDefinitions()
        {
            ChimeraLogger.Log("🔍 Validating assembly definitions...");

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
                ChimeraLogger.LogWarning("⚠️ Assembly definition validation issues:");
                foreach (var issue in issues)
                {
                    ChimeraLogger.LogWarning($"  • {issue}");
                }
            }
            else
            {
                ChimeraLogger.Log($"✅ Validated {asmdefFiles.Length} assembly definitions");
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
