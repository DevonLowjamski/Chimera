using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Core Environmental Renderer
    /// Coordinates environmental rendering subsystems with focused responsibility
    /// </summary>
    public class EnvironmentalRendererCore : MonoBehaviour, ITickable
    {
        [Header("Core Environmental Settings")]
        [SerializeField] private bool _enableEnvironmentalRendering = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 0.1f;

        // Environmental subsystems
        private FogRenderingSystem _fogSystem;
        private WeatherRenderingSystem _weatherSystem;
        private WindRenderingSystem _windSystem;
        private EnvironmentalEffectsManager _effectsManager;
        private EnvironmentalPerformanceOptimizer _performanceOptimizer;

        // Singleton pattern
        public static EnvironmentalRendererCore Instance { get; private set; }

        // Properties
        public bool IsInitialized { get; private set; }
        public FogRenderingSystem FogSystem => _fogSystem;
        public WeatherRenderingSystem WeatherSystem => _weatherSystem;
        public WindRenderingSystem WindSystem => _windSystem;
        public EnvironmentalEffectsManager EffectsManager => _effectsManager;

        // ITickable implementation
        public int TickPriority => 200; // After core systems
        public bool IsTickable => enabled && gameObject.activeInHierarchy && _enableEnvironmentalRendering;

        // Events
        public System.Action OnEnvironmentalSystemInitialized;
        public System.Action<WeatherType> OnWeatherChanged;
        public System.Action<Vector3, float> OnWindChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Initializing environmental renderer core", this);

            // Initialize subsystems
            InitializeFogSystem();
            InitializeWeatherSystem();
            InitializeWindSystem();
            InitializeEffectsManager();
            InitializePerformanceOptimizer();

            IsInitialized = true;
            OnEnvironmentalSystemInitialized?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Environmental renderer core initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized || !_enableEnvironmentalRendering) return;

            // Update subsystems
            _fogSystem?.UpdateFog(deltaTime);
            _weatherSystem?.UpdateWeather(deltaTime);
            _windSystem?.UpdateWind(deltaTime);
            _effectsManager?.UpdateEffects(deltaTime);
            _performanceOptimizer?.UpdateOptimizations(deltaTime);
        }

        /// <summary>
        /// Set weather type across all systems
        /// </summary>
        public void SetWeather(WeatherType weatherType, bool immediate = false)
        {
            _weatherSystem?.SetWeather(weatherType, immediate);
            _fogSystem?.SetWeatherFog(weatherType);
            _windSystem?.SetWeatherWind(weatherType);

            OnWeatherChanged?.Invoke(weatherType);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Weather set to: {weatherType}", this);
        }

        /// <summary>
        /// Set wind parameters across all systems
        /// </summary>
        public void SetWind(Vector3 direction, float strength)
        {
            _windSystem?.SetWind(direction, strength);
            OnWindChanged?.Invoke(direction, strength);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Wind set: {direction} strength {strength}", this);
        }

        /// <summary>
        /// Enable/disable environmental rendering
        /// </summary>
        public void SetEnvironmentalRenderingEnabled(bool enabled)
        {
            _enableEnvironmentalRendering = enabled;

            _fogSystem?.SetEnabled(enabled);
            _weatherSystem?.SetEnabled(enabled);
            _windSystem?.SetEnabled(enabled);
            _effectsManager?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Environmental rendering: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void InitializeFogSystem()
        {
            var fogGO = new GameObject("FogRenderingSystem");
            fogGO.transform.SetParent(transform);
            _fogSystem = fogGO.AddComponent<FogRenderingSystem>();
        }

        private void InitializeWeatherSystem()
        {
            var weatherGO = new GameObject("WeatherRenderingSystem");
            weatherGO.transform.SetParent(transform);
            _weatherSystem = weatherGO.AddComponent<WeatherRenderingSystem>();
        }

        private void InitializeWindSystem()
        {
            var windGO = new GameObject("WindRenderingSystem");
            windGO.transform.SetParent(transform);
            _windSystem = windGO.AddComponent<WindRenderingSystem>();
        }

        private void InitializeEffectsManager()
        {
            var effectsGO = new GameObject("EnvironmentalEffectsManager");
            effectsGO.transform.SetParent(transform);
            _effectsManager = effectsGO.AddComponent<EnvironmentalEffectsManager>();
        }

        private void InitializePerformanceOptimizer()
        {
            var optimizerGO = new GameObject("EnvironmentalPerformanceOptimizer");
            optimizerGO.transform.SetParent(transform);
            _performanceOptimizer = optimizerGO.AddComponent<EnvironmentalPerformanceOptimizer>();
        }

        /// <summary>
        /// Get environmental rendering status
        /// </summary>
        public EnvironmentalStatus GetStatus()
        {
            return new EnvironmentalStatus
            {
                IsInitialized = IsInitialized,
                RenderingEnabled = _enableEnvironmentalRendering,
                CurrentWeather = _weatherSystem?.CurrentWeather ?? WeatherType.Clear,
                WindStrength = _windSystem?.CurrentWindStrength ?? 0f,
                FogDensity = _fogSystem?.CurrentFogDensity ?? 0f
            };
        }
    }

    /// <summary>
    /// Environmental rendering status data
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalStatus
    {
        public bool IsInitialized;
        public bool RenderingEnabled;
        public WeatherType CurrentWeather;
        public float WindStrength;
        public float FogDensity;
    }

}