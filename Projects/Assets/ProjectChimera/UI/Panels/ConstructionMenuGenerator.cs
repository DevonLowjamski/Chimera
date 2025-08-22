using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Generates context-sensitive menu items for construction operations.
    /// Extracted from ConstructionContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class ConstructionMenuGenerator
    {
        private readonly ConstructionDataProvider _dataProvider;
        
        public ConstructionMenuGenerator(ConstructionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }
        
        /// <summary>
        /// Gets menu items for placement mode
        /// </summary>
        public List<string> GetPlacementModeItems()
        {
            return new List<string>
            {
                "Confirm Placement",
                "Cancel Placement",
                "Rotate Facility",
                "Change Facility Type"
            };
        }
        
        /// <summary>
        /// Gets menu items when facilities are selected
        /// </summary>
        public List<string> GetSelectedFacilityItems(int selectedFacilityCount)
        {
            var items = new List<string>
            {
                "Move Facility",
                "Rotate Facility",
                "Upgrade Facility",
                "Delete Facility"
            };
            
            if (selectedFacilityCount > 1)
            {
                items.Add("Group Selection");
                items.Add("Create Schematic");
            }
            else if (selectedFacilityCount == 1)
            {
                items.Add("Duplicate Facility");
                items.Add("Show Blueprint");
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets default construction menu items
        /// </summary>
        public List<string> GetDefaultConstructionItems(List<string> recentOperations)
        {
            var items = new List<string>();
            
            // Facility placement options
            items.Add("Place Facility");
            
            // Available facilities (filtered by requirements)
            var availableFacilities = _dataProvider.GetAvailableFacilities(true)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.Cost);
            
            foreach (var facility in availableFacilities)
            {
                items.Add($"Place {facility.DisplayName}");
            }
            
            // Schematic operations
            items.Add("─────────"); // Separator
            items.Add("Load Schematic");
            
            foreach (var schematic in _dataProvider.GetAvailableSchematics(true))
            {
                items.Add($"Load {schematic.DisplayName}");
            }
            
            // Advanced operations
            items.Add("─────────"); // Separator
            items.Add("Save Current Layout");
            items.Add("Construction Manager");
            items.Add("View Facility Stats");
            
            // Recent operations
            if (recentOperations.Count > 0)
            {
                items.Add("─────────"); // Separator
                items.Add("Recent Operations:");
                items.AddRange(recentOperations.Take(3));
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets facility placement items from data provider
        /// </summary>
        public List<string> GetFacilityPlacementItems()
        {
            var items = new List<string>();
            var availableFacilities = _dataProvider.GetAvailableFacilities(true)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.Cost);
            
            foreach (var facility in availableFacilities)
            {
                items.Add($"Place {facility.DisplayName}");
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets schematic loading items from data provider
        /// </summary>
        public List<string> GetSchematicLoadingItems()
        {
            var items = new List<string>();
            var availableSchematics = _dataProvider.GetAvailableSchematics(true);
            
            foreach (var schematic in availableSchematics)
            {
                items.Add($"Load {schematic.DisplayName}");
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets facilities grouped by category
        /// </summary>
        public Dictionary<string, List<string>> GetFacilitiesByCategory()
        {
            var result = new Dictionary<string, List<string>>();
            var facilities = _dataProvider.GetAvailableFacilities(true);
            
            var categories = facilities.Select(f => f.Category).Distinct().OrderBy(c => c);
            
            foreach (var category in categories)
            {
                var categoryFacilities = facilities
                    .Where(f => f.Category == category)
                    .OrderBy(f => f.Cost)
                    .Select(f => $"Place {f.DisplayName}")
                    .ToList();
                
                result[category] = categoryFacilities;
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets management operation items
        /// </summary>
        public List<string> GetManagementItems()
        {
            return new List<string>
            {
                "Construction Manager",
                "View Facility Stats",
                "Save Current Layout",
                "Export Layout",
                "Import Layout"
            };
        }
        
        /// <summary>
        /// Gets facility manipulation items
        /// </summary>
        public List<string> GetFacilityManipulationItems()
        {
            return new List<string>
            {
                "Move Facility",
                "Rotate Facility", 
                "Upgrade Facility",
                "Delete Facility",
                "Duplicate Facility",
                "Show Blueprint"
            };
        }
        
        /// <summary>
        /// Gets contextual items based on facility type
        /// </summary>
        public List<string> GetContextualItemsByFacilityType(string facilityType)
        {
            var items = new List<string>();
            
            switch (facilityType?.ToLower())
            {
                case "cultivation":
                    items.AddRange(new[] { "Configure Growing Parameters", "Set Climate Controls", "Schedule Maintenance" });
                    break;
                case "processing":
                    items.AddRange(new[] { "Configure Processing Pipeline", "Set Quality Controls", "Schedule Deep Clean" });
                    break;
                case "administration":
                    items.AddRange(new[] { "Configure Security Settings", "Set Access Controls", "Generate Reports" });
                    break;
                case "logistics":
                    items.AddRange(new[] { "Configure Storage Zones", "Set Inventory Limits", "Schedule Inventory Audit" });
                    break;
                default:
                    items.AddRange(new[] { "Configure Settings", "View Details", "Schedule Maintenance" });
                    break;
            }
            
            return items;
        }
    }
}