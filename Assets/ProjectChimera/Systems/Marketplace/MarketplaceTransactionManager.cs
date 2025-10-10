using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Marketplace;

namespace ProjectChimera.Systems.Marketplace
{
    /// <summary>
    /// Marketplace transaction manager - coordinates all marketplace operations.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Skill Points are the universal currency - earn through gameplay, spend on progression or trading"
    ///
    /// **Transaction Flow**:
    /// 1. Buyer selects listing
    /// 2. System validates: buyer has SP, listing available, not own listing
    /// 3. Deduct SP from buyer
    /// 4. Transfer item (genetics/schematic) to buyer
    /// 5. Credit SP to seller (minus 5% fee)
    /// 6. Record transaction history
    /// 7. Update seller reputation
    ///
    /// **Skill Point Integration**:
    /// - Connects to SkillTreeManager for SP balance
    /// - Dual-use currency (progression + marketplace)
    /// - Player earns SP → spends on unlocks OR buys genetics/schematics
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Buy Blue Dream for 75 SP" → Click → Success!
    /// Behind scenes: Payment validation, item transfer, reputation updates, transaction logging.
    /// </summary>
    public class MarketplaceTransactionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GeneticsMarketplace _geneticsMarketplace;
        [SerializeField] private SchematicsMarketplace _schematicsMarketplace;

        // Services (resolved dynamically to avoid circular assembly dependency)
        private object _skillTreeManager;

        // Transaction history
        private List<MarketplaceTransaction> _allTransactions = new List<MarketplaceTransaction>();
        private Dictionary<string, List<MarketplaceTransaction>> _transactionsByUser = new Dictionary<string, List<MarketplaceTransaction>>();

        // Seller profiles
        private Dictionary<string, SellerProfile> _sellerProfiles = new Dictionary<string, SellerProfile>();

        // Current player ID (placeholder - should come from player manager)
        private string _currentPlayerId = "Player1";

        // Events
        public event Action<MarketplaceTransaction> OnTransactionComplete;
        public event Action<string, int> OnSkillPointsSpent;     // userId, amount
        public event Action<string, int> OnSkillPointsEarned;    // userId, amount
        public event Action<SellerProfile> OnReputationUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get services
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                // Resolve SkillTreeManager dynamically to avoid circular assembly dependency
                // (Systems assembly can't reference Systems.Progression assembly)
                var skillTreeManagerType = Type.GetType("ProjectChimera.Systems.Progression.SkillTreeManager, ProjectChimera.Systems.Progression");
                if (skillTreeManagerType != null)
                {
                    _skillTreeManager = container.Resolve(skillTreeManagerType);
                }

                // Get marketplace services if not assigned
                if (_geneticsMarketplace == null)
                    _geneticsMarketplace = container.Resolve<GeneticsMarketplace>();

                if (_schematicsMarketplace == null)
                    _schematicsMarketplace = container.Resolve<SchematicsMarketplace>();

