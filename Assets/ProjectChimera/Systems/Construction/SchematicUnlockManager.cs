using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// SIMPLE: Basic schematic unlock manager aligned with Project Chimera's schematic system vision.
    /// Focuses on essential schematic unlock tracking without complex dependencies.
    /// </summary>
    public class SchematicUnlockManager : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableUnlockSystem = true;
        [SerializeField] private bool _autoUnlockBasicSchematics = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic unlock tracking
        private readonly Dictionary<string, bool> _unlockedSchematics = new Dictionary<string, bool>();
        private readonly List<SchematicSO> _availableSchematics = new List<SchematicSO>();
        private bool _isInitialized = false;

        // Events
        public System.Action<SchematicSO> OnSchematicUnlocked;
        public System.Action<SchematicSO, string> OnUnlockFailed;

        /// <summary>
        /// Initialize the schematic unlock manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            LoadAvailableSchematics();

            if (_autoUnlockBasicSchematics)
            {
                UnlockBasicSchematics();
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log($"[SchematicUnlockManager] Initialized - {GetUnlockedCount()} unlocked, {GetTotalCount()} total");
            }
        }

        /// <summary>
        /// Check if a schematic is unlocked
        /// </summary>
        public bool IsSchematicUnlocked(SchematicSO schematic)
        {
            if (!_enableUnlockSystem || schematic == null) return true;

            return _unlockedSchematics.GetValueOrDefault(schematic.SchematicId, false);
        }

        /// <summary>
        /// Try to unlock a schematic
        /// </summary>
        public bool TryUnlockSchematic(SchematicSO schematic, bool forceUnlock = false)
        {
            if (!_enableUnlockSystem || schematic == null)
            {
                OnUnlockFailed?.Invoke(schematic, "Unlock system disabled");
                return false;
            }

            if (IsSchematicUnlocked(schematic))
            {
                OnUnlockFailed?.Invoke(schematic, "Already unlocked");
                return false;
            }

            if (!forceUnlock && !CanUnlockSchematic(schematic))
            {
                OnUnlockFailed?.Invoke(schematic, "Cannot unlock - requirements not met");
                return false;
            }

            _unlockedSchematics[schematic.SchematicId] = true;
            OnSchematicUnlocked?.Invoke(schematic);

            if (_enableLogging)
            {
                Debug.Log($"[SchematicUnlockManager] Unlocked schematic: {schematic.SchematicName}");
            }

            return true;
        }

        /// <summary>
        /// Check if a schematic can be unlocked
        /// </summary>
        public bool CanUnlockSchematic(SchematicSO schematic)
        {
            if (!_enableUnlockSystem || schematic == null) return false;

            // Simple check - in a real implementation, this would check requirements
            // For now, assume all schematics can be unlocked
            return true;
        }

        /// <summary>
        /// Get all unlocked schematics
        /// </summary>
        public List<SchematicSO> GetUnlockedSchematics()
        {
            return _availableSchematics.FindAll(s => IsSchematicUnlocked(s));
        }

        /// <summary>
        /// Get all available schematics
        /// </summary>
        public List<SchematicSO> GetAvailableSchematics()
        {
            return new List<SchematicSO>(_availableSchematics);
        }

        /// <summary>
        /// Get schematics available for unlock
        /// </summary>
        public List<SchematicSO> GetAvailableForUnlock()
        {
            return _availableSchematics.FindAll(s => !IsSchematicUnlocked(s) && CanUnlockSchematic(s));
        }

        /// <summary>
        /// Get unlock progress (0-1)
        /// </summary>
        public float GetUnlockProgress()
        {
            int total = _availableSchematics.Count;
            if (total == 0) return 1f;

            int unlocked = GetUnlockedCount();
            return (float)unlocked / total;
        }

        /// <summary>
        /// Get total schematic count
        /// </summary>
        public int GetTotalCount()
        {
            return _availableSchematics.Count;
        }

        /// <summary>
        /// Get unlocked schematic count
        /// </summary>
        public int GetUnlockedCount()
        {
            return _unlockedSchematics.Values.Count(locked => locked);
        }

        #region Private Methods

        private void LoadAvailableSchematics()
        {
            // In a real implementation, this would load schematics from resources or database
            // For now, create some basic schematics
            var basicSchematic = new SchematicSO
            {
                SchematicId = "BasicFacility",
                SchematicName = "Basic Facility",
                Description = "A basic facility layout"
            };

            _availableSchematics.Add(basicSchematic);
        }

        private void UnlockBasicSchematics()
        {
            // Unlock basic schematics automatically
            foreach (var schematic in _availableSchematics)
            {
                if (schematic.SchematicId.Contains("Basic"))
                {
                    TryUnlockSchematic(schematic, true);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic schematic data structure
    /// </summary>
    [System.Serializable]
    public class SchematicSO
    {
        public string SchematicId;
        public string SchematicName;
        public string Description;
        public int UnlockCost = 0;
        public bool RequiresPrerequisites = false;
    }
}
