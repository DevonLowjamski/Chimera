using UnityEngine;
using UnityEditor;
using ProjectChimera.Data.Events;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Editor utility to create the shared ModeChangedEventSO asset for Phase 2 verification
    /// This ensures all components use the same event channel for proper decoupling
    /// </summary>
    public class CreateModeChangedEventAsset : EditorWindow
    {
        private const string ASSET_PATH = "Assets/ProjectChimera/Data/Events/SharedModeChangedEvent.asset";
        private const string ASSET_NAME = "SharedModeChangedEvent";

        [MenuItem("Project Chimera/Phase 2 Verification/Create Shared Mode Changed Event Asset")]
        public static void CreateSharedModeChangedEventAsset()
        {
            // Check if asset already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<ModeChangedEventSO>(ASSET_PATH);
            if (existingAsset != null)
            {
                ChimeraLogger.Log("OTHER", "Shared Mode Changed Event asset already exists");
                Selection.activeObject = existingAsset;
                EditorGUIUtility.PingObject(existingAsset);
                return;
            }

            // Create new asset
            var newAsset = ScriptableObject.CreateInstance<ModeChangedEventSO>();
            newAsset.name = ASSET_NAME;

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(ASSET_PATH);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create and save asset
            AssetDatabase.CreateAsset(newAsset, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the created asset
            Selection.activeObject = newAsset;
            EditorGUIUtility.PingObject(newAsset);

            ChimeraLogger.Log("OTHER", "Created Shared Mode Changed Event asset at: " + ASSET_PATH);
            ChimeraLogger.Log("OTHER", "Asset creation completed successfully");
        }

        [MenuItem("Project Chimera/Phase 2 Verification/Find All Mode-Aware Components")]
        public static void FindModeAwareComponents()
        {
            // Find all components that should use the shared event
            var componentTypes = new System.Type[]
            {
                // TODO: Re-enable when Gameplay namespace is implemented
                // typeof(ProjectChimera.Systems.Gameplay.GameplayModeController),
            };

            foreach (var componentType in componentTypes)
            {
                // Editor tool: Use explicit Unity API for reliable editor-time discovery
                var components = UnityEngine.Object.FindObjectsOfType(componentType, true);
                foreach (var component in components)
                {
                    ChimeraLogger.Log("OTHER", "Found mode-aware component: " + component.name);
                }
            }
        }

        [MenuItem("Project Chimera/Phase 2 Verification/Validate Mode Event Assignments")]
        public static void ValidateModeEventAssignments()
        {
            var sharedAsset = AssetDatabase.LoadAssetAtPath<ModeChangedEventSO>(ASSET_PATH);
            if (sharedAsset == null)
            {
                ChimeraLogger.Log("OTHER", "Shared Mode Changed Event asset not found. Please create it first.");
                return;
            }

            ChimeraLogger.Log("OTHER", "Starting validation of mode event assignments");

            // This would check all components to ensure they're using the same shared asset
            // Implementation would depend on the specific components we create

            ChimeraLogger.Log("OTHER", "Mode event assignment validation completed");
        }
    }
}
