using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.UI.Components;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Handles genetics-specific contextual menu logic including strain management,
    /// breeding operations, and genetic analysis.
    /// Extracted from ContextualMenuController.cs for better maintainability.
    /// </summary>
    public class GeneticsContextMenu : IModeContextualMenuProvider
    {
        // Genetics State
        private readonly HashSet<string> _selectedStrains = new HashSet<string>();
        private string _currentAnalysisTarget = string.Empty;
        
        // Data Provider and Components
        private readonly GeneticsDataProvider _dataProvider = new GeneticsDataProvider();
        private readonly GeneticsMenuActionHandler _actionHandler = new GeneticsMenuActionHandler();
        private readonly GeneticsMenuGenerator _menuGenerator;
        private readonly GeneticsBreedingManager _breedingManager = new GeneticsBreedingManager();
        
        // Events
        public event System.Action<string> OnStrainSelected;
        public event System.Action<string, string> OnBreedingStarted;
        public event System.Action<string> OnAnalysisStarted;
        public event System.Action<string, string> OnGeneticsActionTriggered;
        public event System.Action<List<string>> OnMenuItemsChanged;
        
        public string ProviderMode => "genetics";
        public bool IsActive { get; private set; } = false;
        
        public GeneticsContextMenu()
        {
            // Initialize components
            _menuGenerator = new GeneticsMenuGenerator(_dataProvider);
            _actionHandler.ConnectToMainHandler((action, data) => OnGeneticsActionTriggered?.Invoke(action, data));
            _breedingManager.ConnectToMainHandlers(
                (parentA, parentB) => OnBreedingStarted?.Invoke(parentA, parentB),
                (action, data) => OnGeneticsActionTriggered?.Invoke(action, data),
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
            OnGeneticsActionTriggered?.Invoke("item_clicked", itemId);
        }
        
        /// <summary>
        /// Gets menu items for the current genetics context
        /// </summary>
        public List<string> GetMenuItems()
        {
            var menuItems = new List<string>();
            
            // Context-sensitive menu items based on current state
            if (_breedingManager.IsInBreedingMode)
            {
                menuItems.AddRange(_menuGenerator.GetBreedingModeItems(_breedingManager.SelectedParentA, _breedingManager.SelectedParentB));
            }
            else if (_selectedStrains.Count > 0)
            {
                menuItems.AddRange(_menuGenerator.GetSelectedStrainItems(_selectedStrains.Count));
            }
            else
            {
                menuItems.AddRange(_menuGenerator.GetDefaultGeneticsItems(_breedingManager.GetBreedingHistory()));
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
                // Handle breeding mode actions
                if (_breedingManager.IsInBreedingMode)
                {
                    return HandleBreedingModeSelection(menuItem);
                }
                
                // Handle strain selection
                if (menuItem.StartsWith("• "))
                {
                    return HandleStrainSelection(menuItem);
                }
                
                // Handle general genetics actions
                return HandleGeneralGeneticsAction(menuItem);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GeneticsContextMenu] Error handling menu selection: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Handles breeding mode specific selections
        /// </summary>
        private bool HandleBreedingModeSelection(string menuItem)
        {
            switch (menuItem)
            {
                case "Select Parent A":
                    return _actionHandler.ShowParentSelector("A");
                case "Select Parent B":
                    return _actionHandler.ShowParentSelector("B");
                case "Execute Cross":
                    return _breedingManager.ExecuteBreeding();
                case "Preview Offspring":
                    return _actionHandler.PreviewBreedingResults();
                case "Set Breeding Parameters":
                    return _actionHandler.ShowBreedingParameters();
                case "Cancel Breeding":
                    return _breedingManager.CancelBreeding();
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Handles strain selection from menu
        /// </summary>
        private bool HandleStrainSelection(string menuItem)
        {
            var strainName = menuItem.Substring(2); // Remove "• " prefix
            var strainInfo = _dataProvider.GetAvailableStrains(true)
                .FirstOrDefault(s => strainName.StartsWith(s.DisplayName));
            
            if (strainInfo != null)
            {
                SelectStrain(strainInfo.Id);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles general genetics actions
        /// </summary>
        private bool HandleGeneralGeneticsAction(string menuItem)
        {
            switch (menuItem)
            {
                case "View Strain Library":
                    return _actionHandler.OpenStrainLibrary();
                case "Create New Strain":
                    return _actionHandler.CreateNewStrain();
                case "Import Strain":
                    return _actionHandler.ImportStrain();
                case "Analyze Genetics":
                    return _actionHandler.AnalyzeSelectedStrains();
                case "View Strain Details":
                    return _actionHandler.ViewStrainDetails();
                case "Compare Strains":
                    return _actionHandler.CompareSelectedStrains();
                case "Clone Strain":
                    return _actionHandler.CloneSelectedStrain();
                case "Mutate Genes":
                    return _actionHandler.MutateSelectedStrain();
                case "Save Strain":
                    return _actionHandler.SaveSelectedStrain();
                case "Start Breeding":
                    return _breedingManager.StartBreedingMode(_selectedStrains);
                case "Compare Genetics":
                    return _actionHandler.CompareGeneticsDetails();
                case "Genetic Analysis Tools":
                    return _actionHandler.OpenAnalysisTools();
                case "Trait Mapping":
                    return _actionHandler.OpenTraitMapping();
                case "Pedigree Chart":
                    return _actionHandler.OpenPedigreeChart();
                case "Breeding Calculator":
                    return _actionHandler.OpenBreedingCalculator();
                default:
                    Debug.LogWarning($"[GeneticsContextMenu] Unhandled action: {menuItem}");
                    return false;
            }
        }
        
        /// <summary>
        /// Selects a strain for genetics operations
        /// </summary>
        private void SelectStrain(string strainId)
        {
            _selectedStrains.Clear();
            _selectedStrains.Add(strainId);
            
            OnStrainSelected?.Invoke(strainId);
            OnGeneticsActionTriggered?.Invoke("strain-selected", strainId);
            
            // Refresh menu items
            OnMenuItemsChanged?.Invoke(GetMenuItems());
            
            Debug.Log($"[GeneticsContextMenu] Selected strain: {strainId}");
        }
        
        
        
        /// <summary>
        /// Sets strain selection state
        /// </summary>
        public void SetSelectedStrains(List<string> strainIds)
        {
            _selectedStrains.Clear();
            _selectedStrains.UnionWith(strainIds);
            
            // Refresh menu items based on selection
            OnMenuItemsChanged?.Invoke(GetMenuItems());
        }
        
        /// <summary>
        /// Activates the genetics context menu
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            Debug.Log("[GeneticsContextMenu] Activated");
        }
        
        /// <summary>
        /// Deactivates the genetics context menu
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            _breedingManager.Reset();
            _selectedStrains.Clear();
            _currentAnalysisTarget = string.Empty;
            
            Debug.Log("[GeneticsContextMenu] Deactivated");
        }
        
        /// <summary>
        /// Gets breeding history
        /// </summary>
        public List<string> GetBreedingHistory()
        {
            return _breedingManager.GetBreedingHistory();
        }
        
        /// <summary>
        /// Gets available strains info
        /// </summary>
        public Dictionary<string, StrainInfo> GetAvailableStrains()
        {
            return _dataProvider.GetAvailableStrains();
        }
        
        /// <summary>
        /// Gets available genetic traits info
        /// </summary>
        public Dictionary<string, GeneticTrait> GetAvailableTraits()
        {
            return _dataProvider.GetAvailableTraits();
        }
    }
    
}