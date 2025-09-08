using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Construction;
using ProjectChimera.Shared;

namespace ProjectChimera.UI.Panels.Components
{
    /// <summary>
    /// Handles UI element creation and management for the schematic library.
    /// Creates search sections, filter sections, toolbars, content areas, and details panels.
    /// </summary>
    public class SchematicLibraryUIBuilder : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private int _gridColumnsLarge = 4;
        [SerializeField] private int _gridColumnsCompact = 6;
        [SerializeField] private float _detailsPanelWidth = 300f;
        [SerializeField] private bool _enableAdvancedSearch = true;
        [SerializeField] private bool _enableTagFiltering = true;
        [SerializeField] private bool _enableCategoryFiltering = true;
        [SerializeField] private bool _enableComplexityFiltering = true;
        
        // UI Elements
        private VisualElement _rootElement;
        private VisualElement _searchContainer;
        private VisualElement _filterContainer;
        private VisualElement _toolbarContainer;
        private VisualElement _contentContainer;
        private VisualElement _detailsContainer;
        private VisualElement _paginationContainer;
        
        // Search elements
        private TextField _searchField;
        private Toggle _searchByNameToggle;
        private Toggle _searchByDescriptionToggle;
        private Toggle _searchByTagsToggle;
        
        // Filter elements
        private DropdownField _categoryFilter;
        private DropdownField _complexityFilter;
        private DropdownField _tagsFilter;
        private DropdownField _sortDropdown;
        private Toggle _sortOrderToggle;
        
        // Toolbar elements
        private Button _gridViewButton;
        private Button _listViewButton;
        private Button _addSchematicButton;
        private Button _importButton;
        private Button _exportButton;
        private Button _refreshButton;
        
        // Content elements
        private ScrollView _libraryScrollView;
        private VisualElement _libraryGrid;
        private VisualElement _libraryList;
        private VisualElement _emptyState;
        
        // Details panel elements
        private VisualElement _detailsPanel;
        private Label _selectedSchematicName;
        private Label _selectedSchematicDescription;
        private VisualElement _selectedSchematicPreview;
        private VisualElement _selectedSchematicInfo;
        private VisualElement _selectedSchematicActions;
        
        // Pagination elements
        private Button _prevPageButton;
        private Button _nextPageButton;
        private Label _pageInfoLabel;
        private DropdownField _itemsPerPageDropdown;
        
        // Events
        public System.Action<string> OnSearchQueryChanged;
        public System.Action<bool, bool, bool> OnSearchOptionsChanged;
        public System.Action<ConstructionCategory> OnCategoryFilterChanged;
        public System.Action<SchematicComplexity> OnComplexityFilterChanged;
        public System.Action<string> OnTagFilterChanged;
        public System.Action<LibrarySortMode> OnSortModeChanged;
        public System.Action<bool> OnSortOrderChanged;
        public System.Action<LibraryViewMode> OnViewModeChanged;
        public System.Action OnAddSchematicClicked;
        public System.Action OnImportClicked;
        public System.Action OnExportClicked;
        public System.Action OnRefreshClicked;
        public System.Action<int> OnPageChanged;
        public System.Action<int> OnItemsPerPageChanged;
        
        // Properties
        public VisualElement RootElement => _rootElement;
        public TextField SearchField => _searchField;
        public ScrollView LibraryScrollView => _libraryScrollView;
        public VisualElement LibraryGrid => _libraryGrid;
        public VisualElement LibraryList => _libraryList;
        public VisualElement DetailsPanel => _detailsPanel;
        
        /// <summary>
        /// Initialize UI builder with root element
        /// </summary>
        public void Initialize(VisualElement rootElement)
        {
            _rootElement = rootElement;
            CreateCompleteUI();
            SetupEventHandlers();
        }
        
        #region UI Creation
        
        /// <summary>
        /// Create the complete library UI structure
        /// </summary>
        private void CreateCompleteUI()
        {
            if (_rootElement == null) return;
            
            _rootElement.Clear();
            _rootElement.AddToClassList("schematic-library");
            
            CreateSearchSection();
            CreateFilterSection();
            CreateToolbarSection();
            CreateContentSection();
            CreateDetailsSection();
            CreatePaginationSection();
        }
        
        /// <summary>
        /// Create search section with advanced search capabilities
        /// </summary>
        private void CreateSearchSection()
        {
            _searchContainer = new VisualElement();
            _searchContainer.AddToClassList("search-container");
            _rootElement.Add(_searchContainer);
            
            // Main search field
            _searchField = new TextField();
            _searchField.AddToClassList("library-search-field");
            _searchField.value = "";
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            
            // Add placeholder text functionality
            SetupSearchFieldPlaceholder();
            
            _searchContainer.Add(_searchField);
            
            // Search options
            if (_enableAdvancedSearch)
            {
                CreateAdvancedSearchOptions();
            }
        }
        
