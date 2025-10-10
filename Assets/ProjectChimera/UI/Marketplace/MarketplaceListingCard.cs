using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Marketplace;
using ProjectChimera.Systems.Marketplace;
using System;

namespace ProjectChimera.UI.Marketplace
{
    /// <summary>
    /// Marketplace listing card - individual item display.
    ///
    /// GAMEPLAY PURPOSE:
    /// Shows key listing info in compact card format.
    ///
    /// **Card Layout**:
    /// - Item name and icon
    /// - Type indicator (Genetics/Schematic)
    /// - Price in Skill Points
    /// - Seller name and rating
    /// - Key attributes (THC% for genetics, Tier for schematics)
    /// - Verification badge (for blockchain verified genetics)
    /// - Buy button
    /// </summary>
    public class MarketplaceListingCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _sellerText;
        [SerializeField] private Image _sellerRatingStars;
        [SerializeField] private TextMeshProUGUI _attributesText;
        [SerializeField] private GameObject _verifiedBadge;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TextMeshProUGUI _buyButtonText;
        [SerializeField] private Button _detailsButton;

        [Header("Colors")]
        [SerializeField] private Color _geneticsColor = Color.green;
        [SerializeField] private Color _schematicColor = Color.cyan;
        [SerializeField] private Color _affordableColor = Color.white;
        [SerializeField] private Color _unaffordableColor = Color.red;

        // Listing data
        private MarketplaceListing _listing;
        private MarketplaceTransactionManager _transactionManager;
        private System.Action<MarketplaceListing> _onPurchaseClicked;

        /// <summary>
        /// Sets up card with listing data.
        /// </summary>
        public void Setup(MarketplaceListing listing, MarketplaceTransactionManager transactionManager,
            System.Action<MarketplaceListing> onPurchaseClicked)
        {
            _listing = listing;
            _transactionManager = transactionManager;
            _onPurchaseClicked = onPurchaseClicked;

            UpdateDisplay();
            SetupButtons();
        }

        /// <summary>
        /// Updates all display elements.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_listing == null)
                return;

            // Item name
            if (_itemNameText != null)
                _itemNameText.text = _listing.ItemName;

            // Item icon
            if (_itemIcon != null && _listing.ItemIcon != null)
            {
                _itemIcon.sprite = _listing.ItemIcon;
            }

            // Type
            if (_typeText != null)
            {
                _typeText.text = _listing.Type.ToString();
                _typeText.color = _listing.Type == MarketplaceListingType.Genetics ?
                    _geneticsColor : _schematicColor;
            }

            // Price
            if (_priceText != null)
            {
                _priceText.text = $"{_listing.PriceSkillPoints} SP";

                // Color based on affordability
                int playerSP = _transactionManager?.GetPlayerSkillPoints() ?? 0;
                _priceText.color = playerSP >= _listing.PriceSkillPoints ?
                    _affordableColor : _unaffordableColor;
            }

            // Seller
            if (_sellerText != null)
            {
                _sellerText.text = $"Seller: {_listing.SellerName}";
            }

            // Seller rating (as stars)
            if (_sellerRatingStars != null)
            {
                float rating = _listing.SellerReputation;
                _sellerRatingStars.fillAmount = rating / 5f; // 0-5 stars to 0-1 fill
            }

            // Attributes (type specific)
            if (_attributesText != null)
            {
                _attributesText.text = GetAttributesText();
            }

            // Verified badge (genetics only)
            if (_verifiedBadge != null)
            {
                bool showBadge = _listing.Type == MarketplaceListingType.Genetics &&
                                 _listing.GeneticsData.IsBlockchainVerified;
                _verifiedBadge.SetActive(showBadge);
            }

            // Buy button
            UpdateBuyButton();
        }

        /// <summary>
        /// Gets attributes text based on listing type.
        /// </summary>
        private string GetAttributesText()
        {
            switch (_listing.Type)
            {
                case MarketplaceListingType.Genetics:
                    return _listing.GeneticsData.GetSummary();

                case MarketplaceListingType.Schematic:
                    return _listing.SchematicData.GetSummary();

                default:
                    return "";
            }
        }

        /// <summary>
        /// Updates buy button state.
        /// </summary>
        private void UpdateBuyButton()
        {
            if (_buyButton == null || _buyButtonText == null)
                return;

            int playerSP = _transactionManager?.GetPlayerSkillPoints() ?? 0;
            bool canAfford = playerSP >= _listing.PriceSkillPoints;

            _buyButton.interactable = canAfford;
            _buyButtonText.text = canAfford ? "Buy" : "Insufficient SP";
            _buyButtonText.color = canAfford ? _affordableColor : _unaffordableColor;
        }

        /// <summary>
        /// Sets up button listeners.
        /// </summary>
        private void SetupButtons()
        {
            if (_buyButton != null)
                _buyButton.onClick.AddListener(OnBuyClicked);

            if (_detailsButton != null)
                _detailsButton.onClick.AddListener(OnDetailsClicked);
        }

        /// <summary>
        /// Handles buy button click.
        /// </summary>
        private void OnBuyClicked()
        {
            _onPurchaseClicked?.Invoke(_listing);
        }

        /// <summary>
        /// Shows detailed listing view.
        /// </summary>
        private void OnDetailsClicked()
        {
            // TODO: Open detailed view panel with full listing info
            ChimeraLogger.Log("MARKETPLACE", $"Showing details for {_listing.ItemName}", this);
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(OnBuyClicked);

            if (_detailsButton != null)
                _detailsButton.onClick.RemoveListener(OnDetailsClicked);
        }
    }
}
