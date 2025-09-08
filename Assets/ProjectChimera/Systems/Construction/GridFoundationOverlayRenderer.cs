using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Specialized renderer for foundation requirement overlays in 3D grid construction.
    /// Handles visual feedback for foundation requirements, structural support visualization,
    /// and clearance path indicators for multi-level construction.
    /// </summary>
    public class GridFoundationOverlayRenderer : MonoBehaviour, ITickable
    {
        [Header("Foundation Overlay Settings")]
        [SerializeField] private Material _foundationOverlayMaterial;
        [SerializeField] private Color _requiredFoundationColor = new Color(1f, 0.5f, 0f, 0.6f);
        [SerializeField] private Color _missingFoundationColor = new Color(1f, 0f, 0f, 0.6f);
        [SerializeField] private Color _validFoundationColor = new Color(0f, 1f, 0f, 0.4f);
        [SerializeField] private float _overlayHeight = 0.05f;
        [SerializeField] private float _overlayScale = 0.9f;

        [Header("Clearance Visualization")]
        [SerializeField] private bool _showClearanceIndicators = true;
        [SerializeField] private Color _clearanceColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color _obstructionColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Material _clearanceMaterial;
        [SerializeField] private float _clearanceRadius = 2f;

        [Header("Access Path Indicators")]
        [SerializeField] private bool _showAccessPaths = true;
        [SerializeField] private Material _accessPathMaterial;
        [SerializeField] private Color _accessPathColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color _blockedPathColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private float _pathWidth = 0.5f;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableOverlayCaching = true;
        [SerializeField] private int _maxCachedOverlays = 20;
        [SerializeField] private float _updateInterval = 0.1f;

        // Core references
        private GridSystem _gridSystem;
        private HeightClearanceValidator _heightValidator;
        private StructuralIntegrityValidator _structuralValidator;

        // Overlay management
        private List<GameObject> _foundationOverlays = new List<GameObject>();
        private List<GameObject> _clearanceIndicators = new List<GameObject>();
        private List<GameObject> _accessPathIndicators = new List<GameObject>();
        private GameObject _overlayParent;

        // Caching system
        private Dictionary<Vector3Int, OverlayData> _overlayCache = new Dictionary<Vector3Int, OverlayData>();
        private float _lastUpdateTime;

        // Events
        public System.Action<List<Vector3Int>> OnFoundationRequirementsUpdated;
        public System.Action<Vector3Int, float> OnClearanceUpdated;
        public System.Action<List<Vector3Int>> OnAccessPathsUpdated;

        private struct OverlayData
        {
            public bool IsRequired;
            public bool IsMissing;
            public float Clearance;
            public bool HasAccess;
            public float Timestamp;
        }

        private void Awake()
        {
            InitializeComponents();
            SetupOverlayParent();
        }

            public void Tick(float deltaTime)
    {
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateOverlayCache();
                _lastUpdateTime = Time.time;

    }
        }

        private void InitializeComponents()
        {
            _gridSystem = ServiceContainerFactory.Instance?.TryResolve<IGridSystem>() as GridSystem;
            _heightValidator = GetComponent<HeightClearanceValidator>();
            _structuralValidator = GetComponent<StructuralIntegrityValidator>();

            if (_gridSystem == null)
                ChimeraLogger.LogWarning("[GridFoundationOverlayRenderer] GridSystem not found");
        }

        private void SetupOverlayParent()
        {
            _overlayParent = new GameObject("FoundationOverlays");
            _overlayParent.transform.SetParent(transform);
        }

        #region Public API

        /// <summary>
        /// Show foundation requirement overlays for placement validation
        /// </summary>
        public void ShowFoundationOverlays(Vector3Int gridPosition, Vector3Int size, bool requiresFoundation)
        {
            ClearFoundationOverlays();

            if (!requiresFoundation || gridPosition.z == 0) return;

            var requiredPositions = new List<Vector3Int>();
            var missingPositions = new List<Vector3Int>();

            // Check foundation requirements for each cell
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int foundationPos = new Vector3Int(
                        gridPosition.x + x,
                        gridPosition.y + y,
                        gridPosition.z - 1
                    );

                    requiredPositions.Add(foundationPos);

                    if (!HasAdequateFoundation(foundationPos))
                    {
                        missingPositions.Add(foundationPos);
                    }
                }
            }

            // Create visual overlays
            foreach (var pos in requiredPositions)
            {
                bool isMissing = missingPositions.Contains(pos);
                Color overlayColor = isMissing ? _missingFoundationColor : _validFoundationColor;
                CreateFoundationOverlay(pos, overlayColor, isMissing);
            }

            OnFoundationRequirementsUpdated?.Invoke(missingPositions);
        }

        /// <summary>
        /// Show clearance indicators around position
        /// </summary>
        public void ShowClearanceIndicators(Vector3Int gridPosition, Vector3Int size)
        {
            if (!_showClearanceIndicators) return;

            ClearClearanceIndicators();

            // Calculate clearance for the object area
            float minClearance = float.MaxValue;
            bool hasAdequateAccess = true;

            if (_heightValidator != null)
            {
                minClearance = _heightValidator.CalculateVerticalClearance(gridPosition, size);
                hasAdequateAccess = _heightValidator.HasAdequateAccess(gridPosition, size);
            }

            // Create clearance visualization
            Vector3 centerPos = _gridSystem.GridToWorldPosition(gridPosition);
            centerPos += new Vector3(size.x * 0.5f, 0, size.y * 0.5f) * _gridSystem.GridSize;

            Color clearanceColor = hasAdequateAccess ? _clearanceColor : _obstructionColor;
            CreateClearanceIndicator(centerPos, minClearance, clearanceColor);

            OnClearanceUpdated?.Invoke(gridPosition, minClearance);
        }

        /// <summary>
        /// Show access path indicators around object
        /// </summary>
        public void ShowAccessPathIndicators(Vector3Int gridPosition, Vector3Int size)
        {
            if (!_showAccessPaths) return;

            ClearAccessPathIndicators();

            var accessPaths = CalculateAccessPaths(gridPosition, size);

            foreach (var path in accessPaths)
            {
                CreateAccessPathIndicator(path.Start, path.End, path.IsBlocked);
            }

            var blockedPaths = accessPaths.Where(p => p.IsBlocked).Select(p => Vector3Int.RoundToInt(p.Start)).ToList();
            OnAccessPathsUpdated?.Invoke(blockedPaths);
        }

        /// <summary>
        /// Clear all overlay visualizations
        /// </summary>
        public void ClearAllOverlays()
        {
            ClearFoundationOverlays();
            ClearClearanceIndicators();
            ClearAccessPathIndicators();
        }

        #endregion

        #region Overlay Creation

        private void CreateFoundationOverlay(Vector3Int gridPosition, Color color, bool isMissing)
        {
            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridPosition);
            worldPos.y += _overlayHeight;

            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Plane);
            overlay.name = $"FoundationOverlay_{gridPosition}";
            overlay.transform.position = worldPos;
            overlay.transform.localScale = Vector3.one * _gridSystem.GridSize * 0.1f * _overlayScale;
            overlay.transform.SetParent(_overlayParent.transform);

            // Remove collider to avoid interference
            var collider = overlay.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            // Setup material
            var renderer = overlay.GetComponent<Renderer>();
            Material overlayMat = _foundationOverlayMaterial != null ?
                new Material(_foundationOverlayMaterial) :
                CreateOverlayMaterial(color);

            overlayMat.color = color;
            renderer.material = overlayMat;

            // Add pulsing effect for missing foundations
            if (isMissing)
            {
                var pulseScript = overlay.AddComponent<OverlayPulseEffect>();
                pulseScript.Initialize(color, _missingFoundationColor * 1.5f, 2f);
            }

            _foundationOverlays.Add(overlay);
        }

        private void CreateClearanceIndicator(Vector3 worldPos, float clearance, Color color)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = $"ClearanceIndicator_{worldPos}";
            indicator.transform.position = worldPos;
            indicator.transform.localScale = Vector3.one * (clearance * 0.5f);
            indicator.transform.SetParent(_overlayParent.transform);

            // Remove collider
            var collider = indicator.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            // Setup material
            var renderer = indicator.GetComponent<Renderer>();
            Material clearanceMat = _clearanceMaterial != null ?
                new Material(_clearanceMaterial) :
                CreateClearanceMaterial(color);

            renderer.material = clearanceMat;

            _clearanceIndicators.Add(indicator);
        }

        private void CreateAccessPathIndicator(Vector3 start, Vector3 end, bool isBlocked)
        {
            GameObject pathLine = new GameObject($"AccessPath_{start}_{end}");
            pathLine.transform.SetParent(_overlayParent.transform);

            LineRenderer line = pathLine.AddComponent<LineRenderer>();
            line.material = _accessPathMaterial ?? CreateAccessPathMaterial();
            line.startColor = isBlocked ? _blockedPathColor : _accessPathColor;
            line.endColor = isBlocked ? _blockedPathColor : _accessPathColor;
            line.startWidth = _pathWidth;
            line.endWidth = _pathWidth;
            line.positionCount = 2;
            line.useWorldSpace = true;

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            _accessPathIndicators.Add(pathLine);
        }

        #endregion

        #region Material Creation

        private Material CreateOverlayMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.renderQueue = 3000;
            return material;
        }

        private Material CreateClearanceMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
            return material;
        }

        private Material CreateAccessPathMaterial()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _accessPathColor;
            return material;
        }

        #endregion

        #region Helper Methods

        private bool HasAdequateFoundation(Vector3Int foundationPosition)
        {
            if (_gridSystem == null) return false;

            var cell = _gridSystem.GetGridCell(foundationPosition);
            if (cell?.IsOccupied != true) return false;

            var foundationObject = cell.OccupyingObject;
            return foundationObject != null &&
                   (foundationObject.Type == PlaceableType.Structure ||
                    foundationObject.Type == PlaceableType.Equipment);
        }

        private List<AccessPath> CalculateAccessPaths(Vector3Int gridPosition, Vector3Int size)
        {
            var paths = new List<AccessPath>();

            // Calculate access paths around the object perimeter
            for (int side = 0; side < 4; side++)
            {
                var pathData = GetAccessPathForSide(gridPosition, size, side);
                if (pathData.HasValue)
                {
                    paths.Add(pathData.Value);
                }
            }

            return paths;
        }

        private AccessPath? GetAccessPathForSide(Vector3Int gridPos, Vector3Int size, int side)
        {
            Vector3 start, end;
            bool isBlocked = false;

            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridPos);
            float gridSize = _gridSystem.GridSize;

            switch (side)
            {
                case 0: // North
                    start = worldPos + new Vector3(0, 0, size.y * gridSize);
                    end = worldPos + new Vector3(size.x * gridSize, 0, size.y * gridSize);
                    break;
                case 1: // East
                    start = worldPos + new Vector3(size.x * gridSize, 0, 0);
                    end = worldPos + new Vector3(size.x * gridSize, 0, size.y * gridSize);
                    break;
                case 2: // South
                    start = worldPos;
                    end = worldPos + new Vector3(size.x * gridSize, 0, 0);
                    break;
                case 3: // West
                    start = worldPos;
                    end = worldPos + new Vector3(0, 0, size.y * gridSize);
                    break;
                default:
                    return null;
            }

            // Check if path is blocked
            isBlocked = IsPathBlocked(start, end);

            return new AccessPath { Start = start, End = end, IsBlocked = isBlocked };
        }

        private bool IsPathBlocked(Vector3 start, Vector3 end)
        {
            // Simple obstruction check - can be enhanced with more sophisticated pathfinding
            int checkPoints = 5;
            for (int i = 0; i <= checkPoints; i++)
            {
                Vector3 checkPos = Vector3.Lerp(start, end, (float)i / checkPoints);
                Vector3Int gridPos = _gridSystem.WorldToGridPosition(checkPos);

                var cell = _gridSystem.GetGridCell(gridPos);
                if (cell?.IsOccupied == true)
                    return true;
            }

            return false;
        }

        private void UpdateOverlayCache()
        {
            // Clean expired cache entries
            var expiredKeys = _overlayCache.Keys.Where(k =>
                Time.time - _overlayCache[k].Timestamp > 5f).ToList();

            foreach (var key in expiredKeys)
                _overlayCache.Remove(key);
        }

        #endregion

        #region Cleanup Methods

        private void ClearFoundationOverlays()
        {
            foreach (var overlay in _foundationOverlays)
            {
                if (overlay != null) DestroyImmediate(overlay);
            }
            _foundationOverlays.Clear();
        }

        private void ClearClearanceIndicators()
        {
            foreach (var indicator in _clearanceIndicators)
            {
                if (indicator != null) DestroyImmediate(indicator);
            }
            _clearanceIndicators.Clear();
        }

        private void ClearAccessPathIndicators()
        {
            foreach (var indicator in _accessPathIndicators)
            {
                if (indicator != null) DestroyImmediate(indicator);
            }
            _accessPathIndicators.Clear();
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            ClearAllOverlays();
        }

        #endregion

        #region Data Structures

        private struct AccessPath
        {
            public Vector3 Start;
            public Vector3 End;
            public bool IsBlocked;
        }

        #endregion

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
    /// Simple component for pulsing overlay effects
    /// </summary>
    public class OverlayPulseEffect : MonoBehaviour
    {
        private Color _baseColor;
        private Color _pulseColor;
        private float _speed;
        private Renderer _renderer;

        public void Initialize(Color baseColor, Color pulseColor, float speed)
        {
            _baseColor = baseColor;
            _pulseColor = pulseColor;
            _speed = speed;
            _renderer = GetComponent<Renderer>();
        }

        public void Tick(float deltaTime)
        {
            // Update overlay logic here
        }

        protected virtual void Start()
    {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

    #region ITickable Implementation

    public int Priority => TickPriority.ConstructionSystem;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    // NOTE: ITickable Tick method implementation - main Tick method exists above in class

    public void OnRegistered()
    {
        ChimeraLogger.LogVerbose("[GridFoundationOverlayRenderer] Registered with UpdateOrchestrator");
    }

    public void OnUnregistered()
    {
        ChimeraLogger.LogVerbose("[GridFoundationOverlayRenderer] Unregistered from UpdateOrchestrator");
    }

    #endregion
    }
}

