using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectChimera.Systems.Scene
{
    /// <summary>
    /// Runtime validator for Build Settings configuration
    /// Provides debugging and validation functions that can be called during gameplay
    /// </summary>
    public static class BuildSettingsValidator
    {
        /// <summary>
        /// Validate current Build Settings at runtime
        /// Call this from BootManager or during development testing
        /// </summary>
        public static bool ValidateRuntimeBuildSettings()
        {
            ChimeraLogger.Log("[BuildSettingsValidator] Validating runtime Build Settings...");

            bool isValid = true;
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            // Check total scene count
            if (sceneCount != SceneConstants.ALL_SCENES.Length)
            {
                ChimeraLogger.LogError($"[BuildSettingsValidator] Expected {SceneConstants.ALL_SCENES.Length} scenes in build, found {sceneCount}");
                isValid = false;
            }

            // Validate each scene individually
            for (int i = 0; i < sceneCount && i < SceneConstants.ALL_SCENES.Length; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string expectedSceneName = SceneConstants.GetSceneName(i);
                
                if (scenePath == null || scenePath.Length == 0)
                {
                    ChimeraLogger.LogError($"[BuildSettingsValidator] Build index {i}: No scene path found");
                    isValid = false;
                    continue;
                }

                string actualSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                if (actualSceneName != expectedSceneName)
                {
                    ChimeraLogger.LogError($"[BuildSettingsValidator] Build index {i}: Expected '{expectedSceneName}', found '{actualSceneName}'");
                    isValid = false;
                }
                else
                {
                    ChimeraLogger.Log($"[BuildSettingsValidator] Build index {i}: {actualSceneName} ✓");
                }
            }

            // Test scene loading capabilities
            if (isValid)
            {
                ChimeraLogger.Log("[BuildSettingsValidator] Testing scene name resolution...");
                TestSceneNameResolution();
            }

            string result = isValid ? "PASSED" : "FAILED";
            ChimeraLogger.Log($"[BuildSettingsValidator] Runtime Build Settings validation: {result}");
            
            return isValid;
        }

        /// <summary>
        /// Test scene name resolution functions
        /// </summary>
        private static void TestSceneNameResolution()
        {
            // Test key scene resolutions
            var testScenes = new[] { 
                SceneConstants.BOOT_SCENE, 
                SceneConstants.MAIN_MENU_SCENE, 
                SceneConstants.WAREHOUSE_SMALL_BAY 
            };

            foreach (string sceneName in testScenes)
            {
                int buildIndex = SceneConstants.GetBuildIndex(sceneName);
                string resolvedName = SceneConstants.GetSceneName(buildIndex);
                
                if (resolvedName == sceneName)
                {
                    ChimeraLogger.Log($"[BuildSettingsValidator] Scene resolution test: '{sceneName}' <-> {buildIndex} ✓");
                }
                else
                {
                    ChimeraLogger.LogError($"[BuildSettingsValidator] Scene resolution test FAILED: '{sceneName}' -> {buildIndex} -> '{resolvedName}'");
                }
            }
        }

        /// <summary>
        /// Print current runtime build settings to console
        /// Useful for debugging scene loading issues
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void PrintRuntimeBuildSettings()
        {
            if (Debug.isDebugBuild)
            {
                ChimeraLogger.Log("[BuildSettingsValidator] Runtime Build Settings Summary:");
                
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    ChimeraLogger.Log($"[BuildSettingsValidator] Index {i}: {sceneName} ({scenePath})");
                }
            }
        }

        /// <summary>
        /// Check if a specific scene can be loaded by name
        /// </summary>
        public static bool CanLoadScene(string sceneName)
        {
            int buildIndex = SceneConstants.GetBuildIndex(sceneName);
            if (buildIndex < 0)
            {
                ChimeraLogger.LogError($"[BuildSettingsValidator] Scene '{sceneName}' not found in constants");
                return false;
            }

            string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                ChimeraLogger.LogError($"[BuildSettingsValidator] No build path found for scene '{sceneName}' at index {buildIndex}");
                return false;
            }

            ChimeraLogger.Log($"[BuildSettingsValidator] Scene '{sceneName}' can be loaded (index {buildIndex})");
            return true;
        }

        /// <summary>
        /// Get detailed build settings report for debugging
        /// </summary>
        public static string GetBuildSettingsReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Project Chimera Build Settings Report ===");
            
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            report.AppendLine($"Total scenes in build: {sceneCount}");
            report.AppendLine($"Expected scenes: {SceneConstants.ALL_SCENES.Length}");
            report.AppendLine();

            for (int i = 0; i < sceneCount; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                string expectedName = i < SceneConstants.ALL_SCENES.Length ? SceneConstants.ALL_SCENES[i] : "UNKNOWN";
                string status = sceneName == expectedName ? "✓" : "✗";
                
                report.AppendLine($"Index {i}: {sceneName} (Expected: {expectedName}) {status}");
            }

            return report.ToString();
        }
    }
}