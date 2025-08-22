using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.UI.Core;
using ProjectChimera.Systems.Economy;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// UI panel for schematic purchase with Skill Points integration for Phase 8 MVP
    /// Displays available schematics, categories, and purchase functionality
    /// </summary>
    public class SchematicPurchasePanel : UIPanel
    {
        [Header("Schematic Purchase Configuration")]
        [SerializeField] private string _categoryListContainer = "category-list";
        [SerializeField] private string _schematicGridContainer = "schematic-grid";
        [SerializeField] private string _detailsContainer = "schematic-details";
        [SerializeField] private string _skillPointsContainer = "skill-points-display";
        
        [Header("UI Element Names")]
        [SerializeField] private string _skillPointsLabel = "skill-points-amount";
        [SerializeField] private string _categoryButtonTemplate = "category-button";
        [SerializeField] private string _schematicCardTemplate = "schematic-card";
        [SerializeField] private string _selectedSchematicTitle = "selected-title";
        [SerializeField] private string _selectedSchematicDescription = "selected-description";
        [SerializeField] private string _selectedSchematicCost = "selected-cost";
        [SerializeField] private string _selectedSchematicLevel = "selected-level";
        [SerializeField] private string _purchaseButton = "purchase-button";
        [SerializeField] private string _closeButton = "close-button";
        
        [Header("Visual Configuration")]
        [SerializeField] private Color _affordableColor = Color.green;
        [SerializeField] private Color _unaffordableColor = Color.red;
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Color _selectedCategoryColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private bool _enablePurchaseAnimations = true;
        
        // Service references
        private SchematicMarketService _marketService;
        private CurrencyManager _currencyManager;
        
        // UI state
        private SchematicCategoryType _selectedCategory = SchematicCategoryType.GrowRooms;
        private ConstructionSchematicSO _selectedSchematic;
        private List<ConstructionSchematicSO> _displayedSchematics = new List<ConstructionSchematicSO>();
        private float _currentSkillPoints;
        
        // UI elements
        private VisualElement _categoryListElement;
        private VisualElement _schematicGridElement;
        private VisualElement _detailsElement;
        private VisualElement _skillPointsElement;
        private Label _skillPointsLabelElement;
        private Label _selectedTitleElement;
        private Label _selectedDescriptionElement;
        private Label _selectedCostElement;
        private Label _selectedLevelElement;
        private Button _purchaseButtonElement;
        private Button _closeButtonElement;
        
        // Category buttons
        private Dictionary<SchematicCategoryType, Button> _categoryButtons = new Dictionary<SchematicCategoryType, Button>();
        
        // Events
        public System.Action<ConstructionSchematicSO> OnSchematicSelected;
        public System.Action<ConstructionSchematicSO> OnSchematicPurchased;
        public System.Action OnPanelClosed;
        
        protected override void OnPanelInitialized()
        {
            InitializeServices();
            InitializeUIElements();
            SetupEventHandlers();
            RefreshSkillPointsDisplay();
            BuildCategoryList();
            SelectCategory(_selectedCategory);
            
            LogInfo("SchematicPurchasePanel initialized");
        }
        
        protected override void OnAfterShow()
        {
            RefreshSkillPointsDisplay();
            RefreshSchematicGrid();
            RefreshSelectedSchematic();
        }
        
        protected override void OnAfterHide()
        {
            _selectedSchematic = null;
            ClearSelection();
        }
        
        /// <summary>
        /// Select a category to display
        /// </summary>
        public void SelectCategory(SchematicCategoryType category)
        {
            if (_selectedCategory == category) return;
            
            _selectedCategory = category;
            UpdateCategorySelection();
            RefreshSchematicGrid();
            ClearSelection();
            
            LogInfo($"Selected schematic category: {category}");
        }
        
        /// <summary>
        /// Select a specific schematic for detailed view
        /// </summary>
        public void SelectSchematic(ConstructionSchematicSO schematic)
        {
            if (schematic == null) return;
            
            _selectedSchematic = schematic;
            UpdateSchematicDetails();
            OnSchematicSelected?.Invoke(schematic);
            
            LogInfo($"Selected schematic: {schematic.SchematicName}");
        }
        
        /// <summary>
        /// Purchase the selected schematic
        /// </summary>
        public void PurchaseSelectedSchematic()
        {
            if (_selectedSchematic == null || _marketService == null)
            {
                LogError("No schematic selected or market service unavailable");
                return;
            }
            
            bool success = _marketService.PurchaseSchematic(_selectedSchematic);
            if (success)
            {
                OnSchematicPurchased?.Invoke(_selectedSchematic);
                RefreshSkillPointsDisplay();
                RefreshSchematicGrid();
                UpdateSchematicDetails(); // Update purchase button state
                
                LogInfo($"Successfully purchased: {_selectedSchematic.SchematicName}");
            }
            else
            {
                LogWarning($"Failed to purchase: {_selectedSchematic.SchematicName}");
            }
        }
        
        /// <summary>
        /// Close the panel
        /// </summary>
        public void ClosePanel()
        {
            Hide();
            OnPanelClosed?.Invoke();
        }
        
        /// <summary>
        /// Refresh the entire panel content
        /// </summary>
        public void RefreshPanel()
        {
            RefreshSkillPointsDisplay();
            RefreshSchematicGrid();
            RefreshSelectedSchematic();
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
            _categoryListElement = _rootElement.Q<VisualElement>(_categoryListContainer);
            _schematicGridElement = _rootElement.Q<VisualElement>(_schematicGridContainer);
            _detailsElement = _rootElement.Q<VisualElement>(_detailsContainer);
            _skillPointsElement = _rootElement.Q<VisualElement>(_skillPointsContainer);
            
            // Find UI elements
            _skillPointsLabelElement = _rootElement.Q<Label>(_skillPointsLabel);
            _selectedTitleElement = _rootElement.Q<Label>(_selectedSchematicTitle);
            _selectedDescriptionElement = _rootElement.Q<Label>(_selectedSchematicDescription);
            _selectedCostElement = _rootElement.Q<Label>(_selectedSchematicCost);
            _selectedLevelElement = _rootElement.Q<Label>(_selectedSchematicLevel);
            _purchaseButtonElement = _rootElement.Q<Button>(_purchaseButton);
            _closeButtonElement = _rootElement.Q<Button>(_closeButton);
            
            // Set initial visibility
            if (_detailsElement != null)
                _detailsElement.style.display = DisplayStyle.None;
        }
        
        private void SetupEventHandlers()
        {
            // Button handlers
            if (_purchaseButtonElement != null)
                _purchaseButtonElement.clicked += PurchaseSelectedSchematic;
            
            if (_closeButtonElement != null)
                _closeButtonElement.clicked += ClosePanel;
            
            // Service event handlers
            if (_marketService != null)
            {
                _marketService.OnSchematicPurchased += OnSchematicPurchasedHandler;
                _marketService.OnPurchaseError += OnPurchaseErrorHandler;
            }
            
            if (_currencyManager != null)
            {
                _currencyManager.OnCurrencyChanged += OnCurrencyChangedHandler;
            }
        }
        
        private void BuildCategoryList()
        {
            if (_categoryListElement == null || _marketService?.MarketConfiguration == null) return;
            
            _categoryListElement.Clear();
            _categoryButtons.Clear();
            
            var categories = _marketService.MarketConfiguration.SchematicCategories;
            
            foreach (var category in categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
            {
                var categoryButton = CreateCategoryButton(category);
                _categoryListElement.Add(categoryButton);
                _categoryButtons[category.CategoryType] = categoryButton;
            }
        }
        
        private Button CreateCategoryButton(SchematicCategory category)
        {
            var button = new Button();
            button.text = category.CategoryName;
            button.AddToClassList("category-button");
            
            // Styling
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.marginBottom = 4;
            
            // Click handler
            button.clicked += () => SelectCategory(category.CategoryType);
            
            // Hover effects
            button.RegisterCallback<MouseEnterEvent>(_ => {
                if (_selectedCategory != category.CategoryType)
                    button.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            });
            
            button.RegisterCallback<MouseLeaveEvent>(_ => {
                if (_selectedCategory != category.CategoryType)
                    button.style.backgroundColor = Color.clear;
            });
            
            return button;
        }
        
        private void UpdateCategorySelection()
        {
            foreach (var kvp in _categoryButtons)
            {
                var button = kvp.Value;
                var categoryType = kvp.Key;
                
                if (categoryType == _selectedCategory)
                {
                    button.style.backgroundColor = _selectedCategoryColor;
                    button.style.color = Color.white;
                }
                else
                {
                    button.style.backgroundColor = Color.clear;
                    button.style.color = Color.gray;
                }
            }
        }
        
        private void RefreshSchematicGrid()
        {
            if (_schematicGridElement == null || _marketService == null) return;
            
            _schematicGridElement.Clear();
            _displayedSchematics.Clear();
            
            var schematics = _marketService.GetAvailableSchematics(_selectedCategory);
            _displayedSchematics.AddRange(schematics);
            
            // Sort by unlock status, then by level, then by name
            _displayedSchematics.Sort((a, b) => {
                bool aUnlocked = _marketService.IsSchematicUnlocked(a.SchematicId);
                bool bUnlocked = _marketService.IsSchematicUnlocked(b.SchematicId);
                
                if (aUnlocked != bUnlocked)
                    return aUnlocked.CompareTo(bUnlocked); // Unlocked first
                
                if (a.UnlockLevel != b.UnlockLevel)
                    return a.UnlockLevel.CompareTo(b.UnlockLevel);
                
                return a.SchematicName.CompareTo(b.SchematicName);
            });
            
            foreach (var schematic in _displayedSchematics)
            {
                var schematicCard = CreateSchematicCard(schematic);
                _schematicGridElement.Add(schematicCard);
            }
            
            // Show empty state if no schematics
            if (_displayedSchematics.Count == 0)
            {
                var emptyLabel = new Label("No schematics available in this category")
                {
                    style = { 
                        color = Color.gray,
                        fontSize = 14,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        paddingTop = 20,
                        paddingBottom = 20
                    }
                };
                _schematicGridElement.Add(emptyLabel);
            }
        }
        
        private VisualElement CreateSchematicCard(ConstructionSchematicSO schematic)
        {
            var card = new VisualElement();
            card.AddToClassList("schematic-card");
            card.style.flexDirection = FlexDirection.Column;
            card.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Color.gray;
            card.style.borderBottomColor = Color.gray;
            card.style.borderLeftColor = Color.gray;
            card.style.borderRightColor = Color.gray;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.marginBottom = 8;
            card.style.minHeight = 100;
            
            // Icon (placeholder)
            var iconContainer = new VisualElement();
            iconContainer.style.height = 32;
            iconContainer.style.width = 32;
            iconContainer.style.backgroundColor = Color.gray;
            iconContainer.style.alignSelf = Align.Center;
            iconContainer.style.marginBottom = 8;
            
            // Title
            var titleLabel = new Label(schematic.SchematicName)
            {
                style = { 
                    fontSize = 14,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginBottom = 4
                }
            };
            
            // Cost and level info
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Row;
            infoContainer.style.justifyContent = Justify.SpaceBetween;
            
            float cost = _marketService.MarketConfiguration.CalculateSchematicPrice(schematic);
            var costLabel = new Label($"{cost:F0} SP")
            {
                style = { 
                    fontSize = 12,
                    color = _currencyManager != null && _currencyManager.HasSufficientSkillPoints(cost) 
                        ? _affordableColor : _unaffordableColor
                }
            };
            
            var levelLabel = new Label($"Lv.{schematic.UnlockLevel}")
            {
                style = { 
                    fontSize = 12,
                    color = Color.gray
                }
            };
            
            infoContainer.Add(costLabel);
            infoContainer.Add(levelLabel);
            
            // Status indicator
            var statusLabel = new Label(GetSchematicStatusText(schematic))
            {
                style = { 
                    fontSize = 10,
                    color = GetSchematicStatusColor(schematic),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = 4
                }
            };
            
            card.Add(iconContainer);
            card.Add(titleLabel);
            card.Add(infoContainer);
            card.Add(statusLabel);
            
            // Click handler
            card.RegisterCallback<ClickEvent>(_ => SelectSchematic(schematic));
            
            // Hover effects
            card.RegisterCallback<MouseEnterEvent>(_ => {
                card.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            });
            
            card.RegisterCallback<MouseLeaveEvent>(_ => {
                card.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            });
            
            return card;
        }
        
        private void UpdateSchematicDetails()
        {
            if (_selectedSchematic == null || _detailsElement == null)
            {
                if (_detailsElement != null)
                    _detailsElement.style.display = DisplayStyle.None;
                return;
            }
            
            _detailsElement.style.display = DisplayStyle.Flex;
            
            // Update schematic information
            if (_selectedTitleElement != null)
                _selectedTitleElement.text = _selectedSchematic.SchematicName;
            
            if (_selectedDescriptionElement != null)
                _selectedDescriptionElement.text = _selectedSchematic.SchematicDescription;
            
            if (_selectedLevelElement != null)
                _selectedLevelElement.text = $"Required Level: {_selectedSchematic.UnlockLevel}";
            
            // Update cost and purchase button
            float cost = _marketService?.MarketConfiguration.CalculateSchematicPrice(_selectedSchematic) ?? 0f;
            if (_selectedCostElement != null)
            {
                _selectedCostElement.text = $"Cost: {cost:F0} Skill Points";
                _selectedCostElement.style.color = _currencyManager != null && _currencyManager.HasSufficientSkillPoints(cost) 
                    ? _affordableColor : _unaffordableColor;
            }
            
            UpdatePurchaseButtonState();
        }
        
        private void UpdatePurchaseButtonState()
        {
            if (_purchaseButtonElement == null || _selectedSchematic == null) return;
            
            bool isUnlocked = _marketService?.IsSchematicUnlocked(_selectedSchematic.SchematicId) ?? false;
            bool canPurchase = _marketService?.CanPurchaseSchematic(_selectedSchematic) ?? false;
            float cost = _marketService?.MarketConfiguration.CalculateSchematicPrice(_selectedSchematic) ?? 0f;
            bool canAfford = _currencyManager?.HasSufficientSkillPoints(cost) ?? false;
            
            if (isUnlocked)
            {
                _purchaseButtonElement.text = "Already Unlocked";
                _purchaseButtonElement.SetEnabled(false);
                _purchaseButtonElement.style.backgroundColor = _lockedColor;
            }
            else if (!canPurchase)
            {
                _purchaseButtonElement.text = "Requirements Not Met";
                _purchaseButtonElement.SetEnabled(false);
                _purchaseButtonElement.style.backgroundColor = _lockedColor;
            }
            else if (!canAfford)
            {
                _purchaseButtonElement.text = "Insufficient Skill Points";
                _purchaseButtonElement.SetEnabled(false);
                _purchaseButtonElement.style.backgroundColor = _unaffordableColor;
            }
            else
            {
                _purchaseButtonElement.text = $"Purchase ({cost:F0} SP)";
                _purchaseButtonElement.SetEnabled(true);
                _purchaseButtonElement.style.backgroundColor = _affordableColor;
            }
        }
        
        private void RefreshSkillPointsDisplay()
        {
            if (_currencyManager == null) return;
            
            _currentSkillPoints = _currencyManager.GetSkillPointsBalance();
            
            if (_skillPointsLabelElement != null)
            {
                _skillPointsLabelElement.text = $"Skill Points: {_currentSkillPoints:F0}";
            }
        }
        
        private void RefreshSelectedSchematic()
        {
            if (_selectedSchematic != null)
            {
                UpdateSchematicDetails();
            }
        }
        
        private string GetSchematicStatusText(ConstructionSchematicSO schematic)
        {
            if (_marketService?.IsSchematicUnlocked(schematic.SchematicId) == true)
                return "UNLOCKED";
            
            if (_marketService?.CanPurchaseSchematic(schematic) != true)
                return "LOCKED";
            
            float cost = _marketService?.MarketConfiguration.CalculateSchematicPrice(schematic) ?? 0f;
            if (_currencyManager?.HasSufficientSkillPoints(cost) != true)
                return "NEED SP";
            
            return "AVAILABLE";
        }
        
        private Color GetSchematicStatusColor(ConstructionSchematicSO schematic)
        {
            if (_marketService?.IsSchematicUnlocked(schematic.SchematicId) == true)
                return _affordableColor;
            
            if (_marketService?.CanPurchaseSchematic(schematic) != true)
                return _lockedColor;
            
            float cost = _marketService?.MarketConfiguration.CalculateSchematicPrice(schematic) ?? 0f;
            if (_currencyManager?.HasSufficientSkillPoints(cost) != true)
                return _unaffordableColor;
            
            return _affordableColor;
        }
        
        private void ClearSelection()
        {
            _selectedSchematic = null;
            if (_detailsElement != null)
                _detailsElement.style.display = DisplayStyle.None;
        }
        
        private void OnSchematicPurchasedHandler(ConstructionSchematicSO schematic, float cost)
        {
            RefreshPanel();
            LogInfo($"Schematic purchased via event: {schematic.SchematicName}");
        }
        
        private void OnPurchaseErrorHandler(string errorMessage)
        {
            LogWarning($"Purchase error: {errorMessage}");
            // Could show error modal here
        }
        
        private void OnCurrencyChangedHandler(CurrencyType currencyType, float oldAmount, float newAmount)
        {
            if (currencyType == CurrencyType.SkillPoints)
            {
                RefreshSkillPointsDisplay();
                UpdatePurchaseButtonState();
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (_marketService != null)
            {
                _marketService.OnSchematicPurchased -= OnSchematicPurchasedHandler;
                _marketService.OnPurchaseError -= OnPurchaseErrorHandler;
            }
            
            if (_currencyManager != null)
            {
                _currencyManager.OnCurrencyChanged -= OnCurrencyChangedHandler;
            }
            
            base.OnDestroy();
        }
    }
}