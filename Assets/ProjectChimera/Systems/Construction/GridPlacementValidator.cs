using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Grid Placement Validator - Handles placement validation and collision detection
    /// Validates placement positions and ensures valid placement conditions
    /// </summary>
    public class GridPlacementValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private LayerMask _groundLayers = 1 << 0;
        [SerializeField] private float _clearanceRadius = 1.0f;
        [SerializeField] private bool _requireStableGround = true;

        /// <summary>
        /// Validates if an object can be placed at the specified position
        /// </summary>
        public ValidationResult ValidatePlacement(GameObject obj, Vector3 position, Quaternion rotation)
        {
            var result = new ValidationResult();
            result.IsValid = true;

            // Get object bounds
            var bounds = GetObjectBounds(obj, position, rotation);

            // Check ground stability
            if (_requireStableGround && !IsGroundStable(position, bounds))
            {
                result.IsValid = false;
                result.ErrorMessage = "Unstable ground";
                return result;
            }

            // Check for collisions
            if (HasCollisions(bounds, obj))
            {
                result.IsValid = false;
                result.ErrorMessage = "Collision detected";
                return result;
            }

            result.ErrorMessage = "Valid placement";
            return result;
        }

        private Bounds GetObjectBounds(GameObject obj, Vector3 position, Quaternion rotation)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private bool IsGroundStable(Vector3 position, Bounds bounds)
        {
            Vector3 checkPosition = new Vector3(bounds.center.x, position.y - 0.1f, bounds.center.z);
            Vector3 checkSize = new Vector3(bounds.size.x, 0.2f, bounds.size.z);
            return Physics.CheckBox(checkPosition, checkSize / 2, Quaternion.identity, _groundLayers);
        }

        private bool HasCollisions(Bounds bounds, GameObject obj)
        {
            var colliders = Physics.OverlapBox(bounds.center, bounds.size / 2, Quaternion.identity, _collisionLayers);
            foreach (var collider in colliders)
            {
                if (collider.gameObject != obj && !collider.isTrigger)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Validation result structure
        /// </summary>
        public struct ValidationResult
        {
            public bool IsValid;
            public string ErrorMessage;
        }
    }
}
