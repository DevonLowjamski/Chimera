using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Systems.Marketplace;
using ProjectChimera.Data.Marketplace;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;

namespace ProjectChimera.UI.Marketplace
{
    /// <summary>
    /// Marketplace panel - main UI for buying/selling genetics and schematics.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Trade genetics and schematics with other players using Skill Points"
    ///
    /// **Player Actions**:
    /// - Browse genetics marketplace (verified strains)
    /// - Browse schematics marketplace (blueprints)
    /// - Filter by price, rating, type
    /// - View seller profiles and reputation
    /// - Purchase with Skill Points
    /// - List own items for sale
    ///
    /// **UI Layout**:
    /// - Tab navigation (Genetics | Schematics | My Listings | My Purchases)
    /// - Search and filters
    /// - Listing cards with key info
    /// - Seller ratings and verification badges
    /// - Skill point balance display
    /// </summary>
    public class MarketplacePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _closeButton;

        [Header("Tab Navigation")]
        [SerializeField] private Button _geneticsTab;
        [SerializeField] private Button _schematicsTab;
        [SerializeField] private Button _myListingsTab;
        [SerializeField] private Button _myPurchasesTab;

        [Header("Tab Content")]
        [SerializeField] private GameObject _geneticsContent;
        [SerializeField] private GameObject _schematicsContent;
        [SerializeField] private GameObject _myListingsContent;
        [SerializeField] private GameObject _myPurchasesContent;

        [Header("Listing Display")]
        [SerializeField] private Transform _listingContainer;
        [SerializeField] private GameObject _listingCardPrefab;

        [Header("Skill Points")]
        [SerializeField] private TextMeshProUGUI _skillPointsText;
        [SerializeField] private Image _skillPointsIcon;

        [Header("Search & Filters")]
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Dropdown _sortDropdown;
        [SerializeField] private Slider _maxPriceSlider;
        [SerializeField] private TextMeshProUGUI _maxPriceText;
        [SerializeField] private Toggle _verifiedOnlyToggle;

        // Services
        private GeneticsMarketplace _geneticsMarketplace;
        private SchematicsMarketplace _schematicsMarketplace;
        private MarketplaceTransactionManager _transactionManager;

        // State
        private MarketplaceListingType _currentTab = MarketplaceListingType.Genetics;
        private MarketplaceFilters _currentFilters = MarketplaceFilters.Default;
        private List<GameObject> _listingCards = new List<GameObject>();

        private void Start()
        {
            InitializePanel();
            SetupButtonListeners();
        }

        private void InitializePanel()
        {
            // Get services
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _geneticsMarketplace = container.Resolve<GeneticsMarketplace>();
                _schematicsMarketplace = container.Resolve<SchematicsMarketplace>();
                _transactionManager = container.Resolve<MarketplaceTransactionManager>();
            }

            // Subscribe to events
            if (_transactionManager != null)
            {
                _transactionManager.OnTransactionComplete += OnTransactionComplete;
                _transactionManager.OnSkillPointsSpent += OnSkillPointsChanged;
                _transactionManager.OnSkillPointsEarned += OnSkillPointsChanged;
            }

