using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// TIER 2 FILE REFACTORING TOOL
    /// Automated refactoring for files between 550-650 lines
    /// Follows established patterns from Tier 1 refactoring
    /// </summary>
    public class Tier2FileRefactoringTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<FileRefactoringTask> _tier2Files = new List<FileRefactoringTask>();
        private int _currentFileIndex = 0;
        private bool _isAnalyzing = false;
        private bool _autoRefactor = false;

        [MenuItem("Chimera/Refactoring/Tier 2 File Refactoring")]
        public static void ShowWindow()
        {
            var window = GetWindow<Tier2FileRefactoringTool>("Tier 2 Refactoring");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            AnalyzeTier2Files();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TIER 2 FILE REFACTORING TOOL", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Target: 20 files (550-650 lines) → <500 lines per file", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Analysis", GUILayout.Height(30)))
            {
                AnalyzeTier2Files();
            }
            if (GUILayout.Button("Generate Refactoring Report", GUILayout.Height(30)))
            {
                GenerateRefactoringReport();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Files Identified: {_tier2Files.Count}/20", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _tier2Files.Count; i++)
            {
                var file = _tier2Files[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{i + 1}. {file.FileName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Lines: {file.LineCount} | Status: {file.Status}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Path: {file.RelativePath}", EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(file.RefactoringPlan))
                {
                    EditorGUILayout.LabelField("Suggested Refactoring:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.TextArea(file.RefactoringPlan, GUILayout.Height(60));
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Analyze Structure", GUILayout.Height(25)))
                {
                    AnalyzeFileStructure(file);
                }
                if (GUILayout.Button("Generate Component Files", GUILayout.Height(25)))
                {
                    GenerateComponentFiles(file);
                }
                GUI.enabled = file.Status == "Refactored";
                if (GUILayout.Button("Validate", GUILayout.Height(25)))
                {
                    ValidateRefactoring(file);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _autoRefactor = EditorGUILayout.Toggle("Auto-Refactor All", _autoRefactor);
            GUI.enabled = _autoRefactor;
            if (GUILayout.Button("REFACTOR ALL TIER 2 FILES", GUILayout.Height(40)))
            {
                RefactorAllFiles();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void AnalyzeTier2Files()
        {
            _tier2Files.Clear();
            _isAnalyzing = true;

            var projectPath = "Assets/ProjectChimera";
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            foreach (var filePath in csFiles)
            {
                var lineCount = File.ReadAllLines(filePath).Length;

                // Tier 2: 550-650 lines
                if (lineCount >= 550 && lineCount <= 650)
                {
                    var task = new FileRefactoringTask
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        RelativePath = filePath.Replace(Application.dataPath, "Assets"),
                        LineCount = lineCount,
                        Status = "Pending"
                    };

                    _tier2Files.Add(task);
                }
            }

            // Sort by line count (descending)
            _tier2Files = _tier2Files.OrderByDescending(f => f.LineCount).Take(20).ToList();

            _isAnalyzing = false;
            Debug.Log($"✅ Tier 2 Analysis Complete: {_tier2Files.Count} files identified");
        }

        private void AnalyzeFileStructure(FileRefactoringTask file)
        {
            var content = File.ReadAllText(file.FilePath);
            var lines = File.ReadAllLines(file.FilePath);

            // Count methods, classes, structs
            var methodCount = Regex.Matches(content, @"(public|private|protected|internal)\s+\w+\s+\w+\s*\(").Count;
            var classCount = Regex.Matches(content, @"class\s+\w+").Count;
            var structCount = Regex.Matches(content, @"struct\s+\w+").Count;
            var interfaceCount = Regex.Matches(content, @"interface\s+\w+").Count;

            file.MethodCount = methodCount;
            file.ClassCount = classCount;
            file.StructCount = structCount;

            // Generate refactoring plan
            var plan = $"📊 Analysis:\n";
            plan += $"- Methods: {methodCount}\n";
            plan += $"- Classes: {classCount}\n";
            plan += $"- Structs: {structCount}\n";
            plan += $"- Interfaces: {interfaceCount}\n\n";

            plan += $"📝 Suggested Split:\n";

            if (structCount >= 5)
            {
                plan += $"1. DataStructures.cs (~{structCount * 20} lines)\n";
            }

            var estimatedOperationsLines = methodCount * 15;
            var componentsNeeded = Mathf.CeilToInt((float)file.LineCount / 350f);

            plan += $"2. Operations/Logic components ({componentsNeeded-1} files)\n";
            plan += $"3. Coordinator class (~200 lines)\n";

            file.RefactoringPlan = plan;

            Debug.Log($"✅ Analyzed: {file.FileName} - {methodCount} methods, {structCount} structs");
        }

        private void GenerateComponentFiles(FileRefactoringTask file)
        {
            Debug.Log($"🔨 Generating component files for: {file.FileName}");

            var content = File.ReadAllText(file.FilePath);
            var directory = Path.GetDirectoryName(file.FilePath);
            var baseFileName = Path.GetFileNameWithoutExtension(file.FilePath);
            var lines = File.ReadAllLines(file.FilePath);

            // Extract namespace
            var namespaceMatch = Regex.Match(content, @"namespace\s+([\w\.]+)");
            var namespaceName = namespaceMatch.Success ? namespaceMatch.Groups[1].Value : "ProjectChimera";

            // Extract using statements
            var usingStatements = new List<string>();
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("using "))
                {
                    usingStatements.Add(line.Trim());
                }
                else if (!string.IsNullOrWhiteSpace(line.Trim()) && !line.Trim().StartsWith("//"))
                {
                    break;
                }
            }

            // 1. Extract data structures
            if (file.StructCount >= 3)
            {
                var dataStructuresFile = Path.Combine(directory, $"{baseFileName}DataStructures.cs");
                GenerateDataStructuresFile(dataStructuresFile, content, namespaceName, usingStatements);
                Debug.Log($"   ✓ Created: {baseFileName}DataStructures.cs");
            }

            // 2. Mark original as backup
            var backupPath = file.FilePath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(file.FilePath, backupPath);
                Debug.Log($"   ✓ Backup created: {Path.GetFileName(backupPath)}");
            }

            file.Status = "Components Generated";
            Debug.Log($"✅ Component generation complete for: {file.FileName}");
        }

        private void GenerateDataStructuresFile(string outputPath, string content, string namespaceName, List<string> usingStatements)
        {
            var structs = ExtractStructs(content);
            var enums = ExtractEnums(content);

            if (structs.Count == 0 && enums.Count == 0) return;

            var fileContent = "// REFACTORED: Data Structures\n";
            fileContent += "// Extracted from original file for better separation of concerns\n\n";

            foreach (var usingStatement in usingStatements.Distinct())
            {
                fileContent += usingStatement + "\n";
            }

            fileContent += $"\nnamespace {namespaceName}\n{{\n";

            // Add structs
            foreach (var structDef in structs)
            {
                fileContent += "    " + structDef.Replace("\n", "\n    ") + "\n\n";
            }

            // Add enums
            foreach (var enumDef in enums)
            {
                fileContent += "    " + enumDef.Replace("\n", "\n    ") + "\n\n";
            }

            fileContent += "}\n";

            File.WriteAllText(outputPath, fileContent);
        }

        private List<string> ExtractStructs(string content)
        {
            var structs = new List<string>();
            var structMatches = Regex.Matches(content, @"(///.*?\n\s*)*(\[.*?\]\s*)*(public|private|internal)?\s+struct\s+\w+\s*\{[^}]*(?:\{[^}]*\}[^}]*)?\}", RegexOptions.Singleline);

            foreach (Match match in structMatches)
            {
                structs.Add(match.Value.Trim());
            }

            return structs;
        }

        private List<string> ExtractEnums(string content)
        {
            var enums = new List<string>();
            var enumMatches = Regex.Matches(content, @"(///.*?\n\s*)*(\[.*?\]\s*)*(public|private|internal)?\s+enum\s+\w+\s*\{[^}]*\}", RegexOptions.Singleline);

            foreach (Match match in enumMatches)
            {
                enums.Add(match.Value.Trim());
            }

            return enums;
        }

        private void ValidateRefactoring(FileRefactoringTask file)
        {
            var directory = Path.GetDirectoryName(file.FilePath);
            var baseFileName = Path.GetFileNameWithoutExtension(file.FilePath);

            // Check for component files
            var dataStructuresFile = Path.Combine(directory, $"{baseFileName}DataStructures.cs");
            var hasDataStructures = File.Exists(dataStructuresFile);

            // Validate line counts
            var originalBackup = file.FilePath + ".backup";
            if (File.Exists(originalBackup))
            {
                var originalLines = File.ReadAllLines(originalBackup).Length;
                var currentLines = File.ReadAllLines(file.FilePath).Length;

                if (currentLines < 500)
                {
                    file.Status = "✅ Validated";
                    Debug.Log($"✅ {file.FileName}: {originalLines} → {currentLines} lines (PASS)");
                }
                else
                {
                    file.Status = "⚠️ Still Oversized";
                    Debug.LogWarning($"⚠️ {file.FileName}: {currentLines} lines (still > 500)");
                }
            }
        }

        private void RefactorAllFiles()
        {
            EditorUtility.DisplayProgressBar("Tier 2 Refactoring", "Starting batch refactoring...", 0f);

            for (int i = 0; i < _tier2Files.Count; i++)
            {
                var file = _tier2Files[i];
                var progress = (float)(i + 1) / _tier2Files.Count;

                EditorUtility.DisplayProgressBar("Tier 2 Refactoring",
                    $"Processing: {file.FileName} ({i + 1}/{_tier2Files.Count})", progress);

                AnalyzeFileStructure(file);
                GenerateComponentFiles(file);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"✅ Batch refactoring complete: {_tier2Files.Count} files processed");
        }

        private void GenerateRefactoringReport()
        {
            var reportPath = Path.Combine(Application.dataPath, "../Documents/Tier2_Refactoring_Report.md");
            var report = "# TIER 2 FILE REFACTORING REPORT\n\n";
            report += $"**Date**: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"**Files Identified**: {_tier2Files.Count}\n";
            report += $"**Target**: All files <500 lines\n\n";

            report += "## Files to Refactor\n\n";
            report += "| # | File | Lines | Status | Plan |\n";
            report += "|---|------|-------|--------|------|\n";

            for (int i = 0; i < _tier2Files.Count; i++)
            {
                var file = _tier2Files[i];
                var planSummary = file.RefactoringPlan?.Replace("\n", " ").Substring(0, Mathf.Min(50, file.RefactoringPlan?.Length ?? 0)) ?? "Not analyzed";
                report += $"| {i + 1} | `{file.FileName}` | {file.LineCount} | {file.Status} | {planSummary}... |\n";
            }

            report += "\n## Refactoring Strategy\n\n";
            report += "Each file will be split into:\n";
            report += "1. **DataStructures.cs** - All structs, enums, and data classes\n";
            report += "2. **Operations/Logic components** - Specific functional areas\n";
            report += "3. **Coordinator class** - Main class delegating to components\n\n";

            report += "## Progress Tracking\n\n";
            var pending = _tier2Files.Count(f => f.Status == "Pending");
            var generated = _tier2Files.Count(f => f.Status == "Components Generated");
            var validated = _tier2Files.Count(f => f.Status.Contains("Validated"));

            report += $"- Pending: {pending}\n";
            report += $"- Components Generated: {generated}\n";
            report += $"- Validated: {validated}\n";
            report += $"- Completion: {(validated * 100 / Mathf.Max(1, _tier2Files.Count))}%\n";

            File.WriteAllText(reportPath, report);
            Debug.Log($"✅ Report generated: {reportPath}");
            System.Diagnostics.Process.Start(reportPath);
        }
    }

    [System.Serializable]
    public class FileRefactoringTask
    {
        public string FilePath;
        public string FileName;
        public string RelativePath;
        public int LineCount;
        public string Status = "Pending";
        public int MethodCount;
        public int ClassCount;
        public int StructCount;
        public string RefactoringPlan;
    }
}

