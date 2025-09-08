using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Data.Facilities;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Interface for facility management operations
    /// </summary>
    public interface IFacilityManager
    {
        string CurrentFacilityId { get; }
        bool IsInitialized { get; }
        int OwnedFacilitiesCount { get; }
        FacilityProgressionData ProgressionData { get; }
        FacilityTierSO CurrentTier { get; }
        int CurrentTierIndex { get; }
        bool IsLoadingScene { get; }

        // Facility operations
        bool SwitchToFacility(string facilityId);
        bool CanUpgradeCurrentFacility();
        bool UpgradeCurrentFacility();
        bool SellFacility(string facilityId);
        Task<bool> QuickSwitchByTierName(string tierName);
        string GetNextAvailableTier();
        bool CanUpgradeToNextTier();
        Task<bool> PurchaseNewFacilityAsync(string tierName);

        // Progression
        Dictionary<string, object> GetProgressionStatistics();
        FacilityProgressionStatistics GetProgressionStatisticsTyped();
        FacilityDisplayInfo GetCurrentFacilityDisplayInfo();
        List<string> GetAvailableFacilityScenes();
        List<string> GetAvailableFacilitiesForSwitching();
        List<FacilitySwitchInfo> GetAvailableFacilitiesForSwitchingDetailed();
        void CheckForUpgradeAvailability();

        // Events (placeholder for future implementation)
        event Action<string> OnFacilityChanged;
        event Action<FacilityProgressionData> OnProgressionUpdated;

        // Extension method placeholders - implemented via extensions
        void SubscribeToFacilityEvents(
            Action onFacilityUpgraded = null,
            Action onFacilitySwitch = null,
            Action onFacilityPurchased = null,
            Action onFacilitySold = null,
            Action onFacilityUpgradeAvailable = null,
            Action onFacilityValueUpdated = null);
        void UnsubscribeFromFacilityEvents(
            Action<object> onFacilityUpgraded = null,
            Action<object, object> onFacilitySwitch = null,
            Action<object> onFacilityPurchased = null,
            Action<object> onFacilitySold = null,
            Action<object> onFacilityUpgradeAvailable = null,
            Action<object> onFacilityValueUpdated = null);
    }
}
