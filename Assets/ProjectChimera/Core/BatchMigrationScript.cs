using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Batch migration script to accelerate FindObjectOfType to DependencyResolutionHelper migration
    /// This is a one-time tool to complete Phase 0/1 anti-pattern elimination
    /// </summary>
    public class BatchMigrationScript : MonoBehaviour
    {
        [ContextMenu("Run Batch Migration")]
        public void RunBatchMigration()
        {
            string projectPath = Application.dataPath + "/ProjectChimera";
            int migratedFiles = 0;
            int totalPatterns = 0;

            ChimeraLogger.LogInfo("MIGRATION", "Starting batch FindObjectOfType migration...", this);

            // Common migration patterns
            var patterns = new[]
            {
                new MigrationPattern(
                    @"(\w+)\s*=\s*FindObjectOfType<(\w+)>\(\);",
                    "$1 = DependencyResolutionHelper.SafeResolve<$2>(this, \"CORE\");"
                ),
                new MigrationPattern(
                    @"(\w+)\s*=\s*FindObjectOfType<(\w+)>\(\);\s*if\s*\(\1\s*==\s*null\)",
                    "$1 = DependencyResolutionHelper.SafeResolve<$2>(this, \"CORE\");\n\n            if ($1 == null)"
                ),
                new MigrationPattern(
                    @"var\s+(\w+)\s*=\s*FindObjectOfType<(\w+)>\(\);",
                    "var $1 = DependencyResolutionHelper.SafeResolve<$2>(this, \"CORE\");"
                ),
                new MigrationPattern(
                    @"(\w+)\s*=\s*FindObjectOfType<(\w+)>\(\);\s*//",
                    "$1 = DependencyResolutionHelper.SafeResolve<$2>(this, \"CORE\"); //"
                )
            };

            // Find all .cs files with FindObjectOfType
            string[] files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                // Skip certain files
                if (file.Contains("DependencyResolutionHelper.cs") ||
                    file.Contains("AntiPatternMigrationTool.cs") ||
                    file.Contains("QualityGates.cs") ||
                    file.Contains("BatchMigrationScript.cs") ||
                    file.Contains("Editor/") ||
                    file.Contains("Testing/"))
                {
                    continue;
                }

                string content = File.ReadAllText(file);
                string originalContent = content;
                bool hasChanges = false;

                // Apply each pattern
                foreach (var pattern in patterns)
                {
                    if (Regex.IsMatch(content, pattern.Pattern))
                    {
                        content = Regex.Replace(content, pattern.Pattern, pattern.Replacement);
                        hasChanges = true;
                        totalPatterns++;
                    }
                }

                // Add using statement if we made changes and it's not already there
                if (hasChanges && !content.Contains("using ProjectChimera.Core;"))
                {
                    // Find the last using statement and add our using after it
                    var lastUsingMatch = Regex.Match(content, @"using\s+[^;]+;\s*\n");
                    if (lastUsingMatch.Success)
                    {
                        int insertPos = lastUsingMatch.Index + lastUsingMatch.Length;
                        content = content.Insert(insertPos, "using ProjectChimera.Core;\n");
                    }
                }

                // Write back if changed
                if (hasChanges)
                {
                    File.WriteAllText(file, content);
                    migratedFiles++;
                    ChimeraLogger.LogInfo("MIGRATION", $"Migrated: {Path.GetFileName(file)}", this);
                }
            }

            ChimeraLogger.LogInfo("MIGRATION", $"Batch migration complete: {migratedFiles} files, {totalPatterns} patterns migrated", this);
        }

        private struct MigrationPattern
        {
            public string Pattern;
            public string Replacement;

            public MigrationPattern(string pattern, string replacement)
            {
                Pattern = pattern;
                Replacement = replacement;
            }
        }
    }
}