using UnityEngine;
using UnityEditor;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Auto-executes BuildSettingsConfigurator when Unity starts up
    /// Ensures Build Settings are always properly configured for Project Chimera
    /// </summary>
    [InitializeOnLoad]
    public static class AutoConfigureBuildSettings
    {
        static AutoConfigureBuildSettings()
        {
            // Use EditorApplication.delayCall to ensure Unity is fully initialized
            EditorApplication.delayCall += ConfigureBuildSettingsOnStartup;
        }

        private static void ConfigureBuildSettingsOnStartup()
        {
            // Only run this once per editor session
            EditorApplication.delayCall -= ConfigureBuildSettingsOnStartup;

            // Check if Build Settings need configuration
            if (ShouldConfigureBuildSettings())
            {
                Debug.Log("[AutoConfigureBuildSettings] Auto-configuring Build Settings for Project Chimera...");
                BuildSettingsConfigurator.ConfigureBuildSettings();
            }
            else
            {
                Debug.Log("[AutoConfigureBuildSettings] Build Settings already configured correctly.");
            }
        }

        private static bool ShouldConfigureBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes;
            
            // If no scenes configured, definitely need to configure
            if (currentScenes == null || currentScenes.Length == 0)
            {
                Debug.Log("[AutoConfigureBuildSettings] No scenes in Build Settings - configuration needed.");
                return true;
            }

            // Check if first scene is our Boot scene
            if (currentScenes.Length > 0 && !currentScenes[0].path.Contains("01_Boot"))
            {
                Debug.Log("[AutoConfigureBuildSettings] Boot scene not at index 0 - configuration needed.");
                return true;
            }

            // If we have the expected number of scenes, probably configured correctly
            if (currentScenes.Length == 9) // Our 9 Project Chimera scenes
            {
                return false;
            }

            Debug.Log($"[AutoConfigureBuildSettings] Expected 9 scenes, found {currentScenes.Length} - configuration needed.");
            return true;
        }
    }
}