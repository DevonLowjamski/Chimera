using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages facility placement operations and schematic handling for construction context menu.
    /// Extracted from ConstructionContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class ConstructionPlacementManager
    {
        // Placement State
        private bool _isInPlacementMode = false;
        private string _selectedFacilityType = string.Empty;
        private string _selectedSchematic = string.Empty;
        
        // Events
        public event Action<string> OnFacilitySelected;
        public event Action<string> OnSchematicSelected;
        public event Action<string, string> OnConstructionActionTriggered;
        public event Action<List<string>> OnMenuItemsChanged;
        
        public bool IsInPlacementMode => _isInPlacementMode;
        public string SelectedFacilityType => _selectedFacilityType;
        public string SelectedSchematic => _selectedSchematic;
        
        /// <summary>
        /// Starts facility placement mode
        /// </summary>
        public void StartFacilityPlacement(string facilityId)
        {
            _isInPlacementMode = true;
            _selectedFacilityType = facilityId;
            
            OnFacilitySelected?.Invoke(facilityId);
            OnConstructionActionTriggered?.Invoke("start-placement", facilityId);
            
            Debug.Log($"[ConstructionPlacementManager] Started placement mode for: {facilityId}");
        }
        
        /// <summary>
        /// Confirms current facility placement
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!_isInPlacementMode) return false;
            
            _isInPlacementMode = false;
            OnConstructionActionTriggered?.Invoke("confirm-placement", _selectedFacilityType);
            
            var facilityType = _selectedFacilityType;
            _selectedFacilityType = string.Empty;
            
            Debug.Log("[ConstructionPlacementManager] Placement confirmed");
            return true;
        }
        
        /// <summary>
        /// Cancels current facility placement
        /// </summary>
        public bool CancelPlacement()
        {
            if (!_isInPlacementMode) return false;
            
            _isInPlacementMode = false;
            OnConstructionActionTriggered?.Invoke("cancel-placement", _selectedFacilityType);
            
            _selectedFacilityType = string.Empty;
            
            Debug.Log("[ConstructionPlacementManager] Placement canceled");
            return true;
        }
        
        /// <summary>
        /// Loads a schematic
        /// </summary>
        public void LoadSchematic(string schematicId)
        {
            _selectedSchematic = schematicId;
            
            OnSchematicSelected?.Invoke(schematicId);
            OnConstructionActionTriggered?.Invoke("load-schematic", schematicId);
            
            Debug.Log($"[ConstructionPlacementManager] Loaded schematic: {schematicId}");
        }
        
        /// <summary>
        /// Validates placement at specified coordinates
        /// </summary>
        public bool ValidatePlacement(float x, float y, string facilityId)
        {
            // Placeholder validation logic
            Debug.Log($"[ConstructionPlacementManager] Validating placement of {facilityId} at ({x}, {y})");
            
            // Basic validation - would integrate with actual placement system
            if (x < 0 || y < 0 || x > 100 || y > 100)
            {
                Debug.LogWarning("[ConstructionPlacementManager] Placement coordinates out of bounds");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates schematic compatibility
        /// </summary>
        public bool ValidateSchematicCompatibility(string schematicId)
        {
            // Placeholder compatibility logic
            Debug.Log($"[ConstructionPlacementManager] Validating schematic compatibility: {schematicId}");
            
            // Basic validation - would check available space, resources, etc.
            return !string.IsNullOrEmpty(schematicId);
        }
        
        /// <summary>
        /// Gets placement preview information
        /// </summary>
        public PlacementPreviewInfo GetPlacementPreview(string facilityId, float x, float y)
        {
            return new PlacementPreviewInfo
            {
                FacilityId = facilityId,
                X = x,
                Y = y,
                IsValid = ValidatePlacement(x, y, facilityId),
                EstimatedCost = GetEstimatedCost(facilityId),
                RequiredSkillLevel = GetRequiredSkillLevel(facilityId)
            };
        }
        
        /// <summary>
        /// Gets estimated cost for facility placement
        /// </summary>
        private int GetEstimatedCost(string facilityId)
        {
            // Placeholder cost calculation
            switch (facilityId?.ToLower())
            {
                case "greenhouse": return 50000;
                case "drying-room": return 25000;
                case "extraction-lab": return 150000;
                case "security-office": return 30000;
                default: return 10000;
            }
        }
        
        /// <summary>
        /// Gets required skill level for facility
        /// </summary>
        private int GetRequiredSkillLevel(string facilityId)
        {
            // Placeholder skill requirement
            switch (facilityId?.ToLower())
            {
                case "greenhouse": return 1;
                case "drying-room": return 2;
                case "extraction-lab": return 5;
                case "security-office": return 1;
                default: return 1;
            }
        }
        
        /// <summary>
        /// Resets all placement state
        /// </summary>
        public void Reset()
        {
            _isInPlacementMode = false;
            _selectedFacilityType = string.Empty;
            _selectedSchematic = string.Empty;
        }
        
        /// <summary>
        /// Connects events to main handlers
        /// </summary>
        public void ConnectToMainHandlers(
            Action<string> facilitySelected,
            Action<string> schematicSelected,
            Action<string, string> actionTriggered,
            Action<List<string>> menuItemsChanged)
        {
            OnFacilitySelected = facilitySelected;
            OnSchematicSelected = schematicSelected;
            OnConstructionActionTriggered = actionTriggered;
            OnMenuItemsChanged = menuItemsChanged;
        }
        
        /// <summary>
        /// Gets current placement status
        /// </summary>
        public PlacementStatus GetPlacementStatus()
        {
            return new PlacementStatus
            {
                IsInPlacementMode = _isInPlacementMode,
                SelectedFacilityType = _selectedFacilityType,
                SelectedSchematic = _selectedSchematic,
                CanConfirmPlacement = _isInPlacementMode && !string.IsNullOrEmpty(_selectedFacilityType),
                CanCancelPlacement = _isInPlacementMode
            };
        }
    }
    
    /// <summary>
    /// Information about placement preview
    /// </summary>
    public class PlacementPreviewInfo
    {
        public string FacilityId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsValid { get; set; }
        public int EstimatedCost { get; set; }
        public int RequiredSkillLevel { get; set; }
    }
    
    /// <summary>
    /// Current placement status information
    /// </summary>
    public class PlacementStatus
    {
        public bool IsInPlacementMode { get; set; }
        public string SelectedFacilityType { get; set; }
        public string SelectedSchematic { get; set; }
        public bool CanConfirmPlacement { get; set; }
        public bool CanCancelPlacement { get; set; }
    }
}