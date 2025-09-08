using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Build profiles manager for Project Chimera
    /// Manages script defines for different development and deployment scenarios
    /// </summary>
    public static class ChimeraBuildProfiles
    {
        // Core build profiles with their respective script defines
        public static readonly Dictionary<string, string[]> BuildProfiles = new Dictionary<string, string[]>
        {
            ["Development"] = new[]
            {
                "CHIMERA_DEV_LOGS",
                "CHIMERA_VERBOSE_INIT",
                "CHIMERA_ASSERTS",
                "CHIMERA_DEBUG_UI",
                "CHIMERA_PERFORMANCE_PROFILING"
            },
            ["Testing"] = new[]
            {
                "CHIMERA_DEV_LOGS",
                "CHIMERA_ASSERTS",
                "CHIMERA_TESTING_MODE",
                "CHIMERA_MOCK_SERVICES"
            },
            ["Production"] = new[]
            {
                "CHIMERA_PRODUCTION",
                "CHIMERA_OPTIMIZED"
            },
            ["Debug"] = new[]
            {
                "CHIMERA_DEV_LOGS",
                "CHIMERA_VERBOSE_INIT",
                "CHIMERA_ASSERTS",
                "CHIMERA_DEBUG_UI",
                "CHIMERA_PERFORMANCE_PROFILING",
                "CHIMERA_FULL_LOGGING",
                "CHIMERA_VALIDATION_CHECKS"
            },
            ["Release"] = new[]
            {
                "CHIMERA_RELEASE",
                "CHIMERA_OPTIMIZED"
            }
        };

        // Script define descriptions for documentation
        public static readonly Dictionary<string, string> DefineDescriptions = new Dictionary<string, string>
        {
            ["CHIMERA_DEV_LOGS"] = "Enable development logging throughout the system",
            ["CHIMERA_VERBOSE_INIT"] = "Enable verbose logging during system initialization",
            ["CHIMERA_ASSERTS"] = "Enable runtime assertions and validation checks",
            ["CHIMERA_DEBUG_UI"] = "Enable debug UI elements and development tools",
            ["CHIMERA_PERFORMANCE_PROFILING"] = "Enable performance profiling and metrics collection",
            ["CHIMERA_TESTING_MODE"] = "Enable testing-specific features and mock data",
            ["CHIMERA_MOCK_SERVICES"] = "Use mock implementations for external services",
            ["CHIMERA_PRODUCTION"] = "Production build with optimizations and minimal logging",
            ["CHIMERA_OPTIMIZED"] = "Apply performance optimizations and disable debug features",
            ["CHIMERA_FULL_LOGGING"] = "Enable all logging levels and detailed traces",
            ["CHIMERA_VALIDATION_CHECKS"] = "Enable comprehensive validation and error checking",
            ["CHIMERA_RELEASE"] = "Release build configuration",
            ["CHIMERA_SERVICELOCATOR_ERROR"] = "Make ServiceLocator usage cause compile errors"
        };

        /// <summary>
        /// Apply a build profile to the current build target
        /// </summary>
        public static void ApplyBuildProfile(string profileName)
        {
            if (!BuildProfiles.TryGetValue(profileName, out var defines))
            {
                ChimeraLogger.LogError($"[ChimeraBuildProfiles] Unknown build profile: {profileName}");
                return;
            }

            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget).Split(';').ToList();
            
            // Remove all Chimera-specific defines first
            currentDefines.RemoveAll(d => d.StartsWith("CHIMERA_"));
            
            // Add new profile defines
            currentDefines.AddRange(defines);
            
            // Set the new defines
            var newDefinesString = string.Join(";", currentDefines.Where(d => !string.IsNullOrEmpty(d)));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, newDefinesString);
            
            ChimeraLogger.Log($"[ChimeraBuildProfiles] Applied '{profileName}' profile to {buildTarget}");
            ChimeraLogger.Log($"[ChimeraBuildProfiles] Active defines: {string.Join(", ", defines)}");
            
            // Force recompilation
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Get currently active Chimera script defines
        /// </summary>
        public static string[] GetActiveChimeraDefines()
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget).Split(';');
            return currentDefines.Where(d => d.StartsWith("CHIMERA_")).ToArray();
        }

        /// <summary>
        /// Get the currently active build profile name (best guess)
        /// </summary>
        public static string GetActiveBuildProfile()
        {
            var activeDefines = GetActiveChimeraDefines();
            
            foreach (var profile in BuildProfiles)
            {
                if (profile.Value.All(d => activeDefines.Contains(d)) && 
                    activeDefines.Where(d => d.StartsWith("CHIMERA_")).All(d => profile.Value.Contains(d)))
                {
                    return profile.Key;
                }
            }
            
            return "Custom";
        }

        /// <summary>
        /// Add a custom script define
        /// </summary>
        public static void AddScriptDefine(string define)
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget).Split(';').ToList();
            
            if (!currentDefines.Contains(define))
            {
                currentDefines.Add(define);
                var newDefinesString = string.Join(";", currentDefines.Where(d => !string.IsNullOrEmpty(d)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, newDefinesString);
                
                ChimeraLogger.Log($"[ChimeraBuildProfiles] Added script define: {define}");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Remove a script define
        /// </summary>
        public static void RemoveScriptDefine(string define)
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget).Split(';').ToList();
            
            if (currentDefines.Remove(define))
            {
                var newDefinesString = string.Join(";", currentDefines.Where(d => !string.IsNullOrEmpty(d)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, newDefinesString);
                
                ChimeraLogger.Log($"[ChimeraBuildProfiles] Removed script define: {define}");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Clear all Chimera script defines
        /// </summary>
        public static void ClearChimeraDefines()
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget).Split(';').ToList();
            
            var removedCount = currentDefines.RemoveAll(d => d.StartsWith("CHIMERA_"));
            
            if (removedCount > 0)
            {
                var newDefinesString = string.Join(";", currentDefines.Where(d => !string.IsNullOrEmpty(d)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, newDefinesString);
                
                ChimeraLogger.Log($"[ChimeraBuildProfiles] Cleared {removedCount} Chimera script defines");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Validate current build configuration
        /// </summary>
        public static void ValidateBuildConfiguration()
        {
            var activeDefines = GetActiveChimeraDefines();
            var activeProfile = GetActiveBuildProfile();
            
            ChimeraLogger.Log($"[ChimeraBuildProfiles] Build Configuration Validation:");
            ChimeraLogger.Log($"  Active Profile: {activeProfile}");
            ChimeraLogger.Log($"  Active Defines: {string.Join(", ", activeDefines)}");
            ChimeraLogger.Log($"  Build Target: {EditorUserBuildSettings.selectedBuildTargetGroup}");
            
            // Check for conflicting defines
            var conflictingDefines = new[]
            {
                new[] { "CHIMERA_PRODUCTION", "CHIMERA_DEV_LOGS" },
                new[] { "CHIMERA_PRODUCTION", "CHIMERA_DEBUG_UI" },
                new[] { "CHIMERA_OPTIMIZED", "CHIMERA_FULL_LOGGING" }
            };
            
            foreach (var conflict in conflictingDefines)
            {
                if (conflict.All(d => activeDefines.Contains(d)))
                {
                    ChimeraLogger.LogWarning($"[ChimeraBuildProfiles] Potential conflict: {string.Join(" + ", conflict)}");
                }
            }
        }
    }

    /// <summary>
    /// Menu items for easy access to build profiles
    /// </summary>
    public static class ChimeraBuildProfilesMenu
    {
        private const string MenuPrefix = "Project Chimera/Build Profiles/";

        [MenuItem(MenuPrefix + "Apply Development Profile")]
        public static void ApplyDevelopmentProfile()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Development");
        }

        [MenuItem(MenuPrefix + "Apply Testing Profile")]
        public static void ApplyTestingProfile()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Testing");
        }

        [MenuItem(MenuPrefix + "Apply Production Profile")]
        public static void ApplyProductionProfile()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Production");
        }

        [MenuItem(MenuPrefix + "Apply Debug Profile")]
        public static void ApplyDebugProfile()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Debug");
        }

        [MenuItem(MenuPrefix + "Apply Release Profile")]
        public static void ApplyReleaseProfile()
        {
            ChimeraBuildProfiles.ApplyBuildProfile("Release");
        }

        [MenuItem(MenuPrefix + "Clear All Chimera Defines")]
        public static void ClearAllDefines()
        {
            ChimeraBuildProfiles.ClearChimeraDefines();
        }

        [MenuItem(MenuPrefix + "Validate Configuration")]
        public static void ValidateConfiguration()
        {
            ChimeraBuildProfiles.ValidateBuildConfiguration();
        }

        [MenuItem(MenuPrefix + "Show Current Profile")]
        public static void ShowCurrentProfile()
        {
            var activeProfile = ChimeraBuildProfiles.GetActiveBuildProfile();
            var activeDefines = ChimeraBuildProfiles.GetActiveChimeraDefines();
            
            ChimeraLogger.Log($"[ChimeraBuildProfiles] Current Profile: {activeProfile}");
            ChimeraLogger.Log($"[ChimeraBuildProfiles] Active Defines: {string.Join(", ", activeDefines)}");
            
            EditorUtility.DisplayDialog("Current Build Profile", 
                $"Profile: {activeProfile}\n\nDefines:\n{string.Join("\n", activeDefines)}", 
                "OK");
        }
    }
}