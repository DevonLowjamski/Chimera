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
            ChimeraLogger.Log("BUILD", "🚀 Starting automated build preprocessing", null);

            ValidateBuildConfiguration();
            ApplyBuildSpecificSettings(report.summary.platform);
            LogBuildConfiguration();

            ChimeraLogger.Log("BUILD", "✅ Build preprocessing completed successfully", null);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            ChimeraLogger.Log("BUILD", "📋 Starting automated build postprocessing", null);

            LogBuildResults(report);
            GenerateBuildReport(report);
            CleanupTempSettings();

            ChimeraLogger.Log("BUILD", "🎯 Build postprocessing completed successfully", null);
        }

        private void ValidateBuildConfiguration()
        {
            ChimeraBuildProfiles.ValidateBuildConfiguration();

            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();

            // Warn about potentially problematic configurations
            if (activeProfile == "Development" && EditorUserBuildSettings.development == false)
            {
                ChimeraLogger.LogWarning("BUILD", "⚠️ Development profile active but Unity development build disabled", null);
            }

            if (activeDefines.Contains("CHIMERA_PRODUCTION") && EditorUserBuildSettings.development == true)
            {
                ChimeraLogger.LogWarning("BUILD", "⚠️ Production defines active but Unity development build enabled", null);
            }
        }

        private void ApplyBuildSpecificSettings(BuildTarget buildTarget)
        {
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();

            ChimeraLogger.Log("BUILD", $"⚙️ Applying {activeProfile} profile settings for {buildTarget}", null);

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

            ChimeraLogger.Log("BUILD", "✅ Production settings applied: optimizations enabled, debugging disabled", null);
        }

        private void ApplyDevelopmentSettings()
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            EditorUserBuildSettings.connectProfiler = true;
            PlayerSettings.stripEngineCode = false;

            ChimeraLogger.Log("BUILD", "🔧 Development settings applied: debugging enabled, profiler connected", null);
        }

        private void ApplyTestingSettings()
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            PlayerSettings.stripEngineCode = false;

            ChimeraLogger.Log("BUILD", "🧪 Testing settings applied: debugging enabled, no code stripping", null);
        }

        private void LogBuildConfiguration()
        {
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            ChimeraLogger.Log("BUILD", "📋 Build Configuration Summary:", null);
            ChimeraLogger.Log("BUILD", $"   🎯 Active Profile: {activeProfile}", null);
            ChimeraLogger.Log("BUILD", $"   📱 Build Target: {buildTarget}", null);
            ChimeraLogger.Log("BUILD", $"   📱 Target Group: {buildTargetGroup}", null);
            ChimeraLogger.Log("BUILD", $"   🔧 Script Defines: {string.Join(", ", activeDefines)}", null);
            ChimeraLogger.Log("BUILD", $"   🚀 Development Build: {EditorUserBuildSettings.development}", null);
            ChimeraLogger.Log("BUILD", $"   🐛 Allow Debugging: {EditorUserBuildSettings.allowDebugging}", null);
        }

        private void LogBuildResults(BuildReport report)
        {
            var summary = report.summary;

            ChimeraLogger.Log("BUILD", "📊 Build Results Summary:", null);
            ChimeraLogger.Log("BUILD", $"   🎯 Result: {summary.result}", null);
            ChimeraLogger.Log("BUILD", $"   ⏱️ Total Time: {summary.totalTime}", null);
            ChimeraLogger.Log("BUILD", $"   📦 Total Size: {summary.totalSize / (1024 * 1024):F2} MB", null);
            ChimeraLogger.Log("BUILD", $"   📁 Output Path: {summary.outputPath}", null);
            ChimeraLogger.Log("BUILD", $"   ❌ Errors: {summary.totalErrors}", null);
            ChimeraLogger.Log("BUILD", $"   ⚠️ Warnings: {summary.totalWarnings}", null);
            ChimeraLogger.Log("BUILD", $"   🏗️ Platform: {summary.platform}", null);

            if (summary.result != BuildResult.Succeeded)
            {
                ChimeraLogger.LogError("BUILD", $"❌ Build failed with result: {summary.result}. Check build logs for details.", null);
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

                ChimeraLogger.Log("BUILD", $"📄 Build report generated successfully: {fullPath}", null);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError("BUILD", $"❌ Failed to generate build report: {ex.Message}", null);
            }
        }

        private void CleanupTempSettings()
        {
            // Clean up any temporary settings that were applied during build
            ChimeraLogger.Log("BUILD", "🧹 Temporary build settings cleaned up", null);
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

            ChimeraLogger.Log("BUILD", $"🚀 Starting command line build with profile: {profile}", null);

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
                ChimeraLogger.LogError("BUILD", $"❌ Command line build failed with result: {report.summary.result}", null);
                EditorApplication.Exit(1);
            }
            else
            {
                ChimeraLogger.Log("BUILD", $"✅ Command line build completed successfully. Output: {report.summary.outputPath}", null);
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
