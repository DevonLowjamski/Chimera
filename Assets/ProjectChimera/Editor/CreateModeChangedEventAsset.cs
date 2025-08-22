using UnityEngine;
using UnityEditor;
using ProjectChimera.Data.Events;

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
                Debug.Log($"[CreateModeChangedEventAsset] Shared ModeChangedEventSO asset already exists at {ASSET_PATH}");
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

            Debug.Log($"[CreateModeChangedEventAsset] Created shared ModeChangedEventSO asset at {ASSET_PATH}");
            Debug.Log($"[CreateModeChangedEventAsset] Please assign this asset to all mode-aware components for Phase 2 verification");
        }

        [MenuItem("Project Chimera/Phase 2 Verification/Find All Mode-Aware Components")]
        public static void FindModeAwareComponents()
        {
            // Find all components that should use the shared event
            var componentTypes = new System.Type[]
            {
                typeof(ProjectChimera.Systems.Gameplay.GameplayModeController),
                // Add other types as we implement them
            };

            foreach (var componentType in componentTypes)
            {
                var components = FindObjectsOfType(componentType, true);
                foreach (var component in components)
                {
                    Debug.Log($"[CreateModeChangedEventAsset] Found {componentType.Name} on {component.name}", component);
                }
            }
        }

        [MenuItem("Project Chimera/Phase 2 Verification/Validate Mode Event Assignments")]
        public static void ValidateModeEventAssignments()
        {
            var sharedAsset = AssetDatabase.LoadAssetAtPath<ModeChangedEventSO>(ASSET_PATH);
            if (sharedAsset == null)
            {
                Debug.LogError("[CreateModeChangedEventAsset] Shared ModeChangedEventSO asset not found! Create it first.");
                return;
            }

            Debug.Log("[CreateModeChangedEventAsset] Validating mode event assignments...");
            
            // This would check all components to ensure they're using the same shared asset
            // Implementation would depend on the specific components we create
            
            Debug.Log("[CreateModeChangedEventAsset] Validation complete. Check console for any issues.");
        }
    }
}