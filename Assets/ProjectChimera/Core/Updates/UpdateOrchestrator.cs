using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.Updates
{
    /// <summary>
    /// SIMPLE: Basic update orchestrator aligned with Project Chimera's update system vision.
    /// Focuses on essential update management without complex priority systems.
    /// </summary>
    public class UpdateOrchestrator : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = false;

        // Basic tickable management
        private readonly List<ITickable> _tickables = new List<ITickable>();
        private bool _isInitialized = false;

        // Singleton pattern
        private static UpdateOrchestrator _instance;
        public static UpdateOrchestrator Instance => _instance;


        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _isInitialized = true;

            if (_enableLogging)
            {
                Logger.LogInfo("UpdateOrchestrator", "Initialized");
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Sort tickables by priority (lower numbers first)
            _tickables.Sort((a, b) => a.TickPriority.CompareTo(b.TickPriority));

            // Update all registered tickables in priority order
            foreach (var tickable in _tickables)
            {
                if (tickable != null && tickable.IsTickable)
                {
                    try
                    {
                        tickable.Tick(Time.deltaTime);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.LogError("UpdateOrchestrator", $"Error in tickable {tickable.GetType().Name}: {ex.Message}", this);
                    }
                }
            }
        }

        /// <summary>
        /// Register a tickable object
        /// </summary>
        public void RegisterTickable(ITickable tickable)
        {
            if (tickable == null) return;

            if (!_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
                tickable.OnRegistered();

                if (_enableLogging)
                {
                    Logger.LogInfo("UpdateOrchestrator", $"Registered tickable: {tickable.GetType().Name}", this);
                }
            }
        }

        /// <summary>
        /// Unregister a tickable object
        /// </summary>
        public void UnregisterTickable(ITickable tickable)
        {
            if (tickable == null) return;

            if (_tickables.Remove(tickable))
            {
                tickable.OnUnregistered();

                if (_enableLogging)
                {
                    Logger.LogInfo("UpdateOrchestrator", $"Unregistered tickable: {tickable.GetType().Name}", this);
                }
            }
        }


        /// <summary>
        /// Get all registered tickables
        /// </summary>
        public List<ITickable> GetRegisteredTickables()
        {
            return new List<ITickable>(_tickables);
        }

        /// <summary>
        /// Get count of registered tickables
        /// </summary>
        public int GetTickableCount()
        {
            return _tickables.Count;
        }

        /// <summary>
        /// Check if tickable is registered
        /// </summary>
        public bool IsRegistered(ITickable tickable)
        {
            return tickable != null && _tickables.Contains(tickable);
        }

        /// <summary>
        /// Clear all tickables
        /// </summary>
        public void ClearAllTickables()
        {
            int count = _tickables.Count;
            _tickables.Clear();

            if (_enableLogging)
            {
                Logger.LogInfo("UpdateOrchestrator", "Cleared all tickables");
            }
        }

        /// <summary>
        /// Get statistics about the orchestrator
        /// </summary>
        public UpdateOrchestratorStatistics GetStatistics()
        {
            return new UpdateOrchestratorStatistics
            {
                RegisteredTickables = _tickables.Count,
                IsInitialized = _isInitialized,
                EnableLogging = _enableLogging
            };
        }

        /// <summary>
        /// Get status information
        /// </summary>
        public UpdateOrchestratorStatus GetStatus()
        {
            return new UpdateOrchestratorStatus
            {
                Status = _isInitialized ? "Initialized" : "Not Initialized",
                TickableCount = _tickables.Count,
                IsActive = isActiveAndEnabled
            };
        }

        /// <summary>
        /// Clear all registered tickables (alias for ClearAllTickables)
        /// </summary>
        public void ClearAll()
        {
            ClearAllTickables();
        }

        /// <summary>
        /// Register a fixed tickable for FixedUpdate calls
        /// </summary>
        public void RegisterFixedTickable(IFixedTickable fixedTickable)
        {
            // For now, just register as regular tickable
            // In a full implementation, this would use FixedUpdate
            if (fixedTickable is ITickable tickable)
            {
                RegisterTickable(tickable);
            }
        }

        /// <summary>
        /// Unregister a fixed tickable
        /// </summary>
        public void UnregisterFixedTickable(IFixedTickable fixedTickable)
        {
            // For now, just unregister as regular tickable
            // In a full implementation, this would handle FixedUpdate
            if (fixedTickable is ITickable tickable)
            {
                UnregisterTickable(tickable);
            }
        }
    }

    /// <summary>
    /// Statistics for the update orchestrator
    /// </summary>
    [System.Serializable]
    public class UpdateOrchestratorStatistics
    {
        public int RegisteredTickables;
        public bool IsInitialized;
        public bool EnableLogging;
    }

    /// <summary>
    /// Status information for the update orchestrator
    /// </summary>
    [System.Serializable]
    public class UpdateOrchestratorStatus
    {
        public string Status;
        public int TickableCount;
        public bool IsActive;
    }
}
