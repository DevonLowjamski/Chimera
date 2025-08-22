using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles schematic display, item creation, and visual representation in the library.
    /// Creates grid items, list items, and manages the visual display of schematic data.
    /// </summary>
    public class SchematicLibraryDisplayController : MonoBehaviour
    {
        [Header("Display Configuration")]
        [SerializeField] private int _gridColumnsLarge = 4;
        [SerializeField] private int _gridColumnsCompact = 6;
        [SerializeField] private bool _showUnlockStates = true;
        [SerializeField] private bool _showCostInformation = true;
        
        // Dependencies
        private SchematicUnlockManager _unlockManager;
        
        // UI References
        private VisualElement _libraryGrid;
        private VisualElement _libraryList;
        private VisualElement _detailsPanel;
        private VisualElement _emptyState;
        
        // Display state
        private LibraryViewMode _currentViewMode = LibraryViewMode.Grid;
        private SchematicSO _selectedSchematic;
        
        // Events
        public System.Action<SchematicSO> OnSchematicSelected;
        public System.Action<SchematicSO> OnSchematicDoubleClicked;
        public System.Action<SchematicSO> OnSchematicRightClicked;
        
        // Properties
        public LibraryViewMode CurrentViewMode => _currentViewMode;
        public SchematicSO SelectedSchematic => _selectedSchematic;
        
        /// <summary>
        /// Initialize display controller with UI references
        /// </summary>
        public void Initialize(VisualElement libraryGrid, VisualElement libraryList, VisualElement detailsPanel, VisualElement emptyState, SchematicUnlockManager unlockManager = null)
        {
            _libraryGrid = libraryGrid;
            _libraryList = libraryList;
            _detailsPanel = detailsPanel;
            _emptyState = emptyState;
            _unlockManager = unlockManager;
        }
        
        #region Display Management
        
        /// <summary>
        /// Update displayed schematics
        /// </summary>
        public void UpdateDisplay(List<SchematicSO> schematics, LibraryViewMode viewMode)
        {
            _currentViewMode = viewMode;
            
            if (schematics == null || schematics.Count == 0)
            {
                ShowEmptyState();
                return;
            }
            
            HideEmptyState();
            
            if (viewMode == LibraryViewMode.Grid)
            {
                UpdateGridDisplay(schematics);
            }
            else
            {
                UpdateListDisplay(schematics);
            }
        }
        
        /// <summary>
        /// Update grid view display
        /// </summary>
        private void UpdateGridDisplay(List<SchematicSO> schematics)
        {
            if (_libraryGrid == null) return;
            
            _libraryGrid.Clear();
            _libraryGrid.style.display = DisplayStyle.Flex;
            
            if (_libraryList != null)
                _libraryList.style.display = DisplayStyle.None;
            
            foreach (var schematic in schematics)
            {
                var gridItem = CreateGridItem(schematic);
                _libraryGrid.Add(gridItem);
            }
        }
        
        /// <summary>
        /// Update list view display
        /// </summary>
        private void UpdateListDisplay(List<SchematicSO> schematics)
        {
            if (_libraryList == null) return;
            
            _libraryList.Clear();
            _libraryList.style.display = DisplayStyle.Flex;
            
            if (_libraryGrid != null)
                _libraryGrid.style.display = DisplayStyle.None;
            
            foreach (var schematic in schematics)
            {
                var listItem = CreateListItem(schematic);
                _libraryList.Add(listItem);
            }
        }
        
        /// <summary>
        /// Show empty state
        /// </summary>
        private void ShowEmptyState()
        {
            if (_emptyState != null)
                _emptyState.style.display = DisplayStyle.Flex;
            
            if (_libraryGrid != null)
                _libraryGrid.style.display = DisplayStyle.None;
            
            if (_libraryList != null)
                _libraryList.style.display = DisplayStyle.None;
        }
        
        /// <summary>
        /// Hide empty state
        /// </summary>
        private void HideEmptyState()
        {
            if (_emptyState != null)
                _emptyState.style.display = DisplayStyle.None;
        }
        
        #endregion
        
        #region Grid Item Creation
        
        /// <summary>
        /// Create grid item for schematic
        /// </summary>
        private VisualElement CreateGridItem(SchematicSO schematic)
        {
            var gridItem = new VisualElement();
            gridItem.AddToClassList("grid-item");
            
            // Get unlock status
            var unlockData = GetSchematicUnlockData(schematic);
            
            // Apply unlock state styling
            ApplyUnlockStyling(gridItem, unlockData);
            
            // Create preview
            var preview = CreatePreviewElement(schematic, unlockData);
            gridItem.Add(preview);
            
            // Create name label
            var name = new Label(schematic.SchematicName);
            name.AddToClassList("grid-item-name");
            gridItem.Add(name);
            
            // Create info section
            var info = CreateInfoSection(schematic);
            gridItem.Add(info);
            
            // Create cost information
            if (_showCostInformation)
            {
                var costInfo = CreateCostInfo(schematic, unlockData);
                gridItem.Add(costInfo);
            }
            
            // Add unlock info if needed
            if (_showUnlockStates && !unlockData.IsUnlocked && schematic.RequiresUnlock)
            {
                var unlockInfo = CreateUnlockInfo(schematic, unlockData);
                gridItem.Add(unlockInfo);
            }
            
            // Setup interaction
            SetupItemInteraction(gridItem, schematic);
            
            return gridItem;
        }
        
        /// <summary>
        /// Create preview element
        /// </summary>
        private VisualElement CreatePreviewElement(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var preview = new VisualElement();
            preview.AddToClassList("grid-item-preview");
            
            // Set preview image
            if (schematic.PreviewIcon != null)
            {
                preview.style.backgroundImage = new StyleBackground(schematic.PreviewIcon);
            }
            else
            {
                // Add placeholder icon based on category
                var placeholderIcon = new Label(GetCategoryIcon(schematic.Category));
                placeholderIcon.AddToClassList("placeholder-icon");
                preview.Add(placeholderIcon);
            }
            
            // Add lock overlay if needed
            if (!unlockData.IsUnlocked)
            {
                var lockOverlay = CreateLockOverlay();
                preview.Add(lockOverlay);
            }
            
            return preview;
        }
        
        /// <summary>
        /// Create lock overlay
        /// </summary>
        private VisualElement CreateLockOverlay()
        {
            var lockOverlay = new VisualElement();
            lockOverlay.AddToClassList("lock-overlay");
            
            var lockIcon = new Label("🔒");
            lockIcon.AddToClassList("lock-icon");
            lockOverlay.Add(lockIcon);
            
            return lockOverlay;
        }
        
        /// <summary>
        /// Create info section for grid item
        /// </summary>
        private VisualElement CreateInfoSection(SchematicSO schematic)
        {
            var info = new VisualElement();
            info.AddToClassList("grid-item-info");
            
            var complexity = new Label(schematic.Complexity.ToString());
            complexity.AddToClassList("complexity-badge");
            complexity.AddToClassList($"complexity-{schematic.Complexity.ToString().ToLower()}");
            
            var itemCount = new Label($"{schematic.ItemCount} items");
            itemCount.AddToClassList("item-count");
            
            info.Add(complexity);
            info.Add(itemCount);
            
            return info;
        }
        
        /// <summary>
        /// Create cost information element
        /// </summary>
        private VisualElement CreateCostInfo(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var costInfo = new VisualElement();
            costInfo.AddToClassList("grid-item-cost");
            
            if (unlockData.IsUnlocked)
            {
                // Show material cost
                var paymentSystem = FindObjectOfType<ProjectChimera.Systems.Economy.MaterialCostPaymentSystem>();
                if (paymentSystem != null)
                {
                    var paymentData = paymentSystem.GetPaymentDisplayData(schematic);
                    var costLabel = new Label($"💰 {paymentData.FormattedCost}");
                    costLabel.AddToClassList("cost-label");
                    
                    if (!paymentData.CanAfford)
                    {
                        costLabel.AddToClassList("unaffordable");
                    }
                    
                    costInfo.Add(costLabel);
                }
            }
            else
            {
                // Show unlock cost
                if (schematic.RequiresUnlock)
                {
                    var unlockCost = new Label($"🎯 {schematic.SkillPointCost} SP");
                    unlockCost.AddToClassList("unlock-cost");
                    costInfo.Add(unlockCost);
                }
            }
            
            return costInfo;
        }
        
        /// <summary>
        /// Create unlock information element
        /// </summary>
        private VisualElement CreateUnlockInfo(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var unlockInfo = new Label();
            unlockInfo.AddToClassList("unlock-info");
            
            if (unlockData.CanUnlock)
            {
                unlockInfo.text = $"🎯 {schematic.SkillPointCost} SP";
                unlockInfo.AddToClassList("unlockable");
            }
            else
            {
                unlockInfo.text = unlockData.UnlockHint;
                unlockInfo.AddToClassList("locked");
            }
            
            return unlockInfo;
        }
        
        #endregion
        
        #region List Item Creation
        
        /// <summary>
        /// Create list item for schematic
        /// </summary>
        private VisualElement CreateListItem(SchematicSO schematic)
        {
            var listItem = new VisualElement();
            listItem.AddToClassList("list-item");
            listItem.style.flexDirection = FlexDirection.Row;
            
            // Get unlock status
            var unlockData = GetSchematicUnlockData(schematic);
            
            // Apply unlock state styling
            ApplyUnlockStyling(listItem, unlockData);
            
            // Create preview (smaller for list view)
            var preview = CreateListPreview(schematic, unlockData);
            listItem.Add(preview);
            
            // Create content section
            var content = CreateListContent(schematic, unlockData);
            listItem.Add(content);
            
            // Create actions section
            var actions = CreateListActions(schematic, unlockData);
            listItem.Add(actions);
            
            // Setup interaction
            SetupItemInteraction(listItem, schematic);
            
            return listItem;
        }
        
        /// <summary>
        /// Create list item preview
        /// </summary>
        private VisualElement CreateListPreview(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var preview = new VisualElement();
            preview.AddToClassList("list-item-preview");
            
            if (schematic.PreviewIcon != null)
            {
                preview.style.backgroundImage = new StyleBackground(schematic.PreviewIcon);
            }
            else
            {
                var placeholderIcon = new Label(GetCategoryIcon(schematic.Category));
                placeholderIcon.AddToClassList("placeholder-icon");
                preview.Add(placeholderIcon);
            }
            
            // Add lock overlay if needed
            if (!unlockData.IsUnlocked)
            {
                var lockOverlay = CreateLockOverlay();
                preview.Add(lockOverlay);
            }
            
            return preview;
        }
        
        /// <summary>
        /// Create list item content
        /// </summary>
        private VisualElement CreateListContent(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var content = new VisualElement();
            content.AddToClassList("list-item-content");
            content.style.flexGrow = 1;
            
            // Name
            var name = new Label(schematic.SchematicName);
            name.AddToClassList("list-item-name");
            content.Add(name);
            
            // Description
            var description = new Label(schematic.Description);
            description.AddToClassList("list-item-description");
            content.Add(description);
            
            // Metadata row
            var metadata = CreateListMetadata(schematic);
            content.Add(metadata);
            
            // Cost information
            if (_showCostInformation)
            {
                var costMetadata = CreateListCostMetadata(schematic, unlockData);
                content.Add(costMetadata);
            }
            
            return content;
        }
        
        /// <summary>
        /// Create list item metadata
        /// </summary>
        private VisualElement CreateListMetadata(SchematicSO schematic)
        {
            var metadata = new VisualElement();
            metadata.AddToClassList("list-item-metadata");
            metadata.style.flexDirection = FlexDirection.Row;
            
            var itemCount = new Label($"{schematic.ItemCount} items");
            itemCount.AddToClassList("metadata-item");
            
            var complexity = new Label(schematic.Complexity.ToString());
            complexity.AddToClassList("metadata-item");
            complexity.AddToClassList($"complexity-{schematic.Complexity.ToString().ToLower()}");
            
            var date = new Label(schematic.CreationDate.ToString("MMM dd, yyyy"));
            date.AddToClassList("metadata-item");
            
            metadata.Add(itemCount);
            metadata.Add(new Label(" • "));
            metadata.Add(complexity);
            metadata.Add(new Label(" • "));
            metadata.Add(date);
            
            return metadata;
        }
        
        /// <summary>
        /// Create list item cost metadata
        /// </summary>
        private VisualElement CreateListCostMetadata(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var costMetadata = new Label();
            costMetadata.AddToClassList("list-item-cost");
            
            if (unlockData.IsUnlocked)
            {
                var paymentSystem = FindObjectOfType<ProjectChimera.Systems.Economy.MaterialCostPaymentSystem>();
                if (paymentSystem != null)
                {
                    var paymentData = paymentSystem.GetPaymentDisplayData(schematic);
                    costMetadata.text = $"💰 Material Cost: {paymentData.FormattedCost}";
                    
                    if (!paymentData.CanAfford)
                    {
                        costMetadata.AddToClassList("unaffordable");
                        costMetadata.text += " ⚠️ Cannot Afford";
                    }
                }
            }
            else if (schematic.RequiresUnlock)
            {
                costMetadata.text = unlockData.CanUnlock 
                    ? $"🎯 Unlock Cost: {schematic.SkillPointCost} SP" 
                    : $"🔒 {unlockData.UnlockHint}";
                    
                costMetadata.AddToClassList(unlockData.CanUnlock ? "unlockable" : "locked");
            }
            
            return costMetadata;
        }
        
        /// <summary>
        /// Create list item actions
        /// </summary>
        private VisualElement CreateListActions(SchematicSO schematic, SchematicUnlockDisplayData unlockData)
        {
            var actions = new VisualElement();
            actions.AddToClassList("list-item-actions");
            
            if (unlockData.IsUnlocked)
            {
                var useButton = new Button(() => OnSchematicDoubleClicked?.Invoke(schematic));
                useButton.text = "Use";
                useButton.AddToClassList("use-button");
                actions.Add(useButton);
            }
            else if (unlockData.CanUnlock)
            {
                var unlockButton = new Button(() => TryUnlockSchematic(schematic));
                unlockButton.text = "Unlock";
                unlockButton.AddToClassList("unlock-button");
                actions.Add(unlockButton);
            }
            
            return actions;
        }
        
        #endregion
        
        #region Interaction Handling
        
        /// <summary>
        /// Setup item interaction events
        /// </summary>
        private void SetupItemInteraction(VisualElement item, SchematicSO schematic)
        {
            item.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 1)
                {
                    SelectSchematic(schematic);
                }
                else if (evt.clickCount == 2)
                {
                    OnSchematicDoubleClicked?.Invoke(schematic);
                }
            });
            
            item.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 1) // Right click
                {
                    OnSchematicRightClicked?.Invoke(schematic);
                }
            });
        }
        
        /// <summary>
        /// Select schematic and update visual state
        /// </summary>
        private void SelectSchematic(SchematicSO schematic)
        {
            if (_selectedSchematic != schematic)
            {
                // Remove previous selection styling
                ClearSelectionStyling();
                
                _selectedSchematic = schematic;
                
                // Apply selection styling
                ApplySelectionStyling(schematic);
                
                OnSchematicSelected?.Invoke(schematic);
            }
        }
        
        /// <summary>
        /// Clear selection styling from all items
        /// </summary>
        private void ClearSelectionStyling()
        {
            if (_libraryGrid != null)
            {
                ClearSelectionFromContainer(_libraryGrid);
            }
            
            if (_libraryList != null)
            {
                ClearSelectionFromContainer(_libraryList);
            }
        }
        
        /// <summary>
        /// Clear selection styling from container
        /// </summary>
        private void ClearSelectionFromContainer(VisualElement container)
        {
            foreach (var item in container.Children())
            {
                item.RemoveFromClassList("selected");
            }
        }
        
        /// <summary>
        /// Apply selection styling to schematic item
        /// </summary>
        private void ApplySelectionStyling(SchematicSO schematic)
        {
            var container = _currentViewMode == LibraryViewMode.Grid ? _libraryGrid : _libraryList;
            if (container == null) return;
            
            // Find and highlight the selected item
            // This is a simplified approach - in a full implementation, you'd maintain item references
            var items = container.Children().ToList();
            if (items.Count > 0)
            {
                // For now, just highlight the first item as a placeholder
                // In a real implementation, you'd track which item corresponds to which schematic
                foreach (var item in items)
                {
                    // This would need proper item-to-schematic mapping
                    item.AddToClassList("selected");
                    break; // Remove this break when proper mapping is implemented
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get schematic unlock data
        /// </summary>
        private SchematicUnlockDisplayData GetSchematicUnlockData(SchematicSO schematic)
        {
            if (_unlockManager == null)
            {
                // Fallback when unlock manager not available
                return new SchematicUnlockDisplayData
                {
                    Schematic = schematic,
                    IsUnlocked = true,
                    CanUnlock = true,
                    UnlockHint = "",
                    ProgressPercentage = 1f
                };
            }
            
            return _unlockManager.GetSchematicDisplayData(schematic);
        }
        
        /// <summary>
        /// Apply unlock state styling to item
        /// </summary>
        private void ApplyUnlockStyling(VisualElement item, SchematicUnlockDisplayData unlockData)
        {
            if (!unlockData.IsUnlocked)
            {
                item.AddToClassList("locked-item");
                if (!unlockData.CanUnlock)
                {
                    item.AddToClassList("progression-locked");
                }
            }
        }
        
        /// <summary>
        /// Get category icon for placeholder
        /// </summary>
        private string GetCategoryIcon(ConstructionCategory category)
        {
            return category switch
            {
                ConstructionCategory.Structure => "🏗️",
                ConstructionCategory.Equipment => "⚙️",
                ConstructionCategory.Decoration => "🎨",
                ConstructionCategory.Utility => "🔧",
                _ => "📦"
            };
        }
        
        /// <summary>
        /// Try to unlock schematic
        /// </summary>
        private void TryUnlockSchematic(SchematicSO schematic)
        {
            if (_unlockManager == null)
            {
                Debug.LogWarning("Unlock manager not available");
                return;
            }
            
            bool success = _unlockManager.UnlockSchematic(schematic);
            
            if (success)
            {
                // The display will be refreshed by the parent panel
                Debug.Log($"Successfully unlocked: {schematic.SchematicName}");
            }
            else
            {
                Debug.LogWarning($"Failed to unlock: {schematic.SchematicName}");
            }
        }
        
        #endregion
    }
}