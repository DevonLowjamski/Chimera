using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// CRITICAL: Automated Debug.Log migration tool for Project Chimera
    /// Eliminates 1,866 Debug.Log violations by migrating to ChimeraLogger
    /// Based on the Phase 0 roadmap requirements
    /// </summary>
    public class DebugLogAutoMigrationTool : EditorWindow
    {
        [MenuItem("Chimera/Debug.Log Migration/Auto Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<DebugLogAutoMigrationTool>("Debug.Log Auto Migration");
            window.minSize = new Vector2(500, 400);
        }

        private Vector2 _scrollPosition;
        private bool _dryRun = true;
        private bool _createBackups = true;
        private List<string> _migrationLog = new List<string>();
        private Dictionary<string, int> _violationCounts = new Dictionary<string, int>();

        // System-specific category mappings
        private readonly Dictionary<string, string> _systemCategories = new Dictionary<string, string>
        {
            ["Core"] = "CORE",
            ["Systems/Cultivation"] = "CULTIVATION",
            ["Systems/Construction"] = "CONSTRUCTION",
            ["Systems/Genetics"] = "GENETICS",
            ["Systems/Save"] = "SAVE",
            ["Systems/Facilities"] = "FACILITIES",
            ["Systems/Gameplay"] = "GAMEPLAY",
            ["Systems/Rendering"] = "RENDERING",
            ["Systems/Audio"] = "AUDIO",
            ["Systems/Diagnostics"] = "DIAGNOSTICS",
            ["Systems/Scene"] = "SCENE",
            ["Systems/Services"] = "SERVICES",
            ["Editor"] = "EDITOR",
            ["Testing"] = "TEST"
        };

        private void OnGUI()
        {
            GUILayout.Label("Debug.Log Migration Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Configuration
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Configuration", EditorStyles.boldLabel);
            _dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", _dryRun);
            _createBackups = EditorGUILayout.Toggle("Create Backups", _createBackups);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Violations", GUILayout.Height(30)))
            {
                ScanForViolations();
            }
            if (GUILayout.Button("Migrate All Debug.Log", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Migration",
                    $"This will migrate {_violationCounts.Values.Sum()} Debug.Log calls to ChimeraLogger.\n\n" +
                    (_dryRun ? "DRY RUN: No files will be modified." : "WARNING: This will modify files!"),
                    "Proceed", "Cancel"))
                {
                    MigrateAllDebugCalls();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Results display
            if (_violationCounts.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Violation Summary", EditorStyles.boldLabel);
                foreach (var kvp in _violationCounts)
                {
                    EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value} violations");
                }
                EditorGUILayout.LabelField($"Total: {_violationCounts.Values.Sum()} violations", EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            // Migration log
            if (_migrationLog.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Migration Log", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                foreach (var logEntry in _migrationLog)
                {
                    EditorGUILayout.LabelField(logEntry, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        private void ScanForViolations()
        {
            _violationCounts.Clear();
            _migrationLog.Clear();
            _migrationLog.Add("=== SCANNING FOR DEBUG.LOG VIOLATIONS ===");

            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(".backup"))
                .ToArray();

            foreach (var file in csFiles)
            {
                var violations = CountDebugLogViolations(file);
                if (violations > 0)
                {
                    var category = DetermineSystemCategory(file);
                    if (!_violationCounts.ContainsKey(category))
                        _violationCounts[category] = 0;
                    _violationCounts[category] += violations;

                    _migrationLog.Add($"{file}: {violations} violations [{category}]");
                }
            }

            _migrationLog.Add($"=== SCAN COMPLETE: {_violationCounts.Values.Sum()} total violations ===");
            Repaint();
        }

        private int CountDebugLogViolations(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var patterns = new[]
            {
                @"Debug\.Log\s*\(",
                @"Debug\.LogWarning\s*\(",
                @"Debug\.LogError\s*\("
            };

            return patterns.Sum(pattern => Regex.Matches(content, pattern).Count);
        }

        private string DetermineSystemCategory(string filePath)
        {
            // Normalize path separators
            var normalizedPath = filePath.Replace('\\', '/');

            foreach (var mapping in _systemCategories)
            {
                if (normalizedPath.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            return "OTHER";
        }

        private void MigrateAllDebugCalls()
        {
            _migrationLog.Clear();
            _migrationLog.Add($"=== STARTING MIGRATION (DRY RUN: {_dryRun}) ===");

            var csFiles = Directory.GetFiles("Assets/ProjectChimera", "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(".backup"))
                .ToArray();

            int totalMigrated = 0;
            int filesModified = 0;

            foreach (var file in csFiles)
            {
                var migrated = MigrateDebugCallsInFile(file);
                if (migrated > 0)
                {
                    totalMigrated += migrated;
                    filesModified++;
                    _migrationLog.Add($"✓ {file}: {migrated} calls migrated");
                }
            }

            _migrationLog.Add($"=== MIGRATION COMPLETE ===");
            _migrationLog.Add($"Files modified: {filesModified}");
            _migrationLog.Add($"Total calls migrated: {totalMigrated}");

            if (!_dryRun)
            {
                AssetDatabase.Refresh();
                _migrationLog.Add("AssetDatabase refreshed");
            }

            Repaint();
        }

        private int MigrateDebugCallsInFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var originalContent = content;
            var category = DetermineSystemCategory(filePath);
            int migrationsCount = 0;

            // Create backup if enabled and not dry run
            if (_createBackups && !_dryRun)
            {
                File.WriteAllText(filePath + ".backup", originalContent);
            }

            // Add ChimeraLogger using statement if not present
            if (!content.Contains("using ProjectChimera.Core.Logging;"))
            {
                // Find the last using statement or namespace declaration
                var lines = content.Split('\n');
                int insertIndex = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("using ") || lines[i].Trim().StartsWith("namespace"))
                    {
                        insertIndex = i + 1;
                    }
                    else if (lines[i].Trim().StartsWith("namespace"))
                    {
                        break;
                    }
                }

                if (insertIndex > 0 && insertIndex < lines.Length)
                {
                    var newLines = lines.ToList();
                    newLines.Insert(insertIndex, "using ProjectChimera.Core.Logging;");
                    content = string.Join("\n", newLines);
                }
            }

            // Migration patterns with category-specific replacement
            var migrations = new[]
            {
                // Simple Debug.Log with string literal
                new MigrationPattern
                {
                    Pattern = @"Debug\.Log\s*\(\s*""([^""]*)""\s*\)",
                    Replacement = $"ChimeraLogger.Log(\"{category}\", \"$1\", this)"
                },
                // Debug.Log with string interpolation or variables
                new MigrationPattern
                {
                    Pattern = @"Debug\.Log\s*\(\s*([^)]+)\s*\)",
                    Replacement = $"ChimeraLogger.Log(\"{category}\", $1, this)"
                },
                // Debug.LogWarning
                new MigrationPattern
                {
                    Pattern = @"Debug\.LogWarning\s*\(\s*""([^""]*)""\s*\)",
                    Replacement = $"ChimeraLogger.LogWarning(\"{category}\", \"$1\", this)"
                },
                new MigrationPattern
                {
                    Pattern = @"Debug\.LogWarning\s*\(\s*([^)]+)\s*\)",
                    Replacement = $"ChimeraLogger.LogWarning(\"{category}\", $1, this)"
                },
                // Debug.LogError
                new MigrationPattern
                {
                    Pattern = @"Debug\.LogError\s*\(\s*""([^""]*)""\s*\)",
                    Replacement = $"ChimeraLogger.LogError(\"{category}\", \"$1\", this)"
                },
                new MigrationPattern
                {
                    Pattern = @"Debug\.LogError\s*\(\s*([^)]+)\s*\)",
                    Replacement = $"ChimeraLogger.LogError(\"{category}\", $1, this)"
                }
            };

            foreach (var migration in migrations)
            {
                var matches = Regex.Matches(content, migration.Pattern);
                if (matches.Count > 0)
                {
                    content = Regex.Replace(content, migration.Pattern, migration.Replacement);
                    migrationsCount += matches.Count;
                }
            }

            // Only write if content changed and not dry run
            if (content != originalContent && !_dryRun)
            {
                File.WriteAllText(filePath, content);
            }

            return migrationsCount;
        }

        private class MigrationPattern
        {
            public string Pattern { get; set; }
            public string Replacement { get; set; }
        }
    }
}