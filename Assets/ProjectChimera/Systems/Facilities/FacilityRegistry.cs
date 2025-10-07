using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Facilities;
using System.Linq;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// BASIC: Simple facility registry for Project Chimera's facility management.
    /// Focuses on essential facility tracking without complex scene mapping and validation systems.
    /// </summary>
    public class FacilityRegistry : MonoBehaviour
    {
        [Header("Basic Registry Settings")]
        [SerializeField] private bool _enableBasicRegistry = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxFacilities = 10;

        // Basic facility storage
        private readonly Dictionary<string, BasicFacility> _facilities = new Dictionary<string, BasicFacility>();
        private string _currentFacilityId;

        /// <summary>
        /// Events for facility management
        /// </summary>
        public event System.Action<string> OnFacilityAdded;
        public event System.Action<string> OnFacilityRemoved;
        public event System.Action<string> OnCurrentFacilityChanged;

        /// <summary>
        /// Initialize basic facility registry
        /// </summary>
        public void Initialize()
        {
            if (_enableLogging)
            {
                ChimeraLogger.Log("FACILITY", "FacilityRegistry initialized", this);
            }
        }

        // Compatibility properties/methods for validators expecting a richer API
        public string CurrentFacilityId => _currentFacilityId;
        public int OwnedFacilitiesCount => _facilities.Count;

        public OwnedFacility GetFacilityById(string facilityId)
        {
            var basic = GetFacility(facilityId);
            return basic != null ? ToOwnedFacility(basic) : new OwnedFacility();
        }

        public int GetFacilityCountForTier(ProjectChimera.Data.Facilities.FacilityTierSO tier)
        {
            // Basic implementation: we don't model tiers here; treat all as same tier
            return _facilities.Count;
        }

        public IEnumerable<OwnedFacility> OwnedFacilities
        {
            get
            {
                foreach (var kv in _facilities)
                    yield return ToOwnedFacility(kv.Value);
            }
        }

        public RegistryValidationResult ValidateRegistry()
        {
            var result = new RegistryValidationResult
            {
                IsValid = true
            };

            // Simple sanity checks
            foreach (var kv in _facilities)
            {
                if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value.FacilityId))
                {
                    result.IsValid = false;
                    result.Errors.Add("Facility has invalid ID");
                }
            }

            return result;
        }

        private OwnedFacility ToOwnedFacility(BasicFacility basic)
        {
            return new OwnedFacility
            {
                FacilityId = basic.FacilityId,
                FacilityName = basic.FacilityName,
                IsActive = basic.IsActive,
                IsOperational = basic.IsActive,
                MaintenanceLevel = 1f,
                Tier = null,
                TotalPlantsGrown = 0,
                CurrentValue = 0f
            };
        }

        /// <summary>
        /// Add facility to registry
        /// </summary>
        public bool AddFacility(string facilityId, string facilityName, Vector3 position, float size)
        {
            if (!_enableBasicRegistry || _facilities.Count >= _maxFacilities)
            {
                return false;
            }

            if (_facilities.ContainsKey(facilityId))
            {
                return false; // Already exists
            }

            var facility = new BasicFacility
            {
                FacilityId = facilityId,
                FacilityName = facilityName,
                Position = position,
                Size = size,
                IsActive = true,
                DateCreated = System.DateTime.Now
            };

            _facilities[facilityId] = facility;
            OnFacilityAdded?.Invoke(facilityId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FACILITY", $"Facility added: {facilityName} ({facilityId})", this);
            }

            return true;
        }

        /// <summary>
        /// Remove facility from registry
        /// </summary>
        public bool RemoveFacility(string facilityId)
        {
            if (_facilities.Remove(facilityId))
            {
                OnFacilityRemoved?.Invoke(facilityId);

                // If this was the current facility, clear it
                if (_currentFacilityId == facilityId)
                {
                    _currentFacilityId = null;
                    OnCurrentFacilityChanged?.Invoke(null);
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("FACILITY", $"Facility removed: {facilityId}", this);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Set current facility
        /// </summary>
        public bool SetCurrentFacility(string facilityId)
        {
            if (!_facilities.ContainsKey(facilityId))
            {
                return false;
            }

            _currentFacilityId = facilityId;
            OnCurrentFacilityChanged?.Invoke(facilityId);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FACILITY", $"Current facility set: {facilityId}", this);
            }

            return true;
        }

        /// <summary>
        /// Get facility by ID
        /// </summary>
        public BasicFacility GetFacility(string facilityId)
        {
            return _facilities.TryGetValue(facilityId, out var facility) ? facility : null;
        }

        /// <summary>
        /// Get current facility
        /// </summary>
        public BasicFacility GetCurrentFacility()
        {
            return GetFacility(_currentFacilityId);
        }

        /// <summary>
        /// Get all facilities
        /// </summary>
        public List<BasicFacility> GetAllFacilities()
        {
            return new List<BasicFacility>(_facilities.Values);
        }

        /// <summary>
        /// Get facility IDs
        /// </summary>
        public List<string> GetFacilityIds()
        {
            return new List<string>(_facilities.Keys);
        }

        /// <summary>
        /// Check if facility exists
        /// </summary>
        public bool FacilityExists(string facilityId)
        {
            return _facilities.ContainsKey(facilityId);
        }

        /// <summary>
        /// Get facility count
        /// </summary>
        public int GetFacilityCount()
        {
            return _facilities.Count;
        }

        /// <summary>
        /// Get current facility ID
        /// </summary>
        public string GetCurrentFacilityId()
        {
            return _currentFacilityId;
        }

        /// <summary>
        /// Clear all facilities
        /// </summary>
        public void ClearAllFacilities()
        {
            var facilityIds = new List<string>(_facilities.Keys);
            _facilities.Clear();
            _currentFacilityId = null;

            foreach (string facilityId in facilityIds)
            {
                OnFacilityRemoved?.Invoke(facilityId);
            }

            OnCurrentFacilityChanged?.Invoke(null);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FACILITY", "All facilities cleared", this);
            }
        }

        /// <summary>
        /// Get facilities by type
        /// </summary>
        public List<BasicFacility> GetFacilitiesByType(FacilityType type)
        {
            var matchingFacilities = new List<BasicFacility>();

            foreach (var facility in _facilities.Values)
            {
                // For basic implementation, we don't have facility types in BasicFacility
                // This could be expanded if needed
                matchingFacilities.Add(facility);
            }

            return matchingFacilities;
        }

        /// <summary>
        /// Get registry statistics
        /// </summary>
        public RegistryStats GetStats()
        {
            int activeFacilities = _facilities.Count(f => f.Value.IsActive);
            int inactiveFacilities = _facilities.Count(f => !f.Value.IsActive);

            return new RegistryStats
            {
                TotalFacilities = _facilities.Count,
                ActiveFacilities = activeFacilities,
                InactiveFacilities = inactiveFacilities,
                HasCurrentFacility = !string.IsNullOrEmpty(_currentFacilityId),
                IsRegistryEnabled = _enableBasicRegistry
            };
        }
    }

    /// <summary>
    /// Basic facility data
    /// </summary>
    [System.Serializable]
    public class BasicFacility
    {
        public string FacilityId;
        public string FacilityName;
        public Vector3 Position;
        public float Size;
        public bool IsActive;
        public System.DateTime DateCreated;
    }

    /// <summary>
    /// Compatibility type for validators that expect extended facility data
    /// </summary>
    [System.Serializable]
    public class OwnedFacility
    {
        public string FacilityId;
        public string FacilityName;
        public bool IsActive;
        public bool IsOperational;
        public float MaintenanceLevel;
        public ProjectChimera.Data.Facilities.FacilityTierSO Tier;
        public int TotalPlantsGrown;
        public float CurrentValue;
    }

    /// <summary>
    /// Registry validation result for compatibility with validation services
    /// </summary>
    [System.Serializable]
    public class RegistryValidationResult
    {
        public bool IsValid;
        public System.Collections.Generic.List<string> Errors = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> Warnings = new System.Collections.Generic.List<string>();
    }

    // FacilityType enum moved to ProjectChimera.Data.Facilities.FacilityEnums

    /// <summary>
    /// Registry statistics
    /// </summary>
    [System.Serializable]
    public struct RegistryStats
    {
        public int TotalFacilities;
        public int ActiveFacilities;
        public int InactiveFacilities;
        public bool HasCurrentFacility;
        public bool IsRegistryEnabled;
    }
}
