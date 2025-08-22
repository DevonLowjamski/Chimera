using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Provides facility and schematic data for construction operations.
    /// Extracted from ConstructionContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class ConstructionDataProvider
    {
        // Available Facilities and Operations
        private readonly Dictionary<string, FacilityInfo> _availableFacilities = new Dictionary<string, FacilityInfo>();
        private readonly Dictionary<string, SchematicInfo> _availableSchematics = new Dictionary<string, SchematicInfo>();
        
        public ConstructionDataProvider()
        {
            InitializeAvailableFacilities();
            InitializeAvailableSchematics();
        }
        
        /// <summary>
        /// Initializes available facilities for construction
        /// </summary>
        private void InitializeAvailableFacilities()
        {
            var facilities = new Dictionary<string, FacilityInfo>
            {
                ["greenhouse"] = new FacilityInfo
                {
                    Id = "greenhouse",
                    DisplayName = "Greenhouse",
                    Description = "Basic growing facility for cannabis cultivation",
                    Category = "cultivation",
                    Cost = 50000,
                    RequiredSkillLevel = 1,
                    IsAvailable = true
                },
                ["drying-room"] = new FacilityInfo
                {
                    Id = "drying-room",
                    DisplayName = "Drying Room",
                    Description = "Controlled environment for post-harvest drying",
                    Category = "processing",
                    Cost = 25000,
                    RequiredSkillLevel = 2,
                    IsAvailable = true
                },
                ["extraction-lab"] = new FacilityInfo
                {
                    Id = "extraction-lab",
                    DisplayName = "Extraction Lab",
                    Description = "Advanced facility for concentrate production",
                    Category = "processing",
                    Cost = 150000,
                    RequiredSkillLevel = 5,
                    IsAvailable = false // Locked until skill requirement met
                },
                ["security-office"] = new FacilityInfo
                {
                    Id = "security-office",
                    DisplayName = "Security Office",
                    Description = "Required for compliance and monitoring",
                    Category = "administration",
                    Cost = 30000,
                    RequiredSkillLevel = 1,
                    IsAvailable = true
                },
                ["storage-warehouse"] = new FacilityInfo
                {
                    Id = "storage-warehouse",
                    DisplayName = "Storage Warehouse",
                    Description = "Large-scale inventory storage facility",
                    Category = "logistics",
                    Cost = 75000,
                    RequiredSkillLevel = 3,
                    IsAvailable = true
                },
                ["packaging-facility"] = new FacilityInfo
                {
                    Id = "packaging-facility",
                    DisplayName = "Packaging Facility",
                    Description = "Automated packaging and labeling system",
                    Category = "processing",
                    Cost = 95000,
                    RequiredSkillLevel = 4,
                    IsAvailable = true
                },
                ["quality-control-lab"] = new FacilityInfo
                {
                    Id = "quality-control-lab",
                    DisplayName = "Quality Control Lab",
                    Description = "Testing and analysis laboratory",
                    Category = "analysis",
                    Cost = 120000,
                    RequiredSkillLevel = 4,
                    IsAvailable = false
                }
            };
            
            foreach (var facility in facilities)
            {
                _availableFacilities[facility.Key] = facility.Value;
            }
        }
        
        /// <summary>
        /// Initializes available schematics for construction
        /// </summary>
        private void InitializeAvailableSchematics()
        {
            var schematics = new Dictionary<string, SchematicInfo>
            {
                ["starter-facility"] = new SchematicInfo
                {
                    Id = "starter-facility",
                    DisplayName = "Starter Facility",
                    Description = "Basic setup for new cultivators",
                    FacilityCount = 3,
                    TotalCost = 100000,
                    IsCustom = false,
                    IsAvailable = true
                },
                ["commercial-operation"] = new SchematicInfo
                {
                    Id = "commercial-operation",
                    DisplayName = "Commercial Operation",
                    Description = "Large-scale commercial growing setup",
                    FacilityCount = 12,
                    TotalCost = 750000,
                    IsCustom = false,
                    IsAvailable = true
                },
                ["processing-complex"] = new SchematicInfo
                {
                    Id = "processing-complex",
                    DisplayName = "Processing Complex",
                    Description = "Complete post-harvest processing facility",
                    FacilityCount = 8,
                    TotalCost = 500000,
                    IsCustom = false,
                    IsAvailable = true
                },
                ["research-facility"] = new SchematicInfo
                {
                    Id = "research-facility",
                    DisplayName = "Research Facility",
                    Description = "Advanced genetics and breeding research complex",
                    FacilityCount = 6,
                    TotalCost = 400000,
                    IsCustom = false,
                    IsAvailable = false // Requires unlock
                }
            };
            
            foreach (var schematic in schematics)
            {
                _availableSchematics[schematic.Key] = schematic.Value;
            }
        }
        
        /// <summary>
        /// Gets all available facilities
        /// </summary>
        public Dictionary<string, FacilityInfo> GetAvailableFacilities()
        {
            return new Dictionary<string, FacilityInfo>(_availableFacilities);
        }
        
        /// <summary>
        /// Gets facilities filtered by availability
        /// </summary>
        public IEnumerable<FacilityInfo> GetAvailableFacilities(bool availableOnly = true)
        {
            return _availableFacilities.Values.Where(f => !availableOnly || f.IsAvailable);
        }
        
        /// <summary>
        /// Gets facility by ID
        /// </summary>
        public FacilityInfo GetFacility(string facilityId)
        {
            return _availableFacilities.GetValueOrDefault(facilityId);
        }
        
        /// <summary>
        /// Gets facility by display name
        /// </summary>
        public FacilityInfo GetFacilityByDisplayName(string displayName)
        {
            return _availableFacilities.Values.FirstOrDefault(f => f.DisplayName == displayName);
        }
        
        /// <summary>
        /// Gets all available schematics
        /// </summary>
        public Dictionary<string, SchematicInfo> GetAvailableSchematics()
        {
            return new Dictionary<string, SchematicInfo>(_availableSchematics);
        }
        
        /// <summary>
        /// Gets schematics filtered by availability
        /// </summary>
        public IEnumerable<SchematicInfo> GetAvailableSchematics(bool availableOnly = true)
        {
            return _availableSchematics.Values.Where(s => !availableOnly || s.IsAvailable);
        }
        
        /// <summary>
        /// Gets schematic by ID
        /// </summary>
        public SchematicInfo GetSchematic(string schematicId)
        {
            return _availableSchematics.GetValueOrDefault(schematicId);
        }
        
        /// <summary>
        /// Gets schematic by display name
        /// </summary>
        public SchematicInfo GetSchematicByDisplayName(string displayName)
        {
            return _availableSchematics.Values.FirstOrDefault(s => s.DisplayName == displayName);
        }
        
        /// <summary>
        /// Validates if facility is available for construction
        /// </summary>
        public bool IsFacilityAvailable(string facilityId)
        {
            var facility = GetFacility(facilityId);
            return facility != null && facility.IsAvailable;
        }
        
        /// <summary>
        /// Validates if schematic is available for loading
        /// </summary>
        public bool IsSchematicAvailable(string schematicId)
        {
            var schematic = GetSchematic(schematicId);
            return schematic != null && schematic.IsAvailable;
        }
        
        /// <summary>
        /// Gets facilities by category (cultivation, processing, administration, etc.)
        /// </summary>
        public IEnumerable<FacilityInfo> GetFacilitiesByCategory(string category, bool availableOnly = true)
        {
            return GetAvailableFacilities(availableOnly).Where(f => f.Category == category);
        }
        
        /// <summary>
        /// Gets facilities by cost range
        /// </summary>
        public IEnumerable<FacilityInfo> GetFacilitiesByCostRange(int minCost, int maxCost, bool availableOnly = true)
        {
            return GetAvailableFacilities(availableOnly).Where(f => f.Cost >= minCost && f.Cost <= maxCost);
        }
        
        /// <summary>
        /// Gets facilities by skill level requirement
        /// </summary>
        public IEnumerable<FacilityInfo> GetFacilitiesBySkillLevel(int maxSkillLevel, bool availableOnly = true)
        {
            return GetAvailableFacilities(availableOnly).Where(f => f.RequiredSkillLevel <= maxSkillLevel);
        }
    }
    
    /// <summary>
    /// Information about a facility type
    /// </summary>
    public class FacilityInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int Cost { get; set; }
        public int RequiredSkillLevel { get; set; }
        public bool IsAvailable { get; set; }
    }
    
    /// <summary>
    /// Information about a schematic
    /// </summary>
    public class SchematicInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int FacilityCount { get; set; }
        public int TotalCost { get; set; }
        public bool IsCustom { get; set; }
        public bool IsAvailable { get; set; }
    }
}