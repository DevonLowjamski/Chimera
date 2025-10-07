using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Automatically configures Unity Build Settings with all Project Chimera scenes
    /// Ensures consistent scene ordering and build indexes for scene transitions
    /// </summary>
    public static class BuildSettingsConfigurator
    {
        /// <summary>
        /// Scene definitions with stable build indexes and unique names
        /// These indexes must remain consistent across builds for proper scene transitions
        /// </summary>
        private static readonly Dictionary<int, string> SceneDefinitions = new Dictionary<int, string>
        {
            { 0, "Assets/ProjectChimera/Scenes/01_Boot.unity" },                    // Boot scene - always index 0
            { 1, "Assets/ProjectChimera/Scenes/02_MainMenu.unity" },               // Main menu
            { 2, "Assets/ProjectChimera/Scenes/04_Warehouse_Small_Bay.unity" },    // Small bay facility
            { 3, "Assets/ProjectChimera/Scenes/05_Warehouse_Medium_Bay.unity" },   // Medium bay facility
            { 4, "Assets/ProjectChimera/Scenes/06_Warehouse_Small_Standalone.unity" }, // Small standalone
            { 5, "Assets/ProjectChimera/Scenes/07_Warehouse_Large_Standalone.unity" }, // Large standalone
            { 6, "Assets/ProjectChimera/Scenes/08_Warehouse_Massive_Custom.unity" },   // Massive custom facility
            { 7, "Assets/ProjectChimera/Scenes/09_WorldMap.unity" },               // World map interface
            { 8, "Assets/ProjectChimera/Scenes/10_Marketplace.unity" }             // Marketplace scene
        };

        /// <summary>
        /// Configure Build Settings with Project Chimera scenes
        /// Call this from Unity menu: Tools > Project Chimera > Configure Build Settings
        /// </summary>
        [MenuItem("Tools/Project Chimera/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            ChimeraLogger.Log("OTHER", "$1", null);

            var buildScenes = new List<EditorBuildSettingsScene>();
            int processedScenes = 0;
            int missingScenes = 0;

            // Process each scene definition in order
            foreach (var sceneEntry in SceneDefinitions.OrderBy(x => x.Key))
            {
                int buildIndex = sceneEntry.Key;
                string scenePath = sceneEntry.Value;

                if (File.Exists(scenePath))
                {
                    var buildSettingsScene = new EditorBuildSettingsScene(scenePath, true);
                    buildScenes.Add(buildSettingsScene);
                    processedScenes++;

                    ChimeraLogger.Log("OTHER", "$1", null);
                }
                else
                {
                    ChimeraLogger.Log("OTHER", "$1", null);
                    missingScenes++;
                }
            }

            // Update Unity's Build Settings
            EditorBuildSettings.scenes = buildScenes.ToArray();

            // Validation and reporting
            ValidateBuildSettings();

            string summary = $"Build Settings configured successfully!\n" +
                           $"• Processed scenes: {processedScenes}\n" +
                           $"• Missing scenes: {missingScenes}\n" +
                           $"• Total build scenes: {EditorBuildSettings.scenes.Length}";

            if (missingScenes == 0)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                EditorUtility.DisplayDialog("Build Settings Configuration", summary, "OK");
            }
            else
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                EditorUtility.DisplayDialog("Build Settings Configuration", summary + "\n\nCheck console for missing scene details.", "OK");
            }

            // Save project to persist build settings changes
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Validate current Build Settings configuration
        /// </summary>
        [MenuItem("Tools/Project Chimera/Validate Build Settings")]
        public static void ValidateBuildSettings()
        {
            ChimeraLogger.Log("OTHER", "$1", null);

            var currentScenes = EditorBuildSettings.scenes;
            bool isValid = true;
            var validationResults = new List<string>();

            // Check that we have the expected number of scenes
            if (currentScenes.Length != SceneDefinitions.Count)
            {
                validationResults.Add($"Expected {SceneDefinitions.Count} scenes, found {currentScenes.Length}");
                isValid = false;
            }

            // Check each scene individually
            for (int i = 0; i < currentScenes.Length && i < SceneDefinitions.Count; i++)
            {
                var currentScene = currentScenes[i];
                var expectedPath = SceneDefinitions.ContainsKey(i) ? SceneDefinitions[i] : "";

                if (currentScene.path != expectedPath)
                {
                    validationResults.Add($"Index {i}: Expected '{expectedPath}', found '{currentScene.path}'");
                    isValid = false;
                }

                if (!currentScene.enabled)
                {
                    validationResults.Add($"Index {i}: Scene '{currentScene.path}' is disabled");
                    isValid = false;
                }

                if (!File.Exists(currentScene.path))
                {
                    validationResults.Add($"Index {i}: Scene file missing '{currentScene.path}'");
                    isValid = false;
                }
            }

            // Report results
            if (isValid)
            {
                ChimeraLogger.Log("OTHER", "$1", null);
            }
            else
            {
                ChimeraLogger.Log("OTHER", "$1", null);
                foreach (var result in validationResults)
                {
                    ChimeraLogger.Log("OTHER", "$1", null);
                }
            }
        }

        /// <summary>
        /// Get scene name by build index - useful for SceneLoader
        /// </summary>
        public static string GetSceneNameByIndex(int buildIndex)
        {
            if (SceneDefinitions.ContainsKey(buildIndex))
            {
                string scenePath = SceneDefinitions[buildIndex];
                return Path.GetFileNameWithoutExtension(scenePath);
            }

            ChimeraLogger.Log("OTHER", "$1", null);
            return null;
        }

        /// <summary>
        /// Get build index by scene name - useful for SceneLoader
        /// </summary>
        public static int GetBuildIndexBySceneName(string sceneName)
        {
            foreach (var entry in SceneDefinitions)
            {
                string sceneFileName = Path.GetFileNameWithoutExtension(entry.Value);
                if (sceneFileName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Key;
                }
            }

            ChimeraLogger.Log("OTHER", "$1", null);
            return -1;
        }

        /// <summary>
        /// Print current Build Settings configuration to console
        /// </summary>
        [MenuItem("Tools/Project Chimera/Print Build Settings")]
        public static void PrintBuildSettings()
        {
            ChimeraLogger.Log("OTHER", "$1", null);

            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                string status = scene.enabled ? "ENABLED" : "DISABLED";
                string fileName = Path.GetFileNameWithoutExtension(scene.path);

                ChimeraLogger.Log("OTHER", "$1", null);
            }

            ChimeraLogger.Log("OTHER", "$1", null);
        }

        /// <summary>
        /// Get all Project Chimera scene constants for SceneLoader integration
        /// </summary>
        public static class SceneConstants
        {
            public const string BOOT_SCENE = "01_Boot";
            public const string MAIN_MENU_SCENE = "02_MainMenu";
            public const string WAREHOUSE_SMALL_BAY_SCENE = "04_Warehouse_Small_Bay";
            public const string WAREHOUSE_MEDIUM_BAY_SCENE = "05_Warehouse_Medium_Bay";
            public const string WAREHOUSE_SMALL_STANDALONE_SCENE = "06_Warehouse_Small_Standalone";
            public const string WAREHOUSE_LARGE_STANDALONE_SCENE = "07_Warehouse_Large_Standalone";
            public const string WAREHOUSE_MASSIVE_CUSTOM_SCENE = "08_Warehouse_Massive_Custom";
            public const string WORLD_MAP_SCENE = "09_WorldMap";
            public const string MARKETPLACE_SCENE = "10_Marketplace";

            public static readonly Dictionary<string, int> SceneToBuildIndex = new Dictionary<string, int>
            {
                { BOOT_SCENE, 0 },
                { MAIN_MENU_SCENE, 1 },
                { WAREHOUSE_SMALL_BAY_SCENE, 2 },
                { WAREHOUSE_MEDIUM_BAY_SCENE, 3 },
                { WAREHOUSE_SMALL_STANDALONE_SCENE, 4 },
                { WAREHOUSE_LARGE_STANDALONE_SCENE, 5 },
                { WAREHOUSE_MASSIVE_CUSTOM_SCENE, 6 },
                { WORLD_MAP_SCENE, 7 },
                { MARKETPLACE_SCENE, 8 }
            };
        }
    }
}
