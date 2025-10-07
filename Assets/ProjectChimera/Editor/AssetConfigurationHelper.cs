using UnityEngine;
using UnityEditor;
using System.IO;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
using ProjectChimera.Shared;
using ProjectChimera.Data.UI;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Editor
{
    /// <summary>
    /// Editor utility to create properly configured ScriptableObject assets
    /// and prevent Unity Editor configuration errors.
    /// </summary>
    public static class AssetConfigurationHelper
    {
        private const string UI_DATA_BINDING_PATH = "Assets/ProjectChimera/Data/UI/Bindings/"; // legacy; kept for menu
        private const string EVENT_CHANNEL_PATH = "Assets/ProjectChimera/Core/Events/Channels/";
        
        [MenuItem("Project Chimera/Create Example UI Data Bindings")]
        public static void CreateExampleUIDataBindings()
        {
            // Ensure directory exists
            if (!Directory.Exists(UI_DATA_BINDING_PATH))
            {
                Directory.CreateDirectory(UI_DATA_BINDING_PATH);
            }
            
            // UI data binding examples removed in refactor; create placeholder asset if type exists
            ScriptableObject dummy = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(dummy, UI_DATA_BINDING_PATH + "PlaceholderBinding.asset");
            
            var tempEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(tempEvent, UI_DATA_BINDING_PATH + "TemperatureEvent.asset");
            
            var currencyEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(currencyEvent, UI_DATA_BINDING_PATH + "CurrencyEvent.asset");

            AssetDatabase.Refresh();
            ChimeraLogger.Log("OTHER", "Example UI Data Bindings created successfully");
        }
        
        [MenuItem("Project Chimera/Create Example Event Channels")]
        public static void CreateExampleEventChannels()
        {
            // Ensure directory exists
            if (!Directory.Exists(EVENT_CHANNEL_PATH))
            {
                Directory.CreateDirectory(EVENT_CHANNEL_PATH);
            }
            
            // Create Plant Harvested Event
            var plantHarvestedEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(plantHarvestedEvent, EVENT_CHANNEL_PATH + "PlantHarvestedEvent.asset");
            
            // Create Environmental Alert Event
            var environmentalAlertEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(environmentalAlertEvent, EVENT_CHANNEL_PATH + "EnvironmentalAlertEvent.asset");
            
            // Create Currency Changed Event
            var currencyChangedEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(currencyChangedEvent, EVENT_CHANNEL_PATH + "CurrencyChangedEvent.asset");
            
            // Create Equipment Malfunction Event
            var equipmentMalfunctionEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(equipmentMalfunctionEvent, EVENT_CHANNEL_PATH + "EquipmentMalfunctionEvent.asset");
            
            // Create Game State Changed Event
            var gameStateChangedEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(gameStateChangedEvent, EVENT_CHANNEL_PATH + "GameStateChangedEvent.asset");
            
            // Create Research Completed Event
            var researchCompletedEvent = ScriptableObject.CreateInstance<SimpleGameEventSO>();
            CreateAndSaveAsset(researchCompletedEvent, EVENT_CHANNEL_PATH + "ResearchCompletedEvent.asset");

            AssetDatabase.Refresh();
            ChimeraLogger.Log("OTHER", "Example Event Channels created successfully");
        }
        
        [MenuItem("Project Chimera/Fix All Asset Configuration Issues")]
        public static void FixAllAssetConfigurationIssues()
        {
            CreateExampleUIDataBindings();
            CreateExampleEventChannels();
            ValidateExistingAssets();

            ChimeraLogger.Log("OTHER", "All asset configuration issues fixed");
        }
        
        /// <summary>
        /// Creates and saves a ScriptableObject asset at the specified path
        /// </summary>
        private static void CreateAndSaveAsset(ScriptableObject asset, string path)
        {
            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
            {
                ChimeraLogger.Log("OTHER", "Asset already exists at path: " + path);
                return;
            }
            
            // Create the asset
            AssetDatabase.CreateAsset(asset, path);
            
            // Mark as dirty and save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            ChimeraLogger.Log("OTHER", "Created asset at path: " + path);
        }
        
        /// <summary>
        /// Validates existing assets and fixes common configuration issues
        /// </summary>
        private static void ValidateExistingAssets()
        {
            // UI bindings removed in refactor
            // No UIDataBindingSO in refactor; skip
            
            // Find all SimpleGameEventSO assets
            string[] eventGuids = AssetDatabase.FindAssets("t:SimpleGameEventSO");
            foreach (string guid in eventGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var eventChannel = AssetDatabase.LoadAssetAtPath<SimpleGameEventSO>(path);
                
                if (eventChannel != null)
                {
                    // Simple validation not needed for SimpleGameEventSO
                    EditorUtility.SetDirty(eventChannel);
                }
            }
            
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Validates a UI Data Binding asset and fixes common issues
        /// </summary>
        // Removed UI validation as UIDataBindingSO no longer exists in refactor
        
        /// <summary>
        /// Validates an Event Channel asset and fixes common issues
        /// </summary>
        private static void ValidateEventChannel(ChimeraEventSO eventChannel, string path)
        {
            bool needsUpdate = false;
            
            // Check if display name is empty
            if (string.IsNullOrEmpty(eventChannel.DisplayName))
            {
                eventChannel.SetDisplayNameFromAssetName();
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                EditorUtility.SetDirty(eventChannel);
                ChimeraLogger.Log("OTHER", "Validated event channel: " + path);
            }
        }
        
        /// <summary>
        /// Cleans up corrupted asset files that cause XML parsing errors
        /// </summary>
        [MenuItem("Project Chimera/Clean Up Corrupted Assets")]
        public static void CleanUpCorruptedAssets()
        {
            // Find all .asset files in the project
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            
            foreach (string path in assetPaths)
            {
                if (path.EndsWith(".asset"))
                {
                    // Try to load the asset
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    
                    if (asset == null)
                    {
                        // Asset file exists but can't be loaded - likely corrupted
                        ChimeraLogger.Log("OTHER", "Found corrupted asset at path: " + path);

                        // You can uncomment the following line to delete corrupted assets
                        // AssetDatabase.DeleteAsset(path);
                    }
                }
            }
            
            AssetDatabase.Refresh();
            ChimeraLogger.Log("OTHER", "Corrupted asset cleanup completed");
        }
    }
}