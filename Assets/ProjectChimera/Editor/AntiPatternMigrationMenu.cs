using UnityEngine;
using UnityEditor;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Editor menu items for anti-pattern migration
    /// </summary>
    public static class AntiPatternMigrationMenu
    {
        [MenuItem("Project Chimera/Roadmap Compliance/Generate Violation Report")]
        public static void GenerateViolationReport()
        {
            ChimeraLogger.LogInfo("EDITOR", "Generating violation report...");
            
            var report = AntiPatternMigrationTool.GetViolationReport();
            
            ChimeraLogger.LogInfo("EDITOR", "Violation report generated");
            ChimeraLogger.LogInfo("EDITOR", $"Found {report.TotalViolations} violations");
            
            if (report.IsCompliant)
            {
                ChimeraLogger.LogInfo("EDITOR", "✅ Project is fully compliant with roadmap requirements");
            }
            else
            {
                ChimeraLogger.LogWarning("EDITOR", $"❌ Found {report.TotalViolations} violations that need to be addressed");
            }
        }

        [MenuItem("Project Chimera/Roadmap Compliance/Execute Migration (CAREFUL!)")]
        public static void ExecuteMigration()
        {
            if (EditorUtility.DisplayDialog("Execute Anti-Pattern Migration", 
                "This will modify ALL C# files in the project. Backups will be created. Continue?", 
                "Yes, Execute", "Cancel"))
            {
                ChimeraLogger.LogInfo("EDITOR", "🚀 Starting anti-pattern migration...");

                // Get initial report
                var initialReport = AntiPatternMigrationTool.GetViolationReport();
                ChimeraLogger.LogInfo("EDITOR", $"Initial violations: {initialReport.TotalViolations}");
                
                // Execute migration
                AntiPatternMigrationTool.MigrateAllAntiPatterns();
                
                // Get final report
                var finalReport = AntiPatternMigrationTool.GetViolationReport();
                ChimeraLogger.LogInfo("EDITOR", $"Final violations: {finalReport.TotalViolations}");

                var improvement = initialReport.TotalViolations - finalReport.TotalViolations;
                ChimeraLogger.LogInfo("EDITOR", $"✨ Migration reduced violations by {improvement}");

                if (finalReport.IsCompliant)
                {
                    ChimeraLogger.LogInfo("EDITOR", "🎯 Migration completed successfully - project is now compliant!");
                }
                
                // Refresh assets
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Project Chimera/Roadmap Compliance/Restore from Backups")]
        public static void RestoreFromBackups()
        {
            if (EditorUtility.DisplayDialog("Restore from Backups", 
                "This will restore all files from their .backup versions. Continue?", 
                "Yes, Restore", "Cancel"))
            {
                AntiPatternMigrationTool.RestoreFromBackups();
                AssetDatabase.Refresh();
                ChimeraLogger.LogInfo("EDITOR", "Files restored from backups successfully");
            }
        }
    }
}