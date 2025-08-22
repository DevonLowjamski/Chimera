using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;


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

            Debug.Log("[BootSceneValidator] Starting Boot scene validation...");

            // Check for BootManager
            var bootManager = Object.FindObjectOfType<BootManager>();
            if (bootManager == null)
            {
                Debug.LogError("[BootSceneValidator] BootManager not found in Boot scene!");
                isValid = false;
            }
            else
            {
                Debug.Log("[BootSceneValidator] ✅ BootManager found");
            }

            // Check for existing DIGameManager (should not exist in Boot scene initially)
            var existingDIGameManager = Object.FindObjectOfType<DIGameManager>();
            if (existingDIGameManager != null)
            {
                Debug.LogWarning("[BootSceneValidator] ⚠️ DIGameManager already exists in Boot scene - this may indicate a previous boot");
            }
            else
            {
                Debug.Log("[BootSceneValidator] ✅ No existing DIGameManager in Boot scene (expected)");
            }

            // Check ServiceLocator availability (should not exist yet)
            try
            {
                var serviceLocator = ServiceLocator.Instance;
                if (serviceLocator != null)
                {
                    Debug.LogWarning("[BootSceneValidator] ⚠️ ServiceLocator already exists - this may indicate a previous boot");
                }
                else
                {
                    Debug.Log("[BootSceneValidator] ✅ ServiceLocator not yet initialized (expected)");
                }
            }
            catch
            {
                Debug.Log("[BootSceneValidator] ✅ ServiceLocator not yet available (expected)");
            }

            // Check scene name
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != SceneConstants.BOOT_SCENE)
            {
                Debug.LogError($"[BootSceneValidator] Current scene name '{currentScene.name}' does not match expected Boot scene name '{SceneConstants.BOOT_SCENE}'");
                isValid = false;
            }
            else
            {
                Debug.Log("[BootSceneValidator] ✅ Scene name matches expected Boot scene name");
            }

            // Validate target scenes exist
            if (!SceneConstants.IsValidScene(SceneConstants.MAIN_MENU_SCENE))
            {
                Debug.LogError($"[BootSceneValidator] Target scene '{SceneConstants.MAIN_MENU_SCENE}' is not a valid scene!");
                isValid = false;
            }
            else
            {
                Debug.Log($"[BootSceneValidator] ✅ Target scene '{SceneConstants.MAIN_MENU_SCENE}' is valid");
            }

            if (isValid)
            {
                Debug.Log("[BootSceneValidator] 🎉 Boot scene validation PASSED!");
            }
            else
            {
                Debug.LogError("[BootSceneValidator] ❌ Boot scene validation FAILED!");
            }

            return isValid;
        }

        public static void RunRuntimeValidation()
        {
            Debug.Log("[BootSceneValidator] === RUNTIME BOOT VALIDATION ===");
            ValidateBootScene();
        }

        public static bool ValidatePostBoot()
        {
            bool isValid = true;

            Debug.Log("[BootSceneValidator] Starting post-boot validation...");

            // Check if DIGameManager exists and is initialized
            var diGameManager = Object.FindObjectOfType<DIGameManager>();
            if (diGameManager == null)
            {
                Debug.LogError("[BootSceneValidator] DIGameManager not found after boot!");
                isValid = false;
            }
            else if (!diGameManager.IsInitialized)
            {
                Debug.LogError("[BootSceneValidator] DIGameManager found but not initialized!");
                isValid = false;
            }
            else
            {
                Debug.Log("[BootSceneValidator] ✅ DIGameManager exists and is initialized");
                
                // Validate game state
                if (diGameManager.CurrentGameState == GameState.Running)
                {
                    Debug.Log("[BootSceneValidator] ✅ DIGameManager reached GameState.Running");
                }
                else
                {
                    Debug.LogError($"[BootSceneValidator] DIGameManager in unexpected state: {diGameManager.CurrentGameState}");
                    isValid = false;
                }

                // Validate service health
                var healthReport = diGameManager.GetServiceHealthReport();
                if (healthReport.IsHealthy)
                {
                    Debug.Log("[BootSceneValidator] ✅ Service health check passed");
                }
                else
                {
                    Debug.LogWarning($"[BootSceneValidator] ⚠️ Service health issues: {healthReport.CriticalErrors.Count} errors");
                }
            }

            // Check ServiceLocator availability
            try
            {
                var serviceLocator = ServiceLocator.Instance;
                if (serviceLocator != null)
                {
                    Debug.Log("[BootSceneValidator] ✅ ServiceLocator is available after boot");
                    
                    // Try to get SceneLoader service
                    var sceneLoader = serviceLocator.GetService<ISceneLoader>();
                    if (sceneLoader != null)
                    {
                        Debug.Log("[BootSceneValidator] ✅ SceneLoader service is available");
                    }
                    else
                    {
                        Debug.LogError("[BootSceneValidator] SceneLoader service not available!");
                        isValid = false;
                    }
                }
                else
                {
                    Debug.LogError("[BootSceneValidator] ServiceLocator not available after boot!");
                    isValid = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BootSceneValidator] Error accessing ServiceLocator: {e.Message}");
                isValid = false;
            }

            if (isValid)
            {
                Debug.Log("[BootSceneValidator] 🎉 Post-boot validation PASSED!");
            }
            else
            {
                Debug.LogError("[BootSceneValidator] ❌ Post-boot validation FAILED!");
            }

            return isValid;
        }
    }
}