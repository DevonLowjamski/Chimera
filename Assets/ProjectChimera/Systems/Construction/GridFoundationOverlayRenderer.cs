using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// BASIC: Simple grid foundation overlay renderer for Project Chimera's construction system.
    /// Focuses on essential foundation visualization without complex clearance and access path systems.
    /// </summary>
    public class GridFoundationOverlayRenderer : MonoBehaviour
    {
        [Header("Basic Foundation Settings")]
        [SerializeField] private bool _enableBasicOverlay = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private Color _foundationRequiredColor = Color.yellow;
        [SerializeField] private Color _foundationValidColor = Color.green;
        [SerializeField] private Color _foundationInvalidColor = Color.red;
        [SerializeField] private float _overlayHeight = 0.05f;

        // Basic overlay state
        private readonly List<GameObject> _activeOverlays = new List<GameObject>();
        private Vector3Int _currentGridPosition = Vector3Int.zero;
        private bool _isFoundationRequired = false;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for overlay changes
        /// </summary>
        public event System.Action<Vector3Int, bool> OnFoundationStatusChanged;

        /// <summary>
        /// Initialize basic foundation renderer
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Show foundation overlay at position
        /// </summary>
        public void ShowFoundationOverlay(Vector3Int gridPosition, bool foundationRequired)
        {
            if (!_enableBasicOverlay || !_isInitialized) return;

            // Update position if changed
            if (gridPosition != _currentGridPosition)
            {
                _currentGridPosition = gridPosition;
            }

            // Update requirement status if changed
            if (foundationRequired != _isFoundationRequired)
            {
                _isFoundationRequired = foundationRequired;
                OnFoundationStatusChanged?.Invoke(gridPosition, foundationRequired);
            }

            // Create or update overlay
            UpdateFoundationOverlay();
        }

        /// <summary>
        /// Hide foundation overlay
        /// </summary>
        public void HideFoundationOverlay()
        {
            ClearAllOverlays();

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Check if foundation is required at position
        /// </summary>
        public bool IsFoundationRequired(Vector3Int gridPosition)
        {
            // Simple foundation requirement check - could be expanded based on building type
            // For basic implementation, assume foundations are required for multi-level buildings
            return gridPosition.y > 0; // Require foundation if above ground level
        }

        /// <summary>
        /// Validate foundation at position
        /// </summary>
        public bool ValidateFoundation(Vector3Int gridPosition)
        {
            if (!IsFoundationRequired(gridPosition))
                return true; // No foundation needed

            // Basic validation - check if there's a foundation below
            // This would need to check actual game state in a real implementation
            return false; // Assume foundation is missing for basic implementation
        }

        /// <summary>
        /// Get foundation status at position
        /// </summary>
        public FoundationStatus GetFoundationStatus(Vector3Int gridPosition)
        {
            bool required = IsFoundationRequired(gridPosition);
            bool valid = ValidateFoundation(gridPosition);

            return new FoundationStatus
            {
                Position = gridPosition,
                IsRequired = required,
                IsValid = valid,
                StatusColor = GetStatusColor(required, valid)
            };
        }

        /// <summary>
        /// Get overlay statistics
        /// </summary>
        public FoundationOverlayStats GetStats()
        {
            return new FoundationOverlayStats
            {
                ActiveOverlays = _activeOverlays.Count,
                CurrentPosition = _currentGridPosition,
                IsFoundationRequired = _isFoundationRequired,
                IsOverlayEnabled = _enableBasicOverlay,
                IsInitialized = _isInitialized
            };
        }

        #region Private Methods

        private void UpdateFoundationOverlay()
        {
            ClearAllOverlays();

            if (!_isFoundationRequired) return;

            // Create foundation overlay
            GameObject overlay = CreateFoundationOverlay(_currentGridPosition);
            _activeOverlays.Add(overlay);
        }

        private GameObject CreateFoundationOverlay(Vector3Int gridPosition)
        {
            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlay.name = $"FoundationOverlay_{gridPosition.x}_{gridPosition.y}_{gridPosition.z}";

            // Position the overlay
            Vector3 worldPos = GridToWorld(gridPosition);
            overlay.transform.position = worldPos;

            // Scale the overlay (slightly smaller than grid cell)
            overlay.transform.localScale = new Vector3(0.9f, _overlayHeight, 0.9f);

            // Set color based on validation
            bool isValid = ValidateFoundation(gridPosition);
            var renderer = overlay.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetStatusColor(true, isValid);
            }

            // Make it not collide
            var collider = overlay.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return overlay;
        }

        private void ClearAllOverlays()
        {
            foreach (var overlay in _activeOverlays)
            {
                if (overlay != null)
                {
                    Destroy(overlay);
                }
            }

            _activeOverlays.Clear();
        }

        private Color GetStatusColor(bool required, bool valid)
        {
            if (!required) return _foundationValidColor;
            return valid ? _foundationValidColor : _foundationInvalidColor;
        }

        private Vector3 GridToWorld(Vector3Int gridPos)
        {
            // Simple conversion - assuming 1 unit = 1 grid cell
            return new Vector3(gridPos.x, gridPos.y - _overlayHeight / 2f, gridPos.z);
        }

        #endregion
    }

    /// <summary>
    /// Foundation status data
    /// </summary>
    [System.Serializable]
    public struct FoundationStatus
    {
        public Vector3Int Position;
        public bool IsRequired;
        public bool IsValid;
        public Color StatusColor;
    }

    /// <summary>
    /// Foundation overlay statistics
    /// </summary>
    [System.Serializable]
    public struct FoundationOverlayStats
    {
        public int ActiveOverlays;
        public Vector3Int CurrentPosition;
        public bool IsFoundationRequired;
        public bool IsOverlayEnabled;
        public bool IsInitialized;
    }
}
