using UnityEngine;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Core utility rendering infrastructure and component coordination.
    /// Manages rendering configuration, system initialization, and component orchestration.
    /// </summary>
    public class UtilityRenderingCore : ChimeraManager, ITickable
    {
        [Header("Utility Visualization Configuration")]
        [SerializeField] private bool _enableUtilityVisualization = true;
        [SerializeField] private bool _showAllUtilityTypes = true;
        [SerializeField] private bool _showFlowDirections = true;
        [SerializeField] private bool _showCapacityIndicators = true;
        [SerializeField] private bool _showConnectionValidation = true;

        [Header("Rendering Settings")]
        [SerializeField] private LayerMask _utilityLayer = 31;
        [SerializeField] private Material _connectionLineMaterial;
        [SerializeField] private Material _flowIndicatorMaterial;
        [SerializeField] private Material _capacityIndicatorMaterial;

        [Header("Visual Properties")]
        [SerializeField] private float _connectionLineWidth = 0.1f;
        [SerializeField] private float _flowArrowSize = 0.3f;
        [SerializeField] private float _capacityIndicatorSize = 0.5f;
        [SerializeField] private Color _electricalColor = Color.yellow;
        [SerializeField] private Color _waterColor = Color.blue;
        [SerializeField] private Color _airColor = Color.cyan;
        [SerializeField] private Color _dataColor = Color.green;
        [SerializeField] private Color _validConnectionColor = Color.green;
        [SerializeField] private Color _invalidConnectionColor = Color.red;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private float _maxUtilityRenderDistance = 25f;
        [SerializeField] private int _maxVisibleConnections = 200;
        [SerializeField] private bool _enableConnectionCulling = true;

        // System references
        private BlueprintOverlayRenderer _overlayRenderer;
        private UnityEngine.Camera _utilityCamera;

        // Rendering components
        protected UtilityNodeRenderer _nodeRenderer;
        protected UtilityConnectionRenderer _connectionRenderer;
        protected UtilityAnimationController _animationController;
        protected UtilityValidationRenderer _validationRenderer;

        // Core state
        private Dictionary<UtilityType, Color> _utilityColors = new Dictionary<UtilityType, Color>();
        private bool _isUtilityVisualizationActive = false;

        // Properties
        public bool UtilityVisualizationEnabled => _enableUtilityVisualization;
        public LayerMask UtilityLayer => _utilityLayer;
        public UnityEngine.Camera UtilityCamera => _utilityCamera;
        public float ConnectionLineWidth => _connectionLineWidth;
        public float FlowArrowSize => _flowArrowSize;
        public float CapacityIndicatorSize => _capacityIndicatorSize;
        public bool ShowFlowDirections => _showFlowDirections;
        public bool ShowCapacityIndicators => _showCapacityIndicators;
        public bool ShowConnectionValidation => _showConnectionValidation;
        public float MaxUtilityRenderDistance => _maxUtilityRenderDistance;
        public int MaxVisibleConnections => _maxVisibleConnections;
        public bool EnableLOD => _enableLOD;

        // Events
        public System.Action<UtilityType, bool> OnUtilityTypeToggled;
        public System.Action OnRenderingConfigurationChanged;

        public override ManagerPriority Priority => ManagerPriority.Normal;

        protected override void OnManagerInitialize()
        {
            InitializeUtilitySystem();
            SetupUtilityCamera();
            CreateUtilityMaterials();
            InitializeUtilityColors();
            SetupRenderingComponents();

            LogInfo($"UtilityRenderingCore initialized - Max connections: {_maxVisibleConnections}");
        }

        public void Tick(float deltaTime)
        {
            if (!_enableUtilityVisualization) return;

            // Update rendering components
            _animationController?.UpdateAnimations(deltaTime);
            _nodeRenderer?.UpdateLOD();
            _connectionRenderer?.UpdateConnections();
            _validationRenderer?.UpdateValidation();
        }

        /// <summary>
        /// Get utility color for specified type
        /// </summary>
        public Color GetUtilityColor(UtilityType utilityType)
        {
            return _utilityColors.TryGetValue(utilityType, out var color) ? color : Color.white;
        }

        /// <summary>
        /// Get material for rendering component type
        /// </summary>
        public Material GetRenderingMaterial(UtilityRenderingMaterialType materialType)
        {
            return materialType switch
            {
                UtilityRenderingMaterialType.Connection => _connectionLineMaterial,
                UtilityRenderingMaterialType.FlowIndicator => _flowIndicatorMaterial,
                UtilityRenderingMaterialType.CapacityIndicator => _capacityIndicatorMaterial,
                _ => _connectionLineMaterial
            };
        }

        /// <summary>
        /// Create utility material with specified properties
        /// </summary>
        public Material CreateUtilityMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            material.name = name;
            material.color = color;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
            return material;
        }

        /// <summary>
        /// Toggle visibility of specific utility type
        /// </summary>
        public void SetUtilityTypeVisible(UtilityType utilityType, bool visible)
        {
            _nodeRenderer?.SetUtilityTypeVisible(utilityType, visible);
            _connectionRenderer?.SetUtilityTypeVisible(utilityType, visible);
            OnUtilityTypeToggled?.Invoke(utilityType, visible);
        }

        /// <summary>
        /// Update rendering configuration
        /// </summary>
        public void UpdateRenderingConfiguration(UtilityRenderingConfig config)
        {
            _enableUtilityVisualization = config.EnableVisualization;
            _showFlowDirections = config.ShowFlowDirections;
            _showCapacityIndicators = config.ShowCapacityIndicators;
            _showConnectionValidation = config.ShowConnectionValidation;
            _maxUtilityRenderDistance = config.MaxRenderDistance;
            _enableLOD = config.EnableLOD;

            OnRenderingConfigurationChanged?.Invoke();
        }

        private void InitializeUtilitySystem()
        {
            _overlayRenderer = ServiceContainerFactory.Instance?.TryResolve<BlueprintOverlayRenderer>();
            if (_overlayRenderer == null)
            {
                LogWarning("BlueprintOverlayRenderer not found - utility integration limited");
            }
        }

        private void SetupUtilityCamera()
        {
            if (_utilityCamera == null)
            {
                var cameraGO = new GameObject("UtilityLayerCamera");
                _utilityCamera = cameraGO.AddComponent<UnityEngine.Camera>();
                cameraGO.transform.SetParent(transform);
            }

            _utilityCamera.cullingMask = _utilityLayer;
            _utilityCamera.clearFlags = CameraClearFlags.Nothing;
            _utilityCamera.depth = 10; // Render on top of overlay
            _utilityCamera.enabled = _enableUtilityVisualization;
        }

        private void CreateUtilityMaterials()
        {
            if (_connectionLineMaterial == null)
            {
                _connectionLineMaterial = CreateUtilityMaterial("UtilityConnectionMaterial", Color.white);
            }

            if (_flowIndicatorMaterial == null)
            {
                _flowIndicatorMaterial = CreateUtilityMaterial("UtilityFlowMaterial", Color.cyan);
            }

            if (_capacityIndicatorMaterial == null)
            {
                _capacityIndicatorMaterial = CreateUtilityMaterial("UtilityCapacityMaterial", Color.yellow);
            }
        }

        private void InitializeUtilityColors()
        {
            _utilityColors[UtilityType.Electrical] = _electricalColor;
            _utilityColors[UtilityType.Water] = _waterColor;
            _utilityColors[UtilityType.Air] = _airColor;
            _utilityColors[UtilityType.Data] = _dataColor;
        }

        protected virtual void SetupRenderingComponents()
        {
            _nodeRenderer = new UtilityNodeRenderer(this);
            _connectionRenderer = new UtilityConnectionRenderer(this);
            _animationController = new UtilityAnimationController(this);
            _validationRenderer = new UtilityValidationRenderer(this);

            LogInfo("Utility rendering components initialized");
        }

        protected override void OnManagerShutdown()
        {
            _nodeRenderer?.Cleanup();
            _connectionRenderer?.Cleanup();
            _animationController?.Cleanup();
            _validationRenderer?.Cleanup();

            LogInfo("UtilityRenderingCore shutdown completed");
        }

        protected override void Start()
        {
            base.Start();
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        #region ITickable Implementation

        int ITickable.Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered() { }
        public virtual void OnUnregistered() { }

        #endregion

        #region Utility Visualization API

        /// <summary>
        /// Create utility visualization for a schematic
        /// </summary>
        public void CreateUtilityVisualization(SchematicSO schematic, Vector3 position, Quaternion rotation)
        {
            if (_nodeRenderer == null || _connectionRenderer == null) return;

            foreach (var item in schematic.Items)
            {
                _nodeRenderer.CreateUtilityNodesForItem(item, position, rotation);
            }

            // Generate connections between nodes
            var utilityData = new SchematicUtilityData(); // TODO: Extract from schematic
            _connectionRenderer.GenerateUtilityConnections(_nodeRenderer, utilityData);
        }

        /// <summary>
        /// Clear all utility visualizations
        /// </summary>
        public void ClearUtilityVisualization()
        {
            _nodeRenderer?.ClearAllNodes();
            _connectionRenderer?.ClearAllConnections();
            _isUtilityVisualizationActive = false;
        }

        /// <summary>
        /// Update utility visualization position
        /// </summary>
        public void UpdateUtilityPosition(Vector3 newPosition, Quaternion newRotation)
        {
            _nodeRenderer?.UpdateNodePositions(newPosition, newRotation);
        }

        /// <summary>
        /// Get connection information at specified position
        /// </summary>
        public List<UtilityConnectionInfo> GetConnectionInfoAtPosition(Vector3 worldPosition, float radius = 1f)
        {
            return _connectionRenderer?.GetConnectionInfoAtPosition(worldPosition, radius) ?? new List<UtilityConnectionInfo>();
        }

        /// <summary>
        /// Validate current utility layout
        /// </summary>
        public List<UtilityValidationResult> ValidateUtilityLayout()
        {
            // Update connection validation first
            _connectionRenderer?.UpdateConnectionValidation();

            // Get validation results from validation renderer
            if (_validationRenderer != null && _nodeRenderer != null && _connectionRenderer != null)
            {
                return _validationRenderer.ValidateUtilityLayout(_nodeRenderer, _connectionRenderer);
            }

            return new List<UtilityValidationResult>();
        }

        /// <summary>
        /// Get node counts by utility type
        /// </summary>
        public Dictionary<UtilityType, int> UtilityNodeCounts => _nodeRenderer?.GetNodeCounts() ?? new Dictionary<UtilityType, int>();

        /// <summary>
        /// Get active connection count
        /// </summary>
        public int ActiveConnectionCount => _connectionRenderer?.GetActiveConnectionCount() ?? 0;

        #endregion

        // Public API for component access
        public UtilityNodeRenderer GetNodeRenderer() => _nodeRenderer;
        public UtilityConnectionRenderer GetConnectionRenderer() => _connectionRenderer;
        public UtilityAnimationController GetAnimationController() => _animationController;
        public UtilityValidationRenderer GetValidationRenderer() => _validationRenderer;
    }

    /// <summary>
    /// Configuration structure for utility rendering
    /// </summary>
    [System.Serializable]
    public class UtilityRenderingConfig
    {
        public bool EnableVisualization = true;
        public bool ShowFlowDirections = true;
        public bool ShowCapacityIndicators = true;
        public bool ShowConnectionValidation = true;
        public float MaxRenderDistance = 25f;
        public bool EnableLOD = true;
        public int MaxVisibleConnections = 200;
    }

    /// <summary>
    /// Types of utility rendering materials
    /// </summary>
    public enum UtilityRenderingMaterialType
    {
        Connection,
        FlowIndicator,
        CapacityIndicator,
        ValidationIndicator
    }
}