        /// <summary>
        /// Create advanced search options
        /// </summary>
        private void CreateAdvancedSearchOptions()
        {
            var searchOptionsContainer = new VisualElement();
            searchOptionsContainer.AddToClassList("search-options");
            
            _searchByNameToggle = new Toggle("Name");
            _searchByNameToggle.value = true;
            _searchByNameToggle.RegisterValueChangedCallback(OnSearchOptionsValueChanged);
            
            _searchByDescriptionToggle = new Toggle("Description");
            _searchByDescriptionToggle.value = true;
            _searchByDescriptionToggle.RegisterValueChangedCallback(OnSearchOptionsValueChanged);
            
            _searchByTagsToggle = new Toggle("Tags");
            _searchByTagsToggle.value = true;
            _searchByTagsToggle.RegisterValueChangedCallback(OnSearchOptionsValueChanged);
            
            searchOptionsContainer.Add(_searchByNameToggle);
            searchOptionsContainer.Add(_searchByDescriptionToggle);
            searchOptionsContainer.Add(_searchByTagsToggle);
            
            _searchContainer.Add(searchOptionsContainer);
        }
        
        /// <summary>
        /// Create filter section with multiple filter types
        /// </summary>
        private void CreateFilterSection()
        {
            _filterContainer = new VisualElement();
            _filterContainer.AddToClassList("filter-container");
            _rootElement.Add(_filterContainer);
            
            // Category filter
            if (_enableCategoryFiltering)
            {
                CreateCategoryFilter();
            }
            
            // Complexity filter
            if (_enableComplexityFiltering)
            {
                CreateComplexityFilter();
            }
            
            // Tags filter
            if (_enableTagFiltering)
            {
                CreateTagsFilter();
            }
            
            // Sort options
            CreateSortOptions();
        }
        
        /// <summary>
        /// Create category filter dropdown
        /// </summary>
        private void CreateCategoryFilter()
        {
            var categoryOptions = System.Enum.GetNames(typeof(ConstructionCategory)).ToList();
            categoryOptions.Insert(0, "All Categories");
            
            _categoryFilter = new DropdownField("Category", categoryOptions, 0);
            _categoryFilter.AddToClassList("category-filter");
            _categoryFilter.RegisterValueChangedCallback(OnCategoryFilterValueChanged);
            _filterContainer.Add(_categoryFilter);
        }
        
        /// <summary>
        /// Create complexity filter dropdown
        /// </summary>
        private void CreateComplexityFilter()
        {
            var complexityOptions = System.Enum.GetNames(typeof(SchematicComplexity)).ToList();
            complexityOptions.Insert(0, "All Complexities");
            
            _complexityFilter = new DropdownField("Complexity", complexityOptions, 0);
            _complexityFilter.AddToClassList("complexity-filter");
            _complexityFilter.RegisterValueChangedCallback(OnComplexityFilterValueChanged);
            _filterContainer.Add(_complexityFilter);
        }
        
        /// <summary>
        /// Create tags filter dropdown
        /// </summary>
        private void CreateTagsFilter()
        {
            var tagOptions = new List<string> { "All Tags" };
            
            _tagsFilter = new DropdownField("Tags", tagOptions, 0);
            _tagsFilter.AddToClassList("tags-filter");
            _tagsFilter.RegisterValueChangedCallback(OnTagFilterValueChanged);
            _filterContainer.Add(_tagsFilter);
        }
        
        /// <summary>
        /// Create sort options
        /// </summary>
        private void CreateSortOptions()
        {
            var sortOptions = new List<string> { "Name", "Creation Date", "Complexity", "Item Count", "Cost" };
            _sortDropdown = new DropdownField("Sort by", sortOptions, 0);
            _sortDropdown.AddToClassList("sort-dropdown");
            _sortDropdown.RegisterValueChangedCallback(OnSortModeValueChanged);
            _filterContainer.Add(_sortDropdown);
            
            _sortOrderToggle = new Toggle("Ascending");
            _sortOrderToggle.value = true;
            _sortOrderToggle.AddToClassList("sort-order-toggle");
            _sortOrderToggle.RegisterValueChangedCallback(OnSortOrderValueChanged);
            _filterContainer.Add(_sortOrderToggle);
        }
        
