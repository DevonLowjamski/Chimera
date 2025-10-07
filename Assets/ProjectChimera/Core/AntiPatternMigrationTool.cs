using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core
{
    /// <summary>
    /// CRITICAL: Anti-pattern migration tool for roadmap compliance
    /// Systematically eliminates FindObjectOfType, Debug.Log, Resources.Load, and Update() violations
    /// </summary>
    public static class AntiPatternMigrationTool
    {
        private static readonly Dictionary<string, string> FindObjectOfTypeMigrations = new Dictionary<string, string>
        {
            // Core system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<ITimeManager>"] = "ServiceContainer.Resolve<ITimeManager>",
            ["ServiceContainerFactory.Instance?.TryResolve<IServiceHealthMonitor>"] = "ServiceContainer.Resolve<IServiceHealthMonitor>",
            ["ServiceContainerFactory.Instance?.TryResolve<IGCOptimizationManager>"] = "ServiceContainer.Resolve<IGCOptimizationManager>",
            ["ServiceContainerFactory.Instance?.TryResolve<IStreamingCoordinator>"] = "ServiceContainer.Resolve<IStreamingCoordinator>",

            // Rendering system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<IAdvancedRenderingManager>"] = "ServiceContainer.Resolve<IAdvancedRenderingManager>",
            ["ServiceContainerFactory.Instance?.TryResolve<IPlantInstancedRenderer>"] = "ServiceContainer.Resolve<IPlantInstancedRenderer>",
            ["ServiceContainerFactory.Instance?.TryResolve<ICustomLightingRenderer>"] = "ServiceContainer.Resolve<ICustomLightingRenderer>",
            ["ServiceContainerFactory.Instance?.TryResolve<IEnvironmentalRenderer>"] = "ServiceContainer.Resolve<IEnvironmentalRenderer>",

            // Construction system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<IGridInputHandler>"] = "ServiceContainer.Resolve<IGridInputHandler>",
            ["ServiceContainerFactory.Instance?.TryResolve<IGridPlacementController>"] = "ServiceContainer.Resolve<IGridPlacementController>",
            ["ServiceContainerFactory.Instance?.TryResolve<IConstructionSaveProvider>"] = "ServiceContainer.Resolve<IConstructionSaveProvider>",

            // Cultivation system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<IPlantGrowthSystem>"] = "ServiceContainer.Resolve<IPlantGrowthSystem>",
            ["ServiceContainerFactory.Instance?.TryResolve<ICultivationManager>"] = "ServiceContainer.Resolve<ICultivationManager>",
            ["ServiceContainerFactory.Instance?.TryResolve<IPlantStreamingLODIntegration>"] = "ServiceContainer.Resolve<IPlantStreamingLODIntegration>",

            // UI system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<IUIPerformanceMonitor>"] = "ServiceContainer.Resolve<IUIPerformanceMonitor>",
            ["ServiceContainerFactory.Instance?.TryResolve<ICultivationDashboard>"] = "ServiceContainer.Resolve<ICultivationDashboard>",

            // Memory system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<IMemoryProfiler>"] = "ServiceContainer.Resolve<IMemoryProfiler>",
            ["ServiceContainerFactory.Instance?.TryResolve<IPoolManager>"] = "ServiceContainer.Resolve<IPoolManager>",

            // Camera system migrations
            ["ServiceContainerFactory.Instance?.TryResolve<ICameraService>"] = "ServiceContainer.Resolve<ICameraService>",
            ["ServiceContainerFactory.Instance?.TryResolve<ICameraLevelContextualMenuIntegrator>"] = "ServiceContainer.Resolve<ICameraLevelContextualMenuIntegrator>",

            // Generic Unity component finding
            ["Camera.main ?? ServiceContainerFactory.Instance?.TryResolve<ICameraService>()?.MainCamera"] = "Camera.main ?? ServiceContainer.Resolve<ICameraService>().MainCamera",
            ["FindObjectOfType<Light>"] = "ServiceContainer.Resolve<ILightingService>().GetMainLight()",
        };

        private static readonly Dictionary<string, string> DebugLogMigrations = new Dictionary<string, string>
        {
            [@"Debug\.Log\s*\(\s*""([^""]*)""\s*\)"] = @"Logger.Log(""INFO"", ""$1"", this)",
            [@"Debug\.Log\s*\(\s*""([^""]*)""\s*,\s*([^)]+)\s*\)"] = @"Logger.Log(""INFO"", ""$1"", $2)",
            [@"Debug\.LogWarning\s*\(\s*""([^""]*)""\s*\)"] = @"Logger.LogWarning(""WARNING"", ""$1"", this)",
            [@"Debug\.LogWarning\s*\(\s*""([^""]*)""\s*,\s*([^)]+)\s*\)"] = @"Logger.LogWarning(""WARNING"", ""$1"", $2)",
            [@"Debug\.LogError\s*\(\s*""([^""]*)""\s*\)"] = @"Logger.LogError(""ERROR"", ""$1"", this)",
            [@"Debug\.LogError\s*\(\s*""([^""]*)""\s*,\s*([^)]+)\s*\)"] = @"Logger.LogError(""ERROR"", ""$1"", $2)",

            // Complex expressions
            [@"Debug\.Log\s*\(\s*([^;]+)\s*\)"] = @"Logger.Log(""INFO"", $1, this)",
            [@"Debug\.LogWarning\s*\(\s*([^;]+)\s*\)"] = @"Logger.LogWarning(""WARNING"", $1, this)",
            [@"Debug\.LogError\s*\(\s*([^;]+)\s*\)"] = @"Logger.LogError(""ERROR"", $1, this)",
        };

        private static readonly Dictionary<string, string> ResourcesLoadMigrations = new Dictionary<string, string>
        {
            [@"Resources\.Load<([^>]+)>\s*\(\s*""([^""]*)""\s*\)"] = @"await ServiceContainer.Resolve<IAssetManager>().LoadAssetAsync<$1>(""$2"")",
            [@"Resources\.Load\s*\(\s*""([^""]*)""\s*\)"] = @"await ServiceContainer.Resolve<IAssetManager>().LoadAssetAsync<Object>(""$1"")",
        };

        /// <summary>
        /// Migrate all anti-patterns in the codebase
        /// </summary>
        public static void MigrateAllAntiPatterns()
        {
            Logger.LogInfo("AntiPatternMigrationTool", "Starting comprehensive anti-pattern migration");

            var csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("Test") && !f.Contains("backup") && !f.Contains("AntiPatternMigrationTool"))
                .ToArray();

            int totalFiles = csFiles.Length;
            int migratedFiles = 0;

            foreach (var file in csFiles)
            {
                if (MigrateAntiPatternsInFile(file))
                {
                    migratedFiles++;
                }
            }

            Logger.LogInfo("AntiPatternMigrationTool", $"Migration complete: {migratedFiles}/{totalFiles} files migrated");
        }

        /// <summary>
        /// Migrate anti-patterns in a specific file
        /// </summary>
        public static bool MigrateAntiPatternsInFile(string filePath)
        {
            var originalContent = File.ReadAllText(filePath);
            var content = originalContent;
            bool wasModified = false;

            // 1. Migrate FindObjectOfType calls
            foreach (var migration in FindObjectOfTypeMigrations)
            {
                var oldPattern = migration.Key;
                var newPattern = migration.Value;

                if (content.Contains(oldPattern))
                {
                    content = content.Replace(oldPattern, newPattern);
                    wasModified = true;
                }
            }

            // 2. Migrate Debug.Log calls (only in non-editor files)
            if (!filePath.Contains("Editor") && !filePath.Contains("Test"))
            {
                foreach (var migration in DebugLogMigrations)
                {
                    var regex = new Regex(migration.Key);
                    if (regex.IsMatch(content))
                    {
                        content = regex.Replace(content, migration.Value);
                        wasModified = true;
                    }
                }
            }

            // 3. Migrate Resources.Load calls
            foreach (var migration in ResourcesLoadMigrations)
            {
                var regex = new Regex(migration.Key);
                if (regex.IsMatch(content))
                {
                    content = regex.Replace(content, migration.Value);
                    wasModified = true;

                    // Add async/await support if not present
                    if (!content.Contains("using System.Threading.Tasks;"))
                    {
                        content = AddUsingStatement(content, "using System.Threading.Tasks;");
                    }
                }
            }

            // 4. Add necessary using statements
            if (wasModified)
            {
                content = AddRequiredUsingStatements(content, filePath);
            }

            // Write back if modified
            if (wasModified)
            {
                // Create backup
                var backupPath = filePath + ".backup";
                if (!File.Exists(backupPath))
                {
                    File.Copy(filePath, backupPath);
                }

                File.WriteAllText(filePath, content);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Migrate Update() method to ITickable pattern
        /// </summary>
        public static bool MigrateUpdateMethodToTickable(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var originalContent = content;

            // Check if file has Update() method
            var updateMethodPattern = @"(private\s+|protected\s+|public\s+)?void\s+Update\s*\(\s*\)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}";
            var updateMatch = Regex.Match(content, updateMethodPattern, RegexOptions.Singleline);

            if (!updateMatch.Success) return false;

            var updateMethodBody = updateMatch.Groups[2].Value.Trim();

            // Replace Update() with ITickable pattern
            var tickableImplementation = $@"
    public int TickPriority => 100; // Adjust priority as needed
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {{
        {updateMethodBody.Replace("Time.deltaTime", "deltaTime")}
    }}

    private void Awake()
    {{
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }}

    private void OnDestroy()
    {{
        UpdateOrchestrator.Instance.UnregisterTickable(this);
    }}";

            // Remove the original Update method
            content = Regex.Replace(content, updateMethodPattern, "", RegexOptions.Singleline);

            // Add ITickable interface to class declaration if not present
            if (!content.Contains(": ITickable") && !content.Contains(",ITickable"))
            {
                content = Regex.Replace(content, @"(class\s+\w+\s*:\s*[^{]*?)(\{)", @"$1, ITickable$2");
            }

            // Add ITickable implementation
            content = Regex.Replace(content, @"(\}\s*$)", tickableImplementation + "\n}");

            // Add required using statements
            content = AddRequiredUsingStatements(content, filePath);

            if (content != originalContent)
            {
                // Create backup
                var backupPath = filePath + ".backup";
                if (!File.Exists(backupPath))
                {
                    File.Copy(filePath, backupPath);
                }

                File.WriteAllText(filePath, content);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add required using statements based on migrations
        /// </summary>
        private static string AddRequiredUsingStatements(string content, string filePath)
        {
            var requiredUsings = new List<string>();

            // Check what using statements are needed
            if (content.Contains("ServiceContainer.Resolve"))
            {
                requiredUsings.Add("using ProjectChimera.Core;");
            }

            if (content.Contains("ChimeraLogger."))
            {
                requiredUsings.Add("using ProjectChimera.Core.Logging;");
            }

            if (content.Contains("ITickable") || content.Contains("UpdateOrchestrator"))
            {
                requiredUsings.Add("using ProjectChimera.Core.Updates;");
            }

            if (content.Contains("LoadAssetAsync"))
            {
                requiredUsings.Add("using System.Threading.Tasks;");
            }

            // Add missing using statements
            foreach (var usingStatement in requiredUsings)
            {
                if (!content.Contains(usingStatement))
                {
                    content = AddUsingStatement(content, usingStatement);
                }
            }

            return content;
        }

        /// <summary>
        /// Add using statement to file
        /// </summary>
        private static string AddUsingStatement(string content, string usingStatement)
        {
            // Find the last using statement
            var usingPattern = @"^using\s+[^;]+;\s*$";
            var matches = Regex.Matches(content, usingPattern, RegexOptions.Multiline);

            if (matches.Count > 0)
            {
                var lastUsingMatch = matches[matches.Count - 1];
                var insertPosition = lastUsingMatch.Index + lastUsingMatch.Length;
                content = content.Insert(insertPosition, "\n" + usingStatement);
            }
            else
            {
                // No using statements found, add at the beginning
                content = usingStatement + "\n" + content;
            }

            return content;
        }

        /// <summary>
        /// Count remaining violations after migration
        /// </summary>
        public static AntiPatternViolationReport GetViolationReport()
        {
            var report = new AntiPatternViolationReport();
            var csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("Test") && !f.Contains("backup"))
                .ToArray();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);

                // Count FindObjectOfType
                var findObjectMatches = Regex.Matches(content, @"FindObjectOfType\s*<");
                report.FindObjectOfTypeViolations += findObjectMatches.Count;

                // Count ChimeraLogger.LogInfo("AntiPatternMigrationTool", $1)
                if (!file.Contains("Editor") && !file.Contains("Test"))
                {
                    var debugLogMatches = Regex.Matches(content, @"Debug\.Log");
                    report.DebugLogViolations += debugLogMatches.Count;
                }

                // Count Resources.Load
                var resourcesLoadMatches = Regex.Matches(content, @"Resources\.Load");
                report.ResourcesLoadViolations += resourcesLoadMatches.Count;

                // Count Update() methods
                var updateMatches = Regex.Matches(content, @"void\s+Update\s*\(\s*\)");
                report.UpdateMethodViolations += updateMatches.Count;
            }

            return report;
        }

        /// <summary>
        /// Restore files from backups
        /// </summary>
        public static void RestoreFromBackups()
        {
            var backupFiles = Directory.GetFiles(Application.dataPath, "*.backup", SearchOption.AllDirectories);

            foreach (var backupFile in backupFiles)
            {
                var originalFile = backupFile.Replace(".backup", "");
                if (File.Exists(originalFile))
                {
                    File.Copy(backupFile, originalFile, true);
                    File.Delete(backupFile);
                }
            }

            Logger.LogInfo("AntiPatternMigrationTool", $"Restored {backupFiles.Length} files from backups");
        }
    }

    /// <summary>
    /// Anti-pattern violation report
    /// </summary>
    [System.Serializable]
    public class AntiPatternViolationReport
    {
        public int FindObjectOfTypeViolations;
        public int DebugLogViolations;
        public int ResourcesLoadViolations;
        public int UpdateMethodViolations;

        public int TotalViolations => FindObjectOfTypeViolations + DebugLogViolations +
                                     ResourcesLoadViolations + UpdateMethodViolations;

        public bool IsCompliant => TotalViolations == 0;

        public override string ToString()
        {
            return $"Anti-Pattern Violations:\n" +
                   $"- FindObjectOfType: {FindObjectOfTypeViolations}\n" +
                   $"- Debug.Log: {DebugLogViolations}\n" +
                   $"- Resources.Load: {ResourcesLoadViolations}\n" +
                   $"- Update() Methods: {UpdateMethodViolations}\n" +
                   $"Total: {TotalViolations}\n" +
                   $"Compliant: {(IsCompliant ? "✅ YES" : "❌ NO")}";
        }
    }
}
