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
    /// Schematics marketplace - player-to-player blueprint trading.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Share your custom room designs and construction blueprints with other players"
    ///
    /// **Schematic Trading**:
    /// - Sell your efficient room layouts
    /// - Buy advanced blueprints from experienced players
    /// - Share automation setups
    /// - Unlock new construction techniques
    ///
    /// **Player Experience**:
    /// - Design efficient grow room → Save as blueprint
    /// - List blueprint for sale (set price in SP)
    /// - Other players buy and use your design
    /// - Earn passive SP from popular blueprints
    ///
    /// **Blueprint Types**:
    /// - Room Layouts: Complete room designs
    /// - Plumbing Systems: Water distribution networks
    /// - Lighting Setups: Optimal light placement
    /// - Automation Configs: Automated task sequences
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Auto-Water 4x4 Room - Tier 3 - 45 SP"
    /// Behind scenes: Blueprint validation, compatibility checks, skill requirements.
    /// </summary>
    public class SchematicsMarketplace : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _commissionRate = 0.05f;      // 5% marketplace fee
        [SerializeField] private int _maxListingDuration = 60;       // 60 days max (longer than genetics)
        [SerializeField] private int _minListingPrice = 5;           // 5 SP minimum
        [SerializeField] private int _maxListingPrice = 500;         // 500 SP maximum

        // Active listings
        private List<MarketplaceListing> _activeListings = new List<MarketplaceListing>();
        private Dictionary<string, MarketplaceListing> _listingsById = new Dictionary<string, MarketplaceListing>();

        // Schematic categories
        private readonly string[] _validCategories = new string[]
        {
            "Room Layout",
            "Plumbing System",
            "Lighting Setup",
            "Automation Config",
            "Complete Facility"
        };

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
            // Register self
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<SchematicsMarketplace>(this);

            ChimeraLogger.Log("MARKETPLACE",
                "Schematics marketplace initialized", this);
        }

        #region Listing Management

        /// <summary>
        /// Creates a new schematic listing.
        ///
        /// GAMEPLAY:
        /// - Player saves custom blueprint
        /// - Sets price and description
        /// - Lists for other players to buy
        /// </summary>
        public MarketplaceListing CreateListing(
            string sellerId, string sellerName, float sellerReputation,
            string schematicId, string blueprintName, string category, string description,
            SchematicListingData schematicData, int priceSkillPoints, int durationDays)
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

            // Validate category
            if (!_validCategories.Contains(category))
            {
                ChimeraLogger.LogWarning("MARKETPLACE",
                    $"Invalid category: {category}", this);
                return null;
            }

            // Create listing
            var listing = MarketplaceListing.CreateSchematicListing(
                sellerId, sellerName, sellerReputation,
                schematicId, blueprintName, description,
                schematicData, priceSkillPoints, durationDays
            );

            _activeListings.Add(listing);
            _listingsById[listing.ListingId] = listing;

            OnListingCreated?.Invoke(listing);

            ChimeraLogger.Log("MARKETPLACE",
                $"Schematic listed: {blueprintName} at {priceSkillPoints} SP", this);

            return listing;
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
        /// Purchases a schematic listing.
        ///
        /// GAMEPLAY:
        /// - Player browses blueprints
        /// - Finds desired design
        /// - Clicks "Buy"
        /// - Skill points deducted
        /// - Blueprint added to collection
        /// - Seller earns SP (minus 5% fee)
        /// </summary>
        public bool PurchaseListing(string listingId, string buyerId,
            System.Action<SchematicListingData> onSchematicReceived)
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

            // Transfer schematic to buyer
            onSchematicReceived?.Invoke(listing.SchematicData);

            OnListingSold?.Invoke(listing, buyerId);

            ChimeraLogger.Log("MARKETPLACE",
                $"Schematic purchased: {listing.ItemName} by {buyerId} for {listing.PriceSkillPoints} SP", this);

            return true;
        }

        #endregion

        #region Search & Browse

        /// <summary>
        /// Searches listings with filters.
        ///
        /// GAMEPLAY:
        /// - Player searches for specific blueprint types
        /// - Filters by category, tier, price, etc.
        /// - Sorts by price/popularity/rating
        /// </summary>
        public List<MarketplaceListing> SearchListings(MarketplaceFilters filters, string category = null, int? tier = null)
        {
            var results = _activeListings.Where(l => l.Status == ListingStatus.Active).ToList();

            // Type filter (schematics only for this marketplace)
            results = results.Where(l => l.Type == MarketplaceListingType.Schematic).ToList();

            // Category filter
            if (!string.IsNullOrEmpty(category))
                results = results.Where(l => l.SchematicData.Category == category).ToList();

            // Tier filter
            if (tier.HasValue)
                results = results.Where(l => l.SchematicData.TierLevel == tier.Value).ToList();

            // Price filters
            if (filters.MinPrice.HasValue)
                results = results.Where(l => l.PriceSkillPoints >= filters.MinPrice.Value).ToList();

            if (filters.MaxPrice.HasValue)
                results = results.Where(l => l.PriceSkillPoints <= filters.MaxPrice.Value).ToList();

            // Seller rating filter
            if (filters.MinSellerRating.HasValue)
                results = results.Where(l => l.SellerReputation >= filters.MinSellerRating.Value).ToList();

            // Search query (name/description)
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                string query = filters.SearchQuery.ToLower();
                results = results.Where(l =>
                    l.ItemName.ToLower().Contains(query) ||
                    l.ItemDescription.ToLower().Contains(query) ||
                    l.SchematicData.BlueprintName.ToLower().Contains(query)
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
        /// Gets featured/recommended schematics.
        /// </summary>
        public List<MarketplaceListing> GetFeaturedListings(int count = 5)
        {
            return _activeListings
                .Where(l => l.Status == ListingStatus.Active)
                .OrderByDescending(l => l.SellerReputation)
                .ThenByDescending(l => l.ViewCount)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets listings by category.
        /// </summary>
        public List<MarketplaceListing> GetByCategory(string category)
        {
            return _activeListings
                .Where(l => l.Status == ListingStatus.Active && l.SchematicData.Category == category)
                .OrderByDescending(l => l.SchematicData.TierLevel)
                .ToList();
        }

        /// <summary>
        /// Gets listings by tier level.
        /// </summary>
        public List<MarketplaceListing> GetByTier(int tier)
        {
            return _activeListings
                .Where(l => l.Status == ListingStatus.Active && l.SchematicData.TierLevel == tier)
                .OrderBy(l => l.PriceSkillPoints)
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
        /// Gets market analytics for schematics.
        /// </summary>
        public (float avgPrice, string mostTraded, int totalListings) GetMarketAnalytics()
        {
            var activeSchematicListings = _activeListings
                .Where(l => l.Type == MarketplaceListingType.Schematic && l.Status == ListingStatus.Active)
                .ToList();

            float avgPrice = activeSchematicListings.Any() ?
                (float)activeSchematicListings.Average(l => l.PriceSkillPoints) : 0f;

            var mostTraded = activeSchematicListings
                .OrderByDescending(l => l.ViewCount)
                .FirstOrDefault()?.ItemName ?? "None";

            return (avgPrice, mostTraded, activeSchematicListings.Count);
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
        /// Gets all valid categories.
        /// </summary>
        public string[] GetValidCategories()
        {
            return _validCategories;
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
