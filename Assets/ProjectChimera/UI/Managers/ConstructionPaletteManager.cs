using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.UI.Panels;
using ProjectChimera.UI.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Systems.Construction;
using ProjectChimera.Core;

namespace ProjectChimera.UI.Managers
{
    /// <summary>
    /// Manager for the Construction Palette UI system in Project Chimera Phase 4.
    /// Handles palette lifecycle, integration with construction systems, and UI state management.
    /// 
    /// DEPENDENCY INJECTION: Uses constructor injection for testability and explicit dependencies.
    /// </summary>
    public class ConstructionPaletteManager : MonoBehaviour
    {
        [Header("Palette Configuration")]
        [SerializeField] private bool _showPaletteOnStart = true;
        [SerializeField] private bool _hideOnEscape = true;
        
        // Dependencies resolved via DI container (explicit dependencies for testability)
        private ConstructionPalettePanel _palettePanel;
        private GridPlacementController _placementController;
        private UIManager _uiManager;
        
        /// <summary>
        /// Initialize dependencies explicitly for testability.
        /// For testing: call this method with mock dependencies.
        /// For runtime: dependencies are resolved automatically via DI container.
        /// </summary>
        public void Initialize(ConstructionPalettePanel palettePanel = null, 
                              GridPlacementController placementController = null, 
                              UIManager uiManager = null)
        {
            _palettePanel = palettePanel;
            _placementController = placementController;
            _uiManager = uiManager;
        }
        
        [Header("Keyboard Shortcuts")]
        [SerializeField] private KeyCode _togglePaletteKey = KeyCode.B;
        [SerializeField] private KeyCode _constructionTabKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode _schematicsTabKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _toolsTabKey = KeyCode.Alpha3;
        
        [Header("Auto-Discovery")]
        [SerializeField] private bool _autoDiscoverSchematics = true;
        [SerializeField] private string _schematicsPath = "Assets/ProjectChimera/Data/Construction/Schematics";
        
        // State
        private bool _isInitialized = false;
        private bool _isPaletteVisible = false;
        private List<SchematicSO> _discoveredSchematics = new List<SchematicSO>();
        
        // Events
        public System.Action<bool> OnPaletteVisibilityChanged;
        public System.Action<PaletteTab> OnActiveTabChanged;
        public System.Action<SchematicSO> OnSchematicApplied;
        
        private void Awake()
        {
            ResolveDependencies();
        }
        
        private void Start()
        {
            InitializePaletteManager();
        }
        
        private void Update()
        {
            HandleKeyboardInput();
        }
        
        /// <summary>
        /// Resolve dependencies from DI container if not explicitly provided.
        /// This method supports both explicit dependency injection (for testing) 
        /// and automatic resolution (for runtime).
        /// </summary>
        private void ResolveDependencies()
        {
            if (_palettePanel == null)
            {
                _palettePanel = ServiceContainerFactory.Instance?.TryResolve<ConstructionPalettePanel>();
                if (_palettePanel == null)
                {
                    Debug.LogError("[ConstructionPaletteManager] ConstructionPalettePanel not registered in DI container. Explicit dependency injection required for testing.");
                }
            }
            
            if (_placementController == null)
            {
                _placementController = ServiceContainerFactory.Instance?.TryResolve<GridPlacementController>();
                if (_placementController == null)
                {
                    Debug.LogError("[ConstructionPaletteManager] GridPlacementController not registered in DI container. Construction placement will not function properly.");
                }
            }
            
            if (_uiManager == null)
            {
                _uiManager = ServiceContainerFactory.Instance?.TryResolve<UIManager>();
                if (_uiManager == null)
                {
                    Debug.LogWarning("[ConstructionPaletteManager] UIManager not registered in DI container. Some UI integration features may not work.");
                }
            }
        }
        
        /// <summary>
        /// Initialize the palette manager
        /// </summary>
        private void InitializePaletteManager()
        {
            if (_isInitialized) return;
            
            SetupEventHandlers();
            
            if (_autoDiscoverSchematics)
            {
                DiscoverSchematics();
            }
            
            if (_showPaletteOnStart)
            {
                ShowPalette();
            }
            else
            {
                HidePalette();
            }
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Set up event handlers for palette interactions
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_palettePanel != null)
            {
                _palettePanel.OnSchematicSelected += OnSchematicSelectedFromPalette;
                _palettePanel.OnTabChanged += OnTabChangedFromPalette;
                _palettePanel.OnItemSelected += OnItemSelectedFromPalette;
            }
            
            if (_placementController != null)
            {
                _placementController.OnSchematicApplied += OnSchematicAppliedFromController;
            }
        }
        
        /// <summary>
        /// Auto-discover schematic assets in the project
        /// </summary>
        private void DiscoverSchematics()
        {
            _discoveredSchematics.Clear();
            
            // Load all SchematicSO assets from Resources
            var schematics = Resources.LoadAll<SchematicSO>("");
            _discoveredSchematics.AddRange(schematics);
            
            // Add discovered schematics to palette
            if (_palettePanel != null)
            {
                foreach (var schematic in _discoveredSchematics)
                {
                    _palettePanel.AddSchematic(schematic);
                }
            }
            
            Debug.Log($"[ConstructionPaletteManager] Discovered {_discoveredSchematics.Count} schematics");
        }
        
