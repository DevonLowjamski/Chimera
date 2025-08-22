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
    /// Main contract management UI panel for Phase 8 MVP
    /// Provides comprehensive contract overview, management, and workflow controls
    /// </summary>
    public class ContractManagementPanel : UIPanel
    {
        [Header("Contract Management Configuration")]
        [SerializeField] private string _mainTabContainer = "tab-container";
        [SerializeField] private string _availableContractsTab = "available-tab";
        [SerializeField] private string _activeContractsTab = "active-tab";
        [SerializeField] private string _contractHistoryTab = "history-tab";
        [SerializeField] private string _contractStatsTab = "stats-tab";
        
        [Header("Content Containers")]
        [SerializeField] private string _contentContainer = "content-container";
        [SerializeField] private string _availableContractsContainer = "available-contracts";
        [SerializeField] private string _activeContractsContainer = "active-contracts";
        [SerializeField] private string _contractHistoryContainer = "contract-history";
        [SerializeField] private string _contractStatsContainer = "contract-stats";
        
        [Header("Contract Display")]
        [SerializeField] private string _contractCardTemplate = "contract-card";
        [SerializeField] private string _selectedContractPanel = "selected-contract-details";
        [SerializeField] private string _contractActionsPanel = "contract-actions";
        [SerializeField] private string _contractProgressPanel = "progress-panel";
        
        [Header("Action Buttons")]
        [SerializeField] private string _acceptContractButton = "accept-contract-btn";
        [SerializeField] private string _completeContractButton = "complete-contract-btn";
        [SerializeField] private string _cancelContractButton = "cancel-contract-btn";
        [SerializeField] private string _refreshContractsButton = "refresh-contracts-btn";
        [SerializeField] private string _contractFiltersButton = "filters-btn";
        
        [Header("Status Displays")]
        [SerializeField] private string _activeContractsCount = "active-count";
        [SerializeField] private string _totalEarningsDisplay = "total-earnings";
        [SerializeField] private string _completionRateDisplay = "completion-rate";
        [SerializeField] private string _averageQualityDisplay = "average-quality";
        
        [Header("Visual Configuration")]
        [SerializeField] private Color _availableContractColor = Color.green;
        [SerializeField] private Color _activeContractColor = Color.yellow;
        [SerializeField] private Color _completedContractColor = Color.blue;
        [SerializeField] private Color _expiredContractColor = Color.red;
        [SerializeField] private bool _enableContractAnimations = true;
        
        // Service references
        private ContractGenerationService _contractService;
        private ContractTrackingService _trackingService;
        private CurrencyManager _currencyManager;
        
        // UI state
        private ContractManagementTab _currentTab = ContractManagementTab.Available;
        private ActiveContractSO _selectedContract;
        private List<ActiveContractSO> _displayedContracts = new List<ActiveContractSO>();
        private ContractFilterSettings _filterSettings = new ContractFilterSettings();
        
        // UI elements
        private VisualElement _mainTabContainerElement;
        private VisualElement _contentContainerElement;
        private VisualElement _selectedContractPanelElement;
        private VisualElement _contractActionsPanelElement;
        private VisualElement _contractProgressPanelElement;
        
        // Tab buttons
        private Button _availableTabButton;
        private Button _activeTabButton;
        private Button _historyTabButton;
        private Button _statsTabButton;
        
        // Action buttons
        private Button _acceptButton;
        private Button _completeButton;
        private Button _cancelButton;
        private Button _refreshButton;
        private Button _filtersButton;
        
        // Status displays
        private Label _activeCountLabel;
        private Label _totalEarningsLabel;
        private Label _completionRateLabel;
        private Label _averageQualityLabel;
        
        // Events
        public System.Action<ActiveContractSO> OnContractSelected;
        public System.Action<ActiveContractSO> OnContractAccepted;
        public System.Action<ActiveContractSO> OnContractCompleted;
        public System.Action<ContractManagementTab> OnTabChanged;
        
        protected override void OnPanelInitialized()
        {
            InitializeServices();
            InitializeUIElements();
            SetupEventHandlers();
            InitializeTabSystem();
            RefreshContractDisplay();
            UpdateStatusDisplays();
            
            LogInfo("ContractManagementPanel initialized");
        }
        
        protected override void OnAfterShow()
        {
            RefreshContractDisplay();
            UpdateStatusDisplays();
            RefreshSelectedContract();
        }
        
        protected override void OnAfterHide()
        {
            _selectedContract = null;
            ClearSelection();
        }
        
        /// <summary>
        /// Switch to a specific contract management tab
        /// </summary>
        public void SwitchToTab(ContractManagementTab tab)
        {
            if (_currentTab == tab) return;
            
            _currentTab = tab;
            UpdateTabSelection();
            RefreshContractDisplay();
            OnTabChanged?.Invoke(tab);
            
            LogInfo($"Switched to contract tab: {tab}");
        }
        
        /// <summary>
        /// Select a specific contract for detailed view
        /// </summary>
        public void SelectContract(ActiveContractSO contract)
        {
            if (contract == null) return;
            
            _selectedContract = contract;
            UpdateSelectedContractDisplay();
            UpdateContractActions();
            OnContractSelected?.Invoke(contract);
            
            LogInfo($"Selected contract: {contract.ContractTitle}");
        }
        
        /// <summary>
        /// Accept the currently selected available contract
        /// </summary>
        public void AcceptSelectedContract()
        {
            if (_selectedContract == null || _contractService == null)
            {
                LogError("No contract selected or contract service unavailable");
                return;
            }
            
            bool success = _contractService.AcceptContract(_selectedContract);
            if (success)
            {
                OnContractAccepted?.Invoke(_selectedContract);
                RefreshContractDisplay();
                UpdateStatusDisplays();
                
                // Auto-switch to active contracts tab
                SwitchToTab(ContractManagementTab.Active);
                
                LogInfo($"Successfully accepted contract: {_selectedContract.ContractTitle}");
            }
            else
            {
                LogError($"Failed to accept contract: {_selectedContract.ContractTitle}");
            }
        }
        
        /// <summary>
        /// Complete the currently selected active contract
        /// </summary>
        public void CompleteSelectedContract()
        {
            if (_selectedContract == null || _trackingService == null)
            {
                LogError("No contract selected or tracking service unavailable");
                return;
            }
            
            // Get contract progress for completion validation
            var progress = _trackingService.GetContractProgress(_selectedContract.ContractId);
            if (progress == null)
            {
                LogError($"No progress found for contract: {_selectedContract.ContractId}");
                return;
            }
            
            // Attempt to create delivery
            bool success = _trackingService.CreateContractDelivery(_selectedContract.ContractId);
            if (success)
            {
                OnContractCompleted?.Invoke(_selectedContract);
                RefreshContractDisplay();
                UpdateStatusDisplays();
                
                LogInfo($"Contract ready for delivery: {_selectedContract.ContractTitle}");
            }
            else
            {
                LogError($"Failed to complete contract: {_selectedContract.ContractTitle}");
            }
        }
        
        /// <summary>
        /// Refresh the contract display for current tab
        /// </summary>
        public void RefreshContractDisplay()
        {
            if (_contentContainerElement == null) return;
            
            // Clear current display
            ClearContractDisplay();
            
            // Get contracts based on current tab
            var contracts = GetContractsForCurrentTab();
            
            // Apply filters
            contracts = ApplyFilters(contracts);
            
            // Update displayed contracts
            _displayedContracts.Clear();
            _displayedContracts.AddRange(contracts);
            
            // Create contract cards
            foreach (var contract in _displayedContracts)
            {
                var contractCard = CreateContractCard(contract);
                GetCurrentTabContainer().Add(contractCard);
            }
            
            // Show empty state if no contracts
            if (_displayedContracts.Count == 0)
            {
                ShowEmptyState();
            }
            
            LogInfo($"Contract display refreshed: {_displayedContracts.Count} contracts shown");
        }
        
        /// <summary>
        /// Update status displays with current contract data
        /// </summary>
        public void UpdateStatusDisplays()
        {
            if (_contractService == null) return;
            
            // Update active contracts count
            int activeCount = _contractService.ActiveContractsCount;
            if (_activeCountLabel != null)
            {
                _activeCountLabel.text = $"Active: {activeCount}/{_contractService.MaxActiveContracts}";
            }
            
            // Update total earnings (placeholder - would integrate with actual earnings tracking)
            if (_totalEarningsLabel != null && _currencyManager != null)
            {
                _totalEarningsLabel.text = $"Earnings: ${_currencyManager.Cash:F0}";
            }
            
            // Update completion rate (placeholder - would need completion statistics)
            if (_completionRateLabel != null)
            {
                _completionRateLabel.text = "Completion: 85%";
            }
            
            // Update average quality (placeholder - would need quality statistics)
            if (_averageQualityLabel != null)
            {
                _averageQualityLabel.text = "Avg Quality: 78%";
            }
        }
        
        /// <summary>
        /// Show contract filters panel
        /// </summary>
        public void ShowContractFilters()
        {
            // This would open a contract filter panel
            LogInfo("Contract filters requested");
        }
        
        /// <summary>
        /// Apply quality filter to contracts
        /// </summary>
        public void FilterByQuality(float minimumQuality)
        {
            _filterSettings.MinimumQuality = minimumQuality;
            RefreshContractDisplay();
        }
        
        /// <summary>
        /// Apply strain type filter to contracts
        /// </summary>
        public void FilterByStrain(StrainType strainType)
        {
            _filterSettings.StrainFilter = strainType;
            RefreshContractDisplay();
        }
        
        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearFilters()
        {
            _filterSettings = new ContractFilterSettings();
            RefreshContractDisplay();
        }
        
        private void InitializeServices()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _contractService = gameManager.GetManager<ContractGenerationService>();
                _trackingService = gameManager.GetManager<ContractTrackingService>();
                _currencyManager = gameManager.GetManager<CurrencyManager>();
            }
            
            if (_contractService == null)
                LogError("ContractGenerationService not found");
            if (_trackingService == null)
                LogError("ContractTrackingService not found");
            if (_currencyManager == null)
                LogError("CurrencyManager not found");
        }
        
        private void InitializeUIElements()
        {
            if (_rootElement == null) return;
            
            // Find main containers
            _mainTabContainerElement = _rootElement.Q<VisualElement>(_mainTabContainer);
            _contentContainerElement = _rootElement.Q<VisualElement>(_contentContainer);
            _selectedContractPanelElement = _rootElement.Q<VisualElement>(_selectedContractPanel);
            _contractActionsPanelElement = _rootElement.Q<VisualElement>(_contractActionsPanel);
            _contractProgressPanelElement = _rootElement.Q<VisualElement>(_contractProgressPanel);
            
            // Find tab buttons
            _availableTabButton = _rootElement.Q<Button>(_availableContractsTab);
            _activeTabButton = _rootElement.Q<Button>(_activeContractsTab);
            _historyTabButton = _rootElement.Q<Button>(_contractHistoryTab);
            _statsTabButton = _rootElement.Q<Button>(_contractStatsTab);
            
            // Find action buttons
            _acceptButton = _rootElement.Q<Button>(_acceptContractButton);
            _completeButton = _rootElement.Q<Button>(_completeContractButton);
            _cancelButton = _rootElement.Q<Button>(_cancelContractButton);
            _refreshButton = _rootElement.Q<Button>(_refreshContractsButton);
            _filtersButton = _rootElement.Q<Button>(_contractFiltersButton);
            
            // Find status displays
            _activeCountLabel = _rootElement.Q<Label>(_activeContractsCount);
            _totalEarningsLabel = _rootElement.Q<Label>(_totalEarningsDisplay);
            _completionRateLabel = _rootElement.Q<Label>(_completionRateDisplay);
            _averageQualityLabel = _rootElement.Q<Label>(_averageQualityDisplay);
            
            // Set initial visibility
            if (_selectedContractPanelElement != null)
                _selectedContractPanelElement.style.display = DisplayStyle.None;
        }
        
        private void SetupEventHandlers()
        {
            // Tab button handlers
            if (_availableTabButton != null)
                _availableTabButton.clicked += () => SwitchToTab(ContractManagementTab.Available);
            if (_activeTabButton != null)
                _activeTabButton.clicked += () => SwitchToTab(ContractManagementTab.Active);
            if (_historyTabButton != null)
                _historyTabButton.clicked += () => SwitchToTab(ContractManagementTab.History);
            if (_statsTabButton != null)
                _statsTabButton.clicked += () => SwitchToTab(ContractManagementTab.Statistics);
            
            // Action button handlers
            if (_acceptButton != null)
                _acceptButton.clicked += AcceptSelectedContract;
            if (_completeButton != null)
                _completeButton.clicked += CompleteSelectedContract;
            if (_cancelButton != null)
                _cancelButton.clicked += () => { /* Cancel contract logic */ };
            if (_refreshButton != null)
                _refreshButton.clicked += RefreshContractDisplay;
            if (_filtersButton != null)
                _filtersButton.clicked += ShowContractFilters;
            
            // Service event handlers
            if (_contractService != null)
            {
                _contractService.OnContractAccepted += OnContractAcceptedHandler;
                _contractService.OnContractCompleted += OnContractCompletedHandler;
                _contractService.OnContractExpired += OnContractExpiredHandler;
                _contractService.OnAvailableContractsChanged += OnAvailableContractsChangedHandler;
            }
            
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated += OnContractProgressUpdatedHandler;
                _trackingService.OnContractReadyForDelivery += OnContractReadyForDeliveryHandler;
            }
        }
        
        private void InitializeTabSystem()
        {
            UpdateTabSelection();
        }
        
        private void UpdateTabSelection()
        {
            // Update tab button states
            UpdateTabButtonState(_availableTabButton, _currentTab == ContractManagementTab.Available);
            UpdateTabButtonState(_activeTabButton, _currentTab == ContractManagementTab.Active);
            UpdateTabButtonState(_historyTabButton, _currentTab == ContractManagementTab.History);
            UpdateTabButtonState(_statsTabButton, _currentTab == ContractManagementTab.Statistics);
            
            // Show/hide appropriate content containers
            ShowTabContainer(_currentTab);
        }
        
        private void UpdateTabButtonState(Button button, bool isSelected)
        {
            if (button == null) return;
            
            if (isSelected)
            {
                button.AddToClassList("tab-selected");
                button.RemoveFromClassList("tab-unselected");
            }
            else
            {
                button.AddToClassList("tab-unselected");
                button.RemoveFromClassList("tab-selected");
            }
        }
        
        private void ShowTabContainer(ContractManagementTab tab)
        {
            // Hide all containers first
            HideAllTabContainers();
            
            // Show selected container
            var container = GetTabContainer(tab);
            if (container != null)
            {
                container.style.display = DisplayStyle.Flex;
            }
        }
        
        private void HideAllTabContainers()
        {
            var availableContainer = _rootElement.Q<VisualElement>(_availableContractsContainer);
            var activeContainer = _rootElement.Q<VisualElement>(_activeContractsContainer);
            var historyContainer = _rootElement.Q<VisualElement>(_contractHistoryContainer);
            var statsContainer = _rootElement.Q<VisualElement>(_contractStatsContainer);
            
            if (availableContainer != null) availableContainer.style.display = DisplayStyle.None;
            if (activeContainer != null) activeContainer.style.display = DisplayStyle.None;
            if (historyContainer != null) historyContainer.style.display = DisplayStyle.None;
            if (statsContainer != null) statsContainer.style.display = DisplayStyle.None;
        }
        
        private VisualElement GetTabContainer(ContractManagementTab tab)
        {
            return tab switch
            {
                ContractManagementTab.Available => _rootElement.Q<VisualElement>(_availableContractsContainer),
                ContractManagementTab.Active => _rootElement.Q<VisualElement>(_activeContractsContainer),
                ContractManagementTab.History => _rootElement.Q<VisualElement>(_contractHistoryContainer),
                ContractManagementTab.Statistics => _rootElement.Q<VisualElement>(_contractStatsContainer),
                _ => null
            };
        }
        
        private VisualElement GetCurrentTabContainer()
        {
            return GetTabContainer(_currentTab);
        }
        
        private List<ActiveContractSO> GetContractsForCurrentTab()
        {
            if (_contractService == null) return new List<ActiveContractSO>();
            
            return _currentTab switch
            {
                ContractManagementTab.Available => _contractService.AvailableContracts,
                ContractManagementTab.Active => _contractService.ActiveContracts,
                ContractManagementTab.History => new List<ActiveContractSO>(), // Would need completed contracts
                ContractManagementTab.Statistics => new List<ActiveContractSO>(), // Stats view doesn't show contracts
                _ => new List<ActiveContractSO>()
            };
        }
        
        private List<ActiveContractSO> ApplyFilters(List<ActiveContractSO> contracts)
        {
            var filtered = contracts.AsEnumerable();
            
            // Apply strain filter
            if (_filterSettings.StrainFilter.HasValue)
            {
                filtered = filtered.Where(c => c.RequiredStrainType == _filterSettings.StrainFilter.Value);
            }
            
            // Apply quality filter
            if (_filterSettings.MinimumQuality > 0)
            {
                filtered = filtered.Where(c => c.MinimumQuality >= _filterSettings.MinimumQuality);
            }
            
            // Apply value filter
            if (_filterSettings.MinimumValue > 0)
            {
                filtered = filtered.Where(c => c.ContractValue >= _filterSettings.MinimumValue);
            }
            
            return filtered.ToList();
        }
        
        private VisualElement CreateContractCard(ActiveContractSO contract)
        {
            var card = new VisualElement();
            card.AddToClassList("contract-card");
            card.style.flexDirection = FlexDirection.Column;
            card.style.backgroundColor = GetContractColor(contract);
            card.style.marginBottom = 8;
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
            
            // Contract header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            
            var titleLabel = new Label(contract.ContractTitle)
            {
                style = { 
                    fontSize = 16,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            var valueLabel = new Label($"${contract.ContractValue:F0}")
            {
                style = { 
                    fontSize = 14,
                    color = Color.green,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            header.Add(titleLabel);
            header.Add(valueLabel);
            
            // Contract details
            var details = new VisualElement();
            details.style.marginTop = 8;
            
            var strainLabel = new Label($"Strain: {contract.RequiredStrainType}")
            {
                style = { fontSize = 12, color = Color.white }
            };
            
            var quantityLabel = new Label($"Quantity: {contract.QuantityRequired:F1}kg")
            {
                style = { fontSize = 12, color = Color.white }
            };
            
            var qualityLabel = new Label($"Quality: {contract.MinimumQuality:P0}+")
            {
                style = { fontSize = 12, color = Color.white }
            };
            
            var deadlineLabel = new Label($"Deadline: {contract.GetDaysRemaining()} days")
            {
                style = { 
                    fontSize = 12, 
                    color = contract.GetDaysRemaining() <= 3 ? Color.red : Color.white 
                }
            };
            
            details.Add(strainLabel);
            details.Add(quantityLabel);
            details.Add(qualityLabel);
            details.Add(deadlineLabel);
            
            card.Add(header);
            card.Add(details);
            
            // Add progress bar for active contracts
            if (_currentTab == ContractManagementTab.Active && _trackingService != null)
            {
                var progress = _trackingService.GetContractProgress(contract.ContractId);
                if (progress != null)
                {
                    var progressBar = CreateProgressBar(progress.CompletionProgress);
                    progressBar.style.marginTop = 8;
                    card.Add(progressBar);
                }
            }
            
            // Click handler
            card.RegisterCallback<ClickEvent>(_ => SelectContract(contract));
            
            // Hover effects
            card.RegisterCallback<MouseEnterEvent>(_ => {
                card.style.backgroundColor = Color.Lerp(GetContractColor(contract), Color.white, 0.1f);
            });
            
            card.RegisterCallback<MouseLeaveEvent>(_ => {
                card.style.backgroundColor = GetContractColor(contract);
            });
            
            return card;
        }
        
        private VisualElement CreateProgressBar(float progress)
        {
            var progressContainer = new VisualElement();
            progressContainer.style.height = 4;
            progressContainer.style.backgroundColor = Color.gray;
            progressContainer.style.borderTopLeftRadius = 2;
            progressContainer.style.borderTopRightRadius = 2;
            progressContainer.style.borderBottomLeftRadius = 2;
            progressContainer.style.borderBottomRightRadius = 2;
            
            var progressFill = new VisualElement();
            progressFill.style.height = Length.Percent(100);
            progressFill.style.width = Length.Percent(progress * 100);
            progressFill.style.backgroundColor = Color.green;
            progressFill.style.borderTopLeftRadius = 2;
            progressFill.style.borderBottomLeftRadius = 2;
            
            progressContainer.Add(progressFill);
            return progressContainer;
        }
        
        private Color GetContractColor(ActiveContractSO contract)
        {
            return _currentTab switch
            {
                ContractManagementTab.Available => _availableContractColor,
                ContractManagementTab.Active => _activeContractColor,
                ContractManagementTab.History => _completedContractColor,
                _ => Color.gray
            };
        }
        
        private void ClearContractDisplay()
        {
            var container = GetCurrentTabContainer();
            if (container != null)
            {
                container.Clear();
            }
        }
        
        private void ShowEmptyState()
        {
            var container = GetCurrentTabContainer();
            if (container == null) return;
            
            string emptyMessage = _currentTab switch
            {
                ContractManagementTab.Available => "No contracts available",
                ContractManagementTab.Active => "No active contracts",
                ContractManagementTab.History => "No completed contracts",
                ContractManagementTab.Statistics => "Statistics view",
                _ => "No data available"
            };
            
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
            
            container.Add(emptyLabel);
        }
        
        private void UpdateSelectedContractDisplay()
        {
            if (_selectedContract == null || _selectedContractPanelElement == null)
            {
                if (_selectedContractPanelElement != null)
                    _selectedContractPanelElement.style.display = DisplayStyle.None;
                return;
            }
            
            _selectedContractPanelElement.style.display = DisplayStyle.Flex;
            
            // Update contract details in selected panel
            // This would populate detailed contract information
            LogInfo($"Updated selected contract display for: {_selectedContract.ContractTitle}");
        }
        
        private void UpdateContractActions()
        {
            if (_contractActionsPanelElement == null) return;
            
            // Update action button visibility based on contract state and current tab
            bool showAccept = _currentTab == ContractManagementTab.Available && _selectedContract != null;
            bool showComplete = _currentTab == ContractManagementTab.Active && _selectedContract != null;
            
            if (_acceptButton != null)
                _acceptButton.style.display = showAccept ? DisplayStyle.Flex : DisplayStyle.None;
            if (_completeButton != null)
                _completeButton.style.display = showComplete ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void ClearSelection()
        {
            _selectedContract = null;
            if (_selectedContractPanelElement != null)
                _selectedContractPanelElement.style.display = DisplayStyle.None;
        }
        
        private void RefreshSelectedContract()
        {
            if (_selectedContract != null)
            {
                UpdateSelectedContractDisplay();
                UpdateContractActions();
            }
        }
        
        // Event handlers
        private void OnContractAcceptedHandler(ActiveContractSO contract)
        {
            RefreshContractDisplay();
            UpdateStatusDisplays();
        }
        
        private void OnContractCompletedHandler(ActiveContractSO contract)
        {
            RefreshContractDisplay();
            UpdateStatusDisplays();
        }
        
        private void OnContractExpiredHandler(ActiveContractSO contract)
        {
            RefreshContractDisplay();
            UpdateStatusDisplays();
        }
        
        private void OnAvailableContractsChangedHandler(int count)
        {
            if (_currentTab == ContractManagementTab.Available)
            {
                RefreshContractDisplay();
            }
        }
        
        private void OnContractProgressUpdatedHandler(ActiveContractSO contract, float progress)
        {
            if (_currentTab == ContractManagementTab.Active)
            {
                RefreshContractDisplay();
            }
        }
        
        private void OnContractReadyForDeliveryHandler(ActiveContractSO contract, ContractCompletionResult result)
        {
            RefreshContractDisplay();
            UpdateStatusDisplays();
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (_contractService != null)
            {
                _contractService.OnContractAccepted -= OnContractAcceptedHandler;
                _contractService.OnContractCompleted -= OnContractCompletedHandler;
                _contractService.OnContractExpired -= OnContractExpiredHandler;
                _contractService.OnAvailableContractsChanged -= OnAvailableContractsChangedHandler;
            }
            
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated -= OnContractProgressUpdatedHandler;
                _trackingService.OnContractReadyForDelivery -= OnContractReadyForDeliveryHandler;
            }
            
            base.OnDestroy();
        }
    }
    
    /// <summary>
    /// Contract management tab types
    /// </summary>
    public enum ContractManagementTab
    {
        Available,
        Active,
        History,
        Statistics
    }
    
    /// <summary>
    /// Contract filter settings
    /// </summary>
    [System.Serializable]
    public class ContractFilterSettings
    {
        public StrainType? StrainFilter;
        public float MinimumQuality;
        public float MinimumValue;
        public int MaxDaysRemaining;
        public bool ShowOnlyAffordable;
    }
}