using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// SIMPLE: Basic blueprint overlay renderer aligned with Project Chimera's construction vision.
    /// Focuses on essential visual feedback for schematic placement.
    /// </summary>
    public class BlueprintOverlayRenderer : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableRendering = true;
        [SerializeField] private Material _blueprintMaterial;
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private bool _enableLogging = true;

        // Basic overlay tracking
        private readonly Dictionary<GameObject, Material> _activeOverlays = new Dictionary<GameObject, Material>();
        private bool _isInitialized = false;

        // Events
        public System.Action<GameObject> OnOverlayCreated;
        public System.Action<GameObject> OnOverlayDestroyed;

        /// <summary>
        /// Initialize the overlay renderer
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Ensure materials exist
            if (_blueprintMaterial == null)
            {
                _blueprintMaterial = CreateBasicMaterial(Color.blue);
            }

            if (_validPlacementMaterial == null)
            {
                _validPlacementMaterial = CreateBasicMaterial(Color.green);
            }

            if (_invalidPlacementMaterial == null)
            {
                _invalidPlacementMaterial = CreateBasicMaterial(Color.red);
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log("[BlueprintOverlayRenderer] Initialized successfully");
            }
        }

        /// <summary>
        /// Create overlay for a game object
        /// </summary>
        public void CreateOverlay(GameObject targetObject, bool isValidPlacement = true)
        {
            if (!_enableRendering || !_isInitialized || targetObject == null) return;

            // Remove existing overlay if present
            RemoveOverlay(targetObject);

            // Create new overlay
            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var originalMaterials = renderer.materials;
                var overlayMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    overlayMaterials[i] = isValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial;
                }

                renderer.materials = overlayMaterials;
                _activeOverlays[targetObject] = originalMaterials[0]; // Store original for restoration

                OnOverlayCreated?.Invoke(targetObject);

                if (_enableLogging)
                {
                    Debug.Log($"[BlueprintOverlayRenderer] Created overlay for {targetObject.name}");
                }
            }
        }

        /// <summary>
        /// Remove overlay from a game object
        /// </summary>
        public void RemoveOverlay(GameObject targetObject)
        {
            if (targetObject == null) return;

            if (_activeOverlays.TryGetValue(targetObject, out var originalMaterial))
            {
                var renderer = targetObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Restore original material
                    renderer.material = originalMaterial;
                }

                _activeOverlays.Remove(targetObject);
                OnOverlayDestroyed?.Invoke(targetObject);

                if (_enableLogging)
                {
                    Debug.Log($"[BlueprintOverlayRenderer] Removed overlay from {targetObject.name}");
                }
            }
        }

        /// <summary>
        /// Update overlay placement validity
        /// </summary>
        public void UpdateOverlayValidity(GameObject targetObject, bool isValid)
        {
            if (!_enableRendering || !_isInitialized || targetObject == null) return;

            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = isValid ? _validPlacementMaterial : _invalidPlacementMaterial;

                if (_enableLogging)
                {
                    Debug.Log($"[BlueprintOverlayRenderer] Updated validity for {targetObject.name}: {isValid}");
                }
            }
        }

        /// <summary>
        /// Clear all overlays
        /// </summary>
        public void ClearAllOverlays()
        {
            var objectsToRemove = new List<GameObject>(_activeOverlays.Keys);

            foreach (var obj in objectsToRemove)
            {
                RemoveOverlay(obj);
            }

            if (_enableLogging)
            {
                Debug.Log($"[BlueprintOverlayRenderer] Cleared all overlays ({objectsToRemove.Count} objects)");
            }
        }

        /// <summary>
        /// Get active overlay count
        /// </summary>
        public int GetActiveOverlayCount()
        {
            return _activeOverlays.Count;
        }

        /// <summary>
        /// Check if object has overlay
        /// </summary>
        public bool HasOverlay(GameObject targetObject)
        {
            return targetObject != null && _activeOverlays.ContainsKey(targetObject);
        }

        /// <summary>
        /// Set rendering enabled/disabled
        /// </summary>
        public void SetRenderingEnabled(bool enabled)
        {
            _enableRendering = enabled;

            if (!enabled)
            {
                ClearAllOverlays();
            }

            if (_enableLogging)
            {
                Debug.Log($"[BlueprintOverlayRenderer] Rendering enabled: {enabled}");
            }
        }

        #region Private Methods

        private Material CreateBasicMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(color.r, color.g, color.b, 0.5f); // Semi-transparent
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        #endregion
    }

    /// <summary>
    /// Basic overlay instance (placeholder for compatibility)
    /// </summary>
    public class OverlayInstance
    {
        public GameObject TargetObject;
        public Material OriginalMaterial;
        public bool IsValidPlacement;
    }
}