        /// <summary>
        /// Handle keyboard input for palette operations
        /// </summary>
        private void HandleKeyboardInput()
        {
            // Toggle palette visibility
            if (Input.GetKeyDown(_togglePaletteKey))
            {
                TogglePalette();
            }
            
            // Tab shortcuts (only when palette is visible)
            if (_isPaletteVisible && _palettePanel != null)
            {
                if (Input.GetKeyDown(_constructionTabKey))
                {
                    _palettePanel.SwitchToTab(PaletteTab.Construction);
                }
                else if (Input.GetKeyDown(_schematicsTabKey))
                {
                    _palettePanel.SwitchToTab(PaletteTab.Schematics);
                }
                else if (Input.GetKeyDown(_toolsTabKey))
                {
                    _palettePanel.SwitchToTab(PaletteTab.Tools);
                }
            }
            
            // Hide on Escape
            if (_hideOnEscape && Input.GetKeyDown(KeyCode.Escape) && _isPaletteVisible)
            {
                HidePalette();
            }
        }
        
        /// <summary>
        /// Show the construction palette
        /// </summary>
        public void ShowPalette()
        {
            if (_palettePanel != null)
            {
                _palettePanel.Show();
                _isPaletteVisible = true;
                OnPaletteVisibilityChanged?.Invoke(true);
            }
        }
        
        /// <summary>
        /// Hide the construction palette
        /// </summary>
        public void HidePalette()
        {
            if (_palettePanel != null)
            {
                _palettePanel.Hide();
                _isPaletteVisible = false;
                OnPaletteVisibilityChanged?.Invoke(false);
            }
        }
        
        /// <summary>
        /// Toggle palette visibility
        /// </summary>
        public void TogglePalette()
        {
            if (_isPaletteVisible)
            {
                HidePalette();
            }
            else
            {
                ShowPalette();
            }
        }
        
        /// <summary>
        /// Switch to specific tab in the palette
        /// </summary>
        public void SwitchToTab(PaletteTab tab)
        {
            if (_palettePanel != null)
            {
                _palettePanel.SwitchToTab(tab);
            }
        }
        
        /// <summary>
        /// Add a schematic to the palette
        /// </summary>
        public void AddSchematic(SchematicSO schematic)
        {
            if (schematic != null && _palettePanel != null)
            {
                _palettePanel.AddSchematic(schematic);
                
                if (!_discoveredSchematics.Contains(schematic))
                {
                    _discoveredSchematics.Add(schematic);
                }
            }
        }
        
        /// <summary>
        /// Remove a schematic from the palette
        /// </summary>
        public void RemoveSchematic(SchematicSO schematic)
        {
            if (_palettePanel != null)
            {
                _palettePanel.RemoveSchematic(schematic);
                _discoveredSchematics.Remove(schematic);
            }
        }
        
        /// <summary>
        /// Refresh the entire palette
        /// </summary>
        public void RefreshPalette()
        {
            if (_palettePanel != null)
            {
                _palettePanel.RefreshPalette();
            }
        }
        
        /// <summary>
        /// Handle schematic selection from palette
        /// </summary>
        private void OnSchematicSelectedFromPalette(SchematicSO schematic)
        {
            Debug.Log($"[ConstructionPaletteManager] Schematic selected: {schematic.SchematicName}");
            
            // Switch to schematics tab if not already active
            if (_palettePanel != null)
            {
                _palettePanel.SwitchToTab(PaletteTab.Schematics);
            }
        }
        
        /// <summary>
        /// Handle tab change from palette
        /// </summary>
        private void OnTabChangedFromPalette(PaletteTab tab)
        {
            Debug.Log($"[ConstructionPaletteManager] Tab changed to: {tab}");
            OnActiveTabChanged?.Invoke(tab);
        }
        
        /// <summary>
        /// Handle item selection from palette
        /// </summary>
        private void OnItemSelectedFromPalette(PaletteItem item)
        {
            Debug.Log($"[ConstructionPaletteManager] Item selected: {item.Name} ({item.Type})");
        }
        
        /// <summary>
        /// Handle schematic application from placement controller
        /// </summary>
        private void OnSchematicAppliedFromController(SchematicSO schematic, Vector3Int position, List<GameObject> objects)
        {
            Debug.Log($"[ConstructionPaletteManager] Schematic applied: {schematic.SchematicName} at {position} with {objects.Count} objects");
            OnSchematicApplied?.Invoke(schematic);
        }
        
        /// <summary>
        /// Get current palette visibility state
        /// </summary>
        public bool IsPaletteVisible => _isPaletteVisible;
        
        /// <summary>
        /// Get current active tab
        /// </summary>
        public PaletteTab CurrentTab => _palettePanel?.CurrentTab ?? PaletteTab.Construction;
        
        /// <summary>
        /// Get list of discovered schematics
        /// </summary>
        public List<SchematicSO> DiscoveredSchematics => new List<SchematicSO>(_discoveredSchematics);
        
        /// <summary>
        /// Enable or disable palette auto-discovery
        /// </summary>
        public void SetAutoDiscovery(bool enabled)
        {
            _autoDiscoverSchematics = enabled;
            
            if (enabled && _isInitialized)
            {
                DiscoverSchematics();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event handlers
            if (_palettePanel != null)
            {
                _palettePanel.OnSchematicSelected -= OnSchematicSelectedFromPalette;
                _palettePanel.OnTabChanged -= OnTabChangedFromPalette;
                _palettePanel.OnItemSelected -= OnItemSelectedFromPalette;
            }
            
            if (_placementController != null)
            {
                _placementController.OnSchematicApplied -= OnSchematicAppliedFromController;
            }
        }
    }
}