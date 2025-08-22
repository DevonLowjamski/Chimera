using UnityEngine;
using System;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Interface for HVAC control service
    /// Defines contracts for HVAC calculations, control logic, and environmental conditioning
    /// </summary>
    public interface IHVAC_Service
    {
        bool IsInitialized { get; }
        bool EnableAdvancedControl { get; set; }
        float ControlAccuracy { get; set; }
        
        void Initialize();
        void Shutdown();
        
        /// <summary>
        /// Calculate HVAC control response for target environmental conditions
        /// </summary>
        HVACControlResponse CalculateHVACResponse(
            string systemId,
            EnvironmentalConditions currentConditions,
            EnvironmentalConditions targetConditions,
            HVACSystemSpecifications specs = null);
        
        /// <summary>
        /// Simulate HVAC environmental impact over time
        /// </summary>
        EnvironmentalConditions SimulateHVACImpact(
            string systemId,
            EnvironmentalConditions currentConditions,
            HVACControlResponse controlResponse,
            float deltaTime);
        
        /// <summary>
        /// Calculate optimal HVAC settings for target conditions
        /// </summary>
        HVACOptimalSettings CalculateOptimalSettings(
            EnvironmentalConditions currentConditions,
            EnvironmentalConditions targetConditions,
            HVACSystemSpecifications specs,
            OptimizationCriteria criteria = OptimizationCriteria.Efficiency);
        
        /// <summary>
        /// Get HVAC system performance analysis
        /// </summary>
        HVACPerformanceAnalysis GetPerformanceAnalysis(string systemId);
        
        /// <summary>
        /// Get HVAC service performance metrics
        /// </summary>
        HVACPerformanceMetrics GetPerformanceMetrics();
        
        /// <summary>
        /// Register a new HVAC system for tracking
        /// </summary>
        void RegisterHVACSystem(string systemId, HVACSystemSpecifications specs);
        
        /// <summary>
        /// Unregister an HVAC system
        /// </summary>
        void UnregisterHVACSystem(string systemId);
    }
}