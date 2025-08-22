using UnityEngine;
using System;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Component that makes a GameObject placeable on the construction grid system.
    /// All objects that can be placed in the grid-based construction system must have this component.
    /// </summary>
    [System.Serializable]
    public class GridPlaceable : MonoBehaviour
    {
        [Header("Grid Placement Settings")]
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private Vector3 _pivotOffset = Vector3.zero;
        [SerializeField] private bool _canRotate = true;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private PlaceableType _placeableType = PlaceableType.Structure;
        
        [Header("Placement Validation")]
        [SerializeField] private bool _requiresFoundation = false;
        [SerializeField] private bool _blocksOtherObjects = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private float _placementHeight = 0f;
        [SerializeField] private int _heightLevels = 1; // How many height levels this object spans
        
        [Header("Visual Feedback")]
        [SerializeField] private Material _previewMaterial;
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private GameObject _placementPreview;
        
        // Runtime state
        private Vector3Int _gridCoordinate = Vector3Int.zero;
        private bool _isPlaced = false;
        private bool _isPreviewMode = false;
        private GridSystem _gridSystem;
        private Renderer[] _renderers;
        private Material[] _originalMaterials;
        private Collider _collider;
        
        // Events
        public System.Action<GridPlaceable> OnPlacementChanged;
        public System.Action<GridPlaceable, bool> OnValidationChanged;
        public System.Action<GridPlaceable> OnRotated;
        
        // Properties
        public Vector3Int GridSize => _gridSize;
        public Vector3Int GridCoordinate { get => _gridCoordinate; set => _gridCoordinate = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public int HeightLevels => _heightLevels;
        public bool IsPreviewMode => _isPreviewMode;
        public PlaceableType Type => _placeableType;
        public bool CanRotate => _canRotate;
        public bool SnapToGrid => _snapToGrid;
        public bool RequiresFoundation => _requiresFoundation;
        public bool BlocksOtherObjects => _blocksOtherObjects;
        public float PlacementHeight => _placementHeight;
        
        /// <summary>
        /// Get the bounds of this object in world space
        /// </summary>
        public Bounds GetObjectBounds()
        {
            if (_collider != null)
            {
                return _collider.bounds;
            }
            
            // Fallback: calculate bounds from grid size and position
            Vector3 worldPos = transform.position;
            Vector3 size = new Vector3(_gridSize.x, _gridSize.z, _gridSize.y); // Convert grid size to world size
            return new Bounds(worldPos + size * 0.5f, size);
        }
        public GridSystem GridSystem => _gridSystem;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            FindGridSystem();
        }
        
        #region Initialization
        
        private void Initialize()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _collider = GetComponent<Collider>();
            
            // Store original materials
            if (_renderers.Length > 0)
            {
                _originalMaterials = new Material[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _originalMaterials[i] = _renderers[i].material;
                }
            }
            
            // Create preview materials if not assigned
            if (_previewMaterial == null)
                _previewMaterial = CreatePreviewMaterial(new Color(0.5f, 0.5f, 1f, 0.5f));
            
            if (_validPlacementMaterial == null)
                _validPlacementMaterial = CreatePreviewMaterial(new Color(0f, 1f, 0f, 0.7f));
            
            if (_invalidPlacementMaterial == null)
                _invalidPlacementMaterial = CreatePreviewMaterial(new Color(1f, 0f, 0f, 0.7f));
        }
        
        private Material CreatePreviewMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.5f);
            material.renderQueue = 3000;
            return material;
        }
        
        private void FindGridSystem()
        {
            if (_gridSystem == null)
            {
                _gridSystem = FindObjectOfType<GridSystem>();
                if (_gridSystem == null)
                {
                    Debug.LogWarning($"[GridPlaceable] No GridSystem found in scene for {name}");
                }
            }
        }
        
        #endregion
        
        #region Grid Placement
        
        /// <summary>
        /// Attempt to place this object on the grid at the current position
        /// </summary>
        public bool PlaceOnGrid()
        {
            if (_gridSystem == null)
            {
                Debug.LogWarning($"[GridPlaceable] Cannot place {name} - no GridSystem available");
                return false;
            }
            
            return _gridSystem.PlaceObject(this, transform.position);
        }
        
        /// <summary>
        /// Attempt to place this object at a specific world position
        /// </summary>
        public bool PlaceAt(Vector3 worldPosition)
        {
            if (_gridSystem == null)
                return false;
            
            return _gridSystem.PlaceObject(this, worldPosition);
        }
        
        /// <summary>
        /// Attempt to place this object at a specific 3D grid coordinate
        /// </summary>
        public bool PlaceAt(Vector3Int gridCoordinate)
        {
            if (_gridSystem == null)
                return false;
            
            return _gridSystem.PlaceObject(this, gridCoordinate);
        }
        
        /// <summary>
        /// Attempt to place this object at a specific 2D grid coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use PlaceAt(Vector3Int) for 3D coordinates")]
        public bool PlaceAt(Vector2Int gridCoordinate)
        {
            return PlaceAt(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        /// <summary>
        /// Remove this object from the grid
        /// </summary>
        public bool RemoveFromGrid()
        {
            if (_gridSystem == null)
                return false;
            
            return _gridSystem.RemoveObject(this);
        }
        
        /// <summary>
        /// Move this object to a new 3D position on the grid
        /// </summary>
        public bool MoveTo(Vector3Int newGridCoordinate)
        {
            if (_gridSystem == null)
                return false;
            
            // Remove from current position
            if (_isPlaced)
            {
                if (!RemoveFromGrid())
                    return false;
            }
            
            // Place at new position
            return PlaceAt(newGridCoordinate);
        }
        
        /// <summary>
        /// Snap current position to grid
        /// </summary>
        public void SnapPositionToGrid()
        {
            if (_gridSystem == null || !_snapToGrid)
                return;
            
            Vector3 snappedPosition = _gridSystem.SnapToGrid(transform.position);
            transform.position = new Vector3(snappedPosition.x, snappedPosition.y + _placementHeight, snappedPosition.z);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Check if this object can be placed at the current position
        /// </summary>
        public bool CanBePlaced()
        {
            return CanBePlacedAt(transform.position);
        }
        
        /// <summary>
        /// Check if this object can be placed at a specific world position
        /// </summary>
        public bool CanBePlacedAt(Vector3 worldPosition)
        {
            if (_gridSystem == null)
                return false;
            
            Vector3Int gridCoord = _gridSystem.WorldToGridPosition(worldPosition);
            return CanBePlacedAt(gridCoord);
        }
        
        /// <summary>
        /// Check if this object can be placed at a specific 3D grid coordinate
        /// </summary>
        public bool CanBePlacedAt(Vector3Int gridCoordinate)
        {
            if (_gridSystem == null)
                return false;
            
            // Check if area is available
            if (!_gridSystem.IsAreaAvailable(gridCoordinate, _gridSize))
                return false;
            
            // Check foundation requirement
            if (_requiresFoundation && !HasFoundationAt(gridCoordinate))
                return false;
            
            // Check collision
            if (HasCollisionAt(gridCoordinate))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Check if this object can be placed at a specific 2D grid coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use CanBePlacedAt(Vector3Int) for 3D coordinates")]
        public bool CanBePlacedAt(Vector2Int gridCoordinate)
        {
            return CanBePlacedAt(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        private bool HasFoundationAt(Vector3Int gridCoordinate)
        {
            // Use the grid system's foundation requirement check
            if (_gridSystem != null)
            {
                return !_gridSystem.RequiresFoundation(gridCoordinate);
            }
            return true;
        }
        
        /// <summary>
        /// Check foundation at 2D coordinate (backward compatibility)
        /// </summary>
        private bool HasFoundationAt(Vector2Int gridCoordinate)
        {
            return HasFoundationAt(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        private bool HasCollisionAt(Vector3Int gridCoordinate)
        {
            if (_collider == null)
                return false;
            
            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridCoordinate);
            Vector3 checkPosition = worldPos + Vector3.up * _placementHeight;
            
            // Use overlap check to detect collisions
            Bounds bounds = _collider.bounds;
            bounds.center = checkPosition;
            
            Collider[] overlapping = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, _collisionLayers);
            
            foreach (var overlappingCollider in overlapping)
            {
                if (overlappingCollider != _collider && overlappingCollider.gameObject != gameObject)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check collision at 2D coordinate (backward compatibility)
        /// </summary>
        private bool HasCollisionAt(Vector2Int gridCoordinate)
        {
            return HasCollisionAt(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        #endregion
        
        #region Rotation
        
        /// <summary>
        /// Rotate the object 90 degrees clockwise
        /// </summary>
        public void RotateClockwise()
        {
            if (!_canRotate)
                return;
            
            transform.Rotate(0, 90f, 0);
            
            // Update grid size if object is not square
            if (_gridSize.x != _gridSize.y)
            {
                _gridSize = new Vector3Int(_gridSize.y, _gridSize.x, _gridSize.z);
            }
            
            OnRotated?.Invoke(this);
        }
        
        /// <summary>
        /// Rotate the object 90 degrees counter-clockwise
        /// </summary>
        public void RotateCounterClockwise()
        {
            if (!_canRotate)
                return;
            
            transform.Rotate(0, -90f, 0);
            
            // Update grid size if object is not square
            if (_gridSize.x != _gridSize.y)
            {
                _gridSize = new Vector3Int(_gridSize.y, _gridSize.x, _gridSize.z);
            }
            
            OnRotated?.Invoke(this);
        }
        
        /// <summary>
        /// Set specific rotation
        /// </summary>
        public void SetRotation(float yRotation)
        {
            if (!_canRotate)
                return;
            
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            OnRotated?.Invoke(this);
        }
        
        #endregion
        
        #region Visual Feedback
        
        /// <summary>
        /// Enter preview mode with visual feedback
        /// </summary>
        public void EnterPreviewMode()
        {
            _isPreviewMode = true;
            ApplyPreviewMaterial();
            
            // Initialize 3D visual indicators
            Show3DPreviewIndicators();
        }
        
        /// <summary>
        /// Exit preview mode and restore original materials
        /// </summary>
        public void ExitPreviewMode()
        {
            _isPreviewMode = false;
            RestoreOriginalMaterials();
            
            // Clean up 3D visual indicators
            Hide3DPreviewIndicators();
        }
        
        /// <summary>
        /// Update visual feedback based on placement validity with 3D-specific enhancements
        /// </summary>
        public void UpdatePlacementFeedback(bool isValidPlacement)
        {
            if (!_isPreviewMode)
                return;
            
            Material feedbackMaterial = isValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial;
            ApplyMaterial(feedbackMaterial);
            
            // Enhanced 3D visual feedback
            UpdateHeightIndicators(isValidPlacement);
            UpdateFoundationConnectors(isValidPlacement);
            
            OnValidationChanged?.Invoke(this, isValidPlacement);
        }

        /// <summary>
        /// Update height level indicators for 3D placement
        /// </summary>
        private void UpdateHeightIndicators(bool isValidPlacement)
        {
            if (_gridSystem == null) return;

            // Visual indication of height level and foundation requirements
            Color indicatorColor = isValidPlacement ? Color.green : Color.red;
            
            // Show height level indicator if above ground
            if (GridCoordinate.z > 0)
            {
                Debug.DrawLine(
                    transform.position,
                    transform.position + Vector3.up * (GridCoordinate.z * _gridSystem.HeightLevelSpacing),
                    indicatorColor,
                    0.1f
                );
            }
        }

        /// <summary>
        /// Update foundation connection indicators for 3D placement
        /// </summary>
        private void UpdateFoundationConnectors(bool isValidPlacement)
        {
            if (_gridSystem == null || GridCoordinate.z == 0) return;

            Vector3Int foundationPos = new Vector3Int(GridCoordinate.x, GridCoordinate.y, GridCoordinate.z - 1);
            var foundationCell = _gridSystem.GetGridCell(foundationPos);
            
            if (foundationCell?.IsOccupied == true)
            {
                Vector3 foundationWorldPos = _gridSystem.GridToWorldPosition(foundationPos);
                Color connectorColor = isValidPlacement ? Color.cyan : Color.magenta;
                
                // Draw connection line to foundation
                Debug.DrawLine(
                    transform.position,
                    foundationWorldPos,
                    connectorColor,
                    0.1f
                );
            }
        }
        
        private void ApplyPreviewMaterial()
        {
            ApplyMaterial(_previewMaterial);
        }
        
        private void ApplyMaterial(Material material)
        {
            if (_renderers == null || material == null)
                return;
            
            foreach (var renderer in _renderers)
            {
                renderer.material = material;
            }
        }
        
        private void RestoreOriginalMaterials()
        {
            if (_renderers == null || _originalMaterials == null)
                return;
            
            for (int i = 0; i < _renderers.Length && i < _originalMaterials.Length; i++)
            {
                if (_renderers[i] != null && _originalMaterials[i] != null)
                {
                    _renderers[i].material = _originalMaterials[i];
                }
            }
        }

        /// <summary>
        /// Show 3D visual indicators when entering preview mode
        /// </summary>
        private void Show3DPreviewIndicators()
        {
            if (_gridSystem == null) return;
            
            // Show grid footprint outline for 3D objects
            ShowGridFootprintOutline();
            
            // Show vertical extent indicators for multi-level objects
            if (_heightLevels > 1)
            {
                ShowVerticalExtentIndicators();
            }
        }

        /// <summary>
        /// Hide 3D visual indicators when exiting preview mode
        /// </summary>
        private void Hide3DPreviewIndicators()
        {
            // Visual indicators are typically handled by Debug.DrawLine calls
            // which automatically expire, so no explicit cleanup needed
        }

        /// <summary>
        /// Show grid footprint outline for the object
        /// </summary>
        private void ShowGridFootprintOutline()
        {
            if (_gridSystem == null) return;

            Vector3 worldPos = transform.position;
            Vector3 gridSize = new Vector3(
                _gridSize.x * _gridSystem.GridSize,
                0.1f,
                _gridSize.y * _gridSystem.GridSize
            );
            
            // Draw wireframe cube for grid footprint
            Color outlineColor = _isPreviewMode ? Color.yellow : Color.white;
            DrawWireframeCube(worldPos, gridSize, outlineColor);
        }

        /// <summary>
        /// Show vertical extent indicators for multi-level objects
        /// </summary>
        private void ShowVerticalExtentIndicators()
        {
            if (_gridSystem == null) return;

            Vector3 basePos = transform.position;
            float heightSpacing = _gridSystem.HeightLevelSpacing;
            
            for (int level = 0; level < _heightLevels; level++)
            {
                Vector3 levelPos = basePos + Vector3.up * (level * heightSpacing);
                Color levelColor = Color.Lerp(Color.blue, Color.red, (float)level / _heightLevels);
                
                // Draw level indicator
                Debug.DrawRay(levelPos, Vector3.up * 0.5f, levelColor, 0.1f);
                Debug.DrawRay(levelPos, Vector3.right * 0.5f, levelColor, 0.1f);
                Debug.DrawRay(levelPos, Vector3.forward * 0.5f, levelColor, 0.1f);
            }
        }

        /// <summary>
        /// Draw a wireframe cube for visual debugging
        /// </summary>
        private void DrawWireframeCube(Vector3 center, Vector3 size, Color color)
        {
            Vector3 halfSize = size * 0.5f;
            
            // Define the 8 corners of the cube
            Vector3[] corners = new Vector3[8]
            {
                center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), // 0: left-bottom-back
                center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),  // 1: right-bottom-back  
                center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),  // 2: left-top-back
                center + new Vector3(halfSize.x, halfSize.y, -halfSize.z),   // 3: right-top-back
                center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),  // 4: left-bottom-front
                center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),   // 5: right-bottom-front
                center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),   // 6: left-top-front
                center + new Vector3(halfSize.x, halfSize.y, halfSize.z)     // 7: right-top-front
            };
            
            // Draw the 12 edges of the cube
            Debug.DrawLine(corners[0], corners[1], color, 0.1f); // bottom-back
            Debug.DrawLine(corners[2], corners[3], color, 0.1f); // top-back
            Debug.DrawLine(corners[4], corners[5], color, 0.1f); // bottom-front
            Debug.DrawLine(corners[6], corners[7], color, 0.1f); // top-front
            
            Debug.DrawLine(corners[0], corners[2], color, 0.1f); // left-back
            Debug.DrawLine(corners[1], corners[3], color, 0.1f); // right-back
            Debug.DrawLine(corners[4], corners[6], color, 0.1f); // left-front
            Debug.DrawLine(corners[5], corners[7], color, 0.1f); // right-front
            
            Debug.DrawLine(corners[0], corners[4], color, 0.1f); // left-bottom
            Debug.DrawLine(corners[1], corners[5], color, 0.1f); // right-bottom
            Debug.DrawLine(corners[2], corners[6], color, 0.1f); // left-top
            Debug.DrawLine(corners[3], corners[7], color, 0.1f); // right-top
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get the 3D bounds of this object at a specific grid coordinate
        /// </summary>
        public Bounds GetBoundsAt(Vector3Int gridCoordinate)
        {
            Vector3 worldPos = _gridSystem.GridToWorldPosition(gridCoordinate);
            worldPos.y += _placementHeight;
            
            if (_collider != null)
            {
                Bounds bounds = _collider.bounds;
                bounds.center = worldPos;
                return bounds;
            }
            
            // Default bounds if no collider - now 3D aware
            Vector3 size = new Vector3(_gridSize.x * _gridSystem.GridSize, _gridSize.z * _gridSystem.HeightLevelSpacing, _gridSize.y * _gridSystem.GridSize);
            return new Bounds(worldPos, size);
        }
        
        /// <summary>
        /// Get the bounds at a 2D grid coordinate (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetBoundsAt(Vector3Int) for 3D coordinates")]
        public Bounds GetBoundsAt(Vector2Int gridCoordinate)
        {
            return GetBoundsAt(new Vector3Int(gridCoordinate.x, gridCoordinate.y, 0));
        }
        
        /// <summary>
        /// Get all 3D grid coordinates occupied by this object
        /// </summary>
        public Vector3Int[] GetOccupiedCoordinates()
        {
            var coordinates = new Vector3Int[_gridSize.x * _gridSize.y * _gridSize.z];
            int index = 0;
            
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    for (int z = 0; z < _gridSize.z; z++)
                    {
                        coordinates[index] = _gridCoordinate + new Vector3Int(x, y, z);
                        index++;
                    }
                }
            }
            
            return coordinates;
        }
        
        /// <summary>
        /// Get all 2D grid coordinates occupied by this object (backward compatibility)
        /// </summary>
        [System.Obsolete("Use GetOccupiedCoordinates() for 3D coordinates")]
        public Vector2Int[] GetOccupiedCoordinates2D()
        {
            var coordinates3D = GetOccupiedCoordinates();
            var coordinates2D = new Vector2Int[_gridSize.x * _gridSize.y];
            int index = 0;
            
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    coordinates2D[index] = new Vector2Int(_gridCoordinate.x + x, _gridCoordinate.y + y);
                    index++;
                }
            }
            
            return coordinates2D;
        }
        
        /// <summary>
        /// Update 3D grid size (useful for dynamic objects)
        /// </summary>
        public void SetGridSize(Vector3Int newSize)
        {
            if (_isPlaced)
            {
                Debug.LogWarning($"[GridPlaceable] Cannot change grid size of {name} while placed on grid");
                return;
            }
            
            _gridSize = newSize;
        }
        
        /// <summary>
        /// Update 2D grid size (backward compatibility)
        /// </summary>
        [System.Obsolete("Use SetGridSize(Vector3Int) for 3D coordinates")]
        public void SetGridSize(Vector2Int newSize)
        {
            SetGridSize(new Vector3Int(newSize.x, newSize.y, 1));
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (_gridSystem == null)
                return;
            
            // Draw grid footprint
            Gizmos.color = _isPlaced ? Color.green : Color.yellow;
            
            Vector3 center = transform.position;
            Vector3 size = new Vector3(_gridSize.x * _gridSystem.GridSize, 0.1f, _gridSize.y * _gridSystem.GridSize);
            
            Gizmos.DrawWireCube(center, size);
            
            // Draw pivot offset
            if (_pivotOffset != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Vector3 pivotWorld = center + new Vector3(_pivotOffset.x, 0, _pivotOffset.y);
                Gizmos.DrawWireSphere(pivotWorld, 0.1f);
            }
        }
        
        #endregion
    }
    
    [System.Serializable]
    public enum PlaceableType
    {
        Structure,
        Equipment,
        Decoration,
        Utility,
        Vehicle,
        Temporary
    }
}