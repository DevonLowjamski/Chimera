using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.Systems.Facilities;
using ProjectChimera.Data.Facilities;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// World map UI panel for facility selection and management.
    /// Allows players to view owned facilities, upgrade facilities, purchase new ones, and switch between them.
    /// Part of the Phase 5 facility ladder system for Project Chimera.
    /// </summary>
    public class FacilityWorldMapPanel : UIPanel
    {
        [Header("Facility UI Settings")]
        [SerializeField] private string _facilityListName = "facility-list";
        [SerializeField] private string _facilityDetailName = "facility-detail";
        [SerializeField] private string _upgradeButtonName = "upgrade-button";
        [SerializeField] private string _switchButtonName = "switch-button";
        [SerializeField] private string _purchaseButtonName = "purchase-button";
        [SerializeField] private string _sellButtonName = "sell-button";
        [SerializeField] private bool _autoRefreshOnShow = true;
        [SerializeField] private float _refreshInterval = 5f;

        // UI Elements
        private VisualElement _root;
        private ScrollView _facilityList;
        private VisualElement _facilityDetail;
        private Button _upgradeButton;
        private Button _switchButton;
        private Button _purchaseButton;
        private Button _sellButton;
        private Label _statusLabel;
        private Label _portfolioValueLabel;

        // Facility Management
        private FacilityManager _facilityManager;
        private FacilitySwitchInfo _selectedFacility;
        private List<FacilitySwitchInfo> _availableFacilities;

        // Refresh timing
        private float _lastRefreshTime;

        protected override void Start()
        {
            base.Start();
            Debug.Log("[FacilityWorldMapPanel] Initializing facility world map UI...");

            _root = UIDocument?.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[FacilityWorldMapPanel] Root visual element not found!");
                return;
            }

            // Ensure base class can control visibility/transitions
            _rootElement = _root;

            _facilityList = _root.Q<ScrollView>(_facilityListName);
            _facilityDetail = _root.Q<VisualElement>(_facilityDetailName);
            _upgradeButton = _root.Q<Button>(_upgradeButtonName);
            _switchButton = _root.Q<Button>(_switchButtonName);
            _purchaseButton = _root.Q<Button>(_purchaseButtonName);
            _sellButton = _root.Q<Button>(_sellButtonName);
            _statusLabel = _root.Q<Label>("status-label");
            _portfolioValueLabel = _root.Q<Label>("portfolio-value");

            _upgradeButton?.RegisterCallback<ClickEvent>(OnUpgradeButtonClicked);
            _switchButton?.RegisterCallback<ClickEvent>(OnSwitchButtonClicked);
            _purchaseButton?.RegisterCallback<ClickEvent>(OnPurchaseButtonClicked);
            _sellButton?.RegisterCallback<ClickEvent>(OnSellButtonClicked);

            _facilityManager = GameManager.Instance?.GetManager<FacilityManager>();
            if (_facilityManager == null)
            {
                Debug.LogError("[FacilityWorldMapPanel] FacilityManager not found!");
                return;
            }

            SubscribeToFacilityEvents();

            _isInitialized = true;
            Debug.Log("[FacilityWorldMapPanel] Facility world map UI initialized successfully");
        }

        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            if (_autoRefreshOnShow)
            {
                RefreshFacilityData();
            }
        }

        // Using base OnBeforeHide/OnAfterHide hooks as needed (no extra logic required here)

        private void OnDestroy()
        {
            UnsubscribeFromFacilityEvents();
        }

        private void Update()
        {
            if (!IsVisible) return;

            // Auto-refresh facility data periodically
            if (Time.time - _lastRefreshTime >= _refreshInterval)
            {
                RefreshFacilityData();
                _lastRefreshTime = Time.time;
            }
        }

        #region Facility Event Handling

        private void SubscribeToFacilityEvents()
        {
            if (_facilityManager == null) return;

            _facilityManager.SubscribeToFacilityEvents(
                onFacilityUpgraded: OnFacilityUpgraded,
                onFacilitySwitch: OnFacilitySwitch,
                onFacilityPurchased: OnFacilityPurchased,
                onFacilitySold: OnFacilitySold,
                onFacilityUpgradeAvailable: OnFacilityUpgradeAvailable,
                onFacilityValueUpdated: OnFacilityValueUpdated
            );
        }

        private void UnsubscribeFromFacilityEvents()
        {
            if (_facilityManager == null) return;

            _facilityManager.UnsubscribeFromFacilityEvents(
                onFacilityUpgraded: (facility) => OnFacilityUpgraded(),
                onFacilitySwitch: (from, to) => OnFacilitySwitch(),
                onFacilityPurchased: (facility) => OnFacilityPurchased(),
                onFacilitySold: (facilityId) => OnFacilitySold(),
                onFacilityUpgradeAvailable: (facilityId) => OnFacilityUpgradeAvailable(),
                onFacilityValueUpdated: (facility) => OnFacilityValueUpdated()
            );
        }

        private void OnFacilityUpgraded()
        {
            RefreshFacilityData();
            UpdateStatusMessage("Facility upgraded successfully!", StatusType.Success);
        }

        private void OnFacilitySwitch()
        {
            RefreshFacilityData();
            UpdateStatusMessage("Switched to new facility!", StatusType.Success);
        }

        private void OnFacilityPurchased()
        {
            RefreshFacilityData();
            UpdateStatusMessage("New facility purchased!", StatusType.Success);
        }

        private void OnFacilitySold()
        {
            RefreshFacilityData();
            UpdateStatusMessage("Facility sold successfully!", StatusType.Success);
        }

        private void OnFacilityUpgradeAvailable()
        {
            RefreshFacilityData();
            UpdateStatusMessage("New facility upgrades available!", StatusType.Info);
        }

        private void OnFacilityValueUpdated()
        {
            RefreshPortfolioValue();
        }

        #endregion

        #region UI Data Management

        /// <summary>
        /// Refreshes all facility data and updates the UI
        /// </summary>
        public void RefreshFacilityData()
        {
            if (_facilityManager == null) return;

            _availableFacilities = _facilityManager.GetAvailableFacilitiesForSwitching();
            PopulateFacilityList();
            RefreshPortfolioValue();
            RefreshSelectedFacilityDetail();
        }

        private void PopulateFacilityList()
        {
            if (_facilityList == null) return;

            _facilityList.Clear();

            foreach (var facilityInfo in _availableFacilities)
            {
                var facilityElement = CreateFacilityListElement(facilityInfo);
                _facilityList.Add(facilityElement);
            }

            Debug.Log($"[FacilityWorldMapPanel] Populated facility list with {_availableFacilities.Count} facilities");
        }

        private VisualElement CreateFacilityListElement(FacilitySwitchInfo facilityInfo)
        {
            var container = new VisualElement();
            container.AddToClassList("facility-item");

            // Facility name and tier
            var titleLabel = new Label($"{facilityInfo.OwnedFacilityData.FacilityName}");
            titleLabel.AddToClassList("facility-title");
            container.Add(titleLabel);

            var tierLabel = new Label($"Tier: {facilityInfo.Facility.TierName}");
            tierLabel.AddToClassList("facility-tier");
            container.Add(tierLabel);

            // Status
            var statusLabel = new Label(facilityInfo.StatusMessage);
            statusLabel.AddToClassList("facility-status");
            
            if (facilityInfo.IsCurrentFacility)
                statusLabel.AddToClassList("current-facility");
            else if (facilityInfo.CanSwitch)
                statusLabel.AddToClassList("available-facility");
            else
                statusLabel.AddToClassList("unavailable-facility");
                
            container.Add(statusLabel);

            // Performance info
            var performanceContainer = new VisualElement();
            performanceContainer.AddToClassList("facility-performance");

            var revenueLabel = new Label($"Revenue: ${facilityInfo.OwnedFacilityData.TotalRevenue:F0}");
            var plantsLabel = new Label($"Plants: {facilityInfo.OwnedFacilityData.TotalPlantsGrown}");
            var valueLabel = new Label($"Value: ${facilityInfo.OwnedFacilityData.CurrentValue:F0}");

            performanceContainer.Add(revenueLabel);
            performanceContainer.Add(plantsLabel);
            performanceContainer.Add(valueLabel);
            container.Add(performanceContainer);

            // Click handler
            container.RegisterCallback<ClickEvent>(evt => OnFacilitySelected(facilityInfo));

            return container;
        }

        private void RefreshPortfolioValue()
        {
            if (_facilityManager == null || _portfolioValueLabel == null) return;

            var totalValue = _facilityManager.GetTotalPortfolioValue();
            var totalInvestment = _facilityManager.GetTotalInvestment();
            var roi = _facilityManager.GetPortfolioROI();

            _portfolioValueLabel.text = $"Portfolio: ${totalValue:F0} | ROI: {roi:F1}% | Investment: ${totalInvestment:F0}";
        }

        private void RefreshSelectedFacilityDetail()
        {
            if (_selectedFacility.OwnedFacilityData.FacilityId == null || _facilityDetail == null) return;

            _facilityDetail.Clear();

            // Detailed facility information
            var facility = _selectedFacility.OwnedFacilityData;

            var nameLabel = new Label($"Facility: {facility.FacilityName}");
            nameLabel.AddToClassList("detail-title");
            _facilityDetail.Add(nameLabel);

            var tierLabel = new Label($"Tier: {facility.Tier.TierName}");
            var statusLabel = new Label($"Status: {_selectedFacility.StatusMessage}");
            var purchaseLabel = new Label($"Purchased: {facility.PurchaseDate:yyyy-MM-dd}");
            var valueLabel = new Label($"Current Value: ${facility.CurrentValue:F0}");
            var investmentLabel = new Label($"Total Investment: ${facility.TotalInvestment:F0}");

            _facilityDetail.Add(tierLabel);
            _facilityDetail.Add(statusLabel);
            _facilityDetail.Add(purchaseLabel);
            _facilityDetail.Add(valueLabel);
            _facilityDetail.Add(investmentLabel);

            // Performance metrics
            var performanceContainer = new VisualElement();
            performanceContainer.AddToClassList("performance-detail");

            var revenueLabel = new Label($"Total Revenue: ${facility.TotalRevenue:F0}");
            var plantsLabel = new Label($"Plants Grown: {facility.TotalPlantsGrown}");
            var maintenanceLabel = new Label($"Maintenance: {facility.MaintenanceLevel:P0}");

            performanceContainer.Add(revenueLabel);
            performanceContainer.Add(plantsLabel);
            performanceContainer.Add(maintenanceLabel);
            _facilityDetail.Add(performanceContainer);

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (_selectedFacility.OwnedFacilityData.FacilityId == null) return;

            var canUpgrade = _facilityManager.CanUpgradeToNextTier() && _selectedFacility.IsCurrentFacility;
            var canSwitch = _selectedFacility.CanSwitch && !_selectedFacility.IsCurrentFacility;
            var canSell = !_selectedFacility.IsCurrentFacility && _facilityManager.OwnedFacilitiesCount > 1;

            _upgradeButton?.SetEnabled(canUpgrade);
            _switchButton?.SetEnabled(canSwitch);
            _sellButton?.SetEnabled(canSell);
            _purchaseButton?.SetEnabled(true); // Always allow purchase attempts (validation happens in backend)
        }

        #endregion

        #region UI Event Handlers

        private void OnFacilitySelected(FacilitySwitchInfo facilityInfo)
        {
            _selectedFacility = facilityInfo;
            RefreshSelectedFacilityDetail();
            
            Debug.Log($"[FacilityWorldMapPanel] Selected facility: {facilityInfo.OwnedFacilityData.FacilityName}");
        }

        private async void OnUpgradeButtonClicked(ClickEvent evt)
        {
            if (_facilityManager == null || !_selectedFacility.IsCurrentFacility) return;

            var nextTier = _facilityManager.GetNextAvailableTier();
            if (nextTier == null)
            {
                UpdateStatusMessage("No upgrades available", StatusType.Warning);
                return;
            }

            UpdateStatusMessage("Upgrading facility...", StatusType.Info);
            
            var success = await _facilityManager.UpgradeToTierAsync(nextTier);
            if (!success)
            {
                UpdateStatusMessage("Facility upgrade failed", StatusType.Error);
            }
        }

        private async void OnSwitchButtonClicked(ClickEvent evt)
        {
            if (_facilityManager == null || _selectedFacility.OwnedFacilityData.FacilityId == null) return;

            UpdateStatusMessage("Switching facilities...", StatusType.Info);
            
            var result = await _facilityManager.SwitchToFacilityWithResultAsync(_selectedFacility.OwnedFacilityData.FacilityId);
            if (!result.Success)
            {
                UpdateStatusMessage(result.ErrorMessage, StatusType.Error);
            }
        }

        private async void OnPurchaseButtonClicked(ClickEvent evt)
        {
            // For now, purchase the next available tier
            // In a full implementation, this would open a purchase dialog
            if (_facilityManager == null) return;

            var nextTier = _facilityManager.GetNextAvailableTier();
            if (nextTier == null)
            {
                UpdateStatusMessage("No facilities available for purchase", StatusType.Warning);
                return;
            }

            UpdateStatusMessage("Purchasing facility...", StatusType.Info);
            
            var success = await _facilityManager.PurchaseNewFacilityAsync(nextTier);
            if (!success)
            {
                UpdateStatusMessage("Facility purchase failed", StatusType.Error);
            }
        }

        private void OnSellButtonClicked(ClickEvent evt)
        {
            if (_facilityManager == null || _selectedFacility.OwnedFacilityData.FacilityId == null) return;

            UpdateStatusMessage("Selling facility...", StatusType.Info);
            
            var success = _facilityManager.SellFacility(_selectedFacility.OwnedFacilityData.FacilityId);
            if (!success)
            {
                UpdateStatusMessage("Facility sale failed", StatusType.Error);
            }
        }

        #endregion

        #region Status Management

        private void UpdateStatusMessage(string message, StatusType statusType)
        {
            if (_statusLabel == null) return;

            _statusLabel.text = message;
            
            // Remove existing status classes
            _statusLabel.RemoveFromClassList("status-success");
            _statusLabel.RemoveFromClassList("status-warning");
            _statusLabel.RemoveFromClassList("status-error");
            _statusLabel.RemoveFromClassList("status-info");
            
            // Add appropriate status class
            switch (statusType)
            {
                case StatusType.Success:
                    _statusLabel.AddToClassList("status-success");
                    break;
                case StatusType.Warning:
                    _statusLabel.AddToClassList("status-warning");
                    break;
                case StatusType.Error:
                    _statusLabel.AddToClassList("status-error");
                    break;
                case StatusType.Info:
                    _statusLabel.AddToClassList("status-info");
                    break;
            }

            Debug.Log($"[FacilityWorldMapPanel] Status: {message} ({statusType})");
        }

        #endregion

        private enum StatusType
        {
            Info,
            Success,
            Warning,
            Error
        }
    }
}