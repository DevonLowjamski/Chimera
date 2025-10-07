using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Extension methods for string types used in grid construction systems
    /// </summary>
    public static class ConstructionStringExtensions
    {
        /// <summary>
        /// Format cost as currency string
        /// </summary>
        public static string ToCurrency(this float cost)
        {
            return $"${cost:F2}";
        }
        
        /// <summary>
        /// Format percentage with grid construction context
        /// </summary>
        public static string ToPercentage(this float value)
        {
            return $"{value:P1}";
        }
        
        /// <summary>
        /// Clean and format construction item names
        /// </summary>
        public static string ToConstructionName(this string itemName)
        {
            return itemName?.Trim().Replace("_", " ") ?? "Unknown Item";
        }
        
        /// <summary>
        /// Generate construction project ID
        /// </summary>
        public static string ToProjectId(this string baseName)
        {
            string cleanName = baseName?.Replace(" ", "").Replace("-", "") ?? "Project";
            return $"{cleanName}_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";
        }
    }
}