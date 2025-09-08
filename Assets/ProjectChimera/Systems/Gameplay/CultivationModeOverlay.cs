using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Cultivation mode overlay system - provides default view and plant monitoring interface
    /// Responds to gameplay mode changes and shows cultivation-specific UI elements
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class CultivationModeOverlay : MonoBehaviour, ITickable
    {
        [Header("Overlay Configuration")]
        [SerializeField] private bool _enablePlantMonitoring = true;
        [SerializeField] private bool _enableCareTools = true;
        [SerializeField] private bool _showPlantDetails = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Plant Monitoring Panel")]
        [SerializeField] private GameObject _plantMonitoringPanel;
        [SerializeField] private UnityEngine.UI.Toggle _healthIndicatorsToggle;
        [SerializeField] private UnityEngine.UI.Toggle _growthStageToggle;
        [SerializeField] private UnityEngine.UI.Toggle _environmentalNeedsToggle;
        [SerializeField] private UnityEngine.UI.Toggle _harvestReadinessToggle;
        
        [Header("Plant Care Tools")]
        [SerializeField] private GameObject _careToolsPanel;
        [SerializeField] private UnityEngine.UI.Button _wateringToolButton;
        [SerializeField] private UnityEngine.UI.Button _nutrientToolButton;
        [SerializeField] private UnityEngine.UI.Button _pruningToolButton;
        [SerializeField] private UnityEngine.UI.Button _harvestToolButton;
        [SerializeField] private UnityEngine.UI.Button _inspectionToolButton;
        
        [Header("Plant Details Panel")]
        [SerializeField] private GameObject _plantDetailsPanel;
        [SerializeField] private UnityEngine.UI.Text _selectedPlantName;
        [SerializeField] private UnityEngine.UI.Text _plantHealthStatus;
        [SerializeField] private UnityEngine.UI.Text _growthStageInfo;
        [SerializeField] private UnityEngine.UI.Text _environmentalStatus;
        [SerializeField] private UnityEngine.UI.Image _plantPreviewImage;
        [SerializeField] private UnityEngine.UI.Button _detailedViewButton;
        
        [Header("Environmental Controls")]
        [SerializeField] private GameObject _environmentPanel;
        [SerializeField] private UnityEngine.UI.Slider _temperatureControl;
        [SerializeField] private UnityEngine.UI.Slider _humidityControl;
        [SerializeField] private UnityEngine.UI.Slider _lightIntensityControl;
        [SerializeField] private UnityEngine.UI.Toggle _ventilationToggle;
        [SerializeField] private UnityEngine.UI.Text _environmentalReadings;
        
        [Header("Cultivation Overview")]
        [SerializeField] private GameObject _overviewPanel;
        [SerializeField] private UnityEngine.UI.Text _totalPlantsCount;
        [SerializeField] private UnityEngine.UI.Text _healthyPlantsCount;
        [SerializeField] private UnityEngine.UI.Text _plantsNeedingCare;
        [SerializeField] private UnityEngine.UI.Text _readyToHarvestCount;
        [SerializeField] private ProgressBar _overallHealthBar;
        
        [Header("Visual Indicators")]
        [SerializeField] private GameObject _visualIndicatorsRoot;
        [SerializeField] private Material _healthyPlantMaterial;
        [SerializeField] private Material _unhealthyPlantMaterial;
        [SerializeField] private Material _harvestReadyMaterial;
        [SerializeField] private GameObject _indicatorPrefab;
        
        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        
        // Services
        private IGameplayModeController _modeController;
        
        // State tracking
        private bool _isInitialized = false;
        private bool _isCultivationModeActive = false;
        private Transform _selectedPlant = null;
        private float _monitoringUpdateTimer = 0f;
        private const float MONITORING_UPDATE_INTERVAL = 2f; // Update every 2 seconds
        
        // Plant monitoring
        private List<PlantInfo> _monitoredPlants = new List<PlantInfo>();
        private Dictionary<Transform, GameObject> _plantIndicators = new Dictionary<Transform, GameObject>();
        
        // Plant information structure
        [System.Serializable]
        public class PlantInfo
        {
            public Transform plantTransform;
            public string plantName;
            public float health;
            public string growthStage;
            public bool needsWater;
            public bool needsNutrients;
            public bool readyToHarvest;
            public float environmentalScore;
            
            public PlantInfo(Transform plant)
            {
                plantTransform = plant;
                plantName = plant.name;
                // Initialize with placeholder values - in real implementation would read from plant components
                health = Random.Range(0.6f, 1.0f);
                growthStage = GetRandomGrowthStage();
                needsWater = Random.value < 0.3f;
                needsNutrients = Random.value < 0.2f;
                readyToHarvest = Random.value < 0.1f;
                environmentalScore = Random.Range(0.7f, 1.0f);
            }
            
            private string GetRandomGrowthStage()
            {
                string[] stages = { "Seedling", "Vegetative", "Pre-Flower", "Flowering", "Ripening" };
                return stages[Random.Range(0, stages.Length)];
            }
        }
        
        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeOverlay();
        }
        
            public void Tick(float deltaTime)
    {
            if (_isCultivationModeActive)
            {
                UpdatePlantMonitoring();
                UpdateEnvironmentalReadings();
            
    }
        }
        
        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            UnsubscribeFromEvents();
            CleanupIndicators();
        }
        
        private void InitializeOverlay()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ServiceContainerFactory.Instance?.TryResolve<IGameplayModeController>();
                
                if (_modeController == null)
                {
                    ChimeraLogger.LogError("[CultivationModeOverlay] GameplayModeController service not found!");
                    return;
                }
                
                // Subscribe to mode change events
                SubscribeToEvents();
                
                // Subscribe to UI control events
                SubscribeToUIControls();
                
                // Initialize plant monitoring
                RefreshPlantList();
                
                // Initialize overlay visibility based on current mode
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                // Set initial UI state
                InitializeUIState();
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    ChimeraLogger.Log($"[CultivationModeOverlay] Initialized with current mode: {_modeController.CurrentMode}");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CultivationModeOverlay] Error during initialization: {ex.Message}");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                ChimeraLogger.LogWarning("[CultivationModeOverlay] ModeChangedEvent not assigned");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Unsubscribe(OnModeChanged);
            }
        }
        
        private void SubscribeToUIControls()
        {
            // Plant monitoring toggles
            if (_healthIndicatorsToggle != null)
            {
                _healthIndicatorsToggle.onValueChanged.AddListener(OnHealthIndicatorsToggled);
            }
            
            if (_growthStageToggle != null)
            {
                _growthStageToggle.onValueChanged.AddListener(OnGrowthStageToggled);
            }
            
            if (_environmentalNeedsToggle != null)
            {
                _environmentalNeedsToggle.onValueChanged.AddListener(OnEnvironmentalNeedsToggled);
            }
            
            if (_harvestReadinessToggle != null)
            {
                _harvestReadinessToggle.onValueChanged.AddListener(OnHarvestReadinessToggled);
            }
            
            // Plant care tools
            if (_wateringToolButton != null)
            {
                _wateringToolButton.onClick.AddListener(() => OnCareToolSelected("Watering"));
            }
            
            if (_nutrientToolButton != null)
            {
                _nutrientToolButton.onClick.AddListener(() => OnCareToolSelected("Nutrients"));
            }
            
            if (_pruningToolButton != null)
            {
                _pruningToolButton.onClick.AddListener(() => OnCareToolSelected("Pruning"));
            }
            
            if (_harvestToolButton != null)
            {
                _harvestToolButton.onClick.AddListener(() => OnCareToolSelected("Harvest"));
            }
            
            if (_inspectionToolButton != null)
            {
                _inspectionToolButton.onClick.AddListener(() => OnCareToolSelected("Inspection"));
            }
            
            // Environmental controls
            if (_temperatureControl != null)
            {
                _temperatureControl.onValueChanged.AddListener(OnTemperatureChanged);
            }
            
            if (_humidityControl != null)
            {
                _humidityControl.onValueChanged.AddListener(OnHumidityChanged);
            }
            
            if (_lightIntensityControl != null)
            {
                _lightIntensityControl.onValueChanged.AddListener(OnLightIntensityChanged);
            }
            
            if (_ventilationToggle != null)
            {
                _ventilationToggle.onValueChanged.AddListener(OnVentilationToggled);
            }
            
            // Plant details
            if (_detailedViewButton != null)
            {
                _detailedViewButton.onClick.AddListener(OnDetailedViewClicked);
            }
        }
        
        private void InitializeUIState()
        {
            // Set default monitoring toggles
            if (_healthIndicatorsToggle != null) _healthIndicatorsToggle.isOn = true;
            if (_growthStageToggle != null) _growthStageToggle.isOn = true;
            if (_environmentalNeedsToggle != null) _environmentalNeedsToggle.isOn = false;
            if (_harvestReadinessToggle != null) _harvestReadinessToggle.isOn = true;
            
            // Set default environmental controls
            if (_temperatureControl != null) _temperatureControl.value = 75f; // 75°F default
            if (_humidityControl != null) _humidityControl.value = 60f; // 60% humidity default
            if (_lightIntensityControl != null) _lightIntensityControl.value = 80f; // 80% light intensity
            if (_ventilationToggle != null) _ventilationToggle.isOn = true;
            
            // Initialize cultivation overview
            UpdateCultivationOverview();
        }
        
        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }
            
            UpdateOverlayVisibility(eventData.NewMode);
        }
        
        private void UpdateOverlayVisibility(GameplayMode currentMode)
        {
            bool shouldShowOverlay = currentMode == GameplayMode.Cultivation;
            
            if (_isCultivationModeActive == shouldShowOverlay) return;
            
            _isCultivationModeActive = shouldShowOverlay;
            
            // Show/hide main overlay panels
            if (_plantMonitoringPanel != null)
            {
                _plantMonitoringPanel.SetActive(shouldShowOverlay && _enablePlantMonitoring);
            }
            
            if (_careToolsPanel != null)
            {
                _careToolsPanel.SetActive(shouldShowOverlay && _enableCareTools);
            }
            
            if (_plantDetailsPanel != null)
            {
                _plantDetailsPanel.SetActive(shouldShowOverlay && _showPlantDetails);
            }
            
            if (_environmentPanel != null)
            {
                _environmentPanel.SetActive(shouldShowOverlay);
            }
            
            if (_overviewPanel != null)
            {
                _overviewPanel.SetActive(shouldShowOverlay);
            }
            
            // Handle visual indicators
            if (shouldShowOverlay)
            {
                RefreshPlantList();
                ShowPlantIndicators();
            }
            else
            {
                HidePlantIndicators();
            }
            
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Cultivation mode overlay {(shouldShowOverlay ? "shown" : "hidden")}");
            }
        }
        
        private void RefreshPlantList()
        {
            _monitoredPlants.Clear();
            
            // Find all plant objects in the scene
            GameObject[] plantObjects = /* TODO: Replace GameObject.Find */ new GameObject[0];
            foreach (var plantObj in plantObjects)
            {
                _monitoredPlants.Add(new PlantInfo(plantObj.transform));
            }
            
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Refreshed plant list: {_monitoredPlants.Count} plants found");
            }
        }
        
        private void ShowPlantIndicators()
        {
            if (_visualIndicatorsRoot == null || _indicatorPrefab == null) return;
            
            foreach (var plantInfo in _monitoredPlants)
            {
                if (!_plantIndicators.ContainsKey(plantInfo.plantTransform))
                {
                    CreatePlantIndicator(plantInfo);
                }
                
                UpdatePlantIndicatorVisual(plantInfo);
            }
        }
        
        private void HidePlantIndicators()
        {
            foreach (var indicator in _plantIndicators.Values)
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
            }
        }
        
        private void CreatePlantIndicator(PlantInfo plantInfo)
        {
            var indicatorObj = Instantiate(_indicatorPrefab, _visualIndicatorsRoot.transform);
            indicatorObj.transform.position = plantInfo.plantTransform.position + Vector3.up * 2f; // Above plant
            
            // Add click handler for plant selection
            var button = indicatorObj.GetComponent<Button>();
            if (button == null)
            {
                button = indicatorObj.AddComponent<Button>();
            }
            
            button.onClick.AddListener(() => SelectPlant(plantInfo.plantTransform));
            
            _plantIndicators[plantInfo.plantTransform] = indicatorObj;
        }
        
        private void UpdatePlantIndicatorVisual(PlantInfo plantInfo)
        {
            if (!_plantIndicators.TryGetValue(plantInfo.plantTransform, out var indicator)) return;
            
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer == null) return;
            
            // Choose material based on plant status
            Material targetMaterial = _healthyPlantMaterial;
            
            if (plantInfo.readyToHarvest && _harvestReadyMaterial != null)
            {
                targetMaterial = _harvestReadyMaterial;
            }
            else if (plantInfo.health < 0.5f && _unhealthyPlantMaterial != null)
            {
                targetMaterial = _unhealthyPlantMaterial;
            }
            
            if (targetMaterial != null)
            {
                renderer.material = targetMaterial;
            }
            
            // Show/hide indicator based on toggle states
            bool shouldShow = true;
            
            if (_healthIndicatorsToggle != null && !_healthIndicatorsToggle.isOn && plantInfo.health < 0.8f)
            {
                shouldShow = false;
            }
            
            if (_harvestReadinessToggle != null && !_harvestReadinessToggle.isOn && plantInfo.readyToHarvest)
            {
                shouldShow = false;
            }
            
            indicator.SetActive(shouldShow);
        }
        
        private void UpdatePlantMonitoring()
        {
            _monitoringUpdateTimer += Time.deltaTime;
            
            if (_monitoringUpdateTimer >= MONITORING_UPDATE_INTERVAL)
            {
                _monitoringUpdateTimer = 0f;
                
                // Update plant information (placeholder - would read from actual plant components)
                foreach (var plantInfo in _monitoredPlants)
                {
                    UpdatePlantInfo(plantInfo);
                }
                
                // Update visual indicators
                if (_healthIndicatorsToggle != null && _healthIndicatorsToggle.isOn)
                {
                    UpdatePlantIndicators();
                }
                
                // Update cultivation overview
                UpdateCultivationOverview();
                
                // Update selected plant details
                if (_selectedPlant != null)
                {
                    UpdateSelectedPlantDetails();
                }
            }
        }
        
        private void UpdatePlantInfo(PlantInfo plantInfo)
        {
            // Placeholder implementation - in real game would read from plant components
            if (plantInfo.plantTransform == null) return;
            
            // Simulate gradual changes
            plantInfo.health = Mathf.Clamp01(plantInfo.health + Random.Range(-0.02f, 0.03f));
            plantInfo.needsWater = Random.value < 0.1f;
            plantInfo.needsNutrients = Random.value < 0.05f;
            plantInfo.environmentalScore = Mathf.Clamp01(plantInfo.environmentalScore + Random.Range(-0.01f, 0.02f));
        }
        
        private void UpdatePlantIndicators()
        {
            foreach (var plantInfo in _monitoredPlants)
            {
                UpdatePlantIndicatorVisual(plantInfo);
            }
        }
        
        private void UpdateCultivationOverview()
        {
            int totalPlants = _monitoredPlants.Count;
            int healthyPlants = 0;
            int plantsNeedingCare = 0;
            int readyToHarvest = 0;
            float totalHealth = 0f;
            
            foreach (var plantInfo in _monitoredPlants)
            {
                totalHealth += plantInfo.health;
                
                if (plantInfo.health >= 0.8f) healthyPlants++;
                if (plantInfo.needsWater || plantInfo.needsNutrients || plantInfo.health < 0.6f) plantsNeedingCare++;
                if (plantInfo.readyToHarvest) readyToHarvest++;
            }
            
            float overallHealth = totalPlants > 0 ? totalHealth / totalPlants : 0f;
            
            // Update UI text elements
            if (_totalPlantsCount != null) _totalPlantsCount.text = totalPlants.ToString();
            if (_healthyPlantsCount != null) _healthyPlantsCount.text = healthyPlants.ToString();
            if (_plantsNeedingCare != null) _plantsNeedingCare.text = plantsNeedingCare.ToString();
            if (_readyToHarvestCount != null) _readyToHarvestCount.text = readyToHarvest.ToString();
            
            // Update overall health bar
            if (_overallHealthBar != null)
            {
                _overallHealthBar.SetProgress(overallHealth);
            }
        }
        
        private void UpdateEnvironmentalReadings()
        {
            if (_environmentalReadings == null) return;
            
            float temp = _temperatureControl?.value ?? 75f;
            float humidity = _humidityControl?.value ?? 60f;
            float light = _lightIntensityControl?.value ?? 80f;
            bool ventilation = _ventilationToggle?.isOn ?? true;
            
            _environmentalReadings.text = $"Temperature: {temp:F0}°F\nHumidity: {humidity:F0}%\nLight: {light:F0}%\nVentilation: {(ventilation ? "ON" : "OFF")}";
        }
        
        private void SelectPlant(Transform plant)
        {
            _selectedPlant = plant;
            UpdateSelectedPlantDetails();
            
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Selected plant: {plant.name}");
            }
        }
        
        private void UpdateSelectedPlantDetails()
        {
            if (_selectedPlant == null) return;
            
            var plantInfo = _monitoredPlants.Find(p => p.plantTransform == _selectedPlant);
            if (plantInfo == null) return;
            
            // Update plant details UI
            if (_selectedPlantName != null) _selectedPlantName.text = plantInfo.plantName;
            if (_plantHealthStatus != null) _plantHealthStatus.text = $"Health: {(plantInfo.health * 100):F0}%";
            if (_growthStageInfo != null) _growthStageInfo.text = $"Stage: {plantInfo.growthStage}";
            if (_environmentalStatus != null) _environmentalStatus.text = $"Environment: {(plantInfo.environmentalScore * 100):F0}%";
        }
        
        private void CleanupIndicators()
        {
            foreach (var indicator in _plantIndicators.Values)
            {
                if (indicator != null)
                {
                    DestroyImmediate(indicator);
                }
            }
            _plantIndicators.Clear();
        }
        
        #region UI Event Handlers
        
        private void OnHealthIndicatorsToggled(bool enabled)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Health indicators toggled: {enabled}");
            }
            
            UpdatePlantIndicators();
        }
        
        private void OnGrowthStageToggled(bool enabled)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Growth stage indicators toggled: {enabled}");
            }
        }
        
        private void OnEnvironmentalNeedsToggled(bool enabled)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Environmental needs indicators toggled: {enabled}");
            }
        }
        
        private void OnHarvestReadinessToggled(bool enabled)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Harvest readiness indicators toggled: {enabled}");
            }
            
            UpdatePlantIndicators();
        }
        
        private void OnCareToolSelected(string toolName)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Care tool selected: {toolName}");
            }
            
            // Placeholder for tool activation logic
        }
        
        private void OnTemperatureChanged(float temperature)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Temperature set to: {temperature:F0}°F");
            }
        }
        
        private void OnHumidityChanged(float humidity)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Humidity set to: {humidity:F0}%");
            }
        }
        
        private void OnLightIntensityChanged(float intensity)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Light intensity set to: {intensity:F0}%");
            }
        }
        
        private void OnVentilationToggled(bool enabled)
        {
            if (_debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Ventilation: {(enabled ? "ON" : "OFF")}");
            }
        }
        
        private void OnDetailedViewClicked()
        {
            if (_selectedPlant != null && _debugMode)
            {
                ChimeraLogger.Log($"[CultivationModeOverlay] Detailed view requested for: {_selectedPlant.name}");
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually refresh the cultivation overlay
        /// </summary>
        public void RefreshOverlay()
        {
            if (_isInitialized && _modeController != null)
            {
                RefreshPlantList();
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                if (_debugMode)
                {
                    ChimeraLogger.Log("[CultivationModeOverlay] Overlay refreshed manually");
                }
            }
        }
        
        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            ChimeraLogger.Log($"[CultivationModeOverlay] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get current cultivation state
        /// </summary>
        public bool IsCultivationModeActive => _isCultivationModeActive;
        public int MonitoredPlantsCount => _monitoredPlants.Count;
        public Transform SelectedPlant => _selectedPlant;
        
        #endregion
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for testing cultivation mode
        /// </summary>
        [ContextMenu("Test Cultivation Mode Toggle")]
        private void TestCultivationModeToggle()
        {
            if (Application.isPlaying && _modeController != null)
            {
                var currentMode = _modeController.CurrentMode;
                var newMode = currentMode == GameplayMode.Cultivation ? GameplayMode.Construction : GameplayMode.Cultivation;
                _modeController.SetMode(newMode, "Debug Test");
            }
            else
            {
                ChimeraLogger.Log("[CultivationModeOverlay] Test only works during play mode with initialized controller");
            }
        }
        
        #endif
    
    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;
    
    public virtual void OnRegistered() 
    { 
        // Override in derived classes if needed
    }
    
    public virtual void OnUnregistered() 
    { 
        // Override in derived classes if needed
    }

}
    
    /// <summary>
    /// Simple progress bar component for cultivation overview
    /// </summary>
    [System.Serializable]
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _percentageText;
        
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            if (_fillImage != null)
            {
                _fillImage.fillAmount = progress;
            }
            
            if (_percentageText != null)
            {
                _percentageText.text = $"{(progress * 100):F0}%";
            }
        }
    }
}