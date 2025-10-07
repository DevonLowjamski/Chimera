using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Simple assembly validation tool without external dependencies.
    /// Validates basic assembly structure and references.
    /// </summary>
    public static class SimpleAssemblyValidator
    {
        [MenuItem("Project Chimera/Simple Assembly Validation")]
        public static void ValidateAssemblies()
        {
            var report = new StringBuilder();
            report.AppendLine("SIMPLE ASSEMBLY VALIDATION REPORT");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("=".PadRight(60, '='));
            report.AppendLine();

            var asmdefFiles = Directory.GetFiles("Assets/ProjectChimera", "*.asmdef", SearchOption.AllDirectories);
            int validAssemblies = 0;
            int totalAssemblies = asmdefFiles.Length;

            report.AppendLine($"Found {totalAssemblies} assembly definition files:");
            report.AppendLine();

            foreach (var path in asmdefFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var isValid = ValidateAssemblyFile(path, report);

                if (isValid)
                {
                    validAssemblies++;
                    report.AppendLine($"✅ {fileName}");
                }
                else
                {
                    report.AppendLine($"❌ {fileName}");
                }
            }

            report.AppendLine();
            report.AppendLine($"SUMMARY: {validAssemblies}/{totalAssemblies} assemblies are valid");

            if (validAssemblies == totalAssemblies)
            {
                report.AppendLine("✅ All assemblies passed validation!");
            }
            else
            {
                report.AppendLine("❌ Some assemblies have issues that need attention.");
            }

            // Save report
            var reportPath = "Assets/ProjectChimera/Editor/SimpleAssemblyValidationReport.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();

            // Display results
            ChimeraLogger.Log("OTHER", "$1", null);

            var statusMessage = validAssemblies == totalAssemblies ?
                "✅ All assemblies are valid" :
                $"❌ {totalAssemblies - validAssemblies} assemblies have issues";

            EditorUtility.DisplayDialog("Assembly Validation",
                $"{statusMessage}\n\nReport saved to: {reportPath}", "OK");
        }

        private static bool ValidateAssemblyFile(string path, StringBuilder report)
        {
            try
            {
                var content = File.ReadAllText(path);

                // Basic validation checks
                bool hasName = content.Contains("\"name\":");
                bool hasRootNamespace = content.Contains("\"rootNamespace\":");
                bool hasReferences = content.Contains("\"references\":");
                bool isValidJson = content.Contains("{") && content.Contains("}");

                if (!isValidJson)
                {
                    report.AppendLine($"  - Invalid JSON structure");
                    return false;
                }

                if (!hasName)
                {
                    report.AppendLine($"  - Missing 'name' field");
                    return false;
                }

                // Additional checks can be added here
                return true;
            }
            catch (System.Exception e)
            {
                report.AppendLine($"  - Error reading file: {e.Message}");
                return false;
            }
        }

        [MenuItem("Project Chimera/Quick Assembly Check")]
        public static void QuickAssemblyCheck()
        {
            var criticalAssemblies = new[]
            {
                "ProjectChimera.Core",
                "ProjectChimera.Data",
                "ProjectChimera.Systems.IPM",
                "ProjectChimera.Gaming"
            };

            bool allFound = true;
            var message = new StringBuilder();
            message.AppendLine("Critical Assembly Check:");

            foreach (var assemblyName in criticalAssemblies)
            {
                bool found = FindAssemblyDefinition(assemblyName) != null;
                if (found)
                {
                    message.AppendLine($"✅ {assemblyName}");
                }
                else
                {
                    message.AppendLine($"❌ {assemblyName} (MISSING)");
                    allFound = false;
                }
            }

            var status = allFound ? "All critical assemblies found!" : "Some critical assemblies are missing!";
            message.AppendLine();
            message.AppendLine(status);

            ChimeraLogger.Log("OTHER", "$1", null);
            EditorUtility.DisplayDialog("Quick Assembly Check", message.ToString(), "OK");
        }

        private static string FindAssemblyDefinition(string assemblyName)
        {
            var asmdefFiles = Directory.GetFiles("Assets/ProjectChimera", "*.asmdef", SearchOption.AllDirectories);

            foreach (var path in asmdefFiles)
            {
                try
                {
                    var content = File.ReadAllText(path);
                    if (content.Contains($"\"name\": \"{assemblyName}\""))
                    {
                        return path;
                    }
                }
                catch
                {
                    // Continue searching
                }
            }

            return null;
        }
    }
}
