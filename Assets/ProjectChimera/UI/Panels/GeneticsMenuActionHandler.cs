using UnityEngine;
using System;
using ProjectChimera.Core.Logging;

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
                    ChimeraLogger.Log($"[GeneticsMenuActionHandler] Handled item click: {itemId}");
                    break;
            }
        }
        
        // Parent selector operations
        public bool ShowParentSelector(string parent) 
        { 
            OnGeneticsActionTriggered?.Invoke("show-parent-selector", parent); 
            ChimeraLogger.Log($"[GeneticsMenuActionHandler] Showing parent selector: {parent}");
            return true; 
        }
        
        // Breeding operations
        public bool PreviewBreedingResults() 
        { 
            OnGeneticsActionTriggered?.Invoke("preview-offspring", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Previewing breeding results");
            return true; 
        }
        
        public bool ShowBreedingParameters() 
        { 
            OnGeneticsActionTriggered?.Invoke("show-breeding-params", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Showing breeding parameters");
            return true; 
        }
        
        // Strain library operations
        public bool OpenStrainLibrary() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-strain-library", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Opening strain library");
            return true; 
        }
        
        public bool CreateNewStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("create-new-strain", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Creating new strain");
            return true; 
        }
        
        public bool ImportStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("import-strain", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Importing strain");
            return true; 
        }
        
        // Analysis operations
        public bool AnalyzeSelectedStrains() 
        { 
            OnGeneticsActionTriggered?.Invoke("analyze-genetics", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Analyzing selected strains");
            return true; 
        }
        
        public bool ViewStrainDetails() 
        { 
            OnGeneticsActionTriggered?.Invoke("view-strain-details", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Viewing strain details");
            return true; 
        }
        
        public bool CompareSelectedStrains() 
        { 
            OnGeneticsActionTriggered?.Invoke("compare-strains", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Comparing selected strains");
            return true; 
        }
        
        // Strain manipulation operations
        public bool CloneSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("clone-strain", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Cloning selected strain");
            return true; 
        }
        
        public bool MutateSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("mutate-genes", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Mutating selected strain");
            return true; 
        }
        
        public bool SaveSelectedStrain() 
        { 
            OnGeneticsActionTriggered?.Invoke("save-strain", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Saving selected strain");
            return true; 
        }
        
        public bool CompareGeneticsDetails() 
        { 
            OnGeneticsActionTriggered?.Invoke("compare-genetics", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Comparing genetics details");
            return true; 
        }
        
        // Analysis tools
        public bool OpenAnalysisTools() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-analysis-tools", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Opening analysis tools");
            return true; 
        }
        
        public bool OpenTraitMapping() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-trait-mapping", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Opening trait mapping");
            return true; 
        }
        
        public bool OpenPedigreeChart() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-pedigree", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Opening pedigree chart");
            return true; 
        }
        
        public bool OpenBreedingCalculator() 
        { 
            OnGeneticsActionTriggered?.Invoke("open-calculator", ""); 
            ChimeraLogger.Log("[GeneticsMenuActionHandler] Opening breeding calculator");
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
                    ChimeraLogger.LogWarning($"[GeneticsMenuActionHandler] Unknown action: {actionId}");
                    return false;
            }
        }
    }
}