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
    /// UI panel for contract delivery with order details and delivery confirmation for Phase 8 MVP
    /// Displays contract requirements, current progress, and delivery options
    /// </summary>
    public class ContractDeliveryPanel : UIPanel
    {
        [Header("Contract Delivery Configuration")]
        [SerializeField] private string _contractListContainer = "contract-list";
        [SerializeField] private string _contractDetailsContainer = "contract-details";
        [SerializeField] private string _deliveryButtonContainer = "delivery-actions";
        [SerializeField] private string _progressBarContainer = "progress-container";
        
        [Header("UI Element Names")]
        [SerializeField] private string _contractTitleLabel = "contract-title";
        [SerializeField] private string _contractorLabel = "contractor-name";
        [SerializeField] private string _quantityLabel = "quantity-required";
        [SerializeField] private string _qualityLabel = "quality-required";
        [SerializeField] private string _deadlineLabel = "deadline";
        [SerializeField] private string _progressBar = "progress-bar";
        [SerializeField] private string _progressLabel = "progress-text";
        [SerializeField] private string _deliveryButton = "deliver-button";
        [SerializeField] private string _cancelButton = "cancel-button";
        
        [Header("Progress Visualization")]
        [SerializeField] private Color _progressColor = Color.green;
        [SerializeField] private Color _incompleteColor = Color.red;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private bool _enableProgressAnimations = true;
        
        // Service references
        private ContractGenerationService _contractService;
        private ContractTrackingService _trackingService;
        private CurrencyManager _currencyManager;
        
        // UI state
        private List<ActiveContractSO> _activeContracts = new List<ActiveContractSO>();
        private ActiveContractSO _selectedContract;
        private ContractProgress _selectedProgress;
        
        // UI elements
        private VisualElement _contractListElement;
        private VisualElement _contractDetailsElement;
        private VisualElement _deliveryButtonElement;
        private VisualElement _progressBarElement;
        private Label _contractTitleElement;
        private Label _contractorElement;
        private Label _quantityElement;
        private Label _qualityElement;
        private Label _deadlineElement;
        private Label _progressElement;
        private Button _deliverButtonElement;
        private Button _cancelButtonElement;
        private ProgressBar _progressBarControl;
        
        // Panel ID is set via inspector _panelId field in base class
        
        // Events
        public System.Action<ActiveContractSO> OnContractSelected;
        public System.Action<ActiveContractSO> OnDeliveryRequested;
        public System.Action OnPanelClosed;
        
        protected override void OnPanelInitialized()
        {
            InitializeServices();
            InitializeUIElements();
            SetupEventHandlers();
            RefreshContractList();
            
            LogInfo("ContractDeliveryPanel initialized");
        }
        
        protected override void OnAfterShow()
        {
            RefreshContractList();
            RefreshSelectedContract();
        }
        
        protected override void OnAfterHide()
        {
            _selectedContract = null;
            _selectedProgress = null;
        }
        
        /// <summary>
        /// Refresh the list of active contracts
        /// </summary>
        public void RefreshContractList()
        {
            if (_contractService == null) return;
            
            _activeContracts = _contractService.ActiveContracts;
            UpdateContractListUI();
        }
        
        /// <summary>
        /// Select a specific contract for detailed view
        /// </summary>
        public void SelectContract(ActiveContractSO contract)
        {
            if (contract == null) return;
            
            _selectedContract = contract;
            _selectedProgress = _trackingService?.GetContractProgress(contract.ContractId);
            
            UpdateContractDetailsUI();
            OnContractSelected?.Invoke(contract);
            
            LogInfo($"Selected contract: {contract.ContractTitle}");
        }
        
        /// <summary>
        /// Request delivery for the selected contract
        /// </summary>
        public void RequestDelivery()
        {
            if (_selectedContract == null || _trackingService == null)
            {
                LogError("No contract selected or tracking service unavailable");
                return;
            }
            
            // Validate contract is ready for delivery
            var validation = _trackingService.ValidateContractCompletion(_selectedContract.ContractId);
            if (!validation.IsValid)
            {
                ShowDeliveryError(validation.Reason);
                return;
            }
            
            // Create delivery
            bool success = _trackingService.CreateContractDelivery(_selectedContract.ContractId);
            if (success)
            {
                OnDeliveryRequested?.Invoke(_selectedContract);
                ShowDeliverySuccess();
                RefreshContractList();
                ClearSelection();
            }
            else
            {
                ShowDeliveryError("Failed to create delivery");
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
        }
        
        private void InitializeUIElements()
        {
            if (_rootElement == null) return;
            
            // Find main containers
            _contractListElement = _rootElement.Q<VisualElement>(_contractListContainer);
            _contractDetailsElement = _rootElement.Q<VisualElement>(_contractDetailsContainer);
            _deliveryButtonElement = _rootElement.Q<VisualElement>(_deliveryButtonContainer);
            _progressBarElement = _rootElement.Q<VisualElement>(_progressBarContainer);
            
            // Find detail elements
            _contractTitleElement = _rootElement.Q<Label>(_contractTitleLabel);
            _contractorElement = _rootElement.Q<Label>(_contractorLabel);
            _quantityElement = _rootElement.Q<Label>(_quantityLabel);
            _qualityElement = _rootElement.Q<Label>(_qualityLabel);
            _deadlineElement = _rootElement.Q<Label>(_deadlineLabel);
            _progressElement = _rootElement.Q<Label>(_progressLabel);
            
            // Find interactive elements
            _deliverButtonElement = _rootElement.Q<Button>(_deliveryButton);
            _cancelButtonElement = _rootElement.Q<Button>(_cancelButton);
            _progressBarControl = _rootElement.Q<ProgressBar>(_progressBar);
            
            // Set initial visibility
            if (_contractDetailsElement != null)
                _contractDetailsElement.style.display = DisplayStyle.None;
        }
        
        private void SetupEventHandlers()
        {
            // Button handlers
            if (_deliverButtonElement != null)
                _deliverButtonElement.clicked += RequestDelivery;
            
            if (_cancelButtonElement != null)
                _cancelButtonElement.clicked += ClosePanel;
            
            // Service event handlers
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated += OnContractProgressUpdated;
                _trackingService.OnContractReadyForDelivery += OnContractReadyForDelivery;
            }
        }
        
        private void UpdateContractListUI()
        {
            if (_contractListElement == null) return;
            
            // Clear existing list
            _contractListElement.Clear();
            
            // Create contract list items
            foreach (var contract in _activeContracts)
            {
                var contractItem = CreateContractListItem(contract);
                _contractListElement.Add(contractItem);
            }
            
            // Show empty state if no contracts
            if (_activeContracts.Count == 0)
            {
                var emptyLabel = new Label("No active contracts")
                {
                    style = { 
                        color = Color.gray,
                        fontSize = 14,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                _contractListElement.Add(emptyLabel);
            }
        }
        
        private VisualElement CreateContractListItem(ActiveContractSO contract)
        {
            var item = new VisualElement();
            item.AddToClassList("contract-list-item");
            item.style.flexDirection = FlexDirection.Row;
            item.style.justifyContent = Justify.SpaceBetween;
            item.style.alignItems = Align.Center;
            item.style.paddingBottom = 8;
            item.style.paddingTop = 8;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = Color.gray;
            
            // Contract title and contractor
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Column;
            
            var titleLabel = new Label(contract.ContractTitle)
            {
                style = { 
                    fontSize = 14,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            var contractorLabel = new Label($"Contractor: {contract.ContractorName}")
            {
                style = { 
                    fontSize = 12,
                    color = Color.gray
                }
            };
            
            infoContainer.Add(titleLabel);
            infoContainer.Add(contractorLabel);
            
            // Progress indicator
            var progress = _trackingService?.GetContractProgress(contract.ContractId);
            var progressContainer = new VisualElement();
            progressContainer.style.flexDirection = FlexDirection.Column;
            progressContainer.style.alignItems = Align.FlexEnd;
            
            var progressLabel = new Label(progress != null ? $"{progress.CompletionProgress:P0}" : "0%")
            {
                style = { 
                    fontSize = 12,
                    color = GetProgressColor(progress?.CompletionProgress ?? 0f)
                }
            };
            
            var statusLabel = new Label(GetContractStatus(contract, progress))
            {
                style = { 
                    fontSize = 10,
                    color = Color.gray
                }
            };
            
            progressContainer.Add(progressLabel);
            progressContainer.Add(statusLabel);
            
            item.Add(infoContainer);
            item.Add(progressContainer);
            
            // Click handler
            item.RegisterCallback<ClickEvent>(_ => SelectContract(contract));
            
            // Hover effects
            item.RegisterCallback<MouseEnterEvent>(_ => {
                item.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            });
            
            item.RegisterCallback<MouseLeaveEvent>(_ => {
                item.style.backgroundColor = Color.clear;
            });
            
            return item;
        }
        
        private void UpdateContractDetailsUI()
        {
            if (_selectedContract == null || _contractDetailsElement == null)
            {
                if (_contractDetailsElement != null)
                    _contractDetailsElement.style.display = DisplayStyle.None;
                return;
            }
            
            _contractDetailsElement.style.display = DisplayStyle.Flex;
            
            // Update contract information
            if (_contractTitleElement != null)
                _contractTitleElement.text = _selectedContract.ContractTitle;
            
            if (_contractorElement != null)
                _contractorElement.text = $"Contractor: {_selectedContract.ContractorName}";
            
            if (_quantityElement != null)
                _quantityElement.text = $"Quantity: {_selectedContract.QuantityRequired:F1} kg";
            
            if (_qualityElement != null)
                _qualityElement.text = $"Quality: {_selectedContract.MinimumQuality:P0} minimum";
            
            if (_deadlineElement != null)
            {
                int daysRemaining = _selectedContract.GetDaysRemaining();
                _deadlineElement.text = $"Deadline: {daysRemaining} days remaining";
                _deadlineElement.style.color = daysRemaining <= 7 ? _warningColor : Color.white;
            }
            
            // Update progress
            UpdateProgressUI();
            
            // Update delivery button state
            UpdateDeliveryButtonState();
        }
        
        private void UpdateProgressUI()
        {
            if (_selectedProgress == null) return;
            
            // Update progress bar
            if (_progressBarControl != null)
            {
                _progressBarControl.value = _selectedProgress.CompletionProgress * 100f;
                _progressBarControl.style.color = GetProgressColor(_selectedProgress.CompletionProgress);
            }
            
            // Update progress text
            if (_progressElement != null)
            {
                string progressText = $"Progress: {_selectedProgress.CurrentQuantity:F1}/{_selectedContract.QuantityRequired:F1} kg " +
                                    $"({_selectedProgress.CompletionProgress:P0}) - Quality: {_selectedProgress.AverageQuality:P0}";
                _progressElement.text = progressText;
                _progressElement.style.color = GetProgressColor(_selectedProgress.CompletionProgress);
            }
        }
        
        private void UpdateDeliveryButtonState()
        {
            if (_deliverButtonElement == null) return;
            
            bool canDeliver = _selectedProgress != null && _selectedProgress.IsReadyForDelivery;
            
            _deliverButtonElement.SetEnabled(canDeliver);
            _deliverButtonElement.text = canDeliver ? "Deliver Contract" : "Not Ready";
            
            if (canDeliver)
            {
                _deliverButtonElement.style.backgroundColor = _progressColor;
                _deliverButtonElement.style.color = Color.white;
            }
            else
            {
                _deliverButtonElement.style.backgroundColor = Color.gray;
                _deliverButtonElement.style.color = Color.black;
            }
        }
        
        private void OnContractProgressUpdated(ActiveContractSO contract, float progress)
        {
            if (contract == _selectedContract)
            {
                _selectedProgress = _trackingService?.GetContractProgress(contract.ContractId);
                UpdateProgressUI();
                UpdateDeliveryButtonState();
            }
            
            // Update list item as well
            UpdateContractListUI();
        }
        
        private void OnContractReadyForDelivery(ActiveContractSO contract, ContractCompletionResult result)
        {
            if (contract == _selectedContract)
            {
                UpdateDeliveryButtonState();
                
                // Show notification
                LogInfo($"Contract ready for delivery: {contract.ContractTitle} - Estimated payout: ${result.TotalPayout:F0}");
            }
        }
        
        private Color GetProgressColor(float progress)
        {
            if (progress >= 1f) return _progressColor;
            if (progress >= 0.8f) return _warningColor;
            return _incompleteColor;
        }
        
        private string GetContractStatus(ActiveContractSO contract, ContractProgress progress)
        {
            if (progress == null) return "No Progress";
            if (progress.IsReadyForDelivery) return "Ready";
            if (contract.IsExpired()) return "Expired";
            if (progress.CompletionProgress >= 0.8f) return "Near Complete";
            if (progress.CompletionProgress > 0f) return "In Progress";
            return "Pending";
        }
        
        private void ShowDeliverySuccess()
        {
            LogInfo("Delivery created successfully!");
            // Could show a toast notification here
        }
        
        private void ShowDeliveryError(string message)
        {
            LogError($"Delivery failed: {message}");
            // Could show an error modal here
        }
        
        private void ClearSelection()
        {
            _selectedContract = null;
            _selectedProgress = null;
            if (_contractDetailsElement != null)
                _contractDetailsElement.style.display = DisplayStyle.None;
        }
        
        private void RefreshSelectedContract()
        {
            if (_selectedContract != null)
            {
                _selectedProgress = _trackingService?.GetContractProgress(_selectedContract.ContractId);
                UpdateContractDetailsUI();
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (_trackingService != null)
            {
                _trackingService.OnContractProgressUpdated -= OnContractProgressUpdated;
                _trackingService.OnContractReadyForDelivery -= OnContractReadyForDelivery;
            }
            
            base.OnDestroy();
        }
    }
}