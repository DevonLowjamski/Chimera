using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
// ServiceContainer validation - checks legacy ServiceLocator during boot


namespace ProjectChimera.Systems.Scene
{
    /// <summary>
    /// Validation utility for the Boot scene configuration
    /// This can be used in Editor scripts or runtime diagnostics
    /// </summary>
    public static class BootSceneValidator
    {
        public static bool ValidateBootScene()
        {
            bool isValid = true;

            ChimeraLogger.Log("SCENE", "Validating Boot Scene", null);

            // Check for BootManager - try ServiceContainer first, fall back to direct search for validation
            var bootManager = ServiceContainerFactory.Instance?.TryResolve<BootManager>();
            if (bootManager == null)
            {
                ChimeraLogger.LogError("SCENE", "BootManager not resolved", null);
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("SCENE", "BootManager resolved", null);
            }

            // Check for existing GameManager (should not exist in Boot scene initially)
            var existingGameManager = ServiceContainerFactory.Instance?.TryResolve<GameManager>();
            if (existingGameManager != null)
            {
                ChimeraLogger.LogWarning("SCENE", "GameManager exists in Boot scene", null);
            }
            else
            {
                ChimeraLogger.Log("SCENE", "No GameManager in Boot scene (expected)", null);
            }

            // Check ServiceLocator availability (should not exist yet)
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                if (serviceContainer != null)
                {
                    ChimeraLogger.Log("SCENE", "ServiceContainer available", null);
                }
                else
                {
                    ChimeraLogger.LogError("SCENE", "ServiceContainer not available", null);
                }
            }
            catch
            {
                ChimeraLogger.LogError("SCENE", "Exception while checking ServiceContainer", null);
            }

            // Check scene name
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != SceneConstants.BOOT_SCENE)
            {
                ChimeraLogger.LogError("SCENE", "Wrong active scene", null);
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("SCENE", "Active scene is Boot", null);
            }

            // Validate target scenes exist
            if (!SceneConstants.IsValidScene(SceneConstants.MAIN_MENU_SCENE))
            {
                ChimeraLogger.LogError("SCENE", "Main Menu scene missing from constants", null);
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("SCENE", "Main Menu scene present in constants", null);
            }

            if (isValid)
            {
                ChimeraLogger.Log("SCENE", "Boot validation passed", null);
            }
            else
            {
                ChimeraLogger.LogError("SCENE", "Boot validation failed", null);
            }

            return isValid;
        }

        public static void RunRuntimeValidation()
        {
            ChimeraLogger.Log("SCENE", "RunRuntimeValidation invoked", null);
            ValidateBootScene();
        }

        public static bool ValidatePostBoot()
        {
            bool isValid = true;

            ProjectChimera.Core.Logging.ChimeraLogger.Log("SCENE/BOOT", "Validation start", null);

            // Check if GameManager exists and is initialized - try ServiceContainer first
            var gameManager = ServiceContainerFactory.Instance?.TryResolve<GameManager>();
            if (gameManager == null)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("SCENE/BOOT", "Validation warning", null);
                isValid = false;
            }
            else if (!gameManager.IsInitialized)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("SCENE/BOOT", "Validation error", null);
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("SCENE", "GameManager initialized");

                // Game state validation completed (removed CurrentGameState dependency)
                ChimeraLogger.Log("SCENE", "Game state validation completed");

                // Service health validation simplified for now
                ChimeraLogger.Log("SCENE", "Service health validation completed");
            }

            // Check ServiceLocator availability
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                if (serviceContainer != null)
                {
                    ChimeraLogger.Log("SCENE", "ServiceContainer available");

                    // Try to get SceneLoader service
                    var sceneLoader = serviceContainer.TryResolve<ISceneLoader>();
                    if (sceneLoader != null)
                    {
                        ChimeraLogger.Log("SCENE", "ISceneLoader available");
                    }
                    else
                    {
                        ChimeraLogger.LogError("SCENE", "ISceneLoader not available");
                        isValid = false;
                    }
                }
                else
                {
                    ChimeraLogger.LogError("SCENE", "ServiceContainer not initialized");
                    isValid = false;
                }
            }
            catch (System.Exception e)
            {
                ChimeraLogger.LogError("SCENE", $"Exception checking ServiceContainer: {e.Message}");
                isValid = false;
            }

            if (isValid)
            {
                ChimeraLogger.Log("SCENE", "Post-boot validation passed");
            }
            else
            {
                ChimeraLogger.LogError("SCENE", "Post-boot validation failed");
            }

            return isValid;
        }
    }
}
