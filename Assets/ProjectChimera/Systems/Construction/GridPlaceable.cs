using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// BASIC: Core grid placement component for Project Chimera's construction system.
    /// Focuses on essential grid placement functionality.
    /// </summary>
    [System.Serializable]
    public class GridPlaceable : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private Vector3 _pivotOffset = Vector3.zero;
        [SerializeField] private PlaceableType _placeableType = PlaceableType.Structure;
        [SerializeField] private bool _canRotate = true;
        [SerializeField] private bool _snapToGrid = true;

        [Header("Placement Rules")]
        [SerializeField] private bool _requiresFoundation = false;
        [SerializeField] private bool _blocksOtherObjects = true;
        [SerializeField] private LayerMask _collisionLayers = -1;

        // Runtime state
        private Vector3Int _currentGridPosition = Vector3Int.zero;
        private bool _isPlaced = false;
        private bool _isPreviewMode = false;

        /// <summary>
        /// Events for placement operations
        /// </summary>
        public event System.Action<GridPlaceable> OnPlaced;
        public event System.Action<GridPlaceable> OnRemoved;
        public event System.Action<GridPlaceable, Vector3Int> OnPositionChanged;

        /// <summary>
        /// Properties
        /// </summary>
        public Vector3Int GridSize => _gridSize;
        public Vector3Int GridPosition => _currentGridPosition;
        public bool IsPlaced => _isPlaced;
        public bool IsPreviewMode => _isPreviewMode;
        public PlaceableType Type => _placeableType;
        public bool CanRotate => _canRotate;
        public bool RequiresFoundation => _requiresFoundation;

        /// <summary>
        /// Set grid position
        /// </summary>
        public void SetGridPosition(Vector3Int position)
        {
            if (_currentGridPosition == position) return;

            var oldPosition = _currentGridPosition;
            _currentGridPosition = position;

            // Update world position based on grid position
            UpdateWorldPosition();

            OnPositionChanged?.Invoke(this, oldPosition);
        }

        /// <summary>
        /// Place the object
        /// </summary>
        public bool Place()
        {
            if (_isPlaced) return false;

            _isPlaced = true;
            _isPreviewMode = false;

            OnPlaced?.Invoke(this);
            ChimeraLogger.Log($"[GridPlaceable] Placed {name} at {_currentGridPosition}");

            return true;
        }

        /// <summary>
        /// Remove the object
        /// </summary>
        public bool Remove()
        {
            if (!_isPlaced) return false;

            _isPlaced = false;
            OnRemoved?.Invoke(this);
            ChimeraLogger.Log($"[GridPlaceable] Removed {name} from {_currentGridPosition}");

            return true;
        }

        /// <summary>
        /// Set preview mode
        /// </summary>
        public void SetPreviewMode(bool preview)
        {
            _isPreviewMode = preview;

            // Update visual appearance for preview
            UpdatePreviewAppearance();
        }

        /// <summary>
        /// Rotate the object
        /// </summary>
        public void Rotate()
        {
            if (!_canRotate) return;

            // Simple 90-degree rotation around Y axis
            transform.Rotate(0, 90, 0);

            // Update grid size if needed (for rotated objects)
            if (_gridSize.x != _gridSize.z)
            {
                _gridSize = new Vector3Int(_gridSize.z, _gridSize.y, _gridSize.x);
            }
        }

        /// <summary>
        /// Check if placement is valid at position
        /// </summary>
        public bool IsValidPlacement(Vector3Int position)
        {
            // Basic validation - check if position is occupied
            // In a real implementation, this would check the grid system
            return !Physics.CheckBox(
                GridToWorldPosition(position) + Vector3.up * (_gridSize.y * 0.5f),
                new Vector3(_gridSize.x * 0.5f, _gridSize.y * 0.5f, _gridSize.z * 0.5f),
                Quaternion.identity,
                _collisionLayers
            );
        }

        /// <summary>
        /// Get object bounds
        /// </summary>
        public Bounds GetBounds()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            // Fallback bounds calculation
            return new Bounds(transform.position, new Vector3(_gridSize.x, _gridSize.y, _gridSize.z));
        }

        /// <summary>
        /// Get placement cost
        /// </summary>
        public virtual int GetPlacementCost()
        {
            // Basic cost calculation based on size
            return _gridSize.x * _gridSize.y * _gridSize.z;
        }

        /// <summary>
        /// Get placement data
        /// </summary>
        public PlacementData GetPlacementData()
        {
            return new PlacementData
            {
                GridPosition = _currentGridPosition,
                WorldPosition = transform.position,
                Rotation = transform.rotation,
                GridSize = _gridSize,
                Type = _placeableType,
                IsPlaced = _isPlaced
            };
        }

        #region Private Methods

        private void UpdateWorldPosition()
        {
            // Convert grid position to world position
            Vector3 worldPos = GridToWorldPosition(_currentGridPosition);
            transform.position = worldPos + _pivotOffset;
        }

        private Vector3 GridToWorldPosition(Vector3Int gridPos)
        {
            // Simple conversion - assumes 1 unit per grid cell
            return new Vector3(gridPos.x, gridPos.y, gridPos.z);
        }

        private void UpdatePreviewAppearance()
        {
            // Update visual appearance for preview mode
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (_isPreviewMode)
                {
                    // Semi-transparent for preview
                    var color = renderer.material.color;
                    color.a = 0.5f;
                    renderer.material.color = color;
                }
                else
                {
                    // Opaque when placed
                    var color = renderer.material.color;
                    color.a = 1f;
                    renderer.material.color = color;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Placeable types
    /// </summary>
    public enum PlaceableType
    {
        Structure,
        Equipment,
        Plant,
        Utility
    }

    /// <summary>
    /// Placement data
    /// </summary>
    [System.Serializable]
    public struct PlacementData
    {
        public Vector3Int GridPosition;
        public Vector3 WorldPosition;
        public Quaternion Rotation;
        public Vector3Int GridSize;
        public PlaceableType Type;
        public bool IsPlaced;
    }
}