        /// <summary>
        /// Create toolbar with view and action buttons
        /// </summary>
        private void CreateToolbarSection()
        {
            _toolbarContainer = new VisualElement();
            _toolbarContainer.AddToClassList("toolbar-container");
            _rootElement.Add(_toolbarContainer);
            
            // View mode buttons
            CreateViewModeButtons();
            
            // Action buttons
            CreateActionButtons();
        }
        
        /// <summary>
        /// Create view mode buttons
        /// </summary>
        private void CreateViewModeButtons()
        {
            var viewModeContainer = new VisualElement();
            viewModeContainer.AddToClassList("view-mode-container");
            
            _gridViewButton = new Button(() => OnViewModeChanged?.Invoke(LibraryViewMode.Grid));
            _gridViewButton.text = "Grid";
            _gridViewButton.AddToClassList("view-mode-button");
            _gridViewButton.AddToClassList("grid-view-button");
            
            _listViewButton = new Button(() => OnViewModeChanged?.Invoke(LibraryViewMode.List));
            _listViewButton.text = "List";
            _listViewButton.AddToClassList("view-mode-button");
            _listViewButton.AddToClassList("list-view-button");
            
            viewModeContainer.Add(_gridViewButton);
            viewModeContainer.Add(_listViewButton);
            _toolbarContainer.Add(viewModeContainer);
        }
        
        /// <summary>
        /// Create action buttons
        /// </summary>
        private void CreateActionButtons()
        {
            var actionsContainer = new VisualElement();
            actionsContainer.AddToClassList("actions-container");
            
            _addSchematicButton = new Button(() => OnAddSchematicClicked?.Invoke());
            _addSchematicButton.text = "Add";
            _addSchematicButton.AddToClassList("action-button");
            _addSchematicButton.AddToClassList("add-button");
            
            _importButton = new Button(() => OnImportClicked?.Invoke());
            _importButton.text = "Import";
            _importButton.AddToClassList("action-button");
            _importButton.AddToClassList("import-button");
            
            _exportButton = new Button(() => OnExportClicked?.Invoke());
            _exportButton.text = "Export";
            _exportButton.AddToClassList("action-button");
            _exportButton.AddToClassList("export-button");
            
            _refreshButton = new Button(() => OnRefreshClicked?.Invoke());
            _refreshButton.text = "Refresh";
            _refreshButton.AddToClassList("action-button");
            _refreshButton.AddToClassList("refresh-button");
            
            actionsContainer.Add(_addSchematicButton);
            actionsContainer.Add(_importButton);
            actionsContainer.Add(_exportButton);
            actionsContainer.Add(_refreshButton);
            _toolbarContainer.Add(actionsContainer);
        }
        
        /// <summary>
        /// Create content section with grid and list views
        /// </summary>
        private void CreateContentSection()
        {
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("content-container");
            _rootElement.Add(_contentContainer);
            
            _libraryScrollView = new ScrollView();
            _libraryScrollView.AddToClassList("library-scroll-view");
            _contentContainer.Add(_libraryScrollView);
            
            // Grid view container
            _libraryGrid = new VisualElement();
            _libraryGrid.AddToClassList("library-grid");
            _libraryGrid.style.flexDirection = FlexDirection.Row;
            _libraryGrid.style.flexWrap = Wrap.Wrap;
            _libraryScrollView.Add(_libraryGrid);
            
            // List view container
            _libraryList = new VisualElement();
            _libraryList.AddToClassList("library-list");
            _libraryList.style.display = DisplayStyle.None;
            _libraryScrollView.Add(_libraryList);
            
            // Empty state
            CreateEmptyState();
        }
        
        /// <summary>
        /// Create empty state display
        /// </summary>
        private void CreateEmptyState()
        {
            _emptyState = new VisualElement();
            _emptyState.AddToClassList("empty-state");
            _emptyState.style.display = DisplayStyle.None;
            
            var emptyIcon = new Label("📋");
            emptyIcon.AddToClassList("empty-icon");
            
            var emptyTitle = new Label("No Schematics Found");
            emptyTitle.AddToClassList("empty-title");
            
            var emptyMessage = new Label("Try adjusting your search or filter criteria");
            emptyMessage.AddToClassList("empty-message");
            
            _emptyState.Add(emptyIcon);
            _emptyState.Add(emptyTitle);
            _emptyState.Add(emptyMessage);
            _libraryScrollView.Add(_emptyState);
        }
        
