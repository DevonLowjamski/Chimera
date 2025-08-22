using System.Collections.Generic;

namespace ProjectChimera.Systems.Scene
{
    /// <summary>
    /// Central repository for all Project Chimera scene constants and build indexes
    /// These constants must match exactly with BuildSettingsConfigurator
    /// </summary>
    public static class SceneConstants
    {
        // Scene name constants - must match Unity scene file names exactly
        public const string BOOT_SCENE = "01_Boot";
        public const string MAIN_MENU_SCENE = "02_MainMenu";
        public const string WAREHOUSE_SMALL_BAY = "04_Warehouse_Small_Bay";
        public const string WAREHOUSE_MEDIUM_BAY = "05_Warehouse_Medium_Bay";
        public const string WAREHOUSE_SMALL_STANDALONE = "06_Warehouse_Small_Standalone";
        public const string WAREHOUSE_LARGE_STANDALONE = "07_Warehouse_Large_Standalone";
        public const string WAREHOUSE_MASSIVE_CUSTOM = "08_Warehouse_Massive_Custom";
        public const string WORLD_MAP_SCENE = "09_WorldMap";
        public const string MARKETPLACE_SCENE = "10_Marketplace";

        // Build index constants - stable across builds, must match BuildSettingsConfigurator
        public const int BOOT_SCENE_INDEX = 0;
        public const int MAIN_MENU_SCENE_INDEX = 1;
        public const int WAREHOUSE_SMALL_BAY_INDEX = 2;
        public const int WAREHOUSE_MEDIUM_BAY_INDEX = 3;
        public const int WAREHOUSE_SMALL_STANDALONE_INDEX = 4;
        public const int WAREHOUSE_LARGE_STANDALONE_INDEX = 5;
        public const int WAREHOUSE_MASSIVE_CUSTOM_INDEX = 6;
        public const int WORLD_MAP_SCENE_INDEX = 7;
        public const int MARKETPLACE_SCENE_INDEX = 8;

        // Scene name to build index mapping
        public static readonly Dictionary<string, int> SceneToBuildIndex = new Dictionary<string, int>
        {
            { BOOT_SCENE, BOOT_SCENE_INDEX },
            { MAIN_MENU_SCENE, MAIN_MENU_SCENE_INDEX },
            { WAREHOUSE_SMALL_BAY, WAREHOUSE_SMALL_BAY_INDEX },
            { WAREHOUSE_MEDIUM_BAY, WAREHOUSE_MEDIUM_BAY_INDEX },
            { WAREHOUSE_SMALL_STANDALONE, WAREHOUSE_SMALL_STANDALONE_INDEX },
            { WAREHOUSE_LARGE_STANDALONE, WAREHOUSE_LARGE_STANDALONE_INDEX },
            { WAREHOUSE_MASSIVE_CUSTOM, WAREHOUSE_MASSIVE_CUSTOM_INDEX },
            { WORLD_MAP_SCENE, WORLD_MAP_SCENE_INDEX },
            { MARKETPLACE_SCENE, MARKETPLACE_SCENE_INDEX }
        };

        // Build index to scene name mapping
        public static readonly Dictionary<int, string> BuildIndexToScene = new Dictionary<int, string>
        {
            { BOOT_SCENE_INDEX, BOOT_SCENE },
            { MAIN_MENU_SCENE_INDEX, MAIN_MENU_SCENE },
            { WAREHOUSE_SMALL_BAY_INDEX, WAREHOUSE_SMALL_BAY },
            { WAREHOUSE_MEDIUM_BAY_INDEX, WAREHOUSE_MEDIUM_BAY },
            { WAREHOUSE_SMALL_STANDALONE_INDEX, WAREHOUSE_SMALL_STANDALONE },
            { WAREHOUSE_LARGE_STANDALONE_INDEX, WAREHOUSE_LARGE_STANDALONE },
            { WAREHOUSE_MASSIVE_CUSTOM_INDEX, WAREHOUSE_MASSIVE_CUSTOM },
            { WORLD_MAP_SCENE_INDEX, WORLD_MAP_SCENE },
            { MARKETPLACE_SCENE_INDEX, MARKETPLACE_SCENE }
        };

        public static readonly string[] ALL_SCENES = {
            BOOT_SCENE,
            MAIN_MENU_SCENE,
            WAREHOUSE_SMALL_BAY,
            WAREHOUSE_MEDIUM_BAY,
            WAREHOUSE_SMALL_STANDALONE,
            WAREHOUSE_LARGE_STANDALONE,
            WAREHOUSE_MASSIVE_CUSTOM,
            WORLD_MAP_SCENE,
            MARKETPLACE_SCENE
        };

        public static readonly string[] WAREHOUSE_SCENES = {
            WAREHOUSE_SMALL_BAY,
            WAREHOUSE_MEDIUM_BAY,
            WAREHOUSE_SMALL_STANDALONE,
            WAREHOUSE_LARGE_STANDALONE,
            WAREHOUSE_MASSIVE_CUSTOM
        };

        public static readonly string[] MENU_SCENES = {
            MAIN_MENU_SCENE,
            WORLD_MAP_SCENE,
            MARKETPLACE_SCENE
        };

        public static bool IsWarehouseScene(string sceneName)
        {
            return System.Array.Exists(WAREHOUSE_SCENES, scene => scene == sceneName);
        }

        public static bool IsMenuScene(string sceneName)
        {
            return System.Array.Exists(MENU_SCENES, scene => scene == sceneName);
        }

        public static bool IsValidScene(string sceneName)
        {
            return System.Array.Exists(ALL_SCENES, scene => scene == sceneName);
        }

        /// <summary>
        /// Get build index for a scene name
        /// </summary>
        public static int GetBuildIndex(string sceneName)
        {
            if (SceneToBuildIndex.TryGetValue(sceneName, out int buildIndex))
            {
                return buildIndex;
            }
            UnityEngine.Debug.LogError($"[SceneConstants] No build index found for scene '{sceneName}'");
            return -1;
        }

        /// <summary>
        /// Get scene name for a build index
        /// </summary>
        public static string GetSceneName(int buildIndex)
        {
            if (BuildIndexToScene.TryGetValue(buildIndex, out string sceneName))
            {
                return sceneName;
            }
            UnityEngine.Debug.LogError($"[SceneConstants] No scene name found for build index {buildIndex}");
            return null;
        }

        /// <summary>
        /// Check if a build index is valid
        /// </summary>
        public static bool IsValidBuildIndex(int buildIndex)
        {
            return BuildIndexToScene.ContainsKey(buildIndex);
        }
    }
}