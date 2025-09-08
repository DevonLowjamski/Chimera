using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;
using ProjectChimera.UI.Panels.Components;
using ProjectChimera.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Lightweight orchestrator for schematic library functionality.
    /// Coordinates specialized components for search, UI building, data management, and display.
    /// </summary>
    public class SchematicLibraryPanel : UIPanel
    {
        [Header("Library Configuration")]
        [SerializeField] private GridPlacementController _placementController;
        [SerializeField] private SchematicUnlockManager _unlockManager;
        
        [Header("Component Configuration")]
        [SerializeField] private int _itemsPerPage = 20;
        [SerializeField] private LibraryViewMode _defaultViewMode = LibraryViewMode.Grid;
        [SerializeField] private LibrarySortMode _defaultSortMode = LibrarySortMode.Name;
        [SerializeField] private bool _defaultSortAscending = true;
        
        // Specialized components
        private SchematicLibrarySearchController _searchController;
        private SchematicLibraryUIBuilder _uiBuilder;
        private SchematicLibraryDataManager _dataManager;
        private SchematicLibraryDisplayController _displayController;
        
        // Current state
        private LibraryViewMode _currentViewMode;
        private SchematicSO _selectedSchematic;
        
        // Events for external subscribers
        public System.Action<SchematicSO> SchematicSelected;
        public System.Action<SchematicSO> SchematicApplied;
        public System.Action<SchematicSO> SchematicDeleted;
        public System.Action<string> SearchQueryChanged;
        public System.Action<LibraryViewMode> ViewModeChanged;
        
        // Properties
        public SchematicSO SelectedSchematic => _selectedSchematic;
        public LibraryViewMode CurrentViewMode => _currentViewMode;
        
        #region UIPanel Implementation
        
        protected override void OnPanelInitialized()
        {
            InitializeComponents();
            SetupEventHandlers();
            InitializeDefaultState();
            LoadInitialData();
        }
        
        protected virtual void OnPanelDestroy()
        {
            CleanupEventHandlers();
        }
        
        #endregion
        
        #region Component Initialization
        
        /// <summary>
        /// Initialize all specialized components
        /// </summary>
        private void InitializeComponents()
        {
            // Create or get components
            _searchController = GetOrAddComponent<SchematicLibrarySearchController>();
            _uiBuilder = GetOrAddComponent<SchematicLibraryUIBuilder>();
            _dataManager = GetOrAddComponent<SchematicLibraryDataManager>();
            _displayController = GetOrAddComponent<SchematicLibraryDisplayController>();
            
            // Initialize UI builder first
            _uiBuilder.Initialize(_rootElement);
            
            // Initialize other components with UI references
            _displayController.Initialize(
                _uiBuilder.LibraryGrid,
                _uiBuilder.LibraryList,
                _uiBuilder.DetailsPanel,
                FindEmptyStateElement(),
                _unlockManager
            );
            
            // Initialize data manager
            _dataManager.Initialize();
        }
        
        /// <summary>
        /// Get or add component to this GameObject
        /// </summary>
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        /// <summary>
        /// Find empty state element from UI builder
        /// </summary>
        private VisualElement FindEmptyStateElement()
        {
            return _rootElement?.Q("empty-state");
        }
        
        #endregion
        
        #region Event Handling Setup
        
        /// <summary>
        /// Setup all event handlers between components
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_searchController != null)
            {
                _searchController.OnFiltersChanged += OnSearchFiltersChanged;
            }
            
            if (_uiBuilder != null)
            {
                _uiBuilder.OnSearchQueryChanged += OnSearchQueryChanged;
                _uiBuilder.OnCategoryFilterChanged += OnCategoryFilterChanged;
                _uiBuilder.OnComplexityFilterChanged += OnComplexityFilterChanged;
                _uiBuilder.OnTagFilterChanged += OnTagFilterChanged;
                _uiBuilder.OnSortModeChanged += OnSortModeChanged;
                _uiBuilder.OnSortOrderChanged += OnSortOrderChanged;
                _uiBuilder.OnViewModeChanged += OnViewModeChanged;
                _uiBuilder.OnAddSchematicClicked += OnAddSchematicClicked;
                _uiBuilder.OnImportClicked += OnImportClicked;
                _uiBuilder.OnExportClicked += OnExportClicked;
                _uiBuilder.OnRefreshClicked += OnRefreshClicked;
                _uiBuilder.OnPageChanged += OnPageChanged;
                _uiBuilder.OnItemsPerPageChanged += OnItemsPerPageChanged;
                _uiBuilder.OnSearchOptionsChanged += OnSearchOptionsChanged;
            }
            
            if (_dataManager != null)
            {
                _dataManager.OnFilteredSchematicsChanged += OnFilteredSchematicsChanged;
                _dataManager.OnDisplayedSchematicsChanged += OnDisplayedSchematicsChanged;
                _dataManager.OnSelectedSchematicChanged += OnSelectedSchematicChanged;
                _dataManager.OnPaginationChanged += OnPaginationChanged;
                _dataManager.OnSortingChanged += OnSortingChanged;
            }
            
            if (_displayController != null)
            {
                _displayController.OnSchematicSelected += OnSchematicSelected;
                _displayController.OnSchematicDoubleClicked += OnSchematicDoubleClicked;
                _displayController.OnSchematicRightClicked += OnSchematicRightClicked;
            }
        }
        
        /// <summary>
        /// Cleanup event handlers
        /// </summary>
        private void CleanupEventHandlers()
        {
            if (_searchController != null)
            {
                _searchController.OnFiltersChanged -= OnSearchFiltersChanged;
            }
            
            if (_uiBuilder != null)
            {
                _uiBuilder.OnSearchQueryChanged -= OnSearchQueryChanged;
                _uiBuilder.OnCategoryFilterChanged -= OnCategoryFilterChanged;
                _uiBuilder.OnComplexityFilterChanged -= OnComplexityFilterChanged;
                _uiBuilder.OnTagFilterChanged -= OnTagFilterChanged;
                _uiBuilder.OnSortModeChanged -= OnSortModeChanged;
                _uiBuilder.OnSortOrderChanged -= OnSortOrderChanged;
                _uiBuilder.OnViewModeChanged -= OnViewModeChanged;
                _uiBuilder.OnAddSchematicClicked -= OnAddSchematicClicked;
                _uiBuilder.OnImportClicked -= OnImportClicked;
                _uiBuilder.OnExportClicked -= OnExportClicked;
                _uiBuilder.OnRefreshClicked -= OnRefreshClicked;
                _uiBuilder.OnPageChanged -= OnPageChanged;
                _uiBuilder.OnItemsPerPageChanged -= OnItemsPerPageChanged;
                _uiBuilder.OnSearchOptionsChanged -= OnSearchOptionsChanged;
            }
            
            if (_dataManager != null)
            {
                _dataManager.OnFilteredSchematicsChanged -= OnFilteredSchematicsChanged;
                _dataManager.OnDisplayedSchematicsChanged -= OnDisplayedSchematicsChanged;
                _dataManager.OnSelectedSchematicChanged -= OnSelectedSchematicChanged;
                _dataManager.OnPaginationChanged -= OnPaginationChanged;
                _dataManager.OnSortingChanged -= OnSortingChanged;
            }
            
            if (_displayController != null)
            {
                _displayController.OnSchematicSelected -= OnSchematicSelected;
                _displayController.OnSchematicDoubleClicked -= OnSchematicDoubleClicked;
                _displayController.OnSchematicRightClicked -= OnSchematicRightClicked;
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Initialize default panel state
        /// </summary>
        private void InitializeDefaultState()
        {
            _currentViewMode = _defaultViewMode;
            
            // Set initial data manager configuration
            if (_dataManager != null)
            {
                _dataManager.SetItemsPerPage(_itemsPerPage);
                _dataManager.SetSorting(_defaultSortMode, _defaultSortAscending);
            }
            
            // Update UI to reflect initial state
            if (_uiBuilder != null)
            {
                _uiBuilder.UpdateViewModeButtons(_currentViewMode);
            }
        }
        
        /// <summary>
        /// Load initial schematic data
        /// </summary>
        private void LoadInitialData()
        {
            if (_dataManager != null)
            {
                _dataManager.LoadAllSchematics();
            }
            
            // Update tags filter with available tags
            RefreshTagsFilter();
        }
        
        /// <summary>
        /// Refresh tags filter with current data
        /// </summary>
        private void RefreshTagsFilter()
        {
            if (_dataManager != null && _uiBuilder != null)
            {
                var availableTags = _dataManager.GetAllAvailableTags();
                _uiBuilder.UpdateTagsFilter(availableTags);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        public void OnSearchQueryChanged(string query)
        {
            _searchController?.SetSearchQuery(query);
            SearchQueryChanged?.Invoke(query);
        }
        
        private void OnSearchOptionsChanged(bool byName, bool byDescription, bool byTags)
        {
            _searchController?.SetSearchOptions(byName, byDescription, byTags);
        }
        
        private void OnCategoryFilterChanged(ConstructionCategory category)
        {
            _searchController?.SetCategoryFilter(category);
        }
        
        private void OnComplexityFilterChanged(SchematicComplexity complexity)
        {
            _searchController?.SetComplexityFilter(complexity);
        }
        
        private void OnTagFilterChanged(string tag)
        {
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(tag) && tag != "All Tags")
            {
                tags.Add(tag);
            }
            _searchController?.SetTagFilters(tags);
        }
        
        private void OnSortModeChanged(LibrarySortMode sortMode)
        {
            _dataManager?.SetSorting(sortMode, _dataManager.CurrentSortAscending);
        }
        
        private void OnSortOrderChanged(bool ascending)
        {
            _dataManager?.SetSorting(_dataManager.CurrentSortMode, ascending);
        }
        
        public void OnViewModeChanged(LibraryViewMode viewMode)
        {
            _currentViewMode = viewMode;
            _uiBuilder?.UpdateViewModeButtons(viewMode);
            RefreshDisplay();
            ViewModeChanged?.Invoke(viewMode);
        }
        
        private void OnAddSchematicClicked()
        {
            ChimeraLogger.Log("Add Schematic functionality not implemented yet");
        }
        
        private void OnImportClicked()
        {
            ChimeraLogger.Log("Import functionality not implemented yet");
        }
        
        private void OnExportClicked()
        {
            ChimeraLogger.Log("Export functionality not implemented yet");
        }
        
        private void OnRefreshClicked()
        {
            _dataManager?.RefreshData();
            RefreshTagsFilter();
        }
        
        private void OnPageChanged(int delta)
        {
            _dataManager?.NavigatePage(delta);
        }
        
        private void OnItemsPerPageChanged(int itemsPerPage)
        {
            _dataManager?.SetItemsPerPage(itemsPerPage);
        }
        
        private void OnSearchFiltersChanged()
        {
            ApplyCurrentFilters();
        }
        
        private void OnFilteredSchematicsChanged(List<SchematicSO> filteredSchematics)
        {
            // Display controller will be updated via OnDisplayedSchematicsChanged
        }
        
        private void OnDisplayedSchematicsChanged(List<SchematicSO> displayedSchematics)
        {
            RefreshDisplay();
        }
        
        private void OnSelectedSchematicChanged(SchematicSO schematic)
        {
            _selectedSchematic = schematic;
        }
        
        private void OnPaginationChanged(int currentPage, int totalPages, int totalItems)
        {
            _uiBuilder?.UpdatePaginationDisplay(currentPage, totalPages, totalItems);
        }
        
        private void OnSortingChanged(LibrarySortMode sortMode, bool ascending)
        {
            // UI should already be updated by data manager
        }
        
        public void OnSchematicSelected(SchematicSO schematic)
        {
            _dataManager?.SetSelectedSchematic(schematic);
            SchematicSelected?.Invoke(schematic);
        }
        
        private void OnSchematicDoubleClicked(SchematicSO schematic)
        {
            UseSchematic(schematic);
        }
        
        private void OnSchematicRightClicked(SchematicSO schematic)
        {
            ShowSchematicContextMenu(schematic);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Refresh the display with current data
        /// </summary>
        public void RefreshDisplay()
        {
            if (_dataManager != null && _displayController != null)
            {
                var displayedSchematics = _dataManager.DisplayedSchematics;
                _displayController.UpdateDisplay(displayedSchematics, _currentViewMode);
                
                // Update empty state
                bool isEmpty = displayedSchematics.Count == 0;
                _uiBuilder?.SetEmptyStateVisible(isEmpty);
            }
        }
        
        /// <summary>
        /// Apply current search and filter settings
        /// </summary>
        private void ApplyCurrentFilters()
        {
            if (_searchController != null && _dataManager != null)
            {
                var allSchematics = _dataManager.LibrarySchematics;
                var filteredSchematics = _searchController.FilterSchematics(allSchematics);
                _dataManager.SetFilteredSchematics(filteredSchematics);
            }
        }
        
        /// <summary>
        /// Use selected schematic for construction
        /// </summary>
        private void UseSchematic(SchematicSO schematic)
        {
            if (schematic == null) return;
            
            if (_placementController != null)
            {
                _placementController.StartSchematicPlacement(schematic);
                ChimeraLogger.Log($"Started placement for schematic: {schematic.SchematicName}");
            }
            else
            {
                ChimeraLogger.LogWarning("No placement controller assigned");
            }
        }
        
        /// <summary>
        /// Show context menu for schematic
        /// </summary>
        private void ShowSchematicContextMenu(SchematicSO schematic)
        {
            ChimeraLogger.Log($"Context menu for: {schematic.SchematicName}");
        }
        
        /// <summary>
        /// Handle schematic applied event
        /// </summary>
        public void OnSchematicApplied(SchematicSO schematic)
        {
            // Handle schematic application logic here
            LogInfo($"Schematic applied: {schematic?.name}");
            SchematicApplied?.Invoke(schematic);
        }
        
        /// <summary>
        /// Handle schematic deleted event
        /// </summary>
        public void OnSchematicDeleted(SchematicSO schematic)
        {
            // Handle schematic deletion logic here
            _dataManager?.RemoveSchematic(schematic);
            LogInfo($"Schematic deleted: {schematic?.name}");
            SchematicDeleted?.Invoke(schematic);
        }
        
        /// <summary>
        /// Add a new schematic to the library
        /// </summary>
        public void AddSchematic(SchematicSO schematic)
        {
            _dataManager?.AddSchematic(schematic);
            LogInfo($"Schematic added: {schematic?.name}");
        }
        
        /// <summary>
        /// Remove a schematic from the library
        /// </summary>
        public void RemoveSchematic(SchematicSO schematic)
        {
            _dataManager?.RemoveSchematic(schematic);
            LogInfo($"Schematic removed: {schematic?.name}");
        }
        
        #endregion
    }
}