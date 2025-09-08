using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.CI
{
    /// <summary>
    /// CI/CD Performance Build Method for Project Chimera
    /// Creates optimized builds specifically for performance testing and benchmarking
    /// </summary>
    public static class PerformanceBuildMethod
    {
        /// <summary>
        /// Build method called by CI pipeline for performance testing
        /// </summary>
        public static void BuildPerformanceTest()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetPerformanceTestScenes(),
                locationPathName = GetBuildPath(),
                target = EditorUserBuildSettings.activeBuildTarget,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport
            };

            // Configure performance-specific build settings
            ConfigurePerformanceBuildSettings();

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Performance build succeeded: {summary.totalSize} bytes");
                GeneratePerformanceBuildReport(report);
            }
            else
            {
                Debug.LogError($"Performance build failed: {summary.result}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Get scenes specifically designed for performance testing
        /// </summary>
        private static string[] GetPerformanceTestScenes()
        {
            var performanceScenes = new List<string>();

            // Add performance test scenes
            var performanceTestScenes = new[]
            {
                "Assets/ProjectChimera/Scenes/Performance/PlantStressTest.unity",
                "Assets/ProjectChimera/Scenes/Performance/MassiveFacilityTest.unity",
                "Assets/ProjectChimera/Scenes/Performance/GeneticsCalculationTest.unity",
                "Assets/ProjectChimera/Scenes/Performance/EnvironmentalSimulationTest.unity"
            };

            foreach (var scene in performanceTestScenes)
            {
                if (File.Exists(scene))
                {
                    performanceScenes.Add(scene);
                }
                else
                {
                    Debug.LogWarning($"Performance test scene not found: {scene}");
                }
            }

            // Fallback to main scenes if performance scenes don't exist
            if (performanceScenes.Count == 0)
            {
                performanceScenes.AddRange(EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path));
            }

            return performanceScenes.ToArray();
        }

        /// <summary>
        /// Configure Unity build settings optimized for performance testing
        /// </summary>
        private static void ConfigurePerformanceBuildSettings()
        {
            // Graphics Settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.graphicsJobs = true;

            // Performance Settings
            PlayerSettings.stripEngineCode = false; // Keep for profiling
            PlayerSettings.stripUnusedMeshComponents = false; // Keep for analysis
            PlayerSettings.bakeCollisionMeshes = true;

            // Script Compilation Settings
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(EditorUserBuildSettings.selectedBuildTargetGroup, Il2CppCompilerConfiguration.Master);

            // Enable performance profiling
            PlayerSettings.enableInternalProfiler = true;

            // Logging settings for performance builds
            var defines = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';'));

            // Add performance testing defines
            if (!defines.Contains("CHIMERA_PERFORMANCE_BUILD"))
                defines.Add("CHIMERA_PERFORMANCE_BUILD");

            if (!defines.Contains("CHIMERA_PROFILING_ENABLED"))
                defines.Add("CHIMERA_PROFILING_ENABLED");

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", defines));

            Debug.Log("Configured build settings for performance testing");
        }

        /// <summary>
        /// Get the appropriate build path for CI environment
        /// </summary>
        private static string GetBuildPath()
        {
            var platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            var buildPath = $"Builds/Performance/{platform}/ProjectChimera-Performance";

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    return $"{buildPath}.exe";
                case BuildTarget.StandaloneLinux64:
                    return buildPath;
                case BuildTarget.StandaloneOSX:
                    return $"{buildPath}.app";
                default:
                    return buildPath;
            }
        }

        /// <summary>
        /// Generate detailed build report for CI pipeline
        /// </summary>
        private static void GeneratePerformanceBuildReport(BuildReport report)
        {
            var buildReport = new PerformanceBuildReport
            {
                buildTime = System.DateTime.Now,
                platform = report.summary.platform.ToString(),
                totalSize = report.summary.totalSize,
                totalTime = report.summary.totalTime.TotalSeconds,
                buildResult = report.summary.result.ToString(),
                assetCount = report.GetFiles().Length,
                warningCount = report.summary.totalWarnings,
                errorCount = report.summary.totalErrors
            };

            // Add detailed file analysis
            var largestAssets = report.GetFiles()
                .OrderByDescending(f => f.size)
                .Take(20)
                .Select(f => new AssetInfo
                {
                    path = f.path,
                    size = f.size,
                    role = f.role
                })
                .ToArray();

            buildReport.largestAssets = largestAssets;

            // Write build report to JSON for CI consumption
            var reportJson = JsonUtility.ToJson(buildReport, true);
            var reportPath = "performance-build-report.json";
            File.WriteAllText(reportPath, reportJson);

            Debug.Log($"Performance build report generated: {reportPath}");
            Debug.Log($"Build completed in {report.summary.totalTime.TotalSeconds:F2}s with {report.summary.totalWarnings} warnings");
        }
    }

    /// <summary>
    /// Performance build report data structure for CI pipeline
    /// </summary>
    [System.Serializable]
    public class PerformanceBuildReport
    {
        public System.DateTime buildTime;
        public string platform;
        public ulong totalSize;
        public double totalTime;
        public string buildResult;
        public int assetCount;
        public int warningCount;
        public int errorCount;
        public AssetInfo[] largestAssets;
    }

    /// <summary>
    /// Asset information for build analysis
    /// </summary>
    [System.Serializable]
    public class AssetInfo
    {
        public string path;
        public ulong size;
        public string role;
    }
}
