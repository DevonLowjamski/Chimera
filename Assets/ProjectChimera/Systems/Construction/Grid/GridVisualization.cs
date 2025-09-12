using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Systems.Construction.Grid;

namespace ProjectChimera.Systems.Construction.Grid
{
    /// <summary>
    /// Handles all visualization aspects of the grid system.
    /// Manages grid lines, planes, materials, and visual updates.
    /// </summary>
    public class GridVisualization
    {
        private readonly GridTypes.GridBounds _bounds;
        private readonly GridTypes.GridSnapSettings _settings;
        private readonly Transform _parentTransform;

        private GameObject _gridVisualization;
        private LineRenderer[] _gridLines;
        private GameObject _gridPlane;
        private Dictionary<int, GameObject> _heightLevelVisualizations;

        private Material _gridMaterial;
        private Material _gridPlaneMaterial;

        // Events
        public System.Action<bool> OnVisibilityChanged;

        public GridVisualization(
            GridTypes.GridBounds bounds,
            GridTypes.GridSnapSettings settings,
            Transform parentTransform)
        {
            _bounds = bounds;
            _settings = settings;
            _parentTransform = parentTransform;
            _heightLevelVisualizations = new Dictionary<int, GameObject>();
        }

        /// <summary>
        /// Initialize the grid visualization system
        /// </summary>
        public void Initialize()
        {
            CreateVisualizationContainer();
            CreateGridMaterials();
            UpdateVisualization();
        }

        /// <summary>
        /// Create the main visualization container
        /// </summary>
        private void CreateVisualizationContainer()
        {
            if (_gridVisualization != null)
            {
                Object.DestroyImmediate(_gridVisualization);
            }

            _gridVisualization = new GameObject("GridVisualization");
            _gridVisualization.transform.SetParent(_parentTransform);
            _gridVisualization.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Create materials for grid rendering
        /// </summary>
        private void CreateGridMaterials()
        {
            _gridMaterial = CreateDefaultGridMaterial();
            _gridPlaneMaterial = CreateGridPlaneMaterial();
        }

        /// <summary>
        /// Update the entire grid visualization
        /// </summary>
        public void UpdateVisualization()
        {
            if (_settings.ShowGrid && _gridVisualization == null)
            {
                Initialize();
            }

            if (_gridVisualization != null)
            {
                CreateGridLines();
                CreateGridPlane();
                UpdateHeightLevelVisualizations();
            }
        }

        /// <summary>
        /// Create or update grid line renderers
        /// </summary>
        private void CreateGridLines()
        {
            if (!_settings.ShowGrid) return;

            var linesParent = _gridVisualization.transform.Find("GridLines");
            if (linesParent == null)
            {
                linesParent = new GameObject("GridLines").transform;
                linesParent.SetParent(_gridVisualization.transform);
            }

            Vector3Int maxCoords = GetMaxGridCoordinates();

            // Create vertical lines (X direction)
            for (int x = 0; x <= maxCoords.x; x++)
            {
                CreateGridLine(linesParent, $"VerticalLine_{x}",
                    _bounds.Origin + new Vector3(x * _settings.GridSize, _bounds.Origin.y, 0),
                    _bounds.Origin + new Vector3(x * _settings.GridSize, _bounds.Origin.y, _bounds.Dimensions.y));
            }

            // Create horizontal lines (Z direction)
            for (int z = 0; z <= maxCoords.y; z++)
            {
                CreateGridLine(linesParent, $"HorizontalLine_{z}",
                    _bounds.Origin + new Vector3(0, _bounds.Origin.y, z * _settings.GridSize),
                    _bounds.Origin + new Vector3(_bounds.Dimensions.x, _bounds.Origin.y, z * _settings.GridSize));
            }
        }

        /// <summary>
        /// Create a single grid line
        /// </summary>
        private void CreateGridLine(Transform parent, string name, Vector3 start, Vector3 end)
        {
            var lineObj = parent.Find(name);
            if (lineObj == null)
            {
                lineObj = new GameObject(name).transform;
                lineObj.SetParent(parent);
            }

            var lineRenderer = lineObj.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = lineObj.gameObject.AddComponent<LineRenderer>();
            }

            ConfigureLineRenderer(lineRenderer);
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        /// <summary>
        /// Configure line renderer properties
        /// </summary>
        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.material = _gridMaterial;
            lineRenderer.startColor = _settings.GridColor;
            lineRenderer.endColor = _settings.GridColor;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Create or update the grid plane
        /// </summary>
        private void CreateGridPlane()
        {
            if (!_settings.ShowGrid) return;

            if (_gridPlane == null)
            {
                _gridPlane = new GameObject("GridPlane");
                _gridPlane.transform.SetParent(_gridVisualization.transform);

                var meshRenderer = _gridPlane.AddComponent<MeshRenderer>();
                var meshFilter = _gridPlane.AddComponent<MeshFilter>();
                meshFilter.mesh = CreatePlaneMesh();
                meshRenderer.material = _gridPlaneMaterial;
            }

            _gridPlane.transform.position = _bounds.Origin + new Vector3(_bounds.Dimensions.x / 2, _bounds.Origin.y, _bounds.Dimensions.y / 2);
            _gridPlane.transform.localScale = new Vector3(_bounds.Dimensions.x / 10f, 1f, _bounds.Dimensions.y / 10f);
        }

        /// <summary>
        /// Create a plane mesh for the grid base
        /// </summary>
        private Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();

            Vector3[] vertices = {
                new Vector3(-5, 0, -5),
                new Vector3(5, 0, -5),
                new Vector3(5, 0, 5),
                new Vector3(-5, 0, 5)
            };

            int[] triangles = { 0, 2, 1, 0, 3, 2 };
            Vector2[] uv = {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// Create default grid material
        /// </summary>
        private Material CreateDefaultGridMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _settings.GridColor;
            material.SetFloat("_Mode", 3); // Transparent
            material.renderQueue = 3000;
            return material;
        }

        /// <summary>
        /// Create grid plane material
        /// </summary>
        private Material CreateGridPlaneMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(_settings.GridColor.r, _settings.GridColor.g, _settings.GridColor.b, 0.1f);
            material.SetFloat("_Mode", 3); // Transparent
            material.renderQueue = 2999; // Just below grid lines
            return material;
        }

