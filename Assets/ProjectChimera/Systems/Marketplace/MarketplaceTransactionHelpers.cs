using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Marketplace;

namespace ProjectChimera.Systems.Marketplace
{
    /// <summary>
    /// Helper utilities for marketplace transactions and seller profiles.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class MarketplaceTransactionHelpers
    {
        /// <summary>
        /// Gets AvailableSkillPoints from SkillTreeManager using reflection (avoids circular dependency).
        /// </summary>
        public static int GetAvailableSkillPoints(object skillTreeManager)
        {
            if (skillTreeManager == null) return 0;

            var property = skillTreeManager.GetType().GetProperty("AvailableSkillPoints");
            if (property != null)
            {
                return (int)property.GetValue(skillTreeManager);
            }
            return 0;
        }

        /// <summary>
        /// Spends skill points using reflection (avoids circular dependency).
        /// </summary>
        public static bool SpendSkillPoints(object skillTreeManager, int amount, string reason)
        {
            if (skillTreeManager == null) return false;

            var method = skillTreeManager.GetType().GetMethod("SpendSkillPoints");
            if (method != null)
            {
                return (bool)method.Invoke(skillTreeManager, new object[] { amount, reason });
            }
            return false;
        }

        /// <summary>
        /// Awards skill points using reflection (avoids circular dependency).
        /// </summary>
        public static void AwardSkillPoints(object skillTreeManager, int amount, string reason)
        {
            if (skillTreeManager == null) return;

            var method = skillTreeManager.GetType().GetMethod("AwardSkillPoints");
            method?.Invoke(skillTreeManager, new object[] { amount, reason });
        }

        /// <summary>
        /// Gets verified genetics count for seller.
        /// </summary>
        public static int GetVerifiedGeneticsCount(List<MarketplaceTransaction> transactions, string sellerId)
        {
            return transactions
                .Where(t => t.SellerId == sellerId && t.Type == MarketplaceListingType.Genetics)
                .Count();
        }

        /// <summary>
        /// Creates or updates seller profile after sale.
        /// </summary>
        public static SellerProfile CreateOrUpdateSellerProfile(
            Dictionary<string, SellerProfile> sellerProfiles,
            List<MarketplaceTransaction> allTransactions,
            string sellerId,
            MarketplaceListing listing,
            int commission)
        {
            if (!sellerProfiles.ContainsKey(sellerId))
            {
                sellerProfiles[sellerId] = new SellerProfile
                {
                    SellerId = sellerId,
                    SellerName = listing.SellerName,
                    AverageRating = 0f,
                    TotalRatings = 0,
                    TotalSales = 0,
                    TotalListings = 0,
                    GeneticsSold = 0,
                    SchematicsSold = 0,
                    TotalSkillPointsEarned = 0,
                    VerifiedGeneticsPercentage = 0f,
                    DisputeCount = 0
                };
            }

            var profile = sellerProfiles[sellerId];
            profile.TotalSales++;

            if (listing.Type == MarketplaceListingType.Genetics)
            {
                profile.GeneticsSold++;

                // Update verified genetics percentage
                int totalGenetics = profile.GeneticsSold;
                int verifiedCount = GetVerifiedGeneticsCount(allTransactions, sellerId);
                profile.VerifiedGeneticsPercentage = (float)verifiedCount / totalGenetics;
            }
            else if (listing.Type == MarketplaceListingType.Schematic)
            {
                profile.SchematicsSold++;
            }

            profile.TotalSkillPointsEarned += (listing.PriceSkillPoints - commission);

            return profile;
        }
    }
}
