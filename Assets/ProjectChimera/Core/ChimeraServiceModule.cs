using System;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Core service module for Project Chimera fundamental services
    /// Registers essential services that other modules depend on
    /// </summary>
    public class ChimeraServiceModule : ServiceModuleBase
    {
        public override string ModuleName => "ProjectChimera.Core";
        public override Version ModuleVersion => new Version(1, 0, 0);
        public override string[] Dependencies => new string[0]; // Core module has no dependencies

        public override void ConfigureServices(IServiceLocator serviceLocator)
        {
            LogModuleAction("Configuring core services");

            try
            {
                // Register core manager interfaces that will be implemented by MonoBehaviours
                RegisterManagerInterfaces(serviceLocator);
                
                // Register utility services
                RegisterUtilityServices(serviceLocator);
                
                // Register data services
                RegisterDataServices(serviceLocator);

                LogModuleAction("Core services configured successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChimeraServiceModule] Error configuring services: {ex.Message}");
                throw;
            }
        }

        public override void Initialize(IServiceLocator serviceLocator)
        {
            LogModuleAction("Initializing core services");

            try
            {
                // Validate that essential Unity managers are available
                ValidateUnityManagers();
                
                // Initialize core systems
                InitializeCoreServices(serviceLocator);

                LogModuleAction("Core services initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChimeraServiceModule] Error initializing services: {ex.Message}");
                throw;
            }
        }

        public override bool ValidateServices(IServiceLocator serviceLocator)
        {
            LogModuleAction("Validating core services");

            try
            {
                // Validate that all core services are registered
                var validationResults = new[]
                {
                    ValidateService<IServiceLocator>(serviceLocator, "ServiceLocator"),
                    ValidateService<ServiceManager>(serviceLocator, "ServiceManager"),
                };

                var allValid = true;
                foreach (var result in validationResults)
                {
                    allValid &= result;
                }

                if (allValid)
                {
                    LogModuleAction("All core services validated successfully");
                }
                else
                {
                    LogModuleAction("Some core services failed validation");
                }

                return allValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChimeraServiceModule] Error validating services: {ex.Message}");
                return false;
            }
        }

        #region Service Registration

        private void RegisterManagerInterfaces(IServiceLocator serviceLocator)
        {
            var container = serviceLocator as IServiceContainer;
            if (container == null)
            {
                Debug.LogError("[ChimeraServiceModule] ServiceLocator does not implement IServiceContainer. Cannot register services.");
                return;
            }

            // Register factory methods for creating manager interfaces
            // These will fallback to null implementations if no actual manager is found
            
            container.RegisterFactory<ITimeManager>(locator => 
                FindObjectOfType<TimeManager>() as ITimeManager ?? new NullTimeManager());
                
            container.RegisterFactory<IDataManager>(locator => 
                FindObjectOfType<DataManager>() as IDataManager ?? new NullDataManager());
                
            container.RegisterFactory<IEventManager>(locator => 
                FindObjectOfType<EventManager>() as IEventManager ?? new InMemoryEventManager());
                
            container.RegisterFactory<ISettingsManager>(locator => 
                FindObjectOfType<SettingsManager>() as ISettingsManager ?? new PlayerPrefsSettingsManager());
                
            
            LogModuleAction("Manager interface factories registered");
        }

        private void RegisterUtilityServices(IServiceLocator serviceLocator)
        {
            // Register utility services that don't depend on Unity MonoBehaviours
            // Example: Configuration services, math utilities, etc.
            
            LogModuleAction("Utility services configured");
        }

        private void RegisterDataServices(IServiceLocator serviceLocator)
        {
            // Register data access services
            // Example: ScriptableObject managers, serialization services, etc.
            
            LogModuleAction("Data services configured");
        }

        #endregion

        #region Initialization

        private void ValidateUnityManagers()
        {
            // Validate that essential Unity components are available
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[ChimeraServiceModule] GameManager not found - some services may not function correctly");
            }

            var serviceManager = FindObjectOfType<ServiceManager>();
            if (serviceManager == null)
            {
                Debug.LogWarning("[ChimeraServiceModule] ServiceManager not found in scene");
            }
        }

        private void InitializeCoreServices(IServiceLocator serviceLocator)
        {
            // Initialize services that need setup after registration
            // This is where we would set up cross-service dependencies
            
            LogModuleAction("Core service initialization completed");
        }

        #endregion

        #region Validation Helpers

        private bool ValidateService<T>(IServiceLocator serviceLocator, string serviceName) where T : class
        {
            try
            {
                var service = (serviceLocator as IServiceContainer).TryResolve<T>();
                if (service != null)
                {
                    LogModuleAction($"Service '{serviceName}' validated successfully");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[ChimeraServiceModule] Service '{serviceName}' not available");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChimeraServiceModule] Error validating service '{serviceName}': {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Unity Object Finding

        private T FindObjectOfType<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectOfType<T>();
        }

        #endregion
    }

    #region Null Object Pattern Implementations

    /// <summary>
    /// Null implementations for managers that don't exist yet
    /// Prevents null reference exceptions and provides graceful degradation
    /// </summary>
    
    public class NullTimeManager : ITimeManager
    {
        public string ManagerName => "Null Time Manager";
        public bool IsInitialized => true;
        public TimeSpeedLevel CurrentSpeedLevel { get; private set; } = TimeSpeedLevel.Normal;
        public float CurrentTimeScale => 1.0f;
        public bool IsTimePaused => false;

        public void Initialize() { }
        public void Shutdown() { }
        public void Pause() { }
        public void Resume() { }
        public void SetSpeedLevel(TimeSpeedLevel newSpeedLevel) => CurrentSpeedLevel = newSpeedLevel;
        public void IncreaseSpeedLevel() 
        {
            if (CurrentSpeedLevel < TimeSpeedLevel.Maximum) CurrentSpeedLevel++;
        }
        public void DecreaseSpeedLevel()
        {
            if (CurrentSpeedLevel > TimeSpeedLevel.Slow) CurrentSpeedLevel--;
        }
        public void ResetSpeedLevel() => CurrentSpeedLevel = TimeSpeedLevel.Normal;
    }

    public class NullDataManager : IDataManager
    {
        public string ManagerName => "Null Data Manager";
        public bool IsInitialized => true;
        public bool HasSaveData => false;
        public string CurrentSaveFile => null;

        public void Initialize() { }
        public void Shutdown() { }
        public void SaveGame(string saveFileName = null) { }
        public void LoadGame(string saveFileName = null) { }
        public void AutoSave() { }
        public void DeleteSave(string saveFileName) { }
        public System.Collections.Generic.IEnumerable<string> GetSaveFiles() => new string[0];
    }

    public class InMemoryEventManager : IEventManager
    {
        public string ManagerName => "In-Memory Event Manager";
        public bool IsInitialized => true;

        public void Initialize() { }
        public void Shutdown() { }
        public void Subscribe<T>(System.Action<T> callback) where T : class { }
        public void Unsubscribe<T>(System.Action<T> callback) where T : class { }
        public void Publish<T>(T eventData) where T : class { }
        public void PublishImmediate<T>(T eventData) where T : class { }
        public int GetSubscriberCount<T>() where T : class => 0;
    }

    public class PlayerPrefsSettingsManager : ISettingsManager
    {
        public string ManagerName => "PlayerPrefs Settings Manager";
        public bool IsInitialized => true;

        public void Initialize() { }
        public void Shutdown() { }
        public T GetSetting<T>(string key, T defaultValue = default) => defaultValue;
        public void SetSetting<T>(string key, T value) { }
        public bool HasSetting(string key) => false;
        public void SaveSettings() { }
        public void LoadSettings() { }
        public void ResetToDefaults() { }
        public System.Collections.Generic.IEnumerable<string> GetAllSettingKeys() => new string[0];
    }

    #endregion
}