using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Custom editor window for managing Project Chimera build profiles
    /// Provides a GUI for selecting and configuring build profiles with script defines
    /// </summary>
    public class ChimeraBuildProfilesWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _selectedProfile = "Development";
        private bool _showDefineDescriptions = true;
        private bool _autoApplyOnChange = false;
        private string _customDefine = "";

        [MenuItem("Project Chimera/Build Profile Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<ChimeraBuildProfilesWindow>("Chimera Build Profiles");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _selectedProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
        }

        private void OnGUI()
        {
            GUILayout.Label("Project Chimera Build Profile Manager", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            DrawCurrentStatus();
            EditorGUILayout.Space();
            
            DrawProfileSelection();
            EditorGUILayout.Space();
            
            DrawActiveDefines();
            EditorGUILayout.Space();
            
            DrawCustomDefines();
            EditorGUILayout.Space();
            
            DrawActions();
            EditorGUILayout.Space();
            
            if (_showDefineDescriptions)
            {
                DrawDefineDescriptions();
            }
        }

        private void DrawCurrentStatus()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Current Build Configuration", EditorStyles.boldLabel);
            
            var currentProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            EditorGUILayout.LabelField("Active Profile:", currentProfile);
            EditorGUILayout.LabelField("Build Target:", buildTarget.ToString());
            
            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            EditorGUILayout.LabelField("Active Defines:", activeDefines.Length.ToString());
            
            EditorGUILayout.EndVertical();
        }

        private void DrawProfileSelection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Build Profiles", EditorStyles.boldLabel);
            
            var profiles = ChimeraBuildProfiles.BuildProfiles.Keys.ToArray();
            var currentIndex = System.Array.IndexOf(profiles, _selectedProfile);
            if (currentIndex == -1) currentIndex = 0;
            
            var newIndex = EditorGUILayout.Popup("Select Profile:", currentIndex, profiles);
            var newProfile = profiles[newIndex];
            
            if (newProfile != _selectedProfile)
            {
                _selectedProfile = newProfile;
                if (_autoApplyOnChange)
                {
                    ChimeraBuildProfiles.ApplyBuildProfile(_selectedProfile);
                }
            }
            
            // Show selected profile's defines
            if (ChimeraBuildProfiles.BuildProfiles.TryGetValue(_selectedProfile, out var selectedDefines))
            {
                EditorGUILayout.LabelField("Profile Defines:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var define in selectedDefines)
                {
                    EditorGUILayout.LabelField($"• {define}");
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Apply {_selectedProfile} Profile"))
            {
                ChimeraBuildProfiles.ApplyBuildProfile(_selectedProfile);
                ShowNotification(new GUIContent($"Applied {_selectedProfile} profile"));
            }
            
            _autoApplyOnChange = EditorGUILayout.Toggle("Auto Apply", _autoApplyOnChange, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActiveDefines()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Currently Active Defines", EditorStyles.boldLabel);
            
            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            
            if (activeDefines.Length == 0)
            {
                EditorGUILayout.HelpBox("No Chimera script defines are currently active.", MessageType.Info);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
                
                foreach (var define in activeDefines.OrderBy(d => d))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {define}");
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        ChimeraBuildProfiles.RemoveScriptDefine(define);
                        ShowNotification(new GUIContent($"Removed {define}"));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCustomDefines()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Custom Script Defines", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _customDefine = EditorGUILayout.TextField("Add Define:", _customDefine);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(_customDefine);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (!string.IsNullOrWhiteSpace(_customDefine))
                {
                    ChimeraBuildProfiles.AddScriptDefine(_customDefine.Trim());
                    ShowNotification(new GUIContent($"Added {_customDefine}"));
                    _customDefine = "";
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Custom defines will be added to the current build configuration.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Configuration"))
            {
                ChimeraBuildProfiles.ValidateBuildConfiguration();
                ShowNotification(new GUIContent("Configuration validated - check console"));
            }
            
            if (GUILayout.Button("Clear All Chimera Defines"))
            {
                if (EditorUtility.DisplayDialog("Clear All Defines", 
                    "Are you sure you want to remove all Chimera script defines?", 
                    "Yes", "Cancel"))
                {
                    ChimeraBuildProfiles.ClearChimeraDefines();
                    ShowNotification(new GUIContent("All Chimera defines cleared"));
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Window"))
            {
                Repaint();
            }
            
            _showDefineDescriptions = EditorGUILayout.Toggle("Show Descriptions", _showDefineDescriptions);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDefineDescriptions()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Script Define Descriptions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(200));
            
            foreach (var kvp in ChimeraBuildProfiles.DefineDescriptions.OrderBy(d => d.Key))
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(kvp.Value, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void OnInspectorUpdate()
        {
            // Refresh the window periodically to keep it up to date
            Repaint();
        }
    }

    /// <summary>
    /// Build profile preset configurations for quick setup
    /// </summary>
    public static class ChimeraBuildPresets
    {
        [MenuItem("Project Chimera/Quick Setup/Setup for Development")]
        public static void SetupDevelopment()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Development");
            ChimeraLogger.Log("[ChimeraBuildPresets] Configured for Development - Full logging and debug features enabled");
        }

        [MenuItem("Project Chimera/Quick Setup/Setup for Testing")]
        public static void SetupTesting()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Testing");
            ChimeraLogger.Log("[ChimeraBuildPresets] Configured for Testing - Mock services and testing features enabled");
        }

        [MenuItem("Project Chimera/Quick Setup/Setup for Production")]
        public static void SetupProduction()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Production");
            ChimeraLogger.Log("[ChimeraBuildPresets] Configured for Production - Optimized build with minimal logging");
        }

        [MenuItem("Project Chimera/Quick Setup/Enable ServiceLocator Enforcement")]
        public static void EnableServiceLocatorEnforcement()
        {
            ChimeraBuildProfiles.AddScriptDefine("CHIMERA_SERVICELOCATOR_ERROR");
            ChimeraLogger.Log("[ChimeraBuildPresets] ServiceLocator enforcement enabled - ServiceLocator usage will now cause compile errors");
        }

        [MenuItem("Project Chimera/Quick Setup/Disable ServiceLocator Enforcement")]
        public static void DisableServiceLocatorEnforcement()
        {
            ChimeraBuildProfiles.RemoveScriptDefine("CHIMERA_SERVICELOCATOR_ERROR");
            ChimeraLogger.Log("[ChimeraBuildPresets] ServiceLocator enforcement disabled - ServiceLocator usage will show warnings only");
        }
    }
}