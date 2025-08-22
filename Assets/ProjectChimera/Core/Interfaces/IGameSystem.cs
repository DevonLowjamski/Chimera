using System;
using UnityEngine;

namespace ProjectChimera.Core.Interfaces
{
    /// <summary>
    /// Base interface for all game systems in Project Chimera.
    /// Provides common functionality without referencing Data types to avoid circular dependencies.
    /// </summary>
    public interface IGameSystem
    {
        /// <summary>
        /// Whether this system has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Whether this system is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Unique identifier for this system.
        /// </summary>
        string SystemId { get; }

        /// <summary>
        /// Initialize the system with generic configuration.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Activate the system.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivate the system.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Reset the system to its initial state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Update the system (called every frame).
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Cleanup resources when the system is destroyed.
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// Base interface for configurable game systems.
    /// </summary>
    /// <typeparam name="TConfig">Configuration type</typeparam>
    public interface IConfigurableGameSystem<TConfig> : IGameSystem
    {
        /// <summary>
        /// Initialize the system with specific configuration.
        /// </summary>
        void Initialize(TConfig config);

        /// <summary>
        /// Update the system configuration.
        /// </summary>
        void UpdateConfiguration(TConfig config);
    }
} 