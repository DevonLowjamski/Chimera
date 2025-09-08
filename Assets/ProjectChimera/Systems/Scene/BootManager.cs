using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Scene
{
    /// <summary>
    /// Minimal boot manager - scene validation and DI container creation only.
    /// All lifecycle management handled by DIGameManager.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        [Header("Boot Configuration")]
        [SerializeField] private bool _enableDetailedLogging = true;

        private void Start()
        {
            if (_enableDetailedLogging)
                ChimeraLogger.Log("[BootManager] Starting minimal boot sequence...");

            // 1. Validate boot scene
            if (!BootSceneValidator.ValidateBootScene())
            {
                ChimeraLogger.LogError("[BootManager] Boot scene validation failed!");
                return;
            }

            // 2. Create or ensure DIGameManager exists
            var diGameManager = ServiceContainerFactory.Instance?.TryResolve<DIGameManager>();
            if (diGameManager == null)
            {
                var gameManagerObject = new GameObject("DIGameManager");
                diGameManager = gameManagerObject.AddComponent<DIGameManager>();
                DontDestroyOnLoad(gameManagerObject);
            }

            // 3. DIGameManager handles all subsequent initialization
            if (_enableDetailedLogging)
                ChimeraLogger.Log("[BootManager] Handoff to DIGameManager complete");

            // Boot manager's job is done - DIGameManager takes over
        }
    }
}