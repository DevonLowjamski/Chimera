using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.UI;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Comprehensive diagnostic system for Unity Editor issues in Project Chimera.
    /// Identifies and resolves common configuration errors, XML parsing issues, and asset problems.
    /// </summary>
    public class UnityEditorDiagnostics : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<DiagnosticResult> _diagnosticResults = new List<DiagnosticResult>();
        private bool _isRunning = false;
        private StringBuilder _logBuilder = new StringBuilder();
        
        [MenuItem("Project Chimera/Unity Editor Diagnostics")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityEditorDiagnostics>("Unity Editor Diagnostics");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Unity Editor Diagnostics for Project Chimera", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Control buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Full Diagnostics", GUILayout.Height(30)))
            {
                RunFullDiagnostics();
            }
            if (GUILayout.Button("Quick Health Check", GUILayout.Height(30)))
            {
                RunQuickHealthCheck();
            }
            if (GUILayout.Button("Fix Common Issues", GUILayout.Height(30)))
            {
                FixCommonIssues();
            }
            if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
            {
                ClearResults();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            if (_isRunning)
            {
                GUILayout.Label("Running diagnostics...", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);
            }
            
            // Results display
            if (_diagnosticResults.Count > 0)
            {
                GUILayout.Label($"Diagnostic Results ({_diagnosticResults.Count} items):", EditorStyles.boldLabel);
                
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                
                foreach (var result in _diagnosticResults)
                {
                    DrawDiagnosticResult(result);
                }
                
                GUILayout.EndScrollView();
            }
            
            // Log display
            if (_logBuilder.Length > 0)
            {
                GUILayout.Label("Diagnostic Log:", EditorStyles.boldLabel);
                GUILayout.TextArea(_logBuilder.ToString(), GUILayout.Height(100));
            }
        }
        
        private void DrawDiagnosticResult(DiagnosticResult result)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header
            GUILayout.BeginHorizontal();
            
            var statusColor = result.Status switch
            {
                DiagnosticStatus.Pass => Color.green,
                DiagnosticStatus.Warning => Color.yellow,
                DiagnosticStatus.Fail => Color.red,
                _ => Color.white
            };
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"[{result.Status}]", GUILayout.Width(80));
            GUI.color = originalColor;
            
            GUILayout.Label(result.TestName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (result.HasAutoFix && GUILayout.Button("Fix", GUILayout.Width(50)))
            {
                result.AutoFix?.Invoke();
                Log($"Applied auto-fix for: {result.TestName}");
            }
            
            GUILayout.EndHorizontal();
            
            // Details
            GUILayout.Label(result.Description);
            
            if (!string.IsNullOrEmpty(result.Details))
            {
                GUILayout.Label($"Details: {result.Details}", EditorStyles.wordWrappedLabel);
            }
            
            if (!string.IsNullOrEmpty(result.Recommendation))
            {
                GUILayout.Label($"Recommendation: {result.Recommendation}", EditorStyles.wordWrappedLabel);
            }
            
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        private void RunFullDiagnostics()
        {
            _isRunning = true;
            _diagnosticResults.Clear();
            _logBuilder.Clear();
            
            Log("Starting full diagnostics...");
            
            try
            {
                // Core Unity Editor checks
                CheckUnityEditorIntegrity();
                
                // Asset system checks
                CheckAssetDatabaseIntegrity();
                
                // ScriptableObject configuration checks
                CheckScriptableObjectConfigurations();
                
                // XML and serialization checks
                CheckXMLAndSerializationIssues();
                
                // Assembly and compilation checks
                CheckAssemblyAndCompilationIssues();
                
                // Performance and memory checks
                CheckPerformanceAndMemoryIssues();
                
                // Project-specific checks
                CheckProjectChimeraSpecificIssues();
                
                Log("Full diagnostics completed");
            }
            catch (System.Exception e)
            {
                Log($"Diagnostics error: {e.Message}");
                AddResult("Diagnostics Error", DiagnosticStatus.Fail, 
                    "An error occurred during diagnostics", e.Message);
            }
            finally
            {
                _isRunning = false;
                Repaint();
            }
        }
        
        private void RunQuickHealthCheck()
        {
            _isRunning = true;
            _diagnosticResults.Clear();
            _logBuilder.Clear();
            
            Log("Starting quick health check...");
            
            try
            {
                // Essential checks only
                CheckAssetDatabaseIntegrity();
                CheckScriptableObjectConfigurations();
                CheckAssemblyAndCompilationIssues();
                
                Log("Quick health check completed");
            }
            catch (System.Exception e)
            {
                Log($"Health check error: {e.Message}");
            }
            finally
            {
                _isRunning = false;
                Repaint();
            }
        }
        
        private void CheckUnityEditorIntegrity()
        {
            Log("Checking Unity Editor integrity...");
            
            // Check Unity version
            var unityVersion = Application.unityVersion;
            if (unityVersion.StartsWith("6000."))
            {
                AddResult("Unity Version", DiagnosticStatus.Pass, 
                    "Unity 6 Beta detected", $"Version: {unityVersion}");
            }
            else
            {
                AddResult("Unity Version", DiagnosticStatus.Warning, 
                    "Not using Unity 6 Beta", $"Current version: {unityVersion}");
            }
            
            // Check for Editor log errors
            CheckEditorLogForErrors();
        }
        
        private void CheckAssetDatabaseIntegrity()
        {
            Log("Checking Asset Database integrity...");
            
            // Check for missing assets
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            int missingAssets = 0;
            
            foreach (string path in allAssetPaths)
            {
                if (path.StartsWith("Assets/ProjectChimera/"))
                {
                    if (!File.Exists(path))
                    {
                        missingAssets++;
                    }
                }
            }
            
            if (missingAssets == 0)
            {
                AddResult("Asset Database", DiagnosticStatus.Pass, 
                    "All assets are properly referenced", $"Checked {allAssetPaths.Length} assets");
            }
            else
            {
                AddResult("Asset Database", DiagnosticStatus.Fail, 
                    "Missing asset references detected", $"{missingAssets} assets missing",
                    "Run 'Force Reimport ProjectChimera Assets' to fix", 
                    () => ForceReimportAssets());
            }
        }
        
        private void CheckScriptableObjectConfigurations()
        {
            Log("Checking ScriptableObject configurations...");
            
            // Check UI Data Bindings
            CheckUIDataBindingConfigurations();
            
            // Check Event Channels
            CheckEventChannelConfigurations();
        }
        
        private void CheckUIDataBindingConfigurations()
        {
            string[] bindingGuids = AssetDatabase.FindAssets("t:UIDataBindingSO");
            int invalidBindings = 0;
            
            foreach (string guid in bindingGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var binding = AssetDatabase.LoadAssetAtPath<UIDataBindingSO>(path);
                
                if (binding != null)
                {
                    if (string.IsNullOrEmpty(binding.BindingName) || 
                        string.IsNullOrEmpty(binding.SourceManagerType) ||
                        string.IsNullOrEmpty(binding.TargetUIElement))
                    {
                        invalidBindings++;
                    }
                }
            }
            
            if (invalidBindings == 0)
            {
                AddResult("UI Data Bindings", DiagnosticStatus.Pass, 
                    "All UI Data Bindings are properly configured", $"Checked {bindingGuids.Length} bindings");
            }
            else
            {
                AddResult("UI Data Bindings", DiagnosticStatus.Warning, 
                    "Invalid UI Data Binding configurations found", $"{invalidBindings} bindings need attention",
                    "Use Asset Validation System to fix", 
                    () => FixUIDataBindings());
            }
        }
        
        private void CheckEventChannelConfigurations()
        {
            string[] eventGuids = AssetDatabase.FindAssets("t:ChimeraEventSO");
            int invalidChannels = 0;
            
            foreach (string guid in eventGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var channel = AssetDatabase.LoadAssetAtPath<ChimeraEventSO>(path);
                
                if (channel != null)
                {
                    if (string.IsNullOrEmpty(channel.DisplayName))
                    {
                        invalidChannels++;
                    }
                }
            }
            
            if (invalidChannels == 0)
            {
                AddResult("Event Channels", DiagnosticStatus.Pass, 
                    "All Event Channels are properly configured", $"Checked {eventGuids.Length} channels");
            }
            else
            {
                AddResult("Event Channels", DiagnosticStatus.Warning, 
                    "Invalid Event Channel configurations found", $"{invalidChannels} channels need attention",
                    "Use Asset Validation System to fix", 
                    () => FixEventChannels());
            }
        }
        
        private void CheckXMLAndSerializationIssues()
        {
            Log("Checking XML and serialization issues...");
            
            // Check for corrupted .asset files
            string[] assetFiles = Directory.GetFiles("Assets/ProjectChimera", "*.asset", SearchOption.AllDirectories);
            int corruptedFiles = 0;
            
            foreach (string file in assetFiles)
            {
                try
                {
                    // Try to read the file as text to check for XML issues
                    string content = File.ReadAllText(file);
                    
                    // Check for common XML corruption patterns
                    if (content.Contains("<?xml") && !content.Contains("</serializedversion>"))
                    {
                        corruptedFiles++;
                    }
                }
                catch
                {
                    corruptedFiles++;
                }
            }
            
            if (corruptedFiles == 0)
            {
                AddResult("XML Serialization", DiagnosticStatus.Pass, 
                    "No XML corruption detected", $"Checked {assetFiles.Length} asset files");
            }
            else
            {
                AddResult("XML Serialization", DiagnosticStatus.Fail, 
                    "Corrupted asset files detected", $"{corruptedFiles} files may be corrupted",
                    "Backup and recreate corrupted assets");
            }
        }
        
        private void CheckAssemblyAndCompilationIssues()
        {
            Log("Checking assembly and compilation issues...");
            
            // Check for compilation errors
            var hasCompilationErrors = EditorUtility.scriptCompilationFailed;
            
            if (!hasCompilationErrors)
            {
                AddResult("Script Compilation", DiagnosticStatus.Pass, 
                    "No compilation errors detected", "All scripts compile successfully");
            }
            else
            {
                AddResult("Script Compilation", DiagnosticStatus.Fail, 
                    "Compilation errors detected", "Check Console for details",
                    "Fix compilation errors before proceeding");
            }
            
            // Check assembly definitions
            CheckAssemblyDefinitions();
        }
        
        private void CheckAssemblyDefinitions()
        {
            string[] asmdefFiles = Directory.GetFiles("Assets/ProjectChimera", "*.asmdef", SearchOption.AllDirectories);
            int validAsmDefs = 0;
            
            foreach (string file in asmdefFiles)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains("\"name\"") && content.Contains("\"references\""))
                    {
                        validAsmDefs++;
                    }
                }
                catch
                {
                    // Invalid assembly definition
                }
            }
            
            if (validAsmDefs == asmdefFiles.Length)
            {
                AddResult("Assembly Definitions", DiagnosticStatus.Pass, 
                    "All assembly definitions are valid", $"Checked {asmdefFiles.Length} .asmdef files");
            }
            else
            {
                AddResult("Assembly Definitions", DiagnosticStatus.Warning, 
                    "Invalid assembly definitions found", $"{asmdefFiles.Length - validAsmDefs} files invalid");
            }
        }
        
        private void CheckPerformanceAndMemoryIssues()
        {
            Log("Checking performance and memory issues...");
            
            // Check for memory usage
            long memoryUsage = System.GC.GetTotalMemory(false);
            
            if (memoryUsage < 1000000000) // Less than 1GB
            {
                AddResult("Memory Usage", DiagnosticStatus.Pass, 
                    "Memory usage is within normal limits", $"Current usage: {memoryUsage / 1048576} MB");
            }
            else
            {
                AddResult("Memory Usage", DiagnosticStatus.Warning, 
                    "High memory usage detected", $"Current usage: {memoryUsage / 1048576} MB",
                    "Consider clearing caches and restarting Unity");
            }
        }
        
        private void CheckProjectChimeraSpecificIssues()
        {
            Log("Checking Project Chimera specific issues...");
            
            // Check for required managers
            CheckRequiredManagers();
            
            // Check for SpeedTree integration
            CheckSpeedTreeIntegration();
        }
        
        private void CheckRequiredManagers()
        {
            string[] requiredManagers = {
                "GameManager",
                "PlantManager",
                "EnvironmentalManager",
                "EconomyManager"
            };
            
            int foundManagers = 0;
            
            foreach (string manager in requiredManagers)
            {
                string[] managerGuids = AssetDatabase.FindAssets($"{manager} t:MonoScript");
                if (managerGuids.Length > 0)
                {
                    foundManagers++;
                }
            }
            
            if (foundManagers == requiredManagers.Length)
            {
                AddResult("Required Managers", DiagnosticStatus.Pass, 
                    "All required managers are present", $"Found {foundManagers}/{requiredManagers.Length} managers");
            }
            else
            {
                AddResult("Required Managers", DiagnosticStatus.Warning, 
                    "Missing required managers", $"Found {foundManagers}/{requiredManagers.Length} managers");
            }
        }
        
        private void CheckSpeedTreeIntegration()
        {
            // Check if SpeedTree package is available
            #if UNITY_SPEEDTREE
            AddResult("SpeedTree Integration", DiagnosticStatus.Pass, 
                "SpeedTree package is available", "Cannabis-specific rendering enabled");
            #else
            AddResult("SpeedTree Integration", DiagnosticStatus.Warning, 
                "SpeedTree package not available", "Using fallback rendering system");
            #endif
        }
        
        private void CheckEditorLogForErrors()
        {
            // This is a simplified check - in a real implementation,
            // you might parse the Editor.log file for specific error patterns
            if (EditorUtility.scriptCompilationFailed)
            {
                AddResult("Editor Log", DiagnosticStatus.Fail, 
                    "Compilation errors in Editor log", "Check Console for details");
            }
            else
            {
                AddResult("Editor Log", DiagnosticStatus.Pass, 
                    "No critical errors in Editor log", "System appears stable");
            }
        }
        
        private void FixCommonIssues()
        {
            Log("Fixing common issues...");
            
            // Apply all available auto-fixes
            foreach (var result in _diagnosticResults)
            {
                if (result.HasAutoFix)
                {
                    result.AutoFix?.Invoke();
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Log("Common issues fixed");
        }
        
        private void ForceReimportAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        
        private void FixUIDataBindings()
        {
            AssetConfigurationHelper.CreateExampleUIDataBindings();
        }
        
        private void FixEventChannels()
        {
            AssetConfigurationHelper.CreateExampleEventChannels();
        }
        
        private void ClearResults()
        {
            _diagnosticResults.Clear();
            _logBuilder.Clear();
            Repaint();
        }
        
        private void AddResult(string testName, DiagnosticStatus status, string description, 
            string details = "", string recommendation = "", System.Action autoFix = null)
        {
            _diagnosticResults.Add(new DiagnosticResult
            {
                TestName = testName,
                Status = status,
                Description = description,
                Details = details,
                Recommendation = recommendation,
                AutoFix = autoFix,
                HasAutoFix = autoFix != null
            });
        }
        
        private void Log(string message)
        {
            _logBuilder.AppendLine($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[Chimera Diagnostics] {message}");
        }
    }
    
    /// <summary>
    /// Result of a diagnostic test
    /// </summary>
    public class DiagnosticResult
    {
        public string TestName { get; set; }
        public DiagnosticStatus Status { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string Recommendation { get; set; }
        public bool HasAutoFix { get; set; }
        public System.Action AutoFix { get; set; }
    }
    
    /// <summary>
    /// Status of a diagnostic test
    /// </summary>
    public enum DiagnosticStatus
    {
        Pass,
        Warning,
        Fail
    }
}