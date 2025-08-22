using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Generates context-sensitive menu items for genetics operations.
    /// Extracted from GeneticsContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class GeneticsMenuGenerator
    {
        private readonly GeneticsDataProvider _dataProvider;
        
        public GeneticsMenuGenerator(GeneticsDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }
        
        /// <summary>
        /// Gets menu items for breeding mode
        /// </summary>
        public List<string> GetBreedingModeItems(string selectedParentA, string selectedParentB)
        {
            var items = new List<string>();
            
            if (string.IsNullOrEmpty(selectedParentA))
            {
                items.Add("Select Parent A");
            }
            else if (string.IsNullOrEmpty(selectedParentB))
            {
                items.Add("Select Parent B");
            }
            else
            {
                items.Add("Execute Cross");
                items.Add("Preview Offspring");
                items.Add("Set Breeding Parameters");
            }
            
            items.Add("Cancel Breeding");
            return items;
        }
        
        /// <summary>
        /// Gets menu items when strains are selected
        /// </summary>
        public List<string> GetSelectedStrainItems(int selectedStrainCount)
        {
            var items = new List<string>
            {
                "Analyze Genetics",
                "View Strain Details",
                "Compare Strains"
            };
            
            if (selectedStrainCount == 1)
            {
                items.Add("Clone Strain");
                items.Add("Mutate Genes");
                items.Add("Save Strain");
            }
            else if (selectedStrainCount == 2)
            {
                items.Add("Start Breeding");
                items.Add("Compare Genetics");
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets default genetics menu items
        /// </summary>
        public List<string> GetDefaultGeneticsItems(List<string> breedingHistory)
        {
            var items = new List<string>();
            
            // Strain management
            items.Add("View Strain Library");
            items.Add("Create New Strain");
            items.Add("Import Strain");
            
            // Available strains (filtered by availability)
            var availableStrains = _dataProvider.GetAvailableStrains(true)
                .OrderBy(s => s.Type)
                .ThenBy(s => s.DisplayName);
            
            if (availableStrains.Any())
            {
                items.Add("─────────"); // Separator
                items.Add("Select Strain:");
                
                foreach (var strain in availableStrains)
                {
                    items.Add($"• {strain.DisplayName} ({strain.Type})");
                }
            }
            
            // Genetic analysis tools
            items.Add("─────────"); // Separator
            items.Add("Genetic Analysis Tools");
            items.Add("Trait Mapping");
            items.Add("Pedigree Chart");
            items.Add("Breeding Calculator");
            
            // Recent breeding history
            if (breedingHistory.Count > 0)
            {
                items.Add("─────────"); // Separator
                items.Add("Recent Breeding:");
                items.AddRange(breedingHistory.Take(3));
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets strain selection items from data provider
        /// </summary>
        public List<string> GetStrainSelectionItems()
        {
            var items = new List<string>();
            var availableStrains = _dataProvider.GetAvailableStrains(true)
                .OrderBy(s => s.Type)
                .ThenBy(s => s.DisplayName);
            
            foreach (var strain in availableStrains)
            {
                items.Add($"• {strain.DisplayName} ({strain.Type})");
            }
            
            return items;
        }
        
        /// <summary>
        /// Gets analysis tool items
        /// </summary>
        public List<string> GetAnalysisToolItems()
        {
            return new List<string>
            {
                "Genetic Analysis Tools",
                "Trait Mapping", 
                "Pedigree Chart",
                "Breeding Calculator"
            };
        }
        
        /// <summary>
        /// Gets strain management items
        /// </summary>
        public List<string> GetStrainManagementItems()
        {
            return new List<string>
            {
                "View Strain Library",
                "Create New Strain",
                "Import Strain"
            };
        }
    }
}