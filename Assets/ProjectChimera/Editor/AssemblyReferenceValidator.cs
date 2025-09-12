using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// SIMPLE: Basic assembly reference validator aligned with Project Chimera's editor needs.
    /// Focuses on essential assembly validation without complex dependency analysis.
    /// </summary>
    public class AssemblyReferenceValidator : EditorWindow
    {
        private Vector2 _scrollPosition;
        private readonly List<string> _validationMessages = new List<string>();
        private bool _isValidating = false;

        [MenuItem("Project Chimera/Assembly Reference Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssemblyReferenceValidator>("Assembly Validator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Assembly Reference Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Control buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Assemblies", GUILayout.Height(30)))
            {
                ValidateAssemblies();
            }
            if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
            {
                ClearResults();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Status
            if (_isValidating)
            {
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)),
                    0.5f, "Validating...");
            }

            // Results display
            if (_validationMessages.Count > 0)
            {
                GUILayout.Label($"Validation Results ({_validationMessages.Count}):", EditorStyles.boldLabel);

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var message in _validationMessages)
                {
                    GUILayout.Label(message, EditorStyles.wordWrappedLabel);
                }

                GUILayout.EndScrollView();
            }
            else if (!_isValidating)
            {
                GUILayout.Label("No validation results. Click 'Validate Assemblies' to start.",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void ValidateAssemblies()
        {
            _isValidating = true;
            _validationMessages.Clear();

            // Simple validation - check for basic assemblies
            AddMessage("Starting assembly validation...");

            // Check for ProjectChimera.Core assembly
            bool coreAssemblyExists = CheckAssemblyExists("ProjectChimera.Core");
            AddMessage(coreAssemblyExists ?
                "✓ ProjectChimera.Core assembly found" :
                "✗ ProjectChimera.Core assembly missing");

            // Check for ProjectChimera.Data assembly
            bool dataAssemblyExists = CheckAssemblyExists("ProjectChimera.Data");
            AddMessage(dataAssemblyExists ?
                "✓ ProjectChimera.Data assembly found" :
                "✗ ProjectChimera.Data assembly missing");

            // Check for ProjectChimera.Systems assembly
            bool systemsAssemblyExists = CheckAssemblyExists("ProjectChimera.Systems");
            AddMessage(systemsAssemblyExists ?
                "✓ ProjectChimera.Systems assembly found" :
                "✗ ProjectChimera.Systems assembly missing");

            // Summary
            int totalAssemblies = 3;
            int foundAssemblies = (coreAssemblyExists ? 1 : 0) +
                                 (dataAssemblyExists ? 1 : 0) +
                                 (systemsAssemblyExists ? 1 : 0);

            AddMessage($"Validation complete: {foundAssemblies}/{totalAssemblies} assemblies found");

            if (foundAssemblies == totalAssemblies)
            {
                AddMessage("✓ All required assemblies are present");
            }
            else
            {
                AddMessage("⚠ Some assemblies are missing - check project setup");
            }

            _isValidating = false;
        }

        private void ClearResults()
        {
            _validationMessages.Clear();
        }

        private bool CheckAssemblyExists(string assemblyName)
        {
            // Simple check - look for assembly definition files
            string[] assetFiles = AssetDatabase.FindAssets($"{assemblyName} t:asmdef");

            // In a real implementation, you might check for compiled assemblies
            // For now, just check if we found any matching files
            return assetFiles.Length > 0;
        }

        private void AddMessage(string message)
        {
            _validationMessages.Add($"{System.DateTime.Now.ToShortTimeString()}: {message}");

            // Keep only recent messages
            if (_validationMessages.Count > 50)
            {
                _validationMessages.RemoveAt(0);
            }
        }
    }
}
