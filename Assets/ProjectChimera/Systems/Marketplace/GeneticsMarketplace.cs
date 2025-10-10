using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Marketplace;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Systems.Genetics.Blockchain;

namespace ProjectChimera.Systems.Marketplace
{
    /// <summary>
    /// Genetics marketplace - player-to-player strain trading with blockchain verification.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Trade verified genetics with other players using Skill Points as currency"
    ///
    /// **Genetics Trading**:
    /// - Sell your best strains (set your own price in SP)
    /// - Buy rare genetics from other players
    /// - Blockchain verification prevents fraud
    /// - Reputation system builds trust
    ///
    /// **Player Experience**:
    /// - Breed amazing strain → List for sale
    /// - Set price (e.g., "30% THC verified genetics = 100 SP")
    /// - Other players browse/buy
    /// - Earn skill points → spend on progression or more genetics
    ///
    /// **Trust System**:
    /// - Blockchain hash = proof of authenticity
    /// - Seller ratings (1-5 stars from buyers)
    /// - Transaction history (transparency)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Blue Dream - 28% THC ✅ Verified - 75 SP"
    /// Behind scenes: Blockchain validation, reputation algorithms, market analytics.
    /// </summary>
    public class GeneticsMarketplace : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _commissionRate = 0.05f;      // 5% marketplace fee
        [SerializeField] private int _maxListingDuration = 30;       // 30 days max
        [SerializeField] private int _minListingPrice = 10;          // 10 SP minimum
        [SerializeField] private int _maxListingPrice = 1000;        // 1000 SP maximum
        [SerializeField] private bool _requireBlockchainVerification = true;

        // Active listings
        private List<MarketplaceListing> _activeListings = new List<MarketplaceListing>();
        private Dictionary<string, MarketplaceListing> _listingsById = new Dictionary<string, MarketplaceListing>();

        // Services
        private BlockchainGeneticsService _blockchainService;

        // Events
        public event Action<MarketplaceListing> OnListingCreated;
        public event Action<MarketplaceListing, string> OnListingSold;  // listing, buyerId
        public event Action<MarketplaceListing> OnListingCancelled;
        public event Action<MarketplaceListing> OnListingExpired;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get blockchain service
            var container = ServiceContainerFactory.Instance;
            if (container != null)
            {
                _blockchainService = container.Resolve<BlockchainGeneticsService>();

                // Register self
                container.RegisterSingleton<GeneticsMarketplace>(this);
            }

