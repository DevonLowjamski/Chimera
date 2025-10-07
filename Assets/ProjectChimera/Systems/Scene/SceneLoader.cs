using ProjectChimera.Core.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture

namespace ProjectChimera.Systems.Scene
{
    public class SceneLoader : ChimeraManager, ISceneLoader
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
                ChimeraLogger.Log("SCENE", "SceneLoader initialized", this);

            // Track the initial scene
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                _currentActiveScene = activeScene.name;
                _loadedScenes[activeScene.name] = activeScene;

                if (_enableDetailedLogging)
                    ChimeraLogger.Log("SCENE", $"Tracking active scene '{activeScene.name}'", this);
            }

            // Register SceneManager callbacks
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
            SceneManager.sceneUnloaded += OnSceneUnloadedCallback;
        }

        protected override void OnManagerShutdown()
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", "SceneLoader shutdown", this);

            // Unregister callbacks
            SceneManager.sceneLoaded -= OnSceneLoadedCallback;
            SceneManager.sceneUnloaded -= OnSceneUnloadedCallback;

            _loadedScenes.Clear();
        }

        public void LoadSceneAdditive(string sceneName)
        {
            if (_loadedScenes.ContainsKey(sceneName))
            {
                ChimeraLogger.LogWarning("SCENE", $"Scene '{sceneName}' already loaded", this);
                return;
            }

            StartCoroutine(LoadSceneAdditiveCoroutine(sceneName));
        }

        public void UnloadScene(string sceneName)
        {
            if (!_loadedScenes.ContainsKey(sceneName))
            {
                ChimeraLogger.LogWarning("SCENE", $"Scene '{sceneName}' not loaded", this);
                return;
            }

            if (_currentActiveScene == sceneName)
            {
                ChimeraLogger.LogWarning("SCENE", "Cannot unload the currently active scene", this);
                return;
            }

            StartCoroutine(UnloadSceneCoroutine(sceneName));
        }

        public void TransitionToScene(string targetSceneName)
        {
            if (_isTransitioning)
            {
                ChimeraLogger.LogWarning("SCENE", "Transition already in progress", this);
                return;
            }

            if (_currentActiveScene == targetSceneName)
            {
                ChimeraLogger.LogWarning("SCENE", "Already in target scene", this);
                return;
            }

            StartCoroutine(TransitionToSceneCoroutine(targetSceneName));
        }

        public void TransitionToSceneWithPreload(string targetSceneName, params string[] additionalScenes)
        {
            if (_isTransitioning)
            {
                ProjectChimera.Core.Logging.ChimeraLogger.Log("SCENE/LOAD", "Load blocked", this);
                return;
            }

            StartCoroutine(TransitionWithPreloadCoroutine(targetSceneName, additionalScenes));
        }

        private IEnumerator LoadSceneAdditiveCoroutine(string sceneName)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Begin additive load for '{sceneName}'", this);

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
                ChimeraLogger.Log("SCENE", $"Additive load complete for '{sceneName}'", this);

            OnSceneLoadCompleted?.Invoke(sceneName);
            _sceneLoadCompletedEvent?.Raise(sceneName);
        }

        private IEnumerator UnloadSceneCoroutine(string sceneName)
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Begin unload for '{sceneName}'", this);

            OnSceneUnloadStarted?.Invoke(sceneName);
            _sceneUnloadStartedEvent?.Raise(sceneName);

            var asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Unload complete for '{sceneName}'", this);

            OnSceneUnloadCompleted?.Invoke(sceneName);
            _sceneUnloadCompletedEvent?.Raise(sceneName);
        }

        private IEnumerator TransitionToSceneCoroutine(string targetSceneName)
        {
            _isTransitioning = true;
            string previousScene = _currentActiveScene;

            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Transition started: '{previousScene}' -> '{targetSceneName}'", this);

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
                    ChimeraLogger.Log("SCENE", $"Active scene set to '{targetSceneName}'", this);
            }

            // Unload the previous scene if it's different and valid
            if (!string.IsNullOrEmpty(previousScene) && previousScene != targetSceneName && _loadedScenes.ContainsKey(previousScene))
            {
                yield return UnloadSceneCoroutine(previousScene);
            }

            _isTransitioning = false;

            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Transition completed to '{targetSceneName}'", this);

            OnSceneTransitionCompleted?.Invoke(targetSceneName);
            _sceneTransitionCompletedEvent?.Raise(targetSceneName);
        }

        private IEnumerator TransitionWithPreloadCoroutine(string targetSceneName, string[] additionalScenes)
        {
            _isTransitioning = true;
            string previousScene = _currentActiveScene;

            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Transition+preload started: '{previousScene}' -> '{targetSceneName}'", this);

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
                ChimeraLogger.Log("SCENE", $"Scene loaded: '{scene.name}'", this);
        }

        private void OnSceneUnloadedCallback(UnityEngine.SceneManagement.Scene scene)
        {
            if (_loadedScenes.ContainsKey(scene.name))
            {
                _loadedScenes.Remove(scene.name);
            }

            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", $"Scene unloaded: '{scene.name}'", this);
        }

        public void SetActiveScene(string sceneName)
        {
            if (!_loadedScenes.ContainsKey(sceneName))
            {
                ChimeraLogger.LogWarning("SCENE", $"Cannot set active; scene '{sceneName}' not tracked", this);
                return;
            }

            var scene = _loadedScenes[sceneName];
            if (scene.IsValid())
            {
                SceneManager.SetActiveScene(scene);
                _currentActiveScene = sceneName;

                if (_enableDetailedLogging)
                    ChimeraLogger.Log("SCENE", $"Active scene set to '{sceneName}'", this);
            }
        }

        public bool IsSceneLoaded(string sceneName)
        {
            return _loadedScenes.ContainsKey(sceneName);
        }

        public void ForceGarbageCollection()
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log("SCENE", "Force garbage collection invoked", this);

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
