using UnityEngine;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Execute anti-pattern migration for roadmap compliance
    /// </summary>
    public class ExecuteAntiPatternMigration : MonoBehaviour
    {
        [Header("Migration Settings")]
        [SerializeField] private bool executeOnAwake = false;
        [SerializeField] private bool generateReportOnly = true;

        private void Awake()
        {
            if (executeOnAwake)
            {
                ExecuteMigration();
            }
        }

        /// <summary>
        /// Execute the anti-pattern migration process
        /// </summary>
        [ContextMenu("Execute Migration")]
        public void ExecuteMigration()
        {
            Logger.LogInfo("ExecuteAntiPatternMigration", "Starting anti-pattern migration process");

            // Get initial violation report
            var initialReport = AntiPatternMigrationTool.GetViolationReport();
            Logger.LogInfo("ExecuteAntiPatternMigration", $"Initial violations:\n{initialReport}");

            if (!generateReportOnly)
            {
                // Execute the migration
                AntiPatternMigrationTool.MigrateAllAntiPatterns();

                // Get post-migration report
                var finalReport = AntiPatternMigrationTool.GetViolationReport();
                Logger.LogInfo("ExecuteAntiPatternMigration", $"Post-migration violations:\n{finalReport}");

                // Calculate improvement
                var improvement = initialReport.TotalViolations - finalReport.TotalViolations;
                Logger.LogInfo("ExecuteAntiPatternMigration", $"Violations eliminated: {improvement}");

                if (finalReport.IsCompliant)
                {
                    Logger.LogInfo("ExecuteAntiPatternMigration", "🎉 PROJECT CHIMERA IS NOW ROADMAP COMPLIANT!");
                }
                else
                {
                    Logger.LogWarning("ExecuteAntiPatternMigration", $"⚠️ Still have {finalReport.TotalViolations} violations remaining");
                }
            }
        }

        /// <summary>
        /// Generate violation report only
        /// </summary>
        [ContextMenu("Generate Report")]
        public void GenerateReport()
        {
            var report = AntiPatternMigrationTool.GetViolationReport();
            Logger.LogInfo("ExecuteAntiPatternMigration", $"Current violations:\n{report}");
        }

        /// <summary>
        /// Restore from backups if needed
        /// </summary>
        [ContextMenu("Restore Backups")]
        public void RestoreBackups()
        {
            AntiPatternMigrationTool.RestoreFromBackups();
            Logger.LogInfo("ExecuteAntiPatternMigration", "Restored from backups");
        }
    }
}
