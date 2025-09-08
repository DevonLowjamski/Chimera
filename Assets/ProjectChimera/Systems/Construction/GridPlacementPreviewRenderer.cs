using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Specialized 3D preview rendering system for grid-based construction placement.
    /// Handles ghost objects, multi-level grid visualization, height indicators, and validation feedback.
    /// Extracted from GridPlacementController for modular architecture compliance.
    /// </summary>
    public class GridPlacementPreviewRenderer : MonoBehaviour, ITickable
    {
        [Header("3D Ghost Preview Settings")]
        [SerializeField] private Material _ghostPreviewMaterial;
        [SerializeField] private Color _validGhostColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidGhostColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color _unaffordableGhostColor = new Color(1f, 0.5f, 0f, 0.5f);
        [SerializeField] private bool _showGhostOutlines = true;
        [SerializeField] private float _ghostPreviewHeight = 0.1f;

        [Header("Multi-Level Grid Visualization")]
        [SerializeField] private bool _showGridLevels = true;
        [SerializeField] private Material _gridLineMaterial;
        [SerializeField] private Color _gridLineColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color _activeHeightLevelColor = new Color(0f, 1f, 1f, 0.8f);
        [SerializeField] private float _gridLineWidth = 0.02f;
        [SerializeField] private int _maxVisibleLevels = 5;

        [Header("Performance Settings")]
        [SerializeField] private float _previewUpdateRate = 0.016f; // ~60 FPS
        [SerializeField] private bool _enablePreviewCaching = true;
        [SerializeField] private int _maxCachedPreviews = 10;

        // Core references
        private GridSystem _gridSystem;
        private GridInputHandler _inputHandler;
        private GridPlacementValidator _validator;
        private HeightClearanceValidator _heightValidator;

        // Ghost preview management
        private List<GameObject> _ghostPreviewObjects = new List<GameObject>();
        private Dictionary<GameObject, Material[]> _originalMaterials = new Dictionary<GameObject, Material[]>();
        private Dictionary<Vector3Int, GameObject> _cachedGhostObjects = new Dictionary<Vector3Int, GameObject>();

        // Grid visualization
        private Dictionary<int, List<LineRenderer>> _gridLineRenderers = new Dictionary<int, List<LineRenderer>>();
        private int _currentActiveHeight = 0;
        private GameObject _gridVisualizationParent;

        // Component references for specialized rendering
        private GridHeightIndicatorRenderer _heightRenderer;
        private GridFoundationOverlayRenderer _foundationRenderer;

        // Performance tracking
        private float _lastPreviewUpdate;
        private bool _previewDirty = false;

        // Preview events
        public System.Action<Vector3Int, bool> OnPreviewValidityChanged;

        private void Awake()
        {
            InitializeComponents();
            SetupVisualizationParents();
        }

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            InitializeGridVisualization();
        }

            public void Tick(float deltaTime)
    {
            UpdatePreviewRendering();

    }

        private void InitializeComponents()
        {
            _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
            _inputHandler = GetComponent<GridInputHandler>();
            _validator = GetComponent<GridPlacementValidator>();
            _heightValidator = GetComponent<HeightClearanceValidator>();
            _heightRenderer = GetComponent<GridHeightIndicatorRenderer>();
            _foundationRenderer = GetComponent<GridFoundationOverlayRenderer>();

            if (_gridSystem == null)
                ChimeraLogger.LogWarning("[GridPlacementPreviewRenderer] GridSystem not found");
        }

        private void SetupVisualizationParents()
        {
            _gridVisualizationParent = new GameObject("GridVisualization");
            _gridVisualizationParent.transform.SetParent(transform);
        }

        #region Public API

        /// <summary>
        /// Create 3D ghost preview for single object at position
        /// </summary>
        public GameObject CreateGhostPreview(GridPlaceable placeable, Vector3Int gridPosition)
        {
            if (placeable?.gameObject == null) return null;

            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridPosition);
            worldPos.y += _ghostPreviewHeight;

            GameObject ghostObject = Instantiate(placeable.gameObject, worldPos, placeable.transform.rotation);
            ghostObject.name = $"Ghost_{placeable.name}";

            DisableInteractiveComponents(ghostObject);
            ApplyGhostMaterial(ghostObject, _validGhostColor);

            _ghostPreviewObjects.Add(ghostObject);

            if (_enablePreviewCaching)
                _cachedGhostObjects[gridPosition] = ghostObject;

            return ghostObject;
        }

        /// <summary>
        /// Create 3D ghost preview for schematic with multiple objects
        /// </summary>
        public List<GameObject> CreateSchematicGhostPreview(SchematicSO schematic, Vector3Int basePosition, int rotation = 0)
        {
            var ghosts = new List<GameObject>();
            if (schematic?.Items == null) return ghosts;

            foreach (var item in schematic.Items)
            {
                GameObject ghost = CreateGhostPreviewForSchematicItem(item, basePosition, rotation);
                if (ghost != null)
                    ghosts.Add(ghost);
            }

            return ghosts;
        }

        /// <summary>
        /// Update ghost preview position and visual state
        /// </summary>
        public void UpdateGhostPreview(GameObject ghostObject, Vector3Int gridPosition, bool isValid, bool isAffordable = true)
        {
            if (ghostObject == null || _gridSystem == null) return;

            // Update position with 3D height level
            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridPosition);
            worldPos.y += _ghostPreviewHeight;
            ghostObject.transform.position = worldPos;

            // Update visual feedback based on validation state
            Color targetColor = GetValidationColor(isValid, isAffordable);
            ApplyGhostMaterial(ghostObject, targetColor);
        }

        /// <summary>
        /// Show multi-level grid visualization for height level
        /// </summary>
        public void ShowGridLevel(int heightLevel)
        {
            if (!_showGridLevels || _gridSystem == null) return;

            _currentActiveHeight = heightLevel;
            UpdateGridVisualization();
        }

        /// <summary>
        /// Show height level indicators around position
        /// </summary>
        public void ShowHeightIndicators(Vector3Int gridPosition, int maxHeight)
        {
            if (_heightRenderer != null)
                _heightRenderer.ShowHeightIndicators(gridPosition, maxHeight);
        }

        /// <summary>
        /// Show foundation requirement overlays
        /// </summary>
        public void ShowFoundationOverlays(Vector3Int gridPosition, Vector3Int size, bool requiresFoundation)
        {
            if (_foundationRenderer != null)
                _foundationRenderer.ShowFoundationOverlays(gridPosition, size, requiresFoundation);
        }

        /// <summary>
        /// Show clearance and access path indicators
        /// </summary>
        public void ShowClearanceIndicators(Vector3Int gridPosition, Vector3Int size)
        {
            if (_foundationRenderer != null)
            {
                _foundationRenderer.ShowClearanceIndicators(gridPosition, size);
                _foundationRenderer.ShowAccessPathIndicators(gridPosition, size);
            }
        }

        /// <summary>
        /// Clear all preview objects and overlays
        /// </summary>
        public void ClearAllPreviews()
        {
            ClearGhostPreviews();
            if (_heightRenderer != null) _heightRenderer.HideHeightIndicators();
            if (_foundationRenderer != null) _foundationRenderer.ClearAllOverlays();
        }

        /// <summary>
        /// Clear ghost preview objects
        /// </summary>
        public void ClearGhostPreviews()
        {
            foreach (var ghost in _ghostPreviewObjects)
            {
                if (ghost != null)
                    DestroyImmediate(ghost);
            }

            _ghostPreviewObjects.Clear();
            _cachedGhostObjects.Clear();
            _originalMaterials.Clear();
        }

        #endregion

        #region Preview Rendering Core

        private void UpdatePreviewRendering()
        {
            if (Time.time - _lastPreviewUpdate < _previewUpdateRate && !_previewDirty) return;

            UpdateGridVisualization();

            _lastPreviewUpdate = Time.time;
            _previewDirty = false;
        }

        private void UpdateGridVisualization()
        {
            if (!_showGridLevels || _gridSystem == null) return;

            // Update grid line visibility based on current height level
            int minLevel = Mathf.Max(0, _currentActiveHeight - _maxVisibleLevels / 2);
            int maxLevel = Mathf.Min(_gridSystem.MaxHeightLevels - 1, _currentActiveHeight + _maxVisibleLevels / 2);

            for (int level = minLevel; level <= maxLevel; level++)
            {
                UpdateGridLevelVisibility(level, level == _currentActiveHeight);
            }
        }

        private void UpdateGridLevelVisibility(int level, bool isActive)
        {
            if (!_gridLineRenderers.ContainsKey(level))
            {
                CreateGridLevelLines(level);
            }

            var lines = _gridLineRenderers[level];
            Color lineColor = isActive ? _activeHeightLevelColor : _gridLineColor;

            foreach (var line in lines)
            {
                if (line != null)
                {
                    line.material.color = lineColor;
                    line.enabled = true;
                }
            }
        }

        private void CreateGridLevelLines(int level)
        {
            var lines = new List<LineRenderer>();
            float worldY = _gridSystem.GetWorldYForHeightLevel(level);

            // Calculate grid dimensions once
            int gridWidth = Mathf.RoundToInt(_gridSystem.GridDimensions.x / _gridSystem.GridSize);
            int gridDepth = Mathf.RoundToInt(_gridSystem.GridDimensions.y / _gridSystem.GridSize);

            // Create vertical grid lines (X direction)
            for (int x = 0; x <= gridWidth; x++)
            {
                GameObject lineObj = new GameObject($"GridLine_H{level}_X{x}");
                lineObj.transform.SetParent(_gridVisualizationParent.transform);

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = _gridLineMaterial ?? CreateDefaultLineMaterial();
                line.startWidth = _gridLineWidth;
                line.endWidth = _gridLineWidth;
                line.positionCount = 2;
                line.useWorldSpace = true;

                Vector3 start = new Vector3(x * _gridSystem.GridSize, worldY, 0);
                Vector3 end = new Vector3(x * _gridSystem.GridSize, worldY, gridDepth * _gridSystem.GridSize);

                line.SetPosition(0, start);
                line.SetPosition(1, end);

                lines.Add(line);
            }

            // Create horizontal grid lines (Z direction)
            for (int z = 0; z <= gridDepth; z++)
            {
                GameObject lineObj = new GameObject($"GridLine_H{level}_Z{z}");
                lineObj.transform.SetParent(_gridVisualizationParent.transform);

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = _gridLineMaterial ?? CreateDefaultLineMaterial();
                line.startWidth = _gridLineWidth;
                line.endWidth = _gridLineWidth;
                line.positionCount = 2;
                line.useWorldSpace = true;

                Vector3 start = new Vector3(0, worldY, z * _gridSystem.GridSize);
                Vector3 end = new Vector3(gridWidth * _gridSystem.GridSize, worldY, z * _gridSystem.GridSize);

                line.SetPosition(0, start);
                line.SetPosition(1, end);

                lines.Add(line);
            }

            _gridLineRenderers[level] = lines;
        }

        #endregion

        #region Ghost Object Management

        private GameObject CreateGhostPreviewForSchematicItem(SchematicItem item, Vector3Int basePosition, int rotation)
        {
            GameObject prefab = GetPrefabForItem(item);
            if (prefab == null) return null;

            Vector3Int rotatedPos = RotateGridPosition(item.GridPosition, rotation);
            Vector3Int worldPos = basePosition + rotatedPos;

            Vector3 position = _gridSystem.GridToWorldPosition(worldPos);
            position.y += item.Height + _ghostPreviewHeight;

            GameObject ghost = Instantiate(prefab, position, Quaternion.Euler(0, item.Rotation + rotation, 0));
            ghost.name = $"SchematicGhost_{item.ItemName}";

            DisableInteractiveComponents(ghost);
            ApplyGhostMaterial(ghost, _validGhostColor);

            _ghostPreviewObjects.Add(ghost);
            return ghost;
        }

        private void DisableInteractiveComponents(GameObject ghostObject)
        {
            var colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
                collider.enabled = false;

            var rigidbodies = ghostObject.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
                rb.isKinematic = true;

            var placeable = ghostObject.GetComponent<GridPlaceable>();
            if (placeable != null)
                placeable.enabled = false;
        }

        private void ApplyGhostMaterial(GameObject ghostObject, Color color)
        {
            var renderers = ghostObject.GetComponentsInChildren<Renderer>();

            if (!_originalMaterials.ContainsKey(ghostObject))
            {
                var originalMats = new List<Material>();
                foreach (var renderer in renderers)
                {
                    originalMats.AddRange(renderer.materials);
                }
                _originalMaterials[ghostObject] = originalMats.ToArray();
            }

            foreach (var renderer in renderers)
            {
                var materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = _ghostPreviewMaterial ?? CreateGhostMaterial(color);
                }
                renderer.materials = materials;
            }
        }

        private Material CreateGhostMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        private Color GetValidationColor(bool isValid, bool isAffordable)
        {
            if (!isAffordable) return _unaffordableGhostColor;
            return isValid ? _validGhostColor : _invalidGhostColor;
        }

        #endregion

        #region Animation and Visual Effects

        private void InitializeGridVisualization()
        {
            if (!_showGridLevels || _gridSystem == null) return;
            ShowGridLevel(0);
        }

        #endregion

        #region Utility Methods

        private Material CreateDefaultLineMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _gridLineColor;
            return material;
        }

        private GameObject GetPrefabForItem(SchematicItem item)
        {
            // This would typically retrieve from a prefab registry or resource system
            // For now, returning null - actual implementation depends on project architecture
            return null;
        }

        private Vector3Int RotateGridPosition(Vector3Int position, int rotation)
        {
            // Simple 90-degree rotation logic
            switch ((rotation / 90) % 4)
            {
                case 1: return new Vector3Int(-position.y, position.x, position.z);
                case 2: return new Vector3Int(-position.x, -position.y, position.z);
                case 3: return new Vector3Int(position.y, -position.x, position.z);
                default: return position;
            }
        }

        #endregion

        #region Public API for GridPlacementController

        /// <summary>
        /// Show preview for a placeable object
        /// </summary>
        public void ShowPreview(GridPlaceable placeable)
        {
            if (placeable == null) return;

            // Implementation would show ghost preview of the placeable
            ChimeraLogger.Log($"[GridPlacementPreviewRenderer] Showing preview for {placeable.name}");
        }

        /// <summary>
        /// Hide the current preview
        /// </summary>
        public void HidePreview()
        {
            ClearAllPreviews();
            ChimeraLogger.Log("[GridPlacementPreviewRenderer] Preview hidden");
        }

        /// <summary>
        /// Update the preview display
        /// </summary>
        public void UpdatePreview()
        {
            UpdatePreviewRendering();
        }

        /// <summary>
        /// Update preview position to a new grid coordinate
        /// </summary>
        public void UpdatePreviewPosition(Vector3Int newPosition)
        {
            ChimeraLogger.Log($"[GridPlacementPreviewRenderer] Preview position updated to {newPosition}");
            // Implementation would move ghost preview to new position
        }

        /// <summary>
        /// Update visual feedback based on validation results
        /// </summary>
        public void UpdateValidationVisuals(PlacementValidationResult result)
        {
            if (result == null) return;

            Color feedbackColor = result.IsValid ? _validGhostColor : _invalidGhostColor;
            ChimeraLogger.Log($"[GridPlacementPreviewRenderer] Validation visuals updated: {(result.IsValid ? "Valid" : "Invalid")}");
            // Implementation would apply color to ghost preview
        }

        #endregion

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            ClearAllPreviews();
        }

    #region ITickable Implementation

    public int Priority => TickPriority.ConstructionSystem;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public void OnRegistered()
    {
        ChimeraLogger.LogVerbose($"[{GetType().Name}] Registered with UpdateOrchestrator");
    }

    public void OnUnregistered()
    {
        ChimeraLogger.LogVerbose($"[{GetType().Name}] Unregistered from UpdateOrchestrator");
    }

    #endregion
    }
}