            ChimeraLogger.Log("MARKETPLACE",
                "Genetics marketplace initialized", this);
        }

        #region Listing Management

        /// <summary>
        /// Creates a new genetics listing.
        ///
        /// GAMEPLAY:
        /// - Player selects strain to sell
        /// - Sets price in Skill Points
        /// - System verifies blockchain hash
        /// - Listing goes live
        /// </summary>
        public MarketplaceListing CreateListing(
            string sellerId, string sellerName, float sellerReputation,
            PlantGenotype genotype, string description, int priceSkillPoints, int durationDays)
        {
            // Validate price
            if (priceSkillPoints < _minListingPrice || priceSkillPoints > _maxListingPrice)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Price {priceSkillPoints} SP outside allowed range ({_minListingPrice}-{_maxListingPrice})", this);
                return null;
            }

            // Validate duration
            if (durationDays <= 0 || durationDays > _maxListingDuration)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Duration {durationDays} days outside allowed range (1-{_maxListingDuration})", this);
                return null;
            }

            // Verify blockchain hash
            string geneticHash = genotype.GenotypeId ?? genotype.StrainName;
            bool isVerified = _requireBlockchainVerification ?
                VerifyGeneticHash(geneticHash) : true;

            if (_requireBlockchainVerification && !isVerified)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Genetic hash {geneticHash} failed blockchain verification", this);
                return null;
            }

            // Create genetics data
            var geneticsData = new GeneticsListingData
            {
                GeneticHash = geneticHash,
                StrainName = genotype.StrainName,
                Generation = genotype.Generation,
                THCPercentage = genotype.PotencyPotential, // Use PotencyPotential (already in %)
                CBDPercentage = 0f, // Default - CannabisGenotype doesn't have CBD
                YieldPotential = genotype.YieldPotential, // Already in grams
                FloweringDays = genotype.FloweringTime,
                ParentStrain1 = genotype.ParentStrain ?? "Unknown",
                ParentStrain2 = "Unknown", // CannabisGenotype doesn't have Parent2
                IsStabilized = genotype.Generation >= 5,
                IsBlockchainVerified = isVerified,
                VerificationDate = DateTime.Now
            };

            // Create listing
            var listing = MarketplaceListing.CreateGeneticsListing(
                sellerId, sellerName, sellerReputation,
                geneticHash, genotype.StrainName, description,
                geneticsData, priceSkillPoints, durationDays
            );

            _activeListings.Add(listing);
            _listingsById[listing.ListingId] = listing;

            OnListingCreated?.Invoke(listing);

            ChimeraLogger.Log("MARKETPLACE",
                $"Genetics listed: {genotype.StrainName} at {priceSkillPoints} SP " +
                $"(Verified: {isVerified})", this);

            return listing;
        }

        /// <summary>
        /// Verifies genetic hash against blockchain.
        /// </summary>
        private bool VerifyGeneticHash(string geneticHash)
        {
            if (_blockchainService == null || string.IsNullOrEmpty(geneticHash))
                return false;

            // Check if hash exists in blockchain ledger
            // BlockchainGeneticsService doesn't have HasGeneticRecord, use IsBlockchainVerified instead
            return _blockchainService.IsBlockchainVerified(geneticHash);
        }

        /// <summary>
        /// Cancels a listing (seller removes it).
        /// </summary>
        public bool CancelListing(string listingId, string sellerId)
        {
            if (!_listingsById.TryGetValue(listingId, out var listing))
                return false;

            // Verify ownership
            if (listing.SellerId != sellerId)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Player {sellerId} cannot cancel listing {listingId} (not owner)", this);
                return false;
            }

            // Can't cancel if already sold
            if (listing.Status == ListingStatus.Sold)
                return false;

            listing.Status = ListingStatus.Cancelled;
            _activeListings.Remove(listing);

            OnListingCancelled?.Invoke(listing);

            ChimeraLogger.Log("MARKETPLACE",
                $"Listing cancelled: {listingId}", this);

            return true;
        }

        /// <summary>
        /// Processes expired listings.
        /// </summary>
        public void ProcessExpirations()
        {
            var expired = _activeListings.Where(l => l.IsExpired() && l.Status == ListingStatus.Active).ToList();

            foreach (var listing in expired)
            {
                listing.Status = ListingStatus.Expired;
                _activeListings.Remove(listing);

                OnListingExpired?.Invoke(listing);

                ChimeraLogger.Log("MARKETPLACE",
                    $"Listing expired: {listing.ItemName}", this);
            }
        }

        #endregion

        #region Purchasing

        /// <summary>
        /// Purchases a genetics listing.
        ///
        /// GAMEPLAY:
        /// - Player browses listings
        /// - Finds desired strain
        /// - Clicks "Buy"
        /// - Skill points deducted
        /// - Genetics added to seed bank
        /// - Seller earns SP (minus 5% fee)
        /// </summary>
        public bool PurchaseListing(string listingId, string buyerId,
            System.Action<PlantGenotype> onGeneticsReceived)
        {
            if (!_listingsById.TryGetValue(listingId, out var listing))
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Listing {listingId} not found", this);
                return false;
            }

            // Validate listing status
            if (listing.Status != ListingStatus.Active)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Listing {listingId} not available for purchase", this);
                return false;
            }

            // Can't buy your own listing
            if (listing.SellerId == buyerId)
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    "Cannot purchase your own listing", this);
                return false;
            }

            // Note: Skill point deduction happens in TransactionManager
            // This method assumes payment validation already done

            // Mark as sold
            listing.Status = ListingStatus.Sold;
            listing.SoldDate = DateTime.Now;
            _activeListings.Remove(listing);

            // Transfer genetics to buyer
            if (_blockchainService != null && onGeneticsReceived != null)
            {
                // Reconstruct genotype from listing data
                var genotype = ReconstructGenotype(listing.GeneticsData);
                onGeneticsReceived?.Invoke(genotype);
            }

            OnListingSold?.Invoke(listing, buyerId);

            ChimeraLogger.Log("MARKETPLACE",
                $"Genetics purchased: {listing.ItemName} by {buyerId} for {listing.PriceSkillPoints} SP", this);

            return true;
        }

        /// <summary>
        /// Reconstructs PlantGenotype from listing data.
        /// </summary>
        private PlantGenotype ReconstructGenotype(GeneticsListingData data)
        {
            return new PlantGenotype
            {
                StrainName = data.StrainName,
                GenotypeId = data.GeneticHash,
                Generation = data.Generation,
                PotencyPotential = data.THCPercentage, // Already in percentage
                YieldPotential = data.YieldPotential, // Already in grams
                FloweringTime = data.FloweringDays,
                ParentStrain = data.ParentStrain1 != "Unknown" ? data.ParentStrain1 : null
            };
        }

        #endregion

        #region Search & Browse

        /// <summary>
        /// Searches listings with filters.
        ///
        /// GAMEPLAY:
        /// - Player searches for specific traits
        /// - Filters by price, THC%, verification, etc.
        /// - Sorts by price/popularity/rating
        /// </summary>
        public List<MarketplaceListing> SearchListings(MarketplaceFilters filters)
        {
            var results = _activeListings.Where(l => l.Status == ListingStatus.Active).ToList();

            // Type filter (genetics only for this marketplace)
            results = results.Where(l => l.Type == MarketplaceListingType.Genetics).ToList();

            // Price filters
            if (filters.MinPrice.HasValue)
                results = results.Where(l => l.PriceSkillPoints >= filters.MinPrice.Value).ToList();

            if (filters.MaxPrice.HasValue)
                results = results.Where(l => l.PriceSkillPoints <= filters.MaxPrice.Value).ToList();

            // Seller rating filter
            if (filters.MinSellerRating.HasValue)
                results = results.Where(l => l.SellerReputation >= filters.MinSellerRating.Value).ToList();

            // Verified genetics only
            if (filters.VerifiedGeneticsOnly.HasValue && filters.VerifiedGeneticsOnly.Value)
                results = results.Where(l => l.GeneticsData.IsBlockchainVerified).ToList();

            // Search query (name/description)
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                string query = filters.SearchQuery.ToLower();
                results = results.Where(l =>
                    l.ItemName.ToLower().Contains(query) ||
                    l.ItemDescription.ToLower().Contains(query) ||
                    l.GeneticsData.StrainName.ToLower().Contains(query)
                ).ToList();
            }

            // Sort
            results = SortListings(results, filters.SortOrder);

            return results;
        }

        /// <summary>
        /// Sorts listings by specified order.
        /// </summary>
        private List<MarketplaceListing> SortListings(
            List<MarketplaceListing> listings, MarketplaceFilters.MarketplaceSortOrder sortOrder)
        {
            switch (sortOrder)
            {
                case MarketplaceFilters.MarketplaceSortOrder.PriceLowToHigh:
                    return listings.OrderBy(l => l.PriceSkillPoints).ToList();

                case MarketplaceFilters.MarketplaceSortOrder.PriceHighToLow:
                    return listings.OrderByDescending(l => l.PriceSkillPoints).ToList();

                case MarketplaceFilters.MarketplaceSortOrder.RecentlyListed:
                    return listings.OrderByDescending(l => l.ListedDate).ToList();

                case MarketplaceFilters.MarketplaceSortOrder.MostPopular:
                    return listings.OrderByDescending(l => l.ViewCount + l.FavoriteCount).ToList();

                case MarketplaceFilters.MarketplaceSortOrder.HighestRated:
                    return listings.OrderByDescending(l => l.SellerReputation).ToList();

                default:
                    return listings;
            }
        }

        /// <summary>
        /// Gets featured/recommended listings.
        /// </summary>
        public List<MarketplaceListing> GetFeaturedListings(int count = 5)
        {
            return _activeListings
                .Where(l => l.Status == ListingStatus.Active && l.GeneticsData.IsBlockchainVerified)
                .OrderByDescending(l => l.SellerReputation)
                .ThenByDescending(l => l.ViewCount)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets listings by THC percentage range.
        /// </summary>
        public List<MarketplaceListing> GetByTHCRange(float minTHC, float maxTHC)
        {
            return _activeListings
                .Where(l => l.Status == ListingStatus.Active &&
                           l.GeneticsData.THCPercentage >= minTHC &&
                           l.GeneticsData.THCPercentage <= maxTHC)
                .OrderByDescending(l => l.GeneticsData.THCPercentage)
                .ToList();
        }

        /// <summary>
        /// Gets listings by seller.
        /// </summary>
        public List<MarketplaceListing> GetBySeller(string sellerId)
        {
            return _activeListings
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.ListedDate)
                .ToList();
        }

        #endregion

        #region Analytics

        /// <summary>
        /// Gets market analytics for genetics.
        /// </summary>
        public (float avgPrice, string mostTraded, int totalListings) GetMarketAnalytics()
        {
            return MarketplaceQueryHelpers.GetMarketAnalytics(_activeListings, MarketplaceListingType.Genetics);
        }

        /// <summary>
        /// Increments view count for a listing.
        /// </summary>
        public void IncrementViewCount(string listingId)
        {
            if (_listingsById.TryGetValue(listingId, out var listing))
            {
                listing.ViewCount++;
            }
        }

        /// <summary>
        /// Toggles favorite for a listing.
        /// </summary>
        public void ToggleFavorite(string listingId, bool isFavorite)
        {
            if (_listingsById.TryGetValue(listingId, out var listing))
            {
                listing.FavoriteCount += isFavorite ? 1 : -1;
                listing.FavoriteCount = Mathf.Max(0, listing.FavoriteCount);
            }
        }

        #endregion

        /// <summary>
        /// Gets all active listings.
        /// </summary>
        public List<MarketplaceListing> GetAllActiveListings()
        {
            return _activeListings.Where(l => l.Status == ListingStatus.Active).ToList();
        }

        /// <summary>
        /// Gets a specific listing.
        /// </summary>
        public MarketplaceListing GetListing(string listingId)
        {
            return _listingsById.TryGetValue(listingId, out var listing) ? listing : null;
        }

        /// <summary>
        /// Gets commission amount for a sale.
        /// </summary>
        public int CalculateCommission(int salePrice)
        {
            return Mathf.RoundToInt(salePrice * _commissionRate);
        }

        /// <summary>
        /// Gets seller earnings after commission.
        /// </summary>
        public int CalculateSellerEarnings(int salePrice)
        {
            return salePrice - CalculateCommission(salePrice);
        }
    }
}
