using UnityEngine;

namespace ProjectChimera.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public DIGameManager DiManager => DIGameManager.Instance;

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
            return DIGameManager.Instance != null ? DIGameManager.Instance.GetManager<T>() : null;
        }

        // Backward-compat registration to satisfy legacy code paths
        public void RegisterManager(ChimeraManager manager)
        {
            // If dependency-injected game manager exists, forward registration
            if (DIGameManager.Instance != null && manager != null)
            {
                DIGameManager.Instance.RegisterManager(manager);
            }
        }
    }
}
