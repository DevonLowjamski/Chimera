using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// SIMPLE: Basic foundation system for Project Chimera's construction.
    /// Handles essential foundation requirements for building placement.
    /// </summary>
    public class MultiLevelFoundationSystem : MonoBehaviour
    {
        [Header("Foundation Settings")]
        [SerializeField] private bool _requireFoundationsForUpperLevels = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic foundation tracking
        private readonly HashSet<Vector3Int> _foundationPositions = new HashSet<Vector3Int>();

        /// <summary>
        /// Events for foundation changes
        /// </summary>
        public event System.Action<Vector3Int> OnFoundationPlaced;
        public event System.Action<Vector3Int> OnFoundationRemoved;

        /// <summary>
        /// Check if a position requires a foundation
        /// </summary>
        public bool RequiresFoundation(Vector3Int position)
        {
            if (!_requireFoundationsForUpperLevels) return false;
            return position.z > 0; // Ground level (z=0) doesn't need foundation
        }

        /// <summary>
        /// Check if a position can have something built on it
        /// </summary>
        public bool CanBuildAt(Vector3Int position)
        {
            if (!RequiresFoundation(position)) return true;
            return HasFoundation(position);
        }

        /// <summary>
        /// Check if position has a foundation
        /// </summary>
        public bool HasFoundation(Vector3Int position)
        {
            return _foundationPositions.Contains(position);
        }

        /// <summary>
        /// Add a foundation at position
        /// </summary>
        public bool AddFoundation(Vector3Int position)
        {
            if (_foundationPositions.Contains(position))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning($"[MultiLevelFoundationSystem] Foundation already exists at {position}");
                }
                return false;
            }

            _foundationPositions.Add(position);
            OnFoundationPlaced?.Invoke(position);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[MultiLevelFoundationSystem] Foundation added at {position}");
            }

            return true;
        }

        /// <summary>
        /// Remove a foundation from position
        /// </summary>
        public bool RemoveFoundation(Vector3Int position)
        {
            if (!_foundationPositions.Contains(position))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning($"[MultiLevelFoundationSystem] No foundation found at {position}");
                }
                return false;
            }

            _foundationPositions.Remove(position);
            OnFoundationRemoved?.Invoke(position);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[MultiLevelFoundationSystem] Foundation removed from {position}");
            }

            return true;
        }

        /// <summary>
        /// Get all foundation positions
        /// </summary>
        public List<Vector3Int> GetAllFoundationPositions()
        {
            return new List<Vector3Int>(_foundationPositions);
        }

        /// <summary>
        /// Get foundation count
        /// </summary>
        public int GetFoundationCount()
        {
            return _foundationPositions.Count;
        }

        /// <summary>
        /// Clear all foundations
        /// </summary>
        public void ClearAllFoundations()
        {
            var positionsToRemove = new List<Vector3Int>(_foundationPositions);
            _foundationPositions.Clear();

            foreach (var position in positionsToRemove)
            {
                OnFoundationRemoved?.Invoke(position);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[MultiLevelFoundationSystem] Cleared {positionsToRemove.Count} foundations");
            }
        }

        /// <summary>
        /// Validate building placement
        /// </summary>
        public FoundationValidationResult ValidatePlacement(Vector3Int position)
        {
            var result = new FoundationValidationResult();

            if (RequiresFoundation(position) && !HasFoundation(position))
            {
                result.IsValid = false;
                result.ErrorMessage = "Foundation required for building at this height";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Get foundation statistics
        /// </summary>
        public FoundationStatistics GetStatistics()
        {
            return new FoundationStatistics
            {
                TotalFoundations = _foundationPositions.Count,
                RequiresFoundations = _requireFoundationsForUpperLevels
            };
        }
    }

    /// <summary>
    /// Foundation validation result
    /// </summary>
    [System.Serializable]
    public struct FoundationValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
    }

    /// <summary>
    /// Foundation statistics
    /// </summary>
    [System.Serializable]
    public struct FoundationStatistics
    {
        public int TotalFoundations;
        public bool RequiresFoundations;
    }
}
