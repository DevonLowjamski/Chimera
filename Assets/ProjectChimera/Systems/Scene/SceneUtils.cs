using UnityEngine;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Scene
{
    public static class SceneUtils
    {
        public static ISceneLoader GetSceneLoader()
        {
            var serviceLocator = ServiceLocator.Instance;
            if (serviceLocator != null)
            {
                return serviceLocator.GetService<ISceneLoader>();
            }
            
            Debug.LogError("[SceneUtils] ServiceLocator not available - cannot get SceneLoader service");
            return null;
        }

        public static void GoToMainMenu()
        {
            var sceneLoader = GetSceneLoader();
            sceneLoader?.TransitionToScene(SceneConstants.MAIN_MENU_SCENE);
        }

        public static void GoToWorldMap()
        {
            var sceneLoader = GetSceneLoader();
            sceneLoader?.TransitionToScene(SceneConstants.WORLD_MAP_SCENE);
        }

        public static void GoToMarketplace()
        {
            var sceneLoader = GetSceneLoader();
            sceneLoader?.TransitionToScene(SceneConstants.MARKETPLACE_SCENE);
        }

        public static void GoToWarehouse(string warehouseType = SceneConstants.WAREHOUSE_SMALL_BAY)
        {
            if (!SceneConstants.IsWarehouseScene(warehouseType))
            {
                Debug.LogWarning($"[SceneUtils] Invalid warehouse scene: {warehouseType}, defaulting to Small Bay");
                warehouseType = SceneConstants.WAREHOUSE_SMALL_BAY;
            }

            var sceneLoader = GetSceneLoader();
            sceneLoader?.TransitionToScene(warehouseType);
        }

        public static void RestartCurrentScene()
        {
            var sceneLoader = GetSceneLoader();
            if (sceneLoader != null)
            {
                string currentScene = sceneLoader.CurrentActiveScene;
                if (!string.IsNullOrEmpty(currentScene))
                {
                    sceneLoader.TransitionToScene(currentScene);
                }
                else
                {
                    Debug.LogWarning("[SceneUtils] No active scene to restart");
                }
            }
        }

        public static void PreloadWarehouseScenes()
        {
            var sceneLoader = GetSceneLoader();
            if (sceneLoader != null)
            {
                foreach (var warehouseScene in SceneConstants.WAREHOUSE_SCENES)
                {
                    if (!sceneLoader.IsSceneLoaded(warehouseScene))
                    {
                        sceneLoader.LoadSceneAdditive(warehouseScene);
                    }
                }
            }
        }

        public static void UnloadUnusedScenes()
        {
            var sceneLoader = GetSceneLoader();
            if (sceneLoader != null)
            {
                string currentScene = sceneLoader.CurrentActiveScene;
                var loadedScenes = sceneLoader.LoadedScenes;

                foreach (var loadedScene in loadedScenes)
                {
                    if (loadedScene.Key != currentScene && loadedScene.Key != SceneConstants.BOOT_SCENE)
                    {
                        sceneLoader.UnloadScene(loadedScene.Key);
                    }
                }

                sceneLoader.ForceGarbageCollection();
            }
        }
    }
}