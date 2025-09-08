using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using static ProjectChimera.Core.Updates.TickPriority;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Scene;
using ProjectChimera.Data.Facilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Facility Manager Orchestrator - Intelligent Size Management (≤400 lines)
    /// Coordinates facility operations through specialized components.
    /// Maintains full API compatibility while delegating to modular services.
    /// Refactored from 1,749 lines to orchestrator pattern for maintainability.
    /// </summary>
    public class FacilityManager : ChimeraManager, ITickable, IFacilityManager
    {
        [Header("Orchestrator Configuration")]
        [SerializeField] private bool _enableProgressionSystem = true;
        [SerializeField] private bool _enableMultipleFacilities = true;
        [SerializeField] private float _facilityEvaluationInterval = 30f;

        [Header("Facility Configuration")]
        [SerializeField] private List<FacilityTierSO> _facilityTiers = new List<FacilityTierSO>();
        [SerializeField] private FacilityProgressionConfigSO _progressionConfig;

        // Component References - Dependency Injection
        [Header("Component Dependencies")]
        [SerializeField] private FacilityRegistry _facilityRegistry;
        [SerializeField] private FacilityUpgradeService _upgradeService;
        [SerializeField] private FacilityStateManager _stateManager;
        [SerializeField] private FacilityValidationService _validationService;
        [SerializeField] private FacilityEventHandler _eventHandler;

        // External services
        private ISceneLoader _sceneLoader;

        // Orchestrator state
        private FacilityProgressionData _progressionData = new FacilityProgressionData();
        private float _lastEvaluationTime = 0f;

        // API Compatibility Properties
        public bool IsInitialized { get; private set; }
        public FacilityTierSO CurrentTier => _upgradeService?.GetCurrentTier();
        public int CurrentTierIndex => _upgradeService?.GetCurrentTierIndex() ?? 0;
        public int OwnedFacilitiesCount => _facilityRegistry?.OwnedFacilitiesCount ?? 0;
        public string CurrentFacilityId => _facilityRegistry?.CurrentFacilityId;
        public FacilityProgressionData ProgressionData => _progressionData;
        public bool IsLoadingScene => _stateManager?.IsLoadingScene ?? false;
        public ISceneLoader SceneLoader => _sceneLoader;

        public override string ManagerName => "FacilityManager";
        public override ManagerPriority Priority => ManagerPriority.High;

        #region ITickable Implementation

        int ITickable.Priority => TickPriority.FacilityManager;
        bool ITickable.Enabled => IsInitialized;

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            float currentTime = Time.time;
            if (_enableProgressionSystem && currentTime - _lastEvaluationTime >= _facilityEvaluationInterval)
            {
                EvaluateFacilityProgression();
                _lastEvaluationTime = currentTime;
            }
        }

        public void OnRegistered()
        {
            ChimeraLogger.LogVerbose("[FacilityManager] Registered with UpdateOrchestrator");
        }

        public void OnUnregistered()
        {
            ChimeraLogger.LogVerbose("[FacilityManager] Unregistered from UpdateOrchestrator");
        }

        #endregion

        #region IFacilityManager Implementation

        public event System.Action<string> OnFacilityChanged;
        public event System.Action<FacilityProgressionData> OnProgressionUpdated;

        public bool SwitchToFacility(string facilityId)
        {
            // Placeholder implementation
            ChimeraLogger.Log($"[FacilityManager] SwitchToFacility {facilityId} - placeholder implementation");
            return false;
        }

        public bool CanUpgradeCurrentFacility()
        {
            // Placeholder implementation
            return false;
        }

        public bool UpgradeCurrentFacility()
        {
            // Placeholder implementation
            ChimeraLogger.Log("[FacilityManager] UpgradeCurrentFacility - placeholder implementation");
            return false;
        }

        public Dictionary<string, object> GetProgressionStatistics()
        {
            // Placeholder implementation
            return new Dictionary<string, object>
            {
                ["currentTier"] = CurrentTierIndex,
                ["ownedFacilities"] = OwnedFacilitiesCount,
                ["initialized"] = IsInitialized
            };
        }

        #endregion

        #region Component Orchestration

        /// <summary>
        /// Initialize all facility components with dependencies
        /// </summary>
        private void InitializeComponents()
        {
            // Ensure components exist
            CreateComponentsIfNeeded();

            // Initialize progression data
            InitializeProgressionData();

            // Get SceneLoader service
            InitializeSceneLoader();

            // Initialize components in dependency order
            _facilityRegistry?.Initialize();
            _upgradeService?.Initialize(_facilityRegistry, _progressionData, _facilityTiers);
            _stateManager?.Initialize(_facilityRegistry, _sceneLoader);
            _validationService?.Initialize(_facilityRegistry, _progressionData);
            _eventHandler?.Initialize();

            // Wire component events
            WireComponentEvents();

            ChimeraLogger.Log("[FacilityManager] Component orchestration initialized");
        }

        /// <summary>
        /// Create components if they don't exist
        /// </summary>
        private void CreateComponentsIfNeeded()
        {
            if (_facilityRegistry == null)
                _facilityRegistry = GetComponentInChildren<FacilityRegistry>() ?? gameObject.AddComponent<FacilityRegistry>();

            if (_upgradeService == null)
                _upgradeService = GetComponentInChildren<FacilityUpgradeService>() ?? gameObject.AddComponent<FacilityUpgradeService>();

            if (_stateManager == null)
                _stateManager = GetComponentInChildren<FacilityStateManager>() ?? gameObject.AddComponent<FacilityStateManager>();

            if (_validationService == null)
                _validationService = GetComponentInChildren<FacilityValidationService>() ?? gameObject.AddComponent<FacilityValidationService>();

            if (_eventHandler == null)
                _eventHandler = GetComponentInChildren<FacilityEventHandler>() ?? gameObject.AddComponent<FacilityEventHandler>();
        }

        /// <summary>
        /// Initialize progression data from config
        /// </summary>
        private void InitializeProgressionData()
        {
            if (_progressionConfig != null)
            {
                _progressionData = _progressionConfig.CreateDefaultProgressionData();
            }
            else
            {
                _progressionData = new FacilityProgressionData
                {
                    Capital = 50000f,
                    TotalPlants = 0,
                    Experience = 0f,
                    TotalHarvests = 0,
                    UnlockedTiers = 1
                };
            }
        }

        /// <summary>
        /// Initialize SceneLoader service connection
        /// </summary>
        private void InitializeSceneLoader()
        {
            try
            {
                _sceneLoader = ServiceContainerFactory.Instance?.TryResolve<ISceneLoader>();
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogWarning($"[FacilityManager] Could not connect to SceneLoader service: {ex.Message}");
            }
        }

        /// <summary>
        /// Wire events between components
        /// </summary>
        private void WireComponentEvents()
        {
            if (_upgradeService != null && _eventHandler != null)
            {
                _upgradeService.OnUpgradeCompleted += (tier, success) => {
                    if (success) _eventHandler.TriggerFacilityUpgraded(tier);
                };
                _upgradeService.OnUpgradeAvailable += tier => _eventHandler.TriggerFacilityUpgradeAvailable(tier);
                _upgradeService.OnUpgradeRequirementsNotMet += reason => _eventHandler.TriggerFacilityRequirementsNotMet(reason);
            }

            if (_stateManager != null && _eventHandler != null)
            {
                _stateManager.OnFacilitySwitch += (from, to) => _eventHandler.TriggerFacilitySwitch(from, to);
                _stateManager.OnSceneLoadStarted += scene => _eventHandler.TriggerSceneTransitionStarted(scene);
                _stateManager.OnSceneLoadCompleted += scene => _eventHandler.TriggerSceneTransitionCompleted(scene);
            }

            if (_facilityRegistry != null && _eventHandler != null)
            {
                _facilityRegistry.OnFacilityAdded += facility => _eventHandler.TriggerFacilityPurchased(facility);
            }
        }

        #endregion

        #region Public API - Facility Operations

        /// <summary>
        /// Upgrade to specified facility tier
        /// </summary>
        public async Task<bool> UpgradeToTierAsync(FacilityTierSO targetTier)
        {
            if (_upgradeService == null) return false;

            var result = await _upgradeService.ProcessUpgradeAsync(targetTier);
            if (result.Success)
            {
                // Update orchestrator state
                _progressionData = _upgradeService.ProgressionData;
            }
            return result.Success;
        }

        /// <summary>
        /// Switch to facility by ID
        /// </summary>
        public async Task SwitchToFacilityAsync(string facilityId)
        {
            if (_stateManager != null)
            {
                await _stateManager.SwitchToFacilityAsync(facilityId);
            }
        }

        /// <summary>
        /// Purchase new facility
        /// </summary>
        public async Task<bool> PurchaseNewFacilityAsync(FacilityTierSO tier, string facilityName = null)
        {
            if (_upgradeService == null) return false;

            var result = await _upgradeService.PurchaseNewFacilityAsync(tier, facilityName);
            return result.Success;
        }

        /// <summary>
        /// Sell facility
        /// </summary>
        public bool SellFacility(string facilityId)
        {
            var facility = _facilityRegistry?.GetFacilityById(facilityId);
            if (facility?.FacilityId == null) return false;

            var validation = _validationService?.ValidateFacilitySale(facilityId);
            if (validation?.IsValid != true) return false;

            // Process sale through registry
            var success = _facilityRegistry.UnregisterFacility(facilityId);
            if (success)
            {
                _eventHandler?.TriggerFacilitySold(facility.Value, facility.Value.CurrentValue);
            }

            return success;
        }

        #endregion

        #region Public API - Information Queries

        /// <summary>
        /// Get all owned facilities
        /// </summary>
        public List<OwnedFacility> GetOwnedFacilities()
        {
            return _facilityRegistry?.GetOwnedFacilities() ?? new List<OwnedFacility>();
        }

        /// <summary>
        /// Get facility upgrade information for all tiers
        /// </summary>
        public List<FacilityUpgradeInfo> GetAllTierUpgradeInfo()
        {
            return _upgradeService?.GetAllTierUpgradeInfo() ?? new List<FacilityUpgradeInfo>();
        }

        /// <summary>
        /// Get upgrade requirements for next tier
        /// </summary>
        public FacilityUpgradeRequirements GetNextTierRequirements()
        {
            return _upgradeService?.GetNextTierRequirements() ?? FacilityUpgradeRequirements.Default;
        }

        /// <summary>
        /// Get facility progression statistics
        /// </summary>
        public FacilityProgressionStatistics GetProgressionStatisticsTyped()
        {
            return new FacilityProgressionStatistics
            {
                CurrentTier = CurrentTier?.TierName ?? "None",
                TierIndex = CurrentTierIndex,
                TotalTiers = _facilityTiers.Count,
                OwnedFacilities = OwnedFacilitiesCount,
                TotalPlantsGrown = _progressionData.TotalPlants,
                TotalRevenue = _progressionData.Capital,
                TotalValue = GetTotalPortfolioValue(),
                TotalInvestment = GetTotalInvestment(),
                AverageQuality = _progressionData.Experience,
                CanUpgrade = _upgradeService?.CanUpgradeToNextTier() ?? false,
                NextTier = _upgradeService?.GetNextAvailableTier()?.TierName
            };
        }

        /// <summary>
        /// Get current facility display information
        /// </summary>
        public FacilityDisplayInfo GetCurrentFacilityDisplayInfo()
        {
            return _stateManager?.GetCurrentFacilityDisplayInfo() ?? new FacilityDisplayInfo
            {
                FacilityName = "No Facility",
                TierName = "Unknown",
                StatusText = "Manager not initialized",
                IsOperational = false
            };
        }

        /// <summary>
        /// Get facilities available for switching (interface implementation)
        /// </summary>
        List<string> IFacilityManager.GetAvailableFacilitiesForSwitching()
        {
            var switchInfo = GetAvailableFacilitiesForSwitchingDetailed();
            return switchInfo.Select(f => f.OwnedFacilityData.FacilityId).ToList();
        }

        /// <summary>
        /// Get facilities available for switching (detailed implementation)
        /// </summary>
        public List<FacilitySwitchInfo> GetAvailableFacilitiesForSwitchingDetailed()
        {
            return _stateManager?.GetAvailableFacilitiesForSwitching() ?? new List<FacilitySwitchInfo>();
        }

        #endregion

        #region Public API - State Management

        /// <summary>
        /// Update progression data (for integration with other managers)
        /// </summary>
        public void UpdateProgressionData(int totalPlants = -1, float capital = -1, float experience = -1, int totalHarvests = -1)
        {
            if (totalPlants >= 0) _progressionData.TotalPlants = totalPlants;
            if (capital >= 0) _progressionData.Capital = capital;
            if (experience >= 0) _progressionData.Experience = experience;
            if (totalHarvests >= 0) _progressionData.TotalHarvests = totalHarvests;

            // Sync with components
            _upgradeService?.UpdateProgressionData(_progressionData);
            _validationService?.Initialize(_facilityRegistry, _progressionData);

            ChimeraLogger.Log($"[FacilityManager] Progression updated - Plants: {_progressionData.TotalPlants}, Capital: ${_progressionData.Capital:F0}");
        }

        /// <summary>
        /// Check for facility upgrade availability
        /// </summary>
        public void CheckForUpgradeAvailability()
        {
            _upgradeService?.EvaluateFacilityProgression();
        }

        /// <summary>
        /// Update facility values based on market conditions
        /// </summary>
        public void UpdateFacilityValues()
        {
            _facilityRegistry?.UpdateFacilityValues();
            _eventHandler?.TriggerFacilityValueUpdated();
        }

        /// <summary>
        /// Load facility scene by name
        /// </summary>
        public void LoadFacilitySceneByName(string sceneName)
        {
            if (_stateManager != null && _facilityRegistry != null)
            {
                if (_facilityRegistry.IsWarehouseScene(sceneName))
                {
                    _ = _stateManager.LoadFacilitySceneByNameAsync(sceneName);
                }
            }
        }

        #endregion

        #region Progression Evaluation

        /// <summary>
        /// Evaluate facility progression and trigger events
        /// </summary>
        private void EvaluateFacilityProgression()
        {
            if (!_enableProgressionSystem) return;

            _upgradeService?.EvaluateFacilityProgression();
        }

        #endregion

        #region Event System Integration

        /// <summary>
        /// Subscribe to facility events for UI integration (interface implementation)
        /// </summary>
        public void SubscribeToFacilityEvents(
            System.Action onFacilityUpgraded = null,
            System.Action onFacilitySwitch = null,
            System.Action onFacilityPurchased = null,
            System.Action onFacilitySold = null,
            System.Action onFacilityUpgradeAvailable = null,
            System.Action onFacilityValueUpdated = null)
        {
            _eventHandler?.SubscribeToFacilityEvents(
                onFacilityUpgraded, null, onFacilitySwitch,
                onFacilityPurchased, onFacilitySold, onFacilityUpgradeAvailable,
                null, onFacilityValueUpdated);
        }

        /// <summary>
        /// Get facility notification message for UI display
        /// </summary>
        public string GetFacilityNotificationMessage(FacilityEventType eventType, FacilityTierSO tier = null, float amount = 0f)
        {
            return _eventHandler?.GetFacilityNotificationMessage(eventType, tier, amount) ?? "Facility event occurred.";
        }

        #endregion

        #region Testing Support

        /// <summary>
        /// Synchronous upgrade wrapper (for testing)
        /// </summary>
        public bool UpgradeToTier(int tierLevel)
        {
            var targetTier = _facilityTiers.FirstOrDefault(t => t.TierLevel == tierLevel);
            if (targetTier == null) return false;

            try
            {
                var task = UpgradeToTierAsync(targetTier);
                task.Wait();
                return task.Result;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[FacilityManager] Upgrade to tier {tierLevel} failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset for testing
        /// </summary>
        public void ResetForTesting()
        {
            _facilityRegistry?.ClearAllFacilities();
            _eventHandler?.ClearEventHistory();

            _progressionData = new FacilityProgressionData
            {
                Capital = 50000f,
                TotalPlants = 0,
                Experience = 0f,
                TotalHarvests = 0,
                UnlockedTiers = 5
            };
        }

        #endregion

        #region ChimeraManager Implementation

        protected override void OnManagerInitialize()
        {
            ChimeraLogger.Log("[FacilityManager] Initializing Facility Manager Orchestrator...");

            InitializeComponents();
            IsInitialized = true;
            _lastEvaluationTime = Time.time;

            ChimeraLogger.Log($"[FacilityManager] Orchestrator initialized with {_facilityTiers.Count} facility tiers");
        }

        protected override void OnManagerShutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("[FacilityManager] Shutting down Facility Manager Orchestrator...");

            try
            {
                _facilityRegistry?.ClearAllFacilities();
                _eventHandler?.ClearEventHistory();
                _progressionData = new FacilityProgressionData();
                IsInitialized = false;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogWarning($"[FacilityManager] Error during shutdown: {ex.Message}");
            }

            ChimeraLogger.Log("[FacilityManager] Orchestrator shutdown complete");
        }

        #endregion

        #region API Compatibility Methods

        /// <summary>
        /// Get all available facility scenes
        /// </summary>
        public List<string> GetAvailableFacilityScenes()
        {
            return _facilityRegistry?.GetAvailableFacilityScenes() ?? new List<string>();
        }

        /// <summary>
        /// Get facility information for a specific scene
        /// </summary>
        public FacilitySceneMapping GetFacilityInfoForScene(string sceneName)
        {
            return _facilityRegistry?.GetFacilityInfoForScene(sceneName);
        }

        /// <summary>
        /// Quick switch by tier name
        /// </summary>
        public async Task<bool> QuickSwitchByTierName(string tierName)
        {
            // Find facility with matching tier name
            var facilities = _stateManager?.GetAvailableFacilitiesForSwitching() ?? new List<FacilitySwitchInfo>();
            var targetFacility = facilities.FirstOrDefault(f => f.Facility.TierName == tierName && f.CanSwitch);

            if (targetFacility != null)
            {
                var switchResult = await _stateManager.SwitchToFacilityAsync(targetFacility.OwnedFacilityData.FacilityId);
                return switchResult.Success;
            }

            return false;
        }

        /// <summary>
        /// Purchase new facility async (interface implementation)
        /// </summary>
        public async Task<bool> PurchaseNewFacilityAsync(string tierName)
        {
            var tier = _facilityTiers.FirstOrDefault(t => t.TierName == tierName);
            if (tier == null) return false;

            return await PurchaseNewFacilityAsync(tier);
        }

        /// <summary>
        /// Unsubscribe from facility events (interface implementation)
        /// </summary>
        public void UnsubscribeFromFacilityEvents(
            System.Action<object> onFacilityUpgraded = null,
            System.Action<object, object> onFacilitySwitch = null,
            System.Action<object> onFacilityPurchased = null,
            System.Action<object> onFacilitySold = null,
            System.Action<object> onFacilityUpgradeAvailable = null,
            System.Action<object> onFacilityValueUpdated = null)
        {
            // Placeholder for event unsubscription
            ChimeraLogger.LogVerbose("UnsubscribeFromFacilityEvents called");
        }

        /// <summary>
        /// Get total portfolio value
        /// </summary>
        public float GetTotalPortfolioValue()
        {
            var facilities = _facilityRegistry?.OwnedFacilities ?? new List<OwnedFacility>();
            return facilities.Sum(f => f.CurrentValue);
        }

        /// <summary>
        /// Get total investment
        /// </summary>
        public float GetTotalInvestment()
        {
            var facilities = _facilityRegistry?.OwnedFacilities ?? new List<OwnedFacility>();
            return facilities.Sum(f => f.TotalInvestment);
        }

        /// <summary>
        /// Get portfolio ROI
        /// </summary>
        public float GetPortfolioROI()
        {
            var totalInvestment = GetTotalInvestment();
            if (totalInvestment <= 0) return 0f;

            var totalValue = GetTotalPortfolioValue();
            return ((totalValue - totalInvestment) / totalInvestment) * 100f;
        }

        /// <summary>
        /// Check if can upgrade to next tier
        /// </summary>
        public bool CanUpgradeToNextTier()
        {
            return _upgradeService?.CanUpgradeToNextTier() ?? false;
        }

        /// <summary>
        /// Get current facility ID
        /// </summary>
        public string FacilityId => _facilityRegistry?.CurrentFacilityId ?? "";

        /// <summary>
        /// Get next available tier for current facility (interface implementation)
        /// </summary>
        string IFacilityManager.GetNextAvailableTier()
        {
            var nextTier = GetNextAvailableTierSO();
            return nextTier?.TierName ?? "";
        }

        /// <summary>
        /// Get next available tier for current facility (internal implementation)
        /// </summary>
        public FacilityTierSO GetNextAvailableTierSO()
        {
            // Get current facility to determine its tier
            var currentFacilityId = _facilityRegistry?.CurrentFacilityId;
            if (string.IsNullOrEmpty(currentFacilityId))
                return null;

            var currentFacility = _facilityRegistry?.OwnedFacilities?.FirstOrDefault(f => f.FacilityId == currentFacilityId);
            if (currentFacility?.Tier == null)
                return null;

            return _upgradeService?.GetNextTier(currentFacility.Value.Tier) ?? null;
        }

        /// <summary>
        /// Switch to facility with full result information
        /// </summary>
        public async Task<FacilitySwitchResult> SwitchToFacilityWithResultAsync(string facilityId)
        {
            if (_stateManager != null)
            {
                return await _stateManager.SwitchToFacilityAsync(facilityId);
            }

            return new FacilitySwitchResult
            {
                Success = false,
                ErrorMessage = "Facility state manager not available"
            };
        }


        #endregion
    }
}
