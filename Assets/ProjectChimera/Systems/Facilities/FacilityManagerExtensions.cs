using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Data.Facilities;

namespace ProjectChimera.Systems.Facilities
{
    public class FacilitySwitchResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public System.DateTime StartTime { get; set; }
        public string PreviousFacilityId { get; set; }
        public string TargetFacilityId { get; set; }
        public System.DateTime EndTime { get; set; }
        public System.DateTime ActualSwitchTime { get; set; }
        public string SuccessMessage { get; set; }
    }
}

namespace ProjectChimera.Systems.Facilities
{
    public static class FacilityManagerExtensions
    {
        public static void SubscribeToFacilityEvents(this IFacilityManager facilityManager,
            Action onFacilityUpgraded = null,
            Action onFacilitySwitch = null,
            Action onFacilityPurchased = null,
            Action onFacilitySold = null,
            Action onFacilityUpgradeAvailable = null,
            Action onFacilityValueUpdated = null)
        {
            // Placeholder implementation - would wire up to actual events
            // These would typically be connected to the facility manager's internal event system
        }

        public static void UnsubscribeFromFacilityEvents(this IFacilityManager facilityManager,
            Action<object> onFacilityUpgraded = null,
            Action<object, object> onFacilitySwitch = null,
            Action<object> onFacilityPurchased = null,
            Action<object> onFacilitySold = null,
            Action<object> onFacilityUpgradeAvailable = null,
            Action<object> onFacilityValueUpdated = null)
        {
            // Placeholder implementation - would unwire from actual events
        }

        public static List<string> GetAvailableFacilitiesForSwitching(this IFacilityManager facilityManager)
        {
            // Use the interface method
            return facilityManager.GetAvailableFacilitiesForSwitching();
        }

        public static float GetTotalPortfolioValue(this IFacilityManager facilityManager)
        {
            // Calculate total portfolio value from progression data
            var stats = facilityManager.GetProgressionStatisticsTyped();
            return stats?.TotalValue ?? 0f;
        }

        public static float GetTotalInvestment(this IFacilityManager facilityManager)
        {
            // Calculate total investment from progression data
            var stats = facilityManager.GetProgressionStatisticsTyped();
            return stats?.TotalInvestment ?? 0f;
        }

        public static float GetPortfolioROI(this IFacilityManager facilityManager)
        {
            // Calculate ROI from progression data
            var totalValue = facilityManager.GetTotalPortfolioValue();
            var totalInvestment = facilityManager.GetTotalInvestment();

            if (totalInvestment <= 0) return 0f;
            return ((totalValue - totalInvestment) / totalInvestment) * 100f;
        }

        public static bool CanUpgradeToNextTier(this IFacilityManager facilityManager)
        {
            return facilityManager.CanUpgradeToNextTier();
        }

        public static int OwnedFacilitiesCount(this IFacilityManager facilityManager)
        {
            return facilityManager.OwnedFacilitiesCount;
        }

        public static string GetNextAvailableTier(this IFacilityManager facilityManager)
        {
            // Use the interface method
            return facilityManager.GetNextAvailableTier();
        }

        public static async Task<bool> UpgradeToTierAsync(this IFacilityManager facilityManager, string tierName)
        {
            // Quick switch by tier name
            return await facilityManager.QuickSwitchByTierName(tierName);
        }

        public static async Task<FacilitySwitchResult> SwitchToFacilityWithResultAsync(this IFacilityManager facilityManager, string facilityId)
        {
            // Switch to facility and return result with error handling
            try
            {
                bool success = facilityManager.SwitchToFacility(facilityId);
                return new FacilitySwitchResult
                {
                    Success = success,
                    ErrorMessage = success ? null : "Failed to switch to facility"
                };
            }
            catch (System.Exception ex)
            {
                return new FacilitySwitchResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public static async Task<bool> PurchaseNewFacilityAsync(this IFacilityManager facilityManager, string tierName)
        {
            // Use the interface method
            return await facilityManager.PurchaseNewFacilityAsync(tierName);
        }

        public static string CurrentTierName(this IFacilityManager facilityManager)
        {
            return facilityManager.CurrentTier?.TierName ?? "";
        }
    }
}
