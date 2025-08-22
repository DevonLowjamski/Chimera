using UnityEngine;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles genetics-specific menu actions and operations.
    /// Extracted from GeneticsContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class GeneticsMenuActionHandler
    {
        // Events
        public event Action<string, string> OnGeneticsActionTriggered;
        
        public GeneticsMenuActionHandler()
        {
        }
        
        /// <summary>
        /// Handles a menu item click by ID
        /// </summary>
        public void HandleItemClick(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            
            // Route item clicks to appropriate actions based on item ID
            switch (itemId.ToLower())
            {
                case "show-parent-selector":
                    ShowParentSelector("");
                    break;
                case "preview-breeding":
                    PreviewBreedingResults();
                    break;
                case "show-breeding-params":
                    ShowBreedingParameters();
                    break;
                case "open-strain-library":
                    OpenStrainLibrary();
                    break;
                case "create-strain":
                    CreateNewStrain();
                    break;
                default:
                    OnGeneticsActionTriggered?.Invoke("item_clicked", itemId);
                    Debug.Log($"[GeneticsMenuActionHandler] Handled item click: {itemId}");
                    break;
            }
        }
        
        // Parent selector operations
        public bool ShowParentSelector(string parent) 
        { 
            OnGeneticsActionTriggered?.Invoke("show-parent-selector", parent); 
            Debug.Log($"[GeneticsMenuActionHandler] Showing parent selector: {parent}");
            return true; 
        }
        
        // Breeding operations
        public bool PreviewBreedingResults() 
        { 
            OnGeneticsActionTriggered?.Invoke("preview-offspring", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Previewing breeding results");
            return true; 
        }
        
        public bool ShowBreedingParameters() 
        { 
            OnGeneticsActionTriggered?.Invoke("show-breeding-params", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Showing breeding parameters");
            return true; 
        }
        
        // Strain library operations
        public bool OpenStrainLibrary() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-strain-library", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Opening strain library");
            return true; 
        }
        
        public bool CreateNewStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("create-new-strain", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Creating new strain");
            return true; 
        }
        
        public bool ImportStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("import-strain", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Importing strain");
            return true; 
        }
        
        // Analysis operations
        public bool AnalyzeSelectedStrains() 
        { 
            OnGeneticsActionTriggered?.Invoke("analyze-genetics", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Analyzing selected strains");
            return true; 
        }
        
        public bool ViewStrainDetails() 
        { 
            OnGeneticsActionTriggered?.Invoke("view-strain-details", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Viewing strain details");
            return true; 
        }
        
        public bool CompareSelectedStrains() 
        { 
            OnGeneticsActionTriggered?.Invoke("compare-strains", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Comparing selected strains");
            return true; 
        }
        
        // Strain manipulation operations
        public bool CloneSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("clone-strain", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Cloning selected strain");
            return true; 
        }
        
        public bool MutateSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("mutate-genes", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Mutating selected strain");
            return true; 
        }
        
        public bool SaveSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("save-strain", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Saving selected strain");
            return true; 
        }
        
        public bool CompareGeneticsDetails() 
        { 
            OnGeneticsActionTriggered?.Invoke("compare-genetics", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Comparing genetics details");
            return true; 
        }
        
        // Analysis tools
        public bool OpenAnalysisTools() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-analysis-tools", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Opening analysis tools");
            return true; 
        }
        
        public bool OpenTraitMapping() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-trait-mapping", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Opening trait mapping");
            return true; 
        }
        
        public bool OpenPedigreeChart() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-pedigree", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Opening pedigree chart");
            return true; 
        }
        
        public bool OpenBreedingCalculator() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-calculator", ""); 
            Debug.Log("[GeneticsMenuActionHandler] Opening breeding calculator");
            return true; 
        }
        
        /// <summary>
        /// Connects this handler's events to the main genetics menu events
        /// </summary>
        public void ConnectToMainHandler(Action<string, string> mainActionHandler)
        {
            OnGeneticsActionTriggered = mainActionHandler;
        }
        
        /// <summary>
        /// Validates if an action can be performed
        /// </summary>
        public bool CanPerformAction(string actionId)
        {
            // Add validation logic based on current state
            switch (actionId)
            {
                case "show-parent-selector":
                case "preview-offspring":
                case "show-breeding-params":
                case "open-strain-library":
                case "create-new-strain":
                case "import-strain":
                case "analyze-genetics":
                case "view-strain-details":
                case "compare-strains":
                case "clone-strain":
                case "mutate-genes":
                case "save-strain":
                case "compare-genetics":
                case "open-analysis-tools":
                case "open-trait-mapping":
                case "open-pedigree":
                case "open-calculator":
                    return true;
                default:
                    Debug.LogWarning($"[GeneticsMenuActionHandler] Unknown action: {actionId}");
                    return false;
            }
        }
    }
}