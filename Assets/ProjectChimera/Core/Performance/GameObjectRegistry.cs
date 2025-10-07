using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// Centralized registry for tracking GameObjects and MonoBehaviours without using FindObjectOfType
    /// Replaces anti-pattern of FindObjectsOfType for performance metrics and object counting
    /// </summary>
    public interface IGameObjectRegistry
    {
        int GetCount<T>() where T : Component;
        int GetTotalGameObjectCount();
        int GetTotalMonoBehaviourCount();
        int GetCanvasCount();
        int GetUIComponentCount();
        T[] GetAll<T>() where T : Component;
        void RegisterObject<T>(T obj) where T : Component;
        void UnregisterObject<T>(T obj) where T : Component;
        void RegisterGameObject(GameObject obj);
        void UnregisterGameObject(GameObject obj);
        void RegisterCanvas(Canvas canvas);
        void UnregisterCanvas(Canvas canvas);
    }

    /// <summary>
    /// Implementation of centralized GameObject registry
    /// </summary>
    public class GameObjectRegistry : MonoBehaviour, IGameObjectRegistry
    {
        private Dictionary<Type, HashSet<Component>> _registeredObjects = new();
        private HashSet<GameObject> _registeredGameObjects = new();
        private HashSet<Canvas> _registeredCanvases = new();
        private int _totalMonoBehaviourCount = 0;

        private static GameObjectRegistry _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                // Register with ServiceContainer if available
                var serviceContainer = ServiceContainerFactory.Instance;
                serviceContainer?.RegisterSingleton<IGameObjectRegistry>(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public int GetCount<T>() where T : Component
        {
            var type = typeof(T);
            return _registeredObjects.ContainsKey(type) ? _registeredObjects[type].Count : 0;
        }

        public int GetTotalGameObjectCount()
        {
            return _registeredGameObjects.Count;
        }

        public int GetTotalMonoBehaviourCount()
        {
            return _totalMonoBehaviourCount;
        }

        public int GetCanvasCount()
        {
            return _registeredCanvases.Count;
        }

        public int GetUIComponentCount()
        {
            int totalComponents = 0;
            foreach (var canvas in _registeredCanvases)
            {
                if (canvas != null)
                {
                    totalComponents += canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>().Length;
                }
            }
            return totalComponents;
        }

        public T[] GetAll<T>() where T : Component
        {
            var type = typeof(T);
            if (!_registeredObjects.ContainsKey(type))
            {
                return new T[0];
            }

            var results = new List<T>();
            foreach (var obj in _registeredObjects[type])
            {
                if (obj is T typedObj && obj != null)
                {
                    results.Add(typedObj);
                }
            }
            return results.ToArray();
        }

        public void RegisterObject<T>(T obj) where T : Component
        {
            if (obj == null) return;

            var type = typeof(T);
            if (!_registeredObjects.ContainsKey(type))
            {
                _registeredObjects[type] = new HashSet<Component>();
            }

            if (_registeredObjects[type].Add(obj))
            {
                _totalMonoBehaviourCount++;
            }
        }

        public void UnregisterObject<T>(T obj) where T : Component
        {
            if (obj == null) return;

            var type = typeof(T);
            if (_registeredObjects.ContainsKey(type))
            {
                if (_registeredObjects[type].Remove(obj))
                {
                    _totalMonoBehaviourCount--;
                }
            }
        }

        public void RegisterGameObject(GameObject obj)
        {
            if (obj != null)
            {
                _registeredGameObjects.Add(obj);
            }
        }

        public void UnregisterGameObject(GameObject obj)
        {
            if (obj != null)
            {
                _registeredGameObjects.Remove(obj);
            }
        }

        public void RegisterCanvas(Canvas canvas)
        {
            if (canvas != null)
            {
                _registeredCanvases.Add(canvas);
            }
        }

        public void UnregisterCanvas(Canvas canvas)
        {
            if (canvas != null)
            {
                _registeredCanvases.Remove(canvas);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                serviceContainer?.Unregister<IGameObjectRegistry>();
                _instance = null;
            }
        }
    }
}
