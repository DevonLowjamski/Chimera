using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Data.Construction;
using ProjectChimera.Core.Logging;

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
                ChimeraLogger.Log("OTHER", "Blueprint overlay renderer initialized", this);
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
                    ChimeraLogger.Log("OTHER", $"Created overlay for {targetObject.name}", this);
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
                    ChimeraLogger.Log("OTHER", $"Removed overlay from {targetObject.name}", this);
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
                    ChimeraLogger.Log("OTHER", $"Updated overlay validity for {targetObject.name}: {isValid}", this);
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
                ChimeraLogger.Log("OTHER", "Cleared all overlays", this);
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
                ChimeraLogger.Log("OTHER", $"Rendering enabled: {enabled}", this);
            }
        }

        /// <summary>
        /// Create schematic overlay with position and rotation
        /// </summary>
        public OverlayInstance CreateSchematicOverlay(SchematicSO schematic, Vector3 position, Quaternion rotation, OverlayType overlayType = OverlayType.Blueprint)
        {
            if (!_enableRendering || !_isInitialized || schematic == null) return null;

            // Create a visual representation of the schematic
            var overlayObject = new GameObject($"SchematicOverlay_{schematic.name}");
            overlayObject.transform.position = position;
            overlayObject.transform.rotation = rotation;

            // Add visual components based on schematic
            var renderer = overlayObject.AddComponent<MeshRenderer>();
            var meshFilter = overlayObject.AddComponent<MeshFilter>();

            // Use a simple cube mesh for now - could be enhanced with schematic-specific meshes
            meshFilter.mesh = CreateSchematicMesh();
            renderer.material = _blueprintMaterial;

            var overlayInstance = new OverlayInstance
            {
                TargetObject = overlayObject,
                OriginalMaterial = _blueprintMaterial,
                IsValidPlacement = true
            };

            _activeOverlays[overlayObject] = _blueprintMaterial;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", $"Created schematic overlay for {schematic.name}", this);
            }

            return overlayInstance;
        }

        /// <summary>
        /// Move existing overlay to new position and rotation
        /// </summary>
        public void MoveOverlay(OverlayInstance overlay, Vector3 newPosition, Quaternion newRotation, bool isValidPlacement)
        {
            if (overlay?.TargetObject == null) return;

            overlay.TargetObject.transform.position = newPosition;
            overlay.TargetObject.transform.rotation = newRotation;
            overlay.IsValidPlacement = isValidPlacement;

            // Update material based on validity
            var renderer = overlay.TargetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = isValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", $"Moved overlay to {newPosition}", this);
            }
        }

        /// <summary>
        /// Update overlay validation state
        /// </summary>
        public void UpdateOverlayValidation(OverlayInstance overlay, bool isValid)
        {
            if (overlay?.TargetObject == null) return;

            overlay.IsValidPlacement = isValid;
            var renderer = overlay.TargetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = isValid ? _validPlacementMaterial : _invalidPlacementMaterial;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", $"Updated overlay validation: {isValid}", this);
            }
        }

        /// <summary>
        /// Destroy overlay instance
        /// </summary>
        public void DestroyOverlay(OverlayInstance overlay)
        {
            if (overlay?.TargetObject == null) return;

            if (_activeOverlays.ContainsKey(overlay.TargetObject))
            {
                _activeOverlays.Remove(overlay.TargetObject);
            }

            if (overlay.TargetObject != null)
            {
                DestroyImmediate(overlay.TargetObject);
            }

            OnOverlayDestroyed?.Invoke(overlay.TargetObject);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "Destroyed overlay instance", this);
            }
        }

        /// <summary>
        /// Get blueprint material property
        /// </summary>
        public Material BlueprintMaterial => _blueprintMaterial;

        /// <summary>
        /// Get overlay camera (placeholder for compatibility)
        /// </summary>
        public UnityEngine.Camera OverlayCamera { get; set; }

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

        private Mesh CreateSchematicMesh()
        {
            // Create a simple cube mesh for schematic visualization
            var mesh = new Mesh();

            Vector3[] vertices = new Vector3[8]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };

            int[] triangles = new int[36]
            {
                0, 2, 1, 0, 3, 2, // front
                1, 6, 5, 1, 2, 6, // right
                5, 7, 4, 5, 6, 7, // back
                4, 3, 0, 4, 7, 3, // left
                3, 6, 2, 3, 7, 6, // top
                0, 5, 4, 0, 1, 5  // bottom
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
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

    /// <summary>
    /// Overlay type enumeration
    /// </summary>
    public enum OverlayType
    {
        Blueprint,
        Preview,
        Selection,
        Highlight
    }
}
