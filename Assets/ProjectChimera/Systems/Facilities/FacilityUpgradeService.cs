using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectChimera.Data.Facilities;
using ProjectChimera.Data.Economy;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Handles facility upgrade logic, validation, and processing.
    /// Extracted from FacilityManager for modular architecture.
    /// Manages upgrade requirements, cost processing, and tier advancement.
    /// </summary>
    public class FacilityUpgradeService : MonoBehaviour
    {
        [Header("Upgrade Configuration")]
        [SerializeField] private bool _enableUpgradeLogging = true;
        [SerializeField] private bool _requireSequentialUpgrades = true;
        [SerializeField] private float _upgradeProcessingTimeout = 10f;

        [Header("Facility Tiers")]
        [SerializeField] private List<FacilityTierSO> _facilityTiers = new List<FacilityTierSO>();

        // Dependencies (injected by orchestrator)
        private FacilityRegistry _facilityRegistry;
        private FacilityProgressionData _progressionData;

        // Events
        public System.Action<FacilityTierSO> OnUpgradeStarted;
        public System.Action<FacilityTierSO, bool> OnUpgradeCompleted;
        public System.Action<FacilityTierSO> OnUpgradeAvailable;
        public System.Action<string> OnUpgradeRequirementsNotMet;
        public System.Action<string> OnUpgradeError;

        // Properties
        public int TotalTiers => _facilityTiers.Count;
        public bool IsUpgrading { get; private set; }
        public IEnumerable<FacilityTierSO> AvailableTiers => _facilityTiers;

        /// <summary>
        /// Progression data property for external access
        /// </summary>
        public FacilityProgressionData ProgressionData => _progressionData;

        /// <summary>
        /// Initialize the upgrade service with dependencies
        /// </summary>
        public void Initialize(FacilityRegistry facilityRegistry, FacilityProgressionData progressionData, List<FacilityTierSO> facilityTiers = null)
        {
            _facilityRegistry = facilityRegistry;
            _progressionData = progressionData;

            if (facilityTiers != null)
            {
                _facilityTiers = facilityTiers;
            }

            InitializeFacilityTiers();
            LogDebug("Facility upgrade service initialized");
        }

        /// <summary>
        /// Initialize and sort facility tiers
        /// </summary>
        private void InitializeFacilityTiers()
        {
            if (_facilityTiers == null || _facilityTiers.Count == 0)
            {
                LogError("No facility tiers configured");
                return;
            }

            // Sort tiers by level
            _facilityTiers = _facilityTiers.OrderBy(t => t.TierLevel).ToList();
            LogDebug($"Initialized {_facilityTiers.Count} facility tiers");
        }

        #region Upgrade Validation

        /// <summary>
        /// Validate upgrade requirements with detailed feedback
        /// </summary>
        public FacilityUpgradeValidation ValidateUpgradeRequirements(FacilityTierSO targetTier)
        {
            var validation = new FacilityUpgradeValidation();

            if (targetTier == null)
            {
                validation.AddFailure("Target tier is null");
                return validation;
            }

            var currentTier = GetCurrentTier();
            var currentTierIndex = GetTierIndex(currentTier);
            var targetTierIndex = GetTierIndex(targetTier);

            // Check tier sequence
            if (targetTierIndex <= currentTierIndex)
            {
                validation.AddFailure("Cannot downgrade or upgrade to same tier");
                return validation;
            }

            // Check sequential upgrade requirement
            if (_requireSequentialUpgrades && targetTierIndex > currentTierIndex + 1)
            {
                var nextTier = GetNextTier(currentTier);
                validation.AddFailure($"Must upgrade to tier {nextTier?.TierName} first");
                return validation;
            }

            // Validate individual requirements
            var upgradeRequirements = targetTier.GetUpgradeRequirements(_progressionData);
            var requirements = upgradeRequirements.ToStringList();

            // Check capital requirement
            if (_progressionData.Capital < upgradeRequirements.RequiredCapital)
            {
                validation.AddFailure($"Need ${upgradeRequirements.RequiredCapital:F0} (have ${_progressionData.Capital:F0})");
            }

            // Check experience requirement
            if (_progressionData.Experience < upgradeRequirements.RequiredExperience)
            {
                validation.AddFailure($"Need {upgradeRequirements.RequiredExperience:F0} XP (have {_progressionData.Experience:F0} XP)");
            }

            // Check plants grown requirement
            if (_progressionData.TotalPlants < upgradeRequirements.RequiredPlants)
            {
                validation.AddFailure($"Need {upgradeRequirements.RequiredPlants} plants grown (have {_progressionData.TotalPlants})");
            }

            // Check harvest requirement
            if (_progressionData.TotalHarvests < upgradeRequirements.RequiredHarvests)
            {
                validation.AddFailure($"Need {upgradeRequirements.RequiredHarvests} harvests (have {_progressionData.TotalHarvests})");
            }

            validation.CanUpgrade = validation.FailureReasons.Count == 0;
            return validation;
        }

        /// <summary>
        /// Check if player can upgrade to the next facility tier
        /// </summary>
        public bool CanUpgradeToNextTier()
        {
            var nextTier = GetNextAvailableTier();
            if (nextTier == null) return false;

            var validation = ValidateUpgradeRequirements(nextTier);
            return validation.CanUpgrade;
        }

        /// <summary>
        /// Get next available tier for upgrade
        /// </summary>
        public FacilityTierSO GetNextAvailableTier()
        {
            var currentTier = GetCurrentTier();
            var currentTierIndex = GetTierIndex(currentTier);

            for (int i = currentTierIndex + 1; i < _facilityTiers.Count; i++)
            {
                if (_facilityTiers[i].MeetsUpgradeRequirements(_progressionData))
                {
                    return _facilityTiers[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Get detailed upgrade information for all tiers
        /// </summary>
        public List<FacilityUpgradeInfo> GetAllTierUpgradeInfo()
        {
            var upgradeInfos = new List<FacilityUpgradeInfo>();
            var currentTierIndex = GetCurrentTierIndex();

            for (int i = 0; i < _facilityTiers.Count; i++)
            {
                var tier = _facilityTiers[i];
                var requirements = tier.GetUpgradeRequirements(_progressionData);
                var validation = ValidateUpgradeRequirements(tier);

                var info = new FacilityUpgradeInfo
                {
                    Tier = tier,
                    TierIndex = i,
                    Requirements = requirements.ToStringList(),
                    Validation = validation,
                    IsCurrentTier = i == currentTierIndex,
                    IsNextTier = i == currentTierIndex + 1,
                    IsAvailable = validation.CanUpgrade && i == currentTierIndex + 1
                };

                upgradeInfos.Add(info);
            }

            return upgradeInfos;
        }

        /// <summary>
        /// Get upgrade info for specific tier
        /// </summary>
        public FacilityUpgradeInfo GetUpgradeInfo(FacilityTierSO tier)
        {
            if (tier == null) return null;

            var requirements = tier.GetUpgradeRequirements(_progressionData);
            var validation = ValidateUpgradeRequirements(tier);
            var tierIndex = GetTierIndex(tier);
            var currentTierIndex = GetCurrentTierIndex();

            return new FacilityUpgradeInfo
            {
                Tier = tier,
                TierIndex = tierIndex,
                Requirements = requirements.ToStringList(),
                Validation = validation,
                IsCurrentTier = tierIndex == currentTierIndex,
                IsNextTier = tierIndex == currentTierIndex + 1,
                IsAvailable = validation.CanUpgrade && tierIndex == currentTierIndex + 1
            };
        }

        #endregion

        #region Upgrade Processing

        /// <summary>
        /// Process facility upgrade to specified tier
        /// </summary>
        public async Task<FacilityUpgradeResult> ProcessUpgradeAsync(FacilityTierSO targetTier)
        {
            var result = new FacilityUpgradeResult { Success = false };

            if (IsUpgrading)
            {
                result.ErrorMessage = "Upgrade already in progress";
                return result;
            }

            if (targetTier == null)
            {
                result.ErrorMessage = "Target tier is null";
                return result;
            }

            var validation = ValidateUpgradeRequirements(targetTier);
            if (!validation.CanUpgrade)
            {
                result.ErrorMessage = $"Upgrade requirements not met: {validation.GetFailureSummary()}";
                OnUpgradeRequirementsNotMet?.Invoke(result.ErrorMessage);
                return result;
            }

            IsUpgrading = true;
            OnUpgradeStarted?.Invoke(targetTier);

            try
            {
                result.StartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                LogDebug($"Starting upgrade to {targetTier.TierName}");

                // Process upgrade costs
                if (!await ProcessUpgradeCostsAsync(targetTier))
                {
                    result.ErrorMessage = "Failed to process upgrade costs";
                    return result;
                }

                // Update progression data
                UpdateProgressionForUpgrade(targetTier);

                // Create upgraded facility
                var upgradeSuccess = await CreateUpgradedFacilityAsync(targetTier);
                if (!upgradeSuccess)
                {
                    result.ErrorMessage = "Failed to create upgraded facility";
                    return result;
                }

                result.EndTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                result.Success = true;
                result.UpgradeTime = "0 seconds"; // Placeholder for timing calculation
                result.NewTier = targetTier;

                LogDebug($"Successfully upgraded to {targetTier.TierName} in {result.UpgradeTime}");
                OnUpgradeCompleted?.Invoke(targetTier, true);

                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Upgrade failed: {ex.Message}";
                LogError($"Upgrade to {targetTier.TierName} failed: {ex}");
                OnUpgradeError?.Invoke(result.ErrorMessage);
                OnUpgradeCompleted?.Invoke(targetTier, false);
                return result;
            }
            finally
            {
                IsUpgrading = false;
            }
        }

        /// <summary>
        /// Process upgrade costs using currency manager or fallback system
        /// </summary>
        private async Task<bool> ProcessUpgradeCostsAsync(FacilityTierSO targetTier)
        {
            var requirements = targetTier.GetUpgradeRequirements(_progressionData);

            // Try to get CurrencyManager for proper currency handling
            try
            {
                // var currencyManager = GameManager.Instance?.GetManager<ProjectChimera.Systems.Economy.CurrencyManager>();
                // TODO: Replace with proper currency manager when Economy system is re-implemented
                var currencyManager = (object)null;
                if (currencyManager != null)
                {
                    // bool success = currencyManager.SpendCurrency(
                    //     CurrencyType.Cash,
                    //     requirements.RequiredCapital,
                    //     $"Facility upgrade to {targetTier.TierName}",
                    //     TransactionCategory.Facilities
                    // );
                    // TODO: Implement proper currency spending when Economy system is ready
                    bool success = true;

                    if (success)
                    {
                        LogDebug($"CurrencyManager processed upgrade cost: ${requirements.RequiredCapital:F0}");
                        return true;
                    }
                    else
                    {
                        LogError($"CurrencyManager rejected upgrade cost: ${requirements.RequiredCapital:F0}");
                        return false;
                    }
                }
                else
                {
                    LogDebug("CurrencyManager not available, using fallback capital system");
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to access CurrencyManager: {ex.Message}");
            }

            // Fallback to internal capital system
            if (_progressionData.Capital < requirements.RequiredCapital)
            {
                LogError($"Insufficient capital: Need ${requirements.RequiredCapital:F0}, have ${_progressionData.Capital:F0}");
                return false;
            }

            _progressionData.Capital -= requirements.RequiredCapital;
            LogDebug($"Processed upgrade costs: -${requirements.RequiredCapital:F0} (remaining: ${_progressionData.Capital:F0})");

            return true;
        }

        /// <summary>
        /// Update progression data for successful upgrade
        /// </summary>
        private void UpdateProgressionForUpgrade(FacilityTierSO targetTier)
        {
            var tierIndex = GetTierIndex(targetTier);
            _progressionData.UnlockedTiers = Mathf.Max(_progressionData.UnlockedTiers, tierIndex + 1);
            _progressionData.TotalUpgrades++;

            LogDebug($"Updated progression: {_progressionData.UnlockedTiers} tiers unlocked, {_progressionData.TotalUpgrades} total upgrades");
        }

        /// <summary>
        /// Create upgraded facility in registry
        /// </summary>
        private async Task<bool> CreateUpgradedFacilityAsync(FacilityTierSO targetTier)
        {
            try
            {
                var facilityId = System.Guid.NewGuid().ToString();
                var requirements = targetTier.GetUpgradeRequirements(_progressionData);

                var upgradedFacility = new OwnedFacility
                {
                    FacilityId = facilityId,
                    Tier = targetTier,
                    FacilityName = targetTier.TierName,
                    SceneName = targetTier.SceneName,
                    PurchaseDate = System.DateTime.Now,
                    IsActive = true,
                    TotalInvestment = requirements.RequiredCapital,
                    CurrentValue = requirements.RequiredCapital,
                    MaintenanceLevel = 1.0f,
                    LastMaintenance = System.DateTime.Now,
                    TotalPlantsGrown = 0,
                    TotalRevenue = 0f,
                    AverageYield = 0f,
                    IsOperational = true,
                    Notes = "Upgraded facility"
                };

                var success = _facilityRegistry.RegisterFacility(upgradedFacility);
                if (success)
                {
                    _facilityRegistry.SetCurrentFacility(facilityId);
                    LogDebug($"Created upgraded facility: {targetTier.TierName}");
                }

                return success;
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to create upgraded facility: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Facility Purchase System

        /// <summary>
        /// Purchase new facility of specified tier
        /// </summary>
        public async Task<FacilityPurchaseResult> PurchaseNewFacilityAsync(FacilityTierSO tier, string facilityName = null)
        {
            var result = new FacilityPurchaseResult { Success = false };

            if (tier == null)
            {
                result.ErrorMessage = "Cannot purchase null tier";
                return result;
            }

            var tierIndex = GetTierIndex(tier);
            if (tierIndex > _progressionData.UnlockedTiers - 1)
            {
                result.ErrorMessage = $"Tier {tier.TierName} not yet unlocked";
                return result;
            }

            var requirements = tier.GetUpgradeRequirements(_progressionData);

            // Validate affordability
            if (!await ValidateAffordabilityAsync(requirements.RequiredCapital))
            {
                result.ErrorMessage = $"Insufficient capital: Need ${requirements.RequiredCapital:F0}";
                return result;
            }

            try
            {
                result.StartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Process payment
                if (!await ProcessPurchasePaymentAsync(requirements.RequiredCapital, tier))
                {
                    result.ErrorMessage = "Payment processing failed";
                    return result;
                }

                // Create facility
                var facility = CreatePurchasedFacility(tier, facilityName, requirements.RequiredCapital);
                var success = _facilityRegistry.RegisterFacility(facility);

                if (!success)
                {
                    result.ErrorMessage = "Failed to register purchased facility";
                    return result;
                }

                result.EndTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                result.Success = true;
                result.PurchaseTime = "0 seconds"; // Placeholder for timing calculation
                result.PurchasedFacility = facility;

                LogDebug($"Successfully purchased {facility.FacilityName} for ${requirements.RequiredCapital:F0}");
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Purchase failed: {ex.Message}";
                LogError($"Failed to purchase {tier.TierName}: {ex}");
                return result;
            }
        }

        /// <summary>
        /// Validate if player can afford the purchase
        /// </summary>
        private async Task<bool> ValidateAffordabilityAsync(float cost)
        {
            try
            {
                // var currencyManager = GameManager.Instance?.GetManager<ProjectChimera.Systems.Economy.CurrencyManager>();
                // TODO: Replace with proper currency manager when Economy system is re-implemented
                var currencyManager = (object)null;
                if (currencyManager != null)
                {
                    // return currencyManager.GetCurrencyAmount(CurrencyType.Cash) >= cost;
                    return true; // TODO: Implement proper affordability check
                }
                else
                {
                    return _progressionData.Capital >= cost;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to validate affordability: {ex.Message}");
                return _progressionData.Capital >= cost;
            }
        }

        /// <summary>
        /// Process payment for facility purchase
        /// </summary>
        private async Task<bool> ProcessPurchasePaymentAsync(float cost, FacilityTierSO tier)
        {
            try
            {
                // var currencyManager = GameManager.Instance?.GetManager<ProjectChimera.Systems.Economy.CurrencyManager>();
                // TODO: Replace with proper currency manager when Economy system is re-implemented
                var currencyManager = (object)null;
                if (currencyManager != null)
                {
                    // return currencyManager.SpendCurrency(
                    //     CurrencyType.Cash,
                    //     cost,
                    // TODO: Implement proper currency spending when Economy system is ready
                    return true;
                }
                else
                {
                    _progressionData.Capital -= cost;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Payment processing failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create facility structure for purchase
        /// </summary>
        private OwnedFacility CreatePurchasedFacility(FacilityTierSO tier, string facilityName, float cost)
        {
            var facilityId = System.Guid.NewGuid().ToString();
            var tierCount = _facilityRegistry.GetFacilityCountForTier(tier);

            return new OwnedFacility
            {
                FacilityId = facilityId,
                Tier = tier,
                FacilityName = facilityName ?? $"{tier.TierName} #{tierCount + 1}",
                SceneName = tier.SceneName,
                PurchaseDate = System.DateTime.Now,
                IsActive = false,
                TotalInvestment = cost,
                CurrentValue = cost * 0.8f, // 20% immediate depreciation
                MaintenanceLevel = 1.0f,
                LastMaintenance = System.DateTime.Now,
                TotalPlantsGrown = 0,
                TotalRevenue = 0f,
                AverageYield = 0f,
                IsOperational = false,
                Notes = "Newly purchased facility"
            };
        }

        #endregion

        #region Tier Management

        /// <summary>
        /// Get current facility tier
        /// </summary>
        public FacilityTierSO GetCurrentTier()
        {
            var currentFacility = _facilityRegistry?.GetCurrentFacility();
            return currentFacility?.Tier;
        }

        /// <summary>
        /// Get current tier index
        /// </summary>
        public int GetCurrentTierIndex()
        {
            var currentTier = GetCurrentTier();
            return GetTierIndex(currentTier);
        }

        /// <summary>
        /// Get tier index for specific tier
        /// </summary>
        public int GetTierIndex(FacilityTierSO tier)
        {
            if (tier == null) return -1;
            return _facilityTiers.IndexOf(tier);
        }

        /// <summary>
        /// Get next tier after current
        /// </summary>
        public FacilityTierSO GetNextTier(FacilityTierSO currentTier)
        {
            var currentIndex = GetTierIndex(currentTier);
            if (currentIndex >= 0 && currentIndex < _facilityTiers.Count - 1)
            {
                return _facilityTiers[currentIndex + 1];
            }
            return null;
        }

        /// <summary>
        /// Get tier by level
        /// </summary>
        public FacilityTierSO GetTierByLevel(int tierLevel)
        {
            return _facilityTiers.FirstOrDefault(t => t.TierLevel == tierLevel);
        }

        /// <summary>
        /// Get requirements for next tier upgrade
        /// </summary>
        public FacilityUpgradeRequirements GetNextTierRequirements()
        {
            var nextTier = GetNextAvailableTier();
            return nextTier?.GetUpgradeRequirements(_progressionData) ?? FacilityUpgradeRequirements.Default;
        }

        #endregion

        #region Progression Evaluation

        /// <summary>
        /// Evaluate facility progression and trigger events
        /// </summary>
        public void EvaluateFacilityProgression()
        {
            if (CanUpgradeToNextTier())
            {
                var nextTier = GetNextAvailableTier();
                if (nextTier != null)
                {
                    LogDebug($"Facility upgrade available: {nextTier.TierName}");
                    OnUpgradeAvailable?.Invoke(nextTier);
                }
            }
        }

        /// <summary>
        /// Update progression data (called by orchestrator)
        /// </summary>
        public void UpdateProgressionData(FacilityProgressionData newData)
        {
            _progressionData = newData;
            EvaluateFacilityProgression();
        }

        #endregion

        private void LogDebug(string message)
        {
            if (_enableUpgradeLogging)
                ChimeraLogger.Log($"[FacilityUpgradeService] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[FacilityUpgradeService] {message}");
        }
    }

    /// <summary>
    /// Result of facility upgrade operation
    /// </summary>
    [System.Serializable]
    public class FacilityUpgradeResult
    {
        public bool Success;
        public string ErrorMessage;
        public string StartTime;
        public string EndTime;
        public string UpgradeTime;
        public FacilityTierSO NewTier;

        public override string ToString()
        {
            return Success ? $"Upgrade to {NewTier?.TierName} completed in {UpgradeTime}" : ErrorMessage;
        }
    }

    /// <summary>
    /// Result of facility purchase operation
    /// </summary>
    [System.Serializable]
    public class FacilityPurchaseResult
    {
        public bool Success;
        public string ErrorMessage;
        public string StartTime;
        public string EndTime;
        public string PurchaseTime;
        public OwnedFacility PurchasedFacility;

        public override string ToString()
        {
            return Success ? $"Purchased {PurchasedFacility.FacilityName} in {PurchaseTime}" : ErrorMessage;
        }
    }
}
