using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Simple Equipment Placement Manager - Aligned with Project Chimera's vision
    /// Provides basic equipment placement as described in gameplay document
    /// Focuses on simple placement of lights, HVAC, irrigation systems in construction mode
    /// </summary>
    public class SimpleEquipmentPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private float _gridSize = 0.5f;
        [SerializeField] private LayerMask _placementLayerMask;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private bool _showPlacementPreview = true;

        [Header("Equipment Categories")]
        [SerializeField] private List<EquipmentCategory> _equipmentCategories = new List<EquipmentCategory>();

        // Placement state
        private EquipmentDataSO _selectedEquipment;
        private GameObject _placementPreview;
        private bool _isPlacingEquipment = false;
        private Vector3 _currentPlacementPosition;
        private Quaternion _currentPlacementRotation = Quaternion.identity;

        // Placed equipment tracking
        private Dictionary<string, PlacedEquipment> _placedEquipment = new Dictionary<string, PlacedEquipment>();
        private int _nextEquipmentId = 1;

        private void Update()
        {
            HandlePlacementInput();
            UpdatePlacementPreview();
        }

        /// <summary>
        /// Starts placing equipment of the specified type
        /// </summary>
        public void StartPlacingEquipment(EquipmentDataSO equipmentData)
        {
            if (equipmentData == null)
            {
                ChimeraLogger.LogWarning("[SimpleEquipmentPlacementManager] No equipment data provided");
                return;
            }

            _selectedEquipment = equipmentData;
            _isPlacingEquipment = true;

            CreatePlacementPreview();
            ChimeraLogger.Log($"[SimpleEquipmentPlacementManager] Started placing: {equipmentData.EquipmentName}");
        }

        /// <summary>
        /// Cancels the current equipment placement
        /// </summary>
        public void CancelPlacement()
        {
            _isPlacingEquipment = false;
            _selectedEquipment = null;
            DestroyPlacementPreview();

            ChimeraLogger.Log("[SimpleEquipmentPlacementManager] Placement cancelled");
        }

        /// <summary>
        /// Confirms the current equipment placement
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!_isPlacingEquipment || _selectedEquipment == null)
            {
                return false;
            }

            // Check if placement is valid
            if (!IsValidPlacementPosition(_currentPlacementPosition))
            {
                ChimeraLogger.LogWarning("[SimpleEquipmentPlacementManager] Invalid placement position");
                return false;
            }

            // Check if player can afford the equipment
            if (!CanAffordEquipment(_selectedEquipment))
            {
                ChimeraLogger.LogWarning("[SimpleEquipmentPlacementManager] Cannot afford equipment");
                return false;
            }

            // Place the equipment
            PlaceEquipment(_selectedEquipment, _currentPlacementPosition, _currentPlacementRotation);

            // Reset placement state
            CancelPlacement();

            return true;
        }

        /// <summary>
        /// Handles input for equipment placement
        /// </summary>
        private void HandlePlacementInput()
        {
            if (!_isPlacingEquipment) return;

            // Handle mouse input for placement position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _placementLayerMask))
            {
                Vector3 rawPosition = hit.point;

                // Snap to grid if enabled
                if (_snapToGrid)
                {
                    rawPosition = SnapToGrid(rawPosition);
                }

                _currentPlacementPosition = rawPosition;

                // Handle rotation with keyboard input
                if (Input.GetKeyDown(KeyCode.R))
                {
                    _currentPlacementRotation *= Quaternion.Euler(0, 90, 0);
                }
            }

            // Handle placement confirmation
            if (Input.GetMouseButtonDown(0))
            {
                ConfirmPlacement();
            }

            // Handle placement cancellation
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Updates the placement preview
        /// </summary>
        private void UpdatePlacementPreview()
        {
            if (!_isPlacingEquipment || _placementPreview == null) return;

            _placementPreview.transform.position = _currentPlacementPosition;
            _placementPreview.transform.rotation = _currentPlacementRotation;

            // Update preview color based on validity
            bool isValid = IsValidPlacementPosition(_currentPlacementPosition);
            SetPreviewColor(isValid ? Color.green : Color.red);
        }

        /// <summary>
        /// Creates a placement preview object
        /// </summary>
        private void CreatePlacementPreview()
        {
            if (_selectedEquipment == null || !_showPlacementPreview) return;

            _placementPreview = Instantiate(_selectedEquipment.EquipmentPrefab);
            _placementPreview.name = $"{_selectedEquipment.EquipmentName}_Preview";

            // Make preview semi-transparent
            SetPreviewTransparency(0.5f);

            // Disable colliders on preview
            var colliders = _placementPreview.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }

        /// <summary>
        /// Destroys the placement preview
        /// </summary>
        private void DestroyPlacementPreview()
        {
            if (_placementPreview != null)
            {
                Destroy(_placementPreview);
                _placementPreview = null;
            }
        }

        /// <summary>
        /// Places equipment at the specified position
        /// </summary>
        private void PlaceEquipment(EquipmentDataSO equipmentData, Vector3 position, Quaternion rotation)
        {
            // Instantiate the equipment
            GameObject equipmentObject = Instantiate(equipmentData.EquipmentPrefab, position, rotation);
            equipmentObject.name = $"{equipmentData.EquipmentName}_{_nextEquipmentId}";

            // Create placed equipment record
            var placedEquipment = new PlacedEquipment
            {
                EquipmentID = $"equipment_{_nextEquipmentId}",
                EquipmentData = equipmentData,
                GameObject = equipmentObject,
                Position = position,
                Rotation = rotation,
                PlacementDate = System.DateTime.Now,
                IsActive = true
            };

            _placedEquipment[placedEquipment.EquipmentID] = placedEquipment;
            _nextEquipmentId++;

            // Deduct cost from player economy (would integrate with economy system)
            DeductEquipmentCost(equipmentData);

            ChimeraLogger.Log($"[SimpleEquipmentPlacementManager] Placed {equipmentData.EquipmentName} at {position}");
        }

        /// <summary>
        /// Removes equipment by ID
        /// </summary>
        public bool RemoveEquipment(string equipmentId)
        {
            if (!_placedEquipment.TryGetValue(equipmentId, out var equipment))
            {
                return false;
            }

            // Destroy the game object
            if (equipment.GameObject != null)
            {
                Destroy(equipment.GameObject);
            }

            // Remove from tracking
            _placedEquipment.Remove(equipmentId);

            ChimeraLogger.Log($"[SimpleEquipmentPlacementManager] Removed equipment: {equipmentId}");
            return true;
        }

        /// <summary>
        /// Checks if the placement position is valid
        /// </summary>
        private bool IsValidPlacementPosition(Vector3 position)
        {
            // Basic validation - check if position is on a valid surface
            // In a real implementation, this would check for:
            // - Valid floor/ground surface
            // - Sufficient clearance from other objects
            // - Room boundaries
            // - Equipment-specific requirements

            // For now, just check if we're above ground
            return position.y > -0.1f;
        }

        /// <summary>
        /// Snaps position to grid
        /// </summary>
        private Vector3 SnapToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / _gridSize) * _gridSize,
                position.y,
                Mathf.Round(position.z / _gridSize) * _gridSize
            );
        }

        /// <summary>
        /// Sets the transparency of the placement preview
        /// </summary>
        private void SetPreviewTransparency(float alpha)
        {
            if (_placementPreview == null) return;

            var renderers = _placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                var color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        /// <summary>
        /// Sets the color of the placement preview
        /// </summary>
        private void SetPreviewColor(Color color)
        {
            if (_placementPreview == null) return;

            var renderers = _placementPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                color.a = material.color.a; // Preserve transparency
                material.color = color;
            }
        }

        /// <summary>
        /// Checks if player can afford the equipment
        /// </summary>
        private bool CanAffordEquipment(EquipmentDataSO equipment)
        {
            // Would integrate with economy system
            // For now, assume player can afford
            return true;
        }

        /// <summary>
        /// Deducts equipment cost from player economy
        /// </summary>
        private void DeductEquipmentCost(EquipmentDataSO equipment)
        {
            // Would integrate with economy system
            ChimeraLogger.Log($"[SimpleEquipmentPlacementManager] Deducted {equipment.Cost} currency for {equipment.EquipmentName}");
        }

        /// <summary>
        /// Gets all placed equipment
        /// </summary>
        public List<PlacedEquipment> GetPlacedEquipment()
        {
            return new List<PlacedEquipment>(_placedEquipment.Values);
        }

        /// <summary>
        /// Gets equipment by ID
        /// </summary>
        public PlacedEquipment? GetEquipment(string equipmentId)
        {
            if (_placedEquipment.TryGetValue(equipmentId, out var equipment))
            {
                return equipment;
            }
            return null;
        }

        /// <summary>
        /// Gets equipment count by category
        /// </summary>
        public int GetEquipmentCountByCategory(EquipmentCategory category)
        {
            return _placedEquipment.Values.Count(e => e.EquipmentData.Category == category);
        }

        /// <summary>
        /// Checks if equipment is currently being placed
        /// </summary>
        public bool IsPlacingEquipment()
        {
            return _isPlacingEquipment;
        }

        /// <summary>
        /// Gets the currently selected equipment
        /// </summary>
        public EquipmentDataSO GetSelectedEquipment()
        {
            return _selectedEquipment;
        }

        /// <summary>
        /// Gets total equipment count
        /// </summary>
        public int GetTotalEquipmentCount()
        {
            return _placedEquipment.Count;
        }
    }

    /// <summary>
    /// Equipment category enum
    /// </summary>
    public enum EquipmentCategory
    {
        Lighting,
        HVAC,
        Irrigation,
        Monitoring,
        Safety,
        Other
    }

    /// <summary>
    /// Placed equipment data structure
    /// </summary>
    [System.Serializable]
    public class PlacedEquipment
    {
        public string EquipmentID;
        public EquipmentDataSO EquipmentData;
        public GameObject GameObject;
        public Vector3 Position;
        public Quaternion Rotation;
        public System.DateTime PlacementDate;
        public bool IsActive;
    }
}

