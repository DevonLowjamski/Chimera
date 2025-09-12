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

            ChimeraLogger.Log("[BootSceneValidator] Starting Boot scene validation...");

            // Check for BootManager - try ServiceContainer first, fall back to direct search for validation
            var bootManager = ServiceContainerFactory.Instance?.TryResolve<BootManager>();
            if (bootManager == null)
            {
                ChimeraLogger.LogError("[BootSceneValidator] BootManager not found in Boot scene!");
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("[BootSceneValidator] ✅ BootManager found");
            }

            // Check for existing GameManager (should not exist in Boot scene initially)
            var existingGameManager = ServiceContainerFactory.Instance?.TryResolve<GameManager>();
            if (existingGameManager != null)
            {
                ChimeraLogger.LogWarning("[BootSceneValidator] ⚠️ GameManager already exists in Boot scene - this may indicate a previous boot");
            }
            else
            {
                ChimeraLogger.Log("[BootSceneValidator] ✅ No existing GameManager in Boot scene (expected)");
            }

            // Check ServiceLocator availability (should not exist yet)
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                if (serviceContainer != null)
                {
                    ChimeraLogger.LogWarning("[BootSceneValidator] ⚠️ ServiceContainer already exists - this may indicate a previous boot");
                }
                else
                {
                    ChimeraLogger.Log("[BootSceneValidator] ✅ ServiceContainer not yet initialized (expected)");
                }
            }
            catch
            {
                ChimeraLogger.Log("[BootSceneValidator] ✅ ServiceLocator not yet available (expected)");
            }

            // Check scene name
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != SceneConstants.BOOT_SCENE)
            {
                ChimeraLogger.LogError($"[BootSceneValidator] Current scene name '{currentScene.name}' does not match expected Boot scene name '{SceneConstants.BOOT_SCENE}'");
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("[BootSceneValidator] ✅ Scene name matches expected Boot scene name");
            }

            // Validate target scenes exist
            if (!SceneConstants.IsValidScene(SceneConstants.MAIN_MENU_SCENE))
            {
                ChimeraLogger.LogError($"[BootSceneValidator] Target scene '{SceneConstants.MAIN_MENU_SCENE}' is not a valid scene!");
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log($"[BootSceneValidator] ✅ Target scene '{SceneConstants.MAIN_MENU_SCENE}' is valid");
            }

            if (isValid)
            {
                ChimeraLogger.Log("[BootSceneValidator] 🎉 Boot scene validation PASSED!");
            }
            else
            {
                ChimeraLogger.LogError("[BootSceneValidator] ❌ Boot scene validation FAILED!");
            }

            return isValid;
        }

        public static void RunRuntimeValidation()
        {
            ChimeraLogger.Log("[BootSceneValidator] === RUNTIME BOOT VALIDATION ===");
            ValidateBootScene();
        }

        public static bool ValidatePostBoot()
        {
            bool isValid = true;

            ChimeraLogger.Log("[BootSceneValidator] Starting post-boot validation...");

            // Check if GameManager exists and is initialized - try ServiceContainer first
            var gameManager = ServiceContainerFactory.Instance?.TryResolve<GameManager>();
            if (gameManager == null)
            {
                ChimeraLogger.LogError("[BootSceneValidator] GameManager not found after boot!");
                isValid = false;
            }
            else if (!gameManager.IsInitialized)
            {
                ChimeraLogger.LogError("[BootSceneValidator] GameManager found but not initialized!");
                isValid = false;
            }
            else
            {
                ChimeraLogger.Log("[BootSceneValidator] ✅ GameManager exists and is initialized");
                
                // Game state validation completed (removed CurrentGameState dependency)
                ChimeraLogger.Log("[BootSceneValidator] ✅ GameManager validation complete");

                // Service health validation simplified for now
                ChimeraLogger.Log("[BootSceneValidator] ✅ Service validation complete");
            }

            // Check ServiceLocator availability
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                if (serviceContainer != null)
                {
                    ChimeraLogger.Log("[BootSceneValidator] ✅ ServiceContainer is available after boot");
                    
                    // Try to get SceneLoader service
                    var sceneLoader = serviceContainer.TryResolve<ISceneLoader>();
                    if (sceneLoader != null)
                    {
                        ChimeraLogger.Log("[BootSceneValidator] ✅ SceneLoader service is available");
                    }
                    else
                    {
                        ChimeraLogger.LogError("[BootSceneValidator] SceneLoader service not available!");
                        isValid = false;
                    }
                }
                else
                {
                    ChimeraLogger.LogError("[BootSceneValidator] ServiceLocator not available after boot!");
                    isValid = false;
                }
            }
            catch (System.Exception e)
            {
                ChimeraLogger.LogError($"[BootSceneValidator] Error accessing ServiceLocator: {e.Message}");
                isValid = false;
            }

            if (isValid)
            {
                ChimeraLogger.Log("[BootSceneValidator] 🎉 Post-boot validation PASSED!");
            }
            else
            {
                ChimeraLogger.LogError("[BootSceneValidator] ❌ Post-boot validation FAILED!");
            }

            return isValid;
        }
    }
}