using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Construction palette UI panel for Project Chimera Phase 4.
    /// Provides tabbed interface for construction items, schematics, and building tools.
    /// Features drag-and-drop functionality, search/filtering, and category organization.
    /// </summary>
    public class ConstructionPalettePanel : UIPanel
    {
        [Header("Construction Palette Configuration")]
        [SerializeField] private ConstructionCatalog _constructionCatalog;
        [SerializeField] private List<SchematicSO> _availableSchematics = new List<SchematicSO>();
        [SerializeField] private GridPlacementController _placementController;
        
        [Header("Tab Configuration")]
        [SerializeField] private bool _showConstructionTab = true;
        [SerializeField] private bool _showSchematicsTab = true;
        [SerializeField] private bool _showToolsTab = true;
        [SerializeField] private bool _defaultToSchematicsTab = false;
        
        [Header("Search and Filtering")]
        [SerializeField] private bool _enableSearch = true;
        [SerializeField] private bool _enableCategoryFiltering = true;
        [SerializeField] private float _searchUpdateDelay = 0.3f;
        
        [Header("Layout Settings")]
        [SerializeField] private int _itemsPerRow = 4;
        [SerializeField] private float _itemSpacing = 8f;
        [SerializeField] private float _categorySpacing = 16f;
        
        // UI Elements
        private VisualElement _tabContainer;
        private VisualElement _contentContainer;
        private VisualElement _searchContainer;
        private TextField _searchField;
        private DropdownField _categoryFilter;
        
        // Tab elements
        private Button _constructionTab;
        private Button _schematicsTab;
        private Button _toolsTab;
        
        // Content panels
        private VisualElement _constructionPanel;
        private VisualElement _schematicsPanel;
        private VisualElement _toolsPanel;
        
        // Current state
        private PaletteTab _currentTab = PaletteTab.Construction;
        private string _currentSearchQuery = "";
        private ConstructionCategory _currentCategoryFilter = ConstructionCategory.Structure;
        private List<PaletteItem> _allItems = new List<PaletteItem>();
        private List<PaletteItem> _filteredItems = new List<PaletteItem>();
        
        // Events
        public System.Action<PaletteItem> OnItemSelected;
        public System.Action<SchematicSO> OnSchematicSelected;
        public System.Action<PaletteTab> OnTabChanged;
        public System.Action<string> OnSearchChanged;
        
        // Properties
        public PaletteTab CurrentTab => _currentTab;
        
        protected override void Awake()
        {
            base.Awake();
            FindReferences();
        }
        
        protected override void Start()
        {
            base.Start();
            InitializePalette();
        }
        
        /// <summary>
        /// Find required component references
        /// </summary>
        private void FindReferences()
        {
            if (_placementController == null)
            {
                _placementController = FindObjectOfType<GridPlacementController>();
            }
        }
        
        /// <summary>
        /// Initialize the construction palette UI
        /// </summary>
        private void InitializePalette()
        {
            if (!_isInitialized)
            {
                CreateUIStructure();
                PopulateItems();
                SetupEventHandlers();
                
                // Set default tab
                var defaultTab = _defaultToSchematicsTab ? PaletteTab.Schematics : PaletteTab.Construction;
                SwitchToTab(defaultTab);
                
                _isInitialized = true;
            }
        }
        
        /// <summary>
        /// Create the UI structure and elements
        /// </summary>
        private void CreateUIStructure()
        {
            if (_rootElement == null) return;
            
            _rootElement.Clear();
            _rootElement.AddToClassList("construction-palette");
            
            // Create header with tabs
            CreateTabHeader();
            
            // Create search and filter area
            if (_enableSearch || _enableCategoryFiltering)
            {
                CreateSearchAndFilters();
            }
            
            // Create content container
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("palette-content");
            _rootElement.Add(_contentContainer);
            
            // Create tab panels
            CreateTabPanels();
        }
        
        /// <summary>
        /// Create tab header with construction, schematics, and tools tabs
        /// </summary>
        private void CreateTabHeader()
        {
            _tabContainer = new VisualElement();
            _tabContainer.AddToClassList("tab-container");
            _rootElement.Add(_tabContainer);
            
            if (_showConstructionTab)
            {
                _constructionTab = new Button(() => SwitchToTab(PaletteTab.Construction));
                _constructionTab.text = "Construction";
                _constructionTab.AddToClassList("palette-tab");
                _constructionTab.AddToClassList("construction-tab");
                _tabContainer.Add(_constructionTab);
            }
            
            if (_showSchematicsTab)
            {
                _schematicsTab = new Button(() => SwitchToTab(PaletteTab.Schematics));
                _schematicsTab.text = "Schematics";
                _schematicsTab.AddToClassList("palette-tab");
                _schematicsTab.AddToClassList("schematics-tab");
                _tabContainer.Add(_schematicsTab);
            }
            
            if (_showToolsTab)
            {
                _toolsTab = new Button(() => SwitchToTab(PaletteTab.Tools));
                _toolsTab.text = "Tools";
                _toolsTab.AddToClassList("palette-tab");
                _toolsTab.AddToClassList("tools-tab");
                _tabContainer.Add(_toolsTab);
            }
        }
        
        /// <summary>
        /// Create search and filter controls
        /// </summary>
        private void CreateSearchAndFilters()
        {
            _searchContainer = new VisualElement();
            _searchContainer.AddToClassList("search-container");
            _rootElement.Add(_searchContainer);
            
            if (_enableSearch)
            {
                _searchField = new TextField("Search");
                _searchField.AddToClassList("palette-search");
                _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
                _searchContainer.Add(_searchField);
            }
            
            if (_enableCategoryFiltering)
            {
                var categoryOptions = System.Enum.GetNames(typeof(ConstructionCategory)).ToList();
                _categoryFilter = new DropdownField("Category", categoryOptions, 0);
                _categoryFilter.AddToClassList("category-filter");
                _categoryFilter.RegisterValueChangedCallback(OnCategoryFilterChanged);
                _searchContainer.Add(_categoryFilter);
            }
        }
        
        /// <summary>
        /// Create content panels for each tab
        /// </summary>
        private void CreateTabPanels()
        {
            // Construction panel
            _constructionPanel = new ScrollView();
            _constructionPanel.AddToClassList("construction-panel");
            _constructionPanel.AddToClassList("palette-panel");
            _contentContainer.Add(_constructionPanel);
            
            // Schematics panel
            _schematicsPanel = new ScrollView();
            _schematicsPanel.AddToClassList("schematics-panel");
            _schematicsPanel.AddToClassList("palette-panel");
            _contentContainer.Add(_schematicsPanel);
            
            // Tools panel
            _toolsPanel = new ScrollView();
            _toolsPanel.AddToClassList("tools-panel");
            _toolsPanel.AddToClassList("palette-panel");
            _contentContainer.Add(_toolsPanel);
        }
        
        /// <summary>
        /// Populate palette with construction items and schematics
        /// </summary>
        private void PopulateItems()
        {
            _allItems.Clear();
            
            // Add construction templates
            if (_constructionCatalog != null)
            {
                foreach (var template in _constructionCatalog.Templates)
                {
                    _allItems.Add(new PaletteItem
                    {
                        Type = PaletteItemType.Construction,
                        Name = template.TemplateName,
                        Description = template.Description,
                        Icon = template.Icon,
                        Category = template.Category,
                        ConstructionTemplate = template
                    });
                }
            }
            
            // Add schematics
            foreach (var schematic in _availableSchematics)
            {
                if (schematic != null)
                {
                    _allItems.Add(new PaletteItem
                    {
                        Type = PaletteItemType.Schematic,
                        Name = schematic.SchematicName,
                        Description = schematic.Description,
                        Icon = schematic.PreviewIcon,
                        Category = schematic.PrimaryCategory,
                        Schematic = schematic
                    });
                }
            }
            
            // Add tools
            AddToolItems();
            
            RefreshFilteredItems();
        }
        
        /// <summary>
        /// Add tool items to the palette
        /// </summary>
        private void AddToolItems()
        {
            _allItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Tool,
                Name = "Selection Tool",
                Description = "Select and manipulate objects",
                Category = ConstructionCategory.Utility,
                ToolType = PaletteToolType.Selection
            });
            
            _allItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Tool,
                Name = "Delete Tool",
                Description = "Remove selected objects",
                Category = ConstructionCategory.Utility,
                ToolType = PaletteToolType.Delete
            });
            
            _allItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Tool,
                Name = "Copy Tool",
                Description = "Duplicate selected objects",
                Category = ConstructionCategory.Utility,
                ToolType = PaletteToolType.Copy
            });
        }
        
        /// <summary>
        /// Switch to specified tab
        /// </summary>
        public void SwitchToTab(PaletteTab tab)
        {
            _currentTab = tab;
            
            // Update tab button states
            UpdateTabButtonStates();
            
            // Show/hide panels
            _constructionPanel.style.display = tab == PaletteTab.Construction ? DisplayStyle.Flex : DisplayStyle.None;
            _schematicsPanel.style.display = tab == PaletteTab.Schematics ? DisplayStyle.Flex : DisplayStyle.None;
            _toolsPanel.style.display = tab == PaletteTab.Tools ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Refresh content
            RefreshCurrentTabContent();
            
            OnTabChanged?.Invoke(tab);
        }
        
        /// <summary>
        /// Update visual states of tab buttons
        /// </summary>
        private void UpdateTabButtonStates()
        {
            _constructionTab?.RemoveFromClassList("active");
            _schematicsTab?.RemoveFromClassList("active");
            _toolsTab?.RemoveFromClassList("active");
            
            switch (_currentTab)
            {
                case PaletteTab.Construction:
                    _constructionTab?.AddToClassList("active");
                    break;
                case PaletteTab.Schematics:
                    _schematicsTab?.AddToClassList("active");
                    break;
                case PaletteTab.Tools:
                    _toolsTab?.AddToClassList("active");
                    break;
            }
        }
        
        /// <summary>
        /// Refresh content for current tab
        /// </summary>
        private void RefreshCurrentTabContent()
        {
            switch (_currentTab)
            {
                case PaletteTab.Construction:
                    PopulateConstructionPanel();
                    break;
                case PaletteTab.Schematics:
                    PopulateSchematicsPanel();
                    break;
                case PaletteTab.Tools:
                    PopulateToolsPanel();
                    break;
            }
        }
        
        /// <summary>
        /// Populate construction panel with filtered items
        /// </summary>
        private void PopulateConstructionPanel()
        {
            _constructionPanel.Clear();
            
            var constructionItems = _filteredItems.Where(item => item.Type == PaletteItemType.Construction).ToList();
            CreateItemGrid(_constructionPanel, constructionItems);
        }
        
        /// <summary>
        /// Populate schematics panel with filtered items
        /// </summary>
        private void PopulateSchematicsPanel()
        {
            _schematicsPanel.Clear();
            
            var schematicItems = _filteredItems.Where(item => item.Type == PaletteItemType.Schematic).ToList();
            CreateItemGrid(_schematicsPanel, schematicItems);
        }
        
        /// <summary>
        /// Populate tools panel with filtered items
        /// </summary>
        private void PopulateToolsPanel()
        {
            _toolsPanel.Clear();
            
            var toolItems = _filteredItems.Where(item => item.Type == PaletteItemType.Tool).ToList();
            CreateItemGrid(_toolsPanel, toolItems);
        }
        
        /// <summary>
        /// Create grid layout for palette items
        /// </summary>
        private void CreateItemGrid(VisualElement container, List<PaletteItem> items)
        {
            var grid = new VisualElement();
            grid.AddToClassList("palette-grid");
            
            foreach (var item in items)
            {
                var itemElement = CreatePaletteItemElement(item);
                grid.Add(itemElement);
            }
            
            container.Add(grid);
        }
        
        /// <summary>
        /// Create visual element for palette item
        /// </summary>
        private VisualElement CreatePaletteItemElement(PaletteItem item)
        {
            var itemElement = new VisualElement();
            itemElement.AddToClassList("palette-item");
            itemElement.AddToClassList($"palette-item-{item.Type.ToString().ToLower()}");
            
            // Icon
            if (item.Icon != null)
            {
                var iconElement = new VisualElement();
                iconElement.AddToClassList("palette-item-icon");
                iconElement.style.backgroundImage = new StyleBackground(item.Icon);
                itemElement.Add(iconElement);
            }
            
            // Name
            var nameLabel = new Label(item.Name);
            nameLabel.AddToClassList("palette-item-name");
            itemElement.Add(nameLabel);
            
            // Description (as tooltip)
            itemElement.tooltip = item.Description;
            
            // Click handler
            itemElement.RegisterCallback<ClickEvent>(evt => OnPaletteItemClicked(item));
            
            return itemElement;
        }
        
        /// <summary>
        /// Handle palette item click
        /// </summary>
        private void OnPaletteItemClicked(PaletteItem item)
        {
            switch (item.Type)
            {
                case PaletteItemType.Construction:
                    if (item.ConstructionTemplate != null && _placementController != null)
                    {
                        // Start placement mode with the selected template
                        var placeable = CreateGridPlaceableFromTemplate(item.ConstructionTemplate);
                        if (placeable != null)
                        {
                            _placementController.StartPlacement(placeable);
                        }
                    }
                    break;
                    
                case PaletteItemType.Schematic:
                    if (item.Schematic != null && _placementController != null)
                    {
                        _placementController.StartSchematicPlacement(item.Schematic);
                        OnSchematicSelected?.Invoke(item.Schematic);
                    }
                    break;
                    
                case PaletteItemType.Tool:
                    HandleToolSelection(item.ToolType);
                    break;
            }
            
            OnItemSelected?.Invoke(item);
        }
        
        /// <summary>
        /// Create GridPlaceable from construction template
        /// </summary>
        private GridPlaceable CreateGridPlaceableFromTemplate(GridConstructionTemplate template)
        {
            if (template.Prefab == null) return null;
            
            var tempObject = Instantiate(template.Prefab);
            var placeable = tempObject.GetComponent<GridPlaceable>();
            
            if (placeable == null)
            {
                placeable = tempObject.AddComponent<GridPlaceable>();
            }
            
            // Configure placeable with template data
            // This would require access to GridPlaceable's internal fields
            // For now, return the placeable as-is
            
            return placeable;
        }
        
        /// <summary>
        /// Handle tool selection
        /// </summary>
        private void HandleToolSelection(PaletteToolType toolType)
        {
            switch (toolType)
            {
                case PaletteToolType.Selection:
                    // Enable selection mode
                    break;
                case PaletteToolType.Delete:
                    // Enable delete mode
                    break;
                case PaletteToolType.Copy:
                    // Enable copy mode
                    break;
            }
        }
        
        /// <summary>
        /// Set up event handlers
        /// </summary>
        private void SetupEventHandlers()
        {
            // Search field events are already registered in CreateSearchAndFilters
        }
        
        /// <summary>
        /// Handle search value change
        /// </summary>
        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            _currentSearchQuery = evt.newValue;
            OnSearchChanged?.Invoke(_currentSearchQuery);
            
            // Delay search to avoid excessive filtering
            CancelInvoke(nameof(RefreshFilteredItems));
            Invoke(nameof(RefreshFilteredItems), _searchUpdateDelay);
        }
        
        /// <summary>
        /// Handle category filter change
        /// </summary>
        private void OnCategoryFilterChanged(ChangeEvent<string> evt)
        {
            if (System.Enum.TryParse<ConstructionCategory>(evt.newValue, out var category))
            {
                _currentCategoryFilter = category;
                RefreshFilteredItems();
            }
        }
        
        /// <summary>
        /// Refresh filtered items based on search and category
        /// </summary>
        private void RefreshFilteredItems()
        {
            _filteredItems.Clear();
            
            foreach (var item in _allItems)
            {
                bool matchesSearch = string.IsNullOrEmpty(_currentSearchQuery) ||
                                   item.Name.ToLower().Contains(_currentSearchQuery.ToLower()) ||
                                   item.Description.ToLower().Contains(_currentSearchQuery.ToLower());
                
                bool matchesCategory = !_enableCategoryFiltering || item.Category == _currentCategoryFilter;
                
                if (matchesSearch && matchesCategory)
                {
                    _filteredItems.Add(item);
                }
            }
            
            RefreshCurrentTabContent();
        }
        
        /// <summary>
        /// Add schematic to available schematics list
        /// </summary>
        public void AddSchematic(SchematicSO schematic)
        {
            if (schematic != null && !_availableSchematics.Contains(schematic))
            {
                _availableSchematics.Add(schematic);
                
                if (_isInitialized)
                {
                    PopulateItems();
                }
            }
        }
        
        /// <summary>
        /// Remove schematic from available schematics list
        /// </summary>
        public void RemoveSchematic(SchematicSO schematic)
        {
            if (_availableSchematics.Remove(schematic) && _isInitialized)
            {
                PopulateItems();
            }
        }
        
        /// <summary>
        /// Refresh the entire palette
        /// </summary>
        public void RefreshPalette()
        {
            PopulateItems();
        }
    }
    
    /// <summary>
    /// Palette tab types
    /// </summary>
    public enum PaletteTab
    {
        Construction,
        Schematics,
        Tools
    }
    
    /// <summary>
    /// Palette item types
    /// </summary>
    public enum PaletteItemType
    {
        Construction,
        Schematic,
        Tool
    }
    
    /// <summary>
    /// Tool types for palette
    /// </summary>
    public enum PaletteToolType
    {
        Selection,
        Delete,
        Copy,
        Move,
        Rotate
    }
    
    /// <summary>
    /// Represents an item in the construction palette
    /// </summary>
    [System.Serializable]
    public class PaletteItem
    {
        public PaletteItemType Type;
        public string Name;
        public string Description;
        public Sprite Icon;
        public ConstructionCategory Category;
        
        // Type-specific data
        public GridConstructionTemplate ConstructionTemplate;
        public SchematicSO Schematic;
        public PaletteToolType ToolType;
    }
}