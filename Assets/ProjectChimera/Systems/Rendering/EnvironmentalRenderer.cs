using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.Rendering.Environmental;
using WeatherType = ProjectChimera.Systems.Rendering.WeatherType;
using EnvironmentalIWindAffected = ProjectChimera.Systems.Rendering.Environmental.IWindAffected;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// REFACTORED: Legacy Environmental Renderer - Now delegates to EnvironmentalRendererCore
    /// Maintains backward compatibility while using the new focused architecture
    /// </summary>
    public class EnvironmentalRenderer : MonoBehaviour, ITickable
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useLegacyMode = false;

        // Core environmental renderer
        private EnvironmentalRendererCore _environmentalCore;

        // ITickable implementation
        public int TickPriority => 150; // Before other rendering systems
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Legacy compatibility properties
        public bool IsInitialized => _environmentalCore?.IsInitialized ?? false;
        public WeatherType CurrentWeather => _environmentalCore?.WeatherSystem?.CurrentWeather ?? WeatherType.Clear;
        public Vector3 CurrentWindDirection => _environmentalCore?.WindSystem?.CurrentWindDirection ?? Vector3.zero;
        public float CurrentWindStrength => _environmentalCore?.WindSystem?.CurrentWindStrength ?? 0f;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize environmental renderer - now delegates to EnvironmentalRendererCore
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            if (_useLegacyMode)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "⚠️ Using legacy environmental rendering mode", this);
                return;
            }

            // Initialize the new core system
            if (_environmentalCore == null)
            {
                var coreGO = new GameObject("EnvironmentalRendererCore");
                coreGO.transform.SetParent(transform);
                _environmentalCore = coreGO.AddComponent<EnvironmentalRendererCore>();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Environmental renderer initialized with new architecture", this);
        }

        public void Tick(float deltaTime)
        {
            if (_useLegacyMode)
            {
                UpdateEnvironmental();
            }
            // New core system handles its own updates via ITickable
        }

        /// <summary>
        /// Set weather type - delegates to EnvironmentalRendererCore
        /// </summary>
        public void SetWeather(WeatherType weatherType, bool immediate = false)
        {
            if (_environmentalCore != null)
            {
                _environmentalCore.SetWeather(weatherType, immediate);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "⚠️ Cannot set weather: EnvironmentalRendererCore not initialized", this);
            }
        }

        /// <summary>
        /// Set wind parameters - delegates to EnvironmentalRendererCore
        /// </summary>
        public void SetWind(Vector3 direction, float strength)
        {
            if (_environmentalCore != null)
            {
                _environmentalCore.SetWind(direction, strength);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "⚠️ Cannot set wind: EnvironmentalRendererCore not initialized", this);
            }
        }

        /// <summary>
        /// Register object for wind effects - delegates to WindRenderingSystem
        /// </summary>
        public void RegisterWindAffectedObject(IWindAffected windObject)
        {
            if (_environmentalCore?.WindSystem != null)
            {
                _environmentalCore.WindSystem.RegisterWindAffectedObject(ConvertToEnvironmentalWindAffected(windObject));
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "⚠️ Cannot register wind object: WindRenderingSystem not initialized", this);
            }
        }

        /// <summary>
        /// Unregister object from wind effects - delegates to WindRenderingSystem
        /// </summary>
        public void UnregisterWindAffectedObject(IWindAffected windObject)
        {
            if (_environmentalCore?.WindSystem != null)
            {
                _environmentalCore.WindSystem.UnregisterWindAffectedObject(ConvertToEnvironmentalWindAffected(windObject));
            }
        }

        /// <summary>
        /// Create environmental effect - delegates to EnvironmentalEffectsManager
        /// </summary>
        public void CreateEffect(EnvironmentalEffectType effectType, Vector3 position, float intensity = 1f)
        {
            if (_environmentalCore?.EffectsManager != null)
            {
                _environmentalCore.EffectsManager.TriggerEffect(effectType, position, intensity);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "⚠️ Cannot create effect: EnvironmentalEffectsManager not initialized", this);
            }
        }

        /// <summary>
        /// Legacy method for backwards compatibility
        /// </summary>
        public void UpdateEnvironmental()
        {
            // Legacy implementation - only used in legacy mode
            if (_enableLogging)
            {
                ChimeraLogger.Log("RENDERING", "⚠️ UpdateEnvironmental called in legacy mode", this);
            }
        }

        /// <summary>
        /// Enable/disable environmental rendering
        /// </summary>
        public void SetEnvironmentalRenderingEnabled(bool enabled)
        {
            if (_environmentalCore != null)
            {
                _environmentalCore.SetEnvironmentalRenderingEnabled(enabled);
            }
        }

        /// <summary>
        /// Get environmental rendering status
        /// </summary>
        public EnvironmentalStatus GetStatus()
        {
            if (_environmentalCore != null)
            {
                return _environmentalCore.GetStatus();
            }

            return new EnvironmentalStatus
            {
                IsInitialized = false,
                RenderingEnabled = false,
                CurrentWeather = WeatherType.Clear,
                WindStrength = 0f,
                FogDensity = 0f
            };
        }

        #region Type Conversion Methods

        /// <summary>
        /// Convert legacy IWindAffected to Environmental namespace
        /// </summary>
        private EnvironmentalIWindAffected ConvertToEnvironmentalWindAffected(IWindAffected legacyWindAffected)
        {
            if (legacyWindAffected == null) return null;

            // Create an adapter/wrapper for the different interface
            return new WindAffectedAdapter(legacyWindAffected);
        }

        #endregion

        private void OnDestroy()
        {
            if (_environmentalCore != null)
            {
                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "Environmental renderer destroyed", this);
            }
        }
    }

    #region Legacy Compatibility Types

    /// <summary>
    /// Adapter to convert legacy IWindAffected to Environmental IWindAffected
    /// </summary>
    public class WindAffectedAdapter : EnvironmentalIWindAffected
    {
        private readonly IWindAffected _legacyWindAffected;

        public WindAffectedAdapter(IWindAffected legacyWindAffected)
        {
            _legacyWindAffected = legacyWindAffected;
        }

        public void ApplyWind(Vector3 windVector)
        {
            // Convert single windVector to direction and strength for legacy interface
            float windStrength = windVector.magnitude;
            Vector3 windDirection = windStrength > 0 ? windVector.normalized : Vector3.zero;
            _legacyWindAffected?.ApplyWind(windDirection, windStrength);
        }

        public void OnWindChanged(Vector3 windDirection, float windStrength)
        {
            // OnWindChanged not available in IWindAffected interface
            // _legacyWindAffected?.OnWindChanged(windDirection, windStrength);
        }

        // IWindAffected interface implementation
        public void ApplyWindForce(Vector3 windDirection, float windStrength)
        {
            _legacyWindAffected?.ApplyWind(windDirection, windStrength);
        }

        public Vector3 Position => Vector3.zero; // Legacy interface doesn't have Position
        public bool IsActive => _legacyWindAffected != null; // Active if wind affected object exists

        // Transform and IsWindEnabled not available in IWindAffected interface
        // public Transform Transform => transform; // Not available in adapter class
        public bool IsWindEnabled => true; // Default to enabled
    }

    /// <summary>
    /// Legacy interface for wind-affected objects - kept for backwards compatibility
    /// New code should use ProjectChimera.Systems.Rendering.Environmental.IWindAffected
    /// </summary>
    public interface IWindAffected
    {
        void ApplyWind(Vector3 windDirection, float windStrength);
    }


    /// <summary>
    /// Legacy effect types - kept for backwards compatibility
    /// New code should use ProjectChimera.Systems.Rendering.Environmental.EnvironmentalEffectType
    /// </summary>
    public enum EffectType
    {
        Rain,
        Snow,
        Dust,
        Smoke,
        Steam
    }

    #endregion
}