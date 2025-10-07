using System;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Interface for scene management functionality
    /// </summary>
    public interface ISceneManager
    {
        /// <summary>
        /// Load a scene by name
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        void LoadScene(string sceneName);

        /// <summary>
        /// Load a scene asynchronously
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="onComplete">Callback when loading completes</param>
        void LoadSceneAsync(string sceneName, Action onComplete = null);

        /// <summary>
        /// Unload a scene by name
        /// </summary>
        /// <param name="sceneName">Name of the scene to unload</param>
        void UnloadScene(string sceneName);

        /// <summary>
        /// Get the currently active scene name
        /// </summary>
        string GetActiveSceneName();

        /// <summary>
        /// Check if a scene is loaded
        /// </summary>
        /// <param name="sceneName">Name of the scene to check</param>
        /// <returns>True if the scene is loaded</returns>
        bool IsSceneLoaded(string sceneName);
    }
}