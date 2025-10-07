using UnityEngine;
using System.Linq;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

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
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
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

            Logger.LogInfo("ServiceContainerBootstrapper", "$1");
        }

        private void InitializeServiceContainer()
        {
            _container = ServiceContainerFactory.Instance;
            Logger.LogInfo("ServiceContainerBootstrapper", "$1");
        }

        private void RegisterCoreServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterCultivationServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterEnvironmentalServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterGeneticsServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterEconomyServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterProgressionServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void RegisterAIServices()
        {
            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        private void ValidateServiceRegistrations()
        {
            try
            {
                // Validation logic has been removed as it is not supported by the current IServiceContainer interface.
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
            catch (System.Exception ex)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }

        public void RegisterService<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _container?.RegisterSingleton<TInterface, TImplementation>();

            if (_enableDetailedLogging)
            {
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
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
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
                return;
            }

            Logger.LogInfo("ServiceContainerBootstrapper", "$1");
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _container = null;
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
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
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
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
                Logger.LogInfo("ServiceContainerBootstrapper", "$1");
            }
        }
#endif

        #endregion
    }
}