        /// <summary>
        /// Create details section for selected schematic
        /// </summary>
        private void CreateDetailsSection()
        {
            _detailsContainer = new VisualElement();
            _detailsContainer.AddToClassList("details-container");
            _detailsContainer.style.width = _detailsPanelWidth;
            _detailsContainer.style.display = DisplayStyle.None;
            _rootElement.Add(_detailsContainer);
            
            _detailsPanel = new VisualElement();
            _detailsPanel.AddToClassList("details-panel");
            _detailsContainer.Add(_detailsPanel);
            
            CreateDetailsContent();
        }
        
        /// <summary>
        /// Create details panel content
        /// </summary>
        private void CreateDetailsContent()
        {
            var header = new Label("Schematic Details");
            header.AddToClassList("details-header");
            _detailsPanel.Add(header);
            
            _selectedSchematicName = new Label();
            _selectedSchematicName.AddToClassList("selected-name");
            _detailsPanel.Add(_selectedSchematicName);
            
            _selectedSchematicDescription = new Label();
            _selectedSchematicDescription.AddToClassList("selected-description");
            _detailsPanel.Add(_selectedSchematicDescription);
            
            _selectedSchematicPreview = new VisualElement();
            _selectedSchematicPreview.AddToClassList("selected-preview");
            _detailsPanel.Add(_selectedSchematicPreview);
            
            _selectedSchematicInfo = new VisualElement();
            _selectedSchematicInfo.AddToClassList("selected-info");
            _detailsPanel.Add(_selectedSchematicInfo);
            
            _selectedSchematicActions = new VisualElement();
            _selectedSchematicActions.AddToClassList("selected-actions");
            _detailsPanel.Add(_selectedSchematicActions);
        }
        
        /// <summary>
        /// Create pagination section
        /// </summary>
        private void CreatePaginationSection()
        {
            _paginationContainer = new VisualElement();
            _paginationContainer.AddToClassList("pagination-container");
            _rootElement.Add(_paginationContainer);
            
            var paginationControls = new VisualElement();
            paginationControls.AddToClassList("pagination-controls");
            paginationControls.style.flexDirection = FlexDirection.Row;
            paginationControls.style.justifyContent = Justify.SpaceBetween;
            
            // Page navigation
            CreatePageNavigation(paginationControls);
            
            // Items per page
            CreateItemsPerPageControl(paginationControls);
            
            _paginationContainer.Add(paginationControls);
        }
        
        /// <summary>
        /// Create page navigation controls
        /// </summary>
        private void CreatePageNavigation(VisualElement parent)
        {
            var pageNavigation = new VisualElement();
            pageNavigation.AddToClassList("page-navigation");
            pageNavigation.style.flexDirection = FlexDirection.Row;
            
            _prevPageButton = new Button(() => OnPageChanged?.Invoke(-1));
            _prevPageButton.text = "◀";
            _prevPageButton.AddToClassList("page-button");
            _prevPageButton.AddToClassList("prev-button");
            
            _pageInfoLabel = new Label("Page 1 of 1");
            _pageInfoLabel.AddToClassList("page-info");
            
            _nextPageButton = new Button(() => OnPageChanged?.Invoke(1));
            _nextPageButton.text = "▶";
            _nextPageButton.AddToClassList("page-button");
            _nextPageButton.AddToClassList("next-button");
            
            pageNavigation.Add(_prevPageButton);
            pageNavigation.Add(_pageInfoLabel);
            pageNavigation.Add(_nextPageButton);
            parent.Add(pageNavigation);
        }
        
        /// <summary>
        /// Create items per page control
        /// </summary>
        private void CreateItemsPerPageControl(VisualElement parent)
        {
            var itemsPerPageOptions = new List<string> { "10", "20", "50", "100" };
            _itemsPerPageDropdown = new DropdownField("Items per page", itemsPerPageOptions, 1);
            _itemsPerPageDropdown.AddToClassList("items-per-page");
            _itemsPerPageDropdown.RegisterValueChangedCallback(OnItemsPerPageValueChanged);
            parent.Add(_itemsPerPageDropdown);
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Setup all event handlers
        /// </summary>
        private void SetupEventHandlers()
        {
            // Event handlers are already set up in individual creation methods
        }
        
        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            OnSearchQueryChanged?.Invoke(evt.newValue);
        }
        
        private void OnSearchOptionsValueChanged(ChangeEvent<bool> evt)
        {
            OnSearchOptionsChanged?.Invoke(
                _searchByNameToggle?.value ?? true,
                _searchByDescriptionToggle?.value ?? true,
                _searchByTagsToggle?.value ?? true
            );
        }
        
        private void OnCategoryFilterValueChanged(ChangeEvent<string> evt)
        {
            if (System.Enum.TryParse<ConstructionCategory>(evt.newValue, out var category))
            {
                OnCategoryFilterChanged?.Invoke(category);
            }
            else if (evt.newValue == "All Categories")
            {
                OnCategoryFilterChanged?.Invoke(ConstructionCategory.All);
            }
        }
        