        /// <summary>
        /// Update height level visualizations
        /// </summary>
        private void UpdateHeightLevelVisualizations()
        {
            // Clear existing visualizations
            foreach (var kvp in _heightLevelVisualizations)
            {
                if (kvp.Value != null)
                {
                    Object.DestroyImmediate(kvp.Value);
                }
            }
            _heightLevelVisualizations.Clear();

            // Create new visualizations for each height level
            for (int level = 0; level < _bounds.MaxHeightLevels; level++)
            {
                CreateHeightLevelVisualization(level);
            }
        }

        /// <summary>
        /// Create visualization for a specific height level
        /// </summary>
        private void CreateHeightLevelVisualization(int heightLevel)
        {
            float yPos = _bounds.Origin.y + heightLevel * _bounds.HeightLevelSpacing;

            var levelObj = new GameObject($"HeightLevel_{heightLevel}");
            levelObj.transform.SetParent(_gridVisualization.transform);
            levelObj.transform.position = new Vector3(_bounds.Origin.x, yPos, _bounds.Origin.z);

            // Add a subtle plane at each height level
            var meshRenderer = levelObj.AddComponent<MeshRenderer>();
            var meshFilter = levelObj.AddComponent<MeshFilter>();
            meshFilter.mesh = CreatePlaneMesh();

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(_settings.GridColor.r, _settings.GridColor.g, _settings.GridColor.b, 0.05f);
            material.SetFloat("_Mode", 3); // Transparent
            material.renderQueue = 2998;

            meshRenderer.material = material;
            meshRenderer.transform.localScale = new Vector3(_bounds.Dimensions.x / 10f, 1f, _bounds.Dimensions.y / 10f);

            _heightLevelVisualizations[heightLevel] = levelObj;
        }

        /// <summary>
        /// Set grid visibility
        /// </summary>
        public void SetVisibility(bool visible)
        {
            if (_gridVisualization != null)
            {
                _gridVisualization.SetActive(visible);
            }
            OnVisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// Update grid settings and refresh visualization
        /// </summary>
        public void UpdateSettings(GridTypes.GridSnapSettings newSettings)
        {
            // Check if grid size changed significantly
            if (Mathf.Abs(_settings.GridSize - newSettings.GridSize) > 0.01f)
            {
                UpdateVisualization();
            }
            else
            {
                // Just update colors/materials
                UpdateMaterials(newSettings);
            }
        }

        /// <summary>
        /// Update materials with new settings
        /// </summary>
        private void UpdateMaterials(GridTypes.GridSnapSettings newSettings)
        {
            if (_gridMaterial != null)
            {
                _gridMaterial.color = newSettings.GridColor;
            }

            if (_gridPlaneMaterial != null)
            {
                _gridPlaneMaterial.color = new Color(newSettings.GridColor.r, newSettings.GridColor.g, newSettings.GridColor.b, 0.1f);
            }

            // Update line renderers
            if (_gridVisualization != null)
            {
                var lineRenderers = _gridVisualization.GetComponentsInChildren<LineRenderer>();
                foreach (var lineRenderer in lineRenderers)
                {
                    lineRenderer.startColor = newSettings.GridColor;
                    lineRenderer.endColor = newSettings.GridColor;
                }
            }
        }

        /// <summary>
        /// Get the maximum grid coordinates for visualization bounds
        /// </summary>
        private Vector3Int GetMaxGridCoordinates()
        {
            return new Vector3Int(
                Mathf.RoundToInt(_bounds.Dimensions.x / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.y / _settings.GridSize),
                Mathf.RoundToInt(_bounds.Dimensions.z / _bounds.HeightLevelSpacing)
            );
        }

        /// <summary>
        /// Cleanup visualization resources
        /// </summary>
        public void Cleanup()
        {
            if (_gridVisualization != null)
            {
                Object.DestroyImmediate(_gridVisualization);
                _gridVisualization = null;
            }

            if (_gridMaterial != null)
            {
                Object.DestroyImmediate(_gridMaterial);
                _gridMaterial = null;
            }

            if (_gridPlaneMaterial != null)
            {
                Object.DestroyImmediate(_gridPlaneMaterial);
                _gridPlaneMaterial = null;
            }

            _heightLevelVisualizations.Clear();
        }
    }
}
