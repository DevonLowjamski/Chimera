using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Scene
{
    public class SceneLoader : DIChimeraManager, ISceneLoader
    {
        [Header("Scene Loading Configuration")]
        [SerializeField] private float _sceneTransitionDelay = 0.5f;
        [SerializeField] private bool _enableProgressEvents = true;
        [SerializeField] private bool _enableDetailedLogging = true;

        [Header("ScriptableObject Event Channels")]
        [SerializeField] private SceneLoadStartedEventSO _sceneLoadStartedEvent;
        [SerializeField] private SceneLoadCompletedEventSO _sceneLoadCompletedEvent;
        [SerializeField] private SceneUnloadStartedEventSO _sceneUnloadStartedEvent;
        [SerializeField] private SceneUnloadCompletedEventSO _sceneUnloadCompletedEvent;
        [SerializeField] private SceneTransitionStartedEventSO _sceneTransitionStartedEvent;
        [SerializeField] private SceneTransitionCompletedEventSO _sceneTransitionCompletedEvent;
        [SerializeField] private SceneLoadProgressEventSO _sceneLoadProgressEvent;

        private readonly Dictionary<string, UnityEngine.SceneManagement.Scene> _loadedScenes = new Dictionary<string, UnityEngine.SceneManagement.Scene>();
        private bool _isTransitioning = false;
        private string _currentActiveScene = "";

        public event Action<string, float> OnSceneLoadProgress;
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string> OnSceneUnloadStarted; 
        public event Action<string> OnSceneUnloadCompleted;
        public event Action<string, string> OnSceneTransitionStarted;
        public event Action<string> OnSceneTransitionCompleted;

        public bool IsTransitioning => _isTransitioning;
        public string CurrentActiveScene => _currentActiveScene;
        public IReadOnlyDictionary<string, UnityEngine.SceneManagement.Scene> LoadedScenes => _loadedScenes;

        protected override void OnManagerInitialize()
        {
            if (_enableDetailedLogging)
                Debug.Log("[SceneLoader] Initializing SceneLoader service");

            // Track the initial scene
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                _currentActiveScene = activeScene.name;
                _loadedScenes[activeScene.name] = activeScene;
                
                if (_enableDetailedLogging)
                    Debug.Log($"[SceneLoader] Initial scene tracked: {_currentActiveScene}");
            }

            // Register SceneManager callbacks
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
            SceneManager.sceneUnloaded += OnSceneUnloadedCallback;
        }

        protected override void OnManagerShutdown()
        {
            if (_enableDetailedLogging)
                Debug.Log("[SceneLoader] Shutting down SceneLoader service");

            // Unregister callbacks
            SceneManager.sceneLoaded -= OnSceneLoadedCallback;
            SceneManager.sceneUnloaded -= OnSceneUnloadedCallback;

            _loadedScenes.Clear();
        }

        public void LoadSceneAdditive(string sceneName)
        {
            if (_loadedScenes.ContainsKey(sceneName))
            {
                Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is already loaded");
                return;
            }

            StartCoroutine(LoadSceneAdditiveCoroutine(sceneName));
        }

        public void UnloadScene(string sceneName)
        {
            if (!_loadedScenes.ContainsKey(sceneName))
            {
                Debug.LogWarning($"[SceneLoader] Cannot unload scene '{sceneName}' - not currently loaded");
                return;
            }

            if (_currentActiveScene == sceneName)
            {
                Debug.LogError($"[SceneLoader] Cannot unload active scene '{sceneName}' - use TransitionToScene instead");
                return;
            }

            StartCoroutine(UnloadSceneCoroutine(sceneName));
        }

        public void TransitionToScene(string targetSceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneLoader] Cannot transition to '{targetSceneName}' - already transitioning");
                return;
            }

            if (_currentActiveScene == targetSceneName)
            {
                Debug.LogWarning($"[SceneLoader] Scene '{targetSceneName}' is already the active scene");
                return;
            }

            StartCoroutine(TransitionToSceneCoroutine(targetSceneName));
        }

        public void TransitionToSceneWithPreload(string targetSceneName, params string[] additionalScenes)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneLoader] Cannot transition to '{targetSceneName}' - already transitioning");
                return;
            }

            StartCoroutine(TransitionWithPreloadCoroutine(targetSceneName, additionalScenes));
        }

        private IEnumerator LoadSceneAdditiveCoroutine(string sceneName)
        {
            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Loading scene additively: {sceneName}");

            OnSceneLoadStarted?.Invoke(sceneName);
            _sceneLoadStartedEvent?.Raise(sceneName);

            var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                if (_enableProgressEvents)
                {
                    OnSceneLoadProgress?.Invoke(sceneName, asyncLoad.progress);
                    _sceneLoadProgressEvent?.Raise(new SceneLoadProgressData(sceneName, asyncLoad.progress));
                }
                yield return null;
            }

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Scene loaded additively: {sceneName}");

            OnSceneLoadCompleted?.Invoke(sceneName);
            _sceneLoadCompletedEvent?.Raise(sceneName);
        }

        private IEnumerator UnloadSceneCoroutine(string sceneName)
        {
            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Unloading scene: {sceneName}");

            OnSceneUnloadStarted?.Invoke(sceneName);
            _sceneUnloadStartedEvent?.Raise(sceneName);

            var asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Scene unloaded: {sceneName}");

            OnSceneUnloadCompleted?.Invoke(sceneName);
            _sceneUnloadCompletedEvent?.Raise(sceneName);
        }

        private IEnumerator TransitionToSceneCoroutine(string targetSceneName)
        {
            _isTransitioning = true;
            string previousScene = _currentActiveScene;

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Starting transition from '{previousScene}' to '{targetSceneName}'");

            OnSceneTransitionStarted?.Invoke(previousScene, targetSceneName);
            _sceneTransitionStartedEvent?.Raise(new SceneTransitionData(previousScene, targetSceneName));

            // Add transition delay if configured
            if (_sceneTransitionDelay > 0)
            {
                yield return new WaitForSeconds(_sceneTransitionDelay);
            }

            // Load the target scene additively first
            yield return LoadSceneAdditiveCoroutine(targetSceneName);

            // Set the new scene as active
            var targetScene = SceneManager.GetSceneByName(targetSceneName);
            if (targetScene.IsValid())
            {
                SceneManager.SetActiveScene(targetScene);
                _currentActiveScene = targetSceneName;

                if (_enableDetailedLogging)
                    Debug.Log($"[SceneLoader] Set active scene to: {targetSceneName}");
            }

            // Unload the previous scene if it's different and valid
            if (!string.IsNullOrEmpty(previousScene) && previousScene != targetSceneName && _loadedScenes.ContainsKey(previousScene))
            {
                yield return UnloadSceneCoroutine(previousScene);
            }

            _isTransitioning = false;

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Transition completed to: {targetSceneName}");

            OnSceneTransitionCompleted?.Invoke(targetSceneName);
            _sceneTransitionCompletedEvent?.Raise(targetSceneName);
        }

        private IEnumerator TransitionWithPreloadCoroutine(string targetSceneName, string[] additionalScenes)
        {
            _isTransitioning = true;
            string previousScene = _currentActiveScene;

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Starting transition with preload from '{previousScene}' to '{targetSceneName}'");

            OnSceneTransitionStarted?.Invoke(previousScene, targetSceneName);
            _sceneTransitionStartedEvent?.Raise(new SceneTransitionData(previousScene, targetSceneName));

            // Load additional scenes first
            foreach (var sceneName in additionalScenes)
            {
                if (!_loadedScenes.ContainsKey(sceneName))
                {
                    yield return LoadSceneAdditiveCoroutine(sceneName);
                }
            }

            // Now transition to the main scene
            yield return TransitionToSceneCoroutine(targetSceneName);
        }

        private void OnSceneLoadedCallback(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            _loadedScenes[scene.name] = scene;

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Scene loaded callback: {scene.name} (mode: {mode})");
        }

        private void OnSceneUnloadedCallback(UnityEngine.SceneManagement.Scene scene)
        {
            if (_loadedScenes.ContainsKey(scene.name))
            {
                _loadedScenes.Remove(scene.name);
            }

            if (_enableDetailedLogging)
                Debug.Log($"[SceneLoader] Scene unloaded callback: {scene.name}");
        }

        public void SetActiveScene(string sceneName)
        {
            if (!_loadedScenes.ContainsKey(sceneName))
            {
                Debug.LogError($"[SceneLoader] Cannot set active scene '{sceneName}' - scene not loaded");
                return;
            }

            var scene = _loadedScenes[sceneName];
            if (scene.IsValid())
            {
                SceneManager.SetActiveScene(scene);
                _currentActiveScene = sceneName;

                if (_enableDetailedLogging)
                    Debug.Log($"[SceneLoader] Active scene changed to: {sceneName}");
            }
        }

        public bool IsSceneLoaded(string sceneName)
        {
            return _loadedScenes.ContainsKey(sceneName);
        }

        public void ForceGarbageCollection()
        {
            if (_enableDetailedLogging)
                Debug.Log("[SceneLoader] Forcing garbage collection after scene operations");

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }

    public interface ISceneLoader : IChimeraManager
    {
        event Action<string, float> OnSceneLoadProgress;
        event Action<string> OnSceneLoadStarted;
        event Action<string> OnSceneLoadCompleted;
        event Action<string> OnSceneUnloadStarted;
        event Action<string> OnSceneUnloadCompleted;
        event Action<string, string> OnSceneTransitionStarted;
        event Action<string> OnSceneTransitionCompleted;

        bool IsTransitioning { get; }
        string CurrentActiveScene { get; }
        IReadOnlyDictionary<string, UnityEngine.SceneManagement.Scene> LoadedScenes { get; }

        void LoadSceneAdditive(string sceneName);
        void UnloadScene(string sceneName);
        void TransitionToScene(string targetSceneName);
        void TransitionToSceneWithPreload(string targetSceneName, params string[] additionalScenes);
        void SetActiveScene(string sceneName);
        bool IsSceneLoaded(string sceneName);
        void ForceGarbageCollection();
    }
}