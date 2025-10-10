using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Marketplace;

namespace ProjectChimera.Systems.Marketplace
{
    /// <summary>
    /// Helper utilities for marketplace listing queries and filtering.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class MarketplaceQueryHelpers
    {
        /// <summary>
        /// Gets analytics for marketplace listings.
        /// </summary>
        public static (float avgPrice, string mostTraded, int totalListings) GetMarketAnalytics(
            List<MarketplaceListing> listings, MarketplaceListingType type)
        {
            var activeListings = listings
                .Where(l => l.Type == type && l.Status == ListingStatus.Active)
                .ToList();

            float avgPrice = activeListings.Any() ?
                (float)activeListings.Average(l => l.PriceSkillPoints) : 0f;

            var mostTraded = activeListings
                .OrderByDescending(l => l.ViewCount)
                .FirstOrDefault()?.ItemName ?? "None";

            return (avgPrice, mostTraded, activeListings.Count);
        }
    }
}
