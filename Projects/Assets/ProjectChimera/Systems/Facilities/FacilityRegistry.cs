using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Data.Facilities;
using ProjectChimera.Systems.Scene;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Handles facility tracking, discovery, and scene-to-facility mapping.
    /// Extracted from FacilityManager for modular architecture.
    /// Manages owned facilities, scene mappings, and facility information retrieval.
    /// </summary>
    public class FacilityRegistry : MonoBehaviour
    {
        [Header("Registry Configuration")]
        [SerializeField] private bool _enableRegistryLogging = false;
        [SerializeField] private bool _validateOnUpdate = false;
        [SerializeField] private float _validationInterval = 30f;
        
        [Header("Scene Integration")]
        [SerializeField] private string _defaultFacilityScene = SceneConstants.WAREHOUSE_SMALL_BAY;
        
        // Facility storage
        private Dictionary<string, OwnedFacility> _ownedFacilities = new Dictionary<string, OwnedFacility>();
        private Dictionary<string, FacilitySceneMapping> _sceneMapping = new Dictionary<string, FacilitySceneMapping>();
        private string _currentFacilityId;
        
        // Validation tracking
        private float _lastValidationTime = 0f;
        
        // Events
        public System.Action<OwnedFacility> OnFacilityAdded;
        public System.Action<OwnedFacility> OnFacilityRemoved;
        public System.Action<string> OnCurrentFacilityChanged;
        public System.Action<string> OnRegistryValidationFailed;
        
        // Properties
        public int OwnedFacilitiesCount => _ownedFacilities.Count;
        public string CurrentFacilityId => _currentFacilityId;
        public IEnumerable<OwnedFacility> OwnedFacilities => _ownedFacilities.Values;
        public IEnumerable<string> FacilityIds => _ownedFacilities.Keys;
        
        /// <summary>
        /// Initialize the facility registry with scene mapping
        /// </summary>
        public void Initialize()
        {
            InitializeSceneMapping();
            LogDebug("Facility registry initialized");
        }
        
        /// <summary>
        /// Initialize scene mapping for facility tiers to Unity scenes
        /// </summary>
        private void InitializeSceneMapping()
        {
            LogDebug("Initializing facility scene mapping...");
            
            _sceneMapping = new Dictionary<string, FacilitySceneMapping>
            {
                { "Tier1_SmallBay", new FacilitySceneMapping 
                    { TierName = "Small Bay Facility", SceneName = SceneConstants.WAREHOUSE_SMALL_BAY, BuildIndex = SceneConstants.WAREHOUSE_SMALL_BAY_INDEX } },
                { "Tier2_MediumBay", new FacilitySceneMapping 
                    { TierName = "Medium Bay Facility", SceneName = SceneConstants.WAREHOUSE_MEDIUM_BAY, BuildIndex = SceneConstants.WAREHOUSE_MEDIUM_BAY_INDEX } },
                { "Tier3_SmallStandalone", new FacilitySceneMapping 
                    { TierName = "Small Standalone Facility", SceneName = SceneConstants.WAREHOUSE_SMALL_STANDALONE, BuildIndex = SceneConstants.WAREHOUSE_SMALL_STANDALONE_INDEX } },
                { "Tier4_LargeStandalone", new FacilitySceneMapping 
                    { TierName = "Large Standalone Facility", SceneName = SceneConstants.WAREHOUSE_LARGE_STANDALONE, BuildIndex = SceneConstants.WAREHOUSE_LARGE_STANDALONE_INDEX } },
                { "Tier5_MassiveCustom", new FacilitySceneMapping 
                    { TierName = "Massive Custom Facility", SceneName = SceneConstants.WAREHOUSE_MASSIVE_CUSTOM, BuildIndex = SceneConstants.WAREHOUSE_MASSIVE_CUSTOM_INDEX } }
            };
            
            LogDebug($"Scene mapping initialized with {_sceneMapping.Count} facility scenes");
        }
        
        #region Facility Management
        
        /// <summary>
        /// Register a new owned facility
        /// </summary>
        public bool RegisterFacility(OwnedFacility facility)
        {
            if (facility.FacilityId == null)
            {
                LogError("Cannot register facility with null ID");
                return false;
            }
            
            if (_ownedFacilities.ContainsKey(facility.FacilityId))
            {
                LogDebug($"Facility {facility.FacilityId} already registered, updating");
            }
            
            _ownedFacilities[facility.FacilityId] = facility;
            LogDebug($"Registered facility: {facility.FacilityName} ({facility.FacilityId})");
            
            OnFacilityAdded?.Invoke(facility);
            return true;
        }
        
        /// <summary>
        /// Unregister an owned facility
        /// </summary>
        public bool UnregisterFacility(string facilityId)
        {
            if (!_ownedFacilities.TryGetValue(facilityId, out var facility))
            {
                LogError($"Facility {facilityId} not found");
                return false;
            }
            
            _ownedFacilities.Remove(facilityId);
            
            // Clear current facility if it was removed
            if (_currentFacilityId == facilityId)
            {
                _currentFacilityId = null;
                OnCurrentFacilityChanged?.Invoke(null);
            }
            
            LogDebug($"Unregistered facility: {facility.FacilityName}");
            OnFacilityRemoved?.Invoke(facility);
            return true;
        }
        
        /// <summary>
        /// Set the current active facility
        /// </summary>
        public bool SetCurrentFacility(string facilityId)
        {
            if (!string.IsNullOrEmpty(facilityId) && !_ownedFacilities.ContainsKey(facilityId))
            {
                LogError($"Cannot set current facility to unregistered facility: {facilityId}");
                return false;
            }
            
            var previousId = _currentFacilityId;
            _currentFacilityId = facilityId;
            
            LogDebug($"Current facility changed: {previousId} -> {facilityId}");
            OnCurrentFacilityChanged?.Invoke(facilityId);
            return true;
        }
        
        /// <summary>
        /// Get facility by ID
        /// </summary>
        public OwnedFacility GetFacilityById(string facilityId)
        {
            _ownedFacilities.TryGetValue(facilityId, out var facility);
            return facility;
        }
        
        /// <summary>
        /// Get current facility
        /// </summary>
        public OwnedFacility? GetCurrentFacility()
        {
            if (string.IsNullOrEmpty(_currentFacilityId) || !_ownedFacilities.TryGetValue(_currentFacilityId, out var facility))
            {
                return null;
            }
            return facility;
        }
        
        /// <summary>
        /// Get all owned facilities as list
        /// </summary>
        public List<OwnedFacility> GetOwnedFacilities()
        {
            return _ownedFacilities.Values.ToList();
        }
        
        /// <summary>
        /// Get facilities by tier
        /// </summary>
        public List<OwnedFacility> GetFacilitiesForTier(FacilityTierSO tier)
        {
            return _ownedFacilities.Values.Where(f => f.Tier == tier).ToList();
        }
        
        /// <summary>
        /// Get facility count for specific tier
        /// </summary>
        public int GetFacilityCountForTier(FacilityTierSO tier)
        {
            return _ownedFacilities.Values.Count(f => f.Tier == tier);
        }
        
        /// <summary>
        /// Check if facility exists
        /// </summary>
        public bool HasFacility(string facilityId)
        {
            return !string.IsNullOrEmpty(facilityId) && _ownedFacilities.ContainsKey(facilityId);
        }
        
        /// <summary>
        /// Check if has facility of specified tier level
        /// </summary>
        public bool HasFacilityOfTier(int tierLevel)
        {
            return _ownedFacilities.Values.Any(f => f.Tier.TierLevel == tierLevel);
        }
        
        #endregion
        
        #region Scene Mapping
        
        /// <summary>
        /// Get scene name for facility tier
        /// </summary>
        public string GetSceneNameForTier(FacilityTierSO tier)
        {
            if (tier == null)
                return _defaultFacilityScene;
            
            // Try to get from tier ScriptableObject first
            if (!string.IsNullOrEmpty(tier.SceneName))
                return tier.SceneName;
            
            // Fallback to scene mapping
            var mapping = _sceneMapping.Values.FirstOrDefault(m => m.TierName.Contains(tier.TierName));
            return mapping?.SceneName ?? _defaultFacilityScene;
        }
        
        /// <summary>
        /// Get facility information for scene
        /// </summary>
        public FacilitySceneMapping GetFacilityInfoForScene(string sceneName)
        {
            return _sceneMapping.Values.FirstOrDefault(m => m.SceneName == sceneName);
        }
        
        /// <summary>
        /// Get all available facility scenes
        /// </summary>
        public List<string> GetAvailableFacilityScenes()
        {
            return _sceneMapping.Values.Select(m => m.SceneName).ToList();
        }
        
        /// <summary>
        /// Check if scene is a warehouse/facility scene
        /// </summary>
        public bool IsWarehouseScene(string sceneName)
        {
            return SceneConstants.IsWarehouseScene(sceneName);
        }
        
        /// <summary>
        /// Update current facility tracking based on loaded scene
        /// </summary>
        public void UpdateCurrentFacilityFromScene(string sceneName)
        {
            var mapping = _sceneMapping.Values.FirstOrDefault(m => m.SceneName == sceneName);
            if (mapping != null)
            {
                LogDebug($"Updated current facility context to: {mapping.TierName}");
                // Find facility that matches this scene
                var facility = _ownedFacilities.Values.FirstOrDefault(f => 
                    GetSceneNameForTier(f.Tier) == sceneName);
                
                if (facility.FacilityId != null)
                {
                    SetCurrentFacility(facility.FacilityId);
                }
            }
        }
        
        #endregion
        
        #region Portfolio Analysis
        
        /// <summary>
        /// Get total portfolio value of all owned facilities
        /// </summary>
        public float GetTotalPortfolioValue()
        {
            return _ownedFacilities.Values.Sum(f => f.CurrentValue);
        }
        
        /// <summary>
        /// Get total investment made in facilities
        /// </summary>
        public float GetTotalInvestment()
        {
            return _ownedFacilities.Values.Sum(f => f.TotalInvestment);
        }
        
        /// <summary>
        /// Get return on investment across all facilities
        /// </summary>
        public float GetPortfolioROI()
        {
            float totalInvestment = GetTotalInvestment();
            if (totalInvestment <= 0) return 0f;
            
            float totalValue = GetTotalPortfolioValue();
            return ((totalValue - totalInvestment) / totalInvestment) * 100f;
        }
        
        /// <summary>
        /// Update facility values based on market conditions
        /// </summary>
        public void UpdateFacilityValues()
        {
            var keys = new List<string>(_ownedFacilities.Keys);
            foreach (var key in keys)
            {
                var facility = _ownedFacilities[key];
                
                float ageInDays = (float)(DateTime.Now - facility.PurchaseDate).TotalDays;
                float appreciationRate = 0.001f;
                float depreciationRate = 0.002f;
                
                if (facility.IsActive && facility.MaintenanceLevel > 0.7f)
                {
                    facility.CurrentValue *= (1f + appreciationRate);
                }
                else
                {
                    facility.CurrentValue *= (1f - depreciationRate);
                }
                
                // Ensure value doesn't go below 20% of original investment
                facility.CurrentValue = Mathf.Max(facility.CurrentValue, facility.TotalInvestment * 0.2f);
                
                _ownedFacilities[key] = facility;
            }
            
            LogDebug($"Updated values for {_ownedFacilities.Count} facilities");
        }
        
        #endregion
        
        #region Facility Operations
        
        /// <summary>
        /// Rename a facility
        /// </summary>
        public bool RenameFacility(string facilityId, string newName)
        {
            if (!_ownedFacilities.TryGetValue(facilityId, out var facility))
            {
                LogError($"Facility {facilityId} not found");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(newName))
            {
                LogError("Facility name cannot be empty");
                return false;
            }
            
            facility.FacilityName = newName.Trim();
            _ownedFacilities[facilityId] = facility;
            
            LogDebug($"Renamed facility {facilityId} to '{newName}'");
            return true;
        }
        
        /// <summary>
        /// Clear all facilities (for reset/cleanup)
        /// </summary>
        public void ClearAllFacilities()
        {
            LogDebug($"Clearing {_ownedFacilities.Count} facilities");
            
            _ownedFacilities.Clear();
            _currentFacilityId = null;
            
            OnCurrentFacilityChanged?.Invoke(null);
            LogDebug("All facilities cleared");
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate registry integrity
        /// </summary>
        public FacilityRegistryValidation ValidateRegistry()
        {
            var validation = new FacilityRegistryValidation();
            
            foreach (var kvp in _ownedFacilities)
            {
                var facilityId = kvp.Key;
                var facility = kvp.Value;
                
                if (string.IsNullOrEmpty(facility.FacilityId))
                {
                    validation.AddError($"Facility {facilityId} has null/empty FacilityId");
                }
                else if (facility.FacilityId != facilityId)
                {
                    validation.AddError($"Facility ID mismatch: key={facilityId}, facility.Id={facility.FacilityId}");
                }
                
                if (facility.Tier == null)
                {
                    validation.AddError($"Facility {facilityId} has null Tier");
                }
                
                if (string.IsNullOrEmpty(facility.FacilityName))
                {
                    validation.AddWarning($"Facility {facilityId} has no name");
                }
                
                validation.ValidatedFacilities++;
            }
            
            // Validate current facility reference
            if (!string.IsNullOrEmpty(_currentFacilityId) && !_ownedFacilities.ContainsKey(_currentFacilityId))
            {
                validation.AddError($"Current facility ID {_currentFacilityId} not found in registry");
            }
            
            validation.IsValid = validation.Errors.Count == 0;
            return validation;
        }
        
        private void Update()
        {
            if (!_validateOnUpdate) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastValidationTime >= _validationInterval)
            {
                var validation = ValidateRegistry();
                if (!validation.IsValid)
                {
                    LogError($"Registry validation failed: {validation.Errors.Count} errors, {validation.Warnings.Count} warnings");
                    OnRegistryValidationFailed?.Invoke(validation.GetErrorsSummary());
                }
                
                _lastValidationTime = currentTime;
            }
        }
        
        #endregion
        
        private void LogDebug(string message)
        {
            if (_enableRegistryLogging)
                Debug.Log($"[FacilityRegistry] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[FacilityRegistry] {message}");
        }
        
        private void OnDestroy()
        {
            ClearAllFacilities();
        }
    }
    
    /// <summary>
    /// Result of facility registry validation
    /// </summary>
    [System.Serializable]
    public class FacilityRegistryValidation
    {
        public bool IsValid { get; set; }
        public int ValidatedFacilities { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        public string GetErrorsSummary()
        {
            return string.Join("\n", Errors);
        }
        
        public string GetWarningsSummary()
        {
            return string.Join("\n", Warnings);
        }
    }
}