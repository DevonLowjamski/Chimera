using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// BASIC: Simple grid placement preview renderer for Project Chimera's construction system.
    /// Focuses on essential placement preview without complex ghost objects and grid visualization.
    /// </summary>
    public class GridPlacementPreviewRenderer : MonoBehaviour
    {
        [Header("Basic Preview Settings")]
        [SerializeField] private bool _enableBasicPreview = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private Color _validPlacementColor = Color.green;
        [SerializeField] private Color _invalidPlacementColor = Color.red;
        [SerializeField] private float _previewHeightOffset = 0.1f;

        // Basic preview state
        private GameObject _currentPreviewObject;
        private Vector3Int _currentGridPosition;
        private bool _isValidPlacement = false;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for preview changes
        /// </summary>
        public event System.Action<Vector3Int, bool> OnPreviewPositionChanged;

        /// <summary>
        /// Initialize basic preview renderer
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[GridPlacementPreviewRenderer] Initialized successfully");
            }
        }

        /// <summary>
        /// Show placement preview at grid position
        /// </summary>
        public void ShowPreview(Vector3Int gridPosition, bool isValid)
        {
            if (!_enableBasicPreview || !_isInitialized) return;

            // Update position if changed
            if (gridPosition != _currentGridPosition)
            {
                _currentGridPosition = gridPosition;
                OnPreviewPositionChanged?.Invoke(gridPosition, isValid);
            }

            // Update validity if changed
            if (isValid != _isValidPlacement)
            {
                _isValidPlacement = isValid;
            }

            // Create or update preview object
            UpdatePreviewObject();

            if (_enableLogging && Random.value < 0.01f) // Log occasionally
            {
                ChimeraLogger.Log($"[GridPlacementPreviewRenderer] Preview at {gridPosition}, valid: {isValid}");
            }
        }

        /// <summary>
        /// Hide placement preview
        /// </summary>
        public void HidePreview()
        {
            if (_currentPreviewObject != null)
            {
                _currentPreviewObject.SetActive(false);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("[GridPlacementPreviewRenderer] Preview hidden");
            }
        }

        /// <summary>
        /// Clear all preview objects
        /// </summary>
        public void ClearPreview()
        {
            if (_currentPreviewObject != null)
            {
                Destroy(_currentPreviewObject);
                _currentPreviewObject = null;
            }

            _currentGridPosition = Vector3Int.zero;
            _isValidPlacement = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[GridPlacementPreviewRenderer] Preview cleared");
            }
        }

        /// <summary>
        /// Set preview enabled state
        /// </summary>
        public void SetPreviewEnabled(bool enabled)
        {
            _enableBasicPreview = enabled;

            if (!enabled)
            {
                HidePreview();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[GridPlacementPreviewRenderer] Preview {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get current preview position
        /// </summary>
        public Vector3Int GetCurrentPreviewPosition()
        {
            return _currentGridPosition;
        }

        /// <summary>
        /// Check if current placement is valid
        /// </summary>
        public bool IsCurrentPlacementValid()
        {
            return _isValidPlacement;
        }

        /// <summary>
        /// Get preview statistics
        /// </summary>
        public PreviewStats GetStats()
        {
            return new PreviewStats
            {
                CurrentPosition = _currentGridPosition,
                IsValidPlacement = _isValidPlacement,
                HasPreviewObject = _currentPreviewObject != null,
                IsPreviewEnabled = _enableBasicPreview,
                IsInitialized = _isInitialized
            };
        }

        #region Private Methods

        private void UpdatePreviewObject()
        {
            if (_currentPreviewObject == null)
            {
                _currentPreviewObject = CreatePreviewObject();
            }

            // Position the preview
            Vector3 worldPos = GridToWorld(_currentGridPosition);
            _currentPreviewObject.transform.position = worldPos;

            // Update color based on validity
            UpdatePreviewColor();
        }

        private GameObject CreatePreviewObject()
        {
            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "PlacementPreview";
            preview.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f); // Thin cube

            // Add transparent material
            var renderer = preview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateTransparentMaterial();
            }

            // Make it not collide
            var collider = preview.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return preview;
        }

        private Material CreateTransparentMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
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

        private void UpdatePreviewColor()
        {
            if (_currentPreviewObject == null) return;

            var renderer = _currentPreviewObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = _isValidPlacement ? _validPlacementColor : _invalidPlacementColor;
                renderer.material.color = color;
            }
        }

        private Vector3 GridToWorld(Vector3Int gridPos)
        {
            // Simple conversion - assuming 1 unit = 1 grid cell
            return new Vector3(gridPos.x, gridPos.y + _previewHeightOffset, gridPos.z);
        }

        #endregion
    }

    /// <summary>
    /// Preview statistics
    /// </summary>
    [System.Serializable]
    public struct PreviewStats
    {
        public Vector3Int CurrentPosition;
        public bool IsValidPlacement;
        public bool HasPreviewObject;
        public bool IsPreviewEnabled;
        public bool IsInitialized;
    }
}
