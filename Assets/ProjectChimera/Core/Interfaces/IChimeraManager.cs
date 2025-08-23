using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for all Project Chimera manager components.
    /// 
    /// SINGLE RESPONSIBILITY: Defines the contract for manager lifecycle and health monitoring.
    /// 
    /// Provides common functionality for:
    /// - Manager identification and lifecycle management
    /// - Health monitoring and diagnostics
    /// - Basic performance metrics
    /// </summary>
    public interface IChimeraManager
    {
        /// <summary>
        /// Human-readable name of this manager.
        /// </summary>
        string ManagerName { get; }
        
        /// <summary>
        /// Whether this manager has been initialized and is ready for use.
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Initialize the manager and prepare it for use.
        /// Should be idempotent - calling multiple times should not cause issues.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Shutdown the manager and clean up resources.
        /// Should be safe to call even if not initialized.
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Gets comprehensive performance and status metrics.
        /// </summary>
        ManagerMetrics GetMetrics();
        
        /// <summary>
        /// Gets the current operational status.
        /// </summary>
        string GetStatus();
        
        /// <summary>
        /// Validates that the manager is functioning correctly.
        /// </summary>
        bool ValidateHealth();
    }
} 