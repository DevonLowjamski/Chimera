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
            ChimeraLogger.LogVerbose($"[{ManagerName}] Initialized (null implementation)");
        }

        public virtual void Shutdown()
        {
            IsInitialized = false;
            ChimeraLogger.LogVerbose($"[{ManagerName}] Shutdown (null implementation)");
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
            ChimeraLogger.LogVerbose($"[NullDataManager] SaveData: {key}");
            return false; // Null implementation always returns false
        }

        public T LoadData<T>(string key)
        {
            ChimeraLogger.LogVerbose($"[NullDataManager] LoadData: {key}");
            return default;
        }

        public bool HasData(string key) => false;

        public bool DeleteData(string key)
        {
            ChimeraLogger.LogVerbose($"[NullDataManager] DeleteData: {key}");
            return false; // Null implementation always returns false
        }

        public List<string> GetAllKeys()
        {
            ChimeraLogger.LogVerbose("[NullDataManager] GetAllKeys");
            return new List<string>(); // Return empty list
        }

        public void ClearAll()
        {
            ChimeraLogger.LogVerbose("[NullDataManager] ClearAll");
        }

        public void ClearAllData()
        {
            ChimeraLogger.LogVerbose("[NullDataManager] ClearAllData");
        }

        public bool HasSaveData => false;
        public string CurrentSaveFile => null;

        public void SaveGame(string saveName)
        {
            ChimeraLogger.LogVerbose($"[NullDataManager] SaveGame: {saveName}");
        }

        public void LoadGame(string saveName)
        {
            ChimeraLogger.LogVerbose($"[NullDataManager] LoadGame: {saveName}");
        }

        public void AutoSave()
        {
            ChimeraLogger.LogVerbose("[NullDataManager] AutoSave");
        }

        public void DeleteSave(string saveName)
        {
            ChimeraLogger.LogVerbose($"[NullDataManager] DeleteSave: {saveName}");
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
            ChimeraLogger.LogVerbose($"[NullEventManager] Subscribe: {typeof(T).Name}");
        }

        public void Unsubscribe<T>(Action<T> callback) where T : class
        {
            ChimeraLogger.LogVerbose($"[NullEventManager] Unsubscribe: {typeof(T).Name}");
        }

        public void Publish<T>(T eventData) where T : class
        {
            ChimeraLogger.LogVerbose($"[NullEventManager] Publish: {typeof(T).Name}");
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
            ChimeraLogger.LogVerbose("[NullEventManager] ClearAll");
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
            ChimeraLogger.LogVerbose($"[NullSceneManager] LoadScene: {sceneName}");
        }

        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            ChimeraLogger.LogVerbose($"[NullSceneManager] LoadSceneAsync: {sceneName}");
            onComplete?.Invoke();
        }

        public void UnloadScene(string sceneName)
        {
            ChimeraLogger.LogVerbose($"[NullSceneManager] UnloadScene: {sceneName}");
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

            ChimeraLogger.LogWarning($"[NullManagerFactory] No null implementation for: {type.Name}");
            return null;
        }

        public static void RegisterNullManager<T>(Func<IChimeraManager> factory) where T : class, IChimeraManager
        {
            _factories[typeof(T)] = factory;
            ChimeraLogger.LogVerbose($"[NullManagerFactory] Registered null manager: {typeof(T).Name}");
        }
    }
}
