using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.UI.Components;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles construction-specific contextual menu logic including facility placement,
    /// schematic management, and building operations.
    /// Extracted from ContextualMenuController.cs for better maintainability.
    /// </summary>
    public class ConstructionContextMenu : IModeContextualMenuProvider
    {
        // Construction State
        private readonly HashSet<string> _selectedFacilities = new HashSet<string>();
        
        // Data Provider and Components
        private readonly ConstructionDataProvider _dataProvider = new ConstructionDataProvider();
        private readonly ConstructionMenuActionHandler _actionHandler = new ConstructionMenuActionHandler();
        private readonly ConstructionMenuGenerator _menuGenerator;
        private readonly ConstructionPlacementManager _placementManager = new ConstructionPlacementManager();
        private readonly List<string> _recentOperations = new List<string>();
        
        // Events
        public event System.Action<string> OnFacilitySelected;
        public event System.Action<string> OnSchematicSelected;
        public event System.Action<string, string> OnConstructionActionTriggered;
        public event System.Action<List<string>> OnMenuItemsChanged;
        
        public string ProviderMode => "construction";
        public bool IsActive { get; private set; } = false;
        
        public ConstructionContextMenu()
        {
            // Initialize components
            _menuGenerator = new ConstructionMenuGenerator(_dataProvider);
            _actionHandler.ConnectToMainHandler((action, data) => OnConstructionActionTriggered?.Invoke(action, data));
            _placementManager.ConnectToMainHandlers(
                (facilityId) => OnFacilitySelected?.Invoke(facilityId),
                (schematicId) => OnSchematicSelected?.Invoke(schematicId),
                (action, data) => OnConstructionActionTriggered?.Invoke(action, data),
                (items) => OnMenuItemsChanged?.Invoke(items)
            );
        }
        
        
        /// <summary>
        /// Handles a menu item click event
        /// </summary>
        public void HandleItemClicked(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            _actionHandler.HandleItemClick(itemId);
            OnConstructionActionTriggered?.Invoke("item_clicked", itemId);
        }
        
        /// <summary>
        /// Gets menu items for the current construction context
        /// </summary>
        public List<string> GetMenuItems()
        {
            var menuItems = new List<string>();
            
            // Context-sensitive menu items based on current state
            if (_placementManager.IsInPlacementMode)
            {
                menuItems.AddRange(_menuGenerator.GetPlacementModeItems());
            }
            else if (_selectedFacilities.Count > 0)
            {
                menuItems.AddRange(_menuGenerator.GetSelectedFacilityItems(_selectedFacilities.Count));
            }
            else
            {
                menuItems.AddRange(_menuGenerator.GetDefaultConstructionItems(_recentOperations));
            }
            
            return menuItems;
        }
        
        
        /// <summary>
        /// Handles menu item selection
        /// </summary>
        public bool HandleMenuSelection(string menuItem)
        {
            try
            {
                // Handle placement mode actions
                if (_placementManager.IsInPlacementMode)
                {
                    return HandlePlacementModeSelection(menuItem);
                }
                
                // Handle facility-specific actions
                if (menuItem.StartsWith("Place "))
                {
                    return HandleFacilityPlacement(menuItem);
                }
                
                if (menuItem.StartsWith("Load "))
                {
                    return HandleSchematicLoading(menuItem);
                }
                
                // Handle general construction actions
                return HandleGeneralConstructionAction(menuItem);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[ConstructionContextMenu] Error handling menu selection: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Handles placement mode specific selections
        /// </summary>
        private bool HandlePlacementModeSelection(string menuItem)
        {
            switch (menuItem)
            {
                case "Confirm Placement":
                    return _placementManager.ConfirmPlacement();
                case "Cancel Placement":
                    return _placementManager.CancelPlacement();
                case "Rotate Facility":
                    return _actionHandler.RotateCurrentFacility();
                case "Change Facility Type":
                    return _actionHandler.ShowFacilityTypeSelector();
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Handles facility placement initiation
        /// </summary>
        private bool HandleFacilityPlacement(string menuItem)
        {
            var facilityName = menuItem.Substring(6); // Remove "Place " prefix
            var facilityInfo = _dataProvider.GetFacilityByDisplayName(facilityName);
            
            if (facilityInfo != null)
            {
                _placementManager.StartFacilityPlacement(facilityInfo.Id);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles schematic loading
        /// </summary>
        private bool HandleSchematicLoading(string menuItem)
        {
            var schematicName = menuItem.Substring(5); // Remove "Load " prefix
            var schematicInfo = _dataProvider.GetSchematicByDisplayName(schematicName);
            
            if (schematicInfo != null)
            {
                _placementManager.LoadSchematic(schematicInfo.Id);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles general construction actions
        /// </summary>
        private bool HandleGeneralConstructionAction(string menuItem)
        {
            switch (menuItem)
            {
                case "Place Facility":
                    return _actionHandler.ShowFacilitySelector();
                case "Load Schematic":
                    return _actionHandler.ShowSchematicSelector();
                case "Save Current Layout":
                    return _actionHandler.SaveCurrentLayout();
                case "Construction Manager":
                    return _actionHandler.OpenConstructionManager();
                case "View Facility Stats":
                    return _actionHandler.ShowFacilityStats();
                case "Move Facility":
                    return _actionHandler.StartFacilityMove();
                case "Rotate Facility":
                    return _actionHandler.RotateSelectedFacility();
                case "Upgrade Facility":
                    return _actionHandler.ShowUpgradeOptions();
                case "Delete Facility":
                    return _actionHandler.DeleteSelectedFacility();
                default:
                    ChimeraLogger.LogWarning($"[ConstructionContextMenu] Unhandled action: {menuItem}");
                    return false;
            }
        }
        
        
        /// <summary>
        /// Adds operation to recent operations history
        /// </summary>
        private void AddToRecentOperations(string operation)
        {
            _recentOperations.Insert(0, operation);
            
            // Limit history size
            const int maxRecentOperations = 10;
            if (_recentOperations.Count > maxRecentOperations)
            {
                _recentOperations.RemoveAt(_recentOperations.Count - 1);
            }
        }
        
        
        /// <summary>
        /// Sets facility selection state
        /// </summary>
        public void SetSelectedFacilities(List<string> facilityIds)
        {
            _selectedFacilities.Clear();
            _selectedFacilities.UnionWith(facilityIds);
            
            // Refresh menu items based on selection
            OnMenuItemsChanged?.Invoke(GetMenuItems());
        }
        
        /// <summary>
        /// Activates the construction context menu
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            ChimeraLogger.Log("[ConstructionContextMenu] Activated");
        }
        
        /// <summary>
        /// Deactivates the construction context menu
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            _placementManager.Reset();
            _selectedFacilities.Clear();
            
            ChimeraLogger.Log("[ConstructionContextMenu] Deactivated");
        }
        
        /// <summary>
        /// Gets recent operations history
        /// </summary>
        public List<string> GetRecentOperations()
        {
            return new List<string>(_recentOperations);
        }
        
        /// <summary>
        /// Gets available facilities info
        /// </summary>
        public Dictionary<string, FacilityInfo> GetAvailableFacilities()
        {
            return _dataProvider.GetAvailableFacilities();
        }
        
        /// <summary>
        /// Gets available schematics info
        /// </summary>
        public Dictionary<string, SchematicInfo> GetAvailableSchematics()
        {
            return _dataProvider.GetAvailableSchematics();
        }
    }
    
    
    /// <summary>
    /// Interface for mode-specific contextual menu providers
    /// </summary>
    public interface IModeContextualMenuProvider
    {
        string ProviderMode { get; }
        bool IsActive { get; }
        List<string> GetMenuItems();
        bool HandleMenuSelection(string menuItem);
        void Activate();
        void Deactivate();
    }
}