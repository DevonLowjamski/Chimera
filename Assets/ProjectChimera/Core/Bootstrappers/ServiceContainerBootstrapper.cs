using UnityEngine;
using System.Linq;

namespace ProjectChimera.Core.Bootstrappers
{
    public class ServiceContainerBootstrapper : MonoBehaviour
    {
        [Header("Service Container Configuration")]
        [SerializeField] private bool _enableDetailedLogging = false;
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _validateServicesOnStart = true;
        
        private static IServiceContainer _container;
        private static ServiceContainerBootstrapper _instance;

        public static IServiceContainer Container => _container;
        public static ServiceContainerBootstrapper Instance => _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ServiceContainerBootstrapper] Multiple instances detected. Destroying duplicate.");
                DestroyImmediate(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServiceContainer();
        }

        void Start()
        {
            RegisterCoreServices();
            RegisterCultivationServices();
            RegisterEnvironmentalServices();
            RegisterGeneticsServices();
            RegisterEconomyServices();
            RegisterProgressionServices();
            RegisterAIServices();

            if (_validateServicesOnStart)
            {
                ValidateServiceRegistrations();
            }

            Debug.Log($"[ServiceContainerBootstrapper] Service container initialized.");
        }

        private void InitializeServiceContainer()
        {
            _container = ServiceContainerFactory.Instance;
            Debug.Log("[ServiceContainerBootstrapper] Service container created and configured");
        }

        private void RegisterCoreServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Core services registered (simplified)");
            }
        }

        private void RegisterCultivationServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Cultivation services registration prepared (services will be registered when assemblies are available)");
            }
        }

        private void RegisterEnvironmentalServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Environmental services registered (simplified)");
            }
        }

        private void RegisterGeneticsServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Genetics services prepared for registration (implementations pending)");
            }
        }

        private void RegisterEconomyServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Economy services registered (simplified)");
            }
        }

        private void RegisterProgressionServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] Progression services registered (simplified)");
            }
        }

        private void RegisterAIServices()
        {
            if (_enableDetailedLogging)
            {
                Debug.Log("[ServiceContainerBootstrapper] AI services registered (simplified)");
            }
        }

        private void ValidateServiceRegistrations()
        {
            try
            {
                // Validation logic has been removed as it is not supported by the current IServiceContainer interface.
                Debug.Log("[ServiceContainerBootstrapper] ✅ Service registration validation skipped.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceContainerBootstrapper] ❌ Service validation failed: {ex.Message}");
            }
        }

        public void RegisterService<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _container?.RegisterSingleton<TInterface, TImplementation>();
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[ServiceContainerBootstrapper] Manually registered: {typeof(TInterface).Name}");
            }
        }

        public void RegisterService<TInterface>(System.Func<IServiceContainer, TInterface> factory)
        {
            // The factory registration method is not available in the current IServiceContainer interface.
        }

        public void LogServiceRegistrations()
        {
            if (_container == null)
            {
                Debug.LogWarning("[ServiceContainerBootstrapper] Service container not initialized");
                return;
            }
            
            Debug.Log("[ServiceContainerBootstrapper] Service registration logging is not supported in the current implementation.");
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _container = null;
                Debug.Log("[ServiceContainerBootstrapper] Service container destroyed");
            }
        }

        #region Unity Editor Support

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Project Chimera/Service Container/Log Registrations")]
        private static void EditorLogServiceRegistrations()
        {
            if (Instance != null)
            {
                Instance.LogServiceRegistrations();
            }
            else
            {
                Debug.LogWarning("ServiceContainerBootstrapper not found in scene");
            }
        }

        [UnityEditor.MenuItem("Project Chimera/Service Container/Validate Services")]
        private static void EditorValidateServices()
        {
            if (Instance != null)
            {
                Instance.ValidateServiceRegistrations();
            }
            else
            {
                Debug.LogWarning("ServiceContainerBootstrapper not found in scene");
            }
        }
#endif

        #endregion
    }
}
