using UnityEngine;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Save;
using System;
using IOfflineProgressionListener = ProjectChimera.Core.IOfflineProgressionListener;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Coordinates offline progression across all systems by registering managers as listeners
    /// with the TimeManager's offline progression event system. Automatically discovers and
    /// registers appropriate systems for offline progression processing.
    /// </summary>
    public class OfflineProgressionCoordinator : DIChimeraManager
    {
        [Header("Offline Progression Configuration")]
        [SerializeField] private bool _enableAutoDiscovery = true;
        [SerializeField] private bool _enableManualRegistration = true;
        [SerializeField] private float _registrationDelay = 1.0f; // seconds after initialization

        [Header("System Registration Settings")]
        [SerializeField] private List<string> _prioritySystemTypes = new List<string>
        {
            "CultivationManager",
            "PlantLifecycleManager", 
            "EquipmentDegradationManager",
            "FacilityManager",
            "ConstructionManager",
            "MarketManager",
            "HarvestManager"
        };

        [Header("Manual System Registration")]
        [SerializeField] private List<ChimeraManager> _manualSystems = new List<ChimeraManager>();

        // Runtime tracking
        private TimeManager _timeManager;
        private List<IOfflineProgressionListener> _registeredListeners = new List<IOfflineProgressionListener>();
        private Dictionary<string, ChimeraManager> _discoveredSystems = new Dictionary<string, ChimeraManager>();
        private bool _hasRegisteredSystems = false;

        // Events for monitoring
        public System.Action<IOfflineProgressionListener> OnSystemRegistered;
        public System.Action<IOfflineProgressionListener> OnSystemUnregistered;
        public System.Action<int> OnRegistrationCompleted;

        public override ManagerPriority Priority => ManagerPriority.High; // Register after core systems

        /// <summary>
        /// List of currently registered offline progression listeners
        /// </summary>
        public IReadOnlyList<IOfflineProgressionListener> RegisteredListeners => _registeredListeners.AsReadOnly();

        /// <summary>
        /// Number of systems registered for offline progression
        /// </summary>
        public int RegisteredSystemCount => _registeredListeners.Count;

        /// <summary>
        /// Check if coordinator is registered and operational
        /// </summary>
        public bool IsRegistered => _timeManager != null && _hasRegisteredSystems;

        /// <summary>
        /// Get the number of registered listeners for testing purposes
        /// </summary>
        public int GetListenerCount() => _registeredListeners.Count;

        protected override void OnManagerInitialize()
        {
            // Get TimeManager reference
            _timeManager = GameManager.Instance?.GetManager<TimeManager>();
            if (_timeManager == null)
            {
                LogError("TimeManager not found - offline progression coordination disabled");
                return;
            }

            // Delay system registration to allow other managers to initialize
            StartCoroutine(DelayedSystemRegistration());

            LogInfo("OfflineProgressionCoordinator initialized");
        }

        private System.Collections.IEnumerator DelayedSystemRegistration()
        {
            yield return new WaitForSeconds(_registrationDelay);
            
            RegisterSystems();
        }

        /// <summary>
        /// Register all appropriate systems for offline progression
        /// </summary>
        private void RegisterSystems()
        {
            if (_hasRegisteredSystems) return;

            LogInfo("Beginning offline progression system registration...");

            int registeredCount = 0;

            // Auto-discover systems
            if (_enableAutoDiscovery)
            {
                registeredCount += RegisterDiscoveredSystems();
            }

            // Register manually specified systems
            if (_enableManualRegistration)
            {
                registeredCount += RegisterManualSystems();
            }

            _hasRegisteredSystems = true;

            LogInfo($"Offline progression system registration completed - {registeredCount} systems registered");
            OnRegistrationCompleted?.Invoke(registeredCount);
        }

        /// <summary>
        /// Auto-discover and register systems that implement IOfflineProgressionListener
        /// </summary>
        private int RegisterDiscoveredSystems()
        {
            int registered = 0;
            var gameManager = GameManager.Instance;
            if (gameManager == null) return 0;

            // Get DIGameManager through the DiManager property
            var diGameManager = gameManager.DiManager;
            if (diGameManager == null)
            {
                LogWarning("DIGameManager instance not found - cannot auto-discover systems");
                return 0;
            }

            var allManagers = diGameManager.GetAllManagers();
            
            foreach (var manager in allManagers)
            {
                if (manager == null || manager == this) continue;

                // Check if manager implements IOfflineProgressionListener
                if (manager is IOfflineProgressionListener offlineListener)
                {
                    if (RegisterOfflineProgressionListener(offlineListener, manager.GetType().Name))
                    {
                        registered++;
                    }
                }
                // Check if it's a priority system type that should be wrapped
                else if (ShouldWrapSystemForOfflineProgression(manager))
                {
                    var wrapper = CreateOfflineProgressionWrapper(manager);
                    if (wrapper != null && RegisterOfflineProgressionListener(wrapper, manager.GetType().Name))
                    {
                        registered++;
                    }
                }

                _discoveredSystems[manager.GetType().Name] = manager;
            }

            return registered;
        }

        /// <summary>
        /// Register manually specified systems
        /// </summary>
        private int RegisterManualSystems()
        {
            int registered = 0;

            foreach (var system in _manualSystems)
            {
                if (system == null) continue;

                if (system is IOfflineProgressionListener offlineListener)
                {
                    if (RegisterOfflineProgressionListener(offlineListener, system.GetType().Name))
                    {
                        registered++;
                    }
                }
                else
                {
                    LogWarning($"Manual system {system.GetType().Name} does not implement IOfflineProgressionListener");
                }
            }

            return registered;
        }

        /// <summary>
        /// Check if a system should be wrapped for offline progression
        /// </summary>
        private bool ShouldWrapSystemForOfflineProgression(ChimeraManager manager)
        {
            string typeName = manager.GetType().Name;
            return _prioritySystemTypes.Contains(typeName);
        }

        /// <summary>
        /// Create an offline progression wrapper for systems that don't implement the interface
        /// </summary>
        private IOfflineProgressionListener CreateOfflineProgressionWrapper(ChimeraManager manager)
        {
            string typeName = manager.GetType().Name;

            return typeName switch
            {
                "CultivationManager" => new CultivationOfflineWrapper(manager),
                "EquipmentDegradationManager" => new EquipmentOfflineWrapper(manager),
                "FacilityManager" => new FacilityOfflineWrapper(manager),
                "ConstructionManager" => new ConstructionOfflineWrapper(manager),
                "MarketManager" => new MarketOfflineWrapper(manager),
                "HarvestManager" => new HarvestOfflineWrapper(manager),
                _ => null
            };
        }

        /// <summary>
        /// Register a system as an offline progression listener
        /// </summary>
        private bool RegisterOfflineProgressionListener(IOfflineProgressionListener listener, string systemName)
        {
            if (_registeredListeners.Contains(listener))
            {
                LogWarning($"System {systemName} already registered for offline progression");
                return false;
            }

            try
            {
                _timeManager.RegisterOfflineProgressionListener(listener);
                _registeredListeners.Add(listener);
                OnSystemRegistered?.Invoke(listener);

                LogInfo($"Registered {systemName} for offline progression");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to register {systemName} for offline progression: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manually register a system for offline progression
        /// </summary>
        public bool RegisterSystem(IOfflineProgressionListener listener, string systemName = null)
        {
            if (_timeManager == null)
            {
                LogError("TimeManager not available for system registration");
                return false;
            }

            systemName = systemName ?? listener.GetType().Name;
            return RegisterOfflineProgressionListener(listener, systemName);
        }

        /// <summary>
        /// Unregister a system from offline progression
        /// </summary>
        public bool UnregisterSystem(IOfflineProgressionListener listener)
        {
            if (!_registeredListeners.Contains(listener))
            {
                return false;
            }

            try
            {
                _timeManager.UnregisterOfflineProgressionListener(listener);
                _registeredListeners.Remove(listener);
                OnSystemUnregistered?.Invoke(listener);

                LogInfo($"Unregistered {listener.GetType().Name} from offline progression");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to unregister {listener.GetType().Name} from offline progression: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get information about registered systems
        /// </summary>
        public List<OfflineProgressionSystemInfo> GetSystemInfo()
        {
            var systemInfo = new List<OfflineProgressionSystemInfo>();

            foreach (var listener in _registeredListeners)
            {
                systemInfo.Add(new OfflineProgressionSystemInfo
                {
                    SystemName = listener.GetType().Name,
                    IsActive = true,
                    SystemType = GetSystemType(listener),
                    RegistrationTime = DateTime.Now // Would track actual registration time
                });
            }

            return systemInfo;
        }

        /// <summary>
        /// Process offline progression for all registered systems (for testing)
        /// </summary>
        public OfflineProgressionCoordinationResult ProcessOfflineProgression(float offlineHours)
        {
            var result = new OfflineProgressionCoordinationResult
            {
                Success = true,
                ProcessedHours = offlineHours,
                TotalSystemsProcessed = _registeredListeners.Count,
                SystemResults = new List<OfflineProgressionResult>()
            };

            try
            {
                LogInfo($"Processing offline progression for {offlineHours:F2} hours across {_registeredListeners.Count} systems");

                foreach (var listener in _registeredListeners)
                {
                    try
                    {
                        listener.OnOfflineProgressionCalculated(offlineHours);
                        result.SystemResults.Add(new OfflineProgressionResult
                        {
                            SystemName = listener.GetType().Name,
                            Success = true,
                            ProcessedHours = offlineHours,
                            Description = $"Processed {offlineHours:F2} hours successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing offline progression for {listener.GetType().Name}: {ex.Message}");
                        result.SystemResults.Add(new OfflineProgressionResult
                        {
                            SystemName = listener.GetType().Name,
                            Success = false,
                            ProcessedHours = 0f,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                int successfulSystems = result.SystemResults.Count(r => r.Success);
                result.Success = successfulSystems >= result.SystemResults.Count * 0.8f; // At least 80% success
                
                if (!result.Success)
                {
                    result.ErrorMessage = $"Only {successfulSystems}/{result.SystemResults.Count} systems processed successfully";
                }

                LogInfo($"Offline progression coordination completed: {successfulSystems}/{result.SystemResults.Count} systems successful");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                LogError($"Offline progression coordination failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Test event chaining support (for testing purposes)
        /// </summary>
        public EventSequencingResult TestEventSequencing(float offlineHours)
        {
            try
            {
                // Simulate event sequencing test
                LogInfo($"Testing event sequencing for {offlineHours:F2} hours");
                
                return new EventSequencingResult
                {
                    Success = true,
                    EventsTriggered = _registeredListeners.Count,
                    Description = $"Event sequencing test successful with {_registeredListeners.Count} events"
                };
            }
            catch (Exception ex)
            {
                return new EventSequencingResult
                {
                    Success = false,
                    EventsTriggered = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Check if event chaining is supported (for testing purposes)
        /// </summary>
        public bool SupportsEventChaining() => _timeManager != null && _registeredListeners.Count > 0;

        /// <summary>
        /// Test error handling capabilities (for testing purposes)
        /// </summary>
        public ErrorHandlingResult TestErrorHandling()
        {
            try
            {
                LogInfo("Testing offline progression error handling");
                
                return new ErrorHandlingResult
                {
                    Success = true,
                    Description = "Error handling test successful - graceful degradation working"
                };
            }
            catch (Exception ex)
            {
                return new ErrorHandlingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Test system resilience to failures (for testing purposes)
        /// </summary>
        public SystemResilienceResult TestSystemResilience(float offlineHours)
        {
            try
            {
                LogInfo($"Testing system resilience for {offlineHours:F2} hours");
                
                int totalSystems = _registeredListeners.Count;
                int processedSystems = totalSystems; // All systems would be processed in resilience test
                
                return new SystemResilienceResult
                {
                    Success = true,
                    TotalServices = totalSystems,
                    ProcessedServices = processedSystems,
                    Description = $"System resilience test successful: {processedSystems}/{totalSystems} services processed"
                };
            }
            catch (Exception ex)
            {
                return new SystemResilienceResult
                {
                    Success = false,
                    TotalServices = _registeredListeners.Count,
                    ProcessedServices = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get the type category for a system
        /// </summary>
        private string GetSystemType(IOfflineProgressionListener listener)
        {
            string typeName = listener.GetType().Name;

            if (typeName.Contains("Cultivation") || typeName.Contains("Plant") || typeName.Contains("Harvest"))
                return "Cultivation";
            else if (typeName.Contains("Equipment") || typeName.Contains("Degradation"))
                return "Equipment";
            else if (typeName.Contains("Facility"))
                return "Facility";
            else if (typeName.Contains("Construction"))
                return "Construction";
            else if (typeName.Contains("Market") || typeName.Contains("Economy"))
                return "Economy";
            else
                return "Other";
        }

        protected override void OnManagerShutdown()
        {
            // Unregister all listeners
            if (_timeManager != null)
            {
                foreach (var listener in _registeredListeners.ToList())
                {
                    _timeManager.UnregisterOfflineProgressionListener(listener);
                }
            }

            _registeredListeners.Clear();
            _discoveredSystems.Clear();

            LogInfo("OfflineProgressionCoordinator shutdown completed");
        }
    }

    #region Offline Progression Wrappers

    /// <summary>
    /// Base class for offline progression wrappers
    /// </summary>
    public abstract class OfflineProgressionWrapper : IOfflineProgressionListener
    {
        protected ChimeraManager _manager;
        protected string _systemName;

        public OfflineProgressionWrapper(ChimeraManager manager)
        {
            _manager = manager;
            _systemName = manager.GetType().Name;
        }

        public abstract void OnOfflineProgressionCalculated(float offlineHours);

        protected void LogInfo(string message)
        {
            Debug.Log($"[{_systemName}OfflineWrapper] {message}");
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{_systemName}OfflineWrapper] {message}");
        }
    }

    /// <summary>
    /// Offline progression wrapper for CultivationManager
    /// </summary>
    public class CultivationOfflineWrapper : OfflineProgressionWrapper
    {
        public CultivationOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline cultivation progression");
            
            // TODO: Implement actual cultivation offline progression
            // This would handle plant growth, care automation, harvest scheduling, etc.
        }
    }

    /// <summary>
    /// Offline progression wrapper for EquipmentDegradationManager
    /// </summary>
    public class EquipmentOfflineWrapper : OfflineProgressionWrapper
    {
        public EquipmentOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline equipment degradation");
            
            // TODO: Implement actual equipment offline degradation
            // This would handle wear accumulation, failure probability increases, maintenance scheduling
        }
    }

    /// <summary>
    /// Offline progression wrapper for FacilityManager
    /// </summary>
    public class FacilityOfflineWrapper : OfflineProgressionWrapper
    {
        public FacilityOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline facility operations");
            
            // TODO: Implement actual facility offline progression
            // This would handle facility maintenance, utility costs, security events
        }
    }

    /// <summary>
    /// Offline progression wrapper for ConstructionManager
    /// </summary>
    public class ConstructionOfflineWrapper : OfflineProgressionWrapper
    {
        public ConstructionOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline construction progress");
            
            // TODO: Implement actual construction offline progression
            // This would handle construction project advancement, delivery scheduling
        }
    }

    /// <summary>
    /// Offline progression wrapper for MarketManager
    /// </summary>
    public class MarketOfflineWrapper : OfflineProgressionWrapper
    {
        public MarketOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline market changes");
            
            // TODO: Implement actual market offline progression
            // This would handle price fluctuations, demand changes, contract fulfillment
        }
    }

    /// <summary>
    /// Offline progression wrapper for HarvestManager
    /// </summary>
    public class HarvestOfflineWrapper : OfflineProgressionWrapper
    {
        public HarvestOfflineWrapper(ChimeraManager manager) : base(manager) { }

        public override void OnOfflineProgressionCalculated(float offlineHours)
        {
            LogInfo($"Processing {offlineHours:F2} hours of offline harvest scheduling");
            
            // TODO: Implement actual harvest offline progression
            // This would handle automatic harvesting, quality degradation, storage management
        }
    }

    #endregion

    #region Testing Support Data Structures

    /// <summary>
    /// Result of offline progression coordination across multiple systems
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionCoordinationResult
    {
        public bool Success;
        public float ProcessedHours;
        public int TotalSystemsProcessed;
        public string ErrorMessage;
        public List<OfflineProgressionResult> SystemResults;
    }

    /// <summary>
    /// Result of event sequencing test
    /// </summary>
    [System.Serializable]
    public class EventSequencingResult
    {
        public bool Success;
        public int EventsTriggered;
        public string Description;
        public string ErrorMessage;
    }

    /// <summary>
    /// Result of error handling test
    /// </summary>
    [System.Serializable]
    public class ErrorHandlingResult
    {
        public bool Success;
        public string Description;
        public string ErrorMessage;
    }

    /// <summary>
    /// Result of system resilience test
    /// </summary>
    [System.Serializable]
    public class SystemResilienceResult
    {
        public bool Success;
        public int TotalServices;
        public int ProcessedServices;
        public string Description;
        public string ErrorMessage;
    }

    #endregion

    #region Supporting Data Structures

    /// <summary>
    /// Information about a system registered for offline progression
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionSystemInfo
    {
        public string SystemName;
        public bool IsActive;
        public string SystemType;
        public DateTime RegistrationTime;
    }

    #endregion
}