        private void OnComplexityFilterValueChanged(ChangeEvent<string> evt)
        {
            if (System.Enum.TryParse<SchematicComplexity>(evt.newValue, out var complexity))
            {
                OnComplexityFilterChanged?.Invoke(complexity);
            }
            else if (evt.newValue == "All Complexities")
            {
                OnComplexityFilterChanged?.Invoke(SchematicComplexity.All);
            }
        }
        
        private void OnTagFilterValueChanged(ChangeEvent<string> evt)
        {
            OnTagFilterChanged?.Invoke(evt.newValue);
        }
        
        private void OnSortModeValueChanged(ChangeEvent<string> evt)
        {
            if (System.Enum.TryParse<LibrarySortMode>(evt.newValue.Replace(" ", ""), out var sortMode))
            {
                OnSortModeChanged?.Invoke(sortMode);
            }
        }
        
        private void OnSortOrderValueChanged(ChangeEvent<bool> evt)
        {
            OnSortOrderChanged?.Invoke(evt.newValue);
        }
        
        private void OnItemsPerPageValueChanged(ChangeEvent<string> evt)
        {
            if (int.TryParse(evt.newValue, out int itemsPerPage))
            {
                OnItemsPerPageChanged?.Invoke(itemsPerPage);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Update tags filter with available tags
        /// </summary>
        public void UpdateTagsFilter(List<string> availableTags)
        {
            if (_tagsFilter == null) return;
            
            var options = new List<string> { "All Tags" };
            options.AddRange(availableTags);
            
            int currentIndex = _tagsFilter.index;
            _tagsFilter.choices = options;
            
            if (currentIndex < options.Count)
                _tagsFilter.index = currentIndex;
            else
                _tagsFilter.index = 0;
        }
        
        /// <summary>
        /// Update view mode buttons state
        /// </summary>
        public void UpdateViewModeButtons(LibraryViewMode currentMode)
        {
            _gridViewButton?.RemoveFromClassList("active");
            _listViewButton?.RemoveFromClassList("active");
            
            if (currentMode == LibraryViewMode.Grid)
            {
                _gridViewButton?.AddToClassList("active");
                if (_libraryGrid != null) _libraryGrid.style.display = DisplayStyle.Flex;
                if (_libraryList != null) _libraryList.style.display = DisplayStyle.None;
            }
            else
            {
                _listViewButton?.AddToClassList("active");
                if (_libraryGrid != null) _libraryGrid.style.display = DisplayStyle.None;
                if (_libraryList != null) _libraryList.style.display = DisplayStyle.Flex;
            }
        }
        
        /// <summary>
        /// Update pagination display
        /// </summary>
        public void UpdatePaginationDisplay(int currentPage, int totalPages, int totalItems)
        {
            if (_pageInfoLabel != null)
            {
                _pageInfoLabel.text = $"Page {currentPage + 1} of {totalPages} ({totalItems} items)";
            }
            
            if (_prevPageButton != null)
                _prevPageButton.SetEnabled(currentPage > 0);
            
            if (_nextPageButton != null)
                _nextPageButton.SetEnabled(currentPage < totalPages - 1);
        }
        
        /// <summary>
        /// Show or hide empty state
        /// </summary>
        public void SetEmptyStateVisible(bool visible)
        {
            if (_emptyState != null)
            {
                _emptyState.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (_libraryGrid != null)
            {
                _libraryGrid.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            }
            
            if (_libraryList != null && _libraryList.style.display == DisplayStyle.Flex)
            {
                _libraryList.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
        
        /// <summary>
        /// Setup search field placeholder functionality
        /// </summary>
        private void SetupSearchFieldPlaceholder()
        {
            if (_searchField == null) return;
            
            // This would typically be handled with USS, but we can set it programmatically
            _searchField.RegisterCallback<FocusInEvent>(evt =>
            {
                if (string.IsNullOrEmpty(_searchField.value))
                {
                    _searchField.RemoveFromClassList("placeholder");
                }
            });
            
            _searchField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (string.IsNullOrEmpty(_searchField.value))
                {
                    _searchField.AddToClassList("placeholder");
                }
            });
        }
        
        #endregion
    }
    
    // LibraryViewMode enum moved to ProjectChimera.Shared for cross-assembly access
    
    /// <summary>
    /// Library sort modes
    /// </summary>
    public enum LibrarySortMode
    {
        Name,
        CreationDate,
        Complexity,
        ItemCount,
        Cost
    }
}