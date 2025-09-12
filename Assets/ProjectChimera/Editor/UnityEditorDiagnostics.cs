using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// SIMPLE: Basic editor diagnostics aligned with Project Chimera's editor needs.
    /// Focuses on essential validation without complex diagnostic systems.
    /// </summary>
    public class UnityEditorDiagnostics : EditorWindow
    {
        private Vector2 _scrollPosition;
        private readonly List<string> _diagnosticMessages = new List<string>();
        private bool _isRunning = false;

        [MenuItem("Project Chimera/Unity Editor Diagnostics")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityEditorDiagnostics>("Editor Diagnostics");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity Editor Diagnostics", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Control buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Diagnostics", GUILayout.Height(30)))
            {
                RunBasicDiagnostics();
            }
            if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
            {
                ClearResults();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (_isRunning)
            {
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)),
                    0.5f, "Running diagnostics...");
            }

            // Results display
            if (_diagnosticMessages.Count > 0)
            {
                GUILayout.Label($"Diagnostic Results ({_diagnosticMessages.Count}):", EditorStyles.boldLabel);

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                foreach (var message in _diagnosticMessages)
                {
                    GUILayout.Label(message, EditorStyles.wordWrappedLabel);
                }

                GUILayout.EndScrollView();
            }
            else if (!_isRunning)
            {
                GUILayout.Label("No diagnostic results. Click 'Run Diagnostics' to start.",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void RunBasicDiagnostics()
        {
            _isRunning = true;
            _diagnosticMessages.Clear();

            AddMessage("Starting basic diagnostics...");

            // Check for ProjectChimera folder
            bool projectFolderExists = CheckProjectFolder();
            AddMessage(projectFolderExists ?
                "✓ ProjectChimera folder found" :
                "✗ ProjectChimera folder missing");

            // Check for basic scripts
            bool coreScriptsExist = CheckCoreScripts();
            AddMessage(coreScriptsExist ?
                "✓ Core scripts found" :
                "✗ Core scripts missing");

            // Check for data folders
            bool dataFoldersExist = CheckDataFolders();
            AddMessage(dataFoldersExist ?
                "✓ Data folders found" :
                "✗ Data folders missing");

            // Check for scenes
            bool scenesExist = CheckScenes();
            AddMessage(scenesExist ?
                "✓ Scenes found" :
                "✗ Scenes missing");

            // Summary
            int totalChecks = 4;
            int passedChecks = (projectFolderExists ? 1 : 0) +
                              (coreScriptsExist ? 1 : 0) +
                              (dataFoldersExist ? 1 : 0) +
                              (scenesExist ? 1 : 0);

            AddMessage($"Diagnostics complete: {passedChecks}/{totalChecks} checks passed");

            if (passedChecks == totalChecks)
            {
                AddMessage("✓ All basic checks passed - project appears healthy");
            }
            else
            {
                AddMessage("⚠ Some checks failed - review project setup");
            }

            _isRunning = false;
        }

        private void ClearResults()
        {
            _diagnosticMessages.Clear();
        }

        private bool CheckProjectFolder()
        {
            return AssetDatabase.IsValidFolder("Assets/ProjectChimera");
        }

        private bool CheckCoreScripts()
        {
            // Check for some core script files
            string[] coreFiles = {
                "Assets/ProjectChimera/Core/ChimeraManager.cs",
                "Assets/ProjectChimera/Core/GameManager.cs"
            };

            foreach (string file in coreFiles)
            {
                if (!System.IO.File.Exists(file))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckDataFolders()
        {
            string[] dataFolders = {
                "Assets/ProjectChimera/Data",
                "Assets/ProjectChimera/Data/Shared",
                "Assets/ProjectChimera/Data/Genetics"
            };

            foreach (string folder in dataFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckScenes()
        {
            // Check for at least one scene
            string[] scenes = AssetDatabase.FindAssets("t:Scene");

            return scenes.Length > 0;
        }

        private void AddMessage(string message)
        {
            _diagnosticMessages.Add($"{System.DateTime.Now.ToShortTimeString()}: {message}");

            // Keep only recent messages
            if (_diagnosticMessages.Count > 50)
            {
                _diagnosticMessages.RemoveAt(0);
            }
        }
    }
}
