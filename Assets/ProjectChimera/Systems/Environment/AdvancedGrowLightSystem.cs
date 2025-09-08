using ProjectChimera.Core.Logging;
using UnityEngine;
using CultivationLightType = ProjectChimera.Data.Cultivation.LightType;
using LightSpectrum = ProjectChimera.Data.Shared.LightSpectrumData;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Advanced grow light system orchestrator - coordinates all lighting components.
    /// Refactored from 1,499-line monolithic file to lightweight orchestrator pattern.
    /// Manages light control, spectrum, optimization, automation, thermal, and performance systems.
    /// </summary>
    public class AdvancedGrowLightSystem : ChimeraSystem
    {
        [Header("Light Configuration")]
        [SerializeField] private Light _primaryLight;
        [SerializeField] private Light[] _supplementalLights;
        [SerializeField] private float _maxIntensity = 1000f; // PPFD (μmol/m²/s)
        [SerializeField] private float _maxPowerConsumption = 600f; // Watts
        [SerializeField] private float _efficiency = 2.5f; // μmol/J
        
        [Header("Specialized Components")]
        [SerializeField] private GrowLightController _lightController;
        [SerializeField] private GrowLightSpectrumController _spectrumController;
        [SerializeField] private GrowLightPlantOptimizer _plantOptimizer;
        [SerializeField] private GrowLightAutomationSystem _automationSystem;
        [SerializeField] private GrowLightThermalManager _thermalManager;
        [SerializeField] private GrowLightPerformanceMonitor _performanceMonitor;
        
        [Header("System Configuration")]
        [SerializeField] private bool _enableOptimization = true;
        [SerializeField] private bool _enableAutomation = true;
        [SerializeField] private bool _enableThermalManagement = true;
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        
        // System state
        private bool _isInitialized = false;
        private LightSpectrum _currentSpectrum = new LightSpectrum();
        
        // Events (delegated to components)
        public System.Action<bool> OnLightStateChanged;
        public System.Action<float> OnIntensityChanged;
        public System.Action<SpectrumConfiguration> OnSpectrumChanged;
        public System.Action<float> OnOptimizationScoreChanged;
        public System.Action<string> OnSystemAlert;
        
        // Properties (delegated to components)
        public bool IsOn => _lightController?.IsOn ?? false;
        public float CurrentIntensity => _lightController?.CurrentIntensity ?? 0f;
        public float PowerConsumption => _lightController?.PowerConsumption ?? 0f;
        public float CurrentTemperature => _thermalManager?.CurrentTemperature ?? 25f;
        public float OptimizationScore => _plantOptimizer?.CurrentOptimizationScore ?? 0f;
        public bool ThermalThrottlingActive => _thermalManager?.ThermalThrottlingActive ?? false;
        
        // Public properties for external access
        public LightSpectrum CurrentSpectrum => _currentSpectrum;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }
        
        protected override void OnSystemStart()
        {
            if (!_isInitialized)
            {
                LogWarning("System not initialized - call Initialize() first");
                return;
            }
            
            LogDebug("Advanced grow light system started");
        }
        
        protected override void OnSystemStop()
        {
            // Turn off lights safely
            _lightController?.TurnOff();
            
            LogDebug("Advanced grow light system stopped");
        }
        
        #region Initialization
        
        /// <summary>
        /// Initialize all specialized components with dependencies
        /// </summary>
        private void InitializeComponents()
        {
            // Ensure all components exist
            CreateComponentsIfNeeded();
            
            // Initialize light controller first
            if (_lightController != null)
            {
                _lightController.Initialize(_maxIntensity, _maxPowerConsumption, _efficiency, _primaryLight, _supplementalLights);
                SubscribeToLightControllerEvents();
            }
            
            // Initialize spectrum controller
            if (_spectrumController != null)
            {
                _spectrumController.Initialize(_primaryLight, _supplementalLights);
                SubscribeToSpectrumControllerEvents();
            }
            
            // Initialize plant optimizer (depends on light and spectrum controllers)
            if (_plantOptimizer != null && _enableOptimization)
            {
                _plantOptimizer.Initialize(_lightController, _spectrumController);
                SubscribeToPlantOptimizerEvents();
            }
            
            // Initialize automation system (depends on previous components)
            if (_automationSystem != null && _enableAutomation)
            {
                _automationSystem.Initialize(_lightController, _spectrumController, _plantOptimizer);
                SubscribeToAutomationEvents();
            }
            
            // Initialize thermal manager
            if (_thermalManager != null && _enableThermalManagement)
            {
                _thermalManager.Initialize(_lightController);
                SubscribeToThermalManagerEvents();
            }
            
            // Initialize performance monitor (depends on all other components)
            if (_performanceMonitor != null && _enablePerformanceMonitoring)
            {
                _performanceMonitor.Initialize(_lightController, _spectrumController, _thermalManager, _plantOptimizer);
                SubscribeToPerformanceMonitorEvents();
            }
            
            _isInitialized = true;
            LogDebug("All grow light components initialized successfully");
        }
        
        /// <summary>
        /// Create component instances if they don't exist
        /// </summary>
        private void CreateComponentsIfNeeded()
        {
            if (_lightController == null)
            {
                _lightController = GetComponent<GrowLightController>();
                if (_lightController == null)
                    _lightController = gameObject.AddComponent<GrowLightController>();
            }
            
            if (_spectrumController == null)
            {
                _spectrumController = GetComponent<GrowLightSpectrumController>();
                if (_spectrumController == null)
                    _spectrumController = gameObject.AddComponent<GrowLightSpectrumController>();
            }
            
            if (_plantOptimizer == null && _enableOptimization)
            {
                _plantOptimizer = GetComponent<GrowLightPlantOptimizer>();
                if (_plantOptimizer == null)
                    _plantOptimizer = gameObject.AddComponent<GrowLightPlantOptimizer>();
            }
            
            if (_automationSystem == null && _enableAutomation)
            {
                _automationSystem = GetComponent<GrowLightAutomationSystem>();
                if (_automationSystem == null)
                    _automationSystem = gameObject.AddComponent<GrowLightAutomationSystem>();
            }
            
            if (_thermalManager == null && _enableThermalManagement)
            {
                _thermalManager = GetComponent<GrowLightThermalManager>();
                if (_thermalManager == null)
                    _thermalManager = gameObject.AddComponent<GrowLightThermalManager>();
            }
            
            if (_performanceMonitor == null && _enablePerformanceMonitoring)
            {
                _performanceMonitor = GetComponent<GrowLightPerformanceMonitor>();
                if (_performanceMonitor == null)
                    _performanceMonitor = gameObject.AddComponent<GrowLightPerformanceMonitor>();
            }
            
            // Ensure we have required light components
            EnsureLightComponents();
        }
        
        /// <summary>
        /// Ensure primary and supplemental lights exist
        /// </summary>
        private void EnsureLightComponents()
        {
            if (_primaryLight == null)
            {
                _primaryLight = GetComponent<Light>();
                if (_primaryLight == null)
                {
                    _primaryLight = gameObject.AddComponent<Light>();
                    _primaryLight.type = UnityEngine.LightType.Spot;
                    _primaryLight.range = 10f;
                    _primaryLight.spotAngle = 60f;
                    _primaryLight.intensity = 0f;
                    _primaryLight.enabled = false;
                }
            }
            
            if (_supplementalLights == null || _supplementalLights.Length == 0)
            {
                CreateSupplementalLights();
            }
        }
        
        /// <summary>
        /// Create supplemental lights for spectrum control
        /// </summary>
        private void CreateSupplementalLights()
        {
            _supplementalLights = new Light[4]; // UV, Blue, Red, Far-Red
            
            for (int i = 0; i < _supplementalLights.Length; i++)
            {
                GameObject lightGO = new GameObject($"SupplementalLight_{i}");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = Vector3.zero;
                
                var light = lightGO.AddComponent<Light>();
                light.type = UnityEngine.LightType.Spot;
                light.range = _primaryLight.range;
                light.spotAngle = _primaryLight.spotAngle;
                light.intensity = 0f;
                light.enabled = false;
                
                _supplementalLights[i] = light;
            }
            
            // Set spectrum-specific colors
            if (_supplementalLights.Length >= 4)
            {
                _supplementalLights[0].color = new Color(0.4f, 0.1f, 0.8f); // UV
                _supplementalLights[1].color = new Color(0.2f, 0.3f, 1f);   // Blue
                _supplementalLights[2].color = new Color(1f, 0.2f, 0.2f);   // Red
                _supplementalLights[3].color = new Color(0.8f, 0.1f, 0.1f); // Far-Red
            }
        }
        
        #endregion
        
        #region Event Subscriptions
        
        /// <summary>
        /// Subscribe to light controller events
        /// </summary>
        private void SubscribeToLightControllerEvents()
        {
            if (_lightController == null) return;
            
            _lightController.OnLightStateChanged += (state) => OnLightStateChanged?.Invoke(state);
            _lightController.OnIntensityChanged += (intensity) => OnIntensityChanged?.Invoke(intensity);
        }
        
        /// <summary>
        /// Subscribe to spectrum controller events
        /// </summary>
        private void SubscribeToSpectrumControllerEvents()
        {
            if (_spectrumController == null) return;
            
            _spectrumController.OnSpectrumChanged += (spectrum) => OnSpectrumChanged?.Invoke(spectrum);
        }
        
        /// <summary>
        /// Subscribe to plant optimizer events
        /// </summary>
        private void SubscribeToPlantOptimizerEvents()
        {
            if (_plantOptimizer == null) return;
            
            _plantOptimizer.OnOptimizationScoreChanged += (score) => OnOptimizationScoreChanged?.Invoke(score);
        }
        
        /// <summary>
        /// Subscribe to automation system events
        /// </summary>
        private void SubscribeToAutomationEvents()
        {
            if (_automationSystem == null) return;
            
            _automationSystem.OnAutomationEvent += (automationEvent) => 
                OnSystemAlert?.Invoke($"Automation: {automationEvent.Description}");
        }
        
        /// <summary>
        /// Subscribe to thermal manager events
        /// </summary>
        private void SubscribeToThermalManagerEvents()
        {
            if (_thermalManager == null) return;
            
            _thermalManager.OnThermalAlert += (alert) => 
                OnSystemAlert?.Invoke($"Thermal: {alert.Message}");
            
            _thermalManager.OnEmergencyShutdown += () => 
                OnSystemAlert?.Invoke("CRITICAL: Emergency thermal shutdown activated");
        }
        
        /// <summary>
        /// Subscribe to performance monitor events
        /// </summary>
        private void SubscribeToPerformanceMonitorEvents()
        {
            if (_performanceMonitor == null) return;
            
            _performanceMonitor.OnPerformanceAlert += (alert) => 
                OnSystemAlert?.Invoke($"Performance: {alert.Message}");
        }
        
        #endregion
        
        #region Public API - Light Control
        
        /// <summary>
        /// Turn grow light on
        /// </summary>
        public void TurnOn()
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _lightController?.TurnOn();
            LogDebug("Grow light turned ON");
        }
        
        /// <summary>
        /// Turn grow light off
        /// </summary>
        public void TurnOff()
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _lightController?.TurnOff();
            LogDebug("Grow light turned OFF");
        }
        
        /// <summary>
        /// Set light intensity
        /// </summary>
        public void SetIntensity(float intensity)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _lightController?.SetIntensity(intensity);
            LogDebug($"Light intensity set to {intensity} PPFD");
        }
        
        #endregion
        
        #region Public API - Spectrum Control
        
        /// <summary>
        /// Activate a spectrum preset
        /// </summary>
        public void ActivateSpectrumPreset(SpectrumPreset preset)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _spectrumController?.ActivatePreset(preset);
            LogDebug($"Activated spectrum preset: {preset}");
        }
        
        /// <summary>
        /// Set custom spectrum configuration
        /// </summary>
        public void SetCustomSpectrum(SpectrumConfiguration spectrum)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _spectrumController?.SetSpectrum(spectrum);
            LogDebug($"Set custom spectrum: {spectrum.Name}");
        }
        
        #endregion
        
        #region Public API - Plant Optimization
        
        /// <summary>
        /// Register a plant for monitoring and optimization
        /// </summary>
        public void RegisterPlant(GameObject plantObject)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _plantOptimizer?.RegisterPlant(plantObject);
            LogDebug($"Registered plant for optimization: {plantObject.name}");
        }
        
        /// <summary>
        /// Unregister a plant from monitoring
        /// </summary>
        public void UnregisterPlant(GameObject plantObject)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _plantOptimizer?.UnregisterPlant(plantObject);
            LogDebug($"Unregistered plant from optimization: {plantObject.name}");
        }
        
        /// <summary>
        /// Enable or disable plant optimization
        /// </summary>
        public void SetOptimizationEnabled(bool enabled)
        {
            _enableOptimization = enabled;
            _plantOptimizer?.SetOptimizationActive(enabled);
            LogDebug($"Plant optimization {(enabled ? "enabled" : "disabled")}");
        }
        
        #endregion
        
        #region Public API - Automation
        
        /// <summary>
        /// Set photoperiod program
        /// </summary>
        public void SetPhotoperiod(PhotoperiodProgram photoperiod)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _automationSystem?.SetPhotoperiod(photoperiod);
            LogDebug($"Set photoperiod: {photoperiod.Name}");
        }
        
        /// <summary>
        /// Add lighting schedule
        /// </summary>
        public void AddLightingSchedule(LightingSchedule schedule)
        {
            if (!_isInitialized)
            {
                LogError("System not initialized");
                return;
            }
            
            _automationSystem?.AddSchedule(schedule);
            LogDebug($"Added lighting schedule: {schedule.Name}");
        }
        
        /// <summary>
        /// Enable or disable automation
        /// </summary>
        public void SetAutomationEnabled(bool enabled)
        {
            _enableAutomation = enabled;
            _automationSystem?.SetAutomationEnabled(enabled);
            LogDebug($"Automation {(enabled ? "enabled" : "disabled")}");
        }
        
        #endregion
        
        #region Public API - System Status
        
        /// <summary>
        /// Get comprehensive system status
        /// </summary>
        public AdvancedGrowLightStatus GetSystemStatus()
        {
            if (!_isInitialized)
            {
                return new AdvancedGrowLightStatus { IsInitialized = false };
            }
            
            return new AdvancedGrowLightStatus
            {
                IsInitialized = true,
                IsOn = _lightController?.IsOn ?? false,
                CurrentIntensity = _lightController?.CurrentIntensity ?? 0f,
                PowerConsumption = _lightController?.PowerConsumption ?? 0f,
                CurrentSpectrum = _spectrumController?.CurrentSpectrum,
                OptimizationScore = _plantOptimizer?.CurrentOptimizationScore ?? 0f,
                MonitoredPlantsCount = _plantOptimizer?.MonitoredPlantsCount ?? 0,
                CurrentTemperature = _thermalManager?.CurrentTemperature ?? 25f,
                ThermalThrottling = _thermalManager?.ThermalThrottlingActive ?? false,
                AutomationEnabled = _enableAutomation && (_automationSystem?.AutomationEnabled ?? false),
                CurrentPhotoperiod = _automationSystem?.CurrentPhotoperiod,
                PerformanceMetrics = _performanceMonitor?.CurrentMetrics
            };
        }
        
        /// <summary>
        /// Get thermal status
        /// </summary>
        public ThermalStatus GetThermalStatus()
        {
            return _thermalManager?.GetThermalStatus() ?? new ThermalStatus();
        }
        
        /// <summary>
        /// Get performance summary
        /// </summary>
        public PerformanceSummary GetPerformanceSummary(int hoursBack = 24)
        {
            return _performanceMonitor?.GetPerformanceSummary(hoursBack) ?? new PerformanceSummary();
        }
        
        #endregion
        
        #region System Configuration
        
        /// <summary>
        /// Configure system settings
        /// </summary>
        public void ConfigureSystem(AdvancedGrowLightConfig config)
        {
            _maxIntensity = config.MaxIntensity;
            _maxPowerConsumption = config.MaxPowerConsumption;
            _efficiency = config.Efficiency;
            _enableOptimization = config.EnableOptimization;
            _enableAutomation = config.EnableAutomation;
            _enableThermalManagement = config.EnableThermalManagement;
            _enablePerformanceMonitoring = config.EnablePerformanceMonitoring;
            
            if (_isInitialized)
            {
                // Reinitialize components with new settings
                InitializeComponents();
            }
            
            LogDebug("System configuration updated");
        }
        
        #endregion
        
        #region Logging
        
        private void LogDebug(string message)
        {
            if (_enableLogging)
                ChimeraLogger.Log($"[AdvancedGrowLightSystem] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (_enableLogging)
                ChimeraLogger.LogWarning($"[AdvancedGrowLightSystem] {message}");
        }
        
        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[AdvancedGrowLightSystem] {message}");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Cleanup - turn off lights
            _lightController?.TurnOff();
        }
        
        #region Debug Visualization
        
        private void OnDrawGizmosSelected()
        {
            // Draw light coverage area
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Vector3 size = new Vector3(4f, 0.1f, 4f); // Default coverage area
            Gizmos.DrawWireCube(center, size);
            
            // Draw light cone for spot lights
            if (_primaryLight != null && _primaryLight.type == UnityEngine.LightType.Spot)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                float range = _primaryLight.range;
                float angle = _primaryLight.spotAngle;
                
                // Simple cone visualization
                Vector3 forward = transform.forward * range;
                float radius = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * range;
                
                // DrawWireCone is not available in Unity - use alternative
                Gizmos.DrawWireSphere(transform.position + forward, radius);
                Gizmos.DrawRay(transform.position, forward);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// System status information
    /// </summary>
    [System.Serializable]
    public class AdvancedGrowLightStatus
    {
        public bool IsInitialized;
        public bool IsOn;
        public float CurrentIntensity;
        public float PowerConsumption;
        public SpectrumConfiguration CurrentSpectrum;
        public float OptimizationScore;
        public int MonitoredPlantsCount;
        public float CurrentTemperature;
        public bool ThermalThrottling;
        public bool AutomationEnabled;
        public PhotoperiodProgram CurrentPhotoperiod;
        public PerformanceMetrics PerformanceMetrics;
    }
    
    /// <summary>
    /// System configuration
    /// </summary>
    [System.Serializable]
    public class AdvancedGrowLightConfig
    {
        public float MaxIntensity = 1000f;
        public float MaxPowerConsumption = 600f;
        public float Efficiency = 2.5f;
        public bool EnableOptimization = true;
        public bool EnableAutomation = true;
        public bool EnableThermalManagement = true;
        public bool EnablePerformanceMonitoring = true;
    }
}