                // Register self
                container.RegisterSingleton<MarketplaceTransactionManager>(this);
            }

            // Subscribe to marketplace events
            if (_geneticsMarketplace != null)
            {
                _geneticsMarketplace.OnListingSold += OnGeneticsListingSold;
            }

            if (_schematicsMarketplace != null)
            {
                _schematicsMarketplace.OnListingSold += OnSchematicListingSold;
            }

            ChimeraLogger.Log("MARKETPLACE",
                "Transaction manager initialized", this);
        }

        #region Purchasing

        /// <summary>
        /// Purchases a listing (genetics or schematic).
        ///
        /// GAMEPLAY:
        /// - Player clicks "Buy" on listing
        /// - System validates SP balance
        /// - Deducts SP from buyer
        /// - Transfers item to buyer
        /// - Credits seller (minus 5% fee)
        /// - Records transaction
        /// </summary>
        public bool PurchaseListing(string listingId, string buyerId,
            System.Action<object> onItemReceived = null)
        {
            // Try genetics marketplace first
            var geneticsListing = _geneticsMarketplace?.GetListing(listingId);
            if (geneticsListing != null)
            {
                return PurchaseGeneticsListing(geneticsListing, buyerId, onItemReceived);
            }

            // Try schematics marketplace
            var schematicListing = _schematicsMarketplace?.GetListing(listingId);
            if (schematicListing != null)
            {
                return PurchaseSchematicListing(schematicListing, buyerId, onItemReceived);
            }

            ChimeraLogger.LogWarning("MARKETPLACE",
                $"Listing {listingId} not found in any marketplace", this);
            return false;
        }

        /// <summary>
        /// Purchases a genetics listing.
        /// </summary>
        private bool PurchaseGeneticsListing(MarketplaceListing listing, string buyerId,
            System.Action<object> onItemReceived)
        {
            // Validate buyer can afford
            if (!CanAfford(buyerId, listing.PriceSkillPoints))
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Buyer {buyerId} cannot afford listing (needs {listing.PriceSkillPoints} SP)", this);
                return false;
            }

            // Deduct skill points from buyer
            if (!DeductSkillPoints(buyerId, listing.PriceSkillPoints))
                return false;

            // Process purchase through genetics marketplace
            bool success = _geneticsMarketplace.PurchaseListing(
                listing.ListingId, buyerId,
                genotype => onItemReceived?.Invoke(genotype)
            );

            if (!success)
            {
                // Refund on failure
                RefundSkillPoints(buyerId, listing.PriceSkillPoints);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Purchases a schematic listing.
        /// </summary>
        private bool PurchaseSchematicListing(MarketplaceListing listing, string buyerId,
            System.Action<object> onItemReceived)
        {
            // Validate buyer can afford
            if (!CanAfford(buyerId, listing.PriceSkillPoints))
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Buyer {buyerId} cannot afford listing (needs {listing.PriceSkillPoints} SP)", this);
                return false;
            }

            // Deduct skill points from buyer
            if (!DeductSkillPoints(buyerId, listing.PriceSkillPoints))
                return false;

            // Process purchase through schematics marketplace
            bool success = _schematicsMarketplace.PurchaseListing(
                listing.ListingId, buyerId,
                schematic => onItemReceived?.Invoke(schematic)
            );

            if (!success)
            {
                // Refund on failure
                RefundSkillPoints(buyerId, listing.PriceSkillPoints);
                return false;
            }

            return true;
        }

        #endregion

        #region Skill Point Management

        /// <summary>
        /// Checks if user can afford price using MarketplaceTransactionHelpers.
        /// </summary>
        private bool CanAfford(string userId, int priceSkillPoints)
        {
            if (_skillTreeManager == null)
                return false;

            // For current player, check actual skill points
            if (userId == _currentPlayerId)
            {
                int availablePoints = MarketplaceTransactionHelpers.GetAvailableSkillPoints(_skillTreeManager);
                return availablePoints >= priceSkillPoints;
            }

            // For other players, assume they can afford (server would validate)
            return true;
        }

        /// <summary>
        /// Deducts skill points from buyer using MarketplaceTransactionHelpers.
        /// </summary>
        private bool DeductSkillPoints(string userId, int amount)
        {
            if (_skillTreeManager == null)
                return false;

            // Only deduct from current player
            if (userId == _currentPlayerId)
            {
                bool success = MarketplaceTransactionHelpers.SpendSkillPoints(
                    _skillTreeManager, amount, "Marketplace purchase");

                if (success)
                {
                    OnSkillPointsSpent?.Invoke(userId, amount);
                }
                return success;
            }

            return true;
        }

        /// <summary>
        /// Refunds skill points to buyer (on failed transaction) using MarketplaceTransactionHelpers.
        /// </summary>
        private void RefundSkillPoints(string userId, int amount)
        {
            if (_skillTreeManager == null || userId != _currentPlayerId)
                return;

            MarketplaceTransactionHelpers.AwardSkillPoints(_skillTreeManager, amount, "Marketplace refund");
        }

        /// <summary>
        /// Credits skill points to seller using MarketplaceTransactionHelpers.
        /// </summary>
        private void CreditSeller(string sellerId, int amount)
        {
            if (_skillTreeManager == null || sellerId != _currentPlayerId)
                return;

            MarketplaceTransactionHelpers.AwardSkillPoints(_skillTreeManager, amount, "Marketplace sale");
            OnSkillPointsEarned?.Invoke(sellerId, amount);
        }

        #endregion

        #region Transaction Handling

        /// <summary>
        /// Handles genetics listing sold event.
        /// </summary>
        private void OnGeneticsListingSold(MarketplaceListing listing, string buyerId)
        {
            ProcessTransaction(listing, buyerId);
        }

        /// <summary>
        /// Handles schematic listing sold event.
        /// </summary>
        private void OnSchematicListingSold(MarketplaceListing listing, string buyerId)
        {
            ProcessTransaction(listing, buyerId);
        }

        /// <summary>
        /// Processes transaction (payment, recording, reputation).
        /// </summary>
        private void ProcessTransaction(MarketplaceListing listing, string buyerId)
        {
            // Calculate seller earnings (price minus commission)
            int commission = listing.Type == MarketplaceListingType.Genetics ?
                _geneticsMarketplace.CalculateCommission(listing.PriceSkillPoints) :
                _schematicsMarketplace.CalculateCommission(listing.PriceSkillPoints);

            int sellerEarnings = listing.PriceSkillPoints - commission;

            // Credit seller
            CreditSeller(listing.SellerId, sellerEarnings);

            // Create transaction record
            var transaction = MarketplaceTransaction.Create(listing, buyerId);
            _allTransactions.Add(transaction);

            // Track by user
            if (!_transactionsByUser.ContainsKey(buyerId))
                _transactionsByUser[buyerId] = new List<MarketplaceTransaction>();
            _transactionsByUser[buyerId].Add(transaction);

            if (!_transactionsByUser.ContainsKey(listing.SellerId))
                _transactionsByUser[listing.SellerId] = new List<MarketplaceTransaction>();
            _transactionsByUser[listing.SellerId].Add(transaction);

            // Update seller profile
            UpdateSellerProfile(listing.SellerId, listing);

            OnTransactionComplete?.Invoke(transaction);

            ChimeraLogger.Log("MARKETPLACE",
                $"Transaction complete: {listing.ItemName} sold to {buyerId} for {listing.PriceSkillPoints} SP " +
                $"(Seller earned: {sellerEarnings} SP)", this);
        }

        #endregion

        #region Seller Profiles

        /// <summary>
        /// Updates seller profile after sale.
        /// </summary>
        private void UpdateSellerProfile(string sellerId, MarketplaceListing listing)
        {
            int commission = listing.Type == MarketplaceListingType.Genetics ?
                _geneticsMarketplace.CalculateCommission(listing.PriceSkillPoints) :
                _schematicsMarketplace.CalculateCommission(listing.PriceSkillPoints);

            MarketplaceTransactionHelpers.CreateOrUpdateSellerProfile(
                _sellerProfiles, _allTransactions, sellerId, listing, commission);
        }

        /// <summary>
        /// Submits buyer rating for transaction.
        /// </summary>
        public void RateTransaction(string transactionId, float rating, string review = "")
        {
            var transaction = _allTransactions.Find(t => t.TransactionId == transactionId);
            if (transaction == null)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Transaction {transactionId} not found", this);
                return;
            }

            if (transaction.BuyerRated)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    "Transaction already rated", this);
                return;
            }

            // Record rating
            transaction.BuyerRated = true;
            transaction.BuyerRating = Mathf.Clamp(rating, 1f, 5f);
            transaction.BuyerReview = review;

            // Update seller reputation
            if (_sellerProfiles.TryGetValue(transaction.SellerId, out var profile))
            {
                profile.AddRating(transaction.BuyerRating);
                OnReputationUpdated?.Invoke(profile);

                ChimeraLogger.Log("MARKETPLACE",
                    $"Seller {transaction.SellerId} rated {transaction.BuyerRating}/5 stars " +
                    $"(New avg: {profile.AverageRating:F1})", this);
            }
        }

        /// <summary>
        /// Gets seller profile.
        /// </summary>
        public SellerProfile GetSellerProfile(string sellerId)
        {
            return _sellerProfiles.TryGetValue(sellerId, out var profile) ? profile : null;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets transaction history for user.
        /// </summary>
        public List<MarketplaceTransaction> GetUserTransactions(string userId)
        {
            return _transactionsByUser.TryGetValue(userId, out var transactions) ?
                transactions : new List<MarketplaceTransaction>();
        }

        /// <summary>
        /// Gets purchases made by user.
        /// </summary>
        public List<MarketplaceTransaction> GetUserPurchases(string userId)
        {
            return GetUserTransactions(userId)
                .Where(t => t.BuyerId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        /// <summary>
        /// Gets sales made by user.
        /// </summary>
        public List<MarketplaceTransaction> GetUserSales(string userId)
        {
            return GetUserTransactions(userId)
                .Where(t => t.SellerId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        /// <summary>
        /// Gets marketplace analytics.
        /// </summary>
        public MarketplaceAnalytics GetMarketplaceAnalytics()
        {
            (float avgPrice, string mostTraded, int totalListings) geneticsAnalytics =
                _geneticsMarketplace?.GetMarketAnalytics() ?? (0f, "None", 0);
            (float avgPrice, string mostTraded, int totalListings) schematicsAnalytics =
                _schematicsMarketplace?.GetMarketAnalytics() ?? (0f, "None", 0);

            var last24Hours = _allTransactions
                .Where(t => (DateTime.Now - t.TransactionDate).TotalHours <= 24)
                .ToList();

            return new MarketplaceAnalytics
            {
                ActiveListings = geneticsAnalytics.totalListings + schematicsAnalytics.totalListings,
                TotalTransactions = _allTransactions.Count,
                TotalSkillPointsTraded = _allTransactions.Sum(t => t.PriceSkillPoints),
                AverageGeneticsPrice = geneticsAnalytics.avgPrice,
                AverageSchematicPrice = schematicsAnalytics.avgPrice,
                MostTradedGenetics = geneticsAnalytics.mostTraded,
                MostTradedSchematic = schematicsAnalytics.mostTraded,
                TransactionsLast24Hours = last24Hours.Count,
                NewListingsLast24Hours = 0 // Would need listing creation tracking
            };
        }

        #endregion

        /// <summary>
        /// Gets current player's skill point balance using MarketplaceTransactionHelpers.
        /// </summary>
        public int GetPlayerSkillPoints()
        {
            return MarketplaceTransactionHelpers.GetAvailableSkillPoints(_skillTreeManager);
        }

        /// <summary>
        /// Sets current player ID.
        /// </summary>
        public void SetCurrentPlayer(string playerId)
        {
            _currentPlayerId = playerId;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_geneticsMarketplace != null)
            {
                _geneticsMarketplace.OnListingSold -= OnGeneticsListingSold;
            }

            if (_schematicsMarketplace != null)
            {
                _schematicsMarketplace.OnListingSold -= OnSchematicListingSold;
            }
        }
    }
}
