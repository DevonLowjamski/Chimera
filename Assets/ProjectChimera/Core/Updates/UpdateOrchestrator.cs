using ProjectChimera.Core.Logging;
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
                ChimeraLogger.Log("[UpdateOrchestrator] Initialized");
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Update all registered tickables
            foreach (var tickable in _tickables)
            {
                if (tickable != null && tickable.Enabled)
                {
                    try
                    {
                        tickable.Tick(Time.deltaTime);
                    }
                    catch (System.Exception ex)
                    {
                        ChimeraLogger.LogError($"[UpdateOrchestrator] Error updating {tickable.GetType().Name}: {ex.Message}");
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
                    ChimeraLogger.Log($"[UpdateOrchestrator] Registered {tickable.GetType().Name}");
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
                    ChimeraLogger.Log($"[UpdateOrchestrator] Unregistered {tickable.GetType().Name}");
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
                ChimeraLogger.Log($"[UpdateOrchestrator] Cleared {count} tickables");
            }
        }
    }
}
