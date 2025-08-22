using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for all Project Chimera manager components.
    /// Provides common functionality for initialization, lifecycle management, and identification.
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
    }
} 