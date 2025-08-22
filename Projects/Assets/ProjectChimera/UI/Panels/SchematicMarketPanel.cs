using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.UI.Core;
using ProjectChimera.Systems.Economy;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Comprehensive schematic market UI panel for Phase 8 MVP
    /// Provides schematic browsing, filtering, preview, and purchase functionality
    /// </summary>
    public class SchematicMarketPanel : UIPanel
    {
        [Header("Schematic Market Configuration")]
        [SerializeField] private string _categoryFilterContainer = "category-filters";
        [SerializeField] private string _priceFilterContainer = "price-filters";
        [SerializeField] private string _schematicGridContainer = "schematic-grid";
        [SerializeField] private string _schematicDetailsContainer = "schematic-details";
        [SerializeField] private string _purchaseButtonContainer = "purchase-actions";
        
        [Header("Filter Controls")]
        [SerializeField] private string _allCategoriesButton = "all-categories";
        [SerializeField] private string _facilityCategoryButton = "facility-category";
        [SerializeField] private string _equipmentCategoryButton = "equipment-category";
        [SerializeField] private string _advancedCategoryButton = "advanced-category";
        [SerializeField] private string _priceRangeSlider = "price-range";
        [SerializeField] private string _availableOnlyToggle = "available-only";
        [SerializeField] private string _searchField = "search-input";
        
        [Header("Schematic Display")]
        [SerializeField] private string _schematicCardTemplate = "schematic-card";
        [SerializeField] private string _selectedSchematicPanel = "selected-schematic";
        [SerializeField] private string _schematicPreviewImage = "schematic-preview";
        [SerializeField] private string _schematicTitleLabel = "schematic-title";
        [SerializeField] private string _schematicDescriptionLabel = "schematic-description";
        [SerializeField] private string _schematicPriceLabel = "schematic-price";
        [SerializeField] private string _schematicRequirementsLabel = "requirements";
        
        [Header("Purchase Controls")]
        [SerializeField] private string _purchaseButton = "purchase-btn";
        [SerializeField] private string _previewButton = "preview-btn";
        [SerializeField] private string _closeButton = "close-btn";
        [SerializeField] private string _refreshButton = "refresh-btn";
        
        [Header("Status Displays")]
        [SerializeField] private string _skillPointsDisplay = "skill-points";
        [SerializeField] private string _unlockedCountDisplay = "unlocked-count";
        [SerializeField] private string _totalCountDisplay = "total-count";
        [SerializeField] private string _filterStatusDisplay = "filter-status";
        
        [Header("Visual Configuration")]
        [SerializeField] private Color _availableSchematicColor = Color.green;
        [SerializeField] private Color _lockedSchematicColor = Color.red;
        [SerializeField] private Color _ownedSchematicColor = Color.blue;
        [SerializeField] private Color _insufficientFundsColor = Color.gray;
        [SerializeField] private bool _enableSchematicAnimations = true;
        [SerializeField] private float _cardHoverScale = 1.05f;
        
        // Service references
        private SchematicMarketService _marketService;
        private CurrencyManager _currencyManager;
        
        // UI state
        private SchematicCategoryType _selectedCategory = SchematicCategoryType.GrowRooms;
        private ConstructionSchematicSO _selectedSchematic;
        private List<ConstructionSchematicSO> _displayedSchematics = new List<ConstructionSchematicSO>();
        private SchematicFilterSettings _filterSettings = new SchematicFilterSettings();
        
        // UI elements
        private VisualElement _categoryFilterElement;
        private VisualElement _priceFilterElement;
        private VisualElement _schematicGridElement;
        private VisualElement _schematicDetailsElement;
        private VisualElement _selectedSchematicPanelElement;
        
        // Filter controls
        private Button _allCategoriesButtonElement;
        private Button _facilityCategoryButtonElement;
        private Button _equipmentCategoryButtonElement;
        private Button _advancedCategoryButtonElement;
        private Slider _priceRangeSliderElement;
        private Toggle _availableOnlyToggleElement;
        private TextField _searchFieldElement;
        
        // Detail elements
        private VisualElement _schematicPreviewElement;
        private Label _schematicTitleElement;
        private Label _schematicDescriptionElement;
        private Label _schematicPriceElement;
        private Label _schematicRequirementsElement;
        
        // Action buttons
        private Button _purchaseButtonElement;
        private Button _previewButtonElement;
        private Button _closeButtonElement;
        private Button _refreshButtonElement;
        
        // Status displays
        private Label _skillPointsDisplayElement;
        private Label _unlockedCountDisplayElement;
        private Label _totalCountDisplayElement;
        private Label _filterStatusDisplayElement;
        
        // Events
        public System.Action<ConstructionSchematicSO> OnSchematicSelected;
        public System.Action<ConstructionSchematicSO> OnSchematicPurchased;
        public System.Action<SchematicCategoryType> OnCategoryChanged;
        public System.Action OnMarketRefreshed;
        
        protected override void OnPanelInitialized()
        {
            InitializeServices();
            InitializeUIElements();
            SetupEventHandlers();
            InitializeFilters();
            RefreshSchematicDisplay();
            UpdateStatusDisplays();
            
            LogInfo("SchematicMarketPanel initialized with comprehensive market functionality");
        }
        
        protected override void OnAfterShow()
        {
            RefreshSchematicDisplay();
            UpdateStatusDisplays();
            RefreshSelectedSchematic();
        }
        
        protected override void OnAfterHide()
        {
            _selectedSchematic = null;
            ClearSelection();
        }
        
        /// <summary>
        /// Switch to a specific schematic category
        /// </summary>
        public void SwitchToCategory(SchematicCategoryType category)
        {
            if (_selectedCategory == category) return;
            
            _selectedCategory = category;
            _filterSettings.Category = category;
            UpdateCategorySelection();
            RefreshSchematicDisplay();
            OnCategoryChanged?.Invoke(category);
            
            LogInfo($"Switched to schematic category: {category}");
        }
        
        /// <summary>
        /// Select a specific schematic for detailed view
        /// </summary>
        public void SelectSchematic(ConstructionSchematicSO schematic)
        {
            if (schematic == null) return;
            
            _selectedSchematic = schematic;
            UpdateSelectedSchematicDisplay();
            UpdatePurchaseButtonState();
            OnSchematicSelected?.Invoke(schematic);
            
            LogInfo($"Selected schematic: {schematic.SchematicName}");
        }
        
        /// <summary>
        /// Purchase the currently selected schematic
        /// </summary>
        public void PurchaseSelectedSchematic()
        {
            if (_selectedSchematic == null || _marketService == null)
            {
                LogError("No schematic selected or market service unavailable");
                return;
            }
            
            // Attempt purchase
            bool success = _marketService.PurchaseSchematic(_selectedSchematic);
            if (success)
            {
                OnSchematicPurchased?.Invoke(_selectedSchematic);
                ShowPurchaseSuccess();
                RefreshSchematicDisplay();
                UpdateStatusDisplays();
                UpdatePurchaseButtonState();
                
                LogInfo($"Successfully purchased schematic: {_selectedSchematic.SchematicName}");
            }
            else
            {
                ShowPurchaseError("Purchase failed - insufficient funds or requirements not met");
            }
        }
        
        /// <summary>
        /// Apply search filter to schematics
        /// </summary>
        public void ApplySearchFilter(string searchTerm)
        {
            _filterSettings.SearchTerm = searchTerm;
            RefreshSchematicDisplay();
        }
        
        /// <summary>
        /// Apply price range filter
        /// </summary>
        public void ApplyPriceFilter(float minPrice, float maxPrice)
        {
            _filterSettings.MinPrice = minPrice;
            _filterSettings.MaxPrice = maxPrice;
            RefreshSchematicDisplay();
        }
        
        /// <summary>
        /// Toggle available-only filter
        /// </summary>
        public void ToggleAvailableOnly(bool availableOnly)
        {
            _filterSettings.ShowAvailableOnly = availableOnly;
            RefreshSchematicDisplay();
        }
        
        /// <summary>
        /// Refresh the schematic market display
        /// </summary>
        public void RefreshSchematicDisplay()
        {
            if (_schematicGridElement == null || _marketService == null) return;
            
            // Clear current display
            ClearSchematicDisplay();
            
            // Get schematics based on current filters
            var schematics = GetFilteredSchematics();
            
            // Update displayed schematics
            _displayedSchematics.Clear();
            _displayedSchematics.AddRange(schematics);
            
            // Create schematic cards
            foreach (var schematic in _displayedSchematics)
            {
                var schematicCard = CreateSchematicCard(schematic);
                _schematicGridElement.Add(schematicCard);
            }
            
            // Update filter status
            UpdateFilterStatusDisplay();
            
            // Show empty state if no schematics
            if (_displayedSchematics.Count == 0)
            {
                ShowEmptyState();
            }
            
            LogInfo($"Schematic display refreshed: {_displayedSchematics.Count} schematics shown");
        }
        
        /// <summary>
        /// Update status displays with current market data
        /// </summary>
        public void UpdateStatusDisplays()
        {
            if (_marketService == null) return;
            
            // Update skill points
            if (_skillPointsDisplayElement != null && _currencyManager != null)
            {
                _skillPointsDisplayElement.text = $"Skill Points: {_currencyManager.SkillPoints}";
            }
            
            // Update unlocked count
            int unlockedCount = _marketService.UnlockedSchematicsCount;
            if (_unlockedCountDisplayElement != null)
            {
                _unlockedCountDisplayElement.text = $"Unlocked: {unlockedCount}";
            }
            
            // Update total count
            int totalCount = _marketService.TotalSchematicsCount;
            if (_totalCountDisplayElement != null)
            {
                _totalCountDisplayElement.text = $"Total: {totalCount}";
            }
        }
        
        /// <summary>
        /// Refresh the market data from service
        /// </summary>
        public void RefreshMarketData()
        {
            if (_marketService != null)
            {
                // Just refresh the display - the service handles its own refresh logic
                RefreshSchematicDisplay();
                UpdateStatusDisplays();
                OnMarketRefreshed?.Invoke();
                
                LogInfo("Market data refreshed");
            }
        }
        
        /// <summary>
        /// Clear all applied filters
        /// </summary>
        public void ClearAllFilters()
        {
            _filterSettings = new SchematicFilterSettings();
            _selectedCategory = SchematicCategoryType.GrowRooms;
            
            // Reset UI controls
            if (_searchFieldElement != null)
                _searchFieldElement.value = "";
            if (_availableOnlyToggleElement != null)
                _availableOnlyToggleElement.value = false;
            if (_priceRangeSliderElement != null)
                _priceRangeSliderElement.value = _priceRangeSliderElement.highValue;
            
            UpdateCategorySelection();
            RefreshSchematicDisplay();
        }
        
        private void InitializeServices()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _marketService = gameManager.GetManager<SchematicMarketService>();
                _currencyManager = gameManager.GetManager<CurrencyManager>();
            }
            
            if (_marketService == null)
                LogError("SchematicMarketService not found");
            if (_currencyManager == null)
                LogError("CurrencyManager not found");
        }
        
        private void InitializeUIElements()
        {
            if (_rootElement == null) return;
            
            // Find main containers
            _categoryFilterElement = _rootElement.Q<VisualElement>(_categoryFilterContainer);
            _priceFilterElement = _rootElement.Q<VisualElement>(_priceFilterContainer);
            _schematicGridElement = _rootElement.Q<VisualElement>(_schematicGridContainer);
            _schematicDetailsElement = _rootElement.Q<VisualElement>(_schematicDetailsContainer);
            _selectedSchematicPanelElement = _rootElement.Q<VisualElement>(_selectedSchematicPanel);
            
            // Find filter controls
            _allCategoriesButtonElement = _rootElement.Q<Button>(_allCategoriesButton);
            _facilityCategoryButtonElement = _rootElement.Q<Button>(_facilityCategoryButton);
            _equipmentCategoryButtonElement = _rootElement.Q<Button>(_equipmentCategoryButton);
            _advancedCategoryButtonElement = _rootElement.Q<Button>(_advancedCategoryButton);
            _priceRangeSliderElement = _rootElement.Q<Slider>(_priceRangeSlider);
            _availableOnlyToggleElement = _rootElement.Q<Toggle>(_availableOnlyToggle);
            _searchFieldElement = _rootElement.Q<TextField>(_searchField);
            
            // Find detail elements
            _schematicPreviewElement = _rootElement.Q<VisualElement>(_schematicPreviewImage);
            _schematicTitleElement = _rootElement.Q<Label>(_schematicTitleLabel);
            _schematicDescriptionElement = _rootElement.Q<Label>(_schematicDescriptionLabel);
            _schematicPriceElement = _rootElement.Q<Label>(_schematicPriceLabel);
            _schematicRequirementsElement = _rootElement.Q<Label>(_schematicRequirementsLabel);
            
            // Find action buttons
            _purchaseButtonElement = _rootElement.Q<Button>(_purchaseButton);
            _previewButtonElement = _rootElement.Q<Button>(_previewButton);
            _closeButtonElement = _rootElement.Q<Button>(_closeButton);
            _refreshButtonElement = _rootElement.Q<Button>(_refreshButton);
            
            // Find status displays
            _skillPointsDisplayElement = _rootElement.Q<Label>(_skillPointsDisplay);
            _unlockedCountDisplayElement = _rootElement.Q<Label>(_unlockedCountDisplay);
            _totalCountDisplayElement = _rootElement.Q<Label>(_totalCountDisplay);
            _filterStatusDisplayElement = _rootElement.Q<Label>(_filterStatusDisplay);
            
            // Set initial visibility
            if (_selectedSchematicPanelElement != null)
                _selectedSchematicPanelElement.style.display = DisplayStyle.None;
        }
        
        private void SetupEventHandlers()
        {
            // Category button handlers
            if (_allCategoriesButtonElement != null)
                _allCategoriesButtonElement.clicked += () => SwitchToCategory(SchematicCategoryType.GrowRooms);
            if (_facilityCategoryButtonElement != null)
                _facilityCategoryButtonElement.clicked += () => SwitchToCategory(SchematicCategoryType.ProcessingFacilities);
            if (_equipmentCategoryButtonElement != null)
                _equipmentCategoryButtonElement.clicked += () => SwitchToCategory(SchematicCategoryType.AutomationEquipment);
            if (_advancedCategoryButtonElement != null)
                _advancedCategoryButtonElement.clicked += () => SwitchToCategory(SchematicCategoryType.SpecializedEquipment);
            
            // Filter control handlers
            if (_searchFieldElement != null)
                _searchFieldElement.RegisterValueChangedCallback(evt => ApplySearchFilter(evt.newValue));
            if (_availableOnlyToggleElement != null)
                _availableOnlyToggleElement.RegisterValueChangedCallback(evt => ToggleAvailableOnly(evt.newValue));
            if (_priceRangeSliderElement != null)
                _priceRangeSliderElement.RegisterValueChangedCallback(evt => ApplyPriceFilter(0f, evt.newValue));
            
            // Action button handlers
            if (_purchaseButtonElement != null)
                _purchaseButtonElement.clicked += PurchaseSelectedSchematic;
            if (_previewButtonElement != null)
                _previewButtonElement.clicked += () => { /* Preview functionality */ };
            if (_closeButtonElement != null)
                _closeButtonElement.clicked += () => Hide();
            if (_refreshButtonElement != null)
                _refreshButtonElement.clicked += RefreshMarketData;
            
            // Service event handlers
            if (_marketService != null)
            {
                _marketService.OnSchematicPurchased += OnSchematicPurchasedHandler;
                _marketService.OnSchematicUnlocked += OnSchematicUnlockedHandler;
                _marketService.OnMarketRefreshed += OnMarketDataUpdatedHandler;
            }
            
            if (_currencyManager != null)
            {
                _currencyManager.OnCurrencyChanged += OnCurrencyChangedHandler;
            }
        }
        
        private void InitializeFilters()
        {
            // Set up price range slider
            if (_priceRangeSliderElement != null && _marketService != null)
            {
                _priceRangeSliderElement.lowValue = 0f;
                _priceRangeSliderElement.highValue = 1000f; // Default max price
                _priceRangeSliderElement.value = 1000f;
                
                _filterSettings.MaxPrice = 1000f;
            }
            
            UpdateCategorySelection();
        }
        
        private void UpdateCategorySelection()
        {
            // Update category button states
            UpdateCategoryButtonState(_allCategoriesButtonElement, _selectedCategory == SchematicCategoryType.GrowRooms);
            UpdateCategoryButtonState(_facilityCategoryButtonElement, _selectedCategory == SchematicCategoryType.ProcessingFacilities);
            UpdateCategoryButtonState(_equipmentCategoryButtonElement, _selectedCategory == SchematicCategoryType.AutomationEquipment);
            UpdateCategoryButtonState(_advancedCategoryButtonElement, _selectedCategory == SchematicCategoryType.SpecializedEquipment);
        }
        
        private void UpdateCategoryButtonState(Button button, bool isSelected)
        {
            if (button == null) return;
            
            if (isSelected)
            {
                button.AddToClassList("category-selected");
                button.RemoveFromClassList("category-unselected");
            }
            else
            {
                button.AddToClassList("category-unselected");
                button.RemoveFromClassList("category-selected");
            }
        }
        
        private List<ConstructionSchematicSO> GetFilteredSchematics()
        {
            if (_marketService?.MarketConfiguration == null) return new List<ConstructionSchematicSO>();
            
            var schematics = _marketService.MarketConfiguration.GetAllSchematics();
            var filtered = schematics.AsEnumerable();
            
            // Apply category filter
            if (_filterSettings.Category != null)
            {
                filtered = filtered.Where(s => s.CategoryType == _filterSettings.Category);
            }
            
            // Apply search filter
            if (!string.IsNullOrEmpty(_filterSettings.SearchTerm))
            {
                string searchLower = _filterSettings.SearchTerm.ToLower();
                filtered = filtered.Where(s => 
                    s.SchematicName.ToLower().Contains(searchLower) ||
                    s.SchematicDescription.ToLower().Contains(searchLower));
            }
            
            // Apply price filter
            if (_filterSettings.MaxPrice > 0)
            {
                filtered = filtered.Where(s => s.SkillPointCost <= _filterSettings.MaxPrice);
            }
            
            // Apply availability filter
            if (_filterSettings.ShowAvailableOnly)
            {
                filtered = filtered.Where(s => !s.IsUnlocked);
            }
            
            return filtered.ToList();
        }
        
        private VisualElement CreateSchematicCard(ConstructionSchematicSO schematic)
        {
            var card = new VisualElement();
            card.AddToClassList("schematic-card");
            card.style.flexDirection = FlexDirection.Column;
            card.style.backgroundColor = GetSchematicColor(schematic);
            card.style.marginBottom = 8;
            card.style.marginRight = 8;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Color.gray;
            card.style.borderBottomColor = Color.gray;
            card.style.borderLeftColor = Color.gray;
            card.style.borderRightColor = Color.gray;
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.minWidth = 200;
            card.style.maxWidth = 250;
            
            // Schematic header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            
            var nameLabel = new Label(schematic.SchematicName)
            {
                style = { 
                    fontSize = 14,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1
                }
            };
            
            var priceLabel = new Label($"{schematic.SkillPointCost} SP")
            {
                style = { 
                    fontSize = 12,
                    color = CanAffordSchematic(schematic) ? Color.green : _insufficientFundsColor,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            header.Add(nameLabel);
            header.Add(priceLabel);
            
            // Schematic details
            var details = new VisualElement();
            details.style.marginTop = 8;
            
            var categoryLabel = new Label($"Category: {schematic.CategoryType}")
            {
                style = { fontSize = 10, color = Color.gray }
            };
            
            var descriptionLabel = new Label(schematic.SchematicDescription)
            {
                style = { 
                    fontSize = 10, 
                    color = Color.white,
                    whiteSpace = WhiteSpace.Normal,
                    marginTop = 4
                }
            };
            
            details.Add(categoryLabel);
            details.Add(descriptionLabel);
            
            // Status indicator
            var statusLabel = new Label(GetSchematicStatus(schematic))
            {
                style = { 
                    fontSize = 10,
                    color = GetSchematicStatusColor(schematic),
                    marginTop = 4,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            card.Add(header);
            card.Add(details);
            card.Add(statusLabel);
            
            // Click handler
            card.RegisterCallback<ClickEvent>(_ => SelectSchematic(schematic));
            
            // Hover effects
            if (_enableSchematicAnimations)
            {
                card.RegisterCallback<MouseEnterEvent>(_ => {
                    card.style.scale = new Scale(Vector3.one * _cardHoverScale);
                    card.style.backgroundColor = Color.Lerp(GetSchematicColor(schematic), Color.white, 0.1f);
                });
                
                card.RegisterCallback<MouseLeaveEvent>(_ => {
                    card.style.scale = new Scale(Vector3.one);
                    card.style.backgroundColor = GetSchematicColor(schematic);
                });
            }
            
            return card;
        }
        
        private Color GetSchematicColor(ConstructionSchematicSO schematic)
        {
            if (_marketService == null) return Color.gray;
            
            if (schematic.IsUnlocked)
                return _ownedSchematicColor;
            if (!CanAffordSchematic(schematic))
                return _insufficientFundsColor;
            
            return _availableSchematicColor;
        }
        
        private string GetSchematicStatus(ConstructionSchematicSO schematic)
        {
            if (_marketService == null) return "Unknown";
            
            if (schematic.IsUnlocked)
                return "OWNED";
            if (!CanAffordSchematic(schematic))
                return "INSUFFICIENT FUNDS";
            
            return "AVAILABLE";
        }
        
        private Color GetSchematicStatusColor(ConstructionSchematicSO schematic)
        {
            if (_marketService == null) return Color.gray;
            
            if (schematic.IsUnlocked)
                return _ownedSchematicColor;
            if (!CanAffordSchematic(schematic))
                return _insufficientFundsColor;
            
            return _availableSchematicColor;
        }
        
        private bool CanAffordSchematic(ConstructionSchematicSO schematic)
        {
            return _currencyManager != null && _currencyManager.SkillPoints >= schematic.SkillPointCost;
        }
        
        private void ClearSchematicDisplay()
        {
            if (_schematicGridElement != null)
            {
                _schematicGridElement.Clear();
            }
        }
        
        private void ShowEmptyState()
        {
            if (_schematicGridElement == null) return;
            
            string emptyMessage = "No schematics match current filters";
            
            var emptyLabel = new Label(emptyMessage)
            {
                style = { 
                    color = Color.gray,
                    fontSize = 14,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    paddingTop = 40,
                    paddingBottom = 40
                }
            };
            
            _schematicGridElement.Add(emptyLabel);
        }
        
        private void UpdateSelectedSchematicDisplay()
        {
            if (_selectedSchematic == null || _selectedSchematicPanelElement == null)
            {
                if (_selectedSchematicPanelElement != null)
                    _selectedSchematicPanelElement.style.display = DisplayStyle.None;
                return;
            }
            
            _selectedSchematicPanelElement.style.display = DisplayStyle.Flex;
            
            // Update schematic details
            if (_schematicTitleElement != null)
                _schematicTitleElement.text = _selectedSchematic.SchematicName;
            
            if (_schematicDescriptionElement != null)
                _schematicDescriptionElement.text = _selectedSchematic.SchematicDescription;
            
            if (_schematicPriceElement != null)
                _schematicPriceElement.text = $"Cost: {_selectedSchematic.SkillPointCost} Skill Points";
            
            if (_schematicRequirementsElement != null)
            {
                var requirements = GetRequirementsText(_selectedSchematic);
                _schematicRequirementsElement.text = requirements;
            }
            
            LogInfo($"Updated selected schematic display for: {_selectedSchematic.SchematicName}");
        }
        
        private string GetRequirementsText(ConstructionSchematicSO schematic)
        {
            var requirements = new List<string>();
            
            if (schematic.UnlockLevel > 0)
                requirements.Add($"Level {schematic.UnlockLevel}");
            
            if (schematic.PrerequisiteSchematicIds != null && schematic.PrerequisiteSchematicIds.Count > 0)
            {
                foreach (var prereq in schematic.PrerequisiteSchematicIds)
                {
                    requirements.Add($"• {prereq}");
                }
            }
            
            return requirements.Count > 0 ? $"Requirements:\n{string.Join("\n", requirements)}" : "No special requirements";
        }
        
        private void UpdatePurchaseButtonState()
        {
            if (_purchaseButtonElement == null) return;
            
            bool canPurchase = _selectedSchematic != null && 
                              _marketService != null && 
                              !_selectedSchematic.IsUnlocked &&
                              CanAffordSchematic(_selectedSchematic);
            
            _purchaseButtonElement.SetEnabled(canPurchase);
            _purchaseButtonElement.text = canPurchase ? "Purchase Schematic" : "Cannot Purchase";
            
            if (canPurchase)
            {
                _purchaseButtonElement.style.backgroundColor = _availableSchematicColor;
                _purchaseButtonElement.style.color = Color.white;
            }
            else
            {
                _purchaseButtonElement.style.backgroundColor = Color.gray;
                _purchaseButtonElement.style.color = Color.black;
            }
        }
        
        private void UpdateFilterStatusDisplay()
        {
            if (_filterStatusDisplayElement == null) return;
            
            var activeFilters = new List<string>();
            
            if (_filterSettings.Category != null)
                activeFilters.Add($"Category: {_filterSettings.Category}");
            
            if (!string.IsNullOrEmpty(_filterSettings.SearchTerm))
                activeFilters.Add($"Search: '{_filterSettings.SearchTerm}'");
            
            if (_filterSettings.ShowAvailableOnly)
                activeFilters.Add("Available Only");
            
            if (_filterSettings.MaxPrice > 0 && _priceRangeSliderElement != null && 
                _filterSettings.MaxPrice < _priceRangeSliderElement.highValue)
                activeFilters.Add($"Max Price: {_filterSettings.MaxPrice} SP");
            
            string statusText = activeFilters.Count > 0 
                ? $"Filters: {string.Join(", ", activeFilters)} | Showing: {_displayedSchematics.Count}"
                : $"No filters applied | Showing: {_displayedSchematics.Count}";
            
            _filterStatusDisplayElement.text = statusText;
        }
        
        private void ShowPurchaseSuccess()
        {
            LogInfo("Schematic purchased successfully!");
            // Could show a success notification here
        }
        
        private void ShowPurchaseError(string message)
        {
            LogError($"Purchase failed: {message}");
            // Could show an error modal here
        }
        
        private void ClearSelection()
        {
            _selectedSchematic = null;
            if (_selectedSchematicPanelElement != null)
                _selectedSchematicPanelElement.style.display = DisplayStyle.None;
        }
        
        private void RefreshSelectedSchematic()
        {
            if (_selectedSchematic != null)
            {
                UpdateSelectedSchematicDisplay();
                UpdatePurchaseButtonState();
            }
        }
        
        // Event handlers
        private void OnSchematicPurchasedHandler(ConstructionSchematicSO schematic, float cost)
        {
            RefreshSchematicDisplay();
            UpdateStatusDisplays();
            UpdatePurchaseButtonState();
        }
        
        private void OnSchematicUnlockedHandler(ConstructionSchematicSO schematic)
        {
            RefreshSchematicDisplay();
            UpdateStatusDisplays();
        }
        
        private void OnMarketDataUpdatedHandler()
        {
            RefreshSchematicDisplay();
            UpdateStatusDisplays();
        }
        
        private void OnCurrencyChangedHandler(CurrencyType currencyType, float oldValue, float newValue)
        {
            // Only respond to skill points changes
            if (currencyType == CurrencyType.SkillPoints)
            {
                UpdateStatusDisplays();
                UpdatePurchaseButtonState();
                RefreshSchematicDisplay(); // Update affordability colors
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (_marketService != null)
            {
                _marketService.OnSchematicPurchased -= OnSchematicPurchasedHandler;
                _marketService.OnSchematicUnlocked -= OnSchematicUnlockedHandler;
                _marketService.OnMarketRefreshed -= OnMarketDataUpdatedHandler;
            }
            
            if (_currencyManager != null)
            {
                _currencyManager.OnCurrencyChanged -= OnCurrencyChangedHandler;
            }
            
            base.OnDestroy();
        }
    }
    
    /// <summary>
    /// Schematic filter settings
    /// </summary>
    [System.Serializable]
    public class SchematicFilterSettings
    {
        public SchematicCategoryType? Category = null;
        public string SearchTerm = "";
        public float MinPrice = 0f;
        public float MaxPrice = 0f;
        public bool ShowAvailableOnly = false;
        public bool ShowOwnedOnly = false;
    }
}