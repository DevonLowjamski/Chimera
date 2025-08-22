using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Systems.Gameplay;
using ProjectChimera.Data.Events;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Genetics mode overlay system - shows heatmaps and genetic analysis tools
    /// Responds to gameplay mode changes and provides genetics-specific visualizations
    /// Phase 2 implementation following roadmap requirements
    /// </summary>
    public class GeneticsModeOverlay : MonoBehaviour
    {
        [Header("Overlay Configuration")]
        [SerializeField] private bool _enableHeatmapOverlay = true;
        [SerializeField] private bool _enableGeneticTools = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Heatmap Overlays")]
        [SerializeField] private GameObject _heatmapPanel;
        [SerializeField] private Toggle _potencyHeatmapToggle;
        [SerializeField] private Toggle _yieldHeatmapToggle;
        [SerializeField] private Toggle _healthHeatmapToggle;
        [SerializeField] private Toggle _geneticDiversityToggle;
        [SerializeField] private Slider _heatmapIntensity;
        
        [Header("Genetic Analysis Tools")]
        [SerializeField] private GameObject _geneticsToolbar;
        [SerializeField] private Button _breedingAnalysisButton;
        [SerializeField] private Button _phenotypeViewButton;
        [SerializeField] private Button _traitMappingButton;
        [SerializeField] private Button _crossbreedingButton;
        
        [Header("Visualization Controls")]
        [SerializeField] private GameObject _visualizationPanel;
        [SerializeField] private Dropdown _heatmapTypeDropdown;
        [SerializeField] private Toggle _showLegendToggle;
        [SerializeField] private Toggle _animatedHeatmapToggle;
        [SerializeField] private Slider _updateFrequencySlider;
        
        [Header("Genetic Data Display")]
        [SerializeField] private GameObject _geneticDataPanel;
        [SerializeField] private Text _selectedPlantGenetics;
        [SerializeField] private Image _geneticProfileImage;
        [SerializeField] private Button _detailedAnalysisButton;
        
        [Header("Visual Overlays")]
        [SerializeField] private GameObject _heatmapOverlayRoot;
        [SerializeField] private Material _potencyHeatmapMaterial;
        [SerializeField] private Material _yieldHeatmapMaterial;
        [SerializeField] private Material _healthHeatmapMaterial;
        [SerializeField] private Material _diversityHeatmapMaterial;
        
        [Header("Event Channels")]
        [SerializeField] private ModeChangedEventSO _modeChangedEvent;
        
        // Services
        private IGameplayModeController _modeController;
        
        // State tracking
        private bool _isInitialized = false;
        private bool _isGeneticsModeActive = false;
        private HeatmapType _activeHeatmapType = HeatmapType.None;
        private float _heatmapUpdateTimer = 0f;
        
        // Heatmap management
        private Dictionary<HeatmapType, GameObject> _heatmapObjects = new Dictionary<HeatmapType, GameObject>();
        private Dictionary<HeatmapType, Material> _heatmapMaterials = new Dictionary<HeatmapType, Material>();
        private List<Transform> _monitoredPlants = new List<Transform>();
        
        // Heatmap types for genetics analysis
        public enum HeatmapType
        {
            None = 0,
            Potency = 1,
            Yield = 2,
            Health = 3,
            GeneticDiversity = 4
        }
        
        private void Start()
        {
            InitializeOverlay();
        }
        
        private void Update()
        {
            if (_isGeneticsModeActive && _animatedHeatmapToggle != null && _animatedHeatmapToggle.isOn)
            {
                UpdateAnimatedHeatmaps();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupHeatmapObjects();
        }
        
        private void InitializeOverlay()
        {
            try
            {
                // Get the gameplay mode controller service
                _modeController = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance.GetService<IGameplayModeController>();
                
                if (_modeController == null)
                {
                    Debug.LogError("[GeneticsModeOverlay] GameplayModeController service not found!");
                    return;
                }
                
                // Initialize heatmap materials dictionary
                InitializeHeatmapMaterials();
                
                // Subscribe to mode change events
                SubscribeToEvents();
                
                // Subscribe to UI control events
                SubscribeToUIControls();
                
                // Initialize overlay visibility based on current mode
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                // Set initial UI state
                InitializeUIState();
                
                // Find plants to monitor for heatmaps
                RefreshMonitoredPlants();
                
                _isInitialized = true;
                
                if (_debugMode)
                {
                    Debug.Log($"[GeneticsModeOverlay] Initialized with current mode: {_modeController.CurrentMode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GeneticsModeOverlay] Error during initialization: {ex.Message}");
            }
        }
        
        private void InitializeHeatmapMaterials()
        {
            _heatmapMaterials[HeatmapType.Potency] = _potencyHeatmapMaterial;
            _heatmapMaterials[HeatmapType.Yield] = _yieldHeatmapMaterial;
            _heatmapMaterials[HeatmapType.Health] = _healthHeatmapMaterial;
            _heatmapMaterials[HeatmapType.GeneticDiversity] = _diversityHeatmapMaterial;
        }
        
        private void SubscribeToEvents()
        {
            if (_modeChangedEvent != null)
            {
                _modeChangedEvent.Subscribe(OnModeChanged);
            }
            else
            {
                Debug.LogWarning("[GeneticsModeOverlay] ModeChangedEvent not assigned");
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
            // Heatmap toggles
            if (_potencyHeatmapToggle != null)
            {
                _potencyHeatmapToggle.onValueChanged.AddListener((enabled) => OnHeatmapToggleChanged(HeatmapType.Potency, enabled));
            }
            
            if (_yieldHeatmapToggle != null)
            {
                _yieldHeatmapToggle.onValueChanged.AddListener((enabled) => OnHeatmapToggleChanged(HeatmapType.Yield, enabled));
            }
            
            if (_healthHeatmapToggle != null)
            {
                _healthHeatmapToggle.onValueChanged.AddListener((enabled) => OnHeatmapToggleChanged(HeatmapType.Health, enabled));
            }
            
            if (_geneticDiversityToggle != null)
            {
                _geneticDiversityToggle.onValueChanged.AddListener((enabled) => OnHeatmapToggleChanged(HeatmapType.GeneticDiversity, enabled));
            }
            
            // Heatmap intensity
            if (_heatmapIntensity != null)
            {
                _heatmapIntensity.onValueChanged.AddListener(OnHeatmapIntensityChanged);
            }
            
            // Genetic analysis tools
            if (_breedingAnalysisButton != null)
            {
                _breedingAnalysisButton.onClick.AddListener(() => OnGeneticToolSelected("BreedingAnalysis"));
            }
            
            if (_phenotypeViewButton != null)
            {
                _phenotypeViewButton.onClick.AddListener(() => OnGeneticToolSelected("PhenotypeView"));
            }
            
            if (_traitMappingButton != null)
            {
                _traitMappingButton.onClick.AddListener(() => OnGeneticToolSelected("TraitMapping"));
            }
            
            if (_crossbreedingButton != null)
            {
                _crossbreedingButton.onClick.AddListener(() => OnGeneticToolSelected("Crossbreeding"));
            }
            
            // Visualization controls
            if (_heatmapTypeDropdown != null)
            {
                _heatmapTypeDropdown.onValueChanged.AddListener(OnHeatmapTypeDropdownChanged);
            }
            
            if (_showLegendToggle != null)
            {
                _showLegendToggle.onValueChanged.AddListener(OnShowLegendToggleChanged);
            }
            
            if (_animatedHeatmapToggle != null)
            {
                _animatedHeatmapToggle.onValueChanged.AddListener(OnAnimatedHeatmapToggleChanged);
            }
            
            if (_updateFrequencySlider != null)
            {
                _updateFrequencySlider.onValueChanged.AddListener(OnUpdateFrequencyChanged);
            }
            
            // Genetic data display
            if (_detailedAnalysisButton != null)
            {
                _detailedAnalysisButton.onClick.AddListener(OnDetailedAnalysisClicked);
            }
        }
        
        private void InitializeUIState()
        {
            // Set default heatmap toggles to off
            if (_potencyHeatmapToggle != null) _potencyHeatmapToggle.isOn = false;
            if (_yieldHeatmapToggle != null) _yieldHeatmapToggle.isOn = false;
            if (_healthHeatmapToggle != null) _healthHeatmapToggle.isOn = false;
            if (_geneticDiversityToggle != null) _geneticDiversityToggle.isOn = false;
            
            // Set default heatmap intensity
            if (_heatmapIntensity != null)
            {
                _heatmapIntensity.value = 0.8f; // Default 80% intensity
            }
            
            // Initialize dropdown with heatmap types
            if (_heatmapTypeDropdown != null)
            {
                _heatmapTypeDropdown.ClearOptions();
                var options = new List<string> { "None", "Potency", "Yield", "Health", "Genetic Diversity" };
                _heatmapTypeDropdown.AddOptions(options);
                _heatmapTypeDropdown.value = 0; // Default to "None"
            }
            
            // Set default visualization settings
            if (_showLegendToggle != null) _showLegendToggle.isOn = true;
            if (_animatedHeatmapToggle != null) _animatedHeatmapToggle.isOn = false;
            
            // Set default update frequency (1 second)
            if (_updateFrequencySlider != null)
            {
                _updateFrequencySlider.value = 1f;
            }
        }
        
        private void OnModeChanged(ModeChangeEventData eventData)
        {
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Mode changed: {eventData.PreviousMode} → {eventData.NewMode}");
            }
            
            UpdateOverlayVisibility(eventData.NewMode);
        }
        
        private void UpdateOverlayVisibility(GameplayMode currentMode)
        {
            bool shouldShowOverlay = currentMode == GameplayMode.Genetics;
            
            if (_isGeneticsModeActive == shouldShowOverlay) return;
            
            _isGeneticsModeActive = shouldShowOverlay;
            
            // Show/hide main overlay panels
            if (_heatmapPanel != null)
            {
                _heatmapPanel.SetActive(shouldShowOverlay && _enableHeatmapOverlay);
            }
            
            if (_geneticsToolbar != null)
            {
                _geneticsToolbar.SetActive(shouldShowOverlay && _enableGeneticTools);
            }
            
            if (_visualizationPanel != null)
            {
                _visualizationPanel.SetActive(shouldShowOverlay);
            }
            
            if (_geneticDataPanel != null)
            {
                _geneticDataPanel.SetActive(shouldShowOverlay);
            }
            
            // Handle heatmap overlays
            if (shouldShowOverlay)
            {
                RefreshMonitoredPlants();
                ShowActiveHeatmaps();
            }
            else
            {
                HideAllHeatmaps();
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Genetics mode overlay {(shouldShowOverlay ? "shown" : "hidden")}");
            }
        }
        
        private void RefreshMonitoredPlants()
        {
            _monitoredPlants.Clear();
            
            // Find all plant objects in the scene (placeholder implementation)
            var plantObjects = GameObject.FindGameObjectsWithTag("Plant");
            foreach (var plantObj in plantObjects)
            {
                _monitoredPlants.Add(plantObj.transform);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Refreshed monitored plants: {_monitoredPlants.Count} found");
            }
        }
        
        private void ShowActiveHeatmaps()
        {
            // Show heatmaps based on current toggle states
            if (_potencyHeatmapToggle != null && _potencyHeatmapToggle.isOn)
            {
                ShowHeatmap(HeatmapType.Potency);
            }
            
            if (_yieldHeatmapToggle != null && _yieldHeatmapToggle.isOn)
            {
                ShowHeatmap(HeatmapType.Yield);
            }
            
            if (_healthHeatmapToggle != null && _healthHeatmapToggle.isOn)
            {
                ShowHeatmap(HeatmapType.Health);
            }
            
            if (_geneticDiversityToggle != null && _geneticDiversityToggle.isOn)
            {
                ShowHeatmap(HeatmapType.GeneticDiversity);
            }
        }
        
        private void HideAllHeatmaps()
        {
            foreach (var heatmapType in System.Enum.GetValues(typeof(HeatmapType)))
            {
                if ((HeatmapType)heatmapType != HeatmapType.None)
                {
                    HideHeatmap((HeatmapType)heatmapType);
                }
            }
        }
        
        private void ShowHeatmap(HeatmapType heatmapType)
        {
            if (!_heatmapObjects.TryGetValue(heatmapType, out var heatmapObj))
            {
                // Create placeholder heatmap visualization
                CreatePlaceholderHeatmap(heatmapType);
                heatmapObj = _heatmapObjects[heatmapType];
            }
            
            if (heatmapObj != null)
            {
                heatmapObj.SetActive(true);
                UpdateHeatmapData(heatmapType);
            }
            
            _activeHeatmapType = heatmapType;
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] {heatmapType} heatmap shown");
            }
        }
        
        private void HideHeatmap(HeatmapType heatmapType)
        {
            if (_heatmapObjects.TryGetValue(heatmapType, out var heatmapObj) && heatmapObj != null)
            {
                heatmapObj.SetActive(false);
            }
            
            if (_activeHeatmapType == heatmapType)
            {
                _activeHeatmapType = HeatmapType.None;
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] {heatmapType} heatmap hidden");
            }
        }
        
        private void CreatePlaceholderHeatmap(HeatmapType heatmapType)
        {
            var heatmapObj = new GameObject($"Heatmap_{heatmapType}");
            heatmapObj.transform.SetParent(_heatmapOverlayRoot?.transform ?? transform);
            
            // Add visual components for heatmap (placeholder implementation)
            var meshRenderer = heatmapObj.AddComponent<MeshRenderer>();
            var meshFilter = heatmapObj.AddComponent<MeshFilter>();
            
            // Create a simple quad mesh for the heatmap
            meshFilter.mesh = CreateHeatmapQuad();
            
            // Apply appropriate material
            if (_heatmapMaterials.TryGetValue(heatmapType, out var material) && material != null)
            {
                meshRenderer.material = material;
            }
            
            _heatmapObjects[heatmapType] = heatmapObj;
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Created placeholder {heatmapType} heatmap");
            }
        }
        
        private Mesh CreateHeatmapQuad()
        {
            // Create a simple quad mesh for heatmap visualization
            var mesh = new Mesh();
            
            Vector3[] vertices = {
                new Vector3(-5, 0, -5),
                new Vector3( 5, 0, -5),
                new Vector3( 5, 0,  5),
                new Vector3(-5, 0,  5)
            };
            
            int[] triangles = { 0, 1, 2, 0, 2, 3 };
            
            Vector2[] uvs = {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private void UpdateHeatmapData(HeatmapType heatmapType)
        {
            // Placeholder implementation for updating heatmap data based on plant genetics
            // In a real implementation, this would analyze actual plant genetic data
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Updated {heatmapType} heatmap data for {_monitoredPlants.Count} plants");
            }
        }
        
        private void UpdateAnimatedHeatmaps()
        {
            if (_updateFrequencySlider == null) return;
            
            _heatmapUpdateTimer += Time.deltaTime;
            
            if (_heatmapUpdateTimer >= _updateFrequencySlider.value)
            {
                _heatmapUpdateTimer = 0f;
                
                // Update active heatmap with new data
                if (_activeHeatmapType != HeatmapType.None)
                {
                    UpdateHeatmapData(_activeHeatmapType);
                }
            }
        }
        
        private void CleanupHeatmapObjects()
        {
            foreach (var heatmapObj in _heatmapObjects.Values)
            {
                if (heatmapObj != null)
                {
                    DestroyImmediate(heatmapObj);
                }
            }
            _heatmapObjects.Clear();
        }
        
        #region UI Event Handlers
        
        private void OnHeatmapToggleChanged(HeatmapType heatmapType, bool enabled)
        {
            if (!_isGeneticsModeActive) return;
            
            if (enabled)
            {
                // Hide other heatmaps (only one active at a time for clarity)
                HideAllHeatmaps();
                ShowHeatmap(heatmapType);
            }
            else
            {
                HideHeatmap(heatmapType);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] {heatmapType} heatmap toggled: {enabled}");
            }
        }
        
        private void OnHeatmapIntensityChanged(float intensity)
        {
            // Apply intensity to all active heatmaps
            foreach (var kvp in _heatmapObjects)
            {
                if (kvp.Value != null && kvp.Value.activeInHierarchy)
                {
                    var renderer = kvp.Value.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        var color = renderer.material.color;
                        color.a = intensity;
                        renderer.material.color = color;
                    }
                }
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Heatmap intensity changed: {intensity:F2}");
            }
        }
        
        private void OnGeneticToolSelected(string toolName)
        {
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Genetic tool selected: {toolName}");
            }
            
            // Placeholder for genetic tool activation
            UpdateGeneticDataDisplay(toolName);
        }
        
        private void OnHeatmapTypeDropdownChanged(int selectedIndex)
        {
            var heatmapType = (HeatmapType)selectedIndex;
            
            // Hide all heatmaps first
            HideAllHeatmaps();
            
            // Show selected heatmap if not "None"
            if (heatmapType != HeatmapType.None)
            {
                ShowHeatmap(heatmapType);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Heatmap type changed to: {heatmapType}");
            }
        }
        
        private void OnShowLegendToggleChanged(bool showLegend)
        {
            // Toggle heatmap legend visibility (placeholder)
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Heatmap legend visibility: {showLegend}");
            }
        }
        
        private void OnAnimatedHeatmapToggleChanged(bool enabled)
        {
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Animated heatmap: {enabled}");
            }
        }
        
        private void OnUpdateFrequencyChanged(float frequency)
        {
            if (_debugMode)
            {
                Debug.Log($"[GeneticsModeOverlay] Update frequency changed: {frequency:F1}s");
            }
        }
        
        private void OnDetailedAnalysisClicked()
        {
            if (_debugMode)
            {
                Debug.Log("[GeneticsModeOverlay] Detailed genetic analysis requested");
            }
            
            // Placeholder for detailed analysis window
        }
        
        #endregion
        
        #region Helper Methods
        
        private void UpdateGeneticDataDisplay(string selectedTool)
        {
            // Placeholder implementation for updating genetic data display
            if (_selectedPlantGenetics != null)
            {
                _selectedPlantGenetics.text = $"Genetic Analysis: {selectedTool}\nMode: {_activeHeatmapType}\nPlants Monitored: {_monitoredPlants.Count}";
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Manually refresh the genetics overlay (for debugging)
        /// </summary>
        public void RefreshOverlay()
        {
            if (_isInitialized && _modeController != null)
            {
                RefreshMonitoredPlants();
                UpdateOverlayVisibility(_modeController.CurrentMode);
                
                if (_debugMode)
                {
                    Debug.Log("[GeneticsModeOverlay] Overlay refreshed manually");
                }
            }
        }
        
        /// <summary>
        /// Enable/disable debug mode at runtime
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            Debug.Log($"[GeneticsModeOverlay] Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get current overlay state
        /// </summary>
        public bool IsGeneticsModeActive => _isGeneticsModeActive;
        public HeatmapType ActiveHeatmapType => _activeHeatmapType;
        public int MonitoredPlantsCount => _monitoredPlants.Count;
        
        #endregion
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Editor-only method for testing genetics mode toggle
        /// </summary>
        [ContextMenu("Test Genetics Mode Toggle")]
        private void TestGeneticsModeToggle()
        {
            if (Application.isPlaying && _modeController != null)
            {
                var currentMode = _modeController.CurrentMode;
                var newMode = currentMode == GameplayMode.Genetics ? GameplayMode.Cultivation : GameplayMode.Genetics;
                _modeController.SetMode(newMode, "Debug Test");
            }
            else
            {
                Debug.Log("[GeneticsModeOverlay] Test only works during play mode with initialized controller");
            }
        }
        
        #endif
    }
}