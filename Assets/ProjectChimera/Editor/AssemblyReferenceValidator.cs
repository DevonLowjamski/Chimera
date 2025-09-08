using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Comprehensive assembly reference validation tool for Project Chimera.
    /// Validates all assembly definitions and their dependencies after major system changes.
    /// </summary>
    public class AssemblyReferenceValidator : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<AssemblyValidationResult> _validationResults = new List<AssemblyValidationResult>();
        private bool _isValidating = false;
        private int _totalAssemblies = 0;
        private int _validatedAssemblies = 0;
        private int _issuesFound = 0;
        private int _issuesFixed = 0;
        
        [MenuItem("Project Chimera/Assembly Reference Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssemblyReferenceValidator>("Assembly Reference Validator");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Assembly Reference Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Control buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate All Assemblies", GUILayout.Height(30)))
            {
                ValidateAllAssemblies();
            }
            if (GUILayout.Button("Quick Validation", GUILayout.Height(30)))
            {
                QuickValidation();
            }
            if (GUILayout.Button("Fix All Issues", GUILayout.Height(30)))
            {
                FixAllIssues();
            }
            if (GUILayout.Button("Generate Assembly Report", GUILayout.Height(30)))
            {
                GenerateAssemblyReport();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Status information
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Total Assemblies: {_totalAssemblies}", GUILayout.Width(150));
            GUILayout.Label($"Validated: {_validatedAssemblies}", GUILayout.Width(100));
            GUILayout.Label($"Issues Found: {_issuesFound}", GUILayout.Width(100));
            GUILayout.Label($"Issues Fixed: {_issuesFixed}", GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            if (_isValidating)
            {
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)), 
                    (float)_validatedAssemblies / _totalAssemblies, "Validating assemblies...");
            }
            
            GUILayout.Space(10);
            
            // Results display
            if (_validationResults.Count > 0)
            {
                GUILayout.Label($"Validation Results ({_validationResults.Count} assemblies):", EditorStyles.boldLabel);
                
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                
                foreach (var result in _validationResults)
                {
                    DrawValidationResult(result);
                }
                
                GUILayout.EndScrollView();
            }
            else if (!_isValidating)
            {
                GUILayout.Label("No validation results. Click 'Validate All Assemblies' to start.", 
                    EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        private void DrawValidationResult(AssemblyValidationResult result)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            GUILayout.BeginHorizontal();
            
            var statusColor = result.IsValid ? Color.green : Color.red;
            if (result.HasWarnings) statusColor = Color.yellow;
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"[{(result.IsValid ? "VALID" : "INVALID")}]", GUILayout.Width(80));
            GUI.color = originalColor;
            
            GUILayout.Label(result.AssemblyName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (result.HasAutoFixableIssues && GUILayout.Button("Fix", GUILayout.Width(50)))
            {
                FixAssemblyIssues(result);
            }
            
            GUILayout.EndHorizontal();
            
            // Assembly details
            GUILayout.Label($"Path: {result.AssemblyPath}");
            GUILayout.Label($"References: {result.ReferencesCount} | Missing: {result.MissingReferences.Count} | Circular: {result.CircularReferences.Count}");
            
            // Issues
            if (result.Issues.Count > 0)
            {
                GUILayout.Label("Issues:", EditorStyles.boldLabel);
                foreach (var issue in result.Issues)
                {
                    var issueColor = issue.Severity switch
                    {
                        IssueSeverity.Error => Color.red,
                        IssueSeverity.Warning => Color.yellow,
                        _ => Color.white
                    };
                    
                    GUI.color = issueColor;
                    GUILayout.Label($"  • [{issue.Severity}] {issue.Message}");
                    GUI.color = originalColor;
                }
            }
            
            // Missing references
            if (result.MissingReferences.Count > 0)
            {
                GUILayout.Label("Missing References:", EditorStyles.boldLabel);
                foreach (var missing in result.MissingReferences)
                {
                    GUILayout.Label($"  • {missing}", EditorStyles.wordWrappedMiniLabel);
                }
            }
            
            // Circular references
            if (result.CircularReferences.Count > 0)
            {
                GUILayout.Label("Circular References:", EditorStyles.boldLabel);
                foreach (var circular in result.CircularReferences)
                {
                    GUILayout.Label($"  • {circular}", EditorStyles.wordWrappedMiniLabel);
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        private void ValidateAllAssemblies()
        {
            _isValidating = true;
            _validationResults.Clear();
            _totalAssemblies = 0;
            _validatedAssemblies = 0;
            _issuesFound = 0;
            _issuesFixed = 0;
            
            try
            {
                // Find all ProjectChimera assembly definition files
                var asmdefPaths = Directory.GetFiles("Assets/ProjectChimera", "*.asmdef", SearchOption.AllDirectories);
                _totalAssemblies = asmdefPaths.Length;
                
                ChimeraLogger.Log($"[Assembly Validator] Starting validation of {_totalAssemblies} assemblies");
                
                // Create dependency graph
                var dependencyGraph = BuildDependencyGraph(asmdefPaths);
                
                // Validate each assembly
                foreach (var path in asmdefPaths)
                {
                    var result = ValidateAssembly(path, dependencyGraph);
                    _validationResults.Add(result);
                    _validatedAssemblies++;
                    
                    if (!result.IsValid || result.HasWarnings)
                    {
                        _issuesFound += result.Issues.Count;
                    }
                    
                    // Update UI
                    if (_validatedAssemblies % 5 == 0)
                    {
                        Repaint();
                    }
                }
                
                // Sort results by severity
                _validationResults = _validationResults.OrderBy(r => r.IsValid ? 1 : 0)
                    .ThenByDescending(r => r.Issues.Count).ToList();
                
                ChimeraLogger.Log($"[Assembly Validator] Validation complete: {_issuesFound} issues found across {_totalAssemblies} assemblies");
            }
            catch (System.Exception e)
            {
                ChimeraLogger.LogError($"[Assembly Validator] Validation failed: {e.Message}");
            }
            finally
            {
                _isValidating = false;
                Repaint();
            }
        }
        
        private void QuickValidation()
        {
            _isValidating = true;
            _validationResults.Clear();
            
            try
            {
                // Quick validation of critical assemblies only
                var criticalAssemblies = new[]
                {
                    "Assets/ProjectChimera/Core/ProjectChimera.Core.asmdef",
                    "Assets/ProjectChimera/Data/ProjectChimera.Data.asmdef",
                    "Assets/ProjectChimera/Systems/ProjectChimera.Systems.asmdef",
                    "Assets/ProjectChimera/Systems/IPM/ProjectChimera.Systems.IPM.asmdef",
                    "Assets/ProjectChimera/Systems/Gaming/ProjectChimera.Gaming.asmdef"
                };
                
                _totalAssemblies = criticalAssemblies.Length;
                var dependencyGraph = BuildDependencyGraph(criticalAssemblies);
                
                foreach (var path in criticalAssemblies)
                {
                    if (File.Exists(path))
                    {
                        var result = ValidateAssembly(path, dependencyGraph);
                        _validationResults.Add(result);
                    }
                    _validatedAssemblies++;
                }
                
                ChimeraLogger.Log($"[Assembly Validator] Quick validation complete");
            }
            finally
            {
                _isValidating = false;
                Repaint();
            }
        }
        
        private Dictionary<string, AssemblyDefinition> BuildDependencyGraph(string[] asmdefPaths)
        {
            var dependencyGraph = new Dictionary<string, AssemblyDefinition>();
            
            foreach (var path in asmdefPaths)
            {
                try
                {
                    var content = File.ReadAllText(path);
                    var asmdef = ParseAssemblyDefinition(content);
                    asmdef.Path = path;
                    var assemblyName = asmdef.name ?? Path.GetFileNameWithoutExtension(path);
                    
                    dependencyGraph[assemblyName] = asmdef;
                }
                catch (System.Exception e)
                {
                    ChimeraLogger.LogWarning($"[Assembly Validator] Failed to parse {path}: {e.Message}");
                }
            }
            
            return dependencyGraph;
        }
        
        private AssemblyValidationResult ValidateAssembly(string path, Dictionary<string, AssemblyDefinition> dependencyGraph)
        {
            var result = new AssemblyValidationResult
            {
                AssemblyPath = path,
                AssemblyName = Path.GetFileNameWithoutExtension(path),
                Issues = new List<AssemblyIssue>(),
                MissingReferences = new List<string>(),
                CircularReferences = new List<string>(),
                IsValid = true,
                HasWarnings = false
            };
            
            try
            {
                // Parse assembly definition
                var content = File.ReadAllText(path);
                var asmdef = ParseAssemblyDefinition(content);
                
                if (asmdef == null)
                {
                    result.AddIssue(IssueSeverity.Error, "Failed to parse assembly definition");
                    return result;
                }
                
                result.AssemblyName = asmdef.name ?? result.AssemblyName;
                result.ReferencesCount = asmdef.references?.Length ?? 0;
                
                // Validate basic structure
                ValidateBasicStructure(asmdef, result);
                
                // Validate references
                ValidateReferences(asmdef, dependencyGraph, result);
                
                // Check for circular dependencies
                CheckCircularDependencies(asmdef, dependencyGraph, result);
                
                // Project Chimera specific validations
                ValidateProjectChimeraSpecificRules(asmdef, result);
                
                // Update validation status
                result.IsValid = !result.Issues.Any(i => i.Severity == IssueSeverity.Error);
                result.HasWarnings = result.Issues.Any(i => i.Severity == IssueSeverity.Warning);
                result.HasAutoFixableIssues = result.Issues.Any(i => i.CanAutoFix);
            }
            catch (System.Exception e)
            {
                result.AddIssue(IssueSeverity.Error, $"Validation error: {e.Message}");
            }
            
            return result;
        }
        
        private void ValidateBasicStructure(AssemblyDefinition asmdef, AssemblyValidationResult result)
        {
            // Check required fields
            if (string.IsNullOrEmpty(asmdef.name))
            {
                result.AddIssue(IssueSeverity.Error, "Assembly name is missing", true);
            }
            
            if (asmdef.references == null)
            {
                result.AddIssue(IssueSeverity.Warning, "No references defined", true);
            }
            
            // Check naming conventions
            if (!string.IsNullOrEmpty(asmdef.name) && !asmdef.name.StartsWith("ProjectChimera"))
            {
                result.AddIssue(IssueSeverity.Warning, "Assembly name should start with 'ProjectChimera'");
            }
            
            // Check root namespace
            if (string.IsNullOrEmpty(asmdef.rootNamespace))
            {
                result.AddIssue(IssueSeverity.Warning, "Root namespace is not defined", true);
            }
            else if (!asmdef.rootNamespace.StartsWith("ProjectChimera"))
            {
                result.AddIssue(IssueSeverity.Warning, "Root namespace should start with 'ProjectChimera'");
            }
        }
        
        private void ValidateReferences(AssemblyDefinition asmdef, Dictionary<string, AssemblyDefinition> dependencyGraph, AssemblyValidationResult result)
        {
            if (asmdef.references == null) return;
            
            foreach (var reference in asmdef.references)
            {
                // Check if reference exists in ProjectChimera assemblies
                if (reference.StartsWith("ProjectChimera") && !dependencyGraph.ContainsKey(reference))
                {
                    result.MissingReferences.Add(reference);
                    result.AddIssue(IssueSeverity.Error, $"Missing reference: {reference}");
                }
                
                // Check for Unity package references that might not be available
                if (reference.StartsWith("Unity.") && !IsUnityPackageAvailable(reference))
                {
                    result.AddIssue(IssueSeverity.Warning, $"Unity package may not be available: {reference}");
                }
            }
        }
        
        private void CheckCircularDependencies(AssemblyDefinition asmdef, Dictionary<string, AssemblyDefinition> dependencyGraph, AssemblyValidationResult result)
        {
            var visited = new HashSet<string>();
            var stack = new HashSet<string>();
            
            if (HasCircularDependency(asmdef.name, dependencyGraph, visited, stack, result))
            {
                result.AddIssue(IssueSeverity.Error, "Circular dependency detected");
            }
        }
        
        private bool HasCircularDependency(string assemblyName, Dictionary<string, AssemblyDefinition> dependencyGraph, 
            HashSet<string> visited, HashSet<string> stack, AssemblyValidationResult result)
        {
            if (stack.Contains(assemblyName))
            {
                result.CircularReferences.Add(assemblyName);
                return true;
            }
            
            if (visited.Contains(assemblyName)) return false;
            
            visited.Add(assemblyName);
            stack.Add(assemblyName);
            
            if (dependencyGraph.TryGetValue(assemblyName, out var assembly) && assembly.references != null)
            {
                foreach (var reference in assembly.references)
                {
                    if (reference.StartsWith("ProjectChimera") && 
                        HasCircularDependency(reference, dependencyGraph, visited, stack, result))
                    {
                        return true;
                    }
                }
            }
            
            stack.Remove(assemblyName);
            return false;
        }
        
        private void ValidateProjectChimeraSpecificRules(AssemblyDefinition asmdef, AssemblyValidationResult result)
        {
            // Editor assemblies should only be in Editor folders
            if (asmdef.name.Contains("Editor") && !asmdef.Path.Contains("/Editor/"))
            {
                result.AddIssue(IssueSeverity.Warning, "Editor assembly should be in Editor folder");
            }
            
            // Test assemblies should reference test framework
            if (asmdef.name.Contains("Test") && (asmdef.references == null || 
                !asmdef.references.Any(r => r.Contains("TestRunner"))))
            {
                result.AddIssue(IssueSeverity.Warning, "Test assembly should reference Unity Test Framework");
            }
            
            // Core assembly should not reference system assemblies
            if (asmdef.name == "ProjectChimera.Core" && asmdef.references != null)
            {
                var systemReferences = asmdef.references.Where(r => r.StartsWith("ProjectChimera.Systems")).ToList();
                if (systemReferences.Any())
                {
                    result.AddIssue(IssueSeverity.Warning, "Core assembly should not reference system assemblies");
                }
            }
        }
        
        private bool IsUnityPackageAvailable(string packageName)
        {
            // Check for common Unity packages
            var commonPackages = new[]
            {
                "Unity.TextMeshPro", "Unity.ugui", "Cinemachine", 
                "Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Core.Runtime"
            };
            
            return commonPackages.Contains(packageName);
        }
        
        private void FixAllIssues()
        {
            _issuesFixed = 0;
            
            foreach (var result in _validationResults.Where(r => r.HasAutoFixableIssues))
            {
                FixAssemblyIssues(result);
            }
            
            AssetDatabase.Refresh();
            ChimeraLogger.Log($"[Assembly Validator] Fixed {_issuesFixed} issues");
            
            // Re-validate after fixes
            ValidateAllAssemblies();
        }
        
        private void FixAssemblyIssues(AssemblyValidationResult result)
        {
            ChimeraLogger.Log($"[Assembly Validator] Manual fixes required for {result.AssemblyName}");
            ChimeraLogger.Log("Please use Unity Inspector to fix assembly definition issues manually.");
        }
        
        private AssemblyDefinition ParseAssemblyDefinition(string jsonContent)
        {
            var asmdef = new AssemblyDefinition();
            
            // Simple regex-based JSON parsing for assembly definitions
            var nameMatch = Regex.Match(jsonContent, @"""name"":\s*""([^""]+)""");
            if (nameMatch.Success)
                asmdef.name = nameMatch.Groups[1].Value;
                
            var namespaceMatch = Regex.Match(jsonContent, @"""rootNamespace"":\s*""([^""]+)""");
            if (namespaceMatch.Success)
                asmdef.rootNamespace = namespaceMatch.Groups[1].Value;
                
            // Parse references array
            var referencesMatch = Regex.Match(jsonContent, @"""references"":\s*\[(.*?)\]", RegexOptions.Singleline);
            if (referencesMatch.Success)
            {
                var referencesContent = referencesMatch.Groups[1].Value;
                var referenceMatches = Regex.Matches(referencesContent, @"""([^""]+)""");
                var references = new List<string>();
                foreach (Match match in referenceMatches)
                {
                    references.Add(match.Groups[1].Value);
                }
                asmdef.references = references.ToArray();
            }
            else
            {
                asmdef.references = new string[0];
            }
            
            return asmdef;
        }
        
        private void GenerateAssemblyReport()
        {
            var reportPath = "Assets/ProjectChimera/Editor/AssemblyValidationReport.txt";
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("PROJECT CHIMERA ASSEMBLY VALIDATION REPORT");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Assemblies: {_totalAssemblies}");
            report.AppendLine($"Issues Found: {_issuesFound}");
            report.AppendLine();
            
            foreach (var result in _validationResults)
            {
                report.AppendLine($"Assembly: {result.AssemblyName}");
                report.AppendLine($"Path: {result.AssemblyPath}");
                report.AppendLine($"Status: {(result.IsValid ? "VALID" : "INVALID")}");
                report.AppendLine($"References: {result.ReferencesCount}");
                
                if (result.Issues.Count > 0)
                {
                    report.AppendLine("Issues:");
                    foreach (var issue in result.Issues)
                    {
                        report.AppendLine($"  - [{issue.Severity}] {issue.Message}");
                    }
                }
                
                if (result.MissingReferences.Count > 0)
                {
                    report.AppendLine("Missing References:");
                    foreach (var missing in result.MissingReferences)
                    {
                        report.AppendLine($"  - {missing}");
                    }
                }
                
                report.AppendLine();
            }
            
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();
            
            ChimeraLogger.Log($"[Assembly Validator] Report generated: {reportPath}");
            EditorUtility.RevealInFinder(reportPath);
        }
    }
    
    [System.Serializable]
    public class AssemblyDefinition
    {
        public string name;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public object[] versionDefines;
        public bool noEngineReferences;
        
        [System.NonSerialized]
        public string Path;
    }
    
    public class AssemblyValidationResult
    {
        public string AssemblyPath { get; set; }
        public string AssemblyName { get; set; }
        public int ReferencesCount { get; set; }
        public bool IsValid { get; set; }
        public bool HasWarnings { get; set; }
        public bool HasAutoFixableIssues { get; set; }
        public List<AssemblyIssue> Issues { get; set; } = new List<AssemblyIssue>();
        public List<string> MissingReferences { get; set; } = new List<string>();
        public List<string> CircularReferences { get; set; } = new List<string>();
        
        public void AddIssue(IssueSeverity severity, string message, bool canAutoFix = false)
        {
            Issues.Add(new AssemblyIssue
            {
                Severity = severity,
                Message = message,
                CanAutoFix = canAutoFix
            });
        }
    }
    
    public class AssemblyIssue
    {
        public IssueSeverity Severity { get; set; }
        public string Message { get; set; }
        public bool CanAutoFix { get; set; }
    }
    
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }
}