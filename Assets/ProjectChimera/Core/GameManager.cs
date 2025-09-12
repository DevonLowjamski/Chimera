using UnityEngine;

namespace ProjectChimera.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private ManagerRegistry _managerRegistry;
        public ManagerRegistry ManagerRegistry => _managerRegistry;
        
        // Boot system compatibility properties
        public bool IsInitialized { get; private set; }
        public GameState CurrentGameState { get; private set; } = GameState.Initializing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public T GetManager<T>() where T : ChimeraManager
        {
            return _managerRegistry != null ? _managerRegistry.GetManager<T>() : null;
        }

        // Backward-compat registration to satisfy legacy code paths
        public void RegisterManager(ChimeraManager manager)
        {
            // If manager registry exists, forward registration
            if (_managerRegistry != null && manager != null)
            {
                _managerRegistry.RegisterManager(manager);
            }
        }

        // Boot system compatibility methods
        public ChimeraManager[] GetAllManagers()
        {
            return _managerRegistry?.GetAllManagers() ?? new ChimeraManager[0];
        }

        public ServiceHealthReport GetServiceHealthReport()
        {
            // Simplified health report for boot system compatibility
            return new ServiceHealthReport
            {
                IsHealthy = true,
                CriticalErrors = new System.Collections.Generic.List<string>(),
                Warnings = new System.Collections.Generic.List<string>(),
                GeneratedAt = System.DateTime.Now
            };
        }

        private void Start()
        {
            // Initialize the game manager
            IsInitialized = true;
            CurrentGameState = GameState.Running;
        }
    }
}
