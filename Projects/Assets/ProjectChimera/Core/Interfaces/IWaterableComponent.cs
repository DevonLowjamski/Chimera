using UnityEngine;

namespace ProjectChimera.Core.Interfaces
{
    /// <summary>
    /// Interface for components that can receive water from irrigation systems.
    /// This interface breaks the circular dependency between Environment and Cultivation assemblies.
    /// </summary>
    public interface IWaterableComponent
    {
        /// <summary>
        /// Unique identifier for this waterable component
        /// </summary>
        string ComponentId { get; }
        
        /// <summary>
        /// Current water level (0-100%)
        /// </summary>
        float WaterLevel { get; }
        
        /// <summary>
        /// Maximum water capacity
        /// </summary>
        float MaxWaterCapacity { get; }
        
        /// <summary>
        /// Whether this component needs watering
        /// </summary>
        bool NeedsWatering { get; }
        
        /// <summary>
        /// Transform component for positioning
        /// </summary>
        Transform Transform { get; }
        
        /// <summary>
        /// Add water to this component
        /// </summary>
        /// <param name="amount">Amount of water to add</param>
        void AddWater(float amount);
        
        /// <summary>
        /// Get the current moisture level (0-100%)
        /// </summary>
        /// <returns>Moisture percentage</returns>
        float GetMoistureLevel();
        
        /// <summary>
        /// Check if this component is within range of a water source
        /// </summary>
        /// <param name="sourcePosition">Position of the water source</param>
        /// <param name="range">Maximum range</param>
        /// <returns>True if within range</returns>
        bool IsWithinRange(Vector3 sourcePosition, float range);
    }
}