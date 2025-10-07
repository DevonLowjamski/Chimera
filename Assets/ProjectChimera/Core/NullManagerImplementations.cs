using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic null manager implementations aligned with Project Chimera's architecture vision.
    /// Provides minimal fallback implementations for essential managers.
    /// </summary>

    /// <summary>
    /// Basic null manager base class
    /// </summary>
    public abstract class NullManagerBase : IChimeraManager
    {
        public abstract string ManagerName { get; }
        public bool IsInitialized { get; protected set; } = false;

        public virtual void Initialize()
        {
            IsInitialized = true;
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public virtual void Shutdown()
        {
            IsInitialized = false;
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public virtual ManagerMetrics GetMetrics()
        {
            return new ManagerMetrics
            {
                ManagerName = ManagerName,
                IsHealthy = ValidateHealth()
            };
        }

        public virtual string GetStatus()
        {
            return IsInitialized ? "Active (null)" : "Inactive (null)";
        }

        public virtual bool ValidateHealth()
        {
            return true;
        }
    }

    /// <summary>
    /// Null data manager
    /// </summary>
    public class NullDataManager : NullManagerBase, IDataManager
    {
        public override string ManagerName => "NullDataManager";

        public bool SaveData(string key, object data)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            return false; // Null implementation always returns false
        }

        public T LoadData<T>(string key)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            return default;
        }

        public bool HasData(string key) => false;

        public bool DeleteData(string key)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            return false; // Null implementation always returns false
        }

        public List<string> GetAllKeys()
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            return new List<string>(); // Return empty list
        }

        public void ClearAll()
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void ClearAllData()
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public bool HasSaveData => false;
        public string CurrentSaveFile => null;

        public void SaveGame(string saveName)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void LoadGame(string saveName)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void AutoSave()
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void DeleteSave(string saveName)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public IEnumerable<string> GetSaveFiles() => new string[0];
    }

    /// <summary>
    /// Null event manager
    /// </summary>
    public class NullEventManager : NullManagerBase, IEventManager
    {
        public override string ManagerName => "NullEventManager";

        public void Subscribe<T>(Action<T> callback) where T : class
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void Unsubscribe<T>(Action<T> callback) where T : class
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void Publish<T>(T eventData) where T : class
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void PublishImmediate<T>(T eventData) where T : class
        {
            Publish(eventData);
        }

        public int GetSubscriberCount<T>() where T : class
        {
            return 0;
        }

        public void ClearAll()
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }
    }

    /// <summary>
    /// Null scene manager
    /// </summary>
    public class NullSceneManager : NullManagerBase, ISceneManager
    {
        public override string ManagerName => "NullSceneManager";

        public void LoadScene(string sceneName)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            onComplete?.Invoke();
        }

        public void UnloadScene(string sceneName)
        {
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }

        public string GetActiveSceneName()
        {
            return "NullScene";
        }

        public bool IsSceneLoaded(string sceneName) => false;
    }

    /// <summary>
    /// Factory for creating null managers
    /// </summary>
    public static class NullManagerFactory
    {
        private static readonly Dictionary<Type, Func<IChimeraManager>> _factories = new Dictionary<Type, Func<IChimeraManager>>();

        static NullManagerFactory()
        {
            // Register basic null manager factories
            _factories[typeof(IDataManager)] = () => new NullDataManager();
            _factories[typeof(IEventManager)] = () => new NullEventManager();
            _factories[typeof(ISceneManager)] = () => new NullSceneManager();
        }

        public static T CreateNullManager<T>() where T : class, IChimeraManager
        {
            var type = typeof(T);
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory() as T;
            }

            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
            return null;
        }

        public static void RegisterNullManager<T>(Func<IChimeraManager> factory) where T : class, IChimeraManager
        {
            _factories[typeof(T)] = factory;
            ChimeraLogger.LogInfo("NullManagerImplementations", "$1");
        }
    }
}
