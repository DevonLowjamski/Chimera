using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Systems.Camera;
using ProjectChimera.Data.Camera;
using ProjectChimera.Data.Events;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Menus
{
    /// <summary>
    /// Provides context-aware menu actions based on current mode, selected objects, and camera level.
    /// Handles action validation, execution delegation, and menu item filtering.
    /// </summary>
    public class MenuActionProvider : MonoBehaviour
    {
        private MenuCore _menuCore;

        // Menu item collections
        private Dictionary<GameplayMode, List<ContextMenuItem>> _modeMenuItems;
        private Dictionary<string, List<ContextMenuItem>> _objectTypeMenuItems;

        // Camera level integration
        private CameraLevelContextualMenuIntegrator _cameraIntegrator;

        public void Initialize(MenuCore menuCore)
        {
            _menuCore = menuCore;
            SetupMenuItems();

            // Find camera level integrator
            _cameraIntegrator = ServiceContainerFactory.Instance?.TryResolve<CameraLevelContextualMenuIntegrator>();
        }

        public void OnModeChanged(GameplayMode newMode)
        {
            // Update any mode-specific behavior if needed
            LogDebug($"Action provider updated for mode: {newMode}");
        }

        public List<ContextMenuItem> GetValidMenuItems(GameObject targetObject)
        {
            List<ContextMenuItem> validItems = new List<ContextMenuItem>();

            // Add camera level-specific menu items first
            AddCameraLevelMenuItems(validItems, targetObject);

            // Add mode-specific menu items
            AddModeSpecificMenuItems(validItems, targetObject);

            // Add object-specific menu items
            AddObjectSpecificMenuItems(validItems, targetObject);

            return validItems;
        }

        public void ExecuteMenuAction(string actionName, GameObject targetObject)
        {
            // Check if this is a camera level action first
            if (IsCameraLevelAction(actionName))
            {
                ExecuteCameraLevelAction(actionName, targetObject);
                return;
            }

            // Execute based on action category
            switch (GetActionCategory(actionName))
            {
                case ActionCategory.Cultivation:
                    ExecuteCultivationAction(actionName, targetObject);
                    break;
                case ActionCategory.Construction:
                    ExecuteConstructionAction(actionName, targetObject);
                    break;
                case ActionCategory.Genetics:
                    ExecuteGeneticsAction(actionName, targetObject);
                    break;
                case ActionCategory.Object:
                    ExecuteObjectAction(actionName, targetObject);
                    break;
                default:
                    LogWarning($"Unknown action category for: {actionName}");
                    break;
            }
        }

        private void SetupMenuItems()
        {
            SetupModeMenuItems();
            SetupObjectTypeMenuItems();
        }

        private void SetupModeMenuItems()
        {
            _modeMenuItems = new Dictionary<GameplayMode, List<ContextMenuItem>>
            {
                { GameplayMode.Cultivation, CreateCultivationMenuItems() },
                { GameplayMode.Construction, CreateConstructionMenuItems() },
                { GameplayMode.Genetics, CreateGeneticsMenuItems() }
            };
        }

        private List<ContextMenuItem> CreateCultivationMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Water Plant", "WaterPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Add Nutrients", "AddNutrients") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Inspect Plant", "InspectPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Harvest Plant", "HarvestPlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Prune Plant", "PrunePlant") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Plant Seed", "PlantSeed"),
                new ContextMenuItem("Check Environment", "CheckEnvironment"),
                new ContextMenuItem("Schedule Care", "ScheduleCare")
            };
        }

        private List<ContextMenuItem> CreateConstructionMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Place Wall", "PlaceWall"),
                new ContextMenuItem("Add Door", "AddDoor"),
                new ContextMenuItem("Install Window", "InstallWindow"),
                new ContextMenuItem("Add Equipment", "AddEquipment"),
                new ContextMenuItem("Edit Blueprint", "EditBlueprint"),
                new ContextMenuItem("Remove Structure", "RemoveStructure") { requiresSelection = true, validObjectTypes = new[] { "Wall", "Door", "Window", "Equipment" } },
                new ContextMenuItem("View Utilities", "ViewUtilities"),
                new ContextMenuItem("Check Connections", "CheckConnections") { requiresSelection = true, validObjectTypes = new[] { "Equipment" } }
            };
        }

        private List<ContextMenuItem> CreateGeneticsMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Analyze Genetics", "AnalyzeGenetics") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("View Lineage", "ViewLineage") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Start Breeding", "StartBreeding"),
                new ContextMenuItem("Compare Traits", "CompareTraits") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Create Cross", "CreateCross"),
                new ContextMenuItem("View Phenotype", "ViewPhenotype") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Track Inheritance", "TrackInheritance") { requiresSelection = true, validObjectTypes = new[] { "Plant" } },
                new ContextMenuItem("Export Genetics", "ExportGenetics") { requiresSelection = true, validObjectTypes = new[] { "Plant" } }
            };
        }

        private void SetupObjectTypeMenuItems()
        {
            _objectTypeMenuItems = new Dictionary<string, List<ContextMenuItem>>
            {
                { "Plant", CreatePlantMenuItems() },
                { "Equipment", CreateEquipmentMenuItems() },
                { "Facility", CreateFacilityMenuItems() }
            };
        }

        private List<ContextMenuItem> CreatePlantMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Select Plant", "SelectPlant"),
                new ContextMenuItem("Move Plant", "MovePlant"),
                new ContextMenuItem("Clone Plant", "ClonePlant") { validModes = new[] { GameplayMode.Genetics } },
                new ContextMenuItem("Remove Plant", "RemovePlant"),
                new ContextMenuItem("Tag Plant", "TagPlant")
            };
        }

        private List<ContextMenuItem> CreateEquipmentMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Configure Equipment", "ConfigureEquipment"),
                new ContextMenuItem("Move Equipment", "MoveEquipment") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("Repair Equipment", "RepairEquipment"),
                new ContextMenuItem("Upgrade Equipment", "UpgradeEquipment"),
                new ContextMenuItem("Remove Equipment", "RemoveEquipment") { validModes = new[] { GameplayMode.Construction } }
            };
        }

        private List<ContextMenuItem> CreateFacilityMenuItems()
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem("Expand Facility", "ExpandFacility") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("Modify Layout", "ModifyLayout") { validModes = new[] { GameplayMode.Construction } },
                new ContextMenuItem("View Statistics", "ViewStatistics"),
                new ContextMenuItem("Facility Settings", "FacilitySettings")
            };
        }

        private void AddCameraLevelMenuItems(List<ContextMenuItem> validItems, GameObject targetObject)
        {
            if (_cameraIntegrator != null && _cameraIntegrator.AreLevelBasedMenusEnabled)
            {
                var cameraLevelItems = _cameraIntegrator.GetActiveMenuItems();
                foreach (var cameraItem in cameraLevelItems)
                {
                    // Convert camera level menu item to context menu item
                    var contextItem = new ContextMenuItem(cameraItem.displayName, cameraItem.actionName);
                    contextItem.requiresSelection = cameraItem.requiresTarget;
                    if (!string.IsNullOrEmpty(cameraItem.targetTag))
                    {
                        contextItem.validObjectTypes = new[] { cameraItem.targetTag };
                    }

                    if (IsMenuItemValid(contextItem, targetObject))
                    {
                        validItems.Add(contextItem);
                    }
                }
            }
        }

        private void AddModeSpecificMenuItems(List<ContextMenuItem> validItems, GameObject targetObject)
        {
            if (_modeMenuItems.TryGetValue(_menuCore.CurrentMode, out var modeItems))
            {
                foreach (var item in modeItems)
                {
                    if (IsMenuItemValid(item, targetObject))
                    {
                        validItems.Add(item);
                    }
                }
            }
        }

        private void AddObjectSpecificMenuItems(List<ContextMenuItem> validItems, GameObject targetObject)
        {
            if (targetObject != null && _menuCore.ShowObjectSpecificActions)
            {
                string objectType = GetObjectType(targetObject);
                if (_objectTypeMenuItems.TryGetValue(objectType, out var objectItems))
                {
                    foreach (var item in objectItems)
                    {
                        if (IsMenuItemValid(item, targetObject))
                        {
                            validItems.Add(item);
                        }
                    }
                }
            }
        }

        private bool IsMenuItemValid(ContextMenuItem item, GameObject targetObject)
        {
            // Check if item is enabled
            if (!item.isEnabled) return false;

            // Check if item requires selection but no object is selected
            if (item.requiresSelection && targetObject == null) return false;

            // Check if current mode is valid for this item
            bool modeValid = item.validModes.Contains(_menuCore.CurrentMode);
            if (!modeValid) return false;

            // Check if object type is valid for this item
            if (targetObject != null && item.validObjectTypes.Length > 0)
            {
                string objectType = GetObjectType(targetObject);
                bool objectTypeValid = item.validObjectTypes.Contains(objectType);
                if (!objectTypeValid) return false;
            }

            return true;
        }

        private string GetObjectType(GameObject obj)
        {
            // Determine object type based on tags or component types
            if (obj.CompareTag("Plant")) return "Plant";
            if (obj.CompareTag("Equipment")) return "Equipment";
            if (obj.CompareTag("Facility")) return "Facility";
            if (obj.name.Contains("Wall") || obj.name.Contains("Door") || obj.name.Contains("Window"))
                return obj.name.Split('_')[0];

            return "Unknown";
        }

        private ActionCategory GetActionCategory(string actionName)
        {
            // Cultivation actions
            if (actionName.Contains("Plant") || actionName.Contains("Water") || actionName.Contains("Nutrient") ||
                actionName.Contains("Harvest") || actionName.Contains("Prune") || actionName.Contains("Environment") ||
                actionName.Contains("Care"))
            {
                return ActionCategory.Cultivation;
            }

            // Construction actions
            if (actionName.Contains("Place") || actionName.Contains("Build") || actionName.Contains("Wall") ||
                actionName.Contains("Door") || actionName.Contains("Window") || actionName.Contains("Blueprint") ||
                actionName.Contains("Structure") || actionName.Contains("Utilities") || actionName.Contains("Connection"))
            {
                return ActionCategory.Construction;
            }

            // Genetics actions
            if (actionName.Contains("Genetics") || actionName.Contains("Lineage") || actionName.Contains("Breeding") ||
                actionName.Contains("Traits") || actionName.Contains("Cross") || actionName.Contains("Phenotype") ||
                actionName.Contains("Inheritance"))
            {
                return ActionCategory.Genetics;
            }

            // Object actions
            if (actionName.Contains("Select") || actionName.Contains("Move") || actionName.Contains("Clone") ||
                actionName.Contains("Remove") || actionName.Contains("Tag") || actionName.Contains("Configure") ||
                actionName.Contains("Repair") || actionName.Contains("Upgrade") || actionName.Contains("Expand") ||
                actionName.Contains("Modify") || actionName.Contains("Statistics") || actionName.Contains("Settings"))
            {
                return ActionCategory.Object;
            }

            return ActionCategory.Unknown;
        }

        private bool IsCameraLevelAction(string actionName)
        {
            return actionName.StartsWith("ZoomTo") || actionName.StartsWith("FocusOn") ||
                   actionName.Contains("Overview") || actionName.Contains("Environment") ||
                   actionName.Contains("Controls") || actionName.Contains("Layout") ||
                   actionName.Contains("System") || actionName.Contains("Details") ||
                   actionName.Contains("Health") || actionName.Contains("Progress") ||
                   actionName.Contains("Info") || actionName.Contains("Actions") ||
                   actionName.Contains("Stats") || actionName.Contains("Settings");
        }

        private void ExecuteCameraLevelAction(string actionName, GameObject targetObject)
        {
            if (_cameraIntegrator != null)
            {
                Transform target = targetObject ? targetObject.transform : null;
                _cameraIntegrator.ExecuteCameraLevelAction(actionName, target);
            }
        }

        private void ExecuteCultivationAction(string actionName, GameObject targetObject)
        {
            LogDebug($"Executing cultivation action '{actionName}' on object: {(targetObject ? targetObject.name : "None")}");
            // Placeholder for actual cultivation system integration
        }

        private void ExecuteConstructionAction(string actionName, GameObject targetObject)
        {
            LogDebug($"Executing construction action '{actionName}' on object: {(targetObject ? targetObject.name : "None")}");
            // Placeholder for actual construction system integration
        }

        private void ExecuteGeneticsAction(string actionName, GameObject targetObject)
        {
            LogDebug($"Executing genetics action '{actionName}' on object: {(targetObject ? targetObject.name : "None")}");
            // Placeholder for actual genetics system integration
        }

        private void ExecuteObjectAction(string actionName, GameObject targetObject)
        {
            LogDebug($"Executing object action '{actionName}' on object: {(targetObject ? targetObject.name : "None")}");
            // Placeholder for actual object manipulation system integration
        }

        private void LogDebug(string message)
        {
            if (_menuCore.DebugMode)
            {
                ChimeraLogger.Log($"[MenuActionProvider] {message}");
            }
        }

        private void LogWarning(string message)
        {
            ChimeraLogger.LogWarning($"[MenuActionProvider] {message}");
        }

        private enum ActionCategory
        {
            Cultivation,
            Construction,
            Genetics,
            Object,
            Unknown
        }
    }
}
