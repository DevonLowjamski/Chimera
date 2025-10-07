using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Result of placement validation operations
    /// </summary>
    [System.Serializable]
    public class PlacementValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Vector3Int GridPosition { get; set; }
        public string ValidationReason { get; set; }
        public float ValidationScore { get; set; } = 1.0f;

        /// <summary>
        /// Create a successful validation result
        /// </summary>
        public static PlacementValidationResult Success(Vector3Int gridPosition, string message = "Placement valid")
        {
            return new PlacementValidationResult
            {
                IsValid = true,
                Message = message,
                GridPosition = gridPosition,
                ValidationScore = 1.0f
            };
        }

        /// <summary>
        /// Create a failed validation result
        /// </summary>
        public static PlacementValidationResult Failure(Vector3Int gridPosition, string message = "Placement invalid")
        {
            return new PlacementValidationResult
            {
                IsValid = false,
                Message = message,
                GridPosition = gridPosition,
                ValidationScore = 0.0f
            };
        }

        /// <summary>
        /// Add an error to the validation result
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Add a warning to the validation result
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Check if there are any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Check if there are any errors
        /// </summary>
        public bool HasErrors => Errors.Count > 0;
    }
}