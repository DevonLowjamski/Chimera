using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Automated build processor that integrates with Chimera build profiles
    /// Handles pre-build and post-build operations for different build configurations
    /// </summary>
    public class ChimeraAutomatedBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ChimeraLogger.Log("[ChimeraAutomatedBuild] Starting pre-build process...");

            ValidateBuildConfiguration();
            ApplyBuildSpecificSettings(report.summary.platform);
            LogBuildConfiguration();

            ChimeraLogger.Log("[ChimeraAutomatedBuild] Pre-build process completed.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            ChimeraLogger.Log("[ChimeraAutomatedBuild] Starting post-build process...");

            LogBuildResults(report);
            GenerateBuildReport(report);
            CleanupTempSettings();

            ChimeraLogger.Log("[ChimeraAutomatedBuild] Post-build process completed.");
        }

        private void ValidateBuildConfiguration()
        {
            ChimeraBuildProfiles.ValidateBuildConfiguration();

            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();

            // Warn about potentially problematic configurations
            if (activeProfile == "Development" && EditorUserBuildSettings.development == false)
            {
                ChimeraLogger.LogWarning("[ChimeraAutomatedBuild] Development profile active but Unity Development Build is disabled");
            }

            if (activeDefines.Contains("CHIMERA_PRODUCTION") && EditorUserBuildSettings.development == true)
            {
                ChimeraLogger.LogWarning("[ChimeraAutomatedBuild] Production defines active but Unity Development Build is enabled");
            }
        }

        private void ApplyBuildSpecificSettings(BuildTarget buildTarget)
        {
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();

            ChimeraLogger.Log($"[ChimeraAutomatedBuild] Applying settings for {activeProfile} profile on {buildTarget}");

            // Apply profile-specific Unity settings
            switch (activeProfile)
            {
                case "Production":
                case "Release":
                    ApplyProductionSettings();
                    break;

                case "Development":
                case "Debug":
                    ApplyDevelopmentSettings();
                    break;

                case "Testing":
                    ApplyTestingSettings();
                    break;
            }
        }

        private void ApplyProductionSettings()
        {
            // Ensure optimization settings for production
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            PlayerSettings.stripEngineCode = true;

            // Set IL2CPP optimizations if using IL2CPP
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Release);
            }

            ChimeraLogger.Log("[ChimeraAutomatedBuild] Applied production build settings");
        }

        private void ApplyDevelopmentSettings()
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            EditorUserBuildSettings.connectProfiler = true;
            PlayerSettings.stripEngineCode = false;

            ChimeraLogger.Log("[ChimeraAutomatedBuild] Applied development build settings");
        }

        private void ApplyTestingSettings()
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            PlayerSettings.stripEngineCode = false;

            ChimeraLogger.Log("[ChimeraAutomatedBuild] Applied testing build settings");
        }

        private void LogBuildConfiguration()
        {
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            ChimeraLogger.Log($"[ChimeraAutomatedBuild] Build Configuration:");
            ChimeraLogger.Log($"  Profile: {activeProfile}");
            ChimeraLogger.Log($"  Target: {buildTarget} ({buildTargetGroup})");
            ChimeraLogger.Log($"  Defines: {string.Join(", ", activeDefines)}");
            ChimeraLogger.Log($"  Development Build: {EditorUserBuildSettings.development}");
            ChimeraLogger.Log($"  Script Debugging: {EditorUserBuildSettings.allowDebugging}");
            ChimeraLogger.Log($"  IL2CPP: {PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP}");
        }

        private void LogBuildResults(BuildReport report)
        {
            var summary = report.summary;

            ChimeraLogger.Log($"[ChimeraAutomatedBuild] Build Results:");
            ChimeraLogger.Log($"  Result: {summary.result}");
            ChimeraLogger.Log($"  Platform: {summary.platform}");
            ChimeraLogger.Log($"  Total Time: {summary.totalTime}");
            ChimeraLogger.Log($"  Total Size: {summary.totalSize} bytes");
            ChimeraLogger.Log($"  Output Path: {summary.outputPath}");
            ChimeraLogger.Log($"  Total Errors: {summary.totalErrors}");
            ChimeraLogger.Log($"  Total Warnings: {summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
            {
                ChimeraLogger.LogError($"[ChimeraAutomatedBuild] Build failed with result: {summary.result}");
            }
        }

        private void GenerateBuildReport(BuildReport report)
        {
            try
            {
                var reportPath = Path.Combine(Application.dataPath, "../BuildReports");
                Directory.CreateDirectory(reportPath);

                var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
                var fileName = $"BuildReport_{activeProfile}_{report.summary.platform}_{timestamp}.txt";
                var fullPath = Path.Combine(reportPath, fileName);

                using (var writer = new StreamWriter(fullPath))
                {
                    writer.WriteLine("Project Chimera Build Report");
                    writer.WriteLine($"Generated: {System.DateTime.Now}");
                    writer.WriteLine($"Profile: {activeProfile}");
                    writer.WriteLine($"Platform: {report.summary.platform}");
                    writer.WriteLine($"Result: {report.summary.result}");
                    writer.WriteLine($"Total Time: {report.summary.totalTime}");
                    writer.WriteLine($"Total Size: {report.summary.totalSize} bytes");
                    writer.WriteLine($"Output Path: {report.summary.outputPath}");
                    writer.WriteLine($"Errors: {report.summary.totalErrors}");
                    writer.WriteLine($"Warnings: {report.summary.totalWarnings}");
                    writer.WriteLine();

                    writer.WriteLine("Active Script Defines:");
                    var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
                    foreach (var define in activeDefines)
                    {
                        writer.WriteLine($"  - {define}");
                    }
                    writer.WriteLine();

                    writer.WriteLine("Build Steps:");
                    foreach (var step in report.steps)
                    {
                        writer.WriteLine($"  {step.name}: {step.duration}");
                    }
                }

                ChimeraLogger.Log($"[ChimeraAutomatedBuild] Build report saved to: {fullPath}");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[ChimeraAutomatedBuild] Failed to generate build report: {ex.Message}");
            }
        }

        private void CleanupTempSettings()
        {
            // Clean up any temporary settings that were applied during build
            ChimeraLogger.Log("[ChimeraAutomatedBuild] Cleaning up temporary build settings");
        }

        /// <summary>
        /// Command line build method for CI/CD integration
        /// Usage: Unity -quit -batchmode -projectPath . -executeMethod ChimeraAutomatedBuild.BuildFromCommandLine -profile=Production
        /// </summary>
        public static void BuildFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            var profileArg = System.Array.Find(args, arg => arg.StartsWith("-profile="));
            var profile = profileArg?.Substring("-profile=".Length) ?? "Production";

            ChimeraLogger.Log($"[ChimeraAutomatedBuild] Starting command line build with profile: {profile}");

            // Apply the requested profile
            ChimeraBuildProfiles.ApplyBuildProfile(profile);

            // Get build target from command line or use current
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            // Set up build player options
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = GetBuildPath(buildTarget, profile),
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = GetBuildOptions(profile)
            };

            // Start the build
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != BuildResult.Succeeded)
            {
                ChimeraLogger.LogError($"[ChimeraAutomatedBuild] Command line build failed: {report.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                ChimeraLogger.Log($"[ChimeraAutomatedBuild] Command line build succeeded");
                EditorApplication.Exit(0);
            }
        }

        private static string GetBuildPath(BuildTarget buildTarget, string profile)
        {
            var basePath = Path.Combine(Application.dataPath, "../Builds");
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var folderName = $"ProjectChimera_{profile}_{buildTarget}_{timestamp}";

            return Path.Combine(basePath, folderName, GetExecutableName(buildTarget));
        }

        private static string GetExecutableName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "ProjectChimera.exe";
                case BuildTarget.StandaloneOSX:
                    return "ProjectChimera.app";
                case BuildTarget.StandaloneLinux64:
                    return "ProjectChimera";
                default:
                    return "ProjectChimera";
            }
        }

        private static BuildOptions GetBuildOptions(string profile)
        {
            var options = BuildOptions.None;

            switch (profile)
            {
                case "Development":
                case "Debug":
                    options |= BuildOptions.Development;
                    options |= BuildOptions.AllowDebugging;
                    options |= BuildOptions.ConnectWithProfiler;
                    break;

                case "Testing":
                    options |= BuildOptions.Development;
                    break;

                case "Production":
                case "Release":
                    // No additional options for optimized builds
                    break;
            }

            return options;
        }
    }
}
