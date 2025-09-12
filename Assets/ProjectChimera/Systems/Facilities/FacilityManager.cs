using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Facilities;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// SIMPLE: Basic facility manager aligned with Project Chimera's facility management vision.
    /// Focuses on essential facility switching and basic management operations.
    /// </summary>
    public class FacilityManager : MonoBehaviour, IFacilityManager
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private string _currentFacilityId = "DefaultFacility";

        // Basic facility data
        private List<Facility> _availableFacilities = new List<Facility>();
        private Facility _currentFacility;
        private bool _isInitialized = false;

        // Events
        public event System.Action<string> OnFacilityChanged;
        public event System.Action<FacilityProgressionData> OnProgressionUpdated;

        // Properties
        public bool IsInitialized => _isInitialized;
        public string CurrentFacilityId => _currentFacilityId;
        public Facility CurrentFacility => _currentFacility;
        public int OwnedFacilitiesCount => _availableFacilities.Count;

        /// <summary>
        /// Initialize the facility manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            LoadAvailableFacilities();
            SetCurrentFacility(_currentFacilityId);

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[FacilityManager] Initialized successfully");
            }
        }

        /// <summary>
        /// Switch to a different facility
        /// </summary>
        public bool SwitchToFacility(string facilityId)
        {
            if (!_isInitialized) return false;

            var facility = _availableFacilities.Find(f => f.FacilityId == facilityId);
            if (facility == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning($"[FacilityManager] Facility not found: {facilityId}");
                }
                return false;
            }

            _currentFacilityId = facilityId;
            _currentFacility = facility;

            OnFacilityChanged?.Invoke(facilityId);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FacilityManager] Switched to facility: {facilityId}");
            }

            return true;
        }

        /// <summary>
        /// Get all available facilities
        /// </summary>
        public List<Facility> GetAvailableFacilities()
        {
            return new List<Facility>(_availableFacilities);
        }

        /// <summary>
        /// Add a new facility
        /// </summary>
        public bool AddFacility(Facility facility)
        {
            if (facility == null || _availableFacilities.Exists(f => f.FacilityId == facility.FacilityId))
            {
                return false;
            }

            _availableFacilities.Add(facility);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FacilityManager] Added facility: {facility.FacilityId}");
            }

            return true;
        }

        /// <summary>
        /// Remove a facility
        /// </summary>
        public bool RemoveFacility(string facilityId)
        {
            var facility = _availableFacilities.Find(f => f.FacilityId == facilityId);
            if (facility == null) return false;

            _availableFacilities.Remove(facility);

            if (_currentFacilityId == facilityId)
            {
                // Switch to first available facility
                if (_availableFacilities.Count > 0)
                {
                    SwitchToFacility(_availableFacilities[0].FacilityId);
                }
                else
                {
                    _currentFacilityId = null;
                    _currentFacility = null;
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FacilityManager] Removed facility: {facilityId}");
            }

            return true;
        }

        /// <summary>
        /// Get facility by ID
        /// </summary>
        public Facility GetFacility(string facilityId)
        {
            return _availableFacilities.Find(f => f.FacilityId == facilityId);
        }

        /// <summary>
        /// Check if facility exists
        /// </summary>
        public bool HasFacility(string facilityId)
        {
            return _availableFacilities.Exists(f => f.FacilityId == facilityId);
        }

        #region Private Methods

        private void LoadAvailableFacilities()
        {
            // In a real implementation, this would load facilities from save data
            // For now, create a default facility
            var defaultFacility = new Facility
            {
                FacilityId = "DefaultFacility",
                FacilityName = "Main Grow Facility",
                FacilityType = "Warehouse",
                Size = 100f,
                Capacity = 50
            };

            _availableFacilities.Add(defaultFacility);
        }

        private void SetCurrentFacility(string facilityId)
        {
            _currentFacility = GetFacility(facilityId);
            if (_currentFacility == null && _availableFacilities.Count > 0)
            {
                _currentFacilityId = _availableFacilities[0].FacilityId;
                _currentFacility = _availableFacilities[0];
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic facility data
    /// </summary>
    [System.Serializable]
    public class Facility
    {
        public string FacilityId;
        public string FacilityName;
        public string FacilityType;
        public float Size;
        public int Capacity;
        public string Location;
        public bool IsUnlocked = true;
    }

    /// <summary>
    /// Basic facility progression data
    /// </summary>
    [System.Serializable]
    public class FacilityProgressionData
    {
        public string CurrentFacilityId;
        public int FacilityCount;
        public float TotalSize;
        public int TotalCapacity;
    }
}
