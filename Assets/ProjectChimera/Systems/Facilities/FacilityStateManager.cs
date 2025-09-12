using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Facilities
{
    /// <summary>
    /// SIMPLE: Basic facility state manager aligned with Project Chimera's facility state management vision.
    /// Focuses on essential state tracking for facility operations.
    /// </summary>
    public class FacilityStateManager : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = true;

        // Basic state tracking
        private readonly Dictionary<string, FacilityState> _facilityStates = new Dictionary<string, FacilityState>();
        private bool _isInitialized = false;

        // Events
        public System.Action<string, FacilityState> OnFacilityStateChanged;
        public System.Action<string> OnFacilityActivated;
        public System.Action<string> OnFacilityDeactivated;

        /// <summary>
        /// Initialize the state manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[FacilityStateManager] Initialized successfully");
            }
        }

        /// <summary>
        /// Shutdown the state manager
        /// </summary>
        public void Shutdown()
        {
            _facilityStates.Clear();
            _isInitialized = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[FacilityStateManager] Shutdown completed");
            }
        }

        /// <summary>
        /// Set facility state
        /// </summary>
        public void SetFacilityState(string facilityId, FacilityState state)
        {
            var previousState = GetFacilityState(facilityId);
            _facilityStates[facilityId] = state;

            OnFacilityStateChanged?.Invoke(facilityId, state);

            if (state == FacilityState.Active && previousState != FacilityState.Active)
            {
                OnFacilityActivated?.Invoke(facilityId);
            }
            else if (state != FacilityState.Active && previousState == FacilityState.Active)
            {
                OnFacilityDeactivated?.Invoke(facilityId);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FacilityStateManager] Facility {facilityId} state: {state}");
            }
        }

        /// <summary>
        /// Get facility state
        /// </summary>
        public FacilityState GetFacilityState(string facilityId)
        {
            return _facilityStates.GetValueOrDefault(facilityId, FacilityState.Inactive);
        }

        /// <summary>
        /// Check if facility is active
        /// </summary>
        public bool IsFacilityActive(string facilityId)
        {
            return GetFacilityState(facilityId) == FacilityState.Active;
        }

        /// <summary>
        /// Activate facility
        /// </summary>
        public void ActivateFacility(string facilityId)
        {
            SetFacilityState(facilityId, FacilityState.Active);
        }

        /// <summary>
        /// Deactivate facility
        /// </summary>
        public void DeactivateFacility(string facilityId)
        {
            SetFacilityState(facilityId, FacilityState.Inactive);
        }

        /// <summary>
        /// Get all facility states
        /// </summary>
        public Dictionary<string, FacilityState> GetAllFacilityStates()
        {
            return new Dictionary<string, FacilityState>(_facilityStates);
        }

        /// <summary>
        /// Get active facilities
        /// </summary>
        public List<string> GetActiveFacilities()
        {
            return _facilityStates.Where(kvp => kvp.Value == FacilityState.Active)
                                 .Select(kvp => kvp.Key)
                                 .ToList();
        }

        /// <summary>
        /// Clear all facility states
        /// </summary>
        public void ClearAllStates()
        {
            int count = _facilityStates.Count;
            _facilityStates.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FacilityStateManager] Cleared {count} facility states");
            }
        }
    }

    /// <summary>
    /// Basic facility state enum
    /// </summary>
    public enum FacilityState
    {
        Inactive,
        Loading,
        Active,
        Error
    }
}
