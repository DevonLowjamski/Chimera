using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Marketplace
{
    /// <summary>
    /// Marketplace data structures.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST (from Gameplay Doc):
    /// =========================================================
    /// "Skill Points are dual-use currency: unlock progression AND trade in marketplace"
    /// "Players set their own prices for genetics and schematics"
    ///
    /// **Marketplace Economy**:
    /// - Genetics: Trade verified strains (blockchain hash = authenticity)
    /// - Schematics: Share construction blueprints
    /// - Currency: Skill Points (earned via gameplay, spent on trades)
    /// - Player-Driven: Supply/demand sets prices
    ///
    /// **Trust System**:
    /// - Blockchain verification for genetics (prevents fraud)
    /// - Seller reputation score (based on trades)
    /// - Transaction history (transparency)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Buy Blue Dream genetics for 50 SP (Verified ✅)"
    /// Behind scenes: Blockchain hash validation, reputation algorithms, market analytics.
    /// </summary>

    /// <summary>
    /// Marketplace listing types.
    /// </summary>
    public enum MarketplaceListingType
    {
        Genetics,       // Plant strain genetics
        Schematic,      // Construction blueprint
        Equipment,      // Used equipment (future)
        Service         // Player services (future)
    }

    /// <summary>
    /// Listing status.
    /// </summary>
    public enum ListingStatus
    {
        Active,         // Listed and buyable
        Sold,           // Purchase completed
        Expired,        // Listing time expired
        Cancelled       // Seller cancelled
    }

    /// <summary>
    /// Marketplace listing - represents item for sale.
    /// </summary>
    [Serializable]
    public class MarketplaceListing
    {
        public string ListingId;
        public MarketplaceListingType Type;
        public ListingStatus Status;

        // Seller info
        public string SellerId;
        public string SellerName;
        public float SellerReputation;       // 0-5 stars

        // Item details
        public string ItemId;                // Genetic hash or schematic ID
        public string ItemName;              // Display name
        public string ItemDescription;
        public Sprite ItemIcon;

        // Pricing
        public int PriceSkillPoints;         // Price in Skill Points
        public int OriginalPrice;            // For price history

        // Genetics specific (if Type == Genetics)
        public GeneticsListingData GeneticsData;

        // Schematics specific (if Type == Schematic)
        public SchematicListingData SchematicData;

        // Timing
        public DateTime ListedDate;
        public DateTime? SoldDate;
        public int DurationDays;             // How long listing active

        // Stats
        public int ViewCount;
        public int FavoriteCount;

        /// <summary>
        /// Creates a genetics listing.
        /// </summary>
        public static MarketplaceListing CreateGeneticsListing(
            string sellerId, string sellerName, float reputation,
            string geneticHash, string strainName, string description,
            GeneticsListingData geneticsData, int priceSkillPoints, int durationDays)
        {
            return new MarketplaceListing
            {
                ListingId = $"GEN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Type = MarketplaceListingType.Genetics,
                Status = ListingStatus.Active,
                SellerId = sellerId,
                SellerName = sellerName,
                SellerReputation = reputation,
                ItemId = geneticHash,
                ItemName = strainName,
                ItemDescription = description,
                GeneticsData = geneticsData,
                PriceSkillPoints = priceSkillPoints,
                OriginalPrice = priceSkillPoints,
                ListedDate = DateTime.Now,
                DurationDays = durationDays,
                ViewCount = 0,
                FavoriteCount = 0
            };
        }

        /// <summary>
        /// Creates a schematic listing.
        /// </summary>
        public static MarketplaceListing CreateSchematicListing(
            string sellerId, string sellerName, float reputation,
            string schematicId, string blueprintName, string description,
            SchematicListingData schematicData, int priceSkillPoints, int durationDays)
        {
            return new MarketplaceListing
            {
                ListingId = $"SCH_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Type = MarketplaceListingType.Schematic,
                Status = ListingStatus.Active,
                SellerId = sellerId,
                SellerName = sellerName,
                SellerReputation = reputation,
                ItemId = schematicId,
                ItemName = blueprintName,
                ItemDescription = description,
                SchematicData = schematicData,
                PriceSkillPoints = priceSkillPoints,
                OriginalPrice = priceSkillPoints,
                ListedDate = DateTime.Now,
                DurationDays = durationDays,
                ViewCount = 0,
                FavoriteCount = 0
            };
        }

        /// <summary>
        /// Checks if listing is expired.
        /// </summary>
        public bool IsExpired()
        {
            return (DateTime.Now - ListedDate).Days >= DurationDays;
        }
    }

    /// <summary>
    /// Genetics listing data (blockchain verified strains).
    /// </summary>
    [Serializable]
    public struct GeneticsListingData
    {
        public string GeneticHash;           // Blockchain hash (proof of authenticity)
        public string StrainName;
        public int Generation;               // F1, F2, F3, etc.

        // Trait data
        public float THCPercentage;
        public float CBDPercentage;
        public float YieldPotential;         // g/plant
        public int FloweringDays;

        // Genetics info
        public string ParentStrain1;
        public string ParentStrain2;
        public bool IsStabilized;            // F5+ = stabilized

        // Verification
        public bool IsBlockchainVerified;
        public DateTime VerificationDate;

        /// <summary>
        /// Gets genetics summary for display.
        /// </summary>
        public string GetSummary()
        {
            string verified = IsBlockchainVerified ? "✅ Verified" : "⚠️ Unverified";
            return $"Gen {Generation} | THC: {THCPercentage:F1}% | CBD: {CBDPercentage:F1}% | {verified}";
        }
    }

    /// <summary>
    /// Schematic listing data (construction blueprints).
    /// </summary>
    [Serializable]
    public struct SchematicListingData
    {
        public string SchematicId;
        public string BlueprintName;
        public string Category;              // "Room", "Plumbing", "Lighting", etc.

        // Blueprint details
        public Vector2Int Size;              // Grid size
        public int TierLevel;                // 1-5 (basic to advanced)
        public List<string> RequiredUnlocks; // Skill tree nodes needed

        // Costs (if player builds it)
        public int ConstructionCost;
        public int MaintenanceCostPerDay;

        // Benefits
        public string PrimaryBenefit;        // "+20% yield", "Auto water", etc.
        public List<string> SecondaryBenefits;

        /// <summary>
        /// Gets schematic summary for display.
        /// </summary>
        public string GetSummary()
        {
            return $"Tier {TierLevel} | {Size.x}x{Size.y} | {PrimaryBenefit}";
        }
    }

    /// <summary>
    /// Transaction record.
    /// </summary>
    [Serializable]
    public class MarketplaceTransaction
    {
        public string TransactionId;
        public string ListingId;
        public MarketplaceListingType Type;

        // Participants
        public string SellerId;
        public string BuyerId;

        // Item transferred
        public string ItemId;                // Genetic hash or schematic ID
        public string ItemName;

        // Payment
        public int PriceSkillPoints;

        // Timing
        public DateTime TransactionDate;

        // Post-transaction
        public bool BuyerRated;
        public float BuyerRating;            // 1-5 stars
        public string BuyerReview;

        /// <summary>
        /// Creates a transaction record.
        /// </summary>
        public static MarketplaceTransaction Create(
            MarketplaceListing listing, string buyerId)
        {
            return new MarketplaceTransaction
            {
                TransactionId = $"TXN_{Guid.NewGuid().ToString().Substring(0, 12)}",
                ListingId = listing.ListingId,
                Type = listing.Type,
                SellerId = listing.SellerId,
                BuyerId = buyerId,
                ItemId = listing.ItemId,
                ItemName = listing.ItemName,
                PriceSkillPoints = listing.PriceSkillPoints,
                TransactionDate = DateTime.Now,
                BuyerRated = false,
                BuyerRating = 0f
            };
        }
    }

    /// <summary>
    /// Seller profile.
    /// </summary>
    [Serializable]
    public class SellerProfile
    {
        public string SellerId;
        public string SellerName;

        // Reputation
        public float AverageRating;          // 0-5 stars
        public int TotalRatings;
        public int TotalSales;
        public int TotalListings;

        // Sales breakdown
        public int GeneticsSold;
        public int SchematicsSold;

        // Skill points earned
        public int TotalSkillPointsEarned;

        // Trust metrics
        public float VerifiedGeneticsPercentage; // % of genetics that were blockchain verified
        public int DisputeCount;

        /// <summary>
        /// Updates reputation from new rating.
        /// </summary>
        public void AddRating(float newRating)
        {
            float totalRating = AverageRating * TotalRatings;
            TotalRatings++;
            AverageRating = (totalRating + newRating) / TotalRatings;
        }
    }

    /// <summary>
    /// Marketplace search filters.
    /// </summary>
    [Serializable]
    public struct MarketplaceFilters
    {
        public MarketplaceListingType? Type;
        public int? MaxPrice;
        public int? MinPrice;
        public float? MinSellerRating;
        public bool? VerifiedGeneticsOnly;
        public string SearchQuery;
        public MarketplaceSortOrder SortOrder;

        public enum MarketplaceSortOrder
        {
            PriceLowToHigh,
            PriceHighToLow,
            RecentlyListed,
            MostPopular,
            HighestRated
        }

        /// <summary>
        /// Gets default filters (show all).
        /// </summary>
        public static MarketplaceFilters Default => new MarketplaceFilters
        {
            Type = null,
            MaxPrice = null,
            MinPrice = null,
            MinSellerRating = null,
            VerifiedGeneticsOnly = false,
            SearchQuery = "",
            SortOrder = MarketplaceSortOrder.RecentlyListed
        };
    }

    /// <summary>
    /// Market analytics data.
    /// </summary>
    [Serializable]
    public struct MarketplaceAnalytics
    {
        public int ActiveListings;
        public int TotalTransactions;
        public int TotalSkillPointsTraded;

        // Price analytics
        public float AverageGeneticsPrice;
        public float AverageSchematicPrice;

        // Popular items
        public string MostTradedGenetics;
        public string MostTradedSchematic;

        // Market activity
        public int TransactionsLast24Hours;
        public int NewListingsLast24Hours;
    }

    /// <summary>
    /// Marketplace notification types.
    /// </summary>
    public enum MarketplaceNotificationType
    {
        ListingSold,            // Your listing sold
        PurchaseComplete,       // You bought something
        ListingExpired,         // Your listing expired
        PriceAlert,            // Item you're watching changed price
        NewListingMatch,       // New listing matches your saved search
        ReputationUpdate       // Your seller rating changed
    }

    /// <summary>
    /// Marketplace notification.
    /// </summary>
    [Serializable]
    public struct MarketplaceNotification
    {
        public string NotificationId;
        public MarketplaceNotificationType Type;
        public string Message;
        public string RelatedListingId;
        public DateTime Timestamp;
        public bool IsRead;

        /// <summary>
        /// Creates a notification.
        /// </summary>
        public static MarketplaceNotification Create(
            MarketplaceNotificationType type, string message, string listingId = null)
        {
            return new MarketplaceNotification
            {
                NotificationId = Guid.NewGuid().ToString().Substring(0, 8),
                Type = type,
                Message = message,
                RelatedListingId = listingId,
                Timestamp = DateTime.Now,
                IsRead = false
            };
        }
    }
}
