using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectChimera.Data.Facilities;
using ProjectChimera.Systems.Scene;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Manages facility state transitions, scene loading, and switching operations.
    /// Extracted from FacilityManager for modular architecture.
    /// Handles facility activation, deactivation, and scene-based operations.
    /// </summary>
    public class FacilityStateManager : MonoBehaviour, ITickable
    {
        [Header("State Management Configuration")]
        [SerializeField] private bool _enableStateLogging = true;
        [SerializeField] private float _sceneTransitionTimeout = 15f;
        [SerializeField] private bool _preloadNextTierScene = true;
        [SerializeField] private float _stateUpdateInterval = 1f;

        // Dependencies (injected by orchestrator)
        private FacilityRegistry _facilityRegistry;
        private ISceneLoader _sceneLoader;

        // State tracking
        private bool _isLoadingScene = false;
        private float _lastStateUpdate = 0f;
        private Dictionary<string, FacilityState> _facilityStates = new Dictionary<string, FacilityState>();

        // Events
        public System.Action<string> OnSceneLoadStarted;
        public System.Action<string> OnSceneLoadCompleted;
        public System.Action<string> OnSceneLoadFailed;
        public System.Action<string, string> OnFacilitySwitch;
        public System.Action<string> OnFacilitySwitchFailed;
        public System.Action<string, FacilityState> OnFacilityStateChanged;

        // Properties
        public bool IsLoadingScene => _isLoadingScene;
        public int ActiveFacilitiesCount => GetActiveFacilitiesCount();
        public IEnumerable<string> ActiveFacilityIds => GetActiveFacilityIds();

        /// <summary>
        /// Initialize the state manager with dependencies
        /// </summary>
        public void Initialize(FacilityRegistry facilityRegistry, ISceneLoader sceneLoader)
        {
            _facilityRegistry = facilityRegistry;
            _sceneLoader = sceneLoader;

            SubscribeToSceneLoaderEvents();
            SubscribeToRegistryEvents();

            LogDebug("Facility state manager initialized");
        }

        /// <summary>
        /// Subscribe to scene loader events
        /// </summary>
        private void SubscribeToSceneLoaderEvents()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.OnSceneLoadStarted += HandleSceneLoadStarted;
                _sceneLoader.OnSceneLoadCompleted += HandleSceneLoadCompleted;
                _sceneLoader.OnSceneTransitionStarted += HandleSceneTransitionStarted;
                _sceneLoader.OnSceneTransitionCompleted += HandleSceneTransitionCompleted;

                LogDebug("Subscribed to SceneLoader events");
            }
            else
            {
                LogError("Cannot subscribe to SceneLoader events - service not available");
            }
        }

        /// <summary>
        /// Subscribe to facility registry events
        /// </summary>
        private void SubscribeToRegistryEvents()
        {
            if (_facilityRegistry != null)
            {
                _facilityRegistry.OnFacilityAdded += HandleFacilityAdded;
                _facilityRegistry.OnFacilityRemoved += HandleFacilityRemoved;
                _facilityRegistry.OnCurrentFacilityChanged += HandleCurrentFacilityChanged;

                LogDebug("Subscribed to FacilityRegistry events");
            }
        }

        #region Scene Management

        /// <summary>
        /// Load facility scene using SceneLoader service
        /// </summary>
        public async Task<bool> LoadFacilitySceneAsync(FacilityTierSO tier)
        {
            if (_sceneLoader == null)
            {
                LogError("SceneLoader service not available");
                return false;
            }

            if (_isLoadingScene)
            {
                LogError("Scene already loading");
                return false;
            }

            string sceneName = _facilityRegistry.GetSceneNameForTier(tier);
            if (string.IsNullOrEmpty(sceneName))
            {
                LogError($"No scene mapping found for tier {tier.TierName}");
                return false;
            }

            LogDebug($"Loading facility scene: {sceneName}");

            try
            {
                _isLoadingScene = true;
                OnSceneLoadStarted?.Invoke(sceneName);

                // Use SceneLoader for smooth transition
                _sceneLoader.TransitionToScene(sceneName);

                // Wait for scene transition to complete
                float timeoutTime = Time.time + _sceneTransitionTimeout;
                while (_isLoadingScene && Time.time < timeoutTime)
                {
                    await Task.Yield();
                }

                if (_isLoadingScene)
                {
                    LogError($"Scene transition timeout for {sceneName}");
                    _isLoadingScene = false;
                    OnSceneLoadFailed?.Invoke(sceneName);
                    return false;
                }

                LogDebug($"Successfully loaded facility scene: {sceneName}");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to load facility scene {sceneName}: {ex.Message}");
                _isLoadingScene = false;
                OnSceneLoadFailed?.Invoke(sceneName);
                return false;
            }
        }

        /// <summary>
        /// Load facility scene by name
        /// </summary>
        public async Task<bool> LoadFacilitySceneByNameAsync(string sceneName)
        {
            if (!_facilityRegistry.IsWarehouseScene(sceneName))
            {
                LogError($"Scene {sceneName} is not a valid warehouse scene");
                return false;
            }

            LogDebug($"Loading facility scene by name: {sceneName}");

            if (_sceneLoader != null)
            {
                _sceneLoader.TransitionToScene(sceneName);
                return await WaitForSceneLoadCompletion(sceneName);
            }

            return false;
        }

        /// <summary>
        /// Wait for scene load completion
        /// </summary>
        private async Task<bool> WaitForSceneLoadCompletion(string sceneName)
        {
            float timeoutTime = Time.time + _sceneTransitionTimeout;
            while (_isLoadingScene && Time.time < timeoutTime)
            {
                await Task.Yield();
            }

            return !_isLoadingScene;
        }

        #endregion

        #region Facility Switching

        /// <summary>
        /// Switch to specified facility with comprehensive result tracking
        /// </summary>
        public async Task<FacilitySwitchResult> SwitchToFacilityAsync(string facilityId)
        {
            var result = new FacilitySwitchResult { Success = false };

            if (string.IsNullOrEmpty(facilityId))
            {
                result.ErrorMessage = "Facility ID cannot be null or empty";
                return result;
            }

            var facility = _facilityRegistry.GetFacilityById(facilityId);
            if (facility.FacilityId == null)
            {
                result.ErrorMessage = $"Facility {facilityId} not found";
                return result;
            }

            var currentFacilityId = _facilityRegistry.CurrentFacilityId;
            if (facilityId == currentFacilityId)
            {
                result.ErrorMessage = "Already at this facility";
                result.Success = true; // Not really an error
                return result;
            }

            if (!facility.IsOperational)
            {
                result.ErrorMessage = "Facility is not operational";
                return result;
            }

            try
            {
                result.StartTime = System.DateTime.Now;
                result.PreviousFacilityId = currentFacilityId;
                result.TargetFacilityId = facilityId;

                LogDebug($"Switching to facility: {facility.FacilityName}");

                // Update facility states
                await DeactivateCurrentFacilityAsync();

                // Load new facility scene
                var sceneLoadSuccess = await LoadFacilitySceneAsync(facility.Tier);
                if (!sceneLoadSuccess)
                {
                    result.ErrorMessage = "Failed to load facility scene";
                    OnFacilitySwitchFailed?.Invoke(facilityId);
                    return result;
                }

                // Activate new facility
                await ActivateFacilityAsync(facilityId);

                // Update registry
                _facilityRegistry.SetCurrentFacility(facilityId);

                result.EndTime = System.DateTime.Now;
                result.Success = true;
                result.ActualSwitchTime = System.DateTime.Now;
                result.SuccessMessage = $"Successfully switched to {facility.FacilityName}";

                LogDebug($"Facility switch completed at {result.ActualSwitchTime}");
                OnFacilitySwitch?.Invoke(currentFacilityId, facilityId);

                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Failed to switch facilities: {ex.Message}";
                LogError($"Facility switch failed: {ex}");
                OnFacilitySwitchFailed?.Invoke(facilityId);
                return result;
            }
        }

        /// <summary>
        /// Get facilities available for switching
        /// </summary>
        public List<FacilitySwitchInfo> GetAvailableFacilitiesForSwitching()
        {
            var facilities = new List<FacilitySwitchInfo>();
            var currentFacilityId = _facilityRegistry.CurrentFacilityId;

            foreach (var facility in _facilityRegistry.OwnedFacilities)
            {
                var switchInfo = new FacilitySwitchInfo
                {
                    Facility = facility.Tier, // Use the tier directly
                    OwnedFacilityData = facility, // Store the full owned facility data
                    IsCurrentFacility = facility.FacilityId == currentFacilityId,
                    CanSwitch = facility.FacilityId != currentFacilityId && facility.IsOperational,
                    SwitchCost = $"${CalculateFacilitySwitchCost(facility):F0}",
                    EstimatedLoadTime = GetEstimatedSceneLoadTime(facility.SceneName),
                    StatusMessage = GetFacilitySwitchStatusMessage(facility)
                };

                facilities.Add(switchInfo);
            }

            return facilities.OrderBy(f => f.IsCurrentFacility ? 0 : 1)
                          .ThenBy(f => f.Facility.TierLevel)
                          .ToList();
        }

        /// <summary>
        /// Calculate cost to switch to facility
        /// </summary>
        private float CalculateFacilitySwitchCost(OwnedFacility facility)
        {
            // For now, no cost for switching between owned facilities
            // Future implementation might include travel costs, setup costs, etc.
            return 0f;
        }

        /// <summary>
        /// Get estimated scene load time
        /// </summary>
        private float GetEstimatedSceneLoadTime(string sceneName)
        {
            var mapping = _facilityRegistry.GetFacilityInfoForScene(sceneName);
            return mapping?.LoadingEstimateSeconds ?? 3f;
        }

        /// <summary>
        /// Get status message for facility switching
        /// </summary>
        private string GetFacilitySwitchStatusMessage(OwnedFacility facility)
        {
            if (facility.FacilityId == _facilityRegistry.CurrentFacilityId)
                return "Current Location";

            if (!facility.IsOperational)
                return "Not Operational";

            if (facility.MaintenanceLevel < 0.3f)
                return "Needs Maintenance";

            if (facility.IsActive)
                return "Ready";

            return "Available";
        }

        #endregion

        #region Facility State Management

        /// <summary>
        /// Activate facility
        /// </summary>
        public async Task<bool> ActivateFacilityAsync(string facilityId)
        {
            var facility = _facilityRegistry.GetFacilityById(facilityId);
            if (facility.FacilityId == null)
            {
                LogError($"Cannot activate facility {facilityId} - not found");
                return false;
            }

            try
            {
                // Update facility to active state
                facility.IsActive = true;
                facility.IsOperational = true;
                _facilityRegistry.RegisterFacility(facility); // Update in registry

                // Update state tracking
                SetFacilityState(facilityId, FacilityState.Active);

                LogDebug($"Activated facility: {facility.FacilityName}");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to activate facility {facilityId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deactivate current facility
        /// </summary>
        public async Task<bool> DeactivateCurrentFacilityAsync()
        {
            var currentFacilityId = _facilityRegistry.CurrentFacilityId;
            if (string.IsNullOrEmpty(currentFacilityId))
            {
                return true; // Nothing to deactivate
            }

            return await DeactivateFacilityAsync(currentFacilityId);
        }

        /// <summary>
        /// Deactivate facility
        /// </summary>
        public async Task<bool> DeactivateFacilityAsync(string facilityId)
        {
            var facility = _facilityRegistry.GetFacilityById(facilityId);
            if (facility.FacilityId == null)
            {
                LogError($"Cannot deactivate facility {facilityId} - not found");
                return false;
            }

            try
            {
                // Update facility to inactive state
                facility.IsActive = false;
                _facilityRegistry.RegisterFacility(facility); // Update in registry

                // Update state tracking
                SetFacilityState(facilityId, FacilityState.Inactive);

                LogDebug($"Deactivated facility: {facility.FacilityName}");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to deactivate facility {facilityId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set facility state
        /// </summary>
        public void SetFacilityState(string facilityId, FacilityState state)
        {
            var previousState = GetFacilityState(facilityId);
            _facilityStates[facilityId] = state;

            if (previousState != state)
            {
                LogDebug($"Facility {facilityId} state changed: {previousState} -> {state}");
                OnFacilityStateChanged?.Invoke(facilityId, state);
            }
        }

        /// <summary>
        /// Get facility state
        /// </summary>
        public FacilityState GetFacilityState(string facilityId)
        {
            return _facilityStates.TryGetValue(facilityId, out var state) ? state : FacilityState.Inactive;
        }

        /// <summary>
        /// Update facility operational status
        /// </summary>
        public void UpdateFacilityOperationalStatus(string facilityId, bool isOperational)
        {
            var facility = _facilityRegistry.GetFacilityById(facilityId);
            if (facility.FacilityId != null)
            {
                facility.IsOperational = isOperational;
                _facilityRegistry.RegisterFacility(facility);

                var newState = isOperational ? FacilityState.Active : FacilityState.NonOperational;
                SetFacilityState(facilityId, newState);

                LogDebug($"Updated operational status for {facility.FacilityName}: {isOperational}");
            }
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Get count of active facilities
        /// </summary>
        private int GetActiveFacilitiesCount()
        {
            return _facilityStates.Values.Count(s => s == FacilityState.Active);
        }

        /// <summary>
        /// Get active facility IDs
        /// </summary>
        private IEnumerable<string> GetActiveFacilityIds()
        {
            return _facilityStates.Where(kvp => kvp.Value == FacilityState.Active)
                                 .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Get facility display info for current facility
        /// </summary>
        public FacilityDisplayInfo GetCurrentFacilityDisplayInfo()
        {
            var currentFacility = _facilityRegistry.GetCurrentFacility();
            if (!currentFacility.HasValue)
            {
                return new FacilityDisplayInfo
                {
                    FacilityName = "No Facility",
                    TierName = "Unknown",
                    StatusText = "No facility selected",
                    IsOperational = false
                };
            }

            var facility = currentFacility.Value;
            return new FacilityDisplayInfo
            {
                FacilityName = facility.FacilityName,
                TierName = facility.Tier?.TierName ?? "Unknown",
                StatusText = GetFacilitySwitchStatusMessage(facility),
                IsOperational = facility.IsOperational,
                MaintenanceLevel = facility.MaintenanceLevel,
                TotalRevenue = facility.TotalRevenue,
                TotalPlants = facility.TotalPlantsGrown,
                PurchaseDate = facility.PurchaseDate.ToString("yyyy-MM-dd"),
                CurrentValue = facility.CurrentValue
            };
        }

        #endregion

        #region Event Handlers

        private void HandleSceneLoadStarted(string sceneName)
        {
            if (_facilityRegistry.IsWarehouseScene(sceneName))
            {
                LogDebug($"Facility scene load started: {sceneName}");
                _isLoadingScene = true;
                OnSceneLoadStarted?.Invoke(sceneName);
            }
        }

        private void HandleSceneLoadCompleted(string sceneName)
        {
            if (_facilityRegistry.IsWarehouseScene(sceneName))
            {
                LogDebug($"Facility scene load completed: {sceneName}");
                _isLoadingScene = false;
                OnSceneLoadCompleted?.Invoke(sceneName);

                // Update current facility from scene
                _facilityRegistry.UpdateCurrentFacilityFromScene(sceneName);
            }
        }

        private void HandleSceneTransitionStarted(string fromScene, string targetSceneName)
        {
            if (_facilityRegistry.IsWarehouseScene(targetSceneName))
            {
                LogDebug($"Facility transition started: {fromScene} -> {targetSceneName}");
                _isLoadingScene = true;
            }
        }

        private void HandleSceneTransitionCompleted(string targetSceneName)
        {
            if (_facilityRegistry.IsWarehouseScene(targetSceneName))
            {
                LogDebug($"Facility transition completed: {targetSceneName}");
                _isLoadingScene = false;
            }
        }

        private void HandleFacilityAdded(OwnedFacility facility)
        {
            SetFacilityState(facility.FacilityId, FacilityState.Inactive);
            LogDebug($"Added facility to state tracking: {facility.FacilityName}");
        }

        private void HandleFacilityRemoved(OwnedFacility facility)
        {
            _facilityStates.Remove(facility.FacilityId);
            LogDebug($"Removed facility from state tracking: {facility.FacilityName}");
        }

        private void HandleCurrentFacilityChanged(string facilityId)
        {
            LogDebug($"Current facility changed to: {facilityId}");
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            // Unsubscribe from events
            if (_sceneLoader != null)
            {
                _sceneLoader.OnSceneLoadStarted -= HandleSceneLoadStarted;
                _sceneLoader.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
                _sceneLoader.OnSceneTransitionStarted -= HandleSceneTransitionStarted;
                _sceneLoader.OnSceneTransitionCompleted -= HandleSceneTransitionCompleted;
            }

            if (_facilityRegistry != null)
            {
                _facilityRegistry.OnFacilityAdded -= HandleFacilityAdded;
                _facilityRegistry.OnFacilityRemoved -= HandleFacilityRemoved;
                _facilityRegistry.OnCurrentFacilityChanged -= HandleCurrentFacilityChanged;
            }
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            float currentTime = Time.time;
            if (currentTime - _lastStateUpdate >= _stateUpdateInterval)
            {
                UpdateFacilityStates();
                _lastStateUpdate = currentTime;
            }
        }

        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            // Override in derived classes if needed
        }

        public virtual void OnUnregistered()
        {
            // Override in derived classes if needed
        }

        #endregion

        #region State Management

        /// <summary>
        /// Update facility states based on current conditions
        /// </summary>
        private void UpdateFacilityStates()
        {
            foreach (var facility in _facilityRegistry.OwnedFacilities)
            {
                var currentState = GetFacilityState(facility.FacilityId);
                var expectedState = DetermineFacilityState(facility);

                if (currentState != expectedState)
                {
                    SetFacilityState(facility.FacilityId, expectedState);
                }
            }
        }

        /// <summary>
        /// Determine expected facility state based on conditions
        /// </summary>
        private FacilityState DetermineFacilityState(OwnedFacility facility)
        {
            if (!facility.IsOperational)
                return FacilityState.NonOperational;

            if (facility.MaintenanceLevel < 0.1f)
                return FacilityState.Maintenance;

            if (facility.IsActive)
                return FacilityState.Active;

            return FacilityState.Inactive;
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableStateLogging)
                ChimeraLogger.Log($"[FacilityStateManager] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[FacilityStateManager] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Facility operational states
    /// </summary>
    public enum FacilityState
    {
        Inactive,
        Active,
        Loading,
        NonOperational,
        Maintenance,
        Switching
    }
}
