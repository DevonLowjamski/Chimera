using UnityEngine;
using System;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Interface for atmosphere simulation service
    /// Defines contracts for atmospheric physics calculations and environmental modeling
    /// </summary>
    public interface IAtmosphereService
    {
        bool IsInitialized { get; }
        bool EnableAdvancedPhysics { get; set; }
        float SimulationAccuracy { get; set; }
        
        void Initialize();
        void Shutdown();
        
        /// <summary>
        /// Calculate atmospheric conditions for a given position and base conditions
        /// </summary>
        EnvironmentalConditions CalculateAtmosphericConditions(
            Vector3 position, 
            EnvironmentalConditions baseConditions,
            object equipmentState = null);
        
        /// <summary>
        /// Calculate environmental fitness for atmospheric conditions
        /// </summary>
        float CalculateEnvironmentalFitness(
            EnvironmentalConditions conditions,
            EnvironmentalConditions optimalConditions);
        
        /// <summary>
        /// Simulate atmospheric response to equipment changes
        /// </summary>
        AtmosphericResponse SimulateEquipmentResponse(
            Vector3 position,
            EnvironmentalConditions currentConditions,
            object equipmentChange,
            float deltaTime);
        
        /// <summary>
        /// Calculate atmospheric turbulence and mixing effects
        /// </summary>
        AtmosphericTurbulenceData CalculateAtmosphericTurbulence(
            Vector3 position,
            EnvironmentalConditions conditions,
            object equipmentState = null);
        
        /// <summary>
        /// Get atmospheric simulation performance metrics
        /// </summary>
        AtmosphericPerformanceMetrics GetPerformanceMetrics();
        
        /// <summary>
        /// Update atmospheric state for a zone
        /// </summary>
        void UpdateAtmosphericState(string zoneId, EnvironmentalConditions conditions);
        
        /// <summary>
        /// Get atmospheric state for a zone
        /// </summary>
        AtmosphericState GetAtmosphericState(string zoneId);
    }
}