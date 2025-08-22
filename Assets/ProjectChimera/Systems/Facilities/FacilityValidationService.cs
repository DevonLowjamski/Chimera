using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Facilities;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// Handles facility validation, constraints, and integrity checking.
    /// Extracted from FacilityManager for modular architecture.
    /// Validates facility operations, requirements, and business rules.
    /// </summary>
    public class FacilityValidationService : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private bool _enableValidationLogging = false;
        [SerializeField] private bool _strictValidation = true;
        [SerializeField] private float _validationCacheTime = 5f;
        
        // Dependencies
        private FacilityRegistry _facilityRegistry;
        private FacilityProgressionData _progressionData;
        
        // Validation cache
        private Dictionary<string, ValidationCacheEntry> _validationCache = new Dictionary<string, ValidationCacheEntry>();
        
        // Events
        public System.Action<SystemValidationResult> OnValidationCompleted;
        public System.Action<string> OnValidationError;
        public System.Action<string> OnConstraintViolation;
        
        /// <summary>
        /// Initialize validation service with dependencies
        /// </summary>
        public void Initialize(FacilityRegistry facilityRegistry, FacilityProgressionData progressionData)
        {
            _facilityRegistry = facilityRegistry;
            _progressionData = progressionData;
            LogDebug("Facility validation service initialized");
        }
        
        #region Facility Operations Validation
        
        /// <summary>
        /// Validate facility purchase operation
        /// </summary>
        public FacilityOperationValidation ValidateFacilityPurchase(FacilityTierSO tier, float cost)
        {
            var validation = new FacilityOperationValidation { OperationType = "Purchase" };
            
            if (tier == null)
            {
                validation.AddError("Cannot purchase null tier");
                return validation;
            }
            
            // Check tier unlock status
            var tierIndex = GetTierIndex(tier);
            if (tierIndex > _progressionData.UnlockedTiers - 1)
            {
                validation.AddError($"Tier {tier.TierName} not yet unlocked");
            }
            
            // Validate affordability
            if (_progressionData.Capital < cost)
            {
                validation.AddError($"Insufficient capital: Need ${cost:F0}, have ${_progressionData.Capital:F0}");
            }
            
            // Check facility limits
            if (!ValidateFacilityLimits(tier))
            {
                validation.AddError($"Maximum facilities of tier {tier.TierName} already owned");
            }
            
            validation.IsValid = validation.Errors.Count == 0;
            LogValidationResult("Purchase", validation);
            return validation;
        }
        
        /// <summary>
        /// Validate facility sale operation
        /// </summary>
        public FacilityOperationValidation ValidateFacilitySale(string facilityId)
        {
            var validation = new FacilityOperationValidation { OperationType = "Sale" };
            
            if (string.IsNullOrEmpty(facilityId))
            {
                validation.AddError("Facility ID cannot be null or empty");
                return validation;
            }
            
            var facility = _facilityRegistry.GetFacilityById(facilityId);
            if (facility.FacilityId == null)
            {
                validation.AddError($"Facility {facilityId} not found");
                return validation;
            }
            
            // Check if current facility
            if (facilityId == _facilityRegistry.CurrentFacilityId)
            {
                validation.AddError("Cannot sell currently active facility");
            }
            
            // Check minimum facility requirement
            if (_facilityRegistry.OwnedFacilitiesCount <= 1)
            {
                validation.AddError("Cannot sell last remaining facility");
            }
            
            // Check facility state constraints
            if (facility.IsActive && HasActiveOperations(facility))
            {
                validation.AddWarning("Facility has active operations that will be interrupted");
            }
            
            validation.IsValid = validation.Errors.Count == 0;
            LogValidationResult("Sale", validation);
            return validation;
        }
        
        /// <summary>
        /// Validate facility switching operation
        /// </summary>
        public FacilityOperationValidation ValidateFacilitySwitch(string targetFacilityId)
        {
            var validation = new FacilityOperationValidation { OperationType = "Switch" };
            
            if (string.IsNullOrEmpty(targetFacilityId))
            {
                validation.AddError("Target facility ID cannot be null or empty");
                return validation;
            }
            
            var facility = _facilityRegistry.GetFacilityById(targetFacilityId);
            if (facility.FacilityId == null)
            {
                validation.AddError($"Target facility {targetFacilityId} not found");
                return validation;
            }
            
            // Check if already current
            if (targetFacilityId == _facilityRegistry.CurrentFacilityId)
            {
                validation.AddWarning("Already at target facility");
                validation.IsValid = true;
                return validation;
            }
            
            // Check operational status
            if (!facility.IsOperational)
            {
                validation.AddError("Target facility is not operational");
            }
            
            // Check maintenance level
            if (facility.MaintenanceLevel < 0.1f)
            {
                validation.AddError("Target facility requires maintenance before use");
            }
            
            validation.IsValid = validation.Errors.Count == 0;
            LogValidationResult("Switch", validation);
            return validation;
        }
        
        #endregion
        
        #region Business Rules Validation
        
        /// <summary>
        /// Validate facility limits based on tier and progression
        /// </summary>
        private bool ValidateFacilityLimits(FacilityTierSO tier)
        {
            var currentCount = _facilityRegistry.GetFacilityCountForTier(tier);
            var maxAllowed = GetMaxFacilitiesForTier(tier);
            
            return currentCount < maxAllowed;
        }
        
        /// <summary>
        /// Get maximum allowed facilities for tier
        /// </summary>
        private int GetMaxFacilitiesForTier(FacilityTierSO tier)
        {
            // Business rules for facility limits
            switch (tier.TierLevel)
            {
                case 1: return 1; // Only one starter facility
                case 2: return 2; // Can have 2 small facilities
                case 3: return 3; // Can have 3 medium facilities
                case 4: return 2; // Limited large facilities
                case 5: return 1; // Only one massive facility
                default: return 1;
            }
        }
        
        /// <summary>
        /// Check if facility has active operations
        /// </summary>
        private bool HasActiveOperations(OwnedFacility facility)
        {
            // Check for active plants, ongoing processes, etc.
            return facility.TotalPlantsGrown > 0 && facility.IsActive;
        }
        
        /// <summary>
        /// Get tier index for validation
        /// </summary>
        private int GetTierIndex(FacilityTierSO tier)
        {
            // This would normally come from the upgrade service
            // For now, use tier level as index
            return tier.TierLevel - 1;
        }
        
        #endregion
        
        #region Comprehensive Validation
        
        /// <summary>
        /// Perform comprehensive facility system validation
        /// </summary>
        public SystemValidationResult ValidateFacilitySystem()
        {
            var result = new SystemValidationResult { ValidationTime = System.DateTime.Now };
            
            try
            {
                // Validate registry integrity
                var registryValidation = _facilityRegistry.ValidateRegistry();
                result.RegistryValid = registryValidation.IsValid;
                result.AddErrors(registryValidation.Errors);
                result.AddWarnings(registryValidation.Warnings);
                
                // Validate facility states
                ValidateFacilityStates(result);
                
                // Validate progression data consistency
                ValidateProgressionConsistency(result);
                
                // Validate business rules compliance
                ValidateBusinessRulesCompliance(result);
                
                result.IsValid = result.Errors.Count == 0;
                result.ValidationCompleted = System.DateTime.Now;
                
                LogDebug($"System validation completed: {result.IsValid} (Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count})");
                OnValidationCompleted?.Invoke(result);
                
                return result;
            }
            catch (System.Exception ex)
            {
                result.AddError($"System validation failed: {ex.Message}");
                OnValidationError?.Invoke(ex.Message);
                return result;
            }
        }
        
        /// <summary>
        /// Validate facility states consistency
        /// </summary>
        private void ValidateFacilityStates(SystemValidationResult result)
        {
            foreach (var facility in _facilityRegistry.OwnedFacilities)
            {
                // Check facility data integrity
                if (string.IsNullOrEmpty(facility.FacilityName))
                {
                    result.AddWarning($"Facility {facility.FacilityId} has no name");
                }
                
                // Check value consistency
                if (facility.CurrentValue < 0)
                {
                    result.AddError($"Facility {facility.FacilityName} has negative value");
                }
                
                // Check maintenance consistency
                if (facility.MaintenanceLevel < 0 || facility.MaintenanceLevel > 1)
                {
                    result.AddError($"Facility {facility.FacilityName} has invalid maintenance level");
                }
                
                // Check operational consistency
                if (facility.IsActive && !facility.IsOperational)
                {
                    result.AddWarning($"Facility {facility.FacilityName} is active but not operational");
                }
            }
        }
        
        /// <summary>
        /// Validate progression data consistency
        /// </summary>
        private void ValidateProgressionConsistency(SystemValidationResult result)
        {
            if (_progressionData.Capital < 0)
            {
                result.AddError("Progression data has negative capital");
            }
            
            if (_progressionData.TotalPlants < 0)
            {
                result.AddError("Progression data has negative plant count");
            }
            
            if (_progressionData.UnlockedTiers < 1)
            {
                result.AddError("Progression data has invalid unlocked tiers");
            }
        }
        
        /// <summary>
        /// Validate business rules compliance
        /// </summary>
        private void ValidateBusinessRulesCompliance(SystemValidationResult result)
        {
            // Check facility count limits
            var facilityGroups = _facilityRegistry.OwnedFacilities.GroupBy(f => f.Tier.TierLevel);
            foreach (var group in facilityGroups)
            {
                var tierLevel = group.Key;
                var count = group.Count();
                var maxAllowed = GetMaxFacilitiesForTier(group.First().Tier);
                
                if (count > maxAllowed)
                {
                    result.AddError($"Tier {tierLevel} facility limit exceeded: {count}/{maxAllowed}");
                    OnConstraintViolation?.Invoke($"Tier {tierLevel} limit exceeded");
                }
            }
            
            // Check current facility validity
            var currentFacilityId = _facilityRegistry.CurrentFacilityId;
            if (!string.IsNullOrEmpty(currentFacilityId))
            {
                var currentFacility = _facilityRegistry.GetFacilityById(currentFacilityId);
                if (currentFacility.FacilityId == null)
                {
                    result.AddError("Current facility ID references non-existent facility");
                }
            }
        }
        
        #endregion
        
        #region Validation Caching
        
        /// <summary>
        /// Get cached validation result if available
        /// </summary>
        public T GetCachedValidation<T>(string key) where T : class
        {
            if (_validationCache.TryGetValue(key, out var entry))
            {
                if (Time.time - entry.Timestamp < _validationCacheTime)
                {
                    return entry.Result as T;
                }
                else
                {
                    _validationCache.Remove(key);
                }
            }
            return null;
        }
        
        /// <summary>
        /// Cache validation result
        /// </summary>
        public void CacheValidation(string key, object result)
        {
            _validationCache[key] = new ValidationCacheEntry
            {
                Result = result,
                Timestamp = Time.time
            };
        }
        
        /// <summary>
        /// Clear validation cache
        /// </summary>
        public void ClearValidationCache()
        {
            _validationCache.Clear();
            LogDebug("Validation cache cleared");
        }
        
        #endregion
        
        private void LogValidationResult(string operation, FacilityOperationValidation validation)
        {
            if (_enableValidationLogging)
            {
                var status = validation.IsValid ? "PASSED" : "FAILED";
                LogDebug($"{operation} validation {status}: {validation.Errors.Count} errors, {validation.Warnings.Count} warnings");
            }
        }
        
        private void LogDebug(string message)
        {
            if (_enableValidationLogging)
                Debug.Log($"[FacilityValidationService] {message}");
        }
        
        private void OnDestroy()
        {
            ClearValidationCache();
        }
    }
    
    /// <summary>
    /// Result of facility operation validation
    /// </summary>
    [System.Serializable]
    public class FacilityOperationValidation
    {
        public string OperationType;
        public bool IsValid;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        
        public string GetErrorsSummary() => string.Join("\n", Errors);
        public string GetWarningsSummary() => string.Join("\n", Warnings);
    }
    
    /// <summary>
    /// Result of comprehensive system validation
    /// </summary>
    [System.Serializable]
    public class SystemValidationResult
    {
        public bool IsValid;
        public bool RegistryValid;
        public System.DateTime ValidationTime;
        public System.DateTime ValidationCompleted;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
        public void AddErrors(IEnumerable<string> errors) => Errors.AddRange(errors);
        public void AddWarnings(IEnumerable<string> warnings) => Warnings.AddRange(warnings);
    }
    
    /// <summary>
    /// Validation cache entry
    /// </summary>
    internal class ValidationCacheEntry
    {
        public object Result;
        public float Timestamp;
    }
}