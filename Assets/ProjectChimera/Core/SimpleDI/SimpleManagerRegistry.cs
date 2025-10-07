using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core.SimpleDI
{
    /// <summary>
    /// Simple manager registry for Project Chimera
    /// Focused on core cultivation game managers
    /// </summary>
    public class SimpleManagerRegistry : MonoBehaviour
    {
        [Header("Manager Registry")]
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _enableLogging = true;

        // Core game managers - only what we need for cultivation
        private readonly Dictionary<Type, IChimeraManager> _managers = new Dictionary<Type, IChimeraManager>();

        // Manager types we expect for a cultivation game
        private readonly Type[] _expectedManagerTypes = new[]
        {
            typeof(IPlantManager),
            typeof(IEnvironmentalManager),
            typeof(IGeneticsManager),
            typeof(IEconomyManager),
            typeof(IUIManager),
            typeof(ISettingsManager),
            typeof(IEventManager),
            typeof(ITimeManager),
            typeof(IGridSystem),
            typeof(IProgressionManager)
        };

        private void Awake()
        {
            if (_autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize the manager registry
        /// </summary>
        public void Initialize()
        {
            RegisterCoreManagers();
            ValidateManagerRegistration();

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
        }

        /// <summary>
        /// Register core cultivation game managers
        /// </summary>
        private void RegisterCoreManagers()
        {
            MonoBehaviour[] managers;

            // Try to use ServiceContainer first for unified DI approach
            if (ServiceContainerFactory.Instance != null)
            {
                // Use ServiceContainer to get all MonoBehaviour services
                var services = ServiceContainerFactory.Instance.GetServices(typeof(MonoBehaviour));
                managers = services.OfType<MonoBehaviour>().ToArray();

                // If no services found, fallback to scene discovery
                if (managers.Length == 0)
                {
                    // Fallback to scene discovery and register in ServiceContainer
                    var sceneManagers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                    foreach (var manager in sceneManagers)
                    {
                        if (manager is IChimeraManager chimeraManager)
                        {
                            ServiceContainerFactory.Instance.RegisterInstance<IChimeraManager>(chimeraManager);
                        }
                    }
                    managers = sceneManagers;
                }
            }
            else if (ServiceLocator.TryGet<SimpleDIContainer>(out var simpleDIContainer))
            {
                // Use SimpleDIContainer as secondary option
                managers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            }
            else
            {
                // Final fallback to scene discovery
                managers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            }

            foreach (var manager in managers)
            {
                if (manager is IChimeraManager chimeraManager)
                {
                    RegisterManager(chimeraManager);
                }
            }

            // Register with unified ServiceContainer first, then SimpleDI for backward compatibility
            if (ServiceContainerFactory.Instance != null)
            {
                ServiceContainerFactory.Instance.RegisterInstance<SimpleManagerRegistry>(this);
            }
            else if (ServiceLocator.TryGet<SimpleDIContainer>(out var container))
            {
                container.RegisterSingleton<SimpleManagerRegistry, SimpleManagerRegistry>(this);
            }
        }

        /// <summary>
        /// Register a manager
        /// </summary>
        public void RegisterManager(IChimeraManager manager)
        {
            if (manager == null) return;

            Type managerType = manager.GetType();

            // Check if it's a manager interface we care about
            foreach (var interfaceType in managerType.GetInterfaces())
            {
                if (typeof(IChimeraManager).IsAssignableFrom(interfaceType) && interfaceType != typeof(IChimeraManager))
                {
                    _managers[interfaceType] = manager;

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
                }
            }
        }

        /// <summary>
        /// Get a manager by interface type
        /// </summary>
        public T GetManager<T>() where T : class, IChimeraManager
        {
            Type managerType = typeof(T);

            if (_managers.TryGetValue(managerType, out IChimeraManager manager))
            {
                return manager as T;
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");

            return null;
        }

        /// <summary>
        /// Try to get a manager
        /// </summary>
        public bool TryGetManager<T>(out T manager) where T : class, IChimeraManager
        {
            manager = GetManager<T>();
            return manager != null;
        }

        /// <summary>
        /// Check if a manager is registered
        /// </summary>
        public bool IsManagerRegistered<T>() where T : class, IChimeraManager
        {
            return _managers.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get all registered managers
        /// </summary>
        public IEnumerable<IChimeraManager> GetAllManagers()
        {
            return _managers.Values;
        }

        /// <summary>
        /// Validate that expected managers are registered
        /// </summary>
        private void ValidateManagerRegistration()
        {
            int missingCount = 0;

            foreach (var expectedType in _expectedManagerTypes)
            {
                if (!_managers.ContainsKey(expectedType))
                {
                    if (_enableLogging)
                        ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
                    missingCount++;
                }
            }

            if (missingCount == 0)
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
            }
            else
            {
                ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
            }
        }

        /// <summary>
        /// Shutdown all managers
        /// </summary>
        public void ShutdownAllManagers()
        {
            foreach (var manager in _managers.Values)
            {
                try
                {
                    manager.Shutdown();
                    if (_enableLogging)
                        ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogInfo("SimpleManagerRegistry", "$1");
                }
            }

            _managers.Clear();
        }

        /// <summary>
        /// Get manager registration status
        /// </summary>
        public Dictionary<string, bool> GetManagerStatus()
        {
            var status = new Dictionary<string, bool>();

            foreach (var expectedType in _expectedManagerTypes)
            {
                status[expectedType.Name] = _managers.ContainsKey(expectedType);
            }

            return status;
        }

        /// <summary>
        /// Get count of registered managers
        /// </summary>
        public int ManagerCount => _managers.Count;
    }

    /// <summary>
    /// Base interface for all Chimera managers
    /// </summary>
    public interface IChimeraManager
    {
        string ManagerName { get; }
        bool IsInitialized { get; }
        void Shutdown();
    }

    // Core manager interfaces for cultivation game
    public interface IPlantManager : IChimeraManager
    {
        void PlantSeed(Vector3Int position, string geneticsId);
        void WaterPlant(Vector3Int position);
        void HarvestPlant(Vector3Int position);
    }

    public interface IEnvironmentalManager : IChimeraManager
    {
        float Temperature { get; set; }
        float Humidity { get; set; }
        float LightIntensity { get; set; }
        void UpdateEnvironmentalConditions();
    }

    public interface IGeneticsManager : IChimeraManager
    {
        void CreateHybrid(string parent1Id, string parent2Id);
        string GetGeneticsData(string geneticsId);
    }

    public interface IEconomyManager : IChimeraManager
    {
        float PlayerMoney { get; }
        bool CanAfford(float amount);
        void AddMoney(float amount);
        void SpendMoney(float amount);
    }

    public interface IUIManager : IChimeraManager
    {
        void ShowScreen(string screenId);
        void ShowNotification(string message);
    }

    public interface ISettingsManager : IChimeraManager
    {
        T GetSetting<T>(string key);
        void SetSetting<T>(string key, T value);
    }

    public interface IEventManager : IChimeraManager
    {
        void PublishEvent<T>(T eventData) where T : IGameEvent;
        void SubscribeEvent<T>(Action<T> handler) where T : IGameEvent;
    }


    public interface IGridSystem : IChimeraManager
    {
        bool IsValidPosition(Vector3Int position);
        Vector3 GridToWorld(Vector3Int gridPosition);
        Vector3Int WorldToGrid(Vector3 worldPosition);
    }

    public interface IProgressionManager : IChimeraManager
    {
        int PlayerLevel { get; }
        void AddExperience(int amount);
        void UnlockSkill(string skillId);
    }

    /// <summary>
    /// Base game event interface
    /// </summary>
    public interface IGameEvent
    {
        string EventType { get; }
        DateTime Timestamp { get; }
    }
}