            // Hide by default
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            ChimeraLogger.Log("UI",
                "Marketplace panel initialized", this);
        }

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            if (_geneticsTab != null)
                _geneticsTab.onClick.AddListener(() => ShowTab(MarketplaceListingType.Genetics));

            if (_schematicsTab != null)
                _schematicsTab.onClick.AddListener(() => ShowTab(MarketplaceListingType.Schematic));

            if (_myListingsTab != null)
                _myListingsTab.onClick.AddListener(ShowMyListings);

            if (_myPurchasesTab != null)
                _myPurchasesTab.onClick.AddListener(ShowMyPurchases);

            if (_searchInput != null)
                _searchInput.onValueChanged.AddListener(OnSearchChanged);

            if (_sortDropdown != null)
                _sortDropdown.onValueChanged.AddListener(OnSortChanged);

            if (_maxPriceSlider != null)
                _maxPriceSlider.onValueChanged.AddListener(OnMaxPriceChanged);

            if (_verifiedOnlyToggle != null)
                _verifiedOnlyToggle.onValueChanged.AddListener(OnVerifiedToggleChanged);
        }

        /// <summary>
        /// Shows the marketplace panel.
        /// </summary>
        public void ShowPanel()
        {
            UpdateSkillPointsDisplay();
            ShowTab(MarketplaceListingType.Genetics);

            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            ChimeraLogger.Log("UI",
                "Marketplace panel opened", this);
        }

        /// <summary>
        /// Shows a specific marketplace tab.
        /// </summary>
        private void ShowTab(MarketplaceListingType type)
        {
            _currentTab = type;

            // Hide all content
            if (_geneticsContent != null)
                _geneticsContent.SetActive(false);
            if (_schematicsContent != null)
                _schematicsContent.SetActive(false);
            if (_myListingsContent != null)
                _myListingsContent.SetActive(false);
            if (_myPurchasesContent != null)
                _myPurchasesContent.SetActive(false);

            // Show selected content
            switch (type)
            {
                case MarketplaceListingType.Genetics:
                    if (_geneticsContent != null)
                        _geneticsContent.SetActive(true);
                    RefreshListings(MarketplaceListingType.Genetics);
                    break;

                case MarketplaceListingType.Schematic:
                    if (_schematicsContent != null)
                        _schematicsContent.SetActive(true);
                    RefreshListings(MarketplaceListingType.Schematic);
                    break;
            }
        }

        /// <summary>
        /// Shows user's active listings.
        /// </summary>
        private void ShowMyListings()
        {
            // Hide all content
            if (_geneticsContent != null)
                _geneticsContent.SetActive(false);
            if (_schematicsContent != null)
                _schematicsContent.SetActive(false);
            if (_myPurchasesContent != null)
                _myPurchasesContent.SetActive(false);

            if (_myListingsContent != null)
                _myListingsContent.SetActive(true);

            // TODO: Show user's listings
            RefreshMyListings();
        }

        /// <summary>
        /// Shows user's purchase history.
        /// </summary>
        private void ShowMyPurchases()
        {
            // Hide all content
            if (_geneticsContent != null)
                _geneticsContent.SetActive(false);
            if (_schematicsContent != null)
                _schematicsContent.SetActive(false);
            if (_myListingsContent != null)
                _myListingsContent.SetActive(false);

            if (_myPurchasesContent != null)
                _myPurchasesContent.SetActive(true);

            RefreshMyPurchases();
        }

        /// <summary>
        /// Refreshes marketplace listings based on filters.
        /// </summary>
        private void RefreshListings(MarketplaceListingType type)
        {
            // Clear existing cards
            foreach (var card in _listingCards)
            {
                if (card != null)
                    Destroy(card);
            }
            _listingCards.Clear();

            // Get filtered listings
            List<MarketplaceListing> listings;

            if (type == MarketplaceListingType.Genetics && _geneticsMarketplace != null)
            {
                listings = _geneticsMarketplace.SearchListings(_currentFilters);
            }
            else if (type == MarketplaceListingType.Schematic && _schematicsMarketplace != null)
            {
                listings = _schematicsMarketplace.SearchListings(_currentFilters);
            }
            else
            {
                listings = new List<MarketplaceListing>();
            }

            // Create cards
            foreach (var listing in listings)
            {
                CreateListingCard(listing);
            }
        }

        /// <summary>
        /// Refreshes user's listings.
        /// </summary>
        private void RefreshMyListings()
        {
            // Clear existing cards
            foreach (var card in _listingCards)
            {
                if (card != null)
                    Destroy(card);
            }
            _listingCards.Clear();

            // Get user's listings (placeholder - needs player ID)
            // TODO: Implement once player management is in place
        }

        /// <summary>
        /// Refreshes user's purchase history.
        /// </summary>
        private void RefreshMyPurchases()
        {
            // Clear existing cards
            foreach (var card in _listingCards)
            {
                if (card != null)
                    Destroy(card);
            }
            _listingCards.Clear();

            if (_transactionManager == null)
                return;

            // Get user's purchases (placeholder - needs player ID)
            var purchases = _transactionManager.GetUserPurchases("Player1");

            // Create transaction cards
            foreach (var transaction in purchases)
            {
                CreateTransactionCard(transaction);
            }
        }

        /// <summary>
        /// Creates a listing card UI element.
        /// </summary>
        private void CreateListingCard(MarketplaceListing listing)
        {
            if (_listingCardPrefab == null || _listingContainer == null)
                return;

            var cardObj = Instantiate(_listingCardPrefab, _listingContainer);
            var card = cardObj.GetComponent<MarketplaceListingCard>();

            if (card != null)
            {
                card.Setup(listing, _transactionManager, OnPurchaseClicked);
            }

            _listingCards.Add(cardObj);
        }

        /// <summary>
        /// Creates a transaction card UI element.
        /// </summary>
        private void CreateTransactionCard(MarketplaceTransaction transaction)
        {
            if (_listingCardPrefab == null || _listingContainer == null)
                return;

            var cardObj = Instantiate(_listingCardPrefab, _listingContainer);
            // TODO: Set up transaction card display

            _listingCards.Add(cardObj);
        }

        /// <summary>
        /// Handles purchase button click.
        /// </summary>
        private void OnPurchaseClicked(MarketplaceListing listing)
        {
            if (_transactionManager == null)
                return;

            bool success = _transactionManager.PurchaseListing(
                listing.ListingId,
                "Player1", // Placeholder
                item => {
                    // Item received callback
                    ChimeraLogger.Log("UI",
                        $"Purchased: {listing.ItemName}", this);
                }
            );

            if (success)
            {
                // Refresh display
                RefreshListings(_currentTab);
                UpdateSkillPointsDisplay();
            }
        }

        #region Filter Handlers

        private void OnSearchChanged(string searchQuery)
        {
            _currentFilters.SearchQuery = searchQuery;
            RefreshListings(_currentTab);
        }

        private void OnSortChanged(int sortIndex)
        {
            _currentFilters.SortOrder = (MarketplaceFilters.MarketplaceSortOrder)sortIndex;
            RefreshListings(_currentTab);
        }

        private void OnMaxPriceChanged(float maxPrice)
        {
            _currentFilters.MaxPrice = Mathf.RoundToInt(maxPrice);

            if (_maxPriceText != null)
                _maxPriceText.text = $"Max: {_currentFilters.MaxPrice} SP";

            RefreshListings(_currentTab);
        }

        private void OnVerifiedToggleChanged(bool verifiedOnly)
        {
            _currentFilters.VerifiedGeneticsOnly = verifiedOnly;
            RefreshListings(_currentTab);
        }

        #endregion

        #region Event Handlers

        private void OnTransactionComplete(MarketplaceTransaction transaction)
        {
            // Refresh display on any transaction
            RefreshListings(_currentTab);
            UpdateSkillPointsDisplay();
        }

        private void OnSkillPointsChanged(string userId, int amount)
        {
            UpdateSkillPointsDisplay();
        }

        #endregion

        /// <summary>
        /// Updates skill points display.
        /// </summary>
        private void UpdateSkillPointsDisplay()
        {
            if (_skillPointsText != null && _transactionManager != null)
            {
                int skillPoints = _transactionManager.GetPlayerSkillPoints();
                _skillPointsText.text = $"{skillPoints} SP";
            }
        }

        private void OnCloseClicked()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        public void Hide()
        {
            OnCloseClicked();
        }

        public bool IsVisible()
        {
            return _panelRoot != null && _panelRoot.activeSelf;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_transactionManager != null)
            {
                _transactionManager.OnTransactionComplete -= OnTransactionComplete;
                _transactionManager.OnSkillPointsSpent -= OnSkillPointsChanged;
                _transactionManager.OnSkillPointsEarned -= OnSkillPointsChanged;
            }

            // Clean up button listeners
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
